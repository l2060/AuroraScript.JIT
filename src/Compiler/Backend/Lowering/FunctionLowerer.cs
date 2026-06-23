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

            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var lowerer = new FunctionLowererCore(modulePlan, modulePlan.Functions[i], functionsByDeclaration);
                var body = lowerer.LowerBody();
                modulePlan.Functions[i].Body = body;
                var counter = new UnsupportedCounter();
                counter.Count(modulePlan.Functions[i].ParameterDefaults);
                counter.Count(body);
                modulePlan.Functions[i].UnsupportedLoweredStatementCount = counter.StatementCount;
                modulePlan.Functions[i].UnsupportedLoweredExpressionCount = counter.ExpressionCount;
                modulePlan.Functions[i].UnsupportedLoweredNodes = counter.ToArray();
            }
        }

        private sealed class FunctionLowererCore
        {
            private readonly ModulePlan _modulePlan;
            private readonly FunctionPlan _function;
            private readonly IReadOnlyDictionary<FunctionDeclaration, FunctionPlan> _functionsByDeclaration;
            private readonly Dictionary<SymbolId, FunctionId> _directFunctionsBySymbol;
            private readonly Dictionary<string, LocalSlotId> _locals = new(StringComparer.Ordinal);
            private readonly Dictionary<string, UpvalueSlotId> _upvalues = new(StringComparer.Ordinal);

            public FunctionLowererCore(
                ModulePlan modulePlan,
                FunctionPlan function,
                IReadOnlyDictionary<FunctionDeclaration, FunctionPlan> functionsByDeclaration)
            {
                _modulePlan = modulePlan;
                _function = function;
                _functionsByDeclaration = functionsByDeclaration;
                _directFunctionsBySymbol = BuildDirectFunctionMap(modulePlan, functionsByDeclaration);

                for (var i = 0; i < function.LocalSlots.Length; i++)
                {
                    _locals.TryAdd(function.LocalSlots[i].Name, function.LocalSlots[i].Id);
                }
                for (var i = 0; i < function.UpvalueSlots.Length; i++)
                {
                    _upvalues.TryAdd(function.UpvalueSlots[i].Name, function.UpvalueSlots[i].Id);
                }
            }

            private static Dictionary<SymbolId, FunctionId> BuildDirectFunctionMap(
                ModulePlan modulePlan,
                IReadOnlyDictionary<FunctionDeclaration, FunctionPlan> functionsByDeclaration)
            {
                var map = new Dictionary<SymbolId, FunctionId>();
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
                        map[symbol] = function.Id;
                    }
                }

                return map;
            }

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

                var statements = new List<LoweredStatement>(block.Length + block.Functions.Count);
                for (var i = 0; i < block.Functions.Count; i++)
                {
                    statements.Add(LowerStatement(block.Functions[i]));
                }
                for (var i = 0; i < block.Length; i++)
                {
                    statements.Add(LowerStatement(block[i]));
                }
                return new LoweredBlockStatement(block, statements.ToArray());
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
                    _ => new LoweredUnsupportedStatement(node)
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
                    _ => new LoweredUnsupportedStatement(declaration)
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

                    return new LoweredUnsupportedStatement(declaration);
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
                    return new LoweredUnsupportedStatement(declaration);
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
                    _ => new LoweredUnsupportedExpression(expression)
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
                        entries[i] = new LoweredMapEntry(null, new LoweredUnsupportedExpression(null), expression[i]?.Range ?? SourceSpan.None);
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
                        : new LoweredUnsupportedExpression(null);
                }
                return expressions;
            }

            private LoweredExpression LowerLambda(LambdaExpression lambda)
            {
                if (lambda.Function != null && _functionsByDeclaration.TryGetValue(lambda.Function, out var function))
                {
                    return new LoweredLambdaExpression(lambda, function.Id);
                }

                return new LoweredUnsupportedExpression(lambda);
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
                return name != null && _locals.TryGetValue(name, out var slot) ? slot : LocalSlotId.Invalid;
            }

            private UpvalueSlotId ResolveUpvalue(string name)
            {
                return name != null && _upvalues.TryGetValue(name, out var slot) ? slot : UpvalueSlotId.Invalid;
            }
        }

        private sealed class UnsupportedCounter
        {
            public int StatementCount { get; private set; }
            public int ExpressionCount { get; private set; }
            private List<LoweredUnsupportedNode> _nodes;

            public LoweredUnsupportedNode[] ToArray()
            {
                return _nodes == null ? Array.Empty<LoweredUnsupportedNode>() : _nodes.ToArray();
            }

            public void Count(LoweredStatement statement)
            {
                switch (statement)
                {
                    case null:
                        return;
                    case LoweredUnsupportedStatement unsupported:
                        StatementCount++;
                        Add(unsupported.Source, isExpression: false);
                        return;
                    case LoweredBlockStatement block:
                        for (var i = 0; i < block.Statements.Length; i++)
                        {
                            Count(block.Statements[i]);
                        }
                        return;
                    case LoweredExpressionStatement expressionStatement:
                        Count(expressionStatement.Expression);
                        return;
                    case LoweredReturnStatement returnStatement:
                        Count(returnStatement.Expression);
                        return;
                    case LoweredVariableDeclarationStatement variable:
                        Count(variable.Initializer);
                        return;
                    case LoweredObjectDestructuringDeclarationStatement objectDestructuring:
                        Count(objectDestructuring.Initializer);
                        return;
                    case LoweredArrayDestructuringDeclarationStatement arrayDestructuring:
                        Count(arrayDestructuring.Initializer);
                        return;
                    case LoweredIfStatement ifStatement:
                        Count(ifStatement.Condition);
                        Count(ifStatement.Body);
                        Count(ifStatement.Else);
                        return;
                    case LoweredWhileStatement whileStatement:
                        Count(whileStatement.Condition);
                        Count(whileStatement.Body);
                        return;
                    case LoweredForStatement forStatement:
                        Count(forStatement.Initializer);
                        Count(forStatement.Condition);
                        Count(forStatement.Incrementor);
                        Count(forStatement.Body);
                        return;
                    case LoweredForInStatement forInStatement:
                        Count(forInStatement.Initializer);
                        Count(forInStatement.Iterator);
                        Count(forInStatement.Body);
                        return;
                    case LoweredTryStatement tryStatement:
                        Count(tryStatement.Body);
                        Count(tryStatement.CatchBody);
                        Count(tryStatement.FinallyBody);
                        return;
                    case LoweredThrowStatement throwStatement:
                        Count(throwStatement.Expression);
                        return;
                    case LoweredDeleteStatement deleteStatement:
                        Count(deleteStatement.Expression);
                        return;
                    case LoweredDebuggerStatement:
                    case LoweredBreakStatement:
                    case LoweredContinueStatement:
                        return;
                }
            }

            public void Count(LoweredExpression[] expressions)
            {
                if (expressions == null)
                {
                    return;
                }

                for (var i = 0; i < expressions.Length; i++)
                {
                    Count(expressions[i]);
                }
            }

            private void Count(LoweredExpression expression)
            {
                switch (expression)
                {
                    case null:
                    case LoweredLiteralExpression:
                    case LoweredNameExpression:
                    case LoweredLambdaExpression:
                        return;
                    case LoweredUnsupportedExpression unsupported:
                        ExpressionCount++;
                        Add(unsupported.Source, isExpression: true);
                        return;
                    case LoweredBinaryExpression binary:
                        Count(binary.Left);
                        Count(binary.Right);
                        return;
                    case LoweredCallExpression call:
                        Count(call.Target);
                        for (var i = 0; i < call.Arguments.Length; i++)
                        {
                            Count(call.Arguments[i]);
                        }
                        return;
                    case LoweredAssignmentExpression assignment:
                        Count(assignment.Left);
                        Count(assignment.Right);
                        return;
                    case LoweredCompoundExpression compound:
                        Count(compound.Left);
                        Count(compound.Right);
                        return;
                    case LoweredUnaryExpression unary:
                        Count(unary.Expression);
                        return;
                    case LoweredInExpression inExpression:
                        Count(inExpression.Left);
                        Count(inExpression.Right);
                        return;
                    case LoweredGetPropertyExpression property:
                        Count(property.Instance);
                        Count(property.Property);
                        return;
                    case LoweredGetElementExpression element:
                        Count(element.Instance);
                        Count(element.Index);
                        return;
                    case LoweredSetPropertyExpression property:
                        Count(property.Instance);
                        Count(property.Property);
                        Count(property.Value);
                        return;
                    case LoweredSetElementExpression element:
                        Count(element.Instance);
                        Count(element.Index);
                        Count(element.Value);
                        return;
                    case LoweredArrayLiteralExpression array:
                        for (var i = 0; i < array.Elements.Length; i++)
                        {
                            Count(array.Elements[i]);
                        }
                        return;
                    case LoweredMapExpression map:
                        for (var i = 0; i < map.Entries.Length; i++)
                        {
                            Count(map.Entries[i].Value);
                        }
                        return;
                    case LoweredSpreadExpression spread:
                        Count(spread.Expression);
                        return;
                    case LoweredNewExpression @new:
                        Count(@new.Expression);
                        return;
                }
            }

            private void Add(AstNode source, bool isExpression)
            {
                _nodes ??= new List<LoweredUnsupportedNode>();
                _nodes.Add(new LoweredUnsupportedNode(source?.GetType().Name ?? "<null>", source?.Range ?? SourceSpan.None, isExpression));
            }
        }
    }
}
