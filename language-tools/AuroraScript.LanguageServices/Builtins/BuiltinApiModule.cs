using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Builtins;

/// <summary>
/// Describes an opt-in native module exposed through an AuroraScript import.
/// </summary>
public sealed class BuiltinApiModule
{
    private readonly Dictionary<string, BuiltinApiMember> _members;

    public BuiltinApiModule(
        string name,
        string modulePath,
        BuiltinApiDocumentation documentation,
        IReadOnlyDictionary<string, BuiltinApiMember> members)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Module name is required.", nameof(name))
            : name;
        ModulePath = string.IsNullOrWhiteSpace(modulePath)
            ? throw new ArgumentException("Module path is required.", nameof(modulePath))
            : modulePath;
        Documentation = documentation ?? BuiltinApiDocumentation.Empty;
        _members = new Dictionary<string, BuiltinApiMember>(members ?? EmptyMembers, StringComparer.Ordinal);
        Members = _members;
    }

    private static readonly IReadOnlyDictionary<string, BuiltinApiMember> EmptyMembers =
        new Dictionary<string, BuiltinApiMember>(StringComparer.Ordinal);

    public string Name { get; }
    public string ModulePath { get; }
    public BuiltinApiDocumentation Documentation { get; }
    public IReadOnlyList<string> Notes => Documentation.GetNotes(null);
    public IReadOnlyDictionary<string, BuiltinApiMember> Members { get; }

    public bool TryGetMember(string name, out BuiltinApiMember member)
    {
        return _members.TryGetValue(name, out member!);
    }
}
