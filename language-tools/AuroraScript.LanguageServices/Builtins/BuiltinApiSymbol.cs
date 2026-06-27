using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Builtins;

public sealed class BuiltinApiSymbol
{
    private readonly Dictionary<string, BuiltinApiMember> _members;

    public BuiltinApiSymbol(
        string name,
        BuiltinApiKind kind,
        bool readOnly,
        IReadOnlyList<string> notes,
        IReadOnlyDictionary<string, BuiltinApiMember> members)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Symbol name is required.", nameof(name)) : name;
        Kind = kind;
        ReadOnly = readOnly;
        Notes = notes ?? Array.Empty<string>();
        _members = new Dictionary<string, BuiltinApiMember>(members ?? EmptyMembers, StringComparer.Ordinal);
        Members = _members;
    }

    private static readonly IReadOnlyDictionary<string, BuiltinApiMember> EmptyMembers =
        new Dictionary<string, BuiltinApiMember>(StringComparer.Ordinal);

    public string Name { get; }
    public BuiltinApiKind Kind { get; }
    public bool ReadOnly { get; }
    public IReadOnlyList<string> Notes { get; }
    public IReadOnlyDictionary<string, BuiltinApiMember> Members { get; }

    public bool TryGetMember(string name, out BuiltinApiMember member)
    {
        return _members.TryGetValue(name, out member!);
    }
}
