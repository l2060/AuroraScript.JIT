using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class DirectCallSpecializationTests
{
    private const string BitwiseSource = """
        @module(TEST);

        @directCall
        func mask(value) {
            return value | 0;
        }

        @directCall
        func forwardMask(value) {
            return mask(value);
        }

        @directCall
        func shift(value) {
            return value << 0;
        }

        @directCall
        func mixed(value) {
            return (value | 0) + (value << 0);
        }

        export func run() {
            return [
                forwardMask(4294967295),
                shift(4294967295),
                mixed(4294967295)
            ];
        }

        export func fallback(value, operation) {
            if (operation == 1) return mask(value);
            return shift(value);
        }
        """;

    private const string DatumReturnSource = """
        @module(TEST);

        @directCall
        func maybe(value, returnValue) {
            if (returnValue) return value;
            return null;
        }

        export func run() {
            return [maybe(7, true), maybe(7, false)];
        }

        export func fallback(value, returnValue) {
            return maybe(value, returnValue);
        }
        """;

    private const string ScalarCoercionSource = """
        @module(TEST);

        @directCall
        func numeric(value) {
            return (value - 1) * 2;
        }

        @directCall
        func truth(flag) {
            if (flag) return 1;
            return 0;
        }

        @directCall
        func preserve(value) {
            if (value) return value;
            return 0;
        }

        export func relay(value, flag) {
            return [numeric(value), truth(flag), preserve(value)];
        }
        """;

    private const string LocalCoercionSource = """
        @module(TEST);

        export func run(value) {
            var numericOnly = value;
            var booleanOnly = value;
            var preserved = value;
            var numericResult = (numericOnly - 1) * 2;
            var booleanResult = 0;
            if (booleanOnly) booleanResult = 1;
            return [numericResult, booleanResult, preserved];
        }

        export func observableAssignment(value) {
            var local = value;
            return [(local = "4"), local - 1];
        }

        export func objectCase() {
            var value = {};
            var numericOnly = value;
            var booleanOnly = value;
            var numericResult = numericOnly - 1;
            var booleanResult = 0;
            if (booleanOnly) booleanResult = 1;
            return [numericResult != numericResult, booleanResult];
        }
        """;

    private const string EqualityOnlySource = """
        @module(TEST);

        export func equalityOnly(value, other) {
            return [value == null, other != null];
        }
        """;

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task BitwiseOnlyParametersUseInt32AbiWithoutChangingFallbacks(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(BitwiseSource, mode);

        AssertBitwiseResults(domain);
        if (mode == CompilationMode.Persistence)
        {
            AssertBitwiseResults(engine.CreateDomain());
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task PureDatumReturnFunctionsUseTheStaticDirectPath(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(DatumReturnSource, mode);

        AssertDatumReturnResults(domain);
        if (mode == CompilationMode.Persistence)
        {
            AssertDatumReturnResults(engine.CreateDomain());
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CoercionOnlyParametersUseNativeScalarAbiFromDynamicCallers(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(ScalarCoercionSource, mode);

        AssertScalarCoercionResults(domain);
        if (mode == CompilationMode.Persistence)
        {
            AssertScalarCoercionResults(engine.CreateDomain());
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task DemandClosedLocalsUseNativeStorageWithoutChangingValues(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(LocalCoercionSource, mode);

        AssertLocalCoercionResults(domain);
        if (mode == CompilationMode.Persistence)
        {
            AssertLocalCoercionResults(engine.CreateDomain());
        }
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistenceEmitsCoercedInt32AndDatumReturnSignatures()
    {
        using var bitwiseWorkspace = new TestWorkspace();
        await bitwiseWorkspace.CompileModuleAsync(BitwiseSource, CompilationMode.Persistence);

        using (var stream = File.OpenRead(Path.Combine(bitwiseWorkspace.Root, "test-output.dll")))
        using (var peReader = new PEReader(stream))
        {
            var reader = peReader.GetMetadataReader();
            Assert.Equal(
                [0x00, 0x01, 0x08, 0x08],
                reader.GetBlobBytes(FindMethod(reader, "mask$native").Signature));
            Assert.Equal(
                [0x00, 0x01, 0x08, 0x08],
                reader.GetBlobBytes(FindMethod(reader, "forwardMask$native").Signature));
            Assert.Equal(
                [0x00, 0x01, 0x08, 0x08],
                reader.GetBlobBytes(FindMethod(reader, "shift$native").Signature));
            Assert.Equal(
                [0x00, 0x01, 0x0d, 0x0d],
                reader.GetBlobBytes(FindMethod(reader, "mixed$native").Signature));
        }

        using var datumWorkspace = new TestWorkspace();
        await datumWorkspace.CompileModuleAsync(DatumReturnSource, CompilationMode.Persistence);
        using var datumStream = File.OpenRead(Path.Combine(datumWorkspace.Root, "test-output.dll"));
        using var datumReader = new PEReader(datumStream);
        var metadata = datumReader.GetMetadataReader();
        var signature = metadata.GetBlobBytes(FindMethod(metadata, "maybe$native").Signature);

        Assert.Equal(0x00, signature[0]);
        Assert.Equal(0x02, signature[1]);
        Assert.Equal(0x11, signature[2]);
        Assert.Equal([0x08, 0x02], signature[^2..]);
    }

    [Fact]
    public async Task PersistenceEmitsDemandCoercedNumberAndBooleanSignatures()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(ScalarCoercionSource, CompilationMode.Persistence);

        using var stream = File.OpenRead(Path.Combine(workspace.Root, "test-output.dll"));
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        Assert.Equal(
            [0x00, 0x01, 0x0d, 0x0d],
            reader.GetBlobBytes(FindMethod(reader, "numeric$native").Signature));
        Assert.Equal(
            [0x00, 0x01, 0x08, 0x02],
            reader.GetBlobBytes(FindMethod(reader, "truth$native").Signature));
        Assert.False(HasMethod(reader, "preserve$native"));
    }

    [Fact]
    public async Task PersistenceDoesNotCreateNumericCachesForEqualityOnlyParameters()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(EqualityOnlySource, CompilationMode.Persistence);

        using var stream = File.OpenRead(Path.Combine(workspace.Root, "test-output.dll"));
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var method = FindMethodStartingWith(reader, "equalityOnly$typed");
        var body = peReader.GetMethodBody(method.RelativeVirtualAddress);

        if (!body.LocalSignature.IsNil)
        {
            var localSignature = reader.GetStandaloneSignature(body.LocalSignature);
            Assert.DoesNotContain(
                (byte)0x0d,
                reader.GetBlobBytes(localSignature.Signature));
        }
    }

    private static MethodDefinition FindMethod(MetadataReader reader, string name)
    {
        foreach (var handle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(handle);
            if (string.Equals(reader.GetString(method.Name), name, StringComparison.Ordinal))
            {
                return method;
            }
        }
        throw new Xunit.Sdk.XunitException("Persisted method not found: " + name);
    }

    private static MethodDefinition FindMethodStartingWith(
        MetadataReader reader,
        string prefix)
    {
        MethodDefinition? result = null;
        foreach (var handle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(handle);
            if (!reader.GetString(method.Name).StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }
            Assert.False(result.HasValue, "Multiple persisted methods start with: " + prefix);
            result = method;
        }
        return result ?? throw new Xunit.Sdk.XunitException(
            "Persisted method not found with prefix: " + prefix);
    }

    private static bool HasMethod(MetadataReader reader, string name)
    {
        foreach (var handle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(handle);
            if (string.Equals(reader.GetString(method.Name), name, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }
#endif

    private static void AssertBitwiseResults(ScriptDomain domain)
    {
        var large = ScriptDatum.FromNumber(4294967295d);
        var expectedShift = ValueOps.LeftShift(large, ScriptDatum.FromNumber(0)).Number;
        ScriptAssert.Equal(
            new object?[] { -1, expectedShift, -1 + expectedShift },
            TestWorkspace.Execute(domain, "run"));
        ScriptAssert.Equal(
            1,
            TestWorkspace.Execute(
                domain,
                "fallback",
                arguments: [ScriptDatum.FromNumber(1.75), ScriptDatum.FromNumber(1)]));
        ScriptAssert.Equal(
            double.NaN,
            TestWorkspace.Execute(
                domain,
                "fallback",
                arguments: [ScriptDatum.FromString("4"), ScriptDatum.FromNumber(1)]));
        ScriptAssert.Equal(
            expectedShift,
            TestWorkspace.Execute(
                domain,
                "fallback",
                arguments: [ScriptDatum.FromNumber(4294967295d), ScriptDatum.FromNumber(0)]));
    }

    private static void AssertDatumReturnResults(ScriptDomain domain)
    {
        ScriptAssert.Equal(new object?[] { 7, null }, TestWorkspace.Execute(domain, "run"));
        ScriptAssert.Equal(
            "Aurora",
            TestWorkspace.Execute(
                domain,
                "fallback",
                arguments: [ScriptDatum.FromString("Aurora"), ScriptDatum.True]));
        ScriptAssert.Equal(
            null,
            TestWorkspace.Execute(
                domain,
                "fallback",
                arguments: [ScriptDatum.FromString("Aurora"), ScriptDatum.False]));
    }

    private static void AssertScalarCoercionResults(ScriptDomain domain)
    {
        ScriptAssert.Equal(
            new object?[] { 6, 1, "4" },
            TestWorkspace.Execute(
                domain,
                "relay",
                arguments: [ScriptDatum.FromString("4"), ScriptDatum.FromString("yes")]));
        ScriptAssert.Equal(
            new object?[] { -2, 0, 0 },
            TestWorkspace.Execute(
                domain,
                "relay",
                arguments: [ScriptDatum.Null, ScriptDatum.FromString("")]));
    }

    private static void AssertLocalCoercionResults(ScriptDomain domain)
    {
        ScriptAssert.Equal(
            new object?[] { 6, 1, "4" },
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.FromString("4")]));
        ScriptAssert.Equal(
            new object?[] { -2, 0, null },
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.Null]));
        ScriptAssert.Equal(
            new object?[] { 0, 1, true },
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.True]));
        ScriptAssert.Equal(
            new object?[] { "4", 3 },
            TestWorkspace.Execute(
                domain,
                "observableAssignment",
                arguments: [ScriptDatum.Null]));
        ScriptAssert.Equal(
            new object?[] { true, 1 },
            TestWorkspace.Execute(domain, "objectCase"));
    }
}
