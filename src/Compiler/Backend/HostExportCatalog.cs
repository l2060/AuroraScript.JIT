using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AuroraScript.Compiler.Backend
{
    /// <summary>
    /// Immutable compiler view of source-generated host exports.
    /// </summary>
    internal sealed class HostExportCatalog
    {
        private readonly Dictionary<ExportKey, HostExportDescriptor> _exports;

        public HostExportCatalog(IReadOnlyList<Assembly> hostAssemblies)
        {
            ArgumentNullException.ThrowIfNull(hostAssemblies);
            _exports = new Dictionary<ExportKey, HostExportDescriptor>();
            AddAssembly(typeof(AuroraEngine).Assembly);
            for (var i = 0; i < hostAssemblies.Count; i++)
            {
                var assembly = hostAssemblies[i] ??
                    throw new ArgumentException(
                        "Host export assemblies cannot contain null.",
                        nameof(hostAssemblies));
                if (assembly != typeof(AuroraEngine).Assembly)
                {
                    AddAssembly(assembly);
                }
            }
        }

        public bool TryGetGlobal(
            string globalName,
            string memberName,
            out HostExportDescriptor descriptor)
        {
            return _exports.TryGetValue(
                new ExportKey(globalName, memberName),
                out descriptor);
        }

        private void AddAssembly(Assembly assembly)
        {
            var attributes = assembly
                .GetCustomAttributes<AuroraGeneratedExportAttribute>();
            foreach (var attribute in attributes)
            {
                var parameterTypes = new Type[attribute.ParameterKinds.Length];
                for (var i = 0; i < parameterTypes.Length; i++)
                {
                    parameterTypes[i] = GetClrType(attribute.ParameterKinds[i]);
                }

                var method = attribute.DeclaringType.GetMethod(
                    attribute.MethodName,
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    parameterTypes,
                    modifiers: null);
                if (method == null ||
                    method.ReturnType != GetClrType(attribute.ReturnKind))
                {
                    throw new InvalidOperationException(
                        $"Generated Aurora export '{attribute.GlobalName}.{attribute.MemberName}' " +
                        $"does not resolve to a public static Core method.");
                }

                var descriptor = new HostExportDescriptor(
                    method,
                    attribute.ReturnKind,
                    (AuroraExportValueKind[])attribute.ParameterKinds.Clone());
                if (!_exports.TryAdd(
                        new ExportKey(attribute.GlobalName, attribute.MemberName),
                        descriptor))
                {
                    throw new InvalidOperationException(
                        $"Duplicate generated Aurora export " +
                        $"'{attribute.GlobalName}.{attribute.MemberName}'.");
                }
            }
        }

        private static Type GetClrType(AuroraExportValueKind kind)
        {
            return kind switch
            {
                AuroraExportValueKind.Void => typeof(void),
                AuroraExportValueKind.Number => typeof(double),
                AuroraExportValueKind.Int32 => typeof(int),
                AuroraExportValueKind.Boolean => typeof(bool),
                AuroraExportValueKind.String => typeof(string),
                AuroraExportValueKind.Object => typeof(ScriptObject),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        private readonly record struct ExportKey(string GlobalName, string MemberName);
    }

    internal sealed class HostExportDescriptor
    {
        public HostExportDescriptor(
            MethodInfo method,
            AuroraExportValueKind returnKind,
            AuroraExportValueKind[] parameterKinds)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            ReturnKind = returnKind;
            ParameterKinds = parameterKinds ?? throw new ArgumentNullException(nameof(parameterKinds));
        }

        public MethodInfo Method { get; }
        public AuroraExportValueKind ReturnKind { get; }
        public AuroraExportValueKind[] ParameterKinds { get; }
    }
}
