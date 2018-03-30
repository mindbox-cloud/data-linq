using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	public class TestEntity24
	{
		public virtual int Id { get; set; }
		public virtual int? OtherId { get; set; }
		public virtual TestEntity23 Other { get; set; }


		public class TestEntity24Configuration : EntityTypeConfiguration<TestEntity24>
		{
			public TestEntity24Configuration()
			{
				ToTable("Test24");
				HasKey(entity => entity.Id);
				Property(entity => entity.Id);
				HasOptional(entity => entity.Other).WithMany().HasForeignKey(entity => entity.OtherId);
			}
		}
	}
}
