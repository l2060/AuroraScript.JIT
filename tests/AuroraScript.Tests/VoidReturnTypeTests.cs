using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class VoidReturnTypeTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task OrdinaryVoidFunctionsPreserveScriptCallingConvention(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var count = 0;
            export func empty() void { }
            export func stop() void { return; }
            func bump(Number amount) void { count += amount; }
            const initial = bump(2);
            export func cleanup() void {
                try { count += 3; return; } finally { count += 4; }
            }
            export func outer() void {
                func nested() Number { return 7; }
                var lambda = () => 8;
                count += nested() + lambda();
            }
            export func result() { return [count, initial == null]; }
            """, mode);
        using (domain)
        {
            foreach (var name in new[] { "empty", "stop", "cleanup", "outer" })
                ScriptAssert.Equal(null, TestWorkspace.Execute(domain, name));
            ScriptAssert.Equal(new object[] { 24, true }, TestWorkspace.Execute(domain, "result"));
        }
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var name in new[] { "empty", "stop", "cleanup", "outer" })
            {
                Assert.Equal(typeof(ScriptDatum), methods.Single(method => method.Name == name + "$typed").ReturnType);
                Assert.DoesNotContain(methods, method => method.Name == name + "$native");
            }
        }
#endif
    }

    [Theory]
    [InlineData("func bad() void { return 1; }")]
    [InlineData("func bad() void { return null; }")]
    [InlineData("func bad() void { if (true) return 'value'; }")]
    [InlineData("func bad() void { try { return; } finally { return 1; } }")]
    public async Task OrdinaryVoidFunctionsRejectValueReturns(string source)
    {
        using var workspace = new TestWorkspace();
        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => workspace.CompileModuleAsync(source));
        Assert.Contains("cannot return a value", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("func bad(void value) { }")]
    [InlineData("type Bad { void value; }")]
    [InlineData("func bad(value) { return value as void; }")]
    public async Task VoidIsOnlyValidAsAReturnType(string source)
    {
        using var workspace = new TestWorkspace();
        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => workspace.CompileModuleAsync(source));
        Assert.Contains("void", error.Message, StringComparison.Ordinal);
    }
}
