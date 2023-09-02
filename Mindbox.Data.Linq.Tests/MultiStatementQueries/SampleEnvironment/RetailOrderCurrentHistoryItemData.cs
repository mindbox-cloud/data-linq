using Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment.EntityTypes;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment;

public class RetailOrderCurrentHistoryItemData
{
    public RetailOrder RetailOrder { get; set; }

    public RetailOrderHistoryItem RetailOrderHistoryItem { get; set; }
}

