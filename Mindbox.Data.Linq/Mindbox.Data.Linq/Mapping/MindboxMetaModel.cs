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
	}
}
