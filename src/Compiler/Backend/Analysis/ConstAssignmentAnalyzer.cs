using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Analysis
{
    internal static class ConstAssignmentAnalyzer
    {
        public static void Apply(CompileSession session, ModulePlan modulePlan)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(modulePlan);

            var analyzer = new Analyzer(session, modulePlan);
            analyzer.VisitModule();
        }

        private sealed class Analyzer
        {
            private readonly CompileSession _session;
            private readonly ModulePlan _modulePlan;
            private readonly Dictionary<FunctionDeclaration, FunctionPlan> _functionsByDeclaration;
            private readonly Stack<FunctionPlan> _functionStack = new();
            private readonly Stack<int> _scopeStack = new();

            public Analyzer(CompileSession session, ModulePlan modulePlan)
            {
                _session = session;
                _modulePlan = modulePlan;
                _functionsByDeclaration = BuildFunctionMap(modulePlan);
            }

            public void VisitModule()
            {
                var declaration = _modulePlan.Declaration;
                for (var i = 0; i < declaration.Statements.Count; i++)
                {
                    Visit(declaration.Statements[i]);
                }

                for (var i = 0; i < declaration.Functions.Count; i++)
                {
                    VisitFunction(declaration.Functions[i]);
                }
            }

            public void Visit(AstNode node)
            {
                if (node == null)
                {
                    return;
                }

                switch (node)
                {
                    case FunctionDeclaration function:
                        VisitFunction(function);
                        return;
                    case LambdaExpression lambda:
                        VisitFunction(lambda.Function);
                        return;
                    case BlockStatement block:
                        WithNodeScope(block, () =>
                        {
                            for (var i = 0; i < block.Functions.Count; i++)
                            {
                                Visit(block.Functions[i]);
                            }
                            for (var i = 0; i < block.Statements.Count; i++)
                            {
                                Visit(block.Statements[i]);
                            }
                        });
                        return;
                    case VariableDeclaration variable:
                        Visit(variable.Initializer);
                        return;
                    case TryStatement tryStatement:
                        Visit(tryStatement.Body);
                        VisitCatch(tryStatement);
                        Visit(tryStatement.FinallyBody);
                        return;
                    case AssignmentExpression assignment:
                        ValidateAssignmentTarget(assignment.Left);
                        Visit(assignment.Right);
                        return;
                    case CompoundExpression compound:
                        ValidateAssignmentTarget(compound.Left);
                        Visit(compound.Right);
                        return;
                    case UnaryExpression unary when IsIncrementOrDecrement(unary.Operator):
                        ValidateAssignmentTarget(unary.Expression);
                        return;
                    case SetPropertyExpression setProperty:
                        Visit(setProperty.Object);
                        Visit(setProperty.Property);
                        Visit(setProperty.Value);
                        return;
                    case SetElementExpression setElement:
                        Visit(setElement.Object);
                        Visit(setElement.Index);
                        Visit(setElement.Value);
                        return;
                    case TypedDocumentExpression tdoc:
                        Visit(tdoc.Value);
                        return;
                    case CheckExpression check:
                        Visit(check.Value);
                        return;
                    case MapKeyValueExpression mapEntry:
                        Visit(mapEntry.Value);
                        return;
                }

                var visitor = new ChildVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private void VisitCatch(TryStatement statement)
            {
                if (statement.CatchBody == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(statement.CatchVariable))
                {
                    Visit(statement.CatchBody);
                    return;
                }

                var owner = statement.CatchBody is BlockStatement block ? (AstNode)block : statement;
                WithNodeScope(owner, () =>
                {
                    if (statement.CatchBody is BlockStatement catchBlock)
                    {
                        for (var i = 0; i < catchBlock.Functions.Count; i++)
                        {
                            Visit(catchBlock.Functions[i]);
                        }
                        for (var i = 0; i < catchBlock.Statements.Count; i++)
                        {
                            Visit(catchBlock.Statements[i]);
                        }
                    }
                    else
                    {
                        Visit(statement.CatchBody);
                    }
                });
            }

            private void VisitFunction(FunctionDeclaration declaration)
            {
                if (declaration == null ||
                    declaration.Flags == FunctionFlags.Declare ||
                    !_functionsByDeclaration.TryGetValue(declaration, out var function))
                {
                    return;
                }

                _functionStack.Push(function);
                _scopeStack.Push(GetScopeId(function, declaration.Body ?? declaration, 0));
                try
                {
                    for (var i = 0; i < declaration.Parameters.Count; i++)
                    {
                        Visit(declaration.Parameters[i].Initializer);
                    }
                    Visit(declaration.Body);
                }
                finally
                {
                    _scopeStack.Pop();
                    _functionStack.Pop();
                }
            }

            private void ValidateAssignmentTarget(Expression target)
            {
                if (target is GroupExpression group)
                {
                    ValidateAssignmentTarget(group.Expression);
                    return;
                }

                if (target is NameExpression name)
                {
                    ValidateNameAssignment(name);
                    return;
                }

                Visit(target);
            }

            private void ValidateNameAssignment(NameExpression name)
            {
                var value = name?.Identifier?.Value;
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                if (_functionStack.Count != 0)
                {
                    var function = _functionStack.Peek();
                    if (TryGetLocal(function, value, out var local))
                    {
                        if ((local.Flags & BackendSymbolFlags.Const) != 0)
                        {
                            ThrowConstAssignment(value, name);
                        }
                        return;
                    }

                    if (TryGetUpvalue(function, value, out var upvalue))
                    {
                        if (IsConstUpvalue(upvalue))
                        {
                            ThrowConstAssignment(value, name);
                        }
                        return;
                    }
                }

                if (_modulePlan.TryGetSymbol(value, out var symbolId) &&
                    _session.Symbols[symbolId].HasFlag(BackendSymbolFlags.Const))
                {
                    ThrowConstAssignment(value, name);
                }
            }

            private bool TryGetLocal(FunctionPlan function, string name, out LocalSlot slot)
            {
                var locals = function.LocalSlots;
                var scopeId = CurrentScopeId;
                while (scopeId >= 0)
                {
                    for (var i = locals.Length - 1; i >= 0; i--)
                    {
                        if (locals[i].ScopeId == scopeId && locals[i].Name == name)
                        {
                            slot = locals[i];
                            return true;
                        }
                    }

                    scopeId = GetParentScopeId(function, scopeId);
                }

                slot = default;
                return false;
            }

            private int CurrentScopeId => _scopeStack.Count == 0 ? 0 : _scopeStack.Peek();

            private static int GetScopeId(FunctionPlan function, AstNode node, int fallback)
            {
                return node != null &&
                    function.LocalScopeByNode != null &&
                    function.LocalScopeByNode.TryGetValue(node, out var scopeId)
                    ? scopeId
                    : fallback;
            }

            private static int GetParentScopeId(FunctionPlan function, int scopeId)
            {
                var scopes = function.LocalScopes;
                return (uint)scopeId < (uint)scopes.Length ? scopes[scopeId].ParentId : -1;
            }

            private void WithNodeScope(AstNode node, Action action)
            {
                if (_functionStack.Count == 0)
                {
                    action();
                    return;
                }

                var function = _functionStack.Peek();
                var scopeId = GetScopeId(function, node, CurrentScopeId);
                if (scopeId == CurrentScopeId)
                {
                    action();
                    return;
                }

                _scopeStack.Push(scopeId);
                try
                {
                    action();
                }
                finally
                {
                    _scopeStack.Pop();
                }
            }

            private static bool TryGetUpvalue(FunctionPlan function, string name, out UpvalueSlot slot)
            {
                var upvalues = function.UpvalueSlots;
                for (var i = 0; i < upvalues.Length; i++)
                {
                    if (upvalues[i].Name == name)
                    {
                        slot = upvalues[i];
                        return true;
                    }
                }

                slot = default;
                return false;
            }

            private bool IsConstUpvalue(UpvalueSlot upvalue)
            {
                if (!TryGetFunction(upvalue.SourceFunction, out var sourceFunction))
                {
                    return false;
                }

                if (upvalue.SourceLocal.IsValid)
                {
                    return IsConstLocal(sourceFunction, upvalue.SourceLocal);
                }

                if (upvalue.SourceUpvalue.IsValid &&
                    (uint)upvalue.SourceUpvalue.Value < (uint)sourceFunction.UpvalueSlots.Length)
                {
                    return IsConstUpvalue(sourceFunction.UpvalueSlots[upvalue.SourceUpvalue.Value]);
                }

                return false;
            }

            private bool TryGetFunction(FunctionId id, out FunctionPlan function)
            {
                var functions = _modulePlan.Functions;
                for (var i = 0; i < functions.Count; i++)
                {
                    if (functions[i].Id.Equals(id))
                    {
                        function = functions[i];
                        return true;
                    }
                }

                function = null;
                return false;
            }

            private static bool IsConstLocal(FunctionPlan function, LocalSlotId slot)
            {
                var locals = function.LocalSlots;
                for (var i = 0; i < locals.Length; i++)
                {
                    if (locals[i].Id.Equals(slot))
                    {
                        return (locals[i].Flags & BackendSymbolFlags.Const) != 0;
                    }
                }

                return false;
            }

            private static bool IsIncrementOrDecrement(Operator op)
            {
                return op == Operator.PreIncrement ||
                    op == Operator.PostIncrement ||
                    op == Operator.PreDecrement ||
                    op == Operator.PostDecrement;
            }

            private static void ThrowConstAssignment(string name, NameExpression target)
            {
                throw new AuroraCompilationException(
                    AuroraCompilationStage.Binding,
                    target,
                    $"Cannot assign to constant '{name}'.");
            }

            private static Dictionary<FunctionDeclaration, FunctionPlan> BuildFunctionMap(ModulePlan modulePlan)
            {
                var map = new Dictionary<FunctionDeclaration, FunctionPlan>(
                    modulePlan.Functions.Count,
                    ReferenceEqualityComparer.Instance);
                for (var i = 0; i < modulePlan.Functions.Count; i++)
                {
                    var function = modulePlan.Functions[i];
                    if (function.Declaration != null)
                    {
                        map[function.Declaration] = function;
                    }
                }

                return map;
            }
        }

        private readonly struct ChildVisitor : IAstChildVisitor
        {
            private readonly Analyzer _analyzer;

            public ChildVisitor(Analyzer analyzer)
            {
                _analyzer = analyzer;
            }

            public void Visit(AstNode node)
            {
                _analyzer.Visit(node);
            }
        }
    }
}
