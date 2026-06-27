using AuroraScript.LanguageServices.Text;
using System;

namespace AuroraScript.LanguageServices.Features.Rename;

public sealed class TextEdit
{
    public TextEdit(TextRange range, string newText)
    {
        Range = range;
        NewText = newText ?? throw new ArgumentNullException(nameof(newText));
    }

    public TextRange Range { get; }
    public string NewText { get; }
}
