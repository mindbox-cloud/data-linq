using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment.EntityTypes;

[Table(Name = "directcrm.CustomerCustomFieldValues")]
public sealed class CustomerCustomFieldValue
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public long CustomerId { get; set; }

    [Column]
    public string FieldName { get; set; }

    [Column]
    public string FieldValue { get; set; }
}