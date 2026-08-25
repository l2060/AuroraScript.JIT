using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Features.Hover;
using AuroraScript.LanguageServices.Features.SignatureHelp;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal;

internal static class BuiltinQuery
{
    public static bool TryGetHover(
        BuiltinApiCatalog builtins,
        ModuleDeclaration? declaration,
        AstQueryContext context,
        string? locale,
        out HoverResult hover)
    {
        hover = null!;
        if (context.TypeReference != null &&
            builtins.TryGetGlobal(context.TypeReference.Value, out var assertedType))
        {
            hover = new HoverResult(
                BuiltinFormat.FormatGlobal(assertedType, locale),
                TextRange.FromSourceSpan(context.TypeReference.Range));
            return true;
        }

        if (context.PropertyAccess != null &&
            context.IsOnPropertyOwner &&
            context.PropertyAccess.Object is NameExpression ownerName)
        {
            var name = ownerName.Identifier.Value;
            if (BuiltinModuleQuery.TryResolve(builtins, declaration, name, out var module))
            {
                hover = new HoverResult(
                    BuiltinFormat.FormatModule(module, name, locale),
                    TextRange.FromSourceSpan(ownerName.Identifier.Range));
                return true;
            }

            if (!BuiltinModuleQuery.IsImportedName(declaration, name) &&
                builtins.TryGetGlobal(name, out var owner))
            {
                hover = new HoverResult(
                    BuiltinFormat.FormatGlobal(owner, locale),
                    TextRange.FromSourceSpan(ownerName.Identifier.Range));
                return true;
            }
        }

        if (context.PropertyAccess != null &&
            context.IsOnPropertyName &&
            TryResolveMember(builtins, declaration, context.PropertyAccess, out var member))
        {
            var range = context.PropertyAccess.Property is NameExpression name
                ? name.Identifier.Range
                : context.PropertyAccess.Property.Range;
            hover = new HoverResult(BuiltinFormat.FormatMember(member, locale), TextRange.FromSourceSpan(range));
            return true;
        }

        if (context.Name != null &&
            !BuiltinModuleQuery.IsImportedName(declaration, context.Name.Identifier.Value) &&
            builtins.TryGetGlobal(context.Name.Identifier.Value, out var global))
        {
            hover = new HoverResult(BuiltinFormat.FormatGlobal(global, locale), TextRange.FromSourceSpan(context.Name.Range));
            return true;
        }

        return false;
    }

    public static CompletionResult GetCompletions(
        BuiltinApiCatalog builtins,
        ModuleDeclaration? declaration,
        AstQueryContext? context,
        string? locale = null)
    {
        if (context?.PropertyAccess != null &&
            TryResolveOwnerName(context.PropertyAccess.Object, out var ownerName))
        {
            return GetMemberCompletions(builtins, declaration, ownerName, locale);
        }

        return CompleteGlobals(builtins.Globals, locale);
    }

    public static CompletionResult GetMemberCompletions(
        BuiltinApiCatalog builtins,
        ModuleDeclaration? declaration,
        string ownerName,
        string? locale = null)
    {
        if (BuiltinModuleQuery.TryResolve(builtins, declaration, ownerName, out var module))
        {
            return CompleteMembers(module.Members, locale);
        }

        if (BuiltinModuleQuery.IsImportedName(declaration, ownerName))
        {
            return new CompletionResult(Array.Empty<CompletionItem>());
        }

        if (builtins.TryGetGlobal(ownerName, out var owner))
        {
            return CompleteMembers(owner.Members, locale);
        }

        return new CompletionResult(Array.Empty<CompletionItem>());
    }

    public static SignatureHelpResult? GetSignatureHelp(
        BuiltinApiCatalog builtins,
        ModuleDeclaration? declaration,
        AstQueryContext context,
        TextPosition position,
        string? locale = null)
    {
        var call = context.Call;
        if (call == null)
        {
            return null;
        }

        if (context.NewExpression?.Expression == call)
        {
            return GetConstructorSignatureHelp(builtins, call, position, locale, includeNewKeyword: true, requireCallable: false);
        }

        BuiltinApiMember member;
        if (call.Target is GetPropertyExpression propertyAccess)
        {
            if (!TryResolveMember(builtins, declaration, propertyAccess, out member))
            {
                return null;
            }
        }
        else if (call.Target is NameExpression name)
        {
            if (BuiltinModuleQuery.IsImportedName(declaration, name.Identifier.Value) ||
                !builtins.TryGetGlobal(name.Identifier.Value, out var global))
            {
                return null;
            }

            if (global.Kind == BuiltinApiKind.Constructor && global.Callable && global.Constructors.Count != 0)
            {
                return GetConstructorSignatureHelp(builtins, call, position, locale, includeNewKeyword: false, requireCallable: true);
            }

            if (global.Kind != BuiltinApiKind.Function)
            {
                return null;
            }

            member = new BuiltinApiMember(
                string.Empty,
                global.Name,
                BuiltinApiKind.Function,
                "any",
                global.ReadOnly,
                Array.Empty<BuiltinApiParameter>(),
                global.Documentation);
        }
        else
        {
            return null;
        }

        var signature = BuiltinFormat.FormatSignatureInfo(member, locale);
        var activeParameter = GetActiveParameter(call, position);
        return new SignatureHelpResult(new[] { signature }, 0, activeParameter);
    }

    private static SignatureHelpResult? GetConstructorSignatureHelp(
        BuiltinApiCatalog builtins,
        FunctionCallExpression call,
        TextPosition position,
        string? locale,
        bool includeNewKeyword,
        bool requireCallable)
    {
        if (call.Target is not NameExpression name ||
            !builtins.TryGetGlobal(name.Identifier.Value, out var global) ||
            global.Kind != BuiltinApiKind.Constructor ||
            global.Constructors.Count == 0 ||
            requireCallable && !global.Callable)
        {
            return null;
        }

        var signatures = new List<SignatureInformation>(global.Constructors.Count);
        for (var i = 0; i < global.Constructors.Count; i++)
        {
            signatures.Add(BuiltinFormat.FormatConstructorSignatureInfo(
                global.Constructors[i],
                locale,
                includeNewKeyword));
        }

        return new SignatureHelpResult(signatures, 0, GetActiveParameter(call, position));
    }

    private static CompletionResult CompleteGlobals(IReadOnlyDictionary<string, BuiltinApiSymbol> globals, string? locale)
    {
        var items = new List<CompletionItem>(globals.Count);
        foreach (var pair in globals)
        {
            var symbol = pair.Value;
            items.Add(new CompletionItem(
                symbol.Name,
                BuiltinFormat.ToCompletionKind(symbol.Kind),
                BuiltinFormat.FormatCompletionDetail(symbol),
                BuiltinFormat.FormatGlobal(symbol, locale),
                symbol.ReadOnly));
        }

        return new CompletionResult(items);
    }

    private static CompletionResult CompleteMembers(IReadOnlyDictionary<string, BuiltinApiMember> members, string? locale)
    {
        var items = new List<CompletionItem>(members.Count);
        foreach (var pair in members)
        {
            var member = pair.Value;
            items.Add(new CompletionItem(
                member.Name,
                BuiltinFormat.ToCompletionKind(member.Kind),
                BuiltinFormat.FormatCompletionDetail(member),
                BuiltinFormat.FormatMember(member, locale),
                member.ReadOnly));
        }

        return new CompletionResult(items);
    }

    private static bool TryResolveMember(
        BuiltinApiCatalog builtins,
        ModuleDeclaration? declaration,
        GetPropertyExpression propertyAccess,
        out BuiltinApiMember member)
    {
        member = null!;
        if (!TryResolveOwnerName(propertyAccess.Object, out var ownerName) ||
            propertyAccess.Property is not NameExpression property)
        {
            return false;
        }

        if (BuiltinModuleQuery.TryResolve(builtins, declaration, ownerName, out var module))
        {
            return module.TryGetMember(property.Identifier.Value, out member);
        }

        return !BuiltinModuleQuery.IsImportedName(declaration, ownerName) &&
            builtins.TryGetGlobalMember(ownerName, property.Identifier.Value, out member);
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

    private static int GetActiveParameter(FunctionCallExpression call, TextPosition position)
    {
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            if (call.Arguments[i].Range.Contains(position))
            {
                return i;
            }
        }

        var line = position.Line + 1;
        var column = position.Character + 1;
        var active = 0;
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            var argument = call.Arguments[i];
            if (argument.Range.StartLine < line ||
                argument.Range.StartLine == line && argument.Range.StartColumn <= column)
            {
                active = i;
            }
        }

        return active;
    }
}
