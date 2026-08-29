using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AuroraScript.Compiler.Backend.Code
{
    internal static class TypedFunctionBuilder
    {
        public static TypedFunctionCode Build(
            ModulePlan module,
            FunctionPlan function)
        {
            return Build(
                module,
                function,
                new HostExportCatalog(Array.Empty<System.Reflection.Assembly>()));
        }

        public static TypedFunctionCode Build(
            ModulePlan module,
            FunctionPlan function,
            HostExportCatalog hostExports,
            DirectParameterType[] parameterTypes = null,
            IReadOnlyDictionary<FunctionId, FlowValueType> directReturnTypes = null,
            DirectParameterType[][] directParameterTypes = null,
            IReadOnlyDictionary<FunctionId, FlowValueType> universalReturnTypes = null,
            IReadOnlyDictionary<FunctionId, FlowValueType[]> upvalueTypes = null)
        {
            ArgumentNullException.ThrowIfNull(module);
            ArgumentNullException.ThrowIfNull(function);
            ArgumentNullException.ThrowIfNull(hostExports);

            var binder = new NameBinder(module, function);
            binder.Bind();
            var analyzer = new TypeAnalyzer(
                module,
                function,
                binder.Names,
                binder.Declarations,
                hostExports,
                parameterTypes,
                directReturnTypes,
                directParameterTypes,
                universalReturnTypes,
                upvalueTypes != null &&
                    upvalueTypes.TryGetValue(function.Id, out var functionUpvalues)
                        ? functionUpvalues
                        : null);
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
                        case CheckExpression check:
                            BindExpression(check.Value);
                            break;
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
            private readonly ModulePlan _module;
            private readonly FunctionPlan _function;
            private readonly Dictionary<NameExpression, BoundName> _names;
            private readonly Dictionary<VariableDeclaration, LocalSlotId> _declarations;
            private readonly HostExportCatalog _hostExports;
            private readonly Dictionary<Expression, FlowValueType> _expressionTypes;
            private readonly Dictionary<Expression, TypeDeclaration> _structuralTypes;
            private readonly Dictionary<Expression, HostNativeObjectDescriptor> _nativeObjectTypes;
            private readonly FlowValueType[] _locals;
            private readonly TypeDeclaration[] _localStructuralTypes;
            private readonly HostNativeObjectDescriptor[] _localNativeObjectTypes;
            private readonly FlowValueType[] _forcedLocalTypes;
            private readonly bool[] _writtenLocals;
            private readonly DirectParameterType[] _parameterTypes;
            private readonly IReadOnlyDictionary<FunctionId, FlowValueType> _directReturnTypes;
            private readonly IReadOnlyDictionary<FunctionId, FlowValueType> _universalReturnTypes;
            private readonly DirectParameterType[][] _directParameterTypes;
            private readonly FlowValueType[] _upvalueTypes;
            private readonly HashSet<int> _safeInt32Mutations;
            private readonly HashSet<int> _safeInt64Mutations;
            private readonly Dictionary<int, int> _provenStringIndices;
            private readonly Dictionary<ForStatement, CountedLoop> _countedLoops;
            private readonly Dictionary<int, Dictionary<string, FlowValueType>> _localFields;
            private readonly HashSet<int> _invalidLocalFields;
            private readonly Dictionary<int, FlowValueType> _localArrayElements;
            private readonly HashSet<int> _invalidLocalArrayElements;
            private readonly bool _optimisticDirect;
            private bool _changed;
            private FlowValueType _passReturnType;
            private bool _sawReturn;

            public TypeAnalyzer(
                ModulePlan module,
                FunctionPlan function,
                Dictionary<NameExpression, BoundName> names,
                Dictionary<VariableDeclaration, LocalSlotId> declarations,
                HostExportCatalog hostExports,
                DirectParameterType[] parameterTypes,
                IReadOnlyDictionary<FunctionId, FlowValueType> directReturnTypes,
                DirectParameterType[][] directParameterTypes,
                IReadOnlyDictionary<FunctionId, FlowValueType> universalReturnTypes,
                FlowValueType[] upvalueTypes)
            {
                _module = module;
                _function = function;
                _upvalueTypes = upvalueTypes;
                _names = names;
                _declarations = declarations;
                _hostExports = hostExports;
                _expressionTypes = new Dictionary<Expression, FlowValueType>(ReferenceEqualityComparer.Instance);
                _structuralTypes = new Dictionary<Expression, TypeDeclaration>(ReferenceEqualityComparer.Instance);
                _nativeObjectTypes = new Dictionary<Expression, HostNativeObjectDescriptor>(
                    ReferenceEqualityComparer.Instance);
                _locals = new FlowValueType[function.LocalSlots.Length];
                _localStructuralTypes = new TypeDeclaration[function.LocalSlots.Length];
                _localNativeObjectTypes = new HostNativeObjectDescriptor[function.LocalSlots.Length];
                _forcedLocalTypes = new FlowValueType[function.LocalSlots.Length];
                _writtenLocals = new bool[function.LocalSlots.Length];
                _parameterTypes = parameterTypes;
                _directReturnTypes = directReturnTypes;
                _universalReturnTypes = universalReturnTypes;
                _directParameterTypes = directParameterTypes;
                _optimisticDirect = parameterTypes != null;
                _safeInt32Mutations = new HashSet<int>();
                _safeInt64Mutations = new HashSet<int>();
                _provenStringIndices = new Dictionary<int, int>();
                _countedLoops = new Dictionary<ForStatement, CountedLoop>(
                    ReferenceEqualityComparer.Instance);
                _localFields = new Dictionary<int, Dictionary<string, FlowValueType>>();
                _invalidLocalFields = new HashSet<int>();
                _localArrayElements = new Dictionary<int, FlowValueType>();
                _invalidLocalArrayElements = new HashSet<int>();

                var parameterIndex = 0;
                for (var i = 0; i < function.LocalSlots.Length; i++)
                {
                    if (function.LocalSlots[i].IsParameter)
                    {
                        var checkedType = function.LocalSlots[i].Declaration is
                            ParameterDeclaration parameter
                                ? TypeReferenceFacts.GetFlowType(
                                    module.Declaration,
                                    parameter.DeclaredType)
                                : FlowValueType.None;
                        var directParameter = parameterTypes != null &&
                            parameterIndex < parameterTypes.Length &&
                            parameterTypes[parameterIndex].Type != FlowValueType.None
                                ? parameterTypes[parameterIndex]
                                : default;
                        _locals[i] = checkedType == FlowValueType.Number &&
                            directParameter.IsInt32Coercion
                            ? FlowValueType.Int32
                            : checkedType != FlowValueType.None
                                ? checkedType
                                : directParameter.Type != FlowValueType.None
                                    ? FlowValueTypeFacts.GetDirectLocalType(directParameter)
                                    : FlowValueType.Dynamic;
                        if (function.LocalSlots[i].Declaration is
                                ParameterDeclaration typedParameter &&
                            TypeReferenceFacts.TryGetCustomType(
                                module.Declaration,
                                typedParameter.DeclaredType,
                                out var parameterStructuralType))
                        {
                            _localStructuralTypes[i] = parameterStructuralType;
                        }
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
                    _structuralTypes.Clear();
                    _nativeObjectTypes.Clear();
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
                        _structuralTypes.Clear();
                        _nativeObjectTypes.Clear();
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
                var declaredReturnType = FlowValueTypeFacts.FromCheckedTypeName(
                    _function.Declaration?.ReturnType?.Name);
                if (declaredReturnType == FlowValueType.None)
                {
                    declaredReturnType = TypeReferenceFacts.GetFlowType(
                        _module.Declaration,
                        _function.Declaration?.ReturnType);
                }
                if (declaredReturnType != FlowValueType.None &&
                    !(_optimisticDirect &&
                        declaredReturnType == FlowValueType.Number &&
                        FlowValueTypeFacts.IsNumeric(returnType)))
                {
                    returnType = declaredReturnType;
                }
                return new TypedFunctionCode(
                    _function,
                    _names,
                    _declarations,
                    _expressionTypes,
                    _structuralTypes,
                    _nativeObjectTypes,
                    _locals,
                    _localStructuralTypes,
                    _localNativeObjectTypes,
                    _writtenLocals,
                    returnType,
                    _countedLoops);
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
                            if (!_writtenLocals[slot.Value] &&
                                variable.Initializer != null &&
                                _structuralTypes.TryGetValue(
                                    variable.Initializer,
                                    out var initializerStructuralType))
                            {
                                _localStructuralTypes[slot.Value] =
                                    initializerStructuralType;
                            }
                            if (!_writtenLocals[slot.Value] &&
                                !IsCaptured(slot) &&
                                variable.Initializer != null &&
                                _nativeObjectTypes.TryGetValue(
                                    variable.Initializer,
                                    out var initializerNativeType))
                            {
                                _localNativeObjectTypes[slot.Value] = initializerNativeType;
                            }
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
                        var ifBefore = SnapshotStructural();
                        AnalyzeStatement(@if.Body);
                        var thenStructural = SnapshotStructural();
                        RestoreStructural(ifBefore);
                        AnalyzeStatement(@if.Else);
                        IntersectStructural(thenStructural);
                        return;
                    case WhileStatement @while:
                        AnalyzeExpression(@while.Condition);
                        var whileBefore = SnapshotStructural();
                        var whileInt32Slots = GetSafeInt32WhileMutations(@while);
                        for (var i = 0; i < whileInt32Slots.Count; i++)
                        {
                            _safeInt32Mutations.Add(whileInt32Slots[i]);
                        }
                        try
                        {
                            AnalyzeStatement(@while.Body);
                        }
                        finally
                        {
                            for (var i = 0; i < whileInt32Slots.Count; i++)
                            {
                                _safeInt32Mutations.Remove(whileInt32Slots[i]);
                            }
                        }
                        IntersectStructural(whileBefore);
                        return;
                    case ForStatement @for:
                        if (@for.Initializer is Statement initializerStatement) AnalyzeStatement(initializerStatement);
                        else if (@for.Initializer is Expression initializerExpression) AnalyzeExpression(initializerExpression);
                        AnalyzeExpression(@for.Condition);
                        var forBefore = SnapshotStructural();
                        if (TryGetSafeInt32Induction(@for, out var inductionSlot))
                        {
                            _safeInt32Mutations.Add(inductionSlot.Value);
                            var hasStringIndex = TryGetProvenStringIndex(
                                @for,
                                inductionSlot,
                                out var stringSlot);
                            if (hasStringIndex)
                            {
                                _provenStringIndices[inductionSlot.Value] = stringSlot.Value;
                            }
                            try
                            {
                                AnalyzeStatement(@for.Body);
                                AnalyzeExpression(@for.Incrementor);
                            }
                            finally
                            {
                                if (hasStringIndex)
                                {
                                    _provenStringIndices.Remove(inductionSlot.Value);
                                }
                                _safeInt32Mutations.Remove(inductionSlot.Value);
                            }
                        }
                        else if (TryGetCountedInt64Loop(@for, out var countedSlot, out var countedBound))
                        {
                            _countedLoops[@for] = new CountedLoop(countedSlot, countedBound);
                            _safeInt64Mutations.Add(countedSlot.Value);
                            try
                            {
                                AnalyzeStatement(@for.Body);
                                AnalyzeExpression(@for.Incrementor);
                            }
                            finally
                            {
                                _safeInt64Mutations.Remove(countedSlot.Value);
                            }
                        }
                        else
                        {
                            _countedLoops.Remove(@for);
                            AnalyzeStatement(@for.Body);
                            AnalyzeExpression(@for.Incrementor);
                        }
                        IntersectStructural(forBefore);
                        return;
                    case ForInStatement forIn:
                        AnalyzeStatement(forIn.Initializer);
                        AnalyzeExpression(forIn.Iterator?.Right);
                        if (forIn.Iterator?.Left != null && _names.TryGetValue(forIn.Iterator.Left, out var iterator))
                        {
                            if (iterator.Local.IsValid) _writtenLocals[iterator.Local.Value] = true;
                            MergeLocal(iterator.Local, FlowValueType.Dynamic);
                        }
                        var forInBefore = SnapshotStructural();
                        AnalyzeStatement(forIn.Body);
                        IntersectStructural(forInBefore);
                        return;
                    case TryStatement @try:
                        var tryBefore = SnapshotStructural();
                        AnalyzeStatement(@try.Body);
                        if (@try.CatchBody != null)
                        {
                            var afterTry = SnapshotStructural();
                            RestoreStructural(tryBefore);
                            AnalyzeStatement(@try.CatchBody);
                            IntersectStructural(afterTry);
                        }
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
                    case CheckExpression check:
                        AnalyzeExpression(check.Value);
                        type = TypeReferenceFacts.GetFlowType(
                            _module.Declaration,
                            check.AssertedType);
                        break;
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
                        var inductionArithmetic = GetInductionArithmeticType(binary);
                        type = inductionArithmetic != FlowValueType.None
                            ? inductionArithmetic
                            : AnalyzeBinary(
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
                        _structuralTypes.TryGetValue(
                            assignment.Right,
                            out var assignedStructuralType);
                        _nativeObjectTypes.TryGetValue(
                            assignment.Right,
                            out var assignedNativeType);
                        WriteTarget(
                            assignment.Left,
                            type,
                            assignedStructuralType,
                            assignedNativeType);
                        break;
                    case CompoundExpression compound:
                        var left = AnalyzeExpression(compound.Left);
                        var right = AnalyzeExpression(compound.Right);
                        if (compound.Operator.SimplerOperator == Operator.Add)
                        {
                            left = ApplyLocalArrayArithmeticDemand(compound.Left, left);
                            right = ApplyLocalArrayArithmeticDemand(compound.Right, right);
                        }
                        var inductionCompound = GetInductionCompoundType(compound);
                        type = inductionCompound != FlowValueType.None
                            ? inductionCompound
                            : AnalyzeBinary(
                                compound.Operator.SimplerOperator,
                                compound.Left,
                                compound.Right,
                                left,
                                right);
                        WriteTarget(compound.Left, type, null);
                        break;
                    case UnaryExpression unary:
                        var operand = AnalyzeExpression(unary.Expression);
                        type = AnalyzeUnary(unary, operand);
                        if (IsMutation(unary.Operator))
                        {
                            WriteTarget(
                                unary.Expression,
                                GetMutationWriteType(unary),
                                null);
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
                        else if (IsNativeStringCharCodeAtCall(call))
                        {
                            type = IsProvenStringCharCodeAtCall(call)
                                ? FlowValueType.Int32
                                : FlowValueType.Number;
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
                        else if (_function.ImportedNativeCalls.TryGetValue(
                                call,
                                out var importedNative) &&
                            CanUseImportedNativeCall(
                                call,
                                importedNative))
                        {
                            type = TypeReferenceFacts.GetFlowType(
                                importedNative.Declaration.Parent
                                    as ModuleDeclaration,
                                importedNative.Declaration.ReturnType);
                            if (type == FlowValueType.None)
                            {
                                type = FlowValueType.Dynamic;
                            }
                        }
                        else if (TryGetNativeMethodCall(call, out _, out var nativeMethod))
                        {
                            type = GetNativeFlowType(nativeMethod.ReturnKind);
                        }
                        else if (TryGetHostExport(call, out var hostExport))
                        {
                            type = hostExport.ReturnKind switch
                            {
                                AuroraExportValueKind.Void => FlowValueType.Null,
                                AuroraExportValueKind.Number => FlowValueType.Number,
                                AuroraExportValueKind.Int32 => FlowValueType.Int32,
                                AuroraExportValueKind.Boolean => FlowValueType.Boolean,
                                AuroraExportValueKind.String => FlowValueType.String,
                                AuroraExportValueKind.Object => FlowValueType.Object,
                                AuroraExportValueKind.Datum => FlowValueType.Dynamic,
                                _ => FlowValueType.Dynamic
                            };
                        }
                        else
                        {
                            type = FlowValueType.Dynamic;
                        }
                        break;
                    case GetPropertyExpression property:
                        var propertyObjectType = AnalyzeExpression(property.Object);
                        type = (FlowValueTypeFacts.IsPackedArray(propertyObjectType) ||
                                propertyObjectType == FlowValueType.Array ||
                                propertyObjectType == FlowValueType.String) &&
                            IsStaticProperty(property.Property, "length")
                                ? FlowValueType.Int32
                                : TryGetNativeMemberType(property, out var nativeMemberType)
                                    ? nativeMemberType
                                : TryGetStructuralFieldType(
                                    property.Object,
                                    property.Property,
                                    out var structuralFieldType)
                                    ? structuralFieldType
                                : TryGetLocalFieldType(property, out var fieldType)
                                    ? fieldType
                                : TryGetHostExportConstant(property)
                                    ? FlowValueType.Number
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
                var structuralType = InferStructuralType(expression);
                if (structuralType != null)
                {
                    _structuralTypes[expression] = structuralType;
                }
                var nativeObjectType = InferNativeObjectType(expression);
                if (nativeObjectType != null)
                {
                    _nativeObjectTypes[expression] = nativeObjectType;
                }
                return type;
            }

            private HostNativeObjectDescriptor InferNativeObjectType(Expression expression)
            {
                if (_hostExports == null)
                {
                    return null;
                }

                switch (expression)
                {
                    case NewExpression @new:
                        return TryGetNativeConstruction(@new, out var constructed)
                            ? constructed
                            : null;
                    case NameExpression name
                        when _names.TryGetValue(name, out var binding) && binding.IsLocal:
                        return _localNativeObjectTypes[binding.Local.Value];
                    case FunctionCallExpression call
                        when TryGetNativeMethodCall(call, out _, out var method) &&
                            method.ReturnKind == AuroraExportValueKind.Object &&
                            _hostExports.TryGetNativeObject(
                                method.Method.ReturnType,
                                out var returned):
                        return returned;
                    case AssignmentExpression assignment:
                        return _nativeObjectTypes.TryGetValue(assignment.Right, out var assigned)
                            ? assigned
                            : null;
                    case GroupExpression group when group.Expressions.Count != 0:
                        return _nativeObjectTypes.TryGetValue(
                            group.Expressions[group.Expressions.Count - 1],
                            out var grouped)
                                ? grouped
                                : null;
                    default:
                        return null;
                }
            }

            /// <summary>
            /// True when <c>new Name(...)</c> targets a generated native object whose
            /// constructor the emitter can call directly.
            /// </summary>
            private bool TryGetNativeConstruction(
                NewExpression expression,
                out HostNativeObjectDescriptor descriptor)
            {
                descriptor = null;
                var call = expression.Expression;
                if (_hostExports == null ||
                    call == null ||
                    call.Target is not NameExpression target ||
                    !_names.TryGetValue(target, out var binding) ||
                    !binding.IsUnshadowedGlobal ||
                    !_hostExports.TryGetNativeObject(
                        target.Identifier?.Value,
                        out var candidate) ||
                    candidate.Constructor == null ||
                    !CanBindNativeArguments(
                        call,
                        candidate.ConstructorParameterKinds,
                        candidate.RequiredConstructorParameterCount,
                        candidate.Constructor.GetParameters(),
                        prefix: 0))
                {
                    return false;
                }

                descriptor = candidate;
                return true;
            }

            /// <summary>
            /// True when <c>receiver.member(...)</c> resolves to an exported instance
            /// method of a proven native object.
            /// </summary>
            private bool TryGetNativeMethodCall(
                FunctionCallExpression call,
                out HostNativeObjectDescriptor owner,
                out HostNativeMethodDescriptor method)
            {
                owner = null;
                method = null;
                if (call?.Target is not GetPropertyExpression property ||
                    !TryGetStaticPropertyName(property.Property, out var name) ||
                    !_nativeObjectTypes.TryGetValue(property.Object, out var receiver) ||
                    !receiver.TryGetMethod(name, out var candidate) ||
                    !CanBindNativeArguments(
                        call,
                        candidate.ParameterKinds,
                        candidate.RequiredScriptParameterCount,
                        candidate.Method.GetParameters(),
                        prefix: candidate.TakesContext ? 1 : 0))
                {
                    return false;
                }

                owner = receiver;
                method = candidate;
                return true;
            }

            private bool CanBindNativeArguments(
                FunctionCallExpression call,
                AuroraExportValueKind[] parameterKinds,
                int requiredCount,
                ParameterInfo[] clrParameters,
                int prefix)
            {
                if (HasSpreadArgument(call) || call.Arguments.Count < requiredCount)
                {
                    return false;
                }

                var provided = Math.Min(call.Arguments.Count, parameterKinds.Length);
                for (var i = 0; i < provided; i++)
                {
                    var argument = call.Arguments[i];
                    if (!HostExportArgumentFacts.CanPass(
                            parameterKinds[i],
                            clrParameters[prefix + i].ParameterType,
                            _expressionTypes.TryGetValue(argument, out var argumentType)
                                ? argumentType
                                : FlowValueType.Dynamic,
                            _nativeObjectTypes.TryGetValue(argument, out var argumentNative)
                                ? argumentNative.ClrType
                                : null))
                    {
                        return false;
                    }
                }
                return true;
            }

            private static bool HasSpreadArgument(FunctionCallExpression call)
            {
                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    if (call.Arguments[i] is SpreadExpression)
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Native member access is only bound when the receiver is a proven native
            /// object and the member name matches an exported field or method.
            /// </summary>
            private bool TryGetNativeMemberType(
                GetPropertyExpression property,
                out FlowValueType type)
            {
                type = FlowValueType.Dynamic;
                if (!_nativeObjectTypes.TryGetValue(property.Object, out var receiver) ||
                    !TryGetStaticPropertyName(property.Property, out var name))
                {
                    return false;
                }

                if (receiver.TryGetField(name, out var field))
                {
                    type = GetNativeFlowType(field.Kind);
                    return type != FlowValueType.None;
                }
                if (receiver.TryGetMethod(name, out _))
                {
                    // A bare member reference still materializes a bound function.
                    type = FlowValueType.Object;
                    return true;
                }
                return false;
            }

            private static FlowValueType GetNativeFlowType(AuroraExportValueKind kind)
            {
                return kind switch
                {
                    AuroraExportValueKind.Void => FlowValueType.Null,
                    AuroraExportValueKind.Number => FlowValueType.Number,
                    AuroraExportValueKind.Int32 => FlowValueType.Int32,
                    AuroraExportValueKind.Boolean => FlowValueType.Boolean,
                    AuroraExportValueKind.String => FlowValueType.String,
                    AuroraExportValueKind.Object => FlowValueType.Object,
                    _ => FlowValueType.Dynamic
                };
            }

            private TypeDeclaration InferStructuralType(Expression expression)
            {
                if (expression is CheckExpression check &&
                    TypeReferenceFacts.TryGetCustomType(
                        _module.Declaration,
                        check.AssertedType,
                        out var asserted))
                {
                    return asserted;
                }

                if (expression is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal)
                {
                    return _localStructuralTypes[binding.Local.Value];
                }

                if (expression is FunctionCallExpression call &&
                    call.Target is NameExpression target &&
                    _names.TryGetValue(target, out var targetBinding) &&
                    targetBinding.DirectFunction.IsValid)
                {
                    for (var i = 0; i < _module.Functions.Count; i++)
                    {
                        var function = _module.Functions[i];
                        if (function.Id.Equals(targetBinding.DirectFunction) &&
                            TypeReferenceFacts.TryGetCustomType(
                                _module.Declaration,
                                function.Declaration.ReturnType,
                                out var returned))
                        {
                            return returned;
                        }
                    }
                }

                if (expression is GetPropertyExpression property)
                {
                    return InferStructuralFieldType(property);
                }

                if (expression is AssignmentExpression assignment &&
                    _structuralTypes.TryGetValue(assignment.Right, out var assigned))
                {
                    return assigned;
                }

                if (expression is MapExpression)
                {
                    return InferStructuralTypeFromContext(expression);
                }

                return null;
            }

            private TypeDeclaration InferStructuralFieldType(
                GetPropertyExpression property)
            {
                if (!_structuralTypes.TryGetValue(property.Object, out var owner) ||
                    !TryGetStaticPropertyName(property.Property, out var name))
                {
                    return null;
                }

                var module = GetTypeModule(owner);
                for (var i = 0; i < owner.Fields.Count; i++)
                {
                    var field = owner.Fields[i];
                    if (StringComparer.Ordinal.Equals(field.Name.Value, name) &&
                        TypeReferenceFacts.TryGetCustomType(
                            module,
                            field.Type,
                            out var nested))
                    {
                        return nested;
                    }
                }

                return null;
            }

            private static ModuleDeclaration GetTypeModule(TypeDeclaration declaration)
            {
                return declaration.Parent as ModuleDeclaration;
            }

            private TypeDeclaration InferStructuralTypeFromContext(Expression expression)
            {
                var parent = SkipGroups(expression.Parent);
                if (parent is ReturnStatement)
                {
                    return TypeReferenceFacts.TryGetCustomType(
                        _module.Declaration,
                        _function.Declaration?.ReturnType,
                        out var returned)
                            ? returned
                            : null;
                }

                if (parent is FunctionCallExpression call)
                {
                    for (var i = 0; i < call.Arguments.Count; i++)
                    {
                        if (ReferenceEquals(UnwrapGroups(call.Arguments[i]), expression))
                        {
                            return GetCallArgumentStructuralType(call, i);
                        }
                    }
                    return null;
                }

                if (parent is AssignmentExpression assignment &&
                    ReferenceEquals(UnwrapGroups(assignment.Right), expression))
                {
                    return InferStructuralType(assignment.Left);
                }

                if (parent is VariableDeclaration variable &&
                    ReferenceEquals(UnwrapGroups(variable.Initializer), expression) &&
                    _declarations.TryGetValue(variable, out var slot))
                {
                    return _localStructuralTypes[slot.Value];
                }

                return null;
            }

            private TypeDeclaration GetCallArgumentStructuralType(
                FunctionCallExpression call,
                int argumentIndex)
            {
                if (call.Target is not NameExpression target ||
                    !_names.TryGetValue(target, out var binding) ||
                    !binding.DirectFunction.IsValid)
                {
                    return null;
                }

                for (var i = 0; i < _module.Functions.Count; i++)
                {
                    var function = _module.Functions[i];
                    if (!function.Id.Equals(binding.DirectFunction) ||
                        function.Declaration == null ||
                        argumentIndex >= function.Declaration.Parameters.Count)
                    {
                        continue;
                    }

                    return TypeReferenceFacts.TryGetCustomType(
                        _module.Declaration,
                        function.Declaration.Parameters[argumentIndex].DeclaredType,
                        out var parameterType)
                            ? parameterType
                            : null;
                }

                return null;
            }

            private static Expression UnwrapGroups(Expression expression)
            {
                while (expression is GroupExpression group &&
                    group.Expressions.Count == 1)
                {
                    expression = group.Expressions[0];
                }
                return expression;
            }

            private static AstNode SkipGroups(AstNode node)
            {
                while (node is GroupExpression group)
                {
                    node = group.Parent;
                }
                return node;
            }

            /// <summary>
            /// Per-local shape facts that only survive a branch when both paths agree.
            /// </summary>
            private readonly struct ShapeSnapshot
            {
                public ShapeSnapshot(
                    TypeDeclaration[] structural,
                    HostNativeObjectDescriptor[] nativeObjects)
                {
                    Structural = structural;
                    NativeObjects = nativeObjects;
                }

                public TypeDeclaration[] Structural { get; }
                public HostNativeObjectDescriptor[] NativeObjects { get; }
            }

            private ShapeSnapshot SnapshotStructural()
            {
                var structural = new TypeDeclaration[_localStructuralTypes.Length];
                Array.Copy(_localStructuralTypes, structural, structural.Length);
                var nativeObjects =
                    new HostNativeObjectDescriptor[_localNativeObjectTypes.Length];
                Array.Copy(_localNativeObjectTypes, nativeObjects, nativeObjects.Length);
                return new ShapeSnapshot(structural, nativeObjects);
            }

            private void RestoreStructural(ShapeSnapshot snapshot)
            {
                Array.Copy(
                    snapshot.Structural,
                    _localStructuralTypes,
                    snapshot.Structural.Length);
                Array.Copy(
                    snapshot.NativeObjects,
                    _localNativeObjectTypes,
                    snapshot.NativeObjects.Length);
            }

            private void IntersectStructural(ShapeSnapshot other)
            {
                for (var i = 0; i < _localStructuralTypes.Length; i++)
                {
                    if (!ReferenceEquals(_localStructuralTypes[i], other.Structural[i]))
                    {
                        _localStructuralTypes[i] = null;
                        _changed = true;
                    }
                    if (!ReferenceEquals(_localNativeObjectTypes[i], other.NativeObjects[i]))
                    {
                        _localNativeObjectTypes[i] = null;
                        _changed = true;
                    }
                }
            }

            private bool TryGetStructuralFieldType(
                Expression owner,
                Expression property,
                out FlowValueType type)
            {
                type = FlowValueType.Dynamic;
                if (!_structuralTypes.TryGetValue(owner, out var declaration) ||
                    !TryGetStaticPropertyName(property, out var name))
                {
                    return false;
                }

                for (var i = 0; i < declaration.Fields.Count; i++)
                {
                    var field = declaration.Fields[i];
                    if (StringComparer.Ordinal.Equals(field.Name.Value, name))
                    {
                        var module = GetTypeModule(declaration);
                        type = TypeReferenceFacts.GetFlowType(module, field.Type);
                        return type != FlowValueType.None;
                    }
                }
                return false;
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
                        if (i >= call.Arguments.Count &&
                            HasDefaultParameter(function, i))
                        {
                            continue;
                        }
                        return false;
                    }
                }

                return true;
            }

            private bool HasDefaultParameter(
                FunctionId function,
                int parameterIndex)
            {
                for (var i = 0; i < _module.Functions.Count; i++)
                {
                    var candidate = _module.Functions[i];
                    if (candidate.Id.Equals(function))
                    {
                        return parameterIndex <
                                candidate.Declaration.Parameters.Count &&
                            candidate.Declaration.Parameters[parameterIndex]
                                .Initializer != null;
                    }
                }
                return false;
            }

            private bool CanUseImportedNativeCall(
                FunctionCallExpression call,
                FunctionPlan target)
            {
                var parameters = target.Declaration.Parameters;
                for (var i = 0; i < parameters.Count; i++)
                {
                    var type = TypeReferenceFacts.GetFlowType(
                        target.Declaration.Parent as ModuleDeclaration,
                        parameters[i].DeclaredType);
                    if (!RequiresNativeProof(type))
                    {
                        continue;
                    }
                    if (i >= call.Arguments.Count ||
                        !FlowValueTypeFacts.CanPassNativeArgument(
                            new DirectParameterType(type),
                            _expressionTypes.TryGetValue(
                                call.Arguments[i],
                                out var argumentType)
                                    ? argumentType
                                    : FlowValueType.Dynamic))
                    {
                        if (i >= call.Arguments.Count &&
                            parameters[i].Initializer != null)
                        {
                            continue;
                        }
                        return false;
                    }
                }
                return true;
            }

            private static bool RequiresNativeProof(
                FlowValueType type)
            {
                return FlowValueTypeFacts.IsNumeric(type) ||
                    type == FlowValueType.Boolean ||
                    type == FlowValueType.String ||
                    type == FlowValueType.Array ||
                    FlowValueTypeFacts.IsPackedArray(type);
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
                if (binding.Upvalue.IsValid)
                {
                    return GetUpvalueType(binding.Upvalue);
                }
                return FlowValueType.Dynamic;
            }

            private FlowValueType GetUpvalueType(UpvalueSlotId slot)
            {
                if (_upvalueTypes == null ||
                    !slot.IsValid ||
                    (uint)slot.Value >= (uint)_upvalueTypes.Length)
                {
                    return FlowValueType.Dynamic;
                }
                var type = _upvalueTypes[slot.Value];
                return type == FlowValueType.None ? FlowValueType.Dynamic : type;
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

            private bool TryGetHostExport(
                FunctionCallExpression call,
                out HostExportDescriptor descriptor)
            {
                descriptor = null;
                if (call?.Target is not GetPropertyExpression property ||
                    !TryGetStaticPropertyName(property.Property, out var memberName) ||
                    property.Object is not NameExpression receiver ||
                    !_names.TryGetValue(receiver, out var binding) ||
                    !binding.IsUnshadowedGlobal)
                {
                    return false;
                }

                return _hostExports.TryGetGlobal(
                    binding.Name,
                    memberName,
                    out descriptor);
            }

            private bool TryGetHostExportConstant(GetPropertyExpression property)
            {
                return property.Object is NameExpression receiver &&
                    TryGetStaticPropertyName(property.Property, out var memberName) &&
                    _names.TryGetValue(receiver, out var binding) &&
                    binding.IsUnshadowedGlobal &&
                    _hostExports.TryGetConstant(binding.Name, memberName, out _);
            }

            private static bool IsStaticProperty(Expression property, string expected)
            {
                return property is NameExpression name &&
                    StringComparer.Ordinal.Equals(name.Identifier?.Value, expected);
            }

            private bool IsNativeStringCharCodeAtCall(FunctionCallExpression call)
            {
                return call.Target is GetPropertyExpression property &&
                    IsStaticProperty(property.Property, "charCodeAt") &&
                    _expressionTypes.TryGetValue(
                        property.Object,
                        out var receiverType) &&
                    receiverType == FlowValueType.String &&
                    call.Arguments.Count == 1 &&
                    _expressionTypes.TryGetValue(
                        call.Arguments[0],
                        out var indexType) &&
                    indexType == FlowValueType.Int32;
            }

            private bool IsProvenStringCharCodeAtCall(FunctionCallExpression call)
            {
                if (call.Target is not GetPropertyExpression property ||
                    property.Object is not NameExpression receiver ||
                    call.Arguments.Count != 1 ||
                    !_names.TryGetValue(receiver, out var receiverBinding) ||
                    !receiverBinding.IsLocal)
                {
                    return false;
                }

                if (TryGetProvenStringIndexBase(
                        call.Arguments[0],
                        receiverBinding.Local,
                        out var offset) &&
                    offset == 0)
                {
                    return true;
                }

                return offset > 0 &&
                    IsGuardedByStringLength(
                        call,
                        call.Arguments[0],
                        receiverBinding.Local);
            }

            private bool TryGetProvenStringIndexBase(
                Expression expression,
                LocalSlotId receiver,
                out int offset)
            {
                offset = 0;
                if (expression is NameExpression index &&
                    _names.TryGetValue(index, out var indexBinding) &&
                    indexBinding.IsLocal)
                {
                    return _provenStringIndices.TryGetValue(
                            indexBinding.Local.Value,
                            out var stringSlot) &&
                        stringSlot == receiver.Value;
                }

                if (expression is BinaryExpression binary &&
                    binary.Operator == Operator.Add &&
                    binary.Left is NameExpression &&
                    TryEvaluateInt32Constant(binary.Right, out offset) &&
                    offset > 0 && offset <= 32)
                {
                    return TryGetProvenStringIndexBase(
                        binary.Left,
                        receiver,
                        out _);
                }

                return false;
            }

            private bool IsGuardedByStringLength(
                AstNode node,
                Expression index,
                LocalSlotId receiver)
            {
                var current = node;
                while (current?.Parent != null &&
                    current.Parent is not Statement)
                {
                    if (current.Parent is BinaryExpression logical &&
                        logical.Operator == Operator.LogicalAnd &&
                        ReferenceEquals(logical.Right, current) &&
                        ContainsStringIndexUpperBound(
                            logical.Left,
                            index,
                            receiver))
                    {
                        return true;
                    }
                    current = current.Parent;
                }
                return false;
            }

            private bool ContainsStringIndexUpperBound(
                Expression expression,
                Expression index,
                LocalSlotId receiver)
            {
                if (expression is BinaryExpression binary)
                {
                    if (binary.Operator == Operator.LessThan &&
                        IsSameStringIndex(binary.Left, index) &&
                        IsStringLengthValue(binary.Right, receiver))
                    {
                        return true;
                    }
                    if (binary.Operator == Operator.LogicalAnd)
                    {
                        return ContainsStringIndexUpperBound(
                                binary.Left,
                                index,
                                receiver) ||
                            ContainsStringIndexUpperBound(
                                binary.Right,
                                index,
                                receiver);
                    }
                }
                return false;
            }

            private bool IsSameStringIndex(Expression left, Expression right)
            {
                if (left is NameExpression leftName &&
                    right is NameExpression rightName &&
                    _names.TryGetValue(leftName, out var leftBinding) &&
                    _names.TryGetValue(rightName, out var rightBinding))
                {
                    return leftBinding.IsLocal && rightBinding.IsLocal &&
                        leftBinding.Local.Equals(rightBinding.Local);
                }
                if (left is BinaryExpression leftBinary &&
                    right is BinaryExpression rightBinary &&
                    leftBinary.Operator == Operator.Add &&
                    rightBinary.Operator == Operator.Add &&
                    TryEvaluateInt32Constant(leftBinary.Right, out var leftOffset) &&
                    TryEvaluateInt32Constant(rightBinary.Right, out var rightOffset) &&
                    leftOffset == rightOffset)
                {
                    return IsSameStringIndex(
                        leftBinary.Left,
                        rightBinary.Left);
                }
                return false;
            }

            private bool IsStringLengthValue(
                Expression expression,
                LocalSlotId receiver)
            {
                if (expression is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal &&
                    !WritesLocal(_function.Declaration?.Body, binding.Local))
                {
                    expression = (_function.LocalSlots[binding.Local.Value]
                        .Declaration as VariableDeclaration)?.Initializer;
                }
                return expression is GetPropertyExpression length &&
                    IsStaticProperty(length.Property, "length") &&
                    length.Object is NameExpression owner &&
                    _names.TryGetValue(owner, out var ownerBinding) &&
                    ownerBinding.IsLocal &&
                    ownerBinding.Local.Equals(receiver);
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
                    case CheckExpression check:
                        InvalidateLocalArrayElementsUsedAsValue(check.Value);
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

            private void WriteTarget(
                Expression target,
                FlowValueType type,
                TypeDeclaration structuralType,
                HostNativeObjectDescriptor nativeObjectType = null)
            {
                if (target is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal)
                {
                    if (IsCaptured(binding.Local))
                    {
                        nativeObjectType = null;
                    }
                    InvalidateLocalFields(binding.Local);
                    InvalidateLocalArrayElements(binding.Local);
                    _writtenLocals[binding.Local.Value] = true;
                    if (!ReferenceEquals(
                        _localStructuralTypes[binding.Local.Value],
                        structuralType))
                    {
                        _localStructuralTypes[binding.Local.Value] = structuralType;
                        _changed = true;
                    }
                    if (!ReferenceEquals(
                        _localNativeObjectTypes[binding.Local.Value],
                        nativeObjectType))
                    {
                        _localNativeObjectTypes[binding.Local.Value] = nativeObjectType;
                        _changed = true;
                    }
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
                        NativeCoercionKind.Int32Bitwise or NativeCoercionKind.Int32Shift =>
                            FlowValueType.Int32,
                        _ => FlowValueType.None
                    };
                    if (type == FlowValueType.None ||
                        _forcedLocalTypes[i] == type ||
                        _locals[i] == type)
                    {
                        continue;
                    }
                    // The demand only states which native representation every
                    // use needs. Flow analysis may already have proven a
                    // narrower one, and promoting it back would reintroduce the
                    // double round trip this pass exists to remove.
                    if (type == FlowValueType.Number &&
                        _locals[i] is FlowValueType.Int32 or FlowValueType.Int64)
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
                    condition.Left is not NameExpression conditionName)
                {
                    return false;
                }
                if (!_names.TryGetValue(conditionName, out var conditionBinding) ||
                    !conditionBinding.IsLocal ||
                    _locals[conditionBinding.Local.Value] != FlowValueType.Int32 ||
                    !_expressionTypes.TryGetValue(condition.Right, out var boundType) ||
                    boundType != FlowValueType.Int32)
                {
                    return false;
                }

                var writes = new Int32InductionWriteAnalyzer(
                    this,
                    conditionBinding.Local);
                writes.Analyze(statement.Body, rejectNestedLoops: true);
                writes.Analyze(statement.Incrementor, rejectNestedLoops: false);
                if (!writes.IsValid || writes.MaximumDelta <= 0)
                {
                    return false;
                }

                // A single increment cannot overflow before an Int32 upper
                // bound rejects the next iteration. Larger steps can overshoot
                // the bound, so only use Int32 for native lengths. CLR-backed
                // strings and arrays are capped below Int32.MaxValue by more
                // than the small induction steps accepted here.
                if (writes.MaximumDelta > 1 &&
                    (writes.MaximumDelta > 32 ||
                        !IsNativeLengthBound(condition.Right)))
                {
                    return false;
                }

                slot = conditionBinding.Local;
                return true;
            }

            private bool TryGetProvenStringIndex(
                ForStatement statement,
                LocalSlotId inductionSlot,
                out LocalSlotId stringSlot)
            {
                stringSlot = LocalSlotId.Invalid;
                if (statement?.Condition is not BinaryExpression condition ||
                    condition.Operator != Operator.LessThan ||
                    condition.Left is not NameExpression conditionName ||
                    !_names.TryGetValue(conditionName, out var conditionBinding) ||
                    !conditionBinding.IsLocal ||
                    !conditionBinding.Local.Equals(inductionSlot))
                {
                    return false;
                }

                var inductionDeclaration = _function.LocalSlots[inductionSlot.Value]
                    .Declaration as VariableDeclaration;
                if (inductionDeclaration?.Initializer == null ||
                    !TryEvaluateInt32Constant(
                        inductionDeclaration.Initializer,
                        out var initialIndex) ||
                    initialIndex < 0)
                {
                    return false;
                }

                Expression bound = condition.Right;
                if (bound is NameExpression boundName &&
                    _names.TryGetValue(boundName, out var boundBinding) &&
                    boundBinding.IsLocal &&
                    !WritesLocal(_function.Declaration?.Body, boundBinding.Local))
                {
                    bound = (_function.LocalSlots[boundBinding.Local.Value]
                        .Declaration as VariableDeclaration)?.Initializer;
                }

                if (bound is not GetPropertyExpression length ||
                    !IsStaticProperty(length.Property, "length") ||
                    length.Object is not NameExpression stringName ||
                    !_names.TryGetValue(stringName, out var stringBinding) ||
                    !stringBinding.IsLocal ||
                    _locals[stringBinding.Local.Value] != FlowValueType.String)
                {
                    return false;
                }

                stringSlot = stringBinding.Local;
                return true;
            }

            /// <summary>
            /// Recognises <c>for (i = &lt;int&gt;; i &lt; bound; i += &lt;positive&gt;)</c>
            /// where the bound is a loop-invariant number. The counter only ever
            /// holds exact integers, so an <c>Int64</c> counter observes the same
            /// values as the double it would otherwise widen to while letting the
            /// loop compare and index natively.
            /// </summary>
            private bool TryGetCountedInt64Loop(
                ForStatement statement,
                out LocalSlotId slot,
                out Expression bound)
            {
                slot = LocalSlotId.Invalid;
                bound = null;
                if (statement?.Condition is not BinaryExpression condition ||
                    condition.Operator != Operator.LessThan ||
                    condition.Left is not NameExpression conditionName ||
                    !_names.TryGetValue(conditionName, out var conditionBinding) ||
                    !conditionBinding.IsLocal ||
                    IsCaptured(conditionBinding.Local))
                {
                    return false;
                }
                var counterType = _locals[conditionBinding.Local.Value];
                if (counterType != FlowValueType.Int32 &&
                    counterType != FlowValueType.Int64)
                {
                    return false;
                }
                if (!_expressionTypes.TryGetValue(condition.Right, out var boundType) ||
                    !FlowValueTypeFacts.IsNumeric(boundType) ||
                    !IsLoopInvariantBound(condition.Right))
                {
                    return false;
                }

                var writes = new Int32InductionWriteAnalyzer(this, conditionBinding.Local);
                writes.Analyze(statement.Body, rejectNestedLoops: true);
                writes.Analyze(statement.Incrementor, rejectNestedLoops: false);
                if (!writes.IsValid || writes.MaximumDelta <= 0)
                {
                    return false;
                }

                slot = conditionBinding.Local;
                bound = condition.Right;
                return true;
            }

            /// <summary>
            /// Accepts only bounds that can be hoisted in front of the loop:
            /// a constant, or a local that nothing in the function reassigns.
            /// </summary>
            private bool IsLoopInvariantBound(Expression expression)
            {
                while (expression is GroupExpression group &&
                    group.Expressions.Count == 1)
                {
                    expression = group.Expression;
                }
                if (expression is LiteralExpression literal)
                {
                    return literal.Token is NumberToken;
                }
                if (expression is not NameExpression name ||
                    !_names.TryGetValue(name, out var binding))
                {
                    return false;
                }
                if (binding.HasConstant) return true;
                return binding.IsLocal &&
                    !IsCaptured(binding.Local) &&
                    !WritesLocal(_function.Declaration?.Body, binding.Local);
            }

            private bool IsNativeLengthBound(Expression expression)
            {
                while (expression is GroupExpression group &&
                    group.Expressions.Count == 1)
                {
                    expression = group.Expression;
                }

                if (expression is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal &&
                    !WritesLocal(_function.Declaration?.Body, binding.Local))
                {
                    var declaration = _function.LocalSlots[binding.Local.Value]
                        .Declaration as VariableDeclaration;
                    expression = declaration?.Initializer;
                }

                if (expression is not GetPropertyExpression property ||
                    !IsStaticProperty(property.Property, "length") ||
                    !_expressionTypes.TryGetValue(property.Object, out var ownerType))
                {
                    return false;
                }
                return ownerType == FlowValueType.String ||
                    ownerType == FlowValueType.Array ||
                    FlowValueTypeFacts.IsPackedArray(ownerType);
            }

            private List<int> GetSafeInt32WhileMutations(WhileStatement statement)
            {
                var result = new List<int>(2);
                if (statement?.Condition is not BinaryExpression condition)
                {
                    return result;
                }

                TryAdd(condition.Left);
                TryAdd(condition.Right);
                return result;

                void TryAdd(Expression expression)
                {
                    if (expression is not NameExpression name ||
                        !_names.TryGetValue(name, out var binding) ||
                        !binding.IsLocal ||
                        _locals[binding.Local.Value] != FlowValueType.Int32 ||
                        IsCaptured(binding.Local))
                    {
                        return;
                    }

                    var direction = GetGuardedWhileDirection(
                        condition,
                        binding.Local);
                    if (direction == 0)
                    {
                        return;
                    }

                    var writes = new Int32WhileMutationAnalyzer(
                        this,
                        binding.Local,
                        direction);
                    writes.Analyze(statement.Body);
                    if (writes.IsValid && writes.FoundMutation)
                    {
                        result.Add(binding.Local.Value);
                    }
                }
            }

            private int GetGuardedWhileDirection(
                BinaryExpression condition,
                LocalSlotId slot)
            {
                var onLeft = IsLocalName(condition.Left, slot);
                var onRight = IsLocalName(condition.Right, slot);
                if (onLeft == onRight)
                {
                    return 0;
                }

                if (condition.Operator == Operator.LessThan ||
                    condition.Operator == Operator.LessThanOrEqual)
                {
                    return onLeft ? 1 : -1;
                }
                if (condition.Operator == Operator.GreaterThan ||
                    condition.Operator == Operator.GreaterThanOrEqual)
                {
                    return onLeft ? -1 : 1;
                }
                return 0;
            }

            private sealed class Int32WhileMutationAnalyzer
            {
                private readonly TypeAnalyzer _owner;
                private readonly LocalSlotId _slot;
                private readonly int _direction;
                private AstNode _root;

                public Int32WhileMutationAnalyzer(
                    TypeAnalyzer owner,
                    LocalSlotId slot,
                    int direction)
                {
                    _owner = owner;
                    _slot = slot;
                    _direction = direction;
                    IsValid = true;
                }

                public bool IsValid { get; private set; }
                public bool FoundMutation { get; private set; }

                public void Analyze(AstNode node)
                {
                    _root = node;
                    Visit(node);
                }

                private void Visit(AstNode node)
                {
                    if (!IsValid || node == null ||
                        node is FunctionDeclaration or LambdaExpression)
                    {
                        return;
                    }
                    if (!ReferenceEquals(node, _root) &&
                        node is ForStatement or ForInStatement or WhileStatement)
                    {
                        if (_owner.WritesLocal(node, _slot))
                        {
                            IsValid = false;
                        }
                        return;
                    }

                    if (node is UnaryExpression unary &&
                        IsMutation(unary.Operator) &&
                        _owner.IsLocalName(unary.Expression, _slot))
                    {
                        var direction =
                            unary.Operator == Operator.PreIncrement ||
                            unary.Operator == Operator.PostIncrement
                                ? 1
                                : -1;
                        FoundMutation = true;
                        IsValid = direction == _direction;
                        return;
                    }

                    if (node is CompoundExpression compound &&
                        _owner.IsLocalName(compound.Left, _slot))
                    {
                        var op = compound.Operator.SimplerOperator;
                        var direction = op == Operator.Add
                            ? 1
                            : op == Operator.Subtract ? -1 : 0;
                        FoundMutation = true;
                        IsValid = direction == _direction &&
                            TryEvaluateInt32Constant(compound.Right, out var delta) &&
                            delta > 0 && delta <= 32;
                        return;
                    }

                    if (node is AssignmentExpression assignment &&
                        _owner.IsLocalName(assignment.Left, _slot) ||
                        node is ForInStatement forIn &&
                            _owner.IsLocalName(forIn.Iterator?.Left, _slot))
                    {
                        IsValid = false;
                        return;
                    }

                    var visitor = new ChildVisitor(this);
                    AstTraversal.VisitChildren(node, ref visitor);
                }

                private readonly struct ChildVisitor : IAstChildVisitor
                {
                    private readonly Int32WhileMutationAnalyzer _owner;

                    public ChildVisitor(Int32WhileMutationAnalyzer owner)
                    {
                        _owner = owner;
                    }

                    public void Visit(AstNode node)
                    {
                        _owner.Visit(node);
                    }
                }
            }

            private sealed class Int32InductionWriteAnalyzer
            {
                private readonly TypeAnalyzer _owner;
                private readonly LocalSlotId _slot;
                private AstNode _root;
                private bool _rejectNestedLoops;

                public Int32InductionWriteAnalyzer(
                    TypeAnalyzer owner,
                    LocalSlotId slot)
                {
                    _owner = owner;
                    _slot = slot;
                    IsValid = true;
                }

                public bool IsValid { get; private set; }
                public int MaximumDelta { get; private set; }

                public void Analyze(AstNode node, bool rejectNestedLoops)
                {
                    if (!IsValid || node == null) return;
                    _root = node;
                    _rejectNestedLoops = rejectNestedLoops;
                    Visit(node);
                }

                private void Visit(AstNode node)
                {
                    if (!IsValid || node == null ||
                        node is FunctionDeclaration or LambdaExpression)
                    {
                        return;
                    }

                    if (_rejectNestedLoops &&
                        !ReferenceEquals(node, _root) &&
                        node is ForStatement or ForInStatement or WhileStatement)
                    {
                        if (_owner.WritesLocal(node, _slot)) IsValid = false;
                        return;
                    }

                    if (node is AssignmentExpression assignment &&
                        _owner.IsLocalName(assignment.Left, _slot) ||
                        node is ForInStatement forIn &&
                            _owner.IsLocalName(forIn.Iterator?.Left, _slot))
                    {
                        IsValid = false;
                        return;
                    }

                    if (node is UnaryExpression unary &&
                        IsMutation(unary.Operator) &&
                        _owner.IsLocalName(unary.Expression, _slot))
                    {
                        if (unary.Operator == Operator.PreIncrement ||
                            unary.Operator == Operator.PostIncrement)
                        {
                            AddDelta(1);
                            return;
                        }
                        IsValid = false;
                        return;
                    }

                    if (node is CompoundExpression compound &&
                        _owner.IsLocalName(compound.Left, _slot))
                    {
                        if (compound.Operator.SimplerOperator == Operator.Add &&
                            TryEvaluateInt32Constant(compound.Right, out var delta) &&
                            delta > 0)
                        {
                            AddDelta(delta);
                            return;
                        }
                        IsValid = false;
                        return;
                    }

                    var visitor = new ChildVisitor(this);
                    AstTraversal.VisitChildren(node, ref visitor);
                }

                private void AddDelta(int delta)
                {
                    try
                    {
                        MaximumDelta = checked(MaximumDelta + delta);
                    }
                    catch (OverflowException)
                    {
                        IsValid = false;
                    }
                }

                private readonly struct ChildVisitor : IAstChildVisitor
                {
                    private readonly Int32InductionWriteAnalyzer _owner;

                    public ChildVisitor(Int32InductionWriteAnalyzer owner)
                    {
                        _owner = owner;
                    }

                    public void Visit(AstNode node)
                    {
                        _owner.Visit(node);
                    }
                }
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
                        : CanKeepInt64Arithmetic(op, leftExpression, rightExpression, left, right)
                            ? FlowValueType.Int64
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
                        : CanKeepInt64Arithmetic(op, leftExpression, rightExpression, left, right)
                            ? FlowValueType.Int64
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

            private static bool CanKeepInt64Arithmetic(
                Operator op,
                Expression leftExpression,
                Expression rightExpression,
                FlowValueType left,
                FlowValueType right)
            {
                if (!FlowValueTypeFacts.IsNumeric(left) ||
                    !FlowValueTypeFacts.IsNumeric(right) ||
                    left == FlowValueType.Number ||
                    right == FlowValueType.Number)
                {
                    return false;
                }
                if (TryEvaluateInt64Arithmetic(op, leftExpression, rightExpression, out _))
                {
                    return true;
                }
                if (op == Operator.Add)
                {
                    return IsInt64Constant(leftExpression, 0) ||
                        IsInt64Constant(rightExpression, 0);
                }
                if (op == Operator.Subtract)
                {
                    return IsInt64Constant(rightExpression, 0);
                }
                if (op == Operator.Multiply)
                {
                    return IsInt64Constant(leftExpression, 1) ||
                        IsInt64Constant(rightExpression, 1);
                }
                return false;
            }

            private static bool TryEvaluateInt64Arithmetic(
                Operator op,
                Expression leftExpression,
                Expression rightExpression,
                out long value)
            {
                if (!TryEvaluateInt64Constant(leftExpression, out var left) ||
                    !TryEvaluateInt64Constant(rightExpression, out var right))
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
                    return IsExactScriptInteger(value);
                }
                catch (OverflowException)
                {
                    value = 0;
                    return false;
                }
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

            private static bool IsInt64Constant(Expression expression, long expected)
            {
                return TryEvaluateInt64Constant(expression, out var value) && value == expected;
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

            private static bool TryEvaluateInt64Constant(Expression expression, out long value)
            {
                switch (expression)
                {
                    case LiteralExpression { Token: NumberToken number }
                        when IsExactInt64(number.NumberValue):
                        value = (long)number.NumberValue;
                        return true;
                    case UnaryExpression unary:
                        if (!TryEvaluateInt64Constant(unary.Expression, out var operand)) break;
                        if (unary.Operator == Operator.Negate &&
                            operand != 0 && operand != long.MinValue)
                        {
                            value = -operand;
                            return IsExactScriptInteger(value);
                        }
                        if (unary.Operator == Operator.BitwiseNot)
                        {
                            value = ~unchecked((int)operand);
                            return true;
                        }
                        break;
                    case BinaryExpression binary
                        when TryEvaluateInt64Constant(binary.Left, out var left) &&
                            TryEvaluateInt64Constant(binary.Right, out var right):
                        try
                        {
                            if (binary.Operator == Operator.Add) value = checked(left + right);
                            else if (binary.Operator == Operator.Subtract) value = checked(left - right);
                            else if (binary.Operator == Operator.Multiply) value = checked(left * right);
                            else if (binary.Operator == Operator.BitwiseAnd) value = (int)left & (int)right;
                            else if (binary.Operator == Operator.BitwiseOr) value = (int)left | (int)right;
                            else if (binary.Operator == Operator.BitwiseXor) value = (int)left ^ (int)right;
                            else if (binary.Operator == Operator.LeftShift) value = (int)left << ((int)right & 31);
                            else if (binary.Operator == Operator.SignedRightShift) value = (int)left >> ((int)right & 31);
                            else
                            {
                                value = 0;
                                return false;
                            }
                            return IsExactScriptInteger(value);
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

            private static bool IsExactInt64(double value)
            {
                return value >= -9007199254740991d &&
                    value <= 9007199254740991d &&
                    value == Math.Truncate(value) &&
                    (value != 0d || BitConverter.DoubleToInt64Bits(value) >= 0);
            }

            private static bool IsExactScriptInteger(long value)
            {
                return value >= -9007199254740991L && value <= 9007199254740991L;
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
                    if (TryEvaluateInt32Constant(expression, out _)) return FlowValueType.Int32;
                    return TryEvaluateInt64Constant(expression, out _)
                        ? FlowValueType.Int64
                        : FlowValueType.Number;
                }
                return operand;
            }

            private FlowValueType GetMutationWriteType(UnaryExpression expression)
            {
                return GetInductionType(expression.Expression) is var induction &&
                    induction != FlowValueType.None
                        ? induction
                        : FlowValueType.Number;
            }

            private FlowValueType GetInductionCompoundType(CompoundExpression expression)
            {
                if (expression.Operator.SimplerOperator != Operator.Add ||
                    !TryEvaluateInt32Constant(expression.Right, out var delta) ||
                    delta <= 0)
                {
                    return FlowValueType.None;
                }
                return GetInductionType(expression.Left);
            }

            private FlowValueType GetInductionArithmeticType(BinaryExpression expression)
            {
                if (expression.Operator != Operator.Add)
                {
                    return FlowValueType.None;
                }
                if (TryEvaluateInt32Constant(expression.Right, out var right) &&
                    right >= 0 && right <= 32)
                {
                    return GetInductionType(expression.Left);
                }
                if (TryEvaluateInt32Constant(expression.Left, out var left) &&
                    left >= 0 && left <= 32)
                {
                    return GetInductionType(expression.Right);
                }
                return FlowValueType.None;
            }

            /// <summary>
            /// Returns the native storage a proven induction variable keeps for
            /// a small positive step, or <see cref="FlowValueType.None"/> when
            /// the expression is not such a counter.
            /// </summary>
            private FlowValueType GetInductionType(Expression expression)
            {
                if (expression is not NameExpression name ||
                    !_names.TryGetValue(name, out var binding) ||
                    !binding.IsLocal)
                {
                    return FlowValueType.None;
                }
                if (_safeInt32Mutations.Contains(binding.Local.Value))
                {
                    return FlowValueType.Int32;
                }
                return _safeInt64Mutations.Contains(binding.Local.Value)
                    ? FlowValueType.Int64
                    : FlowValueType.None;
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
                        : IsExactInt64(number.NumberValue)
                            ? FlowValueType.Int64
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
                private readonly bool[] _boundaryUses;
                private readonly Dictionary<AstNode, int> _declaredSlots;
                private readonly List<(int Source, int Target)> _copies;

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
                    _boundaryUses = new bool[function.LocalSlots.Length];
                    _declaredSlots = new Dictionary<AstNode, int>();
                    _copies = new List<(int Source, int Target)>();
                    for (var i = 0; i < function.LocalSlots.Length; i++)
                    {
                        var slot = function.LocalSlots[i];
                        _invalid[i] = slot.IsParameter ||
                            isCaptured(slot.Id) ||
                            slot.Declaration is VariableDeclaration { Pattern: not null };
                        if (slot.Declaration != null) _declaredSlots[slot.Declaration] = i;
                    }
                }

                public NativeCoercionKind[] Analyze(AstNode body)
                {
                    Visit(body);
                    for (var i = 0; i < _demands.Length; i++)
                    {
                        // A boundary use keeps the stored value intact, so an
                        // integer demand may not truncate the storage even
                        // though every other use would.
                        if (_boundaryUses[i] && IsInt32Demand(_demands[i]))
                        {
                            _invalid[i] = true;
                        }
                    }
                    PropagateCopies();
                    for (var i = 0; i < _demands.Length; i++)
                    {
                        if (_invalid[i]) _demands[i] = NativeCoercionKind.None;
                    }
                    return _demands;
                }

                // A plain copy into another local forwards that local's demand
                // instead of pinning the source. Only the integer demands are
                // forwarded: they truncate identically no matter how many times
                // they are applied, so narrowing the source stays observable
                // through the copy alone.
                private void PropagateCopies()
                {
                    for (var round = 0; round <= _copies.Count; round++)
                    {
                        var changed = false;
                        foreach (var (source, target) in _copies)
                        {
                            if (_invalid[source]) continue;
                            if (_invalid[target] || !IsInt32Demand(_demands[target]))
                            {
                                _invalid[source] = true;
                                changed = true;
                                continue;
                            }
                            if (_demands[source] == NativeCoercionKind.None)
                            {
                                _demands[source] = NativeCoercionKind.Int32Bitwise;
                                changed = true;
                            }
                            else if (!IsInt32Demand(_demands[source]))
                            {
                                _invalid[source] = true;
                                changed = true;
                            }
                        }
                        if (!changed) break;
                    }
                }

                private static bool IsInt32Demand(NativeCoercionKind demand)
                {
                    return demand is NativeCoercionKind.Int32Bitwise or
                        NativeCoercionKind.Int32Shift;
                }

                private bool TryRecordCopy(int source, AstNode current)
                {
                    var target = -1;
                    if (current.Parent is VariableDeclaration declaration &&
                        ReferenceEquals(declaration.Initializer, current) &&
                        declaration.Pattern == null &&
                        _declaredSlots.TryGetValue(declaration, out var declaredSlot))
                    {
                        target = declaredSlot;
                    }
                    else if (current.Parent is AssignmentExpression assignment &&
                        ReferenceEquals(assignment.Right, current) &&
                        assignment.Parent is ExpressionStatement &&
                        assignment.Left is NameExpression assigned &&
                        _names.TryGetValue(assigned, out var assignedBinding) &&
                        assignedBinding.IsLocal &&
                        (uint)assignedBinding.Local.Value < (uint)_demands.Length)
                    {
                        target = assignedBinding.Local.Value;
                    }
                    if (target < 0 || target == source) return false;
                    _copies.Add((source, target));
                    return true;
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
                        _boundaryUses[binding.Local.Value] = true;
                        return;
                    }

                    var demand = GetUseDemand(current);
                    if (demand == NativeCoercionKind.None)
                    {
                        if (!TryRecordCopy(binding.Local.Value, current))
                        {
                            _invalid[binding.Local.Value] = true;
                        }
                        return;
                    }
                    var existing = _demands[binding.Local.Value];
                    if (existing == NativeCoercionKind.None ||
                        (IsInt32Demand(existing) && IsInt32Demand(demand)))
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
                        var demand = GetBinaryOperandDemand(binary.Operator, current);
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
                            current);
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
                                // The callee truncates the argument itself, so
                                // the caller can hold the integer directly. Both
                                // integer coercions narrow the same way and must
                                // agree, otherwise mixed uses cancel out.
                                if (parameter.IsInt32Coercion)
                                {
                                    return NativeCoercionKind.Int32Bitwise;
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

                private bool IsBoundaryValueUse(AstNode current)
                {
                    // A packed array coerces whatever it stores to its native
                    // element type, so feeding it a promoted local is not
                    // observable. Returns, property stores, and object arrays
                    // keep the original value and must pin the ScriptDatum.
                    return current.Parent is SetElementExpression setElement &&
                        ReferenceEquals(setElement.Value, current) &&
                        FlowValueTypeFacts.IsPackedArray(GetExpressionType(setElement.Object));
                }

                private NativeCoercionKind GetBinaryOperandDemand(Operator op, AstNode operand)
                {
                    if (op == Operator.Subtract ||
                        op == Operator.Multiply ||
                        op == Operator.Divide ||
                        op == Operator.Modulo)
                    {
                        return NativeCoercionKind.ArithmeticNumber;
                    }

                    // Native storage keeps the arithmetic coercion, which turns
                    // null into zero and parses strings. '+' concatenates when
                    // either side is a string, and comparisons leave null
                    // unordered, so both disagree with it. Only an operand that
                    // is already numeric can be demanded here; a dynamic one
                    // falls back to the ScriptDatum plus numeric shadow, which
                    // reproduces those semantics exactly.
                    if (op == Operator.Add ||
                        op == Operator.Equal ||
                        op == Operator.NotEqual ||
                        op == Operator.LessThan ||
                        op == Operator.LessThanOrEqual ||
                        op == Operator.GreaterThan ||
                        op == Operator.GreaterThanOrEqual)
                    {
                        var operandType = GetExpressionType(operand as Expression);
                        if (FlowValueTypeFacts.IsNumeric(operandType) ||
                            operandType == FlowValueType.Boolean)
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
