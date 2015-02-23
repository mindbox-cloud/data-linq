namespace System.Data.Linq.Mapping
{
	/// <summary>
	/// This attribute is applied to functions returning multiple result types,
	/// to declare the possible result types returned from the function.  For
	/// inheritance types, only the root type of the inheritance hierarchy need
	/// be specified.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public sealed class ResultTypeAttribute : Attribute 
	{
		public ResultTypeAttribute(Type type) 
		{
			Type = type;
		}


		public Type Type { get; private set; }
	}
}