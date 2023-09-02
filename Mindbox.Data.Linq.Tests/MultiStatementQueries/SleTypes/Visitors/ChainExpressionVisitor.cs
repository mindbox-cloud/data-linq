using System;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes.Visitors;

class ChainExpressionVisitor
{
    private readonly VisitorContext _visitorContext;

    public ChainSle Chain { get; private set; }

    public ChainExpressionVisitor(ISimplifiedLinqExpression parentExpression, VisitorContext context)
    {
        _visitorContext = context;
        Chain = new ChainSle();
        Chain.ParentExpression = parentExpression;
    }

    private Expression UnwrapNode(Expression node)
    {
        // Removes all converters.
        while (true)
        {
            if (node is not UnaryExpression unaryExpression)
                return node;
            if (unaryExpression.NodeType != ExpressionType.Convert)
                return node;
            node = unaryExpression.Operand;
        }
    }

    private Expression UnwrapNot(Expression expression, out bool isNegated)
    {
        isNegated = false;
        while (true)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    isNegated = !isNegated;
                    expression = (expression as UnaryExpression).Operand;
                    continue;
                case ExpressionType.Convert:
                    expression = (expression as UnaryExpression).Operand;
                    continue;
            }
            return expression;
        }
    }

    public void Visit(Expression node)
    {
        node = UnwrapNot(node, out var isNegated);

        Chain.IsNegated = isNegated;
        var chainCalls = ExpressionOrderFixer.GetReorderedChainCall(node).ToArray();
        if (chainCalls.Length == 0)
            throw new InvalidOperationException();

        if (chainCalls[0] is MethodCallExpression functionMethodCallExpression && functionMethodCallExpression.Method.Name.StartsWith("ValueAsQueryable"))
        {
            chainCalls = ExpressionOrderFixer.GetReorderedChainCall(functionMethodCallExpression.Arguments[0]).ToArray();
            if (chainCalls.Length == 0)
                throw new InvalidOperationException();
        }

        var tableName = ExpressionHelpers.GetTableName(chainCalls[0]);
        if (!string.IsNullOrEmpty(tableName))
            chainCalls = chainCalls.Skip(1).ToArray();
        else if (string.IsNullOrEmpty(tableName) && chainCalls.Length > 1)
        {
            tableName = ExpressionHelpers.GetTableName(chainCalls[1]);
            if (!string.IsNullOrEmpty(tableName))
                chainCalls = chainCalls.Skip(2).ToArray();
        }

        if (string.IsNullOrEmpty(tableName) && UnwrapNode(chainCalls[0]) is ConstantExpression) // plain constant or variable
        {
            Chain.AddChainPart(new FixedValueChainPart());
            return;
        }

        IChainPart lastRowSourceSle;
        if (!string.IsNullOrEmpty(tableName))
            lastRowSourceSle = new TableChainPart(tableName);
        else
        {
            // May be we are accessing table via parameter 
            if (chainCalls[0] is ParameterExpression parameterExpression && _visitorContext.ParameterToSle.TryGetValue(parameterExpression, out var parameterSle))
            {
                lastRowSourceSle = new ReferenceRowSourceChainPart() { ReferenceRowSource = parameterSle };
                chainCalls = chainCalls.Skip(1).ToArray();
            }
            else
                throw new InvalidOperationException();
        }

        Chain.AddChainPart(lastRowSourceSle);

        // Visit all chain parts
        foreach (var chainItemExpression in chainCalls)
        {
            if (chainItemExpression is MethodCallExpression chainCallExpression &&
                (chainCallExpression.Method.DeclaringType == typeof(Queryable) || chainCallExpression.Method.DeclaringType == typeof(Enumerable)))
            {
                if (new[] { "Where", "Any", "Single" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2)
                {
                    var filterParameter = ExtractParameterVariableFromFilterExpression(chainCallExpression.Arguments[1]);
                    _visitorContext.ParameterToSle.Add(filterParameter, lastRowSourceSle);
                    var filter = ExtractFilterLambdaBody(chainCallExpression.Arguments[1]);
                    var filterVisitor = new FilterExpressionVisitor(_visitorContext);
                    filterVisitor.Visit(filter);
                    Chain.AddChainPart(filterVisitor.FilterSle);
                    filterVisitor.FilterSle.ParentExpression = Chain;
                }
                else if ((new[] { "Select", "Sum", "Avg" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2) ||
                    (new[] { "SelectMany" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2))
                {
                    var filterParameter = ExtractParameterVariableFromSelectExpression(chainCallExpression.Arguments[1]);
                    _visitorContext.ParameterToSle.Add(filterParameter, lastRowSourceSle);
                    var selectVisitor = new SelectExpressionVisitor(_visitorContext);
                    selectVisitor.Visit(chainCallExpression.Arguments[1]);
                    lastRowSourceSle = selectVisitor.SelectSle;
                    Chain.AddChainPart(selectVisitor.SelectSle);
                    selectVisitor.SelectSle.ParentExpression = Chain;
                }
                else if (new[] { "Any", "Single", "SingleOrDefault", "First", "FirstOrDefault", "Sum", "Avg" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 1)
                    continue;
                else if (new[] { "Join" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 5)
                {
                    //var resultParameter = ExtractParameterVariableFromSelectExpression(chainCallExpression.Arguments[4]);
                    //_visitorContext.ParameterToSle.Add(resultParameter, lastRowSourceSle);
                    var joinVisitor = new JoinExpressionVisitor(_visitorContext);
                    Chain.AddChainPart(joinVisitor.JoinSle);
                    joinVisitor.Visit(chainCallExpression);
                    lastRowSourceSle = joinVisitor.JoinSle;
                }
                else
                    throw new NotSupportedException();
            }
            else if (chainItemExpression is MemberExpression memberExpression)
            {
                if (memberExpression.Member is PropertyInfo memberProperty)
                {
                    if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(ColumnAttribute)))
                        Chain.AddChainPart(new ColumnAccessChainPart() { ColumnName = memberProperty.Name });
                    else if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
                    {
                        var associationAttribute = memberProperty.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
                        var nextTableName = GetMetaTypeFromAssociation(memberProperty.PropertyType).CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                            .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                        var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
                        var otherTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString();
                        var associationSle = new AssociationChainPart() { ColumnName = currentTableField, NextTableName = nextTableName, NextTableColumnName = otherTableField };
                        lastRowSourceSle = associationSle;
                        Chain.AddChainPart(associationSle);
                    }
                    else
                    {
                        var propertyAccessChainPart = new PropertyAccessChainPart() { PropertyName = memberProperty.Name };
                        Chain.AddChainPart(propertyAccessChainPart);
                        lastRowSourceSle = propertyAccessChainPart;
                    }
                }
                else
                    throw new InvalidOperationException();
            }
            else
                throw new InvalidOperationException();
        }
    }

    private Type GetMetaTypeFromAssociation(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();
        return type;
    }

    private ParameterExpression ExtractParameterVariableFromFilterExpression(Expression filterExpression)
    {
        LambdaExpression lambda;
        if (filterExpression is UnaryExpression unary)
        {
            if (unary.NodeType != ExpressionType.Quote || unary.IsLifted || unary.IsLiftedToNull || unary.Method != null)
                throw new NotSupportedException();
            lambda = (LambdaExpression)unary.Operand;
        }
        else
            lambda = (LambdaExpression)filterExpression;
        if (lambda.ReturnType != typeof(bool) || lambda.TailCall || !string.IsNullOrEmpty(lambda.Name) || lambda.Parameters.Count != 1)
            throw new NotSupportedException();
        return lambda.Parameters[0];
    }

    private ParameterExpression ExtractParameterVariableFromSelectExpression(Expression filterExpression)
    {
        var unary = (UnaryExpression)filterExpression;
        if (unary.NodeType != ExpressionType.Quote || unary.IsLifted || unary.IsLiftedToNull || unary.Method != null)
            throw new NotSupportedException();
        var lambda = (LambdaExpression)unary.Operand;
        if (lambda.TailCall || !string.IsNullOrEmpty(lambda.Name) || lambda.Parameters.Count != 1)
            throw new NotSupportedException();
        return lambda.Parameters[0];
    }

    private Expression ExtractFilterLambdaBody(Expression expression)
    {
        if (expression is UnaryExpression unary)
        {
            if (unary.Method != null)
                throw new InvalidOperationException();
            if (unary.IsLifted || unary.IsLiftedToNull)
                throw new InvalidOperationException();
            return ((LambdaExpression)unary.Operand).Body;
        }
        return ((LambdaExpression)expression).Body;
    }
}