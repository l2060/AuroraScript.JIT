using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Builtins;

public sealed class BuiltinApiMember
{
    public BuiltinApiMember(
        string ownerName,
        string name,
        BuiltinApiKind kind,
        string returnType,
        bool readOnly,
        IReadOnlyList<BuiltinApiParameter> parameters,
        IReadOnlyList<string> notes)
    {
        OwnerName = ownerName ?? string.Empty;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Member name is required.", nameof(name)) : name;
        Kind = kind;
        ReturnType = string.IsNullOrWhiteSpace(returnType) ? "any" : returnType;
        ReadOnly = readOnly;
        Parameters = parameters ?? Array.Empty<BuiltinApiParameter>();
        Notes = notes ?? Array.Empty<string>();
    }

    public string OwnerName { get; }
    public string Name { get; }
    public BuiltinApiKind Kind { get; }
    public string ReturnType { get; }
    public bool ReadOnly { get; }
    public IReadOnlyList<BuiltinApiParameter> Parameters { get; }
    public IReadOnlyList<string> Notes { get; }

    public string FullName => string.IsNullOrEmpty(OwnerName) ? Name : OwnerName + "." + Name;
}
