using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment.EntityTypes;

[Table(Name = "directcrm.SubAreas")]
public sealed class SubArea
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; }
}