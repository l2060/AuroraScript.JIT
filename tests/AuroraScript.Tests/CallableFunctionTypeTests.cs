using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Compiler;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Generic;
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
                [
                    typeof(ScriptContext),
                    typeof(double),
                    typeof(ScriptArray)
                ],
                nativeLambda.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray());
            Assert.Contains(
                methods,
                method => method.Name == "hasMore$native");

            // The callable guard lives in one thunk per contract, so the calling
            // body only performs the call itself.
            var apply = Assert.Single(
                methods,
                method => method.Name.StartsWith(
                    "apply$typed",
                    StringComparison.Ordinal));
            var thunk = Assert.Single(
                methods,
                method => method.Name.StartsWith(
                    "Predicate$callable",
                    StringComparison.Ordinal));
            Assert.Contains(
                StringOptimizationTests.GetCalls(apply),
                called => called.Name == thunk.Name);
            Assert.DoesNotContain(
                StringOptimizationTests.GetCalls(apply),
                called =>
                    called.DeclaringType == typeof(CallFrameOps) &&
                    called.Name ==
                        nameof(CallFrameOps.GetNativeTarget));
            Assert.Contains(
                StringOptimizationTests.GetCalls(thunk),
                called =>
                    called.DeclaringType == typeof(CallFrameOps) &&
                    called.Name ==
                        nameof(CallFrameOps.GetNativeTarget));
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CallableCallGoesThroughOneSharedThunk(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            type Supplier() Number;

            native func useSupplier(Supplier supply) Number {
                return supply() + 1;
            }

            export native func value() Number {
                return 40.5;
            }

            export native func run() Number {
                return useSupplier(value);
            }
            """,
            mode);
        using (domain)
        {
            ScriptAssert.Equal(41.5, TestWorkspace.Execute(domain, "run"));
        }
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var assembly = Assembly.Load(
                File.ReadAllBytes(
                    Path.Combine(workspace.Root, "test-output.dll")));
            var methods = assembly.GetTypes()
                .SelectMany(type => type.GetMethods())
                .ToArray();

            // The call keeps the ordinary native entry, and the guard for the
            // supplied function value lives in the contract thunk alone.
            var entry = Assert.Single(
                methods,
                method => method.Name == "useSupplier$native");
            var thunk = Assert.Single(
                methods,
                method => method.Name.StartsWith(
                    "Supplier$callable",
                    StringComparison.Ordinal));
            var entryCalls = StringOptimizationTests.GetCalls(entry);
            Assert.Contains(entryCalls, call => call.Name == thunk.Name);
            Assert.DoesNotContain(
                entryCalls,
                call => call.Name == nameof(CallFrameOps.GetNativeTarget));

            var thunkCalls = StringOptimizationTests.GetCalls(thunk);
            Assert.Contains(
                thunkCalls,
                call => call.Name == nameof(CallFrameOps.GetNativeTarget));
            Assert.Contains(
                thunkCalls,
                call => call.Name == nameof(CallFrameOps.EnterClosure));

            var run = Assert.Single(
                methods,
                method => method.Name == "run$native");
            Assert.Contains(
                StringOptimizationTests.GetCalls(run),
                call => call.Name == entry.Name);
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CapturingLambdaTakesTheCallableFallbackPath(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            type Adder(Number value, String label) String;

            native func apply(Adder add) String {
                return add(2, 'x');
            }

            export func run() {
                var bonus = 5;
                return apply((value, label) => label + (value + bonus));
            }
            """,
            mode);
        using (domain)
        {
            ScriptAssert.Equal("x7", TestWorkspace.Execute(domain, "run"));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task UnreachableNativeArgumentDropsTheCallableGuard(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            type Action(Object arg) void;

            native func actionCallback(Action callback) void {
                callback(12345);
            }

            export native func run() void {
                actionCallback(handler);
            }

            export native func handler() void {
            }
            """,
            mode);
        using (domain)
        {
            TestWorkspace.Execute(domain, "run");
        }
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var assembly = Assembly.Load(
                File.ReadAllBytes(
                    Path.Combine(workspace.Root, "test-output.dll")));
            var methods = assembly.GetTypes()
                .SelectMany(type => type.GetMethods())
                .ToArray();

            // A Number cannot reach an Object parameter, so the contract has no
            // native path to guard and the call stays a plain dynamic invoke.
            var entry = Assert.Single(
                methods,
                method => method.Name == "actionCallback$native");
            var calls = StringOptimizationTests.GetCalls(entry);
            Assert.DoesNotContain(
                calls,
                call => call.Name == nameof(CallFrameOps.GetNativeTarget));
            Assert.DoesNotContain(
                calls,
                call => call.Name == nameof(CallFrameOps.EnterClosure));
            Assert.Contains(calls, call => call.Name == nameof(CallOps.Invoke1));
            Assert.DoesNotContain(
                methods,
                method => method.Name.StartsWith(
                    "Action$callable",
                    StringComparison.Ordinal));
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

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CallableNativeEntryPreservesContextAndFinally(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            type Factory() Number;
            func apply(Factory callback) Number {
                return callback();
            }
            export func run() Number {
                return apply(() => {
                    try {
                        return 7;
                    }
                    finally {
                        console.log('finally');
                    }
                });
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

    public static IEnumerable<object[]> CallableRegressionCases()
    {
        var cases = new List<(string Source, object Expected)>
        {
            ("""
             type Signed(int64 x) String;
             type Unsigned(uint64 x) String;
             type Narrow(int32 x) Number;
             func kind(x) { return typeof x; }
             func identity(x) { return x; }
             func signed(Signed f) String { return f(1); }
             func unsigned(Unsigned f) String { return f(1L); }
             func narrow(Narrow f) Number { return f(1.5); }
             export func run() { return [signed(kind), unsigned(kind), narrow(identity)]; }
             """, new object[] { "number", "int64", 1.5 }),
            ("""
             type NumberAction(Number x) void;
             type ObjectAction(Object x) void;
             func raw(x) { return 42; }
             func hit(NumberAction f) { return f(1); }
             func miss(ObjectAction f) { return f(1); }
             func spread(NumberAction f) { return f(...[1]); }
             func missing(NumberAction f) { return f(); }
             func extra(NumberAction f) { return f(1,2); }
             func statement(NumberAction f) { f(...[1]); return 7; }
             export func run() {
                 return [hit(raw), miss(raw), spread(raw), missing(raw), extra(raw),
                         hit(x=>{}), statement(raw)];
             }
             """, new object?[] { null, null, null, null, null, null, 7 }),
            ("""
             type Read(Number x) Number;
             func defaults(Number x = 3) Number { return x; }
             func invoke(Read f) { return [f(), f(4,5), f(...[6])]; }
             export func run() { return invoke(defaults); }
             """, new object[] { 3, 4, 6 }),
            ("""
             type Read(Number x);
             func use(Read f) { return f('text'); }
             export func run() { return use(x=>x+1); }
             """, "text1"),
            ("""
             type Action(Number x) void;
             func use(Action f) { return f(); }
             export func run() { return use(...[()=>42]); }
             """, null!),
            ("""
             type Echo(Int32Array x) Int32Array;
             func use(Echo f, Int32Array x) Int32Array { return f(x); }
             export func run() {
                 var a = new Int32Array(2);
                 a[0] = 7;
                 var result = use(x=>x, a);
                 return [result == a, result[0]];
             }
             """, new object[] { true, 7 }),
            ("""
             type Supplier() Number;
             native func sum(Supplier f) Number { return f()+f()+f()+f(); }
             export func run() { return sum(()=>2); }
             """, 8),
            ("""
             type Pair(Number a, Number b) Number;
             var order = '';
             func first() Number { order += 'a'; return 2; }
             func second() Number { order += 'b'; return 3; }
             func raw(a,b) { order += 'f'; return a+b; }
             func apply(Pair f) Number { return f(first(), second()); }
             export func run() {
                 var slow = apply(raw);
                 var fast = apply((a,b)=>a+b);
                 return [slow, fast, order];
             }
             """, new object[] { 5, 5, "abfab" })
        };
        foreach (var count in new[] { 0, 1, 7, 8, 16 })
        {
            var names = Enumerable.Range(0, count).Select(i => "a" + i).ToArray();
            var parameters = string.Join(",", names.Select(n => "Number " + n));
            var arguments = string.Join(",", Enumerable.Range(1, count));
            var sum = count == 0 ? "0" : string.Join("+", names);
            cases.Add(($$"""
                type Sum({{parameters}}) Number;
                func apply(Sum f) Number { return f({{arguments}}); }
                export func run() { return apply(({{string.Join(",", names)}})=>{{sum}}); }
                """, count * (count + 1) / 2));
        }
        foreach (var (source, expected) in cases)
        {
            yield return new object[] { CompilationMode.Dynamic, source, expected };
            yield return new object[] { CompilationMode.OnlyRun, source, expected };
#if NET9_0_OR_GREATER
            yield return new object[] { CompilationMode.Persistence, source, expected };
#endif
        }
    }

    [Theory]
    [MemberData(nameof(CallableRegressionCases))]
    public async Task CallableBoundariesPreserveScriptSemantics(
        CompilationMode mode, string source, object expected)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("@module(TEST); " + source, mode);
        using (domain)
            ScriptAssert.Equal(expected, TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CallableFrameRestoresCallerAfterException(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("lib.as", """
            @module(LIB);
            type Read(Number x) Number;
            var secret = 41;
            export func invoke(Read f) Number { return f(secret); }
            """);
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            import lib from './lib';
            type Read(Number x) Number;
            var secret = 7;
            var seen = 0;
            func invoke(Read f) Number { return f(1); }
            func fail(Number x) Number {
                try { throw 'boom'; }
                finally { seen += secret; }
            }
            export func failCall() Number { return invoke(fail); }
            export func run() {
                try { lib.invoke(fail); } catch (error) {}
                return [secret, seen];
            }
            """, mode);
        using (domain)
        {
            var error = Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "failCall"));
            Assert.NotNull(error.StackTrace);
            Assert.Contains(error.StackTrace, frame => frame.Method.Contains("fail", StringComparison.Ordinal));
            ScriptAssert.Equal(new object[] { 7, 14 }, TestWorkspace.Execute(domain, "run"));
        }
    }

#if NET9_0_OR_GREATER
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task RepeatedCallableSitesShareOneBoundedThunk(int count)
    {
        using var workspace = new TestWorkspace();
        var calls = string.Join(" ", Enumerable.Range(0, count).Select(i => $"var v{i}=f();"));
        var (_, domain) = await workspace.CompileModuleAsync($$"""
            @module(TEST);
            type Supplier() Number;
            native func use(Supplier f) Number { {{calls}} return v{{count-1}}; }
            export func run() { return use(()=>1); }
            """, CompilationMode.Persistence);
        using (domain)
            ScriptAssert.Equal(1, TestWorkspace.Execute(domain, "run"));
        var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
            .GetTypes().SelectMany(t => t.GetMethods()).ToArray();
        Assert.Single(methods, m => m.Name.Contains("$callable", StringComparison.Ordinal));
        var body = Assert.Single(methods, m => m.Name == "use$native").GetMethodBody()!;
        Assert.Empty(body.ExceptionHandlingClauses);
        Assert.True(body.GetILAsByteArray()!.Length < 100 + 40 * count,
            "Shared callable guards must not be copied into every call site.");
    }
#endif

}
