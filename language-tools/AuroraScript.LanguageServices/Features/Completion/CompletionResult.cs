using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Features.Completion;

public sealed class CompletionResult
{
    public CompletionResult(IReadOnlyList<CompletionItem> items)
    {
        Items = items ?? Array.Empty<CompletionItem>();
    }

    public IReadOnlyList<CompletionItem> Items { get; }
}
