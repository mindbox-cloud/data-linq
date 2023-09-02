using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment.EntityTypes;

[Table(Name = "directcrm.Areas")]
public sealed class Area
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; }

    [Column]
    public int SubAreaId { get; set; }

    [Association(ThisKey = nameof(SubAreaId), OtherKey = nameof(EntityTypes.SubArea.Id))]
    public SubArea SubArea { get; set; }
}