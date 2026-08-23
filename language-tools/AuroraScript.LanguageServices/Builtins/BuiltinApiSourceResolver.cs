using AuroraScript.Core;
using AuroraScript.Source;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.LanguageServices.Builtins;

/// <summary>
/// Resolves catalogued native modules for editor parsing without exposing them as
/// workspace entry files.
/// </summary>
internal sealed class BuiltinApiSourceResolver : IScriptSourceResolver
{
    private const string BuiltinRoot = "builtin://";

    private readonly BuiltinApiCatalog _catalog;
    private readonly IScriptSourceResolver _inner;

    public BuiltinApiSourceResolver(BuiltinApiCatalog catalog, IScriptSourceResolver inner)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public string Root => _inner.Root;

    public ValueTask<ScriptSourceReference?> ResolveAsync(
        ScriptSourceReference? importer,
        string requestedPath,
        ScriptResolveContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_catalog.TryGetModule(requestedPath, out var module))
        {
            return new ValueTask<ScriptSourceReference?>(CreateReference(module));
        }

        return _inner.ResolveAsync(importer, requestedPath, context, cancellationToken);
    }

    public ValueTask<ScriptSource> GetSourceAsync(
        ScriptSourceReference reference,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ScriptPath.NormalizedRootsEqual(reference.BaseDirectory, BuiltinRoot) &&
            _catalog.TryGetModule(reference.ModulePath, out var module))
        {
            return new ValueTask<ScriptSource>(new MemorySource(
                BuiltinRoot,
                BuiltinRoot + module.ModulePath,
                $"@module({module.Name});"));
        }

        return _inner.GetSourceAsync(reference, cancellationToken);
    }

    public IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
        ScriptSourceQuery query,
        CancellationToken cancellationToken = default)
    {
        return _inner.GetAllSourcesAsync(query, cancellationToken);
    }

    private static ScriptSourceReference CreateReference(BuiltinApiModule module)
    {
        return new ScriptSourceReference(
            BuiltinRoot,
            BuiltinRoot + module.ModulePath,
            module.ModulePath);
    }
}
