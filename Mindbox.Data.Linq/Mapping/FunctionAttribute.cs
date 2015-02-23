namespace System.Data.Linq.Mapping
{
	/// <summary>
	/// Attribute placed on a method mapped to a User Defined Function.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class FunctionAttribute : Attribute 
	{
		public string Name { get; set; }
		public bool IsComposable { get; set; }
	}
}