using AuroraScript.LanguageServices.Features.Definition;
using AuroraScript.LanguageServices.Text;
using System;
using System.Text.RegularExpressions;

namespace AuroraScript.LanguageServices.Internal;

internal static class LightweightBuiltinDefinitionQuery
{
    private const string IdentifierBoundary = @"[$_\p{L}\p{Nd}]";

    public static bool TryResolve(
        BuiltinDefinitionDocuments builtinDocuments,
        string sourceText,
        TextPosition position,
        out DefinitionLocation location)
    {
        location = null!;
        if (builtinDocuments == null ||
            !TryGetIdentifierAtPosition(sourceText, position, out var token))
        {
            return false;
        }

        var maskedSource = MaskCommentsAndStrings(sourceText);
        if (token.Start >= maskedSource.Length ||
            maskedSource[token.Start] == ' ')
        {
            return false;
        }

        if (TryReadIdentifierBeforeDot(maskedSource, token.Start, out var ownerName))
        {
            return !IsTextuallyDeclaredName(maskedSource, ownerName) &&
                builtinDocuments.TryGetMemberLocation(ownerName, token.Value, out location);
        }

        if (!IsTextuallyDeclaredName(maskedSource, token.Value) &&
            builtinDocuments.TryGetGlobalLocation(token.Value, out location))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetIdentifierAtPosition(
        string sourceText,
        TextPosition position,
        out IdentifierToken token)
    {
        token = default;
        if (string.IsNullOrEmpty(sourceText))
        {
            return false;
        }

        var offset = TextPositionMapper.ToOffset(sourceText, position);
        if (offset >= sourceText.Length)
        {
            offset = sourceText.Length - 1;
        }

        if (offset > 0 && !IsIdentifierPart(sourceText[offset]) && IsIdentifierPart(sourceText[offset - 1]))
        {
            offset--;
        }

        if (offset < 0 || offset >= sourceText.Length || !IsIdentifierPart(sourceText[offset]))
        {
            return false;
        }

        var start = offset;
        while (start > 0 && IsIdentifierPart(sourceText[start - 1]))
        {
            start--;
        }

        if (!IsIdentifierStart(sourceText[start]))
        {
            return false;
        }

        var end = offset + 1;
        while (end < sourceText.Length && IsIdentifierPart(sourceText[end]))
        {
            end++;
        }

        token = new IdentifierToken(sourceText.Substring(start, end - start), start, end);
        return true;
    }

    private static bool TryReadIdentifierBeforeDot(string sourceText, int tokenStart, out string identifier)
    {
        identifier = string.Empty;
        var index = tokenStart - 1;
        while (index >= 0 && char.IsWhiteSpace(sourceText[index]))
        {
            index--;
        }

        if (index < 0 || sourceText[index] != '.')
        {
            return false;
        }

        index--;
        while (index >= 0 && char.IsWhiteSpace(sourceText[index]))
        {
            index--;
        }

        var end = index + 1;
        while (index >= 0 && IsIdentifierPart(sourceText[index]))
        {
            index--;
        }

        var start = index + 1;
        if (start >= end || !IsIdentifierStart(sourceText[start]))
        {
            return false;
        }

        identifier = sourceText.Substring(start, end - start);
        return true;
    }

    private static bool IsTextuallyDeclaredName(string maskedSource, string name)
    {
        if (string.IsNullOrWhiteSpace(maskedSource) || string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var escapedName = Regex.Escape(name);
        return Regex.IsMatch(
                maskedSource,
                $@"(?<!{IdentifierBoundary})(?:var|const|function|func|import)\s+{escapedName}(?!{IdentifierBoundary})",
                RegexOptions.CultureInvariant) ||
            Regex.IsMatch(
                maskedSource,
                $@"(?<!{IdentifierBoundary})(?:function|func)\s+[$_\p{{L}}][$_\p{{L}}\p{{Nd}}]*\s*\([^)]*(?<!{IdentifierBoundary}){escapedName}(?!{IdentifierBoundary})",
                RegexOptions.CultureInvariant);
    }

    private static string MaskCommentsAndStrings(string sourceText)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            return string.Empty;
        }

        var chars = sourceText.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var current = chars[i];
            if (current == '/' && i + 1 < chars.Length && chars[i + 1] == '/')
            {
                MaskUntilLineEnd(chars, ref i);
                continue;
            }

            if (current == '/' && i + 1 < chars.Length && chars[i + 1] == '*')
            {
                MaskBlockComment(chars, ref i);
                continue;
            }

            if (current == '"' || current == '\'' || current == '`')
            {
                MaskQuotedString(chars, ref i, current);
            }
        }

        return new string(chars);
    }

    private static void MaskUntilLineEnd(char[] chars, ref int index)
    {
        while (index < chars.Length && chars[index] != '\r' && chars[index] != '\n')
        {
            chars[index++] = ' ';
        }

        index--;
    }

    private static void MaskBlockComment(char[] chars, ref int index)
    {
        chars[index++] = ' ';
        chars[index++] = ' ';
        while (index < chars.Length)
        {
            var current = chars[index];
            if (current == '*' && index + 1 < chars.Length && chars[index + 1] == '/')
            {
                chars[index++] = ' ';
                chars[index] = ' ';
                return;
            }

            if (current != '\r' && current != '\n')
            {
                chars[index] = ' ';
            }

            index++;
        }

        index--;
    }

    private static void MaskQuotedString(char[] chars, ref int index, char quote)
    {
        chars[index++] = ' ';
        while (index < chars.Length)
        {
            var current = chars[index];
            if (current == '\\')
            {
                chars[index++] = ' ';
                if (index < chars.Length && chars[index] != '\r' && chars[index] != '\n')
                {
                    chars[index] = ' ';
                }
            }
            else if (current == quote)
            {
                chars[index] = ' ';
                return;
            }
            else if (current == '\r' || current == '\n')
            {
                return;
            }
            else
            {
                chars[index] = ' ';
            }

            index++;
        }

        index--;
    }

    private static bool IsIdentifierStart(char value)
    {
        return value == '_' || value == '$' || char.IsLetter(value);
    }

    private static bool IsIdentifierPart(char value)
    {
        return IsIdentifierStart(value) || char.IsDigit(value);
    }

    private readonly record struct IdentifierToken(string Value, int Start, int End);
}
