using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	[Table(Name = "Test19")]
	public class TestEntity19
	{
		protected readonly EntitySet<TestEntity18> others;


		public TestEntity19()
		{
			others = new EntitySet<TestEntity18>(AttachOther, DetachOther);
		}


		[Column(AutoSync = AutoSync.OnInsert, IsDbGenerated = true, IsPrimaryKey = true)]
		public virtual int Id { get; set; }

		[Association(Name = "TestEntity18_Other", Storage = "others", ThisKey = "Id", OtherKey = "OtherId")]
		public virtual EntitySet<TestEntity18> Others
		{
			get { return others; }
			set { others.Assign(value); }
		}


		private void AttachOther(TestEntity18 other)
		{
			if (other == null)
				throw new ArgumentNullException("other");

			other.Other = this;
		}

		private void DetachOther(TestEntity18 other)
		{
			if (other == null)
				throw new ArgumentNullException("other");

			other.Other = null;
		}
	}
}
