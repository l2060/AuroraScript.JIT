using AuroraScript.Compiler.Ast;
using AuroraScript.LanguageServices.Diagnostics;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Parsing;

public sealed class AuroraParseResult
{
    internal AuroraParseResult(ModuleDeclaration? module, IReadOnlyList<LanguageDiagnostic> diagnostics)
    {
        Module = module;
        Diagnostics = diagnostics ?? Array.Empty<LanguageDiagnostic>();
    }

    internal ModuleDeclaration? Module { get; }
    public IReadOnlyList<LanguageDiagnostic> Diagnostics { get; }
    public bool Success => Module != null && Diagnostics.Count == 0;
}
