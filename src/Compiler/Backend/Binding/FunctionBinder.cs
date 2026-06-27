using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Binding
{
    internal static class FunctionBinder
    {
        public static FunctionPlanRegistry RegisterNestedFunctions(CompileSession session, ModulePlan modulePlan)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(modulePlan);

            var functions = new FunctionPlanRegistry(modulePlan);
            RegisterModuleInitializerFunctions(session, modulePlan, functions);
            return functions;
        }

        public static void BindFunctionBodies(
            CompileSession session,
            ModulePlan modulePlan,
            FunctionPlanRegistry functions)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(modulePlan);
            ArgumentNullException.ThrowIfNull(functions);

            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                BindFunction(session, modulePlan, modulePlan.Functions[i], functions);
            }
        }

        private static void RegisterModuleInitializerFunctions(
            CompileSession session,
            ModulePlan modulePlan,
            FunctionPlanRegistry functions)
        {
            var collector = new ModuleInitializerFunctionCollector(session, modulePlan, functions);
            for (var i = 0; i < modulePlan.Declaration.Length; i++)
            {
                collector.Visit(modulePlan.Declaration[i]);
            }
        }

        private static void BindFunction(
            CompileSession session,
            ModulePlan modulePlan,
            FunctionPlan function,
            FunctionPlanRegistry functions)
        {
            if (function.Declaration == null)
            {
                return;
            }

            var binder = new FunctionBodyBinder(session, modulePlan, function, functions);
            binder.Bind();
        }

        internal sealed class FunctionPlanRegistry
        {
            private readonly ModulePlan _modulePlan;
            private Dictionary<FunctionDeclaration, FunctionPlan> _map;

            public FunctionPlanRegistry(ModulePlan modulePlan)
            {
                _modulePlan = modulePlan ?? throw new ArgumentNullException(nameof(modulePlan));
            }

            public bool TryGetValue(FunctionDeclaration declaration, out FunctionPlan function)
            {
                if (declaration == null)
                {
                    function = null;
                    return false;
                }

                return EnsureMap().TryGetValue(declaration, out function);
            }

            public void Add(FunctionDeclaration declaration, FunctionPlan function)
            {
                EnsureMap().Add(declaration, function);
            }

            private Dictionary<FunctionDeclaration, FunctionPlan> EnsureMap()
            {
                if (_map != null)
                {
                    return _map;
                }

                var map = new Dictionary<FunctionDeclaration, FunctionPlan>(
                    Math.Max(4, _modulePlan.Functions.Count),
                    ReferenceEqualityComparer.Instance);
                for (var i = 0; i < _modulePlan.Functions.Count; i++)
                {
                    var function = _modulePlan.Functions[i];
                    if (function.Declaration != null)
                    {
                        map[function.Declaration] = function;
                    }
                }

                _map = map;
                return map;
            }
        }

        private sealed class ModuleInitializerFunctionCollector
        {
            private readonly CompileSession _session;
            private readonly ModulePlan _modulePlan;
            private readonly FunctionPlanRegistry _functions;

            public ModuleInitializerFunctionCollector(
                CompileSession session,
                ModulePlan modulePlan,
                FunctionPlanRegistry functions)
            {
                _session = session;
                _modulePlan = modulePlan;
                _functions = functions;
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
                        Register(function);
                        return;
                    case LambdaExpression lambda:
                        Register(lambda.Function);
                        return;
                }

                var visitor = new ModuleInitializerChildVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private void Register(FunctionDeclaration declaration)
            {
                if (declaration == null ||
                    declaration.Flags == FunctionFlags.Declare ||
                    _functions.TryGetValue(declaration, out _))
                {
                    return;
                }

                var functionId = _session.AllocateFunctionId();
                var functionScope = _session.Scopes.Add(new ScopeInfo(
                    _modulePlan.ModuleScope,
                    _modulePlan.Id,
                    functionId,
                    BackendScopeKind.Function));
                var plan = new FunctionPlan(functionId, _modulePlan.Id, functionScope, declaration, FunctionVisibility.InternalOnly, isModuleFunction: false);
                _modulePlan.AddFunction(plan);
                _functions.Add(declaration, plan);
            }
        }

        private sealed class FunctionBodyBinder
        {
            private readonly CompileSession _session;
            private readonly ModulePlan _modulePlan;
            private readonly FunctionPlan _function;
            private readonly FunctionPlanRegistry _functions;
            private LocalSlotBuilder _localSlots;
            private List<LocalScope> _localScopes;
            private Dictionary<AstNode, int> _localScopeByNode;
            private Stack<int> _scopeStack;
            private List<FunctionId> _nestedFunctions;

            public FunctionBodyBinder(
                CompileSession session,
                ModulePlan modulePlan,
                FunctionPlan function,
                FunctionPlanRegistry functions)
            {
                _session = session;
                _modulePlan = modulePlan;
                _function = function;
                _functions = functions;
                _localSlots = new LocalSlotBuilder(Math.Max(4, function.Declaration?.Parameters.Count ?? 0));
                _localScopes = new List<LocalScope>();
                _localScopeByNode = new Dictionary<AstNode, int>(ReferenceEqualityComparer.Instance);
                _scopeStack = new Stack<int>();
            }

            public void Bind()
            {
                var declaration = _function.Declaration;
                var rootScope = AddLocalScope(-1, declaration.Body ?? declaration);
                _scopeStack.Push(rootScope);
                try
                {
                    if (declaration.Name != null && declaration.Flags == FunctionFlags.Lambda)
                    {
                        DeclareLocal(declaration.Name.Value, BackendSymbolKind.Local, declaration.Access, declaration, false);
                    }

                    for (var i = 0; i < declaration.Parameters.Count; i++)
                    {
                        var parameter = declaration.Parameters[i];
                        if (parameter.Initializer != null)
                        {
                            _function.HasDefaultParameters = true;
                        }
                        if (parameter.Name != null)
                        {
                            DeclareLocal(parameter.Name.Value, BackendSymbolKind.Parameter, MemberAccess.Internal, parameter, true);
                        }
                    }

                    CollectDeclarations(declaration.Body);
                }
                finally
                {
                    _scopeStack.Pop();
                }

                _function.LocalScopes = _localScopes.ToArray();
                _function.LocalScopeByNode = _localScopeByNode;
                _function.LocalSlots = _localSlots.ToArray();
                if (_nestedFunctions != null)
                {
                    _function.NestedFunctions = _nestedFunctions.ToArray();
                }
            }

            private int CurrentScopeId => _scopeStack.Peek();

            private int AddLocalScope(int parentId, AstNode owner)
            {
                var scopeId = _localScopes.Count;
                _localScopes.Add(new LocalScope(scopeId, parentId, owner));
                if (owner != null && !_localScopeByNode.ContainsKey(owner))
                {
                    _localScopeByNode.Add(owner, scopeId);
                }
                return scopeId;
            }

            private void EnterLocalScope(AstNode owner)
            {
                _scopeStack.Push(AddLocalScope(CurrentScopeId, owner));
            }

            private void ExitLocalScope()
            {
                _scopeStack.Pop();
            }

            private bool IsRootBody(BlockStatement block)
            {
                return ReferenceEquals(block, _function.Declaration?.Body);
            }

            private void CollectBlock(BlockStatement block, bool createScope)
            {
                if (createScope)
                {
                    EnterLocalScope(block);
                }

                try
                {
                    for (var i = 0; i < block.Functions.Count; i++)
                    {
                        CollectDeclarations(block.Functions[i]);
                    }
                    for (var i = 0; i < block.Length; i++)
                    {
                        CollectDeclarations(block[i]);
                    }
                }
                finally
                {
                    if (createScope)
                    {
                        ExitLocalScope();
                    }
                }
            }

            public void CollectDeclarations(AstNode node)
            {
                if (node == null)
                {
                    return;
                }

                switch (node)
                {
                    case BlockStatement block:
                        CollectBlock(block, createScope: !IsRootBody(block));
                        return;
                    case VariableDeclaration variable:
                        DeclarePattern(variable);
                        CollectDeclarations(variable.Initializer);
                        return;
                    case FunctionDeclaration nested when !ReferenceEquals(nested, _function.Declaration):
                        DeclareNestedFunction(nested, declareName: true);
                        return;
                    case LambdaExpression lambda:
                        DeclareNestedFunction(lambda.Function, declareName: false);
                        return;
                    case NameExpression name when name.Identifier?.Value == "$args":
                        _function.UsesArgumentsObject = true;
                        return;
                    case TryStatement tryStatement:
                        CollectDeclarations(tryStatement.Body);
                        CollectCatchBlock(tryStatement);
                        CollectDeclarations(tryStatement.FinallyBody);
                        return;
                    case ObjectDestructuringPattern:
                    case ArrayDestructuringPattern:
                        return;
                }

                var visitor = new DeclarationVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private void CollectCatchBlock(TryStatement statement)
            {
                if (statement.CatchBody == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(statement.CatchVariable))
                {
                    CollectDeclarations(statement.CatchBody);
                    return;
                }

                var owner = statement.CatchBody is BlockStatement catchBlock ? (AstNode)catchBlock : statement;
                EnterLocalScope(owner);
                try
                {
                    DeclareCatchVariable(statement);
                    if (statement.CatchBody is BlockStatement block)
                    {
                        CollectBlock(block, createScope: false);
                    }
                    else
                    {
                        CollectDeclarations(statement.CatchBody);
                    }
                }
                finally
                {
                    ExitLocalScope();
                }
            }

            private void DeclareCatchVariable(TryStatement statement)
            {
                if (!string.IsNullOrEmpty(statement.CatchVariable))
                {
                    DeclareLocal(statement.CatchVariable, BackendSymbolKind.Local, MemberAccess.Internal, statement, false);
                }
            }

            private void DeclareNestedFunction(FunctionDeclaration declaration, bool declareName)
            {
                if (declaration == null || declaration.Flags == FunctionFlags.Declare)
                {
                    return;
                }

                if (declareName && declaration.Name != null)
                {
                    DeclareLocal(declaration.Name.Value, BackendSymbolKind.Local, declaration.Access, declaration, false);
                }

                var nestedPlan = EnsureNestedFunction(declaration);
                nestedPlan.ParentLocalScopeId = CurrentScopeId;
                (_nestedFunctions ??= new List<FunctionId>()).Add(nestedPlan.Id);
            }

            private FunctionPlan EnsureNestedFunction(FunctionDeclaration declaration)
            {
                if (_functions.TryGetValue(declaration, out var existing))
                {
                    return existing;
                }

                var functionId = _session.AllocateFunctionId();
                var functionScope = _session.Scopes.Add(new ScopeInfo(
                    _function.Scope,
                    _modulePlan.Id,
                    functionId,
                    BackendScopeKind.Function));
                var plan = new FunctionPlan(functionId, _modulePlan.Id, functionScope, declaration, FunctionVisibility.InternalOnly, isModuleFunction: false);
                _modulePlan.AddFunction(plan);
                _functions.Add(declaration, plan);
                return plan;
            }

            private void DeclarePattern(VariableDeclaration variable)
            {
                if (variable.Name != null)
                {
                    DeclareLocal(variable.Name.Value, BackendSymbolKind.Local, variable.Access, variable, false);
                    return;
                }

                DeclarePattern(variable.Pattern, variable);
            }

            private void DeclarePattern(Expression pattern, VariableDeclaration declaration)
            {
                switch (pattern)
                {
                    case NameExpression name:
                        DeclareLocal(name.Identifier.Value, BackendSymbolKind.Local, declaration.Access, declaration, false);
                        return;
                    case SpreadExpression { Expression: NameExpression spreadName }:
                        DeclareLocal(spreadName.Identifier.Value, BackendSymbolKind.Local, declaration.Access, declaration, false);
                        return;
                    case ObjectDestructuringPattern objectPattern:
                        for (var i = 0; i < objectPattern.Properties.Count; i++)
                        {
                            DeclareLocal(objectPattern.Properties[i].Value, BackendSymbolKind.Local, declaration.Access, declaration, false);
                        }
                        return;
                    case ArrayDestructuringPattern arrayPattern:
                        for (var i = 0; i < arrayPattern.Elements.Count; i++)
                        {
                            DeclarePattern(arrayPattern.Elements[i], declaration);
                        }
                        return;
                }
            }

            private void DeclareLocal(string name, BackendSymbolKind kind, MemberAccess access, AstNode declaration, bool isParameter)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return;
                }

                if (TryGetConflictingLocal(name, CurrentScopeId, out var existing))
                {
                    ThrowDuplicateLocal(name, existing, declaration);
                }

                var slot = new LocalSlot(
                    new LocalSlotId(_localSlots.Count),
                    CurrentScopeId,
                    name,
                    kind,
                    isParameter ? BackendSymbolFlags.None : GetLocalFlags(declaration),
                    access,
                    declaration,
                    typeof(ScriptDatum),
                    isParameter);
                _localSlots.Add(slot);
            }

            private bool TryGetConflictingLocal(string name, int scopeId, out LocalSlot slot)
            {
                if (TryGetLocalInScope(name, scopeId, out slot))
                {
                    return true;
                }

                scopeId = GetParentScopeId(scopeId);
                while (scopeId >= 0)
                {
                    if (TryGetLocalInScope(name, scopeId, out slot) &&
                        (slot.Flags & BackendSymbolFlags.Const) != 0)
                    {
                        return true;
                    }

                    scopeId = GetParentScopeId(scopeId);
                }

                slot = default;
                return false;
            }

            private bool TryGetLocalInScope(string name, int scopeId, out LocalSlot slot)
            {
                for (var i = 0; i < _localSlots.Count; i++)
                {
                    if (_localSlots[i].ScopeId == scopeId &&
                        string.Equals(_localSlots[i].Name, name, StringComparison.Ordinal))
                    {
                        slot = _localSlots[i];
                        return true;
                    }
                }

                slot = default;
                return false;
            }

            private int GetParentScopeId(int scopeId)
            {
                return (uint)scopeId < (uint)_localScopes.Count ? _localScopes[scopeId].ParentId : -1;
            }

            private static void ThrowDuplicateLocal(string name, LocalSlot existing, AstNode declaration)
            {
                var existingLocation = FormatLocation(existing.Declaration?.Range ?? SourceSpan.None);
                var scopeName = existing.ScopeId == 0 ? "function scope" : "block scope";
                throw new AuroraCompilationException(AuroraCompilationStage.Binding, 
                    declaration ?? existing.Declaration,
                    $"Duplicate declaration '{name}' in {scopeName}. Previous declaration: {existingLocation}.");
            }

            private static string FormatLocation(SourceSpan range)
            {
                if (string.IsNullOrEmpty(range.FileName))
                {
                    return $"line:{range.StartLine}, column:{range.StartColumn}";
                }

                return $"{range.FileName} line:{range.StartLine}, column:{range.StartColumn}";
            }

            private static BackendSymbolFlags GetLocalFlags(AstNode declaration)
            {
                return declaration is VariableDeclaration { IsConst: true }
                    ? BackendSymbolFlags.Const
                    : BackendSymbolFlags.None;
            }
        }

        private struct LocalSlotBuilder
        {
            private LocalSlot[] _items;

            public LocalSlotBuilder(int capacity)
            {
                _items = capacity == 0 ? Array.Empty<LocalSlot>() : new LocalSlot[capacity];
                Count = 0;
            }

            public int Count { get; private set; }

            public LocalSlot this[int index] => _items[index];

            public void Add(LocalSlot slot)
            {
                if (Count == _items.Length)
                {
                    Grow();
                }

                _items[Count++] = slot;
            }

            public LocalSlot[] ToArray()
            {
                if (Count == 0)
                {
                    return Array.Empty<LocalSlot>();
                }

                if (Count == _items.Length)
                {
                    return _items;
                }

                var result = new LocalSlot[Count];
                Array.Copy(_items, result, Count);
                return result;
            }

            private void Grow()
            {
                var newSize = _items.Length == 0 ? 4 : _items.Length * 2;
                var replacement = new LocalSlot[newSize];
                if (Count != 0)
                {
                    Array.Copy(_items, replacement, Count);
                }

                _items = replacement;
            }
        }

        private readonly struct ModuleInitializerChildVisitor : IAstChildVisitor
        {
            private readonly ModuleInitializerFunctionCollector _collector;

            public ModuleInitializerChildVisitor(ModuleInitializerFunctionCollector collector)
            {
                _collector = collector;
            }

            public void Visit(AstNode node)
            {
                _collector.Visit(node);
            }
        }

        private readonly struct DeclarationVisitor : IAstChildVisitor
        {
            private readonly FunctionBodyBinder _binder;

            public DeclarationVisitor(FunctionBodyBinder binder)
            {
                _binder = binder;
            }

            public void Visit(AstNode node)
            {
                _binder.CollectDeclarations(node);
            }
        }

    }
}
