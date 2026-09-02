using AuroraScript.Compiler;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Traversal;
using AuroraScript.LanguageServices.Features.Definition;
using AuroraScript.LanguageServices.Features.References;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal sealed class AuroraLocalSymbolIndex
{
    private readonly AuroraModuleIndex _module;
    private readonly List<LocalSymbol> _symbols = new();
    private readonly Dictionary<NameExpression, LocalSymbol> _references = new();
    private readonly Dictionary<FunctionDeclaration, LocalScope> _functionScopes = new();
    private readonly Dictionary<AstNode, LocalScope> _scopesByOwner = new();
    private int _nextSymbolId;

    private AuroraLocalSymbolIndex(AuroraModuleIndex module)
    {
        _module = module;
    }

    public static AuroraLocalSymbolIndex Build(AuroraModuleIndex module)
    {
        var index = new AuroraLocalSymbolIndex(module);
        index.BuildModule(module.Module);
        index.CollectModuleReferences(module.Module);
        return index;
    }

    public bool IsLocalReference(NameExpression name)
    {
        return _references.ContainsKey(name);
    }

    public bool TryGetDeclaration(NameExpression reference, out AstNode declaration)
    {
        if (_references.TryGetValue(reference, out var symbol) &&
            symbol.DeclarationNode != null)
        {
            declaration = symbol.DeclarationNode;
            return true;
        }

        declaration = null!;
        return false;
    }

    public IReadOnlyList<LocalSymbolInfo> GetVisibleSymbols(TextPosition position)
    {
        if (!TryGetScope(position, out var scope))
        {
            return Array.Empty<LocalSymbolInfo>();
        }

        var result = new List<LocalSymbolInfo>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var current = scope; current != null; current = current.Parent)
        {
            foreach (var pair in current.Declarations)
            {
                if (!seen.Add(pair.Key))
                {
                    continue;
                }

                var symbol = pair.Value;
                result.Add(new LocalSymbolInfo(
                    symbol.Name,
                    symbol.DeclarationRange,
                    symbol.HasDeclarationRange,
                    symbol.Kind));
            }
        }

        return result;
    }

    public bool TryGetReferences(
        TextPosition position,
        bool includeDeclaration,
        out IReadOnlyList<ReferenceLocation> references)
    {
        if (!TryResolveSymbol(position, requireDeclaration: false, out var symbol))
        {
            references = Array.Empty<ReferenceLocation>();
            return false;
        }

        references = GetReferences(symbol, includeDeclaration);
        return true;
    }

    public bool TryGetDefinition(TextPosition position, out DefinitionLocation definition)
    {
        if (!TryResolveSymbol(position, requireDeclaration: false, out var symbol) ||
            !symbol.HasDeclarationRange)
        {
            definition = null!;
            return false;
        }

        var path = string.IsNullOrEmpty(symbol.DeclarationRange.FileName)
            ? _module.Path
            : symbol.DeclarationRange.FileName;
        definition = new DefinitionLocation(path, symbol.DeclarationRange);
        return true;
    }

    public bool TryGetRenameReferences(
        TextPosition position,
        out IReadOnlyList<ReferenceLocation> references)
    {
        if (!TryResolveSymbol(position, requireDeclaration: true, out var symbol))
        {
            references = Array.Empty<ReferenceLocation>();
            return false;
        }

        references = GetReferences(symbol, includeDeclaration: true);
        return true;
    }

    private IReadOnlyList<ReferenceLocation> GetReferences(LocalSymbol symbol, bool includeDeclaration)
    {
        var result = new List<ReferenceLocation>();
        var seen = new HashSet<TextRange>();
        if (includeDeclaration && symbol.HasDeclarationRange)
        {
            AddReference(result, seen, symbol.DeclarationRange);
        }

        foreach (var pair in _references)
        {
            if (ReferenceEquals(pair.Value, symbol))
            {
                AddReference(result, seen, TextRange.FromSourceSpan(pair.Key.Identifier.Range));
            }
        }

        return result;
    }

    private bool TryResolveSymbol(TextPosition position, bool requireDeclaration, out LocalSymbol symbol)
    {
        for (var i = 0; i < _symbols.Count; i++)
        {
            var candidate = _symbols[i];
            if (candidate.HasDeclarationRange &&
                Contains(candidate.DeclarationRange, position))
            {
                symbol = candidate;
                return true;
            }
        }

        foreach (var pair in _references)
        {
            if (Contains(TextRange.FromSourceSpan(pair.Key.Identifier.Range), position))
            {
                if (requireDeclaration && !pair.Value.HasDeclarationRange)
                {
                    break;
                }

                symbol = pair.Value;
                return true;
            }
        }

        symbol = null!;
        return false;
    }

    private bool TryGetScope(TextPosition position, out LocalScope scope)
    {
        scope = null!;
        foreach (var pair in _scopesByOwner)
        {
            if (pair.Key.Range.IsValid() &&
                pair.Key.Range.Contains(position) &&
                (scope == null || Contains(scope.Owner?.Range ?? SourceSpan.None, pair.Key.Range)))
            {
                scope = pair.Value;
            }
        }

        return scope != null;
    }

    private void BuildModule(ModuleDeclaration module)
    {
        for (var i = 0; i < module.Statements.Count; i++)
        {
            BuildNestedFunctions(module.Statements[i], parentScope: null);
        }

        for (var i = 0; i < module.Functions.Count; i++)
        {
            BuildFunction(module.Functions[i], parentScope: null);
        }
    }

    private void BuildFunction(FunctionDeclaration function, LocalScope? parentScope)
    {
        if (function == null ||
            function.Flags == FunctionFlags.Declare ||
            _functionScopes.ContainsKey(function))
        {
            return;
        }

        var rootScope = CreateScope(parentScope, function.Body ?? function);
        _functionScopes.Add(function, rootScope);

        if (function.Name != null && function.Flags == FunctionFlags.Lambda)
        {
            Declare(rootScope, function.Name.Value, TextRange.FromSourceSpan(function.Name.Range), LocalSymbolKind.Function);
        }

        for (var i = 0; i < function.Parameters.Count; i++)
        {
            var parameter = function.Parameters[i];
            if (parameter.Name != null)
            {
                Declare(
                    rootScope,
                    parameter.Name.Value,
                    TextRange.FromSourceSpan(parameter.Name.Range),
                    LocalSymbolKind.Variable,
                    parameter);
            }
        }

        CollectDeclarations(function.Body, rootScope, function.Body as BlockStatement);
    }

    private void BuildNestedFunctions(AstNode? node, LocalScope? parentScope)
    {
        if (node == null)
        {
            return;
        }

        switch (node)
        {
            case FunctionDeclaration function:
                BuildFunction(function, parentScope);
                return;
            case LambdaExpression lambda:
                BuildFunction(lambda.Function, parentScope);
                return;
        }

        var visitor = new NestedFunctionBuilder(this, parentScope);
        AstTraversal.VisitChildren(node, ref visitor);
    }

    private void CollectDeclarations(AstNode? node, LocalScope scope, BlockStatement? rootBody)
    {
        if (node == null)
        {
            return;
        }

        switch (node)
        {
            case BlockStatement block:
                var blockScope = ReferenceEquals(block, rootBody) ? scope : CreateScope(scope, block);
                for (var i = 0; i < block.Functions.Count; i++)
                {
                    CollectDeclarations(block.Functions[i], blockScope, rootBody);
                }
                for (var i = 0; i < block.Statements.Count; i++)
                {
                    CollectDeclarations(block.Statements[i], blockScope, rootBody);
                }
                return;
            case VariableDeclaration variable:
                if (variable.IsDeclare)
                {
                    return;
                }
                DeclarePattern(scope, variable);
                CollectDeclarations(variable.Initializer, scope, rootBody);
                return;
            case FunctionDeclaration function:
                if (function.Flags != FunctionFlags.Declare && function.Name != null)
                {
                    Declare(
                        scope,
                        function.Name.Value,
                        TextRange.FromSourceSpan(function.Name.Range),
                        LocalSymbolKind.Function,
                        function);
                }
                BuildFunction(function, scope);
                return;
            case LambdaExpression lambda:
                BuildFunction(lambda.Function, scope);
                return;
            case ObjectDestructuringPattern:
            case ArrayDestructuringPattern:
                return;
            case TryStatement tryStatement:
                CollectDeclarations(tryStatement.Body, scope, rootBody);
                CollectCatchDeclarations(tryStatement, scope, rootBody);
                CollectDeclarations(tryStatement.FinallyBody, scope, rootBody);
                return;
        }

        var visitor = new DeclarationCollector(this, scope, rootBody);
        AstTraversal.VisitChildren(node, ref visitor);
    }

    private void CollectCatchDeclarations(TryStatement statement, LocalScope scope, BlockStatement? rootBody)
    {
        if (statement.CatchBody == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(statement.CatchVariable))
        {
            CollectDeclarations(statement.CatchBody, scope, rootBody);
            return;
        }

        var catchScope = CreateScope(scope, statement.CatchBody ?? statement);
        Declare(catchScope, statement.CatchVariable, null, LocalSymbolKind.Variable);
        if (statement.CatchBody is BlockStatement block)
        {
            for (var i = 0; i < block.Functions.Count; i++)
            {
                CollectDeclarations(block.Functions[i], catchScope, rootBody);
            }
            for (var i = 0; i < block.Statements.Count; i++)
            {
                CollectDeclarations(block.Statements[i], catchScope, rootBody);
            }
            return;
        }

        CollectDeclarations(statement.CatchBody, catchScope, rootBody);
    }

    private void DeclarePattern(LocalScope scope, VariableDeclaration variable)
    {
        var kind = variable.IsConst ? LocalSymbolKind.Constant : LocalSymbolKind.Variable;
        if (variable.Name != null)
        {
            Declare(
                scope,
                variable.Name.Value,
                TextRange.FromSourceSpan(variable.Name.Range),
                kind,
                variable);
            return;
        }

        DeclarePattern(scope, variable.Pattern, kind);
    }

    private void DeclarePattern(LocalScope scope, Expression? pattern, LocalSymbolKind kind = LocalSymbolKind.Variable)
    {
        switch (pattern)
        {
            case NameExpression name:
                Declare(scope, name.Identifier.Value, TextRange.FromSourceSpan(name.Identifier.Range), kind);
                return;
            case SpreadExpression { Expression: NameExpression spreadName }:
                Declare(scope, spreadName.Identifier.Value, TextRange.FromSourceSpan(spreadName.Identifier.Range), kind);
                return;
            case ObjectDestructuringPattern objectPattern:
                for (var i = 0; i < objectPattern.Properties.Count; i++)
                {
                    var property = objectPattern.Properties[i];
                    Declare(scope, property.Value, TextRange.FromSourceSpan(property.Range), kind);
                }
                return;
            case ArrayDestructuringPattern arrayPattern:
                for (var i = 0; i < arrayPattern.Elements.Count; i++)
                {
                    DeclarePattern(scope, arrayPattern.Elements[i], kind);
                }
                return;
        }
    }

    private void Declare(
        LocalScope scope,
        string name,
        TextRange? declarationRange,
        LocalSymbolKind kind,
        AstNode? declarationNode = null)
    {
        if (string.IsNullOrEmpty(name) ||
            scope.Declarations.ContainsKey(name))
        {
            return;
        }

        var symbol = new LocalSymbol(_nextSymbolId++, name, kind, declarationRange, declarationNode)
        {
            Scope = scope
        };
        scope.Declarations.Add(name, symbol);
        _symbols.Add(symbol);
    }

    private void CollectModuleReferences(ModuleDeclaration module)
    {
        for (var i = 0; i < module.Statements.Count; i++)
        {
            CollectReferences(module.Statements[i], currentScope: null);
        }

        for (var i = 0; i < module.Functions.Count; i++)
        {
            CollectFunctionReferences(module.Functions[i]);
        }
    }

    private void CollectFunctionReferences(FunctionDeclaration function)
    {
        if (function == null ||
            !_functionScopes.TryGetValue(function, out var rootScope))
        {
            return;
        }

        for (var i = 0; i < function.Parameters.Count; i++)
        {
            CollectReferences(function.Parameters[i].Initializer, rootScope);
        }

        CollectReferences(function.Body, rootScope);
    }

    private void CollectReferences(AstNode? node, LocalScope? currentScope)
    {
        if (node == null)
        {
            return;
        }

        switch (node)
        {
            case BlockStatement block:
                var blockScope = currentScope;
                if (currentScope != null && !IsFunctionRootBody(block))
                {
                    blockScope = GetBlockScope(block, currentScope);
                }

                for (var i = 0; i < block.Statements.Count; i++)
                {
                    CollectReferences(block.Statements[i], blockScope);
                }
                for (var i = 0; i < block.Functions.Count; i++)
                {
                    CollectFunctionReferences(block.Functions[i]);
                }
                return;
            case FunctionDeclaration function:
                CollectFunctionReferences(function);
                return;
            case LambdaExpression lambda:
                CollectFunctionReferences(lambda.Function);
                return;
            case VariableDeclaration variable:
                CollectReferences(variable.Initializer, currentScope);
                return;
            case NameExpression name:
                if (currentScope != null && TryResolve(currentScope, name.Identifier.Value, out var symbol))
                {
                    _references[name] = symbol;
                }
                return;
            case GetPropertyExpression property:
                CollectReferences(property.Object, currentScope);
                return;
            case SetPropertyExpression property:
                CollectReferences(property.Object, currentScope);
                CollectReferences(property.Value, currentScope);
                return;
            case MapKeyValueExpression mapEntry:
                CollectReferences(mapEntry.Value, currentScope);
                return;
            case TryStatement tryStatement:
                CollectReferences(tryStatement.Body, currentScope);
                CollectCatchReferences(tryStatement, currentScope);
                CollectReferences(tryStatement.FinallyBody, currentScope);
                return;
        }

        var visitor = new ReferenceCollector(this, currentScope);
        AstTraversal.VisitChildren(node, ref visitor);
    }

    private void CollectCatchReferences(TryStatement statement, LocalScope? currentScope)
    {
        if (statement.CatchBody == null)
        {
            return;
        }

        if (currentScope == null || string.IsNullOrEmpty(statement.CatchVariable))
        {
            CollectReferences(statement.CatchBody, currentScope);
            return;
        }

        var catchScope = GetCatchScope(statement, currentScope);
        if (statement.CatchBody is BlockStatement block)
        {
            for (var i = 0; i < block.Statements.Count; i++)
            {
                CollectReferences(block.Statements[i], catchScope);
            }
            for (var i = 0; i < block.Functions.Count; i++)
            {
                CollectFunctionReferences(block.Functions[i]);
            }
            return;
        }

        CollectReferences(statement.CatchBody, catchScope);
    }

    private LocalScope GetBlockScope(BlockStatement block, LocalScope fallback)
    {
        if (_scopesByOwner.TryGetValue(block, out var scope))
        {
            return scope;
        }

        return fallback;
    }

    private LocalScope GetCatchScope(TryStatement statement, LocalScope fallback)
    {
        var owner = statement.CatchBody ?? statement;
        if (_scopesByOwner.TryGetValue(owner, out var scope))
        {
            return scope;
        }

        return fallback;
    }

    private LocalScope CreateScope(LocalScope? parent, AstNode? owner)
    {
        var scope = new LocalScope(parent, owner);
        if (owner != null && !_scopesByOwner.ContainsKey(owner))
        {
            _scopesByOwner.Add(owner, scope);
        }

        return scope;
    }

    private bool IsFunctionRootBody(BlockStatement block)
    {
        foreach (var pair in _functionScopes)
        {
            if (ReferenceEquals(pair.Key.Body, block))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryResolve(LocalScope scope, string name, out LocalSymbol symbol)
    {
        for (var current = scope; current != null; current = current.Parent)
        {
            if (current.Declarations.TryGetValue(name, out symbol!))
            {
                return true;
            }
        }

        symbol = null!;
        return false;
    }

    private void AddReference(List<ReferenceLocation> references, HashSet<TextRange> seen, TextRange range)
    {
        if (seen.Add(range))
        {
            var path = string.IsNullOrEmpty(range.FileName) ? _module.Path : range.FileName;
            references.Add(new ReferenceLocation(path, range));
        }
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

    private static bool Contains(SourceSpan outer, SourceSpan inner)
    {
        if (!outer.IsValid() || !inner.IsValid())
        {
            return false;
        }

        if (inner.StartLine < outer.StartLine || inner.EndLine > outer.EndLine)
        {
            return false;
        }

        if (inner.StartLine == outer.StartLine && inner.StartColumn < outer.StartColumn)
        {
            return false;
        }

        if (inner.EndLine == outer.EndLine && inner.EndColumn > outer.EndColumn)
        {
            return false;
        }

        return true;
    }

    internal readonly record struct LocalSymbolInfo(
        string Name,
        TextRange DeclarationRange,
        bool HasDeclarationRange,
        LocalSymbolKind Kind);

    internal enum LocalSymbolKind
    {
        Variable,
        Constant,
        Function
    }

    private sealed class LocalScope
    {
        public LocalScope(LocalScope? parent, AstNode? owner = null)
        {
            Parent = parent;
            Owner = owner;
        }

        public LocalScope? Parent { get; }
        public AstNode? Owner { get; }
        public Dictionary<string, LocalSymbol> Declarations { get; } = new(StringComparer.Ordinal);
    }

    private sealed class LocalSymbol
    {
        public LocalSymbol(
            int id,
            string name,
            LocalSymbolKind kind,
            TextRange? declarationRange,
            AstNode? declarationNode)
        {
            Id = id;
            Name = name;
            Kind = kind;
            DeclarationRange = declarationRange.GetValueOrDefault();
            HasDeclarationRange = declarationRange.HasValue;
            DeclarationNode = declarationNode;
        }

        public int Id { get; }
        public string Name { get; }
        public LocalSymbolKind Kind { get; }
        public TextRange DeclarationRange { get; }
        public bool HasDeclarationRange { get; }
        public AstNode? DeclarationNode { get; }
        public LocalScope Scope { get; set; } = null!;
    }

    private readonly struct DeclarationCollector : IAstChildVisitor
    {
        private readonly AuroraLocalSymbolIndex _index;
        private readonly LocalScope _scope;
        private readonly BlockStatement? _rootBody;

        public DeclarationCollector(AuroraLocalSymbolIndex index, LocalScope scope, BlockStatement? rootBody)
        {
            _index = index;
            _scope = scope;
            _rootBody = rootBody;
        }

        public void Visit(AstNode node)
        {
            _index.CollectDeclarations(node, _scope, _rootBody);
        }
    }

    private readonly struct NestedFunctionBuilder : IAstChildVisitor
    {
        private readonly AuroraLocalSymbolIndex _index;
        private readonly LocalScope? _parentScope;

        public NestedFunctionBuilder(AuroraLocalSymbolIndex index, LocalScope? parentScope)
        {
            _index = index;
            _parentScope = parentScope;
        }

        public void Visit(AstNode node)
        {
            _index.BuildNestedFunctions(node, _parentScope);
        }
    }

    private readonly struct ReferenceCollector : IAstChildVisitor
    {
        private readonly AuroraLocalSymbolIndex _index;
        private readonly LocalScope? _scope;

        public ReferenceCollector(AuroraLocalSymbolIndex index, LocalScope? scope)
        {
            _index = index;
            _scope = scope;
        }

        public void Visit(AstNode node)
        {
            _index.CollectReferences(node, _scope);
        }
    }
}
