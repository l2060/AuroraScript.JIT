using AuroraScript.Core;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.Source
{
    /// <summary>
    /// Context used when resolving import/include paths.
    /// </summary>
    public sealed class ScriptResolveContext
    {
        /// <summary>
        /// Initializes a new resolve context.
        /// </summary>
        public ScriptResolveContext(string extension, Encoding encoding = null)
        {
            Extension = NormalizeExtension(extension);
            Encoding = encoding ?? Encoding.UTF8;
        }

        /// <summary>
        /// Gets the configured script file extension.
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// Gets the preferred encoding for sources that need text decoding.
        /// </summary>
        public Encoding Encoding { get; }

        internal static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return string.Empty;
            }

            return extension[0] == '.' ? extension : "." + extension;
        }
    }

    /// <summary>
    /// Query options for enumerating script sources.
    /// </summary>
    public sealed class ScriptSourceQuery
    {
        /// <summary>
        /// Initializes a source query.
        /// </summary>
        public ScriptSourceQuery(string extension, Encoding encoding = null)
        {
            Extension = ScriptResolveContext.NormalizeExtension(extension);
            Encoding = encoding ?? Encoding.UTF8;
        }

        /// <summary>
        /// Gets the script extension to enumerate.
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// Gets the preferred encoding for file-based sources.
        /// </summary>
        public Encoding Encoding { get; }
    }

    /// <summary>
    /// Resolves script imports and provides script sources from a backing store such
    /// as memory, a file system, embedded resources, a database, or a virtual file system.
    /// </summary>
    public interface IScriptSourceResolver
    {
        /// <summary>
        /// Gets the source root represented by this resolver.
        /// </summary>
        string Root { get; }

        /// <summary>
        /// Resolves an import/include path into a stable source reference.
        /// </summary>
        ValueTask<ScriptSourceReference?> ResolveAsync(
            ScriptSourceReference? importer,
            string requestedPath,
            ScriptResolveContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the source text for a previously resolved reference.
        /// </summary>
        ValueTask<ScriptSource> GetSourceAsync(
            ScriptSourceReference reference,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Enumerates all sources visible to this resolver.
        /// </summary>
        IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
            ScriptSourceQuery query,
            CancellationToken cancellationToken = default);
    }



}
