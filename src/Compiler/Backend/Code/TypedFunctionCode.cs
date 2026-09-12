using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AuroraScript.Compiler.Backend.Code
{
    [Flags]
    internal enum FlowValueType : uint
    {
        None = 0,
        Null = 1 << 0,
        Boolean = 1 << 1,
        Number = 1 << 2,
        String = 1 << 3,
        Object = 1 << 4,
        Int32Array = 1 << 5,
        Int8Array = 1 << 6,
        BooleanArray = 1 << 7,
        Int32 = 1 << 8,
        Float32Array = 1 << 11,
        Float64Array = 1 << 10,
        UInt8Array = 1 << 13,
        Int16Array = 1 << 14,
        UInt16Array = 1 << 15,
        UInt32Array = 1 << 16,
        Int64Array = 1 << 17,
        UInt64Array = 1 << 18,
        Int64 = 1 << 19,
        UInt32 = 1 << 20,
        UInt64 = 1 << 21,
        Dynamic = Null | Boolean | Number | String | Object |
            Int32Array | Int8Array | BooleanArray | Float32Array | Float64Array |
            UInt8Array | Int16Array | UInt16Array | UInt32Array | Int64Array | UInt64Array |
            Int64 | UInt64
    }

    internal enum NativeCoercionKind : byte
    {
        None,
        ArithmeticNumber,
        Boolean,
        Int32Bitwise,
        Int32Shift
    }

    internal readonly struct DirectParameterType : IEquatable<DirectParameterType>
    {
        public DirectParameterType(
            FlowValueType type,
            NativeCoercionKind coercion = NativeCoercionKind.None,
            HostNativeObjectDescriptor nativeObject = null)
        {
            Type = type;
            Coercion = coercion;
            NativeObject = nativeObject;
        }

        public HostNativeObjectDescriptor NativeObject { get; }
        public FlowValueType Type { get; }
        public NativeCoercionKind Coercion { get; }
        public bool IsCoercion => Coercion != NativeCoercionKind.None;
        public bool IsInt32Coercion =>
            Coercion is NativeCoercionKind.Int32Bitwise or NativeCoercionKind.Int32Shift;

        public static DirectParameterType FromCoercion(NativeCoercionKind coercion)
        {
            return coercion switch
            {
                NativeCoercionKind.ArithmeticNumber => new DirectParameterType(
                    FlowValueType.Number,
                    coercion),
                NativeCoercionKind.Boolean => new DirectParameterType(
                    FlowValueType.Boolean,
                    coercion),
                NativeCoercionKind.Int32Bitwise or NativeCoercionKind.Int32Shift =>
                    new DirectParameterType(FlowValueType.Int32, coercion),
                _ => new DirectParameterType(FlowValueType.Dynamic)
            };
        }

        public bool Equals(DirectParameterType other)
        {
            return Type == other.Type && Coercion == other.Coercion && ReferenceEquals(NativeObject, other.NativeObject);
        }

        public override bool Equals(object obj)
        {
            return obj is DirectParameterType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Coercion, NativeObject);
        }

        public static bool operator ==(DirectParameterType left, DirectParameterType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DirectParameterType left, DirectParameterType right)
        {
            return !left.Equals(right);
        }
    }

    internal static class FlowValueTypeFacts
    {
        public static FlowValueType FromCheckedTypeName(string typeName)
        {
            return TryGetCheckedType(typeName, out var checkedType)
                ? checkedType switch
                {
                    Runtime.CheckedType.Null => FlowValueType.Null,
                    Runtime.CheckedType.Boolean => FlowValueType.Boolean,
                    Runtime.CheckedType.Number => FlowValueType.Number,
                    Runtime.CheckedType.Int32 => FlowValueType.Int32,
                    Runtime.CheckedType.UInt32 => FlowValueType.UInt32,
                    Runtime.CheckedType.Int64 => FlowValueType.Int64,
                    Runtime.CheckedType.UInt64 => FlowValueType.UInt64,
                    Runtime.CheckedType.String => FlowValueType.String,
                    Runtime.CheckedType.Object => FlowValueType.Object,
                    Runtime.CheckedType.Array => FlowValueType.Object,
                    Runtime.CheckedType.Int32Array => FlowValueType.Int32Array,
                    Runtime.CheckedType.Int8Array => FlowValueType.Int8Array,
                    Runtime.CheckedType.Float32Array => FlowValueType.Float32Array,
                    Runtime.CheckedType.Float64Array => FlowValueType.Float64Array,
                    Runtime.CheckedType.BooleanArray => FlowValueType.BooleanArray,
                    Runtime.CheckedType.UInt8Array => FlowValueType.UInt8Array,
                    Runtime.CheckedType.Int16Array => FlowValueType.Int16Array,
                    Runtime.CheckedType.UInt16Array => FlowValueType.UInt16Array,
                    Runtime.CheckedType.UInt32Array => FlowValueType.UInt32Array,
                    Runtime.CheckedType.Int64Array => FlowValueType.Int64Array,
                    Runtime.CheckedType.UInt64Array => FlowValueType.UInt64Array,
                    _ => FlowValueType.None
                }
                : FlowValueType.None;
        }

        public static Runtime.CheckedType GetCheckedType(string typeName)
        {
            if (TryGetCheckedType(typeName, out var checkedType))
            {
                return checkedType;
            }
            throw new ArgumentOutOfRangeException(
                nameof(typeName),
                typeName,
                "Unsupported checked type.");
        }

        public static bool IsCheckedTypeName(string typeName)
        {
            return TryGetCheckedType(typeName, out _);
        }

        private static bool TryGetCheckedType(
            string typeName,
            out Runtime.CheckedType checkedType)
        {
            if (string.Equals(typeName, "int32", StringComparison.Ordinal))
            {
                checkedType = Runtime.CheckedType.Int32;
                return true;
            }
            if (string.Equals(typeName, "uint32", StringComparison.Ordinal))
            {
                checkedType = Runtime.CheckedType.UInt32;
                return true;
            }
            if (string.Equals(typeName, "int64", StringComparison.Ordinal))
            {
                checkedType = Runtime.CheckedType.Int64;
                return true;
            }
            if (string.Equals(typeName, "uint64", StringComparison.Ordinal))
            {
                checkedType = Runtime.CheckedType.UInt64;
                return true;
            }
            if (string.Equals(
                    typeName,
                    nameof(Runtime.CheckedType.Int32),
                    StringComparison.Ordinal) ||
                string.Equals(
                    typeName,
                    nameof(Runtime.CheckedType.UInt32),
                    StringComparison.Ordinal) ||
                string.Equals(
                    typeName,
                    nameof(Runtime.CheckedType.Int64),
                    StringComparison.Ordinal) ||
                string.Equals(
                    typeName,
                    nameof(Runtime.CheckedType.UInt64),
                    StringComparison.Ordinal))
            {
                checkedType = default;
                return false;
            }
            return Enum.TryParse(
                    typeName,
                    ignoreCase: false,
                    out checkedType) &&
                Enum.IsDefined(checkedType);
        }

        public static bool IsPackedArray(FlowValueType type)
        {
            return type is FlowValueType.Int32Array or
                FlowValueType.Int8Array or
                FlowValueType.Float32Array or
                FlowValueType.Float64Array or
                FlowValueType.BooleanArray or
                FlowValueType.UInt8Array or
                FlowValueType.Int16Array or
                FlowValueType.UInt16Array or
                FlowValueType.UInt32Array or
                FlowValueType.Int64Array or
                FlowValueType.UInt64Array;
        }

        public static bool ContainsPackedArray(FlowValueType type)
        {
            const FlowValueType packed = FlowValueType.Int32Array |
                FlowValueType.Int8Array |
                FlowValueType.Float32Array |
                FlowValueType.Float64Array |
                FlowValueType.BooleanArray |
                FlowValueType.UInt8Array |
                FlowValueType.Int16Array |
                FlowValueType.UInt16Array |
                FlowValueType.UInt32Array |
                FlowValueType.Int64Array |
                FlowValueType.UInt64Array;
            return (type & packed) != 0;
        }

        public static bool IsNativeDirectParameter(DirectParameterType parameter)
        {
            var type = parameter.Type;
            return parameter.IsCoercion ||
                type == FlowValueType.Boolean || IsNumeric(type) ||
                IsPackedArray(type);
        }

        public static FlowValueType GetDirectLocalType(DirectParameterType parameter)
        {
            return parameter.Type;
        }

        public static bool IsNumeric(FlowValueType type)
        {
            return type is FlowValueType.Int32 or FlowValueType.UInt32 or
                FlowValueType.Int64 or FlowValueType.UInt64 or
                FlowValueType.Number;
        }

        public static bool ContainsExact64(FlowValueType type)
        {
            return (type & (FlowValueType.Int64 | FlowValueType.UInt64)) != 0;
        }

        public static bool MayShareExact64(
            FlowValueType left,
            FlowValueType right)
        {
            return ((left & right) &
                (FlowValueType.Int64 | FlowValueType.UInt64)) != 0;
        }

        public static bool IsNumberCompatible(FlowValueType type)
        {
            return type is FlowValueType.Int32 or FlowValueType.UInt32 or
                FlowValueType.Number;
        }

        public static bool CanPassNativeArgument(
            DirectParameterType parameter,
            FlowValueType argumentType)
        {
            var parameterType = parameter.Type;
            if (parameter.Coercion is
                NativeCoercionKind.ArithmeticNumber or NativeCoercionKind.Boolean)
            {
                return true;
            }
            return parameterType == argumentType ||
                (IsPackedArray(parameterType) &&
                    argumentType == FlowValueType.Null) ||
                (parameterType == FlowValueType.Number &&
                    IsNumberCompatible(argumentType)) ||
                // A declared int32 accepts any number natively: the call site
                // still runs the exact range check, it just does not need a
                // ScriptDatum or a dynamic dispatch to do it.
                (parameterType == FlowValueType.Int32 &&
                    argumentType is FlowValueType.Number or FlowValueType.UInt32) ||
                (parameterType == FlowValueType.UInt32 &&
                    argumentType is FlowValueType.Number or FlowValueType.Int32) ||
                (parameterType == FlowValueType.Int64 &&
                    argumentType is FlowValueType.Number or FlowValueType.Int32 or
                        FlowValueType.UInt32 or FlowValueType.UInt64) ||
                (parameterType == FlowValueType.UInt64 &&
                    argumentType is FlowValueType.Number or FlowValueType.Int32 or
                        FlowValueType.UInt32 or FlowValueType.Int64) ||
                (parameter.IsInt32Coercion && IsNumberCompatible(argumentType));
        }

        public static FlowValueType Merge(FlowValueType left, FlowValueType right)
        {
            if (left == FlowValueType.None) return right;
            if (right == FlowValueType.None) return left;
            var merged = left | right;
            // Int32 and UInt32 are native representations of Number. The
            // 64-bit integer types are distinct script primitives and must not
            // be absorbed into Number when control-flow paths merge.
            if ((merged & FlowValueType.Number) != 0)
            {
                merged &= ~(FlowValueType.Int32 | FlowValueType.UInt32);
            }
            else if ((merged & FlowValueType.Int32) != 0 &&
                (merged & FlowValueType.UInt32) != 0)
            {
                merged &= ~(FlowValueType.Int32 | FlowValueType.UInt32);
                merged |= FlowValueType.Number;
            }
            return merged;
        }

        public static FlowValueType GetPackedElementType(FlowValueType type)
        {
            return type == FlowValueType.BooleanArray
                ? FlowValueType.Boolean
                : type == FlowValueType.UInt32Array
                    ? FlowValueType.UInt32
                : type == FlowValueType.Int64Array
                    ? FlowValueType.Int64
                : type == FlowValueType.UInt64Array
                    ? FlowValueType.UInt64
                : type is FlowValueType.Float32Array or FlowValueType.Float64Array
                    ? FlowValueType.Number
                : type is FlowValueType.UInt8Array or FlowValueType.Int16Array or
                    FlowValueType.UInt16Array
                    ? FlowValueType.Number
                : IsPackedArray(type)
                    ? FlowValueType.Int32
                    : FlowValueType.Dynamic;
        }

        public static bool TryGetPackedArrayType(string name, out FlowValueType type)
        {
            type = name switch
            {
                "Int32Array" => FlowValueType.Int32Array,
                "Int8Array" => FlowValueType.Int8Array,
                "Float32Array" => FlowValueType.Float32Array,
                "Float64Array" => FlowValueType.Float64Array,
                "BooleanArray" => FlowValueType.BooleanArray,
                "UInt8Array" => FlowValueType.UInt8Array,
                "Int16Array" => FlowValueType.Int16Array,
                "UInt16Array" => FlowValueType.UInt16Array,
                "UInt32Array" => FlowValueType.UInt32Array,
                "Int64Array" => FlowValueType.Int64Array,
                "UInt64Array" => FlowValueType.UInt64Array,
                _ => FlowValueType.None
            };
            return type != FlowValueType.None;
        }
    }

    internal readonly struct BoundName
    {
        public static readonly BoundName Unbound = new(
            null,
            LocalSlotId.Invalid,
            UpvalueSlotId.Invalid,
            SymbolId.Invalid,
            FunctionId.Invalid,
            default,
            NumericLiteralSuffix.None,
            hasConstant: false,
            isDeclaredOnly: false);

        public BoundName(
            string name,
            LocalSlotId local,
            UpvalueSlotId upvalue,
            SymbolId moduleSymbol,
            FunctionId directFunction,
            ScriptDatum constant,
            NumericLiteralSuffix constantNumericHint,
            bool hasConstant,
            bool isDeclaredOnly = false,
            bool isContext = false)
        {
            Name = name;
            Local = local;
            Upvalue = upvalue;
            ModuleSymbol = moduleSymbol;
            DirectFunction = directFunction;
            Constant = constant;
            ConstantNumericHint = constantNumericHint;
            HasConstant = hasConstant;
            IsDeclaredOnly = isDeclaredOnly;
            IsContext = isContext;
        }

        public string Name { get; }
        public LocalSlotId Local { get; }
        public UpvalueSlotId Upvalue { get; }
        public SymbolId ModuleSymbol { get; }
        public FunctionId DirectFunction { get; }
        public ScriptDatum Constant { get; }
        public NumericLiteralSuffix ConstantNumericHint { get; }
        public bool HasConstant { get; }
        public bool IsDeclaredOnly { get; }
        public bool IsContext { get; }
        public bool IsLocal => Local.IsValid && !ModuleSymbol.IsValid;
        public bool IsUnshadowedGlobal =>
            !IsContext &&
            !Local.IsValid &&
            !Upvalue.IsValid &&
            !HasConstant &&
            (!ModuleSymbol.IsValid || IsDeclaredOnly);
    }

    /// <summary>
    /// An ascending <c>for</c> loop whose counter was proven to hold exact
    /// integers, so the emitter can hoist the bound and drive it natively.
    /// </summary>
    internal readonly struct CountedLoop
    {
        public CountedLoop(LocalSlotId counter, Expression bound)
        {
            Counter = counter;
            Bound = bound;
        }

        public LocalSlotId Counter { get; }
        public Expression Bound { get; }
    }

    internal sealed class TypedFunctionCode
    {
        private readonly Dictionary<NameExpression, BoundName> _names;
        private readonly Dictionary<VariableDeclaration, LocalSlotId> _declarations;
        private readonly Dictionary<Expression, FlowValueType> _expressionTypes;
        private readonly Dictionary<Expression, TypeDeclaration> _structuralTypes;
        private readonly Dictionary<Expression, HostNativeObjectDescriptor> _nativeObjectTypes;
        private readonly Dictionary<Expression, Type> _clrTypes;
        private readonly Dictionary<FunctionCallExpression, MethodBase> _clrCalls;
        private readonly Dictionary<Expression, MemberInfo> _clrMembers;
        private readonly Dictionary<ForStatement, CountedLoop> _countedLoops;
        private Dictionary<FunctionCallExpression, HostNativeMethodDescriptor> _nativeCalls;
        private Dictionary<FunctionCallExpression, HostExportDescriptor> _hostCalls;
        private readonly IReadOnlyDictionary<Expression, FlowValueType> _guardedTypes;
        private readonly IReadOnlyDictionary<Expression, HostNativeObjectDescriptor> _guardedNativeTypes;

        public TypedFunctionCode(
            FunctionPlan function,
            Dictionary<NameExpression, BoundName> names,
            Dictionary<VariableDeclaration, LocalSlotId> declarations,
            Dictionary<Expression, FlowValueType> expressionTypes,
            Dictionary<Expression, TypeDeclaration> structuralTypes,
            Dictionary<Expression, HostNativeObjectDescriptor> nativeObjectTypes,
            FlowValueType[] localTypes,
            HostNativeObjectDescriptor[] localNativeObjectTypes,
            bool[] writtenLocals,
            FlowValueType returnType,
            Dictionary<ForStatement, CountedLoop> countedLoops = null,
            Dictionary<FunctionCallExpression, HostNativeMethodDescriptor> nativeCalls = null,
            Dictionary<FunctionCallExpression, HostExportDescriptor> hostCalls = null,
            Dictionary<Expression, Type> clrTypes = null,
            Type[] localClrTypes = null,
            Dictionary<FunctionCallExpression, MethodBase> clrCalls = null,
            Dictionary<Expression, MemberInfo> clrMembers = null)
        {
            Function = function ?? throw new ArgumentNullException(nameof(function));
            _names = names ?? throw new ArgumentNullException(nameof(names));
            _declarations = declarations ?? throw new ArgumentNullException(nameof(declarations));
            _expressionTypes = expressionTypes ?? throw new ArgumentNullException(nameof(expressionTypes));
            _structuralTypes = structuralTypes ?? throw new ArgumentNullException(nameof(structuralTypes));
            _nativeObjectTypes = nativeObjectTypes ?? throw new ArgumentNullException(nameof(nativeObjectTypes));
            LocalTypes = localTypes ?? throw new ArgumentNullException(nameof(localTypes));
            LocalNativeObjectTypes = localNativeObjectTypes ??
                throw new ArgumentNullException(nameof(localNativeObjectTypes));
            WrittenLocals = writtenLocals ?? throw new ArgumentNullException(nameof(writtenLocals));
            ReturnType = returnType;
            _countedLoops = countedLoops;
            _nativeCalls = nativeCalls;
            _hostCalls = hostCalls;
            _clrTypes = clrTypes ?? new Dictionary<Expression, Type>(ReferenceEqualityComparer.Instance);
            LocalClrTypes = localClrTypes ?? new Type[localTypes.Length];
            _clrCalls = clrCalls;
            _clrMembers = clrMembers;
        }

        public bool TryGetHostCall(FunctionCallExpression call, out HostExportDescriptor descriptor)
        {
            descriptor = null;
            return _hostCalls != null && _hostCalls.TryGetValue(call, out descriptor) || _guardedTypes == null;
        }

        public void SetHostCall(FunctionCallExpression call, HostExportDescriptor descriptor) =>
            (_hostCalls ??= new())[call] = descriptor;

        public bool TryGetNativeCall(FunctionCallExpression call, out HostNativeMethodDescriptor method)
        {
            method = null;
            return _nativeCalls != null && _nativeCalls.TryGetValue(call, out method) || _guardedTypes == null;
        }

        public void SetNativeCall(FunctionCallExpression call, HostNativeMethodDescriptor method) =>
            (_nativeCalls ??= new())[call] = method;

        public bool TryGetCountedLoop(ForStatement statement, out CountedLoop loop)
        {
            if (_countedLoops == null || statement == null)
            {
                loop = default;
                return false;
            }
            return _countedLoops.TryGetValue(statement, out loop);
        }

        public FunctionPlan Function { get; }
        public FlowValueType[] LocalTypes { get; }
        public HostNativeObjectDescriptor[] LocalNativeObjectTypes { get; }
        public Type[] LocalClrTypes { get; }
        public bool[] WrittenLocals { get; }
        public FlowValueType ReturnType { get; }

        // Reads dominated by a module declaration with no intervening callback.
        internal Dictionary<NameExpression, VariableDeclaration> ModuleCachedReads { get; init; }

        // Speculative facts are deliberately separate from the proven graph and
        // local storage types. Emission may use them only behind value guards.
        internal readonly record struct PredictionFacts(FlowValueType ReturnType,
            Dictionary<Expression, FlowValueType> Types,
            Dictionary<Expression, HostNativeObjectDescriptor> NativeTypes)
        {
            public FlowValueType GetExpressionType(Expression expression, FlowValueType fallback = FlowValueType.Null) =>
                expression != null && Types != null && Types.TryGetValue(expression, out var type) ? type : fallback;
            public HostNativeObjectDescriptor GetNativeObjectType(Expression expression) =>
                expression != null && NativeTypes != null && NativeTypes.TryGetValue(expression, out var native) ? native : null;

            internal PredictionFacts KeepDifferences(TypedFunctionCode generic, TypedFunctionCode direct)
            {
                if (Types != null)
                    foreach (var pair in Types)
                        if (generic.GetExpressionType(pair.Key) == pair.Value &&
                            (direct == null || direct.GetExpressionType(pair.Key) == pair.Value)) Types.Remove(pair.Key);
                if (NativeTypes != null)
                    foreach (var pair in NativeTypes)
                        if (ReferenceEquals(generic.GetNativeObjectType(pair.Key), pair.Value) &&
                            (direct == null || ReferenceEquals(direct.GetNativeObjectType(pair.Key), pair.Value)))
                            NativeTypes.Remove(pair.Key);
                return new(ReturnType, Types?.Count > 0 ? Types : null, NativeTypes?.Count > 0 ? NativeTypes : null);
            }
        }

        public PredictionFacts? Prediction { get; internal set; }
        internal PredictionFacts GetPredictionFacts() => new(ReturnType, _expressionTypes, _nativeObjectTypes);

        internal void ApplyContextualParameterPredictions(
            IReadOnlyDictionary<int, ContextualParameterType?> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return;
            }

            var current = Prediction;
            var types = current?.Types != null
                ? new Dictionary<Expression, FlowValueType>(
                    current.Value.Types,
                    ReferenceEqualityComparer.Instance)
                : new Dictionary<Expression, FlowValueType>(
                    ReferenceEqualityComparer.Instance);
            var nativeTypes = current?.NativeTypes != null
                ? new Dictionary<Expression, HostNativeObjectDescriptor>(
                    current.Value.NativeTypes,
                    ReferenceEqualityComparer.Instance)
                : new Dictionary<Expression, HostNativeObjectDescriptor>(
                    ReferenceEqualityComparer.Instance);

            var parameterIndex = 0;
            for (var slotIndex = 0; slotIndex < Function.LocalSlots.Length; slotIndex++)
            {
                var slot = Function.LocalSlots[slotIndex];
                if (!slot.IsParameter)
                {
                    continue;
                }
                if (parameters.TryGetValue(parameterIndex, out var parameterType) &&
                    parameterType.HasValue)
                {
                    foreach (var pair in _names)
                    {
                        if (pair.Value.IsLocal && pair.Value.Local.Equals(slot.Id))
                        {
                            types[pair.Key] = parameterType.Value.Type;
                            if (parameterType.Value.NativeObject != null)
                            {
                                nativeTypes[pair.Key] =
                                    parameterType.Value.NativeObject;
                            }
                        }
                    }
                }
                parameterIndex++;
            }

            if (types.Count != 0)
            {
                Prediction = new PredictionFacts(
                    current?.ReturnType ?? ReturnType,
                    types,
                    nativeTypes);
            }
        }

        public TypedFunctionCode WithGuardedTypes(
            IReadOnlyDictionary<Expression, FlowValueType> guardedTypes,
            IReadOnlyDictionary<Expression, HostNativeObjectDescriptor> guardedNativeTypes)
        {
            return new TypedFunctionCode(this, guardedTypes, guardedNativeTypes);
        }

        private TypedFunctionCode(TypedFunctionCode original,
            IReadOnlyDictionary<Expression, FlowValueType> guardedTypes,
            IReadOnlyDictionary<Expression, HostNativeObjectDescriptor> guardedNativeTypes)
            : this(original.Function, original._names, original._declarations,
                original._expressionTypes, original._structuralTypes, original._nativeObjectTypes,
                original.LocalTypes, original.LocalNativeObjectTypes, original.WrittenLocals,
                original.ReturnType, original._countedLoops,
                clrTypes: original._clrTypes,
                localClrTypes: original.LocalClrTypes,
                clrCalls: original._clrCalls,
                clrMembers: original._clrMembers)
        {
            // A bounded overlay avoids copying the entire expression graph at
            // every guarded operation in a large function.
            // Call selections belong to the operand types that selected them.
            // Resolve guarded calls afresh instead of inheriting the ordinary target.
            _guardedTypes = guardedTypes;
            _guardedNativeTypes = guardedNativeTypes;
            ModuleCachedReads = original.ModuleCachedReads;
        }

        public BoundName GetName(NameExpression expression)
        {
            return expression != null && _names.TryGetValue(expression, out var binding)
                ? binding
                : BoundName.Unbound;
        }

        public LocalSlotId GetDeclarationSlot(VariableDeclaration declaration)
        {
            return declaration != null && _declarations.TryGetValue(declaration, out var slot)
                ? slot
                : LocalSlotId.Invalid;
        }

        public FlowValueType GetExpressionType(Expression expression)
        {
            if (expression != null && _guardedTypes != null && _guardedTypes.TryGetValue(expression, out var guarded))
                return guarded;
            return expression != null && _expressionTypes.TryGetValue(expression, out var type)
                ? type
                : FlowValueType.Null;
        }

        public FlowValueType GetLocalType(LocalSlotId slot)
        {
            return slot.IsValid && (uint)slot.Value < (uint)LocalTypes.Length
                ? LocalTypes[slot.Value]
                : FlowValueType.Dynamic;
        }

        public TypeDeclaration GetStructuralType(Expression expression)
        {
            return expression != null &&
                _structuralTypes.TryGetValue(expression, out var type)
                    ? type
                    : null;
        }

        /// <summary>
        /// The host native object type a local was proven to hold on every path, or
        /// null when the local must keep its general script representation.
        /// </summary>
        public HostNativeObjectDescriptor GetLocalNativeObjectType(LocalSlotId slot)
        {
            return slot.IsValid &&
                (uint)slot.Value < (uint)LocalNativeObjectTypes.Length
                    ? LocalNativeObjectTypes[slot.Value]
                    : null;
        }

        /// <summary>
        /// The host native object type an expression was proven to hold, or null when
        /// the value is only known to be a dynamic script object.
        /// </summary>
        public HostNativeObjectDescriptor GetNativeObjectType(Expression expression)
        {
            if (expression != null && _guardedNativeTypes != null && _guardedNativeTypes.TryGetValue(expression, out var guarded))
                return guarded;
            return expression != null &&
                _nativeObjectTypes.TryGetValue(expression, out var descriptor)
                    ? descriptor
                    : null;
        }

        public Type GetClrType(Expression expression)
        {
            return expression != null && _clrTypes.TryGetValue(expression, out var type)
                ? type
                : null;
        }

        public Type GetLocalClrType(LocalSlotId slot)
        {
            return slot.IsValid && (uint)slot.Value < (uint)LocalClrTypes.Length
                ? LocalClrTypes[slot.Value]
                : null;
        }

        public bool TryGetClrCall(FunctionCallExpression call, out MethodBase method)
        {
            method = null;
            return _clrCalls != null && _clrCalls.TryGetValue(call, out method);
        }

        public bool TryGetClrMember(Expression expression, out MemberInfo member)
        {
            member = null;
            return _clrMembers != null && _clrMembers.TryGetValue(expression, out member);
        }
    }
}
