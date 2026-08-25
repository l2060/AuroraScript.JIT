using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.GlobalDeclarations;
using AuroraScript.LanguageServices.Features.Definition;
using AuroraScript.LanguageServices.Text;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal static class AuroraDefinitionResolver
{
    public static DefinitionLocation? Resolve(AuroraWorkspaceIndex index, string path, TextPosition position)
    {
        return Resolve(index, path, position, GlobalDeclarationIndex.Empty);
    }

    public static DefinitionLocation? Resolve(
        AuroraWorkspaceIndex index,
        string path,
        TextPosition position,
        GlobalDeclarationIndex globalDeclarations)
    {
        var module = index.TryGetModule(path);
        if (module == null)
        {
            return null;
        }

        if (TryResolveImportDefinition(index, module, position, out var importDefinition))
        {
            return importDefinition;
        }

        var context = AstQuery.Find(module.Module, position);
        if (context == null)
        {
            return null;
        }

        var localIndex = AuroraLocalSymbolIndex.Build(module);
        if (localIndex.TryGetDefinition(position, out var localDefinition))
        {
            return localDefinition;
        }

        if (AuroraShapeQuery.TryGetFieldDefinition(index, module, context, out var shapeFieldDefinition))
        {
            return shapeFieldDefinition;
        }

        if (context.PropertyAccess != null &&
            context.IsOnPropertyName &&
            TryResolveGlobalMember(index, module, localIndex, context.PropertyAccess, globalDeclarations, out var globalMemberDefinition))
        {
            return globalMemberDefinition;
        }

        if (context.PropertyAccess != null &&
            context.IsOnPropertyName &&
            AuroraObjectMemberIndex.Build(module).TryGetDefinition(context.PropertyAccess, out var objectMemberDefinition))
        {
            return objectMemberDefinition;
        }

        if (context.PropertyAccess != null &&
            context.IsOnPropertyName &&
            TryResolveConstructedObjectMember(index, module, localIndex, context.PropertyAccess, out var constructedMemberDefinition))
        {
            return constructedMemberDefinition;
        }

        if (context.PropertyAccess != null &&
            context.IsOnPropertyName &&
            TryResolveImportedMember(index, module, context.PropertyAccess, out var importedMember))
        {
            return ToLocation(importedMember);
        }

        if (context.PropertyAccess != null &&
            context.IsOnPropertyOwner &&
            context.PropertyAccess.Object is NameExpression propertyOwner)
        {
            var ownerName = propertyOwner.Identifier.Value;
            if (module.ImportsByAlias.TryGetValue(ownerName, out var ownerImport))
            {
                return new DefinitionLocation(module.Path, ownerImport.AliasRange);
            }

            if (module.Symbols.TryGetValue(ownerName, out var ownerSymbol))
            {
                return ToLocation(ownerSymbol);
            }

            if (TryResolveIncludedExport(index, module, ownerName, out var includedOwner))
            {
                return ToLocation(includedOwner);
            }
        }

        if (context.PropertyAccess != null && context.IsOnPropertyName)
        {
            return null;
        }

        if (context.Name != null)
        {
            var name = context.Name.Identifier.Value;

            if (module.Symbols.TryGetValue(name, out var local))
            {
                return ToLocation(local);
            }

            if (module.ImportsByAlias.TryGetValue(name, out var importAlias))
            {
                return new DefinitionLocation(module.Path, importAlias.AliasRange);
            }

            if (TryResolveIncludedExport(index, module, name, out var included))
            {
                return ToLocation(included);
            }

            if (!module.ImportsByAlias.ContainsKey(name) &&
                globalDeclarations.TryGet(name, out var global))
            {
                return ToLocation(global);
            }
        }

        return null;
    }

    private static bool TryResolveGlobalMember(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        AuroraLocalSymbolIndex localIndex,
        GetPropertyExpression propertyAccess,
        GlobalDeclarationIndex globalDeclarations,
        out DefinitionLocation definition)
    {
        definition = null!;
        if (propertyAccess.Object is not NameExpression owner ||
            owner.Identifier.Value != "global" ||
            propertyAccess.Property is not NameExpression property)
        {
            return false;
        }

        var ownerPosition = TextRange.FromSourceSpan(owner.Identifier.Range).Start;
        if (localIndex.TryGetDefinition(ownerPosition, out _) ||
            module.Symbols.ContainsKey("global") ||
            module.ImportsByAlias.ContainsKey("global") ||
            TryResolveIncludedExport(index, module, "global", out _))
        {
            return false;
        }

        if (!globalDeclarations.TryGet(property.Identifier.Value, out var declaration))
        {
            return false;
        }

        definition = ToLocation(declaration);
        return true;
    }

    private static bool TryResolveImportDefinition(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        TextPosition position,
        out DefinitionLocation definition)
    {
        foreach (var import in module.ImportsByAlias.Values)
        {
            if (Contains(import.PathRange, position))
            {
                definition = new DefinitionLocation(import.TargetPath, GetDocumentStartRange(index, import.TargetPath));
                return true;
            }

            if (Contains(import.AliasRange, position))
            {
                definition = new DefinitionLocation(module.Path, import.AliasRange);
                return true;
            }
        }

        for (var i = 0; i < module.Includes.Count; i++)
        {
            var include = module.Includes[i];
            if (Contains(include.PathRange, position))
            {
                definition = new DefinitionLocation(include.TargetPath, GetDocumentStartRange(index, include.TargetPath));
                return true;
            }
        }

        definition = null!;
        return false;
    }

    private static TextRange GetDocumentStartRange(AuroraWorkspaceIndex index, string path)
    {
        var module = index.TryGetModule(path);
        var normalizedPath = module?.Path ?? path;
        return new TextRange(normalizedPath, TextPosition.Zero, TextPosition.Zero);
    }

    private static bool TryResolveConstructedObjectMember(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        AuroraLocalSymbolIndex localIndex,
        GetPropertyExpression propertyAccess,
        out DefinitionLocation definition)
    {
        definition = null!;
        if (propertyAccess.Object is not NameExpression owner ||
            propertyAccess.Property is not NameExpression property ||
            !TryFindVariableInitializer(module, localIndex, owner, out var initializer) ||
            !TryResolveCallTarget(index, module, initializer, out var targetModule, out var function))
        {
            return false;
        }

        return TryResolveReturnedObjectMember(targetModule, function, property.Identifier.Value, out definition);
    }

    private static bool TryResolveReturnedObjectMember(
        AuroraModuleIndex targetModule,
        FunctionDeclaration function,
        string memberName,
        out DefinitionLocation definition)
    {
        definition = null!;
        if (function.Body == null)
        {
            return false;
        }

        var returnExpression = FindReturnExpression(function.Body);
        if (returnExpression == null)
        {
            return false;
        }

        if (TryResolveObjectMemberInExpression(targetModule, returnExpression, memberName, out var range))
        {
            definition = new DefinitionLocation(targetModule.Path, range);
            return true;
        }

        if (returnExpression is NameExpression returnedName &&
            AuroraObjectMemberIndex.Build(targetModule, function).TryGetMember(returnedName.Identifier.Value, memberName, out range))
        {
            definition = new DefinitionLocation(targetModule.Path, range);
            return true;
        }

        if (returnExpression is FunctionCallExpression call &&
            call.Target is NameExpression { Identifier.Value: "Object" } &&
            call.Arguments.Count > 0)
        {
            if (TryResolveObjectMemberInExpression(targetModule, call.Arguments[0], memberName, out range))
            {
                definition = new DefinitionLocation(targetModule.Path, range);
                return true;
            }

            if (call.Arguments[0] is NameExpression objectName &&
                AuroraObjectMemberIndex.Build(targetModule, function).TryGetMember(objectName.Identifier.Value, memberName, out range))
            {
                definition = new DefinitionLocation(targetModule.Path, range);
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveObjectMemberInExpression(
        AuroraModuleIndex module,
        Expression expression,
        string memberName,
        out TextRange range)
    {
        if (expression is MapExpression map)
        {
            for (var i = 0; i < map.Entries.Count; i++)
            {
                if (map.Entries[i] is MapKeyValueExpression entry &&
                    entry.Key != null &&
                    string.Equals(entry.Key.Value, memberName, System.StringComparison.Ordinal))
                {
                    range = TextRange.FromSourceSpan(entry.Key.Range);
                    return true;
                }
            }
        }

        if (expression is NameExpression name &&
            AuroraObjectMemberIndex.Build(module).TryGetMember(name.Identifier.Value, memberName, out range))
        {
            return true;
        }

        range = default;
        return false;
    }

    private static bool TryResolveCallTarget(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        Expression initializer,
        out AuroraModuleIndex targetModule,
        out FunctionDeclaration function)
    {
        targetModule = null!;
        function = null!;
        var call = UnwrapCall(initializer);
        if (call == null)
        {
            return false;
        }

        if (call.Target is NameExpression localName)
        {
            targetModule = module;
            return TryFindFunction(module.Module, localName.Identifier.Value, out function);
        }

        if (call.Target is GetPropertyExpression
            {
                Object: NameExpression alias,
                Property: NameExpression member
            } &&
            module.ImportsByAlias.TryGetValue(alias.Identifier.Value, out var import))
        {
            var importedModule = index.TryGetModule(import.TargetPath);
            if (importedModule == null)
            {
                return false;
            }

            targetModule = importedModule;
            return TryFindFunction(importedModule.Module, member.Identifier.Value, out function);
        }

        return false;
    }

    private static FunctionCallExpression? UnwrapCall(Expression expression)
    {
        return expression switch
        {
            FunctionCallExpression call => call,
            NewExpression newExpression => newExpression.Expression,
            _ => null
        };
    }

    private static bool TryFindVariableInitializer(
        AuroraModuleIndex module,
        AuroraLocalSymbolIndex localIndex,
        NameExpression owner,
        out Expression initializer)
    {
        initializer = null!;
        if (localIndex.TryGetDefinition(TextRange.FromSourceSpan(owner.Identifier.Range).Start, out var localDefinition) &&
            PathComparer.Equals(localDefinition.Path, module.Path))
        {
            return TryFindVariableInitializer(module.Module, owner.Identifier.Value, localDefinition.Range, out initializer);
        }

        if (module.Symbols.TryGetValue(owner.Identifier.Value, out var symbol))
        {
            return TryFindVariableInitializer(module.Module, owner.Identifier.Value, symbol.NameRange, out initializer);
        }

        return false;
    }

    private static bool TryFindVariableInitializer(
        Compiler.Ast.ModuleDeclaration module,
        string variableName,
        TextRange declarationRange,
        out Expression initializer)
    {
        initializer = null!;
        return TryFindVariableInitializer(module.Statements, variableName, declarationRange, out initializer) ||
            TryFindVariableInitializer(module.Functions, variableName, declarationRange, out initializer);
    }

    private static bool TryFindVariableInitializer(
        IReadOnlyList<Compiler.Ast.AstNode> nodes,
        string variableName,
        TextRange declarationRange,
        out Expression initializer)
    {
        initializer = null!;
        for (var i = 0; i < nodes.Count; i++)
        {
            if (TryFindVariableInitializer(nodes[i], variableName, declarationRange, out initializer))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindVariableInitializer(
        Compiler.Ast.AstNode? node,
        string variableName,
        TextRange declarationRange,
        out Expression initializer)
    {
        initializer = null!;
        switch (node)
        {
            case VariableDeclaration variable when variable.Name != null &&
                string.Equals(variable.Name.Value, variableName, System.StringComparison.Ordinal) &&
                SameRange(TextRange.FromSourceSpan(variable.Name.Range), declarationRange) &&
                variable.Initializer != null:
                initializer = variable.Initializer!;
                return true;
            case FunctionDeclaration function:
                return TryFindVariableInitializer(function.Body, variableName, declarationRange, out initializer);
            case BlockStatement block:
                return TryFindVariableInitializer(block.Statements, variableName, declarationRange, out initializer) ||
                    TryFindVariableInitializer(block.Functions, variableName, declarationRange, out initializer);
            case IfStatement ifStatement:
                return TryFindVariableInitializer(ifStatement.Body, variableName, declarationRange, out initializer) ||
                    TryFindVariableInitializer(ifStatement.Else, variableName, declarationRange, out initializer);
            case WhileStatement whileStatement:
                return TryFindVariableInitializer(whileStatement.Body, variableName, declarationRange, out initializer);
            case ForStatement forStatement:
                return TryFindVariableInitializer(forStatement.Initializer, variableName, declarationRange, out initializer) ||
                    TryFindVariableInitializer(forStatement.Body, variableName, declarationRange, out initializer);
            case ForInStatement forInStatement:
                return TryFindVariableInitializer(forInStatement.Initializer, variableName, declarationRange, out initializer) ||
                    TryFindVariableInitializer(forInStatement.Body, variableName, declarationRange, out initializer);
            case TryStatement tryStatement:
                return TryFindVariableInitializer(tryStatement.Body, variableName, declarationRange, out initializer) ||
                    TryFindVariableInitializer(tryStatement.CatchBody, variableName, declarationRange, out initializer) ||
                    TryFindVariableInitializer(tryStatement.FinallyBody, variableName, declarationRange, out initializer);
        }

        return false;
    }

    private static bool TryFindFunction(
        Compiler.Ast.ModuleDeclaration module,
        string functionName,
        out FunctionDeclaration function)
    {
        function = null!;
        for (var i = 0; i < module.Functions.Count; i++)
        {
            if (module.Functions[i].Name != null &&
                string.Equals(module.Functions[i].Name.Value, functionName, System.StringComparison.Ordinal))
            {
                function = module.Functions[i];
                return true;
            }
        }

        return TryFindFunction(module.Statements, functionName, out function);
    }

    private static bool TryFindFunction(
        IReadOnlyList<Compiler.Ast.AstNode> nodes,
        string functionName,
        out FunctionDeclaration function)
    {
        function = null!;
        for (var i = 0; i < nodes.Count; i++)
        {
            if (TryFindFunction(nodes[i], functionName, out function))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindFunction(
        Compiler.Ast.AstNode? node,
        string functionName,
        out FunctionDeclaration function)
    {
        function = null!;
        switch (node)
        {
            case FunctionDeclaration candidate when candidate.Name != null &&
                string.Equals(candidate.Name.Value, functionName, System.StringComparison.Ordinal):
                function = candidate;
                return true;
            case FunctionDeclaration candidate:
                return TryFindFunction(candidate.Body, functionName, out function);
            case BlockStatement block:
                return TryFindFunction(block.Functions, functionName, out function) ||
                    TryFindFunction(block.Statements, functionName, out function);
            case IfStatement ifStatement:
                return TryFindFunction(ifStatement.Body, functionName, out function) ||
                    TryFindFunction(ifStatement.Else, functionName, out function);
            case WhileStatement whileStatement:
                return TryFindFunction(whileStatement.Body, functionName, out function);
            case ForStatement forStatement:
                return TryFindFunction(forStatement.Initializer, functionName, out function) ||
                    TryFindFunction(forStatement.Body, functionName, out function);
            case ForInStatement forInStatement:
                return TryFindFunction(forInStatement.Initializer, functionName, out function) ||
                    TryFindFunction(forInStatement.Body, functionName, out function);
            case TryStatement tryStatement:
                return TryFindFunction(tryStatement.Body, functionName, out function) ||
                    TryFindFunction(tryStatement.CatchBody, functionName, out function) ||
                    TryFindFunction(tryStatement.FinallyBody, functionName, out function);
        }

        return false;
    }

    private static Expression? FindReturnExpression(Compiler.Ast.AstNode? node)
    {
        switch (node)
        {
            case null:
                return null;
            case ReturnStatement returnStatement:
                return returnStatement.Expression;
            case BlockStatement block:
                for (var i = 0; i < block.Statements.Count; i++)
                {
                    var expression = FindReturnExpression(block.Statements[i]);
                    if (expression != null)
                    {
                        return expression;
                    }
                }
                return null;
            case IfStatement ifStatement:
                return FindReturnExpression(ifStatement.Body) ?? FindReturnExpression(ifStatement.Else);
            case WhileStatement whileStatement:
                return FindReturnExpression(whileStatement.Body);
            case ForStatement forStatement:
                return FindReturnExpression(forStatement.Body);
            case ForInStatement forInStatement:
                return FindReturnExpression(forInStatement.Body);
            case TryStatement tryStatement:
                return FindReturnExpression(tryStatement.Body) ??
                    FindReturnExpression(tryStatement.CatchBody) ??
                    FindReturnExpression(tryStatement.FinallyBody);
        }

        return null;
    }

    private static bool TryResolveImportedMember(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        GetPropertyExpression propertyAccess,
        out AuroraSymbolInfo symbol)
    {
        symbol = null!;
        if (propertyAccess.Object is not NameExpression alias ||
            propertyAccess.Property is not NameExpression property)
        {
            return false;
        }

        if (!module.ImportsByAlias.TryGetValue(alias.Identifier.Value, out var import))
        {
            return false;
        }

        var target = index.TryGetModule(import.TargetPath);
        return target != null && target.Exports.TryGetValue(property.Identifier.Value, out symbol!);
    }

    private static bool TryResolveIncludedExport(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        string name,
        out AuroraSymbolInfo symbol)
    {
        var visited = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        return TryResolveIncludedExport(index, module, name, visited, out symbol);
    }

    private static bool TryResolveIncludedExport(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        string name,
        HashSet<string> visited,
        out AuroraSymbolInfo symbol)
    {
        symbol = null!;
        if (!visited.Add(module.Path))
        {
            return false;
        }

        for (var i = 0; i < module.Includes.Count; i++)
        {
            var include = module.Includes[i];
            var includedModule = index.TryGetModule(include.TargetPath);
            if (includedModule == null)
            {
                continue;
            }

            if (includedModule.Exports.TryGetValue(name, out symbol!))
            {
                return true;
            }

            if (TryResolveIncludedExport(index, includedModule, name, visited, out symbol))
            {
                return true;
            }
        }

        return false;
    }

    private static DefinitionLocation ToLocation(AuroraSymbolInfo symbol)
    {
        return new DefinitionLocation(symbol.FilePath, symbol.NameRange);
    }

    private static DefinitionLocation ToLocation(GlobalDeclarationInfo declaration)
    {
        return new DefinitionLocation(declaration.FilePath, TextRange.FromSourceSpan(declaration.NameRange));
    }

    private static bool Contains(TextRange range, TextPosition position)
    {
        if (position.Line < range.Start.Line || position.Line > range.End.Line)
        {
            return false;
        }

        if (position.Line == range.Start.Line && position.Character < range.Start.Character)
        {
            return false;
        }

        if (position.Line == range.End.Line && position.Character > range.End.Character)
        {
            return false;
        }

        return true;
    }

    private static bool SameRange(TextRange left, TextRange right)
    {
        return left.Start.Equals(right.Start) && left.End.Equals(right.End);
    }

    private static System.StringComparer PathComparer => System.OperatingSystem.IsWindows()
        ? System.StringComparer.OrdinalIgnoreCase
        : System.StringComparer.Ordinal;
}
