using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;

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
        Array = 1 << 9,
        Float64Array = 1 << 10,
        UInt8Array = 1 << 13,
        Int16Array = 1 << 14,
        UInt16Array = 1 << 15,
        UInt32Array = 1 << 16,
        Int64Array = 1 << 17,
        UInt64Array = 1 << 18,
        Dynamic = Null | Boolean | Number | String | Object |
            Int32Array | Int8Array | BooleanArray | Float64Array |
            UInt8Array | Int16Array | UInt16Array | UInt32Array | Int64Array | UInt64Array | Array
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
            NativeCoercionKind coercion = NativeCoercionKind.None)
        {
            Type = type;
            Coercion = coercion;
        }

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
            return Type == other.Type && Coercion == other.Coercion;
        }

        public override bool Equals(object obj)
        {
            return obj is DirectParameterType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Coercion);
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
                    Runtime.CheckedType.String => FlowValueType.String,
                    Runtime.CheckedType.Object => FlowValueType.Object,
                    Runtime.CheckedType.Array => FlowValueType.Array,
                    Runtime.CheckedType.Int32Array => FlowValueType.Int32Array,
                    Runtime.CheckedType.Int8Array => FlowValueType.Int8Array,
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

        private static bool TryGetCheckedType(
            string typeName,
            out Runtime.CheckedType checkedType)
        {
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
                IsPackedArray(type) || type == FlowValueType.Array;
        }

        public static FlowValueType GetDirectLocalType(DirectParameterType parameter)
        {
            return parameter.Type;
        }

        public static bool IsNumeric(FlowValueType type)
        {
            return type is FlowValueType.Int32 or FlowValueType.Number;
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
                (parameterType == FlowValueType.Number &&
                    argumentType == FlowValueType.Int32) ||
                (parameter.IsInt32Coercion && IsNumeric(argumentType));
        }

        public static FlowValueType Merge(FlowValueType left, FlowValueType right)
        {
            if (left == FlowValueType.None) return right;
            if (right == FlowValueType.None) return left;
            var merged = left | right;
            // Number is the semantic widening of the internal Int32 representation.
            // Keeping both bits would create a union that has no single native CIL
            // representation and would unnecessarily fall back to ScriptDatum.
            if ((merged & FlowValueType.Number) != 0)
            {
                merged &= ~FlowValueType.Int32;
            }
            return merged;
        }

        public static FlowValueType GetPackedElementType(FlowValueType type)
        {
            return type == FlowValueType.BooleanArray
                ? FlowValueType.Boolean
                : type == FlowValueType.Float64Array
                    ? FlowValueType.Number
                : type is FlowValueType.UInt8Array or FlowValueType.Int16Array or
                    FlowValueType.UInt16Array or FlowValueType.UInt32Array or
                    FlowValueType.Int64Array or FlowValueType.UInt64Array
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
            hasConstant: false);

        public BoundName(
            string name,
            LocalSlotId local,
            UpvalueSlotId upvalue,
            SymbolId moduleSymbol,
            FunctionId directFunction,
            ScriptDatum constant,
            bool hasConstant)
        {
            Name = name;
            Local = local;
            Upvalue = upvalue;
            ModuleSymbol = moduleSymbol;
            DirectFunction = directFunction;
            Constant = constant;
            HasConstant = hasConstant;
        }

        public string Name { get; }
        public LocalSlotId Local { get; }
        public UpvalueSlotId Upvalue { get; }
        public SymbolId ModuleSymbol { get; }
        public FunctionId DirectFunction { get; }
        public ScriptDatum Constant { get; }
        public bool HasConstant { get; }
        public bool IsLocal => Local.IsValid && !ModuleSymbol.IsValid;
        public bool IsUnshadowedGlobal =>
            !Local.IsValid &&
            !Upvalue.IsValid &&
            !ModuleSymbol.IsValid &&
            !HasConstant;
    }

    internal sealed class TypedFunctionCode
    {
        private readonly Dictionary<NameExpression, BoundName> _names;
        private readonly Dictionary<VariableDeclaration, LocalSlotId> _declarations;
        private readonly Dictionary<Expression, FlowValueType> _expressionTypes;
        private readonly Dictionary<Expression, TypeDeclaration> _structuralTypes;

        public TypedFunctionCode(
            FunctionPlan function,
            Dictionary<NameExpression, BoundName> names,
            Dictionary<VariableDeclaration, LocalSlotId> declarations,
            Dictionary<Expression, FlowValueType> expressionTypes,
            Dictionary<Expression, TypeDeclaration> structuralTypes,
            FlowValueType[] localTypes,
            TypeDeclaration[] localStructuralTypes,
            bool[] writtenLocals,
            FlowValueType returnType)
        {
            Function = function ?? throw new ArgumentNullException(nameof(function));
            _names = names ?? throw new ArgumentNullException(nameof(names));
            _declarations = declarations ?? throw new ArgumentNullException(nameof(declarations));
            _expressionTypes = expressionTypes ?? throw new ArgumentNullException(nameof(expressionTypes));
            _structuralTypes = structuralTypes ?? throw new ArgumentNullException(nameof(structuralTypes));
            LocalTypes = localTypes ?? throw new ArgumentNullException(nameof(localTypes));
            LocalStructuralTypes = localStructuralTypes ?? throw new ArgumentNullException(nameof(localStructuralTypes));
            WrittenLocals = writtenLocals ?? throw new ArgumentNullException(nameof(writtenLocals));
            ReturnType = returnType;
        }

        public FunctionPlan Function { get; }
        public FlowValueType[] LocalTypes { get; }
        public TypeDeclaration[] LocalStructuralTypes { get; }
        public bool[] WrittenLocals { get; }
        public FlowValueType ReturnType { get; }

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

        public TypeDeclaration GetLocalStructuralType(LocalSlotId slot)
        {
            return slot.IsValid &&
                (uint)slot.Value < (uint)LocalStructuralTypes.Length
                    ? LocalStructuralTypes[slot.Value]
                    : null;
        }
    }
}
