using System.Collections.Generic;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;

class ResultColumns
{
    private List<ResultColumn> _columns = new();
    private Dictionary<string, ResultColumn> _columnsByName;
    private Dictionary<string, int> _columnOrdinal;

    public IReadOnlyList<ResultColumn> OrderedColumns => _columns;

    public ResultColumns(IEnumerable<ResultColumn> columns)
    {
        int index = 0;
        foreach (var column in columns)
        {
            _columns.Add(column);
            _columnOrdinal.Add(column.Name, index++);
            _columnsByName.Add(column.Name, column);
        }
    }

    public int GetOrdinal(string columnName) => _columnOrdinal[columnName];

    public ResultColumn GetByName(string name) => _columnsByName[name];
}
