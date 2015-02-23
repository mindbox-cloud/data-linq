namespace System.Data.Linq.Mapping
{
	public abstract class DataAttribute : Attribute 
	{
		public string Name { get; set; }
		public string Storage { get; set; }
	}
}