using AuroraScript.Compiler;
using AuroraScript.LanguageServices.Features.References;
using AuroraScript.LanguageServices.Features.Rename;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal static class AuroraRenameResolver
{
    public static RenameResult Rename(
        AuroraWorkspaceIndex index,
        string path,
        TextPosition position,
        string newName)
    {
        if (!IsValidIdentifier(newName))
        {
            return RenameResult.Fail($"Invalid identifier '{newName}'.");
        }

        var module = index.TryGetModule(path);
        if (module == null)
        {
            return RenameResult.Fail("Document is not available in the workspace index.");
        }

        var localIndex = AuroraLocalSymbolIndex.Build(module);
        if (localIndex.TryGetRenameReferences(position, out var localReferences))
        {
            return RenameResult.Ok(ToWorkspaceEdits(localReferences, newName));
        }

        var moduleReferences = AuroraReferenceResolver.Resolve(index, path, position, includeDeclaration: true);
        if (moduleReferences.Count == 0)
        {
            return RenameResult.Fail("No renameable symbol found at the requested position.");
        }

        return RenameResult.Ok(ToWorkspaceEdits(moduleReferences, newName));
    }

    private static IReadOnlyList<WorkspaceTextEdit> ToWorkspaceEdits(IReadOnlyList<ReferenceLocation> references, string newName)
    {
        var changes = new Dictionary<string, List<TextEdit>>(PathComparer);
        var seen = new HashSet<EditKey>();
        for (var i = 0; i < references.Count; i++)
        {
            var reference = references[i];
            if (!seen.Add(new EditKey(reference.Path, reference.Range)))
            {
                continue;
            }

            if (!changes.TryGetValue(reference.Path, out var edits))
            {
                edits = new List<TextEdit>();
                changes.Add(reference.Path, edits);
            }

            edits.Add(new TextEdit(reference.Range, newName));
        }

        var result = new List<WorkspaceTextEdit>(changes.Count);
        foreach (var pair in changes)
        {
            result.Add(new WorkspaceTextEdit(pair.Key, pair.Value));
        }

        return result;
    }

    private static bool IsValidIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value) ||
            !IsIdentifierStart(value[0]) ||
            Symbols.FromString(value) != null)
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            if (!IsIdentifierPart(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsIdentifierStart(char c)
    {
        return (c >= 'a' && c <= 'z') ||
               (c >= 'A' && c <= 'Z') ||
               c == '_' ||
               c == '$' ||
               (c >= 0x4e00 && c <= 0x9fbb);
    }

    private static bool IsIdentifierPart(char c)
    {
        return IsIdentifierStart(c) || (c >= '0' && c <= '9');
    }

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private readonly record struct EditKey(string Path, TextRange Range);
}
