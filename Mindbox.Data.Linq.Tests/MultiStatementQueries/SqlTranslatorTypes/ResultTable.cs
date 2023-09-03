using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;


/// <summary>
/// Table.
/// </summary>
class ResultTable : IEnumerable<ResultRow>
{
    private Dictionary<string, Dictionary<string, List<ResultRow>>> _stringColumnIndexes = new();
    private Dictionary<string, Dictionary<long, List<ResultRow>>> _numberColumnIndexes = new();
    private HashSet<string> _uniqueColumns = new();
    private List<ResultRow> _rows = new List<ResultRow>();

    /// <summary>
    /// Rows.
    /// </summary>
    public IReadOnlyList<ResultRow> Rows => _rows;
    /// <summary>
    /// Result set.
    /// </summary>
    public ResultSet Set { get; private set; }
    /// <summary>
    /// Name.
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// Columns.
    /// </summary>
    public ResultColumns Columns { get; private set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="set">Set.</param>
    /// <param name="name">Name.</param>
    /// <param name="columns">Columns.</param>
    public ResultTable(ResultSet set, string name, ResultColumns columns)
    {
        Set = set;
        Name = name;
        Columns = columns;
    }

    /// <summary>
    /// Gets rows by column name.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <returns>Result rows.</returns>
    public IEnumerable<ResultRow> GetByColumn(string columnName, long value)
    {
        if (_numberColumnIndexes.TryGetValue(columnName, out var index))
            return index.TryGetValue(value, out var result) ? result : Enumerable.Empty<ResultRow>();

        return Filter();

        IEnumerable<ResultRow> Filter()
        {
            foreach (var row in _rows)
                if (row.GetValue<long>(columnName) == value)
                    yield return row;
        }
    }


    /// <summary>
    /// Gets rows by column name.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <returns>Result rows.</returns>
    public IEnumerable<ResultRow> GetByColumn(string columnName, string value)
    {
        if (_stringColumnIndexes.TryGetValue(columnName, out var index))
            return index.TryGetValue(value, out var result) ? result : Enumerable.Empty<ResultRow>();

        return Filter();

        IEnumerable<ResultRow> Filter()
        {
            foreach (var row in _rows)
                if (row.GetValue<string>(columnName) == value)
                    yield return row;
        }
    }

    /// <summary>
    /// Reads rows.
    /// </summary>
    /// <param name="dataReader">Data reader.</param>
    public void ReadRows(IDataReader dataReader)
    {
        while (dataReader.Read())
        {
            var row = ResultRow.Read(this, dataReader);
            foreach (var (columnName, index) in _stringColumnIndexes)
            {
                var value = row.GetValue<string>(columnName);
                if (!index.TryGetValue(value, out var rows))
                {
                    rows = new List<ResultRow>();
                    index.Add(value, rows);
                }
                else if (_uniqueColumns.Contains(columnName))
                    continue;
                rows.Add(row);
            }

            foreach (var (columnName, index) in _numberColumnIndexes)
            {
                var value = row.GetValue<long>(columnName);
                if (!index.TryGetValue(value, out var rows))
                {
                    rows = new List<ResultRow>();
                    index.Add(value, rows);
                }
                else if (_uniqueColumns.Contains(columnName))
                    continue;
                rows.Add(row);
            }
        }
    }

    /// <summary>
    /// Adds index by column.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="isUnique">Shows that index is unique.</param>
    public void AddIndex(string columnName, bool isUnique)
    {
        if (_rows.Count > 0)
            throw new InvalidOperationException();
        var column = Columns.GetByName(columnName);
        if (column.ValueType == typeof(int) || column.ValueType == typeof(long))
            _numberColumnIndexes.Add(columnName, new());
        else if (column.ValueType == typeof(string))
            _stringColumnIndexes.Add(columnName, new());
        else
            throw new NotSupportedException();
        if (isUnique)
            _uniqueColumns.Add(columnName);
    }

    public IEnumerator<ResultRow> GetEnumerator() => _rows.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
