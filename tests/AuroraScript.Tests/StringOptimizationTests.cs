using AuroraScript.Runtime;
using AuroraScript.Core;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class StringOptimizationTests
{
    private const string WordSource = """
        @module(TEST);
        export native function WordToHex(uint32 value) String {
            var result = '';
            var temp = '';
            var octet;
            var count;
            for (count = 0; count <= 3; count++) {
                octet = (value >> (count * 8)) & 255;
                temp = '0' + octet.toString(16);
                result += temp.substring(temp.length - 2, 2);
            }
            return result;
        }
        """;

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task OriginalWordToHexKeepsHistoricalSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(WordSource, mode);
        using var scope = domain;
        uint[] words = [0, 0x12345678, 0x80000000, uint.MaxValue];
        string[] expected = ["00000000", "7531", "0000008", "FFFF"];
        for (var i = 0; i < words.Length; i++)
            ScriptAssert.Equal(expected[i], TestWorkspace.Execute(domain, "WordToHex",
                arguments: [ScriptDatum.FromNumber(words[i])]));

#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            // Inspect the actual CLR signature and locals, not decompiler text.
            var assembly = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")));
            var method = assembly.GetTypes().SelectMany(type => type.GetMethods())
                .Single(method => method.Name == "WordToHex$native");
            Assert.Equal(typeof(string), method.ReturnType);
            var locals = method.GetMethodBody()!.LocalVariables;
            Assert.Contains(locals, local => local.LocalType == typeof(uint));
            Assert.Contains(locals, local => local.LocalType == typeof(int));
            Assert.DoesNotContain(locals, local => local.LocalType == typeof(ScriptDatum));
            var calls = GetCalls(method);
            Assert.Contains(calls, call => call.Name == "FormatString");
            Assert.Contains(calls, call => call.Name == "Substring");
            Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(CallOps) ||
                call.DeclaringType == typeof(ScriptDatum) ||
                call.Name is "TryToNumber" or "ChangeByOne");
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task PrimitiveIntrinsicsMatchDynamicMethods(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func format(Number value, Number radix) String { return value.toString(radix); }
            export func dynamicFormat(value, radix) { return value.toString(radix); }
            export native func formatDefault(Number value) String { return value.toString(); }
            export func dynamicFormatDefault(value) { return value.toString(); }
            export native func slice(String value, Number start, Number end) String { return value.substring(start, end); }
            export func dynamicSlice(value, start, end) { return value.substring(start, end); }
            export native func sliceStart(String value, Number start) String { return value.substring(start); }
            export func dynamicSliceStart(value, start) { return value.substring(start); }
            export func fallback(String value, start, end) String { return value.substring(start, end); }
            export func shapes(Number value, String text) {
                return [value.toString(16, 2), value.toString(...[16]),
                    value.toString(16L), text.substring(), text.substring(...[1, 3])];
            }
            """, mode);
        using var scope = domain;
        double[] numbers = [0, -1, 255, 4294967295, 1.25, double.NaN, double.PositiveInfinity];
        double[] radices = [16, 16.9, 10, 2, double.NaN];
        foreach (var value in numbers)
        {
            AssertSame(domain, "formatDefault", "dynamicFormatDefault", ScriptDatum.FromNumber(value));
            foreach (var radix in radices)
                AssertSame(domain, "format", "dynamicFormat", ScriptDatum.FromNumber(value), ScriptDatum.FromNumber(radix));
        }
        double[] indices = [-2, 0, 1.9, 3, 8, double.NaN, double.PositiveInfinity, 1e30];
        var text = ScriptDatum.FromString("abcdef");
        foreach (var start in indices)
        {
            AssertSame(domain, "sliceStart", "dynamicSliceStart", text, ScriptDatum.FromNumber(start));
            foreach (var end in indices)
                AssertSame(domain, "slice", "dynamicSlice", text, ScriptDatum.FromNumber(start), ScriptDatum.FromNumber(end));
        }
        ScriptDatum[] fallback = [ScriptDatum.Null, ScriptDatum.FromBoolean(true), ScriptDatum.FromString("2"),
            ScriptDatum.FromInt64(2), ScriptDatum.FromUInt64(ulong.MaxValue)];
        foreach (var start in fallback)
            AssertSame(domain, "fallback", "dynamicSlice", text, start, ScriptDatum.FromNumber(4));
        ScriptAssert.Equal(new object[] { "255", "FF", "255", "abcdef", "bc" },
            TestWorkspace.Execute(domain, "shapes", arguments: [ScriptDatum.FromNumber(255), text]));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task InitialNullEliminationRequiresDefiniteWrites(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            func beforeWrite() { var x; var old = x; x = 1; return old; }
            func branch(flag) { var x; if (flag) x = 1; return x; }
            func both(flag) { var x; if (flag) x = 'a'; else x = 'b'; return x; }
            func zeroLoop() { var x; for (var i = 0; i < 0; i++) x = 1; return x; }
            func shortCircuit(flag) { var x; flag && (x = 1); return x; }
            func capture() { var x; var read = () => x; var old = read(); x = 1; return [old, read()]; }
            func exceptional() { var x; try { throw 'error'; x = 1; } catch(e) { return x; } }
            func explicitNull() { var x = null; var old = x; x = 'text'; return old; }
            func overflow() { var x; x = 2147483647; x++; return x; }
            func inclusiveLimit() {
                var i;
                for (i = 2147483647; i <= 2147483647; i++) { if (i < 0) return 'wrapped'; }
                return i;
            }
            func continueRead() {
                var x;
                for (var i = 0; i < 1; x++) { i++; if (i == 1) continue; x = 1; }
                return x;
            }
            export func run() {
                return [beforeWrite(), branch(false), branch(true), both(false), both(true),
                    zeroLoop(), shortCircuit(false), capture(), exceptional(), explicitNull(),
                    overflow(), inclusiveLimit(), continueRead()];
            }
            """, mode);
        using var scope = domain;
        var actual = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "run").Object);
        object?[] expected = [null, null, 1, "b", "a", null, null,
            new object?[] { null, 1 }, null, null, 2147483648d, 2147483648d, double.NaN];
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            if (expected[i] == null)
                Assert.True(actual.GetElement(i).Kind == ValueKind.Null, $"Initial null observable at index {i}: {actual.GetElement(i)}");
            ScriptAssert.Equal(expected[i], actual.GetElement(i));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task StringAbiPreservesChecksAndProtectedReturns(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            native func guarded(String value = 'default') String {
                try { return value + '!'; } finally { var ignored = 1; }
                return 'unreachable';
            }
            native func finalReturn() String {
                try { return 'try'; } finally { return 'finally'; }
                return 'unreachable';
            }
            export func checkedReturn(value) String { return value; }
            export native func invalidNative(Number value) String { return value; }
            export func run() {
                var text = 'start';
                var assigned = (text += null);
                text += true;
                text += 9223372036854775807L;
                text += 18446744073709551615UL;
                return [guarded(), guarded('ok'), finalReturn(), assigned, text];
            }
            """, mode, enableHotReload: true);
        using var scope = domain;
        ScriptAssert.Equal(new object[] { "default!", "ok!", "finally", "startnull",
            "startnullTrue922337203685477580718446744073709551615" }, TestWorkspace.Execute(domain, "run"));
        ScriptAssert.Equal("ok", TestWorkspace.Execute(domain, "checkedReturn", arguments: [ScriptDatum.FromString("ok")]));
        Assert.ThrowsAny<Exception>(() => TestWorkspace.Execute(domain, "checkedReturn", arguments: [ScriptDatum.FromNumber(1)]));
        Assert.ThrowsAny<Exception>(() => TestWorkspace.Execute(domain, "invalidNative", arguments: [ScriptDatum.FromNumber(1)]));
    }

    private static void AssertSame(ScriptDomain domain, string optimized, string dynamic, params ScriptDatum[] args)
    {
        ScriptDatum actual = default, expected = default;
        var expectedError = Record.Exception(() => expected = TestWorkspace.Execute(domain, dynamic, arguments: args));
        var actualError = Record.Exception(() => actual = TestWorkspace.Execute(domain, optimized, arguments: args));
        Assert.Equal(expectedError?.GetType(), actualError?.GetType());
        if (expectedError != null) return;
        Assert.Equal(expected.Kind, actual.Kind);
        Assert.Equal(expected.StringText, actual.StringText);
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ImportedStringAbiAndReloadedWrappers(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("text.as", "export native func greet(String name = 'world') String { return 'hello ' + name; }");
        workspace.WriteSource("main.as", """
            @module(TEST);
            import text from './text';
            export func run() String { return text.greet(); }
            """);
        var engine = workspace.CreateEngine(mode, enableHotReload: true,
            assemblyOut: mode == CompilationMode.Persistence ? Path.Combine(workspace.Root, "strings.dll") : null);
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal("hello world", TestWorkspace.Execute(domain, "run"));
        domain.DynamicPatch(workspace.MemorySource("main.as", """
            @module(TEST);
            import text from './text';
            export func run() String { return text.greet('again') + '!'; }
            """), HotPatchType.Incremental | HotPatchType.IgnoreDepends);
        ScriptAssert.Equal("hello again!", TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task MixedConcatPreservesObservableConversionOrder(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var value = new ObservableString();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export func run() { return (VALUE + ':') + TOUCH(); }
            export func strings(String a, String b, String c) { return a + b + c; }
            """, mode, configureGlobal: global =>
            {
                global.Define("VALUE", ScriptDatum.FromObject(value), false, false);
                global.Define("TOUCH", ScriptDatum.FromBonding(
                    (ScriptContext ctx, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) =>
                    {
                        value.Text = "after";
                        result = ScriptDatum.FromString("done");
                    }), false, false);
            });
        using var scope = domain;
        ScriptAssert.Equal("before:done", TestWorkspace.Execute(domain, "run"));
        ScriptAssert.Equal("abc", TestWorkspace.Execute(domain, "strings",
            arguments: [ScriptDatum.FromString("a"), ScriptDatum.FromString("b"), ScriptDatum.FromString("c")]));
    }

    private sealed class ObservableString : ScriptObject
    {
        public string Text = "before";
        public override string ToString() => Text;
    }

    private static List<MethodBase> GetCalls(MethodInfo method)
    {
        var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null)!)
            .ToDictionary(opcode => unchecked((ushort)opcode.Value));
        var result = new List<MethodBase>();
        var il = method.GetMethodBody()!.GetILAsByteArray()!;
        for (var offset = 0; offset < il.Length;)
        {
            ushort code = il[offset++];
            if (code == 0xfe) code = (ushort)(0xfe00 | il[offset++]);
            var opcode = opcodes[code];
            if (opcode.OperandType == OperandType.InlineMethod)
                result.Add(method.Module.ResolveMethod(BitConverter.ToInt32(il, offset))!);
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
        return result;
    }
}
