using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	public class TestEntity6
	{
		private readonly EntitySet<TestEntity7> personalPermissions;
		private TestEntity6 creator;


		public TestEntity6()
		{
			personalPermissions = new EntitySet<TestEntity7>(AttachPersonalPermission, DetachPersonalPermission);
		}


		public virtual int Id { get; set; }
		public virtual string UserName { get; set; }
		public virtual string PasswordHash { get; set; }
		public virtual string Email { get; set; }
		public virtual string StaffTypeSystemName { get; set; }
		public virtual byte[] RowVersion { get; set; }
		public virtual DateTime? AccountExpirationDateTimeUtc { get; set; }
		public virtual bool IsBlocked { get; set; }
		public virtual int CreatorId { get; set; }
		public virtual DateTime CreationDateTimeUtc { get; set; }
		public virtual string FirstName { get; set; }
		public virtual string LastName { get; set; }
		public virtual string Comment { get; set; }

		[Association(
			Name = "UserPermission_Staff",
			Storage = "personalPermissions",
			ThisKey = "Id",
			OtherKey = "StaffId")]
		public virtual EntitySet<TestEntity7> PersonalPermissions
		{
			get { return personalPermissions; }
			set { personalPermissions.Assign(value); }
		}

		public virtual TestEntity6 Creator
		{
			get { return creator; }
			set
			{
				creator = value;
				CreatorId = value == null ? default(int) : value.Id;
			}
		}


		private void AttachPersonalPermission(TestEntity7 permission)
		{
			permission.Staff = this;
		}

		private void DetachPersonalPermission(TestEntity7 permission)
		{
			permission.Staff = null;
		}


		public class TestEntity6Configuration : EntityTypeConfiguration<TestEntity6>
		{
			public TestEntity6Configuration()
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
				HasRequired(entity => entity.Creator).WithMany().HasForeignKey(entity => entity.CreatorId);
			}
		}
	}
}
