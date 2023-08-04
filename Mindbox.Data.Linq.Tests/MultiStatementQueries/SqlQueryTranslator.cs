using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries;

class SqlQueryTranslator
{
    public static SqlQueryTranslatorResult Transalate(Expression node, IDbColumnTypeProvider columnTypeProvider)
    {
        var table = TranslateCore(node, columnTypeProvider);
        OptimizeTree(table);

        var command = SqlTreeCommandBuilder.Build(table, columnTypeProvider);

        return new SqlQueryTranslatorResult(command);
    }

    private static TableNode2 TranslateCore(Expression expression, IDbColumnTypeProvider columnTypeProvider)
    {
        var rootSle = TranslateToSimplifiedExpression(expression);
        var context = new TranslationContext(columnTypeProvider);
        TranslateChain(context, rootSle);

        // Ensure that all tables are port of join
        foreach (var table in context.Node2ChainParts.Keys)
            if (!context.RootTable.GetAllTableNodes().Contains(table))
                throw new InvalidOperationException($"No connection condition was found for table {table.Name}.");

        return context.RootTable;
    }

    private static void TranslateChain(TranslationContext context, ChainSle chain)
    {
        TableNode2 currentTable = null!;
        foreach (var chainItem in chain.Items)
            if (chainItem is TableChainPart tableChainPart)
            {
                currentTable = new TableNode2(tableChainPart.Name);
                context.AddChainPartForNode(currentTable, tableChainPart);
                if (context.RootTable == null)
                    context.RootTable = currentTable;
            }
            else if (chainItem is SelectChainPart selectChainPart)
            {
                throw new NotSupportedException();
            }
            else if (chainItem is AssociationChainPart associationChainPart)
            {
                var associationTable = new TableNode2(associationChainPart.NextTableName);
                currentTable.AddConnection(new[] { associationChainPart.ColumnName }, associationTable, new string[] { associationChainPart.NextTableColumnName });
                currentTable = associationTable;
            }
            else if (chainItem is FilterChainPart filterChainPart)
            {
                if (filterChainPart.InnerExpression is ChainSle filterChainInnerExpressionAsChain)
                    TranslateChain(context, filterChainInnerExpressionAsChain);
                else
                    TranslateTree(context, (ITreeNodeSle)filterChainPart.InnerExpression);
            }
            else if (chainItem is ReferenceRowSourceChainPart referenceRowSourceChainPart)
            {
                if (currentTable != null)
                    throw new InvalidOperationException();
                var tableNodeByReference = context.GetTableNodeByTablePart((TableChainPart)referenceRowSourceChainPart.ReferenceRowSource);
                if (tableNodeByReference == null)
                    throw new InvalidOperationException();
                currentTable = tableNodeByReference;
                continue;
            }
            else if (chainItem is ColumnAccessChainPart columnAccessChainPart)
            {
                currentTable.AddField(columnAccessChainPart.ColumnName);
            }
            else
                throw new NotSupportedException();

    }

    private static void TranslateTree(TranslationContext context, ITreeNodeSle sle)
    {
        if (sle is FilterBinarySle filterBinary)
        {
            if (IsOperatorAgainstConstant(filterBinary, out var nonConstantSle))
            {
                if (nonConstantSle is ChainSle nonConstantChainSle)
                    TranslateChain(context, nonConstantChainSle);
                else if (nonConstantSle is ITreeNodeSle treeNodeSle)
                    TranslateTree(context, treeNodeSle);
                else
                    throw new NotSupportedException();
            }
            else
            {
                TranslateCore(context, filterBinary.LeftExpression);
                TranslateCore(context, filterBinary.RightExpression);
            }
            DetectConnections(context, filterBinary);
        }
        else
            throw new NotSupportedException();



        static void DetectConnections(TranslationContext context, FilterBinarySle filterBinary)
        {
            // Processing only simple join conditions, like c.Id == b.CustomerId
            if (!filterBinary.IsTopLevelChainEqualityStatement())
                return;
            var left = GetTableAndField(context, filterBinary.LeftExpression);
            var right = GetTableAndField(context, filterBinary.RightExpression);

            if (!left.HasValue || !right.HasValue)
                return;
            if (context.RootTable.GetAllTableNodes().Contains(left.Value.Table))
            {
                var existingConnection = left.Value.Table.Connections.FirstOrDefault(c => c.OtherTable == right.Value.Table);
                if (existingConnection != null)
                    existingConnection.AddFields(left.Value.Field, right.Value.Field);
                else
                    left.Value.Table.AddConnection(new[] { left.Value.Field }, right.Value.Table, new[] { right.Value.Field });
            }
            else if (context.RootTable.GetAllTableNodes().Contains(right.Value.Table))
            {
                var existingConnection = right.Value.Table.Connections.FirstOrDefault(c => c.OtherTable == left.Value.Table);
                if (existingConnection != null)
                    existingConnection.AddFields(right.Value.Field, left.Value.Field);
                else
                    right.Value.Table.AddConnection(new[] { right.Value.Field }, left.Value.Table, new[] { left.Value.Field });
            }
            else
            {
                throw new NotImplementedException();
            }

        }

        static void TranslateCore(TranslationContext context, ISimplifiedLinqExpression expression)
        {
            if (expression is FilterBinarySle filterBinary)
                TranslateTree(context, filterBinary);
            else
                TranslateChain(context, (ChainSle)expression);
        }
    }

    private static (TableNode2 Table, string Field)? GetTableAndField(TranslationContext context, ISimplifiedLinqExpression sle)
    {
        // So far only [variable].[fieldName] parsing is supported
        if (sle is not ChainSle chain)
            return null;
        switch (chain.Items.Count)
        {
            case 1:
                if (chain.Items[0] is ReferenceRowSourceChainPart variableAsReference)
                    return (GetTableNode(context, variableAsReference), context.ColumnTypeProvider.GetPKFields(GetTableNode(context, variableAsReference).Name).Single());
                return null;
            case 2:
                if (chain.Items[0] is not ReferenceRowSourceChainPart referenceChainPart)
                    return null;
                if (chain.Items[1] is ColumnAccessChainPart columnAccessChain)
                    return (GetTableNode(context, referenceChainPart), columnAccessChain.ColumnName);
                else if (chain.Items[1] is AssociationChainPart associationChainPart)
                    return (GetTableNode(context, referenceChainPart), associationChainPart.ColumnName);
                return null;
            default:
                return null;
        }

        static TableNode2 GetTableNode(TranslationContext context, ReferenceRowSourceChainPart referenceRowSourceChainPart)
        {
            var tableNodeReference = context.GetTableNodeByTablePart((TableChainPart)referenceRowSourceChainPart.ReferenceRowSource);
            if (tableNodeReference == null)
                throw new InvalidOperationException();
            return tableNodeReference;
        }
    }

    /// <summary>
    /// Operator against constaint.
    /// We can simply analysis only non constant part and drop constant sle.
    /// Examples: 'c.Password == "asdf"' or 'c.Age > 20' or 'c.CustomerActions.Where(c=>c.ActionTypeId == 12).Count() < 10'
    /// </summary>
    /// <param name="sle"></param>
    /// <param name="nonConstant"></param>
    /// <returns></returns>
    private static bool IsOperatorAgainstConstant(FilterBinarySle sle, out ISimplifiedLinqExpression nonConstant)
    {
        if (sle.LeftExpression is ChainSle leftChain && IsConstant(leftChain))
        {
            nonConstant = sle.RightExpression;
            return true;
        }

        if (sle.RightExpression is ChainSle rightChain && IsConstant(rightChain))
        {
            nonConstant = sle.LeftExpression;
            return true;
        }
        nonConstant = null;
        return false;

        static bool IsConstant(ChainSle chain) => chain.Items.Count == 1 && chain.Items[0] is FixedValueChainPart;
    }

    private static ChainSle TranslateToSimplifiedExpression(Expression expression)
    {
        var visitorContext = new VisitorContext(new DbColumnTypeProvider());
        var visitor = new ChainExpressionVisitor(visitorContext);
        visitor.Visit(expression);
        return visitorContext.Root;
    }

    private static void OptimizeTree(TableNode2 root)
    {
        while (true)
        {
            bool hasOptimization = false;
            foreach (var node in root.GetAllTableNodes())
            {
                hasOptimization = node.OptimizeConnections();
                if (hasOptimization)
                    break;
            }

            if (!hasOptimization)
                break;
        }

    }

    class TranslationContext
    {
        private Dictionary<ISimplifiedLinqExpression, string> _variableNames = new();

        public Dictionary<TableNode2, List<TableChainPart>> Node2ChainParts { get; } = new();
        public Dictionary<Connection, ChainSle> Connection2Chains { get; } = new();
        public IDbColumnTypeProvider ColumnTypeProvider { get; private set; }

        public TableNode2 RootTable { get; set; }

        public TranslationContext(IDbColumnTypeProvider columnTypeProvider)
        {
            ColumnTypeProvider = columnTypeProvider;
        }

        public void AddChainPartForNode(TableNode2 tableNode, TableChainPart chainPart)
        {
            if (!Node2ChainParts.ContainsKey(tableNode))
                Node2ChainParts.Add(tableNode, new List<TableChainPart>());
            Node2ChainParts[tableNode].Add(chainPart);
        }

        public TableNode2 GetTableNodeByTablePart(TableChainPart tableChainPart)
        {
            foreach (var (node, parts) in Node2ChainParts)
                if (parts.Contains(tableChainPart))
                    return node;
            return null;
        }

        public string GetVariableName(ISimplifiedLinqExpression sle, string tableName)
        {
            if (!_variableNames.TryGetValue(sle, out var name))
            {
                name = $"@table{tableName.Replace('.', '_')}";
                if (_variableNames.Values.Contains(name))
                {
                    int counter = 2;
                    while (true)
                    {
                        name = $"@table{tableName.Replace('.', '_')}_{counter++}";
                        if (!_variableNames.Values.Contains(name))
                            break;
                    }
                }
                _variableNames.Add(sle, name);
            }
            return name;
        }
    }


}
/*
class MultiStatementQuery
{
    private List<TableNode> _tables = new();

    public IReadOnlyList<TableNode> Tables => _tables;

    public TableNode AddTable(string rightTableName)
    {
        _tables.Add(new TableNode(rightTableName));
        return _tables.Last();
    }

    public IEnumerable<(TableNode Removed, TableNode ReplacedBy)> OptmizeQuery()
    {
        foreach (var table in _tables.Skip(1))
            if (table.JoinConditions.Count == 0)
                throw new InvalidOperationException("Join conditions missiong for table.");

        return RemoveNeedlessBackLookups().Concat(RemoveDuplicatedJoins());
    }

    private IEnumerable<(TableNode Removed, TableNode ReplacedBy)> RemoveNeedlessBackLookups()
    {
        List<(TableNode Removed, TableNode ReplacedBy)> toReturn = new();
        while (true)
        {
            var toRemoveInfo = GetToRemove();
            if (!toRemoveInfo.HasValue)
                break;
            foreach (var usedColumn in toRemoveInfo.Value.ToRemove.UsedColumns)
                toRemoveInfo.Value.Replacement.AddUsedField(usedColumn);
            _tables.Remove(toRemoveInfo.Value.ToRemove);
            foreach (var table in _tables)
                table.ReplaceTable(toRemoveInfo.Value.ToRemove, toRemoveInfo.Value.Replacement);

            toReturn.Add(toRemoveInfo.Value);
        }
        return toReturn;

        (TableNode ToRemove, TableNode Replacement)? GetToRemove()
        {
            var tablesToCheck = _tables.Where(t => t.JoinConditions.Select(c => c.LeftTable).Distinct().Count() == 1).ToArray();

            for (int i = 0; i < tablesToCheck.Length - 1; i++)
            {
                var replacement = tablesToCheck[i];
                for (int j = i + 1; j < tablesToCheck.Length; j++)
                {
                    var toRemove = tablesToCheck[j];
                    if (!IsSameJoin(replacement, toRemove))
                        continue;

                    if (toRemove.JoinConditions.Select(t => t.LeftTable).Distinct().Single() != replacement)
                        continue;

                    return (toRemove, replacement.JoinConditions.Select(t => t.LeftTable).Distinct().Single());
                }
            }
            return null;
        }

        bool IsSameJoin(TableNode tableA, TableNode tableB)
        {
            var conditionA = tableA.JoinConditions.Single();
            var conditionB = tableB.JoinConditions.Single();

            if (conditionA.LeftTable.TableName != tableB.TableName || conditionB.LeftTable.TableName != tableA.TableName)
                return false;

            if (conditionA.FieldLeft != conditionB.FieldRight || conditionA.FieldRight != conditionB.FieldLeft)
                return false;
            return true;
        }
    }

    private IEnumerable<(TableNode Removed, TableNode ReplacedBy)> RemoveDuplicatedJoins()
    {
        List<(TableNode Removed, TableNode ReplacedBy)> toReturn = new();
        while (true)
        {
            var toRemoveInfo = GetToRemove();
            if (!toRemoveInfo.HasValue)
                break;
            foreach (var usedColumn in toRemoveInfo.Value.ToRemove.UsedColumns)
                toRemoveInfo.Value.Replacement.AddUsedField(usedColumn);
            _tables.Remove(toRemoveInfo.Value.ToRemove);
            foreach (var table in _tables)
                table.ReplaceTable(toRemoveInfo.Value.ToRemove, toRemoveInfo.Value.Replacement);

            toReturn.Add(toRemoveInfo.Value);
        }
        return toReturn;

        (TableNode ToRemove, TableNode Replacement)? GetToRemove()
        {
            for (int i = _tables.Count - 1; i > 0; i--)
            {
                var toRemoveTable = _tables[i];
                for (int j = 0; j < i; j++)
                {
                    var replacementTable = _tables[j];
                    if (toRemoveTable.TableName != replacementTable.TableName)
                        continue;
                    if (toRemoveTable.JoinConditions.Count != replacementTable.JoinConditions.Count)
                        continue;
                    if (toRemoveTable.JoinConditions.Except(replacementTable.JoinConditions).Any() ||
                        replacementTable.JoinConditions.Except(toRemoveTable.JoinConditions).Any())
                        continue;
                    return (toRemoveTable, replacementTable);
                }
            }
            return null;
        }
    }

    public string Dump()
    {
        var sb = new StringBuilder();
        foreach (var table in _tables)
        {
            var columns = string.Empty;
            if (table.UsedColumns.Count > 0)
                columns = $"({string.Join(", ", table.UsedColumns)})";
            sb.AppendLine($"{table.TableName}{columns}");
            if (table.JoinConditions.Count > 0)
                foreach (var condition in table.JoinConditions)
                    sb.AppendLine($"\t{condition.FieldRight} = {condition.LeftTable.TableName}.{condition.FieldLeft}");
        }

        return sb.ToString();
    }
}

[DebuggerDisplay("{TableName}")]
class TableNode
{
    private List<string> _usedColumns = new();
    private List<JoinCondition> _joinConditions = new();

    public string TableName { get; }
    public IReadOnlyList<string> UsedColumns => _usedColumns;
    public IReadOnlyList<JoinCondition> JoinConditions => _joinConditions;

    public TableNode(string tableName)
    {
        TableName = tableName;
    }

    public void AddUsedField(string name)
    {
        if (_usedColumns.Contains(name))
            return;
        _usedColumns.Add(name);
    }

    public void AddJoinCondition(JoinCondition condition)
    {
        if (_joinConditions.Contains(condition))
            throw new ArgumentException("Condition already added.");
        if (condition.LeftTable == this)
            throw new ArgumentException("LeftTable is same as right table in join.");
        _joinConditions.Add(condition);
        condition.LeftTable.AddUsedField(condition.FieldLeft);
        AddUsedField(condition.FieldRight);
    }

    internal void ReplaceTable(TableNode toReplace, TableNode replacement)
    {
        for (int i = 0; i < _joinConditions.Count; i++)
            if (_joinConditions[i].LeftTable == toReplace)
                _joinConditions[i] = new JoinCondition(_joinConditions[i].FieldRight, replacement, _joinConditions[i].FieldLeft);
    }
}

record JoinCondition(string FieldRight, TableNode LeftTable, string FieldLeft);

class TranslationContext
{
    private Dictionary<Expression, TableNode> _expressionToTableMapping = new();
    private Dictionary<TableNode, List<Expression>> _tableFilters = new();

    public MultiStatementQuery Query { get; } = new();
    public TableNode CurrentTable { get; private set; }
    public IDbColumnTypeProvider ColumnTypeProvider { get; }

    public TranslationContext(IDbColumnTypeProvider columnTypeProvider)
    {
        ColumnTypeProvider = columnTypeProvider;
    }

    public TableNode AddTable(string tableName, Expression expression)
    {
        if (expression != null && _expressionToTableMapping.TryGetValue(expression, out var table))
        {
            if (table.TableName != tableName)
                throw new InvalidOperationException("Same expression mapped to different tables. Error in implementation.");
            CurrentTable = table;
        }
        else
        {
            CurrentTable = Query.AddTable(tableName);
            if (expression != null)
                _expressionToTableMapping.Add(expression, CurrentTable);
        }
        return CurrentTable;
    }

    public void AddTableFilter(Expression tableFilter)
    {
        if (!_tableFilters.TryGetValue(CurrentTable, out var filters))
        {
            filters = new List<Expression>();
            _tableFilters.Add(CurrentTable, filters);
        }
        if (!filters.Contains(tableFilter))
            filters.Add(tableFilter);
    }

    public void SetCurrentTable(TableNode table)
    {
        if (Query == null)
            throw new InvalidOperationException("Query not yet constructed.");
        CurrentTable = table;
    }

    public void MapParameterToTable(ParameterExpression parameterExpression)
    {
        if (CurrentTable == null)
            throw new ArgumentException();
        if (_expressionToTableMapping.TryGetValue(parameterExpression, out var tableMapping))
        {
            if (tableMapping != CurrentTable)
                throw new InvalidOperationException("Same expression mapped to different tables. Error in implementation.");
            return;
        }
        _expressionToTableMapping.Add(parameterExpression, CurrentTable);
    }

    public void OptmizeQuery()
    {
        foreach (var entry in Query.OptmizeQuery())
            foreach (var expression in _expressionToTableMapping.Where(i => i.Value == entry.Removed).Select(s => s.Key).ToArray())
            {
                _expressionToTableMapping.Remove(expression);
                _expressionToTableMapping.Add(expression, entry.ReplacedBy);
            }
    }

    public TableNode GetTableFromExpression(Expression parameterExpression)
        => _expressionToTableMapping[parameterExpression];

    public void ResetCurrentTable() => CurrentTable = null;

    internal void FillJoinConditions()
    {
        foreach (var (table, filters) in _tableFilters)
            foreach (var joinCondition in ExpressionHelpers.GetJoinConditions(this, table, filters))
                table.AddJoinCondition(joinCondition);
    }
}
*/

[DebuggerDisplay("{Name}")]
class TableNode2
{
    private List<string> _usedFields = new();
    private List<Connection> _connections = new();

    public string Name { get; private set; }
    public IEnumerable<string> UsedFields => _usedFields;
    public IEnumerable<Connection> Connections => _connections;

    public TableNode2(string name)
    {
        Name = name;
    }

    public void AddField(string name)
    {
        if (_usedFields.Contains(name))
            return;
        _usedFields.Add(name);
        _usedFields.Sort();
    }

    public void AddConnection(IEnumerable<string> fields, TableNode2 otherTable, IEnumerable<string> otherTableFields)
    {
        foreach (var connection in _connections)
        {
            if ((connection.Table == this && connection.TableFields.SequenceEqual(fields) &&
                connection.OtherTable == otherTable && connection.OtherTableFields.SequenceEqual(otherTableFields)) ||
                (connection.Table == otherTable && connection.TableFields.SequenceEqual(otherTableFields) &&
                connection.OtherTable == this && connection.OtherTableFields.SequenceEqual(fields)))
                return;
        }
        foreach (var field in fields)
            AddField(field);
        foreach (var otherField in otherTableFields)
            otherTable.AddField(otherField);

        _connections.Add(new(this, fields, otherTable, otherTableFields));
    }

    public bool OptimizeConnections()
    {
        // Merge duplicated connections
        bool hasChanges = false;
        for (int i = 0; i < _connections.Count - 1; i++)
            for (int j = i; j < _connections.Count; j++)
            {
                var connectionA = _connections[i];
                var connectionB = _connections[j];
                if (connectionA == connectionB)
                    continue;
                if (!connectionA.Equals(connectionB))
                    continue;
                _connections.Remove(connectionB);
                j--;
                hasChanges = true;
                foreach (var connectionBOtherTableConnection in connectionB.OtherTable.Connections)
                    connectionA.OtherTable.AddConnection(connectionBOtherTableConnection.TableFields, connectionBOtherTableConnection.OtherTable, connectionBOtherTableConnection.OtherTableFields);
            }

        // if we have connections of like Customer->CustomerAction->Customer->SomeOtherTable
        // We can actually remove second Customer connection(move all its connections to top Customer) and have something like this
        // Custmoer -> CustomerAction
        //        \ -> SomeOtherTable

        bool lifted = false;
        foreach (var connection in _connections)
        {
            foreach (var subConnection in connection.OtherTable.Connections)
            {

                if (!subConnection.IsSame(connection))
                    continue;
                connection.OtherTable._connections.Remove(subConnection);
                foreach (var movedConnection in subConnection.OtherTable.Connections)
                    _connections.Add(movedConnection);
                lifted = true;
                break;
            }
            if (lifted)
                break;
        }

        return hasChanges || lifted;
    }

    public IEnumerable<TableNode2> GetAllTableNodes()
    {
        return GetAllTableNodes(this);

        static IEnumerable<TableNode2> GetAllTableNodes(TableNode2 tableNode2)
        {
            yield return tableNode2;
            foreach (var connection in tableNode2._connections)
            {
                yield return connection.OtherTable;
                foreach (var table in GetAllTableNodes(connection.OtherTable))
                {
                    yield return table;

                }
            }
        }
    }
}

[DebuggerDisplay("To {OtherTable.Name}")]
class Connection
{
    private List<string> _tableFields;
    private List<string> _otherTableFields;
    private List<(string Field, string OtherField)> _sortedMappedFields = new();

    public TableNode2 Table { get; private set; }
    public IEnumerable<string> TableFields => _tableFields;
    public TableNode2 OtherTable { get; private set; }
    public IEnumerable<string> OtherTableFields => _otherTableFields;
    public IEnumerable<(string Field, string OtherField)> MappedFields => _sortedMappedFields;

    public Connection(TableNode2 table, IEnumerable<string> tableFields, TableNode2 otherTable, IEnumerable<string> otherTableFields)
    {
        Table = table;
        _tableFields = tableFields.ToList();
        OtherTable = otherTable;
        _otherTableFields = otherTableFields.ToList();
        if (TableFields.Count() != OtherTableFields.Count())
            throw new ArgumentException();

        UpdateMappedFields();
    }

    /// <summary>
    /// Add field to connection.
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="otherFiledName"></param>
    public void AddFields(string fieldName, string otherFiledName)
    {
        if (MappedFields.Any(m => m.Field == fieldName && m.OtherField == otherFiledName))
            return;
        _tableFields.Add(fieldName);
        _otherTableFields.Add(otherFiledName);
        UpdateMappedFields();
    }

    private void UpdateMappedFields()
    {
        _sortedMappedFields.Clear();
        for (int i = 0; i < _otherTableFields.Count; i++)
            _sortedMappedFields.Add((_tableFields[i], _otherTableFields[i]));
        _sortedMappedFields.Sort((a, b) => a.Field.CompareTo(b.Field));
    }

    /// <summary>
    /// Checks for equality.
    /// </summary>
    /// <param name="connection">Connection.</param>
    /// <returns>True - equals, false - not.</returns>
    public bool Equals(Connection connection)
    {
        return Table.Name == connection.Table.Name && OtherTable.Name == connection.OtherTable.Name &&
            MappedFields.SequenceEqual(connection.MappedFields);
    }

    /// <summary>
    /// Shows that it is same connection exactly or reveresed connection.
    /// </summary>
    /// <param name="connection">Connection to check.</param>
    /// <returns>True - same or same-reversed, false - not same.</returns>
    public bool IsSame(Connection connection)
    {
        if (Table.Name == connection.OtherTable.Name && OtherTable.Name == connection.Table.Name)
            return MappedFields.SequenceEqual(connection.MappedFields.Select(m => (m.OtherField, m.Field)));
        return Equals(connection);
    }
}

public interface IDbColumnTypeProvider
{
    public string[] GetPKFields(string tableName);

    public string GetSqlType(string tableName, string columnName);
}

public class DbColumnTypeProvider : IDbColumnTypeProvider
{
    public string[] GetPKFields(string tableName)
    {
        return tableName switch
        {
            "directcrm.Customers" => new[] { "Id" },
            "directcrm.CustomerActions" => new[] { "Id" },
            "directcrm.CustomFieldValues" => new[] { "Id" },
            "directcrm.Areas" => new[] { "Id" },
            "directcrm.SubAreas" => new[] { "Id" },
            "directcrm.RetailOrders" => new[] { "Id" },
            _ => throw new NotSupportedException()
        };
    }

    public string GetSqlType(string tableName, string columnName)
    {
        return (tableName, columnName) switch
        {
            ("directcrm.Customers", "Id") => "int not null",
            ("directcrm.Customers", "PasswordHash") => "nvarchar(32) not null",
            ("directcrm.Customers", "PasswordHashSalt") => "varbinary(16) null",
            ("directcrm.Customers", "TempPasswordHash") => "nvarchar(32) not null",
            ("directcrm.Customers", "TempPasswordHashSalt") => "varbinary(16) null",
            ("directcrm.Customers", "IsDeleted") => "bit not null",
            ("directcrm.Customers", "TempPasswordEmail") => "nvarchar(256) not null",
            ("directcrm.Customers", "TempPasswordMobilePhone") => "bigint null",
            ("directcrm.Customers", "AreaId") => "int not null",
            ("directcrm.CustomerActions", "Id") => "bigint not null",
            ("directcrm.CustomerActions", "DateTimeUtc") => "datetime2(7) not null",
            ("directcrm.CustomerActions", "CreationDateTimeUtc") => "datetime2(7) not null",
            ("directcrm.CustomerActions", "PointOfContactId") => "int not null",
            ("directcrm.CustomerActions", "ActionTemplateId") => "int not null",
            ("directcrm.CustomerActions", "CustomerId") => "int not null",
            ("directcrm.CustomerActions", "StaffId") => "int null",
            ("directcrm.CustomerActions", "OriginalCustomerId") => "int not null",
            ("directcrm.CustomerActions", "TransactionalId") => "bigint null",
            ("directcrm.CustomFieldValues", "Id") => "int not null",
            ("directcrm.CustomFieldValues", "CustomerId") => "bigint not null",
            ("directcrm.CustomFieldValues", "FieldName") => "nvarchar(32) not null",
            ("directcrm.CustomFieldValues", "FieldValue") => "nvarchar(32) not null",
            ("directcrm.Areas", "Id") => "int not null",
            ("directcrm.Areas", "Name") => "nvarchar(32) not null",
            ("directcrm.Areas", "SubAreaId") => "int null",
            ("directcrm.SubAreas", "Id") => "int not null",
            ("directcrm.SubAreas", "Name") => "nvarchar(64) not null",
            ("directcrm.RetailOrders", "Id") => "int not null",
            ("directcrm.RetailOrders", "CustomerId") => "int null",
            _ => throw new NotSupportedException()
        };
    }
}