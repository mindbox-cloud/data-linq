using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	public class TestEntity16
	{
		private TestEntity10 other;


		public virtual int Id { get; set; }
		public virtual int OtherId { get; set; }

		public virtual TestEntity10 Other
		{
			get { return other; }
			set
			{
				other = value;
				OtherId = value == null ? default(int) : value.Id;
			}
		}


		public class TestEntity16Configuration : EntityTypeConfiguration<TestEntity16>
		{
			public TestEntity16Configuration()
			{
				ToTable("Test16");
				HasKey(entity => entity.Id);
				Property(entity => entity.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
				HasRequired(entity => entity.Other).WithMany().HasForeignKey(entity => entity.OtherId);
			}
		}
	}
}
