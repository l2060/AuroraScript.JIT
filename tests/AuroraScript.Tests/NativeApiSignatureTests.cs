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

public sealed class NativeApiSignatureTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ConstrainedArgumentsUseNativeSignaturesAndUnknownArgumentsUseAdapters(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var source = """
            @module(TEST);
            export native func join(Array a, String s) String { return a.join(s); }
            export func weakJoin(Array a, s) { return a.join(s); }
            export native func slice(Array a, int32 start, int32 end) Array { return a.slice(start, end); }
            export native func flat(Array a, int32 depth) Array { return a.flat(depth); }
            export native func normalize(String s) String { return Path.normalize(s); }
            export native func normalizePath(Path p) String { return Path.normalize(p); }
            export func weakPath(s) { return Path.normalize(s); }
            export native func test(Regex r, String s) Boolean { return r.test(s); }
            export func weakTest(Regex r, s) { return r.test(s); }
            export native func match(String s, Regex r) Object { return s.match(r); }
            export native func replace(String s, Regex r, String replacement) String { return s.replace(r, replacement); }
            export native func parse(String s) Date { return Date.parse(s); }
            export func weakDate(s) { return Date.parse(s); }
            export native func print(int32 v) void { console.log(v); }
            export func weakPrint(v) { console.log(v); }
            export func resize(Array a, value) {
                try { a.length = value; return a.length; }
                catch (error) { return -1; }
            }
            """;
        using var output = new StringWriter();
        var engine = workspace.CreateEngine(mode, output: output,
            assemblyOut: mode == CompilationMode.Persistence ? Path.Combine(workspace.Root, "test-output.dll") : null);
        workspace.WriteSource("main.as", source);
        await engine.BuildAsync(["main.as"]);
        var domain = engine.CreateDomain();
        var array = ScriptDatum.FromArray(new ScriptArray(new[] { ScriptDatum.FromNumber(1), ScriptDatum.FromNumber(2), ScriptDatum.FromNumber(3) }));
        ScriptAssert.Equal("1,2,3", TestWorkspace.Execute(domain, "join", arguments: [array, ScriptDatum.FromString(",")]));
        ScriptAssert.Equal("1true2true3", TestWorkspace.Execute(domain, "weakJoin", arguments: [array, ScriptDatum.FromBoolean(true)]));
        ScriptAssert.Equal(new object[] { 2, 3 }, TestWorkspace.Execute(domain, "slice", arguments: [array, ScriptDatum.FromNumber(1), ScriptDatum.FromNumber(3)]));
        ScriptAssert.Equal(new object[] { 1, 2, 3 }, TestWorkspace.Execute(domain, "flat", arguments: [array, ScriptDatum.FromNumber(1)]));
        var text = ScriptDatum.FromString("a/../b");
        ScriptAssert.Equal("b", TestWorkspace.Execute(domain, "normalize", arguments: [text]));
        ScriptAssert.Equal("b", TestWorkspace.Execute(domain, "normalizePath", arguments: [ScriptDatum.FromObject(new ScriptPathValue("a/../b"))]));
        ScriptAssert.Equal("", TestWorkspace.Execute(domain, "weakPath", arguments: [ScriptDatum.FromNumber(42)]));
        var regex = ScriptDatum.FromRegex(new ScriptRegex(new System.Text.RegularExpressions.Regex("a"), "g"));
        ScriptAssert.Equal(true, TestWorkspace.Execute(domain, "test", arguments: [regex, ScriptDatum.FromString("abc")]));
        ScriptAssert.Equal(false, TestWorkspace.Execute(domain, "weakTest", arguments: [regex, ScriptDatum.FromNumber(42)]));
        var matches = TestWorkspace.Execute(domain, "match", arguments: [ScriptDatum.FromString("aba"), regex]);
        Assert.Equal(ValueKind.Object, matches.Kind);
        ScriptAssert.Equal(new object[] { "a", "a" }, ScriptDatum.FromArray(Assert.IsType<ScriptArray>(matches.Object)));
        ScriptAssert.Equal("xbx", TestWorkspace.Execute(domain, "replace", arguments: [ScriptDatum.FromString("aba"), regex, ScriptDatum.FromString("x")]));
        foreach (var input in new[] { "2024-02-03", "123", "bad" })
        {
            var direct = TestWorkspace.Execute(domain, "parse", arguments: [ScriptDatum.FromString(input)]);
            var fallback = TestWorkspace.Execute(domain, "weakDate", arguments: [ScriptDatum.FromString(input)]);
            Assert.Equal(fallback.Kind, direct.Kind);
            Assert.Equal(ScriptDatum.ToString(fallback), ScriptDatum.ToString(direct));
        }
        TestWorkspace.Execute(domain, "print", arguments: [ScriptDatum.FromNumber(42)]);
        TestWorkspace.Execute(domain, "weakPrint", arguments: [ScriptDatum.FromNumber(42)]);
        Assert.Equal("42" + Environment.NewLine + "42" + Environment.NewLine, output.ToString());
        ScriptAssert.Equal(2, TestWorkspace.Execute(domain, "resize", arguments: [array, ScriptDatum.FromInt64(2)]));
        foreach (var invalid in new[] { ScriptDatum.FromString("2"), ScriptDatum.FromNumber(1.5), ScriptDatum.FromBoolean(true) })
            ScriptAssert.Equal(-1, TestWorkspace.Execute(domain, "resize", arguments: [array, invalid]));
        Assert.Equal(2, ((ScriptArray)array.Object).Length);
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var (name, core, type) in new[] {
                ("join", "JoinCore", typeof(string)), ("slice", "SliceCore", typeof(int)),
                ("flat", "FlatCore", typeof(int)), ("normalize", "NormalizeCore", typeof(string)),
                ("normalizePath", "NormalizeCore", typeof(ScriptPathValue)), ("test", "Test", typeof(string)),
                ("match", "MatchCore", typeof(ScriptRegex)), ("replace", "ReplaceCore", typeof(string)),
                ("parse", "ParseCore", typeof(string)), ("print", "LogCore", typeof(int)) })
            {
                var calls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == name + "$native"));
                Assert.Contains(calls, call => call.Name == core && call.GetParameters()[^1].ParameterType == type);
                Assert.DoesNotContain(calls, call => call.Name.Contains("InvokeProperty"));
            }
            foreach (var name in new[] { "weakJoin", "weakPath", "weakTest", "weakDate", "weakPrint" })
            {
                var calls = methods.Where(method => method.Name == name || method.Name.StartsWith(name + "$", StringComparison.Ordinal))
                    .SelectMany(StringOptimizationTests.GetCalls).ToArray();
                Assert.Contains(calls, call => call.Name.Contains("InvokeProperty"));
            }
        }
#endif
    }
}
