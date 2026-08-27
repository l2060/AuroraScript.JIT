using AuroraScript.LanguageServices.Builtins;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.LanguageServices.Internal;

internal static class BuiltinTypeFormatter
{
    public enum TypeUsage
    {
        Value,
        Return
    }

    public static string FormatType(string rawType, TypeUsage usage, bool optional, bool variadic)
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

    public static string SafeParameterName(string name, int index)
    {
        if (IsIdentifier(name))
        {
            return name;
        }

        return "arg" + index;
    }

    public static void AppendMappedParameters(StringBuilder builder, IReadOnlyList<BuiltinApiParameter> parameters)
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

    public static string FormatMemberSignature(BuiltinApiMember member)
    {
        var builder = new StringBuilder();
        builder.Append(member.FullName);
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

        return builder.ToString();
    }

    public static string FormatConstructorSignature(BuiltinApiMember constructor, bool includeNewKeyword)
    {
        var builder = new StringBuilder();
        if (includeNewKeyword)
        {
            builder.Append("new ");
        }

        builder.Append(constructor.Name).Append('(');
        AppendMappedParameters(builder, constructor.Parameters);
        builder.Append("): ").Append(FormatType(constructor.ReturnType, TypeUsage.Return, optional: false, variadic: false));
        return builder.ToString();
    }

    public static void AppendJsDoc(
        StringBuilder builder,
        IReadOnlyList<string> notes,
        IReadOnlyList<BuiltinApiParameter>? parameters,
        string? returnType)
    {
        var hasParameters = parameters != null && parameters.Count != 0;
        var hasReturnType = !string.IsNullOrWhiteSpace(returnType);
        if (notes.Count == 0 && !hasParameters && !hasReturnType)
        {
            return;
        }

        builder.Append("/**\n");
        for (var i = 0; i < notes.Count; i++)
        {
            builder.Append("* ").Append(notes[i]).Append('\n');
        }

        if (hasParameters)
        {
            for (var i = 0; i < parameters!.Count; i++)
            {
                var parameter = parameters[i];
                builder
                    .Append("* @param ")
                    .Append(SafeParameterName(parameter.Name, i))
                    .Append(' ')
                    .Append(FormatType(parameter.Type, TypeUsage.Value, parameter.Optional, parameter.Variadic))
                    .Append(".\n");
            }
        }

        if (hasReturnType)
        {
            var type = FormatType(returnType!, TypeUsage.Return, optional: false, variadic: false);
            if (!string.Equals(type, "void", StringComparison.Ordinal))
            {
                builder.Append("* @returns ").Append(type).Append(".\n");
            }
        }

        builder.Append("*/\n");
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
}
