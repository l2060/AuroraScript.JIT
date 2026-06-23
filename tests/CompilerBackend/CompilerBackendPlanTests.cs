using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Emission;
using AuroraScript.Compiler.Backend.Lowering;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using AuroraScript.Compiler.Emits.Builders;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Xunit;

namespace AuroraScript.CompilerBackend.Tests;

public sealed class CompilerBackendPlanTests
{
    [Fact]
    public void GlobalPredefineCreatesModuleAndFunctionPlans()
    {
        var root = Path.GetTempPath();
        var options = EngineOptions.Default
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
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
    public void HotReloadDisablesModuleDirectCallCapability()
    {
        var options = EngineOptions.Default
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(true)
            .WithEnableModuleDirectCall(true);

        var capabilities = CompilationModeCapabilities.FromOptions(options);

        Assert.False(capabilities.CanUseModuleDirectCall);
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
        var options = EngineOptions.Default.WithBaseDirectory(root).WithCompilationMode(CompilationMode.Dynamic);
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
    public void GlobalPredefineKeepsFirstDuplicateModuleSymbol()
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
        var options = EngineOptions.Default.WithBaseDirectory(root).WithCompilationMode(CompilationMode.Dynamic);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var modulePlan = Assert.Single(session.Modules);

        Assert.True(modulePlan.TryGetSymbol("value", out var symbolId));
        Assert.Equal(BackendSymbolKind.ModuleProperty, session.Symbols[symbolId].Kind);
        Assert.Equal(1, session.Scopes[modulePlan.ModuleScope].SymbolCount);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var helper = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "helper");

        Assert.Equal(FunctionVisibility.ModuleVisible, helper.Visibility);
        Assert.False(helper.IsDirectCallCandidate);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
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
                return arguments;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
                yield;
                return first;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var backend = new BackendCompiler(new DynamicBuilder(options), options);

        var session = backend.CreateModulePlans([module]);
        var test = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "test");

        Assert.True(test.UnsupportedLoweredStatementCount > 0);
        Assert.Contains(test.UnsupportedLoweredNodes, node => node.NodeType == "VariableDeclaration" && !node.IsExpression);
        Assert.Contains(test.UnsupportedLoweredNodes, node => node.NodeType == "YieldStatement" && !node.IsExpression);
        Assert.Equal(test.UnsupportedLoweredStatementCount, test.UnsupportedLoweredNodes.Count(node => !node.IsExpression));
        Assert.Equal(test.UnsupportedLoweredExpressionCount, test.UnsupportedLoweredNodes.Count(node => node.IsExpression));
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder).Emit();
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
                yield;
                return 1;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var exception = Assert.Throws<UnsupportedEmissionException>(() => new EmissionSession(session, builder).Emit());

        Assert.Equal("YieldStatement", exception.NodeType);
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
                yield;
                return 1;
            }
            """,
            root);
        var options = EngineOptions.Default
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var runPlan = Assert.Single(Assert.Single(session.Modules).Functions, function => function.Name == "run");
        runPlan.Body = new LoweredBlockStatement(runPlan.Declaration.Body, Array.Empty<LoweredStatement>());

        var report = new EmissionSession(session, builder).Emit();

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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        Assert.Equal(0, run.CilLocalCount);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        Assert.Equal(1, run.CilLocalCount);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
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
        var result = del(null, args);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var args = new[] { ScriptDatum.FromNumber(3), ScriptDatum.FromNumber(4) };
        var result = del(null, args);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(null, Span<ScriptDatum>.Empty);
        Assert.Equal(64, result.Number);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(null, new[] { ScriptDatum.FromNumber(4) });
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(null, new[] { ScriptDatum.FromNumber(8) });
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
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
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var runResult = runDel(null, new[] { ScriptDatum.FromNumber(5) });
        Assert.Equal(11, runResult.Number);

        var helperDel = (ScriptFunctionDelegate2)helper.Method.CreateDelegate(typeof(ScriptFunctionDelegate2));
        var helperResult = helperDel(null, ScriptDatum.FromNumber(6), ScriptDatum.FromNumber(7));
        Assert.Equal(13, helperResult.Number);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        Assert.Equal(2, run.CilLocalCount);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");
        var callback = new BondingFunction(SumArgumentCountAndValues);

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(null, new[] { ScriptDatum.FromObject(callback) });
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");
        var callback = new BondingFunction(SumArgumentCountAndValues);

        Assert.True(run.HasExecutableSkeleton);
        var del = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = del(null, new[] { ScriptDatum.FromObject(callback) });
        Assert.Equal(63, result.Number);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);
        var receiver = new ScriptObject();
        receiver.SetPropertyValue("sum", new BondingFunction(SumArgumentCountAndValues));

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(null, new[] { ScriptDatum.FromObject(receiver) });
        Assert.Equal(63, result.Number);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(null, Span<ScriptDatum>.Empty);
        Assert.Equal(4317, result.Number);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(null, new[] { ScriptDatum.FromObject(new CountingType()) });
        Assert.Equal(19, result.Number);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        Assert.Throws<AuroraRuntimeException>(() => runDel(null, Span<ScriptDatum>.Empty));
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(null, Span<ScriptDatum>.Empty);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(null, Span<ScriptDatum>.Empty);
        Assert.Equal(3, result.Number);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
        var builder = new DynamicBuilder(options);
        var backend = new BackendCompiler(builder, options);

        var session = backend.CreateModulePlans([module]);
        var report = new EmissionSession(session, builder, emitExecutableSkeletons: true).Emit();
        var run = Assert.Single(Assert.Single(report.Modules).Functions, function => function.Name == "run");

        Assert.True(run.HasExecutableSkeleton);
        var runDel = (ScriptFunctionDelegate)run.Method.CreateDelegate(typeof(ScriptFunctionDelegate));
        var result = runDel(null, Span<ScriptDatum>.Empty);
        Assert.Equal(7, result.Number);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(false);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
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
            .WithBaseDirectory(root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithEnableHotReload(false)
            .WithEnableModuleDirectCall(true);
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
        var parser = new AuroraParser(lexer, EngineOptions.Default.WithBaseDirectory(root));
        return parser.Parse();
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

    private delegate void ModuleInitializerDelegate(ScriptContext ctx, Span<ScriptDatum> args);

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
