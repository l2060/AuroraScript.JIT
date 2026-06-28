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
        /// Gets the normalized script file extension used when the requested path omits one.
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
        /// Gets the normalized script extension to enumerate.
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
    /// <remarks>
    /// A resolver owns a stable source namespace rooted at <see cref="Root"/>.
    /// Implementations should normalize roots and stored source keys up front and use '/'
    /// separators internally, including for file-system paths. The parser keeps the raw
    /// import/include text unchanged; module graph construction calls
    /// <see cref="ResolveAsync"/> and then <see cref="GetSourceAsync"/> for the returned
    /// reference.
    /// </remarks>
    public interface IScriptSourceResolver
    {
        /// <summary>
        /// Gets the normalized source root represented by this resolver.
        /// </summary>
        /// <remarks>
        /// The root is part of the resolver identity. A returned
        /// <see cref="ScriptSourceReference.BaseDirectory"/> must identify the resolver
        /// root that can later read the reference.
        /// </remarks>
        string Root { get; }

        /// <summary>
        /// Resolves an import/include path into a stable source reference.
        /// </summary>
        /// <remarks>
        /// <paramref name="requestedPath"/> is the raw path written in the script. When
        /// <paramref name="importer"/> is <c>null</c>, resolve it as an entry path from
        /// <see cref="Root"/>. Otherwise resolve it relative to
        /// <see cref="ScriptSourceReference.FullPath"/> of the importer. Return
        /// <c>null</c> if the source does not exist or the resolved target is outside the
        /// resolver namespace. Resolvers that intentionally bridge to an external store
        /// may still return a reference whose <see cref="ScriptSourceReference.BaseDirectory"/>
        /// is their own <see cref="Root"/> so <see cref="GetSourceAsync"/> can route it
        /// back to the same resolver.
        /// </remarks>
        ValueTask<ScriptSourceReference?> ResolveAsync(
            ScriptSourceReference? importer,
            string requestedPath,
            ScriptResolveContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the source text for a previously resolved reference.
        /// </summary>
        /// <remarks>
        /// Implementations should only read references produced for their own root. In a
        /// composite resolver, <see cref="ScriptSourceReference.BaseDirectory"/> is used
        /// as the routing key before source text is requested.
        /// </remarks>
        ValueTask<ScriptSource> GetSourceAsync(
            ScriptSourceReference reference,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Enumerates all sources visible to this resolver.
        /// </summary>
        /// <remarks>
        /// This method defines what <c>BuildAsync()</c> compiles when no explicit entry
        /// path is provided. Returned sources should use normalized full paths so a
        /// composite resolver can de-duplicate overlays by source identity.
        /// </remarks>
        IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
            ScriptSourceQuery query,
            CancellationToken cancellationToken = default);
    }



}
