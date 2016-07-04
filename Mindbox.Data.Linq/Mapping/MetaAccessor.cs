using System.Reflection;

namespace System.Data.Linq.Mapping
{
	/// <summary>
	/// A strongly-typed MetaAccessor. Used for reading from and writing to
	/// CLR objects.
	/// </summary>
	/// <typeparam name="T">The type of the object</typeparam>
	/// <typeparam name="V">The type of the accessed member</typeparam>
	public abstract class MetaAccessor<TEntity, TMember> : MetaAccessor
	{
		/// <summary>
		/// The underlying CLR type.
		/// </summary>
		public override Type Type
		{
			get { return typeof(TMember); }
		}


		/// <summary>
		/// Set the boxed value on an instance.
		/// </summary>
		public override void SetBoxedValue(ref object instance, object value)
		{
			if (value == null && !typeof(TMember).IsNullable())
				throw Error.CannotAssignNull(Target);

			var tInst = (TEntity)instance;
			SetValue(ref tInst, (TMember)value);
			instance = tInst;
		}

		/// <summary>
		/// Retrieve the boxed value.
		/// </summary>
		public override object GetBoxedValue(object instance)
		{
			return GetValue((TEntity)instance);
		}

		/// <summary>
		/// Gets the strongly-typed value.
		/// </summary>
		public abstract TMember GetValue(TEntity instance);

		/// <summary>
		/// Sets the strongly-typed value
		/// </summary>
		public abstract void SetValue(ref TEntity instance, TMember value);
	}

	/// <summary>
	/// A MetaAccessor
	/// </summary>
	public abstract class MetaAccessor
	{
		/// <summary>
		/// The type of the member accessed by this accessor.
		/// </summary>
		public abstract Type Type { get; }


		/// <summary>
		/// Gets the value as an object.
		/// </summary>
		/// <param name="instance">The instance to get the value from.</param>
		/// <returns>Value.</returns>
		public abstract object GetBoxedValue(object instance);

		/// <summary>
		/// Sets the value as an object.
		/// </summary>
		/// <param name="instance">The instance to set the value into.</param>
		/// <param name="value">The value to set.</param>
		public abstract void SetBoxedValue(ref object instance, object value);

		/// <summary>
		/// True if the instance has a loaded or assigned value.
		/// </summary>
		public virtual bool HasValue(object instance)
		{
			return true;
		}

		/// <summary>
		/// True if the instance has an assigned value.
		/// </summary>
		public virtual bool HasAssignedValue(object instance)
		{
			return true;
		}

		/// <summary>
		/// True if the instance has a value loaded from a deferred source.
		/// </summary>
		public virtual bool HasLoadedValue(object instance)
		{
			return false;
		}

		internal abstract MemberInfo Target { get; }
	}
}