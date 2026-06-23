using AuroraScript.Compiler.Backend.Lowering;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class LocalEmitter
    {
        public void EmitDeclaration(FunctionEmissionContext context, LoweredVariableDeclarationStatement statement)
        {
            context.RecordLocal(statement.Slot);
        }

        public void EmitObjectDestructuringDeclaration(FunctionEmissionContext context, LoweredObjectDestructuringDeclarationStatement statement)
        {
            for (var i = 0; i < statement.Bindings.Length; i++)
            {
                context.RecordLocal(statement.Bindings[i].Slot);
            }
        }

        public void EmitArrayDestructuringDeclaration(FunctionEmissionContext context, LoweredArrayDestructuringDeclarationStatement statement)
        {
            for (var i = 0; i < statement.Bindings.Length; i++)
            {
                context.RecordLocal(statement.Bindings[i].Slot);
            }
        }

        public void EmitFunctionDeclaration(FunctionEmissionContext context, LoweredFunctionDeclarationStatement statement)
        {
            context.RecordNestedFunction(statement.Function);
            context.RecordLocal(statement.LocalSlot);
        }

        public void EmitName(FunctionEmissionContext context, LoweredNameExpression expression)
        {
            context.RecordLocal(expression.LocalSlot);
            context.RecordUpvalue(expression.UpvalueSlot);
            context.RecordModuleSymbol(expression.ModuleSymbol);
        }
    }
}
