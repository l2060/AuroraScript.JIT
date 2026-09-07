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
                return [b.toString(), m.get('x'), m.size, d.year, r.test('a')];
            }
            """, mode, dateTimeFormat: "yyyy-MM-dd");
        ScriptAssert.Equal(new object?[] { "StringBuffer", null, "4-2anull2", "4-2anull2", "z" + Environment.NewLine }, TestWorkspace.Execute(domain, "buffer"));
        ScriptAssert.Equal(new object?[] { "HashMap", null, 1, 2, 1, new object[] { "a", "b" }, new object[] { 1, 2 }, new object[] { "a", "b" }, null, 1, 0, false }, TestWorkspace.Execute(domain, "map"));
        ScriptAssert.Equal(new object[] { "regex", true, true, false, false, true }, TestWorkspace.Execute(domain, "regex"));
        ScriptAssert.Equal(new object?[] { "date", 2024, 2, 3, true, "2024-02-03", "2024/02/03", "number", null, null, null, "date", "date", 0 }, TestWorkspace.Execute(domain, "date"));
        ScriptAssert.Equal(new object?[] { "error", "message", new object[] { "message" }, null, "1" }, TestWorkspace.Execute(domain, "error"));
        Assert.NotEmpty(Assert.IsType<ScriptError>(TestWorkspace.Execute(domain, "errorObject").Object).StackTrace);
        ScriptAssert.Equal(4, TestWorkspace.Execute(domain, "cannotCall"));
        Assert.IsType<StringBuffer>(TestWorkspace.Execute(domain, "alias", arguments: [ScriptDatum.FromObject(StringBuffer.Type)]).Object);
        var regex = new ScriptRegex(new System.Text.RegularExpressions.Regex("a"), "");
        ScriptAssert.Equal(new object[] { "a", 3, 1, 2024, true }, TestWorkspace.Execute(domain, "derived", arguments:
            [ScriptDatum.FromObject(new StringBuffer()), ScriptDatum.FromObject(new ScriptHashMap()), ScriptDatum.FromDate(new ScriptDate(new DateTime(2024, 1, 1))), ScriptDatum.FromRegex(regex)]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var calls = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods())
                .Where(method => method.Name.StartsWith("derived", StringComparison.Ordinal))
                .SelectMany(StringOptimizationTests.GetCalls).ToArray();
            foreach (var type in new[] { typeof(StringBuffer), typeof(ScriptHashMap), typeof(ScriptDate), typeof(ScriptRegex) })
                Assert.Contains(calls, call => call.DeclaringType == type);
        }
#endif
    }
}
