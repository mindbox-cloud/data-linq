using Azure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mindbox.Data.Linq.Tests.MultiStatementQuery;


class SqlQueryTranslator
{
    public static SqlQueryTranslatorResult Transalate(Expression node)
    {
        var context = new TranslationContext();
        TransalateCore(context, node);

        throw new NotImplementedException();

        // var command = SqlTreeCommandBuilder.Build(context.SqlTree);

        // return new SqlQueryTranslatorResult(command);
    }

    private static void TransalateCore(TranslationContext context, Expression node)
    {
        var items = GetExpressionChains(node).ToArray();
    }

    private static IEnumerable<IExpressionEnumeratorItem> GetExpressionChains(Expression node)
    {
        List<Expression> stack = new List<Expression>();
        return GetExpressionsCore(stack, node);

        static IEnumerable<IExpressionEnumeratorItem> GetExpressionsCore(List<Expression> stack, Expression expression, bool reorderedChain = false)
        {
            if (expression.NodeType == ExpressionType.Call)
            {
                var callExpression = (MethodCallExpression)expression;
                if (!reorderedChain)
                {
                    if (callExpression.Method.DeclaringType == typeof(Queryable) || callExpression.Method.DeclaringType == typeof(Enumerable))
                    {
                        var chainItems = GetOrderedQuerableChainCall(expression).ToArray();
                        foreach (var chainItem in chainItems)
                        {
                            foreach (var item in GetExpressionsCore(stack, chainItem, true))
                                yield return item;
                            stack.Add(chainItem);
                        }
                        stack.RemoveRange(stack.Count - chainItems.Length, chainItems.Length);

                        //foreach (var item in GetExpressionsCore(stack, callExpression.Arguments.First()))
                        //    yield return item;
                        //using (new StackPusher(stack, callExpression.Arguments.First()))
                        //using (new StackPusher(stack, expression))
                        //    foreach (var argExpression in callExpression.Arguments.Skip(1))
                        //        foreach (var item in GetExpressionsCore(stack, argExpression))
                        //            yield return item;
                    }
                    else
                        throw new NotSupportedException();
                }
                else
                {
                    if (callExpression.Method.DeclaringType == typeof(Queryable) || callExpression.Method.DeclaringType == typeof(Enumerable))
                    {
                        using (new StackPusher(stack, expression))
                            foreach (var argExpression in callExpression.Arguments.Skip(1))
                                foreach (var item in GetExpressionsCore(stack, argExpression))
                                    yield return item;
                    }
                    else
                        throw new NotSupportedException();
                }
                yield break;
            }

            using (new StackPusher(stack, expression))
                switch (expression.NodeType)
                {
                    case ExpressionType.Parameter:
                    case ExpressionType.Constant:
                        break;
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
                    case ExpressionType.Equal:
                        var binaryExpression = (BinaryExpression)expression;
                        foreach (var item in GetExpressionsCore(stack, binaryExpression.Left))
                            yield return item;
                        foreach (var item in GetExpressionsCore(stack, binaryExpression.Right))
                            yield return item;
                        break;
                    case ExpressionType.MemberAccess:
                        var memberExpression = (MemberExpression)expression;
                        foreach (var item in GetExpressionsCore(stack, memberExpression.Expression))
                            yield return item;
                        break;
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                    case ExpressionType.ArrayLength:
                    case ExpressionType.ArrayIndex:
                    case ExpressionType.Coalesce:
                    case ExpressionType.Conditional:
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                    case ExpressionType.Divide:
                    case ExpressionType.ExclusiveOr:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.Invoke:
                    case ExpressionType.LeftShift:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.ListInit:
                    case ExpressionType.MemberInit:
                    case ExpressionType.Modulo:
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.Negate:
                    case ExpressionType.UnaryPlus:
                    case ExpressionType.NegateChecked:
                    case ExpressionType.New:
                    case ExpressionType.NewArrayInit:
                    case ExpressionType.NewArrayBounds:
                    case ExpressionType.Not:
                    case ExpressionType.NotEqual:
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                    case ExpressionType.Power:
                    case ExpressionType.RightShift:
                    case ExpressionType.Subtract:
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
                        throw new NotSupportedException();
                    case ExpressionType.Call: // Handled above
                    default:
                        throw new NotSupportedException();
                }

            yield return new ExpressionEnumeratorItem(stack.ToList(), expression);
        }
    }

    public static string GetTableNameAndEntityType(ConstantExpression constantExpression)
    {
        var constantValue = constantExpression.Value;
        if (constantValue.GetType().GetGenericTypeDefinition() != typeof(System.Data.Linq.Table<>))
            throw new NotSupportedException();
        var genericParameterType = constantValue.GetType().GenericTypeArguments[0];
        var attribute = genericParameterType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute));
        var tableName = attribute.NamedArguments.Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString()!;
        return tableName;
    }


    private static IEnumerable<Expression> GetOrderedQuerableChainCall(Expression expression)
    {
        List<Expression> toReturn = new List<Expression>();
        while (true)
        {
            switch (expression.NodeType)
            {
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
                        throw new NotSupportedException();
                    break;
                default:
                    throw new NotSupportedException();
            }

        }
    }

    class TranslationContext
    {
        private Dictionary<Expression, SqlNode> _mapping = new();

        public Stack<Expression> Stack { get; private set; } = new();
        public IReadOnlyDictionary<Expression, SqlNode> Mapping => _mapping;

        public void AddMapping(Expression expression, SqlNode node)
            => _mapping.Add(expression, node);
    }

    interface IExpressionEnumeratorItem
    {
        IReadOnlyList<Expression> Stack { get; }
        Expression Expression { get; }
    }

    [DebuggerDisplay("{Expression}")]
    record ExpressionEnumeratorItem(IReadOnlyList<Expression> Stack, Expression Expression) : IExpressionEnumeratorItem;


    class SqlNode
    {

    }

    class SqlNoOpNode : SqlNode
    {

    }

    class SqlTableNode : SqlNode
    {
        public string TableName { get; private set; }

        public SqlTableNode(string tableName)
        {
            TableName = tableName;
        }
    }

    struct StackPusher : IDisposable
    {
        private readonly List<Expression> _stack;

        public StackPusher(List<Expression> stack, Expression expression)
        {
            _stack = stack;
            _stack.Add(expression);
        }


        public void Dispose()
        {
            _stack.RemoveAt(_stack.Count - 1);
        }
    }
}