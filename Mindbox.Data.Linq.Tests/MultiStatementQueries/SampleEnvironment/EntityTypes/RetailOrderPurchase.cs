using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment.EntityTypes;

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