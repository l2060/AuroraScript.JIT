using AuroraScript.Core;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class CustomSourceResolverUsageTests
{
    [Fact]
    public async Task BuildsModuleGraphFromVirtualFileSystem()
    {
        const string root = "vfs://aurora-script-tests/app";
        var resolver = new VirtualFileSystemSourceResolver(root)
            .AddSource(
                "main.as",
                """
                @module(TEST);
                import math from './lib/math';
                include './shared/constants';
                export func run() {
                    return math.add(BASE, OFFSET);
                }
                """)
            .AddSource(
                "lib/math.as",
                """
                @module(MATH);
                export func add(left, right) {
                    return left + right;
                }
                """)
            .AddSource(
                "shared/constants.as",
                """
                export const BASE = 40;
                export const OFFSET = 2;
                """);
        var engine = CreateEngine(root, resolver);

        await engine.BuildAsync(resolver.OpenSource("main.as"));

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
        Assert.Contains("vfs://aurora-script-tests/app/main.as", resolver.OpenedPaths);
        Assert.Contains("vfs://aurora-script-tests/app/lib/math.as", resolver.OpenedPaths);
        Assert.Contains("vfs://aurora-script-tests/app/shared/constants.as", resolver.OpenedPaths);
    }

    [Fact]
    public async Task HonorsConfiguredScriptExtension()
    {
        const string root = "vfs://aurora-script-tests/custom-extension";
        var resolver = new VirtualFileSystemSourceResolver(root)
            .AddSource(
                "main.aurora",
                """
                @module(TEST);
                import value from './feature/value';
                export func run() {
                    return value.number;
                }
                """)
            .AddSource("feature/value.aurora", "@module(VALUE); export const number = 42;");
        var engine = CreateEngine(root, resolver, ".aurora");

        await engine.BuildAsync(resolver.OpenSource("main.aurora"));

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
        Assert.Contains("vfs://aurora-script-tests/custom-extension/feature/value.aurora", resolver.OpenedPaths);
    }

    [Fact]
    public async Task ReportsMissingVirtualDependency()
    {
        const string root = "vfs://aurora-script-tests/missing-dependency";
        var resolver = new VirtualFileSystemSourceResolver(root)
            .AddSource(
                "main.as",
                """
                @module(TEST);
                import missing from './missing';
                export func run() {
                    return 1;
                }
                """);
        var engine = CreateEngine(root, resolver);

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync(resolver.OpenSource("main.as")));

        var diagnostic = Assert.Single(error.Diagnostics);
        Assert.Contains("Import file not found", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("./missing", diagnostic.Message, StringComparison.Ordinal);
    }

    private static AuroraEngine CreateEngine(
        string root,
        IScriptSourceResolver resolver,
        string extension = ".as")
    {
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.Directory = root)
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithCompiler(compiler => compiler.ExtName = extension)
            .WithCompiler(compiler => compiler.SourceResolver = resolver)
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release);
        return new AuroraEngine(options);
    }

    private sealed class VirtualFileSystemSourceResolver : IScriptSourceResolver
    {
        private readonly string _root;
        private readonly Dictionary<string, string> _sources = new(StringComparer.Ordinal);
        private readonly ConcurrentBag<string> _openedPaths = new();

        public VirtualFileSystemSourceResolver(string root)
        {
            _root = NormalizeRoot(root);
        }

        public IReadOnlyCollection<string> OpenedPaths => _openedPaths.ToArray();

        public VirtualFileSystemSourceResolver AddSource(string path, string source)
        {
            _sources[ResolveFromDirectory(_root, path)] = source ?? string.Empty;
            return this;
        }

        public ScriptSource OpenSource(string path)
        {
            var fullPath = ResolveFromDirectory(_root, path);
            return Open(new ScriptSourceReference(_root, fullPath), Encoding.UTF8);
        }

        public bool TryResolve(
            string baseDirectory,
            string currentSourcePath,
            string requestedPath,
            string extension,
            out ScriptSourceReference source)
        {
            var currentDirectory = GetDirectory(currentSourcePath);
            var fullPath = EnsureExtension(ResolveFromDirectory(currentDirectory, requestedPath), extension);
            if (_sources.ContainsKey(fullPath))
            {
                source = new ScriptSourceReference(_root, fullPath);
                return true;
            }

            source = default;
            return false;
        }

        public ScriptSource Open(ScriptSourceReference source, Encoding encoding)
        {
            if (!_sources.TryGetValue(source.FullPath, out var text))
            {
                throw new FileNotFoundException("Virtual script source not found.", source.FullPath);
            }

            _openedPaths.Add(source.FullPath);
            return new MemoryScriptSource(source.BaseDirectory, source.FullPath, text);
        }

        private static string NormalizeRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentException("A virtual source root is required.", nameof(root));
            }

            return root.TrimEnd('/', '\\').Replace('\\', '/');
        }

        private static string ResolveFromDirectory(string directory, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A source path is required.", nameof(path));
            }

            path = path.Replace('\\', '/');
            if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
            {
                return absolute.ToString();
            }

            var baseUriText = directory.EndsWith("/", StringComparison.Ordinal)
                ? directory
                : directory + "/";
            return new Uri(new Uri(baseUriText, UriKind.Absolute), path).ToString();
        }

        private static string GetDirectory(string path)
        {
            path = path.Replace('\\', '/');
            var slash = path.LastIndexOf('/');
            return slash < 0 ? path : path.Substring(0, slash + 1);
        }

        private static string EnsureExtension(string path, string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return path;
            }

            if (extension[0] != '.')
            {
                extension = "." + extension;
            }

            var slash = path.LastIndexOf('/');
            var dot = path.LastIndexOf('.');
            if (dot > slash)
            {
                return string.Equals(path.Substring(dot), extension, StringComparison.Ordinal)
                    ? path
                    : path.Substring(0, dot) + extension;
            }

            return path + extension;
        }
    }
}
