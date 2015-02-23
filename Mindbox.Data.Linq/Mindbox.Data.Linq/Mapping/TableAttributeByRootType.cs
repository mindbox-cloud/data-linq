using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Mapping
{
	internal class TableAttributeByRootType
	{
		public Type RootType { get; set; }
		public TableAttribute Attribute { get; set; }
	}
}
