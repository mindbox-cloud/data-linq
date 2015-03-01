using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	[Table]
	public class TestEntity13 : IValueStorage
	{
		[Column(IsPrimaryKey = true)]
		public int Id { get; set; }

		[Column(CanBeNull = false)]
		public string Value { get; set; }
	}
}
