using System;
using System.Linq;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

static class ChainPartSleExtensions
{
    public static IChainPart GetNext(this IChainPart chainPart)
    {
        for (int i = 0; i < chainPart.Chain.Items.Count; i++)
        {
            if (chainPart == chainPart.Chain.Items[i])
                return i == 0 ? null : chainPart.Chain.Items[i - 1];
        }
        throw new InvalidOperationException();
    }

    public static IChainPart GetPrevious(this IChainPart chainPart)
    {
        var index = -1;
        for (int i = 0; i < chainPart.Chain.Items.Count; i++)
        {
            if (chainPart.Chain.Items[i] == chainPart)
            {
                index = i;
                break;
            }
        }
        if (index == -1)
            throw new InvalidOperationException();
        if (index == 0)
            return null;
        return chainPart.Chain.Items[index - 1];
    }

    public static bool IsLast(this IChainPart chainPart)
        => chainPart.Chain.Items.Last() == chainPart;
}
