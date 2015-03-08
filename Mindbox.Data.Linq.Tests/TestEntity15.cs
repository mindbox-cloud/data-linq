using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	[Table]
	public class TestEntity15
	{
		[Column(IsPrimaryKey = true)]
		public int Id { get; private set; }

		[Column]
		public int Value { get; set; }
	}
}
