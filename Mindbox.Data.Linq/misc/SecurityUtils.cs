using System;
using System.Reflection;

namespace System.Data.Linq
{
    // Code Access Security (ReflectionPermission) is not supported on .NET 5+.
    // This simplified version removes CAS checks which were no-ops on modern .NET.
    internal static class SecurityUtils
    {
        internal static object SecureCreateInstance(Type type)
        {
            return SecureCreateInstance(type, null, false);
        }

        internal static object SecureCreateInstance(Type type, object[] args, bool allowNonPublic)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;
            if (allowNonPublic)
                flags |= BindingFlags.NonPublic;

            return Activator.CreateInstance(type, flags, null, args, null);
        }

        internal static object SecureCreateInstance(Type type, object[] args)
        {
            return SecureCreateInstance(type, args, false);
        }

        internal static object SecureConstructorInvoke(Type type, Type[] argTypes, object[] args, bool allowNonPublic)
        {
            return SecureConstructorInvoke(type, argTypes, args, allowNonPublic, BindingFlags.Default);
        }

        internal static object SecureConstructorInvoke(Type type, Type[] argTypes, object[] args,
                                                       bool allowNonPublic, BindingFlags extraFlags)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | extraFlags;
            if (!allowNonPublic)
                flags &= ~BindingFlags.NonPublic;

            ConstructorInfo ctor = type.GetConstructor(flags, null, argTypes, null);
            if (ctor != null)
                return ctor.Invoke(args);

            return null;
        }

        internal static object FieldInfoGetValue(FieldInfo field, object target)
        {
            if (field.DeclaringType == null)
                throw new NotImplementedException("Global fields are not supported.");

            return field.GetValue(target);
        }

        internal static object MethodInfoInvoke(MethodInfo method, object target, object[] args)
        {
            if (method.DeclaringType == null)
                throw new NotImplementedException("Global methods are not supported.");

            return method.Invoke(target, args);
        }

        internal static object ConstructorInfoInvoke(ConstructorInfo ctor, object[] args)
        {
            return ctor.Invoke(args);
        }

        internal static object ArrayCreateInstance(Type type, int length)
        {
            return Array.CreateInstance(type, length);
        }
    }
}
