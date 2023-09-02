namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

class ColumnAccessChainPart : IChainPart
{
    public ChainSle Chain { get; set; }
    public string ColumnName { get; set; }
}
