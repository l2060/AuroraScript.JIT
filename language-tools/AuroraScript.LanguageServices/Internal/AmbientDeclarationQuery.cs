using AuroraScript.Compiler;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.GlobalDeclarations;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Features.Definition;
using AuroraScript.LanguageServices.Features.Hover;
using AuroraScript.LanguageServices.Features.SignatureHelp;
using AuroraScript.LanguageServices.Internal.SymbolIndex;
using AuroraScript.LanguageServices.Parsing;
using AuroraScript.LanguageServices.Text;
using AuroraScript.LanguageServices.Workspace;
using AuroraScript.Source;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.LanguageServices.Internal;

internal sealed class AmbientContractCatalog
{
    public static readonly AmbientContractCatalog Empty = new(
        GlobalDeclarationIndex.Empty,
        new Dictionary<string, AmbientDeclaration>(StringComparer.Ordinal));

    private readonly GlobalDeclarationIndex _index;
    private readonly Dictionary<string, AmbientDeclaration> _containers;

    private AmbientContractCatalog(
        GlobalDeclarationIndex index,
        Dictionary<string, AmbientDeclaration> containers)
    {
        _index = index;
        _containers = containers;
    }

    public GlobalDeclarationIndex Index => _index;

    public static AmbientContractCatalog Build(
        GlobalDeclarationIndex index,
        AuroraWorkspaceSnapshot? snapshot,
        AuroraParseService? parseService)
    {
        var containers = new Dictionary<string, AmbientDeclaration>(StringComparer.Ordinal);
        if (snapshot != null && parseService != null)
        {
            foreach (var document in snapshot.Documents.Values)
            {
                if (!GlobalDeclarationScanner.IsGlobalFile(document.Text))
                {
                    continue;
                }

                var parsed = parseService.ParseText(document.Path, document.Text, snapshot.BaseDirectory, snapshot);
                if (parsed.Module == null)
                {
                    continue;
                }

                for (var i = 0; i < parsed.Module.AmbientDeclarations.Count; i++)
                {
                    var ambient = parsed.Module.AmbientDeclarations[i];
                    if (ambient.Name != null)
                    {
                        containers[ambient.Name.Value] = ambient;
                    }
                }
            }
        }

        return new AmbientContractCatalog(index, containers);
    }

    public bool TryGetRoot(string name, out GlobalDeclarationInfo declaration)
    {
        return _index.TryGet(name, out declaration);
    }

    public bool TryGetContainer(string name, out AmbientDeclaration declaration)
    {
        return _containers.TryGetValue(name, out declaration);
    }

    public bool TryGetMember(
        string ownerName,
        string memberName,
        bool instanceMembers,
        out AmbientMemberDeclaration member)
    {
        member = null!;
        if (!TryGetContainer(ownerName, out var container))
        {
            return false;
        }

        for (var i = 0; i < container.Members.Count; i++)
        {
            var candidate = container.Members[i];
            if (candidate.Kind == AmbientMemberKind.Constructor)
            {
                continue;
            }

            if (!MatchesRequestedSpace(candidate.IsStatic, instanceMembers))
            {
                continue;
            }

            if (StringComparer.Ordinal.Equals(candidate.Name.Value, memberName))
            {
                member = candidate;
                return true;
            }
        }

        if (_index.TryGet(ownerName, out var info))
        {
            for (var i = 0; i < info.Members.Count; i++)
            {
                var scannerMember = info.Members[i];
                if (!MatchesRequestedSpace(scannerMember.IsStatic, instanceMembers))
                {
                    continue;
                }

                if (StringComparer.Ordinal.Equals(scannerMember.Name, memberName) &&
                    !StringComparer.Ordinal.Equals(scannerMember.Name, "constructor"))
                {
                    member = new AmbientMemberDeclaration(
                        scannerMember.Kind == GlobalDeclarationKind.Const
                            ? AmbientMemberKind.Const
                            : scannerMember.Kind == GlobalDeclarationKind.Function
                                ? AmbientMemberKind.Function
                                : AmbientMemberKind.Var,
                        CreateName(scannerMember.Name, scannerMember.NameRange),
                        scannerMember.IsStatic);
                    member.Range = scannerMember.NameRange;
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryGetConstructor(string className, out AmbientMemberDeclaration constructor)
    {
        constructor = null!;
        if (!TryGetContainer(className, out var container) ||
            container.Kind != AmbientDeclarationKind.Type)
        {
            return false;
        }

        for (var i = 0; i < container.Members.Count; i++)
        {
            if (container.Members[i].Kind == AmbientMemberKind.Constructor)
            {
                constructor = container.Members[i];
                return true;
            }
        }

        return false;
    }

    public IEnumerable<AmbientMemberDeclaration> GetMembers(string ownerName, bool instanceMembers)
    {
        if (TryGetContainer(ownerName, out var container))
        {
            for (var i = 0; i < container.Members.Count; i++)
            {
                var member = container.Members[i];
                if (member.Kind == AmbientMemberKind.Constructor)
                {
                    continue;
                }

                if (MatchesRequestedSpace(member.IsStatic, instanceMembers))
                {
                    yield return member;
                }
            }

            yield break;
        }

        if (_index.TryGet(ownerName, out var info))
        {
            for (var i = 0; i < info.Members.Count; i++)
            {
                var scannerMember = info.Members[i];
                if (StringComparer.Ordinal.Equals(scannerMember.Name, "constructor") ||
                    !MatchesRequestedSpace(scannerMember.IsStatic, instanceMembers))
                {
                    continue;
                }

                var member = new AmbientMemberDeclaration(
                    scannerMember.Kind == GlobalDeclarationKind.Const
                        ? AmbientMemberKind.Const
                        : scannerMember.Kind == GlobalDeclarationKind.Function
                            ? AmbientMemberKind.Function
                            : AmbientMemberKind.Var,
                    CreateName(scannerMember.Name, scannerMember.NameRange),
                    scannerMember.IsStatic);
                member.Range = scannerMember.NameRange;
                yield return member;
            }
        }
    }

    private static IdentifierToken CreateName(string name, SourceSpan range)
    {
        return new IdentifierToken
        {
            Value = name,
            Range = range
        };
    }

    private static bool MatchesRequestedSpace(bool isStatic, bool instanceMembers)
    {
        return isStatic == !instanceMembers;
    }
}

internal static class AmbientDeclarationQuery
{
    public static CompletionResult GetRootCompletions(AmbientContractCatalog catalog)
    {
        var items = new List<CompletionItem>();
        foreach (var declaration in catalog.Index.Declarations.Values)
        {
            items.Add(ToRootCompletion(declaration));
        }

        return new CompletionResult(items);
    }

    public static CompletionResult GetMemberCompletions(
        AmbientContractCatalog catalog,
        string ownerName,
        bool instanceMembers)
    {
        var items = new List<CompletionItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in catalog.GetMembers(ownerName, instanceMembers))
        {
            if (!seen.Add(member.Name.Value))
            {
                continue;
            }

            items.Add(ToMemberCompletion(member, instanceMembers));
        }

        return new CompletionResult(items);
    }

    public static bool TryGetHover(
        AmbientContractCatalog catalog,
        AuroraModuleIndex module,
        AstQueryContext context,
        TextPosition position,
        out HoverResult hover)
    {
        hover = null!;
        if (!TryMatchSymbol(catalog, module, context, position, out var ownerName, out var name, out var instanceMembers, out var range))
        {
            return false;
        }

        return TryGetHover(catalog, ownerName, name, instanceMembers, range, out hover);
    }

    public static bool TryGetHover(
        AmbientContractCatalog catalog,
        string? ownerName,
        string name,
        bool instanceMembers,
        TextRange range,
        out HoverResult hover)
    {
        hover = null!;
        if (string.IsNullOrEmpty(ownerName))
        {
            if (!catalog.TryGetRoot(name, out var root))
            {
                return false;
            }

            hover = new HoverResult(FormatRoot(root, catalog), range);
            return true;
        }

        if (catalog.TryGetMember(ownerName, name, instanceMembers, out var member))
        {
            hover = new HoverResult(FormatMember(ownerName, member), range);
            return true;
        }

        return false;
    }

    public static bool TryGetDefinition(
        AmbientContractCatalog catalog,
        AuroraModuleIndex module,
        AstQueryContext context,
        TextPosition position,
        out DefinitionLocation definition)
    {
        definition = null!;
        if (!TryMatchSymbol(catalog, module, context, position, out var ownerName, out var name, out var instanceMembers, out _))
        {
            return false;
        }

        return TryGetDefinition(catalog, ownerName, name, instanceMembers, out definition);
    }

    public static bool TryGetDefinition(
        AmbientContractCatalog catalog,
        string? ownerName,
        string name,
        bool instanceMembers,
        out DefinitionLocation definition)
    {
        definition = null!;
        if (string.IsNullOrEmpty(ownerName))
        {
            if (!catalog.TryGetRoot(name, out var root))
            {
                return false;
            }

            definition = new DefinitionLocation(root.FilePath, TextRange.FromSourceSpan(root.NameRange));
            return true;
        }

        if (catalog.TryGetMember(ownerName, name, instanceMembers, out var member))
        {
            if (!catalog.TryGetRoot(ownerName, out var owner))
            {
                return false;
            }

            definition = new DefinitionLocation(owner.FilePath, TextRange.FromSourceSpan(member.Name.Range));
            return true;
        }

        return false;
    }

    public static SignatureHelpResult? TryGetSignatureHelp(
        AmbientContractCatalog catalog,
        AuroraModuleIndex? module,
        AstQueryContext context,
        TextPosition position)
    {
        var call = context.Call;
        if (call == null)
        {
            return null;
        }

        AmbientMemberDeclaration? callable = null;
        string ownerName;
        string callableName;
        if (context.NewExpression != null &&
            call.Target is NameExpression constructorName &&
            catalog.TryGetConstructor(constructorName.Identifier.Value, out var constructor))
        {
            callable = constructor;
            ownerName = constructorName.Identifier.Value;
            callableName = constructorName.Identifier.Value;
        }
        else if (call.Target is GetPropertyExpression
            {
                Object: NameExpression owner,
                Property: NameExpression member
            })
        {
            if (catalog.TryGetMember(owner.Identifier.Value, member.Identifier.Value, instanceMembers: false, out var staticMember) &&
                staticMember.Kind == AmbientMemberKind.Function)
            {
                callable = staticMember;
                ownerName = owner.Identifier.Value;
                callableName = member.Identifier.Value;
            }
            else if (module != null &&
                TryGetConstructedClassName(module, owner.Identifier.Value, position, out var className) &&
                catalog.TryGetMember(className, member.Identifier.Value, instanceMembers: true, out var instanceMember) &&
                instanceMember.Kind == AmbientMemberKind.Function)
            {
                callable = instanceMember;
                ownerName = className;
                callableName = member.Identifier.Value;
            }
            else
            {
                return null;
            }
        }
        else if (call.Target is NameExpression functionName &&
            catalog.TryGetRoot(functionName.Identifier.Value, out var root) &&
            root.Kind == GlobalDeclarationKind.Function)
        {
            return FormatBareFunction(root, call, position);
        }
        else
        {
            return null;
        }

        if (callable == null)
        {
            return null;
        }

        var label = callable.Kind == AmbientMemberKind.Constructor
            ? FormatConstructor(ownerName, callable)
            : FormatFunction(callableName, callable);
        var parameters = new List<SignatureParameter>(callable.Parameters.Count);
        for (var i = 0; i < callable.Parameters.Count; i++)
        {
            parameters.Add(new SignatureParameter(
                FormatParameter(callable.Parameters[i]),
                callable.Parameters[i].DeclaredType?.DisplayName ?? string.Empty));
        }

        return new SignatureHelpResult(
            new[] { new SignatureInformation(label, WrapCode(label), parameters) },
            0,
            BuiltinQuery.GetActiveParameter(call, position));
    }

    public static bool TryGetConstructedClassName(
        AuroraModuleIndex module,
        string ownerName,
        TextPosition position,
        out string className)
    {
        className = string.Empty;
        var localIndex = AuroraLocalSymbolIndex.Build(module);
        foreach (var symbol in localIndex.GetVisibleSymbols(position))
        {
            if (!StringComparer.Ordinal.Equals(symbol.Name, ownerName) ||
                !symbol.HasDeclarationRange)
            {
                continue;
            }

            if (TryGetNewClassName(module.Module, ownerName, symbol.DeclarationRange, out className))
            {
                return true;
            }
        }

        if (module.Symbols.TryGetValue(ownerName, out var moduleSymbol) &&
            TryGetNewClassName(module.Module, ownerName, moduleSymbol.NameRange, out className))
        {
            return true;
        }

        return false;
    }

    private static bool TryMatchSymbol(
        AmbientContractCatalog catalog,
        AuroraModuleIndex module,
        AstQueryContext context,
        TextPosition position,
        out string? ownerName,
        out string name,
        out bool instanceMembers,
        out TextRange range)
    {
        ownerName = null;
        name = string.Empty;
        instanceMembers = false;
        range = default;
        var localIndex = AuroraLocalSymbolIndex.Build(module);
        if (context.PropertyAccess != null &&
            context.IsOnPropertyName &&
            context.PropertyAccess.Object is NameExpression owner &&
            context.PropertyAccess.Property is NameExpression property)
        {
            range = TextRange.FromSourceSpan(property.Identifier.Range);
            name = property.Identifier.Value;
            if (StringComparer.Ordinal.Equals(owner.Identifier.Value, "global") &&
                !IsShadowed(module, localIndex, position, "global"))
            {
                return catalog.TryGetRoot(name, out _);
            }

            if (!IsShadowed(module, localIndex, position, owner.Identifier.Value) &&
                catalog.TryGetRoot(owner.Identifier.Value, out _))
            {
                ownerName = owner.Identifier.Value;
                return true;
            }

            if (TryGetConstructedClassName(module, owner.Identifier.Value, position, out var className) &&
                catalog.TryGetRoot(className, out _))
            {
                ownerName = className;
                instanceMembers = true;
                return true;
            }

            return false;
        }

        if (context.PropertyAccess != null &&
            context.IsOnPropertyOwner &&
            context.PropertyAccess.Object is NameExpression ownerToken &&
            !IsShadowed(module, localIndex, position, ownerToken.Identifier.Value))
        {
            name = ownerToken.Identifier.Value;
            range = TextRange.FromSourceSpan(ownerToken.Identifier.Range);
            return catalog.TryGetRoot(name, out _);
        }

        if (context.Name != null &&
            !IsShadowed(module, localIndex, position, context.Name.Identifier.Value))
        {
            name = context.Name.Identifier.Value;
            range = TextRange.FromSourceSpan(context.Name.Identifier.Range);
            return catalog.TryGetRoot(name, out _);
        }

        return false;
    }

    public static bool IsShadowed(
        AuroraModuleIndex module,
        AuroraLocalSymbolIndex localIndex,
        TextPosition position,
        string name)
    {
        foreach (var symbol in localIndex.GetVisibleSymbols(position))
        {
            if (StringComparer.Ordinal.Equals(symbol.Name, name))
            {
                return true;
            }
        }

        return module.Symbols.ContainsKey(name) ||
            module.ImportsByAlias.ContainsKey(name);
    }

    private static CompletionItem ToRootCompletion(GlobalDeclarationInfo declaration)
    {
        return declaration.Kind switch
        {
            GlobalDeclarationKind.Function => new CompletionItem(
                declaration.Name,
                CompletionItemKind.Function,
                "declared function",
                documentation: null,
                readOnly: true),
            GlobalDeclarationKind.Type => new CompletionItem(
                declaration.Name,
                CompletionItemKind.Type,
                "declared type",
                documentation: null,
                readOnly: true),
            GlobalDeclarationKind.Const => new CompletionItem(
                declaration.Name,
                CompletionItemKind.Constant,
                "declared const",
                documentation: null,
                readOnly: true),
            _ => new CompletionItem(
                declaration.Name,
                CompletionItemKind.Variable,
                "declared var",
                documentation: null,
                readOnly: false)
        };
    }

    private static CompletionItem ToMemberCompletion(AmbientMemberDeclaration member, bool instanceMembers)
    {
        var readOnly = member.Kind == AmbientMemberKind.Const;
        var kind = member.Kind == AmbientMemberKind.Function
            ? (instanceMembers ? CompletionItemKind.Method : CompletionItemKind.Function)
            : member.Kind == AmbientMemberKind.Const
                ? CompletionItemKind.Constant
                : CompletionItemKind.Property;
        var detail = member.Kind == AmbientMemberKind.Function
            ? (instanceMembers ? "instance method" : "static function")
            : member.Kind == AmbientMemberKind.Const
                ? (instanceMembers ? "instance const" : "static const")
                : (instanceMembers ? "instance field" : "static field");
        return new CompletionItem(member.Name.Value, kind, detail, FormatMember(string.Empty, member), readOnly);
    }

    private static string FormatRoot(GlobalDeclarationInfo root, AmbientContractCatalog catalog)
    {
        if (catalog.TryGetContainer(root.Name, out var container))
        {
            return WrapCode("declare type " + root.Name);
        }

        return WrapCode(root.Kind switch
        {
            GlobalDeclarationKind.Function => "declare func " + root.Name,
            GlobalDeclarationKind.Const => "declare const " + root.Name,
            GlobalDeclarationKind.Type => "declare type " + root.Name,
            _ => "declare var " + root.Name
        });
    }

    private static string FormatMember(string ownerName, AmbientMemberDeclaration member)
    {
        if (member.Kind == AmbientMemberKind.Constructor)
        {
            return WrapCode(FormatConstructor(ownerName, member));
        }

        if (member.Kind == AmbientMemberKind.Function)
        {
            var prefix = member.IsStatic ? "static " : string.Empty;
            return WrapCode(prefix + FormatFunction(member.Name.Value, member));
        }

        var builder = new StringBuilder();
        if (member.IsStatic)
        {
            builder.Append("static ");
        }

        builder.Append(member.Kind == AmbientMemberKind.Const ? "const " : string.Empty);
        if (member.ReturnType != null)
        {
            builder.Append(member.ReturnType.DisplayName).Append(' ');
        }

        builder.Append(member.Name.Value);
        return WrapCode(builder.ToString());
    }

    private static string FormatFunction(string name, AmbientMemberDeclaration member)
    {
        var builder = new StringBuilder();
        builder.Append("func ").Append(name).Append('(');
        for (var i = 0; i < member.Parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(FormatParameter(member.Parameters[i]));
        }

        builder.Append(')');
        if (member.ReturnType != null)
        {
            builder.Append(' ').Append(member.ReturnType.DisplayName);
        }

        return builder.ToString();
    }

    private static string FormatConstructor(string name, AmbientMemberDeclaration member)
    {
        var builder = new StringBuilder();
        builder.Append("new ").Append(name).Append('(');
        for (var i = 0; i < member.Parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(FormatParameter(member.Parameters[i]));
        }

        builder.Append(')');
        return builder.ToString();
    }

    private static string FormatParameter(ParameterDeclaration parameter)
    {
        var builder = new StringBuilder();
        if (parameter.DeclaredType != null)
        {
            builder.Append(parameter.DeclaredType.DisplayName).Append(' ');
        }

        if (parameter.IsSpreadOperator)
        {
            builder.Append("...");
        }

        builder.Append(parameter.Name?.Value ?? string.Empty);
        return builder.ToString();
    }

    private static SignatureHelpResult? FormatBareFunction(
        GlobalDeclarationInfo root,
        FunctionCallExpression call,
        TextPosition position)
    {
        var label = "declare func " + root.Name;
        return new SignatureHelpResult(
            new[] { new SignatureInformation(label, WrapCode(label), Array.Empty<SignatureParameter>()) },
            0,
            BuiltinQuery.GetActiveParameter(call, position));
    }

    private static string WrapCode(string signature)
    {
        return "```aurorascript\n" + signature + "\n```";
    }

    private static bool TryGetNewClassName(
        ModuleDeclaration module,
        string variableName,
        TextRange declarationRange,
        out string className)
    {
        className = string.Empty;
        if (!TryFindInitializer(module, variableName, declarationRange, out var initializer))
        {
            return false;
        }

        if (initializer is NewExpression { Expression.Target: NameExpression typeName })
        {
            className = typeName.Identifier.Value;
            return !string.IsNullOrEmpty(className);
        }

        return false;
    }

    private static bool TryFindInitializer(
        ModuleDeclaration module,
        string variableName,
        TextRange declarationRange,
        out Expression initializer)
    {
        return TryFindInitializer(module.Statements, variableName, declarationRange, out initializer) ||
            TryFindInitializer(module.Functions, variableName, declarationRange, out initializer);
    }

    private static bool TryFindInitializer(
        IReadOnlyList<AstNode> nodes,
        string variableName,
        TextRange declarationRange,
        out Expression initializer)
    {
        initializer = null!;
        for (var i = 0; i < nodes.Count; i++)
        {
            if (TryFindInitializer(nodes[i], variableName, declarationRange, out initializer))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindInitializer(
        AstNode? node,
        string variableName,
        TextRange declarationRange,
        out Expression initializer)
    {
        initializer = null!;
        switch (node)
        {
            case VariableDeclaration variable when variable.Name != null &&
                StringComparer.Ordinal.Equals(variable.Name.Value, variableName) &&
                SameRange(TextRange.FromSourceSpan(variable.Name.Range), declarationRange) &&
                variable.Initializer != null:
                initializer = variable.Initializer;
                return true;
            case FunctionDeclaration function:
                return TryFindInitializer(function.Body, variableName, declarationRange, out initializer);
            case BlockStatement block:
                return TryFindInitializer(block.Statements, variableName, declarationRange, out initializer) ||
                    TryFindInitializer(block.Functions, variableName, declarationRange, out initializer);
            case IfStatement ifStatement:
                return TryFindInitializer(ifStatement.Body, variableName, declarationRange, out initializer) ||
                    TryFindInitializer(ifStatement.Else, variableName, declarationRange, out initializer);
            case WhileStatement whileStatement:
                return TryFindInitializer(whileStatement.Body, variableName, declarationRange, out initializer);
            case ForStatement forStatement:
                return TryFindInitializer(forStatement.Initializer, variableName, declarationRange, out initializer) ||
                    TryFindInitializer(forStatement.Body, variableName, declarationRange, out initializer);
            default:
                return false;
        }
    }

    private static bool SameRange(TextRange left, TextRange right)
    {
        return left.Start.Equals(right.Start) && left.End.Equals(right.End);
    }
}
