using Castle.Components.DictionaryAdapter;
using Snapshooter.MSTest;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries;


class SqlQueryTranslator
{
    public static SqlQueryTranslatorResult Transalate(Expression node, IDbColumnTypeProvider columntTypeProvider)
    {
        var query = TranslateCore(node);
        // SimplifyTree(root);

        var command = SqlTreeCommandBuilder.Build(query, columntTypeProvider);

        return new SqlQueryTranslatorResult(command);
    }

    private static MultiStatementQuery TranslateCore(Expression expression)
    {
        var context = new TranslationContext();
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
                if (lambdaExpression.ReturnType == typeof(bool) && chainItem.PreviousPreviousExpression is MethodCallExpression lambdCallExpression &&
                        (lambdCallExpression.Method.DeclaringType == typeof(Queryable) || lambdCallExpression.Method.DeclaringType == typeof(Enumerable)))
                {
                    var filterParameterExpression = lambdCallExpression.Method switch
                    {
                        { Name: "Where" or "Any" } when lambdCallExpression.Method.GetParameters().Length == 2
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
                        var nextTableName = propertyInfo.PropertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                            .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                        var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
                        var associationTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString();
                        var currentTable = context.CurrentTable;
                        var associationTable = context.AddTable(nextTableName, null);
                        associationTable.AddJoinCondition(new JoinCondition(associationTableField, currentTable, currentTableField));
                        return;
                    }
                    //else if (toMap.PreviousNode is SqlAssociationFieldNode associationAccessNode)
                    //{
                    //    if (propertyInfo.CustomAttributes.Any(p => p.AttributeType == typeof(ColumnAttribute)))
                    //        return new SqlDataFieldNode(toMap.PreviousNode.TableOwner, propertyInfo.Name);
                    //    var associationAttribute = propertyInfo.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
                    //    if (associationAttribute != null)
                    //    {
                    //        var nextTableName = propertyInfo.PropertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                    //            .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                    //        return new SqlAssociationFieldNode(
                    //            new SqlTableNode(nextTableName),
                    //            associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString(),
                    //            toMap.PreviousNode.TableOwner,
                    //            associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString());
                    //    }
                    //}
                }
                else if (memberExpression.Expression is ConstantExpression memberConstantExpression)// Invocation of constant
                {
                    var memberConstantValue = Expression.Lambda(memberExpression).Compile().DynamicInvoke();
                    if (memberConstantValue == null || memberConstantValue.GetType() == typeof(string))
                        return;
                    var memberTableName = ExpressionHelpers.GetTableName(memberConstantValue);
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
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.ArrayLength:
            case ExpressionType.ArrayIndex:
            case ExpressionType.Coalesce:
            case ExpressionType.Conditional:
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

    public record ExpressionChainItem(ExpressionChain Chain, int Index)
    {
        public Expression Expression => Chain.Items[Index];
        public ExpressionChainItem PreviousChainItem => new ExpressionChainItem(Chain, Index - 1);
        public Expression PreviousPreviousExpression => PreviousChainItem.PreviousChainItem.Expression;
    }

    /*
    private static void SimplifyTree(SqlNode node)
    {
        RemoveNoOps(null, node);
        MergeFilters(node);
        MergeTableAccessOnSameLevel(node);
        RemoveDuplicatedAssociationFields(node);

        void RemoveDuplicatedAssociationFields(SqlNode node)
        {
            for (int i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is SqlAssociationFieldNode associationField)
                {
                    var sameNode = node.Children.Where(c => c != associationField).OfType<SqlAssociationFieldNode>()
                        .Where(o => o.TableName == associationField.TableName && o.ColumnName == associationField.ColumnName).FirstOrDefault();
                    if (sameNode != null)
                    {
                        RewriteTableOwner(associationField, associationField.TableOwner, sameNode.TableOwner);
                        sameNode.Children.InsertRange(0, associationField.Children);
                        node.Children.RemoveAt(i);
                        i--;
                    }
                }
            }

            foreach (var child in node.Children.ToArray())
                RemoveDuplicatedAssociationFields(child);
        }

        void RewriteTableOwner(SqlNode node, SqlTableNode currentOwner, SqlTableNode newOwner)
        {
            if (node.TableOwner == currentOwner)
                node.TryRewriteTableOwner(newOwner);
            foreach (var child in node.Children)
                RewriteTableOwner(child, currentOwner, newOwner);
        }

        void MergeTableAccessOnSameLevel(SqlNode node)
        {
            for (int i = 1; i < node.Children.Count; i++)
                if (node.Children[i - 1] is SqlTableAccessNode tableAccessPrevious && node.Children[i] is SqlTableAccessNode tableAccessCurrent
                    && tableAccessPrevious.TableOwner == tableAccessCurrent.TableOwner)
                {
                    node.Children[i - 1].Children.AddRange(node.Children[i].Children);
                    node.Children.RemoveAt(i);
                    i--;
                }

            foreach (var child in node.Children.ToArray())
                MergeTableAccessOnSameLevel(child);
        }

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

        public SqlFilterNode GetFilterNodeForParameter(ParameterExpression parameterExpression)
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
                        { Name: "Any" } => lambda.Parameters[0],
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
                            return new SqlDataFieldNode(toMap.PreviousNode.TableOwner, propertyInfo.Name);
                        var associationAttribute = propertyInfo.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
                        if (associationAttribute != null)
                        {
                            var nextTableName = propertyInfo.PropertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                                .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                            return new SqlAssociationFieldNode(
                                new SqlTableNode(nextTableName),
                                associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString(),
                                toMap.PreviousNode.TableOwner,
                                associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString());
                        }
                    }
                    else if (toMap.PreviousNode is SqlAssociationFieldNode associationAccessNode)
                    {
                        if (propertyInfo.CustomAttributes.Any(p => p.AttributeType == typeof(ColumnAttribute)))
                            return new SqlDataFieldNode(toMap.PreviousNode.TableOwner, propertyInfo.Name);
                        var associationAttribute = propertyInfo.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
                        if (associationAttribute != null)
                        {
                            var nextTableName = propertyInfo.PropertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                                .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                            return new SqlAssociationFieldNode(
                                new SqlTableNode(nextTableName),
                                associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString(),
                                toMap.PreviousNode.TableOwner,
                                associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString());
                        }
                    }
                }
                else if (memberExpression.Expression is ConstantExpression) // Some invocation of constant
                {
                    var memberConstantValue = Expression.Lambda(memberExpression).Compile().DynamicInvoke();
                    var memberTableName = GetTableName(memberConstantValue);
                    if (!string.IsNullOrEmpty(memberTableName))
                        return new SqlTableNode(memberTableName);
                    return new SqlNoOpNode(toMap.PreviousNode.TableOwner);
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
                return new SqlNoOpNode(toMap.PreviousNode.TableOwner);
            case ExpressionType.Equal:
                var joinConditioNode = TryExtractJoinConition(toMap);
                if (joinConditioNode != null)
                    return joinConditioNode;
                return new SqlNoOpNode(toMap.PreviousNode.TableOwner);
            case ExpressionType.Not:
                var notExpression = (UnaryExpression)expression;
                if (notExpression.IsLifted || notExpression.IsLiftedToNull || notExpression.Method != null)
                    throw new NotSupportedException();
                return new SqlNoOpNode(toMap.PreviousNode.TableOwner);
            case ExpressionType.Convert:
                var convertExpression = (UnaryExpression)expression;
                if (convertExpression.IsLifted || convertExpression.IsLiftedToNull || convertExpression.Method != null)
                    throw new NotSupportedException();
                return new SqlNoOpNode(toMap.PreviousNode.TableOwner);
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.ArrayLength:
            case ExpressionType.ArrayIndex:
            case ExpressionType.Coalesce:
            case ExpressionType.Conditional:
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

    private static SqlJoinConditionFieldNode TryExtractJoinConition(ExpressionToMap toMap)
    {
        var binaryExpression = toMap.Expression as BinaryExpression;
        if (binaryExpression == null)
            return null;

        // Unwrap
        var left = Unwrap(binaryExpression.Left);
        var right = Unwrap(binaryExpression.Right);

        // Extract field
        var fieldLeft = ExtractField(left);
        var fieldRight = ExtractField(right);

        if (!fieldLeft.HasValue || !fieldRight.HasValue)
            return null;

        var previousTable = toMap.PreviousNode.TableOwner;
        if (previousTable == fieldRight.Value.Table)
        {
            var temp = fieldLeft;
            fieldLeft = fieldRight;
            fieldRight = temp;
        }

        return new SqlJoinConditionFieldNode(fieldLeft.Value.Table, fieldLeft.Value.Column, fieldRight.Value.Table, fieldRight.Value.Column);

        static (SqlTableNode Table, string Column)? ExtractField(Expression expression)
        {
            throw new NotImplementedException();
            //turn null;
        }

        static Expression Unwrap(Expression expression)
        {
            while (true)
            {
                if (expression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
                    if (unaryExpression.IsLifted || unaryExpression.IsLiftedToNull || unaryExpression.Method != null)
                    {
                        expression = unaryExpression.Operand;
                        continue;
                    }
                return expression;
            }
        }
    }

    interface IExpressionEnumeratorItem
    {
        IReadOnlyList<Expression> Stack { get; }
        Expression Expression { get; }
    }

    [DebuggerDisplay("{Expression}")]
    record ExpressionEnumeratorItem(IReadOnlyList<Expression> Stack, Expression Expression) : IExpressionEnumeratorItem;

    public abstract class SqlNode
    {
        public abstract SqlTableNode TableOwner { get; }
        public List<SqlNode> Children { get; } = new List<SqlNode>();

        public abstract void TryRewriteTableOwner(SqlTableNode newOwner);
    }

    [DebuggerDisplay("NoOp")]
    public class SqlNoOpNode : SqlNode
    {
        private SqlTableNode _tableOwner;

        public override SqlTableNode TableOwner => _tableOwner;

        public SqlNoOpNode(SqlTableNode tableOwner)
        {
            _tableOwner = tableOwner;
        }

        public override void TryRewriteTableOwner(SqlTableNode newOwner)
            => _tableOwner = newOwner;
    }

    [DebuggerDisplay("Filter")]
    public class SqlFilterNode : SqlNode
    {
        private SqlTableNode _tableOwner;

        public override SqlTableNode TableOwner => _tableOwner;

        public SqlFilterNode(SqlTableNode tableOwner)
        {
            _tableOwner = tableOwner;
        }

        public override void TryRewriteTableOwner(SqlTableNode newOwner)
            => _tableOwner = newOwner;
    }

    [DebuggerDisplay("Table access: {TableOwner.TableName}")]
    public class SqlTableAccessNode : SqlNode
    {
        private SqlTableNode _tableOwner;

        public override SqlTableNode TableOwner => _tableOwner;

        public SqlTableAccessNode(SqlTableNode tableOwner)
        {
            _tableOwner = tableOwner;
        }


        public override void TryRewriteTableOwner(SqlTableNode newOwner)
            => _tableOwner = newOwner;
    }

    [DebuggerDisplay("Data field: {TableOwner.TableName,nq}.{ColumnName,nq}")]
    public class SqlDataFieldNode : SqlNode
    {
        private SqlTableNode _tableOwner;

        public override SqlTableNode TableOwner => _tableOwner;

        public string ColumnName { get; }

        public SqlDataFieldNode(SqlTableNode tableOwner, string columnName)
        {
            _tableOwner = tableOwner;
            ColumnName = columnName;
        }

        public override void TryRewriteTableOwner(SqlTableNode newOwner)
            => _tableOwner = newOwner;
    }

    [DebuggerDisplay("Association field: {PreviousTableOwner.TableName,nq}.{PreviousColumnName,nq} = {TableOwner.TableName,nq}.{ColumnName,nq}")]
    public class SqlAssociationFieldNode : SqlTableNode
    {
        private SqlTableNode _tableOwner;

        public override SqlTableNode TableOwner => _tableOwner;
        public string ColumnName { get; }
        public SqlTableNode PreviousTableOwner { get; }
        public string PreviousColumnName { get; }

        public SqlAssociationFieldNode(SqlTableNode tableOwner, string columnName, SqlTableNode previousTableOwner, string previousColumnName)
            : base(tableOwner.TableName)
        {
            _tableOwner = tableOwner;
            ColumnName = columnName;
            PreviousTableOwner = previousTableOwner;
            PreviousColumnName = previousColumnName;
        }

        public override void TryRewriteTableOwner(SqlTableNode newOwner)
            => _tableOwner = newOwner;
    }

    [DebuggerDisplay("SqlJoinConditionFieldNode : {PreviousTableOwner.TableName,nq}.{PreviousColumnName,nq} = {TableOwner.TableName,nq}.{ColumnName,nq}")]
    public class SqlJoinConditionFieldNode : SqlTableNode
    {
        private SqlTableNode _tableOwner;

        public override SqlTableNode TableOwner => _tableOwner;
        public string ColumnName { get; }
        public SqlTableNode PreviousTableOwner { get; }
        public string PreviousColumnName { get; }

        public SqlJoinConditionFieldNode(SqlTableNode tableOwner, string columnName, SqlTableNode previousTableOwner, string previousColumnName)
            : base(tableOwner.TableName)
        {
            _tableOwner = tableOwner;
            ColumnName = columnName;
            PreviousTableOwner = previousTableOwner;
            PreviousColumnName = previousColumnName;
        }

        public override void TryRewriteTableOwner(SqlTableNode newOwner)
            => _tableOwner = newOwner;
    }

    [DebuggerDisplay("Table")]
    public class SqlTableNode : SqlNode
    {
        public string TableName { get; private set; }

        public override SqlTableNode TableOwner => this;

        public SqlTableNode(string tableName)
        {
            TableName = tableName;
        }

        public override void TryRewriteTableOwner(SqlTableNode newOwner)
            => throw new NotSupportedException();
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



        throw new NotSupportedException();
        /*
        var conditionsArray = conditions.ToArray();
        foreach (var matchingTable in _tables)
        {
            if (matchingTable.TableName != rightTableName)
                continue;

            if (conditionsArray.Length != matchingTable.JoinConditions.Count)
                continue;

            bool hasMisMatch = false;
            foreach (var condition in conditions)
                if (matchingTable.JoinConditions.Count(m => m == condition) != 1)
                {
                    hasMisMatch = true;
                    break;
                }

            if (hasMisMatch)
                continue;

            return matchingTable;
        }

        var joinedTable = new TableNode(rightTableName);
        foreach (var joinCondition in conditionsArray)
            joinedTable.AddJoinConitoin(joinCondition);

        foreach (var condition in conditions)
        {
            condition.LeftTable.AddUsedField(condition.FieldLeft);
            joinedTable.AddUsedField(condition.FieldRight);
        }

        return joinedTable;
        */
    }
}

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
            _ => throw new NotSupportedException()
        };
    }
}