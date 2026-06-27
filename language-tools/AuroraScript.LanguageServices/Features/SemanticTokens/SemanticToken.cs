namespace AuroraScript.LanguageServices.Features.SemanticTokens;

public readonly record struct SemanticToken(
    int Line,
    int Character,
    int Length,
    int Type,
    int Modifiers);
