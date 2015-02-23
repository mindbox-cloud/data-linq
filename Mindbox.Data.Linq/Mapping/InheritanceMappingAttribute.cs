namespace System.Data.Linq.Mapping
{
	/// <summary>
	/// Class attribute used to describe an inheritance hierarchy to be mapped.
	/// For example, 
	/// 
	///     [Table(Name = "People")]
	///     [InheritanceMapping(Code = "P", Type = typeof(Person), IsDefault=true)]
	///     [InheritanceMapping(Code = "C", Type = typeof(Customer))]
	///     [InheritanceMapping(Code = "E", Type = typeof(Employee))]
	///     class Person { ... }
	///     
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class InheritanceMappingAttribute : Attribute 
	{
		/// <summary>
		/// Discriminator value in store column for this type.
		/// </summary>
		public object Code { get; set; }

		/// <summary>
		/// Type to instantiate when Key is matched.
		/// </summary>
		public Type Type { get; set; }

		/// <summary>
		/// If discriminator value in store column is unrecognized then instantiate this type.
		/// </summary>
		public bool IsDefault { get; set; }
	}
}