using AuroraScript.Tests.Infrastructure;
using System.IO;
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
    public async Task TDocParsesAndStringifiesTypedValuesInsideScripts()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func roundTrip() {
                var number = TDoc.parse('Number 42.5');
                var value = TDoc.parse('Object { readonly String id "u-1", Int8Array bytes [1, 2], }');
                var compact = TDoc.stringify(value, false);
                var explicit = TDoc.stringify(value, false, true);
                var pretty = TDoc.stringify(value);
                return [number, value.id, value.bytes[1], compact, explicit, pretty.contains('\n')];
            }
            export func invalid() {
                return TDoc.parse('Object { name }');
            }
            """);

        ScriptAssert.Equal(
            new object?[] { 42.5, "u-1", 2, "{readonly id \"u-1\",Int8Array bytes [1,2]}", "Object {readonly String id \"u-1\",Int8Array bytes [1,2]}", true },
            TestWorkspace.Execute(domain, "roundTrip"));

        var error = Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "invalid"));
        Assert.Contains("TDoc.parse error", error.Message);
    }

    [Fact]
    public async Task TDocStringifySkipsRuntimeOnlyValues()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func objectValue() {
                var runtimeOnly = () => true;
                var value = {
                    name: 'Aurora',
                    cancel: runtimeOnly,
                    nested: { reset: runtimeOnly, count: 2 },
                    values: [1, runtimeOnly, 3],
                };
                return TDoc.stringify(value, false);
            }
            export func rootValue() {
                return TDoc.stringify(() => true, false);
            }
            """);

        Assert.Equal(
            "{name \"Aurora\",nested {count 2},values [1,null,3]}",
            TestWorkspace.Execute(domain, "objectValue"));
        Assert.Equal("null", TestWorkspace.Execute(domain, "rootValue"));
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
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(workspace.Root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithRuntime(runtime => runtime.ConsoleStdOut = TextWriter.Null)
            .WithRuntime(runtime => runtime.ConsoleErrorOut = TextWriter.Null)
            .WithRuntime(runtime => runtime.StringPooling = StringPoolingStrategy.None));
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
        await engine.BuildAsync(sourcePath);
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
    public async Task ValueEqualitySupportsStringBufferAndDate()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var leftBuffer = new StringBuffer('A');
                leftBuffer.append('B');
                var sameBuffer = new StringBuffer('AB');
                var otherBuffer = new StringBuffer('AC');
                var date = Date.parse('2020-01-02');
                var sameDate = new Date('2020-01-02');
                var otherDate = Date.parse('2020-01-03');
                return [
                    leftBuffer == sameBuffer,
                    leftBuffer != otherBuffer,
                    Object.equal(leftBuffer, sameBuffer),
                    Object.deepEqual(leftBuffer, sameBuffer),
                    Object.equal$(leftBuffer, sameBuffer),
                    date == sameDate,
                    date != otherDate,
                    Object.equal(date, sameDate),
                    Object.deepEqual(date, sameDate),
                    Object.equal$(date, sameDate)
                ];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { true, true, true, true, false, true, true, true, true, true },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task PathSupportsProtocolAwareMutableValues()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var path = Path.of('mem://app/scripts', '../shared', 'main');
                var constructed = new Path('res://pkg/modules', './runtime');
                path.changeExt('.as');
                constructed.changeExt('as');
                var clone = path.clone().append('..', 'generated', './config');
                var same = new Path('mem://app/shared/main.as');
                return [
                    Path.isPath(path),
                    Path.isPath(constructed),
                    typeof path,
                    constructed.toString(),
                    path.toString(),
                    path.directoryName(),
                    path.fileName(),
                    path.extName(),
                    path.protocol(),
                    Path.protocol('asset://pkg/textures/ui.png'),
                    Path.protocol('C:/scripts/main.as'),
                    Path.extName('asset://pkg/textures/ui.png'),
                    clone.toString(),
                    Path.join('asset://pkg/textures', './ui', 'button.png'),
                    Path.changeExt('res://pkg/modules/main', 'as'),
                    Path.isRooted('mem://app/main.as'),
                    Path.isUnderRoot('mem://app', 'mem://app/shared/main.as'),
                    Path.isUnderRoot('mem://app', 'mem://app2/shared/main.as'),
                    path == same,
                    path != same,
                    path == constructed,
                    Object.equal(path, same),
                    Object.deepEqual(path, same),
                    Object.equal$(path, same),
                    Object.equal(path, constructed)
                ];
            }
            """);

        ScriptAssert.Equal(
            new object?[]
            {
                true,
                true,
                "Path",
                "res://pkg/modules/runtime.as",
                "mem://app/shared/main.as",
                "mem://app/shared",
                "main.as",
                ".as",
                "mem",
                "asset",
                "",
                ".png",
                "mem://app/shared/generated/config",
                "asset://pkg/textures/ui/button.png",
                "res://pkg/modules/main.as",
                true,
                true,
                false,
                true,
                false,
                false,
                true,
                true,
                false,
                false
            },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ConsoleWritesToConfiguredOutput()
    {
        using var workspace = new TestWorkspace();
        using var output = new System.IO.StringWriter();
        var engine = workspace.CreateEngine(output: output);
        await engine.BuildAsync(workspace.MemorySource(
            "main.as",
            "@module(TEST); export func run() { console.log('value', 42); console.error('failure'); console.time('timer'); console.timeEnd('timer'); }"));

        TestWorkspace.Execute(engine.CreateDomain(), "run");

        Assert.Contains("value", output.ToString());
        Assert.Contains("42", output.ToString());
        Assert.Contains("failure", output.ToString());
        Assert.Contains("timer Used ", output.ToString());
    }

    [Fact]
    public async Task ConsoleAndHotPatchAreStaticOnlyNativeTypes()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        await engine.BuildAsync(workspace.MemorySource(
            "main.as",
            """
            @module(TEST);
            export func names() { return [typeof console, typeof HotPatch]; }
            export func newConsole() { return new console(); }
            export func newHotPatch() { return new HotPatch(); }
            """));
        using var domain = engine.CreateDomain();

        ScriptAssert.Equal(
            new object?[] { "type", "type" },
            TestWorkspace.Execute(domain, "names"));
        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "newConsole"));
        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "newHotPatch"));
    }
}
