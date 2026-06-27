namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal sealed class AuroraResolvedSymbol
{
    private AuroraResolvedSymbol(AuroraSymbolInfo symbol, AuroraImportInfo? importAlias)
    {
        Symbol = symbol;
        ImportAlias = importAlias;
    }

    public AuroraSymbolInfo Symbol { get; }
    public AuroraImportInfo? ImportAlias { get; }

    public static AuroraResolvedSymbol FromSymbol(AuroraSymbolInfo symbol)
    {
        return new AuroraResolvedSymbol(symbol, null);
    }

    public static AuroraResolvedSymbol FromImportAlias(AuroraImportInfo importAlias, AuroraSymbolInfo targetSymbol)
    {
        return new AuroraResolvedSymbol(targetSymbol, importAlias);
    }
}
