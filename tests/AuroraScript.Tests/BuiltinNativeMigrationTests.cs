using AuroraScript.Compiler.Backend;
using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class BuiltinNativeMigrationTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task DateTicksPreservesInt64TypeAndPrecisionAcrossAccessPaths(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func nativeTicks(Date date) int64 { return date.ticks; }
            export func typedTicks(Date date) { return date.ticks; }
            export func dynamicTicks(date) { return date.ticks; }
            export func dynamicKind(date) { return typeof date.ticks; }
            """, mode);
        using (domain)
        {
            // The odd large value loses precision if either path passes through double.
            foreach (var ticks in new[] { 0L, 638679147930000001L, DateTimeOffset.MaxValue.Ticks })
            {
                var date = ScriptDatum.FromDate(new ScriptDate(new DateTimeOffset(ticks, TimeSpan.Zero)));
                foreach (var method in new[] { "nativeTicks", "typedTicks", "dynamicTicks" })
                {
                    var result = TestWorkspace.Execute(domain, method, arguments: [date]);
                    Assert.Equal(ValueKind.Int64, result.Kind);
                    Assert.Equal(ticks, result.Int64);
                }
                ScriptAssert.Equal("int64", TestWorkspace.Execute(domain, "dynamicKind", arguments: [date]));
            }
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task DateDefaultFormatUsesTheInvokingEngineForNativeAndDynamicCalls(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var date = ScriptDatum.FromDate(new ScriptDate(new DateTimeOffset(2024, 2, 3, 4, 5, 6, TimeSpan.FromHours(8))));
        foreach (var (format, expected) in new[] { ("yyyy-MM-dd", "2024-02-03"), ("yyyy|MM|dd HH:mm", "2024|02|03 04:05") })
        {
            var (_, domain) = await workspace.CompileModuleAsync("""
                @module(TEST);
                export native func nativeFormat(Date date) String { return date.toString(); }
                export func typedFormat(Date date) { return date.toString(); }
                export func dynamicFormat(date) { return date.toString(); }
                export func spreadFormat(Date date) { return date.toString(...[]); }
                export func explicitFormat(Date date) { return date.toString('yyyy/MM/dd'); }
                export func dynamicExplicit(date, format) { return date.toString(format); }
                export func nullFormat(Date date) { return date.toString(null); }
                """, mode, dateTimeFormat: format);
            using (domain)
            {
                foreach (var method in new[] { "nativeFormat", "typedFormat", "dynamicFormat", "spreadFormat" })
                    ScriptAssert.Equal(expected, TestWorkspace.Execute(domain, method, arguments: [date]));
                ScriptAssert.Equal("2024/02/03", TestWorkspace.Execute(domain, "explicitFormat", arguments: [date]));
                ScriptAssert.Equal("2024/02/03", TestWorkspace.Execute(domain, "dynamicExplicit", arguments: [date, ScriptDatum.FromString("yyyy/MM/dd")]));
                ScriptAssert.Equal("null", TestWorkspace.Execute(domain, "nullFormat", arguments: [date]));
                ScriptAssert.Equal("null", TestWorkspace.Execute(domain, "dynamicExplicit", arguments: [date, ScriptDatum.Null]));
            }
        }
    }

    [Theory]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task BufferPrimitiveOverloadsMatchDynamicFormatting(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func text(StringBuffer b, String v) void { b.append(v); b.appendLine(v); }
            export native func integer(StringBuffer b, int32 v) void { b.append(v); b.appendLine(v); }
            export native func number(StringBuffer b, Number v) void { b.append(v); b.appendLine(v); }
            export native func boolean(StringBuffer b, Boolean v) void { b.append(v); b.appendLine(v); }
            export native func signed(StringBuffer b, int64 v) void { b.append(v); b.appendLine(v); }
            export native func unsigned(StringBuffer b, uint64 v) void { b.append(v); b.appendLine(v); }
            export func fallback(b, v) { b.append(v); b.appendLine(v); }
            export native func mixed(StringBuffer b) void { b.append('x', 2, true); b.appendLine(null, 'z'); }
            export native func spread(StringBuffer b) void { b.append(...['a', 'b']); }
            """, mode);
        var cases = new (string Name, Type Type, ScriptDatum Value)[] {
            ("text", typeof(string), ScriptDatum.FromString("hello")),
            ("integer", typeof(int), ScriptDatum.FromNumber(-42)),
            ("number", typeof(double), ScriptDatum.FromNumber(1.25)),
            ("boolean", typeof(bool), ScriptDatum.FromBoolean(true)),
            ("signed", typeof(long), ScriptDatum.FromInt64(long.MinValue)),
            ("unsigned", typeof(ulong), ScriptDatum.FromUInt64(ulong.MaxValue))
        };
        foreach (var (name, _, value) in cases)
        {
            var direct = new StringBuffer();
            var dynamic = new StringBuffer();
            TestWorkspace.Execute(domain, name, arguments: [ScriptDatum.FromObject(direct), value]);
            TestWorkspace.Execute(domain, "fallback", arguments: [ScriptDatum.FromObject(dynamic), value]);
            Assert.Equal(dynamic.ToString(), direct.ToString());
            Assert.Equal(ScriptDatum.ToString(value) + ScriptDatum.ToString(value) + Environment.NewLine, direct.ToString());
        }
        var mixed = new StringBuffer();
        TestWorkspace.Execute(domain, "mixed", arguments: [ScriptDatum.FromObject(mixed)]);
        Assert.Equal("x2Truenullz" + Environment.NewLine, mixed.ToString());
        var spread = new StringBuffer();
        TestWorkspace.Execute(domain, "spread", arguments: [ScriptDatum.FromObject(spread)]);
        Assert.Equal("ab", spread.ToString());
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var (name, type, _) in cases)
            {
                var calls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == name + "$native"));
                var appends = calls.Where(call => call.DeclaringType == typeof(StringBuffer) &&
                    call.Name is nameof(StringBuffer.AppendCore) or nameof(StringBuffer.AppendLineCore)).ToArray();
                Assert.Equal(2, appends.Length);
                Assert.All(appends, call => Assert.Equal(type, Assert.Single(call.GetParameters()).ParameterType));
                Assert.DoesNotContain(calls, call => call.Name.Contains("InvokeProperty"));
            }
            foreach (var name in new[] { "mixed", "spread" })
            {
                var calls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == name + "$native"));
                Assert.Contains(calls, call => call.Name.Contains("InvokeProperty"));
                Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(StringBuffer) &&
                    call.Name is nameof(StringBuffer.AppendCore) or nameof(StringBuffer.AppendLineCore));
            }
        }
#endif
    }

    [Fact]
    public void BuiltinsUseGeneratedTypesWithoutChangingObjectInfrastructure()
    {
        var catalog = new HostExportCatalog([]);
        var engine = new AuroraEngine(EngineOptions.Default);
        foreach (var type in new[] { typeof(StringBuffer), typeof(ScriptHashMap), typeof(ScriptRegex), typeof(ScriptDate), typeof(ScriptError) })
        {
            var name = type.GetCustomAttribute<NativeTypeAttribute>()!.TypeName;
            var constructor = Assert.Single(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                ctor => ctor.IsDefined(typeof(ExportAttribute), inherit: false));
            Assert.NotNull(constructor.GetCustomAttribute<ExportAttribute>()!.DynamicAdapter);
            Assert.True(catalog.TryGetNativeObject(name, out var descriptor));
            Assert.Equal(type, descriptor.ClrType);
            Assert.Null(descriptor.Constructor);
            var value = Assert.IsAssignableFrom<ScriptType>(type.GetField("Type")!.GetValue(null));
            Assert.Same(value, engine.Global.GetPropertyDatum(null!, name).Object);
            Assert.Equal(type == typeof(ScriptDate), value.IsFrozen);
        }
        Assert.Null(typeof(AuroraEngine).Assembly.GetType("AuroraScript.Hosting.NativeBuiltinAttribute"));
        Assert.False(typeof(ScriptError).IsSealed);
        Assert.False(typeof(IAuroraNativeInstance).IsAssignableFrom(typeof(ScriptHashMap)));
        Assert.True(new ScriptHashMap().Prototype.IsFrozen);
        Assert.True(new ScriptDate(0).Prototype.IsFrozen);
        Assert.True(new StringBuffer().Prototype.IsFrozen);
        Assert.DoesNotContain(typeof(AuroraEngine).Assembly.GetTypes(), type =>
            type.Name is "StringBufferConstructor" or "ScriptHashMapConstructor" or "ScriptRegexConstructor" or "ScriptDateConstructor" or "ScriptErrorConstructor");
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ConstructorsMembersAndLegacyConversionsArePreserved(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export func buffer() {
                var b = new StringBuffer(42);
                var append = b.append('a', null, 2);
                b.insert(1, '-');
                var text = b.toString();
                var released = b.stringAndRelease();
                b.append('x');
                b.clear();
                b.appendLine('z');
                return [typeof b, append, text, released, b.toString()];
            }
            export func map() {
                var map = new HashMap(-2);
                var set = map.set('a', 1);
                var calls = 0;
                var existing = map.getOrInsert('a', () => { calls++; return 9; });
                var added = map.getOrInsert('b', () => { calls++; return 2; });
                var keys = map.keys;
                var values = map.values;
                var seen = [];
                for (var key in map) { seen.push(key); }
                var removed = map.delete('a');
                var size = map.size;
                map.clear();
                return [typeof map, set, existing, added, calls, keys, values, seen, removed, size, map.size, map.has('a')];
            }
            export func regex() {
                var original = /a+/;
                var same = new Regex(original);
                var two = new Regex('a+', 'g');
                return [typeof same, same == original, same.test('aa'), same.test('x'), same.test(), two.test('x')];
            }
            export func date() {
                var date = new Date('2024-02-03');
                var parsed = Date.parse('2024-02-03');
                return [typeof date, date.year, date.month, date.day, date == parsed,
                    date.toString(), date.toString('yyyy/MM/dd'), typeof date.ticks,
                    new Date(), new Date('bad'), Date.parse('bad'), typeof Date.now(), typeof Date.utcNow(), Object.keys(date).length];
            }
            export func error() {
                var e = new Error('message');
                return [typeof e, e.message, Object.keys(e), new Error(), (new Error(1)).message];
            }
            export func errorObject() { return new Error('trace'); }
            export func cannotCall() {
                var count = 0;
                try { StringBuffer(); } catch (e) { count++; }
                try { HashMap(); } catch (e) { count++; }
                try { Regex('x'); } catch (e) { count++; }
                try { Error('x'); } catch (e) { count++; }
                return count;
            }
            export func alias(type) { return new type(); }
            export func derived(StringBuffer b, HashMap m, Date d, Regex r) {
                b.append('a');
                m.set('x', 3);
                return [b.toString(), m.get('x'), m.size,
                    [d.year, d.month, d.day, d.hour, d.minute, d.second, d.millisecond, d.dayOfYear], r.test('a')];
            }
            export func dynamicDateParts(d) {
                return [d.year, d.month, d.day, d.hour, d.minute, d.second, d.millisecond, d.dayOfYear];
            }
            """, mode, dateTimeFormat: "yyyy-MM-dd");
        ScriptAssert.Equal(new object?[] { "StringBuffer", null, "4-2anull2", "4-2anull2", "z" + Environment.NewLine }, TestWorkspace.Execute(domain, "buffer"));
        ScriptAssert.Equal(new object?[] { "HashMap", null, 1, 2, 1, new object[] { "a", "b" }, new object[] { 1, 2 }, new object[] { "a", "b" }, null, 1, 0, false }, TestWorkspace.Execute(domain, "map"));
        ScriptAssert.Equal(new object[] { "regex", true, true, false, false, true }, TestWorkspace.Execute(domain, "regex"));
        ScriptAssert.Equal(new object?[] { "date", 2024, 2, 3, true, "2024-02-03", "2024/02/03", "int64", null, null, null, "date", "date", 0 }, TestWorkspace.Execute(domain, "date"));
        ScriptAssert.Equal(new object?[] { "error", "message", new object[] { "message" }, null, "1" }, TestWorkspace.Execute(domain, "error"));
        Assert.NotEmpty(Assert.IsType<ScriptError>(TestWorkspace.Execute(domain, "errorObject").Object).StackTrace);
        ScriptAssert.Equal(4, TestWorkspace.Execute(domain, "cannotCall"));
        Assert.IsType<StringBuffer>(TestWorkspace.Execute(domain, "alias", arguments: [ScriptDatum.FromObject(StringBuffer.Type)]).Object);
        var regex = new ScriptRegex(new System.Text.RegularExpressions.Regex("a"), "");
        var date = ScriptDatum.FromDate(new ScriptDate(new DateTimeOffset(2024, 2, 29, 12, 34, 56, 789, TimeSpan.FromHours(8))));
        var dateParts = new object[] { 2024, 2, 29, 12, 34, 56, 789, 60 };
        ScriptAssert.Equal(new object[] { "a", 3, 1, dateParts, true }, TestWorkspace.Execute(domain, "derived", arguments:
            [ScriptDatum.FromObject(new StringBuffer()), ScriptDatum.FromObject(new ScriptHashMap()), date, ScriptDatum.FromRegex(regex)]));
        ScriptAssert.Equal(dateParts, TestWorkspace.Execute(domain, "dynamicDateParts", arguments: [date]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var calls = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods())
                .Where(method => method.Name.StartsWith("derived", StringComparison.Ordinal))
                .SelectMany(StringOptimizationTests.GetCalls).ToArray();
            foreach (var type in new[] { typeof(StringBuffer), typeof(ScriptHashMap), typeof(ScriptDate), typeof(ScriptRegex) })
                Assert.Contains(calls, call => call.DeclaringType == type);
            foreach (var name in new[] { "Year", "Month", "Day", "Hour", "Minute", "Second", "Millisecond", "DayOfYear" })
                Assert.Contains(calls, call => call.DeclaringType == typeof(ScriptDate) && call.Name == "get_" + name);
        }
#endif
    }
}
