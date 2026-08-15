using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests.Runtime;

public sealed class ClosureFunctionContextTests
{
    [Fact]
    public void ScriptInvocationUsesTheActiveContextWithoutChildContextAllocation()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var domain = engine.CreateEmptyDomain(null);
        var root = new ScriptContext(domain);
        ScriptFunctionDelegate0 target = active =>
        {
            Assert.Same(root, active);
            Assert.Null(active.Next);
            return ScriptDatum.FromNumber(42);
        };
        var closure = new ClosureFunction(domain, null, target, Array.Empty<Upvalue>(), "lightweight");

        Assert.Equal(42, closure.Invoke0(root).Number);
        Assert.Null(root.Next);
        Assert.Null(root.Target);
    }

    [Fact]
    public void WarmScriptInvocationDoesNotAllocatePerCall()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var domain = engine.CreateEmptyDomain(null);
        var root = new ScriptContext(domain);
        ScriptFunctionDelegate0 target = static _ => ScriptDatum.Null;
        var closure = new ClosureFunction(domain, null, target, Array.Empty<Upvalue>(), "allocation");
        closure.Invoke0(root);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1_000; i++) closure.Invoke0(root);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void ScriptInvocationReleasesCompatibilityChildContextsOnSuccess()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var domain = engine.CreateEmptyDomain(null);
        var root = new ScriptContext(domain);
        ScriptFunctionDelegate0 target = active =>
        {
            active.With(module: null);
            Assert.NotNull(active.Next);
            return ScriptDatum.Null;
        };
        var closure = new ClosureFunction(domain, null, target, Array.Empty<Upvalue>(), "linked");

        closure.Invoke0(root);

        Assert.Null(root.Next);
    }

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
