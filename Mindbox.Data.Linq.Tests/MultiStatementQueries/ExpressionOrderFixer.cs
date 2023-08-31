﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries;

internal static class ExpressionOrderFixer
{
    public static IEnumerable<ExpressionChain> GetExpressionChains(Expression node)
    {
        return GetExpressionsCore(new List<Expression>(), node);

        static IEnumerable<ExpressionChain> GetExpressionsCore(List<Expression> stack, Expression expression, bool? isLastInChain = null)
        {
            if ((expression.NodeType == ExpressionType.Call || expression.NodeType == ExpressionType.MemberAccess) && isLastInChain is null)
            {
                var chainItems = GetReorderedChainCall(expression).ToArray();
                for (int i = 0; i < chainItems.Length; i++)
                {
                    var chainItem = chainItems[i];
                    foreach (var item in GetExpressionsCore(stack, chainItem, chainItems.Length - 1 == i))
                        yield return item;
                    stack.Add(chainItem);
                }
                stack.RemoveRange(stack.Count - chainItems.Length, chainItems.Length);

                yield break;
            }

            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess:
                case ExpressionType.Parameter:
                case ExpressionType.Constant:
                    if (isLastInChain is true or null)
                        yield return new ExpressionChain(stack.Concat(new[] { expression }).ToList());
                    yield break;
            }


            using (new StackPusher<Expression>(stack, expression))
                switch (expression.NodeType)
                {
                    case ExpressionType.Quote:
                        var quoteExpression = (UnaryExpression)expression;
                        foreach (var item in GetExpressionsCore(stack, quoteExpression.Operand))
                            yield return item;
                        break;
                    case ExpressionType.Lambda:
                        var lambdaExpression = (LambdaExpression)expression;
                        foreach (var item in GetExpressionsCore(stack, lambdaExpression.Body))
                            yield return item;
                        break;
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.NotEqual:
                    case ExpressionType.Divide:
                    case ExpressionType.Multiply:
                    case ExpressionType.Subtract:
                    case ExpressionType.Add:
                    case ExpressionType.Equal:
                        var binaryExpression = (BinaryExpression)expression;
                        foreach (var item in GetExpressionsCore(stack, binaryExpression.Left))
                            yield return item;
                        foreach (var item in GetExpressionsCore(stack, binaryExpression.Right))
                            yield return item;
                        break;
                    case ExpressionType.Call:
                        var callExpression = (MethodCallExpression)expression;
                        if (callExpression.Method.DeclaringType == typeof(Queryable) || callExpression.Method.DeclaringType == typeof(Enumerable))
                            foreach (var argExpression in callExpression.Arguments.Skip(1))
                                foreach (var item in GetExpressionsCore(stack, argExpression))
                                    yield return item;
                        else
                            throw new NotSupportedException();
                        break;
                    case ExpressionType.Not:
                        var notExpression = (UnaryExpression)expression;
                        if (notExpression.IsLifted || notExpression.IsLiftedToNull || notExpression.Method != null)
                            throw new NotSupportedException();
                        foreach (var item in GetExpressionsCore(stack, notExpression.Operand))
                            yield return item;
                        break;
                    case ExpressionType.Convert:
                        var convertExpression = (UnaryExpression)expression;
                        if (convertExpression.Method != null && convertExpression.Method.Name != "op_Implicit")
                            throw new NotSupportedException();
                        foreach (var item in GetExpressionsCore(stack, convertExpression.Operand))
                            yield return item;
                        break;
                    default:
                        throw new NotSupportedException();
                }
        }
    }


    public static IEnumerable<Expression> GetReorderedChainCall(Expression expression)
    {
        if (expression == null || expression.NodeType == ExpressionType.Quote)
            return Enumerable.Empty<Expression>();
        List<Expression> toReturn = new List<Expression>();
        while (true)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Parameter:
                case ExpressionType.Constant:
                    toReturn.Add(expression);
                    toReturn.Reverse();
                    return toReturn;
                case ExpressionType.Call:
                    var callExpression = (MethodCallExpression)expression;
                    if (callExpression.Method.DeclaringType == typeof(Queryable) || callExpression.Method.DeclaringType == typeof(Enumerable))
                    {
                        toReturn.Add(expression);
                        expression = callExpression.Arguments[0];
                    }
                    else
                    {
                        toReturn.Add(expression);
                        toReturn.Reverse();
                        return toReturn;
                    }
                    break;
                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)expression;
                    toReturn.Add(expression);
                    expression = memberExpression.Expression;
                    break;
                default:
                    throw new NotSupportedException();
            }

        }
    }
}

public struct StackPusher<T> : IDisposable
{
    private readonly List<T> _listAsStack;
    private readonly Stack<T> _stack;

    public StackPusher(Stack<T> stack, T expression)
    {
        _stack = stack;
        _stack.Push(expression);
    }

    public StackPusher(List<T> stack, T expression)
    {
        _listAsStack = stack;
        _listAsStack.Add(expression);
    }


    public void Dispose()
    {
        _listAsStack?.RemoveAt(_listAsStack.Count - 1);
        _stack?.Pop();
    }
}

record ExpressionChain(IReadOnlyList<Expression> Items);
