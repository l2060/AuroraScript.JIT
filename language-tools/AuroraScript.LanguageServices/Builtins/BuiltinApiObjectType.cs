using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Builtins;

public sealed class BuiltinApiObjectType
{
    private static readonly IReadOnlyDictionary<string, BuiltinApiMember> EmptyMembers =
        new Dictionary<string, BuiltinApiMember>(StringComparer.Ordinal);

    public BuiltinApiObjectType(
        string name,
        IReadOnlyDictionary<string, BuiltinApiMember> members,
        BuiltinApiDocumentation documentation)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Object type name is required.", nameof(name))
            : name;
        Members = members ?? EmptyMembers;
        Documentation = documentation ?? BuiltinApiDocumentation.Empty;
    }

    public string Name { get; }
    public IReadOnlyDictionary<string, BuiltinApiMember> Members { get; }
    public BuiltinApiDocumentation Documentation { get; }

    public bool TryGetMember(string name, out BuiltinApiMember member)
    {
        return Members.TryGetValue(name, out member!);
    }
}
