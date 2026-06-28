using AuroraScript.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.Source
{
    /// <summary>
    /// In-memory script source resolver.
    /// </summary>
    /// <remarks>
    /// The root and source keys are normalized once when the resolver is built. This
    /// resolver only resolves targets that are inside its root and present in the memory
    /// table. In a composite resolver, placing memory before a file-system resolver lets
    /// memory override any resolved target that falls under the memory root.
    /// </remarks>
    public sealed class MemorySourceResolver : IScriptSourceResolver
    {
        private readonly Dictionary<string, string> _sources;

        /// <summary>
        /// Initializes an in-memory resolver.
        /// </summary>
        public MemorySourceResolver(string root = "mem://app/")
        {
            Root = ScriptPath.NormalizeBaseDirectory(string.IsNullOrWhiteSpace(root) ? "mem://app/" : root);
            _sources = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        /// <inheritdoc />
        public string Root { get; }

        /// <summary>
        /// Adds or replaces a memory source.
        /// </summary>
        /// <remarks>
        /// <paramref name="path"/> may be absolute or relative to <see cref="Root"/>.
        /// It is normalized to a full source path with '/' separators before storage.
        /// </remarks>
        public MemorySourceResolver Add(string path, string source)
        {
            _sources[ScriptPath.GetFullPath(Root, path)] = source ?? string.Empty;
            return this;
        }

        /// <inheritdoc />
        public ValueTask<ScriptSourceReference?> ResolveAsync(
            ScriptSourceReference? importer,
            string requestedPath,
            ScriptResolveContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentPath = ResolveCurrentPath(importer);
            var currentDirectory = importer == null ? Root : ScriptPath.GetDirectoryName(currentPath);
            var fullPath = ScriptPath.EnsureExtension(ScriptPath.Combine(currentDirectory, requestedPath), context?.Extension);
            if (!ScriptPath.IsWithinNormalizedRoot(Root, fullPath))
            {
                return new ValueTask<ScriptSourceReference?>((ScriptSourceReference?)null);
            }

            if (!_sources.ContainsKey(fullPath))
            {
                return new ValueTask<ScriptSourceReference?>((ScriptSourceReference?)null);
            }

            var reference = new ScriptSourceReference(Root, fullPath);
            return new ValueTask<ScriptSourceReference?>(reference);
        }

        private string ResolveCurrentPath(ScriptSourceReference? importer)
        {
            if (importer == null)
            {
                return Root;
            }

            return importer.Value.FullPath;
        }

        /// <inheritdoc />
        public ValueTask<ScriptSource> GetSourceAsync(
            ScriptSourceReference reference,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ScriptPath.NormalizedRootsEqual(reference.BaseDirectory, Root))
            {
                throw new FileNotFoundException("Memory script source not found.", reference.FullPath);
            }

            if (!_sources.TryGetValue(reference.FullPath, out var text))
            {
                throw new FileNotFoundException("Memory script source not found.", reference.FullPath);
            }

            return new ValueTask<ScriptSource>(new MemorySource(reference.BaseDirectory, reference.FullPath, text));
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
            ScriptSourceQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var pair in _sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return new MemorySource(Root, pair.Key, pair.Value);
            }
        }
    }

}
