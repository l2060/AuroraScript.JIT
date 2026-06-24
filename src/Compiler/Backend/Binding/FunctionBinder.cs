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
            }

            public void Bind()
            {
                var declaration = _function.Declaration;
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
                _function.LocalSlots = _localSlots.ToArray();
                if (_nestedFunctions != null)
                {
                    _function.NestedFunctions = _nestedFunctions.ToArray();
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
                        DeclareCatchVariable(tryStatement);
                        CollectDeclarations(tryStatement.Body);
                        CollectDeclarations(tryStatement.CatchBody);
                        CollectDeclarations(tryStatement.FinallyBody);
                        return;
                    case ObjectDestructuringPattern:
                    case ArrayDestructuringPattern:
                        return;
                }

                var visitor = new DeclarationVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
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
                if (string.IsNullOrEmpty(name) || HasLocal(name))
                {
                    return;
                }

                var slot = new LocalSlot(
                    new LocalSlotId(_localSlots.Count),
                    name,
                    kind,
                    isParameter ? BackendSymbolFlags.None : GetLocalFlags(declaration),
                    access,
                    declaration,
                    typeof(ScriptDatum),
                    isParameter);
                _localSlots.Add(slot);
            }

            private bool HasLocal(string name)
            {
                for (var i = 0; i < _localSlots.Count; i++)
                {
                    if (string.Equals(_localSlots[i].Name, name, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
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
