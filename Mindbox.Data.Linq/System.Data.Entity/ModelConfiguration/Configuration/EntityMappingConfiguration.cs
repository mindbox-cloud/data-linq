using System.Linq;
using System.Linq.Expressions;

namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Configures the table and column mapping for an entity type or a sub-set of properties from an entity type.
	/// </summary>
	/// <typeparam name="TEntityType"> The entity type to be mapped. </typeparam>
	public class EntityMappingConfiguration<TEntityType>
		where TEntityType : class
	{
		/// <summary>Initializes a new instance of the <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.EntityMappingConfiguration`1" /> class.</summary>
		public EntityMappingConfiguration()
		{
			throw new NotImplementedException();
		}


		/// <summary>
		/// Configures the properties that will be included in this mapping fragment.
		/// If this method is not called then all properties that have not yet been
		/// included in a mapping fragment will be configured.
		/// </summary>
		/// <typeparam name="TObject"> An anonymous type including the properties to be mapped. </typeparam>
		/// <param name="propertiesExpression"> A lambda expression to an anonymous type that contains the properties to be mapped. C#: t => new { t.Id, t.Property1, t.Property2 } VB.Net: Function(t) New With { p.Id, t.Property1, t.Property2 } </param>
		public void Properties<TObject>(Expression<Func<TEntityType, TObject>> propertiesExpression)
		{
			if (propertiesExpression == null)
				throw new ArgumentNullException("propertiesExpression");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.struct" /> property that is included in this mapping fragment.
		/// </summary>
		/// <typeparam name="T"> The type of the property being configured. </typeparam>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public PropertyMappingConfiguration Property<T>(
			Expression<Func<TEntityType, T>> propertyExpression)
			where T : struct
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.struct?" /> property that is included in this mapping fragment.
		/// </summary>
		/// <typeparam name="T"> The type of the property being configured. </typeparam>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public PropertyMappingConfiguration Property<T>(
			Expression<Func<TEntityType, T?>> propertyExpression)
			where T : struct
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.string" /> property that is included in this mapping fragment.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public PropertyMappingConfiguration Property(Expression<Func<TEntityType, string>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.byte[]" /> property that is included in this mapping fragment.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public PropertyMappingConfiguration Property(Expression<Func<TEntityType, byte[]>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.decimal" /> property that is included in this mapping fragment.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public PropertyMappingConfiguration Property(Expression<Func<TEntityType, decimal>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.decimal?" /> property that is included in this mapping fragment.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public PropertyMappingConfiguration Property(Expression<Func<TEntityType, decimal?>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.DateTime" /> property that is included in this mapping fragment.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public PropertyMappingConfiguration Property(Expression<Func<TEntityType, DateTime>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.DateTime?" /> property that is included in this mapping fragment.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public PropertyMappingConfiguration Property(Expression<Func<TEntityType, DateTime?>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the table name to be mapped to.
		/// </summary>
		/// <param name="tableName"> Name of the table. </param>
		/// <returns> The same configuration instance so that multiple calls can be chained. </returns>
		public EntityMappingConfiguration<TEntityType> ToTable(string tableName)
		{
			if (string.IsNullOrEmpty(tableName))
				throw new ArgumentException("string.IsNullOrEmpty(tableName)", "tableName");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the table name and schema to be mapped to.
		/// </summary>
		/// <param name="tableName"> Name of the table. </param>
		/// <param name="schemaName"> Schema of the table. </param>
		/// <returns> The same configuration instance so that multiple calls can be chained. </returns>
		public EntityMappingConfiguration<TEntityType> ToTable(string tableName, string schemaName)
		{
			if (string.IsNullOrEmpty(tableName))
				throw new ArgumentException("string.IsNullOrEmpty(tableName)", "tableName");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the discriminator column used to differentiate between types in an inheritance hierarchy.
		/// </summary>
		/// <param name="discriminator"> The name of the discriminator column. </param>
		/// <returns> A configuration object to further configure the discriminator column and values. </returns>
		public ValueConditionConfiguration Requires(string discriminator)
		{
			if (string.IsNullOrEmpty(discriminator))
				throw new ArgumentException("string.IsNullOrEmpty(discriminator)", "discriminator");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the discriminator condition used to differentiate between types in an inheritance hierarchy.
		/// </summary>
		/// <typeparam name="TProperty"> The type of the property being used to discriminate between types. </typeparam>
		/// <param name="property"> A lambda expression representing the property being used to discriminate between types. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object to further configure the discriminator condition. </returns>
		public NotNullConditionConfiguration Requires<TProperty>(Expression<Func<TEntityType, TProperty>> property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			throw new NotImplementedException();
		}
	}
}
