using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mindbox.Data.Linq.Mapping;
using Mindbox.Data.Linq.Mapping.Entity;
using Mindbox.Expressions;

namespace System.Data.Entity.ModelConfiguration
{
	/// <summary>
	/// Allows configuration to be performed for an entity type in a model.
	/// </summary>
	/// <typeparam name="TEntityType">The entity type being configured.</typeparam>
	public class EntityTypeConfiguration<TEntityType> : StructuralTypeConfiguration<TEntityType>, IEntityTypeConfiguration
		where TEntityType : class
	{
		private TableAttribute tableAttribute;
		private PropertyInfo primaryKeyProperty;
		private readonly Dictionary<PropertyInfo, IRequiredNavigationPropertyConfiguration> requiredAssociationsByProperty =
			new Dictionary<PropertyInfo, IRequiredNavigationPropertyConfiguration>();
		private readonly Dictionary<PropertyInfo, IOptionalNavigationPropertyConfiguration> optionalAssociationsByProperty =
			new Dictionary<PropertyInfo, IOptionalNavigationPropertyConfiguration>();


		Type IEntityTypeConfiguration.EntityType
		{
			get { return typeof(TEntityType); }
		}

		TableAttribute IEntityTypeConfiguration.TableAttribute
		{
			get { return tableAttribute; }
		}


		/// <summary>
		/// Configures the primary key property(s) for this entity type.
		/// </summary>
		/// <typeparam name="TKey"> The type of the key. </typeparam>
		/// <param name="keyExpression"> A lambda expression representing the property to be used as the primary key. 
		/// C#: t => t.Id VB.Net: Function(t) t.Id 
		/// If the primary key is made up of multiple properties then specify an anonymous type including the properties. 
		/// C#: t => new { t.Id1, t.Id2 } VB.Net: Function(t) New With { t.Id1, t.Id2 } </param>
		/// <returns> The same EntityTypeConfiguration instance so that multiple calls can be chained. </returns>
		public EntityTypeConfiguration<TEntityType> HasKey<TKey>(Expression<Func<TEntityType, TKey>> keyExpression)
		{
			if (keyExpression == null)
				throw new ArgumentNullException("keyExpression");

			primaryKeyProperty = ReflectionExpressions.GetPropertyInfo(keyExpression);
			return this;
		}

		/// <summary>
		/// Excludes a property from the model so that it will not be mapped to the database.
		/// </summary>
		/// <typeparam name="TProperty"> The type of the property to be ignored. </typeparam>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. 
		/// C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> The same EntityTypeConfiguration instance so that multiple calls can be chained. </returns>
		public EntityTypeConfiguration<TEntityType> Ignore<TProperty>(
			Expression<Func<TEntityType, TProperty>> propertyExpression)
		{
			if (propertyExpression == null)
				throw new ArgumentNullException("propertyExpression");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the table name that this entity type is mapped to.
		/// </summary>
		/// <param name="tableName"> The name of the table. </param>
		/// <returns> The same EntityTypeConfiguration instance so that multiple calls can be chained. </returns>
		public EntityTypeConfiguration<TEntityType> ToTable(string tableName)
		{
			if (string.IsNullOrEmpty(tableName))
				throw new ArgumentException("string.IsNullOrEmpty(tableName)", "tableName");

			return ToTable(tableName, null);
		}

		/// <summary>
		/// Configures the table name that this entity type is mapped to.
		/// </summary>
		/// <param name="tableName"> The name of the table. </param>
		/// <param name="schemaName"> The database schema of the table. </param>
		/// <returns> The same EntityTypeConfiguration instance so that multiple calls can be chained. </returns>
		public EntityTypeConfiguration<TEntityType> ToTable(string tableName, string schemaName)
		{
			if (string.IsNullOrEmpty(tableName))
				throw new ArgumentException("string.IsNullOrEmpty(tableName)", "tableName");

			if ((tableAttribute != null) && !string.IsNullOrEmpty(tableAttribute.Name))
				throw new InvalidOperationException("(tableAttribute != null) && !string.IsNullOrEmpty(tableAttribute.Name)");

			tableAttribute = tableAttribute ?? new TableAttribute();
			tableAttribute.Name = string.IsNullOrEmpty(schemaName) ? tableName : schemaName + "." + tableName;
			return this;
		}

		/// <summary>
		/// Allows advanced configuration related to how this entity type is mapped to the database schema.
		/// By default, any configuration will also apply to any type derived from this entity type.
		/// Derived types can be configured via the overload of Map that configures a derived type or
		/// by using an EntityTypeConfiguration for the derived type.
		/// The properties of an entity can be split between multiple tables using multiple Map calls.
		/// Calls to Map are additive, subsequent calls will not override configuration already preformed via Map.
		/// </summary>
		/// <param name="entityMappingConfigurationAction">
		/// An action that performs configuration against an
		/// <see
		///     cref="EntityMappingConfiguration{TEntityType}" />
		/// .
		/// </param>
		/// <returns> The same EntityTypeConfiguration instance so that multiple calls can be chained. </returns>
		public EntityTypeConfiguration<TEntityType> Map(
			Action<EntityMappingConfiguration<TEntityType>> entityMappingConfigurationAction)
		{
			if (entityMappingConfigurationAction == null)
				throw new ArgumentNullException("entityMappingConfigurationAction");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Allows advanced configuration related to how a derived entity type is mapped to the database schema.
		/// Calls to Map are additive, subsequent calls will not override configuration already preformed via Map.
		/// </summary>
		/// <typeparam name="TDerived"> The derived entity type to be configured. </typeparam>
		/// <param name="derivedTypeMapConfigurationAction">
		/// An action that performs configuration against an
		/// <see
		///     cref="EntityMappingConfiguration{TEntityType}" />
		/// .
		/// </param>
		/// <returns> The same EntityTypeConfiguration instance so that multiple calls can be chained. </returns>
		public EntityTypeConfiguration<TEntityType> Map<TDerived>(
			Action<EntityMappingConfiguration<TDerived>> derivedTypeMapConfigurationAction)
			where TDerived : class, TEntityType
		{
			if (derivedTypeMapConfigurationAction == null)
				throw new ArgumentNullException("derivedTypeMapConfigurationAction");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures an optional relationship from this entity type.
		/// Instances of the entity type will be able to be saved to the database without this relationship being specified.
		/// The foreign key in the database will be nullable.
		/// </summary>
		/// <typeparam name="TTargetEntity"> The type of the entity at the other end of the relationship. </typeparam>
		/// <param name="navigationPropertyExpression"> A lambda expression representing the navigation property 
		/// for the relationship. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to further configure the relationship. </returns>
		public OptionalNavigationPropertyConfiguration<TEntityType, TTargetEntity> HasOptional<TTargetEntity>(
			Expression<Func<TEntityType, TTargetEntity>> navigationPropertyExpression)
			where TTargetEntity : class
		{
			if (navigationPropertyExpression == null)
				throw new ArgumentNullException("navigationPropertyExpression");

			var navigationProperty = ReflectionExpressions.GetPropertyInfo(navigationPropertyExpression);
			IOptionalNavigationPropertyConfiguration propertyConfiguration;
			if (!optionalAssociationsByProperty.TryGetValue(navigationProperty, out propertyConfiguration))
			{
				propertyConfiguration =
					new OptionalNavigationPropertyConfiguration<TEntityType, TTargetEntity>(navigationProperty);
				optionalAssociationsByProperty.Add(navigationProperty, propertyConfiguration);
			}
			return (OptionalNavigationPropertyConfiguration<TEntityType, TTargetEntity>)propertyConfiguration;
		}

		/// <summary>
		/// Configures a required relationship from this entity type.
		/// Instances of the entity type will not be able to be saved to the database unless this relationship is specified.
		/// The foreign key in the database will be non-nullable.
		/// </summary>
		/// <typeparam name="TTargetEntity"> The type of the entity at the other end of the relationship. </typeparam>
		/// <param name="navigationPropertyExpression"> A lambda expression representing the navigation property 
		/// for the relationship. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to further configure the relationship. </returns>
		public RequiredNavigationPropertyConfiguration<TEntityType, TTargetEntity> HasRequired<TTargetEntity>(
			Expression<Func<TEntityType, TTargetEntity>> navigationPropertyExpression)
			where TTargetEntity : class
		{
			if (navigationPropertyExpression == null)
				throw new ArgumentNullException("navigationPropertyExpression");

			var navigationProperty = ReflectionExpressions.GetPropertyInfo(navigationPropertyExpression);
			IRequiredNavigationPropertyConfiguration propertyConfiguration;
			if (!requiredAssociationsByProperty.TryGetValue(navigationProperty, out propertyConfiguration))
			{
				propertyConfiguration = 
					new RequiredNavigationPropertyConfiguration<TEntityType, TTargetEntity>(navigationProperty);
				requiredAssociationsByProperty.Add(navigationProperty, propertyConfiguration);
			}
			return (RequiredNavigationPropertyConfiguration<TEntityType, TTargetEntity>)propertyConfiguration;
		}

		/// <summary>
		/// Configures a many relationship from this entity type.
		/// </summary>
		/// <typeparam name="TTargetEntity"> The type of the entity at the other end of the relationship. </typeparam>
		/// <param name="navigationPropertyExpression"> A lambda expression representing the navigation property 
		/// for the relationship. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to further configure the relationship. </returns>
		public ManyNavigationPropertyConfiguration<TEntityType, TTargetEntity> HasMany<TTargetEntity>(
			Expression<Func<TEntityType, ICollection<TTargetEntity>>> navigationPropertyExpression)
			where TTargetEntity : class
		{
			if (navigationPropertyExpression == null)
				throw new ArgumentNullException("navigationPropertyExpression");

			throw new NotImplementedException();
		}


		IEnumerable<ColumnAttributeByMember> IEntityTypeConfiguration.GetColumnAttributesByMember(DbModelBuilder dbModelBuilder)
		{
			if (dbModelBuilder == null)
				throw new ArgumentNullException("dbModelBuilder");

			foreach (var propertyConfiguration in PropertyConfigurationsByProperty.Values)
			{
				var columnAttributeByMember = propertyConfiguration.GetColumnAttribute();
				AdjustIsPrimaryKey(columnAttributeByMember);
				yield return columnAttributeByMember;
			}

			foreach (var requiredNavigationPropertyConfiguration in requiredAssociationsByProperty.Values)
			{
				var columnAttributeByMember = requiredNavigationPropertyConfiguration.TryGetColumnAttribute(dbModelBuilder);
				if (columnAttributeByMember != null)
				{
					AdjustIsPrimaryKey(columnAttributeByMember);
					yield return columnAttributeByMember;
				}
			}

			foreach (var optionalNavigationPropertyConfiguration in optionalAssociationsByProperty.Values)
			{
				var columnAttributeByMember = optionalNavigationPropertyConfiguration.TryGetColumnAttribute(dbModelBuilder);
				if (columnAttributeByMember != null)
				{
					AdjustIsPrimaryKey(columnAttributeByMember);
					yield return columnAttributeByMember;
				}
			}
		}

		PrimitivePropertyConfiguration IEntityTypeConfiguration.GetPrimaryKeyPropertyConfiguration()
		{
			if (primaryKeyProperty == null)
				throw new InvalidOperationException("primaryKeyProperty == null");

			return PropertyConfigurationsByProperty[primaryKeyProperty];
		}

		IEnumerable<AssociationAttributeByMember> IEntityTypeConfiguration.GetAssociationAttributesByMember(
			DbModelBuilder dbModelBuilder)
		{
			if (dbModelBuilder == null)
				throw new ArgumentNullException("dbModelBuilder");

			foreach (var requiredNavigationPropertyConfiguration in requiredAssociationsByProperty.Values)
				yield return requiredNavigationPropertyConfiguration.GetAssociationAttribute(dbModelBuilder);
			foreach (var optionalNavigationPropertyConfiguration in optionalAssociationsByProperty.Values)
				yield return optionalNavigationPropertyConfiguration.GetAssociationAttribute(dbModelBuilder);
		}


		private void AdjustIsPrimaryKey(ColumnAttribute columnAttribute, MemberInfo member)
		{
			if (columnAttribute == null)
				throw new ArgumentNullException("columnAttribute");
			if (member == null)
				throw new ArgumentNullException("member");

			if (member == primaryKeyProperty)
				columnAttribute.IsPrimaryKey = true;
		}

		private void AdjustIsPrimaryKey(ColumnAttributeByMember columnAttributeByMember)
		{
			if (columnAttributeByMember == null)
				throw new ArgumentNullException("columnAttributeByMember");

			AdjustIsPrimaryKey(columnAttributeByMember.Attribute, columnAttributeByMember.Member);
		}
	}
}
