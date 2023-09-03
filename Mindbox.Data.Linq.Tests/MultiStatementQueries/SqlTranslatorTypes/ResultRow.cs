using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;


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
