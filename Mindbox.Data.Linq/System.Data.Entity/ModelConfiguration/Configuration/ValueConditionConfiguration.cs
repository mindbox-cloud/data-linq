using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Configures a discriminator column used to differentiate between types in an inheritance hierarchy.
	/// </summary>
	[DebuggerDisplay("{Discriminator}")]
	public class ValueConditionConfiguration
	{
		/// <summary>
		/// Configures the discriminator value used to identify the entity type being
		/// configured from other types in the inheritance hierarchy.
		/// </summary>
		/// <typeparam name="T"> Type of the discriminator value. </typeparam>
		/// <param name="value"> The value to be used to identify the entity type. </param>
		/// <returns> A configuration object to configure the column used to store discriminator values. </returns>
		public PrimitiveColumnConfiguration HasValue<T>(T value)
			where T : struct
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the discriminator value used to identify the entity type being
		/// configured from other types in the inheritance hierarchy.
		/// </summary>
		/// <typeparam name="T"> Type of the discriminator value. </typeparam>
		/// <param name="value"> The value to be used to identify the entity type. </param>
		/// <returns> A configuration object to configure the column used to store discriminator values. </returns>
		public PrimitiveColumnConfiguration HasValue<T>(T? value)
			where T : struct
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the discriminator value used to identify the entity type being
		/// configured from other types in the inheritance hierarchy.
		/// </summary>
		/// <param name="value"> The value to be used to identify the entity type. </param>
		/// <returns> A configuration object to configure the column used to store discriminator values. </returns>
		public StringColumnConfiguration HasValue(string value)
		{
			throw new NotImplementedException();
		}
	}
}
