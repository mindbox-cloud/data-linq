using System;
using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Mapping
{
	/// <summary>
	/// A mapping source that uses attributes on the context to create the mapping model.
	/// </summary>
	public class MindboxMappingSource : AttributeMappingSource 
	{
		public MindboxMappingSource(MindboxMappingConfiguration configuration, bool isDatabaseMigrated)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration");

			if (!configuration.IsFrozen)
				configuration.Freeze();
			Configuration = configuration;
			IsDatabaseMigrated = isDatabaseMigrated;
		}


		internal MindboxMappingConfiguration Configuration { get; private set; }
		public bool IsDatabaseMigrated { get; }


		protected override MetaModel CreateModel(Type dataContextType)
		{
			if (dataContextType == null)
				throw new ArgumentNullException("dataContextType");

			return new MindboxMetaModel(this, dataContextType);
		}
	}
}