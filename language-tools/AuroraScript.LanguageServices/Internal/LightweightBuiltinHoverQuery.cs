using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.Hover;
using AuroraScript.LanguageServices.Text;
using System;
using System.Text.RegularExpressions;

namespace AuroraScript.LanguageServices.Internal;

internal static class LightweightBuiltinHoverQuery
{
    private const string IdentifierBoundary = @"[$_\p{L}\p{Nd}]";

    public static bool TryResolve(
        BuiltinApiCatalog builtins,
        string sourceText,
        TextPosition position,
        string? locale,
        out HoverResult hover)
    {
        hover = null!;
        if (!TryGetIdentifierAtPosition(sourceText, position, out var token) ||
            token.Start >= sourceText.Length)
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
            if (!IsTextuallyDeclaredName(maskedSource, ownerName) &&
                builtins.TryGetGlobalMember(ownerName, token.Value, out var member))
            {
                hover = new HoverResult(BuiltinFormat.FormatMember(member, locale), RangeFromOffsets(sourceText, token.Start, token.End));
                return true;
            }

            return false;
        }

        if (TryReadIdentifierAfterDot(maskedSource, token.End, out _) &&
            !IsTextuallyDeclaredName(maskedSource, token.Value) &&
            builtins.TryGetGlobal(token.Value, out var owner))
        {
            hover = new HoverResult(BuiltinFormat.FormatGlobal(owner, locale), RangeFromOffsets(sourceText, token.Start, token.End));
            return true;
        }

        if (!IsTextuallyDeclaredName(maskedSource, token.Value) &&
            builtins.TryGetGlobal(token.Value, out var global))
        {
            hover = new HoverResult(BuiltinFormat.FormatGlobal(global, locale), RangeFromOffsets(sourceText, token.Start, token.End));
            return true;
        }

        return false;
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

    private static bool TryGetIdentifierAtPosition(string sourceText, TextPosition position, out IdentifierToken token)
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

    private static bool TryReadIdentifierAfterDot(string sourceText, int tokenEnd, out string identifier)
    {
        identifier = string.Empty;
        var index = tokenEnd;
        while (index < sourceText.Length && char.IsWhiteSpace(sourceText[index]))
        {
            index++;
        }

        if (index >= sourceText.Length || sourceText[index] != '.')
        {
            return false;
        }

        index++;
        while (index < sourceText.Length && char.IsWhiteSpace(sourceText[index]))
        {
            index++;
        }

        var start = index;
        if (start >= sourceText.Length || !IsIdentifierStart(sourceText[start]))
        {
            return false;
        }

        index++;
        while (index < sourceText.Length && IsIdentifierPart(sourceText[index]))
        {
            index++;
        }

        identifier = sourceText.Substring(start, index - start);
        return true;
    }

    private static TextRange RangeFromOffsets(string sourceText, int start, int end)
    {
        return new TextRange(string.Empty, PositionAtOffset(sourceText, start), PositionAtOffset(sourceText, end));
    }

    private static TextPosition PositionAtOffset(string sourceText, int offset)
    {
        var line = 0;
        var character = 0;
        for (var i = 0; i < offset && i < sourceText.Length; i++)
        {
            if (sourceText[i] == '\r')
            {
                if (i + 1 < offset && i + 1 < sourceText.Length && sourceText[i + 1] == '\n')
                {
                    i++;
                }
                line++;
                character = 0;
            }
            else if (sourceText[i] == '\n')
            {
                line++;
                character = 0;
            }
            else
            {
                character++;
            }
        }

        return new TextPosition(line, character);
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
