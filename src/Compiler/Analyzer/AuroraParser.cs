using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Source;
using AuroraScript.Tokens;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AuroraScript.Compiler.Analyzer
{

    internal enum ScopeType { MODULE, FOR, BLOCK, GROUP, FUNCTION }

    internal class ScopeStack : IDisposable
    {
        private ScopeType[] scopeStack = new ScopeType[16];
        private int count;
        public ScopeType Current => scopeStack[count - 1];
        public int Count => count;
        public ScopeType this[int index] => scopeStack[index];

        public ScopeStack Scope(ScopeType type)
        {
            if (count == scopeStack.Length)
            {
                Array.Resize(ref scopeStack, scopeStack.Length * 2);
            }
            scopeStack[count++] = type;
            return this;
        }

        public void Dispose()
        {
            count--;
        }
    }

    internal class AuroraParser
    {
        private sealed class SourceFileVisitor : IAstVisitor
        {
            private string _fileName;

            public void Apply(AstNode node, string fileName)
            {
                _fileName = fileName;
                node?.Accept(this);
            }

            protected override void BeforeVisitNode(AstNode node)
            {
                var range = node.Range;
                if (String.IsNullOrEmpty(range.FileName))
                {
                    range.FileName = _fileName;
                    node.Range = range;
                }
            }

            protected override void VisitArrayDestructuringPattern(ArrayDestructuringPattern node)
            {
                for (int i = 0; i < node.Elements.Count; i++) node.Elements[i]?.Accept(this);
            }

            protected override void VisitMapExpression(MapExpression node)
            {
                for (int i = 0; i < node.Entries.Count; i++)
                {
                    var entry = node.Entries[i];
                    if (entry is MapKeyValueExpression keyValue)
                    {
                        keyValue.Value?.Accept(this);
                    }
                    else
                    {
                        entry?.Accept(this);
                    }
                }
            }

            protected override void VisitEnumDeclaration(EnumDeclaration node)
            {
                if (node.Elements == null) return;
                for (int i = 0; i < node.Elements.Count; i++)
                {
                    var element = node.Elements[i];
                    var range = element.Range;
                    if (String.IsNullOrEmpty(range.FileName))
                    {
                        range.FileName = _fileName;
                        element.Range = range;
                    }
                }
            }
        }

        public AuroraLexer Lexer { get; private set; }
        public ModuleDeclaration Root { get; private set; }

        private readonly ScopeStack scopeStack = new ScopeStack();
        private readonly SourceFileVisitor _sourceFileVisitor = new SourceFileVisitor();

        private readonly EngineOptions _options;
        private List<TDocPathSegment> _tdocPath;
        private bool _allowTDocInterpolation = true;
        private bool _seenEffectiveModuleStatement;
        private bool _seenExplicitModuleMetadata;

        public AuroraParser(AuroraLexer lexer, EngineOptions options)
        {
            _options = options;
            this.Lexer = lexer;
            this.Root = new ModuleDeclaration(this.Lexer.SourceReference);
        }

        public ModuleDeclaration Parse()
        {
            using (scopeStack.Scope(ScopeType.MODULE))
            {
                while (true)
                {
                    if (this.Lexer.TestNext(Symbols.KW_EOF)) break;
                    var statementSymbol = this.Lexer.PeekSymbol();
                    // Use recursion for statements
                    var node = ParseStatement();

                    if (node == null)
                    {
                        if (statementSymbol == Symbols.PT_SEMICOLON)
                        {
                            _seenEffectiveModuleStatement = true;
                        }
                        continue;
                    }

                    node.IsIndependent = true;

                    if (node is ModuleMetaStatement meta)
                    {
                        if (meta.Name.Value == "module" && this.Root.IsGlobalDeclarationFile)
                        {
                            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, meta.Name, "@global() declaration files cannot also declare @module metadata.");
                        }

                        if (meta.Name.Value == "module" && _seenEffectiveModuleStatement)
                        {
                            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, meta.Name, "@module metadata must be the first effective statement in a module.");
                        }

                        if (meta.Name.Value == "module")
                        {
                            if (this.Root.IsGlobalDeclarationFile)
                            {
                                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, meta.Name, "@global() declaration files cannot also declare @module metadata.");
                            }

                            _seenExplicitModuleMetadata = true;
                            this.Root.MetaInfos[meta.Name.Value] = meta.Value?.Value;
                            this.Root.ModuleName = meta.Value?.Value;
                        }
                        else if (meta.Name.Value == "global")
                        {
                            if (_seenEffectiveModuleStatement)
                            {
                                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, meta.Name, "@global() metadata must be the first effective statement in a file.");
                            }

                            if (_seenExplicitModuleMetadata)
                            {
                                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, meta.Name, "@global() declaration files cannot also declare @module metadata.");
                            }

                            if (this.Root.MetaInfos.ContainsKey("module"))
                            {
                                this.Root.MetaInfos.Remove("module");
                            }

                            this.Root.MetaInfos[meta.Name.Value] = true;
                            this.Root.IsGlobalDeclarationFile = true;
                            this.Root.ModuleName = null;
                        }
                        else
                        {
                            this.Root.MetaInfos[meta.Name.Value] = meta.Value?.Value;
                        }
                    }
                    else if (node is FunctionDeclaration func)
                    {
                        RejectGlobalNonDeclareStatement(node);
                        this.Root.AddFunction(func);
                        func.Parent = this.Root;
                    }
                    else if (node is ImportDeclaration importDeclaration)
                    {
                        RejectGlobalNonDeclareStatement(node);
                        this.Root.AddImport(importDeclaration);
                        importDeclaration.Parent = Root;
                    }
                    else
                    {
                        RejectGlobalNonDeclareStatement(node);
                        this.Root.AddStatement(node);
                    }

                    _seenEffectiveModuleStatement = true;
                }
                SetSourceRecursive(this.Root);
            }
            if (scopeStack.Count > 0)
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, Lexer.FullPath, Token.EOF, "An premature ending statement.");
            }
            this.Lexer.Dispose();
            return this.Root;
        }

        /// <summary>
        /// Parses a standalone <c>.tdoc</c> document. The document starts
        /// directly with the TDoc root value (for example
        /// <c>Object { name "Aurora" }</c>) and is represented as a synthetic
        /// expression statement so language-service consumers can reuse the
        /// normal AST traversal and source ranges.
        /// </summary>
        public ModuleDeclaration ParseTDocDocument()
        {
            var previousInterpolationMode = _allowTDocInterpolation;
            _allowTDocInterpolation = false;
            try
            {
                using (scopeStack.Scope(ScopeType.MODULE))
                {
                    var value = ParseTDocValue();
                    if (value == null)
                    {
                        throw new AuroraCompilationException(
                            AuroraCompilationStage.Parsing,
                            Lexer.FullPath,
                            Lexer.LookAtHead(),
                            "A TDoc document requires a root value.");
                    }

                    // A terminator is optional in a standalone document, but
                    // accepting one keeps generated documents convenient.
                    Lexer.TestNext(Symbols.PT_SEMICOLON);
                    if (!Lexer.TestSymbol(Symbols.KW_EOF))
                    {
                        throw new AuroraCompilationException(
                            AuroraCompilationStage.Parsing,
                            Lexer.FullPath,
                            Lexer.LookAtHead(),
                            "A TDoc document must contain exactly one root value.");
                    }

                    var statement = new ExpressionStatement(value)
                    {
                        IsIndependent = true,
                        Range = value.Range
                    };
                    Root.AddStatement(statement);
                    SetSourceRecursive(Root);
                }

                Lexer.Expect(Symbols.KW_EOF);
                Lexer.Dispose();
                return Root;
            }
            finally
            {
                _allowTDocInterpolation = previousInterpolationMode;
            }
        }

        public BlockStatement ParseBlockBody()
        {
            using (scopeStack.Scope(ScopeType.FUNCTION))
            {
                var block = new BlockStatement { IsFunction = true };
                while (true)
                {
                    if (this.Lexer.TestNext(Symbols.KW_EOF)) break;
                    RejectModuleOnlyBlockStatement();
                    var node = ParseStatement();
                    if (node == null) continue;

                    node.IsIndependent = true;
                    if (node is FunctionDeclaration func)
                    {
                        block.AddFunction(func);
                        func.Parent = block;
                    }
                    else
                    {
                        block.AddStatement(node);
                    }
                }
                SetSourceRecursive(block);
                this.Lexer.Dispose();
                return block;
            }
        }

        private void RejectModuleOnlyBlockStatement()
        {
            var symbol = this.Lexer.PeekSymbol();
            if (symbol == null || symbol == Symbols.KW_EOF)
            {
                return;
            }

            if (symbol == Symbols.PT_METAINFO ||
                symbol == Symbols.KW_IMPORT ||
                symbol == Symbols.KW_INCLUDE ||
                symbol == Symbols.KW_EXPORT ||
                symbol == Symbols.KW_DECLARE)
            {
                var token = this.Lexer.LookAtHead();
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, token, $"CompileBlock does not support module-level statement '{token.Value}'.");
            }
        }

        private Statement ParseStatement()
        {
            var symbol = this.Lexer.PeekSymbol();
            if (this.Lexer.IsAtEnd) return null;

            if (symbol == Symbols.PT_SEMICOLON)
            {
                this.Lexer.Expect(Symbols.PT_SEMICOLON);
                return null;
            }

            if (symbol == Symbols.PT_METAINFO) { var res = ParseMetaInfoOrAnnotatedFunction(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.PT_LEFTBRACE) { var res = ParseBlock(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_IMPORT) { var res = ParseImport(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_INCLUDE) { var res = ParseInclude(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_EXPORT) { var res = ParseExportStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_FUNCTION || symbol == Symbols.KW_FUNC) { var res = ParseFunctionDeclaration(MemberAccess.Internal); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_DECLARE) { var res = ParseDeclare(MemberAccess.Internal); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_CONST || symbol == Symbols.KW_VAR) { var res = ParseVariableDeclaration(MemberAccess.Internal); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_ENUM) { var res = ParseEnumDeclaration(MemberAccess.Internal); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_FOR) { var res = ParseForBlock(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_WHILE) { var res = ParseWhileBlock(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_IF) { var res = ParseIfBlock(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_CONTINUE) { var res = ParseContinueStatement(); if (res != null) res.IsIndependent = true; return res; }

            if (symbol == Symbols.KW_BREAK) { var res = ParseBreakStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_RETURN) { var res = ParseReturnStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_THROW) { var res = ParseThrowStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_TRY) { var res = ParseTryStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_DELETE) { var res = ParseDeleteStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (symbol == Symbols.KW_DEBUGGER) { var res = ParseDebuggerStatement(); if (res != null) res.IsIndependent = true; return res; }


            // Expression Statement
            var exp = ParseExpression(0); // 0 is lowest precedence

            if (exp == null)
            {
                // If we are here, we have a token that is not a statement start, and not start of expression.
                // This is a syntax error.
                var token = this.Lexer.LookAtHead();
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, token, $"Unexpected token: {token.Value}");
            }

            var semiRange = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);

            var stmt = new ExpressionStatement(exp);
            stmt.IsIndependent = true;
            SetRange(stmt, exp.Range, semiRange);
            return stmt;
        }

        private void RejectGlobalNonDeclareStatement(Statement node)
        {
            if (!Root.IsGlobalDeclarationFile)
            {
                return;
            }

            if (node is VariableDeclaration { IsDeclare: true } ||
                node is FunctionDeclaration function && (function.Flags & FunctionFlags.Declare) != 0)
            {
                return;
            }

            throw new AuroraCompilationException(
                AuroraCompilationStage.Parsing,
                Lexer.FullPath,
                node.Range,
                "@global() declaration files only allow declare statements.");
        }

        // =================================================================================
        // Pratt Parsing for Expressions
        // =================================================================================

        private Expression ParseExpression(int precedence)
        {
            var symbol = this.Lexer.PeekSymbol();
            if (this.Lexer.IsAtEnd || symbol == Symbols.PT_SEMICOLON || symbol == Symbols.PT_RIGHTPARENTHESIS ||
                symbol == Symbols.PT_RIGHTBRACKET || symbol == Symbols.PT_RIGHTBRACE || symbol == Symbols.PT_COMMA)
            {
                return null;
            }

            Expression expression;
            if (CanParsePrefixWithoutToken(symbol))
            {
                var prefixSymbol = this.Lexer.NextSymbol(out var prefixRange);
                expression = ParsePrefix(prefixSymbol, prefixRange);
            }
            else
            {
                var startToken = this.Lexer.Next();
                expression = ParsePrefix(startToken);
            }
            if (expression == null) return null;

            while (precedence < GetPrecedence(this.Lexer.PeekSymbol()))
            {
                var opSymbol = this.Lexer.NextSymbol(out var opRange);
                var op = Operator.FromSymbols(opSymbol, true); // Infix/Postfix
                expression = ParseInfix(expression, opSymbol, opRange, op);
                if (expression == null) break;
            }

            return expression;
        }

        private Expression ParsePrefix(Token token)
        {
            if (token.Symbol == Symbols.KW_TDOC)
            {
                var value = ParseTDocValue();
                if (value == null)
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        token,
                        "tdoc requires a value.");
                }
                return SetRange(value, token.Range, value.Range);
            }

            // Literals
            if (token is ValueToken vt)
            {
                if (vt.Type == Tokens.ValueType.StringTemplate) return ParseStringTemplate((StringTemplateToken)vt);
                return SetRange(new LiteralExpression(vt), vt.Range, vt.Range);
            }
            if (token is IdentifierToken it) return SetRange(new NameExpression(it), it.Range, it.Range);

            // Grouping (
            if (token.Symbol == Symbols.PT_LEFTPARENTHESIS)
            {

                using (scopeStack.Scope(ScopeType.GROUP))
                {
                    var group = new GroupExpression(Operator.Grouping);
                    if (this.Lexer.TestSymbol(Symbols.PT_RIGHTPARENTHESIS))
                    {
                        this.Lexer.Expect(Symbols.PT_RIGHTPARENTHESIS);
                        return group;
                    }
                    while (true)
                    {
                        var exp = ParseExpression(0);
                        if (exp != null) group.AddExpression(exp);
                        if (this.Lexer.TestNext(Symbols.PT_COMMA)) continue;
                        break;
                    }
                    var rightParen = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTPARENTHESIS);
                    return SetRange(group, token.Range, rightParen);
                }

            }

            // Array Literal [
            if (token.Symbol == Symbols.PT_LEFTBRACKET)
            {
                var arrayLiteral = new ArrayLiteralExpression();
                if (!this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACKET))
                {
                    while (true)
                    {
                        var exp = ParseExpression(0);
                        if (exp != null)
                            arrayLiteral.AddElement(exp);
                        else
                        {
                            var nullLiteral = new LiteralExpression(new NullToken());
                            arrayLiteral.AddElement(nullLiteral);
                        }

                        if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACKET)) break;
                        this.Lexer.Expect(Symbols.PT_COMMA);
                    }
                }
                var rightBracket = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTBRACKET);
                return SetRange(arrayLiteral, token.Range, rightBracket);
            }

            // Object Literal {
            if (token.Symbol == Symbols.PT_LEFTBRACE)
            {
                this.Lexer.RollBack();
                return ParseObjectConstructor();
            }


            // Spread Operator (...)
            if (token.Symbol == Symbols.OP_SPREAD)
            {
                var value = ParseExpression(0); // Lowest precedence
                if (value == null)
                {
                    throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, token, "Spread requires an expression.");
                }
                var spread = new SpreadExpression(value);
                return SetRange(spread, token.Range, (value != null ? value.Range : token.Range));
            }

            // Unary Prefix Operators (!, -, ++, --, ~, typeof, new, ...)
            if (IsPrefixOperator(token.Symbol))
            {
                var op = Operator.FromSymbols(token.Symbol, false);

                // Special case for 'new'
                if (op == Operator.New)
                {
                    return ParseNewExpression(op, token.Range, token);
                }

                // Normal Unary
                var rightUnary = ParseExpression(op.Precedence);
                if (op == Operator.PreIncrement || op == Operator.PreDecrement)
                {
                    EnsureMutationTarget(rightUnary, op);
                }
                var expression = new UnaryExpression(op, UnaryType.Prefix, rightUnary);
                // SetDebug(expression, token); // Redundant if already handled
                if (rightUnary != null) expression.Range = MergeRanges(token.Range, rightUnary.Range);
                return expression;
            }



            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, token, $"Unknown prefix token: {token.Value}");
        }

        private Expression ParsePrefix(Symbols symbol, SourceSpan range)
        {
            // Grouping (
            if (symbol == Symbols.PT_LEFTPARENTHESIS)
            {
                using (scopeStack.Scope(ScopeType.GROUP))
                {
                    var group = new GroupExpression(Operator.Grouping);
                    if (this.Lexer.TestSymbol(Symbols.PT_RIGHTPARENTHESIS))
                    {
                        var rightParen = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTPARENTHESIS);
                        return SetRange(group, range, rightParen);
                    }
                    while (true)
                    {
                        var exp = ParseExpression(0);
                        if (exp != null) group.AddExpression(exp);
                        if (this.Lexer.TestNext(Symbols.PT_COMMA)) continue;
                        break;
                    }
                    var rightParenRange = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTPARENTHESIS);
                    return SetRange(group, range, rightParenRange);
                }
            }

            // Array Literal [
            if (symbol == Symbols.PT_LEFTBRACKET)
            {
                var arrayLiteral = new ArrayLiteralExpression();
                if (!this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACKET))
                {
                    while (true)
                    {
                        var exp = ParseExpression(0);
                        if (exp != null)
                            arrayLiteral.AddElement(exp);
                        else
                        {
                            var nullLiteral = new LiteralExpression(new NullToken());
                            arrayLiteral.AddElement(nullLiteral);
                        }

                        if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACKET)) break;
                        this.Lexer.Expect(Symbols.PT_COMMA);
                    }
                }
                var rightBracket = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTBRACKET);
                return SetRange(arrayLiteral, range, rightBracket);
            }

            // Object Literal {
            if (symbol == Symbols.PT_LEFTBRACE)
            {
                return ParseObjectConstructor(range);
            }

            // Spread Operator (...)
            if (symbol == Symbols.OP_SPREAD)
            {
                var value = ParseExpression(0); // Lowest precedence
                if (value == null)
                {
                    var token = new OperatorToken { Symbol = symbol, Value = symbol?.Name, Range = range };
                    throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, token, "Spread requires an expression.");
                }
                var spread = new SpreadExpression(value);
                return SetRange(spread, range, (value != null ? value.Range : range));
            }

            // Unary Prefix Operators (!, -, ++, --, ~, typeof, new, ...)
            if (IsPrefixOperator(symbol))
            {
                var op = Operator.FromSymbols(symbol, false);

                if (op == Operator.New)
                {
                    var token = new OperatorToken { Symbol = symbol, Value = symbol?.Name, Range = range };
                    return ParseNewExpression(op, range, token);
                }

                var rightUnary = ParseExpression(op.Precedence);
                if (op == Operator.PreIncrement || op == Operator.PreDecrement)
                {
                    EnsureMutationTarget(rightUnary, op);
                }
                var expression = new UnaryExpression(op, UnaryType.Prefix, rightUnary);
                if (rightUnary != null) expression.Range = MergeRanges(range, rightUnary.Range);
                return expression;
            }

            var unexpected = new OperatorToken { Symbol = symbol, Value = symbol?.Name, Range = range };
            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, unexpected, $"Unknown prefix token: {unexpected.Value}");
        }

        private Expression ParseInfix(Expression left, Symbols opSymbol, SourceSpan opRange, Operator op)
        {
            // Member Access .
            if (opSymbol == Symbols.PT_DOT)
            {

                var identifier = this.Lexer.NextOfToken<IdentifierToken, KeywordToken>();
                var dotExp = new GetPropertyExpression(Operator.MemberAccess, left, new NameExpression(identifier));
                return SetRange(dotExp, left.Range, identifier.Range);
            }

            // Index Access [
            // Function Call (
            if (opSymbol == Symbols.PT_LEFTPARENTHESIS)
            {
                var callExp = new FunctionCallExpression(Operator.FunctionCall, left);
                if (!this.Lexer.TestSymbol(Symbols.PT_RIGHTPARENTHESIS))
                {
                    while (true)
                    {
                        var arg = ParseExpression(0);
                        if (arg == null)
                        {
                            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, Lexer.FullPath, Lexer.LookAtHead(), "Function argument requires an expression.");
                        }
                        callExp.AddArgument(arg);

                        if (this.Lexer.TestSymbol(Symbols.PT_RIGHTPARENTHESIS)) break;
                        this.Lexer.Expect(Symbols.PT_COMMA);
                    }
                }
                var rightParen = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTPARENTHESIS);
                return SetRange(callExp, left.Range, rightParen);
            }

            // Index Access [
            if (opSymbol == Symbols.PT_LEFTBRACKET)
            {
                var indexExp = ParseExpression(0);
                if (indexExp == null)
                {
                    throw new AuroraCompilationException(AuroraCompilationStage.Parsing, Lexer.FullPath, Lexer.LookAtHead(), "Array index requires an expression.");
                }
                var getElem = new GetElementExpression(Operator.Index, left, indexExp);
                var rightBracket = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTBRACKET);
                return SetRange(getElem, left.Range, rightBracket);
            }

            // Lambda Expression =>
            if (op == Operator.Lambda)
            {
                return CreateLambda(left);
            }

            // Binary/Assignment
            if (op != null && op.Placement == OperatorPlacement.Binary)
            {
                bool isCompound = (op == Operator.CompoundAdd || op == Operator.CompoundSubtract || op == Operator.CompoundMultiply ||
                                    op == Operator.CompoundDivide || op == Operator.CompoundModulo);

                Expression binary;
                Expression right;
                if (op == Operator.Assignment)
                {
                    right = ParseExpression(op.Precedence - 1); // Right-associative
                    EnsureRightOperand(opSymbol, right);
                    var assign = new AssignmentExpression(op, left, right);
                    binary = assign;
                }
                else if (isCompound)
                {
                    right = ParseExpression(op.Precedence - 1);
                    EnsureRightOperand(opSymbol, right);
                    var compound = new CompoundExpression(op, left, right);
                    binary = compound;
                }
                else if (op == Operator.In)
                {
                    right = ParseExpression(op.Precedence);
                    EnsureRightOperand(opSymbol, right);
                    var inExp = new IncludedExpression(op, left, right);
                    binary = inExp;
                }
                else
                {
                    right = ParseExpression(op.Precedence);
                    EnsureRightOperand(opSymbol, right);
                    var bin = new BinaryExpression(op, left, right);
                    binary = bin;
                }

                SetRange(binary, left.Range, right.Range);

                if (binary is AssignmentExpression assignExp) return OptimizeAssignment(assignExp);
                if (binary is CompoundExpression compoundExp) return OptimizeCompoundAssignment(compoundExp);

                return binary;
            }

            // Postfix (++, --)
            if (op != null && op.Placement == OperatorPlacement.Postfix)
            {
                EnsureMutationTarget(left, op);
                var unary = new UnaryExpression(op, UnaryType.Post, left);
                // SetDebug(unary, opToken); // Redundant
                unary.Range = MergeRanges(left.Range, opRange);
                return unary;
            }

            var opToken = new OperatorToken { Symbol = opSymbol, Value = opSymbol?.Name, Range = opRange };
            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, opToken, "Unexpected operator " + opToken.Value);
        }

        private void EnsureRightOperand(Symbols opSymbol, Expression expression)
        {
            if (expression == null)
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, Lexer.FullPath, Lexer.LookAtHead(), $"Operator '{opSymbol.Name}' requires a right operand.");
            }
        }

        private Expression ParseStringTemplate(StringTemplateToken token)
        {
            var source = this.Lexer.InputData;
            var raw = source.AsSpan(token.Range.Offset + 1, token.Range.Length - 2);

            int i = 0;
            var sb = new StringBuilder();
            var parts = new List<TemplateStringPart>();
            var hasExpression = false;

            void AddStringPart()
            {
                if (sb.Length > 0)
                {
                    var str = sb.ToString();
                    sb.Clear();
                    parts.Add(new TemplateStringPart(str));
                }
            }

            while (i < raw.Length)
            {
                char c = raw[i];
                if (c == '\\')
                {
                    if (i + 1 < raw.Length)
                    {
                        char next = raw[i + 1];
                        switch (next)
                        {
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case '\\': sb.Append('\\'); break;
                            case '`': sb.Append('`'); break;
                            case '{': sb.Append('{'); break;
                            case '}': sb.Append('}'); break;
                            case '$': sb.Append('$'); break;
                            case '"': sb.Append('"'); break;
                            case '\'': sb.Append('\''); break;
                            default: sb.Append(next); break;
                        }
                        i += 2;
                        continue;
                    }
                    else
                    {
                        sb.Append('\\');
                        i++;
                        continue;
                    }
                }
                else if (c == '$' && i + 1 < raw.Length && raw[i + 1] == '{')
                {
                    AddStringPart();

                    // Found ${, need to find matching }
                    int exprStart = i + 2;
                    int braceCount = 1;
                    int exprEnd = -1;

                    for (int j = exprStart; j < raw.Length; j++)
                    {
                        if (raw[j] == '\\') { j++; continue; }
                        if (raw[j] == '{') braceCount++;
                        if (raw[j] == '}')
                        {
                            braceCount--;
                            if (braceCount == 0)
                            {
                                exprEnd = j;
                                break;
                            }
                        }
                    }

                    if (exprEnd == -1)
                    {
                        throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, token, "Template string interpolation missing closing brace '}'");
                    }

                    var expressionSource = raw.Slice(exprStart, exprEnd - exprStart);
                    if (!TryParseTemplateIdentifier(expressionSource, token.Range, out var expr))
                    {
                        string exprText = expressionSource.ToString();
                        var subLexer = new AuroraLexer(this.Lexer.BaseDirectory, new MemorySource(this.Lexer.BaseDirectory, this.Lexer.FullPath, exprText));
                        var subParser = new AuroraParser(subLexer, _options);
                        expr = subParser.ParseExpression(0);
                        if (expr == null)
                        {
                            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, token, "Template string interpolation requires an expression.");
                        }
                        if (!subLexer.IsAtEnd)
                        {
                            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, subLexer.LookAtHead(), "Template string interpolation contains unexpected tokens.");
                        }
                    }

                    if (expr != null)
                    {
                        parts.Add(new TemplateStringPart(expr));
                        hasExpression = true;
                    }

                    i = exprEnd + 1; // Move past }
                    continue;
                }

                sb.Append(c);
                i++;
            }

            AddStringPart();

            if (!hasExpression)
            {
                var text = parts.Count == 0 ? string.Empty : parts[0].Literal;
                return SetRange(new LiteralExpression(new StringToken() { Value = text }), token.Range, token.Range);
            }

            return SetRange(new TemplateStringExpression(parts), token.Range, token.Range);
        }

        // ===================================
        // Helpers
        // ===================================

        private static int GetPrecedence(Symbols symbol)
        {
            if (symbol != null)
            {
                // Is this token operating as Infix?
                // The boolean literal 'true' in FromSymbols(..., true) means "Has Left Operand"
                // In GetPrecedence, we are looking ahead, so we assume we are inside ParseExpression loop where we already have a Left.
                var op = Operator.FromSymbols(symbol, true);
                if (op != null)
                {
                    return op.Precedence;
                }
            }
            return 0;
        }

        private bool IsPrefixOperator(Symbols symbol)
        {
            var op = Operator.FromSymbols(symbol, false);
            return op != null && op.Placement == OperatorPlacement.Prefix;
        }

        private bool CanParsePrefixWithoutToken(Symbols symbol)
        {
            return symbol == Symbols.PT_LEFTPARENTHESIS ||
                   symbol == Symbols.PT_LEFTBRACKET ||
                   symbol == Symbols.PT_LEFTBRACE ||
                   symbol == Symbols.OP_SPREAD ||
                   IsPrefixOperator(symbol);
        }

        private bool IsInsideLoop()
        {
            for (int i = scopeStack.Count - 1; i >= 0; i--)
            {
                if (scopeStack[i] == ScopeType.FOR || scopeStack[i] == ScopeType.FUNCTION)
                {
                    return scopeStack[i] == ScopeType.FOR;
                }
            }
            return false;
        }

        private bool TryParseTemplateIdentifier(ReadOnlySpan<char> expressionSource, SourceSpan range, out Expression expression)
        {
            expression = null;
            var source = expressionSource.Trim();
            if (source.Length == 0 || !IsIdentifierStart(source[0]) || Symbols.FromSpan(source) != null)
            {
                return false;
            }

            for (int i = 1; i < source.Length; i++)
            {
                if (!IsIdentifierPart(source[i]))
                {
                    return false;
                }
            }

            var token = new IdentifierToken { Value = source.ToString(), Range = range };
            expression = SetRange(new NameExpression(token), range, range);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIdentifierStart(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                   c == '_' ||
                   c == '$' ||
                   (c >= 0x4e00 && c <= 0x9fbb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIdentifierPart(char c)
        {
            return IsIdentifierStart(c) || (c >= '0' && c <= '9');
        }

        private Statement ParseMetaInfoOrAnnotatedFunction()
        {
            var annotations = new List<FunctionAnnotation>();
            var annotation = ParseAnnotation();
            if (this.Lexer.TestNext(Symbols.PT_SEMICOLON))
            {
                if (annotation.Name.Value == "global")
                {
                    if (annotation.Arguments.Count != 0)
                    {
                        throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, annotation.Name, "@global() does not accept arguments.");
                    }

                    var globalStatement = SetDebug(new ModuleMetaStatement((IdentifierToken)annotation.Name, null), annotation.Name);
                    return globalStatement;
                }

                if (annotation.Arguments.Count != 1 || annotation.Arguments[0] is not IdentifierToken)
                {
                    throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, annotation.Name, "Module metadata requires exactly one identifier value.");
                }

                var statement = SetDebug(new ModuleMetaStatement((IdentifierToken)annotation.Name, annotation.Arguments[0]), annotation.Name);
                return statement;
            }

            annotations.Add(annotation);
            while (this.Lexer.PeekSymbol() == Symbols.PT_METAINFO)
            {
                annotations.Add(ParseAnnotation());
                if (this.Lexer.TestNext(Symbols.PT_SEMICOLON))
                {
                    throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, annotations[^1].Name, "Function annotations must not end with semicolon.");
                }
            }

            var symbol = this.Lexer.PeekSymbol();
            if (symbol == Symbols.KW_FUNCTION || symbol == Symbols.KW_FUNC)
            {
                return ParseFunctionDeclaration(MemberAccess.Internal, annotations);
            }

            if (symbol == Symbols.KW_EXPORT)
            {
                return ParseExportStatement(annotations);
            }

            var token = this.Lexer.LookAtHead();
            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, token, "Function annotations must be followed by a function declaration.");
        }

        private FunctionAnnotation ParseAnnotation()
        {
            this.Lexer.Expect(Symbols.PT_METAINFO);
            var name = this.Lexer.NextOfKind<IdentifierToken>();
            var arguments = new List<Token>();
            if (this.Lexer.TestNext(Symbols.PT_LEFTPARENTHESIS) &&
                !this.Lexer.TestNext(Symbols.PT_RIGHTPARENTHESIS))
            {
                while (true)
                {
                    arguments.Add(ParseAnnotationArgument());
                    if (this.Lexer.TestNext(Symbols.PT_RIGHTPARENTHESIS))
                    {
                        break;
                    }

                    this.Lexer.Expect(Symbols.PT_COMMA);
                }
            }

            var result = new FunctionAnnotation(name, arguments);
            return SetRange(result, name.Range, this.Lexer.PreviousRange(1));
        }

        private Token ParseAnnotationArgument()
        {
            Token token = this.Lexer.TestNextOfKind<IdentifierToken>();
            token ??= this.Lexer.TestNextOfKind<StringToken>();
            token ??= this.Lexer.TestNextOfKind<NumberToken>();
            token ??= this.Lexer.TestNextOfKind<BooleanToken>();

            if (token == null)
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, this.Lexer.LookAtHead(), "Annotation argument must be an identifier, string, number, or boolean literal.");
            }

            return token;
        }

        private Statement ParseBlock(bool isFunction = false)
        {
            var leftBrace = this.Lexer.NextRangeOfKind(Symbols.PT_LEFTBRACE);
            using (scopeStack.Scope(ScopeType.BLOCK))
            {
                var result = new BlockStatement();
                while (true)
                {
                    var symbol = this.Lexer.PeekSymbol();
                    if (symbol == Symbols.PT_RIGHTBRACE) break;
                    if (symbol == Symbols.KW_EOF) throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, this.Lexer.Previous(), "Unexpected end of file in block");

                    var exp = this.ParseStatement();
                    if (exp is FunctionDeclaration functionDeclaration)
                    {
                        result.AddFunction(functionDeclaration);
                        functionDeclaration.Parent = result;
                    }
                    else if (exp != null)
                    {
                        result.AddStatement(exp);
                    }
                }
                var rightBrace = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTBRACE);
                SetRange(result, leftBrace, rightBrace);

                if (isFunction)
                {
                    result.IsFunction = true;
                    return result;
                }
                return OptimizeStatement(result);
            }
        }
        private Statement ParseInclude()
        {
            var importRange = this.Lexer.NextRangeOfKind(Symbols.KW_INCLUDE);
            if (!this.Root.IsEmpty())
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, importRange, "The Include statement must be placed at the top of the module.");
            }
            StringToken fileToken = this.Lexer.NextOfKind<StringToken>();
            var closed = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);
            var import = new ImportDeclaration() { File = fileToken, Include = true };

            return SetRange(import, importRange, closed);
        }



        private Statement ParseImport()
        {
            var importRange = this.Lexer.NextRangeOfKind(Symbols.KW_IMPORT);
            if (!this.Root.IsEmpty())
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, importRange, "The Import statement must be placed at the top of the module.");
            }
            var module = this.Lexer.NextOfKind<IdentifierToken>();
            this.Lexer.Expect(Symbols.KW_FROM);
            StringToken fileToken = this.Lexer.NextOfKind<StringToken>();
            var closed = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);
            var import = new ImportDeclaration() { Name = module, File = fileToken, Include = false };

            return SetRange(import, importRange, closed);
        }

        private Statement ParseExportStatement(IReadOnlyList<FunctionAnnotation> annotations = null)
        {
            var exportRange = this.Lexer.NextRangeOfKind(Symbols.KW_EXPORT);

            if (scopeStack.Current != ScopeType.MODULE)
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, exportRange, $"Invalid export keyword in row {exportRange.StartLine}, column {exportRange.StartColumn}, scope not supported.");
            }

            var symbol = this.Lexer.PeekSymbol();
            if (symbol == Symbols.KW_FUNCTION || symbol == Symbols.KW_FUNC)
            {
                return ParseFunctionDeclaration(MemberAccess.Export, annotations);
            }
            if (annotations != null && annotations.Count > 0)
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, exportRange, "Function annotations can only be applied to function declarations.");
            }
            else if (symbol == Symbols.KW_VAR)
            {
                return ParseVariableDeclaration(MemberAccess.Export);
            }
            else if (symbol == Symbols.KW_CONST)
            {
                return ParseVariableDeclaration(MemberAccess.Export);
            }
            else if (symbol == Symbols.KW_DECLARE)
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, exportRange, "export declare is not supported. Use declare inside an @global() file.");
            }
            else if (symbol == Symbols.KW_ENUM)
            {
                return ParseEnumDeclaration(MemberAccess.Export);
            }

            var token = this.Lexer.LookAtHead();
            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, token, "Invalid keywords appear in export declaration.");
        }

        private Statement ParseFunctionDeclaration(MemberAccess access = MemberAccess.Internal, IReadOnlyList<FunctionAnnotation> annotations = null)
        {
            var start = this.Lexer.NextRangeOfKind(Symbols.KW_FUNCTION, Symbols.KW_FUNC);
            var functionName = this.Lexer.NextOfKind<IdentifierToken>();
            var func = this.ParseFunction(functionName, access, FunctionFlags.General, annotations);
            return SetRange(func, start, func.Range);
        }

        private FunctionDeclaration ParseFunction(
            IdentifierToken functionName,
            MemberAccess access = MemberAccess.Internal,
            FunctionFlags flags = FunctionFlags.General,
            IReadOnlyList<FunctionAnnotation> annotations = null)
        {
            var leftParenRange = this.Lexer.NextRangeOfKind(Symbols.PT_LEFTPARENTHESIS);
            var arguments = this.ParseFunctionArguments();
            // ParseFunctionArguments consumes the )

            if (flags == FunctionFlags.Lambda)
            {
                this.Lexer.Expect(Symbols.PT_LAMBDA);
            }

            using (scopeStack.Scope(ScopeType.FUNCTION))
            {
                var body = this.ParseBlock();
                if (!(body is BlockStatement))
                {
                    var newBody = new BlockStatement();
                    newBody.AddStatement(body);
                    body = newBody;
                }
                ((BlockStatement)body).IsFunction = true;
                var declaration = new FunctionDeclaration(access, functionName, arguments, body, flags, annotations);
                return SetRange(declaration, (functionName?.Range ?? leftParenRange), body.Range);
            }
        }

        private Statement ParseDeclare(MemberAccess access = MemberAccess.Internal)
        {
            var start = this.Lexer.NextRangeOfKind(Symbols.KW_DECLARE);
            if (!Root.IsGlobalDeclarationFile)
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, start, "declare is only allowed inside @global() declaration files.");
            }

            if (this.Lexer.TestNext(Symbols.KW_FUNCTION) || this.Lexer.TestNext(Symbols.KW_FUNC))
            {
                var funcName = this.Lexer.NextOfKind<IdentifierToken>();
                this.Lexer.Expect(Symbols.PT_LEFTPARENTHESIS);
                var arguments = this.ParseFunctionArguments();
                // ParseFunctionArguments consumes the )
                var semiRange = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);
                var declaration = new FunctionDeclaration(access, funcName, arguments, null, FunctionFlags.Declare);
                return SetRange(declaration, funcName.Range, semiRange);
            }
            if (this.Lexer.TestSymbol(Symbols.KW_VAR) || this.Lexer.TestSymbol(Symbols.KW_CONST))
            {
                var declaration = ParseVariableDeclaration(access);
                if (declaration is VariableDeclaration variable && variable.Name != null && variable.Initializer == null && variable.Pattern == null)
                {
                    variable.IsDeclare = true;
                    return SetRange(variable, start, variable.Range);
                }

                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, declaration.Range, "Declare variables must use a single external variable name without an initializer.");
            }

            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, this.Lexer.LookAtHead(), "The Declare keyword only allows the declaration of external methods or variables");
        }

        private Expression ParseObjectDestructuringPattern()
        {
            var token = this.Lexer.NextRangeOfKind(Symbols.PT_LEFTBRACE);
            var pattern = new ObjectDestructuringPattern();

            while (true)
            {
                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE)) break;

                var propName = this.Lexer.NextOfKind<IdentifierToken>();
                pattern.Properties.Add(propName);

                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE)) break;
                this.Lexer.Expect(Symbols.PT_COMMA);
            }
            var rightBrace = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTBRACE);

            return SetRange(pattern, token, rightBrace);
        }

        private Expression ParseArrayDestructuringPattern()
        {
            var token = this.Lexer.NextRangeOfKind(Symbols.PT_LEFTBRACKET);
            var pattern = new ArrayDestructuringPattern();

            while (true)
            {
                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACKET)) break;

                // Check for spread operator
                if (this.Lexer.TestNext(Symbols.OP_SPREAD))
                {
                    var restName = this.Lexer.NextOfKind<IdentifierToken>();
                    var spread = new SpreadExpression(new NameExpression(restName));
                    pattern.Elements.Add(SetRange(spread, spread.Range, restName.Range));
                    if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACKET)) break;
                    if (this.Lexer.TestNext(Symbols.PT_COMMA)) continue;
                }

                var elemName = this.Lexer.NextOfKind<IdentifierToken>();
                pattern.Elements.Add(new NameExpression(elemName));

                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACKET)) break;
                this.Lexer.Expect(Symbols.PT_COMMA);
            }
            var rightBracket = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTBRACKET);

            return SetRange(pattern, token, rightBracket);
        }

        private Statement ParseVariableDeclaration(MemberAccess access = MemberAccess.Internal)
        {
            var isConst = false;
            SourceSpan start;
            if (this.Lexer.TestSymbol(Symbols.KW_CONST))
            {
                start = this.Lexer.NextRangeOfKind(Symbols.KW_CONST);
                isConst = true;
            }
            else if (this.Lexer.TestSymbol(Symbols.KW_VAR)) start = this.Lexer.NextRangeOfKind(Symbols.KW_VAR);
            else throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, this.Lexer.LookAtHead(), "Variable declaration should be placed after var/const");

            // Check if this is a destructuring pattern
            var nextSymbol = this.Lexer.PeekSymbol();
            if (nextSymbol == Symbols.PT_LEFTBRACE)
            {
                // Object destructuring: var { a, b } = expr;
                var pattern = ParseObjectDestructuringPattern();
                this.Lexer.Expect(Symbols.OP_ASSIGNMENT);
                var init = ParseRequiredExpression("Object destructuring initializer");
                var semi = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);
                var varDecl = new VariableDeclaration(access, isConst, pattern, init);
                return SetRange(varDecl, start, semi);
            }
            else if (nextSymbol == Symbols.PT_LEFTBRACKET)
            {
                // Array destructuring: var [ a, b, ..c ] = expr;
                var pattern = ParseArrayDestructuringPattern();
                this.Lexer.Expect(Symbols.OP_ASSIGNMENT);
                var init = ParseRequiredExpression("Array destructuring initializer");
                var semi = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);
                var varDecl = new VariableDeclaration(access, isConst, pattern, init);
                return SetRange(varDecl, start, semi);
            }

            // Simple identifier logic (no commas allowed, multiple variables not supported by current AST)
            Token varName = this.Lexer.NextOfKind<IdentifierToken>();
            Expression initializer = null;

            if (this.Lexer.TestNext(Symbols.OP_ASSIGNMENT))
            {
                initializer = ParseRequiredExpression("Variable initializer");
            }

            var semiRange = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);

            var variable = new VariableDeclaration(access, isConst, varName, initializer);
            return SetRange(variable, start, semiRange);
        }

        private Expression ParseRequiredExpression(string context)
        {
            var expression = ParseExpression(0);
            if (expression != null)
            {
                return expression;
            }

            var token = Lexer.LookAtHead();
            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, Lexer.FullPath, token, $"{context} requires an expression.");
        }

        private Statement ParseEnumDeclaration(MemberAccess access)
        {
            var start = this.Lexer.NextRangeOfKind(Symbols.KW_ENUM);
            var enumName = this.Lexer.NextOfKind<IdentifierToken>();
            var elements = this.ParseEnumBody();
            var enumDecl = new EnumDeclaration() { Elements = elements, Identifier = enumName, Access = access };
            return SetRange(enumDecl, start, this.Lexer.PreviousRange(1));
        }

        private List<EnumElement> ParseEnumBody()
        {
            this.Lexer.Expect(Symbols.PT_LEFTBRACE);
            var result = new List<EnumElement>(4);
            var elementValue = 0;
            while (true)
            {
                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE)) break;
                var elementName = this.Lexer.NextOfKind<IdentifierToken>();
                if (this.Lexer.TestNext(Symbols.OP_ASSIGNMENT))
                {
                    var token = this.Lexer.NextOfKind<ValueToken>();
                    if (token is not NumberToken numberToken) throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, token, "Enumeration types only apply to integers");
                    if (numberToken.NumberValue % 1 != 0 ||
                        numberToken.NumberValue < int.MinValue ||
                        numberToken.NumberValue > int.MaxValue)
                    {
                        throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, token, "Enumeration values must be 32-bit integers.");
                    }
                    elementValue = (int)numberToken.NumberValue;
                }
                var enumElement = new EnumElement() { Name = elementName, Value = elementValue };
                result.Add(enumElement);
                elementValue++;
                this.Lexer.TestNext(Symbols.PT_COMMA);
            }
            this.Lexer.Expect(Symbols.PT_RIGHTBRACE);
            return result;
        }

        // Helpers Needed
        private Statement OptimizeStatement(Statement statement)
        {
            return statement;
        }

        private Statement ParseForBlock()
        {
            using (scopeStack.Scope(ScopeType.FOR))
            {
                var forRange = this.Lexer.NextRangeOfKind(Symbols.KW_FOR);
                this.Lexer.Expect(Symbols.PT_LEFTPARENTHESIS);
                Statement body = null;
                // Branch 1: `for (var x ...)`
                if (this.Lexer.TestSymbol(Symbols.KW_VAR))
                {

                    var snapshot = this.Lexer.CreateSnapshot();
                    var start = this.Lexer.NextRangeOfKind(Symbols.KW_VAR);
                    var idToken = this.Lexer.NextOfKind<IdentifierToken>();
                    if (this.Lexer.TestNext(Symbols.OP_IN))
                    {
                        // Case: `for (var x in y)`
                        this.Lexer.RestoreSnapshot(snapshot);
                        this.Lexer.Expect(Symbols.KW_VAR);
                        var varName = this.Lexer.NextOfKind<IdentifierToken>();
                        this.Lexer.Expect(Symbols.OP_IN);
                        var right = ParseExpression(0);
                        if (right == null)
                        {
                            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, this.Lexer.LookAtHead(), "for-in expression requires a collection.");
                        }
                        this.Lexer.Expect(Symbols.PT_RIGHTPARENTHESIS);

                        var variable = new VariableDeclaration(MemberAccess.Internal, false, varName, null);
                        var inExp = new InExpression(Operator.In, new NameExpression(varName), right);

                        inExp = SetRange(inExp, varName.Range, right.Range);


                        body = ParseStatement();
                        if (body == null)
                        {
                            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, forRange, "for body statement should not be empty");
                        }
                        var forStmt = new ForInStatement(SetRange(variable, start, varName.Range), inExp, body);
                        return SetRange(forStmt, forRange, (body?.Range ?? right.Range));
                    }
                    else
                    {
                        // Case: `for (var x = 0; ...)`
                        this.Lexer.RestoreSnapshot(snapshot);
                        var initializer = ParseStatement(); // Parses `var x = 0;`

                        var condition = ParseExpression(0);
                        if (condition == null)
                        {
                            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, this.Lexer.LookAtHead(), "for condition requires an expression.");
                        }
                        this.Lexer.Expect(Symbols.PT_SEMICOLON);

                        var increment = ParseExpression(0);
                        this.Lexer.Expect(Symbols.PT_RIGHTPARENTHESIS);

                        body = ParseStatement();
                        if (body == null)
                        {
                            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, forRange, "for body statement should not be empty");
                        }
                        var forStmt = new ForStatement(condition, initializer, increment, body);
                        return SetRange(forStmt, forRange, (body?.Range ?? increment.Range));
                    }
                }

                var startRange = forRange;
                // Branch 2: `for (x ...)`
                var exp = ParseExpression(0);
                if (exp is InExpression inExpr)
                {
                    // Case: `for (x in y)`
                    this.Lexer.Expect(Symbols.PT_RIGHTPARENTHESIS);
                    body = ParseStatement();
                    if (body == null)
                    {
                        throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, startRange, "for body statement should not be empty");
                    }
                    var forStmt = new ForInStatement(null, inExpr, body);
                    return SetRange(forStmt, startRange, (body?.Range ?? inExpr.Range));
                }

                // Case: `for (x = 0; ...)`
                this.Lexer.Expect(Symbols.PT_SEMICOLON);
                var condExpr = ParseExpression(0);
                if (condExpr == null)
                {
                    throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, this.Lexer.LookAtHead(), "for condition requires an expression.");
                }
                this.Lexer.Expect(Symbols.PT_SEMICOLON);
                var incExpr = ParseExpression(0);
                this.Lexer.Expect(Symbols.PT_RIGHTPARENTHESIS);

                body = ParseStatement();
                if (body == null)
                {
                    throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, startRange, "for body statement should not be empty");
                }
                var forStmtLoop = new ForStatement(condExpr, exp, incExpr, body);
                return SetRange(forStmtLoop, startRange, (body?.Range ?? incExpr.Range));
            }
        }

        private Statement ParseWhileBlock()
        {
            using (scopeStack.Scope(ScopeType.FOR))
            {
                var range = this.Lexer.NextRangeOfKind(Symbols.KW_WHILE);
                this.Lexer.Expect(Symbols.PT_LEFTPARENTHESIS);
                var condition = this.ParseExpression(0);
                if (condition == null)
                {
                    throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, range, "while condition requires an expression.");
                }
                this.Lexer.Expect(Symbols.PT_RIGHTPARENTHESIS);
                var body = this.ParseStatement();
                if (body == null) throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, range, "while body statement should not be empty");
                return SetRange(new WhileStatement(condition, body), range, body.Range);
            }
        }



        private Statement ParseIfBlock()
        {
            var range = this.Lexer.NextRangeOfKind(Symbols.KW_IF);
            this.Lexer.Expect(Symbols.PT_LEFTPARENTHESIS);
            var condition = this.ParseExpression(0);
            if (condition == null)
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, range, "if condition requires an expression.");
            }
            this.Lexer.Expect(Symbols.PT_RIGHTPARENTHESIS);

            var body = this.ParseStatement();
            if (body == null) throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, range, "if body statement should not be empty");

            Statement elseStatement = null;
            if (this.Lexer.TestSymbol(Symbols.KW_ELSE))
            {
                elseStatement = this.ParseElseBlock();
            }

            return SetRange(new IfStatement(condition, body, elseStatement), range, (elseStatement ?? body).Range);
        }

        private Statement ParseElseBlock()
        {
            this.Lexer.Expect(Symbols.KW_ELSE);
            // If next is IF, standard parse.
            if (this.Lexer.TestSymbol(Symbols.KW_IF))
            {
                var block = new BlockStatement();
                block.AddStatement(ParseIfBlock());
                return OptimizeStatement(block);
            }
            else
            {
                var block = new BlockStatement();
                var body = this.ParseStatement();
                if (body == null) throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, this.Lexer.Previous(), "else body statement should not be empty");
                block.AddStatement(body);
                return OptimizeStatement(block);
            }
        }

        private TryStatement ParseTryStatement()
        {
            var range = this.Lexer.NextRangeOfKind(Symbols.KW_TRY);

            var body = this.ParseBlock();
            string catchVar = null;
            Statement catchBody = null;
            Statement finallyBody = null;

            if (this.Lexer.TestSymbol(Symbols.KW_CATCH))
            {
                this.Lexer.Expect(Symbols.KW_CATCH);
                if (this.Lexer.TestSymbol(Symbols.PT_LEFTPARENTHESIS))
                {
                    this.Lexer.Expect(Symbols.PT_LEFTPARENTHESIS);
                    var catchToken = this.Lexer.NextOfKind<IdentifierToken>();
                    catchVar = catchToken.Value;
                    this.Lexer.Expect(Symbols.PT_RIGHTPARENTHESIS);
                }
                catchBody = this.ParseBlock();
            }

            if (this.Lexer.TestSymbol(Symbols.KW_FINALLY))
            {
                this.Lexer.Expect(Symbols.KW_FINALLY);
                finallyBody = this.ParseBlock();
            }

            return SetRange(new TryStatement(body, catchVar, catchBody, finallyBody), range, (finallyBody ?? catchBody ?? body).Range);
        }

        private ThrowStatement ParseThrowStatement()
        {
            var range = this.Lexer.NextRangeOfKind(Symbols.KW_THROW);

            var exp = this.ParseExpression(0);
            if (exp == null)
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, range, "throw requires an expression.");
            }
            var semi = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);

            return SetRange(new ThrowStatement(exp), range, semi);
        }

        private Statement ParseContinueStatement()
        {
            var range = this.Lexer.NextRangeOfKind(Symbols.KW_CONTINUE);
            if (!IsInsideLoop())
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, range, "continue statement must be inside a loop.");
            }
            var semi = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);
            return SetRange(new ContinueStatement(), range, semi);
        }


        private Statement ParseBreakStatement()
        {
            var range = this.Lexer.NextRangeOfKind(Symbols.KW_BREAK);
            if (!IsInsideLoop())
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, range, "break statement must be inside a loop.");
            }
            var semi = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);
            return SetRange(new BreakStatement(), range, semi);
        }


        private Statement ParseDebuggerStatement()
        {
            var range = this.Lexer.NextRangeOfKind(Symbols.KW_DEBUGGER);
            var semi = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);
            return SetRange(new DebuggerStatement(), range, semi);
        }




        private Statement ParseReturnStatement()
        {
            var range = this.Lexer.NextRangeOfKind(Symbols.KW_RETURN);
            // Check if next is ; (void return)
            if (this.Lexer.TestSymbol(Symbols.PT_SEMICOLON))
            {
                var semi = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);
                return SetRange(new ReturnStatement(null), range, semi);
            }
            var exp = this.ParseExpression(0);
            var endSemi = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);
            return SetRange(new ReturnStatement(exp), range, endSemi);
        }

        private Statement ParseDeleteStatement()
        {
            var range = this.Lexer.NextRangeOfKind(Symbols.KW_DELETE);
            var exp = this.ParseExpression(0);
            if (exp == null)
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, range, "delete requires an expression.");
            }
            var semi = this.Lexer.NextRangeOfKind(Symbols.PT_SEMICOLON);
            return SetRange(new DeleteStatement(exp), range, semi);
        }
        private Expression ParseObjectConstructor()
        {
            return ParseObjectConstructor(this.Lexer.NextRangeOfKind(Symbols.PT_LEFTBRACE));
        }

        private Expression ParseObjectConstructor(SourceSpan token)
        {
            var constructExpression = new MapExpression(Operator.ObjectLiteral);
            while (true)
            {
                if (this.Lexer.IsAtEnd) throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, this.Lexer.LookAtHead(), "Unexpected end of file in object constructor");
                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE)) break;

                // Spread ...
                if (this.Lexer.TestNext(Symbols.OP_SPREAD))
                {
                    var value = ParseExpression(0);
                    if (value == null)
                    {
                        throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, this.Lexer.LookAtHead(), "Spread requires an expression.");
                    }
                    var spread = new SpreadExpression(value);
                    constructExpression.AddEntry(spread);
                    this.Lexer.TestNext(Symbols.PT_COMMA);
                    continue;
                }

                Token varName = this.Lexer.TestNextOfKind<IdentifierToken>();
                if (varName == null) varName = this.Lexer.TestNextOfKind<StringToken>();
                if (varName == null) varName = this.Lexer.TestNextOfKind<NumberToken>();
                if (varName == null) varName = this.Lexer.TestNextOfKind<BooleanToken>();
                if (varName == null) varName = this.Lexer.TestNextOfKind<NullToken>();

                if (varName == null) throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, this.Lexer.Next(), "Invalid Map construction syntax");

                if (this.Lexer.TestNext(Symbols.PT_COLON))
                {
                    var value = ParseExpression(0);
                    if (value == null)
                    {
                        throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, varName, "Object property value requires an expression.");
                    }
                    var newExp = new MapKeyValueExpression(varName, value);
                    SetRange(newExp, varName.Range, value.Range);
                    constructExpression.AddEntry(newExp);
                }
                else
                {
                    // Shorthand { x } -> { x: x }
                    var nameToken = new NameExpression(varName);
                    SetRange(nameToken, varName.Range, varName.Range);
                    var kv = new MapKeyValueExpression(varName, nameToken);
                    SetRange(kv, varName.Range, varName.Range);
                    constructExpression.AddEntry(kv);
                }

                if (this.Lexer.TestNext(Symbols.PT_COMMA))
                {
                    if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE)) break;
                }
            }
            var rightBrace = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTBRACE);
            return SetRange(constructExpression, token, rightBrace);
        }

        // =================================================================================
        // Native TDoc literal syntax
        // =================================================================================

        private Expression ParseTDocValue()
        {
            var start = this.Lexer.PeekRange();
            string typeName = null;
            Token typeToken = null;

            // A leading identifier followed by a value-shaped token is the
            // optional static type. Property names are parsed by the object
            // member parser before reaching this method, so this rule is
            // unambiguous for values.
            var first = this.Lexer.LookAtHead();
            if (first is IdentifierToken && IsTDocTypePrefix())
            {
                typeToken = this.Lexer.Next();
                typeName = typeToken.Value;
                start = first.Range;
            }

            bool interpolation = false;
            Expression value;
            if (IsTDocInterpolationStart())
            {
                if (!_allowTDocInterpolation)
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        this.Lexer.LookAtHead(),
                        "Standalone TDoc documents do not support $(expression); use a literal value.");
                }

                interpolation = true;
                value = ParseTDocInterpolation();
            }
            else
            {
                value = ParseTDocRawValue();
            }

            ValidateTDocType(typeName, value, first, interpolation);
            var result = new TypedDocumentExpression(value, typeName, interpolation, typeToken);
            return SetRange(result, start, value.Range);
        }

        private Expression ParseTDocRawValue()
        {
            var token = this.Lexer.LookAtHead();
            if (token.Symbol == Symbols.PT_LEFTBRACE)
            {
                return ParseTDocObject(this.Lexer.NextRangeOfKind(Symbols.PT_LEFTBRACE));
            }
            if (token.Symbol == Symbols.PT_LEFTBRACKET)
            {
                return ParseTDocArray(this.Lexer.NextRangeOfKind(Symbols.PT_LEFTBRACKET));
            }
            if (token is ValueToken valueToken)
            {
                if (valueToken.Type == Tokens.ValueType.StringTemplate)
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        token,
                        "TDoc string values must be quoted literals or $(expression).");
                }
                this.Lexer.Next();
                return SetRange(new LiteralExpression(valueToken), valueToken.Range, valueToken.Range);
            }

            // TDoc numbers are still AuroraScript Number values, including a
            // leading minus. Do not admit arbitrary unary expressions here.
            if (token.Symbol == Symbols.OP_SUBTRACT && PeekToken(1) is NumberToken number)
            {
                var minus = this.Lexer.Next();
                this.Lexer.Next();
                var literal = SetRange(new LiteralExpression(number), number.Range, number.Range);
                var unary = new UnaryExpression(Operator.Negate, UnaryType.Prefix, literal);
                return SetRange(unary, minus.Range, number.Range);
            }

            throw new AuroraCompilationException(
                AuroraCompilationStage.Parsing,
                this.Lexer.FullPath,
                token,
                "TDoc values must be literals, arrays, objects, or $(expression).");
        }

        private Expression ParseTDocInterpolation()
        {
            var dollar = this.Lexer.Next();
            this.Lexer.Expect(Symbols.PT_LEFTPARENTHESIS);
            var expression = ParseExpression(0);
            if (expression == null)
            {
                throw new AuroraCompilationException(
                    AuroraCompilationStage.Parsing,
                    this.Lexer.FullPath,
                    this.Lexer.LookAtHead(),
                    "$(...) requires an AuroraScript expression.");
            }
            var right = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTPARENTHESIS);
            return SetRange(expression, dollar.Range, right);
        }

        private Expression ParseTDocArray(SourceSpan start)
        {
            var array = new ArrayLiteralExpression();
            while (!this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACKET))
            {
                if (this.Lexer.IsAtEnd)
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        this.Lexer.LookAtHead(),
                        "Unexpected end of file in TDoc array.");
                }

                PushTDocIndex(array.Elements.Count);
                try
                {
                    array.AddElement(ParseTDocValue());
                }
                finally
                {
                    PopTDocPath();
                }
                if (this.Lexer.TestNext(Symbols.PT_COMMA))
                {
                    if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACKET)) break;
                    continue;
                }
                if (!this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACKET))
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        this.Lexer.LookAtHead(),
                        "TDoc array elements require a comma.");
                }
            }

            var end = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTBRACKET);
            return SetRange(array, start, end);
        }

        private Expression ParseTDocObject(SourceSpan start)
        {
            var map = new MapExpression(Operator.ObjectLiteral);
            var names = new HashSet<string>(StringComparer.Ordinal);

            while (!this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE))
            {
                if (this.Lexer.IsAtEnd)
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        this.Lexer.LookAtHead(),
                        "Unexpected end of file in TDoc object.");
                }

                var readOnly = false;
                Token readOnlyToken = null;
                var first = this.Lexer.LookAtHead();
                if (IsIdentifier(first, "readonly"))
                {
                    readOnly = true;
                    readOnlyToken = this.Lexer.Next();
                    first = this.Lexer.LookAtHead();
                }

                if (!IsTDocPropertyName(first))
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        first,
                        "TDoc object property names must be identifiers or quoted strings.");
                }

                var typeName = (string)null;
                Token typeToken = null;
                var key = this.Lexer.Next();
                var next = this.Lexer.LookAtHead();
                if (key is IdentifierToken &&
                    IsTDocPropertyName(next) &&
                    ((next is IdentifierToken && !IsIdentifier(next, "$")) ||
                        (next is StringToken && IsTDocValueStart(PeekToken(1)))))
                {
                    // Two adjacent identifier positions mean type + property
                    // name. This is intentionally positional and does not
                    // consult a runtime registry during parsing.
                    typeToken = key;
                    typeName = key.Value;
                    key = this.Lexer.Next();
                }

                var keyName = key.Value ?? string.Empty;
                if (!names.Add(keyName))
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        key,
                        $"Duplicate TDoc property '{keyName}'.");
                }

                if (this.Lexer.TestNext(Symbols.PT_COLON))
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        key,
                        "TDoc object members use 'name value' syntax; ':' is not supported.");
                }

                PushTDocProperty(keyName);
                Expression value;
                try
                {
                    value = ParseTDocValue();

                    // Keep the member path active while validating an
                    // explicitly typed property.  Besides making nested
                    // array diagnostics point at the right member, this
                    // also keeps errors raised by Date/packed-array checks
                    // consistent with the runtime binder.
                    if (typeName != null)
                    {
                        var originalValue = value;
                        var originalRange = value.Range;
                        var isInterpolation = originalValue is TypedDocumentExpression interpolation && interpolation.IsInterpolation;
                        ValidateTDocType(typeName, UnwrapTDocValue(originalValue), key, isInterpolation);
                        value = new TypedDocumentExpression(UnwrapTDocValue(originalValue), typeName, isInterpolation, typeToken);
                        SetRange(value, key.Range, originalRange);
                    }
                }
                finally
                {
                    PopTDocPath();
                }

                var entry = new MapKeyValueExpression(key, value, readOnly, readOnlyToken);
                SetRange(entry, key.Range, value.Range);
                map.AddEntry(entry);

                if (this.Lexer.TestNext(Symbols.PT_COMMA))
                {
                    if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE)) break;
                    continue;
                }
                if (!this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE))
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        this.Lexer.LookAtHead(),
                        "TDoc object members require a comma.");
                }
            }

            var end = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTBRACE);
            return SetRange(map, start, end);
        }

        private void ValidateTDocType(string typeName, Expression value, Token token, bool interpolation = false)
        {
            if (string.IsNullOrEmpty(typeName) || value == null) return;
            var raw = UnwrapTDocValue(value);
            // Runtime expressions are deliberately deferred to TypedDocumentBinder;
            // only literal values are checked here.  This keeps compile-time
            // diagnostics useful without weakening the runtime contract.
            if (interpolation || value is TypedDocumentExpression { IsInterpolation: true })
            {
                return;
            }

            var valid = typeName switch
            {
                "Null" => raw is LiteralExpression { Token: NullToken },
                "Object" => raw is MapExpression,
                "Array" => raw is ArrayLiteralExpression,
                "Int32Array" or "Int8Array" or "Float64Array" or "BooleanArray" or
                    "UInt8Array" or "Int16Array" or "UInt16Array" or "UInt32Array" or
                    "Int64Array" or "UInt64Array" => ValidateTDocPackedArray(typeName, raw, token),
                "String" => IsTDocScalar(raw, typeof(StringToken)),
                "Number" => IsTDocNumber(raw) && IsTDocFiniteNumber(raw),
                "Boolean" => IsTDocScalar(raw, typeof(BooleanToken)),
                "StringBuffer" or "Path" => IsTDocScalar(raw, typeof(StringToken)),
                "Date" => ValidateTDocDate(raw),
                "Regex" => raw is MapExpression,
                "HashMap" => raw is ArrayLiteralExpression,
                _ => true // A host-registered alias is resolved by the backend.
            };
            if (!valid)
            {
                throw new AuroraCompilationException(
                    AuroraCompilationStage.Parsing,
                    this.Lexer.FullPath,
                    token,
                    TDocPathMessage($"TDoc type '{typeName}' does not accept this value shape."));
            }
        }

        private bool ValidateTDocPackedArray(string typeName, Expression raw, Token token)
        {
            if (raw is not ArrayLiteralExpression array)
            {
                return false;
            }

            for (var index = 0; index < array.Elements.Count; index++)
            {
                var element = array.Elements[index];
                if (element is TypedDocumentExpression { IsInterpolation: true })
                {
                    continue;
                }

                var value = UnwrapTDocValue(element);
                if (typeName == "BooleanArray")
                {
                    if (value is LiteralExpression { Token: BooleanToken }) continue;
                    if (TryGetTDocNumber(value, out var booleanNumber) &&
                        double.IsFinite(booleanNumber) &&
                        (booleanNumber == 0d || booleanNumber == 1d))
                    {
                        continue;
                    }
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        value.Range,
                        TDocPathMessage("BooleanArray elements must be true, false, 0, or 1.", index));
                }

                if (!TryGetTDocNumber(value, out var number) || !double.IsFinite(number))
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        value.Range,
                        TDocPathMessage($"{typeName} elements must be finite numbers.", index));
                }
                if (typeName != "Float64Array" && Math.Truncate(number) != number)
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        value.Range,
                        TDocPathMessage($"{typeName} elements must be integers.", index));
                }

                if (!IsTDocPackedRange(typeName, value, number))
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        value.Range,
                        TDocPathMessage($"{typeName} element is outside its supported range.", index));
                }
            }
            return true;
        }

        private static bool IsTDocPackedRange(string typeName, Expression value, double number)
        {
            if (typeName == "Int64Array")
            {
                return TryGetExactTDocInt64(value, out _);
            }
            if (typeName == "UInt64Array")
            {
                return TryGetExactTDocUInt64(value, out _);
            }
            return !TypedDocumentBinder.TryGetPackedKind(typeName, out var kind) ||
                TypedDocumentBinder.IsPackedRange(kind, number);
        }

        private static bool TryGetExactTDocInt64(Expression value, out long result)
        {
            result = 0;
            if (!TryGetTDocNumberToken(value, out var token, out var negative)) return false;
            var number = negative ? -token.NumberValue : token.NumberValue;
            if (double.IsFinite(number) && Math.Truncate(number) == number &&
                number >= -9007199254740991d && number <= 9007199254740991d)
            {
                result = (long)number;
                return true;
            }

            // NumberToken.NumberValue is a double and may already have rounded a
            // valid 64-bit literal.  Only wide values need their original
            // spelling to be parsed exactly.
            return TryParseExactTDocInt64(token.Value.AsSpan(), negative, out result);
        }

        private static bool TryGetExactTDocUInt64(Expression value, out ulong result)
        {
            result = 0;
            if (!TryGetTDocNumberToken(value, out var token, out var negative) || negative)
            {
                return false;
            }

            var number = token.NumberValue;
            if (double.IsFinite(number) && Math.Truncate(number) == number &&
                number >= 0d && number <= 9007199254740991d)
            {
                result = (ulong)number;
                return true;
            }

            return TryParseExactTDocUInt64(token.Value.AsSpan(), out result);
        }

        private static bool TryParseExactTDocInt64(
            ReadOnlySpan<char> source,
            bool negative,
            out long result)
        {
            if (!negative && source.IndexOf('_') < 0)
            {
                return TypedDocumentScanner.TryParseInt64Exact(source, out result);
            }

            var capacity = source.Length + (negative ? 1 : 0);
            char[] rented = null;
            Span<char> clean = capacity <= 128
                ? stackalloc char[capacity]
                : (rented = ArrayPool<char>.Shared.Rent(capacity));
            var length = 0;
            if (negative) clean[length++] = '-';
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i] != '_') clean[length++] = source[i];
            }
            var parsed = TypedDocumentScanner.TryParseInt64Exact(clean[..length], out result);
            if (rented != null) ArrayPool<char>.Shared.Return(rented);
            return parsed;
        }

        private static bool TryParseExactTDocUInt64(ReadOnlySpan<char> source, out ulong result)
        {
            if (source.IndexOf('_') < 0)
            {
                return TypedDocumentScanner.TryParseUInt64Exact(source, out result);
            }

            var capacity = source.Length;
            char[] rented = null;
            Span<char> clean = capacity <= 128
                ? stackalloc char[capacity]
                : (rented = ArrayPool<char>.Shared.Rent(capacity));
            var length = 0;
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i] != '_') clean[length++] = source[i];
            }
            var parsed = TypedDocumentScanner.TryParseUInt64Exact(clean[..length], out result);
            if (rented != null) ArrayPool<char>.Shared.Return(rented);
            return parsed;
        }

        private static bool TryGetTDocNumberToken(
            Expression value,
            out NumberToken token,
            out bool negative)
        {
            value = UnwrapTDocValue(value);
            if (value is LiteralExpression { Token: NumberToken number })
            {
                token = number;
                negative = false;
                return true;
            }
            if (value is UnaryExpression unary && unary.Operator == Operator.Negate &&
                unary.Expression is LiteralExpression { Token: NumberToken numberToken })
            {
                token = numberToken;
                negative = true;
                return true;
            }
            token = null;
            negative = false;
            return false;
        }

        private static bool TryGetTDocNumber(Expression value, out double number)
        {
            if (TryGetTDocNumberToken(value, out var token, out var negative))
            {
                number = negative ? -token.NumberValue : token.NumberValue;
                return true;
            }
            number = 0d;
            return false;
        }

        private static bool IsTDocFiniteNumber(Expression value)
        {
            return TryGetTDocNumber(value, out var number) && double.IsFinite(number);
        }

        private bool ValidateTDocDate(Expression raw)
        {
            if (TryGetTDocNumber(raw, out var number))
            {
                var valid = double.IsFinite(number) && Math.Truncate(number) == number &&
                    number >= DateTimeOffset.MinValue.Ticks &&
                    number <= DateTimeOffset.MaxValue.Ticks &&
                    TryGetExactTDocInt64(raw, out var ticks) &&
                    ticks >= DateTimeOffset.MinValue.Ticks && ticks <= DateTimeOffset.MaxValue.Ticks;
                if (!valid)
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Parsing,
                        this.Lexer.FullPath,
                        raw.Range,
                        TDocPathMessage($"Date ticks must be an exactly representable integer in the range 0..{DateTimeOffset.MaxValue.Ticks}."));
                }
                return true;
            }
            if (raw is not LiteralExpression { Token: StringToken text }) return false;
            var format = _options.Runtime.DateTimeFormat;
            if (string.IsNullOrEmpty(format))
            {
                throw new AuroraCompilationException(
                    AuroraCompilationStage.Parsing,
                    this.Lexer.FullPath,
                    raw.Range,
                    TDocPathMessage("EngineOptions.Runtime.DateTimeFormat cannot be null or empty."));
            }
            try
            {
                if (TypedDocumentBinder.TryParseDate(text.Value, format, out _))
                {
                    return true;
                }
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                throw new AuroraCompilationException(
                    AuroraCompilationStage.Parsing,
                    this.Lexer.FullPath,
                    raw.Range,
                    TDocPathMessage($"Invalid EngineOptions.Runtime.DateTimeFormat '{format}'."));
            }
            throw new AuroraCompilationException(
                AuroraCompilationStage.Parsing,
                this.Lexer.FullPath,
                raw.Range,
                TDocPathMessage($"Date value must match EngineOptions.Runtime.DateTimeFormat '{format}'."));
        }

        private void PushTDocProperty(string name)
        {
            (_tdocPath ??= new List<TDocPathSegment>(4)).Add(new TDocPathSegment(name));
        }

        private void PushTDocIndex(int index)
        {
            (_tdocPath ??= new List<TDocPathSegment>(4)).Add(new TDocPathSegment(index));
        }

        private void PopTDocPath()
        {
            if (_tdocPath is { Count: > 0 }) _tdocPath.RemoveAt(_tdocPath.Count - 1);
        }

        private string TDocPathMessage(string message, int? childIndex = null)
        {
            var path = FormatTDocPath(childIndex);
            return message + " (data path " + path + ")";
        }

        private string FormatTDocPath(int? childIndex = null)
        {
            var builder = new StringBuilder("$");
            var count = _tdocPath?.Count ?? 0;
            for (var i = 0; i < count; i++)
            {
                var segment = _tdocPath[i];
                if (segment.IsIndex)
                {
                    builder.Append('[').Append(segment.Index).Append(']');
                }
                else if (IsTDocPathIdentifier(segment.Property))
                {
                    builder.Append('.').Append(segment.Property);
                }
                else
                {
                    builder.Append("[\"");
                    foreach (var value in segment.Property ?? string.Empty)
                    {
                        if (value is '\\' or '"') builder.Append('\\');
                        builder.Append(value);
                    }
                    builder.Append("\"]");
                }
            }
            if (childIndex.HasValue) builder.Append('[').Append(childIndex.Value).Append(']');
            return builder.ToString();
        }

        private static bool IsTDocPathIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) || !IsTDocPathIdentifierStart(value[0])) return false;
            for (var i = 1; i < value.Length; i++)
            {
                var current = value[i];
                if (!IsTDocPathIdentifierStart(current) && !char.IsDigit(current)) return false;
            }
            return true;
        }

        private static bool IsTDocPathIdentifierStart(char value)
        {
            return value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_' or '$' ||
                   value is >= '\u4e00' and <= '\u9fbb';
        }

        private readonly struct TDocPathSegment
        {
            internal TDocPathSegment(string property)
            {
                Property = property;
                Index = 0;
                IsIndex = false;
            }

            internal TDocPathSegment(int index)
            {
                Property = null;
                Index = index;
                IsIndex = true;
            }

            internal string Property { get; }
            internal int Index { get; }
            internal bool IsIndex { get; }
        }

        private static bool IsTDocScalar(Expression expression, Type tokenType)
        {
            return expression is LiteralExpression literal && tokenType.IsInstanceOfType(literal.Token);
        }

        private static bool IsTDocNumber(Expression expression)
        {
            return TryGetTDocNumber(expression, out _);
        }

        private static Expression UnwrapTDocValue(Expression expression)
        {
            return expression is TypedDocumentExpression tdoc ? tdoc.Value : expression;
        }

        private bool IsTDocTypePrefix()
        {
            var next = PeekToken(1);
            return IsTDocValueStart(next);
        }

        private bool IsTDocInterpolationStart()
        {
            var token = this.Lexer.LookAtHead();
            return IsIdentifier(token, "$") && PeekSymbol(1) == Symbols.PT_LEFTPARENTHESIS;
        }

        private bool IsTDocValueStart(Token token)
        {
            if (token is ValueToken || token.Symbol == Symbols.PT_LEFTBRACE || token.Symbol == Symbols.PT_LEFTBRACKET)
            {
                return true;
            }
            if (token.Symbol == Symbols.OP_SUBTRACT)
            {
                return PeekToken(2) is NumberToken;
            }
            return IsIdentifier(token, "$") && PeekSymbol(2) == Symbols.PT_LEFTPARENTHESIS;
        }

        private static bool IsTDocPropertyName(Token token)
        {
            return token is IdentifierToken or StringToken;
        }

        private static bool IsIdentifier(Token token, string value)
        {
            return token is IdentifierToken && StringComparer.Ordinal.Equals(token.Value, value);
        }

        private Token PeekToken(int offset)
        {
            var snapshot = this.Lexer.CreateSnapshot();
            try
            {
                Token token = null;
                for (var i = 0; i <= offset; i++) token = this.Lexer.Next();
                return token;
            }
            finally
            {
                this.Lexer.RestoreSnapshot(snapshot);
            }
        }

        private Symbols PeekSymbol(int offset)
        {
            return PeekToken(offset)?.Symbol;
        }

        private IReadOnlyList<ParameterDeclaration> ParseFunctionArguments()
        {
            if (this.Lexer.TestSymbol(Symbols.PT_RIGHTPARENTHESIS))
            {
                this.Lexer.Expect(Symbols.PT_RIGHTPARENTHESIS);
                return Array.Empty<ParameterDeclaration>();
            }

            var arguments = new List<ParameterDeclaration>(4);
            var argumentNames = new HashSet<string>(StringComparer.Ordinal);
            var seenSpread = false;
            while (true)
            {
                // Check for spread operator
                bool isSpread = this.Lexer.TestNext(Symbols.OP_SPREAD);
                if (seenSpread)
                {
                    throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, this.Lexer.LookAtHead(), "Rest parameter must be the last parameter.");
                }
                seenSpread = isSpread;

                var varname = this.Lexer.NextOfKind<IdentifierToken>();
                if (!argumentNames.Add(varname.Value))
                {
                    throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, varname, $"Duplicate parameter name '{varname.Value}'.");
                }
                Expression defaultValue = null;

                if (this.Lexer.TestNext(Symbols.OP_ASSIGNMENT))
                {
                    if (isSpread)
                    {
                        throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, varname, "Rest parameter cannot have a default value.");
                    }
                    defaultValue = ParseExpression(0);
                    if (defaultValue == null)
                    {
                        throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, varname, "Parameter default value requires an expression.");
                    }
                }

                var param = SetRange(new ParameterDeclaration((Byte)arguments.Count, varname, defaultValue), varname.Range, (defaultValue?.Range ?? varname.Range));
                param.IsSpreadOperator = isSpread;
                arguments.Add(param);

                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTPARENTHESIS)) break;
                this.Lexer.Expect(Symbols.PT_COMMA);
            }
            this.Lexer.Expect(Symbols.PT_RIGHTPARENTHESIS);
            return arguments;
        }

        // Helper for Infix Lambda
        private Expression CreateLambda(Expression left)
        {
            // Left is arguments.
            // Needs to be converted to ParameterDeclarations.
            var args = new List<ParameterDeclaration>(4);

            if (left is GroupExpression group)
            {
                // Extract args from group
                for (int i = 0; i < group.Expressions.Count; i++)
                {
                    var node = group.Expressions[i];
                    if (node is NameExpression name)
                    {
                        args.Add(SetRange(new ParameterDeclaration((byte)args.Count, name.Identifier, null), name.Range, name.Range));
                    }
                    // Assignment/Default value? `(x=1)` -> AssignmentExpression in group
                    else if (node is AssignmentExpression assign && assign.Left is NameExpression nameL)
                    {
                        args.Add(SetRange(new ParameterDeclaration((byte)args.Count, nameL.Identifier, assign.Right), nameL.Range, assign.Right?.Range ?? nameL.Range));
                    }
                }
            }
            else if (left is NameExpression nameExp)
            {
                args.Add(SetRange(new ParameterDeclaration(0, nameExp.Identifier, null), nameExp.Range, nameExp.Range));
            }

            // Ensure unique name
            var position = this.Lexer.PeekRange();
            var nameStr = "lambda_" + position.StartLine + "_" + position.StartColumn;
            var nameToken = new IdentifierToken() { Value = nameStr, LineNumber = position.StartLine };

            // Parse Body
            // Lambda body can be Block or Expression.
            // `=> { ... }` or `=> expr`

            Statement bodyStmt = null;
            if (this.Lexer.TestSymbol(Symbols.PT_LEFTBRACE))
            {
                using (scopeStack.Scope(ScopeType.FUNCTION)) bodyStmt = ParseBlock(true); // true = isFunction
            }
            else
            {
                // Expression body -> ReturnStatement
                using (scopeStack.Scope(ScopeType.FUNCTION))
                {
                    var expr = ParseExpression(0);
                    bodyStmt = new ReturnStatement(expr);
                    var block = new BlockStatement();
                    block.IsFunction = true;
                    block.AddStatement(bodyStmt);
                    bodyStmt = block;
                }
            }

            var funcDecl = new FunctionDeclaration(MemberAccess.Internal, nameToken, args, bodyStmt, FunctionFlags.Lambda);
            SetRange(funcDecl, left.Range, bodyStmt.Range);
            var lambda = new LambdaExpression(funcDecl);
            return SetRange(lambda, left.Range, bodyStmt.Range);
        }

        private Expression OptimizeAssignment(AssignmentExpression assignment)
        {
            EnsureAssignableTarget(assignment.Left, assignment.Operator);
            if (assignment.Left is GetPropertyExpression getter)
            {
                var setter = new SetPropertyExpression(getter.Object, getter.Property, assignment.Right);
                setter.Range = MergeRanges(assignment.Range, getter.Range);
                return setter;
            }
            else if (assignment.Left is GetElementExpression eleGetter)
            {
                var setter = new SetElementExpression(eleGetter.Object, eleGetter.Index, assignment.Right);
                setter.Range = MergeRanges(assignment.Range, eleGetter.Range);
                return setter;
            }
            return assignment;
        }

        private Expression ParseNewExpression(Operator op, SourceSpan newRange, Token token)
        {
            var target = ParseExpression(Operator.FunctionCall.Precedence);
            if (target == null || !Lexer.TestSymbol(Symbols.PT_LEFTPARENTHESIS))
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, token, $"Uncaught TypeError: {target} is not a constructor");
            }

            Lexer.NextRangeOfKind(Symbols.PT_LEFTPARENTHESIS);
            var callExp = new FunctionCallExpression(Operator.FunctionCall, target);
            if (!this.Lexer.TestSymbol(Symbols.PT_RIGHTPARENTHESIS))
            {
                while (true)
                {
                    var arg = ParseExpression(0);
                    if (arg == null)
                    {
                        throw new AuroraCompilationException(AuroraCompilationStage.Parsing, Lexer.FullPath, Lexer.LookAtHead(), "Function argument requires an expression.");
                    }
                    callExp.AddArgument(arg);

                    if (this.Lexer.TestSymbol(Symbols.PT_RIGHTPARENTHESIS)) break;
                    this.Lexer.Expect(Symbols.PT_COMMA);
                }
            }
            var rightParen = this.Lexer.NextRangeOfKind(Symbols.PT_RIGHTPARENTHESIS);
            SetRange(callExp, target.Range, rightParen);
            var newExp = new NewExpression(op, callExp);
            return SetRange(newExp, newRange, rightParen);
        }

        private Expression OptimizeCompoundAssignment(CompoundExpression assignment)
        {
            EnsureAssignableTarget(assignment.Left, assignment.Operator);
            var op = assignment.Operator.SimplerOperator;
            if (op == null)
            {
                return assignment;
            }

            var value = new BinaryExpression(op, assignment.Left, assignment.Right);
            value.Range = MergeRanges(assignment.Left.Range, assignment.Right.Range);
            if (assignment.Left is GetPropertyExpression getter)
            {
                var setter = new SetPropertyExpression(getter.Object, getter.Property, value);
                setter.Range = MergeRanges(assignment.Range, getter.Range);
                return setter;
            }
            return assignment;
        }

        private void EnsureAssignableTarget(Expression target, Operator op)
        {
            if (target is NameExpression || target is GetPropertyExpression || target is GetElementExpression)
            {
                return;
            }

            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, target?.Range ?? this.Lexer.PeekRange(), $"Operator '{op?.Symbol?.Name}' requires an assignable target.");
        }

        private void EnsureMutationTarget(Expression target, Operator op)
        {
            if (target is NameExpression || target is GetPropertyExpression || target is GetElementExpression)
            {
                return;
            }

            throw new AuroraCompilationException(AuroraCompilationStage.Parsing, this.Lexer.FullPath, target?.Range ?? this.Lexer.PeekRange(), $"Operator '{op?.Symbol?.Name}' requires an assignable target.");
        }

        private T SetDebug<T>(T node, Token token) where T : AstNode
        {
            if (node == null) return null;
            if (token != null)
            {
                node.Range = token.Range;
            }
            else
            {
                node.Range = new SourceSpan
                {
                    FileName = this.Lexer.FullPath,
                    StartLine = this.Lexer.LineNumber,
                    StartColumn = this.Lexer.ColumnNumber,
                    EndLine = this.Lexer.LineNumber,
                    EndColumn = this.Lexer.ColumnNumber,
                    Offset = this.Lexer.Offset,
                    Length = 0
                };
            }
            return node;
        }

        private void SetSourceRecursive(AstNode node)
        {
            _sourceFileVisitor.Apply(node, this.Lexer.FullPath);
        }

        private SourceSpan MergeRanges(SourceSpan start, SourceSpan end)
        {
            if (start.StartLine == -1) return end;
            if (end.StartLine == -1) return start;

            return new SourceSpan
            {
                FileName = start.FileName,
                StartLine = start.StartLine,
                StartColumn = start.StartColumn,
                EndLine = end.EndLine,
                EndColumn = end.EndColumn,
                Offset = start.Offset,
                Length = (end.Offset + end.Length) - start.Offset
            };
        }

        private T SetRange<T>(T node, SourceSpan start, SourceSpan end) where T : AstNode
        {
            if (node != null) node.Range = MergeRanges(start, end);
            return node;
        }

        private T SetRange<T>(T node, Token start, Token end) where T : AstNode
        {
            if (node != null) node.Range = MergeRanges(start.Range, end.Range);
            return node;
        }

        private static void FixRange(AstNode node, SourceSpan range)
        {
            if (node == null) return;
            node.Range = range;
            if (node is BinaryExpression binary)
            {
                FixRange(binary.Left, range);
                FixRange(binary.Right, range);
            }
            else if (node is UnaryExpression unary)
            {
                FixRange(unary.Expression, range);
            }
            else if (node is FunctionCallExpression call)
            {
                FixRange(call.Target, range);
                if (call.Arguments != null)
                {
                    foreach (var arg in call.Arguments) FixRange(arg, range);
                }
            }
            else if (node is GroupExpression group)
            {
                FixRange(group.Expression, range);
            }
            else if (node is GetPropertyExpression getProp)
            {
                FixRange(getProp.Object, range);
                FixRange(getProp.Property, range);
            }
            else if (node is GetElementExpression getEle)
            {
                FixRange(getEle.Object, range);
                FixRange(getEle.Index, range);
            }
            else if (node is MapExpression map)
            {
                for (int i = 0; i < map.Entries.Count; i++) FixRange(map.Entries[i], range);
            }
            else if (node is MapKeyValueExpression kv)
            {
                FixRange(kv.Value, range);
            }
        }
    }
}
