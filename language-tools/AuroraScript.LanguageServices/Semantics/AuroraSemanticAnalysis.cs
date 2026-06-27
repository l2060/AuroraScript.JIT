using AuroraScript.LanguageServices.Diagnostics;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Semantics;

public sealed class AuroraSemanticAnalysis
{
    public AuroraSemanticAnalysis(IReadOnlyList<LanguageDiagnostic> diagnostics)
    {
        Diagnostics = diagnostics ?? Array.Empty<LanguageDiagnostic>();
    }

    public IReadOnlyList<LanguageDiagnostic> Diagnostics { get; }
}
