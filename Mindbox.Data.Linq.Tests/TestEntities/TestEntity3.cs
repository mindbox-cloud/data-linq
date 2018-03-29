using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	public class TestEntity3 : RootEntityWithInheritanceMapping
	{
		[Column]
		public int Y { get; set; }
	}
}
