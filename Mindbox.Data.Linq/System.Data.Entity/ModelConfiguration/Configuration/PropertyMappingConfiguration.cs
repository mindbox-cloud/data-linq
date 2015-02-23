namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Used to configure a property in a mapping fragment.
	/// </summary>
	public class PropertyMappingConfiguration
	{
		/// <summary>
		/// Configures the name of the database column used to store the property, in a mapping fragment.
		/// </summary>
		/// <param name="columnName"> The name of the column. </param>
		/// <returns> The same PropertyMappingConfiguration instance so that multiple calls can be chained. </returns>
		public PropertyMappingConfiguration HasColumnName(string columnName)
		{
			throw new NotImplementedException();
		}
	}
}
