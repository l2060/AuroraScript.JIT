using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class StringMemberCompletionTests
{
    [Fact]
    public void UnchangedLiteralReplaceAndPaddingDoNotAllocate()
    {
        const string value = "Aurora";
        for (var i = 0; i < 100; i++)
        {
            StringValue.ReplaceCore(value, "missing", "other");
            StringValue.PadLeftCore(value, 2, "0");
            StringValue.PadRightCore(value, 2, "0");
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        string replaced = "", left = "", right = "";
        for (var i = 0; i < 1000; i++)
        {
            replaced = StringValue.ReplaceCore(value, "missing", "other");
            left = StringValue.PadLeftCore(value, 2, "0");
            right = StringValue.PadRightCore(value, 2, "0");
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Same(value, replaced);
        Assert.Same(value, left);
        Assert.Same(value, right);
    }

    [Fact]
    public void AllOwnPrototypeMembersHaveGeneratedNativeMetadata()
    {
        string[] expected = ["length", "substring", "slice", "charCodeAt", "contains", "indexOf",
            "lastIndexOf", "startsWith", "endsWith", "trim", "trimLeft", "trimRight", "toString",
            "toLowerCase", "toUpperCase", "split", "match", "matchAll", "replace", "padLeft", "padRight"];
        var members = typeof(StringValue).Assembly.GetCustomAttributes<AuroraGeneratedNativeMethodAttribute>()
            .Where(member => member.DeclaringType == typeof(StringValue)).ToArray();
        Assert.Equal(expected.OrderBy(name => name), members.Select(member => member.MemberName).Distinct().OrderBy(name => name));
        Assert.All(members, member => Assert.Equal(typeof(string), member.ReceiverType));
        Assert.All(members.Where(member => member.MemberName is "match" or "matchAll"),
            member => Assert.Equal(AuroraExportValueKind.Datum, member.ReturnKind));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task RemainingMembersPreserveValuesExceptionsAndDatumFallback(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export func split(String text, String separator) { return text.split(separator); }
            export func weakSplit(String text, separator) { return text.split(separator); }
            export func dynamicSplit(text, separator) { return text.split(separator); }
            export native func left(String text, int32 width, String pad) String { return text.padLeft(width, pad); }
            export native func right(String text, int32 width, String pad) String { return text.padRight(width, pad); }
            export func weakLeft(String text, width, pad) { return text.padLeft(width, pad); }
            export func weakRight(String text, width, pad) { return text.padRight(width, pad); }
            export func dynamicLeft(text, width, pad) { return text.padLeft(width, pad); }
            export func dynamicRight(text, width, pad) { return text.padRight(width, pad); }
            export native func replace(String text, String search, String replacement) String { return text.replace(search, replacement); }
            export func weakReplace(String text, search, replacement) { return text.replace(search, replacement); }
            export func dynamicReplace(text, search, replacement) { return text.replace(search, replacement); }
            export func match(String text, String pattern) { return text.match(pattern); }
            export func weakMatch(String text, pattern) { return text.match(pattern); }
            export func dynamicMatch(text, pattern) { return text.match(pattern); }
            export func all(String text, String pattern) { return text.matchAll(pattern); }
            export func weakAll(String text, pattern) { return text.matchAll(pattern); }
            export func dynamicAll(text, pattern) { return text.matchAll(pattern); }
            export func shapes(String text) {
                return [text.split(), text.split(...['a']), text.split('a', 1), text.padLeft(),
                    text.padRight(), text.replace(), text.replace('a'), text.match(), text.matchAll(),
                    text.match(...['a']), text.matchAll('a', 1)];
            }
            export func dynamicShapes(text) {
                return [text.split(), text.split(...['a']), text.split('a', 1), text.padLeft(),
                    text.padRight(), text.replace(), text.replace('a'), text.match(), text.matchAll(),
                    text.match(...['a']), text.matchAll('a', 1)];
            }
            export func splitChain(String text) { return text.split(',').join('|'); }
            export func borrowed(String text) { var split = text.split; var replace = text.replace; return [split(','), replace('a', 'x')]; }
            export func unknownReceiver(text) { return [text.split(','), text.replace('a', 'b')]; }
            """, mode);
        using var scope = domain;
        ScriptDatum[] weak = [ScriptDatum.Null, ScriptDatum.FromBoolean(true), ScriptDatum.FromNumber(3.75),
            ScriptDatum.FromInt64(9007199254740993), ScriptDatum.FromUInt64(ulong.MaxValue),
            ScriptDatum.FromObject(new ScriptObject())];
        foreach (var input in new[] { "", "aba,a", "undefined true 3.75 9007199254740993", "中文😀" })
        {
            var text = ScriptDatum.FromString(input);
            SameCall(domain, "shapes", "dynamicShapes", text);
            foreach (var part in new[] { "", "a", ",", "😀" })
            {
                var search = ScriptDatum.FromString(part);
                SameCall(domain, "split", "dynamicSplit", text, search);
                SameCall(domain, "replace", "dynamicReplace", text, search, ScriptDatum.FromString("xy"));
                SameCall(domain, "match", "dynamicMatch", text, search);
                SameCall(domain, "all", "dynamicAll", text, search);
            }
            foreach (var argument in weak)
            {
                SameCall(domain, "weakSplit", "dynamicSplit", text, argument);
                SameCall(domain, "weakMatch", "dynamicMatch", text, argument);
                SameCall(domain, "weakAll", "dynamicAll", text, argument);
                SameCall(domain, "weakReplace", "dynamicReplace", text, argument, argument);
            }
            foreach (var pattern in new[] { "(?<letter>a)(b)?", "z", "" })
                foreach (var flags in new[] { "", "g" })
                {
                    var regex = ScriptDatum.FromObject(new ScriptRegex(new Regex(pattern), flags));
                    SameCall(domain, "weakMatch", "dynamicMatch", text, regex);
                    SameCall(domain, "weakAll", "dynamicAll", text, regex);
                    SameCall(domain, "weakReplace", "dynamicReplace", text, regex, ScriptDatum.FromString("[$1]"));
                }
            foreach (var width in new[] { int.MinValue, -1, 0, 3, 12 })
                foreach (var pad in new[] { "", "0", "xy", "😀" })
                    foreach (var side in new[] { "Left", "Right" })
                        SameCall(domain, side.ToLowerInvariant(), "dynamic" + side, text,
                            ScriptDatum.FromNumber(width), ScriptDatum.FromString(pad));
            foreach (var width in weak.Concat([ScriptDatum.FromNumber(double.NaN), ScriptDatum.FromNumber(3.9),
                ScriptDatum.FromUInt64(0x100000003), ScriptDatum.FromNumber(double.PositiveInfinity)]))
                foreach (var side in new[] { "Left", "Right" })
                    SameCall(domain, "weak" + side, "dynamic" + side, text, width, ScriptDatum.FromString("0"));
        }
        ScriptAssert.Equal(new object[] { "a", "b" }, TestWorkspace.Execute(domain, "split", arguments: [ScriptDatum.FromString("a,b"), ScriptDatum.FromString(",")]));
        ScriptAssert.Equal("a|b", TestWorkspace.Execute(domain, "splitChain", arguments: [ScriptDatum.FromString("a,b")]));
        ScriptAssert.Equal(new object[] { new object[] { "a", "b" }, "x,b" },
            TestWorkspace.Execute(domain, "borrowed", arguments: [ScriptDatum.FromString("a,b")]));
        var noMatch = TestWorkspace.Execute(domain, "match", arguments: [ScriptDatum.FromString("abc"), ScriptDatum.FromString("z")]);
        Assert.Equal(ValueKind.Object, noMatch.Kind);
        Assert.Same(ScriptObject.Null, noMatch.Object);
        ScriptAssert.Equal(null, TestWorkspace.Execute(domain, "all", arguments: [ScriptDatum.FromString("abc"), ScriptDatum.FromString("z")]));
        var custom = new ScriptObject();
        custom.Define("split", ScriptDatum.FromBonding((ScriptContext ctx, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) => result = ScriptDatum.FromString("custom split")));
        custom.Define("replace", ScriptDatum.FromBonding((ScriptContext ctx, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) => result = ScriptDatum.FromString("custom replace")));
        ScriptAssert.Equal(new object[] { "custom split", "custom replace" },
            TestWorkspace.Execute(domain, "unknownReceiver", arguments: [ScriptDatum.FromObject(custom)]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var name in new[] { "left", "right", "replace" })
            {
                var calls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == name + "$native"));
                Assert.Contains(calls, call => call.DeclaringType == typeof(StringValue));
                Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(CallOps) || call.DeclaringType == typeof(ScriptDatum));
            }
            foreach (var (name, core, argumentType) in new[] {
                ("split", "SplitCore", typeof(string)), ("match", "MatchCore", typeof(string)),
                ("all", "MatchAllCore", typeof(string)), ("weakMatch", "MatchCore", typeof(ScriptDatum)),
                ("weakAll", "MatchAllCore", typeof(ScriptDatum)), ("weakReplace", "ReplaceCore", typeof(ScriptDatum)) })
            {
                var calls = methods.Where(method => method.Name == name || method.Name.StartsWith(name + "$", StringComparison.Ordinal))
                    .SelectMany(StringOptimizationTests.GetCalls).ToArray();
                Assert.Contains(calls, call => call.DeclaringType == typeof(StringValue) && call.Name == core && call.GetParameters()[^1].ParameterType == argumentType);
                Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(CallOps));
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
    public async Task RegexCallbacksPreserveCapturesOrderingReentrancyAndPoolCleanup(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export func run(String text) {
                var captures = [];
                var replaced = text.replace(/(a)(b)?/g, (whole, a, b, index, input) => {
                    captures.push([whole, a, b, index, input]);
                    return 'a'.replace(/a/g, () => 'X');
                });
                return [replaced, captures, text.replace('a', () => 'X')];
            }
            export func large(String text) {
                var captures = [];
                var replaced = text.replace(/(a)(b)?(c)?(d)?(e)?(f)?(g)?/g, (whole, a, b, c, d, e, f, g, index, input) => {
                    captures.push([whole, a, b, c, d, e, f, g, index, input]); return 'X';
                });
                return [replaced, captures];
            }
            export func throws(String text) {
                return text.replace(/(a)(b)?(c)?(d)?(e)?(f)?(g)?/g, () => { throw 'callback failed'; });
            }
            export func metadata(String text) {
                var match = text.match(/(?<letter>a)(b)?/);
                var all = text.matchAll(/(?<letter>a)(b)?/g);
                return [match[0], match.index, match.input, match.groups.letter,
                    all.length, all[1][0], all[1].index, all[1].input, all[1].groups.letter];
            }
            export func ordered() {
                var order = '';
                func receiver() { order += 'r'; return 'abc'; }
                func search() { order += 's'; return /a/; }
                func replacement() { order += 'v'; return 'x'; }
                var result = receiver().replace(search(), replacement()); return [result, order];
            }
            export func replacementShapes(String text) {
                return [text.replace(/a/, 'X'), text.replace(/a/g, 'X'), text.replace(/(a)/g, '[$1]'),
                    text.replace(/z/g, 'X'), text.replace(/a/g, 42), text.replace(/a/g, null)];
            }
            """, mode);
        using var scope = domain;
        ScriptAssert.Equal(new object?[] { "X X", new object?[] {
            new object?[] { "ab", "a", "b", 0, "ab a" }, new object?[] { "a", "a", null, 3, "ab a" } }, "ab a" },
            TestWorkspace.Execute(domain, "run", arguments: [ScriptDatum.FromString("ab a")]));
        for (var i = 0; i < 3; i++)
        {
            Assert.NotNull(Record.Exception(() => TestWorkspace.Execute(domain, "throws", arguments: [ScriptDatum.FromString("abcdefg")])));
            ScriptAssert.Equal(new object?[] { "X X", new object?[] {
                new object?[] { "abcdefg", "a", "b", "c", "d", "e", "f", "g", 0, "abcdefg a" },
                new object?[] { "a", "a", null, null, null, null, null, null, 8, "abcdefg a" } } },
                TestWorkspace.Execute(domain, "large", arguments: [ScriptDatum.FromString("abcdefg a")]));
        }
        ScriptAssert.Equal(new object[] { "ab", 0, "ab a", "a", 2, "a", 3, "ab a", "a" },
            TestWorkspace.Execute(domain, "metadata", arguments: [ScriptDatum.FromString("ab a")]));
        ScriptAssert.Equal(new object[] { "xbc", "rsv" }, TestWorkspace.Execute(domain, "ordered"));
        ScriptAssert.Equal(new object[] { "Xba", "XbX", "[a]b[a]", "aba", "42b42", "nullbnull" },
            TestWorkspace.Execute(domain, "replacementShapes", arguments: [ScriptDatum.FromString("aba")]));
    }

    private static void SameCall(ScriptDomain domain, string native, string dynamic, params ScriptDatum[] args)
    {
        ScriptDatum expected = default, actual = default;
        var expectedError = Record.Exception(() => expected = TestWorkspace.Execute(domain, dynamic, arguments: args));
        var actualError = Record.Exception(() => actual = TestWorkspace.Execute(domain, native, arguments: args));
        Assert.Equal(expectedError?.GetType(), actualError?.GetType());
        if (expectedError == null) SameDatum(expected, actual);
    }

    private static void SameDatum(ScriptDatum expected, ScriptDatum actual)
    {
        Assert.Equal(expected.Kind, actual.Kind);
        if (expected.Object is ScriptArray left)
        {
            var right = Assert.IsType<ScriptArray>(actual.Object);
            Assert.Equal(left.Length, right.Length);
            for (var i = 0; i < left.Length; i++) SameDatum(left.GetElement(i), right.GetElement(i));
        }
        else Assert.Equal(ScriptDatum.ToString(expected), ScriptDatum.ToString(actual));
    }
}
