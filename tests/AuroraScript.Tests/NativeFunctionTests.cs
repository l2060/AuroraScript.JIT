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
    public async Task NativeFunctionsSupportConstantTrailingDefaults(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            const DEFAULT_VALUE = 20 + 1;
            export native func scale(
                Number value = DEFAULT_VALUE,
                Number factor = 2
            ) Number {
                return value * factor;
            }
            export func localCall() Number {
                return scale();
            }
            export native func choose(
                Boolean enabled = true,
                String value = "ok"
            ) String {
                if (enabled) return value;
                return "no";
            }
            """,
            mode);

        ScriptAssert.Equal(
            42,
            TestWorkspace.Execute(domain, "scale"));
        ScriptAssert.Equal(
            6,
            TestWorkspace.Execute(
                domain,
                "scale",
                arguments: [ScriptDatum.FromNumber(3)]));
        ScriptAssert.Equal(
            42,
            TestWorkspace.Execute(domain, "localCall"));
        ScriptAssert.Equal(
            "ok",
            TestWorkspace.Execute(domain, "choose"));
        ScriptAssert.Equal(
            "no",
            TestWorkspace.Execute(
                domain,
                "choose",
                arguments: [ScriptDatum.False]));
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
            export native func add(
                Number left,
                Number right = 22
            ) Number {
                return left + right;
            }
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import math from './math';
            export func run() Number {
                return math.add(20);
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

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NativeVoidFunctionsUseVoidAbiAndExposeNullToScript(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            var state = 0;

            export native func setState(Number value) void {
                state = value;
            }

            native func relay(Number value) void {
                setState(value);
                return;
            }

            native func returnFromFinally() void {
                try {
                    return;
                } finally {
                    state += 1;
                }
            }

            export func run() {
                var result = relay(41);
                returnFromFinally();
                return [
                    state,
                    result == null,
                    setState(5) == null,
                    state
                ];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[] { 42, true, true, 5 },
            TestWorkspace.Execute(domain, "run"));
        ScriptAssert.Equal(
            null,
            TestWorkspace.Execute(
                domain,
                "setState",
                arguments: [ScriptDatum.FromNumber(9)]));

#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
            using var stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);
            var reader = peReader.GetMetadataReader();
            MethodDefinitionHandle relay = default;
            MethodDefinitionHandle setState = default;
            foreach (var handle in reader.MethodDefinitions)
            {
                var method = reader.GetMethodDefinition(handle);
                var name = reader.GetString(method.Name);
                if (name == "relay$native") relay = handle;
                else if (name == "setState$native") setState = handle;
            }

            Assert.False(relay.IsNil);
            Assert.False(setState.IsNil);
            var relayMethod = reader.GetMethodDefinition(relay);
            var signature = reader.GetBlobBytes(relayMethod.Signature);
            Assert.Equal(0x01, signature[2]); // ELEMENT_TYPE_VOID
            var il = peReader.GetMethodBody(
                relayMethod.RelativeVirtualAddress).GetILBytes()!;
            Assert.True(
                ContainsCall(il, MetadataTokens.GetToken(setState)));
            // Metadata token bytes can also contain 0x7e; inspect instruction
            // boundaries rather than treating operands as opcodes.
            Assert.DoesNotContain(System.Reflection.Emit.OpCodes.Ldsfld,
                IntegerSpecializationTests.ReadOpCodes(il.AsSpan()));
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ImportedNativeVoidFunctionMaterializesNullOnlyAsValue(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "worker.as",
            """
            export native func notify() void {
            }
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import worker from './worker';

            export func run() {
                worker.notify();
                return worker.notify() == null;
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

        ScriptAssert.Equal(
            true,
            TestWorkspace.Execute(domain, "run"));
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

        var returnedValue = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => workspace.CompileModuleAsync(
                "native func value() void { return 1; }"));
        Assert.Contains(
            "cannot return a value",
            returnedValue.Message,
            StringComparison.Ordinal);

        var ordinaryVoid = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => workspace.CompileModuleAsync(
                "func value() void { return; }"));
        Assert.Contains(
            "Unknown type 'void'",
            ordinaryVoid.Message,
            StringComparison.Ordinal);

        var voidParameter = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => workspace.CompileModuleAsync(
                "native func value(void input) Number { return 1; }"));
        Assert.Contains(
            "Unknown type 'void'",
            voidParameter.Message,
            StringComparison.Ordinal);

        var voidType = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => workspace.CompileModuleAsync(
                "type void { Number value; }"));
        Assert.Contains(
            "Type declaration 'void' conflicts with a built-in type",
            voidType.Message,
            StringComparison.Ordinal);

        var nonConstantDefault = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => workspace.CompileModuleAsync(
                "native func value(Number input = Math.random()) Number { return input; }"));
        Assert.Contains(
            "must be a compile-time primitive constant",
            nonConstantDefault.Message,
            StringComparison.Ordinal);

        var mismatchedDefault = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => workspace.CompileModuleAsync(
                "native func value(Number input = \"1\") Number { return input; }"));
        Assert.Contains(
            "default type 'String' does not match declared type 'Number'",
            mismatchedDefault.Message,
            StringComparison.Ordinal);

        var nonTrailingDefault = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => workspace.CompileModuleAsync(
                "native func value(Number first = 1, Number second) Number { return first + second; }"));
        Assert.Contains(
            "default parameters must be trailing",
            nonTrailingDefault.Message,
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
