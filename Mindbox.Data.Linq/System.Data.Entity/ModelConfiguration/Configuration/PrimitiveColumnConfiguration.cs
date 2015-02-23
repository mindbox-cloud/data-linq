namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Configures a primitive column from an entity type.
	/// </summary>
	public class PrimitiveColumnConfiguration
	{
		/// <summary>Configures the primitive column to be optional.</summary>
		/// <returns>The same <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.PrimitiveColumnConfiguration" /> instance so that multiple calls can be chained.</returns>
		public PrimitiveColumnConfiguration IsOptional()
		{
			throw new NotImplementedException();
		}

		/// <summary>Configures the primitive column to be required.</summary>
		/// <returns>The same <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.PrimitiveColumnConfiguration" /> instance so that multiple calls can be chained.</returns>
		public PrimitiveColumnConfiguration IsRequired()
		{
			throw new NotImplementedException();
		}

		/// <summary>Configures the data type of the primitive column used to store the property.</summary>
		/// <returns>The same <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.PrimitiveColumnConfiguration" /> instance so that multiple calls can be chained.</returns>
		/// <param name="columnType">The name of the database provider specific data type.</param>
		public PrimitiveColumnConfiguration HasColumnType(string columnType)
		{
			throw new NotImplementedException();
		}

		/// <summary>Configures the order of the primitive column used to store the property. This method is also used to specify key ordering when an entity type has a composite key.</summary>
		/// <returns>The same <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.PrimitiveColumnConfiguration" /> instance so that multiple calls can be chained.</returns>
		/// <param name="columnOrder">The order that this column should appear in the database table.</param>
		public PrimitiveColumnConfiguration HasColumnOrder(int? columnOrder)
		{
			throw new NotImplementedException();
		}
	}
}
