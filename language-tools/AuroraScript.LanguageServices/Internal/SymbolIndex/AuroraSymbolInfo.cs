using AuroraScript.LanguageServices.Text;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal sealed class AuroraSymbolInfo
{
    public AuroraSymbolInfo(
        string name,
        AuroraSymbolKind kind,
        string modulePath,
        string filePath,
        TextRange nameRange,
        bool exported,
        bool declaredExternal = false)
    {
        Name = name;
        Kind = kind;
        ModulePath = modulePath;
        FilePath = filePath;
        NameRange = nameRange;
        Exported = exported;
        DeclaredExternal = declaredExternal;
    }

    public string Name { get; }
    public AuroraSymbolKind Kind { get; }
    public string ModulePath { get; }
    public string FilePath { get; }
    public TextRange NameRange { get; }
    public bool Exported { get; }
    public bool DeclaredExternal { get; }
}
