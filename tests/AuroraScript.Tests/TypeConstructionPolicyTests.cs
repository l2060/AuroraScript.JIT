using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Host;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class TypeConstructionPolicyTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task OnlyPrimitiveConversionTypesCanBeCalled(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var source = new StringBuilder("""
            @module(TEST);
            export func directDate() { return Date('2024-01-01'); }
            export func directArray() { return Array(2); }
            export func directObject() { return Object(); }
            export func directNative() { return Vec2(3, 4); }
            export func shadow(Number) { return Number(1); }
            export func spread(type) { return type(...[1,2,3,4,5,6,7,8,9]); }
            export func effects(type) {
                var n=0;
                func next() { n++; return 1; }
                try { type(next(), next(), next()); } catch(e) { return n; }
                return -1;
            }
            export func construct(type) { return new type(); }
            export func valid() {
                var vec = new Vec2(3,4);
                return [typeof new Date('2024-01-01'), (new Array(2)).length,
                    typeof new Object(), vec.length(), Date.parse('2024-01-01').year,
                    Number('12'), Boolean(0), String(12), Number.valueOf('12'),
                    Boolean.valueOf(1), String.valueOf(12)];
            }
            """);
        for (var count = 0; count <= 9; count++)
        {
            source.Append("\nexport func call").Append(count).Append("(type) { var alias=type; return alias(");
            for (var i = 0; i < count; i++)
            {
                if (i != 0) source.Append(',');
                source.Append('1');
            }
            source.Append("); }");
        }
        var (engine, domain) = await workspace.CompileModuleAsync(source.ToString(), mode, nativeTypes: true);
        foreach (var method in new[] { "directDate", "directArray", "directObject", "directNative" })
            Assert.Contains("new", Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, method)).Message);
        foreach (var name in new[] { "Date", "Array", "Object", "Vec2", "StringBuffer", "HashMap", "Regex", "Error", "Path", "Proxy", "Int8Array" })
        {
            var type = engine.Global.GetPropertyDatum(null!, name);
            for (var count = 0; count <= 9; count++)
                Assert.Contains("new", Assert.Throws<AuroraRuntimeException>(() =>
                    TestWorkspace.Execute(domain, "call" + count, arguments: [type])).Message);
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "spread", arguments: [type]));
            ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "effects", arguments: [type]));
        }
        ScriptAssert.Equal(new object[] { "date", 2, "object", 5, 2024, 12, false, "12", 12, true, "12" },
            TestWorkspace.Execute(domain, "valid"));
        foreach (var name in new[] { "Number", "Boolean", "String" })
        {
            var type = engine.Global.GetPropertyDatum(null!, name);
            var expected = name == "Number" ? (object)1d : name == "Boolean" ? true : "1";
            ScriptAssert.Equal(expected, TestWorkspace.Execute(domain, "call1", arguments: [type]));
            ScriptAssert.Equal(expected, TestWorkspace.Execute(domain, "spread", arguments: [type]));
        }
        var spoof = ScriptDatum.FromObject(new SpoofedNumber());
        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "shadow", arguments: [spoof]));
        ScriptAssert.Equal("custom", TestWorkspace.Execute(domain, "construct", arguments: [spoof]));
    }

    private sealed class SpoofedNumber() : ScriptType("Number", true)
    {
        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result) =>
            result = ScriptDatum.FromString("custom");
    }
}
