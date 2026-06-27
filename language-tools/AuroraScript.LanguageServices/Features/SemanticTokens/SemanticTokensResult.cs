using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Features.SemanticTokens;

public sealed class SemanticTokensResult
{
    public SemanticTokensResult(IReadOnlyList<SemanticToken> tokens)
    {
        Tokens = tokens ?? Array.Empty<SemanticToken>();
    }

    public IReadOnlyList<SemanticToken> Tokens { get; }
}
