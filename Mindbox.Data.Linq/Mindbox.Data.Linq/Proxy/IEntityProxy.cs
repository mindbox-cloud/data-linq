using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Proxy
{
	public interface IEntityProxy
	{
		EntityRef<T> GetEntityRef<T>(MemberInfo getMethod)
			where T : class;

		void SetEntityRef<T>(MemberInfo getMethod, EntityRef<T> entityRef)
			where T : class;

		void HandleEntitySetChanging(object sender, EventArgs e);
	}
}
