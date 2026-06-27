using AuroraScript.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace AuroraScript.LanguageServices.Workspace;

public sealed class AuroraWorkspaceSnapshot
{
    private readonly Dictionary<string, AuroraWorkspaceDocument> _documents;

    public AuroraWorkspaceSnapshot(IEnumerable<AuroraWorkspaceDocument> documents)
        : this(documents, null, version: 0)
    {
    }

    public AuroraWorkspaceSnapshot(IEnumerable<AuroraWorkspaceDocument> documents, string? baseDirectory)
        : this(documents, baseDirectory, version: 0)
    {
    }

    public AuroraWorkspaceSnapshot(IEnumerable<AuroraWorkspaceDocument> documents, string? baseDirectory, long version)
    {
        Version = version;
        BaseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? Directory.GetCurrentDirectory()
            : ScriptPath.NormalizeBaseDirectory(baseDirectory);
        _documents = new Dictionary<string, AuroraWorkspaceDocument>(PathComparer);
        if (documents == null)
        {
            return;
        }

        foreach (var document in documents)
        {
            if (document == null)
            {
                continue;
            }

            var normalized = NormalizePath(document.Path, BaseDirectory);
            _documents[normalized] = new AuroraWorkspaceDocument(
                normalized,
                document.Text,
                document.Version);
        }
    }

    public string BaseDirectory { get; }

    public long Version { get; }

    public IReadOnlyDictionary<string, AuroraWorkspaceDocument> Documents => _documents;

    internal static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    internal bool TryGetDocument(string path, out AuroraWorkspaceDocument document)
    {
        return _documents.TryGetValue(NormalizePath(path, BaseDirectory), out document!);
    }

    internal static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return ScriptPath.NormalizeFullPath(path);
    }

    internal static string NormalizePath(string path, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return ScriptPath.GetFullPath(baseDirectory, path);
    }
}
