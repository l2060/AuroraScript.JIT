using AuroraScript.Compiler;
using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Core;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using AuroraScript.Source;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.IO;

namespace AuroraScript.LanguageServices.Internal;

internal static class SemanticTokenScanner
{
    private const int BraceLevelCount = 6;

    private static readonly HashSet<string> LanguageVariables = new(StringComparer.Ordinal)
    {
        "$arg",
        "$args",
        "$state"
    };

    public static SemanticTokensResult Scan(
        string sourceName,
        string sourceText,
        string? baseDirectory,
        BuiltinApiCatalog builtins)
    {
        baseDirectory ??= Directory.GetCurrentDirectory();
        var fullPath = ScriptPath.GetFullPath(baseDirectory, sourceName);
        try
        {
            using var lexer = new AuroraLexer(baseDirectory, new MemorySource(baseDirectory, fullPath, sourceText));
            var tokenInfos = new AuroraLexer.LexerTokenInfo[lexer.TokenCount];
            for (var i = 0; i < tokenInfos.Length; i++)
            {
                tokenInfos[i] = lexer.GetTokenInfo(i);
            }

            var builder = new SemanticTokenBuilder(sourceText);
            AddLexerTokens(sourceText, tokenInfos, builder);
            AddAstTokens(baseDirectory, fullPath, sourceText, builtins, builder);

            return new SemanticTokensResult(builder.ToSemanticTokens());
        }
        catch (AuroraCompilationException)
        {
            return new SemanticTokensResult(Array.Empty<SemanticToken>());
        }
    }

    private static void AddLexerTokens(
        string sourceText,
        IReadOnlyList<AuroraLexer.LexerTokenInfo> tokens,
        SemanticTokenBuilder builder)
    {
        var braceDepth = 0;
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Range.Length <= 0 || token.Kind == AuroraLexer.LexTokenKind.EndOfFile)
            {
                continue;
            }

            if (token.Kind == AuroraLexer.LexTokenKind.String)
            {
                if (!TryAddStringBlockTokens(sourceText, token, builder))
                {
                    AddStringLiteralTokens(sourceText, token, builder);
                }
                continue;
            }

            var type = GetLexerSemanticType(token, ref braceDepth);
            if (type < 0)
            {
                continue;
            }

            builder.Add(token.Range, type, SemanticTokenPriority.Lexer);
        }
    }

    private static int GetLexerSemanticType(
        AuroraLexer.LexerTokenInfo token,
        ref int braceDepth)
    {
        if (token.Kind == AuroraLexer.LexTokenKind.Keyword)
        {
            return GetKeywordSemanticType(token.Value);
        }

        if (token.Kind == AuroraLexer.LexTokenKind.Punctuator ||
            token.Kind == AuroraLexer.LexTokenKind.Operator)
        {
            return GetPunctuationOrOperatorSemanticType(token.Value, ref braceDepth);
        }

        return token.Kind switch
        {
            AuroraLexer.LexTokenKind.Number => AuroraSemanticTokenTypes.Number,
            AuroraLexer.LexTokenKind.Regex => AuroraSemanticTokenTypes.Regexp,
            AuroraLexer.LexTokenKind.Boolean => AuroraSemanticTokenTypes.Keyword,
            AuroraLexer.LexTokenKind.Null => AuroraSemanticTokenTypes.Keyword,
            AuroraLexer.LexTokenKind.StringTemplate => -1,
            _ => -1
        };
    }

    private static int GetKeywordSemanticType(string value)
    {
        return value switch
        {
            "if" or "else" or "for" or "while" or "break" or "continue" => AuroraSemanticTokenTypes.ControlFlow,
            "return" => AuroraSemanticTokenTypes.Return,
            "throw" => AuroraSemanticTokenTypes.Throw,
            "try" or "catch" or "finally" => AuroraSemanticTokenTypes.Exception,
            "import" or "include" or "from" or "export" => AuroraSemanticTokenTypes.ImportExport,
            _ => AuroraSemanticTokenTypes.Keyword
        };
    }

    private static int GetPunctuationOrOperatorSemanticType(string value, ref int braceDepth)
    {
        switch (value)
        {
            case "{":
                var leftLevel = BraceLevelType(braceDepth);
                braceDepth++;
                return leftLevel;
            case "}":
                if (braceDepth > 0)
                {
                    braceDepth--;
                }
                return BraceLevelType(braceDepth);
            case "(":
            case ")":
                return AuroraSemanticTokenTypes.Parenthesis;
            case "[":
            case "]":
                return AuroraSemanticTokenTypes.Bracket;
            case ",":
                return AuroraSemanticTokenTypes.Comma;
            case ";":
                return AuroraSemanticTokenTypes.Semicolon;
            case ".":
                return AuroraSemanticTokenTypes.Dot;
            case ":":
                return AuroraSemanticTokenTypes.Colon;
            case "in":
            case "typeof":
                return AuroraSemanticTokenTypes.Keyword;
            default:
                return AuroraSemanticTokenTypes.Operator;
        }
    }

    private static int BraceLevelType(int depth)
    {
        return AuroraSemanticTokenTypes.BraceLevel1 + (Math.Max(depth, 0) % BraceLevelCount);
    }

    private static bool TryAddStringBlockTokens(
        string sourceText,
        AuroraLexer.LexerTokenInfo token,
        SemanticTokenBuilder builder)
    {
        if (token.Range.Length < 3 ||
            token.Range.Offset < 0 ||
            token.Range.Offset + token.Range.Length > sourceText.Length ||
            sourceText[token.Range.Offset] != '|' ||
            sourceText[token.Range.Offset + 1] != '>' ||
            sourceText[token.Range.Offset + 2] != ' ')
        {
            return false;
        }

        var line = token.Range.StartLine - 1;
        var character = token.Range.StartColumn - 1;
        var offset = token.Range.Offset;
        var end = token.Range.Offset + token.Range.Length;

        while (offset < end)
        {
            var lineStart = offset;
            while (offset < end && sourceText[offset] != '\r' && sourceText[offset] != '\n')
            {
                offset++;
            }

            AddStringBlockLineToken(sourceText, lineStart, offset, line, character, builder);

            if (offset < end)
            {
                if (sourceText[offset] == '\r' && offset + 1 < end && sourceText[offset + 1] == '\n')
                {
                    offset += 2;
                }
                else
                {
                    offset++;
                }

                line++;
                character = 0;
                while (offset < end && (sourceText[offset] == ' ' || sourceText[offset] == '\t'))
                {
                    offset++;
                    character++;
                }
            }
        }

        return true;
    }

    private static void AddStringBlockLineToken(
        string sourceText,
        int lineStart,
        int lineEnd,
        int line,
        int character,
        SemanticTokenBuilder builder)
    {
        if (lineEnd - lineStart < 3 ||
            sourceText[lineStart] != '|' ||
            sourceText[lineStart + 1] != '>' ||
            sourceText[lineStart + 2] != ' ')
        {
            return;
        }

        var textStart = lineStart + 3;
        var length = lineEnd - textStart;
        if (length <= 0)
        {
            return;
        }

        builder.Add(
            line,
            character + 3,
            length,
            textStart,
            AuroraSemanticTokenTypes.String,
            SemanticTokenPriority.Lexer);
    }

    private static void AddStringLiteralTokens(
        string sourceText,
        AuroraLexer.LexerTokenInfo token,
        SemanticTokenBuilder builder)
    {
        var range = token.Range;
        if (range.Offset < 0 || range.Length <= 0 || range.Offset + range.Length > sourceText.Length)
        {
            return;
        }

        var start = range.Offset;
        var end = range.Offset + range.Length;
        if (end - start < 2)
        {
            builder.Add(range, AuroraSemanticTokenTypes.String, SemanticTokenPriority.Lexer);
            return;
        }

        var segmentStart = start;
        for (var offset = start + 1; offset < end - 1; offset++)
        {
            if (sourceText[offset] != '\\' || offset + 1 >= end)
            {
                continue;
            }

            AddSingleLineSegment(sourceText, segmentStart, offset, AuroraSemanticTokenTypes.String, builder);
            AddSingleLineSegment(sourceText, offset, Math.Min(offset + 2, end), AuroraSemanticTokenTypes.Character, builder);
            offset++;
            segmentStart = offset + 1;
        }

        AddSingleLineSegment(sourceText, segmentStart, end, AuroraSemanticTokenTypes.String, builder);
    }

    private static void AddSingleLineSegment(
        string sourceText,
        int startOffset,
        int endOffset,
        int type,
        SemanticTokenBuilder builder)
    {
        if (endOffset <= startOffset)
        {
            return;
        }

        var offset = startOffset;
        while (offset < endOffset)
        {
            var segmentStart = offset;
            while (offset < endOffset && sourceText[offset] != '\r' && sourceText[offset] != '\n')
            {
                offset++;
            }

            if (offset > segmentStart)
            {
                var position = PositionFromOffset(sourceText, segmentStart);
                builder.Add(
                    position.Line,
                    position.Character,
                    offset - segmentStart,
                    segmentStart,
                    type,
                    SemanticTokenPriority.Lexer);
            }

            if (offset < endOffset)
            {
                if (sourceText[offset] == '\r' && offset + 1 < endOffset && sourceText[offset + 1] == '\n')
                {
                    offset += 2;
                }
                else
                {
                    offset++;
                }
            }
        }
    }

    private static void AddAstTokens(
        string baseDirectory,
        string fullPath,
        string sourceText,
        BuiltinApiCatalog builtins,
        SemanticTokenBuilder builder)
    {
        using var lexer = new AuroraLexer(baseDirectory, new MemorySource(baseDirectory, fullPath, sourceText));
        var parser = new AuroraParser(lexer, EngineOptions.Default);
        var module = parser.Parse();
        var visitor = new SemanticAstVisitor(builtins, builder);
        visitor.Visit(module);
    }

    private static (int Line, int Character) PositionFromOffset(string sourceText, int targetOffset)
    {
        var line = 0;
        var character = 0;
        var limit = Math.Min(Math.Max(targetOffset, 0), sourceText.Length);
        for (var offset = 0; offset < limit; offset++)
        {
            if (sourceText[offset] == '\r')
            {
                if (offset + 1 < limit && sourceText[offset + 1] == '\n')
                {
                    offset++;
                }

                line++;
                character = 0;
            }
            else if (sourceText[offset] == '\n')
            {
                line++;
                character = 0;
            }
            else
            {
                character++;
            }
        }

        return (line, character);
    }

    private enum SemanticTokenPriority
    {
        Lexer = 0,
        Identifier = 1,
        Declaration = 2,
        Ast = 3
    }

    private readonly record struct SemanticTokenEntry(
        int Line,
        int Character,
        int Length,
        int Offset,
        int Type,
        SemanticTokenPriority Priority);

    private sealed class SemanticTokenBuilder
    {
        private readonly string _sourceText;
        private readonly Dictionary<(int Offset, int Length), SemanticTokenEntry> _byRange = new();

        public SemanticTokenBuilder(string sourceText)
        {
            _sourceText = sourceText;
        }

        public void Add(SourceSpan range, int type, SemanticTokenPriority priority)
        {
            if (range.Length <= 0 || range.StartLine <= 0 || range.StartColumn <= 0 || range.StartLine != range.EndLine)
            {
                return;
            }

            Add(
                range.StartLine - 1,
                range.StartColumn - 1,
                range.Length,
                range.Offset,
                type,
                priority);
        }

        public void AddToken(Token token, int type, SemanticTokenPriority priority)
        {
            if (token == null)
            {
                return;
            }

            Add(token.Range, type, priority);
        }

        public void Add(
            int line,
            int character,
            int length,
            int offset,
            int type,
            SemanticTokenPriority priority)
        {
            if (line < 0 || character < 0 || length <= 0 || offset < 0 || offset + length > _sourceText.Length)
            {
                return;
            }

            var entry = new SemanticTokenEntry(line, character, length, offset, type, priority);
            var key = (offset, length);
            if (_byRange.TryGetValue(key, out var existing) && existing.Priority > priority)
            {
                return;
            }

            _byRange[key] = entry;
        }

        public IReadOnlyList<SemanticToken> ToSemanticTokens()
        {
            var entries = new List<SemanticTokenEntry>(_byRange.Values);
            entries.Sort(static (left, right) =>
            {
                var priorityCompare = right.Priority.CompareTo(left.Priority);
                if (priorityCompare != 0)
                {
                    return priorityCompare;
                }

                var lengthCompare = right.Length.CompareTo(left.Length);
                if (lengthCompare != 0)
                {
                    return lengthCompare;
                }

                var lineCompare = left.Line.CompareTo(right.Line);
                if (lineCompare != 0)
                {
                    return lineCompare;
                }

                return left.Character.CompareTo(right.Character);
            });

            var selected = new List<SemanticTokenEntry>(entries.Count);
            foreach (var entry in entries)
            {
                var overlaps = false;
                for (var i = 0; i < selected.Count; i++)
                {
                    var existing = selected[i];
                    if (entry.Offset < existing.Offset + existing.Length &&
                        existing.Offset < entry.Offset + entry.Length)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    selected.Add(entry);
                }
            }

            selected.Sort(static (left, right) =>
            {
                var lineCompare = left.Line.CompareTo(right.Line);
                if (lineCompare != 0)
                {
                    return lineCompare;
                }

                var characterCompare = left.Character.CompareTo(right.Character);
                if (characterCompare != 0)
                {
                    return characterCompare;
                }

                return left.Length.CompareTo(right.Length);
            });

            var result = new List<SemanticToken>(selected.Count);
            foreach (var entry in selected)
            {
                result.Add(new SemanticToken(entry.Line, entry.Character, entry.Length, entry.Type, 0));
            }

            return result;
        }
    }

    private sealed class SemanticAstVisitor : IAstVisitor
    {
        private readonly BuiltinApiCatalog _builtins;
        private readonly SemanticTokenBuilder _builder;
        private readonly Stack<HashSet<string>> _scopes = new();
        private readonly HashSet<string> _moduleEnums = new(StringComparer.Ordinal);

        public SemanticAstVisitor(BuiltinApiCatalog builtins, SemanticTokenBuilder builder)
        {
            _builtins = builtins;
            _builder = builder;
        }

        public void Visit(AstNode? node)
        {
            node?.Accept(this);
        }

        protected override void VisitModule(ModuleDeclaration node)
        {
            PushScope();
            try
            {
                for (var i = 0; i < node.Imports.Count; i++)
                {
                    var importName = node.Imports[i].Name;
                    if (importName != null)
                    {
                        Declare(importName.Value);
                        _builder.AddToken(importName, AuroraSemanticTokenTypes.Namespace, SemanticTokenPriority.Declaration);
                    }
                }

                for (var i = 0; i < node.Statements.Count; i++)
                {
                    Predeclare(node.Statements[i]);
                }

                for (var i = 0; i < node.Functions.Count; i++)
                {
                    Predeclare(node.Functions[i]);
                }

                for (var i = 0; i < node.Imports.Count; i++)
                {
                    node.Imports[i].Accept(this);
                }

                for (var i = 0; i < node.Statements.Count; i++)
                {
                    node.Statements[i].Accept(this);
                }

                for (var i = 0; i < node.Functions.Count; i++)
                {
                    node.Functions[i].Accept(this);
                }
            }
            finally
            {
                PopScope();
            }
        }

        protected override void VisitBlock(BlockStatement node)
        {
            PushScope();
            try
            {
                for (var i = 0; i < node.Statements.Count; i++)
                {
                    Predeclare(node.Statements[i]);
                }

                for (var i = 0; i < node.Functions.Count; i++)
                {
                    Predeclare(node.Functions[i]);
                }

                for (var i = 0; i < node.Statements.Count; i++)
                {
                    node.Statements[i].Accept(this);
                }

                for (var i = 0; i < node.Functions.Count; i++)
                {
                    node.Functions[i].Accept(this);
                }
            }
            finally
            {
                PopScope();
            }
        }

        protected override void VisitFunction(FunctionDeclaration node)
        {
            if (node.Name != null)
            {
                _builder.AddToken(node.Name, AuroraSemanticTokenTypes.Function, SemanticTokenPriority.Declaration);
            }

            PushScope();
            try
            {
                for (var i = 0; i < node.Parameters.Count; i++)
                {
                    if (node.Parameters[i].Name != null)
                    {
                        Declare(node.Parameters[i].Name.Value);
                    }
                }

                for (var i = 0; i < node.Parameters.Count; i++)
                {
                    node.Parameters[i].Accept(this);
                }

                node.Body?.Accept(this);
            }
            finally
            {
                PopScope();
            }
        }

        protected override void VisitParameterDeclaration(ParameterDeclaration node)
        {
            if (node.Name != null)
            {
                _builder.AddToken(node.Name, AuroraSemanticTokenTypes.Parameter, SemanticTokenPriority.Declaration);
            }

            node.Initializer?.Accept(this);
        }

        protected override void VisitVarDeclaration(VariableDeclaration node)
        {
            if (node.Name != null)
            {
                _builder.AddToken(node.Name, AuroraSemanticTokenTypes.Variable, SemanticTokenPriority.Declaration);
            }

            node.Pattern?.Accept(this);
            node.Initializer?.Accept(this);
        }

        protected override void VisitEnumDeclaration(EnumDeclaration node)
        {
            if (node.Identifier != null)
            {
                _builder.AddToken(node.Identifier, AuroraSemanticTokenTypes.Enum, SemanticTokenPriority.Declaration);
            }

            if (node.Elements == null)
            {
                return;
            }

            for (var i = 0; i < node.Elements.Count; i++)
            {
                _builder.AddToken(node.Elements[i].Name, AuroraSemanticTokenTypes.EnumMember, SemanticTokenPriority.Declaration);
            }
        }

        protected override void VisitName(NameExpression node)
        {
            var value = node.Identifier.Value;
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (LanguageVariables.Contains(value))
            {
                _builder.AddToken(node.Identifier, AuroraSemanticTokenTypes.BuiltinVariable, SemanticTokenPriority.Identifier);
                return;
            }

            if (IsDeclared(value))
            {
                if (_moduleEnums.Contains(value))
                {
                    _builder.AddToken(node.Identifier, AuroraSemanticTokenTypes.Enum, SemanticTokenPriority.Identifier);
                }

                return;
            }

            if (_builtins != null && _builtins.TryGetGlobal(value, out var global))
            {
                _builder.AddToken(node.Identifier, GetBuiltinGlobalSemanticType(value, global), SemanticTokenPriority.Identifier);
            }
        }

        protected override void VisitCallExpression(FunctionCallExpression node)
        {
            for (var i = 0; i < node.Arguments.Count; i++)
            {
                node.Arguments[i].Accept(this);
            }

            AddCallTargetToken(node.Target);
            VisitCallTargetChildren(node.Target);
        }

        protected override void VisitGetPropertyExpression(GetPropertyExpression node)
        {
            AddMemberAccessTokens(node.Object, node.Property, isCall: false);
            node.Object.Accept(this);
        }

        protected override void VisitSetPropertyExpression(SetPropertyExpression node)
        {
            AddMemberAccessTokens(node.Object, node.Property, isCall: false);
            node.Object.Accept(this);
            node.Value.Accept(this);
        }

        protected override void VisitMapExpression(MapExpression node)
        {
            for (var i = 0; i < node.Entries.Count; i++)
            {
                var entry = node.Entries[i];
                if (entry is MapKeyValueExpression property)
                {
                    _builder.AddToken(property.Key, AuroraSemanticTokenTypes.MapKey, SemanticTokenPriority.Ast);
                    property.Value.Accept(this);
                }
                else
                {
                    entry.Accept(this);
                }
            }
        }

        protected override void VisitTemplateStringExpression(TemplateStringExpression node)
        {
        }

        private void AddCallTargetToken(Expression target)
        {
            if (target is NameExpression name)
            {
                var value = name.Identifier.Value;
                var type = AuroraSemanticTokenTypes.FunctionCall;
                if (!IsDeclared(value) && _builtins != null && _builtins.TryGetGlobal(value, out var global))
                {
                    type = global.Kind == BuiltinApiKind.Constructor
                        ? AuroraSemanticTokenTypes.Type
                        : global.Kind == BuiltinApiKind.Object
                            ? AuroraSemanticTokenTypes.Object
                            : AuroraSemanticTokenTypes.FunctionCall;
                }

                _builder.AddToken(name.Identifier, type, SemanticTokenPriority.Ast);
                return;
            }

            if (target is GetPropertyExpression getProperty)
            {
                AddMemberAccessTokens(getProperty.Object, getProperty.Property, isCall: true);
            }
        }

        private void VisitCallTargetChildren(Expression target)
        {
            if (target is NameExpression)
            {
                return;
            }

            if (target is GetPropertyExpression getProperty)
            {
                getProperty.Object.Accept(this);
                return;
            }

            target.Accept(this);
        }

        private void AddMemberAccessTokens(Expression owner, Expression property, bool isCall)
        {
            if (owner is NameExpression ownerName)
            {
                var ownerValue = ownerName.Identifier.Value;
                if (!string.IsNullOrEmpty(ownerValue))
                {
                    if (_moduleEnums.Contains(ownerValue))
                    {
                        _builder.AddToken(ownerName.Identifier, AuroraSemanticTokenTypes.Enum, SemanticTokenPriority.Ast);
                    }
                    else if (!IsDeclared(ownerValue) && _builtins != null && _builtins.TryGetGlobal(ownerValue, out var global))
                    {
                        _builder.AddToken(ownerName.Identifier, GetBuiltinGlobalSemanticType(ownerValue, global), SemanticTokenPriority.Ast);
                    }
                }
            }

            if (property is not NameExpression propertyName)
            {
                property.Accept(this);
                return;
            }

            var memberType = isCall
                ? AuroraSemanticTokenTypes.MethodCall
                : AuroraSemanticTokenTypes.Property;

            if (owner is NameExpression enumOwner && _moduleEnums.Contains(enumOwner.Identifier.Value))
            {
                memberType = AuroraSemanticTokenTypes.EnumMember;
            }
            else if (owner is NameExpression builtinOwner &&
                !IsDeclared(builtinOwner.Identifier.Value) &&
                _builtins != null &&
                _builtins.TryGetGlobalMember(builtinOwner.Identifier.Value, propertyName.Identifier.Value, out var member))
            {
                memberType = GetBuiltinMemberSemanticType(member, isCall);
            }

            _builder.AddToken(propertyName.Identifier, memberType, SemanticTokenPriority.Ast);
        }

        private void Predeclare(Statement statement)
        {
            switch (statement)
            {
                case VariableDeclaration variable when variable.Name != null:
                    Declare(variable.Name.Value);
                    break;
                case FunctionDeclaration function:
                    Predeclare(function);
                    break;
                case EnumDeclaration enumDeclaration when enumDeclaration.Identifier != null:
                    Declare(enumDeclaration.Identifier.Value);
                    _moduleEnums.Add(enumDeclaration.Identifier.Value);
                    break;
            }
        }

        private void Predeclare(FunctionDeclaration function)
        {
            if (function.Name != null)
            {
                Declare(function.Name.Value);
            }
        }

        private void PushScope()
        {
            _scopes.Push(new HashSet<string>(StringComparer.Ordinal));
        }

        private void PopScope()
        {
            _scopes.Pop();
        }

        private void Declare(string name)
        {
            if (!string.IsNullOrEmpty(name) && _scopes.Count != 0)
            {
                _scopes.Peek().Add(name);
            }
        }

        private bool IsDeclared(string name)
        {
            foreach (var scope in _scopes)
            {
                if (scope.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetBuiltinGlobalSemanticType(string value, BuiltinApiSymbol global)
        {
            if (string.Equals(value, "global", StringComparison.Ordinal))
            {
                return AuroraSemanticTokenTypes.BuiltinVariable;
            }

            return global.Kind switch
            {
                BuiltinApiKind.Constructor => AuroraSemanticTokenTypes.Type,
                BuiltinApiKind.Object => AuroraSemanticTokenTypes.Object,
                BuiltinApiKind.Function => AuroraSemanticTokenTypes.Function,
                _ => AuroraSemanticTokenTypes.BuiltinVariable
            };
        }

        private static int GetBuiltinMemberSemanticType(BuiltinApiMember member, bool isCall)
        {
            return member.Kind switch
            {
                BuiltinApiKind.Method => isCall ? AuroraSemanticTokenTypes.MethodCall : AuroraSemanticTokenTypes.Method,
                BuiltinApiKind.Function => isCall ? AuroraSemanticTokenTypes.FunctionCall : AuroraSemanticTokenTypes.Function,
                BuiltinApiKind.Constant => AuroraSemanticTokenTypes.Property,
                BuiltinApiKind.Property => AuroraSemanticTokenTypes.Property,
                _ => isCall ? AuroraSemanticTokenTypes.MethodCall : AuroraSemanticTokenTypes.Property
            };
        }
    }
}
