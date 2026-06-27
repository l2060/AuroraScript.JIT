namespace AuroraScript.LanguageServices.Features.SignatureHelp;

public sealed class SignatureParameter
{
    public SignatureParameter(string label, string documentation)
    {
        Label = label;
        Documentation = documentation;
    }

    public string Label { get; }
    public string Documentation { get; }
}
