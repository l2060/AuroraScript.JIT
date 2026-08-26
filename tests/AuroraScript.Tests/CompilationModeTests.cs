using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class CompilationModeTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task AnonymousDependenciesCompileInEveryMode(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("first/shared.as", "export const value = 20;");
        workspace.WriteSource("second/shared.as", "export const value = 22;");
        var (_, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); import first from './first/shared'; import second from './second/shared'; export func run() { return first.value + second.value; }",
            mode);

        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ReleaseCompilationModesProduceSameResult(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func run(value) { return value * 2 + 2; }",
            mode);

        ScriptAssert.Equal(42, TestWorkspace.Execute(
            domain,
            "run",
            arguments: AuroraScript.Runtime.ScriptDatum.FromNumber(20)));

        if (mode == CompilationMode.Persistence)
        {
            var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
            Assert.True(File.Exists(assemblyPath));
            Assert.True(new FileInfo(assemblyPath).Length > 0);
            ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run", arguments: AuroraScript.Runtime.ScriptDatum.FromNumber(20)));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NativeDirectCallsWorkInEveryCompilationMode(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            native func sum(Number value, Number total) Number {
                if (value <= 0) return total;
                return sum(value - 1, total + value);
            }
            export func run() { return sum(100, 0); }
            """,
            mode);

        ScriptAssert.Equal(5050, TestWorkspace.Execute(domain, "run"));
        if (mode == CompilationMode.Persistence)
        {
            ScriptAssert.Equal(5050, TestWorkspace.Execute(engine.CreateDomain(), "run"));
        }
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistedAssemblyRunsAfterLoadingTheSavedPe()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(
            """
            @module(TEST);
            native func sum(Number value, Number total) Number {
                if (value <= 0) return total;
                return sum(value - 1, total + value);
            }
            export func run() { return sum(100, 0); }
            """,
            CompilationMode.Persistence);

        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        AssertNativeNumericMethod(assemblyPath);
        var result = ExecutePersistedAssembly(assemblyPath, workspace, out var loadContextReference);
        // A reflection-inspected assembly can release its managed load-context object
        // one collection before the native PE mapping is fully torn down on Windows.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.False(loadContextReference.IsAlive);
        ScriptAssert.Equal(5050, result);
    }
#endif

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task FinallyLoopTransfersWorkInEveryCompilationMode(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var trace = "";
                for (var i = 0; i < 4; i++) {
                    try { trace += i; }
                    finally {
                        if (i == 0) continue;
                        if (i == 2) break;
                    }
                    trace += "x";
                }
                return trace;
            }
            """,
            mode);

        ScriptAssert.Equal("01x2", TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task RuntimeSemanticsRemainEquivalentAcrossCompilationModes(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            var moduleValue = 3;
            enum Mode { Zero, Four = 4, Five }

            func makeCounter(start) {
                var value = start;
                return () => { value++; return value; };
            }

            func sum8(a, b, c, d, e, f, g, h) {
                return a + b + c + d + e + f + g + h;
            }

            func defaults(a, b = 5) {
                return [a, b, $args.length];
            }

            export func run() {
                var [first, ...middle, last] = [1, 2, 3, 4];
                var { name, age } = { name: 'Aurora', age: 6 };
                middle.push(last);

                var object = { name, age, ...{ tag: 'ok' } };
                object.total = 0;
                for (var key in { a: 1, b: 2 }) object.total += 1;

                var textCount = 0;
                for (var ch in 'abc') textCount++;

                var counter = makeCounter(10);
                var counter1 = counter();
                var counter2 = counter();
                var defaultValues = defaults(2);
                var values = [1, 2, 3, 4, 5, 6, 7, 8];
                var sum = sum8(...values);
                var index = 0;
                values[index++] += 10;

                var logical = 0;
                false && logical++;
                true || logical++;
                true && logical++;
                false || logical++;

                var caught = '';
                try {
                    throw 'failure';
                } catch (error) {
                    caught = error.message;
                } finally {
                    moduleValue++;
                }

                delete object.age;
                return [
                    Mode.Zero, Mode.Four, Mode.Five,
                    first, `${name}:${middle.join(',')}`,
                    object.total, object.age, object.tag, textCount,
                    counter1, counter2,
                    defaultValues[0], defaultValues[1], defaultValues[2],
                    sum, values[0], index, logical, caught, moduleValue
                ];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[]
            {
                0, 4, 5,
                1, "Aurora:2,3,4",
                2, null, "ok", 3,
                11, 12,
                2, 5, 1,
                36, 11, 1, 2, "failure", 4
            },
            TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ImportsAndIncludesRemainEquivalentAcrossCompilationModes(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "dep.as",
            "@module(DEP); export const value = 40; export func add(delta) { return value + delta; }");
        workspace.WriteSource(
            "shared.as",
            "@module(SHARED); export const bonus = 2;");
        var main = workspace.WriteSource(
            "main.as",
            "@module(TEST); import dep from './dep'; include './shared'; export func run() { return [dep.add(bonus), dep.value, bonus]; }");
        var assemblyOut = mode == CompilationMode.Persistence
            ? Path.Combine(workspace.Root, "module-output.dll")
            : null;
        var engine = workspace.CreateEngine(mode, assemblyOut: assemblyOut);

        await engine.BuildAsync(main);

        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(new object?[] { 42, 40, 2 }, TestWorkspace.Execute(domain, "run"));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistenceDebugBuildEmitsAsSequencePoints()
    {
        using var workspace = new TestWorkspace();
        var assemblyPath = Path.Combine(workspace.Root, "debug-output.dll");
        var sourcePath = workspace.WriteSource("main.as",
            """
            @module(TEST);
            var value = 40;
            export func run() {
                var local = value + 2;
                return local;
            }
            """);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(workspace.Root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Persistence)
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Debug)
            .WithOutput(output => output.AssemblyFile = assemblyPath)
            .WithRuntime(runtime => runtime.ConsoleStdOut = TextWriter.Null)
            .WithRuntime(runtime => runtime.ConsoleErrorOut = TextWriter.Null);
        var engine = new AuroraEngine(options);

        await engine.BuildAsync(sourcePath);

        Assert.True(File.Exists(assemblyPath));

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var embeddedPdb = Assert.Single(peReader.ReadDebugDirectory()
            .Where(entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb));
        using var provider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdb);
        var reader = provider.GetMetadataReader();
        var document = Assert.Single(reader.Documents.Where(handle =>
            string.Equals(
                Path.GetFullPath(ReadDocumentName(reader, handle)),
                Path.GetFullPath(sourcePath),
                StringComparison.OrdinalIgnoreCase)));
        var lines = GetVisibleSequencePointLines(reader, document);

        Assert.Contains(2, lines);
        Assert.Contains(4, lines);
        Assert.Contains(5, lines);
    }

    [Fact]
    public async Task PersistenceDebugBuildKeepsTypedVariableMetadata()
    {
        using var workspace = new TestWorkspace();
        var assemblyPath = Path.Combine(workspace.Root, "debug-metadata.dll");
        var sourcePath = workspace.WriteSource("metadata.as",
            """
            @module(TEST);
            var moduleValue = 1;
            export func outer(value) {
                var captured = value;
                func inner(delta) { return captured + delta + moduleValue; }
                var local = 2;
                return inner(local);
            }
            """);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(workspace.Root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Persistence)
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Debug)
            .WithOutput(output => output.AssemblyFile = assemblyPath)
            .WithRuntime(runtime => runtime.ConsoleStdOut = TextWriter.Null)
            .WithRuntime(runtime => runtime.ConsoleErrorOut = TextWriter.Null);

        await new AuroraEngine(options).BuildAsync(sourcePath);

        var metadata = ReadDebuggerMetadata(assemblyPath);
        Assert.Contains(metadata, value => value.StartsWith("v=1;", StringComparison.Ordinal));
        Assert.Contains(metadata, value => value.Contains(";p:value:", StringComparison.Ordinal));
        Assert.Contains(metadata, value => value.Contains(";l:local:", StringComparison.Ordinal));
        Assert.Contains(metadata, value => value.Contains(";c:captured:", StringComparison.Ordinal));
        Assert.Contains(metadata, value => value.Contains(";u:captured:", StringComparison.Ordinal));
        Assert.Contains(metadata, value => value.Contains(";m:moduleValue", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PersistenceReleaseCanOmitStackTraceLocationWrites()
    {
        using var workspace = new TestWorkspace();
        var assemblyPath = Path.Combine(workspace.Root, "trace-disabled.dll");

        await BuildPersistenceStackTraceAssemblyAsync(workspace, assemblyPath, OptimizeOptions.Release, stackTrace: false);

        Assert.False(ReferencesScriptContextLocation(assemblyPath));
    }

    [Fact]
    public async Task PersistenceDebugKeepsStackTraceLocationWritesWhenDisabled()
    {
        using var workspace = new TestWorkspace();
        var assemblyPath = Path.Combine(workspace.Root, "debug-trace-disabled.dll");

        await BuildPersistenceStackTraceAssemblyAsync(workspace, assemblyPath, OptimizeOptions.Debug, stackTrace: false);

        Assert.True(ReferencesScriptContextLocation(assemblyPath));
    }
#endif

#if NET8_0
    [Fact]
    public async Task PersistenceModeRequiresNet9OrLater()
    {
        using var workspace = new TestWorkspace();
        var error = await Assert.ThrowsAsync<PlatformNotSupportedException>(() => workspace.CompileModuleAsync(
            "@module(TEST); export func run() { return 42; }",
            CompilationMode.Persistence));

        Assert.Contains(".NET 9.0", error.Message);
    }
#endif

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReleaseBuildWorksWithHotReloadDisabledOrEnabled(bool enableHotReload)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); func local(value) { return value + 1; } export func run() { return local(41); }",
            enableHotReload: enableHotReload);

        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public void StackTraceOptionDefaultsToEnabledAndCanBeDisabled()
    {
        Assert.True(EngineOptions.Default.Optimization.StackTrace);

        var options = EngineOptions.Default.WithOptimization(optimization => optimization.StackTrace = false);

        Assert.False(options.Optimization.StackTrace);
    }

#if NET9_0_OR_GREATER
    private static async Task BuildPersistenceStackTraceAssemblyAsync(
        TestWorkspace workspace,
        string assemblyPath,
        OptimizeOptions level,
        bool stackTrace)
    {
        var sourcePath = workspace.WriteSource("main.as",
            """
            @module(TEST);
            export func run(value) {
                var local = value + 1;
                if (local > 10) {
                    return local;
                }
                return local + 1;
            }
            """);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(workspace.Root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Persistence)
            .WithOptimization(optimization => optimization.Level = level)
            .WithOptimization(optimization => optimization.StackTrace = stackTrace)
            .WithOutput(output => output.AssemblyFile = assemblyPath)
            .WithRuntime(runtime => runtime.ConsoleStdOut = TextWriter.Null)
            .WithRuntime(runtime => runtime.ConsoleErrorOut = TextWriter.Null);
        var engine = new AuroraEngine(options);

        await engine.BuildAsync(sourcePath);
    }

    private static bool ReferencesScriptContextLocation(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        foreach (var handle in reader.MemberReferences)
        {
            var member = reader.GetMemberReference(handle);
            if (!string.Equals(reader.GetString(member.Name), nameof(AuroraScript.Runtime.ScriptContext.Location), StringComparison.Ordinal))
            {
                continue;
            }

            if (member.Parent.Kind != HandleKind.TypeReference)
            {
                continue;
            }

            var type = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
            if (string.Equals(reader.GetString(type.Namespace), "AuroraScript.Runtime", StringComparison.Ordinal) &&
                string.Equals(reader.GetString(type.Name), nameof(AuroraScript.Runtime.ScriptContext), StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static List<string> ReadDebuggerMetadata(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var result = new List<string>();
        foreach (var handle in reader.CustomAttributes)
        {
            var attribute = reader.GetCustomAttribute(handle);
            if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;
            var constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
            if (constructor.Parent.Kind != HandleKind.TypeReference) continue;
            var type = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);
            if (!string.Equals(reader.GetString(type.Name), "ScriptDebuggerMetadataAttribute", StringComparison.Ordinal)) continue;

            var blob = reader.GetBlobReader(attribute.Value);
            Assert.Equal(1, blob.ReadUInt16());
            result.Add(blob.ReadSerializedString() ?? string.Empty);
        }
        return result;
    }

    private static void AssertNativeNumericMethod(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var methodNames = new List<string>();
        foreach (var handle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(handle);
            var methodName = reader.GetString(method.Name);
            methodNames.Add(methodName);
            if (!string.Equals(methodName, "sum$native", StringComparison.Ordinal))
            {
                continue;
            }

            // DEFAULT, ScriptContext plus two R8 parameters, return R8.
            var signature = reader.GetBlobBytes(method.Signature);
            Assert.Equal(0x00, signature[0]);
            Assert.Equal(3, signature[1]);
            Assert.Equal(0x0d, signature[2]);
            Assert.Equal(0x12, signature[3]); // ScriptContext class marker.
            Assert.Equal(new byte[] { 0x0d, 0x0d }, signature[^2..]);
            Assert.True(
                (method.ImplAttributes & MethodImplAttributes.AggressiveInlining) != 0,
                "Native direct-call methods should carry the general-purpose JIT inlining hint.");
            var body = peReader.GetMethodBody(method.RelativeVirtualAddress);
            var locals = reader.GetStandaloneSignature(body.LocalSignature);
            // LOCAL_SIG, local count 2, local R8, local R8.
            Assert.Equal([0x07, 0x02, 0x0d, 0x0d], reader.GetBlobBytes(locals.Signature));
            return;
        }

        Assert.Fail("The persisted assembly did not contain sum$native. Methods: " + string.Join(", ", methodNames));
    }

    private static string ReadDocumentName(MetadataReader reader, DocumentHandle handle)
    {
        return reader.GetString(reader.GetDocument(handle).Name);
    }

    private static List<int> GetVisibleSequencePointLines(MetadataReader reader, DocumentHandle document)
    {
        var lines = new List<int>();
        foreach (var methodHandle in reader.MethodDebugInformation)
        {
            var method = reader.GetMethodDebugInformation(methodHandle);
            foreach (var point in method.GetSequencePoints())
            {
                var pointDocument = point.Document.IsNil ? method.Document : point.Document;
                if (!point.IsHidden && pointDocument == document)
                {
                    lines.Add(point.StartLine);
                }
            }
        }

        return lines;
    }

    private sealed class PersistedAssemblyLoadContext : AssemblyLoadContext
    {
        public PersistedAssemblyLoadContext()
            : base(nameof(PersistedAssemblyLoadContext), isCollectible: true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var runtimeAssembly = typeof(AuroraEngine).Assembly;
            return AssemblyName.ReferenceMatchesDefinition(assemblyName, runtimeAssembly.GetName())
                ? runtimeAssembly
                : null;
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static ScriptDatum ExecutePersistedAssembly(
        string assemblyPath,
        TestWorkspace workspace,
        out WeakReference loadContextReference)
    {
        var loadContext = new PersistedAssemblyLoadContext();
        loadContextReference = new WeakReference(loadContext);
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            var initializerMethod = assembly
                .GetType("AuroraScriptInitializer", throwOnError: true)!
                .GetMethod("InitializeDomain", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(initializerMethod);

            var initializer = initializerMethod!.CreateDelegate<ScriptFunctionDelegate>();
            var hostEngine = workspace.CreateEngine();
            using var domain = hostEngine.CreateEmptyDomain(globalConfiguration: null);
            initializer(new ScriptContext(domain), Span<ScriptDatum>.Empty);
            return TestWorkspace.Execute(domain, "run");
        }
        finally
        {
            loadContext.Unload();
        }
    }
#endif
}
