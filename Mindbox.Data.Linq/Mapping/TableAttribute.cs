namespace System.Data.Linq.Mapping
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class TableAttribute : Attribute 
	{
		public string Name { get; set; }
	}
}