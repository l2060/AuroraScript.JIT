using AuroraScript.LanguageServices.Builtins;

namespace AuroraScript.LanguageServices.Semantics;

public sealed class SemanticSymbol
{
    private SemanticSymbol(SemanticSymbolKind kind, string name, BuiltinApiSymbol? builtinGlobal, BuiltinApiMember? builtinMember)
    {
        Kind = kind;
        Name = name;
        BuiltinGlobal = builtinGlobal;
        BuiltinMember = builtinMember;
    }

    public SemanticSymbolKind Kind { get; }
    public string Name { get; }
    public BuiltinApiSymbol? BuiltinGlobal { get; }
    public BuiltinApiMember? BuiltinMember { get; }

    public static SemanticSymbol FromBuiltinGlobal(BuiltinApiSymbol symbol)
    {
        return new SemanticSymbol(SemanticSymbolKind.BuiltinGlobal, symbol.Name, symbol, null);
    }

    public static SemanticSymbol FromBuiltinMember(BuiltinApiMember member)
    {
        return new SemanticSymbol(SemanticSymbolKind.BuiltinMember, member.FullName, null, member);
    }
}
