using System.Collections.Generic;
using System.Linq;

namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Configures the table and column mapping of a many:many relationship.
	/// </summary>
	public sealed class ManyToManyAssociationMappingConfiguration : AssociationMappingConfiguration
	{
		/// <summary>
		/// Configures the join table name for the relationship.
		/// </summary>
		/// <param name="tableName"> Name of the table. </param>
		/// <returns> The same ManyToManyAssociationMappingConfiguration instance so that multiple calls can be chained. </returns>
		public ManyToManyAssociationMappingConfiguration ToTable(string tableName)
		{
			if (string.IsNullOrEmpty(tableName))
				throw new ArgumentException("string.IsNullOrEmpty(tableName)", "tableName");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the join table name and schema for the relationship.
		/// </summary>
		/// <param name="tableName"> Name of the table. </param>
		/// <param name="schemaName"> Schema of the table. </param>
		/// <returns> The same ManyToManyAssociationMappingConfiguration instance so that multiple calls can be chained. </returns>
		public ManyToManyAssociationMappingConfiguration ToTable(string tableName, string schemaName)
		{
			if (string.IsNullOrEmpty(tableName))
				throw new ArgumentException("string.IsNullOrEmpty(tableName)", "tableName");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the name of the column(s) for the left foreign key.
		/// The left foreign key points to the parent entity of the navigation property specified in the HasMany call.
		/// </summary>
		/// <param name="keyColumnNames"> The foreign key column names. When using multiple foreign key properties, the properties must be specified in the same order that the the primary key properties were configured for the target entity type. </param>
		/// <returns> The same ManyToManyAssociationMappingConfiguration instance so that multiple calls can be chained. </returns>
		public ManyToManyAssociationMappingConfiguration MapLeftKey(params string[] keyColumnNames)
		{
			if (keyColumnNames == null)
				throw new ArgumentNullException("keyColumnNames");

			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures the name of the column(s) for the right foreign key.
		/// The right foreign key points to the parent entity of the the navigation property specified in the WithMany call.
		/// </summary>
		/// <param name="keyColumnNames"> The foreign key column names. When using multiple foreign key properties, the properties must be specified in the same order that the the primary key properties were configured for the target entity type. </param>
		/// <returns> The same ManyToManyAssociationMappingConfiguration instance so that multiple calls can be chained. </returns>
		public ManyToManyAssociationMappingConfiguration MapRightKey(params string[] keyColumnNames)
		{
			if (keyColumnNames == null)
				throw new ArgumentNullException("keyColumnNames");

			throw new NotImplementedException();
		}
	}
}
