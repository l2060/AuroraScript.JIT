namespace AuroraScript.LanguageServices.Text;

public readonly record struct TextPosition(int Line, int Character)
{
    public static readonly TextPosition Zero = new(0, 0);
}
