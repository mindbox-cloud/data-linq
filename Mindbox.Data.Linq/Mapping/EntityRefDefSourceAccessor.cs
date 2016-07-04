using System.Collections.Generic;
using System.Reflection;

namespace System.Data.Linq.Mapping
{
	internal class EntityRefDefSourceAccessor<T, V> : MetaAccessor<T, IEnumerable<V>> 
		where V : class 
	{
		private readonly MetaAccessor<T, EntityRef<V>> acc;


		internal EntityRefDefSourceAccessor(MetaAccessor<T, EntityRef<V>> acc) 
		{
			this.acc = acc;
		}


		public override IEnumerable<V> GetValue(T instance) 
		{
			var er = acc.GetValue(instance);
			return er.Source;
		}

		public override void SetValue(ref T instance, IEnumerable<V> value) 
		{
			var er = acc.GetValue(instance);
			if (er.HasAssignedValue || er.HasLoadedValue)
				throw Error.EntityRefAlreadyLoaded();
			acc.SetValue(ref instance, new EntityRef<V>(value));
		}

		internal override MemberInfo Target => acc.Target;
	}
}