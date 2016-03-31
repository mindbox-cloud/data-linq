using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests
{
	[Table(Name = "Test26")]
	public class TestEntity26
	{
		private EntityRef<TestEntity1> other1;
		private EntityRef<TestEntity2> other2;

		[Column]
		public int Other1Id { get; set; }

		[Column]
		public int Other2Id { get; set; }

		[Association(Name = "TestEntity26_Other1", Storage = "other1", IsForeignKey = true, ThisKey = "Other1Id", OtherKey = "Id")]
		public virtual TestEntity1 Other1
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


		[Association(Name = "TestEntity26_Other2", Storage = "other2", IsForeignKey = true, ThisKey = "Other2Id", OtherKey = "Id")]
		public virtual TestEntity2 Other2
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
