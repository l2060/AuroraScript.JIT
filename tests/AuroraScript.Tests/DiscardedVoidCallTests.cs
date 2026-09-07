using AuroraScript.Runtime;
using AuroraScript.Runtime.Builtin;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class DiscardedVoidCallTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task DiscardedVoidCallsDoNotMaterializeNullButValuesStillDo(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            native func sink() void { }
            export native func discarded(StringBuffer b) void {
                console.log(1);
                b.append('a');
                (console.log(2), b.append('g'));
                for (console.log(3); b.toString().length < 4; b.append('l')) { }
            }
            export native func extras(StringBuffer b) void { sink(b.append('x')); }
            export native func grouped() Boolean { return (console.log(4), true); }
            export native func cleanup(StringBuffer b) void {
                try { b.append('t'); } finally { b.append('f'); }
            }
            export func values(StringBuffer b) {
                var result = b.append('v');
                return [result == null, console.log(5) == null];
            }
            export func catches(StringBuffer b) {
                try { b.insert(-1, 'x'); return false; } catch (error) { return true; }
            }
            """, mode);
        var buffer = new StringBuffer();
        var argument = ScriptDatum.FromObject(buffer);
        ScriptAssert.Equal(null, TestWorkspace.Execute(domain, "discarded", arguments: [argument]));
        Assert.Equal("agll", buffer.ToString());
        ScriptAssert.Equal(null, TestWorkspace.Execute(domain, "extras", arguments: [argument]));
        ScriptAssert.Equal(true, TestWorkspace.Execute(domain, "grouped"));
        ScriptAssert.Equal(null, TestWorkspace.Execute(domain, "cleanup", arguments: [argument]));
        Assert.Equal("agllxtf", buffer.ToString());
        ScriptAssert.Equal(new object[] { true, true }, TestWorkspace.Execute(domain, "values", arguments: [argument]));
        Assert.Equal("agllxtfv", buffer.ToString());
        ScriptAssert.Equal(true, TestWorkspace.Execute(domain, "catches", arguments: [argument]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var name in new[] { "discarded", "extras", "grouped", "cleanup" })
            {
                var method = methods.Single(method => method.Name == name + "$native");
                Assert.Equal(0, CountNullLoads(method));
            }
            var calls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == "discarded$native"));
            Assert.Equal(3, calls.Count(call => call.DeclaringType == typeof(ConsoleSupport) && call.Name == nameof(ConsoleSupport.LogCore)));
            Assert.Equal(3, calls.Count(call => call.DeclaringType == typeof(StringBuffer) && call.Name == nameof(StringBuffer.AppendCore)));
            Assert.Contains(methods.Where(method => method.Name.StartsWith("values", StringComparison.Ordinal)), method => CountNullLoads(method) > 0);
        }
#endif
    }

#if NET9_0_OR_GREATER
    private static int CountNullLoads(MethodInfo method)
    {
        var il = method.GetMethodBody()!.GetILAsByteArray()!;
        var offset = 0;
        var count = 0;
        foreach (var opcode in IntegerSpecializationTests.ReadOpCodes(il.AsSpan()))
        {
            offset += opcode.Size;
            if (opcode == OpCodes.Ldsfld)
            {
                var field = method.Module.ResolveField(BitConverter.ToInt32(il, offset))!;
                if (field.DeclaringType == typeof(ScriptDatum) && field.Name == nameof(ScriptDatum.Null)) count++;
            }
            offset += opcode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
                _ => 4
            };
        }
        return count;
    }
#endif
}
