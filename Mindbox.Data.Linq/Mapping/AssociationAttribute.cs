namespace System.Data.Linq.Mapping
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
	public sealed class AssociationAttribute : DataAttribute 
	{
		public string ThisKey { get; set; }
		public string OtherKey { get; set; }
		public bool IsUnique { get; set; }
		public bool IsForeignKey { get; set; }
		public string DeleteRule { get; set; }
		public bool DeleteOnNull { get; set; }
	}
}