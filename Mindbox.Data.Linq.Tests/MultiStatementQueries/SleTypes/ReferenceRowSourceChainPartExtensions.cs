using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

static class ReferenceRowSourceChainPartExtensions
{
    public static IChainPart UnwrapCompletely(this ReferenceRowSourceChainPart referenceRowSourceChainPart)
    {
        while (true)
        {
            if (referenceRowSourceChainPart.ReferenceRowSource is ReferenceRowSourceChainPart inner)
                referenceRowSourceChainPart = inner;
            else
                return referenceRowSourceChainPart.ReferenceRowSource;
        }
    }
}
