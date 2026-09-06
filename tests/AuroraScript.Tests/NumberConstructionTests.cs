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

public sealed class NumberConstructionTests
{
    [Fact]
    public void NumberFactoryStaticsAndConstantsUseGeneratedMetadata()
    {
        var assembly = typeof(NumberValue).Assembly;
        var metadata = Assert.Single(
            assembly.GetCustomAttributes<AuroraGeneratedNativeObjectAttribute>(),
            item => item.ObjectType == typeof(NumberValue));
        Assert.Equal("valueOf", metadata.FactoryMemberName);
        Assert.Equal(typeof(double), metadata.ReceiverType);
        Assert.False(metadata.Constructible);

        var members = assembly.GetCustomAttributes<AuroraGeneratedExportAttribute>()
            .Where(item => item.DeclaringType == typeof(NumberValue))
            .Select(item => item.MemberName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[] { "isInfinity", "isInteger", "isNaN", "parseFloat", "parseInt", "valueOf" },
            members);

        var constants = assembly.GetCustomAttributes<AuroraGeneratedConstantAttribute>()
            .Where(item => item.DeclaringType == typeof(NumberValue))
            .Select(item => item.MemberName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "MAX_SAFE_INTEGER", "MAX_VALUE", "MIN_SAFE_INTEGER", "MIN_VALUE",
                "NEGATIVE_INFINITY", "NaN", "POSITIVE_INFINITY"
            },
            constants);

        Assert.Equal("Number", NumberValue.Type.Name);
        Assert.True(NumberValue.Type.IsFrozen);
        Assert.Null(assembly.GetType("AuroraScript.Runtime.Types.TypeConstruct.NumberConstructor"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task FactoryAndStaticsPreserveNumberSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export native func create(Number value) Number { return Number(value); }
            export native func construct(Number value) Number { return new Number(value); }
            export native func valueOf(Number value) Number { return Number.valueOf(value); }
            export native func parseFloat(String value) Number { return Number.parseFloat(value); }
            export native func parseInt(String value) Number { return Number.parseInt(value); }
            export native func integer(Number value) Boolean { return Number.isInteger(value); }
            export func weak(value) { return [Number(value), new Number(value), Number.valueOf(value)]; }
            export func alias(type, value) { return [type(value), new type(value), type.valueOf(value)]; }
            export func invalid(value) {
                var first = Number(value);
                var second = new Number(value);
                var third = Number.valueOf(value);
                return [first != first, second != second, third != third];
            }
            export func empty() {
                var first = Number();
                var second = new Number();
                var third = Number.valueOf();
                return [first != first, second != second, third != third];
            }
            export func constants() {
                return [Number.MAX_VALUE, Number.MIN_VALUE, Number.MAX_SAFE_INTEGER,
                    Number.MIN_SAFE_INTEGER, Number.NaN != Number.NaN,
                    Number.POSITIVE_INFINITY, Number.NEGATIVE_INFINITY];
            }
            """,
            mode);

        Assert.Same(NumberValue.Type, engine.Global.GetPropertyDatum(null, "Number").Object);
        var type = ScriptDatum.FromObject(NumberValue.Type);
        foreach (var value in new[] { -1d, 0d, 1.25d, double.PositiveInfinity })
        {
            var datum = ScriptDatum.FromNumber(value);
            foreach (var name in new[] { "create", "construct", "valueOf" })
            {
                ScriptAssert.Equal(value, TestWorkspace.Execute(domain, name, arguments: [datum]));
            }
            ScriptAssert.Equal(
                new object[] { value, value, value },
                TestWorkspace.Execute(domain, "weak", arguments: [datum]));
            ScriptAssert.Equal(
                new object[] { value, value, value },
                TestWorkspace.Execute(domain, "alias", arguments: [type, datum]));
        }
        ScriptAssert.Equal(
            new object[] { 1D, 1D, 1D },
            TestWorkspace.Execute(domain, "weak", arguments: [ScriptDatum.True]));
        ScriptAssert.Equal(
            new object[] { 12.5D, 12.5D, 12.5D },
            TestWorkspace.Execute(domain, "weak", arguments: [ScriptDatum.FromString("12.5")]));
        ScriptAssert.Equal(
            new object[] { true, true, true },
            TestWorkspace.Execute(domain, "invalid", arguments: [ScriptDatum.Null]));

        ScriptAssert.Equal(12.5d, TestWorkspace.Execute(
            domain, "parseFloat", arguments: [ScriptDatum.FromString("12.5")]));
        ScriptAssert.Equal(12d, TestWorkspace.Execute(
            domain, "parseInt", arguments: [ScriptDatum.FromString("12.9")]));
        ScriptAssert.Equal(true, TestWorkspace.Execute(
            domain, "integer", arguments: [ScriptDatum.FromNumber(12)]));
        ScriptAssert.Equal(new object[] { true, true, true }, TestWorkspace.Execute(domain, "empty"));
        ScriptAssert.Equal(
            new object[]
            {
                double.MaxValue, double.MinValue, 9_007_199_254_740_991d,
                -9_007_199_254_740_991d, true, double.PositiveInfinity,
                double.NegativeInfinity
            },
            TestWorkspace.Execute(domain, "constants"));

#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(typeInfo => typeInfo.GetMethods()).ToArray();
            foreach (var name in new[] { "create", "construct", "valueOf", "parseFloat", "parseInt", "integer" })
            {
                var calls = StringOptimizationTests.GetCalls(
                    methods.Single(method => method.Name == name + "$native"));
                Assert.Contains(calls, call => call.DeclaringType == typeof(NumberValue));
                Assert.DoesNotContain(calls, call =>
                    call.DeclaringType == typeof(CallOps) || call.DeclaringType == typeof(ScriptDatum));
            }
        }
#endif
    }
}
