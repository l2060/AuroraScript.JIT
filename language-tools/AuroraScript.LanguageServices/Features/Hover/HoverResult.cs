using AuroraScript.LanguageServices.Text;

namespace AuroraScript.LanguageServices.Features.Hover;

public sealed class HoverResult
{
    public HoverResult(string contents, TextRange range)
    {
        Contents = contents;
        Range = range;
    }

    public string Contents { get; }
    public TextRange Range { get; }
}
