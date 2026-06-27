namespace AuroraScript.LanguageServices.Features.Definition;

public sealed class BuiltinDocument
{
    public BuiltinDocument(string uri, string text)
    {
        Uri = uri ?? string.Empty;
        Text = text ?? string.Empty;
    }

    public string Uri { get; }
    public string Text { get; }
    public string LanguageId => "aurora";
}
