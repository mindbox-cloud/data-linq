using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	[Table(Name = "Test18")]
	public class TestEntity18
	{
		private EntityRef<TestEntity19> other;


		[Column(AutoSync = AutoSync.OnInsert, IsDbGenerated = true, IsPrimaryKey = true)]
		public virtual int Id { get; set; }

		[Column]
		public virtual int OtherId { get; set; }

		[Association(Name = "TestEntity18_Other", Storage = "other", IsForeignKey = true, ThisKey = "OtherId", OtherKey = "Id")]
		public virtual TestEntity19 Other
		{
			get { return other.Entity; }
			set
			{
				var oldValue = other.Entity;
				if (oldValue != value)
				{
					if (oldValue != null)
					{
						other.Entity = null;
						oldValue.Others.Remove(this);
					}

					if (value == null)
					{
						OtherId = default(int);
					}
					else
					{
						other.Entity = value;
						value.Others.Add(this);
						OtherId = value.Id;
					}
				}
			}
		}
	}
}
