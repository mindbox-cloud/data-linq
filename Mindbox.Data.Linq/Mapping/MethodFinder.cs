using System.Reflection;

namespace System.Data.Linq.Mapping
{
	internal static class MethodFinder {
		internal static MethodInfo FindMethod(Type type, string name, BindingFlags flags, Type[] argTypes) {
			return FindMethod(type, name, flags, argTypes, true);
		}

		internal static MethodInfo FindMethod(Type type, string name, BindingFlags flags, Type[] argTypes, bool allowInherit) {
			for (; type != typeof(object); type = type.BaseType) {
				MethodInfo mi = type.GetMethod(name, flags | BindingFlags.DeclaredOnly, null, argTypes, null);
				if (mi != null || !allowInherit) {
					return mi;
				}
			}
			return null;
		}
	}
}