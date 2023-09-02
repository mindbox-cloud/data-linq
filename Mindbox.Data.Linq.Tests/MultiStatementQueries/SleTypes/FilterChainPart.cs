namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

class FilterChainPart : IChainPartAndTreeNodeSle
{
    public ChainSle Chain { get; set; }
    public ISimplifiedLinqExpression ParentExpression { get; set; }
    public ISimplifiedLinqExpression InnerExpression { get; set; }
}
