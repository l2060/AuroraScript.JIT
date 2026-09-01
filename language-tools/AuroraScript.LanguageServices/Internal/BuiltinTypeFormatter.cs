using AuroraScript.LanguageServices.Builtins;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.LanguageServices.Internal;

internal static class BuiltinTypeFormatter
{
    public const string MarkdownLanguageId = "aurorascript";

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
            type += " | Null";
        }

        if (!variadic)
        {
            return type;
        }

        return type;
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
                .Append(FormatType(parameter.Type, TypeUsage.Value, parameter.Optional, variadic: false))
                .Append(' ')
                .Append(SafeParameterName(parameter.Name, i));
        }
    }

    public static string FormatMemberSignature(BuiltinApiMember member, bool instanceMember = false)
    {
        var builder = new StringBuilder();
        if (!instanceMember)
        {
            builder.Append("static ");
        }

        if (member.Kind is BuiltinApiKind.Method or BuiltinApiKind.Function)
        {
            builder.Append("func ").Append(member.Name).Append('(');
            AppendMappedParameters(builder, member.Parameters);
            builder.Append(") ").Append(FormatType(member.ReturnType, TypeUsage.Return, optional: false, variadic: false));
            return builder.ToString();
        }

        if (member.Kind == BuiltinApiKind.Constant || member.ReadOnly)
        {
            builder.Append("const ");
        }

        builder
            .Append(FormatType(member.ReturnType, TypeUsage.Value, optional: false, variadic: false))
            .Append(' ')
            .Append(member.Name);
        return builder.ToString();
    }

    public static string FormatConstructorSignature(BuiltinApiMember constructor, bool includeNewKeyword = false)
    {
        var builder = new StringBuilder();
        builder.Append("constructor(");
        AppendMappedParameters(builder, constructor.Parameters);
        builder.Append(')');
        return builder.ToString();
    }

    public static string FormatHover(string code, IReadOnlyList<string>? notes = null)
    {
        var builder = new StringBuilder();
        AppendCodeFence(builder, code);
        AppendMarkdownNotes(builder, notes);
        return builder.ToString();
    }

    public static void AppendCodeFence(StringBuilder builder, string code)
    {
        builder.Append("```").Append(MarkdownLanguageId).Append('\n');
        builder.Append(code);
        if (code.Length == 0 || code[code.Length - 1] != '\n')
        {
            builder.Append('\n');
        }

        builder.Append("```");
    }

    public static void AppendMarkdownNotes(StringBuilder builder, IReadOnlyList<string>? notes)
    {
        if (notes == null || notes.Count == 0)
        {
            return;
        }

        for (var i = 0; i < notes.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(notes[i]))
            {
                continue;
            }

            builder.Append("\n\n").Append(notes[i]);
        }
    }

    public static string FormatDeclareType(string typeName, params string[] members)
    {
        var builder = new StringBuilder();
        builder.Append("declare type ").Append(typeName);
        if (members == null || members.Length == 0)
        {
            return builder.ToString();
        }

        builder.Append(" {\n");
        for (var i = 0; i < members.Length; i++)
        {
            builder.Append("    ").Append(members[i]).Append(";\n");
        }

        builder.Append('}');
        return builder.ToString();
    }

    private static string FormatTypeCore(string rawType, TypeUsage usage)
    {
        if (string.IsNullOrWhiteSpace(rawType))
        {
            return "Object";
        }

        var parts = rawType.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            return MapSingleType(parts[0], usage, wholeType: true);
        }

        var mapped = new List<string>(parts.Length);
        for (var i = 0; i < parts.Length; i++)
        {
            var part = MapSingleType(parts[i], TypeUsage.Value, wholeType: false);
            if (!mapped.Contains(part))
            {
                mapped.Add(part);
            }
        }

        return string.Join(" | ", mapped);
    }

    private static string MapSingleType(string rawType, TypeUsage usage, bool wholeType)
    {
        var trimmed = rawType.Trim();
        if (trimmed.EndsWith("[]", StringComparison.Ordinal))
        {
            return MapSingleType(trimmed.Substring(0, trimmed.Length - 2), usage, wholeType) + "[]";
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
            "null" => usage == TypeUsage.Return && wholeType ? "void" : "Null",
            "undefined" => usage == TypeUsage.Return && wholeType ? "void" : "Null",
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
