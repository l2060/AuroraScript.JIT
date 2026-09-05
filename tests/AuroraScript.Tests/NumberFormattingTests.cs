using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using System.Threading.Tasks;

namespace AuroraScript.Tests;

public sealed class NumberFormattingTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NativeFormattingPreservesReceiverWidthAndIntegerRadix(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func number(Number value, int32 radix) String { return value.toString(radix); }
            export native func signed(int32 value, int32 radix) String { return value.toString(radix); }
            export native func unsigned(uint32 value, int32 radix) String { return value.toString(radix); }
            export native func longValue(int64 value, int32 radix) String { return value.toString(radix); }
            export native func ulongValue(uint64 value, int32 radix) String { return value.toString(radix); }
            export native func longDefault(int64 value) String { return value.toString(); }
            export native func ulongDefault(uint64 value) String { return value.toString(); }
            export func dynamicFormat(value, radix) { return value.toString(radix); }
            export func dynamicDefault(value) { return value.toString(); }
            export native func word(uint32 value) String {
                var octet = 0;
                octet = value & 255;
                return octet.toString(16);
            }
            """, mode);
        using var scope = domain;
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            // Different negative signs verify Number's current-culture behavior
            // versus the exact integer types' invariant decimal representation.
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NegativeSign = "~";
            CultureInfo.CurrentCulture = culture;
            foreach (var radix in new[] { 2, 10, 16, -1, int.MaxValue })
            {
                foreach (var value in new[] { double.NaN, double.PositiveInfinity, -0.0, -1.25, 1e30 })
                    Same(domain, "number", ScriptDatum.FromNumber(value), radix);
                foreach (var value in new[] { int.MinValue, -1, 0, 255, int.MaxValue })
                    Same(domain, "signed", ScriptDatum.FromNumber(value), radix);
                foreach (var value in new[] { 0u, 255u, 2147483648u, uint.MaxValue })
                    Same(domain, "unsigned", ScriptDatum.FromNumber(value), radix);
                foreach (var value in new[] { long.MinValue, -1, 0, 9007199254740993L, long.MaxValue })
                {
                    var datum = ScriptDatum.FromInt64(value);
                    Same(domain, "longValue", datum, radix);
                    ScriptAssert.Equal(value.ToString(radix == 16 ? "X" : "D", CultureInfo.InvariantCulture),
                        TestWorkspace.Execute(domain, "longValue", arguments: [datum, ScriptDatum.FromNumber(radix)]));
                    ScriptAssert.Equal(value.ToString(CultureInfo.InvariantCulture),
                        TestWorkspace.Execute(domain, "longDefault", arguments: [datum]));
                }
                foreach (var value in new[] { 0UL, 9007199254740993UL, 0x8000000000000000UL, ulong.MaxValue })
                {
                    var datum = ScriptDatum.FromUInt64(value);
                    Same(domain, "ulongValue", datum, radix);
                    ScriptAssert.Equal(value.ToString(radix == 16 ? "X" : "D", CultureInfo.InvariantCulture),
                        TestWorkspace.Execute(domain, "ulongValue", arguments: [datum, ScriptDatum.FromNumber(radix)]));
                    ScriptAssert.Equal(value.ToString(CultureInfo.InvariantCulture),
                        TestWorkspace.Execute(domain, "ulongDefault", arguments: [datum]));
                }
            }
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
        ScriptAssert.Equal("FF", TestWorkspace.Execute(domain, "word", arguments: [ScriptDatum.FromNumber(uint.MaxValue)]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var assembly = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")));
            var methods = assembly.GetTypes().SelectMany(type => type.GetMethods()).Where(m => m.Name.EndsWith("$native", StringComparison.Ordinal));
            foreach (var method in methods)
            {
                var calls = StringOptimizationTests.GetCalls(method);
                var format = Assert.Single(calls, call => call.Name == "FormatString");
                var receiver = method.Name switch
                {
                    "number$native" => typeof(double),
                    "unsigned$native" => typeof(uint),
                    "longValue$native" or "longDefault$native" => typeof(long),
                    "ulongValue$native" or "ulongDefault$native" => typeof(ulong),
                    _ => typeof(int)
                };
                Assert.Equal(receiver, format.GetParameters()[0].ParameterType);
                Assert.All(format.GetParameters().Skip(1), p => Assert.Equal(typeof(int), p.ParameterType));
                Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(CallOps) || call.DeclaringType == typeof(ScriptDatum));
            }
        }
#endif
    }

    [Fact]
    public void PublicCoresNeverLoseExact64Bits()
    {
        Assert.Equal("9223372036854775807", NumberValue.FormatString(long.MaxValue));
        Assert.Equal("18446744073709551615", NumberValue.FormatString(ulong.MaxValue));
        Assert.Equal("8000000000000000", NumberValue.FormatString(long.MinValue, 16));
        Assert.Equal("FFFFFFFFFFFFFFFF", NumberValue.FormatString(ulong.MaxValue, 16));
        Assert.All(typeof(NumberValue).GetMethods().Where(m => m.Name == "FormatString" && m.GetParameters().Length == 2),
            method => Assert.Equal(typeof(int), method.GetParameters()[1].ParameterType));
    }

    private static void Same(ScriptDomain domain, string method, ScriptDatum value, int radix)
    {
        var args = new[] { value, ScriptDatum.FromNumber(radix) };
        var expected = TestWorkspace.Execute(domain, "dynamicFormat", arguments: args);
        ScriptAssert.Equal(ScriptDatum.ToString(expected), TestWorkspace.Execute(domain, method, arguments: args));
    }
}
