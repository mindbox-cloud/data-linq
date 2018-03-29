using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests
{
	[Table(Name = "Test25")]
	public class TestEntity25
	{
		private EntityRef<RootEntityWithInheritanceMapping> other1;
		private EntityRef<DescendantEntityWithInheritanceMapping> other2;

		[Column]
		public int Other1Id { get; set; }

		[Column]
		public int Other2Id { get; set; }

		[Column]
		public IEnumerable<TestEntity26> Values { get; set; }

		[Association(Name = "TestEntity25_Other1", Storage = "other1", IsForeignKey = true, ThisKey = "Other1Id", OtherKey = "Id")]
		public virtual RootEntityWithInheritanceMapping Other1
		{
			get
			{
				return other1.Entity;
			}
			set
			{
				throw new NotImplementedException();
			}
		}


		[Association(Name = "TestEntity25_Other2", Storage = "other2", IsForeignKey = true, ThisKey = "Other2Id", OtherKey = "Id")]
		public virtual DescendantEntityWithInheritanceMapping Other2
		{
			get
			{
				return other2.Entity;
			}
			set
			{
				throw new NotImplementedException();
			}
		}
	}
}
