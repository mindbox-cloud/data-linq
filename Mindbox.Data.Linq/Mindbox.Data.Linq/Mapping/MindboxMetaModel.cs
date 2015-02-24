using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Text;
using System.Linq;

namespace Mindbox.Data.Linq.Mapping 
{
	internal class MindboxMetaModel : AttributedMetaModel 
	{
        internal MindboxMetaModel(MappingSource mappingSource, Type contextType) 
			: base(mappingSource, contextType)
		{
        }


		internal override AttributedRootType CreateRootType(AttributedMetaTable table, Type type)
		{
			return new MindboxRootType(this, table, type);
		}

		internal override IReadOnlyCollection<TableAttribute> GetTableAttributes(Type type, bool shouldInherit)
		{
			var tableAttributes = base.GetTableAttributes(type, shouldInherit).ToList();

			var configuration = ((MindboxMappingSource)MappingSource).Configuration;
			if (tableAttributes.Any())
				configuration.OnEntityFrameworkIncompatibility(EntityFrameworkIncompatibility.TableAttribute);

			var additionalTableAttribute = configuration.TryGetTableAttribute(type);
			if (additionalTableAttribute != null)
				tableAttributes.Add(additionalTableAttribute);

			for (var currentType = type; shouldInherit && (currentType.BaseType != null); currentType = currentType.BaseType)
			{
				var currentTypeAdditionalTableAttribute = configuration.TryGetTableAttribute(currentType);
				if (currentTypeAdditionalTableAttribute != null)
					tableAttributes.Add(currentTypeAdditionalTableAttribute);
			}

			return tableAttributes;
		}
	}
}
