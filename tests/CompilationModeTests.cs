using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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
            .WithCompiler(compiler => compiler.WithDirectory(workspace.Root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Persistence)
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Debug)
            .WithOutput(output => output.AssemblyFile = assemblyPath)
            .WithRuntime(runtime => runtime.ConsoleStdOut = TextWriter.Null)
            .WithRuntime(runtime => runtime.ConsoleErrorOut = TextWriter.Null);
        var engine = new AuroraEngine(options);

        await engine.BuildAsync(engine.FileSource(sourcePath, Encoding.UTF8));

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

#if NET9_0_OR_GREATER
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
#endif
}
