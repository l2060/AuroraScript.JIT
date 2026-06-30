using AuroraScript.Compiler.GlobalDeclarations;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal;

internal sealed class SemanticExternalSymbols
{
    public static readonly SemanticExternalSymbols Empty = new(new Dictionary<string, int>(StringComparer.Ordinal));

    private readonly Dictionary<string, int> _globals;

    private SemanticExternalSymbols(Dictionary<string, int> globals)
    {
        _globals = globals;
    }

    public static SemanticExternalSymbols FromGlobalDeclarationIndex(GlobalDeclarationIndex index)
    {
        var globals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var declaration in index.Declarations.Values)
        {
            globals[declaration.Name] = declaration.Kind == GlobalDeclarationKind.Function
                ? AuroraSemanticTokenTypes.DeclaredGlobalFunction
                : AuroraSemanticTokenTypes.DeclaredGlobal;
        }

        return globals.Count == 0
            ? Empty
            : new SemanticExternalSymbols(globals);
    }

    public bool TryResolveGlobal(string name, out int type)
    {
        return _globals.TryGetValue(name, out type);
    }

}
