using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Mapping
{
	internal class AssociationAttributeByMember
	{
		public MemberInfo Member { get; set; }
		public AssociationAttribute Attribute { get; set; }
	}
}
