using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Features.SignatureHelp;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.LanguageServices.Internal;

internal static class BuiltinFormat
{
    public static string FormatGlobal(BuiltinApiSymbol symbol)
    {
        var builder = new StringBuilder();
        builder.Append("```aurora\n");
        if (symbol.ReadOnly)
        {
            builder.Append("readonly ");
        }
        builder.Append(FormatKind(symbol.Kind)).Append(' ').Append(symbol.Name).Append("\n```");
        AppendNotes(builder, symbol.Notes);
        return builder.ToString();
    }

    public static string FormatMember(BuiltinApiMember member)
    {
        var builder = new StringBuilder();
        builder.Append("```aurora\n");
        if (member.ReadOnly)
        {
            builder.Append("readonly ");
        }

        if (member.Kind == BuiltinApiKind.Method || member.Kind == BuiltinApiKind.Function)
        {
            builder.Append("func ").Append(member.FullName).Append('(');
            AppendParameters(builder, member.Parameters);
            builder.Append("): ").Append(member.ReturnType);
        }
        else
        {
            builder.Append(FormatKind(member.Kind)).Append(' ').Append(member.FullName).Append(": ").Append(member.ReturnType);
        }

        builder.Append("\n```");
        AppendNotes(builder, member.Notes);
        return builder.ToString();
    }

    public static string FormatCompletionDetail(BuiltinApiSymbol symbol)
    {
        return (symbol.ReadOnly ? "readonly " : string.Empty) + FormatKind(symbol.Kind);
    }

    public static string FormatCompletionDetail(BuiltinApiMember member)
    {
        if (member.Kind == BuiltinApiKind.Method || member.Kind == BuiltinApiKind.Function)
        {
            return FormatSignature(member);
        }

        return (member.ReadOnly ? "readonly " : string.Empty) + member.ReturnType;
    }

    public static SignatureInformation FormatSignatureInfo(BuiltinApiMember member)
    {
        var parameters = new List<SignatureParameter>(member.Parameters.Count);
        for (var i = 0; i < member.Parameters.Count; i++)
        {
            var parameter = member.Parameters[i];
            parameters.Add(new SignatureParameter(FormatParameter(parameter), parameter.Type));
        }

        return new SignatureInformation(FormatSignature(member), FormatMember(member), parameters);
    }

    public static CompletionItemKind ToCompletionKind(BuiltinApiKind kind)
    {
        return kind switch
        {
            BuiltinApiKind.Constructor => CompletionItemKind.Constructor,
            BuiltinApiKind.Object => CompletionItemKind.Object,
            BuiltinApiKind.Function => CompletionItemKind.Function,
            BuiltinApiKind.Method => CompletionItemKind.Method,
            BuiltinApiKind.Constant => CompletionItemKind.Constant,
            BuiltinApiKind.Property => CompletionItemKind.Property,
            _ => CompletionItemKind.Text
        };
    }

    private static string FormatSignature(BuiltinApiMember member)
    {
        var builder = new StringBuilder();
        builder.Append(member.FullName).Append('(');
        AppendParameters(builder, member.Parameters);
        builder.Append("): ").Append(member.ReturnType);
        return builder.ToString();
    }

    private static void AppendParameters(StringBuilder builder, IReadOnlyList<BuiltinApiParameter> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }
            builder.Append(FormatParameter(parameters[i]));
        }
    }

    private static string FormatParameter(BuiltinApiParameter parameter)
    {
        var builder = new StringBuilder();
        if (parameter.Variadic)
        {
            builder.Append("...");
        }
        builder.Append(parameter.Name).Append(": ").Append(parameter.Type);
        if (parameter.Optional)
        {
            builder.Append('?');
        }
        return builder.ToString();
    }

    private static string FormatKind(BuiltinApiKind kind)
    {
        return kind switch
        {
            BuiltinApiKind.Constructor => "constructor",
            BuiltinApiKind.Object => "object",
            BuiltinApiKind.Function => "func",
            BuiltinApiKind.Method => "method",
            BuiltinApiKind.Property => "property",
            BuiltinApiKind.Constant => "const",
            _ => "symbol"
        };
    }

    private static void AppendNotes(StringBuilder builder, IReadOnlyList<string> notes)
    {
        for (var i = 0; i < notes.Count; i++)
        {
            builder.Append("\n\n").Append(notes[i]);
        }
    }
}
