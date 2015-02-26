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
		private bool hasMaxPossibleLength;

	
		internal LengthPropertyConfiguration(PropertyInfo property) 
			: base(property)
		{
		}


		protected bool? HasVariableLength { get; private set; }


		/// <summary>
		/// Configures the property to allow the maximum length supported by the database provider.
		/// </summary>
		/// <returns> The same LengthPropertyConfiguration instance so that multiple calls can be chained. </returns>
		public LengthPropertyConfiguration IsMaxLength()
		{
			hasMaxPossibleLength = true;
			maxLength = null;
			return this;
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

			maxLength = value;
			hasMaxPossibleLength = false;
			return this;
		}

		/// <summary>
		/// Configures the property to be fixed length.
		/// Use HasMaxLength to set the length that the property is fixed to.
		/// </summary>
		/// <returns> The same LengthPropertyConfiguration instance so that multiple calls can be chained. </returns>
		public LengthPropertyConfiguration IsFixedLength()
		{
			HasVariableLength = false;
			return this;
		}

		/// <summary>
		/// Configures the property to be variable length.
		/// Properties are variable length by default.
		/// </summary>
		/// <returns> The same LengthPropertyConfiguration instance so that multiple calls can be chained. </returns>
		public LengthPropertyConfiguration IsVariableLength()
		{
			HasVariableLength = true;
			return this;
		}


		protected override string BuildDbTypeWithoutNullability(ColumnAttribute columnAttribute)
		{
			if (columnAttribute == null)
				throw new ArgumentNullException("columnAttribute");

			var baseDbType = base.BuildDbTypeWithoutNullability(columnAttribute);
			if (baseDbType == null)
				return null;
			var lengthSpecification = (hasMaxPossibleLength ? "max" : (maxLength ?? 15).ToString(CultureInfo.InvariantCulture));
			return baseDbType + "(" + lengthSpecification + ")";
		}
	}
}
