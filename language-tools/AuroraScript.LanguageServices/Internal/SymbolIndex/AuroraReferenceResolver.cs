using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.LanguageServices.Features.References;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal static class AuroraReferenceResolver
{
    public static IReadOnlyList<ReferenceLocation> Resolve(
        AuroraWorkspaceIndex index,
        string path,
        TextPosition position,
        bool includeDeclaration)
    {
        var module = index.TryGetModule(path);
        if (module == null)
        {
            return Array.Empty<ReferenceLocation>();
        }

        var localIndex = AuroraLocalSymbolIndex.Build(module);
        if (localIndex.TryGetReferences(position, includeDeclaration, out var localReferences))
        {
            return localReferences;
        }

        if (!TryResolveTarget(index, module, position, out var target))
        {
            return Array.Empty<ReferenceLocation>();
        }

        var references = new List<ReferenceLocation>();
        if (includeDeclaration)
        {
            references.Add(ToReference(target.Symbol));
        }

        foreach (var pair in index.Modules)
        {
            CollectModuleReferences(index, pair.Value, target, references);
        }

        return references;
    }

    private static bool TryResolveTarget(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        TextPosition position,
        out AuroraResolvedSymbol target)
    {
        target = null!;

        if (TryResolveModuleDeclaration(module, position, out var declared))
        {
            target = AuroraResolvedSymbol.FromSymbol(declared);
            return true;
        }

        if (TryResolveImportAlias(module, position, out var declaredImportAlias, out var importAliasSymbol))
        {
            target = AuroraResolvedSymbol.FromImportAlias(declaredImportAlias, importAliasSymbol);
            return true;
        }

        var context = AstQuery.Find(module.Module, position);
        if (context == null)
        {
            return false;
        }

        if (context.PropertyAccess != null &&
            context.IsOnPropertyName &&
            TryResolveImportedMember(index, module, context.PropertyAccess, out var imported))
        {
            target = AuroraResolvedSymbol.FromSymbol(imported);
            return true;
        }

        if (context.PropertyAccess != null && context.IsOnPropertyName)
        {
            return false;
        }

        if (context.Name == null)
        {
            return false;
        }

        var name = context.Name.Identifier.Value;
        if (module.Symbols.TryGetValue(name, out var local))
        {
            target = AuroraResolvedSymbol.FromSymbol(local);
            return true;
        }

        if (module.ImportsByAlias.TryGetValue(name, out var importAlias))
        {
            target = AuroraResolvedSymbol.FromImportAlias(importAlias, new AuroraSymbolInfo(
                importAlias.Alias,
                AuroraSymbolKind.ImportAlias,
                module.Module.ModulePath ?? module.Path,
                module.Path,
                importAlias.AliasRange,
                exported: false));
            return true;
        }

        if (TryResolveIncludedExport(index, module, name, out var included))
        {
            target = AuroraResolvedSymbol.FromSymbol(included);
            return true;
        }

        return false;
    }

    private static bool TryResolveModuleDeclaration(
        AuroraModuleIndex module,
        TextPosition position,
        out AuroraSymbolInfo symbol)
    {
        foreach (var candidate in module.Symbols.Values)
        {
            if (Contains(candidate.NameRange, position))
            {
                symbol = candidate;
                return true;
            }
        }

        symbol = null!;
        return false;
    }

    private static bool TryResolveImportAlias(
        AuroraModuleIndex module,
        TextPosition position,
        out AuroraImportInfo importAlias,
        out AuroraSymbolInfo symbol)
    {
        foreach (var candidate in module.ImportsByAlias.Values)
        {
            if (!Contains(candidate.AliasRange, position))
            {
                continue;
            }

            importAlias = candidate;
            symbol = new AuroraSymbolInfo(
                candidate.Alias,
                AuroraSymbolKind.ImportAlias,
                module.Module.ModulePath ?? module.Path,
                module.Path,
                candidate.AliasRange,
                exported: false);
            return true;
        }

        importAlias = null!;
        symbol = null!;
        return false;
    }

    private static void CollectModuleReferences(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        AuroraResolvedSymbol target,
        List<ReferenceLocation> references)
    {
        var collector = new Collector(index, module, target, references, AuroraLocalSymbolIndex.Build(module));
        collector.Visit(module.Module);
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

        var targetModule = index.TryGetModule(import.TargetPath);
        return targetModule != null && targetModule.Exports.TryGetValue(property.Identifier.Value, out symbol!);
    }

    private static bool TryResolveIncludedExport(
        AuroraWorkspaceIndex index,
        AuroraModuleIndex module,
        string name,
        out AuroraSymbolInfo symbol)
    {
        var visited = new HashSet<string>(PathComparer);
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

    private static ReferenceLocation ToReference(AuroraSymbolInfo symbol)
    {
        return new ReferenceLocation(symbol.FilePath, symbol.NameRange);
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

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private sealed class Collector
    {
        private readonly AuroraWorkspaceIndex _index;
        private readonly AuroraModuleIndex _module;
        private readonly AuroraResolvedSymbol _target;
        private readonly List<ReferenceLocation> _references;
        private readonly AuroraLocalSymbolIndex _localIndex;

        public Collector(
            AuroraWorkspaceIndex index,
            AuroraModuleIndex module,
            AuroraResolvedSymbol target,
            List<ReferenceLocation> references,
            AuroraLocalSymbolIndex localIndex)
        {
            _index = index;
            _module = module;
            _target = target;
            _references = references;
            _localIndex = localIndex;
        }

        public void Visit(AstNode? node)
        {
            if (node == null)
            {
                return;
            }

            switch (node)
            {
                case ModuleDeclaration module:
                    for (var i = 0; i < module.Statements.Count; i++) Visit(module.Statements[i]);
                    for (var i = 0; i < module.Functions.Count; i++) Visit(module.Functions[i]);
                    return;
                case BlockStatement block:
                    for (var i = 0; i < block.Functions.Count; i++) Visit(block.Functions[i]);
                    for (var i = 0; i < block.Statements.Count; i++) Visit(block.Statements[i]);
                    return;
                case FunctionDeclaration function:
                    for (var i = 0; i < function.Parameters.Count; i++) Visit(function.Parameters[i].Initializer);
                    Visit(function.Body);
                    return;
                case VariableDeclaration variable:
                    Visit(variable.Initializer);
                    return;
                case ExpressionStatement statement:
                    Visit(statement.Expression);
                    return;
                case NameExpression name:
                    VisitName(name);
                    return;
                case GetPropertyExpression property:
                    VisitProperty(property);
                    return;
                case FunctionCallExpression call:
                    Visit(call.Target);
                    for (var i = 0; i < call.Arguments.Count; i++) Visit(call.Arguments[i]);
                    return;
                case NewExpression newExpression:
                    Visit(newExpression.Expression);
                    return;
                case AssignmentExpression assignment:
                    Visit(assignment.Left);
                    Visit(assignment.Right);
                    return;
                case CompoundExpression compound:
                    Visit(compound.Left);
                    Visit(compound.Right);
                    return;
                case BinaryExpression binary:
                    Visit(binary.Left);
                    Visit(binary.Right);
                    return;
                case IncludedExpression included:
                    Visit(included.Left);
                    Visit(included.Right);
                    return;
                case InExpression inExpression:
                    Visit(inExpression.Left);
                    Visit(inExpression.Right);
                    return;
                case PrefixUnaryExpression unary:
                    Visit(unary.Expression);
                    return;
                case GetElementExpression getElement:
                    Visit(getElement.Object);
                    Visit(getElement.Index);
                    return;
                case SetPropertyExpression setProperty:
                    VisitSetProperty(setProperty);
                    Visit(setProperty.Value);
                    return;
                case SetElementExpression setElement:
                    Visit(setElement.Object);
                    Visit(setElement.Index);
                    Visit(setElement.Value);
                    return;
                case GroupExpression group:
                    for (var i = 0; i < group.Expressions.Count; i++) Visit(group.Expressions[i]);
                    return;
                case ArrayLiteralExpression array:
                    for (var i = 0; i < array.Elements.Count; i++) Visit(array.Elements[i]);
                    return;
                case MapExpression map:
                    for (var i = 0; i < map.Entries.Count; i++) Visit(map.Entries[i]);
                    return;
                case MapKeyValueExpression mapEntry:
                    Visit(mapEntry.Value);
                    return;
                case TemplateStringExpression template:
                    for (var i = 0; i < template.Parts.Count; i++) Visit(template.Parts[i].Expression);
                    return;
                case LambdaExpression lambda:
                    Visit(lambda.Function);
                    return;
                case ReturnStatement returnStatement:
                    Visit(returnStatement.Expression);
                    return;
                case ThrowStatement throwStatement:
                    Visit(throwStatement.Expression);
                    return;
                case DeleteStatement deleteStatement:
                    Visit(deleteStatement.Expression);
                    return;
                case IfStatement ifStatement:
                    Visit(ifStatement.Condition);
                    Visit(ifStatement.Body);
                    Visit(ifStatement.Else);
                    return;
                case WhileStatement whileStatement:
                    Visit(whileStatement.Condition);
                    Visit(whileStatement.Body);
                    return;
                case ForStatement forStatement:
                    Visit(forStatement.Initializer);
                    Visit(forStatement.Condition);
                    Visit(forStatement.Incrementor);
                    Visit(forStatement.Body);
                    return;
                case ForInStatement forInStatement:
                    Visit(forInStatement.Initializer);
                    Visit(forInStatement.Iterator);
                    Visit(forInStatement.Body);
                    return;
                case TryStatement tryStatement:
                    Visit(tryStatement.Body);
                    Visit(tryStatement.CatchBody);
                    Visit(tryStatement.FinallyBody);
                    return;
            }
        }

        private void VisitName(NameExpression name)
        {
            if (_localIndex.IsLocalReference(name))
            {
                return;
            }

            var value = name.Identifier.Value;
            if (_target.ImportAlias != null && value == _target.ImportAlias.Alias && _module.Path == _target.Symbol.FilePath)
            {
                _references.Add(new ReferenceLocation(_module.Path, TextRange.FromSourceSpan(name.Identifier.Range)));
                return;
            }

            if (value == _target.Symbol.Name && _module.Path == _target.Symbol.FilePath)
            {
                _references.Add(new ReferenceLocation(_module.Path, TextRange.FromSourceSpan(name.Identifier.Range)));
                return;
            }

            if (value == _target.Symbol.Name && TryResolveIncludedExport(_index, _module, value, out var included) && SameSymbol(included, _target.Symbol))
            {
                _references.Add(new ReferenceLocation(_module.Path, TextRange.FromSourceSpan(name.Identifier.Range)));
            }
        }

        private void VisitProperty(GetPropertyExpression property)
        {
            if (property.Object is NameExpression alias &&
                property.Property is NameExpression member &&
                member.Identifier.Value == _target.Symbol.Name &&
                _module.ImportsByAlias.TryGetValue(alias.Identifier.Value, out var import))
            {
                var importedModule = _index.TryGetModule(import.TargetPath);
                if (importedModule != null &&
                    importedModule.Exports.TryGetValue(member.Identifier.Value, out var importedSymbol) &&
                    SameSymbol(importedSymbol, _target.Symbol))
                {
                    _references.Add(new ReferenceLocation(_module.Path, TextRange.FromSourceSpan(member.Identifier.Range)));
                }
            }

            Visit(property.Object);
        }

        private void VisitSetProperty(SetPropertyExpression property)
        {
            if (property.Object is NameExpression alias &&
                property.Property is NameExpression member &&
                member.Identifier.Value == _target.Symbol.Name &&
                _module.ImportsByAlias.TryGetValue(alias.Identifier.Value, out var import))
            {
                var importedModule = _index.TryGetModule(import.TargetPath);
                if (importedModule != null &&
                    importedModule.Exports.TryGetValue(member.Identifier.Value, out var importedSymbol) &&
                    SameSymbol(importedSymbol, _target.Symbol))
                {
                    _references.Add(new ReferenceLocation(_module.Path, TextRange.FromSourceSpan(member.Identifier.Range)));
                }
            }

            Visit(property.Object);
        }

        private static bool SameSymbol(AuroraSymbolInfo left, AuroraSymbolInfo right)
        {
            return PathComparer.Equals(left.FilePath, right.FilePath) &&
                string.Equals(left.Name, right.Name, StringComparison.Ordinal);
        }
    }
}
