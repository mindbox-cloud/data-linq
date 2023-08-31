using System;
using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries;

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

    [Association(ThisKey = nameof(AreaId), OtherKey = nameof(MultiStatementQueries.Area.Id))]
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

    [Column]
    public int ActionTemplateId { get; set; }

    [Association(ThisKey = nameof(ActionTemplateId), OtherKey = nameof(MultiStatementQueries.ActionTemplate.Id))]
    public ActionTemplate ActionTemplate { get; set; }

    [Column]
    public int CustomerId { get; set; }

    [Association(ThisKey = nameof(CustomerId), OtherKey = nameof(MultiStatementQueries.Customer.Id))]
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

[Table(Name = "directcrm.ActionTemplates")]
public sealed class ActionTemplate
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; }
}

[Table(Name = "directcrm.CustomerActionCustomFieldValues")]
public sealed class CustomerActionCustomFieldValue
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public long CustomerActionId { get; set; }

    [Association(ThisKey = nameof(CustomerActionId), OtherKey = nameof(MultiStatementQueries.CustomerAction.Id))]
    public CustomerAction CustomerAction { get; set; }

    [Column]
    public string FieldName { get; set; }

    [Column]
    public string FieldValue { get; set; }
}

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

[Table(Name = "directcrm.Areas")]
public sealed class Area
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; }

    [Column]
    public int SubAreaId { get; set; }

    [Association(ThisKey = nameof(SubAreaId), OtherKey = nameof(MultiStatementQueries.SubArea.Id))]
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

[Table(Name = "directcrm.RetailOrders")]
public sealed class RetailOrder
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public int CustomerId { get; set; }

    [Column]
    public decimal TotalSum { get; set; }

    [Association(ThisKey = nameof(Id), OtherKey = nameof(RetailOrderHistoryItem.RetailOrderId))]
    public RetailOrderHistoryItem[] History { get; set; }

    [Association(ThisKey = nameof(CustomerId), OtherKey = nameof(Customer.Id))]
    public Customer CurrentCustomer { get; set; }
}


[Table(Name = "directcrm.RetailOrderHistoryItems")]
public sealed class RetailOrderHistoryItem
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public int RetailOrderId { get; set; }

    [Association(ThisKey = nameof(Id), OtherKey = nameof(RetailOrderPurchase.RetailOrderHistoryItemId))]
    public RetailOrderPurchase[] Purchases { get; set; }

    [Column]
    public decimal Amount { get; set; }

    [Column]
    public bool? IsCurrentOtherwiseNull { get; set; }
}

[Table(Name = "directcrm.RetailOrderPurchases")]
public sealed class RetailOrderPurchase
{
    [Column(IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column]
    public int RetailOrderHistoryItemId { get; set; }

    [Column]
    public decimal? PriceForCustomerOfLine { get; set; }

    [Column]
    public decimal? Count { get; set; }
}