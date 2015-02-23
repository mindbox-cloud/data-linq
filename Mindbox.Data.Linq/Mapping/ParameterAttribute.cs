namespace System.Data.Linq.Mapping
{
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
	public sealed class ParameterAttribute : Attribute 
	{
		public string Name { get; set; }
		public string DbType { get; set; }
	}
}