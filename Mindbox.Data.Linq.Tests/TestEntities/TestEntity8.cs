using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	public sealed class TestEntity8 : INotifyPropertyChanging, INotifyPropertyChanged
	{
		private static readonly PropertyChangingEventArgs EmptyChangingEventArgs =
			new PropertyChangingEventArgs(string.Empty);

	
		private int id;
		private byte[] value;


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

		public byte[] Value
		{
			get
			{
				return value;
			}
			set
			{
				if (value != this.value)
				{
					SendPropertyChanging();
					this.value = value;
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


		public class TestEntity8Configuration : EntityTypeConfiguration<TestEntity8>
		{
			public TestEntity8Configuration()
			{
				ToTable("Test8");
				HasKey(entity => entity.Id);
				Property(entity => entity.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
				Property(entity => entity.Value).IsFixedLength().HasMaxLength(5).IsRequired();
			}
		}
	}
}
