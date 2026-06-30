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
    public const int EnumMember = 15;
    public const int Object = 16;
    public const int MethodCall = 17;
    public const int FunctionCall = 18;
    public const int MapKey = 19;
    public const int BuiltinVariable = 20;
    public const int ControlFlow = 21;
    public const int Return = 22;
    public const int Throw = 23;
    public const int Exception = 24;
    public const int ImportExport = 25;
    public const int Character = 26;
    public const int Parenthesis = 27;
    public const int Bracket = 28;
    public const int Comma = 29;
    public const int Semicolon = 30;
    public const int Dot = 31;
    public const int Colon = 32;
    public const int BraceLevel1 = 33;
    public const int BraceLevel2 = 34;
    public const int BraceLevel3 = 35;
    public const int BraceLevel4 = 36;
    public const int BraceLevel5 = 37;
    public const int BraceLevel6 = 38;
    public const int DeclaredGlobal = 39;
    public const int DeclaredGlobalFunction = 40;

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
        "comment",
        "enumMember",
        "object",
        "methodCall",
        "functionCall",
        "mapKey",
        "builtinVariable",
        "controlFlow",
        "return",
        "throw",
        "exception",
        "importExport",
        "character",
        "parenthesis",
        "bracket",
        "comma",
        "semicolon",
        "dot",
        "colon",
        "braceLevel1",
        "braceLevel2",
        "braceLevel3",
        "braceLevel4",
        "braceLevel5",
        "braceLevel6",
        "declaredGlobal",
        "declaredGlobalFunction"
    };
}
