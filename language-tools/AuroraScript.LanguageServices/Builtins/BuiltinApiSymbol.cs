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
        bool callable,
        IReadOnlyList<BuiltinApiMember> constructors,
        BuiltinApiDocumentation documentation,
        IReadOnlyDictionary<string, BuiltinApiMember> members)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Symbol name is required.", nameof(name)) : name;
        Kind = kind;
        ReadOnly = readOnly;
        Callable = callable;
        Constructors = constructors ?? Array.Empty<BuiltinApiMember>();
        Documentation = documentation ?? BuiltinApiDocumentation.Empty;
        _members = new Dictionary<string, BuiltinApiMember>(members ?? EmptyMembers, StringComparer.Ordinal);
        Members = _members;
    }

    private static readonly IReadOnlyDictionary<string, BuiltinApiMember> EmptyMembers =
        new Dictionary<string, BuiltinApiMember>(StringComparer.Ordinal);

    public string Name { get; }
    public BuiltinApiKind Kind { get; }
    public bool ReadOnly { get; }
    public bool Callable { get; }
    public IReadOnlyList<BuiltinApiMember> Constructors { get; }
    public BuiltinApiDocumentation Documentation { get; }
    public IReadOnlyList<string> Notes => Documentation.GetNotes(null);
    public IReadOnlyDictionary<string, BuiltinApiMember> Members { get; }

    public bool TryGetMember(string name, out BuiltinApiMember member)
    {
        return _members.TryGetValue(name, out member!);
    }
}
