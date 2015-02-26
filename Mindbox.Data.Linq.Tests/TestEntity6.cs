using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.ModelConfiguration;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	public sealed class TestEntity6 : INotifyPropertyChanging, INotifyPropertyChanged
	{
		private static readonly PropertyChangingEventArgs EmptyChangingEventArgs =
			new PropertyChangingEventArgs(string.Empty);

	
		private int id;
		private string userName;
		private string passwordHash;
		private string email;
		private string staffTypeSystemName;
		private bool isBlocked;

		private int creatorId;
		private DateTime creationDateTimeUtc;
		private string firstName;
		private string lastName;
		private string comment;

		private Binary rowVersion;
		private DateTime? accountExpirationDateTimeUtc;

		private readonly EntitySet<TestEntity7> personalPermissions;

		private EntityRef<TestEntity6> creator;


		public TestEntity6()
		{
			personalPermissions = new EntitySet<TestEntity7>(AttachPersonalPermission, DetachPersonalPermission);
		}


		[Column(Storage = "id", AutoSync = AutoSync.OnInsert, IsPrimaryKey = true, IsDbGenerated = true)]
		public int Id
		{
			get
			{
				return id;
			}
			set
			{
				if (id != value)
				{
					SendPropertyChanging();
					id = value;
					SendPropertyChanged();
				}
			}
		}

		public string UserName
		{
			get
			{
				return userName;
			}
			set
			{
				if (userName != value)
				{
					SendPropertyChanging();
					userName = value;
					SendPropertyChanged();
				}
			}
		}

		public string PasswordHash
		{
			get
			{
				return passwordHash;
			}
			set
			{
				if (passwordHash != value)
				{
					SendPropertyChanging();
					passwordHash = value;
					SendPropertyChanged();
				}
			}
		}

		public string Email
		{
			get
			{
				return email;
			}
			set
			{
				if (email != value)
				{
					SendPropertyChanging();
					email = value;
					SendPropertyChanged();
				}
			}
		}

		public string StaffTypeSystemName
		{
			get
			{
				return staffTypeSystemName;
			}
			private set
			{
				if (staffTypeSystemName != value)
				{
					SendPropertyChanging();
					staffTypeSystemName = value;
					SendPropertyChanged();
				}
			}
		}

		[Column(Storage = "rowVersion", AutoSync = AutoSync.Always, CanBeNull = false, IsDbGenerated = true, IsVersion = true)]
		public Binary RowVersion
		{
			get
			{
				return rowVersion;
			}
			set
			{
				if (rowVersion != value)
				{
					SendPropertyChanging();
					rowVersion = value;
					SendPropertyChanged();
				}
			}
		}

		[Column(Storage = "accountExpirationDateTimeUtc")]
		public DateTime? AccountExpirationDateTimeUtc
		{
			get { return accountExpirationDateTimeUtc; }
			set
			{
				if (accountExpirationDateTimeUtc != value)
				{
					SendPropertyChanging();
					accountExpirationDateTimeUtc = value;
					SendPropertyChanged();
				}
			}
		}

		public bool IsBlocked
		{
			get { return isBlocked; }
			set
			{
				if (isBlocked != value)
				{
					SendPropertyChanging();
					isBlocked = value;
					SendPropertyChanged();
				}
			}
		}

		[Column(Storage = "creatorId", CanBeNull = false)]
		public int CreatorId
		{
			get
			{
				return creatorId;
			}
			set
			{
				if (creatorId != value)
				{
					SendPropertyChanging();
					creatorId = value;
					SendPropertyChanged();
				}
			}
		}

		[Column(Storage = "creationDateTimeUtc", CanBeNull = false)]
		public DateTime CreationDateTimeUtc
		{
			get
			{
				return creationDateTimeUtc;
			}
			set
			{
				if (creationDateTimeUtc != value)
				{
					SendPropertyChanging();
					creationDateTimeUtc = value;
					SendPropertyChanged();
				}
			}
		}

		public string FirstName
		{
			get
			{
				return firstName;
			}
			set
			{
				if (firstName != value)
				{
					SendPropertyChanging();
					firstName = value;
					SendPropertyChanged();
				}
			}
		}

		public string LastName
		{
			get
			{
				return lastName;
			}
			set
			{
				if (lastName != value)
				{
					SendPropertyChanging();
					lastName = value;
					SendPropertyChanged();
				}
			}
		}

		public string Comment
		{
			get
			{
				return comment;
			}
			set
			{
				if (comment != value)
				{
					SendPropertyChanging();
					comment = value;
					SendPropertyChanged();
				}
			}
		}

		[Association(
			Name = "UserPermission_Staff",
			Storage = "personalPermissions",
			ThisKey = "Id",
			OtherKey = "StaffId")]
		public EntitySet<TestEntity7> PersonalPermissions
		{
			get { return personalPermissions; }
			set { personalPermissions.Assign(value); }
		}

		[Association(
			Name = "Staff_Creator",
			Storage = "creator",
			ThisKey = "CreatorId",
			OtherKey = "Id",
			IsForeignKey = true)]
		public TestEntity6 Creator
		{
			get { return creator.Entity; }
			set
			{
				if ((creator.Entity != value) || !creator.HasLoadedOrAssignedValue)
				{
					SendPropertyChanging();
					creator.Entity = value;
					creatorId = value == null ? default(int) : value.Id;
					SendPropertyChanged();
				}
			}
		}

		public event PropertyChangingEventHandler PropertyChanging;
		public event PropertyChangedEventHandler PropertyChanged;


		private void SendPropertyChanging()
		{
			if (PropertyChanging != null)
				PropertyChanging(this, EmptyChangingEventArgs);
		}

		private void SendPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		private void AttachPersonalPermission(TestEntity7 permission)
		{
			SendPropertyChanging();
			permission.Staff = this;
		}

		private void DetachPersonalPermission(TestEntity7 permission)
		{
			SendPropertyChanging();
			permission.Staff = null;
		}


		public class TestEntity6Configuration : EntityTypeConfiguration<TestEntity6>
		{
			public TestEntity6Configuration()
			{
				ToTable("Staff", "administration");
				Property(entity => entity.IsBlocked);
				Property(entity => entity.UserName).HasMaxLength(100).IsRequired();
				Property(entity => entity.PasswordHash).HasMaxLength(40).IsFixedLength().IsUnicode(false).IsOptional();
				Property(entity => entity.Email).HasMaxLength(254);
				Property(entity => entity.StaffTypeSystemName).HasMaxLength(250).IsRequired();
				Property(entity => entity.FirstName).HasMaxLength(255);
				Property(entity => entity.LastName).HasMaxLength(255);
				Property(entity => entity.Comment).IsMaxLength().IsRequired();
			}
		}
	}
}
