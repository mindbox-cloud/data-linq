using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	[Table]
	[InheritanceMapping(Code = "1", IsDefault = true, Type = typeof(TestEntity1))]
	[InheritanceMapping(Code = "2", Type = typeof(TestEntity2))]
	public class TestEntity1
	{
		[Column(IsPrimaryKey = true)]
		public int Id { get; set; }

		[Column(IsDiscriminator = true)]
		public string Discriminator { get; set; }
	}
}
