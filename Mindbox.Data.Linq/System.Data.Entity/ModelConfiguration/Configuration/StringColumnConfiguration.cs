namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Configures a database column used to store a string values.
	/// </summary>
	public class StringColumnConfiguration : LengthColumnConfiguration
	{
		/// <summary>
		/// Configures the column to allow the maximum length supported by the database provider.
		/// </summary>
		/// <returns> The same StringColumnConfiguration instance so that multiple calls can be chained. </returns>
		public new StringColumnConfiguration IsMaxLength()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the property to have the specified maximum length.
		/// </summary>
		/// <param name="value">
		/// The maximum length for the property. Setting 'null' will result in a default length being used for the column.
		/// </param>
		/// <returns> The same StringColumnConfiguration instance so that multiple calls can be chained. </returns>
		public new StringColumnConfiguration HasMaxLength(int? value)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the column to be fixed length.
		/// Use HasMaxLength to set the length that the property is fixed to.
		/// </summary>
		/// <returns> The same StringColumnConfiguration instance so that multiple calls can be chained. </returns>
		public new StringColumnConfiguration IsFixedLength()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the column to be variable length.
		/// Columns are variable length by default.
		/// </summary>
		/// <returns> The same StringColumnConfiguration instance so that multiple calls can be chained. </returns>
		public new StringColumnConfiguration IsVariableLength()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the column to be optional.
		/// </summary>
		/// <returns> The same StringColumnConfiguration instance so that multiple calls can be chained. </returns>
		public new StringColumnConfiguration IsOptional()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the column to be required.
		/// </summary>
		/// <returns> The same StringColumnConfiguration instance so that multiple calls can be chained. </returns>
		public new StringColumnConfiguration IsRequired()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the data type of the database column.
		/// </summary>
		/// <param name="columnType"> Name of the database provider specific data type. </param>
		/// <returns> The same StringColumnConfiguration instance so that multiple calls can be chained. </returns>
		public new StringColumnConfiguration HasColumnType(string columnType)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the order of the database column.
		/// </summary>
		/// <param name="columnOrder"> The order that this column should appear in the database table. </param>
		/// <returns> The same StringColumnConfiguration instance so that multiple calls can be chained. </returns>
		public new StringColumnConfiguration HasColumnOrder(int? columnOrder)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the column to support Unicode string content.
		/// </summary>
		/// <returns> The same StringColumnConfiguration instance so that multiple calls can be chained. </returns>
		public StringColumnConfiguration IsUnicode()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures whether or not the column supports Unicode string content.
		/// </summary>
		/// <param name="unicode"> Value indicating if the column supports Unicode string content or not. Specifying 'null' will remove the Unicode facet from the column. Specifying 'null' will cause the same runtime behavior as specifying 'false'. </param>
		/// <returns> The same StringColumnConfiguration instance so that multiple calls can be chained. </returns>
		public StringColumnConfiguration IsUnicode(bool? unicode)
		{
			throw new NotImplementedException();
		}
	}
}
