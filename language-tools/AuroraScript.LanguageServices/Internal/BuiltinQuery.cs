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
    public static bool TryGetHover(BuiltinApiCatalog builtins, AstQueryContext context, out HoverResult hover)
    {
        hover = null!;
        if (context.PropertyAccess != null &&
            TryResolveMember(builtins, context.PropertyAccess, out var member))
        {
            var range = context.PropertyAccess.Property is NameExpression name
                ? name.Identifier.Range
                : context.PropertyAccess.Property.Range;
            hover = new HoverResult(BuiltinFormat.FormatMember(member), TextRange.FromSourceSpan(range));
            return true;
        }

        if (context.Name != null &&
            builtins.TryGetGlobal(context.Name.Identifier.Value, out var global))
        {
            hover = new HoverResult(BuiltinFormat.FormatGlobal(global), TextRange.FromSourceSpan(context.Name.Range));
            return true;
        }

        return false;
    }

    public static CompletionResult GetCompletions(BuiltinApiCatalog builtins, AstQueryContext? context)
    {
        if (context?.PropertyAccess != null &&
            TryResolveOwnerName(context.PropertyAccess.Object, out var ownerName) &&
            builtins.TryGetGlobal(ownerName, out var owner))
        {
            return CompleteMembers(owner.Members);
        }

        return CompleteGlobals(builtins.Globals);
    }

    public static CompletionResult GetMemberCompletions(BuiltinApiCatalog builtins, string ownerName)
    {
        if (builtins.TryGetGlobal(ownerName, out var owner))
        {
            return CompleteMembers(owner.Members);
        }

        return new CompletionResult(Array.Empty<CompletionItem>());
    }

    public static SignatureHelpResult? GetSignatureHelp(BuiltinApiCatalog builtins, AstQueryContext context, TextPosition position)
    {
        var call = context.Call;
        if (call == null)
        {
            return null;
        }

        BuiltinApiMember member;
        if (call.Target is GetPropertyExpression propertyAccess)
        {
            if (!TryResolveMember(builtins, propertyAccess, out member))
            {
                return null;
            }
        }
        else if (call.Target is NameExpression name)
        {
            if (!builtins.TryGetGlobal(name.Identifier.Value, out var global) ||
                global.Kind != BuiltinApiKind.Function)
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
                global.Notes);
        }
        else
        {
            return null;
        }

        var signature = BuiltinFormat.FormatSignatureInfo(member);
        var activeParameter = GetActiveParameter(call, position);
        return new SignatureHelpResult(new[] { signature }, 0, activeParameter);
    }

    private static CompletionResult CompleteGlobals(IReadOnlyDictionary<string, BuiltinApiSymbol> globals)
    {
        var items = new List<CompletionItem>(globals.Count);
        foreach (var pair in globals)
        {
            var symbol = pair.Value;
            items.Add(new CompletionItem(
                symbol.Name,
                BuiltinFormat.ToCompletionKind(symbol.Kind),
                BuiltinFormat.FormatCompletionDetail(symbol),
                BuiltinFormat.FormatGlobal(symbol),
                symbol.ReadOnly));
        }

        return new CompletionResult(items);
    }

    private static CompletionResult CompleteMembers(IReadOnlyDictionary<string, BuiltinApiMember> members)
    {
        var items = new List<CompletionItem>(members.Count);
        foreach (var pair in members)
        {
            var member = pair.Value;
            items.Add(new CompletionItem(
                member.Name,
                BuiltinFormat.ToCompletionKind(member.Kind),
                BuiltinFormat.FormatCompletionDetail(member),
                BuiltinFormat.FormatMember(member),
                member.ReadOnly));
        }

        return new CompletionResult(items);
    }

    private static bool TryResolveMember(BuiltinApiCatalog builtins, GetPropertyExpression propertyAccess, out BuiltinApiMember member)
    {
        member = null!;
        return TryResolveOwnerName(propertyAccess.Object, out var ownerName) &&
            propertyAccess.Property is NameExpression property &&
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
