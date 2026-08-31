using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Features.SignatureHelp;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.LanguageServices.Internal;

internal static class BuiltinFormat
{
    private const string MarkdownLanguageId = "aurorascript";

    public static string FormatGlobal(BuiltinApiSymbol symbol, string? locale = null)
    {
        var builder = new StringBuilder();
        builder.Append("```").Append(MarkdownLanguageId).Append('\n');
        BuiltinTypeFormatter.AppendJsDoc(builder, symbol.Documentation.GetNotes(locale), null, null);
        if (symbol.Kind == BuiltinApiKind.Constructor && symbol.Constructors.Count != 0)
        {
            for (var i = 0; i < symbol.Constructors.Count; i++)
            {
                builder.Append(BuiltinTypeFormatter.FormatConstructorSignature(symbol.Constructors[i], includeNewKeyword: true));
                builder.Append(";\n");
            }
        }
        else
        {
            builder.Append(symbol.Name);
            if (symbol.Kind == BuiltinApiKind.Function)
            {
                builder.Append("(): Object");
            }
            builder.Append(";\n");
        }
        builder.Append("```");
        return builder.ToString();
    }

    public static string FormatModule(
        BuiltinApiModule module,
        string? alias = null,
        string? locale = null)
    {
        var builder = new StringBuilder();
        builder.Append("```").Append(MarkdownLanguageId).Append('\n');
        BuiltinTypeFormatter.AppendJsDoc(builder, module.Documentation.GetNotes(locale), null, null);
        builder
            .Append("import ").Append(string.IsNullOrWhiteSpace(alias) ? module.Name : alias)
            .Append(" from \"").Append(module.ModulePath).Append("\";\n")
            .Append("```");
        return builder.ToString();
    }

    public static string FormatMember(BuiltinApiMember member, string? locale = null)
    {
        var builder = new StringBuilder();
        builder.Append("```").Append(MarkdownLanguageId).Append('\n');
        var notes = member.Documentation.GetNotes(locale);
        var parameters = member.Kind is BuiltinApiKind.Method or BuiltinApiKind.Function
            ? member.Parameters
            : null;
        BuiltinTypeFormatter.AppendJsDoc(builder, notes, parameters, member.ReturnType);
        builder.Append(BuiltinTypeFormatter.FormatMemberSignature(member)).Append(";\n```");
        return builder.ToString();
    }

    public static string FormatCompletionDetail(BuiltinApiSymbol symbol)
    {
        if (symbol.Kind == BuiltinApiKind.Constructor && symbol.Constructors.Count != 0)
        {
            return BuiltinTypeFormatter.FormatConstructorSignature(symbol.Constructors[0], includeNewKeyword: true);
        }

        return (symbol.ReadOnly ? "readonly " : string.Empty) + FormatKind(symbol.Kind);
    }

    public static string FormatCompletionDetail(BuiltinApiMember member)
    {
        if (member.Kind == BuiltinApiKind.Method || member.Kind == BuiltinApiKind.Function)
        {
            return BuiltinTypeFormatter.FormatMemberSignature(member);
        }

        return (member.ReadOnly ? "readonly " : string.Empty) +
            BuiltinTypeFormatter.FormatType(member.ReturnType, BuiltinTypeFormatter.TypeUsage.Value, optional: false, variadic: false);
    }

    public static SignatureInformation FormatSignatureInfo(BuiltinApiMember member, string? locale = null)
    {
        var parameters = new List<SignatureParameter>(member.Parameters.Count);
        for (var i = 0; i < member.Parameters.Count; i++)
        {
            var parameter = member.Parameters[i];
            parameters.Add(new SignatureParameter(
                FormatMappedParameter(parameter, i),
                BuiltinTypeFormatter.FormatType(parameter.Type, BuiltinTypeFormatter.TypeUsage.Value, parameter.Optional, parameter.Variadic)));
        }

        return new SignatureInformation(
            BuiltinTypeFormatter.FormatMemberSignature(member),
            FormatMember(member, locale),
            parameters);
    }

    public static SignatureInformation FormatConstructorSignatureInfo(
        BuiltinApiMember constructor,
        string? locale = null,
        bool includeNewKeyword = true)
    {
        var parameters = new List<SignatureParameter>(constructor.Parameters.Count);
        for (var i = 0; i < constructor.Parameters.Count; i++)
        {
            var parameter = constructor.Parameters[i];
            parameters.Add(new SignatureParameter(
                FormatMappedParameter(parameter, i),
                BuiltinTypeFormatter.FormatType(parameter.Type, BuiltinTypeFormatter.TypeUsage.Value, parameter.Optional, parameter.Variadic)));
        }

        return new SignatureInformation(
            BuiltinTypeFormatter.FormatConstructorSignature(constructor, includeNewKeyword),
            FormatConstructor(constructor, locale, includeNewKeyword),
            parameters);
    }

    public static CompletionItemKind ToCompletionKind(BuiltinApiKind kind)
    {
        return kind switch
        {
            BuiltinApiKind.Constructor => CompletionItemKind.Constructor,
            BuiltinApiKind.Type => CompletionItemKind.Type,
            BuiltinApiKind.Object => CompletionItemKind.Object,
            BuiltinApiKind.Function => CompletionItemKind.Function,
            BuiltinApiKind.Method => CompletionItemKind.Method,
            BuiltinApiKind.Constant => CompletionItemKind.Constant,
            BuiltinApiKind.Property => CompletionItemKind.Property,
            _ => CompletionItemKind.Text
        };
    }

    private static string FormatConstructor(BuiltinApiMember constructor, string? locale, bool includeNewKeyword)
    {
        var builder = new StringBuilder();
        builder.Append("```").Append(MarkdownLanguageId).Append('\n');
        BuiltinTypeFormatter.AppendJsDoc(
            builder,
            constructor.Documentation.GetNotes(locale),
            constructor.Parameters,
            constructor.ReturnType);
        builder.Append(BuiltinTypeFormatter.FormatConstructorSignature(constructor, includeNewKeyword)).Append(";\n```");
        return builder.ToString();
    }

    private static string FormatMappedParameter(BuiltinApiParameter parameter, int index)
    {
        var builder = new StringBuilder();
        if (parameter.Variadic)
        {
            builder.Append("...");
        }

        builder
            .Append(BuiltinTypeFormatter.SafeParameterName(parameter.Name, index))
            .Append(": ")
            .Append(BuiltinTypeFormatter.FormatType(parameter.Type, BuiltinTypeFormatter.TypeUsage.Value, parameter.Optional, parameter.Variadic));
        return builder.ToString();
    }

    private static string FormatKind(BuiltinApiKind kind)
    {
        return kind switch
        {
            BuiltinApiKind.Constructor => "constructor",
            BuiltinApiKind.Type => "type",
            BuiltinApiKind.Object => "object",
            BuiltinApiKind.Function => "func",
            BuiltinApiKind.Method => "method",
            BuiltinApiKind.Property => "property",
            BuiltinApiKind.Constant => "const",
            _ => "symbol"
        };
    }
}
