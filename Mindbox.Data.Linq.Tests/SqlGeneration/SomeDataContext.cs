using System;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.SqlGeneration
{
	public class SomeDataContext : DataContext
	{
		public SomeDataContext(IDbConnection connection) : base(connection)
		{
			// empty
		}

		[Function(Name = "NEWID", IsComposable = true)]
		public Guid Random()
		{
			return Guid.NewGuid();
		}
	}
}