using Castle.Components.DictionaryAdapter;
using Snapshooter.MSTest;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries;


class SqlQueryTranslator
{
    public static SqlQueryTranslatorResult Transalate(Expression node, IDbColumnTypeProvider columntTypeProvider)
    {
        var query = TranslateCore(node, columntTypeProvider);
        // SimplifyTree(root);

        var command = SqlTreeCommandBuilder.Build(query, columntTypeProvider);

        return new SqlQueryTranslatorResult(command);
    }

    private static MultiStatementQuery TranslateCore(Expression expression, IDbColumnTypeProvider columntTypeProvider)
    {
        var context = new TranslationContext(columntTypeProvider);

        var visitorContext = new VisitorContext(new DbColumnTypeProvider());
        var visitor = new ChainExpressionVisitor(visitorContext);
        visitor.Visit(expression);

        

        throw new NotSupportedException();
    }

    /*
    private static MultiStatementQuery TranslateCore(Expression expression, IDbColumnTypeProvider columntTypeProvider)
    {
        var context = new TranslationContext(columntTypeProvider);
        var chains = ExpressionOrderFixer.GetExpressionChains(expression).ToArray();
        foreach (var chain in chains)
        {
            context.ResetCurrentTable();
            for (int i = 0; i < chain.Items.Count; i++)
                MapToSqlNode(new ExpressionChainItem(chain, i), context);
        }

        context.FillJoinConditions();
        context.OptmizeQuery();
        return context.Query;
    }

    private static void MapToSqlNode(ExpressionChainItem chainItem, TranslationContext context)
    {
        switch (chainItem.Expression.NodeType)
        {
            case ExpressionType.Constant:
                var tableName = ExpressionHelpers.GetTableName((ConstantExpression)chainItem.Expression);
                if (string.IsNullOrEmpty(tableName))
                {
                    if (context.CurrentTable == null)
                        throw new InvalidOperationException();
                    return;
                }
                context.AddTable(tableName, chainItem.Expression);
                return;
            case ExpressionType.Call:
                var callExpression = (MethodCallExpression)chainItem.Expression;
                if (callExpression.Method.DeclaringType == typeof(Queryable) || callExpression.Method.DeclaringType == typeof(Enumerable))
                    return;
                throw new NotSupportedException();
            case ExpressionType.Quote:
                var quoteExpression = (UnaryExpression)chainItem.Expression;
                if (quoteExpression.Method != null)
                    throw new NotSupportedException();
                if (chainItem.PreviousChainItem.Expression is MethodCallExpression quoteParentMethodExpression &&
                    (quoteParentMethodExpression.Method.DeclaringType == typeof(Queryable) || quoteParentMethodExpression.Method.DeclaringType == typeof(Enumerable)))
                {
                    if (quoteParentMethodExpression.Method.GetParameters().Length == 2)
                    {
                        context.AddTableFilter(((LambdaExpression)quoteExpression.Operand).Body);
                    }
                }
                return;
            case ExpressionType.Lambda:
                var lambdaExpression = (LambdaExpression)chainItem.Expression;
                MethodCallExpression lambdCallExpression = null;
                if (chainItem.PreviousChainItem.Expression is UnaryExpression)
                    lambdCallExpression = chainItem.PreviousPreviousExpression as MethodCallExpression;
                else
                    lambdCallExpression = chainItem.PreviousChainItem.Expression as MethodCallExpression;

                if (lambdCallExpression != null &&
                        (lambdCallExpression.Method.DeclaringType == typeof(Queryable) || lambdCallExpression.Method.DeclaringType == typeof(Enumerable)))
                {
                    var filterParameterExpression = lambdCallExpression.Method switch
                    {
                        { Name: "Where" or "Any" or "Single" } when lambdaExpression.ReturnType == typeof(bool) && lambdCallExpression.Method.GetParameters().Length == 2
                                => lambdaExpression.Parameters[0],
                        { Name: "SelectMany" } when lambdCallExpression.Method.GetParameters().Length == 2
                                => lambdaExpression.Parameters[0],
                        _ => throw new NotSupportedException()
                    };
                    context.MapParameterToTable(filterParameterExpression);
                    return;
                }
                throw new NotSupportedException();
            case ExpressionType.And:
            case ExpressionType.AndAlso:
            case ExpressionType.Or:
            case ExpressionType.OrElse:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.Divide:
            case ExpressionType.Multiply:
            case ExpressionType.Add:
            case ExpressionType.Subtract:
                return;
            case ExpressionType.Parameter:
                context.SetCurrentTable(context.GetTableFromExpression((ParameterExpression)chainItem.Expression));
                return;
            case ExpressionType.MemberAccess:
                var memberExpression = (MemberExpression)chainItem.Expression;
                if (memberExpression.Member is PropertyInfo propertyInfo)
                {
                    // Column access. Like User.Name
                    if (propertyInfo.CustomAttributes.Any(p => p.AttributeType == typeof(ColumnAttribute)))
                    {
                        context.CurrentTable.AddUsedField(propertyInfo.Name);
                        return;
                    }
                    // Association access
                    if (propertyInfo.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
                    {
                        var associationAttribute = propertyInfo.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
                        var propertyType = propertyInfo.PropertyType.IsArray ? propertyInfo.PropertyType.GetElementType() : propertyInfo.PropertyType;
                        var nextTableName = propertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                            .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                        var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
                        var associationTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString();

                        var currentTable = context.CurrentTable;
                        var associationTable = context.AddTable(nextTableName, null);
                        associationTable.AddJoinCondition(new JoinCondition(associationTableField, currentTable, currentTableField));
                        return;
                    }
                }
                else if (memberExpression.Expression is ConstantExpression memberConstantExpression)// Invocation of constant
                {
                    var memberConstantValue = Expression.Lambda(memberExpression).Compile().DynamicInvoke();
                    if (memberConstantValue == null || memberConstantValue.GetType() == typeof(string))
                        return;
                    var memberTableName = ExpressionHelpers.GetTableNameFromObject(memberConstantValue);
                    if (!string.IsNullOrEmpty(memberTableName))
                    {
                        context.AddTable(memberTableName, chainItem.Expression);
                        return;
                    }
                }
                throw new NotSupportedException();
            case ExpressionType.Not:
                var notExpression = (UnaryExpression)chainItem.Expression;
                if (notExpression.IsLifted || notExpression.IsLiftedToNull || notExpression.Method != null)
                    throw new NotSupportedException();
                return;
            case ExpressionType.Convert:
                var convertExpression = (UnaryExpression)chainItem.Expression;
                if (convertExpression.IsLifted || convertExpression.IsLiftedToNull || convertExpression.Method != null)
                    throw new NotSupportedException();
                return;
            case ExpressionType.AddChecked:
            case ExpressionType.ArrayLength:
            case ExpressionType.ArrayIndex:
            case ExpressionType.Coalesce:
            case ExpressionType.Conditional:
            case ExpressionType.ConvertChecked:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.Invoke:
            case ExpressionType.LeftShift:
            case ExpressionType.ListInit:
            case ExpressionType.MemberInit:
            case ExpressionType.Modulo:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.Negate:
            case ExpressionType.UnaryPlus:
            case ExpressionType.NegateChecked:
            case ExpressionType.New:
            case ExpressionType.NewArrayInit:
            case ExpressionType.NewArrayBounds:
            case ExpressionType.Power:
            case ExpressionType.RightShift:
            case ExpressionType.SubtractChecked:
            case ExpressionType.TypeAs:
            case ExpressionType.TypeIs:
            case ExpressionType.Assign:
            case ExpressionType.Block:
            case ExpressionType.DebugInfo:
            case ExpressionType.Decrement:
            case ExpressionType.Dynamic:
            case ExpressionType.Default:
            case ExpressionType.Extension:
            case ExpressionType.Goto:
            case ExpressionType.Increment:
            case ExpressionType.Index:
            case ExpressionType.Label:
            case ExpressionType.RuntimeVariables:
            case ExpressionType.Loop:
            case ExpressionType.Switch:
            case ExpressionType.Throw:
            case ExpressionType.Try:
            case ExpressionType.Unbox:
            case ExpressionType.AddAssign:
            case ExpressionType.AndAssign:
            case ExpressionType.DivideAssign:
            case ExpressionType.ExclusiveOrAssign:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.ModuloAssign:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.OrAssign:
            case ExpressionType.PowerAssign:
            case ExpressionType.RightShiftAssign:
            case ExpressionType.SubtractAssign:
            case ExpressionType.AddAssignChecked:
            case ExpressionType.MultiplyAssignChecked:
            case ExpressionType.SubtractAssignChecked:
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.PostIncrementAssign:
            case ExpressionType.PostDecrementAssign:
            case ExpressionType.TypeEqual:
            case ExpressionType.OnesComplement:
            case ExpressionType.IsTrue:
            case ExpressionType.IsFalse:
            default:
                throw new NotSupportedException();
        }
    }

    public record ExpressionChainItem(ExpressionChain Chain, int Index)
    {
        public Expression Expression => Chain.Items[Index];
        public ExpressionChainItem PreviousChainItem => new ExpressionChainItem(Chain, Index - 1);
        public ExpressionChainItem NextChainItem => Chain.Items.Count <= Index + 1 ? null : new ExpressionChainItem(Chain, Index + 1);
        public Expression PreviousPreviousExpression => PreviousChainItem.PreviousChainItem.Expression;
    }
     */
}

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