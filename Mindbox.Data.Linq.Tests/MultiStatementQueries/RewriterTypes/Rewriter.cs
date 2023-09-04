using Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;
using Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

