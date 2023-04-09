using System;
using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.MultiStatementQuery;

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
    public CustomerCustomFieldValue[] CustomerFieldValues { get; set; }

    [Column]
    public int AreaId { get; set; }

    [Association(ThisKey = nameof(AreaId), OtherKey = nameof(MultiStatementQuery.Area.Id))]
    public Area Area { get; set; }
}

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

    // private EntityRef<PointOfContact> _pointOfContact;
    [Column]
    public int ActionTemplateId { get; set; }

    // private EntityRef<ActionTemplate> _actionTemplate;
    [Column]
    public int CustomerId { get; set; }

    [Association(ThisKey = nameof(CustomerId), OtherKey = nameof(MultiStatementQuery.Customer.Id))]
    public Customer Customer { get; set; }

    [Column]
    public int? StaffId { get; set; }

    // private EntityRef<Staff> Staff;
    [Column]
    public int OriginalCustomerId { get; set; }

    [Column]
    public long? TransactionalId { get; set; }
}

[Table(Name = "directcrm.CustomFieldValues")]
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

[Table(Name = "directcrm.Areas")]
public sealed class Area
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; }

    [Column]
    public int SubAreaId { get; set; }

    [Association(ThisKey = nameof(SubAreaId), OtherKey = nameof(MultiStatementQuery.SubArea.Id))]
    public SubArea SubArea { get; set; }
}

[Table(Name = "directcrm.SubAreas")]
public sealed class SubArea
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; }
}
