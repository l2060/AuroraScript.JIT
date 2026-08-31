using AuroraScript.Compiler.GlobalDeclarations;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal;

internal sealed class SemanticExternalSymbols
{
    public static readonly SemanticExternalSymbols Empty = new(
        new Dictionary<string, int>(StringComparer.Ordinal),
        new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal),
        new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal));

    private readonly Dictionary<string, int> _globals;
    private readonly Dictionary<string, Dictionary<string, int>> _members;
    private readonly Dictionary<string, Dictionary<string, int>> _instanceMembers;
    private readonly HashSet<string> _classes;

    private SemanticExternalSymbols(
        Dictionary<string, int> globals,
        Dictionary<string, Dictionary<string, int>> members,
        Dictionary<string, Dictionary<string, int>> instanceMembers,
        HashSet<string> classes)
    {
        _globals = globals;
        _members = members;
        _instanceMembers = instanceMembers;
        _classes = classes;
    }

    public static SemanticExternalSymbols FromGlobalDeclarationIndex(GlobalDeclarationIndex index)
    {
        var globals = new Dictionary<string, int>(StringComparer.Ordinal);
        var members = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        var instanceMembers = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        var classes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var declaration in index.Declarations.Values)
        {
            globals[declaration.Name] = declaration.Kind switch
            {
                GlobalDeclarationKind.Function => AuroraSemanticTokenTypes.DeclaredGlobalFunction,
                GlobalDeclarationKind.Type => AuroraSemanticTokenTypes.Type,
                _ => AuroraSemanticTokenTypes.DeclaredGlobal
            };

            if (declaration.Kind != GlobalDeclarationKind.Type ||
                declaration.Members.Count == 0)
            {
                continue;
            }

            classes.Add(declaration.Name);

            var staticMap = new Dictionary<string, int>(StringComparer.Ordinal);
            var instanceMap = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < declaration.Members.Count; i++)
            {
                var member = declaration.Members[i];
                if (StringComparer.Ordinal.Equals(member.Name, "constructor"))
                {
                    continue;
                }

                var tokenType = member.Kind == GlobalDeclarationKind.Function
                    ? AuroraSemanticTokenTypes.Method
                    : member.Kind == GlobalDeclarationKind.Const
                        ? AuroraSemanticTokenTypes.DeclaredGlobal
                        : AuroraSemanticTokenTypes.Property;
                if (member.IsStatic)
                {
                    staticMap[member.Name] = tokenType;
                }
                else
                {
                    instanceMap[member.Name] = tokenType;
                }
            }

            if (staticMap.Count != 0)
            {
                members[declaration.Name] = staticMap;
            }

            if (instanceMap.Count != 0)
            {
                instanceMembers[declaration.Name] = instanceMap;
            }
        }

        return globals.Count == 0
            ? Empty
            : new SemanticExternalSymbols(globals, members, instanceMembers, classes);
    }

    public bool TryResolveGlobal(string name, out int type)
    {
        return _globals.TryGetValue(name, out type);
    }

    public bool TryResolveMember(string ownerName, string memberName, out int type)
    {
        type = -1;
        return _members.TryGetValue(ownerName, out var members) &&
            members.TryGetValue(memberName, out type);
    }

    public bool TryResolveInstanceMember(string className, string memberName, out int type)
    {
        type = -1;
        return _instanceMembers.TryGetValue(className, out var members) &&
            members.TryGetValue(memberName, out type);
    }

    public bool IsClass(string name)
    {
        return _classes.Contains(name);
    }

}
