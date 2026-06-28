using AuroraScript.Core;
using AuroraScript.Source;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.LanguageServices.Workspace;

internal sealed class WorkspaceScriptSourceResolver : IScriptSourceResolver
{
    private readonly AuroraWorkspaceSnapshot _snapshot;
    private readonly IScriptSourceResolver _fallback;

    public WorkspaceScriptSourceResolver(AuroraWorkspaceSnapshot snapshot, IScriptSourceResolver fallback)
    {
        _snapshot = snapshot ?? new AuroraWorkspaceSnapshot(Array.Empty<AuroraWorkspaceDocument>());
        _fallback = fallback == null || ReferenceEquals(fallback, FileScriptSourceResolver.Instance)
            ? ScriptSources.FileSystem(_snapshot.BaseDirectory)
            : fallback;
    }

    public string Root => _snapshot.BaseDirectory;

    public async ValueTask<ScriptSourceReference?> ResolveAsync(
        ScriptSourceReference? importer,
        string requestedPath,
        ScriptResolveContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentPath = ResolveCurrentPath(importer);
        var currentDirectory = importer == null ? _snapshot.BaseDirectory : ScriptPath.GetDirectoryName(currentPath);
        var fullPath = ScriptPath.EnsureExtension(ScriptPath.Combine(currentDirectory, requestedPath), context?.Extension);
        if (_snapshot.TryGetDocument(fullPath, out _))
        {
            return new ScriptSourceReference(_snapshot.BaseDirectory, fullPath, ScriptPath.GetModulePath(_snapshot.BaseDirectory, fullPath));
        }

        return await _fallback.ResolveAsync(importer, requestedPath, context, cancellationToken).ConfigureAwait(false);
    }

    private string ResolveCurrentPath(ScriptSourceReference? importer)
    {
        if (importer == null)
        {
            return _snapshot.BaseDirectory;
        }

        return importer.Value.FullPath;
    }

    public async ValueTask<ScriptSource> GetSourceAsync(
        ScriptSourceReference reference,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ScriptPath.NormalizedRootsEqual(reference.BaseDirectory, _snapshot.BaseDirectory) &&
            _snapshot.TryGetDocument(reference.FullPath, out var document))
        {
            return new MemorySource(reference.BaseDirectory, document.Path, document.Text);
        }

        return await _fallback.GetSourceAsync(reference, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
        ScriptSourceQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var seen = new HashSet<string>(ScriptPath.Comparer);
        foreach (var document in _snapshot.Documents.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (seen.Add(document.Path))
            {
                yield return new MemorySource(_snapshot.BaseDirectory, document.Path, document.Text);
            }
        }

        await foreach (var source in _fallback.GetAllSourcesAsync(query, cancellationToken).ConfigureAwait(false))
        {
            if (seen.Add(source.FullPath))
            {
                yield return source;
            }
        }
    }
}
