using Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;
using System;
using System.Collections;
using System.Linq.Expressions;

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

        var methodInfo = typeof(EnumerableExtensions).GetMethod(nameof(EnumerableExtensions.Any));

        var anyExpression = Expression.Call(null, methodInfo, rewrittenExpression);
        return Expression.Lambda<Func<ResultSet, bool>>(anyExpression, parameter);
    }
}

internal static class EnumerableExtensions
{
    public static bool Any(this IEnumerable enumerable)
    {
        foreach (var _ in enumerable)
            return true;
        return false;
    }
}

