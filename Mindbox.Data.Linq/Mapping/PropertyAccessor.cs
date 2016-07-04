using System.Reflection;
using System.Reflection.Emit;

namespace System.Data.Linq.Mapping
{
	internal static class PropertyAccessor 
	{
		internal static MetaAccessor Create(Type objectType, PropertyInfo pi, MetaAccessor storageAccessor) 
		{
			Delegate dset = null;
			Delegate drset = null;
			var dgetType = typeof(DGet<,>).MakeGenericType(objectType, pi.PropertyType);
			var getMethod = pi.GetGetMethod(true);

			var dget = Delegate.CreateDelegate(dgetType, getMethod, true);
			if (dget == null)
				throw Error.CouldNotCreateAccessorToProperty(objectType, pi.PropertyType, pi);

			if (pi.CanWrite) 
			{
				if (objectType.IsValueType)
				{
					var mset = new DynamicMethod(
						"xset_" + pi.Name,
						typeof(void),
						new[]
						{
							objectType.MakeByRefType(), 
							pi.PropertyType
						},
						true);
					var gen = mset.GetILGenerator();
					gen.Emit(OpCodes.Ldarg_0);
					if (!objectType.IsValueType)
						gen.Emit(OpCodes.Ldind_Ref);
					gen.Emit(OpCodes.Ldarg_1);
					gen.Emit(OpCodes.Call, pi.GetSetMethod(true));
					gen.Emit(OpCodes.Ret);
					drset = mset.CreateDelegate(typeof(DRSet<,>).MakeGenericType(objectType, pi.PropertyType));
				}
				else
				{
					dset = Delegate.CreateDelegate(
						typeof(DSet<,>).MakeGenericType(objectType, pi.PropertyType),
						pi.GetSetMethod(true),
						true);
				}
			}

			var saType = storageAccessor == null ? pi.PropertyType : storageAccessor.Type;
			return (MetaAccessor)Activator.CreateInstance(
				typeof(Accessor<,,>).MakeGenericType(objectType, pi.PropertyType, saType),
				BindingFlags.Instance | BindingFlags.NonPublic, 
				null, 
				new object[]
				{
					pi, 
					dget, 
					dset, 
					drset, 
					storageAccessor
				}, 
				null);
		}


		private class Accessor<T, V, V2> : MetaAccessor<T, V> 
			where V2 : V 
		{
			private readonly PropertyInfo pi;
			private readonly DGet<T, V> dget;
			private readonly DSet<T, V> dset;
			private readonly DRSet<T, V> drset;
			private readonly MetaAccessor<T, V2> storage;


			internal Accessor(PropertyInfo pi, DGet<T, V> dget, DSet<T, V> dset, DRSet<T, V> drset, MetaAccessor<T, V2> storage) 
			{
				this.pi = pi;
				this.dget = dget;
				this.dset = dset;
				this.drset = drset;
				this.storage = storage;
			}


			public override V GetValue(T instance) 
			{
				return dget(instance);
			}

			public override void SetValue(ref T instance, V value) 
			{
				if (dset != null)
					dset(instance, value);
				else if (drset != null)
					drset(ref instance, value);
				else if (storage != null)
					storage.SetValue(ref instance, (V2)value);
				else
					throw Error.UnableToAssignValueToReadonlyProperty(pi);
			}

			internal override MemberInfo Target => pi;
		}
	}
}