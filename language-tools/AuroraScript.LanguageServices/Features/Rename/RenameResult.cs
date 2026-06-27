using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Features.Rename;

public sealed class RenameResult
{
    private RenameResult(bool success, string? errorMessage, IReadOnlyList<WorkspaceTextEdit> changes)
    {
        Success = success;
        ErrorMessage = errorMessage;
        Changes = changes ?? Array.Empty<WorkspaceTextEdit>();
    }

    public bool Success { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyList<WorkspaceTextEdit> Changes { get; }

    public static RenameResult Ok(IReadOnlyList<WorkspaceTextEdit> changes)
    {
        return new RenameResult(true, null, changes);
    }

    public static RenameResult Fail(string message)
    {
        return new RenameResult(false, string.IsNullOrWhiteSpace(message) ? "Rename failed." : message, Array.Empty<WorkspaceTextEdit>());
    }
}
