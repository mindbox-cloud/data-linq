using System.Reflection;

namespace System.Data.Linq.Mapping
{
	internal class EntityRefDefValueAccessor<T, V> : MetaAccessor<T, V>
		where V : class
	{
		private readonly MetaAccessor<T, EntityRef<V>> acc;


		internal EntityRefDefValueAccessor(MetaAccessor<T, EntityRef<V>> acc)
		{
			this.acc = acc;
		}


		public override V GetValue(T instance)
		{
			var er = acc.GetValue(instance);
			return er.UnderlyingValue;
		}

		public override void SetValue(ref T instance, V value)
		{
			acc.SetValue(ref instance, new EntityRef<V>(value));
		}

		internal override MemberInfo Target => acc.Target;
	}
}