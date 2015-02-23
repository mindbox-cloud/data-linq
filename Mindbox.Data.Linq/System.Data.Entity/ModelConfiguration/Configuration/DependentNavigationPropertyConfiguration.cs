using System.Linq;
using System.Linq.Expressions;

namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Configures a relationship that can support foreign key properties that are exposed in the object model.
	/// </summary>
	/// <typeparam name="TDependentEntityType"> The dependent entity type. </typeparam>
	public class DependentNavigationPropertyConfiguration<TDependentEntityType> :
		ForeignKeyNavigationPropertyConfiguration
		where TDependentEntityType : class
	{
		/// <summary>
		/// Configures the relationship to use foreign key property(s) that are exposed in the object model.
		/// If the foreign key property(s) are not exposed in the object model then use the Map method.
		/// </summary>
		/// <typeparam name="TKey"> The type of the key. </typeparam>
		/// <param name="foreignKeyExpression"> A lambda expression representing the property to be used as the foreign key. If the foreign key is made up of multiple properties then specify an anonymous type including the properties. When using multiple foreign key properties, the properties must be specified in the same order that the the primary key properties were configured for the principal entity type. </param>
		/// <returns> A configuration object that can be used to further configure the relationship. </returns>
		public CascadableNavigationPropertyConfiguration HasForeignKey<TKey>(
			Expression<Func<TDependentEntityType, TKey>> foreignKeyExpression)
		{
			if (foreignKeyExpression == null)
				throw new ArgumentNullException("foreignKeyExpression");

			throw new NotImplementedException();
		}
	}
}
