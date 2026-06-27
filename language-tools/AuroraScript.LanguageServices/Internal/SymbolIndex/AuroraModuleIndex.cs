using AuroraScript.Compiler.Ast;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal sealed class AuroraModuleIndex
{
    private readonly Dictionary<string, AuroraSymbolInfo> _symbols = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AuroraSymbolInfo> _exports = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AuroraImportInfo> _importsByAlias = new(StringComparer.Ordinal);

    public AuroraModuleIndex(string path, string text, ModuleDeclaration module)
    {
        Path = path;
        Text = text;
        Module = module;
    }

    public string Path { get; }
    public string Text { get; }
    public ModuleDeclaration Module { get; }
    public IReadOnlyDictionary<string, AuroraSymbolInfo> Symbols => _symbols;
    public IReadOnlyDictionary<string, AuroraSymbolInfo> Exports => _exports;
    public IReadOnlyDictionary<string, AuroraImportInfo> ImportsByAlias => _importsByAlias;
    public List<AuroraImportInfo> Includes { get; } = new();

    public void AddSymbol(AuroraSymbolInfo symbol)
    {
        _symbols[symbol.Name] = symbol;
        if (symbol.Exported)
        {
            _exports[symbol.Name] = symbol;
        }
    }

    public void AddImport(AuroraImportInfo import)
    {
        if (import.Include)
        {
            Includes.Add(import);
        }
        else
        {
            _importsByAlias[import.Alias] = import;
        }
    }
}
