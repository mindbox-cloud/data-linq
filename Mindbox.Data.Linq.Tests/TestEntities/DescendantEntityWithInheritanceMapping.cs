using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	public class DescendantEntityWithInheritanceMapping : RootEntityWithInheritanceMapping
	{
		[Column]
		public int X { get; set; }

		[Column]
		public int? CreatorId2 { get; set; }

		[Association(
			Name = "Staff_Creator2",
			Storage = "creator2",
			ThisKey = "CreatorId2",
			OtherKey = "Id",
			IsForeignKey = true)]
		public TestEntity20 Creator2 { get; set; }
	}
}
