using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Analysis;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Lowering
{
    internal static class FunctionLowerer
    {
        public static void LowerModule(ModulePlan modulePlan, FunctionBinder.FunctionPlanRegistry functions)
        {
            ArgumentNullException.ThrowIfNull(modulePlan);
            ArgumentNullException.ThrowIfNull(functions);

            var directFunctionsBySymbol = HasDirectCallCandidate(modulePlan)
                ? BuildDirectFunctionMap(modulePlan, functions)
                : null;
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var lowerer = new FunctionLowererCore(modulePlan, modulePlan.Functions[i], functions, directFunctionsBySymbol);
                var body = lowerer.LowerBody();
                modulePlan.Functions[i].Body = body;
                modulePlan.Functions[i].UnsupportedLoweredStatementCount = lowerer.UnsupportedStatementCount;
                modulePlan.Functions[i].UnsupportedLoweredExpressionCount = lowerer.UnsupportedExpressionCount;
                modulePlan.Functions[i].UnsupportedLoweredNodes = lowerer.UnsupportedNodes;
            }
        }

        private static bool HasDirectCallCandidate(ModulePlan modulePlan)
        {
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                if (modulePlan.Functions[i].IsDirectCallCandidate)
                {
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<SymbolId, FunctionId> BuildDirectFunctionMap(
            ModulePlan modulePlan,
            FunctionBinder.FunctionPlanRegistry functions)
        {
            Dictionary<SymbolId, FunctionId> map = null;
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var function = modulePlan.Functions[i];
                if (!function.IsDirectCallCandidate ||
                    string.IsNullOrEmpty(function.Name) ||
                    !modulePlan.TryGetSymbol(function.Name, out var symbol))
                {
                    continue;
                }

                if (function.Declaration != null &&
                    functions.TryGetValue(function.Declaration, out var resolved) &&
                    resolved.Id.Equals(function.Id))
                {
                    map ??= new Dictionary<SymbolId, FunctionId>();
                    map[symbol] = function.Id;
                }
            }

            return map;
        }

        private sealed class FunctionLowererCore
        {
            private readonly ModulePlan _modulePlan;
            private readonly FunctionPlan _function;
            private readonly FunctionBinder.FunctionPlanRegistry _functions;
            private readonly Dictionary<SymbolId, FunctionId> _directFunctionsBySymbol;
            private Stack<int> _scopeStack;
            private List<LoweredUnsupportedNode> _unsupportedNodes;

            public FunctionLowererCore(
                ModulePlan modulePlan,
                FunctionPlan function,
                FunctionBinder.FunctionPlanRegistry functions,
                Dictionary<SymbolId, FunctionId> directFunctionsBySymbol)
            {
                _modulePlan = modulePlan;
                _function = function;
                _functions = functions;
                _directFunctionsBySymbol = directFunctionsBySymbol;
                _scopeStack = null;
            }

            public int UnsupportedStatementCount { get; private set; }
            public int UnsupportedExpressionCount { get; private set; }
            public LoweredUnsupportedNode[] UnsupportedNodes => _unsupportedNodes == null ? Array.Empty<LoweredUnsupportedNode>() : _unsupportedNodes.ToArray();

            public LoweredBlockStatement LowerBody()
            {
                _scopeStack = new Stack<int>();
                _scopeStack.Push(GetScopeId(_function.Declaration?.Body ?? _function.Declaration, 0));
                _function.ParameterDefaults = LowerParameterDefaults();
                try
                {
                    return LowerBlock(_function.Declaration?.Body);
                }
                finally
                {
                    _scopeStack.Pop();
                }
            }

            private LoweredExpression[] LowerParameterDefaults()
            {
                if (!_function.HasDefaultParameters)
                {
                    return Array.Empty<LoweredExpression>();
                }

                var parameters = _function.Declaration?.Parameters;
                if (parameters == null || parameters.Count == 0)
                {
                    return Array.Empty<LoweredExpression>();
                }

                LoweredExpression[] defaults = null;
                for (var i = 0; i < parameters.Count; i++)
                {
                    var initializer = parameters[i].Initializer;
                    if (initializer == null)
                    {
                        continue;
                    }

                    defaults ??= new LoweredExpression[parameters.Count];
                    defaults[i] = LowerExpression(initializer);
                }

                return defaults ?? Array.Empty<LoweredExpression>();
            }

            private LoweredBlockStatement LowerBlock(AstNode node)
            {
                if (node is not BlockStatement block)
                {
                    return new LoweredBlockStatement(node, node == null ? Array.Empty<LoweredStatement>() : new[] { LowerStatement(node) });
                }

                return WithNodeScope(block, () =>
                {
                    var statementCount = block.Statements.Count + block.Functions.Count;
                    if (statementCount == 0)
                    {
                        return new LoweredBlockStatement(block, Array.Empty<LoweredStatement>());
                    }

                    var statements = new LoweredStatement[statementCount];
                    var index = 0;
                    for (var i = 0; i < block.Functions.Count; i++)
                    {
                        statements[index++] = LowerStatement(block.Functions[i]);
                    }
                    for (var i = 0; i < block.Statements.Count; i++)
                    {
                        statements[index++] = LowerStatement(block.Statements[i]);
                    }
                    return new LoweredBlockStatement(block, statements);
                });
            }

            private LoweredStatement LowerStatement(AstNode node)
            {
                return node switch
                {
                    BlockStatement block => LowerBlock(block),
                    ReturnStatement statement => new LoweredReturnStatement(statement, LowerExpression(statement.Expression)),
                    ExpressionStatement statement => new LoweredExpressionStatement(statement, LowerExpression(statement.Expression)),
                    VariableDeclaration declaration => LowerVariableDeclaration(declaration),
                    FunctionDeclaration declaration => LowerFunctionDeclaration(declaration),
                    IfStatement statement => new LoweredIfStatement(
                        statement,
                        LowerExpression(statement.Condition),
                        LowerOptionalStatement(statement.Body),
                        LowerOptionalStatement(statement.Else)),
                    WhileStatement statement => new LoweredWhileStatement(
                        statement,
                        LowerExpression(statement.Condition),
                        LowerOptionalStatement(statement.Body)),
                    ForStatement statement => new LoweredForStatement(
                        statement,
                        LowerForInitializer(statement.Initializer),
                        LowerExpression(statement.Condition),
                        LowerExpression(statement.Incrementor),
                        LowerOptionalStatement(statement.Body)),
                    ForInStatement statement => new LoweredForInStatement(
                        statement,
                        LowerForInInitializer(statement.Initializer),
                        LowerIn(statement.Iterator),
                        LowerOptionalStatement(statement.Body)),
                    TryStatement statement => LowerTry(statement),
                    ThrowStatement statement => new LoweredThrowStatement(statement, LowerExpression(statement.Expression)),
                    DeleteStatement statement => new LoweredDeleteStatement(statement, LowerExpression(statement.Expression)),
                    DebuggerStatement statement => new LoweredDebuggerStatement(statement),
                    BreakStatement statement => new LoweredBreakStatement(statement),
                    ContinueStatement statement => new LoweredContinueStatement(statement),
                    _ => UnsupportedStatement(node)
                };
            }

            private LoweredStatement LowerOptionalStatement(AstNode node)
            {
                return node == null ? null : LowerStatement(node);
            }

            private LoweredStatement LowerForInitializer(AstNode node)
            {
                return node switch
                {
                    null => null,
                    Expression expression => new LoweredExpressionStatement(node, LowerExpression(expression)),
                    _ => LowerStatement(node)
                };
            }

            private LoweredStatement LowerForInInitializer(VariableDeclaration declaration)
            {
                return declaration == null ? null : LowerVariableDeclaration(declaration);
            }

            private LoweredStatement LowerTry(TryStatement statement)
            {
                var body = LowerOptionalStatement(statement.Body);
                var catchSlot = LocalSlotId.Invalid;
                LoweredStatement catchBody = null;
                if (string.IsNullOrEmpty(statement.CatchVariable) || statement.CatchBody == null)
                {
                    catchSlot = LocalSlotId.Invalid;
                    catchBody = LowerOptionalStatement(statement.CatchBody);
                }
                else
                {
                    var owner = statement.CatchBody is BlockStatement block ? (AstNode)block : statement;
                    WithNodeScope(owner, () =>
                    {
                        catchSlot = ResolveLocal(statement.CatchVariable);
                        catchBody = statement.CatchBody is BlockStatement catchBlock
                            ? LowerBlockWithoutScope(catchBlock)
                            : LowerStatement(statement.CatchBody);
                        return 0;
                    });
                }

                return new LoweredTryStatement(
                    statement,
                    body,
                    statement.CatchVariable,
                    catchSlot,
                    catchBody,
                    LowerOptionalStatement(statement.FinallyBody));
            }

            private LoweredStatement LowerVariableDeclaration(VariableDeclaration declaration)
            {
                if (declaration.Name != null)
                {
                    return new LoweredVariableDeclarationStatement(
                        declaration,
                        ResolveLocal(declaration.Name.Value),
                        LowerExpression(declaration.Initializer));
                }

                return declaration.Pattern switch
                {
                    ObjectDestructuringPattern objectPattern => LowerObjectDestructuringDeclaration(declaration, objectPattern),
                    ArrayDestructuringPattern arrayPattern => LowerArrayDestructuringDeclaration(declaration, arrayPattern),
                    _ => UnsupportedStatement(declaration)
                };
            }

            private LoweredStatement LowerObjectDestructuringDeclaration(
                VariableDeclaration declaration,
                ObjectDestructuringPattern pattern)
            {
                var bindings = new LoweredObjectDestructuringBinding[pattern.Properties.Count];
                for (var i = 0; i < pattern.Properties.Count; i++)
                {
                    var property = pattern.Properties[i];
                    bindings[i] = new LoweredObjectDestructuringBinding(property, ResolveLocal(property.Value));
                }

                return new LoweredObjectDestructuringDeclarationStatement(
                    declaration,
                    LowerExpression(declaration.Initializer),
                    bindings);
            }

            private LoweredStatement LowerArrayDestructuringDeclaration(
                VariableDeclaration declaration,
                ArrayDestructuringPattern pattern)
            {
                var bindings = new List<LoweredArrayDestructuringBinding>(pattern.Elements.Count);
                var restIndex = -1;
                for (var i = 0; i < pattern.Elements.Count; i++)
                {
                    if (pattern.Elements[i] is SpreadExpression)
                    {
                        restIndex = i;
                        break;
                    }
                }

                var trailingCount = restIndex >= 0 ? pattern.Elements.Count - restIndex - 1 : 0;
                for (var i = 0; i < pattern.Elements.Count; i++)
                {
                    var element = pattern.Elements[i];
                    if (element == null)
                    {
                        continue;
                    }

                    if (element is NameExpression name)
                    {
                        bindings.Add(new LoweredArrayDestructuringBinding(
                            ResolveLocal(name.Identifier.Value),
                            i,
                            isRest: false,
                            trailingCount: restIndex >= 0 && i > restIndex ? pattern.Elements.Count - i : 0));
                        continue;
                    }

                    if (element is SpreadExpression { Expression: NameExpression spreadName })
                    {
                        bindings.Add(new LoweredArrayDestructuringBinding(
                            ResolveLocal(spreadName.Identifier.Value),
                            i,
                            isRest: true,
                            trailingCount));
                        continue;
                    }

                    return UnsupportedStatement(declaration);
                }

                return new LoweredArrayDestructuringDeclarationStatement(
                    declaration,
                    LowerExpression(declaration.Initializer),
                    bindings.ToArray());
            }

            private LoweredStatement LowerFunctionDeclaration(FunctionDeclaration declaration)
            {
                if (!_functions.TryGetValue(declaration, out var function))
                {
                    return UnsupportedStatement(declaration);
                }

                function.ParentLocalScopeId = CurrentScopeId;
                var localSlot = declaration.Name == null ? LocalSlotId.Invalid : ResolveLocal(declaration.Name.Value);
                return new LoweredFunctionDeclarationStatement(declaration, function.Id, localSlot);
            }

            private LoweredExpression LowerExpression(Expression expression)
            {
                return expression switch
                {
                    null => null,
                    GroupExpression group => LowerExpression(group.Expression),
                    LiteralExpression literal => new LoweredLiteralExpression(literal),
                    TemplateStringExpression template => LowerTemplateString(template),
                    NameExpression name => LowerName(name),
                    BinaryExpression binary => new LoweredBinaryExpression(binary, LowerExpression(binary.Left), LowerExpression(binary.Right)),
                    AssignmentExpression assignment => new LoweredAssignmentExpression(assignment, LowerAssignmentTarget(assignment.Left), LowerExpression(assignment.Right)),
                    CompoundExpression compound => new LoweredCompoundExpression(compound, LowerAssignmentTarget(compound.Left), LowerExpression(compound.Right)),
                    UnaryExpression unary => new LoweredUnaryExpression(unary, LowerUnaryOperand(unary)),
                    InExpression inExpression => LowerIn(inExpression),
                    IncludedExpression included => LowerIncluded(included),
                    GetPropertyExpression property => new LoweredGetPropertyExpression(property, LowerExpression(property.Object), LowerPropertyName(property.Property)),
                    GetElementExpression element => new LoweredGetElementExpression(element, LowerExpression(element.Object), LowerExpression(element.Index)),
                    SetPropertyExpression property => new LoweredSetPropertyExpression(property, LowerExpression(property.Object), LowerPropertyName(property.Property), LowerExpression(property.Value)),
                    SetElementExpression element => new LoweredSetElementExpression(element, LowerExpression(element.Object), LowerExpression(element.Index), LowerExpression(element.Value)),
                    ArrayLiteralExpression array => LowerArrayLiteral(array),
                    MapExpression map => LowerMap(map),
                    SpreadExpression spread => new LoweredSpreadExpression(spread, LowerExpression(spread.Expression)),
                    NewExpression newExpression => new LoweredNewExpression(newExpression, LowerCall(newExpression.Expression)),
                    FunctionCallExpression call => LowerCall(call),
                    LambdaExpression lambda => LowerLambda(lambda),
                    _ => UnsupportedExpression(expression)
                };
            }

            private LoweredExpression LowerTemplateString(TemplateStringExpression expression)
            {
                var parts = new LoweredTemplateStringPart[expression.PartCount];
                for (var i = 0; i < expression.PartCount; i++)
                {
                    var part = expression.Parts[i];
                    parts[i] = part.IsLiteral
                        ? new LoweredTemplateStringPart(part.Literal)
                        : new LoweredTemplateStringPart(LowerExpression(part.Expression));
                }

                return new LoweredTemplateStringExpression(expression, parts);
            }

            private LoweredExpression LowerAssignmentTarget(Expression expression)
            {
                return expression is NameExpression name
                    ? LowerName(name, allowInline: false)
                    : LowerExpression(expression);
            }

            private LoweredExpression LowerUnaryOperand(UnaryExpression unary)
            {
                if (IsIncrementOrDecrement(unary.Operator) && unary.Expression is NameExpression name)
                {
                    return LowerName(name, allowInline: false);
                }

                return LowerExpression(unary.Expression);
            }

            private LoweredExpression LowerPropertyName(Expression expression)
            {
                return expression is NameExpression name
                    ? new LoweredNameExpression(name, LocalSlotId.Invalid, UpvalueSlotId.Invalid, SymbolId.Invalid)
                    : LowerExpression(expression);
            }

            private LoweredCallExpression LowerCall(FunctionCallExpression call)
            {
                var arguments = call.Arguments.Count == 0
                    ? Array.Empty<LoweredExpression>()
                    : new LoweredExpression[call.Arguments.Count];
                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    arguments[i] = LowerExpression(call.Arguments[i]);
                }

                var target = LowerExpression(call.Target);
                return new LoweredCallExpression(call, target, arguments, ResolveDirectFunction(target));
            }

            private FunctionId ResolveDirectFunction(LoweredExpression target)
            {
                return target is LoweredNameExpression name &&
                    name.ModuleSymbol.IsValid &&
                    _directFunctionsBySymbol != null &&
                    _directFunctionsBySymbol.TryGetValue(name.ModuleSymbol, out var function)
                    ? function
                    : FunctionId.Invalid;
            }

            private LoweredInExpression LowerIn(InExpression expression)
            {
                return expression == null ? null : new LoweredInExpression(expression, LowerName(expression.Left), LowerExpression(expression.Right));
            }

            private LoweredInExpression LowerIncluded(IncludedExpression expression)
            {
                return expression == null ? null : new LoweredInExpression(expression, LowerExpression(expression.Left), LowerExpression(expression.Right));
            }

            private LoweredArrayLiteralExpression LowerArrayLiteral(ArrayLiteralExpression expression)
            {
                if (expression.Elements.Count == 0)
                {
                    return new LoweredArrayLiteralExpression(expression, Array.Empty<LoweredExpression>());
                }

                var elements = new LoweredExpression[expression.Elements.Count];
                for (var i = 0; i < expression.Elements.Count; i++)
                {
                    elements[i] = LowerExpression(expression.Elements[i]);
                }
                return new LoweredArrayLiteralExpression(expression, elements);
            }

            private LoweredMapExpression LowerMap(MapExpression expression)
            {
                if (expression.Entries.Count == 0)
                {
                    return new LoweredMapExpression(expression, Array.Empty<LoweredMapEntry>());
                }

                var entries = new LoweredMapEntry[expression.Entries.Count];
                for (var i = 0; i < expression.Entries.Count; i++)
                {
                    if (expression.Entries[i] is MapKeyValueExpression entry)
                    {
                        entries[i] = new LoweredMapEntry(entry.Key, LowerExpression(entry.Value), entry.Range);
                    }
                    else if (expression.Entries[i] is Expression value)
                    {
                        entries[i] = new LoweredMapEntry(null, LowerExpression(value), value.Range);
                    }
                    else
                    {
                        entries[i] = new LoweredMapEntry(null, UnsupportedExpression(null), expression.Entries[i]?.Range ?? SourceSpan.None);
                    }
                }

                return new LoweredMapExpression(expression, entries);
            }

            private LoweredExpression LowerLambda(LambdaExpression lambda)
            {
                if (lambda.Function != null && _functions.TryGetValue(lambda.Function, out var function))
                {
                    return new LoweredLambdaExpression(lambda, function.Id);
                }

                return UnsupportedExpression(lambda);
            }

            private LoweredExpression LowerName(NameExpression name, bool allowInline = true)
            {
                if (name == null)
                {
                    return new LoweredNameExpression(null, LocalSlotId.Invalid, UpvalueSlotId.Invalid, SymbolId.Invalid);
                }

                var value = name.Identifier?.Value;
                if (value == null)
                {
                    return new LoweredNameExpression(name, LocalSlotId.Invalid, UpvalueSlotId.Invalid, SymbolId.Invalid);
                }

                var local = ResolveLocal(value);
                var upvalue = local.IsValid ? UpvalueSlotId.Invalid : ResolveUpvalue(value);
                var moduleSymbol = local.IsValid || upvalue.IsValid || !_modulePlan.TryGetSymbol(value, out var symbol)
                    ? SymbolId.Invalid
                    : symbol;
                if (allowInline &&
                    moduleSymbol.IsValid &&
                    _modulePlan.TryGetInlineConstant(moduleSymbol, out var constant))
                {
                    var literal = ModuleConstInliningAnalyzer.CreateLiteralExpression(constant, name.Range);
                    return new LoweredLiteralExpression(literal);
                }

                return new LoweredNameExpression(name, local, upvalue, moduleSymbol);
            }

            private static bool IsIncrementOrDecrement(Operator op)
            {
                return op == Operator.PreIncrement ||
                    op == Operator.PostIncrement ||
                    op == Operator.PreDecrement ||
                    op == Operator.PostDecrement;
            }

            private LocalSlotId ResolveLocal(string name)
            {
                if (name == null)
                {
                    return LocalSlotId.Invalid;
                }

                var locals = _function.LocalSlots;
                var scopeId = CurrentScopeId;
                while (scopeId >= 0)
                {
                    for (var i = locals.Length - 1; i >= 0; i--)
                    {
                        if (locals[i].ScopeId == scopeId && locals[i].Name == name)
                        {
                            return locals[i].Id;
                        }
                    }

                    scopeId = GetParentScopeId(scopeId);
                }

                return LocalSlotId.Invalid;
            }

            private int CurrentScopeId => _scopeStack != null && _scopeStack.Count != 0 ? _scopeStack.Peek() : 0;

            private int GetScopeId(AstNode node, int fallback)
            {
                return node != null &&
                    _function.LocalScopeByNode != null &&
                    _function.LocalScopeByNode.TryGetValue(node, out var scopeId)
                    ? scopeId
                    : fallback;
            }

            private int GetParentScopeId(int scopeId)
            {
                var scopes = _function.LocalScopes;
                return (uint)scopeId < (uint)scopes.Length ? scopes[scopeId].ParentId : -1;
            }

            private T WithNodeScope<T>(AstNode node, Func<T> action)
            {
                var scopeId = GetScopeId(node, CurrentScopeId);
                if (scopeId == CurrentScopeId)
                {
                    return action();
                }

                _scopeStack.Push(scopeId);
                try
                {
                    return action();
                }
                finally
                {
                    _scopeStack.Pop();
                }
            }

            private LoweredBlockStatement LowerBlockWithoutScope(BlockStatement block)
            {
                var statementCount = block.Statements.Count + block.Functions.Count;
                if (statementCount == 0)
                {
                    return new LoweredBlockStatement(block, Array.Empty<LoweredStatement>());
                }

                var statements = new LoweredStatement[statementCount];
                var index = 0;
                for (var i = 0; i < block.Functions.Count; i++)
                {
                    statements[index++] = LowerStatement(block.Functions[i]);
                }
                for (var i = 0; i < block.Statements.Count; i++)
                {
                    statements[index++] = LowerStatement(block.Statements[i]);
                }
                return new LoweredBlockStatement(block, statements);
            }

            private UpvalueSlotId ResolveUpvalue(string name)
            {
                if (name == null)
                {
                    return UpvalueSlotId.Invalid;
                }

                var upvalues = _function.UpvalueSlots;
                for (var i = 0; i < upvalues.Length; i++)
                {
                    if (upvalues[i].Name == name)
                    {
                        return upvalues[i].Id;
                    }
                }

                return UpvalueSlotId.Invalid;
            }

            private LoweredUnsupportedStatement UnsupportedStatement(AstNode source)
            {
                UnsupportedStatementCount++;
                AddUnsupported(source, isExpression: false);
                return new LoweredUnsupportedStatement(source);
            }

            private LoweredUnsupportedExpression UnsupportedExpression(Expression source)
            {
                UnsupportedExpressionCount++;
                AddUnsupported(source, isExpression: true);
                return new LoweredUnsupportedExpression(source);
            }

            private void AddUnsupported(AstNode source, bool isExpression)
            {
                _unsupportedNodes ??= new List<LoweredUnsupportedNode>();
                _unsupportedNodes.Add(new LoweredUnsupportedNode(source?.GetType().Name ?? "<null>", source?.Range ?? SourceSpan.None, isExpression));
            }
        }
    }
}
