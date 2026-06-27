using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Features.Rename;

public sealed class WorkspaceTextEdit
{
    public WorkspaceTextEdit(string path, IReadOnlyList<TextEdit> edits)
    {
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Path is required.", nameof(path)) : path;
        Edits = edits ?? Array.Empty<TextEdit>();
    }

    public string Path { get; }
    public IReadOnlyList<TextEdit> Edits { get; }
}
