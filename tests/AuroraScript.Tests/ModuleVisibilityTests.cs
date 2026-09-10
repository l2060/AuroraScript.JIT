using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ModuleVisibilityTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ExternalReadsAndCallsReturnNullWithoutRunningHiddenFunctions(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("private.as", """
            @module(PRIVATE);
            var count = 0;
            const secret = 42;
            func hidden() { count++; return 123; }
            native func nativeHidden() Number { count++; return 456; }
            export func calls() { return count; }
            export func inside() { return hidden() + secret; }
            export func insideElement() { return global.getModule('PRIVATE')['secret']; }
            """);
        var (engine, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            import m from './private';
            export func check() {
                var side = 0;
                var dynamic = m;
                var key = 'hidden';
                return [m.secret, dynamic['secret'], m.hidden(side++), m.hidden(...[side++]),
                    dynamic[key](side++), (dynamic.hidden)(side++), dynamic[key](...[side++]),
                    m.nativeHidden(), dynamic['nativeHidden'](), side, m.calls()];
            }
            """, mode);
        AssertHiddenResults(TestWorkspace.Execute(domain, "check"));
        using var block = engine.CompileBlock("""
            import m from './private';
            var side = 0;
            var dynamic = m;
            var key = 'hidden';
            return [m.secret, dynamic['secret'], m.hidden(side++), m.hidden(...[side++]),
                dynamic[key](side++), (dynamic.hidden)(side++), dynamic[key](...[side++]),
                m.nativeHidden(), dynamic['nativeHidden'](), side, m.calls()];
            """, new CompileBlockOptions { Domain = domain });
        AssertHiddenResults(block.Invoke(Array.Empty<ScriptDatum>()));
        ScriptAssert.Equal(null, domain.Execute("PRIVATE", "hidden"));
        Assert.Null(domain.GetMethod("PRIVATE", "hidden"));
        ScriptAssert.Equal(0, TestWorkspace.Execute(domain, "calls", "PRIVATE"));
        ScriptAssert.Equal(165, TestWorkspace.Execute(domain, "inside", "PRIVATE"));
        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "insideElement", "PRIVATE"));
    }

    [Fact]
    public void OrdinaryNullCallsStillThrowAndComputedCallsResolveBeforeArguments()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        using var bad = engine.CompileBlock("var value = null; return value();");
        Assert.Throws<AuroraRuntimeException>(() => bad.Invoke(Array.Empty<ScriptDatum>()));
        using var block = engine.CompileBlock("""
            var a = {call: () => 1};
            func replace() { a.call = () => 2; }
            return a['call'](replace());
            """);
        ScriptAssert.Equal(1, block.Invoke(Array.Empty<ScriptDatum>()));
    }

    private static void AssertHiddenResults(ScriptDatum result)
    {
        var array = Assert.IsType<ScriptArray>(result.Object);
        for (var i = 0; i < 9; i++) ScriptAssert.Equal(null, array.GetElement(i));
        ScriptAssert.Equal(5, array.GetElement(9));
        ScriptAssert.Equal(0, array.GetElement(10));
    }
}
