using System.Data.Common;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using System.Linq;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment;

class DataContextWithFunctions : DataContext
{
    public DataContextWithFunctions(DbConnection connection) : base(connection)
    {
    }

    [Function(Name = "dbo.ValueAsQueryableDecimal", IsComposable = true)]
    public IQueryable<ValueAsQueryableDecimalResultDto> ValueAsQueryableDecimal(
        [Parameter(Name = "value", DbType = "decimal")] decimal? value)
    {
        return new[] { new ValueAsQueryableDecimalResultDto { Value = value } }
            .AsQueryable();
    }
}


