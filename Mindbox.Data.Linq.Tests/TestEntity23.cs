using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	public class TestEntity23
	{
		public virtual int Id { get; set; }
		public virtual int Value { get; set; }


		public class TestEntity23Configuration : EntityTypeConfiguration<TestEntity23>
		{
			public TestEntity23Configuration()
			{
				ToTable("Test23");
				HasKey(entity => entity.Id);
				Property(entity => entity.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
				Property(entity => entity.Value).IsRequired();
			}
		}
	}
}
