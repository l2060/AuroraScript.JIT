using AuroraScript.Compiler;
using AuroraScript.LanguageServices.Text;

namespace AuroraScript.LanguageServices.Internal;

internal static class SourceSpanExtensions
{
    public static bool IsValid(this SourceSpan span)
    {
        return span.StartLine > 0 && span.EndLine > 0;
    }

    public static bool Contains(this SourceSpan span, TextPosition position)
    {
        if (!span.IsValid())
        {
            return false;
        }

        var line = position.Line + 1;
        var column = position.Character + 1;
        if (line < span.StartLine || line > span.EndLine)
        {
            return false;
        }

        if (line == span.StartLine && column < span.StartColumn)
        {
            return false;
        }

        if (line == span.EndLine && column > span.EndColumn)
        {
            return false;
        }

        return true;
    }

    public static bool ContainsOffset(this SourceSpan span, int offset)
    {
        return span.Offset >= 0 && offset >= span.Offset && offset <= span.Offset + span.Length;
    }
}
