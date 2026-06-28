using AuroraScript.Core;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.Source
{
    /// <summary>
    /// Default script source resolver that loads scripts from the file system.
    /// </summary>
    /// <remarks>
    /// The root is normalized once at construction and all internal paths use '/'
    /// separators. <c>BuildAsync()</c> enumerates files under <see cref="Root"/>. Entry
    /// paths resolve from <see cref="Root"/>, while dependency paths resolve from the
    /// importing file's full path. A dependency can therefore point outside
    /// <see cref="Root"/> when the relative path and file system allow it; the returned
    /// reference still uses this resolver root so reads route back here.
    /// </remarks>
    public sealed class FileSystemScriptSourceResolver : IScriptSourceResolver
    {
        private readonly Encoding _encoding;

        /// <summary>
        /// Initializes a file-system resolver.
        /// </summary>
        public FileSystemScriptSourceResolver(string root = null, Encoding encoding = null)
        {
            Root = ScriptPath.NormalizeBaseDirectory(root);
            _encoding = encoding ?? Encoding.UTF8;
        }

        /// <inheritdoc />
        public string Root { get; }

        /// <inheritdoc />
        public ValueTask<ScriptSourceReference?> ResolveAsync(
            ScriptSourceReference? importer,
            string requestedPath,
            ScriptResolveContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var baseDirectory = Root;
            var currentPath = ResolveCurrentPath(importer, baseDirectory);
            var currentDirectory = importer == null ? baseDirectory : ScriptPath.GetDirectoryName(currentPath);
            var fullPath = ScriptPath.EnsureExtension(ScriptPath.Combine(currentDirectory, requestedPath), context?.Extension);
            if (!File.Exists(fullPath))
            {
                return new ValueTask<ScriptSourceReference?>((ScriptSourceReference?)null);
            }

            var reference = new ScriptSourceReference(baseDirectory, fullPath);
            return new ValueTask<ScriptSourceReference?>(reference);
        }

        private string ResolveCurrentPath(ScriptSourceReference? importer, string baseDirectory)
        {
            if (importer == null)
            {
                return baseDirectory;
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
                throw new FileNotFoundException("Script source not found.", reference.FullPath);
            }

            if (!File.Exists(reference.FullPath))
            {
                throw new FileNotFoundException("Script source not found.", reference.FullPath);
            }

            return new ValueTask<ScriptSource>(new FileSource(reference.BaseDirectory, reference.FullPath, _encoding));
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
            ScriptSourceQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var root = Root;
            if (!Directory.Exists(root))
            {
                yield break;
            }

            var extension = string.IsNullOrWhiteSpace(query?.Extension) ? ".as" : query.Extension;
            foreach (var file in Directory.EnumerateFiles(root, "*" + extension, SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return new FileSource(root, file, query.Encoding ?? _encoding);
            }
        }
    }

}
