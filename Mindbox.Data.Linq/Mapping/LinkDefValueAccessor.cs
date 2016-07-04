using System.Reflection;

namespace System.Data.Linq.Mapping
{
	internal class LinkDefValueAccessor<T, V> : MetaAccessor<T, V> {
		MetaAccessor<T, Link<V>> acc;
		internal LinkDefValueAccessor(MetaAccessor<T, Link<V>> acc) {
			this.acc = acc;
		}
		public override V GetValue(T instance) {
			Link<V> link = this.acc.GetValue(instance);
			return link.UnderlyingValue;
		}
		public override void SetValue(ref T instance, V value) {
			this.acc.SetValue(ref instance, new Link<V>(value));
		}

		internal override MemberInfo Target => acc.Target;
	}
}