using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Code
{
    [Flags]
    internal enum FlowValueType : ushort
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
        // Direct-call ABI markers. They never escape into expression flow: the
        // callee sees an Int32 local, while the caller performs the same numeric
        // conversion that the corresponding bitwise operation used before the
        // call was specialized.
        Int32Bitwise = 1 << 11,
        Int32Shift = 1 << 12,
        Dynamic = Null | Boolean | Number | String | Object |
            Int32Array | Int8Array | BooleanArray | Float64Array | Array
    }

    internal static class FlowValueTypeFacts
    {
        public static bool IsPackedArray(FlowValueType type)
        {
            return type is FlowValueType.Int32Array or
                FlowValueType.Int8Array or
                FlowValueType.Float64Array or
                FlowValueType.BooleanArray;
        }

        public static bool ContainsPackedArray(FlowValueType type)
        {
            const FlowValueType packed = FlowValueType.Int32Array |
                FlowValueType.Int8Array |
                FlowValueType.Float64Array |
                FlowValueType.BooleanArray;
            return (type & packed) != 0;
        }

        public static bool IsNativeDirectParameter(FlowValueType type)
        {
            return type == FlowValueType.Boolean || IsNumeric(type) ||
                IsInt32Coercion(type) ||
                IsPackedArray(type) || type == FlowValueType.Array;
        }

        public static bool IsInt32Coercion(FlowValueType type)
        {
            return type is FlowValueType.Int32Bitwise or FlowValueType.Int32Shift;
        }

        public static FlowValueType GetDirectLocalType(FlowValueType type)
        {
            return IsInt32Coercion(type) ? FlowValueType.Int32 : type;
        }

        public static bool IsNumeric(FlowValueType type)
        {
            return type is FlowValueType.Int32 or FlowValueType.Number;
        }

        public static bool CanPassNativeArgument(
            FlowValueType parameterType,
            FlowValueType argumentType)
        {
            return parameterType == argumentType ||
                (parameterType == FlowValueType.Number &&
                    argumentType == FlowValueType.Int32) ||
                (IsInt32Coercion(parameterType) && IsNumeric(argumentType));
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
                : type is FlowValueType.Int32Array or FlowValueType.Int8Array
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

        public TypedFunctionCode(
            FunctionPlan function,
            Dictionary<NameExpression, BoundName> names,
            Dictionary<VariableDeclaration, LocalSlotId> declarations,
            Dictionary<Expression, FlowValueType> expressionTypes,
            FlowValueType[] localTypes,
            bool[] writtenLocals,
            FlowValueType returnType)
        {
            Function = function ?? throw new ArgumentNullException(nameof(function));
            _names = names ?? throw new ArgumentNullException(nameof(names));
            _declarations = declarations ?? throw new ArgumentNullException(nameof(declarations));
            _expressionTypes = expressionTypes ?? throw new ArgumentNullException(nameof(expressionTypes));
            LocalTypes = localTypes ?? throw new ArgumentNullException(nameof(localTypes));
            WrittenLocals = writtenLocals ?? throw new ArgumentNullException(nameof(writtenLocals));
            ReturnType = returnType;
        }

        public FunctionPlan Function { get; }
        public FlowValueType[] LocalTypes { get; }
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
    }
}
