using AuroraScript.Tests.Infrastructure;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class BuiltInLibraryTests
{
    [Fact]
    public async Task MathFunctionsAndConstantsReturnExpectedValues()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                return [Math.abs(-4), Math.max(2, 5), Math.min(2, 5), Math.pow(2, 5), Math.floor(2.9), Math.round(2.6), Math.PI > 3];
            }
            """);

        ScriptAssert.Equal(new object?[] { 4, 5, 2, 32, 2, 3, true }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task StringPrototypeCoversSearchSliceReplaceSplitTrimPaddingAndCase()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var text = '  AuroraScript  ';
                var clean = text.trim();
                return [clean.length, clean.contains('Script'), clean.indexOf('ora'), clean.startsWith('Aur'), clean.endsWith('Script'), clean.substring(1, 4), clean.replace('Aurora', 'A'), clean.split('S').join('|'), clean.toLowerCase(), clean.toUpperCase(), '7'.padLeft(3, '0')];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { 12, true, 3, true, true, "uro", "AScript", "Aurora|cript", "aurorascript", "AURORASCRIPT", "007" },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ArrayPrototypeCoversMutationSearchTransformAndReduction()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var values = [3, 1, 2];
                values.push(4);
                var popped = values.pop();
                values.sort();
                var mapped = values.map((value) => value * 2);
                var filtered = mapped.filter((value) => value > 2);
                var reduced = filtered.reduce((sum, value) => sum + value, 0);
                return [popped, values.join(','), values.indexOf(2), values.has(3), mapped.join(','), filtered.join(','), reduced, values.slice(1, 3).join(',')];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { 4, "1,2,3", 1, true, "2,4,6", "4,6", 10, "2,3" },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task JsonRoundTripsNestedValuesAndRejectsCircularReferences()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func roundTrip() {
                var value = JSON.parse('{"name":"Aurora","items":[1,true,null]}');
                return [value.name, value.items[0], value.items[1], value.items[2], JSON.stringify(value)];
            }
            export func circular() {
                var value = {};
                value.self = value;
                return JSON.stringify(value);
            }
            """);

        ScriptAssert.Equal(
            new object?[] { "Aurora", 1, true, null, "{\"name\":\"Aurora\",\"items\":[1,true,null]}" },
            TestWorkspace.Execute(domain, "roundTrip"));
        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "circular"));
    }

    [Fact]
    public async Task HashMapSupportsPrimitiveAndObjectKeys()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var map = new HashMap();
                var key = { id: 1 };
                map.set('name', 'Aurora');
                map.set(key, 42);
                var before = [map.size, map.has('name'), map.get('name'), map.get(key)];
                map.delete('name');
                return [before[0], before[1], before[2], before[3], map.has('name'), map.keys.length, map.values.length];
            }
            """);

        ScriptAssert.Equal(new object?[] { 2, true, "Aurora", 42, false, 1, 1 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task HashMapSupportsCapacityAndLazyGetOrInsert()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var calls = 0;
                var map = new HashMap(16);
                map.set("exists", 1);
                var hit = map.getOrInsert("exists", () => {
                    calls = calls + 1;
                    return 2;
                });
                var inserted = map.getOrInsert("missing", 3);
                return [hit, inserted, map.get("missing"), calls, map.size];
            }
            """);

        ScriptAssert.Equal(new object?[] { 1, 3, 3, 0, 2 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task HashMapEnumerationUsesSnapshotWhenMutated()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var map = new HashMap();
                map.set("a", 1);
                map.set("b", 2);
                var keys = [];
                for (var key in map) {
                    keys.push(key);
                    map.set("c", 3);
                    map.delete("b");
                }
                return [keys.length, keys.indexOf("a") >= 0, keys.indexOf("b") >= 0, map.has("c"), map.has("b")];
            }
            """);

        ScriptAssert.Equal(new object?[] { 2, true, true, true, false }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task HashMapStringKeysWorkWithoutStringPooling()
    {
        using var workspace = new TestWorkspace();
        var engine = new AuroraEngine(EngineOptions.Default
            .WithBaseDirectory(workspace.Root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithOptimizeOption(OptimizeOptions.Release)
            .WithEnableHotReload(false)
            .WithConsoleStdOut(TextWriter.Null)
            .WithConsoleErrorOut(TextWriter.Null)
            .WithStringPooling(StringPoolingStrategy.None));
        var sourcePath = workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            export func run() {
                var map = new HashMap();
                map.set("k" + 1, 42);
                return map.get("k1");
            }
            """);
        await engine.BuildAsync(engine.FileSource(sourcePath, Encoding.UTF8));
        var domain = engine.CreateDomain();

        Assert.Equal(42d, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task HashMapKeepsStringAndNumberKeysSeparate()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var map = new HashMap();
                map.set(1, "number");
                map.set("1", "string");
                return [map.get(1), map.get("1"), map.size];
            }
            """);

        ScriptAssert.Equal(new object?[] { "number", "string", 2 }, TestWorkspace.Execute(domain, "run"));
    }


    [Fact]
    public async Task RegexLiteralAndConstructorSupportFlagsAndMatching()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var literal = /aurora/i;
                var constructed = new Regex('^script$', 'i');
                return [literal.test('AURORA Script'), constructed.test('SCRIPT'), /x/.test('no')];
            }
            """);

        ScriptAssert.Equal(new object?[] { true, true, false }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task StringBufferSupportsAppendInsertClearAndRelease()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var buffer = new StringBuffer('AC');
                buffer.insert(1, 'B');
                buffer.append('D', 1);
                var first = buffer.toString();
                buffer.clear();
                buffer.append('done');
                return [first, buffer.stringAndRelease()];
            }
            """);

        ScriptAssert.Equal(new object?[] { "ABCD1", "done" }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ConsoleWritesToConfiguredOutput()
    {
        using var workspace = new TestWorkspace();
        using var output = new System.IO.StringWriter();
        var engine = workspace.CreateEngine(output: output);
        await engine.BuildAsync(engine.MemorySource(
            "main.as",
            "@module(TEST); export func run() { console.log('value', 42); console.error('failure'); }"));

        TestWorkspace.Execute(engine.CreateDomain(), "run");

        Assert.Contains("value", output.ToString());
        Assert.Contains("42", output.ToString());
        Assert.Contains("failure", output.ToString());
    }
}
