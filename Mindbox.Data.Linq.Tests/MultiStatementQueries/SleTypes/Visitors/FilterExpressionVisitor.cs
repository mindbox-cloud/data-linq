using System.Linq.Expressions;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes.Visitors;

class FilterExpressionVisitor
{
    private readonly VisitorContext _visitorContext;

    /// <summary>
    /// Filter sle.
    /// </summary>
    public FilterChainPart FilterSle { get; private set; }

    public FilterExpressionVisitor(VisitorContext context)
    {
        _visitorContext = context;
        FilterSle = new FilterChainPart();
    }

    public void Visit(Expression node)
    {
        if (node is BinaryExpression binaryExpression)
        {
            var visitor = new FilterBinaryExpressionVisitor(FilterSle, _visitorContext);
            FilterSle.InnerExpression = visitor.FilterBinarySle;
            visitor.Visit(binaryExpression);
        }
        else
        {
            var visitor = new ChainExpressionVisitor(FilterSle, _visitorContext);
            FilterSle.InnerExpression = visitor.Chain;
            visitor.Visit(node);
            FilterSle.InnerExpression = visitor.Chain;
        }
    }
}
