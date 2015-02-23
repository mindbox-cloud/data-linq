using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mindbox.Data.Linq.Mapping;
using Mindbox.Data.Linq.Mapping.Entity;

namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Allows derived configuration classes for entities and complex types to be registered with a
	/// <see cref="DbModelBuilder" />.
	/// </summary>
	public class ConfigurationRegistrar
	{
		private readonly List<IEntityTypeConfiguration> entityTypeConfigurations = new List<IEntityTypeConfiguration>();


		/// <summary>
		/// Discovers all types that inherit from <see cref="EntityTypeConfiguration" /> or
		/// <see cref="ComplexTypeConfiguration" /> in the given assembly and adds an instance
		/// of each discovered type to this registrar.
		/// </summary>
		/// <remarks>
		/// Note that only types that are abstract or generic type definitions are skipped. Every
		/// type that is discovered and added must provide a parameterless constructor.
		/// </remarks>
		/// <param name="assembly">The assembly containing model configurations to add.</param>
		/// <returns>The same ConfigurationRegistrar instance so that multiple calls can be chained.</returns>
		public virtual ConfigurationRegistrar AddFromAssembly(Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Adds an <see cref="EntityTypeConfiguration" /> to the <see cref="DbModelBuilder" />.
		/// Only one <see cref="EntityTypeConfiguration" /> can be added for each type in a model.
		/// </summary>
		/// <typeparam name="TEntityType"> The entity type being configured. </typeparam>
		/// <param name="entityTypeConfiguration"> The entity type configuration to be added. </param>
		/// <returns> The same ConfigurationRegistrar instance so that multiple calls can be chained. </returns>
		public virtual ConfigurationRegistrar Add<TEntityType>(
			EntityTypeConfiguration<TEntityType> entityTypeConfiguration)
			where TEntityType : class
		{
			if (entityTypeConfiguration == null)
				throw new ArgumentNullException("entityTypeConfiguration");

			entityTypeConfigurations.Add(entityTypeConfiguration);
			return this;
		}


		internal IEnumerable<TableAttributeByRootType> GetTableAttributesByRootType()
		{
			foreach (var entityTypeConfiguration in entityTypeConfigurations)
				if (entityTypeConfiguration.TableAttribute != null)
					yield return new TableAttributeByRootType
					{
						RootType = entityTypeConfiguration.EntityType,
						Attribute = entityTypeConfiguration.TableAttribute
					};
		}
	}
}
