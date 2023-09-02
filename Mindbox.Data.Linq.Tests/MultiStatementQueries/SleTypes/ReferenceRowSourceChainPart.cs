using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

class ReferenceRowSourceChainPart : IChainPart
{
    public ChainSle Chain { get; set; }
    public IChainPart ReferenceRowSource { get; set; }
}
