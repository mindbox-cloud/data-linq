using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Mapping
{
	internal class MindboxRootType : AttributedRootType
	{
		internal MindboxRootType(AttributedMetaModel model, AttributedMetaTable table, Type type) 
			: base(model, table, type)
		{
		}


		protected override ICollection<InheritanceMappingAttribute> GetInheritanceMappingAttributes(
			Type type,
			AttributedMetaModel model)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (model == null)
				throw new ArgumentNullException("model");

			var mindboxMappingSource = (MindboxMappingSource)model.MappingSource;
			return base
				.GetInheritanceMappingAttributes(type, model)
				.Concat(mindboxMappingSource.Configuration.GetAdditionalInheritanceAttributes(type))
				.ToList();
		}
	}
}
