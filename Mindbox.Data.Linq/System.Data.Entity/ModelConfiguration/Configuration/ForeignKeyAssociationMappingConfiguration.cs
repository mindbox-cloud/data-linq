using System.Collections.Generic;
using System.Linq;

namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Configures the table and column mapping of a relationship that does not expose foreign key properties in the object model.
	/// </summary>
	public sealed class ForeignKeyAssociationMappingConfiguration : AssociationMappingConfiguration
	{
		/// <summary>
		/// Configures the name of the column(s) for the foreign key.
		/// </summary>
		/// <param name="keyColumnNames"> The foreign key column names. When using multiple foreign key properties, the properties must be specified in the same order that the the primary key properties were configured for the target entity type. </param>
		/// <returns> The same ForeignKeyAssociationMappingConfiguration instance so that multiple calls can be chained. </returns>
		public ForeignKeyAssociationMappingConfiguration MapKey(params string[] keyColumnNames)
		{
			if (keyColumnNames == null)
				throw new ArgumentNullException("keyColumnNames");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the table name that the foreign key column(s) reside in.
		/// The table that is specified must already be mapped for the entity type.
		/// If you want the foreign key(s) to reside in their own table then use the Map method
		/// on <see cref="T:System.Data.Entity.ModelConfiguration.EntityTypeConfiguration" /> to perform
		/// entity splitting to create the table with just the primary key property. Foreign keys can
		/// then be added to the table via this method.
		/// </summary>
		/// <param name="tableName"> Name of the table. </param>
		/// <returns> The same ForeignKeyAssociationMappingConfiguration instance so that multiple calls can be chained. </returns>
		public ForeignKeyAssociationMappingConfiguration ToTable(string tableName)
		{
			if (string.IsNullOrEmpty(tableName))
				throw new ArgumentException("string.IsNullOrEmpty(tableName)", "tableName");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the table name and schema that the foreign key column(s) reside in.
		/// The table that is specified must already be mapped for the entity type.
		/// If you want the foreign key(s) to reside in their own table then use the Map method
		/// on <see cref="T:System.Data.Entity.ModelConfiguration.EntityTypeConfiguration" /> to perform
		/// entity splitting to create the table with just the primary key property. Foreign keys can
		/// then be added to the table via this method.
		/// </summary>
		/// <param name="tableName"> Name of the table. </param>
		/// <param name="schemaName"> Schema of the table. </param>
		/// <returns> The same ForeignKeyAssociationMappingConfiguration instance so that multiple calls can be chained. </returns>
		public ForeignKeyAssociationMappingConfiguration ToTable(string tableName, string schemaName)
		{
			if (string.IsNullOrEmpty(tableName))
				throw new ArgumentException("string.IsNullOrEmpty(tableName)", "tableName");

			throw new NotImplementedException();
		}
	}
}
