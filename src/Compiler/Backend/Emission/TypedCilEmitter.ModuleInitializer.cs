using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed partial class TypedCilEmitter
    {
        internal void EmitInitializerBody(ILGenerator il, Action emitBody)
        {
            EmitMethodBody(_module.InitializerFunction, _moduleCode.Initializer, il,
                FunctionCallConvention.Span, directMode: false, directParameterTypes: null,
                StackValueKind.Void, nativeReturn: null, out _, emitBody);
        }

        internal void EmitInitializerDatum(Expression expression) => EmitDatum(expression);
        internal void EmitInitializerDiscarded(Expression expression) => EmitExpressionDiscarded(expression);

        internal void EmitInitializerVariable(VariableDeclaration declaration)
        {
            EmitVariable(declaration);
            ConvertToDatum(EmitLocalValue(_code.GetDeclarationSlot(declaration)));
        }

        private bool TryEmitModuleCachedRead(NameExpression name, out StackValueKind kind)
        {
            kind = default;
            if (!_function.IsModuleInitializer || _code.ModuleCachedReads == null ||
                !_code.ModuleCachedReads.TryGetValue(name, out var declaration)) return false;
            kind = EmitLocalValue(_code.GetDeclarationSlot(declaration));
            return true;
        }

        private void EmitModuleCacheWrite(BoundName binding)
        {
            if (!_function.IsModuleInitializer || !IsModuleBinding(binding) || !binding.Local.IsValid) return;
            // Module storage stays observable; its local mirror uses the same
            // representation and conversion helpers as ordinary function locals.
            _il.Emit(OpCodes.Dup);
            EmitDatumToNativeParameter(_il, new DirectParameterType(
                _code.GetLocalType(binding.Local), nativeObject: _code.GetLocalNativeObjectType(binding.Local)));
            EmitStoreLocalFromStack(binding.Local);
        }
    }
}
