using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Code
{
    internal static class TypedFunctionBuilder
    {
        public static TypedFunctionCode Build(
            ModulePlan module,
            FunctionPlan function,
            FlowValueType[] parameterTypes = null,
            IReadOnlyDictionary<FunctionId, FlowValueType> directReturnTypes = null,
            FlowValueType[][] directParameterTypes = null)
        {
            ArgumentNullException.ThrowIfNull(module);
            ArgumentNullException.ThrowIfNull(function);

            var binder = new NameBinder(module, function);
            binder.Bind();
            var analyzer = new TypeAnalyzer(
                function,
                binder.Names,
                binder.Declarations,
                parameterTypes,
                directReturnTypes,
                directParameterTypes);
            return analyzer.Analyze();
        }

        private sealed class NameBinder
        {
            private readonly ModulePlan _module;
            private readonly FunctionPlan _function;
            private readonly Stack<int> _scopes = new();
            private readonly Dictionary<SymbolId, FunctionId> _directFunctions;

            public NameBinder(ModulePlan module, FunctionPlan function)
            {
                _module = module;
                _function = function;
                Names = new Dictionary<NameExpression, BoundName>(ReferenceEqualityComparer.Instance);
                Declarations = new Dictionary<VariableDeclaration, LocalSlotId>(ReferenceEqualityComparer.Instance);
                _directFunctions = BuildDirectFunctionMap(module);

                for (var i = 0; i < function.LocalSlots.Length; i++)
                {
                    if (function.LocalSlots[i].Declaration is VariableDeclaration declaration)
                    {
                        Declarations.TryAdd(declaration, function.LocalSlots[i].Id);
                    }
                }
            }

            public Dictionary<NameExpression, BoundName> Names { get; }
            public Dictionary<VariableDeclaration, LocalSlotId> Declarations { get; }

            public void Bind()
            {
                var declaration = _function.Declaration;
                if (declaration == null)
                {
                    return;
                }

                _scopes.Push(GetScopeId(declaration.Body ?? declaration, 0));
                try
                {
                    for (var i = 0; i < declaration.Parameters.Count; i++)
                    {
                        BindExpression(declaration.Parameters[i].Initializer);
                    }
                    BindNode(declaration.Body);
                }
                finally
                {
                    _scopes.Pop();
                }
            }

            private void BindNode(AstNode node)
            {
                switch (node)
                {
                    case null:
                        return;
                    case Statement statement:
                        BindStatement(statement);
                        return;
                    case Expression expression:
                        BindExpression(expression);
                        return;
                }
            }

            private void BindStatement(Statement statement)
            {
                var pushed = EnterScope(statement);
                try
                {
                    switch (statement)
                    {
                        case BlockStatement block:
                            for (var i = 0; i < block.Functions.Count; i++) BindStatement(block.Functions[i]);
                            for (var i = 0; i < block.Statements.Count; i++) BindStatement(block.Statements[i]);
                            break;
                        case VariableDeclaration variable:
                            BindExpression(variable.Pattern);
                            BindExpression(variable.Initializer);
                            break;
                        case FunctionDeclaration nested:
                            // Nested bodies are compiled as their own FunctionPlan.
                            if (nested.Flags == FunctionFlags.Declare)
                            {
                                BindNode(nested.Body);
                            }
                            break;
                        case ExpressionStatement expression:
                            BindExpression(expression.Expression);
                            break;
                        case ReturnStatement @return:
                            BindExpression(@return.Expression);
                            break;
                        case IfStatement @if:
                            BindExpression(@if.Condition);
                            BindNode(@if.Body);
                            BindNode(@if.Else);
                            break;
                        case WhileStatement @while:
                            BindExpression(@while.Condition);
                            BindNode(@while.Body);
                            break;
                        case ForStatement @for:
                            BindNode(@for.Initializer);
                            BindExpression(@for.Condition);
                            BindExpression(@for.Incrementor);
                            BindNode(@for.Body);
                            break;
                        case ForInStatement forIn:
                            BindNode(forIn.Initializer);
                            BindExpression(forIn.Iterator);
                            BindNode(forIn.Body);
                            break;
                        case TryStatement @try:
                            BindNode(@try.Body);
                            BindNode(@try.CatchBody);
                            BindNode(@try.FinallyBody);
                            break;
                        case ThrowStatement @throw:
                            BindExpression(@throw.Expression);
                            break;
                        case DeleteStatement delete:
                            BindExpression(delete.Expression);
                            break;
                    }
                }
                finally
                {
                    if (pushed) _scopes.Pop();
                }
            }

            private void BindExpression(Expression expression)
            {
                if (expression == null)
                {
                    return;
                }

                var pushed = EnterScope(expression);
                try
                {
                    switch (expression)
                    {
                        case NameExpression name:
                            Names[name] = ResolveName(name);
                            break;
                        case BinaryExpression binary:
                            BindExpression(binary.Left);
                            BindExpression(binary.Right);
                            break;
                        case AssignmentExpression assignment:
                            BindExpression(assignment.Left);
                            BindExpression(assignment.Right);
                            break;
                        case CompoundExpression compound:
                            BindExpression(compound.Left);
                            BindExpression(compound.Right);
                            break;
                        case UnaryExpression unary:
                            BindExpression(unary.Expression);
                            break;
                        case GroupExpression group:
                            for (var i = 0; i < group.Expressions.Count; i++) BindExpression(group.Expressions[i]);
                            break;
                        case FunctionCallExpression call:
                            BindExpression(call.Target);
                            for (var i = 0; i < call.Arguments.Count; i++) BindExpression(call.Arguments[i]);
                            break;
                        case GetPropertyExpression property:
                            BindExpression(property.Object);
                            // A member name is a key, not a lexical name.
                            if (property.Property is not NameExpression) BindExpression(property.Property);
                            break;
                        case SetPropertyExpression property:
                            BindExpression(property.Object);
                            if (property.Property is not NameExpression) BindExpression(property.Property);
                            BindExpression(property.Value);
                            break;
                        case GetElementExpression element:
                            BindExpression(element.Object);
                            BindExpression(element.Index);
                            break;
                        case SetElementExpression element:
                            BindExpression(element.Object);
                            BindExpression(element.Index);
                            BindExpression(element.Value);
                            break;
                        case ArrayLiteralExpression array:
                            for (var i = 0; i < array.Elements.Count; i++) BindExpression(array.Elements[i]);
                            break;
                        case MapExpression map:
                            for (var i = 0; i < map.Entries.Count; i++) BindExpression(map.Entries[i]);
                            break;
                        case MapKeyValueExpression entry:
                            BindExpression(entry.Value);
                            break;
                        case TemplateStringExpression template:
                            for (var i = 0; i < template.Parts.Count; i++) BindExpression(template.Parts[i].Expression);
                            break;
                        case IncludedExpression included:
                            BindExpression(included.Left);
                            BindExpression(included.Right);
                            break;
                        case InExpression @in:
                            BindExpression(@in.Left);
                            BindExpression(@in.Right);
                            break;
                        case NewExpression @new:
                            BindExpression(@new.Expression);
                            break;
                        case SpreadExpression spread:
                            BindExpression(spread.Expression);
                            break;
                    }
                }
                finally
                {
                    if (pushed) _scopes.Pop();
                }
            }

            private BoundName ResolveName(NameExpression expression)
            {
                var name = expression.Identifier?.Value;
                if (string.IsNullOrEmpty(name))
                {
                    return default;
                }

                var local = ResolveLocal(name);
                var upvalue = local.IsValid ? UpvalueSlotId.Invalid : ResolveUpvalue(name);
                var moduleSymbol = local.IsValid || upvalue.IsValid || !_module.TryGetSymbol(name, out var symbol)
                    ? SymbolId.Invalid
                    : symbol;
                var directFunction = moduleSymbol.IsValid && _directFunctions.TryGetValue(moduleSymbol, out var direct)
                    ? direct
                    : FunctionId.Invalid;
                var constant = default(ScriptDatum);
                var hasConstant = moduleSymbol.IsValid && _module.TryGetInlineConstant(moduleSymbol, out constant);
                return new BoundName(name, local, upvalue, moduleSymbol, directFunction, constant, hasConstant);
            }

            private LocalSlotId ResolveLocal(string name)
            {
                var scopeId = _scopes.Count == 0 ? 0 : _scopes.Peek();
                while (scopeId >= 0)
                {
                    for (var i = _function.LocalSlots.Length - 1; i >= 0; i--)
                    {
                        var local = _function.LocalSlots[i];
                        if (local.ScopeId == scopeId && StringComparer.Ordinal.Equals(local.Name, name))
                        {
                            return local.Id;
                        }
                    }
                    scopeId = GetParentScopeId(scopeId);
                }
                return LocalSlotId.Invalid;
            }

            private UpvalueSlotId ResolveUpvalue(string name)
            {
                for (var i = 0; i < _function.UpvalueSlots.Length; i++)
                {
                    if (StringComparer.Ordinal.Equals(_function.UpvalueSlots[i].Name, name))
                    {
                        return _function.UpvalueSlots[i].Id;
                    }
                }
                return UpvalueSlotId.Invalid;
            }

            private bool EnterScope(AstNode node)
            {
                var current = _scopes.Count == 0 ? -1 : _scopes.Peek();
                var scope = GetScopeId(node, current);
                if (scope < 0 || scope == current)
                {
                    return false;
                }
                _scopes.Push(scope);
                return true;
            }

            private int GetScopeId(AstNode node, int fallback)
            {
                return node != null &&
                    _function.LocalScopeByNode != null &&
                    _function.LocalScopeByNode.TryGetValue(node, out var scope)
                        ? scope
                        : fallback;
            }

            private int GetParentScopeId(int scope)
            {
                return (uint)scope < (uint)_function.LocalScopes.Length
                    ? _function.LocalScopes[scope].ParentId
                    : -1;
            }

            private static Dictionary<SymbolId, FunctionId> BuildDirectFunctionMap(ModulePlan module)
            {
                var result = new Dictionary<SymbolId, FunctionId>();
                for (var i = 0; i < module.Functions.Count; i++)
                {
                    var function = module.Functions[i];
                    if (function.IsDirectCallCandidate &&
                        !string.IsNullOrEmpty(function.Name) &&
                        module.TryGetSymbol(function.Name, out var symbol))
                    {
                        result[symbol] = function.Id;
                    }
                }
                return result;
            }
        }

        private sealed class TypeAnalyzer
        {
            private readonly FunctionPlan _function;
            private readonly Dictionary<NameExpression, BoundName> _names;
            private readonly Dictionary<VariableDeclaration, LocalSlotId> _declarations;
            private readonly Dictionary<Expression, FlowValueType> _expressionTypes;
            private readonly FlowValueType[] _locals;
            private readonly bool[] _writtenLocals;
            private readonly FlowValueType[] _parameterTypes;
            private readonly IReadOnlyDictionary<FunctionId, FlowValueType> _directReturnTypes;
            private readonly FlowValueType[][] _directParameterTypes;
            private bool _changed;
            private FlowValueType _passReturnType;

            public TypeAnalyzer(
                FunctionPlan function,
                Dictionary<NameExpression, BoundName> names,
                Dictionary<VariableDeclaration, LocalSlotId> declarations,
                FlowValueType[] parameterTypes,
                IReadOnlyDictionary<FunctionId, FlowValueType> directReturnTypes,
                FlowValueType[][] directParameterTypes)
            {
                _function = function;
                _names = names;
                _declarations = declarations;
                _expressionTypes = new Dictionary<Expression, FlowValueType>(ReferenceEqualityComparer.Instance);
                _locals = new FlowValueType[function.LocalSlots.Length];
                _writtenLocals = new bool[function.LocalSlots.Length];
                _parameterTypes = parameterTypes;
                _directReturnTypes = directReturnTypes;
                _directParameterTypes = directParameterTypes;

                var parameterIndex = 0;
                for (var i = 0; i < function.LocalSlots.Length; i++)
                {
                    if (function.LocalSlots[i].IsParameter)
                    {
                        _locals[i] = parameterTypes != null &&
                            parameterIndex < parameterTypes.Length &&
                            parameterTypes[parameterIndex] != FlowValueType.None
                                ? parameterTypes[parameterIndex]
                                : FlowValueType.Dynamic;
                        parameterIndex++;
                    }
                    else if (IsCaptured(function.LocalSlots[i].Id))
                    {
                        _locals[i] = FlowValueType.Dynamic;
                    }
                }
            }

            public TypedFunctionCode Analyze()
            {
                var body = _function.Declaration?.Body;
                var passLimit = Math.Max(4, _locals.Length + 2);
                for (var pass = 0; pass < passLimit; pass++)
                {
                    _changed = false;
                    _passReturnType = FlowValueType.None;
                    _expressionTypes.Clear();
                    AnalyzeStatement(body as Statement);
                    if (!_changed) break;
                }

                _expressionTypes.Clear();
                _passReturnType = FlowValueType.None;
                AnalyzeStatement(body as Statement);
                var returnType = _passReturnType == FlowValueType.None ? FlowValueType.Null : _passReturnType;
                return new TypedFunctionCode(
                    _function,
                    _names,
                    _declarations,
                    _expressionTypes,
                    _locals,
                    _writtenLocals,
                    returnType);
            }

            private void AnalyzeStatement(Statement statement)
            {
                switch (statement)
                {
                    case null:
                        return;
                    case BlockStatement block:
                        for (var i = 0; i < block.Functions.Count; i++) AnalyzeStatement(block.Functions[i]);
                        for (var i = 0; i < block.Statements.Count; i++) AnalyzeStatement(block.Statements[i]);
                        return;
                    case VariableDeclaration variable:
                        if (variable.Pattern != null)
                        {
                            AnalyzeExpression(variable.Initializer);
                            for (var localIndex = 0; localIndex < _function.LocalSlots.Length; localIndex++)
                            {
                                if (ReferenceEquals(_function.LocalSlots[localIndex].Declaration, variable))
                                {
                                    MergeLocal(_function.LocalSlots[localIndex].Id, FlowValueType.Dynamic);
                                }
                            }
                            return;
                        }
                        if (_declarations.TryGetValue(variable, out var slot))
                        {
                            MergeLocal(slot, variable.Initializer == null
                                ? FlowValueType.Null
                                : AnalyzeExpression(variable.Initializer));
                        }
                        else
                        {
                            AnalyzeExpression(variable.Initializer);
                        }
                        return;
                    case FunctionDeclaration:
                        return;
                    case ExpressionStatement expression:
                        AnalyzeExpression(expression.Expression);
                        return;
                    case ReturnStatement @return:
                        _passReturnType |= @return.Expression == null
                            ? FlowValueType.Null
                            : AnalyzeExpression(@return.Expression);
                        return;
                    case IfStatement @if:
                        AnalyzeExpression(@if.Condition);
                        AnalyzeStatement(@if.Body);
                        AnalyzeStatement(@if.Else);
                        return;
                    case WhileStatement @while:
                        AnalyzeExpression(@while.Condition);
                        AnalyzeStatement(@while.Body);
                        return;
                    case ForStatement @for:
                        if (@for.Initializer is Statement initializerStatement) AnalyzeStatement(initializerStatement);
                        else if (@for.Initializer is Expression initializerExpression) AnalyzeExpression(initializerExpression);
                        AnalyzeExpression(@for.Condition);
                        AnalyzeStatement(@for.Body);
                        AnalyzeExpression(@for.Incrementor);
                        return;
                    case ForInStatement forIn:
                        AnalyzeStatement(forIn.Initializer);
                        AnalyzeExpression(forIn.Iterator?.Right);
                        if (forIn.Iterator?.Left != null && _names.TryGetValue(forIn.Iterator.Left, out var iterator))
                        {
                            if (iterator.Local.IsValid) _writtenLocals[iterator.Local.Value] = true;
                            MergeLocal(iterator.Local, FlowValueType.Dynamic);
                        }
                        AnalyzeStatement(forIn.Body);
                        return;
                    case TryStatement @try:
                        AnalyzeStatement(@try.Body);
                        AnalyzeStatement(@try.CatchBody);
                        AnalyzeStatement(@try.FinallyBody);
                        return;
                    case ThrowStatement @throw:
                        AnalyzeExpression(@throw.Expression);
                        return;
                    case DeleteStatement delete:
                        AnalyzeExpression(delete.Expression);
                        return;
                }
            }

            private FlowValueType AnalyzeExpression(Expression expression)
            {
                if (expression == null)
                {
                    return FlowValueType.Null;
                }

                FlowValueType type;
                switch (expression)
                {
                    case LiteralExpression literal:
                        type = GetLiteralType(literal);
                        break;
                    case NameExpression name:
                        type = AnalyzeName(name);
                        break;
                    case BinaryExpression binary:
                        type = AnalyzeBinary(binary.Operator, AnalyzeExpression(binary.Left), AnalyzeExpression(binary.Right));
                        break;
                    case AssignmentExpression assignment:
                        type = AnalyzeExpression(assignment.Right);
                        AnalyzeExpression(assignment.Left);
                        WriteTarget(assignment.Left, type);
                        break;
                    case CompoundExpression compound:
                        var left = AnalyzeExpression(compound.Left);
                        var right = AnalyzeExpression(compound.Right);
                        type = AnalyzeBinary(compound.Operator.SimplerOperator, left, right);
                        WriteTarget(compound.Left, type);
                        break;
                    case UnaryExpression unary:
                        var operand = AnalyzeExpression(unary.Expression);
                        type = AnalyzeUnary(unary.Operator, operand);
                        if (IsMutation(unary.Operator)) WriteTarget(unary.Expression, FlowValueType.Number);
                        break;
                    case GroupExpression group:
                        type = FlowValueType.Null;
                        for (var i = 0; i < group.Expressions.Count; i++) type = AnalyzeExpression(group.Expressions[i]);
                        break;
                    case FunctionCallExpression call:
                        AnalyzeExpression(call.Target);
                        for (var i = 0; i < call.Arguments.Count; i++) AnalyzeExpression(call.Arguments[i]);
                        if (call.Target is NameExpression targetName &&
                            _names.TryGetValue(targetName, out var targetBinding) &&
                            targetBinding.DirectFunction.IsValid &&
                            _directReturnTypes != null &&
                            _directReturnTypes.TryGetValue(targetBinding.DirectFunction, out var directReturn) &&
                            CanUseDirectReturn(call, targetBinding.DirectFunction))
                        {
                            type = directReturn;
                        }
                        else
                        {
                            type = FlowValueType.Dynamic;
                        }
                        break;
                    case GetPropertyExpression property:
                        AnalyzeExpression(property.Object);
                        type = FlowValueType.Dynamic;
                        break;
                    case SetPropertyExpression property:
                        AnalyzeExpression(property.Object);
                        type = AnalyzeExpression(property.Value);
                        break;
                    case GetElementExpression element:
                        AnalyzeExpression(element.Object);
                        AnalyzeExpression(element.Index);
                        type = FlowValueType.Dynamic;
                        break;
                    case SetElementExpression element:
                        AnalyzeExpression(element.Object);
                        AnalyzeExpression(element.Index);
                        type = AnalyzeExpression(element.Value);
                        break;
                    case ArrayLiteralExpression array:
                        for (var i = 0; i < array.Elements.Count; i++) AnalyzeExpression(array.Elements[i]);
                        type = FlowValueType.Object;
                        break;
                    case MapExpression map:
                        for (var i = 0; i < map.Entries.Count; i++) AnalyzeExpression(map.Entries[i]);
                        type = FlowValueType.Object;
                        break;
                    case MapKeyValueExpression entry:
                        type = AnalyzeExpression(entry.Value);
                        break;
                    case TemplateStringExpression template:
                        for (var i = 0; i < template.Parts.Count; i++) AnalyzeExpression(template.Parts[i].Expression);
                        type = FlowValueType.String;
                        break;
                    case IncludedExpression included:
                        AnalyzeExpression(included.Left);
                        AnalyzeExpression(included.Right);
                        type = FlowValueType.Boolean;
                        break;
                    case InExpression @in:
                        AnalyzeExpression(@in.Left);
                        AnalyzeExpression(@in.Right);
                        type = FlowValueType.Boolean;
                        break;
                    case NewExpression @new:
                        AnalyzeExpression(@new.Expression);
                        type = FlowValueType.Object;
                        break;
                    case LambdaExpression:
                        type = FlowValueType.Object;
                        break;
                    case SpreadExpression spread:
                        type = AnalyzeExpression(spread.Expression);
                        break;
                    default:
                        type = FlowValueType.Dynamic;
                        break;
                }

                _expressionTypes[expression] = type;
                return type;
            }

            private bool CanUseDirectReturn(FunctionCallExpression call, FunctionId function)
            {
                if (_directParameterTypes == null ||
                    !function.IsValid ||
                    (uint)function.Value >= (uint)_directParameterTypes.Length)
                {
                    return false;
                }

                var parameters = _directParameterTypes[function.Value];
                if (parameters == null)
                {
                    return false;
                }

                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    if (call.Arguments[i] is SpreadExpression)
                    {
                        return false;
                    }
                }

                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i] != FlowValueType.Number)
                    {
                        continue;
                    }

                    if (i >= call.Arguments.Count ||
                        !_expressionTypes.TryGetValue(call.Arguments[i], out var argumentType) ||
                        argumentType != FlowValueType.Number)
                    {
                        return false;
                    }
                }

                return true;
            }

            private FlowValueType AnalyzeName(NameExpression name)
            {
                if (!_names.TryGetValue(name, out var binding))
                {
                    return FlowValueType.Dynamic;
                }
                if (binding.HasConstant)
                {
                    return FromDatum(binding.Constant);
                }
                if (binding.IsLocal)
                {
                    var type = _locals[binding.Local.Value];
                    return type == FlowValueType.None ? FlowValueType.Dynamic : type;
                }
                return FlowValueType.Dynamic;
            }

            private void WriteTarget(Expression target, FlowValueType type)
            {
                if (target is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal)
                {
                    _writtenLocals[binding.Local.Value] = true;
                    MergeLocal(binding.Local, type);
                }
            }

            private void MergeLocal(LocalSlotId slot, FlowValueType type)
            {
                if (!slot.IsValid || (uint)slot.Value >= (uint)_locals.Length)
                {
                    return;
                }
                if (IsCaptured(slot)) type = FlowValueType.Dynamic;
                var merged = _locals[slot.Value] | (type == FlowValueType.None ? FlowValueType.Dynamic : type);
                if (merged != _locals[slot.Value])
                {
                    _locals[slot.Value] = merged;
                    _changed = true;
                }
            }

            private bool IsCaptured(LocalSlotId slot)
            {
                for (var i = 0; i < _function.CapturedLocalSlots.Length; i++)
                {
                    if (_function.CapturedLocalSlots[i].SourceLocal.Equals(slot)) return true;
                }
                return false;
            }

            private static FlowValueType AnalyzeBinary(Operator op, FlowValueType left, FlowValueType right)
            {
                if (op == Operator.LogicalAnd || op == Operator.LogicalOr) return left | right;
                if (op == Operator.Equal || op == Operator.NotEqual ||
                    op == Operator.LessThan || op == Operator.LessThanOrEqual ||
                    op == Operator.GreaterThan || op == Operator.GreaterThanOrEqual)
                {
                    return FlowValueType.Boolean;
                }
                if (op == Operator.Add)
                {
                    if (left == FlowValueType.String || right == FlowValueType.String) return FlowValueType.String;
                    var nonNumeric = FlowValueType.String | FlowValueType.Object;
                    return (left & nonNumeric) == 0 && (right & nonNumeric) == 0
                        ? FlowValueType.Number
                        : FlowValueType.Dynamic;
                }
                if (op == Operator.BitwiseOr)
                {
                    if ((left & FlowValueType.Null) == 0) return FlowValueType.Number;
                    return left == FlowValueType.Null
                        ? right
                        : FlowValueType.Number | right;
                }
                if (op == Operator.Subtract || op == Operator.Multiply || op == Operator.Divide ||
                    op == Operator.Modulo || op == Operator.BitwiseAnd ||
                    op == Operator.BitwiseXor || op == Operator.LeftShift ||
                    op == Operator.SignedRightShift || op == Operator.UnSignedRightShift)
                {
                    return FlowValueType.Number;
                }
                return FlowValueType.Dynamic;
            }

            private static FlowValueType AnalyzeUnary(Operator op, FlowValueType operand)
            {
                if (op == Operator.LogicalNot) return FlowValueType.Boolean;
                if (op == Operator.TypeOf) return FlowValueType.String;
                if (op == Operator.PostIncrement || op == Operator.PostDecrement) return operand;
                if (op == Operator.Negate || op == Operator.BitwiseNot ||
                    op == Operator.PreIncrement || op == Operator.PreDecrement)
                {
                    return FlowValueType.Number;
                }
                return operand;
            }

            private static bool IsMutation(Operator op)
            {
                return op == Operator.PreIncrement || op == Operator.PostIncrement ||
                    op == Operator.PreDecrement || op == Operator.PostDecrement;
            }

            private static FlowValueType GetLiteralType(LiteralExpression literal)
            {
                return literal.Token switch
                {
                    NullToken => FlowValueType.Null,
                    BooleanToken => FlowValueType.Boolean,
                    NumberToken => FlowValueType.Number,
                    StringToken => FlowValueType.String,
                    RegexToken => FlowValueType.Object,
                    _ => FlowValueType.Dynamic
                };
            }

            private static FlowValueType FromDatum(ScriptDatum datum)
            {
                return datum.Kind switch
                {
                    ValueKind.Null => FlowValueType.Null,
                    ValueKind.Boolean => FlowValueType.Boolean,
                    ValueKind.Number => FlowValueType.Number,
                    ValueKind.String => FlowValueType.String,
                    _ => FlowValueType.Object
                };
            }
        }
    }
}
