using System.Linq.Expressions;

namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Allows configuration to be performed for a type in a model.
	/// </summary>
	/// <typeparam name="TStructuralType"> The type to be configured. </typeparam>
	public abstract class StructuralTypeConfiguration<TStructuralType>
		where TStructuralType : class
	{
		/// <summary>
		/// Configures a <see cref="T:System.struct" /> property that is defined on this type.
		/// </summary>
		/// <typeparam name="T"> The type of the property being configured. </typeparam>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public PrimitivePropertyConfiguration Property<T>(
			Expression<Func<TStructuralType, T>> propertyExpression)
			where T : struct
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.struct?" /> property that is defined on this type.
		/// </summary>
		/// <typeparam name="T"> The type of the property being configured. </typeparam>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public PrimitivePropertyConfiguration Property<T>(
			Expression<Func<TStructuralType, T?>> propertyExpression)
			where T : struct
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.string" /> property that is defined on this type.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public StringPropertyConfiguration Property(Expression<Func<TStructuralType, string>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.byte[]" /> property that is defined on this type.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public BinaryPropertyConfiguration Property(Expression<Func<TStructuralType, byte[]>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.decimal" /> property that is defined on this type.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public DecimalPropertyConfiguration Property(Expression<Func<TStructuralType, decimal>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.decimal?" /> property that is defined on this type.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public DecimalPropertyConfiguration Property(Expression<Func<TStructuralType, decimal?>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.DateTime" /> property that is defined on this type.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public DateTimePropertyConfiguration Property(Expression<Func<TStructuralType, DateTime>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.DateTime?" /> property that is defined on this type.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public DateTimePropertyConfiguration Property(Expression<Func<TStructuralType, DateTime?>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.DateTimeOffset" /> property that is defined on this type.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public DateTimePropertyConfiguration Property(
			Expression<Func<TStructuralType, DateTimeOffset>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.DateTimeOffset?" /> property that is defined on this type.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public DateTimePropertyConfiguration Property(
			Expression<Func<TStructuralType, DateTimeOffset?>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.TimeSpan" /> property that is defined on this type.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public DateTimePropertyConfiguration Property(Expression<Func<TStructuralType, TimeSpan>> propertyExpression)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures a <see cref="T:System.TimeSpan?" /> property that is defined on this type.
		/// </summary>
		/// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
		/// <returns> A configuration object that can be used to configure the property. </returns>
		public DateTimePropertyConfiguration Property(Expression<Func<TStructuralType, TimeSpan?>> propertyExpression)
		{
			throw new NotImplementedException();
		}
	}
}
