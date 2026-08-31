using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AuroraScript.Compiler.Backend
{
    /// <summary>
    /// Immutable compiler view of source-generated host exports.
    /// </summary>
    internal sealed partial class HostExportCatalog
    {
        private readonly Dictionary<ExportKey, HostExportDescriptor> _exports;
        private readonly Dictionary<ExportKey, FieldInfo> _constants;

        public HostExportCatalog(IReadOnlyList<Type> nativeTypes)
        {
            ArgumentNullException.ThrowIfNull(nativeTypes);
            _exports = new Dictionary<ExportKey, HostExportDescriptor>();
            _constants = new Dictionary<ExportKey, FieldInfo>();
            _nativeObjects = new Dictionary<string, HostNativeObjectDescriptor>(StringComparer.Ordinal);
            _nativeObjectsByClrType = new Dictionary<Type, HostNativeObjectDescriptor>();
            AddAssembly(typeof(AuroraEngine).Assembly);
            for (var i = 0; i < nativeTypes.Count; i++)
            {
                var nativeType = nativeTypes[i] ??
                    throw new ArgumentException(
                        "Native types cannot contain null.",
                        nameof(nativeTypes));
                if (nativeType.Assembly != typeof(AuroraEngine).Assembly)
                {
                    AddAssembly(nativeType.Assembly, nativeType);
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

        public bool TryGetConstant(
            string globalName,
            string memberName,
            out FieldInfo field)
        {
            return _constants.TryGetValue(
                new ExportKey(globalName, memberName),
                out field);
        }

        private void AddAssembly(Assembly assembly, Type selectedType = null)
        {
            AddNativeObjects(assembly, selectedType);
            foreach (var attribute in assembly.GetCustomAttributes<AuroraGeneratedConstantAttribute>())
            {
                if (selectedType != null && attribute.DeclaringType != selectedType)
                {
                    continue;
                }

                var field = attribute.DeclaringType.GetField(
                    attribute.FieldName,
                    BindingFlags.Public | BindingFlags.Static);
                if (field == null || field.FieldType != typeof(double))
                {
                    throw new InvalidOperationException(
                        $"Generated Aurora constant '{attribute.GlobalName}.{attribute.MemberName}' " +
                        $"does not resolve to a public static double field.");
                }

                var key = new ExportKey(
                    attribute.GlobalName,
                    attribute.MemberName);
                if (_exports.ContainsKey(key) ||
                    !_constants.TryAdd(key, field))
                {
                    throw new InvalidOperationException(
                        $"Duplicate generated Aurora constant " +
                        $"'{attribute.GlobalName}.{attribute.MemberName}'.");
                }
            }

            var attributes = assembly
                .GetCustomAttributes<AuroraGeneratedExportAttribute>();
            foreach (var attribute in attributes)
            {
                if (selectedType != null && attribute.DeclaringType != selectedType)
                {
                    continue;
                }

                var method = ResolveCoreMethod(attribute);
                if (method == null)
                {
                    throw new InvalidOperationException(
                        $"Generated Aurora export '{attribute.GlobalName}.{attribute.MemberName}' " +
                        $"does not resolve to a public static Core method.");
                }

                var descriptor = new HostExportDescriptor(
                    method,
                    attribute.ReturnKind,
                    (AuroraExportValueKind[])attribute.ParameterKinds.Clone(),
                    attribute.TakesContext,
                    attribute.TakesThisObject);
                var key = new ExportKey(
                    attribute.GlobalName,
                    attribute.MemberName);
                if (_constants.ContainsKey(key) ||
                    !_exports.TryAdd(key, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Duplicate generated Aurora export " +
                        $"'{attribute.GlobalName}.{attribute.MemberName}'.");
                }
            }
        }

        private static MethodInfo ResolveCoreMethod(
            AuroraGeneratedExportAttribute attribute)
        {
            var methods = attribute.DeclaringType.GetMethods(
                BindingFlags.Public | BindingFlags.Static);
            foreach (var method in methods)
            {
                if (!StringComparer.Ordinal.Equals(method.Name, attribute.MethodName) ||
                    !MatchesClrType(attribute.ReturnKind, method.ReturnType))
                {
                    continue;
                }

                var export = method.GetCustomAttribute<AuroraExportAttribute>();
                if (export == null ||
                    !StringComparer.Ordinal.Equals(
                        GetScriptName(export.ScriptName, method.Name),
                        attribute.MemberName))
                {
                    continue;
                }

                var parameters = method.GetParameters();
                var expectedCount = attribute.ParameterKinds.Length +
                    (attribute.TakesContext ? 1 : 0) +
                    (attribute.TakesThisObject ? 1 : 0);
                if (parameters.Length != expectedCount)
                {
                    continue;
                }

                var index = 0;
                if (attribute.TakesContext &&
                    parameters[index++].ParameterType != typeof(ScriptContext))
                {
                    continue;
                }
                if (attribute.TakesThisObject &&
                    parameters[index++].ParameterType != typeof(ScriptObject))
                {
                    continue;
                }

                var matches = true;
                for (var i = 0; i < attribute.ParameterKinds.Length; i++)
                {
                    if (!MatchesClrType(
                            attribute.ParameterKinds[i],
                            parameters[index + i].ParameterType))
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                {
                    return method;
                }
            }

            return null;
        }

        private static string GetScriptName(string scriptName, string methodName)
        {
            if (!string.IsNullOrWhiteSpace(scriptName))
            {
                return scriptName;
            }
            if (methodName.EndsWith("Core", StringComparison.Ordinal) &&
                methodName.Length > 4)
            {
                methodName = methodName.Substring(0, methodName.Length - 4);
            }
            return methodName.Length == 0
                ? methodName
                : char.ToLowerInvariant(methodName[0]) + methodName.Substring(1);
        }

        private static bool MatchesClrType(
            AuroraExportValueKind kind,
            Type type)
        {
            return kind == AuroraExportValueKind.Object
                ? typeof(ScriptObject).IsAssignableFrom(type)
                : type == GetClrType(kind);
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
                AuroraExportValueKind.Datum => typeof(ScriptDatum),
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
            AuroraExportValueKind[] parameterKinds,
            bool takesContext = false,
            bool takesThisObject = false)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            ReturnKind = returnKind;
            ParameterKinds = parameterKinds ?? throw new ArgumentNullException(nameof(parameterKinds));
            TakesContext = takesContext;
            TakesThisObject = takesThisObject;
            RequiredScriptParameterCount = CountRequiredScriptParameters(
                method,
                takesContext,
                takesThisObject);
        }

        public MethodInfo Method { get; }
        public AuroraExportValueKind ReturnKind { get; }
        public AuroraExportValueKind[] ParameterKinds { get; }
        public bool TakesContext { get; }
        public bool TakesThisObject { get; }
        public int RequiredScriptParameterCount { get; }

        public Type GetScriptParameterType(int index)
        {
            var prefix = (TakesContext ? 1 : 0) +
                (TakesThisObject ? 1 : 0);
            return Method.GetParameters()[prefix + index].ParameterType;
        }

        private static int CountRequiredScriptParameters(
            MethodInfo method,
            bool takesContext,
            bool takesThisObject)
        {
            var parameters = method.GetParameters();
            var start = 0;
            if (takesContext)
            {
                start++;
            }
            if (takesThisObject)
            {
                start++;
            }

            var required = 0;
            for (var i = start; i < parameters.Length; i++)
            {
                if (parameters[i].HasDefaultValue)
                {
                    break;
                }

                required++;
            }

            return required;
        }
    }
}
