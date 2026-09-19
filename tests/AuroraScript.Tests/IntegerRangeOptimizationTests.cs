using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class IntegerRangeOptimizationTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NumericStorageDoesNotIntroduceRoundingBranchesForUnboundedCounters(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func ascending(Number limit) Number {
                var cursor = 0;
                for (; cursor < limit; cursor++) { if (cursor == 8) break; }
                return cursor;
            }
            export native func descending(Number limit) Number {
                var cursor = 0;
                while (cursor > limit) { cursor--; if (cursor == -8) break; }
                return cursor;
            }
            export native func bounded(int32 count) int32 {
                var cursor = 0;
                for (; cursor < count; cursor++) {}
                return cursor;
            }
            export native func boundedWhile(int32 count) int32 {
                var cursor = 0;
                while (cursor < count) cursor++;
                return cursor;
            }
            export native func whileUp(Number limit) Number {
                var cursor = 2147483647;
                var steps = 0;
                while (cursor <= limit) { cursor++; if (++steps == 4) break; }
                return cursor;
            }
            export native func whileDown(int32 limit) Number {
                var cursor = -2147483648;
                var steps = 0;
                while (cursor >= limit) { cursor--; if (++steps == 4) break; }
                return cursor;
            }
            export native func whileOvershoot(int32 limit) Number {
                var cursor = 2147483646;
                var steps = 0;
                while (cursor < limit) { cursor++; cursor++; if (++steps == 4) break; }
                return cursor;
            }
            export native func wide() Number {
                var cursor = 2147483647;
                for (var step = 0; step < 4; step++) cursor++;
                return cursor;
            }
            export func precision() {
                var positive = 9007199254740991;
                var negative = -9007199254740991;
                for (var step = 0; step < 3; step++) { positive++; negative--; }
                return [positive, negative, typeof positive, typeof negative];
            }
            """, mode);
        using var scope = domain;
        foreach (var limit in new[] { double.NaN, double.NegativeInfinity, -3.5, -0.0, 0.0, 3.5, 5.0, double.PositiveInfinity })
        {
            var ascending = 0d;
            for (; ascending < limit; ascending++) { if (ascending == 8) break; }
            var descending = 0d;
            while (descending > limit) { descending--; if (descending == -8) break; }
            ScriptAssert.Equal(ascending, TestWorkspace.Execute(domain, "ascending", arguments: [ScriptDatum.FromNumber(limit)]));
            ScriptAssert.Equal(descending, TestWorkspace.Execute(domain, "descending", arguments: [ScriptDatum.FromNumber(limit)]));
        }
        ScriptAssert.Equal(17, TestWorkspace.Execute(domain, "bounded", arguments: [ScriptDatum.FromNumber(17)]));
        ScriptAssert.Equal(17, TestWorkspace.Execute(domain, "boundedWhile", arguments: [ScriptDatum.FromNumber(17)]));
        ScriptAssert.Equal(2147483648D, TestWorkspace.Execute(domain, "whileUp", arguments: [ScriptDatum.FromNumber(int.MaxValue)]));
        ScriptAssert.Equal(-2147483649D, TestWorkspace.Execute(domain, "whileDown", arguments: [ScriptDatum.FromNumber(int.MinValue)]));
        ScriptAssert.Equal(2147483648D, TestWorkspace.Execute(domain, "whileOvershoot", arguments: [ScriptDatum.FromNumber(int.MaxValue)]));
        ScriptAssert.Equal(2147483651D, TestWorkspace.Execute(domain, "wide"));
        ScriptAssert.Equal(new object[] { 9007199254740992D, -9007199254740992D, "number", "number" },
            TestWorkspace.Execute(domain, "precision"));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var name in new[] { "ascending", "descending", "bounded", "boundedWhile" })
            {
                var method = methods.Single(method => method.Name == name + "$native");
                Assert.DoesNotContain(method.GetMethodBody()!.LocalVariables, local => local.LocalType == typeof(long));
                if (name is "bounded" or "boundedWhile")
                    Assert.DoesNotContain(method.GetMethodBody()!.LocalVariables, local => local.LocalType == typeof(double));
                StringOptimizationTests.GetCalls(method, opcode =>
                {
                    Assert.NotEqual(OpCodes.Conv_I8, opcode);
                    if (name != "descending") Assert.NotEqual(OpCodes.Conv_R8, opcode);
                });
            }
            Assert.Contains(methods.Single(method => method.Name == "wide$native").GetMethodBody()!.LocalVariables,
                local => local.LocalType == typeof(long));
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task DiscardedMutationsDoNotMaterializeValuesAndObservedMutationsStillDo(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func signed32(int32 value) int32 { value++; --value; (value++, --value); return value; }
            export native func unsigned32(uint32 value) uint32 { value++; --value; (value++, --value); return value; }
            export native func signed64(int64 value) int64 { value++; --value; (value++, --value); return value; }
            export native func unsigned64(uint64 value) uint64 { value++; --value; (value++, --value); return value; }
            export native func number(Number value) Number { value++; --value; (value++, --value); return value; }
            export func observed() {
                var value = 3;
                return [value++, ++value, value--, --value, value];
            }
            """, mode);
        using var scope = domain;
        foreach (var name in new[] { "signed32", "unsigned32", "number" })
            ScriptAssert.Equal(3, TestWorkspace.Execute(domain, name, arguments: [ScriptDatum.FromNumber(3)]));
        var signed = TestWorkspace.Execute(domain, "signed64", arguments: [ScriptDatum.FromInt64(3)]);
        Assert.Equal(ValueKind.Int64, signed.Kind);
        Assert.Equal(3L, signed.Int64);
        var unsigned = TestWorkspace.Execute(domain, "unsigned64", arguments: [ScriptDatum.FromUInt64(3)]);
        Assert.Equal(ValueKind.UInt64, unsigned.Kind);
        Assert.Equal(3UL, unsigned.UInt64);
        ScriptAssert.Equal(new object[] { 3, 5, 5, 3, 3 }, TestWorkspace.Execute(domain, "observed"));
        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "signed32", arguments: [ScriptDatum.FromNumber(int.MaxValue)]));
        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "unsigned32", arguments: [ScriptDatum.FromNumber(uint.MaxValue)]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var name in new[] { "signed32", "unsigned32", "signed64", "unsigned64", "number" })
                StringOptimizationTests.GetCalls(methods.Single(method => method.Name == name + "$native"), opcode =>
                {
                    Assert.NotEqual(OpCodes.Dup, opcode);
                    Assert.NotEqual(OpCodes.Pop, opcode);
                });
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task PackedIndexesNarrowOnlyWhenInvalidValuesCannotWrapIntoBounds(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            native func read(Int8Array data, Int32Array marks, int32 first, int32 second, Boolean enabled) int32 {
                if (first < 0 || second < 0) return -1;
                var index = first + second;
                if (enabled && data[index] && marks[index]) return index;
                return -1;
            }
            native func write(Int32Array data, int32 first, int32 second) int32 {
                if (first < 0 || second < 0) return -1;
                var index = first + second;
                data[index] = (index = 0);
                return data[index];
            }
            native func update(Int32Array data, int32 first, int32 second) int32 {
                if (first < 0 || second < 0) return -1;
                var index = first + second;
                data[index] += 2;
                return data[index]++;
            }
            native func outside(Int32Array data, int32 offset, Boolean negative) int32 {
                if (offset < 0) return -1;
                var index = 4294967296 + offset;
                if (negative) index = -index;
                return data[index] + data[index];
            }
            export func readCase(int32 first, int32 second, Boolean enabled) {
                var data = new Int8Array(2); data.fill(1);
                var marks = new Int32Array(2); marks.fill(1);
                return read(data, marks, first, second, enabled);
            }
            export func writeCase(int32 first, int32 second) {
                var data = new Int32Array(2); data.fill(7);
                var value = write(data, first, second);
                return [value, data[0], data[1]];
            }
            export func updateCase(int32 first, int32 second) {
                var data = new Int32Array(2); data.fill(7);
                var value = update(data, first, second);
                return [value, data[0], data[1]];
            }
            export func outsideCase(Boolean negative) { return outside(new Int32Array(2), 0, negative); }
            """, mode);
        using var scope = domain;
        var zero = ScriptDatum.FromNumber(0);
        var one = ScriptDatum.FromNumber(1);
        var maximum = ScriptDatum.FromNumber(int.MaxValue);
        ScriptAssert.Equal(1, TestWorkspace.Execute(domain, "readCase", arguments: [zero, one, ScriptDatum.FromBoolean(true)]));
        ScriptAssert.Equal(-1, TestWorkspace.Execute(domain, "readCase", arguments: [maximum, maximum, ScriptDatum.FromBoolean(false)]));
        ScriptAssert.Equal(new object[] { 7, 7, 0 }, TestWorkspace.Execute(domain, "writeCase", arguments: [zero, one]));
        ScriptAssert.Equal(new object[] { 9, 7, 10 }, TestWorkspace.Execute(domain, "updateCase", arguments: [zero, one]));
        foreach (var second in new[] { zero, one, maximum })
        {
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "readCase",
                arguments: [maximum, second, ScriptDatum.FromBoolean(true)]));
            foreach (var name in new[] { "writeCase", "updateCase" })
                Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, name, arguments: [maximum, second]));
        }
        foreach (var negative in new[] { false, true })
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "outsideCase", arguments: [ScriptDatum.FromBoolean(negative)]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var method = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).Single(method => method.Name == "read$native");
            StringOptimizationTests.GetCalls(method, opcode => Assert.NotEqual(OpCodes.Conv_R8, opcode));
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CharacterBoundsRequireAnUnchangedIndexAndReceiver(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func scan(String text) Number {
                var length = text.length;
                var total = 0;
                for (var i = 0; i < length; i++) {
                    var code = text.charCodeAt(i);
                    if (code == 13 && i + 1 < length && text.charCodeAt(i + 1) == 10) {
                        i++; code = 10;
                    }
                    total += code;
                }
                return total;
            }
            export native func changed(String text) Number {
                for (var i = 0; i < text.length; i++) {
                    i = text.length; return text.charCodeAt(i);
                }
                return 0;
            }
            export native func nested(String text) Number {
                var total = 0;
                for (var i = 0; i < text.length; i++) {
                    var j = 0;
                    while (j < 2) { total += text.charCodeAt(i); i++; j++; }
                }
                return total;
            }
            export native func iterated(String text) Number {
                var total = 0;
                for (var i = 0; i < text.length; i++) {
                    for (var item in [0, 1]) { total += text.charCodeAt(i); i++; }
                }
                return total;
            }
            export native func mutable(String text) Number {
                var length = text.length;
                for (var i = 0; i < length; i++) {
                    text = ''; return text.charCodeAt(i);
                }
                return 0;
            }
            export func captured(String text) Number {
                var i = 0;
                var change = () => { i = text.length; };
                if (i < text.length) { change(); return text.charCodeAt(i); }
                return 0;
            }
            export native func shortCircuit(String text) Number {
                var i = 0;
                if (i < text.length && ++i > 0) return text.charCodeAt(i);
                return 0;
            }
            export native func caught(String text) Number {
                var i = 0;
                if (i < text.length) {
                    try { i = text.length; throw 'changed'; } catch (error) {}
                    return text.charCodeAt(i);
                }
                return 0;
            }
            """, mode);
        using var scope = domain;
        foreach (var input in new[] { "", "abc", "a\r\nb", "a\r", "\u4e2d\u6587\ud83d\ude00" })
        {
            var expected = input.Replace("\r\n", "\n").Sum(c => (int)c);
            ScriptAssert.Equal(expected, TestWorkspace.Execute(domain, "scan", arguments: [ScriptDatum.FromString(input)]));
        }
        foreach (var name in new[] { "changed", "nested", "iterated", "mutable", "captured", "shortCircuit", "caught" })
            Assert.True(double.IsNaN(TestWorkspace.Execute(domain, name, arguments: [ScriptDatum.FromString("x")]).Number), name);
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            var calls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == "scan$native"));
            Assert.Contains(calls, call => call.DeclaringType == typeof(string) && call.Name == "get_Chars");
            Assert.DoesNotContain(calls, call => call.Name == "CharCodeAtCore");
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CountedAccumulationAndQuotientsPreserveWideNumberResults(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func count(String text) Number {
                var total = 2147483646;
                for (var i = 0; i < text.length; i++) {
                    var code = text.charCodeAt(i);
                    if (code < 128) total++; else if (code < 2048) total += 2; else total += 3;
                }
                return total;
            }
            export native func quotient(int32 input) Number {
                var value = input + 4294967296;
                return (value - value % 64) / 64;
            }
            export native func negativeQuotient(int32 input) Number {
                var value = input - 4294967296;
                return (value - value % 64) / 64;
            }
            export native func changedBound() Number {
                var end = 2; var total = 2147483646;
                for (var i = 0; i < end; i++) total++;
                end = 0; return total;
            }
            export native func initializerWrite() Number {
                var total = 2147483646;
                for (var i = (total += 2) - 2147483648; i < 1; i++) total++;
                return total;
            }
            export native func nestedBody() Number {
                var total = 2147483646; var j = 0;
                for (var i = 0; i < 1; i++)
                    while (j < 3) { total++; j++; }
                return total;
            }
            export native func conditionWrite() Number {
                var total = 2147483645;
                for (var i = 0; i < (i = 0) + 2; i++) {
                    total++;
                    if (total > 2147483647) break;
                }
                return total;
            }
            export func identity() {
                var value = 2147483647; for (var i = 0; i < 2; i++) value += 2;
                return typeof value;
            }
            """, mode);
        using var scope = domain;
        ScriptAssert.Equal(2147483652D, TestWorkspace.Execute(domain, "count", arguments: [ScriptDatum.FromString("a\u00e9\u4e2d")]));
        ScriptAssert.Equal(2147483646D, TestWorkspace.Execute(domain, "count", arguments: [ScriptDatum.FromString("")]));
        ScriptAssert.Equal(2147483648D, TestWorkspace.Execute(domain, "changedBound"));
        ScriptAssert.Equal(2147483649D, TestWorkspace.Execute(domain, "initializerWrite"));
        ScriptAssert.Equal(2147483649D, TestWorkspace.Execute(domain, "nestedBody"));
        ScriptAssert.Equal(2147483648D, TestWorkspace.Execute(domain, "conditionWrite"));
        ScriptAssert.Equal("number", TestWorkspace.Execute(domain, "identity"));
        foreach (var input in new[] { int.MinValue, -65, -1, 0, 1, 65, int.MaxValue })
        {
            ScriptAssert.Equal((double)(((long)input + 4294967296) / 64),
                TestWorkspace.Execute(domain, "quotient", arguments: [ScriptDatum.FromNumber(input)]));
            ScriptAssert.Equal((double)(((long)input - 4294967296) / 64),
                TestWorkspace.Execute(domain, "negativeQuotient", arguments: [ScriptDatum.FromNumber(input)]));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ResetCounterProofRequiresEveryWriteToReachTheReset(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func reset(int32 count) Number {
                var shift = 0;
                for (var i = 0; i < count; i++) {
                    shift += 8;
                    if (shift == 32) shift = 0;
                }
                return shift;
            }
            export native func missedReset() Number {
                var value = 2147483640;
                for (var i = 0; i < 2; i++) {
                    value += 8;
                    if (value == 2147483647) value = 0;
                }
                return value;
            }
            export native func interruptedReset() Number {
                var value = 2147483640;
                for (var i = 0; i < 2; i++) {
                    value += 4;
                    continue;
                    if (value == 2147483644) value = 0;
                }
                return value;
            }
            """, mode);
        using var scope = domain;
        foreach (var count in new[] { 0, 1, 3, 4, 5, 1001 })
            ScriptAssert.Equal(count % 4 * 8, TestWorkspace.Execute(domain, "reset", arguments: [ScriptDatum.FromNumber(count)]));
        ScriptAssert.Equal(2147483656D, TestWorkspace.Execute(domain, "missedReset"));
        ScriptAssert.Equal(2147483648D, TestWorkspace.Execute(domain, "interruptedReset"));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var method = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).Single(method => method.Name == "reset$native");
            Assert.DoesNotContain(method.GetMethodBody()!.LocalVariables, local => local.LocalType == typeof(double));
        }
#endif
    }
}
