using AuroraScript.Hosting;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Code;
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
            if (_nativeObjects.TryGetValue(typeName, out descriptor) && !descriptor.IsValueReceiver) return true;
            descriptor = null;
            return false;
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
            if (_nativeObjectsByClrType.TryGetValue(clrType, out descriptor) && !descriptor.IsValueReceiver) return true;
            descriptor = null;
            return false;
        }

        public bool TryGetNativeValue(FlowValueType type, out HostNativeObjectDescriptor descriptor)
        {
            descriptor = null;
            var clrType = type switch
            {
                FlowValueType.String => typeof(string),
                FlowValueType.Number or FlowValueType.Int32 or FlowValueType.UInt32 => typeof(double),
                FlowValueType.Int64 => typeof(long),
                FlowValueType.UInt64 => typeof(ulong),
                _ => null
            };
            return clrType != null && _nativeObjectsByClrType.TryGetValue(clrType, out descriptor) && descriptor.IsValueReceiver;
        }

        public bool TryGetValueFactory(string name, out HostExportDescriptor factory)
        {
            factory = null;
            return name != null && _nativeObjects.TryGetValue(name, out var owner) && owner.IsValueReceiver &&
                owner.FactoryMemberName != null && TryGetGlobal(name, owner.FactoryMemberName, out factory);
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

                if (attribute.ReceiverType != null
                    ? !IsPrimitiveReceiver(attribute.ReceiverType) || attribute.Constructible ||
                        attribute.ObjectType.Assembly != typeof(ScriptObject).Assembly
                    : !typeof(ScriptObject).IsAssignableFrom(attribute.ObjectType) ||
                        !typeof(IAuroraNativeInstance).IsAssignableFrom(attribute.ObjectType))
                {
                    throw new InvalidOperationException(
                        $"Generated Aurora native object '{attribute.TypeName}' does not " +
                        $"resolve to a generated ScriptObject native instance type or an engine-owned value receiver.");
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
                    (AuroraExportValueKind[])attribute.ConstructorParameterKinds.Clone(),
                    attribute.ReceiverType) { FactoryMemberName = attribute.FactoryMemberName };
                if (!_nativeObjects.TryAdd(attribute.TypeName, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Duplicate generated Aurora native object '{attribute.TypeName}'.");
                }
                if (!_nativeObjectsByClrType.TryAdd(descriptor.ClrType, descriptor))
                    throw new InvalidOperationException($"Duplicate generated Aurora native receiver '{descriptor.ClrType}'.");
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
                    owner.DeclaringType != attribute.DeclaringType)
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
                    owner.DeclaringType != attribute.DeclaringType)
                {
                    continue;
                }

                var method = ResolveInstanceMethod(attribute, owner);
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
                    attribute.TakesContext,
                    owner.IsValueReceiver,
                    attribute.IsGetter,
                    attribute.RequiresIndexProof));
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
            AuroraGeneratedNativeMethodAttribute attribute, HostNativeObjectDescriptor owner)
        {
            foreach (var method in attribute.DeclaringType.GetMethods(
                BindingFlags.Public | (owner.IsValueReceiver ? BindingFlags.Static : BindingFlags.Instance)))
            {
                if (!StringComparer.Ordinal.Equals(method.Name, attribute.MethodName) ||
                    !MatchesClrType(attribute.ReturnKind, method.ReturnType) ||
                    method.GetCustomAttribute<AuroraExportAttribute>() is not { } export ||
                    (owner.IsValueReceiver
                        ? export.Target != AuroraExportTarget.Instance
                        : export.Target == AuroraExportTarget.Type))
                {
                    continue;
                }

                var parameters = method.GetParameters();
                var expectedCount = attribute.ParameterKinds.Length +
                    (attribute.TakesContext ? 1 : 0) + (owner.IsValueReceiver ? 1 : 0);
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
                if (owner.IsValueReceiver)
                {
                    var receiverType = attribute.ReceiverType ?? owner.ClrType;
                    if (receiverType != owner.ClrType &&
                        !(owner.ClrType == typeof(double) && (receiverType == typeof(int) || receiverType == typeof(uint)))) continue;
                    if (parameters[index++].ParameterType != receiverType) continue;
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

        private static bool IsPrimitiveReceiver(Type type) => type == typeof(string) || type == typeof(double) ||
            type == typeof(long) || type == typeof(ulong);
    }

    /// <summary>
    /// Compiler view of one host native object type or engine-owned primitive receiver.
    /// </summary>
    internal sealed class HostNativeObjectDescriptor
    {
        private readonly Dictionary<string, HostNativeFieldDescriptor> _fields;
        private readonly Dictionary<string, HostNativeMethodDescriptor> _methods;

        public HostNativeObjectDescriptor(
            string typeName,
            Type clrType,
            ConstructorInfo constructor,
            AuroraExportValueKind[] constructorParameterKinds,
            Type receiverType = null)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            DeclaringType = clrType ?? throw new ArgumentNullException(nameof(clrType));
            ClrType = receiverType ?? clrType;
            IsValueReceiver = receiverType != null;
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
        public Type DeclaringType { get; }
        public bool IsValueReceiver { get; }
        public string FactoryMemberName { get; internal set; }
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
            if (!_fields.ContainsKey(method.MemberName) && _methods.TryAdd(method.MemberName, method)) return;
            if (IsValueReceiver && _methods.TryGetValue(method.MemberName, out var first))
            {
                for (var current = first; current != null; current = current.NextOverload)
                {
                    if (current.IsGetter != method.IsGetter || current.IsGetter ||
                        current.ReceiverType == method.ReceiverType && current.RequiresIndexProof == method.RequiresIndexProof &&
                        current.ParameterKinds.AsSpan().SequenceEqual(method.ParameterKinds))
                        throw new InvalidOperationException($"Duplicate generated Aurora native member '{TypeName}.{method.MemberName}'.");
                }
                method.NextOverload = first.NextOverload;
                first.NextOverload = method;
                return;
            }
            throw new InvalidOperationException($"Duplicate generated Aurora native member '{TypeName}.{method.MemberName}'.");
        }

        public HostNativeMethodDescriptor GetValueGetter(string name)
            => IsValueReceiver && TryGetMethod(name, out var method) && method.IsGetter ? method : null;

        /// <summary>Bind exact-arity value members; ambiguous or coercive calls retain the dynamic adapter.</summary>
        public HostNativeMethodDescriptor BindValueMethod(string name, IReadOnlyList<Expression> arguments,
            IReadOnlyDictionary<Expression, FlowValueType> types, bool indexIsInBounds, FlowValueType receiver)
        {
            if (!IsValueReceiver || !TryGetMethod(name, out var first) || first.IsGetter) return null;
            HostNativeMethodDescriptor best = null;
            var bestCost = int.MaxValue;
            var ambiguous = false;
            for (var candidate = first; candidate != null; candidate = candidate.NextOverload)
            {
                if (candidate.ParameterKinds.Length != arguments.Count || candidate.RequiresIndexProof && !indexIsInBounds) continue;
                var receiverCost = candidate.GetReceiverCost(receiver);
                if (receiverCost < 0) continue;
                var cost = candidate.RequiresIndexProof ? -100 : receiverCost;
                var matches = true;
                for (var i = 0; i < arguments.Count; i++)
                {
                    if (arguments[i] is SpreadExpression || !types.TryGetValue(arguments[i], out var type) ||
                        !HostExportArgumentFacts.CanPass(candidate.ParameterKinds[i], candidate.GetScriptParameterType(i), type))
                    {
                        matches = false;
                        break;
                    }
                    if (candidate.ParameterKinds[i] == AuroraExportValueKind.Datum) cost += 2;
                    else if (candidate.ParameterKinds[i] == AuroraExportValueKind.Number && type != FlowValueType.Number) cost++;
                }
                if (!matches || cost > bestCost) continue;
                if (cost == bestCost) { ambiguous = true; continue; }
                best = candidate;
                bestCost = cost;
                ambiguous = false;
            }
            return ambiguous ? null : best;
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
            bool takesContext,
            bool isValueReceiver = false,
            bool isGetter = false,
            bool requiresIndexProof = false)
        {
            MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
            Method = method ?? throw new ArgumentNullException(nameof(method));
            ReturnKind = returnKind;
            ParameterKinds = parameterKinds ?? throw new ArgumentNullException(nameof(parameterKinds));
            TakesContext = takesContext;
            IsValueReceiver = isValueReceiver;
            IsGetter = isGetter;
            RequiresIndexProof = requiresIndexProof;
            _parameters = method.GetParameters();
            RequiredScriptParameterCount = HostNativeObjectDescriptor.CountRequiredParameters(
                _parameters, ParameterOffset);
        }

        public string MemberName { get; }
        public MethodInfo Method { get; }
        public AuroraExportValueKind ReturnKind { get; }
        public AuroraExportValueKind[] ParameterKinds { get; }
        public bool TakesContext { get; }
        public bool IsValueReceiver { get; }
        public bool IsGetter { get; }
        public bool RequiresIndexProof { get; }
        public Type ReceiverType => IsValueReceiver ? _parameters[TakesContext ? 1 : 0].ParameterType : null;
        internal HostNativeMethodDescriptor NextOverload { get; set; }
        private readonly ParameterInfo[] _parameters;
        private int ParameterOffset => (TakesContext ? 1 : 0) + (IsValueReceiver ? 1 : 0);
        public int RequiredScriptParameterCount { get; }

        public int GetReceiverCost(FlowValueType type)
        {
            var clrType = type switch
            {
                FlowValueType.String => typeof(string),
                FlowValueType.Number => typeof(double),
                FlowValueType.Int32 => typeof(int),
                FlowValueType.UInt32 => typeof(uint),
                FlowValueType.Int64 => typeof(long),
                FlowValueType.UInt64 => typeof(ulong),
                _ => null
            };
            if (clrType == ReceiverType) return 0;
            return ReceiverType == typeof(double) && type is FlowValueType.Int32 or FlowValueType.UInt32 ? 1 : -1;
        }

        public Type GetScriptParameterType(int index)
        {
            return _parameters[ParameterOffset + index].ParameterType;
        }

        public ParameterInfo GetScriptParameter(int index)
        {
            return _parameters[ParameterOffset + index];
        }
    }
}
