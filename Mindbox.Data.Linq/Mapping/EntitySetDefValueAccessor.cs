using System.Collections.Generic;
using System.Reflection;

namespace System.Data.Linq.Mapping
{
	internal class EntitySetDefValueAccessor<T, V> : MetaAccessor<T, IEnumerable<V>> where V : class {
		MetaAccessor<T, EntitySet<V>> acc;
		internal EntitySetDefValueAccessor(MetaAccessor<T, EntitySet<V>> acc) {
			this.acc = acc;
		}
		public override IEnumerable<V> GetValue(T instance) {
			EntitySet<V> eset = this.acc.GetValue(instance);
			return eset.GetUnderlyingValues();
		}
		public override void SetValue(ref T instance, IEnumerable<V> value) {
			EntitySet<V> eset = this.acc.GetValue(instance);
			if (eset == null) {
				eset = new EntitySet<V>();
				this.acc.SetValue(ref instance, eset);
			}
			eset.Assign(value);
		}

		internal override MemberInfo Target => acc.Target;
	}
}