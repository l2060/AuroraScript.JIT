using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
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
            DirectParameterType[] parameterTypes = null,
            IReadOnlyDictionary<FunctionId, FlowValueType> directReturnTypes = null,
            DirectParameterType[][] directParameterTypes = null,
            IReadOnlyDictionary<FunctionId, FlowValueType> universalReturnTypes = null)
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
                directParameterTypes,
                universalReturnTypes);
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
                        case TypedDocumentExpression tdoc:
                            BindExpression(tdoc.Value);
                            break;
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
            private readonly FlowValueType[] _forcedLocalTypes;
            private readonly bool[] _writtenLocals;
            private readonly DirectParameterType[] _parameterTypes;
            private readonly IReadOnlyDictionary<FunctionId, FlowValueType> _directReturnTypes;
            private readonly IReadOnlyDictionary<FunctionId, FlowValueType> _universalReturnTypes;
            private readonly DirectParameterType[][] _directParameterTypes;
            private readonly HashSet<int> _safeInt32Mutations;
            private readonly Dictionary<int, Dictionary<string, FlowValueType>> _localFields;
            private readonly HashSet<int> _invalidLocalFields;
            private readonly Dictionary<int, FlowValueType> _localArrayElements;
            private readonly HashSet<int> _invalidLocalArrayElements;
            private readonly bool _optimisticDirect;
            private bool _changed;
            private FlowValueType _passReturnType;
            private bool _sawReturn;

            public TypeAnalyzer(
                FunctionPlan function,
                Dictionary<NameExpression, BoundName> names,
                Dictionary<VariableDeclaration, LocalSlotId> declarations,
                DirectParameterType[] parameterTypes,
                IReadOnlyDictionary<FunctionId, FlowValueType> directReturnTypes,
                DirectParameterType[][] directParameterTypes,
                IReadOnlyDictionary<FunctionId, FlowValueType> universalReturnTypes)
            {
                _function = function;
                _names = names;
                _declarations = declarations;
                _expressionTypes = new Dictionary<Expression, FlowValueType>(ReferenceEqualityComparer.Instance);
                _locals = new FlowValueType[function.LocalSlots.Length];
                _forcedLocalTypes = new FlowValueType[function.LocalSlots.Length];
                _writtenLocals = new bool[function.LocalSlots.Length];
                _parameterTypes = parameterTypes;
                _directReturnTypes = directReturnTypes;
                _universalReturnTypes = universalReturnTypes;
                _directParameterTypes = directParameterTypes;
                _optimisticDirect = parameterTypes != null;
                _safeInt32Mutations = new HashSet<int>();
                _localFields = new Dictionary<int, Dictionary<string, FlowValueType>>();
                _invalidLocalFields = new HashSet<int>();
                _localArrayElements = new Dictionary<int, FlowValueType>();
                _invalidLocalArrayElements = new HashSet<int>();

                var parameterIndex = 0;
                for (var i = 0; i < function.LocalSlots.Length; i++)
                {
                    if (function.LocalSlots[i].IsParameter)
                    {
                        _locals[i] = parameterTypes != null &&
                            parameterIndex < parameterTypes.Length &&
                            parameterTypes[parameterIndex].Type != FlowValueType.None
                                ? FlowValueTypeFacts.GetDirectLocalType(parameterTypes[parameterIndex])
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
                    _sawReturn = false;
                    _expressionTypes.Clear();
                    AnalyzeStatement(body as Statement);
                    if (!_changed) break;
                }

                var storageChanged = ApplyExactNumericStorage(body);
                storageChanged |= ApplyLocalCoercionStorage(body);
                if (storageChanged)
                {
                    for (var pass = 0; pass < passLimit; pass++)
                    {
                        _changed = false;
                        _passReturnType = FlowValueType.None;
                        _sawReturn = false;
                        _expressionTypes.Clear();
                        AnalyzeStatement(body as Statement);
                        if (!_changed) break;
                    }
                }

                _expressionTypes.Clear();
                _passReturnType = FlowValueType.None;
                _sawReturn = false;
                AnalyzeStatement(body as Statement);
                var returnType = _sawReturn
                    ? _passReturnType
                    : FlowValueType.Null;
                var sequentialReturn = new SequentialReturnTypeAnalyzer(
                    _function,
                    _names,
                    _declarations,
                    _expressionTypes,
                    _parameterTypes,
                    IsCaptured).Analyze(body as Statement);
                if (sequentialReturn != FlowValueType.None)
                {
                    returnType = sequentialReturn;
                }
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
                            var initializerType = variable.Initializer == null
                                ? FlowValueType.Null
                                : AnalyzeExpression(variable.Initializer);
                            MergeLocal(slot, initializerType);
                            if (variable.Initializer is MapExpression map)
                            {
                                MergeLocalFields(slot, map);
                            }
                            if (variable.Initializer is ArrayLiteralExpression array)
                            {
                                MergeLocalArrayElements(slot, array);
                            }
                            else if (variable.Initializer is FunctionCallExpression arrayFactory &&
                                IsArrayFactoryCall(arrayFactory))
                            {
                                MergeEmptyLocalArrayElements(slot);
                            }
                            else
                            {
                                InvalidateLocalArrayElementsUsedAsValue(variable.Initializer);
                            }
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
                        _sawReturn = true;
                        InvalidateLocalArrayElementsUsedAsValue(@return.Expression);
                        if (!_function.IsDirectCallCandidate)
                        {
                            InvalidateLocalFieldsUsedAsValue(@return.Expression);
                        }
                        _passReturnType = FlowValueTypeFacts.Merge(
                            _passReturnType,
                            @return.Expression == null
                                ? FlowValueType.Null
                                : AnalyzeExpression(@return.Expression));
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
                        if (TryGetSafeInt32Induction(@for, out var inductionSlot))
                        {
                            _safeInt32Mutations.Add(inductionSlot.Value);
                            try
                            {
                                AnalyzeExpression(@for.Incrementor);
                            }
                            finally
                            {
                                _safeInt32Mutations.Remove(inductionSlot.Value);
                            }
                        }
                        else
                        {
                            AnalyzeExpression(@for.Incrementor);
                        }
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
                        InvalidateLocalFieldsForMutation(delete.Expression);
                        InvalidateLocalArrayElementsForMutation(delete.Expression);
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
                    case TypedDocumentExpression tdoc:
                        var inferredTDocType = AnalyzeExpression(tdoc.Value);
                        type = GetTypedDocumentFlowType(tdoc, inferredTDocType);
                        break;
                    case LiteralExpression literal:
                        type = GetLiteralType(literal);
                        break;
                    case NameExpression name:
                        type = AnalyzeName(name);
                        break;
                    case BinaryExpression binary:
                        var binaryLeft = AnalyzeExpression(binary.Left);
                        var binaryRight = AnalyzeExpression(binary.Right);
                        if (binary.Operator == Operator.Add)
                        {
                            binaryLeft = ApplyLocalArrayArithmeticDemand(binary.Left, binaryLeft);
                            binaryRight = ApplyLocalArrayArithmeticDemand(binary.Right, binaryRight);
                        }
                        type = AnalyzeBinary(
                            binary.Operator,
                            binary.Left,
                            binary.Right,
                            binaryLeft,
                            binaryRight);
                        break;
                    case AssignmentExpression assignment:
                        type = AnalyzeExpression(assignment.Right);
                        InvalidateLocalArrayElementsUsedAsValue(assignment.Right);
                        AnalyzeExpression(assignment.Left);
                        WriteTarget(assignment.Left, type);
                        break;
                    case CompoundExpression compound:
                        var left = AnalyzeExpression(compound.Left);
                        var right = AnalyzeExpression(compound.Right);
                        if (compound.Operator.SimplerOperator == Operator.Add)
                        {
                            left = ApplyLocalArrayArithmeticDemand(compound.Left, left);
                            right = ApplyLocalArrayArithmeticDemand(compound.Right, right);
                        }
                        type = AnalyzeBinary(
                            compound.Operator.SimplerOperator,
                            compound.Left,
                            compound.Right,
                            left,
                            right);
                        WriteTarget(compound.Left, type);
                        break;
                    case UnaryExpression unary:
                        var operand = AnalyzeExpression(unary.Expression);
                        type = AnalyzeUnary(unary, operand);
                        if (IsMutation(unary.Operator))
                        {
                            WriteTarget(unary.Expression, GetMutationWriteType(unary));
                        }
                        break;
                    case GroupExpression group:
                        type = FlowValueType.Null;
                        for (var i = 0; i < group.Expressions.Count; i++) type = AnalyzeExpression(group.Expressions[i]);
                        break;
                    case FunctionCallExpression call:
                        AnalyzeExpression(call.Target);
                        var isDirectCall = IsDirectFunctionCall(call);
                        var isLocalArrayPush = TryGetLocalArrayPush(call, out var pushSlot);
                        if (!isLocalArrayPush)
                        {
                            InvalidateLocalArrayElementsForCallTarget(call.Target);
                        }
                        for (var i = 0; i < call.Arguments.Count; i++)
                        {
                            AnalyzeExpression(call.Arguments[i]);
                            InvalidateLocalArrayElementsUsedAsValue(call.Arguments[i]);
                            if (!isDirectCall)
                            {
                                InvalidateLocalFieldsUsedAsValue(call.Arguments[i]);
                            }
                        }
                        if (isLocalArrayPush)
                        {
                            UpdateLocalArrayPush(pushSlot, call.Arguments);
                        }
                        if (IsArrayFactoryCall(call))
                        {
                            type = FlowValueType.Array;
                        }
                        else if (call.Target is NameExpression targetName &&
                            _names.TryGetValue(targetName, out var targetBinding) &&
                            targetBinding.DirectFunction.IsValid &&
                            _directReturnTypes != null &&
                            _directReturnTypes.TryGetValue(targetBinding.DirectFunction, out var directReturn) &&
                            ((_optimisticDirect && directReturn == FlowValueType.None) ||
                                (directReturn != FlowValueType.None &&
                                    CanUseDirectReturn(call, targetBinding.DirectFunction))))
                        {
                            type = directReturn;
                        }
                        else if (call.Target is NameExpression universalTarget &&
                            _names.TryGetValue(universalTarget, out var universalBinding) &&
                            universalBinding.DirectFunction.IsValid &&
                            _universalReturnTypes != null &&
                            _universalReturnTypes.TryGetValue(
                                universalBinding.DirectFunction,
                                out var universalReturn) &&
                            universalReturn != FlowValueType.None &&
                            universalReturn != FlowValueType.Dynamic)
                        {
                            type = universalReturn;
                        }
                        else
                        {
                            type = FlowValueType.Dynamic;
                        }
                        break;
                    case GetPropertyExpression property:
                        var propertyObjectType = AnalyzeExpression(property.Object);
                        type = (FlowValueTypeFacts.IsPackedArray(propertyObjectType) ||
                                propertyObjectType == FlowValueType.Array) &&
                            IsStaticProperty(property.Property, "length")
                                ? FlowValueType.Int32
                                : TryGetLocalFieldType(property, out var fieldType)
                                    ? fieldType
                                    : FlowValueType.Dynamic;
                        if (!TryGetStaticPropertyName(property.Property, out _))
                        {
                            InvalidateLocalFieldsUsedAsValue(property.Object);
                        }
                        break;
                    case SetPropertyExpression property:
                        AnalyzeExpression(property.Object);
                        type = AnalyzeExpression(property.Value);
                        UpdateLocalField(property.Object, property.Property, type);
                        InvalidateLocalArrayElementsUsedAsValue(property.Value);
                        InvalidateLocalArrayElementsUsedAsValue(property.Object);
                        break;
                    case GetElementExpression element:
                        var elementObjectType = AnalyzeExpression(element.Object);
                        var indexType = AnalyzeExpression(element.Index);
                        InvalidateLocalFieldsUsedAsValue(element.Object);
                        type = FlowValueTypeFacts.IsPackedArray(elementObjectType)
                            ? FlowValueTypeFacts.GetPackedElementType(elementObjectType)
                            : FlowValueType.Dynamic;
                        break;
                    case SetElementExpression element:
                        AnalyzeExpression(element.Object);
                        var setIndexType = AnalyzeExpression(element.Index);
                        InvalidateLocalFieldsUsedAsValue(element.Object);
                        type = AnalyzeExpression(element.Value);
                        UpdateLocalArrayElement(element.Object, setIndexType, type);
                        InvalidateLocalArrayElementsUsedAsValue(element.Value);
                        break;
                    case ArrayLiteralExpression array:
                        for (var i = 0; i < array.Elements.Count; i++)
                        {
                            AnalyzeExpression(array.Elements[i]);
                            InvalidateLocalArrayElementsUsedAsValue(array.Elements[i]);
                        }
                        type = FlowValueType.Array;
                        break;
                    case MapExpression map:
                        for (var i = 0; i < map.Entries.Count; i++)
                        {
                            AnalyzeExpression(map.Entries[i]);
                            if (map.Entries[i] is MapKeyValueExpression entry)
                            {
                                InvalidateLocalArrayElementsUsedAsValue(entry.Value);
                            }
                        }
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
                        type = GetPackedArrayConstructionType(@new, out var packedType)
                            ? packedType
                            : IsArrayConstruction(@new)
                                ? FlowValueType.Array
                                : FlowValueType.Object;
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

            private static FlowValueType GetTypedDocumentFlowType(
                TypedDocumentExpression expression,
                FlowValueType inferred)
            {
                if (expression.IsInterpolation || ContainsTDocInterpolation(expression.Value))
                {
                    // An explicit array type is also a runtime-checked cast. Keep
                    // that exact type in flow so one boundary check unlocks native
                    // element access for the rest of the local hot path.
                    return expression.TypeName switch
                    {
                        "Array" => FlowValueType.Array,
                        "Int32Array" => FlowValueType.Int32Array,
                        "Int8Array" => FlowValueType.Int8Array,
                        "Float64Array" => FlowValueType.Float64Array,
                        "BooleanArray" => FlowValueType.BooleanArray,
                        "UInt8Array" => FlowValueType.UInt8Array,
                        "Int16Array" => FlowValueType.Int16Array,
                        "UInt16Array" => FlowValueType.UInt16Array,
                        "UInt32Array" => FlowValueType.UInt32Array,
                        "Int64Array" => FlowValueType.Int64Array,
                        "UInt64Array" => FlowValueType.UInt64Array,
                        _ => FlowValueType.Dynamic
                    };
                }
                return expression.TypeName switch
                {
                    null or "" => inferred,
                    "Null" => FlowValueType.Null,
                    "Boolean" => FlowValueType.Boolean,
                    "Number" => FlowValueType.Number,
                    "String" => FlowValueType.String,
                    "Object" or "StringBuffer" or "Date" or "Regex" or "Path" or "HashMap" => FlowValueType.Object,
                    "Array" => FlowValueType.Array,
                    "Int32Array" => FlowValueType.Int32Array,
                    "Int8Array" => FlowValueType.Int8Array,
                    "Float64Array" => FlowValueType.Float64Array,
                    "BooleanArray" => FlowValueType.BooleanArray,
                    "UInt8Array" => FlowValueType.UInt8Array,
                    "Int16Array" => FlowValueType.Int16Array,
                    "UInt16Array" => FlowValueType.UInt16Array,
                    "UInt32Array" => FlowValueType.UInt32Array,
                    "Int64Array" => FlowValueType.Int64Array,
                    "UInt64Array" => FlowValueType.UInt64Array,
                    _ => FlowValueType.Object
                };
            }

            private static bool ContainsTDocInterpolation(Expression expression)
            {
                if (expression is TypedDocumentExpression tdoc)
                {
                    return tdoc.IsInterpolation || ContainsTDocInterpolation(tdoc.Value);
                }
                if (expression is ArrayLiteralExpression array)
                {
                    for (var i = 0; i < array.Elements.Count; i++)
                    {
                        if (ContainsTDocInterpolation(array.Elements[i])) return true;
                    }
                }
                else if (expression is MapExpression map)
                {
                    for (var i = 0; i < map.Entries.Count; i++)
                    {
                        if (ContainsTDocInterpolation(map.Entries[i])) return true;
                    }
                }
                else if (expression is MapKeyValueExpression entry)
                {
                    return ContainsTDocInterpolation(entry.Value);
                }
                return false;
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
                    if (!FlowValueTypeFacts.IsNativeDirectParameter(parameters[i]))
                    {
                        continue;
                    }

                    if (i >= call.Arguments.Count ||
                        !_expressionTypes.TryGetValue(call.Arguments[i], out var argumentType) ||
                        !FlowValueTypeFacts.CanPassNativeArgument(parameters[i], argumentType))
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

            private bool GetPackedArrayConstructionType(
                NewExpression expression,
                out FlowValueType type)
            {
                type = FlowValueType.None;
                if (expression?.Expression?.Target is not NameExpression name ||
                    !_names.TryGetValue(name, out var binding) ||
                    !binding.IsUnshadowedGlobal)
                {
                    return false;
                }
                return FlowValueTypeFacts.TryGetPackedArrayType(binding.Name, out type);
            }

            private bool IsArrayConstruction(NewExpression expression)
            {
                return expression?.Expression?.Target is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.IsUnshadowedGlobal &&
                    StringComparer.Ordinal.Equals(binding.Name, "Array");
            }

            private bool IsArrayFactoryCall(FunctionCallExpression expression)
            {
                if (expression?.Target is not GetPropertyExpression property ||
                    !IsStaticProperty(property.Property, "withCapacity") ||
                    property.Object is not NameExpression name ||
                    !_names.TryGetValue(name, out var binding))
                {
                    return false;
                }
                return binding.IsUnshadowedGlobal &&
                    StringComparer.Ordinal.Equals(binding.Name, "Array");
            }

            private static bool IsStaticProperty(Expression property, string expected)
            {
                return property is NameExpression name &&
                    StringComparer.Ordinal.Equals(name.Identifier?.Value, expected);
            }

            private bool IsDirectFunctionCall(FunctionCallExpression call)
            {
                return call?.Target is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.DirectFunction.IsValid;
            }

            private void MergeLocalFields(LocalSlotId slot, MapExpression map)
            {
                if (!slot.IsValid ||
                    _invalidLocalFields.Contains(slot.Value) ||
                    IsCaptured(slot))
                {
                    return;
                }

                var fields = new Dictionary<string, FlowValueType>(StringComparer.Ordinal);
                for (var i = 0; i < map.Entries.Count; i++)
                {
                    if (map.Entries[i] is not MapKeyValueExpression entry ||
                        string.IsNullOrEmpty(entry.Key?.Value))
                    {
                        InvalidateLocalFields(slot);
                        return;
                    }
                    if (_expressionTypes.TryGetValue(entry.Value, out var fieldType) &&
                        (fieldType == FlowValueType.Array ||
                            FlowValueTypeFacts.IsPackedArray(fieldType)))
                    {
                        fields[entry.Key.Value] = fieldType;
                    }
                    else
                    {
                        // A later duplicate field replaces the earlier value.
                        fields.Remove(entry.Key.Value);
                    }
                }

                if (!_localFields.TryGetValue(slot.Value, out var existing))
                {
                    _localFields[slot.Value] = fields;
                    _changed = true;
                    return;
                }

                List<string> mismatches = null;
                foreach (var pair in existing)
                {
                    if (!fields.TryGetValue(pair.Key, out var fieldType) ||
                        fieldType != pair.Value)
                    {
                        (mismatches ??= new List<string>()).Add(pair.Key);
                    }
                }
                if (mismatches == null)
                {
                    return;
                }
                for (var i = 0; i < mismatches.Count; i++)
                {
                    existing.Remove(mismatches[i]);
                }
                _changed = true;
            }

            private bool TryGetLocalFieldType(
                GetPropertyExpression property,
                out FlowValueType type)
            {
                type = FlowValueType.Dynamic;
                return property.Object is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal &&
                    TryGetStaticPropertyName(property.Property, out var fieldName) &&
                    _localFields.TryGetValue(binding.Local.Value, out var fields) &&
                    fields.TryGetValue(fieldName, out type);
            }

            private FlowValueType ApplyLocalArrayArithmeticDemand(
                Expression expression,
                FlowValueType type)
            {
                if (type != FlowValueType.Dynamic ||
                    expression is not GetElementExpression element ||
                    !_expressionTypes.TryGetValue(element.Index, out var indexType) ||
                    !FlowValueTypeFacts.IsNumeric(indexType) ||
                    !TryGetLocalArrayElementType(element.Object, out _))
                {
                    return type;
                }

                // Keep the element expression itself dynamic so identity-sensitive
                // uses still observe ScriptDatum semantics. Only '+' receives the
                // proof that this value can contain a Number or an array hole.
                return FlowValueType.Number;
            }

            private void MergeLocalArrayElements(
                LocalSlotId slot,
                ArrayLiteralExpression array)
            {
                if (!slot.IsValid ||
                    _invalidLocalArrayElements.Contains(slot.Value) ||
                    IsCaptured(slot))
                {
                    return;
                }

                for (var i = 0; i < array.Elements.Count; i++)
                {
                    var element = array.Elements[i];
                    if (element is SpreadExpression ||
                        (element != null &&
                            (!_expressionTypes.TryGetValue(element, out var elementType) ||
                                !IsLocalArrayArithmeticExpressionValue(element, elementType))))
                    {
                        InvalidateLocalArrayElements(slot);
                        return;
                    }
                }

                MergeEmptyLocalArrayElements(slot);
            }

            private void MergeEmptyLocalArrayElements(LocalSlotId slot)
            {
                if (!slot.IsValid ||
                    _invalidLocalArrayElements.Contains(slot.Value) ||
                    IsCaptured(slot))
                {
                    return;
                }
                if (!_localArrayElements.ContainsKey(slot.Value))
                {
                    // A missing element reads as Null. Null and numeric values use
                    // the same arithmetic '+' branch, so holes do not invalidate
                    // this narrowly scoped fact.
                    _localArrayElements[slot.Value] = FlowValueType.Number;
                    _changed = true;
                }
            }

            private bool TryGetLocalArrayPush(
                FunctionCallExpression call,
                out LocalSlotId slot)
            {
                slot = LocalSlotId.Invalid;
                if (call?.Target is not GetPropertyExpression property ||
                    !IsStaticProperty(property.Property, "push") ||
                    !TryGetLocalSlot(property.Object, out slot) ||
                    !_localArrayElements.ContainsKey(slot.Value))
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
                return true;
            }

            private void UpdateLocalArrayPush(
                LocalSlotId slot,
                IReadOnlyList<Expression> arguments)
            {
                for (var i = 0; i < arguments.Count; i++)
                {
                    if (!_expressionTypes.TryGetValue(arguments[i], out var argumentType) ||
                        !IsLocalArrayArithmeticExpressionValue(arguments[i], argumentType))
                    {
                        InvalidateLocalArrayElements(slot);
                        return;
                    }
                }
            }

            private void UpdateLocalArrayElement(
                Expression objectExpression,
                FlowValueType indexType,
                FlowValueType valueType)
            {
                if (!TryGetLocalSlot(objectExpression, out var slot) ||
                    !_localArrayElements.ContainsKey(slot.Value))
                {
                    return;
                }
                if (!FlowValueTypeFacts.IsNumeric(indexType) ||
                    !IsLocalArrayArithmeticValue(valueType))
                {
                    InvalidateLocalArrayElements(slot);
                }
            }

            private bool TryGetLocalArrayElementType(
                Expression objectExpression,
                out FlowValueType type)
            {
                type = FlowValueType.Dynamic;
                return TryGetLocalSlot(objectExpression, out var slot) &&
                    _localArrayElements.TryGetValue(slot.Value, out type);
            }

            private static bool IsLocalArrayArithmeticValue(FlowValueType type)
            {
                return FlowValueTypeFacts.IsNumeric(type) ||
                    type == FlowValueType.Null;
            }

            private bool IsLocalArrayArithmeticExpressionValue(
                Expression expression,
                FlowValueType type)
            {
                return IsLocalArrayArithmeticValue(type) ||
                    (expression is GetElementExpression element &&
                        _expressionTypes.TryGetValue(element.Index, out var indexType) &&
                        FlowValueTypeFacts.IsNumeric(indexType) &&
                        TryGetLocalArrayElementType(element.Object, out _));
            }

            private void UpdateLocalField(
                Expression objectExpression,
                Expression propertyExpression,
                FlowValueType valueType)
            {
                if (!TryGetLocalSlot(objectExpression, out var slot) ||
                    !_localFields.TryGetValue(slot.Value, out var fields))
                {
                    return;
                }
                if (!TryGetStaticPropertyName(propertyExpression, out var fieldName))
                {
                    InvalidateLocalFields(slot);
                    return;
                }
                if (fields.TryGetValue(fieldName, out var fieldType) &&
                    fieldType != valueType)
                {
                    InvalidateLocalFields(slot);
                }
            }

            private void InvalidateLocalFieldsForMutation(Expression expression)
            {
                switch (expression)
                {
                    case GetPropertyExpression property:
                        InvalidateLocalFieldsUsedAsValue(property.Object);
                        break;
                    case GetElementExpression element:
                        InvalidateLocalFieldsUsedAsValue(element.Object);
                        break;
                    default:
                        InvalidateLocalFieldsUsedAsValue(expression);
                        break;
                }
            }

            private void InvalidateLocalArrayElementsForMutation(Expression expression)
            {
                switch (expression)
                {
                    case GetPropertyExpression property:
                        InvalidateLocalArrayElementsUsedAsValue(property.Object);
                        break;
                    case GetElementExpression element:
                        InvalidateLocalArrayElementsUsedAsValue(element.Object);
                        break;
                    default:
                        InvalidateLocalArrayElementsUsedAsValue(expression);
                        break;
                }
            }

            private void InvalidateLocalArrayElementsForCallTarget(Expression target)
            {
                if (target is GetPropertyExpression property)
                {
                    InvalidateLocalArrayElementsUsedAsValue(property.Object);
                }
                else if (target is GetElementExpression element)
                {
                    InvalidateLocalArrayElementsUsedAsValue(element.Object);
                }
            }

            private void InvalidateLocalArrayElementsUsedAsValue(Expression expression)
            {
                switch (expression)
                {
                    case null:
                        return;
                    case NameExpression:
                        if (TryGetLocalSlot(expression, out var slot))
                        {
                            InvalidateLocalArrayElements(slot);
                        }
                        return;
                    case TypedDocumentExpression tdoc:
                        InvalidateLocalArrayElementsUsedAsValue(tdoc.Value);
                        return;
                    case SpreadExpression spread:
                        InvalidateLocalArrayElementsUsedAsValue(spread.Expression);
                        return;
                    case GroupExpression group:
                        for (var i = 0; i < group.Expressions.Count; i++)
                        {
                            InvalidateLocalArrayElementsUsedAsValue(group.Expressions[i]);
                        }
                        return;
                }
            }

            private void InvalidateLocalFieldsUsedAsValue(Expression expression)
            {
                if (TryGetLocalSlot(expression, out var slot))
                {
                    InvalidateLocalFields(slot);
                }
            }

            private bool TryGetLocalSlot(Expression expression, out LocalSlotId slot)
            {
                slot = LocalSlotId.Invalid;
                if (expression is not NameExpression name ||
                    !_names.TryGetValue(name, out var binding) ||
                    !binding.IsLocal)
                {
                    return false;
                }
                slot = binding.Local;
                return true;
            }

            private void InvalidateLocalFields(LocalSlotId slot)
            {
                if (!slot.IsValid || !_invalidLocalFields.Add(slot.Value))
                {
                    return;
                }
                if (_localFields.Remove(slot.Value))
                {
                    _changed = true;
                }
            }

            private void InvalidateLocalArrayElements(LocalSlotId slot)
            {
                if (!slot.IsValid || !_invalidLocalArrayElements.Add(slot.Value))
                {
                    return;
                }
                if (_localArrayElements.Remove(slot.Value))
                {
                    _changed = true;
                }
            }

            private static bool TryGetStaticPropertyName(
                Expression property,
                out string name)
            {
                if (property is NameExpression nameExpression &&
                    !string.IsNullOrEmpty(nameExpression.Identifier?.Value))
                {
                    name = nameExpression.Identifier.Value;
                    return true;
                }
                name = null;
                return false;
            }

            private void WriteTarget(Expression target, FlowValueType type)
            {
                if (target is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal)
                {
                    InvalidateLocalFields(binding.Local);
                    InvalidateLocalArrayElements(binding.Local);
                    _writtenLocals[binding.Local.Value] = true;
                    MergeLocal(binding.Local, type);
                }
                else if (target is GetElementExpression element)
                {
                    var indexType = _expressionTypes.TryGetValue(element.Index, out var analyzedIndex)
                        ? analyzedIndex
                        : FlowValueType.Dynamic;
                    UpdateLocalArrayElement(element.Object, indexType, type);
                }
            }

            private void MergeLocal(LocalSlotId slot, FlowValueType type)
            {
                if (!slot.IsValid || (uint)slot.Value >= (uint)_locals.Length)
                {
                    return;
                }
                if (IsCaptured(slot)) type = FlowValueType.Dynamic;
                else if (_forcedLocalTypes[slot.Value] != FlowValueType.None)
                {
                    type = _forcedLocalTypes[slot.Value];
                }
                if (type == FlowValueType.None && _optimisticDirect)
                {
                    return;
                }
                var merged = FlowValueTypeFacts.Merge(
                    _locals[slot.Value],
                    type == FlowValueType.None ? FlowValueType.Dynamic : type);
                if (merged != _locals[slot.Value])
                {
                    _locals[slot.Value] = merged;
                    _changed = true;
                }
            }

            private bool ApplyLocalCoercionStorage(AstNode body)
            {
                var demands = new LocalCoercionAnalyzer(
                    _function,
                    _names,
                    _expressionTypes,
                    _directParameterTypes,
                    IsCaptured).Analyze(body);
                var changed = false;
                for (var i = 0; i < demands.Length; i++)
                {
                    var type = demands[i] switch
                    {
                        NativeCoercionKind.ArithmeticNumber => FlowValueType.Number,
                        NativeCoercionKind.Boolean => FlowValueType.Boolean,
                        _ => FlowValueType.None
                    };
                    if (type == FlowValueType.None || _forcedLocalTypes[i] == type)
                    {
                        continue;
                    }
                    _forcedLocalTypes[i] = type;
                    _locals[i] = type;
                    changed = true;
                }
                return changed;
            }

            private bool ApplyExactNumericStorage(AstNode body)
            {
                var candidates = new ExactNumericDefinitionAnalyzer(
                    _function,
                    _names,
                    _expressionTypes,
                    IsCaptured).Analyze(body);
                var changed = false;
                for (var i = 0; i < candidates.Length; i++)
                {
                    if (!candidates[i] ||
                        FlowValueTypeFacts.IsNumeric(_locals[i]) ||
                        _forcedLocalTypes[i] == FlowValueType.Number)
                    {
                        continue;
                    }
                    _forcedLocalTypes[i] = FlowValueType.Number;
                    _locals[i] = FlowValueType.Number;
                    changed = true;
                }
                return changed;
            }

            private bool IsCaptured(LocalSlotId slot)
            {
                for (var i = 0; i < _function.CapturedLocalSlots.Length; i++)
                {
                    if (_function.CapturedLocalSlots[i].SourceLocal.Equals(slot)) return true;
                }
                return false;
            }

            private bool TryGetSafeInt32Induction(ForStatement statement, out LocalSlotId slot)
            {
                slot = LocalSlotId.Invalid;
                if (statement?.Condition is not BinaryExpression condition ||
                    condition.Operator != Operator.LessThan ||
                    condition.Left is not NameExpression conditionName ||
                    statement.Incrementor is not UnaryExpression increment ||
                    (increment.Operator != Operator.PostIncrement &&
                        increment.Operator != Operator.PreIncrement) ||
                    increment.Expression is not NameExpression incrementName)
                {
                    return false;
                }
                if (!_names.TryGetValue(conditionName, out var conditionBinding) ||
                    !_names.TryGetValue(incrementName, out var incrementBinding) ||
                    !conditionBinding.IsLocal ||
                    !incrementBinding.IsLocal ||
                    !conditionBinding.Local.Equals(incrementBinding.Local) ||
                    _locals[conditionBinding.Local.Value] != FlowValueType.Int32 ||
                    !_expressionTypes.TryGetValue(condition.Right, out var boundType) ||
                    boundType != FlowValueType.Int32 ||
                    WritesLocal(statement.Body, conditionBinding.Local))
                {
                    return false;
                }
                slot = conditionBinding.Local;
                return true;
            }

            private bool WritesLocal(AstNode node, LocalSlotId slot)
            {
                if (node == null || node is FunctionDeclaration or LambdaExpression)
                {
                    return false;
                }
                if (node is AssignmentExpression assignment && IsLocalName(assignment.Left, slot) ||
                    node is CompoundExpression compound && IsLocalName(compound.Left, slot) ||
                    node is UnaryExpression unary && IsMutation(unary.Operator) &&
                        IsLocalName(unary.Expression, slot) ||
                    node is ForInStatement forIn && IsLocalName(forIn.Iterator?.Left, slot))
                {
                    return true;
                }
                var detector = new LocalWriteDetector(this, slot);
                AstTraversal.VisitChildren(node, ref detector);
                return detector.Found;
            }

            private bool IsLocalName(Expression expression, LocalSlotId slot)
            {
                return expression is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal &&
                    binding.Local.Equals(slot);
            }

            private struct LocalWriteDetector : IAstChildVisitor
            {
                private readonly TypeAnalyzer _owner;
                private readonly LocalSlotId _slot;

                public LocalWriteDetector(TypeAnalyzer owner, LocalSlotId slot)
                {
                    _owner = owner;
                    _slot = slot;
                    Found = false;
                }

                public bool Found;

                public void Visit(AstNode node)
                {
                    if (!Found && _owner.WritesLocal(node, _slot))
                    {
                        Found = true;
                    }
                }
            }

            private static FlowValueType AnalyzeBinary(
                Operator op,
                Expression leftExpression,
                Expression rightExpression,
                FlowValueType left,
                FlowValueType right)
            {
                if (op == Operator.LogicalAnd || op == Operator.LogicalOr)
                {
                    return FlowValueTypeFacts.Merge(left, right);
                }
                if (op == Operator.Equal || op == Operator.NotEqual ||
                    op == Operator.LessThan || op == Operator.LessThanOrEqual ||
                    op == Operator.GreaterThan || op == Operator.GreaterThanOrEqual)
                {
                    return FlowValueType.Boolean;
                }
                if (op == Operator.Add)
                {
                    if (left == FlowValueType.String || right == FlowValueType.String) return FlowValueType.String;
                    var nonNumeric = FlowValueType.String | FlowValueType.Object |
                        FlowValueType.Int32Array | FlowValueType.Int8Array |
                        FlowValueType.BooleanArray | FlowValueType.Float64Array |
                        FlowValueType.UInt8Array | FlowValueType.Int16Array |
                        FlowValueType.UInt16Array | FlowValueType.UInt32Array |
                        FlowValueType.Int64Array | FlowValueType.UInt64Array |
                        FlowValueType.Array;
                    if ((left & nonNumeric) != 0 || (right & nonNumeric) != 0)
                    {
                        return FlowValueType.Dynamic;
                    }
                    return CanKeepInt32Arithmetic(op, leftExpression, rightExpression, left, right)
                        ? FlowValueType.Int32
                        : FlowValueType.Number;
                }
                if (op == Operator.BitwiseOr)
                {
                    if ((left & FlowValueType.Null) == 0)
                    {
                        return FlowValueTypeFacts.IsNumeric(left) && FlowValueTypeFacts.IsNumeric(right)
                            ? FlowValueType.Int32
                            : FlowValueType.Number;
                    }
                    return left == FlowValueType.Null
                        ? right
                        : FlowValueTypeFacts.Merge(FlowValueType.Number, right);
                }
                if (op == Operator.Subtract || op == Operator.Multiply)
                {
                    return CanKeepInt32Arithmetic(op, leftExpression, rightExpression, left, right)
                        ? FlowValueType.Int32
                        : FlowValueType.Number;
                }
                if (op == Operator.BitwiseAnd || op == Operator.BitwiseXor ||
                    op == Operator.LeftShift || op == Operator.SignedRightShift)
                {
                    return FlowValueType.Int32;
                }
                if (op == Operator.Divide || op == Operator.Modulo ||
                    op == Operator.UnSignedRightShift)
                {
                    return FlowValueType.Number;
                }
                return FlowValueType.Dynamic;
            }

            private static bool CanKeepInt32Arithmetic(
                Operator op,
                Expression leftExpression,
                Expression rightExpression,
                FlowValueType left,
                FlowValueType right)
            {
                if (left != FlowValueType.Int32 || right != FlowValueType.Int32)
                {
                    return false;
                }
                if (TryEvaluateInt32Arithmetic(op, leftExpression, rightExpression, out _))
                {
                    return true;
                }
                if (op == Operator.Add)
                {
                    return IsInt32Constant(leftExpression, 0) ||
                        IsInt32Constant(rightExpression, 0);
                }
                if (op == Operator.Subtract)
                {
                    return IsInt32Constant(rightExpression, 0);
                }
                if (op == Operator.Multiply)
                {
                    return IsInt32Constant(leftExpression, 1) ||
                        IsInt32Constant(rightExpression, 1);
                }
                return false;
            }

            private static bool TryEvaluateInt32Arithmetic(
                Operator op,
                Expression leftExpression,
                Expression rightExpression,
                out int value)
            {
                if (!TryEvaluateInt32Constant(leftExpression, out var left) ||
                    !TryEvaluateInt32Constant(rightExpression, out var right))
                {
                    value = 0;
                    return false;
                }
                try
                {
                    if (op == Operator.Add) value = checked(left + right);
                    else if (op == Operator.Subtract) value = checked(left - right);
                    else if (op == Operator.Multiply) value = checked(left * right);
                    else
                    {
                        value = 0;
                        return false;
                    }
                    return true;
                }
                catch (OverflowException)
                {
                    value = 0;
                    return false;
                }
            }

            private static bool IsInt32Constant(Expression expression, int expected)
            {
                return TryEvaluateInt32Constant(expression, out var value) && value == expected;
            }

            private static bool TryEvaluateInt32Constant(Expression expression, out int value)
            {
                switch (expression)
                {
                    case LiteralExpression { Token: NumberToken number }
                        when IsExactInt32(number.NumberValue):
                        value = (int)number.NumberValue;
                        return true;
                    case UnaryExpression unary:
                        if (!TryEvaluateInt32Constant(unary.Expression, out var operand)) break;
                        if (unary.Operator == Operator.Negate &&
                            operand != 0 && operand != int.MinValue)
                        {
                            value = -operand;
                            return true;
                        }
                        if (unary.Operator == Operator.BitwiseNot)
                        {
                            value = ~operand;
                            return true;
                        }
                        break;
                    case BinaryExpression binary
                        when TryEvaluateInt32Constant(binary.Left, out var left) &&
                            TryEvaluateInt32Constant(binary.Right, out var right):
                        try
                        {
                            if (binary.Operator == Operator.Add) value = checked(left + right);
                            else if (binary.Operator == Operator.Subtract) value = checked(left - right);
                            else if (binary.Operator == Operator.Multiply) value = checked(left * right);
                            else if (binary.Operator == Operator.BitwiseAnd) value = left & right;
                            else if (binary.Operator == Operator.BitwiseOr) value = left | right;
                            else if (binary.Operator == Operator.BitwiseXor) value = left ^ right;
                            else if (binary.Operator == Operator.LeftShift) value = left << (right & 31);
                            else if (binary.Operator == Operator.SignedRightShift) value = left >> (right & 31);
                            else
                            {
                                value = 0;
                                return false;
                            }
                            return true;
                        }
                        catch (OverflowException)
                        {
                            break;
                        }
                }
                value = 0;
                return false;
            }

            private static bool IsExactInt32(double value)
            {
                return value >= int.MinValue && value <= int.MaxValue &&
                    value == Math.Truncate(value) &&
                    (value != 0d || BitConverter.DoubleToInt64Bits(value) >= 0);
            }

            private FlowValueType AnalyzeUnary(UnaryExpression expression, FlowValueType operand)
            {
                var op = expression.Operator;
                if (op == Operator.LogicalNot) return FlowValueType.Boolean;
                if (op == Operator.TypeOf) return FlowValueType.String;
                if (op == Operator.BitwiseNot) return FlowValueType.Int32;
                if (IsMutation(op))
                {
                    var writeType = GetMutationWriteType(expression);
                    return op == Operator.PostIncrement || op == Operator.PostDecrement
                        ? writeType == FlowValueType.Int32 ? FlowValueType.Int32 : operand
                        : writeType;
                }
                if (op == Operator.Negate)
                {
                    return TryEvaluateInt32Constant(expression, out _)
                        ? FlowValueType.Int32
                        : FlowValueType.Number;
                }
                return operand;
            }

            private FlowValueType GetMutationWriteType(UnaryExpression expression)
            {
                return expression.Expression is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal &&
                    _safeInt32Mutations.Contains(binding.Local.Value)
                        ? FlowValueType.Int32
                        : FlowValueType.Number;
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
                    NumberToken number => IsExactInt32(number.NumberValue)
                        ? FlowValueType.Int32
                        : FlowValueType.Number,
                    StringToken => FlowValueType.String,
                    RegexToken => FlowValueType.Object,
                    _ => FlowValueType.Dynamic
                };
            }

            private sealed class SequentialReturnTypeAnalyzer
            {
                private readonly FunctionPlan _function;
                private readonly IReadOnlyDictionary<NameExpression, BoundName> _names;
                private readonly IReadOnlyDictionary<VariableDeclaration, LocalSlotId> _declarations;
                private readonly IReadOnlyDictionary<Expression, FlowValueType> _expressionTypes;
                private readonly DirectParameterType[] _parameterTypes;
                private readonly Func<LocalSlotId, bool> _isCaptured;
                private FlowValueType _returnType;
                private bool _sawReturn;
                private bool _valid = true;

                public SequentialReturnTypeAnalyzer(
                    FunctionPlan function,
                    IReadOnlyDictionary<NameExpression, BoundName> names,
                    IReadOnlyDictionary<VariableDeclaration, LocalSlotId> declarations,
                    IReadOnlyDictionary<Expression, FlowValueType> expressionTypes,
                    DirectParameterType[] parameterTypes,
                    Func<LocalSlotId, bool> isCaptured)
                {
                    _function = function;
                    _names = names;
                    _declarations = declarations;
                    _expressionTypes = expressionTypes;
                    _parameterTypes = parameterTypes;
                    _isCaptured = isCaptured;
                }

                public FlowValueType Analyze(Statement body)
                {
                    var locals = new FlowValueType[_function.LocalSlots.Length];
                    var parameterIndex = 0;
                    for (var i = 0; i < _function.LocalSlots.Length; i++)
                    {
                        var slot = _function.LocalSlots[i];
                        if (slot.IsParameter)
                        {
                            locals[i] = _parameterTypes != null &&
                                parameterIndex < _parameterTypes.Length
                                    ? _parameterTypes[parameterIndex].Type
                                    : FlowValueType.Dynamic;
                            parameterIndex++;
                        }
                        else if (_isCaptured(slot.Id))
                        {
                            locals[i] = FlowValueType.Dynamic;
                        }
                    }

                    AnalyzeStatement(body, locals);
                    return _valid && _sawReturn ? _returnType : FlowValueType.None;
                }

                private void AnalyzeStatement(Statement statement, FlowValueType[] locals)
                {
                    if (statement == null || !_valid) return;
                    switch (statement)
                    {
                        case BlockStatement block:
                            for (var i = 0; i < block.Statements.Count; i++)
                            {
                                AnalyzeStatement(block.Statements[i], locals);
                            }
                            return;
                        case VariableDeclaration declaration:
                            if (declaration.Pattern != null)
                            {
                                foreach (var slot in _function.LocalSlots)
                                {
                                    if (ReferenceEquals(slot.Declaration, declaration))
                                    {
                                        locals[slot.Id.Value] = FlowValueType.Dynamic;
                                    }
                                }
                            }
                            else if (_declarations.TryGetValue(declaration, out var slot))
                            {
                                locals[slot.Value] = declaration.Initializer == null
                                    ? FlowValueType.Null
                                    : AnalyzeExpression(declaration.Initializer, locals);
                            }
                            return;
                        case ExpressionStatement expression:
                            AnalyzeExpression(expression.Expression, locals);
                            return;
                        case ReturnStatement @return:
                            _sawReturn = true;
                            _returnType = FlowValueTypeFacts.Merge(
                                _returnType,
                                @return.Expression == null
                                    ? FlowValueType.Null
                                    : AnalyzeExpression(@return.Expression, locals));
                            return;
                        case IfStatement @if:
                            AnalyzeExpression(@if.Condition, locals);
                            var thenLocals = (FlowValueType[])locals.Clone();
                            var elseLocals = (FlowValueType[])locals.Clone();
                            AnalyzeStatement(@if.Body, thenLocals);
                            AnalyzeStatement(@if.Else, elseLocals);
                            MergeEnvironments(locals, thenLocals, elseLocals);
                            return;
                        case WhileStatement @while:
                            AnalyzeLoop(@while.Condition, @while.Body, null, locals);
                            return;
                        case ForStatement @for:
                            if (@for.Initializer is Statement initializerStatement)
                            {
                                AnalyzeStatement(initializerStatement, locals);
                            }
                            else if (@for.Initializer is Expression initializerExpression)
                            {
                                AnalyzeExpression(initializerExpression, locals);
                            }
                            AnalyzeLoop(@for.Condition, @for.Body, @for.Incrementor, locals);
                            return;
                        case ThrowStatement @throw:
                            AnalyzeExpression(@throw.Expression, locals);
                            return;
                        case DeleteStatement delete:
                            AnalyzeExpression(delete.Expression, locals);
                            return;
                        case FunctionDeclaration:
                            return;
                        case ForInStatement:
                        case TryStatement:
                            _valid = false;
                            return;
                    }
                }

                private void AnalyzeLoop(
                    Expression condition,
                    Statement body,
                    Expression increment,
                    FlowValueType[] locals)
                {
                    AnalyzeExpression(condition, locals);
                    var entry = (FlowValueType[])locals.Clone();
                    var state = (FlowValueType[])locals.Clone();
                    var passLimit = Math.Max(2, locals.Length + 1);
                    for (var pass = 0; pass < passLimit; pass++)
                    {
                        var bodyState = (FlowValueType[])state.Clone();
                        AnalyzeStatement(body, bodyState);
                        AnalyzeExpression(increment, bodyState);
                        var changed = false;
                        for (var i = 0; i < state.Length; i++)
                        {
                            var merged = FlowValueTypeFacts.Merge(entry[i], bodyState[i]);
                            if (merged == state[i]) continue;
                            state[i] = merged;
                            changed = true;
                        }
                        if (!changed) break;
                    }
                    Array.Copy(state, locals, state.Length);
                }

                private FlowValueType AnalyzeExpression(
                    Expression expression,
                    FlowValueType[] locals)
                {
                    if (expression == null) return FlowValueType.Null;
                    switch (expression)
                    {
                        case NameExpression name:
                            if (_names.TryGetValue(name, out var binding))
                            {
                                if (binding.HasConstant) return FromDatum(binding.Constant);
                                if (binding.IsLocal)
                                {
                                    var type = locals[binding.Local.Value];
                                    return type == FlowValueType.None
                                        ? FlowValueType.Dynamic
                                        : type;
                                }
                            }
                            return GetKnownType(expression);
                        case AssignmentExpression assignment:
                            var assigned = AnalyzeExpression(assignment.Right, locals);
                            if (TryGetLocal(assignment.Left, out var assignmentSlot))
                            {
                                locals[assignmentSlot.Value] = assigned;
                            }
                            return assigned;
                        case CompoundExpression compound:
                            var compoundLeft = AnalyzeExpression(compound.Left, locals);
                            var compoundRight = AnalyzeExpression(compound.Right, locals);
                            var compoundType = AnalyzeBinary(
                                compound.Operator.SimplerOperator,
                                compound.Left,
                                compound.Right,
                                compoundLeft,
                                compoundRight);
                            if (TryGetLocal(compound.Left, out var compoundSlot))
                            {
                                locals[compoundSlot.Value] = compoundType;
                            }
                            return compoundType;
                        case UnaryExpression unary:
                            var operand = AnalyzeExpression(unary.Expression, locals);
                            if (!IsMutation(unary.Operator))
                            {
                                return unary.Operator == Operator.LogicalNot
                                    ? FlowValueType.Boolean
                                    : unary.Operator == Operator.TypeOf
                                        ? FlowValueType.String
                                        : unary.Operator == Operator.Negate ||
                                            unary.Operator == Operator.BitwiseNot
                                                ? FlowValueType.Number
                                                : operand;
                            }
                            if (TryGetLocal(unary.Expression, out var mutationSlot))
                            {
                                locals[mutationSlot.Value] = FlowValueType.Number;
                            }
                            return unary.Operator == Operator.PostIncrement ||
                                unary.Operator == Operator.PostDecrement
                                    ? operand
                                    : FlowValueType.Number;
                        case BinaryExpression binary:
                            return AnalyzeBinary(
                                binary.Operator,
                                binary.Left,
                                binary.Right,
                                AnalyzeExpression(binary.Left, locals),
                                AnalyzeExpression(binary.Right, locals));
                        case GroupExpression group:
                            var groupType = FlowValueType.Null;
                            for (var i = 0; i < group.Expressions.Count; i++)
                            {
                                groupType = AnalyzeExpression(group.Expressions[i], locals);
                            }
                            return groupType;
                        default:
                            return GetKnownType(expression);
                    }
                }

                private FlowValueType GetKnownType(Expression expression)
                {
                    return _expressionTypes.TryGetValue(expression, out var type)
                        ? type
                        : FlowValueType.Dynamic;
                }

                private bool TryGetLocal(Expression expression, out LocalSlotId slot)
                {
                    if (expression is NameExpression name &&
                        _names.TryGetValue(name, out var binding) &&
                        binding.IsLocal)
                    {
                        slot = binding.Local;
                        return true;
                    }
                    slot = LocalSlotId.Invalid;
                    return false;
                }

                private static void MergeEnvironments(
                    FlowValueType[] target,
                    FlowValueType[] left,
                    FlowValueType[] right)
                {
                    for (var i = 0; i < target.Length; i++)
                    {
                        target[i] = FlowValueTypeFacts.Merge(left[i], right[i]);
                    }
                }
            }

            private sealed class ExactNumericDefinitionAnalyzer
            {
                private readonly FunctionPlan _function;
                private readonly IReadOnlyDictionary<NameExpression, BoundName> _names;
                private readonly IReadOnlyDictionary<Expression, FlowValueType> _expressionTypes;
                private readonly List<Expression>[] _definitions;
                private readonly bool[] _eligible;

                public ExactNumericDefinitionAnalyzer(
                    FunctionPlan function,
                    IReadOnlyDictionary<NameExpression, BoundName> names,
                    IReadOnlyDictionary<Expression, FlowValueType> expressionTypes,
                    Func<LocalSlotId, bool> isCaptured)
                {
                    _function = function;
                    _names = names;
                    _expressionTypes = expressionTypes;
                    _definitions = new List<Expression>[function.LocalSlots.Length];
                    _eligible = new bool[function.LocalSlots.Length];
                    for (var i = 0; i < function.LocalSlots.Length; i++)
                    {
                        var slot = function.LocalSlots[i];
                        _definitions[i] = new List<Expression>();
                        _eligible[i] = !slot.IsParameter &&
                            !isCaptured(slot.Id) &&
                            slot.Declaration is not VariableDeclaration { Pattern: not null };
                    }
                }

                public bool[] Analyze(AstNode body)
                {
                    Visit(body);
                    for (var i = 0; i < _eligible.Length; i++)
                    {
                        if (!_eligible[i] || _definitions[i].Count == 0)
                        {
                            _eligible[i] = false;
                            continue;
                        }
                        for (var j = 0; j < _definitions[i].Count; j++)
                        {
                            var definition = _definitions[i][j];
                            if (definition != null &&
                                _expressionTypes.TryGetValue(definition, out var type) &&
                                FlowValueTypeFacts.IsNumeric(type))
                            {
                                continue;
                            }
                            _eligible[i] = false;
                            break;
                        }
                    }
                    return _eligible;
                }

                private void Visit(AstNode node)
                {
                    if (node == null || node is FunctionDeclaration or LambdaExpression)
                    {
                        return;
                    }
                    switch (node)
                    {
                        case VariableDeclaration declaration:
                            for (var i = 0; i < _function.LocalSlots.Length; i++)
                            {
                                if (!ReferenceEquals(
                                    _function.LocalSlots[i].Declaration,
                                    declaration))
                                {
                                    continue;
                                }
                                if (declaration.Pattern != null)
                                {
                                    _eligible[i] = false;
                                }
                                else
                                {
                                    _definitions[i].Add(declaration.Initializer);
                                }
                            }
                            break;
                        case AssignmentExpression assignment
                            when TryGetLocal(assignment.Left, out var assignmentSlot):
                            _definitions[assignmentSlot.Value].Add(assignment.Right);
                            break;
                        case CompoundExpression compound
                            when TryGetLocal(compound.Left, out var compoundSlot):
                            _definitions[compoundSlot.Value].Add(compound);
                            break;
                        case UnaryExpression unary
                            when IsMutation(unary.Operator) &&
                                TryGetLocal(unary.Expression, out _):
                            // Increment/decrement always stores a Number, including
                            // when the previous ScriptDatum was null or a string.
                            break;
                        case ForInStatement forIn
                            when TryGetLocal(forIn.Iterator?.Left, out var iteratorSlot):
                            _eligible[iteratorSlot.Value] = false;
                            break;
                    }
                    var visitor = new DefinitionChildVisitor(this);
                    AstTraversal.VisitChildren(node, ref visitor);
                }

                private bool TryGetLocal(Expression expression, out LocalSlotId slot)
                {
                    if (expression is NameExpression name &&
                        _names.TryGetValue(name, out var binding) &&
                        binding.IsLocal)
                    {
                        slot = binding.Local;
                        return true;
                    }
                    slot = LocalSlotId.Invalid;
                    return false;
                }

                private readonly struct DefinitionChildVisitor : IAstChildVisitor
                {
                    private readonly ExactNumericDefinitionAnalyzer _owner;

                    public DefinitionChildVisitor(ExactNumericDefinitionAnalyzer owner)
                    {
                        _owner = owner;
                    }

                    public void Visit(AstNode node)
                    {
                        _owner.Visit(node);
                    }
                }
            }

            private sealed class LocalCoercionAnalyzer
            {
                private readonly FunctionPlan _function;
                private readonly IReadOnlyDictionary<NameExpression, BoundName> _names;
                private readonly IReadOnlyDictionary<Expression, FlowValueType> _expressionTypes;
                private readonly DirectParameterType[][] _directParameters;
                private readonly Func<LocalSlotId, bool> _isCaptured;
                private readonly NativeCoercionKind[] _demands;
                private readonly bool[] _invalid;

                public LocalCoercionAnalyzer(
                    FunctionPlan function,
                    IReadOnlyDictionary<NameExpression, BoundName> names,
                    IReadOnlyDictionary<Expression, FlowValueType> expressionTypes,
                    DirectParameterType[][] directParameters,
                    Func<LocalSlotId, bool> isCaptured)
                {
                    _function = function;
                    _names = names;
                    _expressionTypes = expressionTypes;
                    _directParameters = directParameters;
                    _isCaptured = isCaptured;
                    _demands = new NativeCoercionKind[function.LocalSlots.Length];
                    _invalid = new bool[function.LocalSlots.Length];
                    for (var i = 0; i < function.LocalSlots.Length; i++)
                    {
                        var slot = function.LocalSlots[i];
                        _invalid[i] = slot.IsParameter ||
                            isCaptured(slot.Id) ||
                            slot.Declaration is VariableDeclaration { Pattern: not null };
                    }
                }

                public NativeCoercionKind[] Analyze(AstNode body)
                {
                    Visit(body);
                    for (var i = 0; i < _demands.Length; i++)
                    {
                        if (_invalid[i]) _demands[i] = NativeCoercionKind.None;
                    }
                    return _demands;
                }

                private void Visit(AstNode node)
                {
                    if (node == null || node is FunctionDeclaration or LambdaExpression)
                    {
                        return;
                    }
                    if (node is NameExpression name)
                    {
                        RecordUse(name);
                    }
                    var visitor = new ChildVisitor(this);
                    AstTraversal.VisitChildren(node, ref visitor);
                }

                private void RecordUse(NameExpression name)
                {
                    if (!_names.TryGetValue(name, out var binding) ||
                        !binding.IsLocal ||
                        (uint)binding.Local.Value >= (uint)_demands.Length ||
                        _invalid[binding.Local.Value])
                    {
                        return;
                    }

                    AstNode current = name;
                    while (current.Parent is GroupExpression group &&
                        group.Expressions.Count == 1 &&
                        ReferenceEquals(group.Expression, current))
                    {
                        current = group;
                    }

                    // A discarded simple assignment can convert at the store.
                    // Any observable assignment result, compound mutation, or
                    // increment/decrement must retain full ScriptDatum semantics.
                    if (current.Parent is AssignmentExpression assignment &&
                        ReferenceEquals(assignment.Left, current))
                    {
                        if (assignment.Parent is ExpressionStatement)
                        {
                            return;
                        }
                        _invalid[binding.Local.Value] = true;
                        return;
                    }
                    if (current.Parent is CompoundExpression compound &&
                            ReferenceEquals(compound.Left, current) ||
                        current.Parent is UnaryExpression mutation &&
                            ReferenceEquals(mutation.Expression, current) &&
                            IsMutation(mutation.Operator))
                    {
                        _invalid[binding.Local.Value] = true;
                        return;
                    }

                    // Property/element stores and returns can box at the
                    // boundary. They must not pin the local to ScriptDatum when
                    // every observable use already demands a number or boolean.
                    if (IsBoundaryValueUse(current))
                    {
                        return;
                    }

                    var demand = GetUseDemand(current);
                    if (demand == NativeCoercionKind.None)
                    {
                        _invalid[binding.Local.Value] = true;
                        return;
                    }
                    var existing = _demands[binding.Local.Value];
                    if (existing == NativeCoercionKind.None)
                    {
                        _demands[binding.Local.Value] = demand;
                    }
                    else if (existing != demand)
                    {
                        _invalid[binding.Local.Value] = true;
                    }
                }

                private NativeCoercionKind GetUseDemand(AstNode current)
                {
                    if (current.Parent is BinaryExpression binary &&
                        (ReferenceEquals(binary.Left, current) ||
                            ReferenceEquals(binary.Right, current)))
                    {
                        var demand = GetBinaryOperandDemand(
                            binary.Operator,
                            ReferenceEquals(binary.Left, current) ? binary.Right : binary.Left);
                        if (demand != NativeCoercionKind.None)
                        {
                            return demand;
                        }
                    }
                    if (current.Parent is CompoundExpression compound &&
                        ReferenceEquals(compound.Right, current))
                    {
                        var demand = GetBinaryOperandDemand(
                            compound.Operator.SimplerOperator,
                            compound.Left);
                        if (demand != NativeCoercionKind.None)
                        {
                            return demand;
                        }
                    }
                    if (IsNumericIndexDemand(current))
                    {
                        return NativeCoercionKind.ArithmeticNumber;
                    }
                    if (current.Parent is UnaryExpression unary &&
                        ReferenceEquals(unary.Expression, current))
                    {
                        if (unary.Operator == Operator.Negate)
                        {
                            return NativeCoercionKind.ArithmeticNumber;
                        }
                        if (unary.Operator == Operator.LogicalNot)
                        {
                            return NativeCoercionKind.Boolean;
                        }
                    }
                    if (current.Parent is IfStatement @if &&
                        ReferenceEquals(@if.Condition, current) ||
                        current.Parent is WhileStatement @while &&
                            ReferenceEquals(@while.Condition, current) ||
                        current.Parent is ForStatement @for &&
                            ReferenceEquals(@for.Condition, current))
                    {
                        return NativeCoercionKind.Boolean;
                    }
                    if (current.Parent is FunctionCallExpression call)
                    {
                        var argumentIndex = -1;
                        for (var i = 0; i < call.Arguments.Count; i++)
                        {
                            if (!ReferenceEquals(call.Arguments[i], current)) continue;
                            argumentIndex = i;
                            break;
                        }
                        if (argumentIndex >= 0 &&
                            call.Target is NameExpression target &&
                            _names.TryGetValue(target, out var targetBinding) &&
                            targetBinding.DirectFunction.IsValid &&
                            _directParameters != null &&
                            (uint)targetBinding.DirectFunction.Value <
                                (uint)_directParameters.Length)
                        {
                            var parameters = _directParameters[targetBinding.DirectFunction.Value];
                            if (parameters != null && argumentIndex < parameters.Length)
                            {
                                var parameter = parameters[argumentIndex];
                                if (parameter.Coercion is NativeCoercionKind.ArithmeticNumber or
                                    NativeCoercionKind.Boolean)
                                {
                                    return parameter.Coercion;
                                }
                                if (FlowValueTypeFacts.IsNumeric(parameter.Type))
                                {
                                    return NativeCoercionKind.ArithmeticNumber;
                                }
                                if (parameter.Type == FlowValueType.Boolean)
                                {
                                    return NativeCoercionKind.Boolean;
                                }
                            }
                        }
                    }
                    return NativeCoercionKind.None;
                }

                private static bool IsBoundaryValueUse(AstNode current)
                {
                    if (current.Parent is ReturnStatement)
                    {
                        return true;
                    }
                    if (current.Parent is SetPropertyExpression setProperty &&
                        ReferenceEquals(setProperty.Value, current))
                    {
                        return true;
                    }
                    return current.Parent is SetElementExpression setElement &&
                        ReferenceEquals(setElement.Value, current);
                }

                private NativeCoercionKind GetBinaryOperandDemand(Operator op, Expression sibling)
                {
                    if (op == Operator.Subtract ||
                        op == Operator.Multiply ||
                        op == Operator.Divide ||
                        op == Operator.Modulo ||
                        op == Operator.LessThan ||
                        op == Operator.LessThanOrEqual ||
                        op == Operator.GreaterThan ||
                        op == Operator.GreaterThanOrEqual)
                    {
                        return NativeCoercionKind.ArithmeticNumber;
                    }
                    if (op == Operator.Add ||
                        op == Operator.Equal ||
                        op == Operator.NotEqual)
                    {
                        var siblingType = GetExpressionType(sibling);
                        if (siblingType == FlowValueType.String)
                        {
                            return NativeCoercionKind.None;
                        }
                        if (FlowValueTypeFacts.IsNumeric(siblingType) ||
                            siblingType == FlowValueType.Boolean)
                        {
                            return NativeCoercionKind.ArithmeticNumber;
                        }
                    }
                    return NativeCoercionKind.None;
                }

                private bool IsNumericIndexDemand(AstNode current)
                {
                    Expression objectExpression = null;
                    if (current.Parent is GetElementExpression getElement &&
                        ReferenceEquals(getElement.Index, current))
                    {
                        objectExpression = getElement.Object;
                    }
                    else if (current.Parent is SetElementExpression setElement &&
                        ReferenceEquals(setElement.Index, current))
                    {
                        objectExpression = setElement.Object;
                    }
                    if (objectExpression == null)
                    {
                        return false;
                    }
                    var objectType = GetExpressionType(objectExpression);
                    return objectType == FlowValueType.Array ||
                        FlowValueTypeFacts.IsPackedArray(objectType);
                }

                private FlowValueType GetExpressionType(Expression expression)
                {
                    return expression != null &&
                        _expressionTypes.TryGetValue(expression, out var type)
                        ? type
                        : FlowValueType.Dynamic;
                }

                private readonly struct ChildVisitor : IAstChildVisitor
                {
                    private readonly LocalCoercionAnalyzer _owner;

                    public ChildVisitor(LocalCoercionAnalyzer owner)
                    {
                        _owner = owner;
                    }

                    public void Visit(AstNode node)
                    {
                        _owner.Visit(node);
                    }
                }
            }

            private static FlowValueType FromDatum(ScriptDatum datum)
            {
                return datum.Kind switch
                {
                    ValueKind.Null => FlowValueType.Null,
                    ValueKind.Boolean => FlowValueType.Boolean,
                    ValueKind.Number => IsExactInt32(datum.Number)
                        ? FlowValueType.Int32
                        : FlowValueType.Number,
                    ValueKind.String => FlowValueType.String,
                    _ => datum.Reference switch
                    {
                        Runtime.Types.ScriptInt32Array => FlowValueType.Int32Array,
                        Runtime.Types.ScriptInt8Array => FlowValueType.Int8Array,
                        Runtime.Types.ScriptFloat64Array => FlowValueType.Float64Array,
                        Runtime.Types.ScriptBooleanArray => FlowValueType.BooleanArray,
                        Runtime.Types.ScriptUInt8Array => FlowValueType.UInt8Array,
                        Runtime.Types.ScriptInt16Array => FlowValueType.Int16Array,
                        Runtime.Types.ScriptUInt16Array => FlowValueType.UInt16Array,
                        Runtime.Types.ScriptUInt32Array => FlowValueType.UInt32Array,
                        Runtime.Types.ScriptInt64Array => FlowValueType.Int64Array,
                        Runtime.Types.ScriptUInt64Array => FlowValueType.UInt64Array,
                        Runtime.Types.ScriptArray => FlowValueType.Array,
                        _ => FlowValueType.Object
                    }
                };
            }
        }
    }
}
