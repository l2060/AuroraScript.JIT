using AuroraScript.Compiler;
using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Emission;
using AuroraScript.Compiler.Backend.Lowering;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
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
    public void GlobalPredefineCreatesModuleAndFunctionPlans()
    {
        var root = Path.GetTempPath();
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
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
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
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
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
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
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
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
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(Path.GetTempPath()));
        var engine = new AuroraEngine(options);

        var error = Assert.Throws<AuroraCompilationException>(() => engine.CompileBlock(body));

        Assert.Contains("Cannot assign to constant 'a'", error.Message);
    }

    [Fact]
    public void CompileBlockRejectsDuplicateDeclarationInSameBlock()
    {
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(Path.GetTempPath()));
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
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(Path.GetTempPath()));
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
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(Path.GetTempPath()));
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
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(Path.GetTempPath()));
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
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(Path.GetTempPath()));
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
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
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
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
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
        var options = EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(root)).WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helper = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var runPlan = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var runReturn = Assert.IsType<LoweredReturnStatement>(Assert.Single(runPlan.Body.Statements));
        var call = Assert.IsType<LoweredCallExpression>(runReturn.Expression);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var initialize = (ModuleInitializerDelegate)moduleResult.Initializer.CreateDelegate(typeof(ModuleInitializerDelegate));
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = new ScriptModule("TEST", root);
        var ctx = new ScriptContext(domain) { Module = runtimeModule };

        Assert.Equal(DirectCallDirective.PreserveClosure, helper.DirectCallDirective);
        Assert.True(helper.IsDirectCallCandidate);
        Assert.True(helper.RequiresClosureObject);
        Assert.True(call.DirectFunction.Equals(helper.Id));

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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helper = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var returnStatement = Assert.IsType<LoweredReturnStatement>(Assert.Single(run.Body.Statements));
        var call = Assert.IsType<LoweredCallExpression>(returnStatement.Expression);

        Assert.Equal(FunctionVisibility.Exported, helper.Visibility);
        Assert.True(helper.IsDirectCallCandidate);
        Assert.True(helper.RequiresClosureObject);
        Assert.True(call.DirectFunction.Equals(helper.Id));
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = true)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helper = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var returnStatement = Assert.IsType<LoweredReturnStatement>(Assert.Single(run.Body.Statements));
        var call = Assert.IsType<LoweredCallExpression>(returnStatement.Expression);

        Assert.True(session.Capabilities.CanUseModuleDirectCall);
        Assert.True(helper.IsDirectCallCandidate);
        Assert.True(call.DirectFunction.Equals(helper.Id));
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
    public void LoweringResolvesNamesToLocalUpvalueAndModuleSymbols()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var outer = Assert.Single(modulePlan.Functions, function => function.Name == "outer");
        var inner = Assert.Single(modulePlan.Functions, function => function.Name == "inner");

        Assert.NotNull(outer.Body);
        Assert.NotNull(inner.Body);
        var outerReturn = Assert.IsType<LoweredReturnStatement>(Assert.Single(outer.Body.Statements.OfType<LoweredReturnStatement>()));
        var outerCall = Assert.IsType<LoweredCallExpression>(outerReturn.Expression);
        var outerTarget = Assert.IsType<LoweredNameExpression>(outerCall.Target);
        Assert.True(outerTarget.LocalSlot.IsValid);
        Assert.Equal("inner", outerTarget.Name);

        var innerReturn = Assert.IsType<LoweredReturnStatement>(Assert.Single(inner.Body.Statements));
        var names = new List<LoweredNameExpression>();
        CollectLoweredNames(innerReturn.Expression, names);

        Assert.Contains(names, name => name.Name == "local" && name.UpvalueSlot.IsValid);
        Assert.Contains(names, name => name.Name == "delta" && name.LocalSlot.IsValid);
        Assert.Contains(names, name => name.Name == "MODULE_VALUE" && name.ModuleSymbol.IsValid);
        Assert.Equal(0, outer.UnsupportedLoweredStatementCount);
        Assert.Equal(0, outer.UnsupportedLoweredExpressionCount);
        Assert.Empty(outer.UnsupportedLoweredNodes);
        Assert.Equal(0, inner.UnsupportedLoweredStatementCount);
        Assert.Equal(0, inner.UnsupportedLoweredExpressionCount);
        Assert.Empty(inner.UnsupportedLoweredNodes);
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var returned = GetSingleReturnExpression(modulePlan, "run");
        var name = Assert.IsType<LoweredNameExpression>(returned);

        Assert.False(modulePlan.HasInlineConstants);
        Assert.Equal("a5", name.Name);
        Assert.True(name.ModuleSymbol.IsValid);
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = true)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var returned = GetSingleReturnExpression(modulePlan, "run");
        var literal = Assert.IsType<LoweredLiteralExpression>(returned);
        var number = Assert.IsType<NumberToken>(literal.Token);

        Assert.True(modulePlan.HasInlineConstants);
        Assert.True(modulePlan.TryGetSymbol("a5", out var symbolId));
        Assert.True(modulePlan.TryGetInlineConstant(symbolId, out var constant));
        Assert.Equal(ValueKind.Number, constant.Kind);
        Assert.Equal(5, constant.Number);
        Assert.Equal(5, number.NumberValue);
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);
        var expectedNum = 3.141592678987654321d;

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var returned = GetSingleReturnExpression(modulePlan, "run");
        var array = Assert.IsType<LoweredArrayLiteralExpression>(returned);

        AssertInlineNumber(modulePlan, "NUM", expectedNum);
        AssertInlineString(modulePlan, "STR", "this is string");
        AssertInlineBoolean(modulePlan, "BOOL", true);
        AssertInlineNumber(modulePlan, "BASE", 10);
        AssertInlineNumber(modulePlan, "COMPLEX", 10 * expectedNum + 5);
        AssertInlineString(modulePlan, "TAG", "10_1");
        AssertInlineString(modulePlan, "TEMPLATE", "this is string10_10_1");
        Assert.Equal(6, array.Elements.Length);
        Assert.All(array.Elements, element => Assert.IsType<LoweredLiteralExpression>(element));
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var builder = new RecordingBuilder(options);
        var backend = new BackendCompiler(builder, options);
        var expectedNum = 3.141592678987654321d;

        var session = backend.CreateModulePlans([module]);
        new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();

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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var returned = GetSingleReturnExpression(modulePlan, "run");
        var name = Assert.IsType<LoweredNameExpression>(returned);

        Assert.True(modulePlan.TryGetSymbol("fv", out var symbolId));
        Assert.False(modulePlan.TryGetInlineConstant(symbolId, out _));
        Assert.Equal("fv", name.Name);
        Assert.True(name.ModuleSymbol.IsValid);
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var returned = GetSingleReturnExpression(modulePlan, "run");
        var name = Assert.IsType<LoweredNameExpression>(returned);

        Assert.True(modulePlan.TryGetSymbol("a1", out var a1Symbol));
        Assert.True(modulePlan.TryGetInlineConstant(a1Symbol, out _));
        Assert.True(modulePlan.TryGetSymbol("a5", out var a5Symbol));
        Assert.False(modulePlan.TryGetInlineConstant(a5Symbol, out _));
        Assert.Equal("a5", name.Name);
        Assert.True(name.ModuleSymbol.IsValid);
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var assignmentStatement = Assert.IsType<LoweredExpressionStatement>(run.Body.Statements[0]);
        var assignment = Assert.IsType<LoweredAssignmentExpression>(assignmentStatement.Expression);
        var assignmentTarget = Assert.IsType<LoweredNameExpression>(assignment.Left);
        var incrementStatement = Assert.IsType<LoweredExpressionStatement>(run.Body.Statements[1]);
        var increment = Assert.IsType<LoweredUnaryExpression>(incrementStatement.Expression);
        var incrementTarget = Assert.IsType<LoweredNameExpression>(increment.Expression);
        var returnStatement = Assert.IsType<LoweredReturnStatement>(run.Body.Statements[2]);

        Assert.Equal("value", assignmentTarget.Name);
        Assert.True(assignmentTarget.ModuleSymbol.IsValid);
        Assert.Equal("value", incrementTarget.Name);
        Assert.True(incrementTarget.ModuleSymbol.IsValid);
        Assert.IsType<LoweredLiteralExpression>(returnStatement.Expression);
    }

    [Fact]
    public void LoweringMarksModuleDirectCallTarget()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helper = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var run = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var runReturn = Assert.IsType<LoweredReturnStatement>(Assert.Single(run.Body.Statements));
        var call = Assert.IsType<LoweredCallExpression>(runReturn.Expression);

        Assert.True(helper.IsDirectCallCandidate);
        Assert.Equal(FunctionVisibility.InternalOnly, helper.Visibility);
        Assert.Equal(helper.Id, call.DirectFunction);
    }

    [Fact]
    public void LoweringRepresentsLambdaExpressionsByFunctionId()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var test = Assert.Single(modulePlan.Functions, function => function.Name == "test");
        var lambda = Assert.Single(modulePlan.Functions, function => function.IsLambda);

        var loweredReturn = Assert.IsType<LoweredReturnStatement>(Assert.Single(test.Body.Statements));
        var loweredLambda = Assert.IsType<LoweredLambdaExpression>(loweredReturn.Expression);
        Assert.Equal(lambda.Id, loweredLambda.Function);
    }

    [Fact]
    public void LoweringRepresentsControlFlowAndHighFrequencyOperators()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var test = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "test");

        Assert.Empty(test.UnsupportedLoweredNodes);
        var expressionStatements = test.Body.Statements.OfType<LoweredExpressionStatement>().ToArray();
        Assert.IsType<LoweredAssignmentExpression>(expressionStatements[0].Expression);
        Assert.IsType<LoweredCompoundExpression>(expressionStatements[1].Expression);

        var ifStatement = Assert.Single(test.Body.Statements.OfType<LoweredIfStatement>());
        Assert.IsType<LoweredBinaryExpression>(ifStatement.Condition);
        Assert.IsType<LoweredBlockStatement>(ifStatement.Body);
        Assert.IsType<LoweredBlockStatement>(ifStatement.Else);

        var whileStatement = Assert.Single(test.Body.Statements.OfType<LoweredWhileStatement>());
        var whileBody = Assert.IsType<LoweredBlockStatement>(whileStatement.Body);
        Assert.IsType<LoweredBreakStatement>(Assert.Single(whileBody.Statements));

        var forStatement = Assert.Single(test.Body.Statements.OfType<LoweredForStatement>());
        Assert.IsType<LoweredVariableDeclarationStatement>(forStatement.Initializer);
        Assert.IsType<LoweredBinaryExpression>(forStatement.Condition);
        Assert.IsType<LoweredUnaryExpression>(forStatement.Incrementor);
        var forBody = Assert.IsType<LoweredBlockStatement>(forStatement.Body);
        Assert.IsType<LoweredContinueStatement>(Assert.Single(forBody.Statements));
    }

    [Fact]
    public void LoweringRepresentsForInAndExceptionStatements()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var test = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "test");

        Assert.Empty(test.UnsupportedLoweredNodes);
        var forIn = Assert.Single(test.Body.Statements.OfType<LoweredForInStatement>());
        Assert.IsType<LoweredVariableDeclarationStatement>(forIn.Initializer);
        Assert.NotNull(forIn.Iterator);
        Assert.IsType<LoweredNameExpression>(forIn.Iterator.Left);
        var forInBody = Assert.IsType<LoweredBlockStatement>(forIn.Body);
        Assert.IsType<LoweredContinueStatement>(Assert.Single(forInBody.Statements));

        var tryStatement = Assert.Single(test.Body.Statements.OfType<LoweredTryStatement>());
        Assert.Equal("error", tryStatement.CatchVariable);
        Assert.True(tryStatement.CatchSlot.IsValid);
        Assert.Contains(test.LocalSlots, slot => slot.Name == "error" && slot.Id.Equals(tryStatement.CatchSlot));
        var tryBody = Assert.IsType<LoweredBlockStatement>(tryStatement.Body);
        Assert.IsType<LoweredThrowStatement>(Assert.Single(tryBody.Statements));
        var catchBody = Assert.IsType<LoweredBlockStatement>(tryStatement.CatchBody);
        Assert.IsType<LoweredDeleteStatement>(catchBody.Statements[0]);
        Assert.IsType<LoweredDebuggerStatement>(catchBody.Statements[1]);
        var finallyBody = Assert.IsType<LoweredBlockStatement>(tryStatement.FinallyBody);
        Assert.IsType<LoweredDebuggerStatement>(Assert.Single(finallyBody.Statements));
    }

    [Fact]
    public void LoweringRepresentsObjectArrayMapAndConstructorExpressions()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var test = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "test");

        Assert.Empty(test.UnsupportedLoweredNodes);
        var declarations = test.Body.Statements.OfType<LoweredVariableDeclarationStatement>().ToArray();
        var array = Assert.IsType<LoweredArrayLiteralExpression>(declarations[0].Initializer);
        Assert.Contains(array.Elements, expression => expression is LoweredSpreadExpression);
        Assert.Contains(array.Elements, expression => expression is LoweredGetElementExpression);
        Assert.Contains(array.Elements, expression => expression is LoweredGetPropertyExpression);

        var map = Assert.IsType<LoweredMapExpression>(declarations[1].Initializer);
        Assert.Equal(3, map.Entries.Length);
        Assert.Contains(map.Entries, entry => entry.Key?.Value == "first" && entry.Value is LoweredGetPropertyExpression);
        Assert.Contains(map.Entries, entry => entry.Key?.Value == "second" && entry.Value is LoweredGetElementExpression);
        Assert.Contains(map.Entries, entry => entry.Key == null && entry.Value is LoweredSpreadExpression);

        var expressionStatements = test.Body.Statements.OfType<LoweredExpressionStatement>().ToArray();
        var setProperty = Assert.IsType<LoweredSetPropertyExpression>(expressionStatements[0].Expression);
        Assert.IsType<LoweredNewExpression>(setProperty.Value);
        Assert.IsType<LoweredSetElementExpression>(expressionStatements[1].Expression);
    }

    [Fact]
    public void LoweringCountsUnsupportedNodes()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var test = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "test");

        Assert.True(test.UnsupportedLoweredStatementCount > 0);
        Assert.Contains(test.UnsupportedLoweredNodes, node => node.NodeType == "EnumDeclaration" && !node.IsExpression);
        Assert.Equal(test.UnsupportedLoweredStatementCount, test.UnsupportedLoweredNodes.Count(node => !node.IsExpression));
        Assert.Equal(test.UnsupportedLoweredExpressionCount, test.UnsupportedLoweredNodes.Count(node => node.IsExpression));
    }

    [Fact]
    public void LoweringRepresentsDestructuringDeclarations()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var test = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "test");

        Assert.Empty(test.UnsupportedLoweredNodes);
        var array = Assert.IsType<LoweredArrayDestructuringDeclarationStatement>(test.Body.Statements[0]);
        Assert.Equal(3, array.Bindings.Length);
        Assert.False(array.Bindings[0].IsRest);
        Assert.True(array.Bindings[1].IsRest);
        Assert.Equal(1, array.Bindings[1].TrailingCount);
        Assert.Equal(1, array.Bindings[2].TrailingCount);

        var obj = Assert.IsType<LoweredObjectDestructuringDeclarationStatement>(test.Body.Statements[1]);
        Assert.Equal(new[] { "name", "age" }, obj.Bindings.Select(binding => binding.Property.Value).ToArray());
    }

    [Fact]
    public void EmissionPassConsumesSupportedLoweredPlan()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
    public void EmissionPassRejectsUnsupportedLoweredNodes()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
    public void EmissionPassConsumesLoweredBodyInsteadOfAst()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var runPlan = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "run");
        runPlan.Body = new LoweredBlockStatement(runPlan.Declaration.Body, Array.Empty<LoweredStatement>());

        var report = new EmissionSession(session, builder, collectDiagnostics: true).Emit();

        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");
        Assert.Equal(1, run.StatementCount);
        Assert.Equal(0, run.ExpressionCount);
    }

    [Fact]
    public void EmissionSkeletonExecutesLiteralReturn()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        Assert.Equal(0, run.CilLocalCount);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(42, result.Number);
    }

    [Fact]
    public void EmissionSkeletonStoresAndLoadsLocal()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        Assert.Equal(1, run.CilLocalCount);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal("ok", ScriptDatum.ToString(result));
    }

    [Fact]
    public void EmissionSkeletonInitializesParameterLocalsFromSpanArguments()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        Assert.Equal(1, run.CilLocalCount);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var args = new ScriptDatum[1];
        args[0] = ScriptDatum.FromNumber(7);
        var result = del(CreateTestContext(), args);
        Assert.Equal(7, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesBinaryArithmeticAndComparison()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var args = new[] { ScriptDatum.FromNumber(3), ScriptDatum.FromNumber(4) };
        var result = del(CreateTestContext(), args);
        Assert.Equal(1, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesLocalAssignment()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(5, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesCompoundAndUnaryLocalOperators()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(64, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesElementCompoundAddOnce()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(31, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesPropertyAndElementUnaryMutation()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(22541, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesLogicalShortCircuitAndBitwiseOperators()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(14, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesWhileLoop()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), new[] { ScriptDatum.FromNumber(4) });
        Assert.Equal(10, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesForLoopWithBreakAndContinue()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), new[] { ScriptDatum.FromNumber(8) });
        Assert.Equal(8, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesForInAcrossArrayObjectAndString()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(323, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesForInWithBreakAndContinue()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(4, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesForwardModuleDirectCallWithFastArity()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helperPlan = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var helper = Assert.Single(moduleResult.Functions, function => function.Name == "helper");

        Assert.True(run.HasExecutableSkeleton);
        Assert.True(helper.HasExecutableSkeleton);
        Assert.Equal(FunctionCallConvention.Fast2, helperPlan.CallConvention);
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var ctx = new ScriptContext(domain) { Module = new ScriptModule("TEST", root) };
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helperPlan = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var initialize = (ModuleInitializerDelegate)moduleResult.Initializer.CreateDelegate(typeof(ModuleInitializerDelegate));
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = new ScriptModule("TEST", root);
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
    public void EmissionSkeletonHoistsUncapturedLocalFunctionDeclarations()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helperPlan = Assert.Single(modulePlan.Functions, function => function.Name == "helper" && !function.IsModuleFunction);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var helper = Assert.Single(moduleResult.Functions, function => function.Function.Equals(helperPlan.Id));
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var ctx = new ScriptContext(domain) { Module = new ScriptModule("TEST", root) };

        Assert.True(run.HasExecutableSkeleton);
        Assert.True(helper.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(ctx, new[] { ScriptDatum.FromNumber(2) });
        Assert.Equal(11, result.Number);
    }

    [Fact]
    public void EmissionSkeletonEvaluatesExtraDirectCallArgumentsInOrder()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        Assert.Equal(3, run.CilLocalCount);
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var ctx = new ScriptContext(domain) { Module = new ScriptModule("TEST", root) };
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(ctx, Span<ScriptDatum>.Empty);
        Assert.Equal(4, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesRegularFunctionObjectCallsWithFastArity()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");
        var callback = new BondingFunction(SumArgumentCountAndValues);

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), new[] { ScriptDatum.FromObject(callback) });
        Assert.Equal(40, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesRegularFunctionObjectCallsWithMaterializedArguments()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");
        var callback = new BondingFunction(SumArgumentCountAndValues);

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), new[] { ScriptDatum.FromObject(callback) });
        Assert.Equal(63, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesSpreadFunctionObjectCalls()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");
        var callback = new BondingFunction(SumArgumentCountAndValues);

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(CreateTestContext(), new[] { ScriptDatum.FromObject(callback) });
        Assert.Equal(21, result.Number);
    }

    [Fact]
    public void EmissionSkeletonMaterializesUncapturedLambdaArguments()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var lambdaPlan = Assert.Single(modulePlan.Functions, function => function.IsLambda);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var lambda = Assert.Single(moduleResult.Functions, function => function.Function.Equals(lambdaPlan.Id));
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var ctx = new ScriptContext(domain) { Module = new ScriptModule("TEST", root) };
        var callback = new BondingFunction(InvokeLambdaWithTwoNumbers);

        Assert.True(lambda.HasExecutableSkeleton);
        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(ctx, new[] { ScriptDatum.FromObject(callback) });
        Assert.Equal(5, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesDefaultParameterFunctions()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var addPlan = Assert.Single(modulePlan.Functions, function => function.Name == "add");
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var add = Assert.Single(moduleResult.Functions, function => function.Name == "add");
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = new ScriptModule("TEST", root);
        var ctx = new ScriptContext(domain) { Module = runtimeModule };

        Assert.True(addPlan.HasDefaultParameters);
        Assert.Equal(FunctionCallConvention.Span, addPlan.CallConvention);
        Assert.True(add.HasExecutableSkeleton);
        Assert.True(run.HasExecutableSkeleton);
        Assert.True(moduleResult.HasExecutableInitializer);

        var initialize = (ModuleInitializerDelegate)moduleResult.Initializer.CreateDelegate(typeof(ModuleInitializerDelegate));
        initialize(ctx, Span<ScriptDatum>.Empty);

        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(ctx, Span<ScriptDatum>.Empty);
        Assert.Equal(15, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesArgsObjectFunctions()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var countPlan = Assert.Single(modulePlan.Functions, function => function.Name == "count");
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var count = Assert.Single(moduleResult.Functions, function => function.Name == "count");
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = new ScriptModule("TEST", root);
        var ctx = new ScriptContext(domain) { Module = runtimeModule };

        Assert.True(countPlan.UsesArgumentsObject);
        Assert.False(countPlan.IsDirectCallCandidate);
        Assert.Equal(FunctionCallConvention.Span, countPlan.CallConvention);
        Assert.True(count.HasExecutableSkeleton);
        Assert.True(run.HasExecutableSkeleton);
        Assert.True(moduleResult.HasExecutableInitializer);

        var initialize = (ModuleInitializerDelegate)moduleResult.Initializer.CreateDelegate(typeof(ModuleInitializerDelegate));
        initialize(ctx, Span<ScriptDatum>.Empty);

        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(ctx, Span<ScriptDatum>.Empty);
        Assert.Equal(22, result.Number);
    }

    [Fact]
    public void EmissionSkeletonMaterializesWideModuleCallsInsteadOfInvalidDirectCall()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helperPlan = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var helper = Assert.Single(moduleResult.Functions, function => function.Name == "helper");
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = new ScriptModule("TEST", root);
        var ctx = new ScriptContext(domain) { Module = runtimeModule };

        Assert.False(helperPlan.IsDirectCallCandidate);
        Assert.Equal(FunctionVisibility.ModuleVisible, helperPlan.Visibility);
        Assert.Equal(FunctionCallConvention.Span, helperPlan.CallConvention);
        Assert.True(helper.HasExecutableSkeleton);
        Assert.True(run.HasExecutableSkeleton);
        Assert.True(moduleResult.HasExecutableInitializer);

        var initialize = (ModuleInitializerDelegate)moduleResult.Initializer.CreateDelegate(typeof(ModuleInitializerDelegate));
        initialize(ctx, Span<ScriptDatum>.Empty);

        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(ctx, Span<ScriptDatum>.Empty);
        Assert.Equal(10, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesPropertyCallsWithFastAndMaterializedArguments()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);
        var receiver = new ScriptObject();
        receiver.SetPropertyValue("sum", new BondingFunction(SumArgumentCountAndValues));

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), new[] { ScriptDatum.FromObject(receiver) });
        Assert.Equal(63, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesRegexLiteralCalls()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.True(result.Boolean);
    }

    [Fact]
    public void EmissionSkeletonExecutesSpreadPropertyCalls()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);
        var receiver = new ScriptObject();
        receiver.SetPropertyValue("sum", new BondingFunction(SumArgumentCountAndValues));

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), new[] { ScriptDatum.FromObject(receiver) });
        Assert.Equal(20, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesArrayAndMapLiteralFastPaths()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(15, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesArrayAndMapSpreadLiterals()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(4317, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesDestructuringDeclarations()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var runPlan = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "run");
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.Empty(runPlan.UnsupportedLoweredNodes);
        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(1246, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesElementGetAndSet()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(9, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesFixedPropertySet()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(5, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesConstructorFastAndMaterializedArguments()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), new[] { ScriptDatum.FromObject(new CountingType()) });
        Assert.Equal(19, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesSpreadConstructorCalls()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), new[] { ScriptDatum.FromObject(new CountingType()) });
        Assert.Equal(20, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesInExpression()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.True(result.Boolean);
    }

    [Fact]
    public void EmissionSkeletonExecutesThrowStatement()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        Assert.Throws<AuroraRuntimeException>(() => runDel(CreateTestContext(), Span<ScriptDatum>.Empty));
    }

    [Fact]
    public void EmissionSkeletonExecutesDeleteStatement()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.True(result.Boolean);
    }

    [Fact]
    public void EmissionSkeletonExecutesBareTryStatement()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(9, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesTryCatchFinallyStatement()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(5, result.Number);
    }

    [Fact]
    public void EmissionSkeletonExecutesTryFinallyStatement()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(3, result.Number);
    }

    [Fact]
    public void EmissionSkeletonSwallowsThrowInTryFinallyWithoutCatch()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(CreateTestContext(), Span<ScriptDatum>.Empty);
        Assert.Equal(2, result.Number);
    }

    [Fact]
    public void EmissionSkeletonAcceptsDebuggerStatement()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var plan = backend.CreateCompileBlockPlan(block, ["value"], "compile-block-plan.as");
        var report = new EmissionSession(plan.Session, builder, emitExecutableSkeletons: true).Emit();
        var entry = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Function.Equals(plan.Function.Id));

        Assert.True(entry.HasExecutableSkeleton);
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = true);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateHotPatchPlans(patch, [], ["oldValue"], out var mainModule);
        var version = Assert.Single(mainModule.Functions, function => function.Name == "version");
        var returnStatement = Assert.IsType<LoweredReturnStatement>(Assert.Single(version.Body.Statements));
        var binary = Assert.IsType<LoweredBinaryExpression>(returnStatement.Expression);
        var oldValue = Assert.IsType<LoweredNameExpression>(binary.Left);

        Assert.True(mainModule.TryGetSymbol("oldValue", out _));
        Assert.True(oldValue.ModuleSymbol.IsValid);
        Assert.False(oldValue.LocalSlot.IsValid);
        Assert.False(oldValue.UpvalueSlot.IsValid);
        Assert.Single(session.Modules);
    }

    [Fact]
    public void EmissionSkeletonDoesNotDirectCallWhenModuleDirectCallIsDisabled()
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);
        var helperPlan = Assert.Single(modulePlan.Functions, function => function.Name == "helper");
        var runPlan = Assert.Single(modulePlan.Functions, function => function.Name == "run");
        var runReturn = Assert.IsType<LoweredReturnStatement>(Assert.Single(runPlan.Body.Statements));
        var call = Assert.IsType<LoweredCallExpression>(runReturn.Expression);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var moduleResult = Assert.Single(report.Modules);
        var helper = Assert.Single(moduleResult.Functions, function => function.Name == "helper");
        var run = Assert.Single(moduleResult.Functions, function => function.Name == "run");
        var engine = new AuroraEngine(options);
        var domain = engine.CreateEmptyDomain(null);
        var runtimeModule = new ScriptModule("TEST", root);
        var ctx = new ScriptContext(domain) { Module = runtimeModule };

        Assert.False(call.DirectFunction.IsValid);
        Assert.Equal(FunctionCallConvention.Span, helperPlan.CallConvention);
        Assert.True(helper.HasExecutableSkeleton);
        Assert.True(run.HasExecutableSkeleton);
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
            .WithCompiler(compiler => compiler.WithDirectory(root))
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
        using var lexer = new AuroraLexer(root, new TextSource(root, Path.Combine(root, "backend-plan-test.as"), source));
        var parser = new AuroraParser(lexer, EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(root)));
        return parser.Parse();
    }

    private static AuroraScript.Compiler.Ast.Statements.BlockStatement ParseBlock(string source, string root)
    {
        using var lexer = new AuroraLexer(root, new TextSource(root, Path.Combine(root, "backend-block-test.as"), source));
        var parser = new AuroraParser(lexer, EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(root)));
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

    private static LoweredExpression GetSingleReturnExpression(ModulePlan modulePlan, string functionName)
    {
        var function = Assert.Single(modulePlan.Functions, candidate => candidate.Name == functionName);
        var statement = Assert.IsType<LoweredReturnStatement>(Assert.Single(function.Body.Statements));
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
            var method = new DynamicMethod(module.ModuleName, typeof(ScriptDatum), s_standardParameters, typeof(RecordingBuilder).Module, true);
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

        public override (MethodInfo Method, ILGenerator IL) DefineMethod(string moduleName, string methodName, Type returnType, Type[] parameterTypes)
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

    private static void CollectLoweredNames(LoweredExpression expression, List<LoweredNameExpression> names)
    {
        switch (expression)
        {
            case null:
                return;
            case LoweredNameExpression name:
                names.Add(name);
                return;
            case LoweredBinaryExpression binary:
                CollectLoweredNames(binary.Left, names);
                CollectLoweredNames(binary.Right, names);
                return;
            case LoweredCallExpression call:
                CollectLoweredNames(call.Target, names);
                for (var i = 0; i < call.Arguments.Length; i++)
                {
                    CollectLoweredNames(call.Arguments[i], names);
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
