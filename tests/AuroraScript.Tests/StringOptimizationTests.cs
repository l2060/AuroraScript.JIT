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
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class StringOptimizationTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CommonStringMembersUseNativeCoresAndPreserveDynamicSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func lower(String text) String { return text.toLowerCase(); }
            export func dynamicLower(text) { return text.toLowerCase(); }
            export native func upper(String text) String { return text.toUpperCase(); }
            export func dynamicUpper(text) { return text.toUpperCase(); }
            export native func trim(String text) String { return text.trim(); }
            export func dynamicTrim(text) { return text.trim(); }
            export native func left(String text) String { return text.trimLeft(); }
            export func dynamicLeft(text) { return text.trimLeft(); }
            export native func right(String text) String { return text.trimRight(); }
            export func dynamicRight(text) { return text.trimRight(); }
            export native func identity(String text) String { return text.toString(); }
            export func dynamicIdentity(text) { return text.toString(); }
            export native func contains(String text, String search) Boolean { return text.contains(search); }
            export func dynamicContains(text, search) { return text.contains(search); }
            export native func starts(String text, String search) Boolean { return text.startsWith(search); }
            export func dynamicStarts(text, search) { return text.startsWith(search); }
            export native func ends(String text, String search) Boolean { return text.endsWith(search); }
            export func dynamicEnds(text, search) { return text.endsWith(search); }
            export native func first(String text, String search) int32 { return text.indexOf(search); }
            export func dynamicFirst(text, search) { return text.indexOf(search); }
            export native func last(String text, String search) int32 { return text.lastIndexOf(search); }
            export func dynamicLast(text, search) { return text.lastIndexOf(search); }
            export native func alias(String text, int32 start, int32 end) String { return text.slice(start, end); }
            export func dynamicAlias(text, start, end) { return text.slice(start, end); }
            export func shapes(String text) {
                return [text.trim(42), text.trim(...[]), text.startsWith(), text.indexOf(),
                    text.lastIndexOf(null), text.contains(null), text.endsWith(123), text.trimLeft()];
            }
            export func readTwice(String text) String {
                var result = text.trim;
                return result() + result();
            }
            """, mode);
        using var scope = domain;
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "en-US", "tr-TR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                foreach (var input in new[] { "", " Iıiİ Aurora I\t", "中文😀\0AbAb", "\u2003 \r\n" })
                {
                    var text = ScriptDatum.FromString(input);
                    foreach (var name in new[] { "lower", "upper", "trim", "left", "right", "identity" })
                        AssertSame(domain, name, "dynamic" + char.ToUpperInvariant(name[0]) + name.Substring(1), text);
                    foreach (var search in new[] { "", "I", "aba", "Ab", "😀", "\0" })
                        foreach (var name in new[] { "contains", "starts", "ends", "first", "last" })
                            AssertSame(domain, name, "dynamic" + char.ToUpperInvariant(name[0]) + name.Substring(1),
                                text, ScriptDatum.FromString(search));
                    AssertSame(domain, "alias", "dynamicAlias", text, ScriptDatum.FromNumber(1), ScriptDatum.FromNumber(3));
                }
            }
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
        ScriptAssert.Equal(new object?[] { "abc", "abc", false, -1, -1, false, false, "abc " },
            TestWorkspace.Execute(domain, "shapes", arguments: [ScriptDatum.FromString(" abc ")]));
        ScriptAssert.Equal("abcabc", TestWorkspace.Execute(domain, "readTwice", arguments: [ScriptDatum.FromString(" abc ")]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var assembly = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")));
            var methods = assembly.GetTypes().SelectMany(type => type.GetMethods()).Where(method => method.Name.EndsWith("$native", StringComparison.Ordinal));
            foreach (var method in methods)
            {
                var calls = GetCalls(method);
                Assert.Contains(calls, call => call.DeclaringType == typeof(StringValue));
                Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(CallOps) || call.DeclaringType == typeof(ScriptDatum));
            }
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task StringLengthRangesDoNotNarrowOverflowOrMutableLoopLocals(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func overflow(String text) Number { return text.length + 2147483647; }
            export native func stable(String text) int32 {
                var result = 0;
                for (var i = 0; i < 3; i++) { result = text.length - 2; text = text + 'x'; }
                return result;
            }
            export native func mutable(String text) Number {
                var offset = text.length;
                var result = 0;
                for (var i = 0; i < 3; i++) { result = text.length + offset; offset = offset + 1073741824; }
                return result;
            }
            """, mode);
        using var scope = domain;
        ScriptAssert.Equal(2147483650D, TestWorkspace.Execute(domain, "overflow", arguments: [ScriptDatum.FromString("abc")]));
        ScriptAssert.Equal(0, TestWorkspace.Execute(domain, "stable", arguments: [ScriptDatum.FromString("")]));
        ScriptAssert.Equal(2147483654D, TestWorkspace.Execute(domain, "mutable", arguments: [ScriptDatum.FromString("abc")]));
    }

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
    public async Task InitializedWordToHexRetainsIntegerMaskedLocal(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(WordSource.Replace("var octet;", "var octet = 0;", StringComparison.Ordinal), mode);
        using var scope = domain;
        ScriptAssert.Equal("7531", TestWorkspace.Execute(domain, "WordToHex", arguments: [ScriptDatum.FromNumber(0x12345678u)]));
        ScriptAssert.Equal("FFFF", TestWorkspace.Execute(domain, "WordToHex", arguments: [ScriptDatum.FromNumber(uint.MaxValue)]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var assembly = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")));
            var method = assembly.GetTypes().SelectMany(type => type.GetMethods()).Single(method => method.Name == "WordToHex$native");
            Assert.DoesNotContain(method.GetMethodBody()!.LocalVariables, local => local.LocalType == typeof(double) || local.LocalType == typeof(ScriptDatum));
            Assert.Contains(GetCalls(method), call => call.Name == nameof(StringValue.Substring));
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task MaskNarrowingPreservesUnsignedAndWideIntegerSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export func mask(uint32 value) {
                return [value & 255, 255 & value, value & 2147483647, value & -1,
                    value & 0x80000000u, value ^ 255, value | 255, value & 0];
            }
            export func wide() { return [0xFFFFFFFFFFFFFFFFUL & 255UL, -1L & 255L]; }
            """, mode);
        using var scope = domain;
        ScriptAssert.Equal(new object[] { 255D, 255D, 2147483647D, 4294967295D, 2147483648D, 4294967040D, 4294967295D, 0D },
            TestWorkspace.Execute(domain, "mask", arguments: [ScriptDatum.FromNumber(uint.MaxValue)]));
        var wide = TestWorkspace.Execute(domain, "wide");
        Assert.True(ScriptDatum.TryGetArray(wide, out var array));
        Assert.Equal(ValueKind.UInt64, array.GetElement(0).Kind);
        Assert.Equal(ValueKind.Int64, array.GetElement(1).Kind);
    }

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
        if (expected.Kind == ValueKind.Number) Assert.Equal(expected.Number, actual.Number);
        else if (expected.Kind == ValueKind.Boolean) Assert.Equal(expected.Boolean, actual.Boolean);
        else Assert.Equal(expected.StringText, actual.StringText);
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

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task Int32SubstringUsesNativeIndicesWithoutChangingBoundaries(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func sliceInt(String text, int32 start, int32 end) String { return text.substring(start, end); }
            export native func startInt(String text, int32 start) String { return text.substring(start); }
            export native func mixed(String text, int32 start, Number end) String { return text.substring(start, end); }
            export native func unsigned(String text, uint32 start, uint32 end) String { return text.substring(start, end); }
            export func dynamicSlice(text, start, end) { return text.substring(start, end); }
            export func dynamicStart(text, start) { return text.substring(start); }
            var order = '';
            native func receiver() String { order += 'r'; return 'abcdef'; }
            native func start() int32 { order += 's'; return 1; }
            native func end() int32 { order += 'e'; return 3; }
            export func evaluationOrder() { var result = receiver().substring(start(), end()); return [result, order]; }
            """, mode);
        using var scope = domain;
        int[] indices = [int.MinValue, int.MinValue + 1, -9, -1, 0, 1, 3, 6, 7, int.MaxValue];
        foreach (var input in new[] { "", "abcdef", "中文😀" })
        {
            var text = ScriptDatum.FromString(input);
            foreach (var start in indices)
            {
                AssertSame(domain, "startInt", "dynamicStart", text, ScriptDatum.FromNumber(start));
                foreach (var end in indices)
                    AssertSame(domain, "sliceInt", "dynamicSlice", text,
                        ScriptDatum.FromNumber(start), ScriptDatum.FromNumber(end));
            }
            AssertSame(domain, "mixed", "dynamicSlice", text, ScriptDatum.FromNumber(1), ScriptDatum.FromNumber(3.75));
            AssertSame(domain, "unsigned", "dynamicSlice", text, ScriptDatum.FromNumber(uint.MaxValue), ScriptDatum.FromNumber(0u));
        }
        ScriptAssert.Equal(new object[] { "bc", "rse" }, TestWorkspace.Execute(domain, "evaluationOrder"));

#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var assembly = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")));
            var methods = assembly.GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var name in new[] { "sliceInt", "startInt", "mixed", "unsigned" })
            {
                var method = methods.Single(method => method.Name == name + "$native");
                var calls = GetCalls(method);
                if (name is "sliceInt" or "startInt")
                {
                    var substring = Assert.Single(calls, call => call.Name == nameof(StringValue.Substring));
                    Assert.All(substring.GetParameters().Skip(1), parameter => Assert.Equal(typeof(int), parameter.ParameterType));
                    Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(CallOps) || call.DeclaringType == typeof(ScriptDatum));
                }
                else
                {
                    Assert.DoesNotContain(calls, call => call.Name == nameof(StringValue.Substring));
                    Assert.Contains(calls, call => call.DeclaringType == typeof(CallOps));
                }
            }
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task StringMemberBindingsPreserveLengthAndCharCodeIndexProofs(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func length(String text) int32 { return text.length; }
            export native func code(String text, int32 index) Number { return text.charCodeAt(index); }
            export func dynamicCode(text, index) { return text.charCodeAt(index); }
            export func fallback(String text, index) { return text.charCodeAt(index); }
            export native func sum(String text) Number {
                var result = 0;
                for (var i = 0; i < text.length; i++) result += text.charCodeAt(i);
                return result;
            }
            export func shapes(String text) {
                return [text.charCodeAt(), text.charCodeAt(...[1]), text.charCodeAt(1, 2)];
            }
            """, mode);
        using var scope = domain;
        var text = ScriptDatum.FromString("abc");
        ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "length", arguments: [text]));
        ScriptAssert.Equal(294, TestWorkspace.Execute(domain, "sum", arguments: [text]));
        ScriptAssert.Equal(0, TestWorkspace.Execute(domain, "sum", arguments: [ScriptDatum.FromString("")]));
        foreach (var index in new[] { int.MinValue, -1, 0, 2, 3, int.MaxValue })
            AssertSame(domain, "code", "dynamicCode", text, ScriptDatum.FromNumber(index));
        foreach (var index in new[] { ScriptDatum.Null, ScriptDatum.FromNumber(1.9), ScriptDatum.FromInt64(1),
            ScriptDatum.FromUInt64(ulong.MaxValue), ScriptDatum.FromNumber(double.NaN), ScriptDatum.FromString("1") })
            AssertSame(domain, "fallback", "dynamicCode", text, index);
        ScriptAssert.Equal(new object[] { -1, 98, 98 }, TestWorkspace.Execute(domain, "shapes", arguments: [text]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var assembly = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")));
            var methods = assembly.GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            Assert.Contains(GetCalls(methods.Single(method => method.Name == "length$native")),
                call => call.Name == nameof(StringValue.LengthCore));
            Assert.Contains(GetCalls(methods.Single(method => method.Name == "code$native")),
                call => call.Name == nameof(StringValue.CharCodeAtCore));
            Assert.Contains(GetCalls(methods.Single(method => method.Name == "sum$native")),
                call => call.Name == nameof(StringValue.CharCodeAtInt32Core));
        }
#endif
    }

    private sealed class ObservableString : ScriptObject
    {
        public string Text = "before";
        public override string ToString() => Text;
    }

    internal static List<MethodBase> GetCalls(MethodInfo method)
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
