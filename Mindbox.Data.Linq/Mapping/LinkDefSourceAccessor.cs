using System.Collections.Generic;
using System.Reflection;

namespace System.Data.Linq.Mapping
{
	internal class LinkDefSourceAccessor<T, V> : MetaAccessor<T, IEnumerable<V>> {
		MetaAccessor<T, Link<V>> acc;
		internal LinkDefSourceAccessor(MetaAccessor<T, Link<V>> acc) {
			this.acc = acc;
		}
		public override IEnumerable<V> GetValue(T instance) {
			Link<V> link = this.acc.GetValue(instance);
			return (IEnumerable<V>)link.Source;
		}
		public override void SetValue(ref T instance, IEnumerable<V> value) {
			Link<V> link = this.acc.GetValue(instance);
			if (link.HasAssignedValue || link.HasLoadedValue) {
				throw Error.LinkAlreadyLoaded();
			}
			this.acc.SetValue(ref instance, new Link<V>(value));
		}

		internal override MemberInfo Target => acc.Target;
	}
}