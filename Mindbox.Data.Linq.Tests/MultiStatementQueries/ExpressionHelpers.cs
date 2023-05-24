using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries;

internal static class ExpressionHelpers
{
    public static string GetTableName(ConstantExpression constantExpression)
    {
        var constantValue = constantExpression.Value;
        return GetTableName(constantValue);
    }

    public static string GetTableName(object tableOb)
    {
        if (!tableOb.GetType().IsGenericType || tableOb.GetType().GetGenericTypeDefinition() != typeof(System.Data.Linq.Table<>))
            return null;
        var genericParameterType = tableOb.GetType().GenericTypeArguments[0];
        var attribute = genericParameterType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute));
        var tableName = attribute.NamedArguments.Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString()!;
        return tableName;
    }

    internal static IEnumerable<JoinCondition> GetJoinConditions(TranslationContext translationContext, TableNode table, IEnumerable<Expression> filters)
    {
        List<JoinCondition> toReturn = new();
        var filtersArray = filters.Where(f => f is BinaryExpression).ToArray();
        foreach (var filter in filters.Where(f => f is BinaryExpression))
        {
            var binaryExpression = (BinaryExpression)filter;
            if (binaryExpression.NodeType != ExpressionType.Equal)
                continue;
            var leftPart = ExtractTableField(translationContext, binaryExpression.Left);
            if (leftPart == null)
                continue;
            var rightPart = ExtractTableField(translationContext, binaryExpression.Right);
            if (rightPart == null)
                continue;
            if (leftPart.Table != table && rightPart.Table != table)
                continue;
            if (rightPart.Table == table)
                (leftPart, rightPart) = (rightPart, leftPart);
            var condition = new JoinCondition(leftPart.Field, rightPart.Table, rightPart.Field);
            if (!toReturn.Contains(condition))
                toReturn.Add(condition);
        }
        return toReturn;
    }

    private static TableAndField ExtractTableField(TranslationContext translationContext, Expression expression)
    {
        expression = Unwrap(expression);

        if (expression is MemberExpression memberExpression && memberExpression.Member is PropertyInfo memberProperty)
        {
            if (memberExpression.Expression is ParameterExpression memberParameterExpression)
            {
                var table = translationContext.GetTableFromExpression(memberParameterExpression);
                if (table != null)
                {
                    // Column access. Like User.Name
                    if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(ColumnAttribute)))
                        return new TableAndField(table, memberProperty.Name);
                    // Association access
                    if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
                    {
                        var associationAttribute = memberProperty.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
                        var nextTableName = memberProperty.PropertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                            .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                        var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
                        return new TableAndField(table, currentTableField);
                    }
                }
            }
            if (memberExpression.Expression is MemberExpression innerMemberExpression && innerMemberExpression.Member is PropertyInfo innerMemberProperty)
            {
                if (innerMemberExpression.Expression is ParameterExpression innerMemberParameterExpression)
                {
                    var table = translationContext.GetTableFromExpression(innerMemberParameterExpression);
                    if (table != null)
                    {
                        if (innerMemberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
                        {
                            var associationAttribute = innerMemberProperty.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
                            var nextTableName = innerMemberProperty.PropertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                                .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                            var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
                            var nextTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString();
                            if (nextTableField == memberProperty.Name)
                                return new TableAndField(table, currentTableField);
                        }
                    }
                }
            }
        }

        if (expression is ParameterExpression parameterExpression)
        {
            var table = translationContext.GetTableFromExpression(parameterExpression);
            if (table != null)
                return new TableAndField(table, translationContext.ColumnTypeProvider.GetPKFields(table.TableName).Single());
        }

        return null;

        static Expression Unwrap(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var unaryExpression = (UnaryExpression)expression;
                if (unaryExpression.IsLifted || unaryExpression.IsLiftedToNull || unaryExpression.Method != null)
                    throw new NotSupportedException();
                return unaryExpression.Operand;
            }
            return expression;
        }
    }

    private record TableAndField(TableNode Table, string Field);
}
