using System.Data.Linq.Mapping;
using System.Linq;
using System.Linq.Expressions;

namespace Mindbox.Data.Linq.Tests.MultiStatementQuery;

internal static class ExpressionHelpers
{
    public static string GetTableName(ConstantExpression constantExpression)
    {
        var constantValue = constantExpression.Value;
        return GetTableName(constantValue);
    }

    private static string GetTableName(object constantValue)
    {
        if (!constantValue.GetType().IsGenericType || constantValue.GetType().GetGenericTypeDefinition() != typeof(System.Data.Linq.Table<>))
            return null;
        var genericParameterType = constantValue.GetType().GenericTypeArguments[0];
        var attribute = genericParameterType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute));
        var tableName = attribute.NamedArguments.Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString()!;
        return tableName;
    }

}
