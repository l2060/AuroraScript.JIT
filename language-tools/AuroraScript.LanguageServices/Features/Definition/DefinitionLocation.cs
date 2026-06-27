using AuroraScript.LanguageServices.Text;

namespace AuroraScript.LanguageServices.Features.Definition;

public sealed class DefinitionLocation
{
    public DefinitionLocation(string path, TextRange range)
    {
        Path = path;
        Range = range;
    }

    public string Path { get; }
    public TextRange Range { get; }
}
