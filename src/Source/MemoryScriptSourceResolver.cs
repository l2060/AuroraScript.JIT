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
    public sealed class MemorySourceResolver : IScriptSourceResolver
    {
        private readonly Dictionary<string, string> _sources;

        /// <summary>
        /// Initializes an in-memory resolver.
        /// </summary>
        public MemorySourceResolver(string root = "mem://app/")
        {
            Root = NormalizeRoot(root);
            _sources = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        /// <inheritdoc />
        public string Root { get; }

        /// <summary>
        /// Adds or replaces a memory source.
        /// </summary>
        public MemorySourceResolver Add(string path, string source)
        {
            _sources[ResolveFromDirectory(Root, path)] = source ?? string.Empty;
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
            var currentDirectory = importer == null ? Root : GetDirectory(currentPath);
            var fullPath = EnsureExtension(ResolveFromDirectory(currentDirectory, requestedPath), context?.Extension);
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

            return ResolveFromDirectory(Root, importer.Value.ModulePath);
        }

        /// <inheritdoc />
        public ValueTask<ScriptSource> GetSourceAsync(
            ScriptSourceReference reference,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

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

        private static string NormalizeRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                root = "mem://app/";
            }

            root = root.TrimEnd('/', '\\').Replace('\\', '/');
            return root + "/";
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

            var baseUriText = directory.EndsWith("/", StringComparison.Ordinal) ? directory : directory + "/";
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
