using System;
using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment.EntityTypes;

[Table(Name = "directcrm.Customers")]
public sealed class Customer
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public string PasswordHash { get; set; }

    [Column]
    public Guid? PasswordHashSalt { get; set; }

    [Column]
    public string TempPasswordHash { get; set; }

    [Column]
    public Guid? TempPasswordHashSalt { get; set; }

    [Column]
    public bool IsDeleted { get; set; }

    [Column]
    public string TempPasswordEmail { get; set; }

    [Column]
    public long? TempPasswordMobilePhone { get; set; }

    [Association(ThisKey = nameof(Id), OtherKey = nameof(CustomerCustomFieldValue.CustomerId))]
    public CustomerCustomFieldValue[] CustomFieldValues { get; set; }

    [Column]
    public int AreaId { get; set; }

    [Association(ThisKey = nameof(AreaId), OtherKey = nameof(EntityTypes.Area.Id))]
    public Area Area { get; set; }
}

