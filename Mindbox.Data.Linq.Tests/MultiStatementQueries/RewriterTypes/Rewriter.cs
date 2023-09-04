﻿using Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;
using Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.AccessControl;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.RewriterTypes;

internal class Rewriter
{
    public Expression<Func<ResultSet, bool>> Rewrite(Expression expression)
    {
        var enumerableRewriter = new EnumerableRewriter();
        var enumerableExpression = enumerableRewriter.Visit(expression);

        var parameter = Expression.Parameter(typeof(ResultSet), "resultSet");
        var visitor = new RewriterVisitor(parameter);
        var rewrittenExpression = visitor.Visit(enumerableExpression);

        var anyExpression = Expression.Call(null, GetMethod(() => Enumerable.Any<ResultRow>(null)), rewrittenExpression);
        return Expression.Lambda<Func<ResultSet, bool>>(anyExpression, parameter);
    }

    public MethodInfo GetMethod(Expression<Action> e) => (e.Body as MethodCallExpression).Method;
}

internal class RewriterVisitor : ExpressionVisitor
{
    private readonly ParameterExpression _resultSetParameter;
    private readonly MethodInfo _getTableMethodInfo;
    private readonly MethodInfo _getValueMethodInfo;
    private Dictionary<ParameterExpression, ParameterExpression> _parameterMapping = new();

    public RewriterVisitor(ParameterExpression resultSetParameter)
    {
        _resultSetParameter = resultSetParameter;
        _getTableMethodInfo = typeof(ResultSet).GetMethod(nameof(ResultSet.GetTable));
        _getValueMethodInfo = typeof(ResultRow).GetMethod(nameof(ResultRow.GetValue));
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var tableName = ExpressionHelpers.GetTableName(node);
        if (!string.IsNullOrEmpty(tableName))
            return Expression.Call(_resultSetParameter, _getTableMethodInfo, Expression.Constant(tableName));
        return base.VisitConstant(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var tableAttribute = node.Member.DeclaringType.CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(TableAttribute));
        if (tableAttribute != null)
        {
            var objectExpression = Visit(node.Expression);
            return Expression.Call(objectExpression, _getValueMethodInfo.MakeGenericMethod(node.Type), Expression.Constant(node.Member.Name));
        }
        return base.VisitMember(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (_parameterMapping.ContainsKey(node))
            return _parameterMapping[node];
        var tableAttribute = node.Type.CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(TableAttribute));
        if (tableAttribute != null)
        {
            var mappedParameter = Expression.Parameter(typeof(ResultRow), node.Name);
            _parameterMapping.Add(node, mappedParameter);
            return mappedParameter;
        }
        return base.VisitParameter(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Enumerable))
        {
            List<Type> genericArgs = new();
            foreach (var argType in node.Method.GetGenericArguments())
            {
                var tableAttribute = argType.CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(TableAttribute));
                if (tableAttribute != null)
                    genericArgs.Add(typeof(ResultRow));
                else
                    genericArgs.Add(argType);
            }
            List<Type> parameterTypes = new();
            foreach (var parameterInfo in node.Method.GetParameters())
            {
                var tableAttribute = parameterInfo.ParameterType.CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(TableAttribute));
                if (tableAttribute != null)
                    parameterTypes.Add(typeof(ResultRow));
                else
                    parameterTypes.Add(parameterInfo.ParameterType);
            }
            List<Expression> parameters = new();
            foreach (var parameter in node.Arguments)
                parameters.Add(Visit(parameter));
            return Expression.Call(node.Object, node.Method.GetGenericMethodDefinition().MakeGenericMethod(genericArgs.ToArray()), parameters.ToArray());
        }
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        return base.VisitUnary(node);
    }

    [return: NotNullIfNotNull("node")]
    public override Expression Visit(Expression node)
    {
        if (node is LambdaExpression lambda)
        {
            List<ParameterExpression> parameters = new();
            foreach (var item in lambda.Parameters)
                parameters.Add((ParameterExpression)Visit(item));
            var body = Visit(lambda.Body);
            return Expression.Lambda(body, parameters);
        }
        return base.Visit(node);
    }
}

