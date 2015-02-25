using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using Mindbox.Data.Linq.Mapping;

namespace System.Data.Entity
{
	/// <summary>
	/// DbModelBuilder is used to map CLR classes to a database schema.
	/// </summary>
	public class DbModelBuilder
	{
		private readonly ConfigurationRegistrar configurations = new ConfigurationRegistrar();


		/// <summary>
		/// Gets the <see cref="ConfigurationRegistrar" /> for this DbModelBuilder.
		/// The registrar allows derived entity and complex type configurations to be registered with this builder.
		/// </summary>
		public virtual ConfigurationRegistrar Configurations
		{
			get { return configurations; }
		}


		/// <summary>
		/// Excludes a type from the model. This is used to remove types from the model that were added
		/// by convention during initial model discovery.
		/// </summary>
		/// <typeparam name="T"> The type to be excluded. </typeparam>
		/// <returns> The same DbModelBuilder instance so that multiple calls can be chained. </returns>
		public virtual DbModelBuilder Ignore<T>()
			where T : class
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the default database schema name. This default database schema name is used
		/// for database objects that do not have an explicitly configured schema name.
		/// </summary>
		/// <param name="schema"> The name of the default database schema. </param>
		/// <returns> The same DbModelBuilder instance so that multiple calls can be chained. </returns>
		public virtual DbModelBuilder HasDefaultSchema(string schema)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Excludes the specified type(s) from the model. This is used to remove types from the model that were added
		/// by convention during initial model discovery.
		/// </summary>
		/// <param name="types"> The types to be excluded from the model. </param>
		/// <returns> The same DbModelBuilder instance so that multiple calls can be chained. </returns>
		public virtual DbModelBuilder Ignore(IEnumerable<Type> types)
		{
			if (types == null)
				throw new ArgumentNullException("types");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Registers an entity type as part of the model and returns an object that can be used to
		/// configure the entity. This method can be called multiple times for the same entity to
		/// perform multiple lines of configuration.
		/// </summary>
		/// <typeparam name="TEntityType"> The type to be registered or configured. </typeparam>
		/// <returns> The configuration object for the specified entity type. </returns>
		public virtual EntityTypeConfiguration<TEntityType> Entity<TEntityType>()
			where TEntityType : class
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Registers an entity type as part of the model.
		/// </summary>
		/// <param name="entityType"> The type to be registered. </param>
		/// <remarks>
		/// This method is provided as a convenience to allow entity types to be registered dynamically
		/// without the need to use MakeGenericMethod in order to call the normal generic Entity method.
		/// This method does not allow further configuration of the entity type using the fluent APIs since
		/// these APIs make extensive use of generic type parameters.
		/// </remarks>
		public virtual void RegisterEntityType(Type entityType)
		{
			if (entityType == null)
				throw new ArgumentNullException("entityType");

			throw new NotImplementedException();
		}


		internal IEnumerable<TableAttributeByRootType> GetTableAttributesByRootType()
		{
			return configurations.GetTableAttributesByRootType();
		}

		internal IEnumerable<ColumnAttributeByMember> GetColumnAttributesByMember()
		{
			return configurations.GetColumnAttributesByMember();
		}

		internal void Validate()
		{
			configurations.Validate();
		}
	}
}
