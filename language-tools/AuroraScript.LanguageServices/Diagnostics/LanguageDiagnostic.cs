using AuroraScript.LanguageServices.Text;

namespace AuroraScript.LanguageServices.Diagnostics;

public sealed class LanguageDiagnostic
{
    public LanguageDiagnostic(string code, string message, TextRange range, LanguageDiagnosticSeverity severity)
    {
        Code = code;
        Message = message;
        Range = range;
        Severity = severity;
    }

    public string Code { get; }
    public string Message { get; }
    public TextRange Range { get; }
    public LanguageDiagnosticSeverity Severity { get; }
}
