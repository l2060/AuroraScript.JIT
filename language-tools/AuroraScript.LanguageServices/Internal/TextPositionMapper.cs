using AuroraScript.LanguageServices.Text;

namespace AuroraScript.LanguageServices.Internal;

internal static class TextPositionMapper
{
    public static int ToOffset(string text, TextPosition position)
    {
        var targetLine = position.Line;
        var targetCharacter = position.Character;
        var line = 0;
        var character = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (line == targetLine && character == targetCharacter)
            {
                return i;
            }

            var ch = text[i];
            if (ch == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }
                line++;
                character = 0;
                continue;
            }

            if (ch == '\n')
            {
                line++;
                character = 0;
                continue;
            }

            character++;
        }

        return text.Length;
    }
}
