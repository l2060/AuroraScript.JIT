namespace AuroraScript.LanguageServices.Features.Completion;

public sealed class CompletionItem
{
    public CompletionItem(string label, CompletionItemKind kind, string? detail, string? documentation, bool readOnly)
    {
        Label = label;
        Kind = kind;
        Detail = detail ?? string.Empty;
        Documentation = documentation ?? string.Empty;
        ReadOnly = readOnly;
    }

    public string Label { get; }
    public CompletionItemKind Kind { get; }
    public string Detail { get; }
    public string Documentation { get; }
    public bool ReadOnly { get; }
}
