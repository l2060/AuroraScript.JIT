using AuroraScript.Core;
using AuroraScript.Runtime.Package;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.Source
{
    /// <summary>
    /// Adds the engine's built-in module namespace to another source resolver.
    /// </summary>
    /// <remarks>
    /// Bare names registered in <see cref="BuiltinModuleRegistry"/> are resolved from
    /// the <c>builtin://</c> namespace. Relative imports are delegated to the wrapped
    /// resolver, so a project can still import <c>./fs</c> or <c>../fs</c> as files.
    /// </remarks>
    internal sealed class BuiltinScriptSourceResolver : IScriptSourceResolver
    {
        private readonly IScriptSourceResolver _inner;
        private readonly BuiltinModuleRegistry _registry;

        internal BuiltinScriptSourceResolver(
            IScriptSourceResolver inner,
            BuiltinModuleRegistry registry)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
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
            cancellationToken.ThrowIfCancellationRequested();

            if (_registry.TryResolve(requestedPath, out var builtin))
            {
                return new ValueTask<ScriptSourceReference?>(builtin.Reference);
            }

            return _inner.ResolveAsync(importer, requestedPath, context, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask<ScriptSource> GetSourceAsync(
            ScriptSourceReference reference,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_registry.TryGet(reference, out var builtin))
            {
                return new ValueTask<ScriptSource>(new MemorySource(
                    builtin.Reference.BaseDirectory,
                    builtin.Reference.FullPath,
                    builtin.Source));
            }

            return _inner.GetSourceAsync(reference, cancellationToken);
        }

        /// <inheritdoc />
        public IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
            ScriptSourceQuery query,
            CancellationToken cancellationToken = default)
        {
            // Built-ins are dependencies, not project entry points.
            return _inner.GetAllSourcesAsync(query, cancellationToken);
        }
    }
}
