using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.GlobalDeclarations;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal static class AuroraCompletionResolver
{
    public static CompletionResult GetCompletions(
        AuroraWorkspaceIndex index,
        string path,
        TextPosition position,
        AmbientContractCatalog? ambient = null)
    {
        var module = index.TryGetModule(path);
        if (module == null)
        {
            return new CompletionResult(Array.Empty<CompletionItem>());
        }

        var context = AstQuery.Find(module.Module, position);
        if (context?.PropertyAccess != null)
        {
            var shapeCompletions = AuroraShapeQuery.GetFieldCompletions(
                index,
                module,
                context.PropertyAccess.Object);
            CompletionResult objectCompletions = new CompletionResult(Array.Empty<CompletionItem>());
            CompletionResult importCompletions = new CompletionResult(Array.Empty<CompletionItem>());
            CompletionResult ambientCompletions = new CompletionResult(Array.Empty<CompletionItem>());
            if (TryResolveOwnerName(context.PropertyAccess.Object, out var ownerName))
            {
                objectCompletions = AuroraDefinitionResolver.GetObjectMemberCompletions(
                    index,
                    module,
                    ownerName,
                    position);
                importCompletions = GetImportMemberCompletions(index, module, ownerName);
                ambientCompletions = GetAmbientMemberCompletions(
                    index,
                    module,
                    ownerName,
                    position,
                    ambient);
            }

            var merged = Merge(shapeCompletions, objectCompletions, importCompletions, ambientCompletions);
            if (merged.Items.Count != 0 || context.IsAfterMemberAccessDot)
            {
                return merged;
            }
        }

        return Merge(
            GetGlobalCompletions(index, module, position),
            ambient == null
                ? new CompletionResult(Array.Empty<CompletionItem>())
                : FilterShadowedAmbientRoots(index, module, position, ambient));
    }

    public static CompletionResult GetMemberCompletions(
        AuroraWorkspaceIndex index,
        string path,
        string ownerName,
        TextPosition position,
        AmbientContractCatalog? ambient = null)
    {
        var module = index.TryGetModule(path);
        if (module == null)
        {
            return new CompletionResult(Array.Empty<CompletionItem>());
        }

        return Merge(
            AuroraShapeQuery.GetFieldCompletionsForName(index, module, ownerName, position),
            AuroraDefinitionResolver.GetObjectMemberCompletions(index, module, ownerName, position),
            GetImportMemberCompletions(index, module, ownerName),
            GetAmbientMemberCompletions(index, module, ownerName, position, ambient));
    }

    private static CompletionResult GetGlobalCompletions(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        TextPosition position)
    {
        var items = new List<CompletionItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        var localIndex = AuroraLocalSymbolIndex.Build(module);
        foreach (var symbol in localIndex.GetVisibleSymbols(position))
        {
            Add(items, seen, ToLocalCompletion(symbol));
        }

        foreach (var symbol in module.Symbols.Values)
        {
            Add(items, seen, ToModuleCompletion(symbol));
        }

        foreach (var import in module.ImportsByAlias.Values)
        {
            Add(items, seen, new CompletionItem(
                import.Alias,
                CompletionItemKind.Module,
                "module alias",
                import.TargetPath,
                readOnly: true));
        }

        AddIncludedExports(index, module, items, seen, new HashSet<string>(PathComparer));
        return new CompletionResult(items);
    }

    private static CompletionResult GetImportMemberCompletions(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        string ownerName)
    {
        if (!module.ImportsByAlias.TryGetValue(ownerName, out var import))
        {
            return new CompletionResult(Array.Empty<CompletionItem>());
        }

        var target = index.TryGetModule(import.TargetPath);
        if (target == null)
        {
            return new CompletionResult(Array.Empty<CompletionItem>());
        }

        var items = new List<CompletionItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var symbol in target.Exports.Values)
        {
            Add(items, seen, ToImportedMemberCompletion(symbol));
        }

        return new CompletionResult(items);
    }

    private static CompletionResult GetAmbientMemberCompletions(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        string ownerName,
        TextPosition position,
        AmbientContractCatalog? ambient)
    {
        if (ambient == null)
        {
            return new CompletionResult(Array.Empty<CompletionItem>());
        }

        var localIndex = AuroraLocalSymbolIndex.Build(module);
        if (StringComparer.Ordinal.Equals(ownerName, "global"))
        {
            return AmbientDeclarationQuery.IsShadowed(module, localIndex, position, "global")
                ? new CompletionResult(Array.Empty<CompletionItem>())
                : AmbientDeclarationQuery.GetRootCompletions(ambient);
        }

        if (!AmbientDeclarationQuery.IsShadowed(module, localIndex, position, ownerName) &&
            ambient.TryGetRoot(ownerName, out _))
        {
            return AmbientDeclarationQuery.GetMemberCompletions(ambient, ownerName, instanceMembers: false);
        }

        if (AmbientDeclarationQuery.TryGetConstructedClassName(module, ownerName, position, out var className) &&
            !AmbientDeclarationQuery.IsShadowed(module, localIndex, position, className) &&
            ambient.TryGetRoot(className, out var classRoot) &&
            classRoot.Kind == GlobalDeclarationKind.Type)
        {
            return AmbientDeclarationQuery.GetMemberCompletions(ambient, className, instanceMembers: true);
        }

        _ = index;
        return new CompletionResult(Array.Empty<CompletionItem>());
    }

    private static CompletionResult FilterShadowedAmbientRoots(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        TextPosition position,
        AmbientContractCatalog ambient)
    {
        var localIndex = AuroraLocalSymbolIndex.Build(module);
        var items = new List<CompletionItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in AmbientDeclarationQuery.GetRootCompletions(ambient).Items)
        {
            if (AmbientDeclarationQuery.IsShadowed(module, localIndex, position, item.Label) ||
                TryResolveIncludedExport(index, module, item.Label))
            {
                continue;
            }

            if (seen.Add(item.Label))
            {
                items.Add(item);
            }
        }

        return new CompletionResult(items);
    }

    private static bool TryResolveIncludedExport(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        string name)
    {
        var visited = new HashSet<string>(PathComparer);
        return ContainsIncludedExport(index, module, name, visited);
    }

    private static bool ContainsIncludedExport(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        string name,
        HashSet<string> visited)
    {
        if (!visited.Add(module.Path))
        {
            return false;
        }

        for (var i = 0; i < module.Includes.Count; i++)
        {
            var included = index.TryGetModule(module.Includes[i].TargetPath);
            if (included == null)
            {
                continue;
            }

            if (included.Exports.ContainsKey(name) ||
                ContainsIncludedExport(index, included, name, visited))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddIncludedExports(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        List<CompletionItem> items,
        HashSet<string> seen,
        HashSet<string> visited)
    {
        if (!visited.Add(module.Path))
        {
            return;
        }

        for (var i = 0; i < module.Includes.Count; i++)
        {
            var included = index.TryGetModule(module.Includes[i].TargetPath);
            if (included == null)
            {
                continue;
            }

            foreach (var symbol in included.Exports.Values)
            {
                Add(items, seen, ToIncludedCompletion(symbol));
            }

            AddIncludedExports(index, included, items, seen, visited);
        }
    }

    private static CompletionResult Merge(params CompletionResult[] results)
    {
        var items = new List<CompletionItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var result in results)
        {
            if (result == null)
            {
                continue;
            }

            for (var i = 0; i < result.Items.Count; i++)
            {
                Add(items, seen, result.Items[i]);
            }
        }

        return new CompletionResult(items);
    }

    private static CompletionItem ToLocalCompletion(AuroraLocalSymbolIndex.LocalSymbolInfo symbol)
    {
        var kind = symbol.Kind switch
        {
            AuroraLocalSymbolIndex.LocalSymbolKind.Function => CompletionItemKind.Function,
            AuroraLocalSymbolIndex.LocalSymbolKind.Constant => CompletionItemKind.Constant,
            _ => CompletionItemKind.Variable
        };
        var detail = symbol.Kind switch
        {
            AuroraLocalSymbolIndex.LocalSymbolKind.Function => "local function",
            AuroraLocalSymbolIndex.LocalSymbolKind.Constant => "local const",
            _ => "local variable"
        };

        return new CompletionItem(
            symbol.Name,
            kind,
            detail,
            documentation: null,
            readOnly: symbol.Kind == AuroraLocalSymbolIndex.LocalSymbolKind.Constant);
    }

    private static CompletionItem ToModuleCompletion(AuroraSymbolInfo symbol)
    {
        return new CompletionItem(
            symbol.Name,
            ToGlobalKind(symbol.Kind),
            ToGlobalDetail(symbol.Kind),
            documentation: null,
            readOnly: symbol.Kind is AuroraSymbolKind.Constant or AuroraSymbolKind.Enum);
    }

    private static CompletionItem ToIncludedCompletion(AuroraSymbolInfo symbol)
    {
        return new CompletionItem(
            symbol.Name,
            ToGlobalKind(symbol.Kind),
            "included " + ToGlobalDetail(symbol.Kind),
            documentation: null,
            readOnly: symbol.Kind is AuroraSymbolKind.Constant or AuroraSymbolKind.Enum);
    }

    private static CompletionItem ToImportedMemberCompletion(AuroraSymbolInfo symbol)
    {
        return new CompletionItem(
            symbol.Name,
            ToMemberKind(symbol.Kind),
            ToMemberDetail(symbol.Kind),
            documentation: null,
            readOnly: symbol.Kind is AuroraSymbolKind.Constant or AuroraSymbolKind.Enum);
    }

    private static CompletionItemKind ToGlobalKind(AuroraSymbolKind kind)
    {
        return kind switch
        {
            AuroraSymbolKind.Function => CompletionItemKind.Function,
            AuroraSymbolKind.Constant => CompletionItemKind.Constant,
            AuroraSymbolKind.Enum => CompletionItemKind.Enum,
            AuroraSymbolKind.Type => CompletionItemKind.Type,
            AuroraSymbolKind.ImportAlias => CompletionItemKind.Module,
            _ => CompletionItemKind.Variable
        };
    }

    private static CompletionItemKind ToMemberKind(AuroraSymbolKind kind)
    {
        return kind switch
        {
            AuroraSymbolKind.Function => CompletionItemKind.Method,
            AuroraSymbolKind.Constant => CompletionItemKind.Constant,
            AuroraSymbolKind.Enum => CompletionItemKind.Enum,
            AuroraSymbolKind.Type => CompletionItemKind.Type,
            _ => CompletionItemKind.Property
        };
    }

    private static string ToGlobalDetail(AuroraSymbolKind kind)
    {
        return kind switch
        {
            AuroraSymbolKind.Function => "module function",
            AuroraSymbolKind.Constant => "module const",
            AuroraSymbolKind.Enum => "enum",
            AuroraSymbolKind.Type => "type",
            AuroraSymbolKind.ImportAlias => "module alias",
            _ => "module variable"
        };
    }

    private static string ToMemberDetail(AuroraSymbolKind kind)
    {
        return kind switch
        {
            AuroraSymbolKind.Function => "exported function",
            AuroraSymbolKind.Constant => "exported const",
            AuroraSymbolKind.Enum => "exported enum",
            AuroraSymbolKind.Type => "exported type",
            _ => "exported variable"
        };
    }

    private static void Add(
        List<CompletionItem> items,
        HashSet<string> seen,
        CompletionItem item)
    {
        if (seen.Add(item.Label))
        {
            items.Add(item);
        }
    }

    private static bool TryResolveOwnerName(Expression expression, out string ownerName)
    {
        if (expression is NameExpression name)
        {
            ownerName = name.Identifier.Value;
            return true;
        }

        ownerName = string.Empty;
        return false;
    }

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}
