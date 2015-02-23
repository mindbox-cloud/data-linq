namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Configures a condition used to discriminate between types in an inheritance hierarchy based on the values assigned to a property.
	/// </summary>
	public class NotNullConditionConfiguration
	{
		/// <summary>
		/// Configures the condition to require a value in the property.
		/// Rows that do not have a value assigned to column that this property is stored in are
		/// assumed to be of the base type of this entity type.
		/// </summary>
		public void HasValue()
		{
			throw new NotImplementedException();
		}
	}
}
