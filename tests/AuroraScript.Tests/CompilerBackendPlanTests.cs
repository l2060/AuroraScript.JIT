using AuroraScript.Compiler;
using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Emission;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Source;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace AuroraScript.Tests;

public sealed class CompilerBackendPlanTests
{
    [Fact]
    public void TypedModuleCodeCarriesNumericEvidenceThroughRecursion()
    {
        var root = Path.GetTempPath();
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var module = Parse(
            """
            @module(TEST);
            @directCall
            func sum(value, total) {
                if (value <= 0) return total;
                return sum(value - 1, total + value);
            }
            export func run() { return sum(100, 0); }
            """,
            root);
        var session = new BackendCompiler(new DynamicBuilder(options), options).CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var sum = Assert.Single(modulePlan.Functions, function => function.Name == "sum");

        var code = TypedModuleCode.Build(modulePlan);

        Assert.Equal(
            new DirectParameterType[]
            {
                new DirectParameterType(FlowValueType.Number),
                new DirectParameterType(FlowValueType.Number)
            },
            code.GetDirectParameters(sum.Id));
        Assert.Equal(FlowValueType.Number, code.GetDirect(sum.Id).ReturnType);
    }

    [Fact]
    public void TypedFunctionCodeKeepsNumericLoopLocalsNative()
    {
        var root = Path.GetTempPath();
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var module = Parse(
            """
            @module(TEST);
            export func run(iterations = 1000) {
                var sum = 0;
                var enabled = true;
                var label = "Aurora";
                enabled = !enabled;
                label += "Script";
                for (var i = 0; i < iterations; i++) {
                    sum = sum + ((i * 3) - (i / 2));
                }
                return sum;
            }
            """,
            root);
        var session = new BackendCompiler(new DynamicBuilder(options), options).CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var function = Assert.Single(modulePlan.Functions);

        var code = TypedFunctionBuilder.Build(modulePlan, function);

        Assert.Equal(FlowValueType.Dynamic, code.GetLocalType(function.LocalSlots.Single(slot => slot.Name == "iterations").Id));
        Assert.Equal(FlowValueType.Number, code.GetLocalType(function.LocalSlots.Single(slot => slot.Name == "sum").Id));
        Assert.Equal(FlowValueType.Number, code.GetLocalType(function.LocalSlots.Single(slot => slot.Name == "i").Id));
        Assert.Equal(FlowValueType.Boolean, code.GetLocalType(function.LocalSlots.Single(slot => slot.Name == "enabled").Id));
        Assert.Equal(FlowValueType.String, code.GetLocalType(function.LocalSlots.Single(slot => slot.Name == "label").Id));
    }

    [Fact]
    public void TypedFunctionCodeUsesLocalNumericArrayFactsOnlyForAdditionDemand()
    {
        var root = Path.GetTempPath();
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var module = Parse(
            """
            @module(TEST);
            export func numericArrayAdd() {
                var values = [41];
                return values[0] + 1;
            }
            """,
            root);
        var session = new BackendCompiler(new DynamicBuilder(options), options).CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var function = Assert.Single(modulePlan.Functions, candidate => candidate.Name == "numericArrayAdd");
        var binary = Assert.IsType<BinaryExpression>(
            GetSingleReturnExpression(modulePlan, "numericArrayAdd"));

        var code = TypedFunctionBuilder.Build(modulePlan, function);

        Assert.Equal(FlowValueType.Number, code.GetExpressionType(binary));
        Assert.Equal(FlowValueType.Dynamic, code.GetExpressionType(binary.Left));
    }

    [Fact]
    public void TypedFunctionCodePromotesOnlyDemandClosedDynamicLocals()
    {
        var root = Path.GetTempPath();
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var module = Parse(
            """
            @module(TEST);
            export func run(value) {
                var numericOnly = value;
                var booleanOnly = value;
                var preserved = value;
                var numericResult = (numericOnly - 1) * 2;
                var booleanResult = 0;
                if (booleanOnly) booleanResult = 1;
                return [numericResult, booleanResult, preserved];
            }
            """,
            root);
        var session = new BackendCompiler(new DynamicBuilder(options), options).CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var function = Assert.Single(modulePlan.Functions);

        var code = TypedFunctionBuilder.Build(modulePlan, function);

        Assert.Equal(
            FlowValueType.Number,
            code.GetLocalType(function.LocalSlots.Single(slot => slot.Name == "numericOnly").Id));
        Assert.Equal(
            FlowValueType.Boolean,
            code.GetLocalType(function.LocalSlots.Single(slot => slot.Name == "booleanOnly").Id));
        Assert.Equal(
            FlowValueType.Dynamic,
            code.GetLocalType(function.LocalSlots.Single(slot => slot.Name == "preserved").Id));
    }

    [Fact]
    public void TypedFunctionCodeCoercesNumericLocalsUsedAsAddsIndexesAndStores()
    {
        var root = Path.GetTempPath();
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var module = Parse(
            """
            @module(TEST);
            export func numericUses(state) {
                var width = state.width;
                var searchId = state.searchId + 1;
                var minCost = state.minCost;
                var current = 1;
                var values = new Int32Array(8);
                if (searchId > 100) searchId = 1;
                state.searchId = searchId;
                values[current + width] = searchId;
                return (current * width) - (width - 1) + minCost;
            }
            export func concatKeepsDynamic(state) {
                var width = state.width;
                return "x" + width;
            }
            export func equalityWithNullKeepsDynamic(value) {
                var key = value;
                return key == null;
            }
            """,
            root);
        var session = new BackendCompiler(new DynamicBuilder(options), options).CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var numericUses = Assert.Single(modulePlan.Functions, function => function.Name == "numericUses");
        var concatKeeps = Assert.Single(modulePlan.Functions, function => function.Name == "concatKeepsDynamic");
        var equalityOnly = Assert.Single(modulePlan.Functions, function => function.Name == "equalityWithNullKeepsDynamic");

        var numericCode = TypedFunctionBuilder.Build(modulePlan, numericUses);
        var concatCode = TypedFunctionBuilder.Build(modulePlan, concatKeeps);
        var equalityCode = TypedFunctionBuilder.Build(modulePlan, equalityOnly);

        Assert.Equal(
            FlowValueType.Number,
            numericCode.GetLocalType(numericUses.LocalSlots.Single(slot => slot.Name == "width").Id));
        Assert.Equal(
            FlowValueType.Number,
            numericCode.GetLocalType(numericUses.LocalSlots.Single(slot => slot.Name == "searchId").Id));
        Assert.Equal(
            FlowValueType.Number,
            numericCode.GetLocalType(numericUses.LocalSlots.Single(slot => slot.Name == "minCost").Id));
        Assert.Equal(
            FlowValueType.Dynamic,
            concatCode.GetLocalType(concatKeeps.LocalSlots.Single(slot => slot.Name == "width").Id));
        Assert.Equal(
            FlowValueType.Dynamic,
            equalityCode.GetLocalType(equalityOnly.LocalSlots.Single(slot => slot.Name == "key").Id));
    }

    [Fact]
    public void TypedModuleCodePropagatesUniversalNumericReturnsAfterMutation()
    {
        var root = Path.GetTempPath();
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var module = Parse(
            """
            @module(TEST);
            @directCall
            func bump(value) {
                value++;
                return value;
            }
            export func run() {
                var length = 0;
                length = bump(length);
                return length;
            }
            """,
            root);
        var session = new BackendCompiler(new DynamicBuilder(options), options).CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var bump = Assert.Single(modulePlan.Functions, function => function.Name == "bump");
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");

        var code = TypedModuleCode.Build(modulePlan);

        Assert.Equal(FlowValueType.Number, code.GetGeneric(bump.Id).ReturnType);
        Assert.Equal(
            FlowValueType.Number,
            code.GetGeneric(run.Id).GetLocalType(
                run.LocalSlots.Single(slot => slot.Name == "length").Id));
    }

    [Fact]
    public void TypedModuleCodeUsesGenericAndDirectViewsForCoercionAbi()
    {
        var root = Path.GetTempPath();
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var module = Parse(
            """
            @module(TEST);
            @directCall
            func numeric(value) {
                return value - 1;
            }
            @directCall
            func relay(value) {
                return numeric(value);
            }
            export func run() {
                return relay(4);
            }
            """,
            root);
        var session = new BackendCompiler(new DynamicBuilder(options), options).CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var numeric = Assert.Single(modulePlan.Functions, function => function.Name == "numeric");

        var code = TypedModuleCode.Build(modulePlan);
        var parameter = Assert.Single(code.GetDirectParameters(numeric.Id));

        Assert.Equal(FlowValueType.Number, parameter.Type);
        Assert.Equal(NativeCoercionKind.ArithmeticNumber, parameter.Coercion);
    }

    [Fact]
    public void GlobalPredefineCreatesModuleAndFunctionPlans()
    {
        var root = Path.GetTempPath();
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var module = Parse(
            """
            @module(TEST);
            const secret = 40;
            func helper(value) { return value + secret; }
            export func run() { return helper(2); }
            """,
            root);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);

        Assert.True(session.Capabilities.CanUseModuleDirectCall);
        var modulePlan = Assert.Single(session.Modules);
        Assert.Equal("TEST", modulePlan.Name);
        Assert.True(modulePlan.ModuleScope.IsValid);
        Assert.Equal(2, modulePlan.Functions.Count);
        Assert.Contains(modulePlan.Functions, function => function.Name == "helper" && function.IsModuleFunction && function.Visibility == FunctionVisibility.InternalOnly && function.IsDirectCallCandidate);
        Assert.Contains(modulePlan.Functions, function => function.Name == "run" && function.IsModuleFunction && function.Visibility == FunctionVisibility.Exported);
        Assert.Equal(3, session.Scopes[modulePlan.ModuleScope].SymbolCount);
        Assert.Equal(2, modulePlan.Functions.Select(function => function.Id.Value).Distinct().Count());
    }

    [Fact]
    public void HotReloadDoesNotDisableModuleDirectCallCapability()
    {
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = true)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);

        var capabilities = CompilationModeCapabilities.FromOptions(options);

        Assert.True(capabilities.CanUseModuleDirectCall);
        Assert.True(capabilities.CanInferAutoModuleDirectCall);
    }

    [Fact]
    public void GlobalPredefineIncludesDestructuringAndLinkedIncludeExports()
    {
        using var workspace = new BackendTestWorkspace();
        var root = workspace.Root;
        workspace.WriteSource("shared.as", "@module(SHARED); export const INCLUDED = 2;");
        var included = Parse(
            """
            @module(SHARED);
            export const INCLUDED = 2;
            const HIDDEN = 40;
            export func visible() { return INCLUDED; }
            """,
            root);
        var main = Parse(
            """
            @module(TEST);
            include 'shared';
            var { localA, localB } = { localA: 1, localB: 2 };
            var [first, ...rest] = [1, 2, 3];
            """,
            root);
        main.Imports.Single(import => import.Include).Module = included;
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([main]);
        var modulePlan = Assert.Single(session.Modules);

        Assert.True(modulePlan.TryGetSymbol("INCLUDED", out _));
        Assert.True(modulePlan.TryGetSymbol("visible", out _));
        Assert.False(modulePlan.TryGetSymbol("HIDDEN", out _));
        Assert.True(modulePlan.TryGetSymbol("localA", out _));
        Assert.True(modulePlan.TryGetSymbol("localB", out _));
        Assert.True(modulePlan.TryGetSymbol("first", out _));
        Assert.True(modulePlan.TryGetSymbol("rest", out _));
        Assert.Equal(6, session.Scopes[modulePlan.ModuleScope].SymbolCount);
    }

    [Fact]
    public void GlobalPredefineMarksGlobalDeclarationsAsCompileTimeOnlySymbols()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            """,
            root);
        var globals = AuroraScript.Compiler.GlobalDeclarations.GlobalDeclarationScanner.BuildIndex([
            ("globals.as",
            """
            @global();
            declare var HOST_VALUE;
            declare const HOST_CONST;
            """)
        ]);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var backend = new BackendCompiler(new DynamicBuilder(options), options, globals);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);

        Assert.True(modulePlan.TryGetSymbol("HOST_VALUE", out var valueSymbol));
        Assert.True(session.Symbols[valueSymbol].HasFlag(BackendSymbolFlags.DeclaredOnly));
        Assert.False(session.Symbols[valueSymbol].HasFlag(BackendSymbolFlags.Const));
        Assert.True(modulePlan.TryGetSymbol("HOST_CONST", out var constSymbol));
        Assert.True(session.Symbols[constSymbol].HasFlag(BackendSymbolFlags.DeclaredOnly));
        Assert.True(session.Symbols[constSymbol].HasFlag(BackendSymbolFlags.Const));
    }

    [Fact]
    public void GlobalPredefineRejectsDuplicateModuleSymbol()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            const value = 1;
            var value = 2;
            func value() { return 3; }
            """,
            root);
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var error = Assert.Throws<AuroraCompilationException>(() => backend.CreateModulePlans([module]));

        Assert.Contains("Duplicate declaration 'value'", error.Message);
        Assert.Contains("module scope", error.Message);
    }

    [Fact]
    public void GlobalPredefineRejectsDuplicateModuleFunctions()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func testTextTemplate() { return 1; }
            export func testTextTemplate(n) { return n; }
            """,
            root);
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var error = Assert.Throws<AuroraCompilationException>(() => backend.CreateModulePlans([module]));

        Assert.Contains("Duplicate declaration 'testTextTemplate'", error.Message);
        Assert.Contains("module scope", error.Message);
    }

    [Fact]
    public void FunctionBinderRejectsDuplicateParameterAndLocalDeclaration()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(n) {
                const n = { a: 1, b: 2 };
                return n;
            }
            """,
            root);
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var error = Assert.Throws<AuroraCompilationException>(() => backend.CreateModulePlans([module]));

        Assert.Contains("Duplicate declaration 'n'", error.Message);
        Assert.Contains("function scope", error.Message);
    }

    [Theory]
    [InlineData("const a = 123; a = { b: 1234 };")]
    [InlineData("const a = 123; a += 1;")]
    [InlineData("const a = 123; a++;")]
    public void CompileBlockRejectsConstAssignment(string body)
    {
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(Path.GetTempPath()));
        var engine = new AuroraEngine(options);

        var error = Assert.Throws<AuroraCompilationException>(() => engine.CompileBlock(body));

        Assert.Contains("Cannot assign to constant 'a'", error.Message);
    }

    [Fact]
    public void CompileBlockRejectsDuplicateDeclarationInSameBlock()
    {
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(Path.GetTempPath()));
        var engine = new AuroraEngine(options);

        var error = Assert.Throws<AuroraCompilationException>(() => engine.CompileBlock(
            """
            const a = 123;
            var a = { b: 1234 };
            """));

        Assert.Contains("Duplicate declaration 'a'", error.Message);
    }

    [Fact]
    public void CompileBlockRejectsDeclarationShadowingVisibleOuterConst()
    {
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(Path.GetTempPath()));
        var engine = new AuroraEngine(options);

        var error = Assert.Throws<AuroraCompilationException>(() => engine.CompileBlock(
            """
            const a = 123;
            {
                var a = { b: 1234 };
            }
            """));

        Assert.Contains("Duplicate declaration 'a'", error.Message);
    }

    [Fact]
    public void CompileBlockAllowsSameDeclarationNameInSiblingBlocks()
    {
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(Path.GetTempPath()));
        var engine = new AuroraEngine(options);

        var block = engine.CompileBlock(
            """
            {
                var a = 1;
            }
            {
                var a = 2;
            }
            """);

        Assert.NotNull(block);
    }

    [Fact]
    public void CompileBlockAllowsDeclarationShadowingVisibleOuterVar()
    {
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(Path.GetTempPath()));
        var engine = new AuroraEngine(options);

        var block = engine.CompileBlock(
            """
            var a = 123;
            {
                var a = 123456;
                console.log(a);
            }
            """);

        Assert.NotNull(block);
    }

    [Fact]
    public void CompileBlockRejectsAssignmentToBlockScopedConst()
    {
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(Path.GetTempPath()));
        var engine = new AuroraEngine(options);

        var error = Assert.Throws<AuroraCompilationException>(() => engine.CompileBlock(
            """
            {
                const a = 123;
                a = 456;
            }
            """));

        Assert.Contains("Cannot assign to constant 'a'", error.Message);
    }

    [Fact]
    public void BackendRejectsAssignmentToCapturedConst()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                const value = 1;
                func mutate() {
                    value = 2;
                }
                return mutate;
            }
            """,
            root);
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var error = Assert.Throws<AuroraCompilationException>(() => backend.CreateModulePlans([module]));

        Assert.Contains("Cannot assign to constant 'value'", error.Message);
    }

    [Fact]
    public void BackendRejectsAssignmentToInheritedCapturedConst()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                const value = 1;
                func outer() {
                    func inner() {
                        value = 2;
                    }
                    return inner;
                }
                return outer;
            }
            """,
            root);
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var error = Assert.Throws<AuroraCompilationException>(() => backend.CreateModulePlans([module]));

        Assert.Contains("Cannot assign to constant 'value'", error.Message);
    }

    [Fact]
    public void BackendAllowsAssignmentToCapturedVarShadowingModuleConst()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export const value = 1;
            export func run() {
                var value = 2;
                func mutate() {
                    value = 3;
                }
                mutate();
                return value;
            }
            """,
            root);
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);

        Assert.Single(session.Modules);
    }

    [Fact]
    public void ModuleDirectCallIsOptIn()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func helper(value) { return value + 1; }
            export func run() { return helper(41); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var helper = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "helper");

        Assert.Equal(FunctionVisibility.ModuleVisible, helper.Visibility);
        Assert.False(helper.IsDirectCallCandidate);
    }

    [Fact]
    public void FunctionAnnotationDirectCallPreservesModuleFunctionObject()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            @directCall
            func helper(value) { return value + 1; }
            export const exposed = helper;
            export func run(value) { return helper(value); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helper = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var runPlan = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var call = Assert.IsType<FunctionCallExpression>(GetSingleReturnExpression(modulePlan, "run"));
        var callTarget = Assert.IsType<NameExpression>(call.Target);
        var callBinding = TypedFunctionBuilder.Build(modulePlan, runPlan).GetName(callTarget);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var initialize = (ModuleInitializerDelegate)moduleResult.Initializer.CreateDelegate(typeof(ModuleInitializerDelegate));
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = CreateRuntimeModule(root);
        var ctx = new ScriptContext(domain) { Module = runtimeModule };

        Assert.Equal(DirectCallDirective.PreserveClosure, helper.DirectCallDirective);
        Assert.True(helper.IsDirectCallCandidate);
        Assert.True(helper.RequiresClosureObject);
        Assert.True(callBinding.DirectFunction.Equals(helper.Id));

        initialize(ctx, Span<ScriptDatum>.Empty);
        Assert.IsType<ClosureFunction>(runtimeModule.GetPropertyValue("helper"));

        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(ctx, new[] { ScriptDatum.FromNumber(41) });
        Assert.Equal(42, result.Number);
    }

    [Fact]
    public void FunctionAnnotationDirectCallFalseDisablesModuleDirectCall()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            @directCall(false)
            func helper(value) { return value + 1; }
            export func run(value) { return helper(value); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var helper = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "helper");

        Assert.Equal(DirectCallDirective.Disabled, helper.DirectCallDirective);
        Assert.False(helper.IsDirectCallCandidate);
        Assert.True(helper.RequiresClosureObject);
    }

    [Fact]
    public void FunctionAnnotationDirectCallCanTargetExportedFunction()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            @directCall
            export func helper(value) { return value + 1; }
            export func run(value) { return helper(value); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helper = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var call = Assert.IsType<FunctionCallExpression>(GetSingleReturnExpression(modulePlan, "run"));
        var callTarget = Assert.IsType<NameExpression>(call.Target);
        var callBinding = TypedFunctionBuilder.Build(modulePlan, run).GetName(callTarget);

        Assert.Equal(FunctionVisibility.Exported, helper.Visibility);
        Assert.True(helper.IsDirectCallCandidate);
        Assert.True(helper.RequiresClosureObject);
        Assert.True(callBinding.DirectFunction.Equals(helper.Id));
    }

    [Fact]
    public void FunctionAnnotationKeepsHighArityNativeSpecializationEligible()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            @directCall
            func mix(a, b, c, d, e, f, g, h) {
                a = a ^ b;
                return a ^ c ^ d ^ e ^ f ^ g ^ h;
            }
            export func run() { return mix(1, 2, 3, 4, 5, 6, 7, 8); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var mix = Assert.Single(modulePlan.Functions, function => function.Name == "mix");
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var call = Assert.IsType<FunctionCallExpression>(GetSingleReturnExpression(modulePlan, "run"));
        var target = Assert.IsType<NameExpression>(call.Target);
        var binding = TypedFunctionBuilder.Build(modulePlan, run).GetName(target);

        Assert.True(mix.IsDirectCallCandidate);
        Assert.True(mix.RequiresClosureObject);
        Assert.True(binding.DirectFunction.Equals(mix.Id));
    }

    [Fact]
    public void FunctionAnnotationDirectCallWorksWhenHotReloadIsEnabled()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            @directCall
            func helper(value) { return value + 1; }
            export func run(value) { return helper(value); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = true)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helper = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var call = Assert.IsType<FunctionCallExpression>(GetSingleReturnExpression(modulePlan, "run"));
        var callTarget = Assert.IsType<NameExpression>(call.Target);
        var callBinding = TypedFunctionBuilder.Build(modulePlan, run).GetName(callTarget);

        Assert.True(session.Capabilities.CanUseModuleDirectCall);
        Assert.True(helper.IsDirectCallCandidate);
        Assert.True(callBinding.DirectFunction.Equals(helper.Id));
    }

    [Fact]
    public void UnsupportedFunctionAnnotationIsRejectedByBackendBinding()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            @unknown(false)
            func helper() { return 1; }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var error = Assert.Throws<AuroraCompilationException>(() => backend.CreateModulePlans([module]));

        Assert.Contains("Unsupported function annotation '@unknown'", error.Message);
    }

    [Fact]
    public void ModuleAnalysisHandlesMultipleModulesIndependently()
    {
        var root = Path.GetTempPath();
        var first = Parse(
            """
            @module(FIRST);
            func helperA(value) { return value + 1; }
            export func runA() { return helperA(41); }
            """,
            root);
        var second = Parse(
            """
            @module(SECOND);
            func helperB(value) { return value + 2; }
            export func runB() { return helperB(40); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([first, second]);

        Assert.Equal(2, session.Modules.Length);
        Assert.Contains(session.Modules[0].Functions, function => function.Name == "helperA" && function.IsDirectCallCandidate);
        Assert.Contains(session.Modules[1].Functions, function => function.Name == "helperB" && function.IsDirectCallCandidate);
        Assert.Equal(4, session.Modules.SelectMany(module => module.Functions).Select(function => function.Id.Value).Distinct().Count());
    }

    [Fact]
    public void FunctionBindingCreatesParameterAndLocalSlots()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func sample(a, b = 2) {
                const localConst = a + b;
                var { left, right } = { left: 1, right: 2 };
                var [first, ...rest] = [1, 2, 3];
                return $args;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var sample = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "sample");

        Assert.True(sample.HasDefaultParameters);
        Assert.True(sample.UsesArgumentsObject);
        Assert.Equal(
            new[] { "a", "b", "localConst", "left", "right", "first", "rest" },
            sample.LocalSlots.Select(slot => slot.Name).ToArray());
        Assert.Equal(new[] { true, true, false, false, false, false, false }, sample.LocalSlots.Select(slot => slot.IsParameter).ToArray());
        Assert.Contains(sample.LocalSlots, slot => slot.Name == "localConst" && (slot.Flags & BackendSymbolFlags.Const) != 0);
    }

    [Fact]
    public void FunctionBindingDeclaresCatchVariableSlot()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func sample() {
                try {
                    throw 1;
                } catch (error) {
                    return error;
                }
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var sample = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "sample");

        Assert.Contains(sample.LocalSlots, slot => slot.Name == "error" && slot.Kind == BackendSymbolKind.Local);
    }

    [Fact]
    public void FunctionBindingRegistersNestedFunctions()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func outer(value) {
                func inner(delta) { return value + delta; }
                var lambda = (x) => x + value;
                return inner(1) + lambda(2);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var outer = Assert.Single(modulePlan.Functions, function => function.Name == "outer");

        Assert.Equal(3, modulePlan.Functions.Count);
        Assert.Equal(2, outer.NestedFunctions.Length);
        Assert.Contains(modulePlan.Functions, function => function.Name == "inner" && !function.IsModuleFunction);
        Assert.Contains(modulePlan.Functions, function => function.Name.StartsWith("lambda_", StringComparison.Ordinal) && !function.IsModuleFunction);
        Assert.Contains(outer.LocalSlots, slot => slot.Name == "inner" && slot.Kind == BackendSymbolKind.Local);
        Assert.Contains(outer.LocalSlots, slot => slot.Name == "lambda" && slot.Kind == BackendSymbolKind.Local);
        Assert.Equal(3, modulePlan.Functions.Select(function => function.Id.Value).Distinct().Count());
    }

    [Fact]
    public void ClosurePlannerCapturesParentLocal()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func outer(value) {
                var local = value + 1;
                func inner(delta) { return local + delta; }
                return inner(2);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var outer = Assert.Single(modulePlan.Functions, function => function.Name == "outer");
        var inner = Assert.Single(modulePlan.Functions, function => function.Name == "inner");
        var localSlot = Assert.Single(outer.LocalSlots, slot => slot.Name == "local");

        var upvalue = Assert.Single(inner.UpvalueSlots);
        Assert.Equal("local", upvalue.Name);
        Assert.Equal(outer.Id, upvalue.SourceFunction);
        Assert.Equal(localSlot.Id, upvalue.SourceLocal);
        Assert.False(upvalue.IsInherited);

        var captured = Assert.Single(outer.CapturedLocalSlots);
        Assert.Equal("local", captured.Name);
        Assert.Equal(localSlot.Id, captured.SourceLocal);
    }

    [Fact]
    public void ClosurePlannerCreatesInheritedUpvalueChain()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func outer(value) {
                var local = value + 1;
                func middle() {
                    func inner(delta) { return local + delta; }
                    return inner(2);
                }
                return middle();
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var outer = Assert.Single(modulePlan.Functions, function => function.Name == "outer");
        var middle = Assert.Single(modulePlan.Functions, function => function.Name == "middle");
        var inner = Assert.Single(modulePlan.Functions, function => function.Name == "inner");
        var localSlot = Assert.Single(outer.LocalSlots, slot => slot.Name == "local");

        var middleUpvalue = Assert.Single(middle.UpvalueSlots);
        Assert.Equal("local", middleUpvalue.Name);
        Assert.Equal(outer.Id, middleUpvalue.SourceFunction);
        Assert.Equal(localSlot.Id, middleUpvalue.SourceLocal);
        Assert.False(middleUpvalue.IsInherited);

        var innerUpvalue = Assert.Single(inner.UpvalueSlots);
        Assert.Equal("local", innerUpvalue.Name);
        Assert.Equal(middle.Id, innerUpvalue.SourceFunction);
        Assert.True(innerUpvalue.IsInherited);
        Assert.Equal(middleUpvalue.Id, innerUpvalue.SourceUpvalue);

        var captured = Assert.Single(outer.CapturedLocalSlots);
        Assert.Equal("local", captured.Name);
        Assert.Equal(localSlot.Id, captured.SourceLocal);
    }

    [Fact]
    public void LambdaPassedAsValueRequiresClosureObjectButCanCacheWhenNonCapturing()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func test() {
                somecall((a, b) => a + b);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var lambda = Assert.Single(modulePlan.Functions, function => function.IsLambda);

        Assert.False(lambda.IsModuleFunction);
        Assert.True(lambda.RequiresClosureObject);
        Assert.True(lambda.CanCacheClosureObject);
        Assert.Empty(lambda.UpvalueSlots);
    }

    [Fact]
    public void CapturingLambdaPassedAsValueRequiresFreshClosureObject()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func test(offset) {
                somecall((a, b) => a + b + offset);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var test = Assert.Single(modulePlan.Functions, function => function.Name == "test");
        var lambda = Assert.Single(modulePlan.Functions, function => function.IsLambda);

        Assert.True(lambda.RequiresClosureObject);
        Assert.False(lambda.CanCacheClosureObject);
        var upvalue = Assert.Single(lambda.UpvalueSlots);
        Assert.Equal("offset", upvalue.Name);
        Assert.Equal(test.Id, upvalue.SourceFunction);
    }

    [Fact]
    public void TypedBindingResolvesNamesToLocalUpvalueAndModuleSymbols()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            const MODULE_VALUE = 10;
            func outer(value) {
                var local = value + MODULE_VALUE;
                func inner(delta) { return local + delta + MODULE_VALUE; }
                return inner(2);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var outer = Assert.Single(modulePlan.Functions, function => function.Name == "outer");
        var inner = Assert.Single(modulePlan.Functions, function => function.Name == "inner");

        var outerCall = Assert.IsType<FunctionCallExpression>(GetSingleReturnExpression(modulePlan, "outer"));
        var outerTarget = Assert.IsType<NameExpression>(outerCall.Target);
        var outerCode = TypedFunctionBuilder.Build(modulePlan, outer);
        var outerBinding = outerCode.GetName(outerTarget);
        Assert.True(outerBinding.Local.IsValid);
        Assert.Equal("inner", outerBinding.Name);

        var innerExpression = GetSingleReturnExpression(modulePlan, "inner");
        var names = new List<NameExpression>();
        CollectNames(innerExpression, names);
        var innerCode = TypedFunctionBuilder.Build(modulePlan, inner);

        Assert.Contains(names, name => innerCode.GetName(name) is { Name: "local", Upvalue.IsValid: true });
        Assert.Contains(names, name => innerCode.GetName(name) is { Name: "delta", Local.IsValid: true });
        Assert.Contains(names, name => innerCode.GetName(name) is { Name: "MODULE_VALUE", ModuleSymbol.IsValid: true });
    }

    [Fact]
    public void ModuleConstInliningIsDisabledByDefault()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export const a1 = 1;
            export const a5 = a1 + 4;
            export func run() { return a5; }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var returned = GetSingleReturnExpression(modulePlan, "run");
        var name = Assert.IsType<NameExpression>(returned);
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var binding = TypedFunctionBuilder.Build(modulePlan, run).GetName(name);

        Assert.False(modulePlan.HasInlineConstants);
        Assert.Equal("a5", binding.Name);
        Assert.True(binding.ModuleSymbol.IsValid);
        Assert.False(binding.HasConstant);
    }

    [Fact]
    public void ModuleConstInliningFoldsChainedConstantsWhenEnabled()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export const a1 = 1;
            export const a5 = a1 + 4;
            export func run() { return a5; }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = true)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var returned = GetSingleReturnExpression(modulePlan, "run");
        var name = Assert.IsType<NameExpression>(returned);
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var binding = TypedFunctionBuilder.Build(modulePlan, run).GetName(name);

        Assert.True(modulePlan.HasInlineConstants);
        Assert.True(modulePlan.TryGetSymbol("a5", out var symbolId));
        Assert.True(modulePlan.TryGetInlineConstant(symbolId, out var constant));
        Assert.Equal(ValueKind.Number, constant.Kind);
        Assert.Equal(5, constant.Number);
        Assert.True(binding.HasConstant);
        Assert.Equal(5, binding.Constant.Number);
    }

    [Fact]
    public void ModuleConstInliningFoldsPrimitiveAndMixedStringConstants()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export const NUM = 3.141592678987654321;
            export const STR = 'this is string';
            export const BOOL = true;
            export const BASE = 10;
            export const COMPLEX = BASE * NUM + 5;
            export const TAG = BASE + '_' + 1;
            export const TEMPLATE = STR + BASE + '_' + TAG;
            export func run() { return [NUM, STR, BOOL, COMPLEX, TAG, TEMPLATE]; }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);
        var expectedNum = 3.141592678987654321d;

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var returned = GetSingleReturnExpression(modulePlan, "run");
        var array = Assert.IsType<ArrayLiteralExpression>(returned);
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var code = TypedFunctionBuilder.Build(modulePlan, run);

        AssertInlineNumber(modulePlan, "NUM", expectedNum);
        AssertInlineString(modulePlan, "STR", "this is string");
        AssertInlineBoolean(modulePlan, "BOOL", true);
        AssertInlineNumber(modulePlan, "BASE", 10);
        AssertInlineNumber(modulePlan, "COMPLEX", 10 * expectedNum + 5);
        AssertInlineString(modulePlan, "TAG", "10_1");
        AssertInlineString(modulePlan, "TEMPLATE", "this is string10_10_1");
        Assert.Equal(6, array.Elements.Count);
        Assert.All(array.Elements, element =>
        {
            var name = Assert.IsType<NameExpression>(element);
            Assert.True(code.GetName(name).HasConstant);
        });
    }

    [Fact]
    public void ModuleConstInliningEmitsFoldedValuesInModuleInitializer()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export const NUM = 3.141592678987654321;
            export const STR = 'this is string';
            export const BOOL = true;
            export const BASE = 10;
            export const COMPLEX = BASE * NUM + 5;
            export const TAG = BASE + '_' + 1;
            export const TEMPLATE = STR + BASE + '_' + TAG;
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var builder = new RecordingBuilder(options);
        var backend = new BackendCompiler(builder, options);
        var expectedNum = 3.141592678987654321d;

        var session = backend.CreateModulePlans([module]);
        new EmissionSession(session, builder, emitExecutableCode: true).Emit();

        Assert.Contains(builder.NumberLoads, number => Math.Abs(number - (10 * expectedNum + 5)) < 1e-12);
        Assert.Contains("10_1", builder.StringLoads);
        Assert.Contains("this is string10_10_1", builder.StringLoads);
    }

    [Fact]
    public void ModuleConstInliningDoesNotFoldRuntimeCallConstants()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export const a1 = 1;
            func make() { return 5; }
            export const fv = make();
            export func run() { return fv; }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var returned = GetSingleReturnExpression(modulePlan, "run");
        var name = Assert.IsType<NameExpression>(returned);
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var binding = TypedFunctionBuilder.Build(modulePlan, run).GetName(name);

        Assert.True(modulePlan.TryGetSymbol("fv", out var symbolId));
        Assert.False(modulePlan.TryGetInlineConstant(symbolId, out _));
        Assert.Equal("fv", binding.Name);
        Assert.True(binding.ModuleSymbol.IsValid);
        Assert.False(binding.HasConstant);
    }

    [Fact]
    public void ModuleConstInliningDoesNotFoldForwardReferences()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export const a5 = a1 + 4;
            export const a1 = 1;
            export func run() { return a5; }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var returned = GetSingleReturnExpression(modulePlan, "run");
        var name = Assert.IsType<NameExpression>(returned);
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var binding = TypedFunctionBuilder.Build(modulePlan, run).GetName(name);

        Assert.True(modulePlan.TryGetSymbol("a1", out var a1Symbol));
        Assert.True(modulePlan.TryGetInlineConstant(a1Symbol, out _));
        Assert.True(modulePlan.TryGetSymbol("a5", out var a5Symbol));
        Assert.False(modulePlan.TryGetInlineConstant(a5Symbol, out _));
        Assert.Equal("a5", binding.Name);
        Assert.True(binding.ModuleSymbol.IsValid);
        Assert.False(binding.HasConstant);
    }

    [Fact]
    public void ModuleConstInliningRejectsConstMutationTargets()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export const a1 = 1;
            export func run() {
                a1 = 2;
                a1++;
                return a1;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var error = Assert.Throws<AuroraCompilationException>(() => backend.CreateModulePlans([module]));

        Assert.Contains("Cannot assign to constant 'a1'", error.Message);
    }

    [Fact]
    public void ModuleConstInliningKeepsMutableMutationTargetsAsNames()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export const a1 = 1;
            export var value = 0;
            export func run() {
                value = a1;
                value++;
                return a1;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var body = Assert.IsType<BlockStatement>(run.Declaration.Body);
        var assignmentStatement = Assert.IsType<ExpressionStatement>(body.Statements[0]);
        var assignment = Assert.IsType<AssignmentExpression>(assignmentStatement.Expression);
        var assignmentTarget = Assert.IsType<NameExpression>(assignment.Left);
        var incrementStatement = Assert.IsType<ExpressionStatement>(body.Statements[1]);
        var increment = Assert.IsType<UnaryExpression>(incrementStatement.Expression);
        var incrementTarget = Assert.IsType<NameExpression>(increment.Expression);
        var returnStatement = Assert.IsType<ReturnStatement>(body.Statements[2]);
        var returnName = Assert.IsType<NameExpression>(returnStatement.Expression);
        var code = TypedFunctionBuilder.Build(modulePlan, run);

        Assert.Equal("value", code.GetName(assignmentTarget).Name);
        Assert.True(code.GetName(assignmentTarget).ModuleSymbol.IsValid);
        Assert.Equal("value", code.GetName(incrementTarget).Name);
        Assert.True(code.GetName(incrementTarget).ModuleSymbol.IsValid);
        Assert.True(code.GetName(returnName).HasConstant);
    }

    [Fact]
    public void ModuleConstInliningDoesNotFoldDotPropertyNamesButKeepsElementIndexes()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            const width = 8;
            export func run(obj, arr) {
                return [obj.width, arr[width]];
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var returned = GetSingleReturnExpression(modulePlan, "run");
        var array = Assert.IsType<ArrayLiteralExpression>(returned);
        var property = Assert.IsType<GetPropertyExpression>(array.Elements[0]);
        var propertyName = Assert.IsType<NameExpression>(property.Property);
        var element = Assert.IsType<GetElementExpression>(array.Elements[1]);
        var index = Assert.IsType<NameExpression>(element.Index);
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var code = TypedFunctionBuilder.Build(modulePlan, run);

        Assert.Equal("width", propertyName.Identifier.Value);
        Assert.False(code.GetName(propertyName).Local.IsValid);
        Assert.False(code.GetName(propertyName).Upvalue.IsValid);
        Assert.False(code.GetName(propertyName).ModuleSymbol.IsValid);
        Assert.True(code.GetName(index).HasConstant);
        Assert.Equal(8, code.GetName(index).Constant.Number);
    }

    [Fact]
    public void TypedBindingMarksModuleDirectCallTarget()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func helper(value) { return value + 1; }
            export func run(value) { return helper(value); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helper = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var call = Assert.IsType<FunctionCallExpression>(GetSingleReturnExpression(modulePlan, "run"));
        var target = Assert.IsType<NameExpression>(call.Target);
        var binding = TypedFunctionBuilder.Build(modulePlan, run).GetName(target);

        Assert.True(helper.IsDirectCallCandidate);
        Assert.Equal(FunctionVisibility.InternalOnly, helper.Visibility);
        Assert.Equal(helper.Id, binding.DirectFunction);
    }

    [Fact]
    public void AstLambdaMapsToRegisteredFunctionPlan()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func test() {
                return (a, b) => a + b;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var test = Assert.Single(modulePlan.Functions, function => function.Name == "test");
        var lambda = Assert.Single(modulePlan.Functions, function => function.IsLambda);

        var lambdaExpression = Assert.IsType<LambdaExpression>(GetSingleReturnExpression(modulePlan, "test"));
        Assert.Same(lambda.Declaration, lambdaExpression.Function);
    }

    [Fact]
    public void AstRepresentsControlFlowAndHighFrequencyOperators()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func test(limit) {
                limit = limit + 1;
                limit += 1;
                if (limit > 0) {
                    return limit;
                } else {
                    return 0;
                }
                while (limit > 0) {
                    break;
                }
                for (var i = 0; i < limit; i++) {
                    continue;
                }
                return limit;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var test = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "test");

        var body = Assert.IsType<BlockStatement>(test.Declaration.Body);
        var expressionStatements = body.Statements.OfType<ExpressionStatement>().ToArray();
        Assert.IsType<AssignmentExpression>(expressionStatements[0].Expression);
        Assert.IsType<CompoundExpression>(expressionStatements[1].Expression);

        var ifStatement = Assert.Single(body.Statements.OfType<IfStatement>());
        Assert.IsType<BinaryExpression>(ifStatement.Condition);
        Assert.IsType<BlockStatement>(ifStatement.Body);
        Assert.IsType<BlockStatement>(ifStatement.Else);

        var whileStatement = Assert.Single(body.Statements.OfType<WhileStatement>());
        var whileBody = Assert.IsType<BlockStatement>(whileStatement.Body);
        Assert.IsType<BreakStatement>(Assert.Single(whileBody.Statements));

        var forStatement = Assert.Single(body.Statements.OfType<ForStatement>());
        Assert.IsType<VariableDeclaration>(forStatement.Initializer);
        Assert.IsType<BinaryExpression>(forStatement.Condition);
        Assert.IsType<UnaryExpression>(forStatement.Incrementor);
        var forBody = Assert.IsType<BlockStatement>(forStatement.Body);
        Assert.IsType<ContinueStatement>(Assert.Single(forBody.Statements));
    }

    [Fact]
    public void AstRepresentsForInAndExceptionStatements()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func test(items, obj) {
                for (var item in items) {
                    continue;
                }
                try {
                    throw obj;
                } catch (error) {
                    delete obj.value;
                    debugger;
                } finally {
                    debugger;
                }
                return obj;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var test = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "test");

        var body = Assert.IsType<BlockStatement>(test.Declaration.Body);
        var forIn = Assert.Single(body.Statements.OfType<ForInStatement>());
        Assert.IsType<VariableDeclaration>(forIn.Initializer);
        Assert.NotNull(forIn.Iterator);
        Assert.IsType<NameExpression>(forIn.Iterator.Left);
        var forInBody = Assert.IsType<BlockStatement>(forIn.Body);
        Assert.IsType<ContinueStatement>(Assert.Single(forInBody.Statements));

        var tryStatement = Assert.Single(body.Statements.OfType<TryStatement>());
        Assert.Equal("error", tryStatement.CatchVariable);
        Assert.Contains(test.LocalSlots, slot => slot.Name == "error" && ReferenceEquals(slot.Declaration, tryStatement));
        var tryBody = Assert.IsType<BlockStatement>(tryStatement.Body);
        Assert.IsType<ThrowStatement>(Assert.Single(tryBody.Statements));
        var catchBody = Assert.IsType<BlockStatement>(tryStatement.CatchBody);
        Assert.IsType<DeleteStatement>(catchBody.Statements[0]);
        Assert.IsType<DebuggerStatement>(catchBody.Statements[1]);
        var finallyBody = Assert.IsType<BlockStatement>(tryStatement.FinallyBody);
        Assert.IsType<DebuggerStatement>(Assert.Single(finallyBody.Statements));
    }

    [Fact]
    public void AstRepresentsObjectArrayMapAndConstructorExpressions()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func factory() { return 1; }
            func test(obj, index, values) {
                var array = [1, ...values, obj[index], obj.value];
                var map = { first: obj.value, second: obj[index], ...obj };
                obj.value = new factory();
                obj[index] = array;
                return map;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var test = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "test");

        var body = Assert.IsType<BlockStatement>(test.Declaration.Body);
        var declarations = body.Statements.OfType<VariableDeclaration>().ToArray();
        var array = Assert.IsType<ArrayLiteralExpression>(declarations[0].Initializer);
        Assert.Contains(array.Elements, expression => expression is SpreadExpression);
        Assert.Contains(array.Elements, expression => expression is GetElementExpression);
        Assert.Contains(array.Elements, expression => expression is GetPropertyExpression);

        var map = Assert.IsType<MapExpression>(declarations[1].Initializer);
        Assert.Equal(3, map.Entries.Count);
        Assert.Contains(map.Entries, entry => entry is MapKeyValueExpression { Key.Value: "first", Value: GetPropertyExpression });
        Assert.Contains(map.Entries, entry => entry is MapKeyValueExpression { Key.Value: "second", Value: GetElementExpression });
        Assert.Contains(map.Entries, entry => entry is SpreadExpression);

        var expressionStatements = body.Statements.OfType<ExpressionStatement>().ToArray();
        var setProperty = Assert.IsType<SetPropertyExpression>(expressionStatements[0].Expression);
        Assert.IsType<NameExpression>(setProperty.Property);
        Assert.IsType<NewExpression>(setProperty.Value);
        var setElement = Assert.IsType<SetElementExpression>(expressionStatements[1].Expression);
        Assert.IsType<NameExpression>(setElement.Index);
    }

    [Fact]
    public void AstKeepsUnsupportedNodesForEmissionValidation()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func test(value) {
                var [first, ...rest] = value;
                enum Unsupported { Value }
                return first;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var test = Assert.Single(modulePlan.Functions, function => function.Name == "test");
        var body = Assert.IsType<BlockStatement>(test.Declaration.Body);
        Assert.Contains(body.Statements, statement => statement is EnumDeclaration);

        var exception = Assert.Throws<UnsupportedEmissionException>(() =>
            new EmissionSession(session, new DynamicBuilder(options), collectDiagnostics: true).Emit());
        Assert.Equal("EnumDeclaration", exception.NodeType);
        Assert.False(exception.IsExpression);
    }

    [Fact]
    public void AstRepresentsDestructuringDeclarations()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func test(value, obj) {
                var [first, ...middle, last] = value;
                var { name, age } = obj;
                return first + last + middle.length + age;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var test = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "test");

        var body = Assert.IsType<BlockStatement>(test.Declaration.Body);
        var arrayDeclaration = Assert.IsType<VariableDeclaration>(body.Statements[0]);
        var array = Assert.IsType<ArrayDestructuringPattern>(arrayDeclaration.Pattern);
        Assert.Equal(3, array.Elements.Count);
        Assert.IsType<NameExpression>(array.Elements[0]);
        Assert.IsType<SpreadExpression>(array.Elements[1]);
        Assert.IsType<NameExpression>(array.Elements[2]);

        var objectDeclaration = Assert.IsType<VariableDeclaration>(body.Statements[1]);
        var obj = Assert.IsType<ObjectDestructuringPattern>(objectDeclaration.Pattern);
        Assert.Equal(new[] { "name", "age" }, obj.Properties.Select(property => property.Value).ToArray());
    }

    [Fact]
    public void EmissionPassConsumesSupportedBoundAst()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func helper(value) { return value + 1; }
            export func run(items, obj) {
                for (var item in items) {
                    if (item > 0) {
                        break;
                    }
                }
                try {
                    obj.value = helper(1);
                } catch (error) {
                    return error;
                } finally {
                    debugger;
                }
                return obj.value;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, collectDiagnostics: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var helper = Assert.Single(moduleResult.Functions, function => function.Name == "helper");
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");

        Assert.Equal(2, report.FunctionCount);
        Assert.True(helper.IsDirectCallCandidate);
        Assert.True(run.StatementCount > 0);
        Assert.True(run.ExpressionCount > 0);
        Assert.True(run.SequencePointCount > 0);
        Assert.Contains(run.SequencePoints, range => range.StartLine > 0);
        Assert.True(run.LocalSlotReferenceCount > 0);
        Assert.True(run.ModuleSymbolReferenceCount > 0);
        Assert.True(run.DirectCallCandidateReferenceCount > 0);
        Assert.True(run.CatchSlotReferenceCount > 0);
    }

    [Fact]
    public void EmissionPassRejectsUnsupportedAstNodes()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                enum Unsupported { Value }
                return 1;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var exception = Assert.Throws<UnsupportedEmissionException>(() => new EmissionSession(session, builder, collectDiagnostics: true).Emit());

        Assert.Equal("EnumDeclaration", exception.NodeType);
        Assert.False(exception.IsExpression);
    }

    [Fact]
    public void EmissionPassReadsBoundAstDirectly()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                return 1;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, collectDiagnostics: true).Emit();

        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");
        Assert.Equal(2, run.StatementCount);
        Assert.Equal(1, run.ExpressionCount);
    }

    [Fact]
    public void TypedEmitterExecutesLiteralReturn()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                return 42;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        Assert.Equal(0, run.CilLocalCount);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(42, result.Number);
    }

    [Fact]
    public void TypedEmitterStoresAndLoadsLocal()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var value = "ok";
                return value;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        Assert.Equal(1, run.CilLocalCount);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal("ok", ScriptDatum.ToString(result));
    }

    [Fact]
    public void TypedEmitterInitializesParameterLocalsFromSpanArguments()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(value) {
                return value;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        Assert.Equal(1, run.CilLocalCount);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var args = new ScriptDatum[1];
        args[0] = ScriptDatum.FromNumber(7);
        var result = del(CreateTestContext(), args);
        Assert.Equal(7, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesBinaryArithmeticAndComparison()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(left, right) {
                if (left + right * 2 > 10) {
                    return 1;
                }
                return 0;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var args = new[] { ScriptDatum.FromNumber(3), ScriptDatum.FromNumber(4) };
        var result = del(CreateTestContext(), args);
        Assert.Equal(1, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesLocalAssignment()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var value = 1;
                value = value + 4;
                return value;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(5, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesCompoundAndUnaryLocalOperators()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var value = 2;
                value += 3;
                value *= 4;
                return value++ + ++value + value--;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(64, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesElementCompoundAddOnce()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var index = 0;
                var values = [1];
                values[index++] += 2;
                return values[0] * 10 + index;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(31, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesPropertyAndElementUnaryMutation()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var obj = { value: 2 };
                var before = obj.value++;
                var after = --obj.value;
                var index = 0;
                var values = [4];
                var elementBefore = values[index++]++;
                return before * 10000 + after * 1000 + values[0] * 100 + elementBefore * 10 + index;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(22541, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesLogicalShortCircuitAndBitwiseOperators()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var value = 0;
                var left = false && (value = 1);
                var right = true || (value = 2);
                if (!left && right) {
                    return value + ((~1 & 6) | (1 << 3));
                }
                return 99;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(14, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesWhileLoop()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(value) {
                var total = 0;
                while (value > 0) {
                    total = total + value;
                    value = value - 1;
                }
                return total;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), new[] { ScriptDatum.FromNumber(4) });
        Assert.Equal(10, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesForLoopWithBreakAndContinue()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(limit) {
                var total = 0;
                for (var i = 0; i < limit; i = i + 1) {
                    if (i == 2) {
                        continue;
                    }
                    if (i == 5) {
                        break;
                    }
                    total = total + i;
                }
                return total;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), new[] { ScriptDatum.FromNumber(8) });
        Assert.Equal(8, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesForInAcrossArrayObjectAndString()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var arrayCount = 0;
                for (var item in [10, 20, 30]) {
                    arrayCount++;
                }
                var objectCount = 0;
                for (var key in { a: 1, b: 2 }) {
                    objectCount++;
                }
                var textCount = 0;
                for (var ch in "abc") {
                    textCount++;
                }
                return arrayCount * 100 + objectCount * 10 + textCount;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(323, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesForInWithBreakAndContinue()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var total = 0;
                for (var item in [1, 2, 3, 4]) {
                    if (item == 2) {
                        continue;
                    }
                    if (item == 4) {
                        break;
                    }
                    total = total + item;
                }
                return total;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(4, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesForwardModuleDirectCallWithFastArity()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(value) {
                return helper(value, 2) + helper(1, 3);
            }
            func helper(left, right) {
                return left + right;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helperPlan = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var helper = Assert.Single(moduleResult.Functions, function => function.Name == "helper");

        Assert.True(run.HasExecutableCode);
        Assert.True(helper.HasExecutableCode);
        Assert.Equal(FunctionCallConvention.Fast2, helperPlan.CallConvention);
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var ctx = new ScriptContext(domain) { Module = CreateRuntimeModule(root) };
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var runResult = runDel(ctx, new[] { ScriptDatum.FromNumber(5) });
        Assert.Equal(11, runResult.Number);

        var helperDel = (ScriptFunctionDelegate2)helper.Method.CreateDelegate(typeof(ScriptFunctionDelegate2));
        var helperResult = helperDel(CreateTestContext(), ScriptDatum.FromNumber(6), ScriptDatum.FromNumber(7));
        Assert.Equal(13, helperResult.Number);
    }

    [Fact]
    public void ModuleInitializerExecutesPrunedModuleDirectCall()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            var total = helper(40);
            func helper(value) {
                return value + 2;
            }
            export func run() {
                return total;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helperPlan = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var initialize = (ModuleInitializerDelegate)moduleResult.Initializer.CreateDelegate(typeof(ModuleInitializerDelegate));
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = CreateRuntimeModule(root);
        var ctx = new ScriptContext(domain) { Module = runtimeModule };

        Assert.True(helperPlan.IsDirectCallCandidate);
        Assert.False(helperPlan.RequiresClosureObject);
        initialize(ctx, Span<ScriptDatum>.Empty);
        Assert.Same(ScriptObject.Null, runtimeModule.GetPropertyValue("helper"));

        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(ctx, Span<ScriptDatum>.Empty);
        Assert.Equal(42, result.Number);
    }

    [Fact]
    public void TypedEmitterHoistsUncapturedLocalFunctionDeclarations()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(value) {
                var result = helper(value);
                func helper(input) {
                    return input + 4;
                }
                return result + helper(1);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helperPlan = Assert.Single(modulePlan.Functions, function => function.Name == "helper" && !function.IsModuleFunction);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var helper = Assert.Single(moduleResult.Functions, function => function.Function.Equals(helperPlan.Id));
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var ctx = new ScriptContext(domain) { Module = CreateRuntimeModule(root) };

        Assert.True(run.HasExecutableCode);
        Assert.True(helper.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(ctx, new[] { ScriptDatum.FromNumber(2) });
        Assert.Equal(11, result.Number);
    }

    [Fact]
    public void TypedEmitterEvaluatesExtraDirectCallArgumentsInOrder()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var value = 1;
                helper(value = value + 1, value = value + 2);
                return value;
            }
            func helper(value) {
                return value;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        Assert.Equal(1, run.CilLocalCount);
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var ctx = new ScriptContext(domain) { Module = CreateRuntimeModule(root) };
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(ctx, Span<ScriptDatum>.Empty);
        Assert.Equal(4, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesRegularFunctionObjectCallsWithFastArity()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(callback) {
                return callback() + callback(1, 2) + callback(1, 2, 3, 4, 5, 6, 7);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");
        var callback = new BondingFunction(SumArgumentCountAndValues);

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), new[] { ScriptDatum.FromObject(callback) });
        Assert.Equal(40, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesRegularFunctionObjectCallsWithMaterializedArguments()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(callback) {
                var value = 0;
                return callback(
                    value = value + 1,
                    value = value + 1,
                    value = value + 1,
                    value = value + 1,
                    value = value + 1,
                    value = value + 1,
                    value = value + 1,
                    value = value + 1,
                    value = value + 1) + value;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");
        var callback = new BondingFunction(SumArgumentCountAndValues);

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), new[] { ScriptDatum.FromObject(callback) });
        Assert.Equal(63, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesSpreadFunctionObjectCalls()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(callback) {
                var prefix = [1, 2];
                var suffix = [4, 5];
                return callback(0, ...prefix, 3, ...suffix);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");
        var callback = new BondingFunction(SumArgumentCountAndValues);

        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), new[] { ScriptDatum.FromObject(callback) });
        Assert.Equal(21, result.Number);
    }

    [Fact]
    public void TypedEmitterMaterializesUncapturedLambdaArguments()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(callback) {
                return callback((a, b) => a + b, 2, 3);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var lambdaPlan = Assert.Single(modulePlan.Functions, function => function.IsLambda);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var lambda = Assert.Single(moduleResult.Functions, function => function.Function.Equals(lambdaPlan.Id));
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var ctx = new ScriptContext(domain) { Module = CreateRuntimeModule(root) };
        var callback = new BondingFunction(InvokeLambdaWithTwoNumbers);

        Assert.True(lambda.HasExecutableCode);
        Assert.True(run.HasExecutableCode);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(ctx, new[] { ScriptDatum.FromObject(callback) });
        Assert.Equal(5, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesDefaultParameterFunctions()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func add(a, b = a + 3) {
                return b;
            }
            export func run() {
                return add(2) + add(2, 10);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var addPlan = Assert.Single(modulePlan.Functions, function => function.Name == "add");
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var add = Assert.Single(moduleResult.Functions, function => function.Name == "add");
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = CreateRuntimeModule(root);
        var ctx = new ScriptContext(domain) { Module = runtimeModule };

        Assert.True(addPlan.HasDefaultParameters);
        Assert.Equal(FunctionCallConvention.Span, addPlan.CallConvention);
        Assert.True(add.HasExecutableCode);
        Assert.True(run.HasExecutableCode);
        Assert.True(moduleResult.HasExecutableInitializer);

        var initialize = (ModuleInitializerDelegate)moduleResult.Initializer.CreateDelegate(typeof(ModuleInitializerDelegate));
        initialize(ctx, Span<ScriptDatum>.Empty);

        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(ctx, Span<ScriptDatum>.Empty);
        Assert.Equal(15, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesArgsObjectFunctions()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func count(a, b = 5) {
                return a + b + $args.length;
            }
            export func run() {
                return count(2) + count(2, 10);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var countPlan = Assert.Single(modulePlan.Functions, function => function.Name == "count");
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var count = Assert.Single(moduleResult.Functions, function => function.Name == "count");
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = CreateRuntimeModule(root);
        var ctx = new ScriptContext(domain) { Module = runtimeModule };

        Assert.True(countPlan.UsesArgumentsObject);
        Assert.False(countPlan.IsDirectCallCandidate);
        Assert.Equal(FunctionCallConvention.Span, countPlan.CallConvention);
        Assert.True(count.HasExecutableCode);
        Assert.True(run.HasExecutableCode);
        Assert.True(moduleResult.HasExecutableInitializer);

        var initialize = (ModuleInitializerDelegate)moduleResult.Initializer.CreateDelegate(typeof(ModuleInitializerDelegate));
        initialize(ctx, Span<ScriptDatum>.Empty);

        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(ctx, Span<ScriptDatum>.Empty);
        Assert.Equal(22, result.Number);
    }

    [Fact]
    public void TypedEmitterSpecializesWideNativeCallsAndKeepsDynamicFallback()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func helper(a, b, c, d, e, f, g, h, i) { return a + i; }
            export func run() { return helper(1, 2, 3, 4, 5, 6, 7, 8, 9); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helperPlan = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var helper = Assert.Single(moduleResult.Functions, function => function.Name == "helper");
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = CreateRuntimeModule(root);
        var ctx = new ScriptContext(domain) { Module = runtimeModule };

        Assert.True(helperPlan.IsDirectCallCandidate);
        Assert.Equal(FunctionVisibility.ModuleVisible, helperPlan.Visibility);
        Assert.True(helperPlan.RequiresClosureObject);
        Assert.Equal(FunctionCallConvention.Span, helperPlan.CallConvention);
        Assert.True(helper.HasExecutableCode);
        Assert.True(run.HasExecutableCode);
        Assert.True(moduleResult.HasExecutableInitializer);

        var initialize = (ModuleInitializerDelegate)moduleResult.Initializer.CreateDelegate(typeof(ModuleInitializerDelegate));
        initialize(ctx, Span<ScriptDatum>.Empty);

        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(ctx, Span<ScriptDatum>.Empty);
        Assert.Equal(10, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesWideClosureFallbackWhenNativeEmissionIsNotPossible()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func helper(a, b, c, d, e, f, g, h, i) {
                a.push(i);
                return a[0];
            }
            export func run() { return helper([], 2, 3, 4, 5, 6, 7, 8, 9); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helperPlan = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var helper = Assert.Single(moduleResult.Functions, function => function.Name == "helper");
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = CreateRuntimeModule(root);
        var ctx = new ScriptContext(domain) { Module = runtimeModule };

        Assert.True(helperPlan.IsDirectCallCandidate);
        Assert.Equal(FunctionVisibility.ModuleVisible, helperPlan.Visibility);
        Assert.True(helperPlan.RequiresClosureObject);
        Assert.Equal(FunctionCallConvention.Span, helperPlan.CallConvention);
        Assert.True(helper.HasExecutableCode);
        Assert.True(run.HasExecutableCode);

        var initialize = (ModuleInitializerDelegate)moduleResult.Initializer.CreateDelegate(typeof(ModuleInitializerDelegate));
        initialize(ctx, Span<ScriptDatum>.Empty);

        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(ctx, Span<ScriptDatum>.Empty);
        Assert.Equal(9, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesPropertyCallsWithFastAndMaterializedArguments()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(obj) {
                return obj.sum(1, 2, 3) + obj.sum(1, 2, 3, 4, 5, 6, 7, 8, 9);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);
        var receiver = new ScriptObject();
        receiver.SetPropertyValue("sum", new BondingFunction(SumArgumentCountAndValues));

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), new[] { ScriptDatum.FromObject(receiver) });
        Assert.Equal(63, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesRegexLiteralCalls()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                return /aurora/i.test('AURORA Script');
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.True(result.Boolean);
    }

    [Fact]
    public void TypedEmitterExecutesSpreadPropertyCalls()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(obj) {
                var values = [2, 3, 4];
                return obj.sum(1, ...values, 5);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);
        var receiver = new ScriptObject();
        receiver.SetPropertyValue("sum", new BondingFunction(SumArgumentCountAndValues));

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), new[] { ScriptDatum.FromObject(receiver) });
        Assert.Equal(20, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesArrayAndMapLiteralFastPaths()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var array = [1, 2, 3, 4];
                var map = { first: array.length, second: 5, third: 6 };
                return map.first + map.second + map.third;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(15, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesArrayAndMapSpreadLiterals()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var value = 3;
                var array = [1, ...[2, 3], 4];
                var source = { a: 1, b: 2 };
                var map = { ...source, value, b: 4 };
                return array.length * 1000 + array[2] * 100 + map.a * 10 + map.b + value;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(4317, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesDestructuringDeclarations()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var [first, ...middle, last] = [1, 2, 3, 4];
                var { name, age } = { name: 'Aurora', age: 6 };
                return first * 1000 + middle.length * 100 + last * 10 + age;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var runPlan = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "run");
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(1246, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesElementGetAndSet()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var array = [1, 2, 3];
                array[1] = 5;
                return array[0] + array[1] + array[2];
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(9, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesFixedPropertySet()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var obj = { value: 1, other: 2 };
                obj.value = obj.other + 3;
                return obj.value;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(5, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesConstructorFastAndMaterializedArguments()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(Type) {
                return new Type(1, 2) + new Type(1, 2, 3, 4);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), new[] { ScriptDatum.FromObject(new CountingType()) });
        Assert.Equal(19, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesSpreadConstructorCalls()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run(Type) {
                var values = [2, 3, 4];
                return new Type(1, ...values, 5);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), new[] { ScriptDatum.FromObject(new CountingType()) });
        Assert.Equal(20, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesInExpression()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var obj = { first: 1 };
                var key = "first";
                return key in obj;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.True(result.Boolean);
    }

    [Fact]
    public void TypedEmitterExecutesThrowStatement()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                throw "boom";
                return 1;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        Assert.Throws<AuroraRuntimeException>(() => runDel(CreateTestContext(), Span<ScriptDatum>.Empty));
    }

    [Fact]
    public void TypedEmitterExecutesDeleteStatement()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var obj = { first: 1, second: 2 };
                var array = [1, 2, 3];
                delete obj.first;
                delete array[1];
                return (obj.first == null) && (array[1] == null);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.True(result.Boolean);
    }

    [Fact]
    public void TypedEmitterExecutesBareTryStatement()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                try {
                    return 9;
                }
                return 1;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(9, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesTryCatchFinallyStatement()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var value = 0;
                try {
                    throw "boom";
                    value = 1;
                } catch (error) {
                    value = value + 2;
                } finally {
                    value = value + 3;
                }
                return value;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(5, result.Number);
    }

    [Fact]
    public void TypedEmitterExecutesTryFinallyStatement()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var value = 0;
                try {
                    value = 1;
                } finally {
                    value = value + 2;
                }
                return value;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(3, result.Number);
    }

    [Fact]
    public void TypedEmitterSwallowsThrowInTryFinallyWithoutCatch()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                var value = 0;
                try {
                    throw "boom";
                    value = 1;
                } finally {
                    value = value + 2;
                }
                return value;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(2, result.Number);
    }

    [Fact]
    public void TypedEmitterAcceptsDebuggerStatement()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            export func run() {
                debugger;
                return 7;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableCode);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(7, result.Number);
    }

    [Fact]
    public void CompileBlockPlanEmitsExecutableEntry()
    {
        var root = Path.GetTempPath();
        var block = ParseBlock(
            """
            function add(left, right) {
                return left + right;
            }
            return add(value, 2);
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var plan = backend.CreateCompileBlockPlan(block, ["value"], "compile-block-plan.as");
        var report = new EmissionSession(plan.Session, builder, emitExecutableCode: true).Emit();
        var entry = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Function.Equals(plan.Function.Id));

        Assert.True(entry.HasExecutableCode);
        Assert.Equal(FunctionCallConvention.Span, plan.Function.CallConvention);
        var del = (ScriptFunctionDelegate)entry.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var ctx = new ScriptContext(new AuroraEngine(options).CreateEmptyDomain(null));
        var result = del(ctx, new[] { ScriptDatum.FromNumber(40) });
        Assert.Equal(42, result.Number);
    }

    [Fact]
    public void HotPatchPlanResolvesExistingModuleMembers()
    {
        var root = Path.GetTempPath();
        var patch = Parse(
            """
            @module(TEST);
            export func version() { return oldValue + 1; }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateHotPatchPlans(patch, [], ["oldValue"], out var mainModule);
        var version = Assert.Single(mainModule.Functions, function => function.Name == "version");
        var binary = Assert.IsType<BinaryExpression>(GetSingleReturnExpression(mainModule, "version"));
        var oldValue = Assert.IsType<NameExpression>(binary.Left);
        var binding = TypedFunctionBuilder.Build(mainModule, version).GetName(oldValue);

        Assert.True(mainModule.TryGetSymbol("oldValue", out _));
        Assert.True(binding.ModuleSymbol.IsValid);
        Assert.False(binding.Local.IsValid);
        Assert.False(binding.Upvalue.IsValid);
        Assert.Single(session.Modules);
    }

    [Fact]
    public void TypedEmitterDoesNotDirectCallWhenModuleDirectCallIsDisabled()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func helper(value) { return value + 1; }
            export func run(value) { return helper(value); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helperPlan = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var runPlan = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var call = Assert.IsType<FunctionCallExpression>(GetSingleReturnExpression(modulePlan, "run"));
        var callTarget = Assert.IsType<NameExpression>(call.Target);
        var callBinding = TypedFunctionBuilder.Build(modulePlan, runPlan).GetName(callTarget);
        var report = new EmissionSession(session, builder, emitExecutableCode: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var helper = Assert.Single(moduleResult.Functions, function => function.Name == "helper");
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = CreateRuntimeModule(root);
        var ctx = new ScriptContext(domain) { Module = runtimeModule };

        Assert.False(callBinding.DirectFunction.IsValid);
        Assert.Equal(FunctionCallConvention.Span, helperPlan.CallConvention);
        Assert.True(helper.HasExecutableCode);
        Assert.True(run.HasExecutableCode);
        Assert.True(moduleResult.HasExecutableInitializer);

        var initialize = (ModuleInitializerDelegate)moduleResult.Initializer.CreateDelegate(typeof(ModuleInitializerDelegate));
        initialize(ctx, Span<ScriptDatum>.Empty);

        Assert.IsType<ClosureFunction>(runtimeModule.GetPropertyValue("helper"));
        Assert.IsType<ClosureFunction>(runtimeModule.GetPropertyValue("run"));

        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(ctx, new[] { ScriptDatum.FromNumber(41) });
        Assert.Equal(42, result.Number);
    }

    [Fact]
    public void ModuleDirectCallRejectsFunctionValueRead()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func helper(value) { return value + 1; }
            export const exposed = helper;
            export func run() { return helper(41); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var helper = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "helper");

        Assert.Equal(FunctionVisibility.ModuleVisible, helper.Visibility);
        Assert.False(helper.IsDirectCallCandidate);
    }

    [Fact]
    public void ModuleDirectCallRejectsAssignedFunctionName()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func helper(value) { return value + 1; }
            helper = 42;
            export func run() { return helper(41); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var helper = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "helper");

        Assert.Equal(FunctionVisibility.ModuleVisible, helper.Visibility);
        Assert.False(helper.IsDirectCallCandidate);
    }

    [Fact]
    public void ModuleDirectCallRejectsSpreadFunctionCall()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            var values = [41];
            func helper(value) { return value + 1; }
            export func run() { return helper(...values); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var helper = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "helper");

        Assert.Equal(FunctionVisibility.ModuleVisible, helper.Visibility);
        Assert.False(helper.IsDirectCallCandidate);
    }

    [Fact]
    public void ModuleDirectCallIgnoresShadowedParameterCall()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func helper(value) { return value + 1; }
            export func run(helper) { return helper(41); }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var helper = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "helper");

        Assert.Equal(FunctionVisibility.ModuleVisible, helper.Visibility);
        Assert.False(helper.IsDirectCallCandidate);
    }

    [Fact]
    public void ModuleDirectCallIgnoresShadowedLocalCall()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func helper(value) { return value + 1; }
            export func run() {
                var helper = 42;
                return helper(41);
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var helper = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "helper");

        Assert.Equal(FunctionVisibility.ModuleVisible, helper.Visibility);
        Assert.False(helper.IsDirectCallCandidate);
    }

    [Fact]
    public void AstTraversalVisitsBlockFunctionsWithoutChildNodeEnumeration()
    {
        var root = Path.GetTempPath();
        var module = Parse(
            """
            @module(TEST);
            func helper(value) { return value + 1; }
            export func run() { return helper(41); }
            """,
            root);
        var visitor = new CountingVisitor();

        AstTraversal.VisitDescendants(module, ref visitor);

        Assert.Equal(2, visitor.FunctionCount);
        Assert.Contains("helper", visitor.NameExpressions);
    }

    private static ModuleDeclaration Parse(string source, string root)
    {
        using var lexer = new AuroraLexer(root, new MemorySource(root, Path.Combine(root, "backend-plan-test.as"), source));
        var parser = new AuroraParser(lexer, EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root)));
        return parser.Parse();
    }

    private static ScriptModule CreateRuntimeModule(string root, string moduleName = "TEST")
    {
        var fullPath = ScriptPath.GetFullPath(root, "backend-plan-test.as");
        var modulePath = ScriptPath.GetModulePath(root, fullPath);
        return new ScriptModule(moduleName, new ScriptSourceReference(root, fullPath, modulePath));
    }

    private static AuroraScript.Compiler.Ast.Statements.BlockStatement ParseBlock(string source, string root)
    {
        using var lexer = new AuroraLexer(root, new MemorySource(root, Path.Combine(root, "backend-block-test.as"), source));
        var parser = new AuroraParser(lexer, EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root)));
        return parser.ParseBlockBody();
    }

    private static void SumArgumentCountAndValues([NotNull] ScriptContext ctx, ScriptObject target, [NotNull] Span<ScriptDatum> args, ref ScriptDatum result)
    {
        var total = args.Length;
        for (var i = 0; i < args.Length; i++)
        {
            total += (int)args[i].Number;
        }
        result = ScriptDatum.FromNumber(total);
    }

    private static void InvokeLambdaWithTwoNumbers([NotNull] ScriptContext ctx, ScriptObject target, [NotNull] Span<ScriptDatum> args, ref ScriptDatum result)
    {
        var function = Assert.IsType<ClosureFunction>(args[0].Object);
        result = function.InvokeClr(ctx, args[1], args[2]);
    }

    private static ScriptContext CreateTestContext()
    {
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var domain = new AuroraEngine(options).CreateEmptyDomain(null);
        return new ScriptContext(domain);
    }

    private delegate void ModuleInitializerDelegate(ScriptContext ctx, Span<ScriptDatum> args);

    private static Expression GetSingleReturnExpression(ModulePlan modulePlan, string functionName)
    {
        var function = Assert.Single(modulePlan.Functions, candidate => candidate.Name == functionName);
        var body = Assert.IsType<BlockStatement>(function.Declaration.Body);
        var statement = Assert.Single(body.Statements.OfType<ReturnStatement>());
        return statement.Expression;
    }

    private static void AssertInlineNumber(ModulePlan modulePlan, string name, double expected)
    {
        var constant = GetInlineConstant(modulePlan, name);
        Assert.Equal(ValueKind.Number, constant.Kind);
        Assert.Equal(expected, constant.Number, precision: 12);
    }

    private static void AssertInlineString(ModulePlan modulePlan, string name, string expected)
    {
        var constant = GetInlineConstant(modulePlan, name);
        Assert.Equal(ValueKind.String, constant.Kind);
        Assert.Equal(expected, constant.String.Value);
    }

    private static void AssertInlineBoolean(ModulePlan modulePlan, string name, bool expected)
    {
        var constant = GetInlineConstant(modulePlan, name);
        Assert.Equal(ValueKind.Boolean, constant.Kind);
        Assert.Equal(expected, constant.Boolean);
    }

    private static ScriptDatum GetInlineConstant(ModulePlan modulePlan, string name)
    {
        Assert.True(modulePlan.TryGetSymbol(name, out var symbolId));
        Assert.True(modulePlan.TryGetInlineConstant(symbolId, out var constant));
        return constant;
    }

    private sealed class CountingType : ScriptType
    {
        public CountingType() : base("Counting", true)
        {
        }

        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var total = args.Length;
            for (var i = 0; i < args.Length; i++)
            {
                total += (int)args[i].Number;
            }

            result = ScriptDatum.FromNumber(total);
        }
    }

    private sealed class RecordingBuilder : AbstractCILBuilder
    {
        private static readonly Type[] s_standardParameters = [typeof(ScriptContext), typeof(Span<ScriptDatum>)];
        private MethodInfo _entryPoint = null!;

        public RecordingBuilder(EngineOptions options) : base(options)
        {
        }

        public List<double> NumberLoads { get; } = new List<double>();
        public List<string> StringLoads { get; } = new List<string>();
        public List<bool> BooleanLoads { get; } = new List<bool>();

        public override (MethodInfo Method, ILGenerator IL) DefineDynamicMethod(ModuleDeclaration module)
        {
            var method = new DynamicMethod(module.ModuleName ?? module.Source.ModulePath ?? "AuroraModule", typeof(ScriptDatum), s_standardParameters, typeof(RecordingBuilder).Module, true);
            return (method, method.GetILGenerator());
        }

        public override (MethodInfo Method, ILGenerator IL) DefineBlockMethod(string methodName)
        {
            var method = new DynamicMethod(methodName, typeof(ScriptDatum), s_standardParameters, typeof(RecordingBuilder).Module, true);
            return (method, method.GetILGenerator());
        }

        public override (MethodInfo Method, ILGenerator IL) DefineModuleInitMethod(ModuleDeclaration module)
        {
            var method = new DynamicMethod("Initialize", typeof(void), s_standardParameters, typeof(RecordingBuilder).Module, true);
            return (method, method.GetILGenerator());
        }

        public override (MethodInfo Method, ILGenerator IL) DefineDomainInitMethod()
        {
            var method = new DynamicMethod(EntryPointMethodName, typeof(ScriptDatum), s_standardParameters, typeof(RecordingBuilder).Module, true);
            _entryPoint = method;
            return (method, method.GetILGenerator());
        }

        public override (MethodInfo Method, ILGenerator IL) DefineMethod(
            string moduleKey,
            string methodName,
            Type returnType,
            Type[] parameterTypes,
            bool aggressiveInlining = false)
        {
            var method = new DynamicMethod(methodName, returnType, parameterTypes, typeof(RecordingBuilder).Module, true);
            return (method, method.GetILGenerator());
        }

        public override MethodInfo GetRuntimeEntryPoint()
        {
            return _entryPoint;
        }

        public override void SetLocalSymInfo(LocalBuilder local, string name)
        {
        }

        public override void MarkSequencePoint(AstNode node, ILGenerator il)
        {
        }

        public override void MarkSequencePoint(SourceSpan range, ILGenerator il)
        {
        }

        public override LoadState LoadNumber(ILGenerator il, double number)
        {
            NumberLoads.Add(number);
            return base.LoadNumber(il, number);
        }

        public override LoadState LoadString(ILGenerator il, string value)
        {
            StringLoads.Add(value);
            return base.LoadString(il, value);
        }

        public override LoadState LoadBoolean(ILGenerator il, bool b)
        {
            BooleanLoads.Add(b);
            return base.LoadBoolean(il, b);
        }
    }

    private static void CollectNames(Expression expression, List<NameExpression> names)
    {
        switch (expression)
        {
            case null:
                return;
            case NameExpression name:
                names.Add(name);
                return;
            case BinaryExpression binary:
                CollectNames(binary.Left, names);
                CollectNames(binary.Right, names);
                return;
            case FunctionCallExpression call:
                CollectNames(call.Target, names);
                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    CollectNames(call.Arguments[i], names);
                }
                return;
        }
    }

    private struct CountingVisitor : IAstChildVisitor
    {
        private List<string> _nameExpressions;

        public int FunctionCount;
        public List<string> NameExpressions => _nameExpressions ??= new List<string>();

        public void Visit(AstNode node)
        {
            if (node is FunctionDeclaration)
            {
                FunctionCount++;
            }
            else if (node is AuroraScript.Compiler.Ast.Expressions.NameExpression name)
            {
                NameExpressions.Add(name.Identifier.Value);
            }
        }
    }
}
