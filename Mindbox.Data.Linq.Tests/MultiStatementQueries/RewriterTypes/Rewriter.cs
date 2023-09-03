using Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;
using Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.RewriterTypes;

internal class Rewriter
{
    public Expression<Func<ResultSet, bool>> Rewrite(Expression expression)
    {
        var parameter = Expression.Parameter(typeof(ResultSet), "resultSet");
        var visitor = new RewriterVisitor(parameter);
        var rewrittenExpression = visitor.Visit(expression);

        var anyExpression = Expression.Call(null, GetMethod(() => Enumerable.Any<ResultRow>(null)), rewrittenExpression);
        return Expression.Lambda<Func<ResultSet, bool>>(anyExpression, parameter);
    }

    public MethodInfo GetMethod(Expression<Action> e) => (e.Body as MethodCallExpression).Method;
}

internal class RewriterVisitor : ExpressionVisitor
{
    private readonly ParameterExpression _resultSetParameter;
    private readonly MethodInfo _getTableMethodInfo;

    public RewriterVisitor(ParameterExpression resultSetParameter)
    {
        _resultSetParameter = resultSetParameter;
        _getTableMethodInfo = typeof(ResultSet).GetMethod(nameof(ResultSet.GetTable));
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var tableName = ExpressionHelpers.GetTableName(node);
        if (!string.IsNullOrEmpty(tableName))
            return Expression.Call(_resultSetParameter, _getTableMethodInfo, Expression.Constant(tableName));
        return base.VisitConstant(node);
    }

    [return: NotNullIfNotNull("node")]
    public override Expression Visit(Expression node)
    {
        return base.Visit(node);
    }
}


