using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	[Table(Name = "administration.StaffPermissions")]
	public sealed class TestEntity7 : INotifyPropertyChanging, INotifyPropertyChanged
	{

		private static readonly PropertyChangingEventArgs EmptyChangingEventArgs =
			new PropertyChangingEventArgs(string.Empty);


		private int staffId;
		private EntityRef<TestEntity6> staff;
		private string permissionSystemName;


		[Column(Storage = "staffId", IsPrimaryKey = true)]
		public int StaffId
		{
			get
			{
				return staffId;
			}
			set
			{
				if (staffId != value)
				{
					if (staff.HasLoadedOrAssignedValue)
						throw new ForeignKeyReferenceAlreadyHasValueException();

					SendPropertyChanging();
					staffId = value;
					SendPropertyChanged();
				}
			}
		}

		[Association(
			Name = "StaffPermission_Staff",
			Storage = "staff",
			IsForeignKey = true,
			ThisKey = "StaffId",
			OtherKey = "Id")]
		public TestEntity6 Staff
		{
			get { return staff.Entity; }
			set
			{
				var previousValue = staff.Entity;
				if ((previousValue != value) || !staff.HasLoadedOrAssignedValue)
				{
					SendPropertyChanging();

					if (previousValue != null)
					{
						staff.Entity = null;
						previousValue.PersonalPermissions.Remove(this);
					}

					staff.Entity = value;

					if (value != null)
					{
						value.PersonalPermissions.Add(this);
						staffId = value.Id;
					}
					else
					{
						staffId = default(int);
					}

					SendPropertyChanged();
				}
			}
		}

		[Column(Storage = "permissionSystemName", IsPrimaryKey = true, CanBeNull = false)]
		public string PermissionSystemName
		{
			get
			{
				return permissionSystemName;
			}
			set
			{
				if (permissionSystemName != value)
				{
					SendPropertyChanging();
					permissionSystemName = value;
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

	}
}
