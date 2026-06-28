using AuroraScript.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.Source
{

    /// <summary>
    /// Backward-compatible alias for the default file-system resolver.
    /// </summary>
    public sealed class FileScriptSourceResolver : IScriptSourceResolver
    {
        /// <summary>
        /// Gets the shared file-system resolver instance.
        /// </summary>
        public static readonly FileScriptSourceResolver Instance = new FileScriptSourceResolver();

        private readonly FileSystemScriptSourceResolver _inner;

        private FileScriptSourceResolver()
        {
            _inner = new FileSystemScriptSourceResolver();
        }

        /// <inheritdoc />
        public string Root => _inner.Root;

        /// <inheritdoc />
        public ValueTask<ScriptSourceReference?> ResolveAsync(
            ScriptSourceReference? importer,
            string requestedPath,
            ScriptResolveContext context,
            CancellationToken cancellationToken = default)
        {
            return _inner.ResolveAsync(importer, requestedPath, context, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask<ScriptSource> GetSourceAsync(
            ScriptSourceReference reference,
            CancellationToken cancellationToken = default)
        {
            return _inner.GetSourceAsync(reference, cancellationToken);
        }

        /// <inheritdoc />
        public IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
            ScriptSourceQuery query,
            CancellationToken cancellationToken = default)
        {
            return _inner.GetAllSourcesAsync(query, cancellationToken);
        }
    }

}
