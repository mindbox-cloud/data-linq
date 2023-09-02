namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

class PropertyAccessChainPart : IRowSourceChainPart
{
    public ChainSle Chain { get; set; }
    public string PropertyName { get; set; }
}
