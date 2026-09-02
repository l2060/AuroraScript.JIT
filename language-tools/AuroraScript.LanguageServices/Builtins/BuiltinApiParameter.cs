using System;

namespace AuroraScript.LanguageServices.Builtins;

public sealed class BuiltinApiParameter
{
    public BuiltinApiParameter(string name, string type, bool optional, bool variadic, string? defaultValue = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Parameter name is required.", nameof(name)) : name;
        Type = string.IsNullOrWhiteSpace(type) ? "any" : type;
        DefaultValue = string.IsNullOrWhiteSpace(defaultValue) ? null : defaultValue;
        Optional = optional || DefaultValue != null;
        Variadic = variadic;
    }

    public string Name { get; }
    public string Type { get; }
    public bool Optional { get; }
    public bool Variadic { get; }
    public string? DefaultValue { get; }
}
