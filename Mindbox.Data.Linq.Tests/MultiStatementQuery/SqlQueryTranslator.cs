using Azure;
using Castle.Components.DictionaryAdapter.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests.MultiStatementQuery;


class SqlQueryTranslator
{
    public static SqlQueryTranslatorResult Transalate(Expression node)
    {
        var context = new TranslationContext();
        var root = TransalateCore(context, node);
        SimplifyTree(root);

        throw new NotImplementedException();

        // var command = SqlTreeCommandBuilder.Build(context.SqlTree);

        // return new SqlQueryTranslatorResult(command);
    }

    private static SqlNode TransalateCore(TranslationContext context, Expression expression)
    {
        SqlNode root = null;
        var items = GetExpressionChains(expression).ToArray();
        foreach (var item in items)
        {
            var fullStack = new List<Expression>(item.Stack) { item.Expression };
            for (int i = 0; i < fullStack.Count; i++)
            {
                if (context.Mapping.ContainsKey(fullStack[i]))
                    continue;
                var toMap = new ExpressionToMap(context, fullStack, i);
                var node = MapExpressions(toMap);
                if (root == null)
                    root = node;

                if (toMap.PreviousNode != null)
                    toMap.PreviousNode.Children.Add(node);

                context.AddMapping(fullStack[i], node);
            }
        }
        return root;
    }

    private static void SimplifyTree(SqlNode node)
    {
        RemoveNoOps(null, node);
        MergeFilters(node);

        void MergeFilters(SqlNode node)
        {
            foreach (var child in node.Children.ToArray())
                MergeFilters(child);

            for (int i = 1; i < node.Children.Count; i++)
                if (node.Children[i - 1] is SqlFilterNode && node.Children[i] is SqlFilterNode)
                {
                    node.Children[i - 1].Children.AddRange(node.Children[i].Children);
                    node.Children.RemoveAt(i);
                    i--;
                }
        }

        void RemoveNoOps(SqlNode parentNode, SqlNode node)
        {
            foreach (var child in node.Children.ToArray())
                RemoveNoOps(node, child);

            if (node is SqlNoOpNode)
            {
                var index = parentNode.Children.IndexOf(node);
                parentNode.Children.RemoveAt(index);
                parentNode.Children.InsertRange(index, node.Children);
            }
        }
    }

    record ExpressionToMap(TranslationContext Context, List<Expression> Stack, int Index)
    {
        private ExpressionToMap _previous;

        public Expression Expression => Stack[Index];

        public bool IsFirst => Index == 0;

        public ExpressionToMap Previous
        {
            get
            {
                if (Index == 0)
                    return null;
                if (_previous == null)
                    _previous = new(Context, Stack, Index - 1);
                return _previous;
            }
        }

        public SqlNode PreviousNode => PreviousExpression == null ? null : Context.Mapping[PreviousExpression];
        public Expression PreviousExpression => Index == 0 ? null : Stack[Index - 1];
        public Expression PreviousPreviousExpression => Index <= 1 ? null : Stack[Index - 2];

        public SqlFilterNode? GetFilterNodeForParameter(ParameterExpression parameterExpression)
        {
            var current = Previous;
            while (current != null)
            {
                var node = Context.Mapping[current.Expression];
                if (node is SqlFilterNode filterNode)
                {
                    var lambda = (LambdaExpression)current.Expression;
                    var lambdaCallExpression = (MethodCallExpression)current?.Previous?.Previous?.Expression;
                    var lambdaFilterParameterExpression = lambdaCallExpression.Method switch
                    {
                        { Name: "Where" } => lambda.Parameters[0],
                        _ => throw new NotSupportedException()
                    };
                    if (lambdaFilterParameterExpression == parameterExpression)
                        return filterNode;
                }

                current = current.Previous;
            }
            return null;
        }
    }

    private static SqlNode MapExpressions(ExpressionToMap toMap)
    {
        var expression = toMap.Expression;
        switch (expression.NodeType)
        {
            case ExpressionType.Constant:
                var tableName = GetTableName((ConstantExpression)expression);
                if (string.IsNullOrEmpty(tableName))
                    return new SqlNoOpNode(toMap.PreviousNode.TableOwner.TableOwner);
                return new SqlTableNode(tableName);
            case ExpressionType.Call:
                var callExpression = (MethodCallExpression)expression;
                if (callExpression.Method.DeclaringType == typeof(Queryable) || callExpression.Method.DeclaringType == typeof(Enumerable))
                    return new SqlNoOpNode(toMap.PreviousNode.TableOwner.TableOwner);
                throw new NotSupportedException();
            case ExpressionType.Quote:
                var quoteExpression = (UnaryExpression)expression;
                if (quoteExpression.Method == null)
                    return new SqlNoOpNode(toMap.PreviousNode.TableOwner.TableOwner);
                throw new NotSupportedException();
            case ExpressionType.Lambda:
                var lambdaExpression = (LambdaExpression)expression;
                if (lambdaExpression.ReturnType == typeof(bool) && toMap.PreviousPreviousExpression is MethodCallExpression lambdCallExpression &&
                    (lambdCallExpression.Method.DeclaringType == typeof(Queryable) || lambdCallExpression.Method.DeclaringType == typeof(Enumerable)))
                {
                    return new SqlFilterNode(toMap.PreviousNode.TableOwner);
                }
                throw new NotSupportedException();
            case ExpressionType.Parameter:
                var filterNodeForParameter = toMap.GetFilterNodeForParameter((ParameterExpression)expression)
                    ?? throw new NotSupportedException("Filter node not found but was expected.");
                return new SqlTableAccessNode(filterNodeForParameter.TableOwner);
            case ExpressionType.MemberAccess:
                var memberExpression = (MemberExpression)expression;
                if (memberExpression.Member is PropertyInfo propertyInfo)
                {
                    if (toMap.PreviousNode is SqlTableAccessNode memberFromTableAccessNode)
                    {
                        if (propertyInfo.CustomAttributes.Any(p => p.AttributeType == typeof(ColumnAttribute)))
                            return new SqlTableFieldAccessNode(toMap.PreviousNode.TableOwner, propertyInfo.Name);
                    }
                }
                throw new NotSupportedException();
            case ExpressionType.And:
            case ExpressionType.AndAlso:
            case ExpressionType.Or:
            case ExpressionType.OrElse:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Equal:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
                return new SqlNoOpNode(toMap.PreviousNode.TableOwner);
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.ArrayLength:
            case ExpressionType.ArrayIndex:
            case ExpressionType.Coalesce:
            case ExpressionType.Conditional:
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
            case ExpressionType.Divide:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.Invoke:
            case ExpressionType.LeftShift:
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
            default:
                throw new NotSupportedException();
        }
    }

    private static IEnumerable<IExpressionEnumeratorItem> GetExpressionChains(Expression node)
    {
        return GetExpressionsCore(new List<Expression>(), node);

        static IEnumerable<IExpressionEnumeratorItem> GetExpressionsCore(List<Expression> stack, Expression expression, bool? isLastInChain = null)
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
                        yield return new ExpressionEnumeratorItem(stack.ToList(), expression);
                    yield break;
            }


            using (new StackPusher(stack, expression))
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
                    default:
                        throw new NotSupportedException();
                }

            // yield return new ExpressionEnumeratorItem(stack.ToList(), expression);
        }
    }

    public static string GetTableName(ConstantExpression constantExpression)
    {
        var constantValue = constantExpression.Value;
        if (!constantValue.GetType().IsGenericType || constantValue.GetType().GetGenericTypeDefinition() != typeof(System.Data.Linq.Table<>))
            return null;
        var genericParameterType = constantValue.GetType().GenericTypeArguments[0];
        var attribute = genericParameterType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute));
        var tableName = attribute.NamedArguments.Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString()!;
        return tableName;
    }


    private static IEnumerable<Expression> GetReorderedChainCall(Expression expression)
    {
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
                        throw new NotSupportedException();
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

    abstract class SqlNode
    {
        public abstract SqlTableNode TableOwner { get; }
        public List<SqlNode> Children { get; } = new List<SqlNode>();
    }

    [DebuggerDisplay("NoOp")]
    class SqlNoOpNode : SqlNode
    {
        private SqlTableNode _tableOwner;

        public override SqlTableNode TableOwner => _tableOwner;

        public SqlNoOpNode(SqlTableNode tableOwner)
        {
            _tableOwner = tableOwner;
        }
    }

    [DebuggerDisplay("Filter")]
    class SqlFilterNode : SqlNode
    {
        private SqlTableNode _tableOwner;

        public override SqlTableNode TableOwner => _tableOwner;

        public SqlFilterNode(SqlTableNode tableOwner)
        {
            _tableOwner = tableOwner;
        }
    }

    [DebuggerDisplay("Table access: {TableOwner.TableName}")]
    class SqlTableAccessNode : SqlNode
    {
        private SqlTableNode _tableOwner;

        public override SqlTableNode TableOwner => _tableOwner;

        public SqlTableAccessNode(SqlTableNode tableOwner)
        {
            _tableOwner = tableOwner;
        }
    }

    [DebuggerDisplay("Table field access: {TableOwner.TableName,nq}.{ColumnName,nq}")]
    class SqlTableFieldAccessNode : SqlNode
    {
        private SqlTableNode _tableOwner;

        public override SqlTableNode TableOwner => _tableOwner;

        public string ColumnName { get; }

        public SqlTableFieldAccessNode(SqlTableNode tableOwner, string columnName)
        {
            _tableOwner = tableOwner;
            ColumnName = columnName;
        }
    }

    [DebuggerDisplay("Table")]
    class SqlTableNode : SqlNode
    {
        public string TableName { get; private set; }

        public override SqlTableNode TableOwner => this;

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