using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Features.Formatting;

public sealed class FormattingResult
{
    public FormattingResult(IReadOnlyList<TextEdit> edits)
    {
        Edits = edits ?? Array.Empty<TextEdit>();
    }

    public IReadOnlyList<TextEdit> Edits { get; }

    public static FormattingResult Empty { get; } = new(Array.Empty<TextEdit>());
}
