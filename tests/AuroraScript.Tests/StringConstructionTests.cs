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

public sealed class StringConstructionTests
{
    [Fact]
    public void StringFactoryAndStaticMembersUseGeneratedMetadata()
    {
        var assembly = typeof(StringValue).Assembly;
        var metadata = Assert.Single(assembly.GetCustomAttributes<AuroraGeneratedNativeObjectAttribute>(),
            item => item.ObjectType == typeof(StringValue));
        Assert.Equal("valueOf", metadata.FactoryMemberName);
        Assert.Equal(typeof(string), metadata.ReceiverType);
        Assert.False(metadata.Constructible); // No CLR StringValue constructor is invoked by native new.
        var members = assembly.GetCustomAttributes<AuroraGeneratedExportAttribute>()
            .Where(item => item.DeclaringType == typeof(StringValue)).ToArray();
        Assert.Equal(new[] { "compare", "fromCharCode", "valueOf" }, members.Select(item => item.MemberName).OrderBy(name => name));
        Assert.Equal("String", StringValue.Type.Name);
        Assert.True(StringValue.Type.IsFrozen);
        Assert.Null(assembly.GetType("AuroraScript.Runtime.Types.TypeConstruct.StringConstructor"));
        Assert.Null(typeof(StringValue).GetMethod("TOSTRING", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task IndependentStringWrappersRetainTextEquality(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export func run(left, right) {
                var map = new HashMap(); map.set(left, 42);
                return [left == right, map.get(right), left.length, right.toString()];
            }
            """, mode);
        using var scope = domain;
        var text = new string(['a', 'b', 'c']);
        var first = StringValue.Of(text);
        var second = StringValue.Of(text);
        Assert.NotSame(first, second);
        Assert.Same(text, first.Value);
        Assert.Same(text, second.Value);
        Assert.Same(StringValue.Empty, StringValue.Of(""));
        Assert.Same(StringValue.Empty, StringValue.Of(null!));
        ScriptAssert.Equal(new object[] { true, 42, 3, "abc" }, TestWorkspace.Execute(domain, "run",
            arguments: [ScriptDatum.FromString(first), ScriptDatum.FromString(second)]));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task FactoriesAndStaticsPreserveDynamicConversionAndNativeSignatures(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func create(String value) String { return String(value); }
            export native func construct(String value) String { return new String(value); }
            export native func valueOf(String value) String { return String.valueOf(value); }
            export native func empty() String { return String(); }
            export native func emptyNew() String { return new String(); }
            export native func code(int32 value) String { return String.fromCharCode(value); }
            export native func compare(String left, String right) int32 { return String.compare(left, right); }
            export native func chain(String value) int32 { return new String(value).trim().length; }
            export func weak(value) { return [String(value), new String(value), String.valueOf(value)]; }
            export func weakCode(value) { return String.fromCharCode(value); }
            export func weakCompare(left, right) { return String.compare(left, right); }
            export func alias(type, value) { return [type(value), new type(value), type.valueOf(value)]; }
            export func dynamicStatics(type, value) { return [type.fromCharCode(value), type.compare(value, 'a')]; }
            export func shapes() {
                var ctor = String;
                return [String(), new String(), String.valueOf(), String.fromCharCode(), String.compare(),
                    String(...[42]), new String(...[true]), String.valueOf(...[null]),
                    String.fromCharCode(65, 66), ctor('a'), new ctor('b')];
            }
            export func order() {
                var log = '';
                func value() { log += 'v'; return 'a'; }
                func extra() { log += 'e'; return 0; }
                return [String(value(), extra()), new String(value(), extra()),
                    String.valueOf(value(), extra()), String.compare(value(), value(), extra()), log];
            }
            export func shadow(String) { return String('a'); }
            export func shadowNew(String) { return new String('a'); }
            export func identity(value) { var f = value.toString; return [value.toString(), value.toString(42), value.toString(...[]), f()]; }
            """, mode);
        using var scope = domain;
        var type = ScriptDatum.FromObject(StringValue.Type);
        ScriptAssert.Equal("", TestWorkspace.Execute(domain, "empty"));
        ScriptAssert.Equal("", TestWorkspace.Execute(domain, "emptyNew"));
        Assert.Same(StringValue.Type, engine.Global.GetPropertyDatum(null, "String").Object);
        foreach (var value in new[] { "", "abc", " 中文😀 ", "\0x" })
        {
            var text = ScriptDatum.FromString(value);
            foreach (var name in new[] { "create", "construct", "valueOf" })
                ScriptAssert.Equal(value, TestWorkspace.Execute(domain, name, arguments: [text]));
            ScriptAssert.Equal(value.Trim().Length, TestWorkspace.Execute(domain, "chain", arguments: [text]));
            ScriptAssert.Equal(new object[] { value, value, value, value }, TestWorkspace.Execute(domain, "identity", arguments: [text]));
        }
        (ScriptDatum Value, string Text)[] cases = [
            (ScriptDatum.Null, "null"), (ScriptDatum.FromBoolean(true), "true"), (ScriptDatum.FromNumber(1.25), "1.25"),
            (ScriptDatum.FromInt64(9007199254740993), "9007199254740993"),
            (ScriptDatum.FromUInt64(ulong.MaxValue), "18446744073709551615"),
            (ScriptDatum.FromObject(new ScriptObject()), ""), (ScriptDatum.FromString("2"), "2")];
        foreach (var (value, expected) in cases)
        {
            ScriptAssert.Equal(new object[] { expected, expected, expected }, TestWorkspace.Execute(domain, "weak", arguments: [value]));
            ScriptAssert.Equal(new object[] { expected, expected, expected }, TestWorkspace.Execute(domain, "alias", arguments: [type, value]));
            var dynamicResults = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "dynamicStatics", arguments: [type, value]).Object);
            var code = TestWorkspace.Execute(domain, "weakCode", arguments: [value]);
            Assert.Equal(dynamicResults.GetElement(0).StringText, code.StringText);
            Assert.Equal(dynamicResults.GetElement(1).Number, TestWorkspace.Execute(domain, "weakCompare", arguments: [value, ScriptDatum.FromString("a")]).Number);
        }
        foreach (var value in new[] { int.MinValue, -1, 0, 65, 0xd83d, 0xffff, 0x10001, int.MaxValue })
            ScriptAssert.Equal(((char)value).ToString(), TestWorkspace.Execute(domain, "code", arguments: [ScriptDatum.FromNumber(value)]));
        ScriptAssert.Equal("A", TestWorkspace.Execute(domain, "weakCode", arguments: [ScriptDatum.FromNumber(65.9)]));
        ScriptAssert.Equal("A", TestWorkspace.Execute(domain, "weakCode", arguments: [ScriptDatum.FromInt64(0x100000041)]));
        foreach (var left in new[] { "", "abc", "axy", "z", "😀" })
            foreach (var right in new[] { "", "a", "z", "😀" })
                ScriptAssert.Equal(left.Length == 0 || right.Length == 0 ? 1 : left[0].CompareTo(right[0]),
                    TestWorkspace.Execute(domain, "compare", arguments: [ScriptDatum.FromString(left), ScriptDatum.FromString(right)]));
        ScriptAssert.Equal(new object[] { "", "", "", "", 1, "42", "true", "null", "A", "a", "b" }, TestWorkspace.Execute(domain, "shapes"));
        ScriptAssert.Equal(new object[] { "a", "a", "a", 0, "vevevevve" }, TestWorkspace.Execute(domain, "order"));
        var fake = ScriptDatum.FromObject(new CustomType());
        ScriptAssert.Equal("custom", TestWorkspace.Execute(domain, "shadow", arguments: [fake]));
        ScriptAssert.Equal("custom", TestWorkspace.Execute(domain, "shadowNew", arguments: [fake]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var name in new[] { "create", "construct", "valueOf", "empty", "emptyNew", "code", "compare", "chain" })
            {
                var calls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == name + "$native"));
                Assert.Contains(calls, call => call.DeclaringType == typeof(StringValue));
                Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(CallOps) || call.DeclaringType == typeof(ScriptDatum));
            }
        }
#endif
    }

    private sealed class CustomType() : ScriptType("Custom", true)
    {
        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)
            => result = ScriptDatum.FromString("custom");
    }
}
