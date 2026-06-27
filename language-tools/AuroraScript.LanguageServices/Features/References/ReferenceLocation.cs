using AuroraScript.LanguageServices.Text;

namespace AuroraScript.LanguageServices.Features.References;

public sealed class ReferenceLocation
{
    public ReferenceLocation(string path, TextRange range)
    {
        Path = path;
        Range = range;
    }

    public string Path { get; }
    public TextRange Range { get; }
}
