using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment.EntityTypes;


[Table(Name = "directcrm.CustomerActionCustomFieldValues")]
public sealed class CustomerActionCustomFieldValue
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public long CustomerActionId { get; set; }

    [Association(ThisKey = nameof(CustomerActionId), OtherKey = nameof(EntityTypes.CustomerAction.Id))]
    public CustomerAction CustomerAction { get; set; }

    [Column]
    public string FieldName { get; set; }

    [Column]
    public string FieldValue { get; set; }
}