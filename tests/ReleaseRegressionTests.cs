using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ReleaseRegressionTests
{
    [Fact]
    public async Task DirectCallFastPathsCoverZeroThroughSevenAndFallbackArity()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func f0() { return 0; }
            func f1(a) { return a; }
            func f2(a,b) { return a+b; }
            func f3(a,b,c) { return a+b+c; }
            func f4(a,b,c,d) { return a+b+c+d; }
            func f5(a,b,c,d,e) { return a+b+c+d+e; }
            func f6(a,b,c,d,e,f) { return a+b+c+d+e+f; }
            func f7(a,b,c,d,e,f,g) { return a+b+c+d+e+f+g; }
            func f8(a,b,c,d,e,f,g,h) { return a+b+c+d+e+f+g+h; }
            export func run() { return [f0(),f1(1),f2(1,2),f3(1,2,3),f4(1,2,3,4),f5(1,2,3,4,5),f6(1,2,3,4,5,6),f7(1,2,3,4,5,6,7),f8(1,2,3,4,5,6,7,8)]; }
            """,
            enableHotReload: false);

        ScriptAssert.Equal(
            new object?[] { 0, 1, 3, 6, 10, 15, 21, 28, 36 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ClosureCapturesLoopAndNestedBlockVariablesWithoutSharingSlots()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var callbacks = [];
                for (var i = 0; i < 3; i++) {
                    var captured = i;
                    callbacks.push(() => captured);
                }
                var outer = 10;
                var nested;
                {
                    var inner = 20;
                    nested = () => outer + inner;
                }
                return [callbacks[0](), callbacks[1](), callbacks[2](), nested()];
            }
            """);

        ScriptAssert.Equal(new object?[] { 0, 1, 2, 30 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task NestedClosureRemainsCallableAfterCreatingContextWasReturned()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func create(start) {
                var value = start;
                return () => { value += 2; return value; };
            }
            """);

        var created = TestWorkspace.Execute(domain, "create", arguments: ScriptDatum.FromNumber(10));
        var callback = Assert.IsType<AuroraScript.Runtime.Types.ClosureFunction>(created.Object);

        ScriptAssert.Equal(12, callback.InvokeClrDetached(AuroraScript.Runtime.Types.ScriptObject.Null));
        ScriptAssert.Equal(14, callback.InvokeClrDetached(AuroraScript.Runtime.Types.ScriptObject.Null));
    }

    [Fact]
    public async Task DeepMemberChainsAndMixedIndexAccessRemainStackBalanced()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var root = { a: { b: { c: [{ value: 40 }, { value: 42 }] } } };
                root.a.b.c[0].value += 2;
                return [root.a.b.c[0].value, root['a']['b']['c'][1]['value']];
            }
            """);

        ScriptAssert.Equal(new object?[] { 42, 42 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public void ArraySpreadCopiesOnlyLogicalElementsNotBackingCapacity()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            "var source = [2, 3]; return [1, ...source, 4];");

        ScriptAssert.Equal(
            new object?[] { 1, 2, 3, 4 },
            block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public async Task RepeatedReleaseCompilationDoesNotLeakTokenPayloadAcrossSources()
    {
        using var workspace = new TestWorkspace();
        for (var i = 0; i < 64; i++)
        {
            var engine = workspace.CreateEngine();
            var source = engine.MemorySource(
                $"module-{i}.as",
                $"@module(M{i}); export func value() {{ return 'value-{i}'; }}");
            await engine.BuildAsync(source);
            var domain = engine.CreateDomain();
            ScriptAssert.Equal($"value-{i}", domain.Execute($"M{i}", "value"));
        }
    }

    [Fact]
    public async Task LargeSourceWithCommentsUnicodeAndCrLfCompilesInRelease()
    {
        using var workspace = new TestWorkspace();
        var source = new StringBuilder("@module(TEST);\r\n");
        for (var i = 0; i < 500; i++)
        {
            source.Append("// line ").Append(i).Append("\r\n");
            source.Append("var value").Append(i).Append(" = ").Append(i).Append("; /* block */\r\n");
        }
        source.Append("export func 结果() { return value499 + 1; }");

        var (_, domain) = await workspace.CompileModuleAsync(source.ToString());

        ScriptAssert.Equal(500, TestWorkspace.Execute(domain, "结果"));
    }

    [Fact]
    public async Task ConfusedReleaseModePreservesObservableBehavior()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); const secret = 40; export func run() { return secret + 2; }",
            enableConfused: true);

        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task EmptyAndMetadataOnlyModulesInitializeSuccessfully()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        await engine.BuildAsync(
            engine.MemorySource("empty.as", "@module(EMPTY);"),
            engine.MemorySource("test.as", "@module(TEST); export func run() { return 42; }"));

        var domain = engine.CreateDomain();
        Assert.NotSame(AuroraScript.Runtime.Types.ScriptObject.Null, domain.GetModule("EMPTY"));
        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }
}
