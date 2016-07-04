using System.Reflection;

namespace System.Data.Linq.Mapping
{
	internal class EntityRefValueAccessor<T, V> : MetaAccessor<T, V> 
		where V : class 
	{
		private readonly MetaAccessor<T, EntityRef<V>> acc;


		internal EntityRefValueAccessor(MetaAccessor<T, EntityRef<V>> acc) 
		{
			this.acc = acc;
		}


		public override V GetValue(T instance) 
		{
			var er = acc.GetValue(instance);
			return er.Entity;
		}

		public override void SetValue(ref T instance, V value) 
		{
			acc.SetValue(ref instance, new EntityRef<V>(value));
		}

		public override bool HasValue(object instance) 
		{
			var er = acc.GetValue((T)instance);
			return er.HasValue;
		}

		public override bool HasAssignedValue(object instance) 
		{
			var er = acc.GetValue((T)instance);
			return er.HasAssignedValue;
		}

		public override bool HasLoadedValue(object instance) 
		{
			var er = acc.GetValue((T)instance);
			return er.HasLoadedValue;
		}

		internal override MemberInfo Target => acc.Target;
	}
}