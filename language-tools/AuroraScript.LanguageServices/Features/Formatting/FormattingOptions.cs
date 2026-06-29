namespace AuroraScript.LanguageServices.Features.Formatting;

public sealed class FormattingOptions
{
    public FormattingOptions(int tabSize = 4, bool insertSpaces = true)
    {
        TabSize = tabSize <= 0 ? 4 : tabSize;
        InsertSpaces = insertSpaces;
    }

    public int TabSize { get; }
    public bool InsertSpaces { get; }
}
