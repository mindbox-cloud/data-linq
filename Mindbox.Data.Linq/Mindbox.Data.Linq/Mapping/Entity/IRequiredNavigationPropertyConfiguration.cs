using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Mapping.Entity
{
	internal interface IRequiredNavigationPropertyConfiguration
	{
		ColumnAttributeByMember TryGetColumnAttribute(DbModelBuilder dbModelBuilder);

		AssociationAttributeByMember GetAssociationAttribute(DbModelBuilder dbModelBuilder);
	}
}
