using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Mapping
{
	/// <summary>
	/// A mapping source that uses attributes on the context to create the mapping model.
	/// </summary>
	public class MindboxMappingSource : AttributeMappingSource
	{
		[Obsolete]
		public MindboxMappingSource(MindboxMappingConfiguration configuration, bool isDatabaseMigrated)
			: this(configuration, isDatabaseMigrated, new Dictionary<string, bool>())
		{
		}

		public MindboxMappingSource(
			MindboxMappingConfiguration configuration,
			bool isDatabaseMigrated,
			Dictionary<string, bool> databaseMigrationStatus)
		{
			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			if (!configuration.IsFrozen)
				configuration.Freeze();

			Configuration = configuration;
			IsDatabaseMigrated = isDatabaseMigrated;
			DatabaseMigrationStatus = databaseMigrationStatus;
		}

		internal MindboxMappingConfiguration Configuration { get; private set; }
		public bool IsDatabaseMigrated { get; }
		public Dictionary<string, bool> DatabaseMigrationStatus { get; }

		protected override MetaModel CreateModel(Type dataContextType)
		{
			if (dataContextType == null)
				throw new ArgumentNullException("dataContextType");

			return new MindboxMetaModel(this, dataContextType);
		}
	}
}