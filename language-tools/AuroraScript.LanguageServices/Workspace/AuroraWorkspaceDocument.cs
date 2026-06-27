using System;

namespace AuroraScript.LanguageServices.Workspace;

public sealed class AuroraWorkspaceDocument
{
    public AuroraWorkspaceDocument(string path, string text)
        : this(path, text, version: 0)
    {
    }

    public AuroraWorkspaceDocument(string path, string text, int version)
    {
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Document path is required.", nameof(path)) : path;
        Text = text ?? string.Empty;
        Version = version;
    }

    public string Path { get; }
    public string Text { get; }
    public int Version { get; }
}
