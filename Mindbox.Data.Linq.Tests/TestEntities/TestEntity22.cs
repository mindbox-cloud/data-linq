using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	public class TestEntity22
	{
		private TestEntity23 other;


		public virtual int Id { get; set; }
		public virtual int? OtherId { get; set; }

		public virtual TestEntity23 Other
		{
			get { return other; }
			set
			{
				other = value;
				OtherId = value == null ? (int?)null : value.Id;
			}
		}


		public class TestEntity22Configuration : EntityTypeConfiguration<TestEntity22>
		{
			public TestEntity22Configuration()
			{
				ToTable("Test22");
				HasKey(entity => entity.Id);
				Property(entity => entity.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
				HasOptional(entity => entity.Other).WithMany().HasForeignKey(entity => entity.OtherId);
			}
		}
	}
}
