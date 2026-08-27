using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class NativeFunctionTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ExportedNativeFunctionUsesDatumShell(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export native func add(Number a, Number b) Number {
                return a + b;
            }
            export func localCall(Number value) Number {
                return add(value, 2);
            }
            export native func greet(String name) String {
                return "hello " + name;
            }
            """,
            mode);

        ScriptAssert.Equal(
            7,
            TestWorkspace.Execute(
                domain,
                "add",
                arguments: [ScriptDatum.FromNumber(3), ScriptDatum.FromNumber(4)]));
        ScriptAssert.Equal(
            7,
            TestWorkspace.Execute(
                domain,
                "localCall",
                arguments: [ScriptDatum.FromNumber(5)]));
        ScriptAssert.Equal(
            "hello Aurora",
            TestWorkspace.Execute(
                domain,
                "greet",
                arguments: [ScriptDatum.FromString("Aurora")]));
        if (mode == CompilationMode.Persistence)
        {
            using var persisted = engine.CreateDomain();
            ScriptAssert.Equal(
                7,
                TestWorkspace.Execute(
                    persisted,
                    "add",
                    arguments: [ScriptDatum.FromNumber(3), ScriptDatum.FromNumber(4)]));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NativeBodyCanCrossDatumBoundaries(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export native func scale(Number value, Object options) Number {
                return value * options.factor;
            }
            """,
            mode);
        var options = new ScriptObject();
        options.Define("factor", ScriptDatum.FromNumber(3));

        ScriptAssert.Equal(
            12,
            TestWorkspace.Execute(
                domain,
                "scale",
                arguments:
                [
                    ScriptDatum.FromNumber(4),
                    ScriptDatum.FromObject(options)
                ]));
    }

    [Fact]
    public async Task EscapedPrivateNativeFunctionKeepsDatumEntry()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            native func increment(Number value) Number {
                return value + 1;
            }
            export func getIncrement() {
                return increment;
            }
            export func invokeEscaped(Number value) Number {
                var callback = getIncrement();
                return callback(value);
            }
            """);

        ScriptAssert.Equal(
            42,
            TestWorkspace.Execute(
                domain,
                "invokeEscaped",
                arguments: [ScriptDatum.FromNumber(41)]));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ImportedNativeFunctionUsesProvenNativeEntry(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "math.as",
            """
            export native func add(Number left, Number right) Number {
                return left + right;
            }
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import math from './math';
            export func run() Number {
                return math.add(20, 22);
            }
            """);

        var assemblyPath = mode == CompilationMode.Persistence
            ? Path.Combine(workspace.Root, "test-output.dll")
            : null;
        var engine = workspace.CreateEngine(
            mode,
            assemblyOut: assemblyPath);
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));

        if (mode != CompilationMode.Persistence)
        {
            return;
        }
        using var stream = File.OpenRead(assemblyPath!);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        MethodDefinitionHandle native = default;
        MethodDefinitionHandle caller = default;
        foreach (var handle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(handle);
            var name = reader.GetString(method.Name);
            if (name == "add$native") native = handle;
            else if (name == "run$typed") caller = handle;
        }
        Assert.False(native.IsNil);
        Assert.False(caller.IsNil);
        var callerMethod = reader.GetMethodDefinition(caller);
        var il = peReader.GetMethodBody(
            callerMethod.RelativeVirtualAddress).GetILBytes();
        Assert.True(ContainsCall(
            il,
            MetadataTokens.GetToken(native)));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistenceOnlyWrapsCallsThatRentArgumentBuffers()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            native func sum(Number a, Number b, Number c, Number d, Number e, Number f, Number g, Number h) Number {
                return a + b + c + d + e + f + g + h;
            }
            func first(a, b, c, d, e, f, g, h) {
                return a;
            }
            export func runNative() Number {
                return sum(1, 2, 3, 4, 5, 6, 7, 8);
            }
            export func runDynamic() {
                var callback = first;
                return callback(1, 2, 3, 4, 5, 6, 7, 8);
            }
            """);
        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        var engine = workspace.CreateEngine(
            CompilationMode.Persistence,
            assemblyOut: assemblyPath);
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(36, TestWorkspace.Execute(domain, "runNative"));
        ScriptAssert.Equal(1, TestWorkspace.Execute(domain, "runDynamic"));

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        MethodDefinitionHandle native = default;
        MethodDefinitionHandle nativeCaller = default;
        MethodDefinitionHandle dynamicCaller = default;
        foreach (var handle in reader.MethodDefinitions)
        {
            var name = reader.GetString(reader.GetMethodDefinition(handle).Name);
            if (name == "sum$native") native = handle;
            else if (name == "runNative$typed") nativeCaller = handle;
            else if (name == "runDynamic$typed") dynamicCaller = handle;
        }

        Assert.False(native.IsNil);
        Assert.False(nativeCaller.IsNil);
        Assert.False(dynamicCaller.IsNil);
        var nativeBody = peReader.GetMethodBody(
            reader.GetMethodDefinition(nativeCaller).RelativeVirtualAddress);
        var dynamicBody = peReader.GetMethodBody(
            reader.GetMethodDefinition(dynamicCaller).RelativeVirtualAddress);
        Assert.Empty(nativeBody.ExceptionRegions);
        Assert.True(ContainsCall(
            nativeBody.GetILBytes(),
            MetadataTokens.GetToken(native)));
        Assert.NotEmpty(dynamicBody.ExceptionRegions);
    }
#endif

    [Fact]
    public async Task NativeDirectCallsPreserveScriptStackFrames()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            native func fail() Number {
                throw new Error("native failure");
            }
            export native func run() Number {
                return fail();
            }
            """);

        var error = Assert.Throws<AuroraRuntimeException>(
            () => TestWorkspace.Execute(domain, "run"));
        Assert.Contains(
            error.StackTrace,
            frame => frame.Method.Contains("fail", StringComparison.Ordinal));
        Assert.Contains(
            error.StackTrace,
            frame => frame.Method.Contains("run", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NativeFunctionAssignmentIsRejected()
    {
        using var workspace = new TestWorkspace();
        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() =>
            workspace.CompileModuleAsync(
                """
                @module(TEST);
                native func value() Number { return 1; }
                value = null;
                """));

        Assert.Contains(
            "Native function 'value' cannot be assigned",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task NativeFunctionsRequireStableDeclarationShape()
    {
        using var workspace = new TestWorkspace();
        var missingReturn = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => workspace.CompileModuleAsync(
                "native func value(Number input) { return input; }"));
        Assert.Contains(
            "requires a declared return type",
            missingReturn.Message,
            StringComparison.Ordinal);

        var defaultParameter = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => workspace.CompileModuleAsync(
                "native func value(Number input = 1) Number { return input; }"));
        Assert.Contains(
            "cannot declare default parameters",
            defaultParameter.Message,
            StringComparison.Ordinal);
    }

    private static bool ContainsCall(
        ReadOnlySpan<byte> il,
        int metadataToken)
    {
        for (var i = 0; i + 5 <= il.Length; i++)
        {
            if (il[i] == 0x28 &&
                BinaryPrimitives.ReadInt32LittleEndian(
                    il.Slice(i + 1, 4)) == metadataToken)
            {
                return true;
            }
        }
        return false;
    }
}
