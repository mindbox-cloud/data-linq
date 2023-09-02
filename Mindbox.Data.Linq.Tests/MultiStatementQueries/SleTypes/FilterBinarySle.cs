using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

class FilterBinarySle : ITreeNodeSle
{
    public ISimplifiedLinqExpression ParentExpression { get; set; }
    public ISimplifiedLinqExpression LeftExpression { get; set; }
    public ISimplifiedLinqExpression RightExpression { get; set; }
    public FilterBinaryOperator Operator { get; set; }
}
