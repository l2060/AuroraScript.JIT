using AuroraScript.Compiler.Ast.Expressions;
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
        TextPosition position)
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
            if (shapeCompletions.Items.Count != 0)
            {
                return shapeCompletions;
            }

            if (TryResolveOwnerName(context.PropertyAccess.Object, out var ownerName))
            {
                return GetMemberCompletions(index, module, ownerName);
            }

            if (context.IsAfterMemberAccessDot)
            {
                return shapeCompletions;
            }
        }

        return GetGlobalCompletions(index, module, position);
    }

    public static CompletionResult GetMemberCompletions(
        AuroraWorkspaceIndex index,
        string path,
        string ownerName)
    {
        var module = index.TryGetModule(path);
        return module == null
            ? new CompletionResult(Array.Empty<CompletionItem>())
            : GetMemberCompletions(index, module, ownerName);
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

    private static CompletionResult GetMemberCompletions(
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
