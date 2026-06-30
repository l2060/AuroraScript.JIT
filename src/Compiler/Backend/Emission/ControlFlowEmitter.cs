using AuroraScript.Compiler.Backend.Lowering;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class ControlFlowEmitter
    {
        private readonly LocalEmitter _locals;
        private readonly ExpressionEmitter _expressions;

        public ControlFlowEmitter(LocalEmitter locals, ExpressionEmitter expressions)
        {
            _locals = locals;
            _expressions = expressions;
        }

        public void Emit(FunctionEmissionContext context, LoweredStatement statement)
        {
            if (statement == null || statement is LoweredNoOpStatement)
            {
                return;
            }

            context.RecordStatement(statement);
            switch (statement)
            {
                case LoweredUnsupportedStatement unsupported:
                    throw context.Unsupported(unsupported);
                case LoweredBlockStatement block:
                    for (var i = 0; i < block.Statements.Length; i++)
                    {
                        Emit(context, block.Statements[i]);
                    }
                    return;
                case LoweredExpressionStatement expressionStatement:
                    _expressions.Emit(context, expressionStatement.Expression);
                    return;
                case LoweredReturnStatement returnStatement:
                    _expressions.Emit(context, returnStatement.Expression);
                    return;
                case LoweredVariableDeclarationStatement variable:
                    _locals.EmitDeclaration(context, variable);
                    _expressions.Emit(context, variable.Initializer);
                    return;
                case LoweredObjectDestructuringDeclarationStatement objectDestructuring:
                    _locals.EmitObjectDestructuringDeclaration(context, objectDestructuring);
                    _expressions.Emit(context, objectDestructuring.Initializer);
                    return;
                case LoweredArrayDestructuringDeclarationStatement arrayDestructuring:
                    _locals.EmitArrayDestructuringDeclaration(context, arrayDestructuring);
                    _expressions.Emit(context, arrayDestructuring.Initializer);
                    return;
                case LoweredFunctionDeclarationStatement function:
                    _locals.EmitFunctionDeclaration(context, function);
                    return;
                case LoweredIfStatement ifStatement:
                    _expressions.Emit(context, ifStatement.Condition);
                    Emit(context, ifStatement.Body);
                    Emit(context, ifStatement.Else);
                    return;
                case LoweredWhileStatement whileStatement:
                    _expressions.Emit(context, whileStatement.Condition);
                    Emit(context, whileStatement.Body);
                    return;
                case LoweredForStatement forStatement:
                    Emit(context, forStatement.Initializer);
                    _expressions.Emit(context, forStatement.Condition);
                    _expressions.Emit(context, forStatement.Incrementor);
                    Emit(context, forStatement.Body);
                    return;
                case LoweredForInStatement forInStatement:
                    Emit(context, forInStatement.Initializer);
                    _expressions.Emit(context, forInStatement.Iterator);
                    Emit(context, forInStatement.Body);
                    return;
                case LoweredTryStatement tryStatement:
                    context.RecordCatchSlot(tryStatement.CatchSlot);
                    Emit(context, tryStatement.Body);
                    Emit(context, tryStatement.CatchBody);
                    Emit(context, tryStatement.FinallyBody);
                    return;
                case LoweredThrowStatement throwStatement:
                    _expressions.Emit(context, throwStatement.Expression);
                    return;
                case LoweredDeleteStatement deleteStatement:
                    _expressions.Emit(context, deleteStatement.Expression);
                    return;
                case LoweredDebuggerStatement:
                case LoweredBreakStatement:
                case LoweredContinueStatement:
                    return;
            }
        }
    }
}
