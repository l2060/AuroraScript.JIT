using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Code
{
    [Flags]
    internal enum FlowValueType : byte
    {
        None = 0,
        Null = 1 << 0,
        Boolean = 1 << 1,
        Number = 1 << 2,
        String = 1 << 3,
        Object = 1 << 4,
        Dynamic = Null | Boolean | Number | String | Object
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
