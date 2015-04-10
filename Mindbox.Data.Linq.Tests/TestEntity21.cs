using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	public class TestEntity21
	{
		private TestEntity21 creator;


		public virtual int Id { get; set; }
		public virtual string UserName { get; set; }
		public virtual string PasswordHash { get; set; }
		public virtual string Email { get; set; }
		public virtual string StaffTypeSystemName { get; set; }
		public virtual byte[] RowVersion { get; set; }
		public virtual DateTime? AccountExpirationDateTimeUtc { get; set; }
		public virtual bool IsBlocked { get; set; }
		public virtual int? CreatorId { get; protected set; }
		public virtual DateTime CreationDateTimeUtc { get; set; }
		public virtual string FirstName { get; set; }
		public virtual string LastName { get; set; }
		public virtual string Comment { get; set; }

		public virtual TestEntity21 Creator
		{
			get { return creator; }
			set
			{
				creator = value;
				CreatorId = value == null ? (int?)null : value.Id;
			}
		}


		public class TestEntity21Configuration : EntityTypeConfiguration<TestEntity21>
		{
			public TestEntity21Configuration()
			{
				ToTable("Staff", "administration");
				HasKey(entity => entity.Id);
				Property(entity => entity.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
				Property(entity => entity.IsBlocked);
				Property(entity => entity.UserName).HasMaxLength(100).IsRequired();
				Property(entity => entity.PasswordHash).HasMaxLength(40).IsFixedLength().IsUnicode(false).IsOptional();
				Property(entity => entity.Email).HasMaxLength(254);
				Property(entity => entity.StaffTypeSystemName).HasMaxLength(250).IsRequired();
				Property(entity => entity.FirstName).HasMaxLength(255);
				Property(entity => entity.LastName).HasMaxLength(255);
				Property(entity => entity.Comment).IsMaxLength().IsRequired();
				Property(entity => entity.CreationDateTimeUtc);
				Property(entity => entity.AccountExpirationDateTimeUtc);
				Property(entity => entity.RowVersion).IsRowVersion();
				HasOptional(entity => entity.Creator).WithMany().HasForeignKey(entity => entity.CreatorId);
			}
		}
	}
}
