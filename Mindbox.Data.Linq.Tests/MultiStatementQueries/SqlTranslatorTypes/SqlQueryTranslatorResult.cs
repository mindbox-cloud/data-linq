using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;
class SqlQueryTranslatorResult
{
    private List<string> _tableReadOrder = new();
    private IDbColumnTypeProvider _columnTypeProvider;


    /// <summary>
    /// Command text.
    /// </summary>
    public string CommandText { get; private set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="commandText">Command text</param>
    /// <param name="columnTypeProvider">Column type provider.</param>
    /// <param name="tableReadOrder">Table read order.</param>
    public SqlQueryTranslatorResult(string commandText, IDbColumnTypeProvider columnTypeProvider, IEnumerable<string> tableReadOrder)
    {
        CommandText = commandText;
        _tableReadOrder.AddRange(tableReadOrder);
        _columnTypeProvider = columnTypeProvider;
    }

    /// <summary>
    /// Executes command and returns result set.
    /// </summary>
    /// <param name="connection">Connection.</param>
    /// <returns>ResultSet</returns>
    public ResultSet Execute(IDbConnection connection)
    {
        using var command = connection.CreateCommand();

        command.CommandText = CommandText;

        using var reader = command.ExecuteReader();

        ResultSet toReturn = new();
        int tableIndex = 0;
        do
        {
            if (!toReturn.HasTable(_tableReadOrder[tableIndex]))
            {
                var columns = CreateColumns(reader);
                var table = new ResultTable(toReturn, _tableReadOrder[tableIndex], columns);
                toReturn.AddTable(table);
            }
            toReturn.GetTable(_tableReadOrder[tableIndex]).ReadRows(reader);
            tableIndex++;
        } while (reader.NextResult());

        return toReturn;
    }

    private ResultColumns CreateColumns(IDataReader reader)
    {
        List<ResultColumn> columns = new();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            var type = reader.GetFieldType(i);
            columns.Add(new(name, type));
        }
        return new ResultColumns(columns);
    }
}

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

class ResultColumn
{
    public Type ValueType { get; private set; }
    public string Name { get; private set; }

    public ResultColumn(string name, Type valueType)
    {
        Name = name;
        ValueType = valueType;
    }

    public static bool IsFloatNumeric(Type type) => type == typeof(decimal);

    public static bool IsIntegerNumeric(Type type) => type == typeof(int) || type == typeof(long);
}

class ResultSet
{
    private Dictionary<string, ResultTable> _tables = new();

    public void AddTable(ResultTable table) => _tables.Add(table.Name, table);

    public ResultTable GetTable(string table) => _tables[table];

    public bool HasTable(string table) => _tables.ContainsKey(table);
}

/// <summary>
/// Table.
/// </summary>
class ResultTable
{
    private Dictionary<string, Dictionary<string, List<ResultRow>>> _stringColumnIndexes = new();
    private Dictionary<string, Dictionary<long, List<ResultRow>>> _numberColumnIndexes = new();
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
                rows.Add(row);
            }
        }
    }

    /// <summary>
    /// Adds index by column.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    public void AddIndex(string columnName)
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
    }
}

class ResultRow
{
    private Dictionary<string, string> _stringValues = new();
    private Dictionary<string, long> _numberValues = new();
    private Dictionary<string, decimal> _floatValues = new();
    private Dictionary<string, bool> _boolValues = new();
    private HashSet<string> _nullValues = new();

    public ResultTable Table { get; private set; }

    /// <summary>
    /// Retrieves rows from another table by id.
    /// </summary>
    /// <param name="tableName">Table name.</param>
    /// <param name="id">Id</param>
    /// <returns>Row.</returns>
    public IEnumerable<ResultRow> GetReferenced(string referenceTableName, string referenceColumnName, string columnName)
    {
        if (IsNull(columnName))
            return Enumerable.Empty<ResultRow>();
        var referenceTable = Table.Set.GetTable(referenceTableName);
        var column = Table.Columns.GetByName(columnName);
        if (ResultColumn.IsIntegerNumeric(column.ValueType))
            return referenceTable.GetByColumn(referenceColumnName, GetValue<long>(columnName));
        else if (column.ValueType == typeof(string))
            return referenceTable.GetByColumn(referenceColumnName, GetValue<string>(columnName));
        else
            throw new NotSupportedException();
    }

    public static ResultRow Read(ResultTable table, IDataReader dataReader)
    {
        ResultRow toReturn = new();
        for (int i = 0; i < table.Columns.OrderedColumns.Count; i++)
        {
            var column = table.Columns.OrderedColumns[i];
            var ordinal = dataReader.GetOrdinal(column.Name);
            if (dataReader.IsDBNull(ordinal))
            {
                toReturn._nullValues.Add(column.Name);
                continue;
            }
            var fieldType = dataReader.GetFieldType(ordinal);
            if (ResultColumn.IsIntegerNumeric(fieldType))
            {
                if (fieldType == typeof(long))
                    toReturn._numberValues.Add(column.Name, dataReader.GetInt64(ordinal));
                else if (fieldType == typeof(int))
                    toReturn._numberValues.Add(column.Name, dataReader.GetInt32(ordinal));
                else if (fieldType == typeof(short))
                    toReturn._numberValues.Add(column.Name, dataReader.GetInt16(ordinal));
                else if (fieldType == typeof(byte))
                    toReturn._numberValues.Add(column.Name, dataReader.GetByte(ordinal));
                else
                    throw new NotSupportedException();
            }
            else if (ResultColumn.IsFloatNumeric(fieldType))
            {
                if (fieldType == typeof(float))
                    toReturn._floatValues.Add(column.Name, (decimal)dataReader.GetFloat(ordinal));
                else if (fieldType == typeof(double))
                    toReturn._floatValues.Add(column.Name, (decimal)dataReader.GetDouble(ordinal));
                else if (fieldType == typeof(decimal))
                    toReturn._floatValues.Add(column.Name, dataReader.GetDecimal(ordinal));
                else
                    throw new NotSupportedException();
            }
            else if (fieldType == typeof(string))
                toReturn._stringValues.Add(column.Name, dataReader.GetString(ordinal));
            else if (fieldType == typeof(bool))
                toReturn._boolValues.Add(column.Name, dataReader.GetBoolean(ordinal));
        }
        return toReturn;
    }

    public bool IsNull(string columnName) => _nullValues.Contains(columnName);

    public T GetValue<T>(string columnName)
    {
        if (IsNull(columnName))
        {
            if (typeof(T) == typeof(string))
                return default;
            if (Nullable.GetUnderlyingType(typeof(T)) != null)
                return default;
            throw new InvalidOperationException();
        }

        var column = Table.Columns.GetByName(columnName);
        if (typeof(T) == typeof(int) || typeof(T) == typeof(long))
        {
            if (!ResultColumn.IsIntegerNumeric(column.ValueType))
                throw new InvalidOperationException();
            return (T)(object)_numberValues[columnName];
        }
        if (typeof(T) == typeof(string))
        {
            if (column.ValueType != typeof(string))
                throw new InvalidOperationException();
            return (T)(object)_stringValues[columnName];
        }
        if (typeof(T) == typeof(bool))
        {
            if (column.ValueType != typeof(bool))
                throw new InvalidOperationException();
            return (T)(object)_boolValues[columnName];
        }
        throw new InvalidOperationException();
    }
}


