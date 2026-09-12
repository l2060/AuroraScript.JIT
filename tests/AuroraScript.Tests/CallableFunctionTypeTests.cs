using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Compiler;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class CallableFunctionTypeTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task StrongFunctionTypeDrivesLambdaNativeEntry(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            type Predicate(Number value, Array items) Boolean;

            func apply(
                Predicate callback,
                Number value,
                Array items) Boolean {
                return callback(value, items);
            }

            func hasMore(Number value, Array items) Boolean {
                return items.length > value;
            }

            export func run() {
                return
                    apply(
                        (value, items) => items.length > value,
                        1,
                        [1, 2]) &&
                    apply(hasMore, 1, [1, 2]);
            }
            """,
            mode);
        using (domain)
        {
            ScriptAssert.Equal(
                true,
                TestWorkspace.Execute(domain, "run"));
        }
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var assembly = Assembly.Load(
                File.ReadAllBytes(
                    Path.Combine(
                        workspace.Root,
                        "test-output.dll")));
            var methods = assembly.GetTypes()
                .SelectMany(type => type.GetMethods())
                .ToArray();
            var nativeLambda = Assert.Single(
                methods,
                method =>
                    method.Name.StartsWith(
                        "lambda_",
                        StringComparison.Ordinal) &&
                    method.Name.EndsWith(
                        "$native",
                        StringComparison.Ordinal));
            Assert.Equal(typeof(bool), nativeLambda.ReturnType);
            Assert.Equal(
                [typeof(double), typeof(ScriptArray)],
                nativeLambda.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray());
            Assert.Contains(
                methods,
                method => method.Name == "hasMore$native");

            var apply = Assert.Single(
                methods,
                method => method.Name.StartsWith(
                    "apply$typed",
                    StringComparison.Ordinal));
            Assert.Contains(
                StringOptimizationTests.GetCalls(apply),
                called =>
                    called.DeclaringType == typeof(CallFrameOps) &&
                    called.Name ==
                        nameof(CallFrameOps.GetNativeTarget));
        }
#endif
    }

    [Fact]
    public async Task WeakFunctionTypeKeepsDynamicInvocation()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            type Callback();
            func apply(Callback callback) { return callback(2); }
            export func run() { return apply(value => value + 3); }
            """);
        using (domain)
        {
            ScriptAssert.Equal(
                5,
                TestWorkspace.Execute(domain, "run"));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task StrongVoidFunctionTypeSupportsActionCallbacks(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            type Action(Number value) void;
            func apply(Action callback) Number {
                callback(42);
                return 7;
            }
            export func run() Number {
                return apply(value => {});
            }
            """,
            mode);
        using (domain)
        {
            ScriptAssert.Equal(7, TestWorkspace.Execute(domain, "run"));
        }
    }

    [Fact]
    public async Task CapturingLambdaFallsBackToDatumClosure()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            type Transform(Number value) Number;
            func apply(Transform callback, Number value) Number {
                return callback(value);
            }
            export func run() {
                var offset = 4;
                return apply(value => value + offset, 3);
            }
            """);
        using (domain)
        {
            ScriptAssert.Equal(
                7,
                TestWorkspace.Execute(domain, "run"));
        }
    }

    [Fact]
    public async Task StrongFunctionTypeReportsConventionMismatch()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        await engine.BuildAsync(
            workspace.MemorySource(
                "main.as",
                """
                @module(TEST);
                type Predicate(Number value) Boolean;
                func apply(Predicate callback) Boolean {
                    return callback(1);
                }
                export func run() {
                    return apply((left, right) => true);
                }
                """));

        var warning = Assert.Single(
            engine.CompilationWarnings,
            item => item.Message.Contains(
                "expected 1 parameters but found 2",
                System.StringComparison.Ordinal));
        Assert.Equal(
            AuroraCompilationDiagnosticSeverity.Warning,
            warning.Severity);

        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(
            true,
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ExportedFunctionTypeFlowsThroughQualifiedImportWithoutRuntimeExport()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "contracts.as",
            """
            @module(CONTRACTS);
            export type Predicate(Number value, Array items) Boolean;
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import contracts from './contracts';
            func apply(
                contracts.Predicate callback,
                Number value,
                Array items) Boolean {
                return callback(value, items);
            }
            export func run() {
                return apply(
                    (value, items) => items.length > value,
                    1,
                    [1, 2]);
            }
            """);

        var engine = workspace.CreateEngine();
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateDomain();

        ScriptAssert.Equal(true, TestWorkspace.Execute(domain, "run"));
        Assert.DoesNotContain(
            "Predicate",
            domain.GetModule("CONTRACTS").EnumerationKeys());
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task DeclaredCallableTypeResolvesInsideModules(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "globals.as",
            """
            @global();
            declare type Action(Number value) void;
            declare type Transform(Number value) Number;
            """);
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func notify(Action callback) Number {
                callback(1);
                return 3;
            }
            func apply(Transform callback, Number value) Number {
                return callback(value);
            }
            export func run() Number {
                return notify(value => {}) + apply(value => value * 2, 4);
            }
            """,
            mode);
        using (domain)
        {
            ScriptAssert.Equal(11, TestWorkspace.Execute(domain, "run"));
        }
    }

    [Fact]
    public async Task DeclaredCallableTypeCannotBeUsedAsValue()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "globals.as",
            """
            @global();
            declare type Action(Number value) void;
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            export func run() { return Action; }
            """);

        var engine = workspace.CreateEngine();
        var error = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => engine.BuildAsync(["main.as"]));
        Assert.Contains(
            "Function type 'Action' is compile-time only",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportedFunctionTypeMustBeExported()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "contracts.as",
            """
            @module(CONTRACTS);
            type Predicate(Number value) Boolean;
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import contracts from './contracts';
            export func run(contracts.Predicate callback) Boolean {
                return callback(1);
            }
            """);

        var engine = workspace.CreateEngine();
        var error = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => engine.BuildAsync(["main.as"]));
        Assert.Contains(
            "Unknown or inaccessible type 'contracts.Predicate'",
            error.Message,
            StringComparison.Ordinal);
    }
}
