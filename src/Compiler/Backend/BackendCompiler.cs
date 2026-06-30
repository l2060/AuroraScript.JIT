using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Analysis;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Lowering;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.GlobalDeclarations;
using AuroraScript.Tokens;
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
        private readonly GlobalDeclarationIndex _globalDeclarations;

        public BackendCompiler(AbstractCILBuilder builder, EngineOptions options)
            : this(builder, options, GlobalDeclarationIndex.Empty)
        {
        }

        public BackendCompiler(
            AbstractCILBuilder builder,
            EngineOptions options,
            GlobalDeclarationIndex globalDeclarations)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _globalDeclarations = globalDeclarations ?? GlobalDeclarationIndex.Empty;
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
                PredefineModule(session, modulePlan, _globalDeclarations);
            }
            session.Modules = plans;
            AnalyzeModuleConstants(session, plans, cancellationToken);
            var functionMaps = RegisterNestedFunctions(session, plans, cancellationToken);
            AnalyzeModules(session, plans, functionMaps, cancellationToken);
            ValidateConstAssignments(session, plans, cancellationToken);
            return session;
        }

        public CompileBlockPlan CreateCompileBlockPlan(BlockStatement body, IReadOnlyList<string> parameters, string sourceName, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(body);

            var blockPlan = new CompileBlockPlan(body, parameters, sourceName);
            var session = CreateCompileBlockSession(cancellationToken);
            var modulePlan = new ModulePlan(new ModuleId(0), CreateCompileBlockModule(body, blockPlan.Parameters, blockPlan.SourceName));
            PredefineCompileBlockModule(session, modulePlan);
            session.Modules = [modulePlan];

            var functionMaps = RegisterNestedFunctions(session, [modulePlan], cancellationToken);
            FunctionBinder.BindFunctionBodies(session, modulePlan, functionMaps[0]);
            ClosurePlanner.PlanModule(modulePlan);
            ValidateConstAssignments(session, [modulePlan], cancellationToken);
            FunctionLowerer.LowerModule(modulePlan, functionMaps[0]);

            blockPlan.Session = session;
            blockPlan.Module = modulePlan;
            blockPlan.Function = modulePlan.Functions[0];
            return blockPlan;
        }

        private CompileSession CreateCompileBlockSession(CancellationToken cancellationToken)
        {
            return new CompileSession(
                _options,
                CompilationModeCapabilities.FromOptions(_options).WithoutModuleDirectCall(),
                cancellationToken);
        }

        private static void PredefineCompileBlockModule(CompileSession session, ModulePlan modulePlan)
        {
            var moduleScope = session.Scopes.Add(new ScopeInfo(
                ScopeId.Invalid,
                modulePlan.Id,
                FunctionId.Invalid,
                BackendScopeKind.CompileBlock));
            modulePlan.ModuleScope = moduleScope;

            var function = modulePlan.Declaration.Functions[0];
            var functionId = session.AllocateFunctionId();
            var functionScope = session.Scopes.Add(new ScopeInfo(
                moduleScope,
                modulePlan.Id,
                functionId,
                BackendScopeKind.Function));
            modulePlan.AddFunction(new FunctionPlan(
                functionId,
                modulePlan.Id,
                functionScope,
                function,
                FunctionVisibility.Exported,
                isModuleFunction: true));
        }

        public CompileSession CreateHotPatchPlans(
            ModuleDeclaration mainModule,
            ModuleDeclaration[] dependencies,
            IReadOnlyCollection<string> existingMainModuleSymbols,
            out ModulePlan mainModulePlan,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mainModule);
            dependencies ??= Array.Empty<ModuleDeclaration>();

            var modules = new ModuleDeclaration[dependencies.Length + 1];
            Array.Copy(dependencies, modules, dependencies.Length);
            modules[^1] = mainModule;

            var session = CreateSession(cancellationToken);
            var plans = new ModulePlan[modules.Length];
            for (var i = 0; i < modules.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var modulePlan = new ModulePlan(new ModuleId(i), modules[i]);
                plans[i] = modulePlan;
                PredefineModule(session, modulePlan, _globalDeclarations);
            }

            mainModulePlan = plans[^1];
            AddExistingModuleSymbols(session, mainModulePlan, existingMainModuleSymbols);
            session.Modules = plans;
            AnalyzeModuleConstants(session, plans, cancellationToken);

            var functionMaps = RegisterNestedFunctions(session, plans, cancellationToken);
            AnalyzeModules(session, plans, functionMaps, cancellationToken);
            ValidateConstAssignments(session, plans, cancellationToken);
            return session;
        }

        private static void AnalyzeModuleConstants(
            CompileSession session,
            ModulePlan[] plans,
            CancellationToken cancellationToken)
        {
            if (!session.Options.Optimization.EnableModuleConstInlining)
            {
                return;
            }

            for (var i = 0; i < plans.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ModuleConstInliningAnalyzer.Apply(session, plans[i]);
            }
        }

        private static void ValidateConstAssignments(
            CompileSession session,
            ModulePlan[] plans,
            CancellationToken cancellationToken)
        {
            for (var i = 0; i < plans.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ConstAssignmentAnalyzer.Apply(session, plans[i]);
            }
        }

        private static FunctionBinder.FunctionPlanRegistry[] RegisterNestedFunctions(
            CompileSession session,
            ModulePlan[] plans,
            CancellationToken cancellationToken)
        {
            var functionMaps = new FunctionBinder.FunctionPlanRegistry[plans.Length];
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
            FunctionBinder.FunctionPlanRegistry[] functionMaps,
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

        private static void PredefineModule(
            CompileSession session,
            ModulePlan modulePlan,
            GlobalDeclarationIndex globalDeclarations)
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
                AddModuleSymbol(session, modulePlan, moduleScope, import.Name.Value, BackendSymbolKind.ImportAlias, BackendSymbolFlags.Imported | BackendSymbolFlags.ModuleVisible, import);
                symbolCount++;
            }

            for (var i = 0; i < module.Statements.Count; i++)
            {
                var child = module.Statements[i];
                switch (child)
                {
                    case VariableDeclaration variable:
                        symbolCount += AddVariableSymbols(session, modulePlan, moduleScope, variable);
                        break;
                    case EnumDeclaration enumDeclaration when enumDeclaration.Identifier != null:
                        AddModuleSymbol(session, modulePlan, moduleScope, enumDeclaration.Identifier.Value, BackendSymbolKind.Enum, GetAccessFlags(enumDeclaration.Access), enumDeclaration);
                        symbolCount++;
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
                AddModuleSymbol(session, modulePlan, moduleScope, function.Name.Value, BackendSymbolKind.Function, flags, function);
                symbolCount++;

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

            symbolCount += AddGlobalDeclarationSymbols(session, modulePlan, moduleScope, globalDeclarations);

            session.Scopes[moduleScope] = session.Scopes[moduleScope].WithSymbolRange(firstSymbol, symbolCount);
        }

        private static int AddGlobalDeclarationSymbols(
            CompileSession session,
            ModulePlan modulePlan,
            ScopeId moduleScope,
            GlobalDeclarationIndex globalDeclarations)
        {
            var count = 0;
            foreach (var declaration in globalDeclarations.Declarations.Values)
            {
                if (modulePlan.TryGetSymbol(declaration.Name, out _))
                {
                    continue;
                }

                var flags = BackendSymbolFlags.DeclaredOnly | BackendSymbolFlags.ModuleVisible;
                if (declaration.Kind == GlobalDeclarationKind.Const)
                {
                    flags |= BackendSymbolFlags.Const;
                }

                AddModuleSymbol(
                    session,
                    modulePlan,
                    moduleScope,
                    declaration.Name,
                    declaration.Kind == GlobalDeclarationKind.Function
                        ? BackendSymbolKind.Function
                        : BackendSymbolKind.ModuleProperty,
                    flags,
                    CreateAmbientDeclarationNode(declaration));
                count++;
            }

            return count;
        }

        private static AstNode CreateAmbientDeclarationNode(GlobalDeclarationInfo declaration)
        {
            if (declaration.Kind == GlobalDeclarationKind.Function)
            {
                var function = new FunctionDeclaration(
                    MemberAccess.Internal,
                    CreateIdentifier(declaration.Name, declaration.NameRange),
                    Array.Empty<ParameterDeclaration>(),
                    null,
                    FunctionFlags.Declare);
                function.Range = declaration.DeclarationRange;
                return function;
            }

            var variable = new VariableDeclaration(
                MemberAccess.Internal,
                declaration.Kind == GlobalDeclarationKind.Const,
                CreateIdentifier(declaration.Name, declaration.NameRange),
                null)
            {
                IsDeclare = true,
                Range = declaration.DeclarationRange
            };
            return variable;
        }

        private static ModuleDeclaration CreateCompileBlockModule(BlockStatement body, IReadOnlyList<string> parameters, string sourceName)
        {
            var module = new ModuleDeclaration(string.Empty)
            {
                ModuleName = "__compile_block__",
                ModulePath = sourceName,
                FullPath = sourceName
            };
            var function = new FunctionDeclaration(
                MemberAccess.Export,
                CreateIdentifier("__compile_block_entry__", body.Range),
                CreateParameters(parameters),
                body,
                FunctionFlags.General);
            module.AddFunction(function);
            return module;
        }

        private static ParameterDeclaration[] CreateParameters(IReadOnlyList<string> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return Array.Empty<ParameterDeclaration>();
            }

            var result = new ParameterDeclaration[parameters.Count];
            for (var i = 0; i < parameters.Count; i++)
            {
                result[i] = new ParameterDeclaration((byte)i, CreateIdentifier(parameters[i], SourceSpan.None), null);
            }

            return result;
        }

        private static IdentifierToken CreateIdentifier(string value, SourceSpan range)
        {
            return new IdentifierToken
            {
                Value = value,
                Range = range
            };
        }

        private static void AddExistingModuleSymbols(
            CompileSession session,
            ModulePlan modulePlan,
            IReadOnlyCollection<string> existingSymbols)
        {
            if (existingSymbols == null || existingSymbols.Count == 0)
            {
                return;
            }

            var moduleScope = modulePlan.ModuleScope;
            foreach (var name in existingSymbols)
            {
                if (string.IsNullOrEmpty(name) ||
                    modulePlan.TryGetSymbol(name, out _))
                {
                    continue;
                }

                AddModuleSymbol(
                    session,
                    modulePlan,
                    moduleScope,
                    name,
                    BackendSymbolKind.ModuleProperty,
                    BackendSymbolFlags.ModuleVisible,
                    modulePlan.Declaration);
            }
        }

        private static int AddIncludedExportSymbols(CompileSession session, ModulePlan modulePlan, ScopeId moduleScope, ModuleDeclaration includedModule)
        {
            if (includedModule == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < includedModule.Statements.Count; i++)
            {
                var child = includedModule.Statements[i];
                switch (child)
                {
                    case VariableDeclaration variable when variable.Access == MemberAccess.Export:
                        count += AddVariableSymbols(session, modulePlan, moduleScope, variable);
                        break;
                    case EnumDeclaration enumDeclaration when enumDeclaration.Access == MemberAccess.Export && enumDeclaration.Identifier != null:
                        AddModuleSymbol(session, modulePlan, moduleScope, enumDeclaration.Identifier.Value, BackendSymbolKind.Enum, GetAccessFlags(enumDeclaration.Access), enumDeclaration);
                        count++;
                        break;
                }
            }

            for (var i = 0; i < includedModule.Functions.Count; i++)
            {
                var function = includedModule.Functions[i];
                if (function.Access == MemberAccess.Export && function.Flags != FunctionFlags.Declare && function.Name != null)
                {
                    AddModuleSymbol(session, modulePlan, moduleScope, function.Name.Value, BackendSymbolKind.Function, GetAccessFlags(function.Access), function);
                    count++;
                }
            }
            return count;
        }

        private static int AddVariableSymbols(CompileSession session, ModulePlan modulePlan, ScopeId moduleScope, VariableDeclaration variable)
        {
            var flags = GetAccessFlags(variable.Access);
            if (variable.IsDeclare)
            {
                flags |= BackendSymbolFlags.DeclaredOnly;
            }
            if (variable.IsConst)
            {
                flags |= BackendSymbolFlags.Const;
            }

            if (variable.Name != null)
            {
                AddModuleSymbol(session, modulePlan, moduleScope, variable.Name.Value, BackendSymbolKind.ModuleProperty, flags, variable);
                return 1;
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
                    AddModuleSymbol(session, modulePlan, moduleScope, name.Identifier.Value, BackendSymbolKind.ModuleProperty, flags, declaration);
                    return 1;
                case SpreadExpression { Expression: NameExpression spreadName }:
                    AddModuleSymbol(session, modulePlan, moduleScope, spreadName.Identifier.Value, BackendSymbolKind.ModuleProperty, flags, declaration);
                    return 1;
                case ObjectDestructuringPattern objectPattern:
                    var objectCount = 0;
                    for (var i = 0; i < objectPattern.Properties.Count; i++)
                    {
                        AddModuleSymbol(session, modulePlan, moduleScope, objectPattern.Properties[i].Value, BackendSymbolKind.ModuleProperty, flags, declaration);
                        objectCount++;
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

        private static void AddModuleSymbol(
            CompileSession session,
            ModulePlan modulePlan,
            ScopeId moduleScope,
            string name,
            BackendSymbolKind kind,
            BackendSymbolFlags flags,
            AstNode declaration)
        {
            if (modulePlan.TryGetSymbol(name, out var existingSymbolId))
            {
                ThrowDuplicateModuleSymbol(session, name, existingSymbolId, declaration);
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
        }

        private static void ThrowDuplicateModuleSymbol(
            CompileSession session,
            string name,
            SymbolId existingSymbolId,
            AstNode declaration)
        {
            var existing = session.Symbols[existingSymbolId];
            var existingLocation = FormatLocation(existing.Declaration?.Range ?? SourceSpan.None);
            throw new AuroraCompilationException(AuroraCompilationStage.Binding, 
                declaration ?? existing.Declaration,
                $"Duplicate declaration '{name}' in module scope. Previous declaration: {existingLocation}.");
        }

        private static string FormatLocation(SourceSpan range)
        {
            if (string.IsNullOrEmpty(range.FileName))
            {
                return $"line:{range.StartLine}, column:{range.StartColumn}";
            }

            return $"{range.FileName} line:{range.StartLine}, column:{range.StartColumn}";
        }

        private static BackendSymbolFlags GetAccessFlags(MemberAccess access)
        {
            return access == MemberAccess.Export
                ? BackendSymbolFlags.Exported | BackendSymbolFlags.ModuleVisible
                : BackendSymbolFlags.ModuleVisible;
        }
    }
}
