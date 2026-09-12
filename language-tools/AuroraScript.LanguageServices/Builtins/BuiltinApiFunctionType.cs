using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Builtins;

public sealed class BuiltinApiFunctionType
{
    public BuiltinApiFunctionType(
        string name,
        IReadOnlyList<BuiltinApiParameter> parameters,
        string returnType,
        BuiltinApiDocumentation documentation)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Function type name is required.", nameof(name))
            : name;
        Parameters = parameters ?? Array.Empty<BuiltinApiParameter>();
        ReturnType = string.IsNullOrWhiteSpace(returnType) ? "any" : returnType;
        Documentation = documentation ?? BuiltinApiDocumentation.Empty;
    }

    public string Name { get; }
    public IReadOnlyList<BuiltinApiParameter> Parameters { get; }
    public string ReturnType { get; }
    public BuiltinApiDocumentation Documentation { get; }
}
