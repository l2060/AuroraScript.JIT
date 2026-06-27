using AuroraScript.LanguageServices.Text;

namespace AuroraScript.LanguageServices.Internal;

internal static class LightweightCompletionQuery
{
    public static bool TryGetMemberOwner(string sourceText, TextPosition position, out string ownerName)
    {
        ownerName = string.Empty;
        var offset = TextPositionMapper.ToOffset(sourceText, position);
        var i = offset - 1;

        while (i >= 0 && char.IsWhiteSpace(sourceText[i]))
        {
            i--;
        }

        if (i < 0 || sourceText[i] != '.')
        {
            return false;
        }

        i--;
        while (i >= 0 && char.IsWhiteSpace(sourceText[i]))
        {
            i--;
        }

        var end = i + 1;
        while (i >= 0 && IsIdentifierPart(sourceText[i]))
        {
            i--;
        }

        var start = i + 1;
        if (start >= end || !IsIdentifierStart(sourceText[start]))
        {
            return false;
        }

        ownerName = sourceText.Substring(start, end - start);
        return true;
    }

    private static bool IsIdentifierStart(char ch)
    {
        return ch == '_' || char.IsLetter(ch);
    }

    private static bool IsIdentifierPart(char ch)
    {
        return ch == '_' || char.IsLetterOrDigit(ch);
    }
}
