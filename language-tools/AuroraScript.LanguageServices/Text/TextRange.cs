using AuroraScript.Compiler;

namespace AuroraScript.LanguageServices.Text;

public readonly record struct TextRange(string FileName, TextPosition Start, TextPosition End)
{
    public static TextRange FromSourceSpan(SourceSpan span)
    {
        var startLine = span.StartLine > 0 ? span.StartLine - 1 : 0;
        var startColumn = span.StartColumn > 0 ? span.StartColumn - 1 : 0;
        var endLine = span.EndLine > 0 ? span.EndLine - 1 : startLine;
        var endColumn = span.EndColumn > 0 ? span.EndColumn - 1 : startColumn;
        return new TextRange(
            span.FileName ?? string.Empty,
            new TextPosition(startLine, startColumn),
            new TextPosition(endLine, endColumn));
    }
}
