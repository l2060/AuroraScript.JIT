using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Features.SignatureHelp;

public sealed class SignatureInformation
{
    public SignatureInformation(string label, string documentation, IReadOnlyList<SignatureParameter> parameters)
    {
        Label = label;
        Documentation = documentation;
        Parameters = parameters ?? Array.Empty<SignatureParameter>();
    }

    public string Label { get; }
    public string Documentation { get; }
    public IReadOnlyList<SignatureParameter> Parameters { get; }
}
