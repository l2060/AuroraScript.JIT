using AuroraScript.Core;
using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class IntegerHostExportTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NativeIntegerReturnsStayExactThroughDirectAndAdapterCalls(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("main.as", """
            @module(TEST);
            export native func signed() int64 { return IntegerExports.signed(false); }
            export native func unsigned() uint64 { return IntegerExports.unsigned(); }
            export func direct() {
                var host = new IntegerExports();
                return [IntegerExports.signed(true), signed(), unsigned(), host.signedOdd(), host.unsignedOdd()];
            }
            export func adapters() {
                var signedCall = IntegerExports.signed;
                var unsignedCall = IntegerExports.unsigned;
                var host = new IntegerExports();
                var instanceSigned = host.signedOdd;
                var instanceUnsigned = host.unsignedOdd;
                return [signedCall(true), signedCall(false), unsignedCall(), instanceSigned(), instanceUnsigned()];
            }
            """);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = ScriptSources.FileSystem(workspace.Root))
            .WithCompiler(compiler => compiler.Mode = mode)
            .WithCompiler(compiler => compiler.WithNativeTypes(typeof(IntegerExportHost)))
            .WithOutput(output => output.AssemblyFile = Path.Combine(workspace.Root, "integers.dll"));
        var engine = new AuroraEngine(options);
        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();

        foreach (var method in new[] { "direct", "adapters" })
        {
            var values = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, method).Object);
            AssertSigned(long.MinValue, values.GetElement(0));
            AssertSigned(long.MaxValue, values.GetElement(1));
            AssertUnsigned(ulong.MaxValue, values.GetElement(2));
            AssertSigned(9007199254740993L, values.GetElement(3));
            AssertUnsigned(9223372036854775809UL, values.GetElement(4));
        }
    }

    private static void AssertSigned(long expected, ScriptDatum actual)
    {
        Assert.Equal(ValueKind.Int64, actual.Kind);
        Assert.Equal(expected, actual.Int64);
    }

    private static void AssertUnsigned(ulong expected, ScriptDatum actual)
    {
        Assert.Equal(ValueKind.UInt64, actual.Kind);
        Assert.Equal(expected, actual.UInt64);
    }
}

[AuroraNativeType("IntegerExports")]
public sealed partial class IntegerExportHost : ScriptObject
{
    [AuroraExport]
    public IntegerExportHost() { }

    [AuroraExport("signed")]
    public static long Signed(bool minimum) => minimum ? long.MinValue : long.MaxValue;

    [AuroraExport("unsigned")]
    public static ulong Unsigned() => ulong.MaxValue;

    [AuroraExport("signedOdd")]
    public long SignedOdd() => 9007199254740993L;

    [AuroraExport("unsignedOdd")]
    public ulong UnsignedOdd() => 9223372036854775809UL;
}
