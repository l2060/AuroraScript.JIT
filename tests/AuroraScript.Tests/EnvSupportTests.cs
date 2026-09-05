using AuroraScript.Runtime;
using AuroraScript.Runtime.Builtin;
using AuroraScript.Runtime.Types;
using AuroraScript.Compiler.Backend;
using AuroraScript.Hosting;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class EnvSupportTests
{
    [Fact]
    public void ClockCatalogPreservesNativeLongSignature()
    {
        var catalog = new HostExportCatalog([]);
        Assert.True(catalog.TryGetGlobal("Env", "ticks", out var export));
        Assert.Equal(typeof(long), export.Method.ReturnType);
        Assert.Equal(AuroraExportValueKind.Int64, export.ReturnKind);
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ClockExportsPreserveInt64TicksAndMillisecondUnits(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export native func ticks() int64 { return Env.ticks(); }
            export func read() {
                var clock = Env.ticks;
                return [clock(), Env.elapsedMs(), clock()];
            }
            """, mode);
        using var domainScope = domain;

        var before = TestWorkspace.Execute(domain, "ticks");
        var readings = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "read").Object);
        var after = TestWorkspace.Execute(domain, "ticks");
        var first = readings.GetElement(0);
        var milliseconds = readings.GetElement(1);
        var last = readings.GetElement(2);

        Assert.Equal(ValueKind.Int64, before.Kind);
        Assert.Equal(ValueKind.Int64, after.Kind);
        Assert.Equal(ValueKind.Int64, first.Kind);
        Assert.Equal(ValueKind.Int64, last.Kind);
        Assert.Equal(ValueKind.Number, milliseconds.Kind);
        Assert.True(before.Int64 >= 0);
        Assert.True(first.Int64 >= before.Int64);
        Assert.True(last.Int64 >= first.Int64);
        Assert.True(after.Int64 >= last.Int64);
        Assert.InRange(milliseconds.Number, first.Int64 / 10000.0, last.Int64 / 10000.0);
    }

    [Fact]
    public void TicksReturnsNativeLongWithoutPerCallAllocation()
    {
        for (var i = 0; i < 10000; i++)
        {
            EnvSupport.Ticks();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        long result = 0;
        for (var i = 0; i < 10000; i++)
        {
            result = EnvSupport.Ticks();
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(result >= 0);
        Assert.Equal(0, allocated);
    }
}
