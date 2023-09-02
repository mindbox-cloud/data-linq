namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

class TableChainPart : IRowSourceChainPart
{
    public string Name { get; private set; }

    public ChainSle Chain { get; set; }

    public TableChainPart(string name)
    {
        Name = name;
    }
}
