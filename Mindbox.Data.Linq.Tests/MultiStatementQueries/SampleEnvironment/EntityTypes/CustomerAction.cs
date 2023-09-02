using System;
using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment.EntityTypes;

[Table(Name = "directcrm.CustomerActions")]
public sealed class CustomerAction
{
    [Column(IsPrimaryKey = true)]
    public long Id { get; set; }

    [Column]
    public DateTime DateTimeUtc { get; set; }

    [Column]
    public DateTime CreationDateTimeUtc { get; set; }

    [Column]
    public int PointOfContactId { get; set; }

    [Column]
    public int ActionTemplateId { get; set; }

    [Association(ThisKey = nameof(ActionTemplateId), OtherKey = nameof(EntityTypes.ActionTemplate.Id))]
    public ActionTemplate ActionTemplate { get; set; }

    [Column]
    public int CustomerId { get; set; }

    [Association(ThisKey = nameof(CustomerId), OtherKey = nameof(EntityTypes.Customer.Id))]
    public Customer Customer { get; set; }

    [Column]
    public int? StaffId { get; set; }

    [Column]
    public int OriginalCustomerId { get; set; }

    [Column]
    public long? TransactionalId { get; set; }

    [Association(ThisKey = nameof(Id), OtherKey = nameof(CustomerActionCustomFieldValue.CustomerActionId))]
    public CustomerActionCustomFieldValue[] CustomFieldValues { get; set; }

}
