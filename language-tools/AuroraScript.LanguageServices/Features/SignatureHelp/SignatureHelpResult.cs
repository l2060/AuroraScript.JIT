using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Features.SignatureHelp;

public sealed class SignatureHelpResult
{
    public SignatureHelpResult(IReadOnlyList<SignatureInformation> signatures, int activeSignature, int activeParameter)
    {
        Signatures = signatures ?? Array.Empty<SignatureInformation>();
        ActiveSignature = activeSignature;
        ActiveParameter = activeParameter;
    }

    public IReadOnlyList<SignatureInformation> Signatures { get; }
    public int ActiveSignature { get; }
    public int ActiveParameter { get; }
}
