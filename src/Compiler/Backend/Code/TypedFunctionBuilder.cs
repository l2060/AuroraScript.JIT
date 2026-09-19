using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Analysis;
using AuroraScript.Compiler.Backend.Traversal;
using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AuroraScript.Compiler.Backend.Code
{
    internal static partial class TypedFunctionBuilder
    {
        // (n - n % d) / d is exact truncating integer division for an exact integer n
        // and a positive integer d. Restrict n to a name: repeated reads must
        // not invoke getters or evaluate other side effects.
        internal static bool TryGetExactIntegerQuotient(Expression expression,
            out NameExpression value, out int divisor)
        {
            value = null;
            divisor = 0;
            static Expression Unwrap(Expression node)
            {
                while (node is GroupExpression group && group.Expressions.Count == 1) node = group.Expression;
                return node;
            }
            if (Unwrap(expression) is not BinaryExpression divide || divide.Operator != Operator.Divide ||
                Unwrap(divide.Right) is not LiteralExpression { Token: NumberToken denominator } ||
                !TypeCheckOps.IsInt32(denominator.NumberValue) || denominator.NumberValue <= 0 ||
                Unwrap(divide.Left) is not BinaryExpression subtract || subtract.Operator != Operator.Subtract ||
                Unwrap(subtract.Left) is not NameExpression name ||
                Unwrap(subtract.Right) is not BinaryExpression remainder || remainder.Operator != Operator.Modulo ||
                Unwrap(remainder.Left) is not NameExpression repeated || name.Identifier.Value != repeated.Identifier.Value ||
                Unwrap(remainder.Right) is not LiteralExpression { Token: NumberToken modulo } ||
                modulo.NumberValue != denominator.NumberValue ||
                denominator.Suffix is NumericLiteralSuffix.Int64 or NumericLiteralSuffix.UInt64 ||
                modulo.Suffix is NumericLiteralSuffix.Int64 or NumericLiteralSuffix.UInt64)
                return false;
            value = name;
            divisor = (int)denominator.NumberValue;
            return true;
        }

        internal static FlowValueType GetGuardedBinaryType(BinaryExpression expression,
            Func<Expression, FlowValueType> getType)
        {
            return TypeAnalyzer.AnalyzeBinary(null, expression.Operator,
                expression.Left, expression.Right, getType(expression.Left), getType(expression.Right));
        }

        internal static bool CanCompleteNormally(Statement statement)
        {
            // Conservatively account for an implicit return. Nested functions
            // do not return from their enclosing function; loops may complete.
            switch (statement)
            {
                case ReturnStatement or ThrowStatement:
                    return false;
                case BlockStatement block:
                    foreach (var child in block.Statements)
                        if (!CanCompleteNormally(child)) return false;
                    return true;
                case IfStatement conditional:
                    return conditional.Else == null || CanCompleteNormally(conditional.Body) ||
                        CanCompleteNormally(conditional.Else);
                case TryStatement guarded:
                    return (guarded.FinallyBody == null || CanCompleteNormally(guarded.FinallyBody)) &&
                        (CanCompleteNormally(guarded.Body) ||
                            guarded.CatchBody != null && CanCompleteNormally(guarded.CatchBody));
                default:
                    return true;
            }
        }

        internal sealed class FunctionBinding
        {
            public FunctionBinding(
                ModulePlan module,
                FunctionPlan function,
                Dictionary<NameExpression, BoundName> names,
                Dictionary<VariableDeclaration, LocalSlotId> declarations,
                IReadOnlyList<ReturnStatement> returns,
                IReadOnlyList<FunctionCallExpression> calls,
                int bodyCallStart)
            {
                Module = module;
                Function = function;
                Names = names;
                Declarations = declarations;
                Returns = returns;
                Calls = calls;
                BodyCallStart = bodyCallStart;
                foreach (var binding in names.Values)
                {
                    HasDirectFunctionReference |= binding.DirectFunction.IsValid;
                    HasUpvalueReference |= binding.Upvalue.IsValid;
                }
            }

            public ModulePlan Module { get; }
            public FunctionPlan Function { get; }
            public Dictionary<NameExpression, BoundName> Names { get; }
            public Dictionary<VariableDeclaration, LocalSlotId> Declarations { get; }
            public IReadOnlyList<ReturnStatement> Returns { get; }
            public IReadOnlyList<FunctionCallExpression> Calls { get; }
            public int BodyCallStart { get; }
            public bool HasDirectFunctionReference { get; }
            public bool HasUpvalueReference { get; }
            public bool[] UnobservedInitialNulls { get; set; }
            public sbyte[] LocalWrites { get; set; }
        }

        public static TypedFunctionCode Build(
            ModulePlan module,
            FunctionPlan function)
        {
            return Build(
                module,
                function,
                new HostExportCatalog(Array.Empty<Type>()));
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
            ArgumentNullException.ThrowIfNull(hostExports);

            return Analyze(
                Bind(module, function),
                hostExports,
                parameterTypes,
                directReturnTypes,
                directParameterTypes,
                universalReturnTypes,
                upvalueTypes);
        }

        internal static FunctionBinding Bind(
            ModulePlan module,
            FunctionPlan function)
        {
            ArgumentNullException.ThrowIfNull(module);
            ArgumentNullException.ThrowIfNull(function);

            return Bind(
                module,
                function,
                BuildDirectFunctionMap(module));
        }

        internal static FunctionBinding BindModule(ModulePlan module, FunctionBinding[] bindings)
        {
            ArgumentNullException.ThrowIfNull(module);
            var directFunctions = BuildDirectFunctionMap(module);
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                bindings[function.Id.Value] = Bind(
                    module,
                    function,
                    directFunctions);
            }
            return Bind(module, module.InitializerFunction, directFunctions);
        }

        private static FunctionBinding Bind(
            ModulePlan module,
            FunctionPlan function,
            Dictionary<SymbolId, FunctionId> directFunctions)
        {
            var binder = new NameBinder(module, function, directFunctions);
            binder.Bind();
            return new FunctionBinding(
                module,
                function,
                binder.Names,
                binder.Declarations,
                binder.Returns ?? (IReadOnlyList<ReturnStatement>)Array.Empty<ReturnStatement>(),
                binder.Calls ?? (IReadOnlyList<FunctionCallExpression>)Array.Empty<FunctionCallExpression>(),
                binder.BodyCallStart);
        }

        internal static TypedFunctionCode Analyze(
            FunctionBinding binding,
            HostExportCatalog hostExports,
            DirectParameterType[] parameterTypes = null,
            IReadOnlyDictionary<FunctionId, FlowValueType> directReturnTypes = null,
            DirectParameterType[][] directParameterTypes = null,
            IReadOnlyDictionary<FunctionId, FlowValueType> universalReturnTypes = null,
            IReadOnlyDictionary<FunctionId, FlowValueType[]> upvalueTypes = null,
            Func<Expression, CallableReturnPrediction?> callableReturnPrediction = null)
        {
            ArgumentNullException.ThrowIfNull(binding);
            ArgumentNullException.ThrowIfNull(hostExports);

            var analyzer = new TypeAnalyzer(
                binding,
                hostExports,
                parameterTypes,
                directReturnTypes,
                directParameterTypes,
                universalReturnTypes,
                upvalueTypes != null &&
                    upvalueTypes.TryGetValue(binding.Function.Id, out var functionUpvalues)
                        ? functionUpvalues
                        : null,
                callableReturnPrediction);
            return analyzer.Analyze();
        }

        private static Dictionary<SymbolId, FunctionId> BuildDirectFunctionMap(
            ModulePlan module)
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

        private sealed class NameBinder
        {
            private readonly ModulePlan _module;
            private readonly FunctionPlan _function;
            private readonly Stack<int> _scopes = new();
            private readonly Dictionary<SymbolId, FunctionId> _directFunctions;
            private Statement _loop;
            private int _loopFinallyDepth;
            private int _finallyDepth;
            public List<ReturnStatement> Returns { get; private set; }
            public List<FunctionCallExpression> Calls { get; private set; }
            public int BodyCallStart { get; private set; }

            public NameBinder(
                ModulePlan module,
                FunctionPlan function,
                Dictionary<SymbolId, FunctionId> directFunctions)
            {
                _module = module;
                _function = function;
                Names = new Dictionary<NameExpression, BoundName>(ReferenceEqualityComparer.Instance);
                Declarations = new Dictionary<VariableDeclaration, LocalSlotId>(ReferenceEqualityComparer.Instance);
                _directFunctions = directFunctions;

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
                    BodyCallStart = Calls?.Count ?? 0;
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
                            (Returns ??= new()).Add(@return);
                            _function.HasReturnInFinally |= _finallyDepth != 0;
                            BindExpression(@return.Expression);
                            break;
                        case BreakStatement or ContinueStatement:
                            if (_loop != null && _finallyDepth > _loopFinallyDepth)
                                (_function.LoopsWithFinallyTransfer ??= new()).Add(_loop);
                            break;
                        case IfStatement @if:
                            BindExpression(@if.Condition);
                            BindNode(@if.Body);
                            BindNode(@if.Else);
                            break;
                        case WhileStatement @while:
                            BindExpression(@while.Condition);
                            BindLoop(@while, @while.Body);
                            break;
                        case ForStatement @for:
                            BindNode(@for.Initializer);
                            BindExpression(@for.Condition);
                            BindExpression(@for.Incrementor);
                            BindLoop(@for, @for.Body);
                            break;
                        case ForInStatement forIn:
                            BindNode(forIn.Initializer);
                            BindExpression(forIn.Iterator);
                            BindLoop(forIn, forIn.Body);
                            break;
                        case TryStatement @try:
                            _function.HasProtectedRegion = true;
                            BindNode(@try.Body);
                            BindNode(@try.CatchBody);
                            _finallyDepth++;
                            BindNode(@try.FinallyBody);
                            _finallyDepth--;
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

            private void BindLoop(Statement loop, Statement body)
            {
                var previous = (_loop, _loopFinallyDepth);
                _loop = loop;
                _loopFinallyDepth = _finallyDepth;
                BindNode(body);
                (_loop, _loopFinallyDepth) = previous;
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
                            (Calls ??= new()).Add(call);
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
                // Initializer variables have local storage and an observable module
                // binding. A local slot alone does not prove that a read can use it.
                var moduleSymbol = (local.IsValid && !_function.IsModuleInitializer) || upvalue.IsValid || !_module.TryGetSymbol(name, out var symbol)
                    ? SymbolId.Invalid
                    : symbol;
                var directFunction = moduleSymbol.IsValid && _directFunctions.TryGetValue(moduleSymbol, out var direct)
                    ? direct
                    : FunctionId.Invalid;
                var constant = default(InlineConstant);
                var hasConstant = moduleSymbol.IsValid &&
                    _module.TryGetInlineConstantInfo(moduleSymbol, out constant);
                var isDeclaredOnly = _module.IsDeclaredOnly(name);
                return new BoundName(
                    name,
                    local,
                    upvalue,
                    moduleSymbol,
                    directFunction,
                    constant.Value,
                    constant.NumericHint,
                    hasConstant,
                    isDeclaredOnly,
                    isContext: _function.IsModuleInitializer && _module.Declaration.TryGetContext(name, out _));
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

        }

        private sealed partial class TypeAnalyzer
        {
            private readonly FunctionBinding _binding;
            private readonly ModulePlan _module;
            private readonly FunctionPlan _function;
            private readonly Dictionary<NameExpression, BoundName> _names;
            private readonly Dictionary<VariableDeclaration, LocalSlotId> _declarations;
            private readonly HostExportCatalog _hostExports;
            private readonly ClrTypeCatalog _clrCatalog;
            private readonly Dictionary<Expression, FlowValueType> _expressionTypes;
            private Dictionary<FunctionCallExpression, HostNativeMethodDescriptor> _nativeCalls;
            private Dictionary<FunctionCallExpression, HostExportDescriptor> _hostCalls;
            private Func<Expression, FlowValueType> _hostArgumentType;
            private Func<Expression, Type> _hostArgumentClrType;
            private Func<Expression, FlowValueType> HostArgumentType => _hostArgumentType ??=
                argument => _expressionTypes.TryGetValue(argument, out var type) ? type : FlowValueType.Dynamic;
            private Func<Expression, Type> HostArgumentClrType => _hostArgumentClrType ??=
                argument => _nativeObjectTypes.TryGetValue(argument, out var native) ? native.ClrType : null;
            private readonly Dictionary<Expression, TypeDeclaration> _structuralTypes;
            private readonly Dictionary<Expression, HostNativeObjectDescriptor> _nativeObjectTypes;
            private readonly FlowValueType[] _locals;
            private readonly TypeDeclaration[] _localStructuralTypes;
            private readonly HostNativeObjectDescriptor[] _localNativeObjectTypes;
            private readonly Dictionary<Expression, Type> _clrTypes;
            private readonly Type[] _localClrTypes;
            private Dictionary<FunctionCallExpression, MethodBase> _clrCalls;
            private Dictionary<Expression, MemberInfo> _clrMembers;
            private List<ShapeSnapshot> _shapeSnapshots;
            private int _shapeSnapshotCount;
            private readonly FlowValueType[] _forcedLocalTypes;
            private readonly bool[] _writtenLocals;
            private bool[] _unobservedInitialNulls;
            private readonly DirectParameterType[] _parameterTypes;
            private readonly IReadOnlyDictionary<FunctionId, FlowValueType> _directReturnTypes;
            private readonly IReadOnlyDictionary<FunctionId, FlowValueType> _universalReturnTypes;
            private readonly DirectParameterType[][] _directParameterTypes;
            private readonly FlowValueType[] _upvalueTypes;
            private readonly HashSet<int> _safeInt32Mutations;
            private readonly Dictionary<int, Dictionary<string, FlowValueType>> _localFields;
            private readonly HashSet<int> _invalidLocalFields;
            private readonly bool _optimisticDirect;
            private readonly Func<Expression, CallableReturnPrediction?> _callableReturnPrediction;
            private List<Expression>[] _localDefinitions;
            private bool[] _definitionCandidates;
            private bool _changed;
            private readonly Dictionary<int, (long Min, long Max)> _guardedIntegerRanges = new();
            private readonly Dictionary<Expression, (long Min, long Max)> _expressionIntegerRanges = new(ReferenceEqualityComparer.Instance);
            private readonly Dictionary<UnaryExpression, FlowValueType> _mutationWriteTypes = new(ReferenceEqualityComparer.Instance);
            private readonly Dictionary<int, (long Min, long Max)> _invariantIntegerRanges = new();
            private FlowValueType _passReturnType;
            private bool _sawReturn;

            public TypeAnalyzer(
                FunctionBinding binding,
                HostExportCatalog hostExports,
                DirectParameterType[] parameterTypes,
                IReadOnlyDictionary<FunctionId, FlowValueType> directReturnTypes,
                DirectParameterType[][] directParameterTypes,
                IReadOnlyDictionary<FunctionId, FlowValueType> universalReturnTypes,
                FlowValueType[] upvalueTypes,
                Func<Expression, CallableReturnPrediction?> callableReturnPrediction)
            {
                _binding = binding;
                var module = binding.Module;
                var function = binding.Function;
                _module = module;
                _callableReturnPrediction = callableReturnPrediction;
                _function = function;
                _upvalueTypes = upvalueTypes;
                _names = binding.Names;
                _declarations = binding.Declarations;
                _hostExports = hostExports;
                _clrCatalog = hostExports.ClrTypes;
                _expressionTypes = new Dictionary<Expression, FlowValueType>(ReferenceEqualityComparer.Instance);
                _structuralTypes = new Dictionary<Expression, TypeDeclaration>(ReferenceEqualityComparer.Instance);
                _nativeObjectTypes = new Dictionary<Expression, HostNativeObjectDescriptor>(
                    ReferenceEqualityComparer.Instance);
                _locals = new FlowValueType[function.LocalSlots.Length];
                _localStructuralTypes = new TypeDeclaration[function.LocalSlots.Length];
                _localNativeObjectTypes = new HostNativeObjectDescriptor[function.LocalSlots.Length];
                _clrTypes = new Dictionary<Expression, Type>(ReferenceEqualityComparer.Instance);
                _localClrTypes = new Type[function.LocalSlots.Length];
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

                var parameterIndex = 0;
                for (var i = 0; i < function.LocalSlots.Length; i++)
                {
                    if (function.LocalSlots[i].IsParameter)
                    {
                        var checkedType = function.LocalSlots[i].Declaration is
                            ParameterDeclaration parameter
                                ? TypeReferenceFacts.GetFlowType(
                                    module.Declaration,
                                    parameter.DeclaredType,
                                    hostExports)
                                : FlowValueType.None;
                        var directParameter = parameterTypes != null &&
                            parameterIndex < parameterTypes.Length &&
                            parameterTypes[parameterIndex].Type != FlowValueType.None
                                ? parameterTypes[parameterIndex]
                                : default;
                        var parameterType = checkedType == FlowValueType.Number &&
                            directParameter.IsInt32Coercion
                            ? FlowValueType.Int32
                            : checkedType != FlowValueType.None
                                ? checkedType
                                : directParameter.Type != FlowValueType.None
                                    ? FlowValueTypeFacts.GetDirectLocalType(directParameter)
                                    : FlowValueType.Dynamic;
                        // Every closure cell stores a ScriptDatum, including typed parameters.
                        _locals[i] = IsCaptured(function.LocalSlots[i].Id)
                                ? FlowValueType.Dynamic
                                : parameterType;
                        // Only a declared type that is also the storage pins the
                        // slot. A declared Number that every use narrows to a
                        // bitwise Int32 must stay free to widen again.
                        if (checkedType != FlowValueType.None && _locals[i] == checkedType)
                        {
                            _forcedLocalTypes[i] = checkedType;
                        }
                        if (function.LocalSlots[i].Declaration is
                                ParameterDeclaration typedParameter &&
                            TypeReferenceFacts.TryGetCustomType(
                                module.Declaration,
                                typedParameter.DeclaredType,
                                out var parameterStructuralType))
                        {
                            _localStructuralTypes[i] = parameterStructuralType;
                        }
                        if (function.LocalSlots[i].Declaration is
                                ParameterDeclaration nativeParameter &&
                            !IsCaptured(function.LocalSlots[i].Id) &&
                            TypeReferenceFacts.TryGetNativeObject(
                                hostExports,
                                nativeParameter.DeclaredType,
                                out var parameterNativeType))
                        {
                            _localNativeObjectTypes[i] = parameterNativeType;
                            if (_locals[i] == FlowValueType.None ||
                                _locals[i] == FlowValueType.Dynamic)
                            {
                                _locals[i] = FlowValueType.Object;
                            }
                        }
                        if (function.LocalSlots[i].Declaration is
                                ParameterDeclaration clrParameter &&
                            TypeReferenceFacts.TryGetClrType(
                                hostExports,
                                clrParameter.DeclaredType,
                                out var parameterClrType))
                        {
                            _localClrTypes[i] = parameterClrType;
                            _locals[i] = FlowValueType.Object;
                        }
                        parameterIndex++;
                    }
                    else if (function.LocalSlots[i].Declaration is ContextDeclaration context)
                    {
                        if (TypeReferenceFacts.TryGetNativeObject(
                            hostExports,
                            context.DeclaredType,
                            out var contextNative))
                        {
                            _locals[i] = FlowValueType.Object;
                            _localNativeObjectTypes[i] = contextNative;
                        }
                        else
                        {
                            _locals[i] = FlowValueType.Object;
                        }
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
                _unobservedInitialNulls = _binding.UnobservedInitialNulls ??= new InitialNullReadAnalyzer(
                    _function, _names, _declarations, IsCaptured).Analyze(body);
                _localDefinitions = new LocalDefinitionCollector(_function, _names, IsCaptured).Analyze(body, out _definitionCandidates);
                var passLimit = Math.Max(4, _locals.Length + 2);
                var needsFinalAnalysis = AnalyzeToFixedPoint(
                    body as Statement,
                    passLimit);

                var storageChanged = ApplyExactNumericStorage(body);
                storageChanged |= ApplyLocalCoercionStorage(body);
                if (storageChanged)
                {
                    needsFinalAnalysis = AnalyzeToFixedPoint(
                        body as Statement,
                        passLimit);
                }

                // A converged pass already contains the final expression facts.
                // Preserve the historical extra pass only when the safety limit
                // was exhausted while facts were still changing.
                if (needsFinalAnalysis)
                {
                    PrepareAnalysisPass(clearObjectFacts: false);
                    AnalyzeStatement(body as Statement);
                }
                // Derive invariants only from a completed, conservative analysis
                // of every definition. This can narrow loop-carried copies of an
                // index that has already passed an array bounds check.
                for (var pass = 0; pass < passLimit && ApplyProvenIntegerRanges(); pass++)
                    AnalyzeToFixedPoint(body as Statement, passLimit);
                var returnType = _sawReturn
                    ? _passReturnType
                    : FlowValueType.Null;
                var sequentialReturn = new SequentialReturnTypeAnalyzer(
                    _function,
                    _names,
                    _declarations,
                    _expressionTypes,
                    _mutationWriteTypes,
                    _parameterTypes,
                    IsCaptured).Analyze(body as Statement);
                if (sequentialReturn != FlowValueType.None)
                {
                    returnType = sequentialReturn;
                }
                if (CanCompleteNormally(body as Statement))
                    returnType = FlowValueTypeFacts.Merge(returnType, FlowValueType.Null);
                var declaredReturnType = FlowValueTypeFacts.FromCheckedTypeName(
                    _function.Declaration?.ReturnType?.Name);
                if (declaredReturnType == FlowValueType.None)
                {
                    declaredReturnType = TypeReferenceFacts.GetFlowType(
                        _module.Declaration,
                        _function.Declaration?.ReturnType,
                        _hostExports);
                }
                if (declaredReturnType != FlowValueType.None &&
                    !(_optimisticDirect &&
                        declaredReturnType == FlowValueType.Number &&
                        FlowValueTypeFacts.IsNumberCompatible(returnType)))
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
                    _localNativeObjectTypes,
                    _writtenLocals,
                    returnType,
                    _nativeCalls,
                    _hostCalls,
                    _clrTypes,
                    _localClrTypes,
                    _clrCalls,
                    _clrMembers)
                {
                    ModuleCachedReads = _moduleCachedReads,
                    IntegerRanges = _expressionIntegerRanges,
                    LocalIntegerRanges = _invariantIntegerRanges
                };
            }

            private bool AnalyzeToFixedPoint(
                Statement body,
                int passLimit)
            {
                for (var pass = 0; pass < passLimit; pass++)
                {
                    PrepareAnalysisPass(clearObjectFacts: true);
                    AnalyzeStatement(body);
                    if (!_changed)
                    {
                        return false;
                    }
                }
                return true;
            }

            private void PrepareAnalysisPass(bool clearObjectFacts)
            {
                _changed = false;
                _passReturnType = FlowValueType.None;
                _sawReturn = false;
                _guardedIntegerRanges.Clear();
                _expressionIntegerRanges.Clear();
                _mutationWriteTypes.Clear();
                _stringIndexBounds.Clear();
                _indexWriteVersions.Clear();
                _moduleValues?.Clear();
                _moduleCachedReads?.Clear();
                _moduleValueEpoch = 0;
                _expressionTypes.Clear();
                _nativeCalls?.Clear();
                _hostCalls?.Clear();
                _clrCalls?.Clear();
                _clrMembers?.Clear();
                if (clearObjectFacts)
                {
                    _structuralTypes.Clear();
                    _nativeObjectTypes.Clear();
                    _clrTypes.Clear();
                }
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
                            if (!_function.IsModuleInitializer && variable.Initializer == null && _unobservedInitialNulls[slot.Value])
                                return;
                            var initializerType = variable.Initializer == null
                                ? FlowValueType.Null
                                : AnalyzeExpression(variable.Initializer);
                            MergeLocal(slot, initializerType);
                            RecordIntegerRange(slot, TryGetIntegerRange(variable.Initializer, out var initializerMin, out var initializerMax), initializerMin, initializerMax);
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
                            if (!_writtenLocals[slot.Value] &&
                                !IsCaptured(slot) &&
                                variable.Initializer != null &&
                                _clrTypes.TryGetValue(variable.Initializer, out var initializerClrType))
                            {
                                _localClrTypes[slot.Value] = initializerClrType;
                            }
                            if (variable.Initializer is MapExpression map)
                            {
                                MergeLocalFields(slot, map);
                            }

                        }
                        else
                        {
                            AnalyzeExpression(variable.Initializer);
                        }
                        if (_function.IsModuleInitializer) RecordModuleVariable(variable);
                        return;
                    case FunctionDeclaration:
                        return;
                    case ExpressionStatement expression:
                        AnalyzeExpression(expression.Expression);
                        return;
                    case ReturnStatement @return:
                        _sawReturn = true;
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
                        var stringBoundsBeforeIf = _stringIndexBounds.Count;
                        var integerBeforeIf = new Dictionary<int, (long Min, long Max)>(_guardedIntegerRanges);
                        RefineIntegerCondition(@if.Condition);
                        var ifBefore = SnapshotStructural();
                        AnalyzeStatement(@if.Body);
                        var thenStructural = SnapshotStructural();
                        var integerThen = new Dictionary<int, (long Min, long Max)>(_guardedIntegerRanges);
                        RestoreStringBounds(stringBoundsBeforeIf);
                        RestoreIntegerRanges(integerBeforeIf);
                        RefineIntegerCondition(@if.Condition, truth: false);
                        RestoreStructural(ifBefore);
                        AnalyzeStatement(@if.Else);
                        RestoreStringBounds(stringBoundsBeforeIf);
                        IntersectStructural(thenStructural);
                        if (!CanFallThrough(@if.Else)) RestoreIntegerRanges(integerThen);
                        else if (CanFallThrough(@if.Body)) MergeIntegerRanges(integerThen);
                        _shapeSnapshotCount -= 2;
                        return;
                    case WhileStatement @while:
                        var stringBoundsBeforeWhile = _stringIndexBounds.Count;
                        var integerBeforeWhile = RetainLoopInvariantRanges(@while);
                        AnalyzeExpression(@while.Condition);
                        RefineIntegerCondition(@while.Condition);
                        var whileBefore = SnapshotStructural();
                        AnalyzeStatement(@while.Body);
                        IntersectStructural(whileBefore);
                        RestoreIntegerRanges(integerBeforeWhile);
                        RestoreStringBounds(stringBoundsBeforeWhile);
                        _shapeSnapshotCount--;
                        return;
                    case ForStatement @for:
                        if (@for.Initializer is Statement initializerStatement) AnalyzeStatement(initializerStatement);
                        else if (@for.Initializer is Expression initializerExpression) AnalyzeExpression(initializerExpression);
                        var integerEntryFor = new Dictionary<int, (long Min, long Max)>(_guardedIntegerRanges);
                        var integerBeforeFor = RetainLoopInvariantRanges(@for);
                        var stringBoundsBeforeFor = _stringIndexBounds.Count;
                        AnalyzeExpression(@for.Condition);
                        RefineIntegerCondition(@for.Condition);
                        var forBefore = SnapshotStructural();
                        if (TryGetSafeInt32Induction(@for, out var inductionSlot))
                        {
                            if (integerEntryFor.TryGetValue(inductionSlot.Value, out var initialRange) &&
                                _guardedIntegerRanges.TryGetValue(inductionSlot.Value, out var guardedRange))
                                _guardedIntegerRanges[inductionSlot.Value] = (Math.Max(initialRange.Min, guardedRange.Min), guardedRange.Max);
                            _safeInt32Mutations.Add(inductionSlot.Value);
                            try
                            {
                                AnalyzeStatement(@for.Body);
                                // A continue may bypass the body's final writes
                                // before reaching this shared incrementor.
                                RetainLoopInvariantRanges(@for.Body);
                                AnalyzeExpression(@for.Incrementor);
                            }
                            finally
                            {
                                _safeInt32Mutations.Remove(inductionSlot.Value);
                            }
                        }
                        else
                        {
                            AnalyzeStatement(@for.Body);
                            RetainLoopInvariantRanges(@for.Body);
                            AnalyzeExpression(@for.Incrementor);
                        }
                        IntersectStructural(forBefore);
                        RestoreIntegerRanges(integerBeforeFor);
                        RestoreStringBounds(stringBoundsBeforeFor);
                        _shapeSnapshotCount--;
                        return;
                    case ForInStatement forIn:
                        InvalidateStringBounds(forIn);
                        _guardedIntegerRanges.Clear();
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
                        _guardedIntegerRanges.Clear();
                        _shapeSnapshotCount--;
                        return;
                    case TryStatement @try:
                        _guardedIntegerRanges.Clear();
                        var tryBefore = SnapshotStructural();
                        AnalyzeStatement(@try.Body);
                        if (@try.CatchBody != null)
                        {
                            var afterTry = SnapshotStructural();
                            RestoreStructural(tryBefore);
                            _guardedIntegerRanges.Clear();
                            AnalyzeStatement(@try.CatchBody);
                            IntersectStructural(afterTry);
                            _shapeSnapshotCount--;
                        }
                        _guardedIntegerRanges.Clear();
                        AnalyzeStatement(@try.FinallyBody);
                        _guardedIntegerRanges.Clear();
                        _shapeSnapshotCount--;
                        return;
                    case ThrowStatement @throw:
                        AnalyzeExpression(@throw.Expression);
                        return;
                    case DeleteStatement delete:
                        AnalyzeExpression(delete.Expression);
                        InvalidateLocalFieldsForMutation(delete.Expression);
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
                            check.AssertedType,
                            _hostExports);
                        break;
                    case TypedDocumentExpression tdoc:
                        var inferredTDocType = AnalyzeExpression(tdoc.Value);
                        type = GetTypedDocumentFlowType(tdoc, inferredTDocType);
                        break;
                    case LiteralExpression literal:
                        type = LiteralTypeFacts.GetType(literal);
                        break;
                    case NameExpression name:
                        type = AnalyzeName(name);
                        break;
                    case BinaryExpression binary:
                        var binaryLeft = AnalyzeExpression(binary.Left);
                        var stringBoundsBeforeRight = _stringIndexBounds.Count;
                        Dictionary<int, (long Min, long Max)> shortCircuitRanges = null;
                        if (binary.Operator == Operator.LogicalAnd || binary.Operator == Operator.LogicalOr)
                        {
                            shortCircuitRanges = new(_guardedIntegerRanges);
                            RefineIntegerCondition(binary.Left, binary.Operator == Operator.LogicalAnd);
                        }
                        var binaryRight = AnalyzeExpression(binary.Right);
                        RestoreStringBounds(stringBoundsBeforeRight);
                        if (shortCircuitRanges != null) MergeIntegerRanges(shortCircuitRanges);
                        type = AnalyzeBinary(
                                this,
                                binary.Operator,
                                binary.Left,
                                binary.Right,
                                binaryLeft,
                                binaryRight);
                        if (TryGetExactIntegerQuotient(binary, out var numerator, out var divisor) &&
                            TryGetIntegerRange(numerator, out var numeratorMin, out var numeratorMax) &&
                            numeratorMin >= -9007199254740991L && numeratorMax <= 9007199254740991L)
                            type = FitsInt32(numeratorMin / divisor, numeratorMax / divisor) ? FlowValueType.Int32 : FlowValueType.Number;
                        if (type == FlowValueType.Number)
                        {
                            var ranged = TryKeepRangedIntegerArithmetic(
                                binary.Operator,
                                binary.Left,
                                binary.Right,
                                binaryLeft,
                                binaryRight);
                            if (ranged != FlowValueType.None)
                            {
                                type = ranged;
                            }
                        }
                        break;
                    case AssignmentExpression assignment:
                        type = AnalyzeExpression(assignment.Right);
                        AnalyzeExpression(assignment.Left);
                        var assignmentRange = TryGetIntegerRange(assignment.Right, out var assignmentMin, out var assignmentMax);
                        _structuralTypes.TryGetValue(
                            assignment.Right,
                            out var assignedStructuralType);
                        _nativeObjectTypes.TryGetValue(
                            assignment.Right,
                            out var assignedNativeType);
                        _clrTypes.TryGetValue(
                            assignment.Right,
                            out var assignedClrType);
                        WriteTarget(
                            assignment.Left,
                            type,
                            assignedStructuralType,
                            assignedNativeType,
                            assignedClrType);
                        RecordIntegerRange(assignment.Left, assignmentRange, assignmentMin, assignmentMax);
                        break;
                    case CompoundExpression compound:
                        var left = AnalyzeExpression(compound.Left);
                        var right = AnalyzeExpression(compound.Right);
                        var inductionCompound = GetInductionCompoundType(compound);
                        type = inductionCompound != FlowValueType.None
                            ? inductionCompound
                            : AnalyzeBinary(
                                this,
                                compound.Operator.SimplerOperator,
                                compound.Left,
                                compound.Right,
                                left,
                                right);
                        if (type == FlowValueType.Number)
                        {
                            var ranged = TryKeepRangedIntegerArithmetic(
                                compound.Operator.SimplerOperator,
                                compound.Left,
                                compound.Right,
                                left,
                                right);
                            if (ranged != FlowValueType.None)
                            {
                                type = ranged;
                            }
                        }
                        if (TryGetDeclaredNumericType(compound.Left, out var declaredCompound) &&
                            CanStoreCompoundResult(declaredCompound, type))
                        {
                            type = declaredCompound;
                        }
                        var compoundRange = TryGetBinaryIntegerRange(compound.Operator.SimplerOperator,
                            compound.Left, compound.Right, out var compoundMin, out var compoundMax);
                        WriteTarget(compound.Left, type, null);
                        RecordIntegerRange(compound.Left, compoundRange, compoundMin, compoundMax);
                        break;
                    case UnaryExpression unary:
                        var operand = AnalyzeExpression(unary.Expression);
                        type = AnalyzeUnary(unary, operand);
                        if (IsMutation(unary.Operator))
                        {
                            var writeType = GetMutationWriteType(unary);
                            _mutationWriteTypes[unary] = writeType;
                            var mutationRange = TryGetIntegerRange(unary.Expression, out var mutationMin, out var mutationMax);
                            var mutationDelta = unary.Operator == Operator.PreIncrement || unary.Operator == Operator.PostIncrement ? 1 : -1;
                            WriteTarget(
                                unary.Expression,
                                writeType,
                                null);
                            RecordIntegerRange(unary.Expression, mutationRange,
                                (long)(double)(mutationMin + mutationDelta), (long)(double)(mutationMax + mutationDelta));
                        }
                        break;
                    case GroupExpression group:
                        type = FlowValueType.Null;
                        for (var i = 0; i < group.Expressions.Count; i++) type = AnalyzeExpression(group.Expressions[i]);
                        break;
                    case FunctionCallExpression call:
                        _hostCalls?.Remove(call);
                        AnalyzeExpression(call.Target);
                        var isDirectCall = IsDirectFunctionCall(call);

                        for (var i = 0; i < call.Arguments.Count; i++)
                        {
                            AnalyzeExpression(call.Arguments[i]);
                            if (!isDirectCall)
                            {
                                InvalidateLocalFieldsUsedAsValue(call.Arguments[i]);
                            }
                        }

                        if (TryBindClrCall(call, out var clrCall))
                        {
                            type = ClrTypeCatalog.GetFlowType(
                                clrCall is MethodInfo clrMethod
                                    ? clrMethod.ReturnType
                                    : clrCall.DeclaringType);
                        }
                        else if (TryGetNativeValueCallType(call, out var stringCallType))
                        {
                            type = stringCallType;
                        }
                        else if (TryGetValueFactory(call, out var valueFactory) &&
                            ScriptType.IsPrimitiveConversion(valueFactory.Method.DeclaringType))
                        {
                            type = GetNativeFlowType(valueFactory.ReturnKind);
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
                        else if (_callableReturnPrediction?.Invoke(call.Target) is CallableReturnPrediction predictedReturn)
                        {
                            // A prediction is analyzed in a separate graph. It must
                            // never become a proof or select a direct-call ABI.
                            type = predictedReturn.Type;
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
                            type = GetNativeFlowType(hostExport.ReturnKind);
                        }
                        else if (TryGetCallableType(
                                     call.Target,
                                     out var callable,
                                     out var callableModule) &&
                            callable.ReturnType != null)
                        {
                            type = TypeReferenceFacts.GetFlowType(
                                callableModule,
                                callable.ReturnType,
                                _hostExports);
                            if (type == FlowValueType.None)
                            {
                                type = FlowValueType.Dynamic;
                            }
                        }
                        else
                        {
                            type = FlowValueType.Dynamic;
                        }
                        break;
                    case GetPropertyExpression property:
                        var propertyObjectType = AnalyzeExpression(property.Object);
                        BindLoadedConstant(property);
                        type = _function.CompileTimeProperties.TryGetValue(
                                property,
                                out var propertyConstant)
                            ? FromInlineConstant(propertyConstant)
                            : TryBindClrMember(property, write: false, out var clrMember)
                                ? ClrTypeCatalog.GetFlowType(
                                    clrMember is PropertyInfo clrProperty
                                        ? clrProperty.PropertyType
                                        : ((FieldInfo)clrMember).FieldType)
                            : TryGetNativeValuePropertyType(propertyObjectType, property.Property, out var stringPropertyType)
                                ? stringPropertyType
                            : FlowValueTypeFacts.IsPackedArray(propertyObjectType) &&
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
                                : TryGetHostExportConstant(property, out var constantType)
                                    ? constantType
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
                        break;
                    case GetElementExpression element:
                        var elementObjectType = AnalyzeExpression(element.Object);
                        var indexType = AnalyzeExpression(element.Index);
                        InvalidateLocalFieldsUsedAsValue(element.Object);
                        type = FlowValueTypeFacts.IsPackedArray(elementObjectType)
                            ? FlowValueTypeFacts.GetPackedElementType(elementObjectType)
                            : FlowValueType.Dynamic;
                        if (FlowValueTypeFacts.IsPackedArray(elementObjectType) &&
                            FlowValueTypeFacts.IsNumberCompatible(indexType) &&
                            TryGetIntegerRange(element.Index, out _, out _))
                            RefineIntegerRange(element.Index, 0, int.MaxValue - 33);
                        break;
                    case SetElementExpression element:
                        var setObjectType = AnalyzeExpression(element.Object);
                        var setIndexType = AnalyzeExpression(element.Index);
                        InvalidateLocalFieldsUsedAsValue(element.Object);
                        type = AnalyzeExpression(element.Value);
                        if (UnwrapGroups(element.Index) is NameExpression setIndex &&
                            _names.TryGetValue(setIndex, out var indexBinding) && indexBinding.IsLocal &&
                            !WritesLocal(element.Value, indexBinding.Local))
                        {
                            if (FlowValueTypeFacts.IsPackedArray(setObjectType) &&
                                FlowValueTypeFacts.IsNumberCompatible(setIndexType) &&
                                TryGetIntegerRange(element.Index, out _, out _))
                                RefineIntegerRange(element.Index, 0, int.MaxValue - 33);
                            else if (setIndexType == FlowValueType.Int32 &&
                                _nativeObjectTypes.TryGetValue(element.Object, out var indexOwner) &&
                                indexOwner.ClrType == typeof(ScriptArray))
                                // Array accepts negative indices (including no-op writes).
                                RefineIntegerRange(element.Index, int.MinValue, int.MaxValue - 33);
                        }
                        break;
                    case ArrayLiteralExpression array:
                        for (var i = 0; i < array.Elements.Count; i++)
                        {
                            AnalyzeExpression(array.Elements[i]);
                        }
                        type = FlowValueType.Object;
                        break;
                    case MapExpression map:
                        for (var i = 0; i < map.Entries.Count; i++)
                        {
                            AnalyzeExpression(map.Entries[i]);
                        }
                        type = FlowValueType.Object;
                        break;
                    case MapKeyValueExpression entry:
                        type = AnalyzeExpression(entry.Value);
                        break;
                    case TemplateStringExpression template:
                        for (var i = 0; i < template.Parts.Count; i++)
                        {
                            if (template.Parts[i].IsLiteral) continue;
                            AnalyzeExpression(template.Parts[i].Expression);
                            InvalidateModuleCoercion(template.Parts[i].Expression);
                        }
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
                        type = TryGetValueFactory(@new.Expression, out var newFactory)
                            ? GetNativeFlowType(newFactory.ReturnKind)
                            : GetPackedArrayConstructionType(@new, out var packedType)
                            ? packedType
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
                if (FlowValueTypeFacts.IsNumberCompatible(type) &&
                    TryGetIntegerRange(expression, out var expressionMin, out var expressionMax))
                    _expressionIntegerRanges[expression] = (expressionMin, expressionMax);
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
                var clrType = InferClrType(expression);
                if (clrType != null)
                {
                    _clrTypes[expression] = clrType;
                }
                InvalidateModuleValuesAfter(expression);
                return type;
            }

            private Type InferClrType(Expression expression)
            {
                switch (expression)
                {
                    case CheckExpression check
                        when TypeReferenceFacts.TryGetClrType(
                            _hostExports,
                            check.AssertedType,
                            out var assertedClrType):
                        return assertedClrType;
                    case NameExpression name:
                        var binding = _names.TryGetValue(name, out var bound)
                            ? bound
                            : BoundName.Unbound;
                        if (binding.IsUnshadowedGlobal &&
                            _clrCatalog.TryGetType(
                                name.Identifier?.Value,
                                0,
                                out var staticType))
                        {
                            return staticType;
                        }
                        return binding.IsLocal
                            ? _localClrTypes[binding.Local.Value]
                            : null;
                    case NewExpression construction
                        when TryBindClrConstructor(construction, out var constructedType):
                        return constructedType;
                    case FunctionCallExpression call
                        when _clrCalls != null &&
                            _clrCalls.TryGetValue(call, out var method) &&
                            method is MethodInfo info &&
                            _clrCatalog.TryGetType(info.ReturnType, out var returned):
                        return returned;
                    case GetPropertyExpression property
                        when _clrMembers != null &&
                            _clrMembers.TryGetValue(property, out var member):
                        var memberType = member is PropertyInfo propertyInfo
                            ? propertyInfo.PropertyType
                            : ((FieldInfo)member).FieldType;
                        return _clrCatalog.TryGetType(memberType, out var memberReturned)
                            ? memberReturned
                            : null;
                    case AssignmentExpression assignment:
                        return _clrTypes.TryGetValue(assignment.Right, out var assigned)
                            ? assigned
                            : null;
                    case GroupExpression group when group.Expressions.Count != 0:
                        return _clrTypes.TryGetValue(
                            group.Expressions[group.Expressions.Count - 1],
                            out var grouped)
                                ? grouped
                                : null;
                    default:
                        return null;
                }
            }

            private bool TryBindClrConstructor(
                NewExpression construction,
                out Type constructedType)
            {
                constructedType = null;
                if (construction?.Expression is not FunctionCallExpression call ||
                    call.Target is not NameExpression name ||
                    !_names.TryGetValue(name, out var binding) ||
                    !binding.IsUnshadowedGlobal ||
                    !_clrCatalog.TryGetType(
                        name.Identifier?.Value,
                        TypeAccess.Constructor,
                        out constructedType))
                {
                    return false;
                }

                var constructor = _clrCatalog.Bind(
                    constructedType,
                    null,
                    isStatic: true,
                    call,
                    GetAnalyzedType,
                    GetAnalyzedClrType);
                if (constructor is not ConstructorInfo)
                {
                    constructedType = null;
                    return false;
                }

                (_clrCalls ??= new Dictionary<FunctionCallExpression, MethodBase>(
                    ReferenceEqualityComparer.Instance))[call] = constructor;
                return true;
            }

            private bool TryBindClrCall(FunctionCallExpression call, out MethodBase method)
            {
                method = null;
                if (call?.Target is not GetPropertyExpression
                    {
                        Object: { } receiver,
                        Property: NameExpression member
                    } ||
                    !_clrTypes.TryGetValue(receiver, out var owner))
                {
                    return false;
                }

                var isStatic = IsStaticClrReceiver(receiver, owner);
                method = _clrCatalog.Bind(
                    owner,
                    member.Identifier?.Value,
                    isStatic,
                    call,
                    GetAnalyzedType,
                    GetAnalyzedClrType);
                if (method == null)
                {
                    return false;
                }

                (_clrCalls ??= new Dictionary<FunctionCallExpression, MethodBase>(
                    ReferenceEqualityComparer.Instance))[call] = method;
                return true;
            }

            private bool TryBindClrMember(
                GetPropertyExpression property,
                bool write,
                out MemberInfo member)
            {
                member = null;
                if (property?.Object == null ||
                    property.Property is not NameExpression name ||
                    !_clrTypes.TryGetValue(property.Object, out var owner))
                {
                    return false;
                }

                member = _clrCatalog.BindMember(
                    owner,
                    name.Identifier?.Value,
                    IsStaticClrReceiver(property.Object, owner),
                    write);
                if (member == null)
                {
                    return false;
                }

                (_clrMembers ??= new Dictionary<Expression, MemberInfo>(
                    ReferenceEqualityComparer.Instance))[property] = member;
                return true;
            }

            private bool IsStaticClrReceiver(Expression expression, Type owner)
            {
                return expression is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.IsUnshadowedGlobal &&
                    _clrCatalog.TryGetType(
                        name.Identifier?.Value,
                        TypeAccess.Static,
                        out var registered) &&
                    registered == owner;
            }

            private FlowValueType GetAnalyzedType(Expression expression)
            {
                return expression != null &&
                    _expressionTypes.TryGetValue(expression, out var type)
                        ? type
                        : FlowValueType.Dynamic;
            }

            private Type GetAnalyzedClrType(Expression expression)
            {
                return expression != null &&
                    _clrTypes.TryGetValue(expression, out var type)
                        ? type
                        : null;
            }

            private HostNativeObjectDescriptor InferNativeObjectType(Expression expression)
            {
                if (expression is FunctionCallExpression predictedCall &&
                    _callableReturnPrediction?.Invoke(predictedCall.Target)?.NativeObject is { } predictedNative)
                    return predictedNative;
                if (_hostExports == null)
                {
                    return null;
                }

                switch (expression)
                {
                    case ArrayLiteralExpression:
                        return _hostExports.TryGetNativeObject(typeof(ScriptArray), out var arrayType) ? arrayType : null;
                    case TypedDocumentExpression { TypeName: "Array" }:
                        return _hostExports.TryGetNativeObject(typeof(ScriptArray), out var documentArray) ? documentArray : null;
                    case NewExpression @new:
                        return TryGetNativeConstruction(@new, out var constructed)
                            ? constructed
                            : null;
                    case TypedDocumentExpression tdoc
                        when !string.IsNullOrEmpty(tdoc.TypeName) &&
                            _hostExports.TryGetNativeObject(tdoc.TypeName, out var documentType) &&
                            typeof(INativeTypedDocument).IsAssignableFrom(documentType.ClrType):
                        return documentType;
                    case CheckExpression check
                        when TypeReferenceFacts.TryGetNativeObject(
                            _hostExports,
                            check.AssertedType,
                            out var asserted):
                        return asserted;
                    case NameExpression name when _function.IsModuleInitializer &&
                        _module.Declaration.TryGetContext(name.Identifier?.Value, out var context) &&
                        TypeReferenceFacts.TryGetNativeObject(_hostExports, context.DeclaredType, out var contextType):
                        return contextType;
                    case NameExpression name when TryGetModuleValue(name, out var moduleValue):
                        return moduleValue.NativeType;
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
                    case FunctionCallExpression call
                        when TryGetHostExport(call, out var export) &&
                            export.ReturnKind == AuroraExportValueKind.Object &&
                            _hostExports.TryGetNativeObject(
                                export.Method.ReturnType,
                                out var returned):
                        return returned;
                    case FunctionCallExpression call
                        when TryGetHostExportContract(
                            call,
                            out var contractReturned):
                        return contractReturned;
                    case FunctionCallExpression call
                        when TryGetScriptNativeReturn(call, out var scriptReturned):
                        return scriptReturned;
                    case FunctionCallExpression call
                        when TryGetCallableType(
                                call.Target,
                                out var callable,
                                out _) &&
                            TypeReferenceFacts.TryGetNativeObject(
                                _hostExports,
                                callable.ReturnType,
                                out var callableReturned):
                        return callableReturned;
                    case GetPropertyExpression property
                        when _nativeObjectTypes.TryGetValue(
                                property.Object,
                                out var receiver) &&
                            TryGetStaticPropertyName(
                                property.Property,
                                out var memberName):
                        if (receiver.TryGetField(memberName, out var field) &&
                            field.Kind == AuroraExportValueKind.Object &&
                            _hostExports.TryGetNativeObject(
                                field.Field.FieldType,
                                out var fieldType))
                        {
                            return fieldType;
                        }
                        if (receiver.TryGetGetter(memberName, out var getter) &&
                            getter.ReturnKind == AuroraExportValueKind.Object &&
                            _hostExports.TryGetNativeObject(
                                getter.Method.ReturnType,
                                out var getterType))
                        {
                            return getterType;
                        }
                        return null;
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

            private bool TryGetScriptNativeReturn(
                FunctionCallExpression call,
                out HostNativeObjectDescriptor descriptor)
            {
                descriptor = null;
                if (TypeReferenceFacts.TryGetNativeObject(
                    _hostExports, GetDirectCallDeclaration(call)?.ReturnType, out descriptor))
                {
                    return true;
                }

                if (_function.ImportedNativeCalls.TryGetValue(call, out var imported) &&
                    TypeReferenceFacts.TryGetNativeObject(
                        _hostExports,
                        imported.Declaration?.ReturnType,
                        out descriptor))
                {
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Identifies the native result of a constructor call, including constructors
            /// whose argument conversions are handled by a compatibility adapter.
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
                    candidate.Constructor != null && !CanBindNativeArguments(
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
                    !receiver.TryGetMethod(name, out var candidate))
                {
                    return false;
                }

                owner = receiver;
                if (_nativeCalls == null || !_nativeCalls.TryGetValue(call, out method))
                {
                    method = HostExportArgumentFacts.SelectNativeOverload(
                        candidate, call.Arguments, HostArgumentType, HostArgumentClrType);
                    (_nativeCalls ??= new())[call] = method;
                }
                if (method != null) return true;
                owner = null;
                return false;
            }

            private bool CanBindNativeArguments(
                FunctionCallExpression call,
                AuroraExportValueKind[] parameterKinds,
                int requiredCount,
                ParameterInfo[] clrParameters,
                int prefix,
                bool useDynamicForExtraArguments = false)
            {
                var hasParams = HostExportArgumentFacts.HasParams(parameterKinds);
                if (HasSpreadArgument(call) || call.Arguments.Count < requiredCount ||
                    !hasParams && useDynamicForExtraArguments && call.Arguments.Count > parameterKinds.Length)
                {
                    return false;
                }

                var provided = hasParams ? call.Arguments.Count : Math.Min(call.Arguments.Count, parameterKinds.Length);
                for (var i = 0; i < provided; i++)
                {
                    var argument = call.Arguments[i];
                    HostExportArgumentFacts.GetArgumentParameter(parameterKinds, clrParameters, prefix, i,
                        out var parameterKind, out var parameterType);
                    if (!HostExportArgumentFacts.CanPass(
                            parameterKind,
                            parameterType,
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
                if (receiver.TryGetGetter(name, out var getter))
                {
                    type = GetNativeFlowType(getter.ReturnKind);
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
                    AuroraExportValueKind.Int64 => FlowValueType.Int64,
                    AuroraExportValueKind.UInt64 => FlowValueType.UInt64,
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
                if (expression is NameExpression moduleName && TryGetModuleValue(moduleName, out var moduleValue))
                    return moduleValue.StructuralType;

                if (expression is FunctionCallExpression predictedCall &&
                    _callableReturnPrediction?.Invoke(predictedCall.Target)?.StructuralType is { } predictedStructural)
                    return predictedStructural;

                if (expression is FunctionCallExpression call &&
                    TypeReferenceFacts.TryGetCustomType(
                        _module.Declaration, GetDirectCallDeclaration(call)?.ReturnType,
                        out var returned))
                {
                    return returned;
                }

                if (expression is FunctionCallExpression callableCall &&
                    TryGetCallableType(
                        callableCall.Target,
                        out var callable,
                        out var callableModule) &&
                    TypeReferenceFacts.TryGetCustomType(
                        callableModule,
                        callable.ReturnType,
                        out var callableReturned))
                {
                    return callableReturned;
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
                var declaration = GetDirectCallDeclaration(call);
                return declaration != null && argumentIndex < declaration.Parameters.Count &&
                    TypeReferenceFacts.TryGetCustomType(
                        _module.Declaration, declaration.Parameters[argumentIndex].DeclaredType,
                        out var parameterType) ? parameterType : null;
            }

            private bool TryGetCallableType(
                Expression expression,
                out FunctionTypeDeclaration declaration,
                out ModuleDeclaration declarationModule) =>
                TypeReferenceFacts.TryGetCallableType(
                    _module.Declaration, _function, _names, expression,
                    out declaration, out declarationModule);

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
                    HostNativeObjectDescriptor[] nativeObjects,
                    Type[] clrTypes)
                {
                    Structural = structural;
                    NativeObjects = nativeObjects;
                    ClrTypes = clrTypes;
                }

                public TypeDeclaration[] Structural { get; }
                public HostNativeObjectDescriptor[] NativeObjects { get; }
                public Type[] ClrTypes { get; }
            }

            private ShapeSnapshot SnapshotStructural()
            {
                // Scratch storage follows branch nesting, not the number of analysis passes.
                var snapshots = _shapeSnapshots ??= new List<ShapeSnapshot>();
                if (_shapeSnapshotCount == snapshots.Count)
                {
                    snapshots.Add(new ShapeSnapshot(
                        new TypeDeclaration[_localStructuralTypes.Length],
                        new HostNativeObjectDescriptor[_localNativeObjectTypes.Length],
                        new Type[_localClrTypes.Length]));
                }
                var snapshot = snapshots[_shapeSnapshotCount++];
                Array.Copy(_localStructuralTypes, snapshot.Structural, _localStructuralTypes.Length);
                Array.Copy(_localNativeObjectTypes, snapshot.NativeObjects, _localNativeObjectTypes.Length);
                Array.Copy(_localClrTypes, snapshot.ClrTypes, _localClrTypes.Length);
                return snapshot;
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
                Array.Copy(snapshot.ClrTypes, _localClrTypes, snapshot.ClrTypes.Length);
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
                    if (_localClrTypes[i] != other.ClrTypes[i])
                    {
                        _localClrTypes[i] = null;
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
                        "Array" => FlowValueType.Object,
                        "Int32Array" => FlowValueType.Int32Array,
                        "Int8Array" => FlowValueType.Int8Array,
                        "Float32Array" => FlowValueType.Float32Array,
                        "Float64Array" => FlowValueType.Float64Array,
                        "BooleanArray" => FlowValueType.BooleanArray,
                        "UInt8Array" => FlowValueType.UInt8Array,
                        "Int16Array" => FlowValueType.Int16Array,
                        "UInt16Array" => FlowValueType.UInt16Array,
                        "UInt32Array" => FlowValueType.UInt32Array,
                        "Int64Array" => FlowValueType.Int64Array,
                        "UInt64Array" => FlowValueType.UInt64Array,
                        "Int64" => FlowValueType.Int64,
                        "UInt64" => FlowValueType.UInt64,
                        _ => FlowValueType.Dynamic
                    };
                }
                return expression.TypeName switch
                {
                    null or "" => inferred,
                    "Null" => FlowValueType.Null,
                    "Boolean" => FlowValueType.Boolean,
                    "Number" => FlowValueType.Number,
                    "Int64" => FlowValueType.Int64,
                    "UInt64" => FlowValueType.UInt64,
                    "String" => FlowValueType.String,
                    "Object" or "StringBuffer" or "Date" or "Regex" or "Path" or "HashMap" => FlowValueType.Object,
                    "Array" => FlowValueType.Object,
                    "Int32Array" => FlowValueType.Int32Array,
                    "Int8Array" => FlowValueType.Int8Array,
                    "Float32Array" => FlowValueType.Float32Array,
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
                var index = _module.GetFunctionIndex(function);
                if (_directParameterTypes == null ||
                    (uint)index >= (uint)_directParameterTypes.Length)
                {
                    return false;
                }

                var parameters = _directParameterTypes[index];
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
                            _module.HasDefaultParameter(function, i))
                        {
                            continue;
                        }
                        return false;
                    }
                }

                return true;
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
                    FlowValueTypeFacts.IsPackedArray(type);
            }

            private static bool CanStoreCompoundResult(
                FlowValueType declared,
                FlowValueType result)
            {
                if (declared is FlowValueType.Int64 or FlowValueType.UInt64)
                {
                    return FlowValueTypeFacts.IsNumeric(result);
                }
                return FlowValueTypeFacts.IsNumberCompatible(declared) &&
                    FlowValueTypeFacts.IsNumberCompatible(result);
            }

            private FlowValueType AnalyzeName(NameExpression name)
            {
                if (!_names.TryGetValue(name, out var binding))
                {
                    return FlowValueType.Dynamic;
                }
                if (binding.IsContext && _module.Declaration.TryGetContext(binding.Name, out var context))
                {
                    var contextType = TypeReferenceFacts.GetFlowType(_module.Declaration, context.DeclaredType, _hostExports);
                    return contextType == FlowValueType.None ? FlowValueType.Object : contextType;
                }
                if (binding.HasConstant)
                {
                    return FromDatum(
                        binding.Constant,
                        binding.ConstantNumericHint);
                }
                if (binding.IsLocal)
                {
                    var type = _locals[binding.Local.Value];
                    if (type == FlowValueType.Number && _guardedIntegerRanges.TryGetValue(binding.Local.Value, out var range) &&
                        FitsInt32(range.Min, range.Max)) return FlowValueType.Int32;
                    return type == FlowValueType.None ? FlowValueType.Dynamic : type;
                }
                if (binding.Upvalue.IsValid)
                {
                    return GetUpvalueType(binding.Upvalue);
                }
                if (!TryGetModuleValue(name, out var moduleValue)) return FlowValueType.Dynamic;
                if (moduleValue.CacheDeclaration != null)
                    (_moduleCachedReads ??= new())[name] = moduleValue.CacheDeclaration;
                return moduleValue.Type;
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

            private bool TryGetValueFactory(FunctionCallExpression call, out HostExportDescriptor factory)
            {
                factory = null;
                if (call?.Target is not NameExpression name || !_names.TryGetValue(name, out var binding) ||
                    !binding.IsUnshadowedGlobal || !_hostExports.TryGetValueFactory(binding.Name, out factory) ||
                    call.Arguments.Count < factory.RequiredScriptParameterCount ||
                    factory.UseDynamicForExtraArguments && call.Arguments.Count > factory.ParameterKinds.Length) return false;
                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    if (call.Arguments[i] is SpreadExpression) return false;
                    if (i < factory.ParameterKinds.Length &&
                        !HostExportArgumentFacts.CanPass(factory.ParameterKinds[i], factory.GetScriptParameterType(i),
                            _expressionTypes[call.Arguments[i]])) return false;
                }
                return true;
            }

            private bool TryGetHostExport(FunctionCallExpression call, out HostExportDescriptor descriptor)
            {
                if (_hostCalls != null && _hostCalls.TryGetValue(call, out descriptor))
                    return descriptor != null;
                descriptor = null;
                if (call?.Target is GetPropertyExpression property &&
                    TryGetStaticPropertyName(property.Property, out var memberName) &&
                    property.Object is NameExpression receiver && _names.TryGetValue(receiver, out var binding))
                {
                    var import = LoadedImportFacts.Resolve(_module, _function, binding);
                    if (import != null) descriptor = LoadedImportFacts.GetNative(import, memberName, _hostExports);
                    else if (_hostExports.TryResolveExportOwner(binding, receiver.Identifier?.Value,
                        _module.Declaration.Imports, out var ownerName))
                        _hostExports.TryGetGlobal(ownerName, memberName, out descriptor);
                    if (descriptor != null)
                    {
                        HostExportArgumentFacts.TrySelectOverload(descriptor, call.Arguments,
                            HostArgumentType, HostArgumentClrType, out descriptor);
                    }
                }
                (_hostCalls ??= new())[call] = descriptor;
                return descriptor != null;
            }

            private bool TryGetHostExportContract(
                FunctionCallExpression call,
                out HostNativeObjectDescriptor nativeType)
            {
                nativeType = null;
                if (call?.Target is not GetPropertyExpression property ||
                    !TryGetStaticPropertyName(
                        property.Property,
                        out var memberName) ||
                    property.Object is not NameExpression receiver ||
                    !_names.TryGetValue(receiver, out var binding))
                {
                    return false;
                }

                HostExportDescriptor descriptor = null;
                var import = LoadedImportFacts.Resolve(
                    _module,
                    _function,
                    binding);
                if (import != null)
                {
                    descriptor = LoadedImportFacts.GetNative(
                        import,
                        memberName,
                        _hostExports);
                }
                else if (_hostExports.TryResolveExportOwner(
                    binding,
                    receiver.Identifier?.Value,
                    _module.Declaration.Imports,
                    out var ownerName))
                {
                    _hostExports.TryGetGlobal(
                        ownerName,
                        memberName,
                        out descriptor);
                }
                if (descriptor == null)
                {
                    return false;
                }

                Type returnType = null;
                for (var candidate = descriptor;
                    candidate != null;
                    candidate = candidate.NextOverload)
                {
                    if (candidate.ReturnKind != AuroraExportValueKind.Object ||
                        returnType != null &&
                        returnType != candidate.Method.ReturnType)
                    {
                        return false;
                    }
                    returnType = candidate.Method.ReturnType;
                }
                return returnType != null &&
                    _hostExports.TryGetNativeObject(returnType, out nativeType);
            }

            private void BindLoadedConstant(GetPropertyExpression property)
            {
                if (property.Object is not NameExpression receiver ||
                    !TryGetStaticPropertyName(property.Property, out var name) ||
                    !_names.TryGetValue(receiver, out var binding)) return;
                var import = LoadedImportFacts.Resolve(_module, _function, binding);
                if (!LoadedImportFacts.TryGetStatic(import, name, out var value) ||
                    value.Kind is not (ValueKind.Null or ValueKind.Boolean or ValueKind.Number or
                        ValueKind.Int64 or ValueKind.UInt64 or ValueKind.String)) return;
                _function.CompileTimeProperties[property] = new InlineConstant(value);
                LoadedImportFacts.Record(import, name, value);
            }

            private bool TryGetHostExportConstant(GetPropertyExpression property, out FlowValueType type)
            {
                type = FlowValueType.None;
                if (property.Object is NameExpression receiver &&
                    TryGetStaticPropertyName(property.Property, out var memberName) &&
                    _names.TryGetValue(receiver, out var binding) &&
                    _hostExports.TryResolveExportOwner(
                        binding,
                        receiver.Identifier?.Value,
                        _module.Declaration.Imports,
                        out var ownerName) &&
                    _hostExports.TryGetConstant(ownerName, memberName, out var field))
                {
                    type = field.FieldType == typeof(bool) ? FlowValueType.Boolean : FlowValueType.Number;
                    return true;
                }
                return false;
            }

            private static bool IsStaticProperty(Expression property, string expected)
            {
                return property is NameExpression name &&
                    StringComparer.Ordinal.Equals(name.Identifier?.Value, expected);
            }

            private bool TryGetNativeValuePropertyType(
                FlowValueType receiver, Expression property, out FlowValueType type)
            {
                var member = TryGetStaticPropertyName(property, out var name) && _hostExports.TryGetNativeValue(receiver, out var owner)
                    ? owner.GetValueGetter(name) : null;
                type = member != null ? GetNativeFlowType(member.ReturnKind) : FlowValueType.None;
                return member != null;
            }

            private bool TryGetNativeValueCallType(FunctionCallExpression call, out FlowValueType type)
            {
                type = FlowValueType.None;
                _nativeCalls?.Remove(call);
                if (call.Target is not GetPropertyExpression property ||
                    !TryGetStaticPropertyName(property.Property, out var name) ||
                    !_expressionTypes.TryGetValue(property.Object, out var receiver))
                    return false;
                if (!_hostExports.TryGetNativeValue(receiver, out var owner)) return false;
                var binding = owner.BindValueMethod(name, call.Arguments, HostArgumentType, receiver, HostArgumentClrType);
                (_nativeCalls ??= new Dictionary<FunctionCallExpression, HostNativeMethodDescriptor>())[call] = binding;
                if (binding == null) return false;
                type = GetNativeFlowType(binding.ReturnKind);
                if (binding.Method.DeclaringType == typeof(StringValue) && binding.Method.Name == nameof(StringValue.CharCodeAtCore) &&
                    IsBoundedCharacterRead(call, property)) type = FlowValueType.Int32;
                return true;
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
                        FlowValueTypeFacts.IsPackedArray(fieldType))
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
                HostNativeObjectDescriptor nativeObjectType = null,
                Type clrType = null)
            {
                WriteModuleTarget(target, type, structuralType, nativeObjectType);
                if (target is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    (binding.IsLocal || _function.IsModuleInitializer && binding.Local.IsValid))
                {
                    if (IsCaptured(binding.Local))
                    {
                        nativeObjectType = null;
                        clrType = null;
                    }
                    InvalidateLocalFields(binding.Local);
                    _guardedIntegerRanges.Remove(binding.Local.Value);
                    _indexWriteVersions[binding.Local.Value] = IndexVersion(binding.Local.Value) + 1;
                    _writtenLocals[binding.Local.Value] = true;
                    if (_function.LocalSlots[binding.Local.Value].Declaration is ParameterDeclaration parameter)
                    {
                        if (TypeReferenceFacts.TryGetCustomType(_module.Declaration, parameter.DeclaredType, out var declaredShape))
                            structuralType = declaredShape;
                        if (!IsCaptured(binding.Local) &&
                            TypeReferenceFacts.TryGetNativeObject(_hostExports, parameter.DeclaredType, out var declaredNative))
                            nativeObjectType = declaredNative;
                    }
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
                    if (_localClrTypes[binding.Local.Value] != clrType)
                    {
                        _localClrTypes[binding.Local.Value] = clrType;
                        _changed = true;
                    }
                    MergeLocal(binding.Local, type);

                }
                else if (target is GetElementExpression element)
                {
                    var indexType = _expressionTypes.TryGetValue(element.Index, out var analyzedIndex)
                        ? analyzedIndex
                        : FlowValueType.Dynamic;
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
                    _module,
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
                        _locals[i] is FlowValueType.Int32 or FlowValueType.UInt32 or
                            FlowValueType.Int64 or FlowValueType.UInt64)
                    {
                        continue;
                    }
                    _forcedLocalTypes[i] = type;
                    _locals[i] = type;
                    changed = true;
                }
                return changed;
            }

            private bool IsInt32PackedElement(GetElementExpression element)
            {
                return _expressionTypes.TryGetValue(element.Object, out var owner) &&
                    owner is FlowValueType.Int32Array or
                        FlowValueType.Int8Array or
                        FlowValueType.Int16Array or
                        FlowValueType.UInt8Array or
                        FlowValueType.UInt16Array;
            }

            private FunctionDeclaration GetDirectCallDeclaration(FunctionCallExpression call)
            {
                if (call?.Target is not NameExpression target ||
                    !_names.TryGetValue(target, out var binding) ||
                    !binding.DirectFunction.IsValid)
                {
                    return null;
                }
                var index = _module.GetFunctionIndex(binding.DirectFunction);
                return index >= 0 ? _module.Functions[index].Declaration : null;
            }

            private FlowValueType GetDeclaredCallReturnType(FunctionCallExpression call) =>
                TypeReferenceFacts.GetFlowType(
                    _module.Declaration, GetDirectCallDeclaration(call)?.ReturnType, _hostExports);

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
                    (condition.Operator != Operator.LessThan &&
                        condition.Operator != Operator.LessThanOrEqual) ||
                    condition.Left is not NameExpression conditionName)
                {
                    return false;
                }
                if (!_names.TryGetValue(conditionName, out var conditionBinding) ||
                    !conditionBinding.IsLocal ||
                    IsCaptured(conditionBinding.Local) ||
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

                // Inclusive bounds need room for the final increment. Do not
                // infer wrapping storage for an unannotated counter at MaxValue.
                if (condition.Operator == Operator.LessThanOrEqual &&
                    (!TryEvaluateInt32Constant(condition.Right, out var inclusiveBound) ||
                        (long)inclusiveBound + writes.MaximumDelta > int.MaxValue))
                    return false;

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
                    !FunctionWritesLocal(binding.Local))
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
                    FlowValueTypeFacts.IsPackedArray(ownerType);
            }

            private sealed class Int32InductionWriteAnalyzer
            {
                private readonly TypeAnalyzer _owner;
                private readonly LocalSlotId _slot;
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
                        node is ForStatement or ForInStatement or WhileStatement)
                    {
                        if (_owner.WritesLocal(node, _slot)) IsValid = false;
                        return;
                    }

                    if (node is IfStatement conditional)
                    {
                        Visit(conditional.Condition);
                        var before = MaximumDelta;
                        Visit(conditional.Body);
                        var then = MaximumDelta;
                        MaximumDelta = before;
                        Visit(conditional.Else);
                        MaximumDelta = Math.Max(then, MaximumDelta);
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

            private bool FunctionWritesLocal(LocalSlotId slot)
            {
                // Syntactic writes do not change between fixed-point iterations.
                var writes = _binding.LocalWrites ??= new sbyte[_locals.Length];
                if (writes[slot.Value] == 0)
                {
                    writes[slot.Value] = WritesLocal(_function.Declaration?.Body, slot)
                        ? (sbyte)1 : (sbyte)-1;
                }
                return writes[slot.Value] > 0;
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

            // CLR string storage can also carry script null. Only specialize + when
            // at least one operand is known to produce an actual string.
            private bool StringMayBeNull(Expression expression, HashSet<int> visiting = null)
            {
                while (expression is GroupExpression group && group.Expressions.Count == 1)
                    expression = group.Expression;
                if (expression is LiteralExpression or TemplateStringExpression ||
                    expression is UnaryExpression unary && unary.Operator == Operator.TypeOf)
                    return expression is LiteralExpression literal && literal.Token is not StringToken;
                if (expression is BinaryExpression binary && binary.Operator == Operator.Add)
                    return StringMayBeNull(binary.Left, visiting) && StringMayBeNull(binary.Right, visiting);
                if (expression is CompoundExpression compound && compound.Operator.SimplerOperator == Operator.Add)
                    return StringMayBeNull(compound.Left, visiting) && StringMayBeNull(compound.Right, visiting);
                if (expression is NameExpression name && _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal && _definitionCandidates[binding.Local.Value])
                {
                    var slot = binding.Local.Value;
                    visiting ??= new();
                    if (!visiting.Add(slot)) return false;
                    foreach (var definition in _localDefinitions[slot])
                        if (definition != null || !_unobservedInitialNulls[slot])
                            if (StringMayBeNull(definition, visiting)) { visiting.Remove(slot); return true; }
                    visiting.Remove(slot);
                    return _localDefinitions[slot].Count == 0;
                }
                return true;
            }
            public static FlowValueType AnalyzeBinary(
                TypeAnalyzer analyzer,
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
                    if (left == FlowValueType.String || right == FlowValueType.String)
                        return left == FlowValueType.String && !(analyzer?.StringMayBeNull(leftExpression) ?? true) ||
                            right == FlowValueType.String && !(analyzer?.StringMayBeNull(rightExpression) ?? true)
                            ? FlowValueType.String : FlowValueType.Dynamic;
                    var nonNumeric = FlowValueType.String | FlowValueType.Object |
                        FlowValueType.Int32Array | FlowValueType.Int8Array |
                        FlowValueType.BooleanArray | FlowValueType.Float32Array | FlowValueType.Float64Array |
                        FlowValueType.UInt8Array | FlowValueType.Int16Array |
                        FlowValueType.UInt16Array | FlowValueType.UInt32Array |
                        FlowValueType.Int64Array | FlowValueType.UInt64Array;
                    if ((left & nonNumeric) != 0 || (right & nonNumeric) != 0)
                    {
                        return FlowValueType.Dynamic;
                    }
                    if (left == FlowValueType.Int64 && right == FlowValueType.Int64)
                    {
                        return FlowValueType.Int64;
                    }
                    if (left == FlowValueType.UInt64 && right == FlowValueType.UInt64)
                    {
                        return FlowValueType.UInt64;
                    }
                    if (FlowValueTypeFacts.MayShareExact64(left, right))
                    {
                        return FlowValueType.Dynamic;
                    }
                    if (FlowValueTypeFacts.ContainsExact64(left) ||
                        FlowValueTypeFacts.ContainsExact64(right))
                    {
                        return FlowValueType.Number;
                    }
                    return CanKeepInt32Arithmetic(
                            analyzer,
                            op,
                            leftExpression,
                            rightExpression,
                            left,
                            right)
                        ? FlowValueType.Int32
                        : FlowValueType.Number;
                }
                if (op == Operator.BitwiseOr)
                {
                    if ((left & FlowValueType.Null) != 0)
                    {
                        return left == FlowValueType.Null
                            ? right
                            : FlowValueTypeFacts.Merge(FlowValueType.Number, right);
                    }
                    if (left == FlowValueType.Int64 && right == FlowValueType.Int64)
                    {
                        return FlowValueType.Int64;
                    }
                    if (left == FlowValueType.UInt64 && right == FlowValueType.UInt64)
                    {
                        return FlowValueType.UInt64;
                    }
                    if (FlowValueTypeFacts.MayShareExact64(left, right))
                    {
                        return FlowValueType.Dynamic;
                    }
                    if (FlowValueTypeFacts.ContainsExact64(left) ||
                        FlowValueTypeFacts.ContainsExact64(right))
                    {
                        return FlowValueType.Number;
                    }
                    return FlowValueTypeFacts.IsNumberCompatible(left) &&
                        FlowValueTypeFacts.IsNumberCompatible(right)
                        ? FlowValueType.Int32
                        : FlowValueType.Number;
                }
                if (op == Operator.Subtract || op == Operator.Multiply)
                {
                    if (left == FlowValueType.Int64 && right == FlowValueType.Int64)
                    {
                        return FlowValueType.Int64;
                    }
                    if (left == FlowValueType.UInt64 && right == FlowValueType.UInt64)
                    {
                        return FlowValueType.UInt64;
                    }
                    if (FlowValueTypeFacts.MayShareExact64(left, right))
                    {
                        return FlowValueType.Dynamic;
                    }
                    if (FlowValueTypeFacts.ContainsExact64(left) ||
                        FlowValueTypeFacts.ContainsExact64(right))
                    {
                        return FlowValueType.Number;
                    }
                    return CanKeepInt32Arithmetic(
                            analyzer,
                            op,
                            leftExpression,
                            rightExpression,
                            left,
                            right)
                        ? FlowValueType.Int32
                        : FlowValueType.Number;
                }
                if (op == Operator.BitwiseAnd || op == Operator.BitwiseXor ||
                    op == Operator.LeftShift || op == Operator.SignedRightShift)
                {
                    if (left == FlowValueType.Int64 &&
                        right == FlowValueType.Int64)
                    {
                        return FlowValueType.Int64;
                    }
                    if (left == FlowValueType.UInt64 &&
                        right == FlowValueType.UInt64)
                    {
                        return FlowValueType.UInt64;
                    }
                    if (FlowValueTypeFacts.MayShareExact64(left, right))
                    {
                        return FlowValueType.Dynamic;
                    }
                    if (FlowValueTypeFacts.ContainsExact64(left) ||
                        FlowValueTypeFacts.ContainsExact64(right))
                    {
                        return FlowValueType.Number;
                    }
                    // A non-negative Int32 mask clears the sign bit even when
                    // the other operand is UInt32. Keep the exact narrow result
                    // instead of merging an Int32 initializer with UInt32 to Number.
                    if (op == Operator.BitwiseAnd &&
                        (TryEvaluateInt32Constant(leftExpression, out var leftMask) && leftMask >= 0 ||
                            TryEvaluateInt32Constant(rightExpression, out var rightMask) && rightMask >= 0))
                    {
                        return FlowValueType.Int32;
                    }
                    return FlowValueType.Int32;
                }
                if (op == Operator.Modulo)
                {
                    if (left == FlowValueType.Int64 && right == FlowValueType.Int64)
                    {
                        return FlowValueType.Int64;
                    }
                    if (left == FlowValueType.UInt64 && right == FlowValueType.UInt64)
                    {
                        return FlowValueType.UInt64;
                    }
                    if (FlowValueTypeFacts.MayShareExact64(left, right))
                    {
                        return FlowValueType.Dynamic;
                    }
                    if (FlowValueTypeFacts.ContainsExact64(left) ||
                        FlowValueTypeFacts.ContainsExact64(right))
                    {
                        return FlowValueType.Number;
                    }
                    return FlowValueType.Number;
                }
                if (op == Operator.UnSignedRightShift)
                {
                    if (left == FlowValueType.Int64 &&
                        right == FlowValueType.Int64)
                    {
                        return FlowValueType.UInt64;
                    }
                    if (left == FlowValueType.UInt64 &&
                        right == FlowValueType.UInt64)
                    {
                        return FlowValueType.UInt64;
                    }
                    if (FlowValueTypeFacts.MayShareExact64(left, right))
                    {
                        return FlowValueType.Dynamic;
                    }
                    if (FlowValueTypeFacts.ContainsExact64(left) ||
                        FlowValueTypeFacts.ContainsExact64(right))
                    {
                        return FlowValueType.Number;
                    }
                    return FlowValueTypeFacts.IsNumberCompatible(left) && FlowValueTypeFacts.IsNumberCompatible(right)
                        ? FlowValueType.UInt32
                        : FlowValueType.Number;
                }
                if (op == Operator.Divide)
                {
                    if (left == FlowValueType.Int64 && right == FlowValueType.Int64)
                    {
                        return FlowValueType.Int64;
                    }
                    if (left == FlowValueType.UInt64 && right == FlowValueType.UInt64)
                    {
                        return FlowValueType.UInt64;
                    }
                    if (FlowValueTypeFacts.MayShareExact64(left, right))
                    {
                        return FlowValueType.Dynamic;
                    }
                    return FlowValueType.Number;
                }
                return FlowValueType.Dynamic;
            }

            private static bool CanKeepInt32Arithmetic(
                TypeAnalyzer analyzer,
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
                if (op != Operator.Add &&
                    op != Operator.Subtract &&
                    op != Operator.Multiply)
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

            private bool TryGetDeclaredNumericType(
                Expression expression,
                out FlowValueType type)
            {
                type = FlowValueType.None;
                expression = UnwrapGroups(expression);
                if (expression is CheckExpression check)
                {
                    type = TypeReferenceFacts.GetFlowType(
                        _module.Declaration,
                        check.AssertedType,
                        _hostExports);
                    return type is FlowValueType.Int32 or
                        FlowValueType.UInt32 or
                        FlowValueType.Int64 or
                        FlowValueType.UInt64 or
                        FlowValueType.Number;
                }
                if (expression is NameExpression name &&
                    _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal)
                {
                    type = _forcedLocalTypes[binding.Local.Value];
                    return type is FlowValueType.Int32 or
                        FlowValueType.UInt32 or
                        FlowValueType.Int64 or
                        FlowValueType.UInt64 or
                        FlowValueType.Number;
                }
                if (expression is GetElementExpression element &&
                    IsInt32PackedElement(element))
                {
                    type = FlowValueType.Int32;
                    return true;
                }
                if (expression is GetElementExpression unsignedElement &&
                    _expressionTypes.TryGetValue(unsignedElement.Object, out var arrayType) &&
                    arrayType == FlowValueType.UInt32Array)
                {
                    type = FlowValueType.UInt32;
                    return true;
                }
                if (expression is GetPropertyExpression property &&
                    TryGetStaticPropertyName(property.Property, out var fieldName) &&
                    _structuralTypes.TryGetValue(property.Object, out var owner))
                {
                    var module = GetTypeModule(owner);
                    for (var i = 0; i < owner.Fields.Count; i++)
                    {
                        var field = owner.Fields[i];
                        if (StringComparer.Ordinal.Equals(field.Name.Value, fieldName))
                        {
                            type = TypeReferenceFacts.GetFlowType(module, field.Type);
                            return type is FlowValueType.Int32 or
                                FlowValueType.UInt32 or
                                FlowValueType.Int64 or
                                FlowValueType.UInt64 or
                                FlowValueType.Number;
                        }
                    }
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
                    !TryEvaluateInt32Constant(rightExpression, out var right) ||
                    op == Operator.Multiply && (left == 0 && right < 0 || right == 0 && left < 0))
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
                        when number.Suffix != NumericLiteralSuffix.Number &&
                            number.Suffix != NumericLiteralSuffix.Int64 &&
                            number.Suffix != NumericLiteralSuffix.UInt64 &&
                            number.Suffix != NumericLiteralSuffix.UInt32 &&
                            NumericLiteralFacts.IsExactInt32(number.NumberValue):
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
                            else if (binary.Operator == Operator.Multiply &&
                                !(left == 0 && right < 0 || right == 0 && left < 0)) value = checked(left * right);
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
                        when number.Suffix == NumericLiteralSuffix.Int64 &&
                            number.TryGetInt64(out var literalValue):
                        value = literalValue;
                        return true;
                    case UnaryExpression
                    {
                        Operator: var negate,
                        Expression: LiteralExpression
                        {
                            Token: NumberToken
                            {
                                Suffix: NumericLiteralSuffix.Int64
                            } number
                        }
                    } when negate == Operator.Negate &&
                        number.TryGetNegatedInt64(out var minimum):
                        value = minimum;
                        return true;
                    case UnaryExpression unary:
                        if (!TryEvaluateInt64Constant(unary.Expression, out var operand)) break;
                        if (unary.Operator == Operator.Negate &&
                            operand != 0 && operand != long.MinValue)
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
                        when TryEvaluateInt64Constant(binary.Left, out var left) &&
                            TryEvaluateInt64Constant(binary.Right, out var right):
                        try
                        {
                            if (binary.Operator == Operator.Add) value = unchecked(left + right);
                            else if (binary.Operator == Operator.Subtract) value = unchecked(left - right);
                            else if (binary.Operator == Operator.Multiply) value = unchecked(left * right);
                            else if (binary.Operator == Operator.BitwiseAnd) value = left & right;
                            else if (binary.Operator == Operator.BitwiseOr) value = left | right;
                            else if (binary.Operator == Operator.BitwiseXor) value = left ^ right;
                            else if (binary.Operator == Operator.LeftShift) value = left << ((int)right & 63);
                            else if (binary.Operator == Operator.SignedRightShift) value = left >> ((int)right & 63);
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
                return NumericLiteralFacts.IsExactInt32(value);
            }

            private static bool IsExactInt64(double value)
            {
                return NumericLiteralFacts.IsExactInt64(value);
            }

            private FlowValueType AnalyzeUnary(UnaryExpression expression, FlowValueType operand)
            {
                var op = expression.Operator;
                if (op == Operator.LogicalNot) return FlowValueType.Boolean;
                if (op == Operator.TypeOf) return FlowValueType.String;
                if (op == Operator.BitwiseNot)
                {
                    if (operand is FlowValueType.Int64 or FlowValueType.UInt64)
                    {
                        return operand;
                    }
                    if (FlowValueTypeFacts.ContainsExact64(operand))
                    {
                        return FlowValueType.Dynamic;
                    }
                    return FlowValueType.Int32;
                }
                if (IsMutation(op))
                {
                    var writeType = GetMutationWriteType(expression);
                    return op == Operator.PostIncrement || op == Operator.PostDecrement
                        ? writeType is FlowValueType.Int32 or FlowValueType.UInt32 or
                            FlowValueType.Int64 or FlowValueType.UInt64
                            ? writeType
                            : operand
                        : writeType;
                }
                if (op == Operator.Negate)
                {
                    if (operand is FlowValueType.Int64 or FlowValueType.UInt64)
                    {
                        return operand;
                    }
                    if (FlowValueTypeFacts.ContainsExact64(operand))
                    {
                        return FlowValueType.Dynamic;
                    }
                    if (TryEvaluateInt32Constant(expression, out _)) return FlowValueType.Int32;
                    if (expression.Expression is LiteralExpression
                        {
                            Token: NumberToken
                            {
                                Suffix: NumericLiteralSuffix.Int64
                            } number
                        } &&
                        number.TryGetNegatedInt64(out _))
                    {
                        return FlowValueType.Int64;
                    }
                    return FlowValueType.Number;
                }
                return operand;
            }

            private FlowValueType GetMutationWriteType(UnaryExpression expression)
            {
                if (TryGetDeclaredNumericType(expression.Expression, out var declared))
                {
                    return declared;
                }
                if (GetInductionType(expression.Expression) is var induction &&
                    induction != FlowValueType.None)
                {
                    return induction;
                }
                if (!_expressionTypes.TryGetValue(expression.Expression, out var operand))
                {
                    return FlowValueType.Dynamic;
                }
                if (operand is FlowValueType.Int64 or FlowValueType.UInt64)
                {
                    return operand;
                }
                if (FlowValueTypeFacts.ContainsExact64(operand))
                {
                    return FlowValueType.Dynamic;
                }
                if (
                    !FlowValueTypeFacts.IsNumberCompatible(operand) ||
                    !TryGetIntegerRange(expression.Expression, out var min, out var max))
                {
                    return FlowValueType.Number;
                }
                var delta = expression.Operator == Operator.PreIncrement ||
                    expression.Operator == Operator.PostIncrement
                        ? 1L
                        : -1L;
                if (!TryAddRange(min, max, delta, delta, out var nextMin, out var nextMax))
                {
                    return FlowValueType.Number;
                }
                if (operand is FlowValueType.Int32 or FlowValueType.Number && FitsInt32(nextMin, nextMax))
                {
                    return FlowValueType.Int32;
                }
                if (operand == FlowValueType.UInt32 && FitsUInt32(nextMin, nextMax))
                {
                    return FlowValueType.UInt32;
                }
                return FlowValueType.Number;
            }

            private FlowValueType TryKeepRangedIntegerArithmetic(
                Operator op, Expression leftExpression, Expression rightExpression,
                FlowValueType left, FlowValueType right)
            {
                // A narrow result can have wider operands. Emission preserves
                // their width until after the operation (especially remainder).
                return FlowValueTypeFacts.IsNumberCompatible(left) && FlowValueTypeFacts.IsNumberCompatible(right) &&
                    TryGetBinaryIntegerRange(op, leftExpression, rightExpression, out var min, out var max) && FitsInt32(min, max)
                    ? FlowValueType.Int32 : FlowValueType.None;
            }

            private bool TryGetBinaryIntegerRange(Operator op, Expression left, Expression right, out long min, out long max)
            {
                min = max = 0;
                if (!TryGetIntegerRange(left, out var a, out var b) || !TryGetIntegerRange(right, out var c, out var d)) return false;
                if (op == Operator.Add && !TryAddRange(a, b, c, d, out min, out max) ||
                    op == Operator.Subtract && !TrySubtractRange(a, b, c, d, out min, out max)) return false;
                if (op == Operator.Multiply)
                {
                    // An integer representation must preserve the sign of zero.
                    if (a <= 0 && b >= 0 && c < 0 || c <= 0 && d >= 0 && a < 0) return false;
                    try
                    {
                        min = Math.Min(Math.Min(checked(a * c), checked(a * d)), Math.Min(checked(b * c), checked(b * d)));
                        max = Math.Max(Math.Max(checked(a * c), checked(a * d)), Math.Max(checked(b * c), checked(b * d)));
                    }
                    catch (OverflowException) { return false; }
                }
                else if (op == Operator.Modulo)
                {
                    if (a < 0 || c <= 0 && d >= 0) return false;
                    max = Math.Min(b, Math.Max(Math.Abs(c), Math.Abs(d)) - 1);
                }
                else if (op != Operator.Add && op != Operator.Subtract) return false;
                // Track integral ranges even when an operation needs IEEE rounding.
                // Storage selection separately requires exact integer execution.
                if (min < -(1L << 62) || max > (1L << 62)) return false;
                min = (long)(double)min;
                max = (long)(double)max;
                return true;
            }

            private void RecordIntegerRange(Expression target, bool valid, long min, long max)
            {
                if (target is NameExpression name && _names.TryGetValue(name, out var binding) && binding.IsLocal)
                    RecordIntegerRange(binding.Local, valid, min, max);
            }

            private void RecordIntegerRange(LocalSlotId slot, bool valid, long min, long max)
            {
                if (!IsCaptured(slot) && FlowValueTypeFacts.IsNumberCompatible(_locals[slot.Value]) &&
                    valid && min <= max && min >= -(1L << 62) && max <= (1L << 62))
                    _guardedIntegerRanges[slot.Value] = (min, max);
                else _guardedIntegerRanges.Remove(slot.Value);
            }

            private bool ApplyProvenIntegerRanges()
            {
                var changed = false;
                for (var i = 0; i < _locals.Length; i++)
                {
                    if (!_definitionCandidates[i] || !FlowValueTypeFacts.IsNumberCompatible(_locals[i])) continue;
                    long min = long.MaxValue, max = long.MinValue;
                    var bounded = TryGetResetCounterRange(i, out var boundedMin, out var boundedMax) ||
                        TryGetCountedAccumulationRange(i, out boundedMin, out boundedMax);
                    var increments = false;
                    var decrements = false;
                    foreach (var definition in _localDefinitions[i])
                    {
                        if (definition == null && _unobservedInitialNulls[i]) continue;
                        if (definition is UnaryExpression mutation && IsMutation(mutation.Operator))
                        {
                            if (mutation.Operator == Operator.PreIncrement || mutation.Operator == Operator.PostIncrement)
                                increments = true;
                            else decrements = true;
                            continue;
                        }
                        if (definition == null ||
                            !_expressionIntegerRanges.TryGetValue(definition, out var range))
                        { min = long.MaxValue; break; }
                        min = Math.Min(min, range.Min);
                        max = Math.Max(max, range.Max);
                    }
                    if (bounded) { min = boundedMin; max = boundedMax; }
                    if (min > max) continue;
                    if (!bounded && (increments || decrements))
                    {
                        // Unit steps stay integral and stop at +/-2^53 under Number
                        // semantics. Start with every nonmutation definition; then
                        // intersect this induction fact with each evaluated operand.
                        const long limit = 9007199254740992L;
                        if (min < -limit || max > limit) continue;
                        var initialMin = min;
                        var initialMax = max;
                        foreach (var definition in _localDefinitions[i])
                        {
                            if (definition is not UnaryExpression mutation || !IsMutation(mutation.Operator)) continue;
                            var delta = mutation.Operator == Operator.PreIncrement || mutation.Operator == Operator.PostIncrement ? 1 : -1;
                            var range = _expressionIntegerRanges.TryGetValue(mutation.Expression, out var evaluated)
                                ? evaluated : (-limit, limit);
                            var nextMin = Math.Max(-limit, range.Item1 + delta);
                            var nextMax = Math.Min(limit, range.Item2 + delta);
                            min = Math.Min(min, decrements ? nextMin : Math.Max(initialMin, nextMin));
                            max = Math.Max(max, increments ? nextMax : Math.Min(initialMax, nextMax));
                        }
                    }
                    if (!_invariantIntegerRanges.TryGetValue(i, out var previous) || previous != (min, max))
                    {
                        _invariantIntegerRanges[i] = (min, max);
                        changed = true;
                    }
                    if (_locals[i] == FlowValueType.Number && FitsInt32(min, max))
                    {
                        _locals[i] = _forcedLocalTypes[i] = FlowValueType.Int32;
                        changed = true;
                    }
                }
                return changed;
            }

            // These facts describe the current path, not all writes in a function.
            // A branch join widens them; loop back edges retain only invariants.
            private void RestoreIntegerRanges(Dictionary<int, (long Min, long Max)> ranges)
            {
                _guardedIntegerRanges.Clear();
                foreach (var pair in ranges) _guardedIntegerRanges.Add(pair.Key, pair.Value);
            }

            private void MergeIntegerRanges(Dictionary<int, (long Min, long Max)> other)
            {
                var current = new Dictionary<int, (long Min, long Max)>(_guardedIntegerRanges);
                _guardedIntegerRanges.Clear();
                foreach (var pair in current)
                    if (other.TryGetValue(pair.Key, out var range))
                        _guardedIntegerRanges[pair.Key] =
                            (Math.Min(pair.Value.Min, range.Min), Math.Max(pair.Value.Max, range.Max));
            }

            private Dictionary<int, (long Min, long Max)> RetainLoopInvariantRanges(AstNode loop)
            {
                InvalidateStringBounds(loop);
                var ranges = new Dictionary<int, (long Min, long Max)>(_guardedIntegerRanges);
                foreach (var pair in ranges)
                    if (WritesLocal(loop, new LocalSlotId(pair.Key))) _guardedIntegerRanges.Remove(pair.Key);
                return new(_guardedIntegerRanges);
            }

            private static bool CanFallThrough(Statement statement)
            {
                if (statement is BreakStatement or ContinueStatement) return false;
                if (statement is BlockStatement block)
                {
                    foreach (var child in block.Statements) if (!CanFallThrough(child)) return false;
                    return true;
                }
                if (statement is IfStatement conditional)
                    return CanFallThrough(conditional.Body) || CanFallThrough(conditional.Else);
                return CanCompleteNormally(statement);
            }

            private void RefineIntegerCondition(Expression condition, bool truth = true)
            {
                condition = UnwrapGroups(condition);
                if (condition is UnaryExpression unary && unary.Operator == Operator.LogicalNot)
                {
                    RefineIntegerCondition(unary.Expression, !truth);
                    return;
                }
                if (condition is GetElementExpression element && IsRangeCondition(element) &&
                    TryGetIntegerRange(element.Index, out _, out _))
                {
                    RefineIntegerRange(element.Index, 0, int.MaxValue - 33);
                    return;
                }
                if (condition is not BinaryExpression binary) return;
                if (binary.Operator == (truth ? Operator.LogicalAnd : Operator.LogicalOr))
                {
                    if (!IsRangeCondition(binary)) return;
                    RefineIntegerCondition(binary.Left, truth);
                    RefineIntegerCondition(binary.Right, truth);
                    return;
                }
                RefineStringIndexBound(binary, truth);
                // Re-evaluating the operands' range is valid only for pure reads.
                if (!IsRangeOperand(binary.Left) || !IsRangeOperand(binary.Right) ||
                    !_expressionTypes.TryGetValue(binary.Left, out var leftType) || !FlowValueTypeFacts.IsNumberCompatible(leftType) ||
                    !_expressionTypes.TryGetValue(binary.Right, out var rightType) || !FlowValueTypeFacts.IsNumberCompatible(rightType) ||
                    !TryGetIntegerRange(binary.Left, out var leftMin, out var leftMax) ||
                    !TryGetIntegerRange(binary.Right, out var rightMin, out var rightMax)) return;
                var op = binary.Operator;
                if (!truth)
                    op = op == Operator.LessThan ? Operator.GreaterThanOrEqual :
                        op == Operator.LessThanOrEqual ? Operator.GreaterThan :
                        op == Operator.GreaterThan ? Operator.LessThanOrEqual :
                        op == Operator.GreaterThanOrEqual ? Operator.LessThan :
                        op == Operator.NotEqual ? Operator.Equal : op == Operator.Equal ? Operator.NotEqual : null;
                if (op == Operator.LessThan || op == Operator.LessThanOrEqual)
                {
                    var delta = op == Operator.LessThan ? 1 : 0;
                    RefineIntegerRange(binary.Left, leftMin, Math.Min(leftMax, rightMax - delta));
                    RefineIntegerRange(binary.Right, Math.Max(rightMin, leftMin + delta), rightMax);
                }
                else if (op == Operator.GreaterThan || op == Operator.GreaterThanOrEqual)
                {
                    var delta = op == Operator.GreaterThan ? 1 : 0;
                    RefineIntegerRange(binary.Left, Math.Max(leftMin, rightMin + delta), leftMax);
                    RefineIntegerRange(binary.Right, rightMin, Math.Min(rightMax, leftMax - delta));
                }
                else if (op == Operator.Equal)
                {
                    RefineIntegerRange(binary.Left, Math.Max(leftMin, rightMin), Math.Min(leftMax, rightMax));
                    RefineIntegerRange(binary.Right, Math.Max(leftMin, rightMin), Math.Min(leftMax, rightMax));
                }
                else if (op == Operator.NotEqual)
                {
                    if (rightMin == rightMax)
                        RefineIntegerRange(binary.Left, leftMin == rightMin ? leftMin + 1 : leftMin,
                            leftMax == rightMax ? leftMax - 1 : leftMax);
                    if (leftMin == leftMax)
                        RefineIntegerRange(binary.Right, rightMin == leftMin ? rightMin + 1 : rightMin,
                            rightMax == leftMax ? rightMax - 1 : rightMax);
                }
            }

            private bool IsRangeOperand(Expression expression)
            {
                expression = UnwrapGroups(expression);
                return expression is NameExpression || TryEvaluateInt32Constant(expression, out _) ||
                    expression is GetPropertyExpression property && IsStaticProperty(property.Property, "length") &&
                    IsRangeCondition(property.Object) && _expressionTypes.TryGetValue(property.Object, out var receiver) &&
                    (receiver == FlowValueType.String || FlowValueTypeFacts.IsPackedArray(receiver));
            }

            private bool IsRangeCondition(Expression expression) => UnwrapGroups(expression) switch
            {
                NameExpression or LiteralExpression => true,
                BinaryExpression binary => IsRangeCondition(binary.Left) && IsRangeCondition(binary.Right),
                UnaryExpression unary when !IsMutation(unary.Operator) => IsRangeCondition(unary.Expression),
                GetElementExpression element => _expressionTypes.TryGetValue(element.Object, out var receiver) &&
                    FlowValueTypeFacts.IsPackedArray(receiver) && IsRangeCondition(element.Object) && IsRangeCondition(element.Index),
                GetPropertyExpression property => IsRangeOperand(property),
                _ => false
            };

            private void RefineIntegerRange(Expression expression, long min, long max)
            {
                if (UnwrapGroups(expression) is NameExpression name && _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal && !IsCaptured(binding.Local) && min <= max)
                {
                    if (_guardedIntegerRanges.TryGetValue(binding.Local.Value, out var previous))
                    {
                        min = Math.Max(min, previous.Min);
                        max = Math.Min(max, previous.Max);
                    }
                    if (min <= max) _guardedIntegerRanges[binding.Local.Value] = (min, max);
                }
            }

            private bool TryGetIntegerRange(Expression expression, out long min, out long max)
            {
                expression = UnwrapGroups(expression);
                if (expression == null) { min = max = 0; return false; }
                if (_expressionIntegerRanges.TryGetValue(expression, out var evaluated))
                {
                    min = evaluated.Min;
                    max = evaluated.Max;
                    return true;
                }
                if (expression is NameExpression guarded && _names.TryGetValue(guarded, out var guardedBinding) &&
                    guardedBinding.IsLocal &&
                    (_guardedIntegerRanges.TryGetValue(guardedBinding.Local.Value, out var range) ||
                        _invariantIntegerRanges.TryGetValue(guardedBinding.Local.Value, out range)))
                {
                    min = range.Min;
                    max = range.Max;
                    return true;
                }
                if (expression is GetPropertyExpression length && IsStaticProperty(length.Property, "length") &&
                    _expressionTypes.TryGetValue(length.Object, out var receiverType) &&
                    (receiverType == FlowValueType.String || FlowValueTypeFacts.IsPackedArray(receiverType) ||
                        _nativeObjectTypes.TryGetValue(length.Object, out var lengthOwner) && lengthOwner.ClrType == typeof(ScriptArray)))
                {
                    min = 0;
                    max = receiverType == FlowValueType.String || FlowValueTypeFacts.IsPackedArray(receiverType)
                        ? int.MaxValue - 32 : int.MaxValue;
                    return true;
                }
                if (expression is GetPropertyExpression property &&
                    _function.CompileTimeProperties.TryGetValue(property, out var constant) &&
                    constant.Value.Kind == ValueKind.Number &&
                    IsExactInt64(constant.Value.Number) &&
                    constant.Value.Number >= -9007199254740991d && constant.Value.Number <= 9007199254740991d &&
                    BitConverter.DoubleToInt64Bits(constant.Value.Number) != long.MinValue)
                {
                    min = max = (long)constant.Value.Number;
                    return true;
                }
                if (TryEvaluateInt32Constant(expression, out var int32))
                {
                    min = max = int32;
                    return true;
                }
                if (expression is LiteralExpression { Token: NumberToken literal } &&
                    literal.Suffix is not (NumericLiteralSuffix.Number or NumericLiteralSuffix.Int64 or NumericLiteralSuffix.UInt64) &&
                    literal.NumberValue >= -(1L << 62) && literal.NumberValue <= (1L << 62) &&
                    Math.Truncate(literal.NumberValue) == literal.NumberValue &&
                    BitConverter.DoubleToInt64Bits(literal.NumberValue) != long.MinValue)
                {
                    min = max = (long)literal.NumberValue;
                    return true;
                }
                if (expression is NameExpression constantName && _names.TryGetValue(constantName, out var constantBinding) &&
                    constantBinding.HasConstant && constantBinding.Constant.Kind == ValueKind.Number &&
                    TypeCheckOps.IsInt32(constantBinding.Constant.Number))
                {
                    min = max = (int)constantBinding.Constant.Number;
                    return true;
                }
                if (expression is GetElementExpression packed && _expressionTypes.TryGetValue(packed.Object, out var packedType))
                {
                    (min, max) = packedType switch
                    {
                        FlowValueType.Int8Array => (sbyte.MinValue, sbyte.MaxValue),
                        FlowValueType.UInt8Array => (byte.MinValue, byte.MaxValue),
                        FlowValueType.Int16Array => (short.MinValue, short.MaxValue),
                        FlowValueType.UInt16Array => (ushort.MinValue, ushort.MaxValue),
                        _ => (0L, -1L)
                    };
                    if (min <= max) return true;
                }
                if (expression is FunctionCallExpression call && _expressionTypes.TryGetValue(call, out var callType) &&
                    callType == FlowValueType.Int32 && _nativeCalls != null && _nativeCalls.TryGetValue(call, out var nativeCall) &&
                    nativeCall?.Method.DeclaringType == typeof(StringValue) && nativeCall.Method.Name == nameof(StringValue.CharCodeAtCore))
                {
                    min = 0; max = char.MaxValue;
                    return true;
                }
                if (TryGetExactIntegerQuotient(expression, out var dividend, out var divisor) &&
                    TryGetIntegerRange(dividend, out var dividendMin, out var dividendMax) &&
                    dividendMin >= -9007199254740991L && dividendMax <= 9007199254740991L)
                {
                    min = dividendMin / divisor; max = dividendMax / divisor;
                    return true;
                }
                if (expression is BinaryExpression binary &&
                    _expressionTypes.TryGetValue(binary, out var binaryType) && FlowValueTypeFacts.IsNumberCompatible(binaryType) &&
                    TryGetBinaryIntegerRange(binary.Operator, binary.Left, binary.Right, out min, out max)) return true;
                if (expression is CompoundExpression compound &&
                    _expressionTypes.TryGetValue(compound, out var compoundType) && FlowValueTypeFacts.IsNumberCompatible(compoundType) &&
                    TryGetBinaryIntegerRange(compound.Operator.SimplerOperator, compound.Left, compound.Right, out min, out max)) return true;
                if (expression is UnaryExpression unary &&
                    TryGetIntegerRange(unary.Expression, out var operandMin, out var operandMax))
                {
                    if (unary.Operator == Operator.Negate && (operandMin > 0 || operandMax < 0))
                    {
                        min = -operandMax;
                        max = -operandMin;
                        return true;
                    }
                    if (IsMutation(unary.Operator) && operandMin >= -9007199254740992L && operandMax <= 9007199254740992L)
                    {
                        var delta = unary.Operator == Operator.PreIncrement ? 1 : unary.Operator == Operator.PreDecrement ? -1 : 0;
                        min = Math.Max(-9007199254740992L, operandMin + delta);
                        max = Math.Min(9007199254740992L, operandMax + delta);
                        return true;
                    }
                }
                if (_expressionTypes.TryGetValue(expression, out var known) && known == FlowValueType.Int32)
                {
                    min = int.MinValue;
                    max = int.MaxValue;
                    return true;
                }
                min = 0;
                max = 0;
                return false;
            }

            private static bool FitsInt32(long min, long max)
            {
                return min >= int.MinValue && max <= int.MaxValue;
            }

            private static bool FitsUInt32(long min, long max)
            {
                return min >= uint.MinValue && max <= uint.MaxValue;
            }

            private static bool TryAddRange(
                long leftMin,
                long leftMax,
                long rightMin,
                long rightMax,
                out long min,
                out long max)
            {
                try
                {
                    min = checked(leftMin + rightMin);
                    max = checked(leftMax + rightMax);
                    return true;
                }
                catch (OverflowException)
                {
                    min = 0;
                    max = 0;
                    return false;
                }
            }

            private static bool TrySubtractRange(
                long leftMin,
                long leftMax,
                long rightMin,
                long rightMax,
                out long min,
                out long max)
            {
                try
                {
                    min = checked(leftMin - rightMax);
                    max = checked(leftMax - rightMin);
                    return true;
                }
                catch (OverflowException)
                {
                    min = 0;
                    max = 0;
                    return false;
                }
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
                return FlowValueType.None;
            }

            private static bool IsMutation(Operator op)
            {
                return op == Operator.PreIncrement || op == Operator.PostIncrement ||
                    op == Operator.PreDecrement || op == Operator.PostDecrement;
            }

            private sealed class SequentialReturnTypeAnalyzer
            {
                private readonly FunctionPlan _function;
                private readonly IReadOnlyDictionary<NameExpression, BoundName> _names;
                private readonly IReadOnlyDictionary<VariableDeclaration, LocalSlotId> _declarations;
                private readonly IReadOnlyDictionary<Expression, FlowValueType> _expressionTypes;
                private readonly IReadOnlyDictionary<UnaryExpression, FlowValueType> _mutationWriteTypes;
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
                    IReadOnlyDictionary<UnaryExpression, FlowValueType> mutationWriteTypes,
                    DirectParameterType[] parameterTypes,
                    Func<LocalSlotId, bool> isCaptured)
                {
                    _function = function;
                    _names = names;
                    _declarations = declarations;
                    _expressionTypes = expressionTypes;
                    _mutationWriteTypes = mutationWriteTypes;
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
                                if (binding.HasConstant)
                                {
                                    return FromDatum(
                                        binding.Constant,
                                        binding.ConstantNumericHint);
                                }
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
                            var knownCompound = GetKnownType(compound);
                            var compoundType = AnalyzeBinary(
                                null,
                                compound.Operator.SimplerOperator,
                                compound.Left,
                                compound.Right,
                                compoundLeft,
                                compoundRight);
                            if (knownCompound is FlowValueType.Int32 or
                                FlowValueType.UInt32 or FlowValueType.Int64 or
                                FlowValueType.UInt64)
                            {
                                compoundType = knownCompound;
                            }
                            if (TryGetLocal(compound.Left, out var compoundSlot))
                            {
                                locals[compoundSlot.Value] = compoundType;
                            }
                            return compoundType;
                        case UnaryExpression unary:
                            var operand = AnalyzeExpression(unary.Expression, locals);
                            var knownUnary = GetKnownType(unary);
                            if (!IsMutation(unary.Operator))
                            {
                                if (knownUnary is FlowValueType.Int32 or
                                    FlowValueType.UInt32 or FlowValueType.Int64 or
                                    FlowValueType.UInt64)
                                {
                                    return knownUnary;
                                }
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
                                // Postfix returns the old value, whose type may
                                // be narrower than the value written back.
                                locals[mutationSlot.Value] = _mutationWriteTypes[unary];
                            }
                            return unary.Operator == Operator.PostIncrement ||
                                unary.Operator == Operator.PostDecrement
                                    ? operand
                                    : knownUnary is FlowValueType.Int32 or
                                        FlowValueType.UInt32 or FlowValueType.Int64 or
                                        FlowValueType.UInt64
                                        ? knownUnary
                                        : FlowValueType.Number;
                        case BinaryExpression binary:
                            var knownBinary = GetKnownType(binary);
                            if (knownBinary is FlowValueType.Int32 or
                                FlowValueType.UInt32 or FlowValueType.Int64 or
                                FlowValueType.UInt64)
                            {
                                return knownBinary;
                            }
                            return AnalyzeBinary(
                                null,
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

            /// <summary>
            /// Proves that an implicit initial null cannot be read. This does not
            /// make an uninitialized declaration an integer storage annotation.
            /// </summary>
            private sealed class InitialNullReadAnalyzer
            {
                private readonly IReadOnlyDictionary<NameExpression, BoundName> _names;
                private readonly IReadOnlyDictionary<VariableDeclaration, LocalSlotId> _declarations;
                private readonly bool[] _safe;
                private bool[] _assigned;

                public InitialNullReadAnalyzer(
                    FunctionPlan function,
                    IReadOnlyDictionary<NameExpression, BoundName> names,
                    IReadOnlyDictionary<VariableDeclaration, LocalSlotId> declarations,
                    Func<LocalSlotId, bool> isCaptured)
                {
                    _names = names;
                    _declarations = declarations;
                    _safe = new bool[function.LocalSlots.Length];
                    _assigned = new bool[_safe.Length];
                    for (var i = 0; i < _safe.Length; i++)
                    {
                        var slot = function.LocalSlots[i];
                        _safe[i] = !slot.IsParameter && !isCaptured(slot.Id) &&
                            slot.Declaration is VariableDeclaration { Pattern: null, Initializer: null };
                    }
                }

                public bool[] Analyze(AstNode body)
                {
                    if (Array.IndexOf(_safe, true) >= 0) Visit(body);
                    return _safe;
                }

                private void Visit(AstNode node)
                {
                    switch (node)
                    {
                        case null:
                        case FunctionDeclaration:
                        case LambdaExpression:
                            return;
                        case TryStatement:
                        case ForInStatement:
                            // Exceptional edges and iterator binding are deliberately
                            // outside this small definite-write analysis.
                            Array.Clear(_safe, 0, _safe.Length);
                            return;
                        case NameExpression name:
                            if (_names.TryGetValue(name, out var read) && read.IsLocal &&
                                !_assigned[read.Local.Value]) _safe[read.Local.Value] = false;
                            return;
                        case VariableDeclaration declaration:
                            Visit(declaration.Initializer);
                            if (_declarations.TryGetValue(declaration, out var slot))
                                _assigned[slot.Value] = declaration.Initializer != null;
                            return;
                        case AssignmentExpression assignment:
                            if (assignment.Left is NameExpression target &&
                                _names.TryGetValue(target, out var write) && write.IsLocal)
                            {
                                Visit(assignment.Right);
                                _assigned[write.Local.Value] = true;
                                return;
                            }
                            break;
                        case IfStatement branch:
                            Visit(branch.Condition);
                            var before = (bool[])_assigned.Clone();
                            Visit(branch.Body);
                            var thenState = _assigned;
                            _assigned = before;
                            Visit(branch.Else);
                            for (var i = 0; i < _assigned.Length; i++)
                                _assigned[i] &= thenState[i];
                            return;
                        case BinaryExpression binary when
                            binary.Operator == Operator.LogicalAnd || binary.Operator == Operator.LogicalOr:
                            Visit(binary.Left);
                            VisitOptional(binary.Right);
                            return;
                        case ForStatement loop:
                            Visit(loop.Initializer);
                            Visit(loop.Condition);
                            VisitOptional(loop.Body);
                            // Continue can bypass any body write; the increment
                            // may only rely on writes made before entering the body.
                            VisitOptional(loop.Incrementor);
                            return;
                        case WhileStatement loop:
                            Visit(loop.Condition);
                            VisitOptional(loop.Body);
                            return;
                    }
                    var visitor = new ChildVisitor(this);
                    AstTraversal.VisitChildren(node, ref visitor);
                }

                private void VisitOptional(AstNode node)
                {
                    var entry = (bool[])_assigned.Clone();
                    Visit(node);
                    _assigned = entry;
                }

                private readonly struct ChildVisitor : IAstChildVisitor
                {
                    private readonly InitialNullReadAnalyzer _owner;
                    public ChildVisitor(InitialNullReadAnalyzer owner) => _owner = owner;
                    public void Visit(AstNode node) => _owner.Visit(node);
                }
            }

            /// <summary>Collects all definitions for whole-local storage decisions.</summary>
            private sealed class LocalDefinitionCollector
            {
                private readonly FunctionPlan _function;
                private readonly IReadOnlyDictionary<NameExpression, BoundName> _names;
                private readonly List<Expression>[] _definitions;
                private readonly bool[] _candidates;

                public LocalDefinitionCollector(
                    FunctionPlan function,
                    IReadOnlyDictionary<NameExpression, BoundName> names,
                    Func<LocalSlotId, bool> isCaptured)
                {
                    _function = function;
                    _names = names;
                    _definitions = new List<Expression>[function.LocalSlots.Length];
                    _candidates = new bool[function.LocalSlots.Length];
                    for (var i = 0; i < function.LocalSlots.Length; i++)
                    {
                        var slot = function.LocalSlots[i];
                        _definitions[i] = new List<Expression>();
                        _candidates[i] = !slot.IsParameter &&
                            !isCaptured(slot.Id) &&
                            slot.Declaration is not ContextDeclaration &&
                            slot.Declaration is not VariableDeclaration { Pattern: not null };
                    }
                }

                public List<Expression>[] Analyze(AstNode body, out bool[] candidates)
                {
                    Visit(body);
                    candidates = _candidates;
                    return _definitions;
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
                                    _candidates[i] = false;
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
                                TryGetLocal(unary.Expression, out var mutationSlot):
                            _definitions[mutationSlot.Value].Add(unary);
                            break;
                        case ForInStatement forIn
                            when TryGetLocal(forIn.Iterator?.Left, out var iteratorSlot):
                            _candidates[iteratorSlot.Value] = false;
                            break;
                    }
                    var visitor = new CollectorChildVisitor(this);
                    AstTraversal.VisitChildren(node, ref visitor);
                }

                private bool TryGetLocal(Expression expression, out LocalSlotId slot)
                {
                    if (expression is NameExpression name &&
                        _names.TryGetValue(name, out var binding) &&
                        binding.Local.IsValid)
                    {
                        slot = binding.Local;
                        return true;
                    }
                    slot = LocalSlotId.Invalid;
                    return false;
                }

                private readonly struct CollectorChildVisitor : IAstChildVisitor
                {
                    private readonly LocalDefinitionCollector _owner;

                    public CollectorChildVisitor(LocalDefinitionCollector owner)
                    {
                        _owner = owner;
                    }

                    public void Visit(AstNode node)
                    {
                        _owner.Visit(node);
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
                                FlowValueTypeFacts.IsNumberCompatible(type))
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
                        binding.Local.IsValid)
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
                private readonly ModulePlan _module;
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
                    ModulePlan module,
                    FunctionPlan function,
                    IReadOnlyDictionary<NameExpression, BoundName> names,
                    IReadOnlyDictionary<Expression, FlowValueType> expressionTypes,
                    DirectParameterType[][] directParameters,
                    Func<LocalSlotId, bool> isCaptured)
                {
                    _module = module;
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
                        if (FlowValueTypeFacts.MayShareExact64(
                            GetExpressionType(binary.Left),
                            GetExpressionType(binary.Right)))
                        {
                            return NativeCoercionKind.None;
                        }
                        var demand = GetBinaryOperandDemand(binary.Operator, current);
                        if (demand != NativeCoercionKind.None)
                        {
                            return demand;
                        }
                    }
                    if (current.Parent is CompoundExpression compound &&
                        ReferenceEquals(compound.Right, current))
                    {
                        if (FlowValueTypeFacts.MayShareExact64(
                            GetExpressionType(compound.Left),
                            GetExpressionType(compound.Right)))
                        {
                            return NativeCoercionKind.None;
                        }
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
                            return FlowValueTypeFacts.ContainsExact64(
                                GetExpressionType(unary.Expression))
                                    ? NativeCoercionKind.None
                                    : NativeCoercionKind.ArithmeticNumber;
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
                            _module.GetFunctionIndex(targetBinding.DirectFunction) is var targetIndex &&
                            (uint)targetIndex < (uint)_directParameters.Length)
                        {
                            var parameters = _directParameters[targetIndex];
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
                                if (FlowValueTypeFacts.IsNumberCompatible(parameter.Type))
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
                        if (FlowValueTypeFacts.IsNumberCompatible(operandType) ||
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
                    return FlowValueTypeFacts.IsPackedArray(objectType);
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

            private static FlowValueType FromInlineConstant(
                InlineConstant constant)
            {
                return FromDatum(constant.Value, constant.NumericHint);
            }

            private static FlowValueType FromDatum(
                ScriptDatum datum,
                NumericLiteralSuffix numericHint = NumericLiteralSuffix.None)
            {
                return datum.Kind switch
                {
                    ValueKind.Null => FlowValueType.Null,
                    ValueKind.Boolean => FlowValueType.Boolean,
                    ValueKind.Number => numericHint switch
                    {
                        NumericLiteralSuffix.Number => FlowValueType.Number,
                        NumericLiteralSuffix.Int32 => FlowValueType.Int32,
                        NumericLiteralSuffix.UInt32 => FlowValueType.UInt32,
                        NumericLiteralSuffix.Int64 => FlowValueType.Number,
                        NumericLiteralSuffix.UInt64 => FlowValueType.Number,
                        _ => IsExactInt32(datum.Number)
                            ? FlowValueType.Int32
                            : FlowValueType.Number
                    },
                    ValueKind.Int64 => FlowValueType.Int64,
                    ValueKind.UInt64 => FlowValueType.UInt64,
                    ValueKind.String => FlowValueType.String,
                    _ => datum.Reference switch
                    {
                        Runtime.Types.ScriptInt32Array => FlowValueType.Int32Array,
                        Runtime.Types.ScriptInt8Array => FlowValueType.Int8Array,
                        Runtime.Types.ScriptFloat32Array => FlowValueType.Float32Array,
                        Runtime.Types.ScriptFloat64Array => FlowValueType.Float64Array,
                        Runtime.Types.ScriptBooleanArray => FlowValueType.BooleanArray,
                        Runtime.Types.ScriptUInt8Array => FlowValueType.UInt8Array,
                        Runtime.Types.ScriptInt16Array => FlowValueType.Int16Array,
                        Runtime.Types.ScriptUInt16Array => FlowValueType.UInt16Array,
                        Runtime.Types.ScriptUInt32Array => FlowValueType.UInt32Array,
                        Runtime.Types.ScriptInt64Array => FlowValueType.Int64Array,
                        Runtime.Types.ScriptUInt64Array => FlowValueType.UInt64Array,
                        _ => FlowValueType.Object
                    }
                };
            }
        }
    }
}
