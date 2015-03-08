using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	[Table]
	public class TestEntity14
	{
		[Column(IsPrimaryKey = true)]
		public int Id { get; protected set; }

		[Column]
		public int Value { get; set; }
	}
}
