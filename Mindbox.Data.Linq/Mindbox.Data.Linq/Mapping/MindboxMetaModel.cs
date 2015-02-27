using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Reflection;
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

			if (!shouldInherit && (tableAttributes.Count > 1))
				throw new InvalidOperationException("!shouldInherit && (tableAttributes.Count > 1)");

			return tableAttributes;
		}

		internal override ColumnAttribute TryGetColumnAttribute(MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			var baseAttribute = base.TryGetColumnAttribute(member);

			var configuration = ((MindboxMappingSource)MappingSource).Configuration;
			var additionalAttribute = configuration.TryGetColumnAttribute(member);
			if ((baseAttribute != null) && (additionalAttribute != null))
				throw new InvalidOperationException("(baseAttribute != null) && (additionalAttribute != null)");

			return baseAttribute ?? additionalAttribute;
		}

		internal override AssociationAttribute TryGetAssociationAttribute(MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			var baseAttribute = base.TryGetAssociationAttribute(member);

			var configuration = ((MindboxMappingSource)MappingSource).Configuration;
			var additionalAttribute = configuration.TryGetAssociationAttribute(member);
			if ((baseAttribute != null) && (additionalAttribute != null))
				throw new InvalidOperationException("(baseAttribute != null) && (additionalAttribute != null)");

			return baseAttribute ?? additionalAttribute;
		}
	}
}
