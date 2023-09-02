using System;
using System.Collections.Generic;
using System.Data;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;
class SqlQueryTranslatorResult
{
    /// <summary>
    /// Command text.
    /// </summary>
    public string CommandText { get; private set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="commandText">Command text</param>
    public SqlQueryTranslatorResult(string commandText)
    {
        CommandText = commandText;
    }

    /// <summary>
    /// Executes command and returns result set.
    /// </summary>
    /// <param name="connection">Connection.</param>
    /// <returns>ResultSet</returns>
    public ResultSet Execute(IDbConnection connection)
    {
        throw new NotImplementedException();
    }
}

class ResultSet
{
    private Dictionary<string, ResultSetTable> _tables = new();


    public ResultSetTable GetTable(string tableName) => _tables[tableName];
}

class ResultSetTable
{
    /// <summary>
    /// Result set.
    /// </summary>
    public ResultSet Set { get; private set; }
    /// <summary>
    /// Name.
    /// </summary>
    public string Name { get; private set; }

    public ResultSetTable()
    {

    }
}

class ResultSetTableRow
{
    public ResultSetTable Table { get; private set; }

    /// <summary>
    /// Retrieves rows from another table by id.
    /// </summary>
    /// <param name="tableName">Table name.</param>
    /// <param name="id">Id</param>
    /// <returns>Row.</returns>
    public IEnumerable<ResultSetTableRow> GetReferencedByIntId(string tableName, int id)
    {

    }

    /// <summary>
    /// Retrieves rows from another table by id.
    /// </summary>
    /// <param name="tableName">Table name.</param>
    /// <param name="id">Id</param>
    /// <returns>Row.</returns>
    public IEnumerable<ResultSetTableRow> GetReferencedByLongId(string tableName, long id)
    {

    }
}


