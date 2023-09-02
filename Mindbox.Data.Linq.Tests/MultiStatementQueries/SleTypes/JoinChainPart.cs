using System.Collections.Generic;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

class JoinChainPart : ISelectChainPart
{
    public ChainSle Inner { get; set; }
    public ChainSle InnerKeySelectorSle { get; set; }
    public ChainSle OuterKeySelectorSle { get; set; }
    public ChainSle Chain { get; set; }
    public ISimplifiedLinqExpression ParentExpression { get; set; }
    public SelectChainPartType ChainPartType { get; set; } = SelectChainPartType.Simple;
    public Dictionary<string, ChainSle> NamedChains { get; } = new Dictionary<string, ChainSle>();
}
