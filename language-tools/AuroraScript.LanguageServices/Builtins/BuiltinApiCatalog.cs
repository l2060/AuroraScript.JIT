using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Builtins;

public sealed class BuiltinApiCatalog
{
    private readonly Dictionary<string, BuiltinApiSymbol> _globals;
    private readonly Dictionary<string, BuiltinApiModule> _modules;
    private readonly Dictionary<string, IReadOnlyDictionary<string, BuiltinApiMember>> _prototypes;

    public BuiltinApiCatalog(
        string versionTarget,
        IReadOnlyDictionary<string, BuiltinApiSymbol> globals,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, BuiltinApiMember>> prototypes)
        : this(versionTarget, globals, prototypes, EmptyModules)
    {
    }

    public BuiltinApiCatalog(
        string versionTarget,
        IReadOnlyDictionary<string, BuiltinApiSymbol> globals,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, BuiltinApiMember>> prototypes,
        IReadOnlyDictionary<string, BuiltinApiModule> modules)
    {
        VersionTarget = versionTarget ?? string.Empty;
        _globals = new Dictionary<string, BuiltinApiSymbol>(globals ?? EmptyGlobals, StringComparer.Ordinal);
        _modules = new Dictionary<string, BuiltinApiModule>(modules ?? EmptyModules, StringComparer.Ordinal);
        _prototypes = new Dictionary<string, IReadOnlyDictionary<string, BuiltinApiMember>>(prototypes ?? EmptyPrototypes, StringComparer.Ordinal);
        Globals = _globals;
        Modules = _modules;
        Prototypes = _prototypes;
    }

    private static readonly IReadOnlyDictionary<string, BuiltinApiSymbol> EmptyGlobals =
        new Dictionary<string, BuiltinApiSymbol>(StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, BuiltinApiModule> EmptyModules =
        new Dictionary<string, BuiltinApiModule>(StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, BuiltinApiMember>> EmptyPrototypes =
        new Dictionary<string, IReadOnlyDictionary<string, BuiltinApiMember>>(StringComparer.Ordinal);

    public string VersionTarget { get; }
    public IReadOnlyDictionary<string, BuiltinApiSymbol> Globals { get; }
    public IReadOnlyDictionary<string, BuiltinApiModule> Modules { get; }
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, BuiltinApiMember>> Prototypes { get; }

    public bool TryGetGlobal(string name, out BuiltinApiSymbol symbol)
    {
        return _globals.TryGetValue(name, out symbol!);
    }

    public bool TryGetGlobalMember(string ownerName, string memberName, out BuiltinApiMember member)
    {
        member = null!;
        return _globals.TryGetValue(ownerName, out var owner) && owner.TryGetMember(memberName, out member);
    }

    public bool TryGetModule(string modulePath, out BuiltinApiModule module)
    {
        return _modules.TryGetValue(modulePath, out module!);
    }

    public bool TryGetModuleMember(string modulePath, string memberName, out BuiltinApiMember member)
    {
        member = null!;
        return _modules.TryGetValue(modulePath, out var module) &&
            module.TryGetMember(memberName, out member);
    }

    public bool TryGetPrototypeMember(string prototypeName, string memberName, out BuiltinApiMember member)
    {
        if (_prototypes.TryGetValue(prototypeName, out var members) &&
            members.TryGetValue(memberName, out var resolved))
        {
            member = resolved;
            return true;
        }

        member = null!;
        return false;
    }

    public bool TryGetPrototype(
        string prototypeName,
        out IReadOnlyDictionary<string, BuiltinApiMember> members)
    {
        return _prototypes.TryGetValue(prototypeName, out members!);
    }
}
