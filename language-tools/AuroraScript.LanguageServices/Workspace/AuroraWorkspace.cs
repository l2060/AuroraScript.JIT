using AuroraScript.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace AuroraScript.LanguageServices.Workspace;

public sealed class AuroraWorkspace
{
    private readonly Dictionary<string, AuroraWorkspaceDocument> _documents =
        new(AuroraWorkspaceSnapshot.PathComparer);
    private long _version;

    public AuroraWorkspace()
        : this(Directory.GetCurrentDirectory())
    {
    }

    public AuroraWorkspace(string baseDirectory)
    {
        BaseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? Directory.GetCurrentDirectory()
            : ScriptPath.NormalizeBaseDirectory(baseDirectory);
    }

    public string BaseDirectory { get; }

    public long Version => _version;

    public IReadOnlyCollection<AuroraWorkspaceDocument> Documents => _documents.Values;

    public void OpenOrUpdate(string path, string text, int version = 0)
    {
        var normalized = NormalizePath(path);
        var normalizedText = text ?? string.Empty;
        if (_documents.TryGetValue(normalized, out var existing) &&
            existing.Version == version &&
            string.Equals(existing.Text, normalizedText, StringComparison.Ordinal))
        {
            return;
        }

        _documents[normalized] = new AuroraWorkspaceDocument(normalized, normalizedText, version);
        _version++;
    }

    public void Close(string path)
    {
        if (_documents.Remove(NormalizePath(path)))
        {
            _version++;
        }
    }

    public bool TryGetDocument(string path, out AuroraWorkspaceDocument document)
    {
        return _documents.TryGetValue(NormalizePath(path), out document!);
    }

    public AuroraWorkspaceSnapshot CreateSnapshot()
    {
        return new AuroraWorkspaceSnapshot(_documents.Values, BaseDirectory, _version);
    }

    public AuroraWorkspace Rebase(string baseDirectory)
    {
        var rebased = new AuroraWorkspace(baseDirectory);
        foreach (var document in _documents.Values)
        {
            rebased.OpenOrUpdate(document.Path, document.Text, document.Version);
        }

        return rebased;
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Document path is required.", nameof(path));
        }

        return ScriptPath.GetFullPath(BaseDirectory, path);
    }
}
