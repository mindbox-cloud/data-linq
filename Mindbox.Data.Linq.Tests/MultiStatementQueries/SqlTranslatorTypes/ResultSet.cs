using System.Collections.Generic;
using System.Data;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;

class ResultSet
{
    private Dictionary<string, ResultTable> _tables = new();

    public ResultTable GetTable(string tableName) => _tables[tableName];

    public void Fill(SqlQueryTranslatorResult translatorResult, IDbColumnTypeProvider typeProvider, IDbConnection connection)
    {
        using var command = connection.CreateCommand();

        command.CommandText = translatorResult.CommandText;

        using var reader = command.ExecuteReader();

        int tableIndex = 0;
        do
        {
            if (!_tables.ContainsKey(translatorResult.TableReadOrder[tableIndex]))
            {
                var columns = CreateColumns(reader);
                var table = new ResultTable(this, translatorResult.TableReadOrder[tableIndex], columns);
                _tables.Add(table.Name, table);
            }
            _tables[translatorResult.TableReadOrder[tableIndex]].ReadRows(reader);
            tableIndex++;
        } while (reader.NextResult());
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
