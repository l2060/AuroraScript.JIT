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
    /// <remarks>
    /// Resolution is ordered: each resolver receives the original importer and requested
    /// path, and the first non-null reference wins. Source reads are routed by exact
    /// normalized <see cref="ScriptSourceReference.BaseDirectory"/> match, not by scanning
    /// target paths. <c>GetAllSourcesAsync</c> de-duplicates by normalized full path, so
    /// sources from earlier resolvers hide later sources with the same identity.
    /// </remarks>
    public sealed class CompositeScriptSourceResolver : IScriptSourceResolver
    {
        private readonly List<ResolverEntry> _resolvers = new();

        /// <inheritdoc />
        public string Root => _resolvers.Count == 0 ? string.Empty : _resolvers[0].Root;

        /// <summary>
        /// Adds a resolver to the composition.
        /// </summary>
        /// <remarks>
        /// The resolver root is normalized at add time so later comparisons do not need
        /// to re-normalize roots on every routing check.
        /// </remarks>
        public CompositeScriptSourceResolver Add(IScriptSourceResolver resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _resolvers.Add(new ResolverEntry(resolver));
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
                var resolver = _resolvers[i].Resolver;
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
                var entry = _resolvers[i];
                if (!ScriptPath.NormalizedRootsEqual(reference.BaseDirectory, entry.Root))
                {
                    continue;
                }

                try
                {
                    return await entry.Resolver.GetSourceAsync(reference, cancellationToken).ConfigureAwait(false);
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
            var seen = new HashSet<SourceIdentity>(SourceIdentityComparer.Instance);
            for (var i = 0; i < _resolvers.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await foreach (var source in _resolvers[i].Resolver.GetAllSourcesAsync(query, cancellationToken).ConfigureAwait(false))
                {
                    if (seen.Add(new SourceIdentity(source.FullPath)))
                    {
                        yield return source;
                    }
                }
            }
        }

        internal bool TryFindLongestRoot(string fullPath, out string root)
        {
            root = null;
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return false;
            }

            var bestLength = -1;
            for (var i = 0; i < _resolvers.Count; i++)
            {
                var candidate = _resolvers[i].Root;
                if (candidate.Length <= bestLength ||
                    !ScriptPath.IsWithinNormalizedRoot(candidate, fullPath))
                {
                    continue;
                }

                root = candidate;
                bestLength = candidate.Length;
            }

            return root != null;
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

        private readonly struct ResolverEntry
        {
            public ResolverEntry(IScriptSourceResolver resolver)
            {
                Resolver = resolver;
                Root = ScriptPath.NormalizeBaseDirectory(resolver.Root);
            }

            public IScriptSourceResolver Resolver { get; }

            public string Root { get; }
        }

        private readonly struct SourceIdentity
        {
            public SourceIdentity(string path)
            {
                Path = path ?? string.Empty;
            }

            public string Path { get; }
        }

        private sealed class SourceIdentityComparer : IEqualityComparer<SourceIdentity>
        {
            public static readonly SourceIdentityComparer Instance = new();

            public bool Equals(SourceIdentity x, SourceIdentity y)
            {
                return ScriptPath.Comparer.Equals(x.Path, y.Path);
            }

            public int GetHashCode(SourceIdentity obj)
            {
                unchecked
                {
                    return ScriptPath.Comparer.GetHashCode(obj.Path);
                }
            }
        }
    }
}
