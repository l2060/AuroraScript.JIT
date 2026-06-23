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
        public static Dictionary<FunctionDeclaration, FunctionPlan> RegisterNestedFunctions(CompileSession session, ModulePlan modulePlan)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(modulePlan);

            var functionsByDeclaration = BuildFunctionMap(modulePlan);
            RegisterModuleInitializerFunctions(session, modulePlan, functionsByDeclaration);
            return functionsByDeclaration;
        }

        public static void BindFunctionBodies(
            CompileSession session,
            ModulePlan modulePlan,
            Dictionary<FunctionDeclaration, FunctionPlan> functionsByDeclaration)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(modulePlan);
            ArgumentNullException.ThrowIfNull(functionsByDeclaration);

            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                BindFunction(session, modulePlan, modulePlan.Functions[i], functionsByDeclaration);
            }
        }

        private static Dictionary<FunctionDeclaration, FunctionPlan> BuildFunctionMap(ModulePlan modulePlan)
        {
            var functionsByDeclaration = new Dictionary<FunctionDeclaration, FunctionPlan>(
                Math.Max(4, modulePlan.Functions.Count),
                ReferenceEqualityComparer.Instance);
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                functionsByDeclaration[modulePlan.Functions[i].Declaration] = modulePlan.Functions[i];
            }
            return functionsByDeclaration;
        }

        private static void RegisterModuleInitializerFunctions(
            CompileSession session,
            ModulePlan modulePlan,
            Dictionary<FunctionDeclaration, FunctionPlan> functionsByDeclaration)
        {
            var collector = new ModuleInitializerFunctionCollector(session, modulePlan, functionsByDeclaration);
            for (var i = 0; i < modulePlan.Declaration.Length; i++)
            {
                collector.Visit(modulePlan.Declaration[i]);
            }
        }

        private static void BindFunction(
            CompileSession session,
            ModulePlan modulePlan,
            FunctionPlan function,
            Dictionary<FunctionDeclaration, FunctionPlan> functionsByDeclaration)
        {
            if (function.Declaration == null)
            {
                return;
            }

            var binder = new FunctionBodyBinder(session, modulePlan, function, functionsByDeclaration);
            binder.Bind();
        }

        private sealed class ModuleInitializerFunctionCollector
        {
            private readonly CompileSession _session;
            private readonly ModulePlan _modulePlan;
            private readonly Dictionary<FunctionDeclaration, FunctionPlan> _functionsByDeclaration;

            public ModuleInitializerFunctionCollector(
                CompileSession session,
                ModulePlan modulePlan,
                Dictionary<FunctionDeclaration, FunctionPlan> functionsByDeclaration)
            {
                _session = session;
                _modulePlan = modulePlan;
                _functionsByDeclaration = functionsByDeclaration;
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
                    _functionsByDeclaration.ContainsKey(declaration))
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
                _functionsByDeclaration.Add(declaration, plan);
            }
        }

        private sealed class FunctionBodyBinder
        {
            private readonly CompileSession _session;
            private readonly ModulePlan _modulePlan;
            private readonly FunctionPlan _function;
            private readonly Dictionary<FunctionDeclaration, FunctionPlan> _functionsByDeclaration;
            private readonly List<LocalSlot> _localSlots = new();
            private List<FunctionId> _nestedFunctions;

            public FunctionBodyBinder(
                CompileSession session,
                ModulePlan modulePlan,
                FunctionPlan function,
                Dictionary<FunctionDeclaration, FunctionPlan> functionsByDeclaration)
            {
                _session = session;
                _modulePlan = modulePlan;
                _function = function;
                _functionsByDeclaration = functionsByDeclaration;
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
                if (_functionsByDeclaration.TryGetValue(declaration, out var existing))
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
                _functionsByDeclaration.Add(declaration, plan);
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
