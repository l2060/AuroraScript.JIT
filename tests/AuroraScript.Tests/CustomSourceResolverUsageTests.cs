using AuroraScript.Core;
using AuroraScript.Source;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
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

        await engine.BuildAsync("main.as");

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

        await engine.BuildAsync("main.aurora");

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

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync("main.as"));

        var diagnostic = Assert.Single(error.Diagnostics);
        Assert.Contains("Import file not found", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("./missing", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildsAllSourcesFromConfiguredResolver()
    {
        const string root = "vfs://aurora-script-tests/all";
        var resolver = new VirtualFileSystemSourceResolver(root)
            .AddSource("main.as", "@module(TEST); export func run() { return 42; }")
            .AddSource("helper.as", "@module(HELPER); export const value = 1;");
        var engine = CreateEngine(root, resolver);

        await engine.BuildAsync();

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
        Assert.Contains("vfs://aurora-script-tests/all/main.as", resolver.OpenedPaths);
        Assert.Contains("vfs://aurora-script-tests/all/helper.as", resolver.OpenedPaths);
    }

    [Fact]
    public async Task BuildAwaitsAsynchronousResolverOperations()
    {
        const string root = "vfs://aurora-script-tests/async";
        var resolver = new AsyncVirtualFileSystemSourceResolver(root)
            .AddSource(
                "main.as",
                """
                @module(TEST);
                import value from './value';
                export func run() {
                    return value.number;
                }
                """)
            .AddSource("value.as", "@module(VALUE); export const number = 42;");
        var engine = CreateEngine(root, resolver);

        await engine.BuildAsync("main.as");

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
        Assert.True(resolver.ResolveAwaitCount >= 2);
        Assert.True(resolver.SourceAwaitCount >= 2);
    }

    private static AuroraEngine CreateEngine(
        string root,
        IScriptSourceResolver resolver,
        string extension = ".as")
    {
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
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
            _root = ScriptPath.NormalizeBaseDirectory(root);
        }

        public IReadOnlyCollection<string> OpenedPaths => _openedPaths.ToArray();

        public VirtualFileSystemSourceResolver AddSource(string path, string source)
        {
            _sources[ScriptPath.GetFullPath(_root, path)] = source ?? string.Empty;
            return this;
        }

        public ScriptSource OpenSource(string path)
        {
            var fullPath = ScriptPath.GetFullPath(_root, path);
            return GetSourceAsync(new ScriptSourceReference(_root, fullPath))
                .AsTask()
                .GetAwaiter()
                .GetResult();
        }

        public string Root => _root;

        public ValueTask<ScriptSourceReference?> ResolveAsync(
            ScriptSourceReference? importer,
            string requestedPath,
            ScriptResolveContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentSourcePath = ResolveCurrentPath(importer);
            var currentDirectory = importer == null ? _root : ScriptPath.GetDirectoryName(currentSourcePath);
            var fullPath = ScriptPath.EnsureExtension(ScriptPath.Combine(currentDirectory, requestedPath), context.Extension);
            if (!ScriptPath.IsWithinNormalizedRoot(_root, fullPath))
            {
                return new ValueTask<ScriptSourceReference?>((ScriptSourceReference?)null);
            }

            if (_sources.ContainsKey(fullPath))
            {
                return new ValueTask<ScriptSourceReference?>(new ScriptSourceReference(_root, fullPath));
            }

            return new ValueTask<ScriptSourceReference?>((ScriptSourceReference?)null);
        }

        public ValueTask<ScriptSource> GetSourceAsync(
            ScriptSourceReference source,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ScriptPath.IsWithinNormalizedRoot(_root, source.FullPath))
            {
                throw new FileNotFoundException("Virtual script source not found.", source.FullPath);
            }

            if (!_sources.TryGetValue(source.FullPath, out var text))
            {
                throw new FileNotFoundException("Virtual script source not found.", source.FullPath);
            }

            _openedPaths.Add(source.FullPath);
            return new ValueTask<ScriptSource>(new MemorySource(source.BaseDirectory, source.FullPath, text));
        }

        public async IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
            ScriptSourceQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var pair in _sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                _openedPaths.Add(pair.Key);
                yield return new MemorySource(_root, pair.Key, pair.Value);
            }
        }

        private string ResolveCurrentPath(ScriptSourceReference? importer)
        {
            if (importer == null)
            {
                return _root;
            }

            return importer.Value.FullPath;
        }
    }

    private sealed class AsyncVirtualFileSystemSourceResolver : IScriptSourceResolver
    {
        private readonly VirtualFileSystemSourceResolver _inner;
        private int _resolveAwaitCount;
        private int _sourceAwaitCount;

        public AsyncVirtualFileSystemSourceResolver(string root)
        {
            _inner = new VirtualFileSystemSourceResolver(root);
        }

        public int ResolveAwaitCount => Volatile.Read(ref _resolveAwaitCount);

        public int SourceAwaitCount => Volatile.Read(ref _sourceAwaitCount);

        public string Root => _inner.Root;

        public AsyncVirtualFileSystemSourceResolver AddSource(string path, string source)
        {
            _inner.AddSource(path, source);
            return this;
        }

        public async ValueTask<ScriptSourceReference?> ResolveAsync(
            ScriptSourceReference? importer,
            string requestedPath,
            ScriptResolveContext context,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _resolveAwaitCount);
            return await _inner.ResolveAsync(importer, requestedPath, context, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<ScriptSource> GetSourceAsync(
            ScriptSourceReference source,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _sourceAwaitCount);
            return await _inner.GetSourceAsync(source, cancellationToken).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
            ScriptSourceQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var source in _inner.GetAllSourcesAsync(query, cancellationToken).ConfigureAwait(false))
            {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                yield return source;
            }
        }
    }
}
