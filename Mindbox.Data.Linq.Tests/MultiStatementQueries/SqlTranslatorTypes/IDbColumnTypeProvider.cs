namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;

public interface IDbColumnTypeProvider
{
    public string[] GetPKFields(string tableName);

    public string GetSqlType(string tableName, string columnName);
}
