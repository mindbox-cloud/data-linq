using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

class ChainSle : ITreeNodeSle
{
    private List<IChainPart> _items = new();

    public IReadOnlyList<IChainPart> Items => _items;

    public ISimplifiedLinqExpression ParentExpression { get; set; }

    public bool IsNegated { get; set; }

    public void AddChainPart(IChainPart chainPart)
    {
        if (chainPart.Chain != null)
            throw new InvalidOperationException();
        _items.Add(chainPart);
        chainPart.Chain = this;
    }
}