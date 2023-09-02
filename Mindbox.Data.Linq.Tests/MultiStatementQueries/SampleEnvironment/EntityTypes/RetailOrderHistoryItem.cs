using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment.EntityTypes;

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