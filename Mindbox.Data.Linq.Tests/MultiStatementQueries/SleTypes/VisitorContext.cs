using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

class VisitorContext
{
    public Dictionary<ParameterExpression, IChainPart> ParameterToSle { get; private set; } = new();
    public IDbColumnTypeProvider ColumnTypeProvider { get; private set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columnTypeProvider">Column provider.</param>
    public VisitorContext(IDbColumnTypeProvider columnTypeProvider)
    {
        ColumnTypeProvider = columnTypeProvider;
    }
}