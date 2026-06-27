using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.LanguageServices.Features.Definition;
using AuroraScript.LanguageServices.Text;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal static class AuroraDefinitionResolver
{
    public static DefinitionLocation? Resolve(AuroraWorkspaceIndex index, string path, TextPosition position)
    {
        var module = index.TryGetModule(path);
        if (module == null)
        {
            return null;
        }

        var context = AstQuery.Find(module.Module, position);
        if (context == null)
        {
            return null;
        }

        if (context.PropertyAccess != null &&
            context.IsOnPropertyName &&
            TryResolveImportedMember(index, module, context.PropertyAccess, out var importedMember))
        {
            return ToLocation(importedMember);
        }

        if (context.Name != null)
        {
            var name = context.Name.Identifier.Value;

            if (module.Symbols.TryGetValue(name, out var local))
            {
                return ToLocation(local);
            }

            if (module.ImportsByAlias.TryGetValue(name, out var importAlias))
            {
                return new DefinitionLocation(importAlias.TargetPath, importAlias.AliasRange);
            }

            if (TryResolveIncludedExport(index, module, name, out var included))
            {
                return ToLocation(included);
            }
        }

        return null;
    }

    private static bool TryResolveImportedMember(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        GetPropertyExpression propertyAccess,
        out AuroraSymbolInfo symbol)
    {
        symbol = null!;
        if (propertyAccess.Object is not NameExpression alias ||
            propertyAccess.Property is not NameExpression property)
        {
            return false;
        }

        if (!module.ImportsByAlias.TryGetValue(alias.Identifier.Value, out var import))
        {
            return false;
        }

        var target = index.TryGetModule(import.TargetPath);
        return target != null && target.Exports.TryGetValue(property.Identifier.Value, out symbol!);
    }

    private static bool TryResolveIncludedExport(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        string name,
        out AuroraSymbolInfo symbol)
    {
        var visited = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        return TryResolveIncludedExport(index, module, name, visited, out symbol);
    }

    private static bool TryResolveIncludedExport(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        string name,
        HashSet<string> visited,
        out AuroraSymbolInfo symbol)
    {
        symbol = null!;
        if (!visited.Add(module.Path))
        {
            return false;
        }

        for (var i = 0; i < module.Includes.Count; i++)
        {
            var include = module.Includes[i];
            var includedModule = index.TryGetModule(include.TargetPath);
            if (includedModule == null)
            {
                continue;
            }

            if (includedModule.Exports.TryGetValue(name, out symbol!))
            {
                return true;
            }

            if (TryResolveIncludedExport(index, includedModule, name, visited, out symbol))
            {
                return true;
            }
        }

        return false;
    }

    private static DefinitionLocation ToLocation(AuroraSymbolInfo symbol)
    {
        return new DefinitionLocation(symbol.FilePath, symbol.NameRange);
    }
}
