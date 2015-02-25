using System.Data.Linq.Mapping;
using System.Globalization;
using System.Reflection;

namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Used to configure a property with length facets for an entity type or complex type.
	/// </summary>
	public abstract class LengthPropertyConfiguration : PrimitivePropertyConfiguration
	{
		private int? maxLength;
		private bool? hasVariableLength;


		internal LengthPropertyConfiguration(PropertyInfo property) 
			: base(property)
		{
		}


		/// <summary>
		/// Configures the property to allow the maximum length supported by the database provider.
		/// </summary>
		/// <returns> The same LengthPropertyConfiguration instance so that multiple calls can be chained. </returns>
		public LengthPropertyConfiguration IsMaxLength()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the property to have the specified maximum length.
		/// </summary>
		/// <param name="value"> The maximum length for the property. Setting 'null' will remove any maximum length restriction from the property and a default length will be used for the database column. </param>
		/// <returns> The same LengthPropertyConfiguration instance so that multiple calls can be chained. </returns>
		public LengthPropertyConfiguration HasMaxLength(int? value)
		{
			if (value <= 0)
				throw new ArgumentException("value <= 0", "value");
			if ((maxLength != null) && (maxLength != value))
				throw new ArgumentException("(maxLength != null) && (maxLength != value)", "value");

			maxLength = value;
			return this;
		}

		/// <summary>
		/// Configures the property to be fixed length.
		/// Use HasMaxLength to set the length that the property is fixed to.
		/// </summary>
		/// <returns> The same LengthPropertyConfiguration instance so that multiple calls can be chained. </returns>
		public LengthPropertyConfiguration IsFixedLength()
		{
			if (hasVariableLength == true)
				throw new InvalidOperationException("hasVariableLength == true");

			hasVariableLength = false;
			return this;
		}

		/// <summary>
		/// Configures the property to be variable length.
		/// Properties are variable length by default.
		/// </summary>
		/// <returns> The same LengthPropertyConfiguration instance so that multiple calls can be chained. </returns>
		public LengthPropertyConfiguration IsVariableLength()
		{
			if (hasVariableLength == false)
				throw new InvalidOperationException("hasVariableLength == false");

			hasVariableLength = true;
			return this;
		}


		protected override string BuildDbTypeWithoutNullability(ColumnAttribute columnAttribute)
		{
			if (columnAttribute == null)
				throw new ArgumentNullException("columnAttribute");

			var baseDbType = base.BuildDbTypeWithoutNullability(columnAttribute);
			return (baseDbType == null) || (maxLength == null) ? 
				baseDbType : 
				baseDbType + "(" + maxLength.Value.ToString(CultureInfo.InvariantCulture) + ")";
		}
	}
}
