using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	public class TestEntity17
	{
		public virtual int Id { get; set; }
		public virtual byte[] Value { get; set; }


		public class TestEntity17Configuration : EntityTypeConfiguration<TestEntity17>
		{
			public TestEntity17Configuration()
			{
				ToTable("Test17");
				HasKey(entity => entity.Id);
				Property(entity => entity.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
				Property(entity => entity.Value).IsFixedLength().HasMaxLength(5).IsRequired();
			}
		}
	}
}
