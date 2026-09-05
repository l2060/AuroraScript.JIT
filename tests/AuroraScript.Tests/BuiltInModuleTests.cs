using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class BuiltInModuleTests
{
    [Fact]
    public void BuiltInsAreOptInAndOptionsRemainImmutable()
    {
        var original = EngineOptions.Default;
        var configured = original.WithBuiltIns(builtIns =>
            builtIns.Add(BuiltInModules.FileSystem));

        Assert.Empty(original.BuiltIns);
        Assert.Single(configured.BuiltIns);
        Assert.Same(BuiltInModules.FileSystem, configured.BuiltIns[0]);

        var duplicate = Assert.Throws<InvalidOperationException>(() =>
            configured.WithBuiltIns(builtIns => builtIns.Add(BuiltInModules.FileSystem)));
        Assert.Contains("already configured", duplicate.Message, StringComparison.Ordinal);

        var cleared = configured.WithBuiltIns(builtIns => builtIns.Clear());
        Assert.Empty(cleared.BuiltIns);
        Assert.Single(configured.BuiltIns);
    }

    [Fact]
    public async Task DefaultOptionsDoNotResolveBuiltInModules()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "main.as",
            "@module(TEST); import fs from 'fs'; export func run() { return fs.readText('missing'); }");
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableFileSystem: false));

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() =>
            engine.BuildAsync("main.as"));

        Assert.Contains(
            error.Diagnostics,
            diagnostic => diagnostic.Message.Contains("Import file not found: fs", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task FileSystemModuleCanBeExplicitlyEnabled(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var textPath = Path.Combine(workspace.Root, "value.txt");
        File.WriteAllText(textPath, "Aurora builtin");
        workspace.WriteSource(
            "main.as",
            "@module(TEST); import fs from 'fs'; export func run(path) { return fs.readText(path); }");
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableFileSystem: true, mode));

        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();

        ScriptAssert.Equal(
            "Aurora builtin",
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromString(textPath)));
    }

    [Fact]
    public void BuiltInSelectionsDoNotLeakAcrossEngines()
    {
        using var workspace = new TestWorkspace();
        var enabledEngine = new AuroraEngine(CreateOptions(workspace.Root, enableFileSystem: true));
        var defaultEngine = new AuroraEngine(CreateOptions(workspace.Root, enableFileSystem: false));

        using var enabledDomain = enabledEngine.CreateEmptyDomain(null);
        using var defaultDomain = defaultEngine.CreateEmptyDomain(null);

        Assert.NotSame(ScriptObject.Null, enabledDomain.GetModule("fs"));
        Assert.Same(ScriptObject.Null, defaultDomain.GetModule("fs"));
    }

    [Fact]
    public void BuiltInModuleInstancesAreIsolatedBetweenDomains()
    {
        using var workspace = new TestWorkspace();
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableFileSystem: true));
        using var firstDomain = engine.CreateEmptyDomain(null);
        using var secondDomain = engine.CreateEmptyDomain(null);
        var firstModule = firstDomain.GetModule("fs");
        var secondModule = secondDomain.GetModule("fs");

        Assert.NotSame(ScriptObject.Null, firstModule);
        Assert.NotSame(ScriptObject.Null, secondModule);
        Assert.NotSame(firstModule, secondModule);

        firstModule.Define("domainMarker", ScriptDatum.FromNumber(1));

        Assert.Same(ScriptObject.Null, secondModule.GetPropertyValue("domainMarker"));
    }

    [Fact]
    public async Task BareBuiltInPathTakesPriorityButRelativePathUsesProjectResolver()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("fs.as", "@module(LOCAL_FS); export const value = 42;");
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import native from 'fs';
            import local from './fs';
            export func run() { return [native.readText != null, local.value]; }
            """);
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableFileSystem: true));

        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();

        ScriptAssert.Equal(
            new object?[] { true, 42 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task CustomBuiltInDefinitionCreatesRuntimeModule()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "main.as",
            "@module(TEST); import custom from 'custom'; export func run() { return custom.answer; }");
        var custom = new BuiltInModuleDefinition(
            "custom",
            module => module.Define(
                "answer",
                ScriptDatum.FromNumber(42),
                writeable: false,
                enumerable: false));
        var options = CreateOptions(workspace.Root, enableFileSystem: false)
            .WithBuiltIns(builtIns => builtIns.Add(custom));
        var engine = new AuroraEngine(options);

        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();

        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ProjectModuleCannotReuseEnabledBuiltInName()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("main.as", "@module(fs); export const value = 1;");
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableFileSystem: true));

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() =>
            engine.BuildAsync("main.as"));

        Assert.Contains("conflicts with the enabled built-in module", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileSystemModuleProvidesBasicTextAndDirectoryOperations()
    {
        using var workspace = new TestWorkspace();
        var dataRoot = Path.Combine(workspace.Root, "fs-data");
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import fs from 'fs';

            export func run(root) {
                var rootPath = Path.of(root);
                var sourceDir = Path.of(rootPath, 'source');
                var emptyDir = Path.of(sourceDir, 'empty');
                var file = Path.of(sourceDir, 'zeta.txt');
                var alphaFile = Path.of(sourceDir, 'alpha.txt');
                var copiedDir = Path.of(rootPath, 'source-copy');
                var movedDir = Path.of(rootPath, 'source-moved');

                var made = fs.mkDir(emptyDir);
                var wrote = fs.writeText(file, 'alpha');
                var appended = fs.appendText(Path.of(file), '-beta');
                fs.writeText(alphaFile, 'first');
                var exists = fs.exist(file);
                var isFile = fs.isFile(file);
                var isDir = fs.isDir(sourceDir);
                var text = fs.readText(Path.of(file));
                var size = fs.size(Path.of(file));
                var names = fs.dir(Path.of(sourceDir));
                var copied = fs.copy(sourceDir, copiedDir);
                var moved = fs.move(copiedDir, movedDir);
                var movedText = fs.readText(Path.of(movedDir, 'zeta.txt'));
                var deletedAlpha = fs.delete(alphaFile);
                var deletedFile = fs.delete(file);
                var deletedMissing = fs.delete(file);
                var deletedEmpty = fs.delete(emptyDir);
                var deletedSource = fs.delete(sourceDir);
                var deletedTree = fs.delete(movedDir, true);

                return [
                    fs.exist(root), made, wrote, appended, exists, isFile, isDir, text, size, names,
                    copied, moved, movedText, deletedAlpha, deletedFile, deletedMissing,
                    deletedEmpty, deletedSource, deletedTree, fs.exist(movedDir)
                ];
            }
            """);
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableFileSystem: true));

        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();

        ScriptAssert.Equal(
            new object?[]
            {
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                "alpha-beta",
                10,
                new object?[] { "alpha.txt", "empty", "zeta.txt" },
                true,
                true,
                "alpha-beta",
                true,
                true,
                false,
                true,
                true,
                true,
                false
            },
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: ScriptDatum.FromString(dataRoot)));
        Assert.False(Directory.Exists(Path.Combine(dataRoot, "source")));
        Assert.False(Directory.Exists(Path.Combine(dataRoot, "source-moved")));
    }

    [Fact]
    public async Task FileSystemModuleReadsWritesAndAppendsBytes()
    {
        using var workspace = new TestWorkspace();
        var path = Path.Combine(workspace.Root, "bytes.bin");
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import fs from 'fs';

            export func run(path) {
                var first = new UInt8Array(3);
                first[0] = 1;
                first[1] = 2;
                first[2] = 255;
                var last = new UInt8Array(2);
                last[0] = 3;
                last[1] = 4;

                var wrote = fs.writeBytes(Path.of(path), first);
                var appended = fs.appendBytes(path, last);
                var bytes = fs.readBytes(Path.of(path));
                return [wrote, appended, bytes[0], bytes[1], bytes[2], bytes[3], bytes[4]];
            }
            """);
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableFileSystem: true));

        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();

        ScriptAssert.Equal(
            new object?[] { true, true, 1, 2, 255, 3, 4 },
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromString(path)));
        Assert.Equal(new byte[] { 1, 2, 255, 3, 4 }, File.ReadAllBytes(path));
    }

    [Fact]
    public async Task FileSystemCopyAndMoveRequireExplicitOverwrite()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import fs from 'fs';

            export func copy(source, destination, overwrite = false) {
                return fs.copy(Path.of(source), Path.of(destination), overwrite);
            }

            export func move(source, destination, overwrite = false) {
                return fs.move(Path.of(source), Path.of(destination), overwrite);
            }
            """);
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableFileSystem: true));
        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();

        var copySource = Path.Combine(workspace.Root, "copy-source.txt");
        var copyDestination = Path.Combine(workspace.Root, "copy-destination.txt");
        File.WriteAllText(copySource, "new copy");
        File.WriteAllText(copyDestination, "old copy");

        var copyError = Assert.Throws<AuroraRuntimeException>(() =>
            ExecutePathOperation(domain, "copy", copySource, copyDestination, overwrite: false));
        Assert.Contains("fs.copy failed", copyError.Message, StringComparison.Ordinal);
        ScriptAssert.Equal(
            true,
            ExecutePathOperation(domain, "copy", copySource, copyDestination, overwrite: true));
        Assert.Equal("new copy", File.ReadAllText(copyDestination));

        var moveSource = Path.Combine(workspace.Root, "move-source.txt");
        var moveDestination = Path.Combine(workspace.Root, "move-destination.txt");
        File.WriteAllText(moveSource, "new move");
        File.WriteAllText(moveDestination, "old move");

        var moveError = Assert.Throws<AuroraRuntimeException>(() =>
            ExecutePathOperation(domain, "move", moveSource, moveDestination, overwrite: false));
        Assert.Contains("fs.move failed", moveError.Message, StringComparison.Ordinal);
        ScriptAssert.Equal(
            true,
            ExecutePathOperation(domain, "move", moveSource, moveDestination, overwrite: true));
        Assert.False(File.Exists(moveSource));
        Assert.Equal("new move", File.ReadAllText(moveDestination));

        var directorySource = Path.Combine(workspace.Root, "directory-source");
        var directoryDestination = Path.Combine(workspace.Root, "directory-destination");
        Directory.CreateDirectory(directorySource);
        Directory.CreateDirectory(directoryDestination);
        File.WriteAllText(Path.Combine(directorySource, "new.txt"), "new directory");
        File.WriteAllText(Path.Combine(directoryDestination, "old.txt"), "old directory");

        var directoryError = Assert.Throws<AuroraRuntimeException>(() =>
            ExecutePathOperation(domain, "move", directorySource, directoryDestination, overwrite: false));
        Assert.Contains("destination directory already exists", directoryError.Message, StringComparison.OrdinalIgnoreCase);
        ScriptAssert.Equal(
            true,
            ExecutePathOperation(domain, "move", directorySource, directoryDestination, overwrite: true));
        Assert.False(Directory.Exists(directorySource));
        Assert.False(File.Exists(Path.Combine(directoryDestination, "old.txt")));
        Assert.Equal(
            "new directory",
            File.ReadAllText(Path.Combine(directoryDestination, "new.txt")));
    }

    [Fact]
    public async Task FileSystemModuleValidatesArgumentsAndRecursiveDeleteIsOptIn()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import fs from 'fs';

            export func remove(path, recursive = false) {
                return fs.delete(Path.of(path), recursive);
            }

            export func invalidPath(value) { return fs.exist(value); }
            export func invalidBytes(path) { return fs.writeBytes(path, 'bytes'); }
            export func invalidRecursive(path) { return fs.delete(path, 'yes'); }
            export func list(path) { return fs.dir(path); }
            """);
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableFileSystem: true));
        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();

        var directory = Path.Combine(workspace.Root, "non-empty");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "value.txt"), "value");

        var deleteError = Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "remove",
                arguments: ScriptDatum.FromString(directory)));
        Assert.Contains("fs.delete failed", deleteError.Message, StringComparison.Ordinal);
        Assert.True(Directory.Exists(directory));

        ScriptAssert.Equal(
            true,
            TestWorkspace.Execute(
                domain,
                "remove",
                "TEST",
                ScriptDatum.FromString(directory),
                ScriptDatum.FromBoolean(true)));
        ScriptAssert.Equal(
            false,
            TestWorkspace.Execute(
                domain,
                "remove",
                arguments: ScriptDatum.FromString(directory)));

        var pathError = Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "invalidPath",
                arguments: ScriptDatum.FromNumber(42)));
        Assert.Contains("non-empty string or Path", pathError.Message, StringComparison.Ordinal);

        var bytesError = Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "invalidBytes",
                arguments: ScriptDatum.FromString(Path.Combine(workspace.Root, "invalid.bin"))));
        Assert.Contains("UInt8Array", bytesError.Message, StringComparison.Ordinal);

        var recursiveError = Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "invalidRecursive",
                arguments: ScriptDatum.FromString(workspace.Root)));
        Assert.Contains("'recursive' to be a boolean", recursiveError.Message, StringComparison.Ordinal);

        var listError = Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "list",
                arguments: ScriptDatum.FromString(Path.Combine(workspace.Root, "missing"))));
        Assert.Contains("fs.dir failed", listError.Message, StringComparison.Ordinal);
    }

    private static ScriptDatum ExecutePathOperation(
        ScriptDomain domain,
        string method,
        string source,
        string destination,
        bool overwrite)
    {
        return TestWorkspace.Execute(
            domain,
            method,
            "TEST",
            ScriptDatum.FromString(source),
            ScriptDatum.FromString(destination),
            ScriptDatum.FromBoolean(overwrite));
    }

    private static EngineOptions CreateOptions(
        string root,
        bool enableFileSystem,
        CompilationMode mode = CompilationMode.Dynamic)
    {
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = mode)
            .WithRuntime(runtime => runtime.ConsoleStdOut = TextWriter.Null)
            .WithRuntime(runtime => runtime.ConsoleErrorOut = TextWriter.Null);

        return enableFileSystem
            ? options.WithBuiltIns(builtIns => builtIns.Add(BuiltInModules.FileSystem))
            : options;
    }
}
