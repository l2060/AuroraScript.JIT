using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Features.SignatureHelp;
using System;
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
        builder.Append("export declare ").Append(symbol.Name);
        if (symbol.Kind == BuiltinApiKind.Function)
        {
            builder.Append("(): Object");
        }
        builder.Append(";\n```");
        AppendNotes(builder, symbol.Documentation.GetNotes(locale));
        return builder.ToString();
    }

    public static string FormatMember(BuiltinApiMember member, string? locale = null)
    {
        var builder = new StringBuilder();
        builder.Append("```").Append(MarkdownLanguageId).Append('\n');
        builder.Append("export declare ").Append(member.FullName);

        if (member.Kind == BuiltinApiKind.Method || member.Kind == BuiltinApiKind.Function)
        {
            builder.Append('(');
            AppendMappedParameters(builder, member.Parameters);
            builder.Append("): ").Append(FormatType(member.ReturnType, TypeUsage.Return, optional: false, variadic: false));
        }
        else
        {
            builder.Append(": ").Append(FormatType(member.ReturnType, TypeUsage.Value, optional: false, variadic: false));
        }

        builder.Append(";\n```");
        AppendNotes(builder, member.Documentation.GetNotes(locale));
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

    public static SignatureInformation FormatSignatureInfo(BuiltinApiMember member, string? locale = null)
    {
        var parameters = new List<SignatureParameter>(member.Parameters.Count);
        for (var i = 0; i < member.Parameters.Count; i++)
        {
            var parameter = member.Parameters[i];
            parameters.Add(new SignatureParameter(FormatParameter(parameter), parameter.Type));
        }

        return new SignatureInformation(FormatSignature(member), FormatMember(member, locale), parameters);
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

    private static void AppendMappedParameters(StringBuilder builder, IReadOnlyList<BuiltinApiParameter> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            var parameter = parameters[i];
            if (parameter.Variadic)
            {
                builder.Append("...");
            }

            builder
                .Append(SafeParameterName(parameter.Name, i))
                .Append(": ")
                .Append(FormatType(parameter.Type, TypeUsage.Value, parameter.Optional, parameter.Variadic));
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

    private static string FormatType(string rawType, TypeUsage usage, bool optional, bool variadic)
    {
        var type = FormatTypeCore(rawType, usage);
        if (optional && !ContainsNullType(rawType) && !string.Equals(type, "void", StringComparison.Ordinal))
        {
            type += " | null";
        }

        if (!variadic)
        {
            return type;
        }

        return type.Contains(" | ", StringComparison.Ordinal)
            ? "(" + type + ")[]"
            : type + "[]";
    }

    private static string FormatTypeCore(string rawType, TypeUsage usage)
    {
        if (string.IsNullOrWhiteSpace(rawType))
        {
            return "Object";
        }

        var parts = rawType.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > 1)
        {
            var mapped = new List<string>(parts.Length);
            for (var i = 0; i < parts.Length; i++)
            {
                var part = MapSingleType(parts[i], usage);
                if (!mapped.Contains(part))
                {
                    mapped.Add(part);
                }
            }

            return string.Join(" | ", mapped);
        }

        return MapSingleType(rawType, usage);
    }

    private static string MapSingleType(string rawType, TypeUsage usage)
    {
        var trimmed = rawType.Trim();
        if (trimmed.EndsWith("[]", StringComparison.Ordinal))
        {
            return MapSingleType(trimmed.Substring(0, trimmed.Length - 2), usage) + "[]";
        }

        return trimmed.ToLowerInvariant() switch
        {
            "number" => "Number",
            "string" => "String",
            "boolean" => "Boolean",
            "bool" => "Boolean",
            "array" => "Array",
            "date" => "Date",
            "object" => "Object",
            "any" => "Object",
            "regex" => "Regex",
            "regexp" => "Regex",
            "function" => "Function",
            "func" => "Function",
            "null" => usage == TypeUsage.Return ? "void" : "null",
            "undefined" => usage == TypeUsage.Return ? "void" : "null",
            "void" => "void",
            _ => trimmed
        };
    }

    private static bool ContainsNullType(string rawType)
    {
        var parts = rawType.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (string.Equals(part, "null", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(part, "undefined", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(part, "void", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string SafeParameterName(string name, int index)
    {
        if (IsIdentifier(name))
        {
            return name;
        }

        return "arg" + index;
    }

    private static bool IsIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !IsIdentifierStart(value[0]))
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            if (!IsIdentifierPart(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_' || value == '$';
    }

    private static bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_' || value == '$';
    }

    private static void AppendNotes(StringBuilder builder, IReadOnlyList<string> notes)
    {
        for (var i = 0; i < notes.Count; i++)
        {
            builder.Append("\n\n").Append(notes[i]);
        }
    }

    private enum TypeUsage
    {
        Value,
        Return
    }
}
