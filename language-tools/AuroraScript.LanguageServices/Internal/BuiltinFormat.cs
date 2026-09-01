using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Features.SignatureHelp;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.LanguageServices.Internal;

internal static class BuiltinFormat
{
    public static string FormatGlobal(BuiltinApiSymbol symbol, string? locale = null)
    {
        string code;
        if (symbol.Kind == BuiltinApiKind.Constructor && symbol.Constructors.Count != 0)
        {
            var members = new string[symbol.Constructors.Count];
            for (var i = 0; i < symbol.Constructors.Count; i++)
            {
                members[i] = BuiltinTypeFormatter.FormatConstructorSignature(symbol.Constructors[i]);
            }

            code = BuiltinTypeFormatter.FormatDeclareType(symbol.Name, members);
        }
        else if (symbol.Kind == BuiltinApiKind.Function)
        {
            code = "func " + symbol.Name + "() Object;";
        }
        else
        {
            code = BuiltinTypeFormatter.FormatDeclareType(symbol.Name) + ";";
        }

        return BuiltinTypeFormatter.FormatHover(code, symbol.Documentation.GetNotes(locale));
    }

    public static string FormatModule(
        BuiltinApiModule module,
        string? alias = null,
        string? locale = null)
    {
        var code = "import " + (string.IsNullOrWhiteSpace(alias) ? module.Name : alias) +
            " from \"" + module.ModulePath + "\";";
        return BuiltinTypeFormatter.FormatHover(code, module.Documentation.GetNotes(locale));
    }

    public static string FormatMember(BuiltinApiMember member, string? locale = null, bool instanceMember = false)
    {
        var code = BuiltinTypeFormatter.FormatDeclareType(
            member.OwnerName,
            BuiltinTypeFormatter.FormatMemberSignature(member, instanceMember));
        return BuiltinTypeFormatter.FormatHover(code, member.Documentation.GetNotes(locale));
    }

    public static string FormatCompletionDetail(BuiltinApiSymbol symbol)
    {
        if (symbol.Kind == BuiltinApiKind.Constructor && symbol.Constructors.Count != 0)
        {
            return BuiltinTypeFormatter.FormatConstructorSignature(symbol.Constructors[0]);
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

        var signature = BuiltinTypeFormatter.FormatConstructorSignature(constructor);
        return new SignatureInformation(
            signature,
            FormatConstructor(constructor, locale),
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

    private static string FormatConstructor(BuiltinApiMember constructor, string? locale)
    {
        var code = BuiltinTypeFormatter.FormatDeclareType(
            constructor.OwnerName,
            BuiltinTypeFormatter.FormatConstructorSignature(constructor));
        return BuiltinTypeFormatter.FormatHover(code, constructor.Documentation.GetNotes(locale));
    }

    private static string FormatMappedParameter(BuiltinApiParameter parameter, int index)
    {
        var builder = new StringBuilder();
        if (parameter.Variadic)
        {
            builder.Append("...");
        }

        builder
            .Append(BuiltinTypeFormatter.FormatType(parameter.Type, BuiltinTypeFormatter.TypeUsage.Value, parameter.Optional, variadic: false))
            .Append(' ')
            .Append(BuiltinTypeFormatter.SafeParameterName(parameter.Name, index));
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
