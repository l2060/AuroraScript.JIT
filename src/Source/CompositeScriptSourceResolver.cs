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
    /// Composes multiple resolvers. Earlier resolvers have higher priority.
    /// </summary>
    public sealed class CompositeScriptSourceResolver : IScriptSourceResolver
    {
        private readonly List<IScriptSourceResolver> _resolvers = new();

        /// <inheritdoc />
        public string Root => _resolvers.Count == 0 ? string.Empty : _resolvers[0].Root;

        /// <summary>
        /// Adds a resolver to the composition.
        /// </summary>
        public CompositeScriptSourceResolver Add(IScriptSourceResolver resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _resolvers.Add(resolver);
            return this;
        }

        /// <inheritdoc />
        public async ValueTask<ScriptSourceReference?> ResolveAsync(
            ScriptSourceReference? importer,
            string requestedPath,
            ScriptResolveContext context,
            CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < _resolvers.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var resolver = _resolvers[i];
                var resolved = await resolver.ResolveAsync(importer, requestedPath, context, cancellationToken).ConfigureAwait(false);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public async ValueTask<ScriptSource> GetSourceAsync(
            ScriptSourceReference reference,
            CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < _resolvers.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var resolver = _resolvers[i];
                try
                {
                    return await resolver.GetSourceAsync(reference, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsSourceReadFailure(ex))
                {
                }
            }

            throw new FileNotFoundException("Script source not found.", reference.FullPath);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
            ScriptSourceQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var seen = new HashSet<string>(ScriptPath.Comparer);
            for (var i = 0; i < _resolvers.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await foreach (var source in _resolvers[i].GetAllSourcesAsync(query, cancellationToken).ConfigureAwait(false))
                {
                    if (seen.Add(source.SourcePath))
                    {
                        yield return source;
                    }
                }
            }
        }

        private static bool IsSourceReadFailure(Exception exception)
        {
            return exception is FileNotFoundException
                or DirectoryNotFoundException
                or IOException
                or UnauthorizedAccessException
                or ArgumentException
                or NotSupportedException
                or KeyNotFoundException;
        }
    }
}
