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

            if (ScriptPath.Comparer.Equals(importer.Value.BaseDirectory, baseDirectory))
            {
                return importer.Value.FullPath;
            }

            return ScriptPath.GetFullPath(baseDirectory, importer.Value.ModulePath);
        }

        /// <inheritdoc />
        public ValueTask<ScriptSource> GetSourceAsync(
            ScriptSourceReference reference,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
