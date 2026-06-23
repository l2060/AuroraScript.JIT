using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Lowering
{
    internal static class FunctionLowerer
    {
        public static void LowerModule(ModulePlan modulePlan, IReadOnlyDictionary<FunctionDeclaration, FunctionPlan> functionsByDeclaration)
        {
            ArgumentNullException.ThrowIfNull(modulePlan);
            ArgumentNullException.ThrowIfNull(functionsByDeclaration);

            var directFunctionsBySymbol = BuildDirectFunctionMap(modulePlan, functionsByDeclaration);
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var lowerer = new FunctionLowererCore(modulePlan, modulePlan.Functions[i], functionsByDeclaration, directFunctionsBySymbol);
                var body = lowerer.LowerBody();
                modulePlan.Functions[i].Body = body;
                modulePlan.Functions[i].UnsupportedLoweredStatementCount = lowerer.UnsupportedStatementCount;
                modulePlan.Functions[i].UnsupportedLoweredExpressionCount = lowerer.UnsupportedExpressionCount;
                modulePlan.Functions[i].UnsupportedLoweredNodes = lowerer.UnsupportedNodes;
            }
        }

        private static Dictionary<SymbolId, FunctionId> BuildDirectFunctionMap(
            ModulePlan modulePlan,
            IReadOnlyDictionary<FunctionDeclaration, FunctionPlan> functionsByDeclaration)
        {
            Dictionary<SymbolId, FunctionId> map = null;
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var function = modulePlan.Functions[i];
                if (!function.IsDirectCallCandidate ||
                    function.Visibility != FunctionVisibility.InternalOnly ||
                    string.IsNullOrEmpty(function.Name) ||
                    !modulePlan.TryGetSymbol(function.Name, out var symbol))
                {
                    continue;
                }

                if (function.Declaration != null &&
                    functionsByDeclaration.TryGetValue(function.Declaration, out var resolved) &&
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
            private readonly IReadOnlyDictionary<FunctionDeclaration, FunctionPlan> _functionsByDeclaration;
            private readonly Dictionary<SymbolId, FunctionId> _directFunctionsBySymbol;
            private List<LoweredUnsupportedNode> _unsupportedNodes;

            public FunctionLowererCore(
                ModulePlan modulePlan,
                FunctionPlan function,
                IReadOnlyDictionary<FunctionDeclaration, FunctionPlan> functionsByDeclaration,
                Dictionary<SymbolId, FunctionId> directFunctionsBySymbol)
            {
                _modulePlan = modulePlan;
                _function = function;
                _functionsByDeclaration = functionsByDeclaration;
                _directFunctionsBySymbol = directFunctionsBySymbol;
            }

            public int UnsupportedStatementCount { get; private set; }
            public int UnsupportedExpressionCount { get; private set; }
            public LoweredUnsupportedNode[] UnsupportedNodes => _unsupportedNodes == null ? Array.Empty<LoweredUnsupportedNode>() : _unsupportedNodes.ToArray();

            public LoweredBlockStatement LowerBody()
            {
                _function.ParameterDefaults = LowerParameterDefaults();
                return LowerBlock(_function.Declaration?.Body);
            }

            private LoweredExpression[] LowerParameterDefaults()
            {
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

                var statements = new LoweredStatement[block.Length + block.Functions.Count];
                var index = 0;
                for (var i = 0; i < block.Functions.Count; i++)
                {
                    statements[index++] = LowerStatement(block.Functions[i]);
                }
                for (var i = 0; i < block.Length; i++)
                {
                    statements[index++] = LowerStatement(block[i]);
                }
                return new LoweredBlockStatement(block, statements);
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
                    TryStatement statement => new LoweredTryStatement(
                        statement,
                        LowerOptionalStatement(statement.Body),
                        statement.CatchVariable,
                        ResolveLocal(statement.CatchVariable),
                        LowerOptionalStatement(statement.CatchBody),
                        LowerOptionalStatement(statement.FinallyBody)),
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
                if (!_functionsByDeclaration.TryGetValue(declaration, out var function))
                {
                    return UnsupportedStatement(declaration);
                }

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
                    NameExpression name => LowerName(name),
                    BinaryExpression binary => new LoweredBinaryExpression(binary, LowerExpression(binary.Left), LowerExpression(binary.Right)),
                    AssignmentExpression assignment => new LoweredAssignmentExpression(assignment, LowerExpression(assignment.Left), LowerExpression(assignment.Right)),
                    CompoundExpression compound => new LoweredCompoundExpression(compound, LowerExpression(compound.Left), LowerExpression(compound.Right)),
                    UnaryExpression unary => new LoweredUnaryExpression(unary, LowerExpression(unary.Expression)),
                    InExpression inExpression => LowerIn(inExpression),
                    IncludedExpression included => LowerIncluded(included),
                    GetPropertyExpression property => new LoweredGetPropertyExpression(property, LowerExpression(property.Object), LowerExpression(property.Property)),
                    GetElementExpression element => new LoweredGetElementExpression(element, LowerExpression(element.Object), LowerExpression(element.Index)),
                    SetPropertyExpression property => new LoweredSetPropertyExpression(property, LowerExpression(property.Object), LowerExpression(property.Property), LowerExpression(property.Value)),
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

            private LoweredCallExpression LowerCall(FunctionCallExpression call)
            {
                var arguments = new LoweredExpression[call.Arguments.Count];
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
                var elements = LowerChildExpressions(expression);
                return new LoweredArrayLiteralExpression(expression, elements);
            }

            private LoweredMapExpression LowerMap(MapExpression expression)
            {
                if (expression.Length == 0)
                {
                    return new LoweredMapExpression(expression, Array.Empty<LoweredMapEntry>());
                }

                var entries = new LoweredMapEntry[expression.Length];
                for (var i = 0; i < expression.Length; i++)
                {
                    if (expression[i] is MapKeyValueExpression entry)
                    {
                        entries[i] = new LoweredMapEntry(entry.Key, LowerExpression(entry.Value), entry.Range);
                    }
                    else if (expression[i] is Expression value)
                    {
                        entries[i] = new LoweredMapEntry(null, LowerExpression(value), value.Range);
                    }
                    else
                    {
                        entries[i] = new LoweredMapEntry(null, UnsupportedExpression(null), expression[i]?.Range ?? SourceSpan.None);
                    }
                }

                return new LoweredMapExpression(expression, entries);
            }

            private LoweredExpression[] LowerChildExpressions(AstNode node)
            {
                if (node.Length == 0)
                {
                    return Array.Empty<LoweredExpression>();
                }

                var expressions = new LoweredExpression[node.Length];
                for (var i = 0; i < node.Length; i++)
                {
                    expressions[i] = node[i] is Expression expression
                        ? LowerExpression(expression)
                        : UnsupportedExpression(null);
                }
                return expressions;
            }

            private LoweredExpression LowerLambda(LambdaExpression lambda)
            {
                if (lambda.Function != null && _functionsByDeclaration.TryGetValue(lambda.Function, out var function))
                {
                    return new LoweredLambdaExpression(lambda, function.Id);
                }

                return UnsupportedExpression(lambda);
            }

            private LoweredNameExpression LowerName(NameExpression name)
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
                return new LoweredNameExpression(name, local, upvalue, moduleSymbol);
            }

            private LocalSlotId ResolveLocal(string name)
            {
                if (name == null)
                {
                    return LocalSlotId.Invalid;
                }

                var locals = _function.LocalSlots;
                for (var i = 0; i < locals.Length; i++)
                {
                    if (locals[i].Name == name)
                    {
                        return locals[i].Id;
                    }
                }

                return LocalSlotId.Invalid;
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
