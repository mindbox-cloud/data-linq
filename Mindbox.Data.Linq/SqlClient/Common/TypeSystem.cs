using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Linq;

namespace System.Data.Linq.SqlClient 
{
    internal static class TypeSystem 
	{
		private static ILookup<string, MethodInfo> sequenceMethods;
		private static ILookup<string, MethodInfo> queryMethods;


        internal static bool IsSequenceType(Type seqType) 
		{
            return seqType != typeof(string)
                   && seqType != typeof(byte[])
                   && seqType != typeof(char[])
                   && FindIEnumerable(seqType) != null;
        }

        internal static bool HasIEnumerable(Type seqType) 
		{
            return FindIEnumerable(seqType) != null;
        }

		internal static Type GetFlatSequenceType(Type elementType)
		{
			return FindIEnumerable(elementType) ?? typeof(IEnumerable<>).MakeGenericType(elementType);
		}

		internal static Type GetSequenceType(Type elementType)
		{
			return typeof(IEnumerable<>).MakeGenericType(elementType);
		}

		internal static Type GetElementType(Type seqType)
		{
			var ienum = FindIEnumerable(seqType);
			return ienum == null ? seqType : ienum.GetGenericArguments()[0];
		}

		internal static bool IsNullableType(Type type)
		{
			return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		internal static bool IsNullAssignable(Type type)
		{
			return !type.IsValueType || IsNullableType(type);
		}

		internal static Type GetNonNullableType(Type type)
		{
			return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
		}

		internal static Type GetMemberType(MemberInfo mi)
		{
			var fi = mi as FieldInfo;
			if (fi != null) 
				return fi.FieldType;
			var pi = mi as PropertyInfo;
			if (pi != null) 
				return pi.PropertyType;
			var ei = mi as EventInfo;
			if (ei != null) 
				return ei.EventHandlerType;
			return null;
		}

		internal static IEnumerable<FieldInfo> GetAllFields(Type type, BindingFlags flags)
		{
			var seen = new Dictionary<MetaPosition, FieldInfo>();
			var currentType = type;
			do
			{
				foreach (var fi in currentType.GetFields(flags))
					if (fi.IsPrivate || type == currentType)
						seen[new MetaPosition(fi)] = fi;
				currentType = currentType.BaseType;
			} while (currentType != null);
			return seen.Values;
		}

		internal static IEnumerable<PropertyInfo> GetAllProperties(Type type, BindingFlags flags)
		{
			var seen = new Dictionary<MetaPosition, PropertyInfo>();
			var currentType = type;
			do
			{
				foreach (var pi in currentType.GetProperties(flags))
					if (type == currentType || IsPrivate(pi))
						seen[new MetaPosition(pi)] = pi;
				currentType = currentType.BaseType;
			} while (currentType != null);
			return seen.Values;
		}

		internal static MethodInfo FindSequenceMethod(string name, Type[] args, params Type[] typeArgs)
		{
			if (sequenceMethods == null)
				sequenceMethods = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public).ToLookup(m => m.Name);
			var mi = sequenceMethods[name].FirstOrDefault(m => ArgsMatchExact(m, args, typeArgs));
			if (mi == null)
				return null;
			return typeArgs == null ? mi : mi.MakeGenericMethod(typeArgs);
		}

		internal static MethodInfo FindSequenceMethod(string name, IEnumerable sequence)
		{
			return FindSequenceMethod(
				name, 
				new[]
				{
					sequence.GetType()
				}, 
				new[]
				{
					GetElementType(sequence.GetType())
				});
		}

		internal static MethodInfo FindQueryableMethod(string name, Type[] args, params Type[] typeArgs)
		{
			if (queryMethods == null)
				queryMethods = typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public).ToLookup(m => m.Name);
			var mi = queryMethods[name].FirstOrDefault(m => ArgsMatchExact(m, args, typeArgs));
			if (mi == null)
				throw Error.NoMethodInTypeMatchingArguments(typeof(Queryable));
			return typeArgs == null ? mi : mi.MakeGenericMethod(typeArgs);
		}

		internal static MethodInfo FindStaticMethod(Type type, string name, Type[] args, params Type[] typeArgs)
		{
			var mi = type
				.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				.FirstOrDefault(m => m.Name == name && ArgsMatchExact(m, args, typeArgs));
			if (mi == null)
				throw Error.NoMethodInTypeMatchingArguments(type);
			return typeArgs == null ? mi : mi.MakeGenericMethod(typeArgs);
		}

		/// <summary>
		/// Returns true if the type is one of the built in simple types.
		/// </summary>
		internal static bool IsSimpleType(Type type)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				type = type.GetGenericArguments()[0];

			if (type.IsEnum)
				return true;

			if (type == typeof(Guid))
				return true;

			var tc = Type.GetTypeCode(type);
			switch (tc)
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
				case TypeCode.Char:
				case TypeCode.String:
				case TypeCode.Boolean:
				case TypeCode.DateTime:
					return true;

				case TypeCode.Object:
					return (typeof(TimeSpan) == type) || (typeof(DateTimeOffset) == type);

				default:
					return false;
			}
		}


	    private static Type FindIEnumerable(Type seqType) 
		{
            if (seqType == null || seqType == typeof(string))
                return null;
            if (seqType.IsArray)
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
            if (seqType.IsGenericType) 
			{
                foreach (var arg in seqType.GetGenericArguments()) 
				{
                    var ienum = typeof(IEnumerable<>).MakeGenericType(arg);
					if (ienum.IsAssignableFrom(seqType))
						return ienum;
				}
            }
            var ifaces = seqType.GetInterfaces();
            if (ifaces != null && ifaces.Length > 0) 
			{
                foreach (var iface in ifaces) 
				{
                    var ienum = FindIEnumerable(iface);
                    if (ienum != null) 
						return ienum;
                }
            }
	        if (seqType.BaseType != null && seqType.BaseType != typeof(object))
		        return FindIEnumerable(seqType.BaseType);
	        return null;
        }

        private static bool IsPrivate(PropertyInfo pi) 
		{
            var mi = pi.GetGetMethod() ?? pi.GetSetMethod();
	        return (mi == null) || mi.IsPrivate;
		}

        private static bool ArgsMatchExact(MethodInfo m, Type[] argTypes, Type[] typeArgs) 
		{
            var mParams = m.GetParameters();
            if (mParams.Length != argTypes.Length)
                return false;
	        if (!m.IsGenericMethodDefinition && m.IsGenericMethod && m.ContainsGenericParameters)
		        m = m.GetGenericMethodDefinition();
	        if (m.IsGenericMethodDefinition) 
			{
                if (typeArgs == null || typeArgs.Length == 0)
                    return false;
                if (m.GetGenericArguments().Length != typeArgs.Length)
                    return false;
                m = m.MakeGenericMethod(typeArgs);
                mParams = m.GetParameters();
            }
            else if (typeArgs != null && typeArgs.Length > 0) 
			{
                return false;
            }
            for (int i = 0, n = argTypes.Length; i < n; i++) 
			{
                var parameterType = mParams[i].ParameterType;
                var argType = argTypes[i];
                if (!parameterType.IsAssignableFrom(argType))
                    return false;
            }
            return true;
        }
    }
}
