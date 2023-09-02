using System;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

static class TreeNodeSleExtensions
{
    public static ChainSle GetChain(this ITreeNodeSle node)
    {
        if (node is IChainPart chainPart)
            return chainPart.Chain;
        while (true)
        {
            if (node.ParentExpression == null)
                throw new InvalidOperationException("Top of each node should be chain part");
            if (node.ParentExpression is IChainPart parentChainPart)
                return parentChainPart.Chain;
            if (node.ParentExpression is not ITreeNodeSle parentNode)
                throw new InvalidOperationException("Parent should be chain part or tree node");
            node = parentNode;
        }
    }

    /// <summary>
    /// Shows that node is FilterBinarySle and represents top level equality statement
    /// </summary>
    /// <param name="node">Node.</param>
    /// <returns>True - yes, false - not.</returns>
    public static bool IsTopLevelChainEqualityStatement(this ITreeNodeSle node)
    {
        if (node is not FilterBinarySle filter)
            return false;
        if (filter.Operator != FilterBinaryOperator.ChainsEqual)
            return false;

        var parent = node.ParentExpression as FilterBinarySle;
        while (parent != null)
        {
            if (parent.Operator != FilterBinaryOperator.FilterBinaryAnd)
                return false;
            parent = parent.ParentExpression as FilterBinarySle;
        }

        return true;
    }

}
