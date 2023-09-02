namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

class AssociationChainPart : IRowSourceChainPart
{
    public string ColumnName { get; set; }
    public string NextTableName { get; set; }
    public string NextTableColumnName { get; set; }
    public ChainSle Chain { get; set; }
}
