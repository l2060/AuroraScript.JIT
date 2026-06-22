using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ConcurrentRuntimeTests
{
    [Fact]
    public async Task SameDomainSupportsConcurrentPureFunctionExecution()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func calculate(value) {
                var total = 0;
                for (var i = 0; i < value; i++) total += i;
                return total;
            }
            """);

        var tasks = Enumerable.Range(1, 128)
            .Select(value => Task.Run(() => TestWorkspace.Execute(
                domain,
                "calculate",
                arguments: ScriptDatum.FromNumber(value))))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        for (var i = 0; i < results.Length; i++)
        {
            ScriptAssert.Equal(i * (i + 1) / 2, results[i]);
        }
    }

    [Fact]
    public async Task MultipleDomainsExecuteConcurrentlyWithoutStateBleed()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        await engine.BuildAsync(engine.MemorySource(
            "main.as",
            "@module(TEST); var count = 0; export func increment() { count++; return count; }"));
        var domains = Enumerable.Range(0, 32).Select(_ => engine.CreateDomain()).ToArray();

        var results = await Task.WhenAll(domains.Select(domain => Task.Run(() =>
        {
            for (var i = 0; i < 10; i++) TestWorkspace.Execute(domain, "increment");
            return TestWorkspace.Execute(domain, "increment");
        })));

        Assert.All(results, result => ScriptAssert.Equal(11, result));
    }

    [Fact]
    public async Task DetachedClosureCanBeInvokedConcurrentlyAfterOriginatingCallReturns()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func create() { return (value) => value * 2; }");
        var closureDatum = TestWorkspace.Execute(domain, "create");
        var closure = Assert.IsType<AuroraScript.Runtime.Types.ClosureFunction>(closureDatum.Object);

        var results = await Task.WhenAll(Enumerable.Range(0, 128).Select(value => Task.Run(() =>
            closure.InvokeClrDetached(
                AuroraScript.Runtime.Types.ScriptObject.Null,
                ScriptDatum.FromNumber(value)))));

        for (var i = 0; i < results.Length; i++) ScriptAssert.Equal(i * 2, results[i]);
    }
}
