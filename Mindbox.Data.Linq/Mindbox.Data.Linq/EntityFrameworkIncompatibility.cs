using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq
{
	public enum EntityFrameworkIncompatibility
	{
		TableAttribute = 1,
		ColumnAttribute = 2,
		AssociationAttribute = 3
	}
}
