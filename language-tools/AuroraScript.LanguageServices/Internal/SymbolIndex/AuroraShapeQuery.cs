using AuroraScript.Compiler;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Features.Definition;
using AuroraScript.LanguageServices.Features.Hover;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal static class AuroraShapeQuery
{
    public static CompletionResult GetFieldCompletions(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        Expression owner)
    {
        if (!TryGetShape(
                owner,
                module.Module,
                index,
                module.Path,
                out var shape))
        {
            return new CompletionResult(Array.Empty<CompletionItem>());
        }

        var items = new List<CompletionItem>(shape.Fields.Count);
        for (var i = 0; i < shape.Fields.Count; i++)
        {
            var field = shape.Fields[i];
            items.Add(new CompletionItem(
                field.Name.Value,
                CompletionItemKind.Property,
                field.Type.DisplayName,
                documentation: null,
                readOnly: false));
        }

        return new CompletionResult(items);
    }

    public static CompletionResult GetFieldCompletionsForName(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        string ownerName,
        TextPosition position)
    {
        if (!TryGetShapeForName(
                ownerName,
                position,
                module.Module,
                index,
                module.Path,
                out var shape))
        {
            return new CompletionResult(Array.Empty<CompletionItem>());
        }

        var items = new List<CompletionItem>(shape.Fields.Count);
        for (var i = 0; i < shape.Fields.Count; i++)
        {
            var field = shape.Fields[i];
            items.Add(new CompletionItem(
                field.Name.Value,
                CompletionItemKind.Property,
                field.Type.DisplayName,
                documentation: null,
                readOnly: false));
        }

        return new CompletionResult(items);
    }

    public static bool TryGetFieldHover(
        ModuleDeclaration module,
        AstQueryContext context,
        AuroraWorkspaceIndex? index,
        string? sourcePath,
        out HoverResult hover)
    {
        hover = null!;
        if (!TryGetField(
                context,
                module,
                index,
                sourcePath,
                out var field))
        {
            return false;
        }

        var range = context.PropertyAccess!.Property is NameExpression property
            ? TextRange.FromSourceSpan(property.Identifier.Range)
            : TextRange.FromSourceSpan(context.PropertyAccess.Property.Range);
        var builder = new StringBuilder();
        builder
            .Append("```aurorascript\n")
            .Append(field.Type.DisplayName)
            .Append(' ')
            .Append(field.Name.Value)
            .Append("\n```");
        hover = new HoverResult(builder.ToString(), range);
        return true;
    }

    public static bool TryGetFieldDefinition(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        AstQueryContext context,
        out DefinitionLocation definition)
    {
        definition = null!;
        if (!TryGetField(
                context,
                module.Module,
                index,
                module.Path,
                out var field))
        {
            return false;
        }

        var path = GetDeclaringPath(field, module.Path);
        definition = new DefinitionLocation(
            path,
            TextRange.FromSourceSpan(field.Name.Range));
        return true;
    }

    public static bool TryGetShape(
        Expression? expression,
        ModuleDeclaration useModule,
        AuroraWorkspaceIndex? index,
        string? usePath,
        out TypeDeclaration shape)
    {
        shape = null!;
        if (!TryGetTypeReference(
                expression,
                useModule,
                index,
                usePath,
                out var reference,
                out var resolveModule,
                out var resolvePath))
        {
            return false;
        }

        return TryResolveType(
            resolveModule,
            reference,
            index,
            resolvePath,
            out shape);
    }

    public static bool TryResolveType(
        ModuleDeclaration module,
        TypeReference reference,
        AuroraWorkspaceIndex? index,
        string? modulePath,
        out TypeDeclaration declaration)
    {
        declaration = null!;
        if (reference == null)
        {
            return false;
        }

        if (reference.Qualifier == null)
        {
            return module.TryGetType(reference.Name, out declaration);
        }

        if (index == null || string.IsNullOrEmpty(modulePath))
        {
            return false;
        }

        var indexedModule = index.TryGetModule(modulePath);
        if (indexedModule == null ||
            !indexedModule.ImportsByAlias.TryGetValue(
                reference.QualifierName,
                out var import))
        {
            return false;
        }

        var target = index.TryGetModule(import.TargetPath);
        if (target == null ||
            !target.Module.TryGetType(reference.Name, out declaration) ||
            declaration.Access != MemberAccess.Export)
        {
            declaration = null!;
            return false;
        }

        return true;
    }

    private static bool TryGetShapeForName(
        string ownerName,
        TextPosition position,
        ModuleDeclaration useModule,
        AuroraWorkspaceIndex? index,
        string? usePath,
        out TypeDeclaration shape)
    {
        shape = null!;
        if (!TryGetNameTypeAtPosition(
                ownerName,
                position,
                useModule,
                index,
                usePath,
                out var reference,
                out var resolveModule,
                out var resolvePath))
        {
            return false;
        }

        return TryResolveType(resolveModule, reference, index, resolvePath, out shape);
    }

    private static bool TryGetField(
        AstQueryContext context,
        ModuleDeclaration module,
        AuroraWorkspaceIndex? index,
        string? sourcePath,
        out TypeFieldDeclaration field)
    {
        field = null!;
        if (context.PropertyAccess == null ||
            !context.IsOnPropertyName ||
            context.PropertyAccess.Property is not NameExpression property ||
            !TryGetShape(
                context.PropertyAccess.Object,
                module,
                index,
                sourcePath,
                out var shape))
        {
            return false;
        }

        return TryFindField(shape, property.Identifier.Value, out field);
    }

    private static bool TryFindField(
        TypeDeclaration shape,
        string name,
        out TypeFieldDeclaration field)
    {
        for (var i = 0; i < shape.Fields.Count; i++)
        {
            if (StringComparer.Ordinal.Equals(shape.Fields[i].Name.Value, name))
            {
                field = shape.Fields[i];
                return true;
            }
        }

        field = null!;
        return false;
    }

    private static bool TryGetTypeReference(
        Expression? expression,
        ModuleDeclaration useModule,
        AuroraWorkspaceIndex? index,
        string? usePath,
        out TypeReference reference,
        out ModuleDeclaration resolveModule,
        out string resolvePath)
    {
        reference = null!;
        resolveModule = useModule;
        resolvePath = usePath ?? string.Empty;
        switch (expression)
        {
            case CheckExpression check:
                if (check.AssertedType == null)
                {
                    return false;
                }

                reference = check.AssertedType;
                return true;
            case NameExpression name:
                return TryGetNameType(name, useModule, index, usePath, out reference, out resolveModule, out resolvePath);
            case GetPropertyExpression getProperty when getProperty.Property is NameExpression property:
                if (!TryGetShape(
                        getProperty.Object,
                        useModule,
                        index,
                        usePath,
                        out var owner) ||
                    !TryFindField(owner, property.Identifier.Value, out var field))
                {
                    return false;
                }

                reference = field.Type;
                resolveModule = owner.Parent as ModuleDeclaration ?? useModule;
                resolvePath = GetDeclaringPath(owner, usePath);
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetNameTypeAtPosition(
        string identifier,
        TextPosition position,
        ModuleDeclaration useModule,
        AuroraWorkspaceIndex? index,
        string? usePath,
        out TypeReference reference,
        out ModuleDeclaration resolveModule,
        out string resolvePath)
    {
        reference = null!;
        resolveModule = useModule;
        resolvePath = usePath ?? string.Empty;
        var function = FindEnclosingFunction(useModule, position);
        if (function != null &&
            TryGetNameTypeInFunction(identifier, function, useModule, index, usePath, out reference, out resolveModule, out resolvePath))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetNameType(
        NameExpression name,
        ModuleDeclaration useModule,
        AuroraWorkspaceIndex? index,
        string? usePath,
        out TypeReference reference,
        out ModuleDeclaration resolveModule,
        out string resolvePath)
    {
        reference = null!;
        resolveModule = useModule;
        resolvePath = usePath ?? string.Empty;
        var identifier = name.Identifier.Value;
        for (var node = name.Parent; node != null; node = node.Parent)
        {
            if (node is FunctionDeclaration function &&
                TryGetNameTypeInFunction(identifier, function, useModule, index, usePath, out reference, out resolveModule, out resolvePath))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetNameTypeInFunction(
        string identifier,
        FunctionDeclaration function,
        ModuleDeclaration useModule,
        AuroraWorkspaceIndex? index,
        string? usePath,
        out TypeReference reference,
        out ModuleDeclaration resolveModule,
        out string resolvePath)
    {
        reference = null!;
        resolveModule = useModule;
        resolvePath = usePath ?? string.Empty;
        for (var i = 0; i < function.Parameters.Count; i++)
        {
            var parameter = function.Parameters[i];
            if (parameter.DeclaredType != null &&
                parameter.Name != null &&
                StringComparer.Ordinal.Equals(parameter.Name.Value, identifier))
            {
                reference = parameter.DeclaredType;
                return true;
            }
        }

        return TryFindVariableType(function.Body, identifier, function, useModule, index, usePath, out reference, out resolveModule, out resolvePath);
    }

    private static FunctionDeclaration? FindEnclosingFunction(ModuleDeclaration module, TextPosition position)
    {
        FunctionDeclaration? found = null;
        FindEnclosingFunction(module, position, ref found);
        return found;
    }

    private static void FindEnclosingFunction(AstNode? node, TextPosition position, ref FunctionDeclaration? found)
    {
        if (node == null)
        {
            return;
        }

        switch (node)
        {
            case FunctionDeclaration function when Contains(function.Range, position):
                found = function;
                FindEnclosingFunction(function.Body, position, ref found);
                return;
            case ModuleDeclaration module:
                for (var i = 0; i < module.Functions.Count; i++)
                {
                    FindEnclosingFunction(module.Functions[i], position, ref found);
                }

                for (var i = 0; i < module.Statements.Count; i++)
                {
                    FindEnclosingFunction(module.Statements[i], position, ref found);
                }

                return;
            case BlockStatement block:
                for (var i = 0; i < block.Functions.Count; i++)
                {
                    FindEnclosingFunction(block.Functions[i], position, ref found);
                }

                for (var i = 0; i < block.Statements.Count; i++)
                {
                    FindEnclosingFunction(block.Statements[i], position, ref found);
                }

                return;
        }
    }

    private static bool Contains(SourceSpan range, TextPosition position)
    {
        var line = position.Line + 1;
        var column = position.Character + 1;
        if (line < range.StartLine || line > range.EndLine)
        {
            return false;
        }

        if (line == range.StartLine && column < range.StartColumn)
        {
            return false;
        }

        return line != range.EndLine || column <= range.EndColumn;
    }

    private static bool TryFindVariableType(
        AstNode? body,
        string identifier,
        FunctionDeclaration function,
        ModuleDeclaration useModule,
        AuroraWorkspaceIndex? index,
        string? usePath,
        out TypeReference reference,
        out ModuleDeclaration resolveModule,
        out string resolvePath)
    {
        reference = null!;
        resolveModule = useModule;
        resolvePath = usePath ?? string.Empty;
        TypeReference? found = null;
        CollectVariableType(body, identifier, function, useModule, index, usePath, ref found, ref resolveModule, ref resolvePath);
        if (found == null)
        {
            return false;
        }

        reference = found;
        return true;
    }

    private static void CollectVariableType(
        AstNode? node,
        string identifier,
        FunctionDeclaration function,
        ModuleDeclaration useModule,
        AuroraWorkspaceIndex? index,
        string? usePath,
        ref TypeReference? found,
        ref ModuleDeclaration resolveModule,
        ref string resolvePath)
    {
        if (node == null)
        {
            return;
        }

        switch (node)
        {
            case VariableDeclaration variable
                when variable.Name != null &&
                    StringComparer.Ordinal.Equals(variable.Name.Value, identifier) &&
                    variable.Initializer != null &&
                    TryGetInitializerType(
                        variable.Initializer,
                        useModule,
                        index,
                        usePath,
                        out var initializerType,
                        out var initializerModule,
                        out var initializerPath):
                found = initializerType;
                resolveModule = initializerModule;
                resolvePath = initializerPath;
                CollectVariableType(variable.Initializer, identifier, function, useModule, index, usePath, ref found, ref resolveModule, ref resolvePath);
                return;
            case BlockStatement block:
                for (var i = 0; i < block.Statements.Count; i++)
                {
                    CollectVariableType(block.Statements[i], identifier, function, useModule, index, usePath, ref found, ref resolveModule, ref resolvePath);
                }
                return;
            case IfStatement ifStatement:
                CollectVariableType(ifStatement.Body, identifier, function, useModule, index, usePath, ref found, ref resolveModule, ref resolvePath);
                CollectVariableType(ifStatement.Else, identifier, function, useModule, index, usePath, ref found, ref resolveModule, ref resolvePath);
                return;
            case WhileStatement whileStatement:
                CollectVariableType(whileStatement.Body, identifier, function, useModule, index, usePath, ref found, ref resolveModule, ref resolvePath);
                return;
            case ForStatement forStatement:
                CollectVariableType(forStatement.Initializer, identifier, function, useModule, index, usePath, ref found, ref resolveModule, ref resolvePath);
                CollectVariableType(forStatement.Body, identifier, function, useModule, index, usePath, ref found, ref resolveModule, ref resolvePath);
                return;
            case TryStatement tryStatement:
                CollectVariableType(tryStatement.Body, identifier, function, useModule, index, usePath, ref found, ref resolveModule, ref resolvePath);
                CollectVariableType(tryStatement.CatchBody, identifier, function, useModule, index, usePath, ref found, ref resolveModule, ref resolvePath);
                CollectVariableType(tryStatement.FinallyBody, identifier, function, useModule, index, usePath, ref found, ref resolveModule, ref resolvePath);
                return;
        }
    }

    private static bool TryGetInitializerType(
        Expression initializer,
        ModuleDeclaration useModule,
        AuroraWorkspaceIndex? index,
        string? usePath,
        out TypeReference reference,
        out ModuleDeclaration resolveModule,
        out string resolvePath)
    {
        reference = null!;
        resolveModule = useModule;
        resolvePath = usePath ?? string.Empty;
        switch (initializer)
        {
            case CheckExpression check when check.AssertedType != null:
                reference = check.AssertedType;
                return true;
            case NewExpression newExpression when
                UnwrapCall(newExpression.Expression) is FunctionCallExpression { Target: NameExpression typeName }:
                reference = typeName.Identifier != null
                    ? new TypeReference(typeName.Identifier)
                    : null!;
                return reference != null;
            case FunctionCallExpression call:
                return TryGetCallReturnType(call, useModule, index, usePath, out reference, out resolveModule, out resolvePath);
            default:
                var unwrapped = UnwrapCall(initializer);
                return unwrapped != null &&
                    TryGetCallReturnType(unwrapped, useModule, index, usePath, out reference, out resolveModule, out resolvePath);
        }
    }

    private static FunctionCallExpression? UnwrapCall(Expression expression)
    {
        return expression switch
        {
            FunctionCallExpression call => call,
            NewExpression newExpression => newExpression.Expression as FunctionCallExpression ?? UnwrapCall(newExpression.Expression),
            _ => null
        };
    }

    private static bool TryGetCallReturnType(
        FunctionCallExpression call,
        ModuleDeclaration useModule,
        AuroraWorkspaceIndex? index,
        string? usePath,
        out TypeReference reference,
        out ModuleDeclaration resolveModule,
        out string resolvePath)
    {
        reference = null!;
        resolveModule = useModule;
        resolvePath = usePath ?? string.Empty;
        if (call.Target is NameExpression localName &&
            TryFindFunction(useModule, localName.Identifier.Value, out var function) &&
            function.ReturnType != null)
        {
            reference = function.ReturnType;
            return true;
        }

        if (call.Target is GetPropertyExpression
            {
                Object: NameExpression alias,
                Property: NameExpression member
            } &&
            index != null &&
            !string.IsNullOrEmpty(usePath))
        {
            var indexedModule = index.TryGetModule(usePath);
            if (indexedModule != null &&
                indexedModule.ImportsByAlias.TryGetValue(alias.Identifier.Value, out var import))
            {
                var target = index.TryGetModule(import.TargetPath);
                if (target != null &&
                    TryFindFunction(target.Module, member.Identifier.Value, out var imported) &&
                    imported.Access == MemberAccess.Export &&
                    imported.ReturnType != null)
                {
                    reference = imported.ReturnType;
                    resolveModule = target.Module;
                    resolvePath = target.Path;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryFindFunction(ModuleDeclaration module, string name, out FunctionDeclaration function)
    {
        function = null!;
        for (var i = 0; i < module.Functions.Count; i++)
        {
            if (module.Functions[i].Name != null &&
                StringComparer.Ordinal.Equals(module.Functions[i].Name.Value, name))
            {
                function = module.Functions[i];
                return true;
            }
        }

        return TryFindFunction(module.Statements, name, out function);
    }

    private static bool TryFindFunction(IReadOnlyList<AstNode> nodes, string name, out FunctionDeclaration function)
    {
        function = null!;
        for (var i = 0; i < nodes.Count; i++)
        {
            if (TryFindFunction(nodes[i], name, out function))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindFunction(AstNode? node, string name, out FunctionDeclaration function)
    {
        function = null!;
        switch (node)
        {
            case FunctionDeclaration candidate when candidate.Name != null &&
                StringComparer.Ordinal.Equals(candidate.Name.Value, name):
                function = candidate;
                return true;
            case FunctionDeclaration candidate:
                return TryFindFunction(candidate.Body, name, out function);
            case BlockStatement block:
                return TryFindFunction(block.Functions, name, out function) ||
                    TryFindFunction(block.Statements, name, out function);
            default:
                return false;
        }
    }

    private static string GetDeclaringPath(AstNode node, string? fallback)
    {
        if (node is TypeFieldDeclaration field &&
            field.Parent is TypeDeclaration parentType)
        {
            node = parentType;
        }

        if (node is TypeDeclaration type &&
            type.Parent is ModuleDeclaration module &&
            !string.IsNullOrEmpty(module.Source.FullPath))
        {
            return AuroraWorkspaceIndex.NormalizePath(module.Source.FullPath);
        }

        if (!string.IsNullOrEmpty(node.FileName))
        {
            return AuroraWorkspaceIndex.NormalizePath(node.FileName);
        }

        return fallback ?? string.Empty;
    }
}
