namespace System.Data.Linq.Mapping
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class DatabaseAttribute : Attribute 
	{
		public string Name { get; set; }
	}
}