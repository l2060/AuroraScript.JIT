using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests.Runtime;

public sealed class ClosureFunctionContextTests
{
    [Fact]
    public async Task InvokeClrDetached_CanRunAfterOriginalContextWasReturned()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            export function increment(value) {
                return value + 1;
            }
            """,
            configureGlobal: _ => { });

        var callback = Assert.IsType<ClosureFunction>(domain.GetMethod("TEST", "increment"));

        var result = await Task.Run(
            () => callback.InvokeClrDetached(ScriptObject.Null, ScriptDatum.FromNumber(41)));

        ScriptAssert.Equal(42, result);
    }

    [Fact]
    public async Task ReusingReturnedContext_FailsWithoutCreatingAReleaseCycle()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            export function identity(value) {
                return value;
            }
            """,
            configureGlobal: _ => { });

        var callback = Assert.IsType<ClosureFunction>(domain.GetMethod("TEST", "identity"));
        var expiredContext = new ScriptContext(domain);
        expiredContext.Release();

        var exception = Assert.Throws<AuroraRuntimeException>(
            () => callback.InvokeClr(expiredContext, ScriptDatum.FromNumber(1)));

        Assert.Contains("no longer active", exception.ToString());
    }

    [Fact]
    public async Task InvokeClrDetached_SupportsConcurrentContextPoolAccess()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            export function increment(value) {
                return value + 1;
            }
            """,
            configureGlobal: _ => { });

        var callback = Assert.IsType<ClosureFunction>(domain.GetMethod("TEST", "increment"));
        var invocations = new Task<ScriptDatum>[512];
        for (var i = 0; i < invocations.Length; i++)
        {
            var value = i;
            invocations[i] = Task.Run(
                () => callback.InvokeClrDetached(ScriptObject.Null, ScriptDatum.FromNumber(value)));
        }

        var results = await Task.WhenAll(invocations);
        for (var i = 0; i < results.Length; i++)
        {
            ScriptAssert.Equal(i + 1, results[i]);
        }
    }
}
