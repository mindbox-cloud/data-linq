using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	[Table]
	[InheritanceMapping(Code = "1", IsDefault = true, Type = typeof(RootEntityWithInheritanceMapping))]
	[InheritanceMapping(Code = "2", Type = typeof(DescendantEntityWithInheritanceMapping))]
	public class RootEntityWithInheritanceMapping
	{
		[Column(IsPrimaryKey = true)]
		public int Id { get; set; }

		[Column(IsDiscriminator = true)]
		public string Discriminator { get; set; }

		private EntityRef<TestEntity20> creator1;

		[Association(
			Name = "Staff_Creator",
			Storage = "creator1",
			ThisKey = "CreatorId1",
			OtherKey = "Id",
			IsForeignKey = true)]
		public TestEntity20 Creator1 { get; set; }
	}
}
