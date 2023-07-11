﻿using System;
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
	[Table(Name = "administration.Staff")]
	public sealed class TestEntity4 : INotifyPropertyChanging, INotifyPropertyChanged
	{
		public const string IdMigrationIdentifier = "TestEntity4_Id";

		private static readonly PropertyChangingEventArgs EmptyChangingEventArgs =
			new PropertyChangingEventArgs(string.Empty);


		private long id;
		private string userName;
		private string passwordHash;
		private string email;
		private string staffTypeSystemName;
		private bool isBlocked;

		private long creatorId;
		private DateTime creationDateTimeUtc;
		private string firstName;
		private string lastName;
		private string comment;

		private Binary rowVersion;
		private DateTime? accountExpirationDateTimeUtc;

		private readonly EntitySet<TestEntity5> personalPermissions;

		private EntityRef<TestEntity4> creator;


		public TestEntity4()
		{
			personalPermissions = EntitySet<TestEntity5>.Create(AttachPersonalPermission, DetachPersonalPermission);
		}


		[Column(
			Storage = "id",
			AutoSync = AutoSync.OnInsert,
			IsPrimaryKey = true,
			IsDbGenerated = true,
			MigrationIdentifier = IdMigrationIdentifier,
			DbType = "int identity not null",
			DbTypeAfterDatabaseMigration = "bigint identity not null")]
		public long Id
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

		[Column(Storage = "userName", CanBeNull = false, DbType = "nvarchar(100) not null")]
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

		[Column(Name = "PasswordHash", Storage = "passwordHash", CanBeNull = true, DbType = "char(40) null")]
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

		[Column(Storage = "email", CanBeNull = true, DbType = "nvarchar(254) null")]
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

		[Column(Storage = "staffTypeSystemName", CanBeNull = false, DbType = "nvarchar(250) not null")]
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

		[Column(
			Storage = "rowVersion",
			AutoSync = AutoSync.Always,
			CanBeNull = false,
			IsDbGenerated = true,
			IsVersion = true,
			DbType = "rowversion not null")]
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

		[Column(Storage = "accountExpirationDateTimeUtc", DbType = "datetime null")]
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

		[Column(Storage = "isBlocked", DbType = "bit not null")]
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

		[Column(
			Storage = "creatorId",
			CanBeNull = false,
			DbType = "int not null",
			DbTypeAfterDatabaseMigration = "bigint not null")]
		public long CreatorId
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

		[Column(Storage = "creationDateTimeUtc", CanBeNull = false, DbType = "datetime not null")]
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

		[Column(Storage = "firstName", CanBeNull = true, DbType = "nvarchar(255) null")]
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

		[Column(Storage = "lastName", CanBeNull = true, DbType = "nvarchar(255) null")]
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

		[Column(Storage = "comment", CanBeNull = false, DbType = "nvarchar(max) not null")]
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
		public EntitySet<TestEntity5> PersonalPermissions
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
		public TestEntity4 Creator
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

		private void AttachPersonalPermission(TestEntity5 permission)
		{
			SendPropertyChanging();
			permission.Staff = this;
		}

		private void DetachPersonalPermission(TestEntity5 permission)
		{
			SendPropertyChanging();
			permission.Staff = null;
		}
	}
}
