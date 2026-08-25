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
                return TryGetNameType(name, useModule, usePath, out reference, out resolveModule, out resolvePath);
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

    private static bool TryGetNameType(
        NameExpression name,
        ModuleDeclaration useModule,
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
            if (node is FunctionDeclaration function)
            {
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

                if (TryFindVariableType(function.Body, name, out reference))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryFindVariableType(
        AstNode? body,
        NameExpression use,
        out TypeReference reference)
    {
        reference = null!;
        TypeReference? found = null;
        CollectVariableType(body, use, ref found);
        if (found == null)
        {
            return false;
        }

        reference = found;
        return true;
    }

    private static void CollectVariableType(
        AstNode? node,
        NameExpression use,
        ref TypeReference? found)
    {
        if (node == null)
        {
            return;
        }

        switch (node)
        {
            case VariableDeclaration variable
                when variable.Name != null &&
                    StringComparer.Ordinal.Equals(variable.Name.Value, use.Identifier.Value) &&
                    IsBefore(variable.Range, use.Range) &&
                    variable.Initializer is CheckExpression check &&
                    check.AssertedType != null:
                found = check.AssertedType;
                CollectVariableType(variable.Initializer, use, ref found);
                return;
            case BlockStatement block:
                for (var i = 0; i < block.Statements.Count; i++)
                {
                    CollectVariableType(block.Statements[i], use, ref found);
                }
                return;
            case IfStatement ifStatement:
                CollectVariableType(ifStatement.Body, use, ref found);
                CollectVariableType(ifStatement.Else, use, ref found);
                return;
            case WhileStatement whileStatement:
                CollectVariableType(whileStatement.Body, use, ref found);
                return;
            case ForStatement forStatement:
                CollectVariableType(forStatement.Initializer, use, ref found);
                CollectVariableType(forStatement.Body, use, ref found);
                return;
            case TryStatement tryStatement:
                CollectVariableType(tryStatement.Body, use, ref found);
                CollectVariableType(tryStatement.CatchBody, use, ref found);
                CollectVariableType(tryStatement.FinallyBody, use, ref found);
                return;
        }
    }

    private static bool IsBefore(SourceSpan candidate, SourceSpan use)
    {
        if (candidate.EndLine < use.StartLine)
        {
            return true;
        }

        return candidate.EndLine == use.StartLine &&
            candidate.EndColumn <= use.StartColumn;
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
