using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mindbox.Data.Linq.Mapping;
using Mindbox.Data.Linq.Mapping.Entity;

namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Configures an required relationship from an entity type.
	/// </summary>
	/// <typeparam name="TEntityType"> The entity type that the relationship originates from. </typeparam>
	/// <typeparam name="TTargetEntityType"> The entity type that the relationship targets. </typeparam>
	public class RequiredNavigationPropertyConfiguration<TEntityType, TTargetEntityType> :
		IRequiredNavigationPropertyConfiguration
		where TEntityType : class
		where TTargetEntityType : class
	{
		private ForeignKeyNavigationPropertyConfiguration foreignKeyConfiguration;
		private readonly PropertyInfo associationProperty;


		internal RequiredNavigationPropertyConfiguration(PropertyInfo associationProperty)
		{
			if (associationProperty == null)
				throw new ArgumentNullException("associationProperty");

			this.associationProperty = associationProperty;
		}


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
			if (foreignKeyConfiguration == null)
				foreignKeyConfiguration = new DependentNavigationPropertyConfiguration<TEntityType>(
					associationProperty,
					isRequired: true);
			return (DependentNavigationPropertyConfiguration<TEntityType>)foreignKeyConfiguration;
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

		ColumnAttributeByMember IRequiredNavigationPropertyConfiguration.TryGetColumnAttribute(DbModelBuilder dbModelBuilder)
		{
			if (dbModelBuilder == null)
				throw new ArgumentNullException("dbModelBuilder");

			if (foreignKeyConfiguration == null)
				throw new InvalidOperationException("foreignKeyConfiguration == null");

			return foreignKeyConfiguration.TryGetColumnAttribute(dbModelBuilder);
		}

		AssociationAttributeByMember IRequiredNavigationPropertyConfiguration.GetAssociationAttribute(
			DbModelBuilder dbModelBuilder)
		{
			if (dbModelBuilder == null)
				throw new ArgumentNullException("dbModelBuilder");

			if (foreignKeyConfiguration == null)
				throw new InvalidOperationException("foreignKeyConfiguration == null");

			return foreignKeyConfiguration.GetAssociationAttribute(dbModelBuilder);
		}
	}
}
