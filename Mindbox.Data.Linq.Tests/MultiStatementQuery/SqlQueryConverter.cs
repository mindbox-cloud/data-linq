using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mindbox.Data.Linq.Tests.MultiStatementQuery;


class SqlQueryConverter
{
    public static SqlQueryConverterResult Analyze(Expression node)
    {
        if (!IsQuerableChainCall(node))
            throw new NotSupportedException($"Top level statement should method be of type {typeof(Queryable)} or {typeof(Enumerable)}.");

        var context = new SqlAnalyzerContext();

        // Root level we expect following structure
        //  [0]     - Table
        //  [1..n]  - Where clauses
        var chainCall = GetOrderedQuerableChainCall(node).ToArray();

        context.SqlTree.Table = new SqlTable() { Name = GetTableNameAndEntityType(chainCall[0]).Value.TableName };
        context.SqlTree.Table.AddField("Id");

        context.CallStack.Push(context.SqlTree.Table);
        foreach (var expression in chainCall.Skip(1))
            AnalyzeWhere(context, expression);
        context.CallStack.Pop();

        var command = SqlTreeCommandBuilder.Build(context.SqlTree);

        return new SqlQueryConverterResult(command);
    }

    private static void AnalyzeWhere(SqlAnalyzerContext context, Expression expression)
    {
        var callExpression = expression as MethodCallExpression;
        if (callExpression == null)
            throw new NotSupportedException("Expression is not MethodCallExpression.");
        if (callExpression.Method.Name != "Where")
            throw new NotSupportedException("MethodCallExpression is not Where extension call.");

        var filterUnary = callExpression.Arguments[1] as UnaryExpression;
        if (filterUnary.Method != null)
            throw new NotSupportedException();

        var filterLambda = filterUnary.Operand as LambdaExpression;
        if (filterLambda.Parameters.Count != 1)
            throw new NotSupportedException();

        context.ParameterMapping.Add(filterLambda.Parameters[0], context.CallStack.Peek());
        AnalyzeWhereBody(context, filterLambda.Body);
        context.ParameterMapping.Remove(filterLambda.Parameters[0]);
    }

    private static void AnalyzeWhereBody(SqlAnalyzerContext context, Expression body)
    {
        switch (body.NodeType)
        {
            case ExpressionType.OrElse:
            case ExpressionType.AndAlso:
                var logicalBinaryExpression = body as BinaryExpression;
                if (logicalBinaryExpression.Conversion != null || logicalBinaryExpression.Type != typeof(bool))
                    throw new NotSupportedException();
                AnalyzeWhereBody(context, logicalBinaryExpression.Left);
                AnalyzeWhereBody(context, logicalBinaryExpression.Right);
                return;
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Equal:
                var binaryExpression = body as BinaryExpression;
                if (!new string[] { null, "op_Equality" }.Contains(binaryExpression.Method?.Name)
                    || binaryExpression.Conversion != null || binaryExpression.Type != typeof(bool))
                    throw new NotSupportedException();

                var binaryExpressionParts = (binaryExpression.Left, binaryExpression.Right);
                // Reorder parts for simplified analysis
                if (GetTableFieldAccessPropertyInfo(context, binaryExpression.Right) != null)
                    binaryExpressionParts = (binaryExpression.Right, binaryExpression.Left);

                // If table field is compared against constant or variable -> we can ignore that expression
                // as it doesn't extend scope
                var binaryExpressionTableAndField = GetTableFieldAccessPropertyInfo(context, binaryExpressionParts.Left);
                if (binaryExpressionTableAndField != null && IsConstantOrVariable(binaryExpressionParts.Right))
                {
                    binaryExpressionTableAndField.Value.Table.AddField(binaryExpressionTableAndField.Value.Field.Name);
                    return;
                }
                
                throw new NotSupportedException();
            case ExpressionType.MemberAccess:
                var memberAccess = body as MemberExpression;
                // If it is just bool field access - we can ignore it as it doesn't extend scope
                var memberAccessExpressionTableAndField = GetTableFieldAccessPropertyInfo(context, memberAccess);
                if (memberAccessExpressionTableAndField != null && memberAccess.Type == typeof(bool))
                {
                    memberAccessExpressionTableAndField.Value.Table.AddField(memberAccessExpressionTableAndField.Value.Field.Name);
                    return;
                }
                throw new NotSupportedException();
            case ExpressionType.Not:
                var notExpression = body as UnaryExpression;
                if (notExpression.Method != null)
                    throw new NotSupportedException();
                // If it is just bool field access - we can ignore it as it doesn't extend scope
                var mnotExpressionTableAndField = GetTableFieldAccessPropertyInfo(context, notExpression.Operand);
                if (mnotExpressionTableAndField != null && notExpression.Operand.Type == typeof(bool))
                {
                    mnotExpressionTableAndField.Value.Table.AddField(mnotExpressionTableAndField.Value.Field.Name);
                    return;
                }
                throw new NotSupportedException();
            default:
                throw new NotSupportedException();
        }
    }

    private static (SqlTable Table, PropertyInfo Field)? GetTableFieldAccessPropertyInfo(SqlAnalyzerContext context, Expression expression)
    {
        var propertyExpression = expression as MemberExpression;
        if (propertyExpression == null)
            return null;
        if (propertyExpression.Member is not PropertyInfo propertyInfo)
            return null;
        if (!propertyInfo.CustomAttributes.Any(p => p.AttributeType == typeof(ColumnAttribute)))
            return null;

        // Check that field acces is related to table on top of call stack
        var table = context.CallStack.Peek();
        if (!context.ParameterMapping.Where(p => p.Key == propertyExpression.Expression && p.Value == table).Any())
            return null;

        return (table, propertyInfo);
    }

    private static bool IsConstantOrVariable(Expression expression)
    {
        return expression is ConstantExpression ||
            (expression is MemberExpression memberExpression && memberExpression.Member is FieldInfo && memberExpression.Expression is ConstantExpression);
    }

    private static bool IsQuerableChainCall(Expression expression)
    {
        bool isMultiStatementChain = expression is MethodCallExpression methodCallExpression &&
            (methodCallExpression.Method.DeclaringType == typeof(Queryable) || methodCallExpression.Method.DeclaringType == typeof(Enumerable));
        if (isMultiStatementChain)
            return true;
        return GetTableNameAndEntityType(expression) is not null;
    }

    private static IEnumerable<Expression> GetOrderedQuerableChainCall(Expression expression)
    {
        List<Expression> toReturn = new List<Expression>();
        while (true)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    if (GetTableNameAndEntityType(expression) is null)
                        throw new NotSupportedException();
                    toReturn.Add(expression);
                    toReturn.Reverse();
                    return toReturn;
                case ExpressionType.MemberAccess:
                    var memberAccessExpression = (MemberExpression)expression;
                    throw new NotSupportedException();
                case ExpressionType.Call:
                    var callExpression = (MethodCallExpression)expression;
                    if (callExpression.Method.DeclaringType == typeof(Queryable))
                        switch (callExpression.Method.Name)
                        {
                            case "Any" when callExpression.Arguments.Count == 2 && callExpression.Arguments[0] is Expression anyFirstParameter:
                                expression = anyFirstParameter;
                                throw new NotSupportedException();
                            case "Where" when callExpression.Arguments[0] is Expression whereFirstParameter:
                                toReturn.Add(callExpression);
                                expression = whereFirstParameter;
                                break;
                            case "SelectMany" when callExpression.Arguments[0] is Expression selectManyFirstParameter:
                                toReturn.Add(callExpression);
                                expression = selectManyFirstParameter;
                                break;
                            case "Join" when callExpression.Arguments[0] is Expression joinFirstParameter:
                                expression = joinFirstParameter;
                                throw new NotSupportedException();
                            case "Select" when callExpression.Arguments[0] is Expression selectFirstParameter:
                                toReturn.Add(callExpression.Arguments[1]);
                                expression = selectFirstParameter;
                                throw new NotSupportedException();
                            default:
                                throw new NotSupportedException();
                        }
                    else if (callExpression.Method.DeclaringType == typeof(Enumerable))
                        switch (callExpression.Method.Name)
                        {
                            case "Any" when callExpression.Arguments.Count == 2:
                                toReturn.Add(callExpression);
                                expression = callExpression.Arguments[0];
                                break;
                            case "Any" when callExpression.Arguments.Count == 1:
                                toReturn.Add(callExpression);
                                expression = callExpression.Arguments[0];
                                break;
                            case "Average" when callExpression.Arguments.Count == 1:
                                //toReturn.Expressions.Add(new("Average", null));
                                expression = callExpression.Arguments[0];
                                throw new NotSupportedException();
                            case "Where" when callExpression.Arguments[0] is Expression querableWhereFirstParameter:
                                toReturn.Add(callExpression);
                                expression = querableWhereFirstParameter;
                                break;
                            case "Select" when callExpression.Arguments[0] is Expression selectFirstParameter:
                                var selectLambdaExpression = (LambdaExpression)callExpression.Arguments[1];
                                //toReturn.Expressions.Add(("Select", selectLambdaExpression.Body));
                                expression = selectFirstParameter;
                                throw new NotSupportedException();
                            default:
                                throw new NotSupportedException();
                        }
                    else
                        throw new NotSupportedException();
                    break;
            }

        }
    }

    public static (string TableName, Type EntityType)? GetTableNameAndEntityType(Expression expression)
    {
        var constantExpression = expression as ConstantExpression;
        if (constantExpression == null)
            return null;
        var constantValue = constantExpression.Value;
        if (constantValue.GetType().GetGenericTypeDefinition() != typeof(System.Data.Linq.Table<>))
            throw new NotSupportedException();
        var genericParameterType = constantValue.GetType().GenericTypeArguments[0];
        var attribute = genericParameterType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute));
        var typedValue = attribute.NamedArguments.Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue;
        var tableName = attribute.NamedArguments.Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString()!;
        return (tableName, genericParameterType);
    }
}


class SqlTable
{
    private List<SqlTableLink> _linkedTables = new();
    private List<string> _fields = new();
    public string Name { get; set; }
    public IEnumerable<string> Fields => _fields;
    public IEnumerable<SqlTableLink> LinkedTables => _linkedTables;

    public bool AddField(string field)
    {
        if (_fields.Contains(field))
            return false;
        _fields.Add(field);
        return true;
    }

    public bool AddLinkedTable(SqlTableLink link)
    {
        foreach (var existingLink in _linkedTables.Where(l => l.RightTable.Name == link.RightTable.Name))
        {
            if (existingLink.Connections.Count() != link.Connections.Count())
                continue;
            if (existingLink.Connections.Select(c => (c.LeftFieldName, c.RightFieldName))
                    .SequenceEqual(link.Connections.Select(c => (c.LeftFieldName, c.RightFieldName))))
                return false;
        }
        _linkedTables.Add(link);
        foreach (var conneciton in link.Connections)
            AddField(conneciton.LeftFieldName);
        return true;
    }
}

class SqlAnalyzerContext
{
    public SqlTree SqlTree { get; private set; } = new();

    public Stack<SqlTable> CallStack { get; private set; } = new();

    public Dictionary<Expression, SqlTable> ParameterMapping { get; private set; } = new();
}

class SqlTree
{
    public SqlTable Table { get; set; }
}

class SqlTableLink
{
    public SqlTable RightTable { get; set; }
    public IEnumerable<SqlTableLinkConnection> Connections { get; private set; }
    public SqlTableLink(SqlTable rightTable, IEnumerable<SqlTableLinkConnection> connections)
    {
        RightTable = rightTable;
        Connections = connections.OrderBy(o => o.LeftFieldName).ToArray();
    }
}

record SqlTableLinkConnection(string LeftFieldName, string RightFieldName);