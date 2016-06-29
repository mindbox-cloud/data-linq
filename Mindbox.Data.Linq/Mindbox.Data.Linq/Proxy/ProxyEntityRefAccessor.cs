using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Proxy
{
	internal class ProxyEntityRefAccessor<TEntity, TMember> : MetaAccessor<TEntity, EntityRef<TMember>>
		where TEntity : class
		where TMember : class
	{
		private readonly PropertyInfo property;


		public ProxyEntityRefAccessor(PropertyInfo property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			this.property = property;
		}


		public override EntityRef<TMember> GetValue(TEntity instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			var proxy = instance as IEntityProxy;
			return proxy == null ?
				new EntityRef<TMember>((TMember)property.GetValue(instance)) :
				((IEntityProxy)instance).GetEntityRef<TMember>(property.GetGetMethod());
		}

		public override void SetValue(ref TEntity instance, EntityRef<TMember> value)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			((IEntityProxy)instance).SetEntityRef(property.GetGetMethod(), value);
		}

		public override MemberInfo Target => property;
	}
}
