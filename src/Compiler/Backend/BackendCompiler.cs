using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Analysis;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Lowering;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Emits.Builders;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.Compiler.Backend
{
    internal sealed class BackendCompiler
    {
        private readonly AbstractCILBuilder _builder;
        private readonly EngineOptions _options;

        public BackendCompiler(AbstractCILBuilder builder, EngineOptions options)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public CompileSession CreateSession(CancellationToken cancellationToken = default)
        {
            return new CompileSession(_options, cancellationToken);
        }

        public CompileSession CreateModulePlans(ModuleDeclaration[] modules, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(modules);

            var session = CreateSession(cancellationToken);
            var plans = new ModulePlan[modules.Length];
            for (var i = 0; i < modules.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var moduleId = new ModuleId(i);
                var module = modules[i] ?? throw new ArgumentException("Module collection cannot contain null.", nameof(modules));
                var modulePlan = new ModulePlan(moduleId, module);
                plans[i] = modulePlan;
                PredefineModule(session, modulePlan);
            }
            session.Modules = plans;
            var functionMaps = RegisterNestedFunctions(session, plans, cancellationToken);
            AnalyzeModules(session, plans, functionMaps, cancellationToken);
            return session;
        }

        private static Dictionary<FunctionDeclaration, FunctionPlan>[] RegisterNestedFunctions(
            CompileSession session,
            ModulePlan[] plans,
            CancellationToken cancellationToken)
        {
            var functionMaps = new Dictionary<FunctionDeclaration, FunctionPlan>[plans.Length];
            for (var i = 0; i < plans.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                functionMaps[i] = FunctionBinder.RegisterNestedFunctions(session, plans[i]);
            }
            return functionMaps;
        }

        private static void AnalyzeModules(
            CompileSession session,
            ModulePlan[] plans,
            Dictionary<FunctionDeclaration, FunctionPlan>[] functionMaps,
            CancellationToken cancellationToken)
        {
            if (!session.Capabilities.CanAnalyzeModulesInParallel || plans.Length <= 1)
            {
                for (var i = 0; i < plans.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ModuleUsageAnalyzer.Apply(session, plans[i]);
                    FunctionBinder.BindFunctionBodies(session, plans[i], functionMaps[i]);
                    ClosurePlanner.PlanModule(plans[i]);
                    FunctionLowerer.LowerModule(plans[i], functionMaps[i]);
                }
                return;
            }

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken
            };
            Parallel.For(0, plans.Length, parallelOptions, i =>
            {
                ModuleUsageAnalyzer.Apply(session, plans[i]);
                FunctionBinder.BindFunctionBodies(session, plans[i], functionMaps[i]);
                ClosurePlanner.PlanModule(plans[i]);
                FunctionLowerer.LowerModule(plans[i], functionMaps[i]);
            });
        }

        private static void PredefineModule(CompileSession session, ModulePlan modulePlan)
        {
            var moduleScope = session.Scopes.Add(new ScopeInfo(
                ScopeId.Invalid,
                modulePlan.Id,
                FunctionId.Invalid,
                BackendScopeKind.Module));
            modulePlan.ModuleScope = moduleScope;

            var firstSymbol = new SymbolId(session.Symbols.Count);
            var symbolCount = 0;
            var module = modulePlan.Declaration;

            for (var i = 0; i < module.Imports.Count; i++)
            {
                var import = module.Imports[i];
                if (import.Include)
                {
                    symbolCount += AddIncludedExportSymbols(session, modulePlan, moduleScope, import.Module);
                    continue;
                }

                if (import.Name == null)
                {
                    continue;
                }
                if (AddModuleSymbol(session, modulePlan, moduleScope, import.Name.Value, BackendSymbolKind.ImportAlias, BackendSymbolFlags.Imported | BackendSymbolFlags.ModuleVisible, import))
                {
                    symbolCount++;
                }
            }

            for (var i = 0; i < module.Length; i++)
            {
                var child = module[i];
                switch (child)
                {
                    case VariableDeclaration variable:
                        symbolCount += AddVariableSymbols(session, modulePlan, moduleScope, variable);
                        break;
                    case EnumDeclaration enumDeclaration when enumDeclaration.Identifier != null:
                        if (AddModuleSymbol(session, modulePlan, moduleScope, enumDeclaration.Identifier.Value, BackendSymbolKind.Enum, GetAccessFlags(enumDeclaration.Access), enumDeclaration))
                        {
                            symbolCount++;
                        }
                        break;
                }
            }

            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                if (function.Flags == FunctionFlags.Declare || function.Name == null)
                {
                    continue;
                }

                var flags = GetAccessFlags(function.Access);
                if (AddModuleSymbol(session, modulePlan, moduleScope, function.Name.Value, BackendSymbolKind.Function, flags, function))
                {
                    symbolCount++;
                }

                var functionId = session.AllocateFunctionId();
                var functionScope = session.Scopes.Add(new ScopeInfo(
                    moduleScope,
                    modulePlan.Id,
                    functionId,
                    BackendScopeKind.Function));
                var visibility = function.Access == MemberAccess.Export
                    ? FunctionVisibility.Exported
                    : FunctionVisibility.ModuleVisible;
                modulePlan.AddFunction(new FunctionPlan(functionId, modulePlan.Id, functionScope, function, visibility, isModuleFunction: true));
            }

            session.Scopes[moduleScope] = session.Scopes[moduleScope].WithSymbolRange(firstSymbol, symbolCount);
        }

        private static int AddIncludedExportSymbols(CompileSession session, ModulePlan modulePlan, ScopeId moduleScope, ModuleDeclaration includedModule)
        {
            if (includedModule == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < includedModule.Length; i++)
            {
                var child = includedModule[i];
                switch (child)
                {
                    case VariableDeclaration variable when variable.Access == MemberAccess.Export:
                        count += AddVariableSymbols(session, modulePlan, moduleScope, variable);
                        break;
                    case EnumDeclaration enumDeclaration when enumDeclaration.Access == MemberAccess.Export && enumDeclaration.Identifier != null:
                        if (AddModuleSymbol(session, modulePlan, moduleScope, enumDeclaration.Identifier.Value, BackendSymbolKind.Enum, GetAccessFlags(enumDeclaration.Access), enumDeclaration))
                        {
                            count++;
                        }
                        break;
                }
            }

            for (var i = 0; i < includedModule.Functions.Count; i++)
            {
                var function = includedModule.Functions[i];
                if (function.Access == MemberAccess.Export && function.Flags != FunctionFlags.Declare && function.Name != null)
                {
                    if (AddModuleSymbol(session, modulePlan, moduleScope, function.Name.Value, BackendSymbolKind.Function, GetAccessFlags(function.Access), function))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private static int AddVariableSymbols(CompileSession session, ModulePlan modulePlan, ScopeId moduleScope, VariableDeclaration variable)
        {
            var flags = GetAccessFlags(variable.Access);
            if (variable.IsConst)
            {
                flags |= BackendSymbolFlags.Const;
            }

            if (variable.Name != null)
            {
                return AddModuleSymbol(session, modulePlan, moduleScope, variable.Name.Value, BackendSymbolKind.ModuleProperty, flags, variable) ? 1 : 0;
            }

            return AddPatternSymbols(session, modulePlan, moduleScope, variable.Pattern, flags, variable);
        }

        private static int AddPatternSymbols(
            CompileSession session,
            ModulePlan modulePlan,
            ScopeId moduleScope,
            Expression pattern,
            BackendSymbolFlags flags,
            AstNode declaration)
        {
            switch (pattern)
            {
                case NameExpression name:
                    return AddModuleSymbol(session, modulePlan, moduleScope, name.Identifier.Value, BackendSymbolKind.ModuleProperty, flags, declaration) ? 1 : 0;
                case SpreadExpression { Expression: NameExpression spreadName }:
                    return AddModuleSymbol(session, modulePlan, moduleScope, spreadName.Identifier.Value, BackendSymbolKind.ModuleProperty, flags, declaration) ? 1 : 0;
                case ObjectDestructuringPattern objectPattern:
                    var objectCount = 0;
                    for (var i = 0; i < objectPattern.Properties.Count; i++)
                    {
                        if (AddModuleSymbol(session, modulePlan, moduleScope, objectPattern.Properties[i].Value, BackendSymbolKind.ModuleProperty, flags, declaration))
                        {
                            objectCount++;
                        }
                    }
                    return objectCount;
                case ArrayDestructuringPattern arrayPattern:
                    var count = 0;
                    for (var i = 0; i < arrayPattern.Elements.Count; i++)
                    {
                        count += AddPatternSymbols(session, modulePlan, moduleScope, arrayPattern.Elements[i], flags, declaration);
                    }
                    return count;
                default:
                    return 0;
            }
        }

        private static bool AddModuleSymbol(
            CompileSession session,
            ModulePlan modulePlan,
            ScopeId moduleScope,
            string name,
            BackendSymbolKind kind,
            BackendSymbolFlags flags,
            AstNode declaration)
        {
            if (modulePlan.TryGetSymbol(name, out _))
            {
                return false;
            }

            var symbolId = session.Symbols.Add(new SymbolInfo(
                name,
                kind,
                flags,
                moduleScope,
                modulePlan.Id,
                FunctionId.Invalid,
                declaration is FunctionDeclaration function ? function.Access :
                declaration is VariableDeclaration variable ? variable.Access :
                declaration is EnumDeclaration enumDeclaration ? enumDeclaration.Access :
                MemberAccess.Internal,
                declaration));
            modulePlan.TryDeclareSymbol(name, symbolId);
            return true;
        }

        private static BackendSymbolFlags GetAccessFlags(MemberAccess access)
        {
            return access == MemberAccess.Export
                ? BackendSymbolFlags.Exported | BackendSymbolFlags.ModuleVisible
                : BackendSymbolFlags.ModuleVisible;
        }
    }
}
