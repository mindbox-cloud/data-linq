using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Configures an required relationship from an entity type.
	/// </summary>
	/// <typeparam name="TEntityType"> The entity type that the relationship originates from. </typeparam>
	/// <typeparam name="TTargetEntityType"> The entity type that the relationship targets. </typeparam>
	public class RequiredNavigationPropertyConfiguration<TEntityType, TTargetEntityType>
		where TEntityType : class
		where TTargetEntityType : class
	{
		/// <summary>
		/// Configures the relationship to be required:many with a navigation property on the other side of the relationship.
		/// </summary>
		/// <param name="navigationPropertyExpression"> An lambda expression representing the navigation property on the other end of the relationship. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to further configure the relationship. </returns>
		public DependentNavigationPropertyConfiguration<TEntityType> WithMany(
			Expression<Func<TTargetEntityType, ICollection<TEntityType>>> navigationPropertyExpression)
		{
			if (navigationPropertyExpression == null)
				throw new ArgumentNullException("navigationPropertyExpression");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the relationship to be required:many without a navigation property on the other side of the relationship.
		/// </summary>
		/// <returns> A configuration object that can be used to further configure the relationship. </returns>
		public DependentNavigationPropertyConfiguration<TEntityType> WithMany()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the relationship to be required:optional with a navigation property on the other side of the relationship.
		/// </summary>
		/// <param name="navigationPropertyExpression"> An lambda expression representing the navigation property on the other end of the relationship. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to further configure the relationship. </returns>
		public ForeignKeyNavigationPropertyConfiguration WithOptional(
			Expression<Func<TTargetEntityType, TEntityType>> navigationPropertyExpression)
		{
			if (navigationPropertyExpression == null)
				throw new ArgumentNullException("navigationPropertyExpression");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the relationship to be required:optional without a navigation property on the other side of the relationship.
		/// </summary>
		/// <returns> A configuration object that can be used to further configure the relationship. </returns>
		public ForeignKeyNavigationPropertyConfiguration WithOptional()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the relationship to be required:required with a navigation property on the other side of the relationship.
		/// The entity type being configured will be the dependent and contain a foreign key to the principal.
		/// The entity type that the relationship targets will be the principal in the relationship.
		/// </summary>
		/// <param name="navigationPropertyExpression"> An lambda expression representing the navigation property on the other end of the relationship. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to further configure the relationship. </returns>
		public ForeignKeyNavigationPropertyConfiguration WithRequiredDependent(
			Expression<Func<TTargetEntityType, TEntityType>> navigationPropertyExpression)
		{
			if (navigationPropertyExpression == null)
				throw new ArgumentNullException("navigationPropertyExpression");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the relationship to be required:required without a navigation property on the other side of the relationship.
		/// The entity type being configured will be the dependent and contain a foreign key to the principal.
		/// The entity type that the relationship targets will be the principal in the relationship.
		/// </summary>
		/// <returns> A configuration object that can be used to further configure the relationship. </returns>
		public ForeignKeyNavigationPropertyConfiguration WithRequiredDependent()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the relationship to be required:required with a navigation property on the other side of the relationship.
		/// The entity type being configured will be the principal in the relationship.
		/// The entity type that the relationship targets will be the dependent and contain a foreign key to the principal.
		/// </summary>
		/// <param name="navigationPropertyExpression"> An lambda expression representing the navigation property on the other end of the relationship. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to further configure the relationship. </returns>
		public ForeignKeyNavigationPropertyConfiguration WithRequiredPrincipal(
			Expression<Func<TTargetEntityType, TEntityType>> navigationPropertyExpression)
		{
			if (navigationPropertyExpression == null)
				throw new ArgumentNullException("navigationPropertyExpression");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the relationship to be required:required without a navigation property on the other side of the relationship.
		/// The entity type being configured will be the principal in the relationship.
		/// The entity type that the relationship targets will be the dependent and contain a foreign key to the principal.
		/// </summary>
		/// <returns> A configuration object that can be used to further configure the relationship. </returns>
		public ForeignKeyNavigationPropertyConfiguration WithRequiredPrincipal()
		{
			throw new NotImplementedException();
		}
	}
}
