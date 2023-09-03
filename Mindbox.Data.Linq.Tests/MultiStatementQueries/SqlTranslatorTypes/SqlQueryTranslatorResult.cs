using System.Collections.Generic;
using System.Linq;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;
class SqlQueryTranslatorResult
{
    /// <summary>
    /// Command text.
    /// </summary>
    public string CommandText { get; private set; }

    /// <summary>
    /// Table read order.
    /// </summary>
    public IReadOnlyList<string> TableReadOrder { get; private set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="originalExpression">Original expression.</param>
    /// <param name="commandText">Command text</param>
    /// <param name="tableReadOrder">Table read order.</param>
    public SqlQueryTranslatorResult(string commandText, IEnumerable<string> tableReadOrder)
    {
        CommandText = commandText;
        TableReadOrder = tableReadOrder.ToArray();
    }
}

