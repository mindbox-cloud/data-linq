using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries;

class SqlQueryTranslator
{
    public static SqlQueryTranslatorResult Translate(Expression node, IDbColumnTypeProvider columnTypeProvider)
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
        ISelectChainPart currentSelectChainPart = null;
        TableNode2 currentTable = null!;
        foreach (var chainItem in chain.Items)
        {
            if (chainItem is TableChainPart tableChainPart)
            {
                currentTable = new TableNode2(tableChainPart.Name);
                currentSelectChainPart = null;
                context.AddChainPartForNode(currentTable, tableChainPart);
                if (context.RootTable == null)
                    context.RootTable = currentTable;
            }
            else if (chainItem is SelectChainPart selectChainPart)
            {
                foreach (var innerChain in selectChainPart.NamedChains.Values)
                    TranslateChain(context, innerChain);
            }
            else if (chainItem is JoinChainPart joinChainPart)
            {
                TranslateChain(context, joinChainPart.Inner);
                foreach (var innerChain in joinChainPart.NamedChains.Values)
                    TranslateChain(context, innerChain);
                DetectConnections(context, joinChainPart);
            }
            else if (chainItem is AssociationChainPart associationChainPart)
            {
                if (currentSelectChainPart != null)
                    switch (currentSelectChainPart.ChainPartType)
                    {
                        case SelectChainPartType.Simple:
                            currentTable = context.GetTableNodeByTablePart(currentSelectChainPart.NamedChains[""].Items.Last());
                            if (currentTable == null)
                                throw new NotSupportedException();
                            currentSelectChainPart = null;
                            var associationTable = new TableNode2(associationChainPart.NextTableName);
                            currentTable.AddConnection(new[] { associationChainPart.ColumnName }, associationTable, new string[] { associationChainPart.NextTableColumnName });
                            currentTable = associationTable;
                            currentSelectChainPart = null;
                            break;
                        case SelectChainPartType.Complex:
                            throw new NotSupportedException();
                        default:
                            throw new NotSupportedException();
                    }
                else
                {
                    if (currentTable == null)
                        throw new NotSupportedException();
                    var associationTable = new TableNode2(associationChainPart.NextTableName);
                    currentTable.AddConnection(new[] { associationChainPart.ColumnName }, associationTable, new string[] { associationChainPart.NextTableColumnName });
                    currentTable = associationTable;
                    currentSelectChainPart = null;
                }
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
                if (referenceRowSourceChainPart.ReferenceRowSource is TableChainPart tableReferenceRowSource)
                {
                    if (currentTable != null)
                        throw new InvalidOperationException();
                    var tableNodeByReference = context.GetTableNodeByTablePart(tableReferenceRowSource);
                    if (tableNodeByReference == null)
                        throw new InvalidOperationException();
                    currentTable = tableNodeByReference;
                    currentSelectChainPart = null;
                }
                else if (referenceRowSourceChainPart.ReferenceRowSource is ISelectChainPart selectReferenceRowSource)
                {
                    currentTable = null;
                    currentSelectChainPart = selectReferenceRowSource;
                }
                else if (referenceRowSourceChainPart.ReferenceRowSource is AssociationChainPart associationReferenceRowSource)
                {
                    var tableNodeByReference = context.GetTableNodeByTablePart(associationReferenceRowSource);
                    if (tableNodeByReference == null)
                        throw new InvalidOperationException();
                    currentTable = tableNodeByReference;
                    currentSelectChainPart = null;
                }
                else if (referenceRowSourceChainPart.ReferenceRowSource is PropertyAccessChainPart propertyAccessChainPart)
                {
                    if (UnwrapReferenceSources(propertyAccessChainPart.GetPrevious()) is SelectChainPart propertyForSelect)
                    {
                        if (!propertyForSelect.NamedChains.ContainsKey(propertyAccessChainPart.PropertyName))
                            throw new NotSupportedException();
                        currentTable = context.GetTableNodeByTablePart(propertyForSelect.NamedChains[propertyAccessChainPart.PropertyName].Items.Last());
                        if (currentTable == null)
                            throw new NotSupportedException();
                        currentSelectChainPart = null;
                    }
                    else
                        throw new NotSupportedException();
                }
                else
                    throw new NotSupportedException();
                continue;
            }
            else if (chainItem is ColumnAccessChainPart columnAccessChainPart)
            {
                if (currentSelectChainPart != null)
                    switch (currentSelectChainPart.ChainPartType)
                    {
                        case SelectChainPartType.Simple:
                            currentTable = context.GetTableNodeByTablePart(currentSelectChainPart.NamedChains[""].Items.Last());
                            if (currentTable == null)
                                throw new NotSupportedException();
                            currentSelectChainPart = null;
                            currentTable.AddField(columnAccessChainPart.ColumnName);
                            break;
                        case SelectChainPartType.Complex:
                            throw new NotSupportedException();
                        default:
                            throw new NotSupportedException();
                    }
                else
                {
                    if (currentTable == null)
                        throw new NotSupportedException();
                    currentTable.AddField(columnAccessChainPart.ColumnName);
                }
            }
            else if (chainItem is PropertyAccessChainPart propertyAccessChainPart)
            {
                if (currentSelectChainPart != null && currentSelectChainPart.NamedChains.ContainsKey(propertyAccessChainPart.PropertyName))
                {
                    currentTable = context.GetTableNodeByTablePart(UnwrapReferenceSources(currentSelectChainPart.NamedChains[propertyAccessChainPart.PropertyName].Items.Last()));
                    if (currentTable == null)
                        throw new NotSupportedException();
                    currentSelectChainPart = null;
                }
                else
                    throw new NotSupportedException();
            }
            else
                throw new NotSupportedException();

            if (currentTable != null)
                context.AddChainPartForNode(currentTable, chainItem);
        }
    }

    static void DetectConnections(TranslationContext context, JoinChainPart joinPart)
    {
        var left = GetTableAndField(context, joinPart.InnerKeySelectorSle);
        var right = GetTableAndField(context, joinPart.OuterKeySelectorSle);

        if (!left.HasValue || !right.HasValue)
            throw new NotImplementedException();
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

    private static IChainPart UnwrapReferenceSources(IChainPart chainPart)
    {
        while (true)
        {
            if (chainPart is ReferenceRowSourceChainPart referenceRowSourceChainPart)
                chainPart = referenceRowSourceChainPart.ReferenceRowSource;
            else
                return chainPart;
        }
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
        // So far only supported
        // - [variable] - assumes PK access
        // - [variable].[fieldName] 
        // - [variable].[association] 
        // - [variable].[association].[fieldName] - fieldName is PK
        if (sle is not ChainSle chain)
            return null;
        switch (chain.Items.Count)
        {
            case 1:
                if (chain.Items[0] is ReferenceRowSourceChainPart variableAsReference)
                    return (GetTableNode(context, variableAsReference), context.ColumnTypeProvider.GetPKFields(GetTableNode(context, variableAsReference).Name).Single());
                return null;
            case 2:
                {
                    if (chain.Items[0] is not ReferenceRowSourceChainPart referenceChainPart)
                        return null;
                    if (chain.Items[1] is ColumnAccessChainPart columnAccessChain)
                        return (GetTableNode(context, referenceChainPart), columnAccessChain.ColumnName);
                    else if (chain.Items[1] is AssociationChainPart associationChainPart)
                        return (GetTableNode(context, referenceChainPart), associationChainPart.ColumnName);
                    return null;
                }
            case 3:
                {
                    if (chain.Items[0] is not ReferenceRowSourceChainPart referenceChainPart)
                        return null;
                    if (chain.Items[1] is not AssociationChainPart associationChainPart)
                        return null;
                    if (chain.Items[2] is not ColumnAccessChainPart columnAccessChain)
                        return null;
                    if (columnAccessChain.ColumnName != context.ColumnTypeProvider.GetPKFields(associationChainPart.NextTableName).Single())
                        return null;
                    return (GetTableNode(context, referenceChainPart), associationChainPart.ColumnName);
                }
            default:
                return null;
        }

        static TableNode2 GetTableNode(TranslationContext context, ReferenceRowSourceChainPart referenceRowSourceChainPart)
        {
            if (referenceRowSourceChainPart.ReferenceRowSource is TableChainPart tableChainPart)
            {
                var tableNodeReference = context.GetTableNodeByTablePart(tableChainPart);
                if (tableNodeReference == null)
                    throw new InvalidOperationException();
                return tableNodeReference;
            }
            else if (referenceRowSourceChainPart.ReferenceRowSource is SelectChainPart selectChainPart)
            {
                switch (selectChainPart.ChainPartType)
                {
                    case SelectChainPartType.Simple:
                        var tableReferenceSimple = context.GetTableNodeByTablePart(selectChainPart.NamedChains[""].Items.Last());
                        if (tableReferenceSimple == null)
                            throw new InvalidOperationException();
                        return tableReferenceSimple;
                    case SelectChainPartType.Complex:
                        throw new NotSupportedException();
                    default:
                        throw new NotSupportedException();
                }
            }
            else if (referenceRowSourceChainPart.ReferenceRowSource is AssociationChainPart associationChainPart)
            {
                var tableNodeReference = context.GetTableNodeByTablePart(associationChainPart);
                if (tableNodeReference == null)
                    throw new InvalidOperationException();
                return tableNodeReference;
            }
            else if (referenceRowSourceChainPart.ReferenceRowSource is PropertyAccessChainPart propertyAccessChainPart)
            {
                if (UnwrapReferenceSources(propertyAccessChainPart.GetPrevious()) is SelectChainPart propertyForSelect)
                {
                    if (!propertyForSelect.NamedChains.ContainsKey(propertyAccessChainPart.PropertyName))
                        throw new NotSupportedException();
                    var tableReferenceSimple = context.GetTableNodeByTablePart(propertyForSelect.NamedChains[propertyAccessChainPart.PropertyName].Items.Last());
                    if (tableReferenceSimple == null)
                        throw new InvalidOperationException();
                    return tableReferenceSimple;
                }
                else
                    throw new NotSupportedException();
                throw new NotSupportedException();

            }
            else
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Operator against constant.
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
        var visitor = new ChainExpressionVisitor(null, visitorContext);
        visitor.Visit(expression);
        return visitor.Chain;
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

        public Dictionary<TableNode2, List<IChainPart>> Node2ChainParts { get; } = new();
        public Dictionary<Connection, ChainSle> Connection2Chains { get; } = new();
        public IDbColumnTypeProvider ColumnTypeProvider { get; private set; }

        public TableNode2 RootTable { get; set; }

        public TranslationContext(IDbColumnTypeProvider columnTypeProvider)
        {
            ColumnTypeProvider = columnTypeProvider;
        }

        public void AddChainPartForNode(TableNode2 tableNode, IChainPart chainPart)
        {
            if (!Node2ChainParts.ContainsKey(tableNode))
                Node2ChainParts.Add(tableNode, new());
            if (Node2ChainParts[tableNode].Contains(chainPart))
                return;
            Node2ChainParts[tableNode].Add(chainPart);
        }

        public TableNode2 GetTableNodeByTablePart(IChainPart tableChainPart)
        {
            foreach (var (node, parts) in Node2ChainParts)
                foreach (var part in parts)
                    if (part == tableChainPart)
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

        // if we have connections like Customer->CustomerAction->Customer->SomeOtherTable
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
        hasChanges |= lifted;

        return hasChanges;
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
    /// Shows that it is same connection exactly or reversed connection.
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
            "directcrm.CustomerCustomFieldValues" => new[] { "Id" },
            "directcrm.CustomerActionCustomFieldValues" => new[] { "Id" },
            "directcrm.Areas" => new[] { "Id" },
            "directcrm.ActionTemplates" => new[] { "Id" },
            "directcrm.SubAreas" => new[] { "Id" },
            "directcrm.RetailOrders" => new[] { "Id" },
            "directcrm.RetailOrderHistoryItems" => new[] { "Id" },
            "directcrm.RetailOrderPurchases" => new[] { "Id" },
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
            ("directcrm.CustomerActionCustomFieldValues", "Id") => "int not null",
            ("directcrm.CustomerActionCustomFieldValues", "CustomerActionId") => "bigint not null",
            ("directcrm.CustomerActionCustomFieldValues", "FieldName") => "nvarchar(32) not null",
            ("directcrm.CustomerActionCustomFieldValues", "FieldValue") => "nvarchar(32) not null",
            ("directcrm.CustomerCustomFieldValues", "Id") => "int not null",
            ("directcrm.CustomerCustomFieldValues", "CustomerId") => "int not null",
            ("directcrm.CustomerCustomFieldValues", "FieldName") => "nvarchar(32) not null",
            ("directcrm.CustomerCustomFieldValues", "FieldValue") => "nvarchar(32) not null",
            ("directcrm.Areas", "Id") => "int not null",
            ("directcrm.Areas", "Name") => "nvarchar(32) not null",
            ("directcrm.Areas", "SubAreaId") => "int null",
            ("directcrm.ActionTemplates", "Id") => "int not null",
            ("directcrm.ActionTemplates", "Name") => "nvarchar(32) not null",
            ("directcrm.SubAreas", "Id") => "int not null",
            ("directcrm.SubAreas", "Name") => "nvarchar(64) not null",
            ("directcrm.RetailOrders", "Id") => "int not null",
            ("directcrm.RetailOrders", "CustomerId") => "int null",
            ("directcrm.RetailOrders", "TotalSum") => "float not null",
            ("directcrm.RetailOrderHistoryItems", "Id") => "int not null",
            ("directcrm.RetailOrderHistoryItems", "IsCurrentOtherwiseNull") => "bit null",
            ("directcrm.RetailOrderHistoryItems", "RetailOrderId") => "int not null",
            ("directcrm.RetailOrderHistoryItems", "Amount") => "decimal(18,2) null",
            ("directcrm.RetailOrderPurchases", "Count") => "decimal(18,2) not null",
            ("directcrm.RetailOrderPurchases", "PriceForCustomerOfLine") => "decimal(18,2) not null",
            ("directcrm.RetailOrderPurchases", "RetailOrderHistoryItemId") => "bigint not null",
            _ => throw new NotSupportedException()
        };
    }
}