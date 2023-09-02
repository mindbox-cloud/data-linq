using System.Collections.Generic;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

interface ISelectChainPart : IRowSourceChainPart, IChainPartAndTreeNodeSle
{
    SelectChainPartType ChainPartType { get; set; }
    Dictionary<string, ChainSle> NamedChains { get; }
}
