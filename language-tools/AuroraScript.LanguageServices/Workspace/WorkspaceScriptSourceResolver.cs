using AuroraScript.Core;
using System;
using System.IO;
using System.Text;

namespace AuroraScript.LanguageServices.Workspace;

internal sealed class WorkspaceScriptSourceResolver : IScriptSourceResolver
{
    private readonly AuroraWorkspaceSnapshot _snapshot;
    private readonly IScriptSourceResolver _fallback;

    public WorkspaceScriptSourceResolver(AuroraWorkspaceSnapshot snapshot, IScriptSourceResolver fallback)
    {
        _snapshot = snapshot ?? new AuroraWorkspaceSnapshot(Array.Empty<AuroraWorkspaceDocument>());
        _fallback = fallback ?? FileScriptSourceResolver.Instance;
    }

    public bool TryResolve(
        string baseDirectory,
        string currentSourcePath,
        string requestedPath,
        string extension,
        out ScriptSourceReference source)
    {
        var currentDirectory = ScriptPath.GetDirectoryName(currentSourcePath);
        var fullPath = ScriptPath.EnsureExtension(ScriptPath.Combine(currentDirectory, requestedPath), extension);
        if (_snapshot.TryGetDocument(fullPath, out _))
        {
            source = new ScriptSourceReference(baseDirectory, fullPath);
            return true;
        }

        if (_fallback.TryResolve(baseDirectory, currentSourcePath, requestedPath, extension, out var fallbackSource))
        {
            source = fallbackSource;
            return true;
        }

        source = default;
        return false;
    }

    public ScriptSource Open(ScriptSourceReference source, Encoding encoding)
    {
        if (_snapshot.TryGetDocument(source.FullPath, out var document))
        {
            return new MemoryScriptSource(source.BaseDirectory, document.Path, document.Text);
        }

        return _fallback.Open(source, encoding);
    }
}
