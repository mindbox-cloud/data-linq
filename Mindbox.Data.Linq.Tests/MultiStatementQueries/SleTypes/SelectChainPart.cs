using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

class SelectChainPart : ISelectChainPart
{
    public ChainSle Chain { get; set; }
    public ISimplifiedLinqExpression ParentExpression { get; set; }
    public SelectChainPartType ChainPartType { get; set; } = SelectChainPartType.Simple;
    public Dictionary<string, ChainSle> NamedChains { get; } = new Dictionary<string, ChainSle>();
}
