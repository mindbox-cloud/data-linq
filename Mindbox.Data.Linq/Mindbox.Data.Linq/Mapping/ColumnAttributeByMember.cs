using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Mapping
{
	internal class ColumnAttributeByMember
	{
		public MemberInfo Member { get; set; }
		public ColumnAttribute Attribute { get; set; }
	}
}
