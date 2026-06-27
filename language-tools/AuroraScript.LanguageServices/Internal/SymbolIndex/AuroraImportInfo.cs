using AuroraScript.LanguageServices.Text;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal sealed class AuroraImportInfo
{
    public AuroraImportInfo(string alias, string targetPath, TextRange aliasRange, bool include)
    {
        Alias = alias;
        TargetPath = targetPath;
        AliasRange = aliasRange;
        Include = include;
    }

    public string Alias { get; }
    public string TargetPath { get; }
    public TextRange AliasRange { get; }
    public bool Include { get; }
}
