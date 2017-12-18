namespace System.Data.Linq.Mapping
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
	public sealed class ColumnAttribute : DataAttribute 
	{
		private bool canBeNull = true;


		public ColumnAttribute()
		{
			AutoSync = AutoSync.Default;
			UpdateCheck = UpdateCheck.Always;
		}


		public string DbType { get; set; }
		public string DbTypeAfterDatabaseMigration { get; set; }
		public string Expression { get; set; }
		public bool IsPrimaryKey { get; set; }
		public bool IsDbGenerated { get; set; }
		public bool IsVersion { get; set; }
		public UpdateCheck UpdateCheck { get; set; }
		public AutoSync AutoSync { get; set; }
		public bool IsDiscriminator { get; set; }

		public bool CanBeNull 
		{ 
			get { return canBeNull; }
			set 
			{
				CanBeNullSet = true;
				canBeNull = value;
			}
		}


		internal bool CanBeNullSet { get; private set; }
	}
}