using AuroraScript.Runtime.Types;
using System;
using System.Reflection;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Shared shape checks for host <see cref="NativeTypeAttribute"/> types and
    /// <see cref="NativePackageAttribute"/> types. Compiler options and package
    /// definitions must agree on what is selectable.
    /// </summary>
    internal static class NativeExportType
    {
        public static void RequireHostNativeType(Type type, string paramName)
        {
            ArgumentNullException.ThrowIfNull(type, paramName);
            RequireGeneratedClass(type, paramName);
            RequireNativeTypeAttribute(type, paramName);
            if (type.GetCustomAttribute<NativePackageAttribute>() != null)
            {
                throw new ArgumentException(
                    $"Native package '{type.FullName}' cannot be listed in NativeTypes. Enable it with WithPackages.",
                    paramName);
            }

            if (type.IsDefined(typeof(NativeReceiverAttribute), inherit: false))
            {
                throw new ArgumentException(
                    $"Native value receiver '{type.FullName}' cannot replace an engine-owned immutable prototype.",
                    paramName);
            }

            if (type.Assembly == typeof(AuroraEngine).Assembly)
            {
                throw new ArgumentException(
                    $"Engine infrastructure type '{type.FullName}' is always available and cannot be listed in NativeTypes.",
                    paramName);
            }

            RequireMethod(
                type,
                "Register",
                paramName,
                typeof(ScriptObject),
                typeof(bool),
                typeof(bool));
        }

        public static NativeTypeAttribute RequireNativePackageType(Type type, string paramName)
        {
            ArgumentNullException.ThrowIfNull(type, paramName);
            RequireGeneratedClass(type, paramName);
            var native = RequireNativeTypeAttribute(type, paramName);
            if (type.GetCustomAttribute<NativePackageAttribute>() == null)
            {
                throw new ArgumentException(
                    $"Type '{type.FullName}' is not marked with NativePackageAttribute.",
                    paramName);
            }

            if (type.IsDefined(typeof(NativeReceiverAttribute), inherit: false))
            {
                throw new ArgumentException(
                    $"Native package '{type.FullName}' cannot declare a native receiver.",
                    paramName);
            }

            RequireMethod(type, "RegisterPackage", paramName, typeof(ScriptObject));
            return native;
        }

        public static bool IsSelectableHostNativeType(Type type)
        {
            return type != null &&
                type.Assembly != typeof(AuroraEngine).Assembly &&
                IsGeneratedClass(type) &&
                type.IsDefined(typeof(NativeTypeAttribute), inherit: false) &&
                !type.IsDefined(typeof(NativeReceiverAttribute), inherit: false) &&
                type.GetCustomAttribute<NativePackageAttribute>() == null &&
                FindMethod(type, "Register", typeof(ScriptObject), typeof(bool), typeof(bool)) !=
                    null;
        }

        private static NativeTypeAttribute RequireNativeTypeAttribute(Type type, string paramName)
        {
            return type.GetCustomAttribute<NativeTypeAttribute>() ??
                throw new ArgumentException(
                    $"Type '{type.FullName}' is not marked with NativeTypeAttribute.",
                    paramName);
        }

        private static void RequireGeneratedClass(Type type, string paramName)
        {
            if (IsGeneratedClass(type))
            {
                return;
            }

            throw new ArgumentException(
                $"Type '{type.FullName}' must be a sealed, non-generic, top-level ScriptObject class.",
                paramName);
        }

        private static bool IsGeneratedClass(Type type)
        {
            return type.IsClass &&
                type.IsSealed &&
                !type.IsAbstract &&
                !type.IsGenericType &&
                !type.IsNested &&
                typeof(ScriptObject).IsAssignableFrom(type);
        }

        private static void RequireMethod(
            Type type,
            string name,
            string paramName,
            params Type[] parameterTypes)
        {
            if (FindMethod(type, name, parameterTypes) != null)
            {
                return;
            }

            throw new InvalidOperationException(
                name == "RegisterPackage"
                    ? $"Native package '{type.FullName}' does not expose its generated RegisterPackage method."
                    : $"Native type '{type.FullName}' does not expose its generated Register method.");
        }

        private static MethodInfo FindMethod(Type type, string name, params Type[] parameterTypes)
        {
            return type.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: parameterTypes,
                modifiers: null);
        }
    }
}
