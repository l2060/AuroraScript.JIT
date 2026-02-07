using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Core;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AuroraScript.Compiler.Analyzer
{

    internal enum ScopeType { MODULE, FOR, BLOCK, GROUP, FUNCTION }

    internal class ScopeStack : IDisposable
    {
        private Stack<ScopeType> scopeStack = new Stack<ScopeType>();
        private int MaxStack = 0;
        public ScopeType Current => scopeStack.Peek();
        public int Count => scopeStack.Count;

        public ScopeStack Scope(ScopeType type)
        {
            scopeStack.Push(type);
            if (scopeStack.Count > MaxStack)
            {
                MaxStack = scopeStack.Count;
            }
            return this;
        }

        public void Dispose()
        {
            scopeStack.Pop();
        }
    }

    internal class AuroraParser
    {
        public AuroraLexer Lexer { get; private set; }
        public ModuleDeclaration Root { get; private set; }

        private readonly ScopeStack scopeStack = new ScopeStack();

        private readonly EngineOptions _options;

        public AuroraParser(AuroraLexer lexer, EngineOptions options)
        {
            _options = options;
            this.Lexer = lexer;
            this.Root = new ModuleDeclaration(this.Lexer.Directory);
            this.Root.FullPath = lexer.FullPath;
            if (lexer.FullPath.StartsWith(lexer.BaseDirectory))
            {
                this.Root.ModulePath = lexer.FullPath.Substring(lexer.BaseDirectory.Length).Replace("\\", "/");
            }
            else
            {
                this.Root.ModulePath = Path.GetRelativePath(lexer.BaseDirectory, lexer.FullPath).Replace("\\", "/");
            }

            // Remove leading /
            if (this.Root.ModulePath.StartsWith("/"))
            {
                this.Root.ModulePath = this.Root.ModulePath.Substring(1);
            }
            // Set default module name
            var moduleDefaultName = this.Root.ModulePath;
            if (moduleDefaultName.EndsWith(_options.ExtName))
            {
                moduleDefaultName = moduleDefaultName.Substring(0, moduleDefaultName.Length - 3);
            }
            this.Root.MetaInfos.Add("module", moduleDefaultName);
            this.Root.ModuleName = moduleDefaultName;
        }

        public ModuleDeclaration Parse()
        {
            using (scopeStack.Scope(ScopeType.MODULE))
            {
                while (true)
                {
                    if (this.Lexer.TestNext(Symbols.KW_EOF)) break;
                    // Use recursion for statements
                    var node = ParseStatement();

                    if (node == null) continue;

                    node.IsIndependent = true;

                    if (node is ModuleMetaStatement meta)
                    {
                        this.Root.MetaInfos[meta.Name.Value] = meta.Value?.Value;
                        if (meta.Name.Value == "module")
                        {
                            this.Root.ModuleName = meta.Value?.Value;
                        }
                    }
                    else if (node is FunctionDeclaration func)
                    {
                        this.Root.Functions.Add(func);
                        func.Parent = this.Root;
                    }
                    else if (node is ImportDeclaration importDeclaration)
                    {
                        this.Root.Imports.Add(importDeclaration);
                        importDeclaration.Parent = Root;
                    }
                    else
                    {
                        this.Root.AddNode(node);
                    }
                }
                SetSourceRecursive(this.Root);
            }
            if (scopeStack.Count > 0)
            {
                throw new AuroraParseException(Lexer.FullPath, Token.EOF, "An premature ending statement.");
            }
            return this.Root;
        }

        private Statement ParseStatement()
        {
            var token = this.Lexer.LookAtHead();
            if (token == null || token == Token.EOF) return null;

            if (token.Symbol == Symbols.PT_SEMICOLON)
            {
                this.Lexer.Next();
                return null;
            }

            if (token.Symbol == Symbols.PT_METAINFO) { var res = ParseMetaInfo(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.PT_LEFTBRACE) { var res = ParseBlock(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_IMPORT) { var res = ParseImport(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_INCLUDE) { var res = ParseInclude(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_EXPORT) { var res = ParseExportStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_FUNCTION || token.Symbol == Symbols.KW_FUNC) { var res = ParseFunctionDeclaration(MemberAccess.Internal); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_DECLARE) { var res = ParseDeclare(MemberAccess.Internal); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_CONST || token.Symbol == Symbols.KW_VAR) { var res = ParseVariableDeclaration(MemberAccess.Internal); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_ENUM) { var res = ParseEnumDeclaration(MemberAccess.Internal); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_FOR) { var res = ParseForBlock(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_WHILE) { var res = ParseWhileBlock(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_IF) { var res = ParseIfBlock(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_CONTINUE) { var res = ParseContinueStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_YIELD) { var res = ParseYieldStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_BREAK) { var res = ParseBreakStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_RETURN) { var res = ParseReturnStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_THROW) { var res = ParseThrowStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_TRY) { var res = ParseTryStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_DELETE) { var res = ParseDeleteStatement(); if (res != null) res.IsIndependent = true; return res; }
            if (token.Symbol == Symbols.KW_DEBUGGER) { var res = ParseDebuggerStatement(); if (res != null) res.IsIndependent = true; return res; }


            // Expression Statement
            var exp = ParseExpression(0); // 0 is lowest precedence

            if (exp == null)
            {
                // If we are here, we have a token that is not a statement start, and not start of expression.
                // This is a syntax error.
                throw new AuroraParseException(this.Lexer.FullPath, token, $"Unexpected token: {token.Value}");
            }

            Token semi = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);

            var stmt = new ExpressionStatement(exp);
            stmt.IsIndependent = true;
            SetRange(stmt, exp.Range, (semi != null ? semi.Range : exp.Range));
            return stmt;
        }

        // =================================================================================
        // Pratt Parsing for Expressions
        // =================================================================================

        private Expression ParseExpression(int precedence)
        {
            var token = this.Lexer.LookAtHead();
            if (token == null || token.Symbol == Symbols.PT_SEMICOLON || token.Symbol == Symbols.PT_RIGHTPARENTHESIS ||
                token.Symbol == Symbols.PT_RIGHTBRACKET || token.Symbol == Symbols.PT_RIGHTBRACE || token.Symbol == Symbols.PT_COMMA)
            {
                return null;
            }

            var startToken = this.Lexer.Next();
            Expression expression = ParsePrefix(startToken);
            if (expression == null) return null;

            while (precedence < GetPrecedence(this.Lexer.LookAtHead()))
            {
                var nextToken = this.Lexer.Next();
                var op = Operator.FromSymbols(nextToken.Symbol, true); // Infix/Postfix
                expression = ParseInfix(expression, nextToken, op);
                if (expression == null) break;
            }

            return expression;
        }

        private Expression ParsePrefix(Token token)
        {
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
                    if (this.Lexer.LookAtHead().Symbol == Symbols.PT_RIGHTPARENTHESIS)
                    {
                        this.Lexer.Next();
                        return group;
                    }
                    while (true)
                    {
                        var exp = ParseExpression(0);
                        if (exp != null) group.AddNode(exp);
                        if (this.Lexer.TestNext(Symbols.PT_COMMA)) continue;
                        break;
                    }
                    var rightParen = this.Lexer.NextOfKind(Symbols.PT_RIGHTPARENTHESIS);
                    return SetRange(group, token.Range, rightParen.Range);
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
                            arrayLiteral.AddNode(exp);
                        else
                        {
                            var nullLiteral = new LiteralExpression(new NullToken());
                            arrayLiteral.AddNode(nullLiteral);
                        }

                        if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACKET)) break;
                        this.Lexer.NextOfKind(Symbols.PT_COMMA);
                    }
                }
                var rightBracket = this.Lexer.NextOfKind(Symbols.PT_RIGHTBRACKET);
                return SetRange(arrayLiteral, token.Range, rightBracket.Range);
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
                    var right = ParseExpression(op.Precedence);
                    if (right is not FunctionCallExpression funcCall)
                    {
                        throw new AuroraParseException(this.Lexer.FullPath, token, $"Uncaught TypeError: {right} is not a constructor");
                    }
                    var binary = new NewExpression(op, funcCall);
                    if (right != null) binary.Range = MergeRanges(token.Range, right.Range);
                    return binary;
                }

                // Normal Unary
                var rightUnary = ParseExpression(op.Precedence);
                var expression = new UnaryExpression(op, UnaryType.Prefix, rightUnary);
                // SetDebug(expression, token); // Redundant if already handled
                if (rightUnary != null) expression.Range = MergeRanges(token.Range, rightUnary.Range);
                return expression;
            }



            throw new AuroraParseException(this.Lexer.FullPath, token, $"Unknown prefix token: {token.Value}");
        }

        private Expression ParseInfix(Expression left, Token opToken, Operator op)
        {
            // Member Access .
            if (opToken.Symbol == Symbols.PT_DOT)
            {

                var identifier = this.Lexer.NextOfToken<IdentifierToken, KeywordToken>();
                var dotExp = new GetPropertyExpression(Operator.MemberAccess, left, new NameExpression(identifier));
                return SetRange(dotExp, left.Range, identifier.Range);
            }

            // Index Access [
            // Function Call (
            if (opToken.Symbol == Symbols.PT_LEFTPARENTHESIS)
            {
                var callExp = new FunctionCallExpression(Operator.FunctionCall, left);
                if (!this.Lexer.TestSymbol(Symbols.PT_RIGHTPARENTHESIS))
                {
                    while (true)
                    {
                        var arg = ParseExpression(0);
                        if (arg != null) callExp.AddArgument(arg);

                        if (this.Lexer.TestSymbol(Symbols.PT_RIGHTPARENTHESIS)) break;
                        this.Lexer.NextOfKind(Symbols.PT_COMMA);
                    }
                }
                var rightParen = this.Lexer.NextOfKind(Symbols.PT_RIGHTPARENTHESIS);
                return SetRange(callExp, left.Range, rightParen.Range);
            }

            // Index Access [
            if (opToken.Symbol == Symbols.PT_LEFTBRACKET)
            {
                var indexExp = ParseExpression(0);
                var getElem = new GetElementExpression(Operator.Index, left, indexExp);
                var rightBracket = this.Lexer.NextOfKind(Symbols.PT_RIGHTBRACKET);
                return SetRange(getElem, left.Range, rightBracket.Range);
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
                if (op == Operator.Assignment)
                {
                    var right = ParseExpression(op.Precedence - 1); // Right-associative
                    var assign = new AssignmentExpression(op, left, right);
                    binary = assign;
                }
                else if (isCompound)
                {
                    var right = ParseExpression(op.Precedence - 1);
                    var compound = new CompoundExpression(op, left, right);
                    binary = compound;
                }
                else if (op == Operator.In)
                {
                    var right = ParseExpression(op.Precedence);
                    var inExp = new IncludedExpression(op, left, right);
                    binary = inExp;
                }
                else
                {
                    var right = ParseExpression(op.Precedence);
                    var bin = new BinaryExpression(op, left, right);
                    binary = bin;
                }

                if (binary != null) SetRange(binary, left.Range, (binary.ChildNodes.GetEnumerator().MoveNext() ? binary.ChildNodes.Last() : binary).Range);

                if (binary is AssignmentExpression assignExp) return OptimizeAssignment(assignExp);

                return binary;
            }

            // Postfix (++, --)
            if (op != null && op.Placement == OperatorPlacement.Postfix)
            {
                var unary = new UnaryExpression(op, UnaryType.Post, left);
                // SetDebug(unary, opToken); // Redundant
                unary.Range = MergeRanges(left.Range, opToken.Range);
                return unary;
            }

            throw new AuroraParseException(this.Lexer.FullPath, opToken, "Unexpected operator " + opToken.Value);
        }

        private Expression ParseStringTemplate(StringTemplateToken token)
        {
            var source = this.Lexer.InputData;
            var raw = source.Substring(token.Range.Offset + 1, token.Range.Length - 2);

            Expression result = null;

            int i = 0;
            var sb = new StringBuilder();

            void AddStringPart()
            {
                if (sb.Length > 0)
                {
                    Expression cursmt = null;
                    var str = sb.ToString();
                    sb.Clear();
                    var st = new StringToken() { Value = str };
                    var lit = new LiteralExpression(st);
                    if (result == null)
                    {
                        cursmt = lit;
                    }
                    else
                    {
                        cursmt = new BinaryExpression(Operator.Add, result, lit);
                        SetRange(cursmt, result.Range, result.Range);
                    }


                    result = cursmt;
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
                        throw new AuroraParseException(this.Lexer.FullPath, token, "Template string interpolation missing closing brace '}'");
                    }

                    string exprText = raw.Substring(exprStart, exprEnd - exprStart);
                    var subLexer = new AuroraLexer(this.Lexer.BaseDirectory, new TextSource(this.Lexer.BaseDirectory, this.Lexer.FullPath, exprText));
                    var subParser = new AuroraParser(subLexer, _options);
                    var expr = subParser.ParseExpression(0);

                    if (expr != null)
                    {
                        if (result == null)
                        {
                            var empty = new StringToken() { Value = "" };
                            result = new BinaryExpression(Operator.Add, new LiteralExpression(empty), expr);
                        }
                        else
                        {
                            result = new BinaryExpression(Operator.Add, result, expr);
                        }
                        SetRange(result, token.Range, token.Range); // Base range is the whole template
                    }

                    i = exprEnd + 1; // Move past }
                    continue;
                }

                sb.Append(c);
                i++;
            }

            AddStringPart();

            var finalExpr = result ?? new LiteralExpression(new StringToken() { Value = "" });
            return finalExpr;
        }

        // ===================================
        // Helpers
        // ===================================

        private static int GetPrecedence(Token token)
        {
            if (token is PunctuatorToken || token is OperatorToken)
            {
                // Is this token operating as Infix?
                // The boolean literal 'true' in FromSymbols(..., true) means "Has Left Operand"
                // In GetPrecedence, we are looking ahead, so we assume we are inside ParseExpression loop where we already have a Left.
                var op = Operator.FromSymbols(token.Symbol, true);
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

        // ... (Include other parsing methods like ParseBlock, ParseFunction, ParseObjectConstructor from original file but cleaned up)

        // Placeholder for missing methods to make it compile initially
        private Statement ParseMetaInfo()
        {
            this.Lexer.NextOfKind(Symbols.PT_METAINFO);
            var metaName = this.Lexer.NextOfKind<IdentifierToken>();
            this.Lexer.NextOfKind(Symbols.PT_LEFTPARENTHESIS);
            var token = this.Lexer.TestNextOfKind<IdentifierToken>();
            this.Lexer.NextOfKind(Symbols.PT_RIGHTPARENTHESIS);
            this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
            var statement = SetDebug(new ModuleMetaStatement(metaName, token), metaName);
            return statement;
        }

        private Statement ParseBlock(bool isFunction = false)
        {
            var leftBrace = this.Lexer.NextOfKind(Symbols.PT_LEFTBRACE);
            using (scopeStack.Scope(ScopeType.BLOCK))
            {
                var result = new BlockStatement();
                while (true)
                {
                    var token = this.Lexer.LookAtHead();
                    if (token.Symbol == Symbols.PT_RIGHTBRACE) break;
                    if (token == Token.EOF) throw new AuroraParseException(this.Lexer.FullPath, this.Lexer.Previous(), "Unexpected end of file in block");

                    var exp = this.ParseStatement();
                    if (exp is FunctionDeclaration functionDeclaration)
                    {
                        result.Functions.Add(functionDeclaration);
                        functionDeclaration.Parent = result;
                    }
                    else if (exp != null)
                    {
                        result.AddNode(exp);
                    }
                }
                var rightBrace = this.Lexer.NextOfKind(Symbols.PT_RIGHTBRACE);
                SetRange(result, leftBrace.Range, rightBrace.Range);

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
            var importToken = this.Lexer.NextOfKind(Symbols.KW_INCLUDE);
            if (!this.Root.IsEmpty())
            {
                throw new AuroraParseException(this.Lexer.FullPath, importToken, $"The Include statement must be placed at the top of the module.");
            }
            StringToken fileToken = this.Lexer.NextOfKind<StringToken>();
            var closed = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
            var (fullPath, modulePath) = ResolveImportPath(fileToken.Value);
            if (!File.Exists(fullPath))
            {
                throw new AuroraEmitException(importToken, $"include file not found: {fileToken.Value}");
            }
            var import = new ImportDeclaration() { File = fileToken, FullPath = fullPath, ModulePath = modulePath, Include = true };

            return SetRange(import, importToken.Range, closed.Range);
        }



        private Statement ParseImport()
        {
            var importToken = this.Lexer.NextOfKind(Symbols.KW_IMPORT);
            if (!this.Root.IsEmpty())
            {
                throw new AuroraParseException(this.Lexer.FullPath, importToken, $"The Import statement must be placed at the top of the module.");
            }
            var module = this.Lexer.NextOfKind<IdentifierToken>();
            this.Lexer.NextOfKind(Symbols.KW_FROM);
            StringToken fileToken = this.Lexer.NextOfKind<StringToken>();
            var closed = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
            var (fullPath, modulePath) = ResolveImportPath(fileToken.Value);
            if (!File.Exists(fullPath))
            {
                throw new AuroraEmitException(importToken, $"Import file not found: {fileToken.Value}");
            }
            var import = new ImportDeclaration() { Name = module, File = fileToken, FullPath = fullPath, ModulePath = modulePath, Include = false };

            return SetRange(import, importToken.Range, closed.Range);
        }

        private Statement ParseExportStatement()
        {
            var exportToken = this.Lexer.NextOfKind(Symbols.KW_EXPORT);

            if (scopeStack.Current != ScopeType.MODULE)
            {
                throw new AuroraParseException(this.Lexer.FullPath, exportToken, $"Invalid “export” keyword in row {exportToken.LineNumber}, column {exportToken.ColumnNumber}, scope not supported.");
            }

            var token = this.Lexer.LookAtHead();
            if (token is KeywordToken && (token.Symbol == Symbols.KW_FUNCTION || token.Symbol == Symbols.KW_FUNC))
            {
                return ParseFunctionDeclaration(MemberAccess.Export);
            }
            else if (token is KeywordToken && token.Symbol == Symbols.KW_VAR)
            {
                return ParseVariableDeclaration(MemberAccess.Export);
            }
            else if (token is KeywordToken && token.Symbol == Symbols.KW_CONST)
            {
                return ParseVariableDeclaration(MemberAccess.Export);
            }
            else if (token is KeywordToken && token.Symbol == Symbols.KW_DECLARE)
            {
                return ParseDeclare(MemberAccess.Export);
                // Declare doesn't take Access in original call? Check.
                // Original: return ParseDeclare(currentScope); 
                // Wait, logic says: "The Declare keyword only allows the declaration of external methods"
                // ParseDeclare just returns FunctionDeclaration with flags.
                // But let's check signatures.
            }
            else if (token is KeywordToken && token.Symbol == Symbols.KW_ENUM)
            {
                return ParseEnumDeclaration(MemberAccess.Export);
            }

            throw new AuroraParseException(this.Lexer.FullPath, token, "Invalid keywords appear in export declaration.");
        }

        private Statement ParseFunctionDeclaration(MemberAccess access = MemberAccess.Internal)
        {
            var start = this.Lexer.NextOfKind(Symbols.KW_FUNCTION, Symbols.KW_FUNC);
            var functionName = this.Lexer.NextOfKind<IdentifierToken>();
            var func = this.ParseFunction(functionName, access, FunctionFlags.General);
            return SetRange(func, start.Range, func.Range);
        }

        private FunctionDeclaration ParseFunction(IdentifierToken functionName, MemberAccess access = MemberAccess.Internal, FunctionFlags flags = FunctionFlags.General)
        {
            var token = this.Lexer.NextOfKind(Symbols.PT_LEFTPARENTHESIS);
            var arguments = this.ParseFunctionArguments();
            // ParseFunctionArguments consumes the )

            if (flags == FunctionFlags.Lambda)
            {
                this.Lexer.NextOfKind(Symbols.PT_LAMBDA);
            }

            using (scopeStack.Scope(ScopeType.FUNCTION))
            {
                var body = this.ParseBlock();
                if (!(body is BlockStatement))
                {
                    var newBody = new BlockStatement();
                    newBody.AddNode(body);
                    body = newBody;
                }
                ((BlockStatement)body).IsFunction = true;
                var declaration = new FunctionDeclaration(access, functionName, arguments, body, flags);
                return SetRange(declaration, (functionName?.Range ?? token.Range), body.Range);
            }
        }

        private Statement ParseDeclare(MemberAccess access = MemberAccess.Internal)
        {
            this.Lexer.NextOfKind(Symbols.KW_DECLARE);
            if (this.Lexer.TestNext(Symbols.KW_FUNCTION) || this.Lexer.TestNext(Symbols.KW_FUNC))
            {
                var funcName = this.Lexer.NextOfKind<IdentifierToken>();
                this.Lexer.NextOfKind(Symbols.PT_LEFTPARENTHESIS);
                var arguments = this.ParseFunctionArguments();
                // ParseFunctionArguments consumes the )
                this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
                var declaration = new FunctionDeclaration(access, funcName, arguments, null, FunctionFlags.Declare);
                return SetRange(declaration, funcName.Range, this.Lexer.Previous(1).Range); // (1) because ;
            }
            throw new AuroraParseException(this.Lexer.FullPath, this.Lexer.LookAtHead(), "The Declare keyword only allows the declaration of external methods");
        }

        private Expression ParseObjectDestructuringPattern()
        {
            var token = this.Lexer.NextOfKind(Symbols.PT_LEFTBRACE);
            var pattern = new ObjectDestructuringPattern();

            while (true)
            {
                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE)) break;

                var propName = this.Lexer.NextOfKind<IdentifierToken>();
                pattern.Properties.Add(propName);

                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE)) break;
                this.Lexer.NextOfKind(Symbols.PT_COMMA);
            }
            var rightBrace = this.Lexer.NextOfKind(Symbols.PT_RIGHTBRACE);

            return SetRange(pattern, token.Range, rightBrace.Range);
        }

        private Expression ParseArrayDestructuringPattern()
        {
            var token = this.Lexer.NextOfKind(Symbols.PT_LEFTBRACKET);
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
                this.Lexer.NextOfKind(Symbols.PT_COMMA);
            }
            var rightBracket = this.Lexer.NextOfKind(Symbols.PT_RIGHTBRACKET);

            return SetRange(pattern, token.Range, rightBracket.Range);
        }

        private Statement ParseVariableDeclaration(MemberAccess access = MemberAccess.Internal)
        {
            var isConst = false;
            Token start = null;
            if (this.Lexer.TestSymbol(Symbols.KW_CONST)) start = this.Lexer.Next();
            if (start != null) isConst = true;
            else if (this.Lexer.TestSymbol(Symbols.KW_VAR)) start = this.Lexer.Next();
            else throw new AuroraParseException(this.Lexer.FullPath, this.Lexer.LookAtHead(), "Variable declaration should be placed after var/const");

            // Check if this is a destructuring pattern
            var nextToken = this.Lexer.LookAtHead();
            if (nextToken.Symbol == Symbols.PT_LEFTBRACE)
            {
                // Object destructuring: var { a, b } = expr;
                var pattern = ParseObjectDestructuringPattern();
                this.Lexer.NextOfKind(Symbols.OP_ASSIGNMENT);
                var init = this.ParseExpression(0);
                var semi = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
                var varDecl = new VariableDeclaration(access, isConst, pattern, init);
                return SetRange(varDecl, start.Range, semi.Range);
            }
            else if (nextToken.Symbol == Symbols.PT_LEFTBRACKET)
            {
                // Array destructuring: var [ a, b, ..c ] = expr;
                var pattern = ParseArrayDestructuringPattern();
                this.Lexer.NextOfKind(Symbols.OP_ASSIGNMENT);
                var init = this.ParseExpression(0);
                var semi = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
                var varDecl = new VariableDeclaration(access, isConst, pattern, init);
                return SetRange(varDecl, start.Range, semi.Range);
            }

            // Simple identifier logic (no commas allowed, multiple variables not supported by current AST)
            Token varName = this.Lexer.NextOfKind<IdentifierToken>();
            Expression initializer = null;

            if (this.Lexer.TestNext(Symbols.OP_ASSIGNMENT))
            {
                initializer = this.ParseExpression(0);
            }

            var semiToken = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);

            var variable = new VariableDeclaration(access, isConst, varName, initializer);
            return SetRange(variable, start.Range, (semiToken?.Range ?? initializer?.Range ?? varName.Range));
        }

        private Statement ParseEnumDeclaration(MemberAccess access)
        {
            var start = this.Lexer.NextOfKind(Symbols.KW_ENUM);
            var enumName = this.Lexer.NextOfKind<IdentifierToken>();
            var elements = this.ParseEnumBody();
            var enumDecl = new EnumDeclaration() { Elements = elements, Identifier = enumName, Access = access };
            return SetRange(enumDecl, start.Range, this.Lexer.Previous().Range);
        }

        private List<EnumElement> ParseEnumBody()
        {
            this.Lexer.NextOfKind(Symbols.PT_LEFTBRACE);
            var result = new List<EnumElement>();
            var elementValue = 0;
            while (true)
            {
                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE)) break;
                var elementName = this.Lexer.NextOfKind<IdentifierToken>();
                if (this.Lexer.TestNext(Symbols.OP_ASSIGNMENT))
                {
                    var token = this.Lexer.NextOfKind<ValueToken>();
                    if (token.Type != Tokens.ValueType.Number) throw new AuroraParseException(this.Lexer.FullPath, token, "Enumeration types only apply to integers");
                    elementValue = Int32.Parse(token.Value);
                }
                var enumElement = new EnumElement() { Name = elementName, Value = elementValue };
                result.Add(enumElement);
                elementValue++;
                this.Lexer.TestNext(Symbols.PT_COMMA);
            }
            this.Lexer.NextOfKind(Symbols.PT_RIGHTBRACE);
            return result;
        }

        // Helpers Needed
        private Statement OptimizeStatement(Statement statement)
        {
            //var node = statement;
            //var parent = statement.Parent; // Parent logic normally managed by AST add, but statement here might be fresh?
            //                               // Original: "var parent = statement.Parent;"
            //while (node is BlockStatement block && block.Length == 1 && block.Functions.Count == 0)
            //{
            //    node = (Statement)block.ChildNodes.First();
            //    node.Remove(); // Detach from block
            //    if (parent != null) parent.AddNode(node);
            //}
            return statement;
        }

        private (string, string) ResolveImportPath(string path)
        {
            var fullPath = Path.GetFullPath(Path.Combine(this.Lexer.Directory, path));
            var extension = Path.GetExtension(fullPath).ToLower();
            if (extension != _options.ExtName)
            {
                fullPath = Path.ChangeExtension(fullPath, _options.ExtName);
            }
            var modulePath = Path.GetRelativePath(this.Lexer.BaseDirectory, fullPath).Replace("\\", "/");
            return (fullPath, modulePath);
        }
        private Statement ParseForBlock()
        {
            using (scopeStack.Scope(ScopeType.FOR))
            {
                var forToken = this.Lexer.NextOfKind(Symbols.KW_FOR);
                this.Lexer.NextOfKind(Symbols.PT_LEFTPARENTHESIS);
                Statement body = null;
                // Branch 1: `for (var x ...)`
                if (this.Lexer.TestSymbol(Symbols.KW_VAR))
                {

                    var snapshot = this.Lexer.CreateSnapshot();
                    var start = this.Lexer.Next(); // var
                    var idToken = this.Lexer.NextOfKind<IdentifierToken>();
                    if (this.Lexer.TestNext(Symbols.OP_IN))
                    {
                        // Case: `for (var x in y)`
                        this.Lexer.RestoreSnapshot(snapshot);
                        this.Lexer.Next(); // var
                        var varName = this.Lexer.NextOfKind<IdentifierToken>();
                        this.Lexer.Next(); // in
                        var right = ParseExpression(0);
                        this.Lexer.NextOfKind(Symbols.PT_RIGHTPARENTHESIS);

                        var variable = new VariableDeclaration(MemberAccess.Internal, false, varName, null);
                        var inExp = new InExpression(Operator.In, new NameExpression(varName), right);

                        inExp = SetRange(inExp, varName.Range, right.Range);


                        body = ParseStatement();
                        var forStmt = new ForInStatement(SetRange(variable, start.Range, varName.Range), inExp, body);
                        return SetRange(forStmt, forToken.Range, (body?.Range ?? right.Range));
                    }
                    else
                    {
                        // Case: `for (var x = 0; ...)`
                        this.Lexer.RestoreSnapshot(snapshot);
                        var initializer = ParseStatement(); // Parses `var x = 0;`

                        var condition = ParseExpression(0);
                        this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);

                        var increment = ParseExpression(0);
                        this.Lexer.NextOfKind(Symbols.PT_RIGHTPARENTHESIS);

                        body = ParseStatement();
                        var forStmt = new ForStatement(condition, initializer, increment, body);
                        return SetRange(forStmt, forToken.Range, (body?.Range ?? increment.Range));
                    }
                }

                var startToken = forToken;
                // Branch 2: `for (x ...)`
                var exp = ParseExpression(0);
                if (exp is InExpression inExpr)
                {
                    // Case: `for (x in y)`
                    this.Lexer.NextOfKind(Symbols.PT_RIGHTPARENTHESIS);
                    body = ParseStatement();
                    var forStmt = new ForInStatement(null, inExpr, body);
                    return SetRange(forStmt, startToken.Range, (body?.Range ?? inExpr.Range));
                }

                // Case: `for (x = 0; ...)`
                this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
                var condExpr = ParseExpression(0);
                this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
                var incExpr = ParseExpression(0);
                this.Lexer.NextOfKind(Symbols.PT_RIGHTPARENTHESIS);

                body = ParseStatement();
                var forStmtLoop = new ForStatement(condExpr, exp, incExpr, body);
                return SetRange(forStmtLoop, startToken.Range, (body?.Range ?? incExpr.Range));
            }
        }

        private Statement ParseWhileBlock()
        {
            var token = this.Lexer.NextOfKind(Symbols.KW_WHILE);
            this.Lexer.NextOfKind(Symbols.PT_LEFTPARENTHESIS);
            var condition = this.ParseExpression(0);
            this.Lexer.NextOfKind(Symbols.PT_RIGHTPARENTHESIS);
            var body = this.ParseStatement();
            // if (body == null) throw new AuroraParseException(this.Lexer.FullPath, token, "while body statement should not be empty");
            // Original parser throws? Code viewer says: "if (body == null) throw..."
            if (body == null) throw new AuroraParseException(this.Lexer.FullPath, token, "while body statement should not be empty");
            return SetRange(new WhileStatement(condition, body), token.Range, body.Range);
        }



        private Statement ParseIfBlock()
        {
            var token = this.Lexer.NextOfKind(Symbols.KW_IF);
            this.Lexer.NextOfKind(Symbols.PT_LEFTPARENTHESIS);
            var condition = this.ParseExpression(0);
            this.Lexer.NextOfKind(Symbols.PT_RIGHTPARENTHESIS);

            var body = this.ParseStatement();
            if (body == null) throw new AuroraParseException(this.Lexer.FullPath, token, "if body statement should not be empty");

            Statement elseStatement = null;
            if (this.Lexer.LookAtHead().Symbol == Symbols.KW_ELSE)
            {
                elseStatement = this.ParseElseBlock();
            }

            return SetRange(new IfStatement(condition, body, elseStatement), token.Range, (elseStatement ?? body).Range);
        }

        private Statement ParseElseBlock()
        {
            this.Lexer.NextOfKind(Symbols.KW_ELSE);
            // If next is IF, standard parse.
            if (this.Lexer.TestSymbol(Symbols.KW_IF))
            {
                // "else if" is just an IfStatement inside the else.
                // Original parser wraps it in Block?
                // "BlockStatement block = new... block.AddNode(ParseIfBlock...)"
                var block = new BlockStatement();
                block.AddNode(ParseIfBlock());
                return OptimizeStatement(block);
            }
            else
            {
                var block = new BlockStatement();
                var body = this.ParseStatement();
                if (body == null) throw new AuroraParseException(this.Lexer.FullPath, this.Lexer.Previous(), "else body statement should not be empty");
                block.AddNode(body);
                return OptimizeStatement(block);
            }
        }

        private TryStatement ParseTryStatement()
        {
            var token = this.Lexer.NextOfKind(Symbols.KW_TRY);

            var body = this.ParseBlock();
            string catchVar = null;
            Statement catchBody = null;
            Statement finallyBody = null;

            if (this.Lexer.LookAtHead().Symbol == Symbols.KW_CATCH)
            {
                this.Lexer.NextOfKind(Symbols.KW_CATCH);
                if (this.Lexer.LookAtHead().Symbol == Symbols.PT_LEFTPARENTHESIS)
                {
                    this.Lexer.NextOfKind(Symbols.PT_LEFTPARENTHESIS);
                    var catchToken = this.Lexer.Next();
                    catchVar = catchToken.Value;
                    this.Lexer.NextOfKind(Symbols.PT_RIGHTPARENTHESIS);
                }
                catchBody = this.ParseBlock();
            }

            if (this.Lexer.LookAtHead().Symbol == Symbols.KW_FINALLY)
            {
                this.Lexer.NextOfKind(Symbols.KW_FINALLY);
                finallyBody = this.ParseBlock();
            }

            return SetRange(new TryStatement(body, catchVar, catchBody, finallyBody), token.Range, (finallyBody ?? catchBody ?? body).Range);
        }

        private ThrowStatement ParseThrowStatement()
        {
            var token = this.Lexer.NextOfKind(Symbols.KW_THROW);

            var exp = this.ParseExpression(0);
            var semi = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);

            return SetRange(new ThrowStatement(exp), token.Range, (semi != null ? semi.Range : exp.Range));
        }

        private Statement ParseContinueStatement()
        {
            var token = this.Lexer.NextOfKind(Symbols.KW_CONTINUE);
            var semi = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
            return SetRange(new ContinueStatement(), token.Range, semi.Range);
        }

        private Statement ParseYieldStatement()
        {
            var token = this.Lexer.NextOfKind(Symbols.KW_YIELD);
            var semi = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
            return SetRange(new YieldStatement(), token.Range, semi.Range);
        }

        private Statement ParseBreakStatement()
        {
            var token = this.Lexer.NextOfKind(Symbols.KW_BREAK);
            var semi = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
            return SetRange(new BreakStatement(), token.Range, semi.Range);
        }


        private Statement ParseDebuggerStatement()
        {
            var token = this.Lexer.NextOfKind(Symbols.KW_DEBUGGER);
            var semi = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
            return SetRange(new DebuggerStatement(), token.Range, semi.Range);
        }




        private Statement ParseReturnStatement()
        {
            var token = this.Lexer.NextOfKind(Symbols.KW_RETURN);
            // Check if next is ; (void return)
            if (this.Lexer.TestSymbol(Symbols.PT_SEMICOLON))
            {
                var semi = this.Lexer.Next();
                return SetRange(new ReturnStatement(null), token.Range, semi.Range);
            }
            var exp = this.ParseExpression(0);
            var endSemi = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
            return SetRange(new ReturnStatement(exp), token.Range, endSemi.Range);
        }

        private Statement ParseDeleteStatement()
        {
            var token = this.Lexer.NextOfKind(Symbols.KW_DELETE);
            var exp = this.ParseExpression(0);
            var semi = this.Lexer.NextOfKind(Symbols.PT_SEMICOLON);
            return SetRange(new DeleteStatement(exp), token.Range, (semi?.Range ?? exp.Range));
        }
        private Expression ParseObjectConstructor()
        {
            var token = this.Lexer.NextOfKind(Symbols.PT_LEFTBRACE);
            var constructExpression = new MapExpression(Operator.ObjectLiteral);
            while (true)
            {
                if (this.Lexer.TestAtHead<EndOfFileToken>()) throw new AuroraParseException(this.Lexer.FullPath, this.Lexer.LookAtHead(), "Unexpected end of file in object constructor");
                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE)) break;

                // Spread ...
                if (this.Lexer.TestNext(Symbols.OP_SPREAD))
                {
                    var value = ParseExpression(0);
                    var spread = new SpreadExpression(value);
                    constructExpression.AddNode(spread);
                    this.Lexer.TestNext(Symbols.PT_COMMA);
                    continue;
                }

                Token varName = this.Lexer.TestNextOfKind<IdentifierToken>();
                if (varName == null) varName = this.Lexer.TestNextOfKind<StringToken>();
                if (varName == null) varName = this.Lexer.TestNextOfKind<NumberToken>();
                if (varName == null) varName = this.Lexer.TestNextOfKind<BooleanToken>();
                if (varName == null) varName = this.Lexer.TestNextOfKind<NullToken>();

                if (varName == null) throw new AuroraParseException(this.Lexer.FullPath, this.Lexer.Next(), "Invalid Map construction syntax");

                if (this.Lexer.TestNext(Symbols.PT_COLON))
                {
                    var value = ParseExpression(0);
                    var newExp = new MapKeyValueExpression(varName, value);
                    SetRange(newExp, varName.Range, value.Range);
                    constructExpression.AddNode(newExp);
                }
                else
                {
                    // Shorthand { x } -> { x: x }
                    var nameToken = new NameExpression(varName);
                    SetRange(nameToken, varName.Range, varName.Range);
                    var kv = new MapKeyValueExpression(varName, nameToken);
                    SetRange(kv, varName.Range, varName.Range);
                    constructExpression.AddNode(kv);
                }

                if (this.Lexer.TestNext(Symbols.PT_COMMA))
                {
                    if (this.Lexer.TestSymbol(Symbols.PT_RIGHTBRACE)) break;
                }
            }
            var rightBrace = this.Lexer.NextOfKind(Symbols.PT_RIGHTBRACE);
            return SetRange(constructExpression, token.Range, rightBrace.Range);
        }

        private List<ParameterDeclaration> ParseFunctionArguments()
        {
            var arguments = new List<ParameterDeclaration>();
            while (true)
            {
                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTPARENTHESIS)) break;

                // Check for spread operator
                bool isSpread = this.Lexer.TestNext(Symbols.OP_SPREAD);

                var varname = this.Lexer.NextOfKind<IdentifierToken>();
                Expression defaultValue = null;

                if (this.Lexer.TestNext(Symbols.OP_ASSIGNMENT))
                {
                    defaultValue = ParseExpression(0);
                }

                var param = new ParameterDeclaration((Byte)arguments.Count, varname, defaultValue);
                param.IsSpreadOperator = isSpread;
                arguments.Add(param);

                if (this.Lexer.TestSymbol(Symbols.PT_RIGHTPARENTHESIS)) break;
                this.Lexer.NextOfKind(Symbols.PT_COMMA);
            }
            this.Lexer.NextOfKind(Symbols.PT_RIGHTPARENTHESIS);
            return arguments;
        }

        // Helper for Infix Lambda
        private Expression CreateLambda(Expression left)
        {
            // Left is arguments.
            // Needs to be converted to ParameterDeclarations.
            var args = new List<ParameterDeclaration>();

            if (left is GroupExpression group)
            {
                // Extract args from group
                foreach (var node in group.ChildNodes)
                {
                    if (node is NameExpression name)
                    {
                        args.Add(new ParameterDeclaration((byte)args.Count, name.Identifier, null));
                    }
                    // Assignment/Default value? `(x=1)` -> AssignmentExpression in group
                    else if (node is AssignmentExpression assign && assign.Left is NameExpression nameL)
                    {
                        args.Add(new ParameterDeclaration((byte)args.Count, nameL.Identifier, assign.Right));
                    }
                }
            }
            else if (left is NameExpression nameExp)
            {
                args.Add(new ParameterDeclaration(0, nameExp.Identifier, null));
            }

            // Ensure unique name
            var position = this.Lexer.LookAtHead();
            var nameStr = "lambda_" + position.LineNumber + "_" + position.ColumnNumber;
            var nameToken = new IdentifierToken() { Value = nameStr, LineNumber = position.LineNumber };

            // Parse Body
            // Lambda body can be Block or Expression.
            // `=> { ... }` or `=> expr`

            Statement bodyStmt = null;
            if (this.Lexer.LookAtHead().Symbol == Symbols.PT_LEFTBRACE)
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
                    // Need wrapping Block? FunctionDeclaration expects Body as BlockStatement?
                    // Original: "var declaration = new FunctionDeclaration(..., body, flags);"
                    // Ctor: public FunctionDeclaration(..., Statement body, ...)
                    // So single statement is fine?
                    // But `((BlockStatement)body).IsFunction = true;` implies it casts.
                    // So I better wrap it.
                    var block = new BlockStatement();
                    block.IsFunction = true;
                    block.AddNode(bodyStmt);
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
            if (node == null) return;
            if (string.IsNullOrEmpty(node.FileName))
            {
                node.Range.SetFileName(this.Lexer.FullPath);
            }
            foreach (var child in node.ChildNodes)
            {
                SetSourceRecursive(child);
            }
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
                foreach (var child in map.ChildNodes) FixRange(child, range);
            }
            else if (node is MapKeyValueExpression kv)
            {
                FixRange(kv.Value, range);
            }
        }
    }
}
