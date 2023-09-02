using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;

[DebuggerDisplay("To {OtherTable.Name}")]
class Connection
{
    private List<string> _tableFields;
    private List<string> _otherTableFields;
    private List<(string Field, string OtherField)> _sortedMappedFields = new();

    public TableNode Table { get; private set; }
    public IEnumerable<string> TableFields => _tableFields;
    public TableNode OtherTable { get; private set; }
    public IEnumerable<string> OtherTableFields => _otherTableFields;
    public IEnumerable<(string Field, string OtherField)> MappedFields => _sortedMappedFields;

    public Connection(TableNode table, IEnumerable<string> tableFields, TableNode otherTable, IEnumerable<string> otherTableFields)
    {
        Table = table;
        _tableFields = tableFields.ToList();
        OtherTable = otherTable;
        _otherTableFields = otherTableFields.ToList();
        if (TableFields.Count() != OtherTableFields.Count())
            throw new ArgumentException();

        UpdateMappedFields();
    }

    /// <summary>
    /// Add field to connection.
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="otherFiledName"></param>
    public void AddFields(string fieldName, string otherFiledName)
    {
        if (MappedFields.Any(m => m.Field == fieldName && m.OtherField == otherFiledName))
            return;
        _tableFields.Add(fieldName);
        _otherTableFields.Add(otherFiledName);
        UpdateMappedFields();
    }

    private void UpdateMappedFields()
    {
        _sortedMappedFields.Clear();
        for (int i = 0; i < _otherTableFields.Count; i++)
            _sortedMappedFields.Add((_tableFields[i], _otherTableFields[i]));
        _sortedMappedFields.Sort((a, b) => a.Field.CompareTo(b.Field));
    }

    /// <summary>
    /// Checks for equality.
    /// </summary>
    /// <param name="connection">Connection.</param>
    /// <returns>True - equals, false - not.</returns>
    public bool Equals(Connection connection)
    {
        return Table.Name == connection.Table.Name && OtherTable.Name == connection.OtherTable.Name &&
            MappedFields.SequenceEqual(connection.MappedFields);
    }

    /// <summary>
    /// Shows that it is same connection exactly or reversed connection.
    /// </summary>
    /// <param name="connection">Connection to check.</param>
    /// <returns>True - same or same-reversed, false - not same.</returns>
    public bool IsSame(Connection connection)
    {
        if (Table.Name == connection.OtherTable.Name && OtherTable.Name == connection.Table.Name)
            return MappedFields.SequenceEqual(connection.MappedFields.Select(m => (m.OtherField, m.Field)));
        return Equals(connection);
    }
}
