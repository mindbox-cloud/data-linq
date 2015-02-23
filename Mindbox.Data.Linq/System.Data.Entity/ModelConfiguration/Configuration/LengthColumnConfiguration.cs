namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Used to configure a column with length facets for an entity type or complex type.
	/// </summary>
	public abstract class LengthColumnConfiguration : PrimitiveColumnConfiguration
	{
		/// <summary>Configures the column to allow the maximum length supported by the database provider.</summary>
		/// <returns>The same <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.LengthColumnConfiguration" /> instance so that multiple calls can be chained.</returns>
		public LengthColumnConfiguration IsMaxLength()
		{
			throw new NotImplementedException();
		}

		/// <summary>Configures the column to have the specified maximum length.</summary>
		/// <returns>The same <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.LengthColumnConfiguration" /> instance so that multiple calls can be chained.</returns>
		/// <param name="value">The maximum length for the column. Setting the value to null will remove any maximum length restriction from the column and a default length will be used for the database column.</param>
		public LengthColumnConfiguration HasMaxLength(int? value)
		{
			throw new NotImplementedException();
		}

		/// <summary>Configures the column to be fixed length.</summary>
		/// <returns>The same <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.LengthColumnConfiguration" /> instance so that multiple calls can be chained.</returns>
		public LengthColumnConfiguration IsFixedLength()
		{
			throw new NotImplementedException();
		}

		/// <summary>Configures the column to be variable length.</summary>
		/// <returns>The same <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.LengthColumnConfiguration" /> instance so that multiple calls can be chained.</returns>
		public LengthColumnConfiguration IsVariableLength()
		{
			throw new NotImplementedException();
		}
	}
}
