using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment.EntityTypes;

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
