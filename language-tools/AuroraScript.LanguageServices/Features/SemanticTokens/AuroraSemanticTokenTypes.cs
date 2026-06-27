namespace AuroraScript.LanguageServices.Features.SemanticTokens;

public static class AuroraSemanticTokenTypes
{
    public const int Namespace = 0;
    public const int Type = 1;
    public const int Class = 2;
    public const int Enum = 3;
    public const int Function = 4;
    public const int Method = 5;
    public const int Property = 6;
    public const int Variable = 7;
    public const int Parameter = 8;
    public const int Keyword = 9;
    public const int Operator = 10;
    public const int String = 11;
    public const int Number = 12;
    public const int Regexp = 13;
    public const int Comment = 14;

    public static readonly string[] Legend =
    {
        "namespace",
        "type",
        "class",
        "enum",
        "function",
        "method",
        "property",
        "variable",
        "parameter",
        "keyword",
        "operator",
        "string",
        "number",
        "regexp",
        "comment"
    };
}
