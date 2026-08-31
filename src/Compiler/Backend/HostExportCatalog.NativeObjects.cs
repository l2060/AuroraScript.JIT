using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AuroraScript.Compiler.Backend
{
    internal sealed partial class HostExportCatalog
    {
        private readonly Dictionary<string, HostNativeObjectDescriptor> _nativeObjects;
        private readonly Dictionary<Type, HostNativeObjectDescriptor> _nativeObjectsByClrType;

        /// <summary>
        /// Resolves an instantiable native object type by its script constructor name.
        /// </summary>
        public bool TryGetNativeObject(
            string typeName,
            out HostNativeObjectDescriptor descriptor)
        {
            if (typeName == null)
            {
                descriptor = null;
                return false;
            }
            return _nativeObjects.TryGetValue(typeName, out descriptor);
        }

        /// <summary>
        /// Resolves the native object type a proven CLR reference points at, which lets
        /// the compiler keep chaining direct member access across method returns.
        /// </summary>
        public bool TryGetNativeObject(
            Type clrType,
            out HostNativeObjectDescriptor descriptor)
        {
            if (clrType == null)
            {
                descriptor = null;
                return false;
            }
            return _nativeObjectsByClrType.TryGetValue(clrType, out descriptor);
        }

        private void AddNativeObjects(Assembly assembly, Type selectedType)
        {
            var pending = new List<HostNativeObjectDescriptor>();
            foreach (var attribute in
                assembly.GetCustomAttributes<AuroraGeneratedNativeObjectAttribute>())
            {
                if (selectedType != null && attribute.ObjectType != selectedType)
                {
                    continue;
                }

                if (!typeof(ScriptObject).IsAssignableFrom(attribute.ObjectType) ||
                    !typeof(IAuroraNativeInstance).IsAssignableFrom(attribute.ObjectType))
                {
                    throw new InvalidOperationException(
                        $"Generated Aurora native object '{attribute.TypeName}' does not " +
                        $"resolve to a generated ScriptObject native instance type.");
                }

                var constructor = attribute.Constructible
                    ? ResolveConstructor(attribute)
                    : null;
                if (attribute.Constructible && constructor == null)
                {
                    throw new InvalidOperationException(
                        $"Generated Aurora native object '{attribute.TypeName}' does not " +
                        $"resolve to a public constructor.");
                }

                var descriptor = new HostNativeObjectDescriptor(
                    attribute.TypeName,
                    attribute.ObjectType,
                    constructor,
                    (AuroraExportValueKind[])attribute.ConstructorParameterKinds.Clone());
                if (!_nativeObjects.TryAdd(attribute.TypeName, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Duplicate generated Aurora native object '{attribute.TypeName}'.");
                }
                _nativeObjectsByClrType[attribute.ObjectType] = descriptor;
                pending.Add(descriptor);
            }

            if (pending.Count == 0)
            {
                return;
            }

            foreach (var attribute in
                assembly.GetCustomAttributes<AuroraGeneratedNativeFieldAttribute>())
            {
                if (selectedType != null && attribute.DeclaringType != selectedType)
                {
                    continue;
                }

                if (!_nativeObjects.TryGetValue(attribute.TypeName, out var owner) ||
                    owner.ClrType != attribute.DeclaringType)
                {
                    continue;
                }

                var field = attribute.DeclaringType.GetField(
                    attribute.FieldName,
                    BindingFlags.Public | BindingFlags.Instance);
                if (field == null ||
                    !MatchesClrType(attribute.Kind, field.FieldType))
                {
                    throw new InvalidOperationException(
                        $"Generated Aurora native field '{attribute.TypeName}.{attribute.MemberName}' " +
                        $"does not resolve to a public instance field.");
                }

                owner.AddField(new HostNativeFieldDescriptor(
                    attribute.MemberName,
                    field,
                    attribute.Kind,
                    attribute.IsReadOnly || field.IsInitOnly));
            }

            foreach (var attribute in
                assembly.GetCustomAttributes<AuroraGeneratedNativeMethodAttribute>())
            {
                if (selectedType != null && attribute.DeclaringType != selectedType)
                {
                    continue;
                }

                if (!_nativeObjects.TryGetValue(attribute.TypeName, out var owner) ||
                    owner.ClrType != attribute.DeclaringType)
                {
                    continue;
                }

                var method = ResolveInstanceMethod(attribute);
                if (method == null)
                {
                    throw new InvalidOperationException(
                        $"Generated Aurora native method '{attribute.TypeName}.{attribute.MemberName}' " +
                        $"does not resolve to a public instance Core method.");
                }

                owner.AddMethod(new HostNativeMethodDescriptor(
                    attribute.MemberName,
                    method,
                    attribute.ReturnKind,
                    (AuroraExportValueKind[])attribute.ParameterKinds.Clone(),
                    attribute.TakesContext));
            }
        }

        private static ConstructorInfo ResolveConstructor(
            AuroraGeneratedNativeObjectAttribute attribute)
        {
            foreach (var constructor in attribute.ObjectType.GetConstructors(
                BindingFlags.Public | BindingFlags.Instance))
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length != attribute.ConstructorParameterKinds.Length)
                {
                    continue;
                }

                var matches = true;
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (!MatchesClrType(
                            attribute.ConstructorParameterKinds[i],
                            parameters[i].ParameterType))
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                {
                    return constructor;
                }
            }

            return null;
        }

        private static MethodInfo ResolveInstanceMethod(
            AuroraGeneratedNativeMethodAttribute attribute)
        {
            foreach (var method in attribute.DeclaringType.GetMethods(
                BindingFlags.Public | BindingFlags.Instance))
            {
                if (!StringComparer.Ordinal.Equals(method.Name, attribute.MethodName) ||
                    !MatchesClrType(attribute.ReturnKind, method.ReturnType) ||
                    method.GetCustomAttribute<AuroraExportAttribute>() == null)
                {
                    continue;
                }

                var parameters = method.GetParameters();
                var expectedCount = attribute.ParameterKinds.Length +
                    (attribute.TakesContext ? 1 : 0);
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
    }

    /// <summary>
    /// Compiler view of one instantiable host native object type.
    /// </summary>
    internal sealed class HostNativeObjectDescriptor
    {
        private readonly Dictionary<string, HostNativeFieldDescriptor> _fields;
        private readonly Dictionary<string, HostNativeMethodDescriptor> _methods;

        public HostNativeObjectDescriptor(
            string typeName,
            Type clrType,
            ConstructorInfo constructor,
            AuroraExportValueKind[] constructorParameterKinds)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            ClrType = clrType ?? throw new ArgumentNullException(nameof(clrType));
            Constructor = constructor;
            ConstructorParameterKinds = constructorParameterKinds ??
                throw new ArgumentNullException(nameof(constructorParameterKinds));
            RequiredConstructorParameterCount = constructor == null
                ? 0
                : CountRequiredParameters(constructor.GetParameters(), 0);
            _fields = new Dictionary<string, HostNativeFieldDescriptor>(StringComparer.Ordinal);
            _methods = new Dictionary<string, HostNativeMethodDescriptor>(StringComparer.Ordinal);
        }

        public string TypeName { get; }
        public Type ClrType { get; }
        public ConstructorInfo Constructor { get; }
        public AuroraExportValueKind[] ConstructorParameterKinds { get; }
        public int RequiredConstructorParameterCount { get; }

        public bool TryGetField(string name, out HostNativeFieldDescriptor field)
        {
            if (name == null)
            {
                field = null;
                return false;
            }
            return _fields.TryGetValue(name, out field);
        }

        public bool TryGetMethod(string name, out HostNativeMethodDescriptor method)
        {
            if (name == null)
            {
                method = null;
                return false;
            }
            return _methods.TryGetValue(name, out method);
        }

        internal void AddField(HostNativeFieldDescriptor field)
        {
            if (!_fields.TryAdd(field.MemberName, field))
            {
                throw new InvalidOperationException(
                    $"Duplicate generated Aurora native member '{TypeName}.{field.MemberName}'.");
            }
        }

        internal void AddMethod(HostNativeMethodDescriptor method)
        {
            if (_fields.ContainsKey(method.MemberName) ||
                !_methods.TryAdd(method.MemberName, method))
            {
                throw new InvalidOperationException(
                    $"Duplicate generated Aurora native member '{TypeName}.{method.MemberName}'.");
            }
        }

        internal static int CountRequiredParameters(ParameterInfo[] parameters, int start)
        {
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

    /// <summary>
    /// Compiler view of one exported native object instance field.
    /// </summary>
    internal sealed class HostNativeFieldDescriptor
    {
        public HostNativeFieldDescriptor(
            string memberName,
            FieldInfo field,
            AuroraExportValueKind kind,
            bool isReadOnly)
        {
            MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Kind = kind;
            IsReadOnly = isReadOnly;
        }

        public string MemberName { get; }
        public FieldInfo Field { get; }
        public AuroraExportValueKind Kind { get; }
        public bool IsReadOnly { get; }
    }

    /// <summary>
    /// Compiler view of one exported native object instance method.
    /// </summary>
    internal sealed class HostNativeMethodDescriptor
    {
        public HostNativeMethodDescriptor(
            string memberName,
            MethodInfo method,
            AuroraExportValueKind returnKind,
            AuroraExportValueKind[] parameterKinds,
            bool takesContext)
        {
            MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
            Method = method ?? throw new ArgumentNullException(nameof(method));
            ReturnKind = returnKind;
            ParameterKinds = parameterKinds ?? throw new ArgumentNullException(nameof(parameterKinds));
            TakesContext = takesContext;
            RequiredScriptParameterCount = HostNativeObjectDescriptor.CountRequiredParameters(
                method.GetParameters(),
                takesContext ? 1 : 0);
        }

        public string MemberName { get; }
        public MethodInfo Method { get; }
        public AuroraExportValueKind ReturnKind { get; }
        public AuroraExportValueKind[] ParameterKinds { get; }
        public bool TakesContext { get; }
        public int RequiredScriptParameterCount { get; }

        public Type GetScriptParameterType(int index)
        {
            return Method.GetParameters()[(TakesContext ? 1 : 0) + index].ParameterType;
        }

        public ParameterInfo GetScriptParameter(int index)
        {
            return Method.GetParameters()[(TakesContext ? 1 : 0) + index];
        }
    }
}
