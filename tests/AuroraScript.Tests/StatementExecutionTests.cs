using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class StatementExecutionTests
{
    [Fact]
    public async Task ExecutesControlFlowBranchesLoopsBreakAndContinue()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run(limit) {
                var total = 0;
                for (var i = 0; i < limit; i++) {
                    if (i == 2) continue;
                    if (i == 6) break;
                    total += i;
                }
                var count = 0;
                while (count < 3) {
                    total += 10;
                    count++;
                }
                if (total > 40) return total;
                else return -1;
            }
            """);

        ScriptAssert.Equal(43, TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromNumber(10)));
    }

    [Fact]
    public async Task ExecutesForInAcrossArrayObjectAndString()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var arrayCount = 0;
                for (var item in [10, 20, 30]) arrayCount++;
                var objectCount = 0;
                for (var key in { a: 1, b: 2 }) objectCount++;
                var textCount = 0;
                for (var ch in 'abc') textCount++;
                return [arrayCount, objectCount, textCount];
            }
            """);

        ScriptAssert.Equal(new object?[] { 3, 2, 3 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ExecutesClosuresRecursionAndIndependentUpvalues()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func makeCounter(start) {
                var value = start;
                return () => { value++; return value; };
            }
            func factorial(value) {
                if (value <= 1) return 1;
                return value * factorial(value - 1);
            }
            export func run() {
                var first = makeCounter(0);
                var second = makeCounter(10);
                return [first(), first(), second(), factorial(6)];
            }
            """);

        ScriptAssert.Equal(new object?[] { 1, 2, 11, 720 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ExecutesDefaultParametersSpreadAndHighArityCalls()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func sum(a, b, c, d, e, f, g, h) {
                return a + b + c + d + e + f + g + h;
            }
            func defaults(a, b = 5) { return [a, b]; }
            export func run() {
                var values = [1, 2, 3, 4, 5, 6, 7, 8];
                var first = defaults(2);
                return [sum(...values), first[0], first[1]];
            }
            """);

        ScriptAssert.Equal(new object?[] { 36, 2, 5 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ExecutesDestructuringTemplatesAndCollectionMethods()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var [first, ...middle, last] = [1, 2, 3, 4];
                var { name, age } = { name: 'Aurora', age: 6 };
                middle.push(last);
                return [`${name}:${age}`, first, middle.join(',')];
            }
            """);

        ScriptAssert.Equal(new object?[] { "Aurora:6", 1, "2,3,4" }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ExecutesThrowCatchAndFinally()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var result = '';
                try {
                    throw 'failure';
                } catch (error) {
                    result = 'caught:' + error.message;
                } finally {
                    result += ':finally';
                }
                return result;
            }
            """);

        ScriptAssert.Equal("caught:failure:finally", TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task BlockScopeAllowsShadowingOuterVar()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var a = 123;
                var inner = null;
                {
                    var a = 123456;
                    inner = a;
                }
                return [a, inner];
            }
            """);

        ScriptAssert.Equal(new object?[] { 123, 123456 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ModuleVariablesAreIsolatedAcrossDomains()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var path = workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            var count = 0;
            export func next() { count++; return count; }
            """);
        await engine.BuildAsync(path);
        var firstDomain = engine.CreateDomain();
        var secondDomain = engine.CreateDomain();

        ScriptAssert.Equal(1, TestWorkspace.Execute(firstDomain, "next"));
        ScriptAssert.Equal(2, TestWorkspace.Execute(firstDomain, "next"));
        ScriptAssert.Equal(1, TestWorkspace.Execute(secondDomain, "next"));
    }
}
