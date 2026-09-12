using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Code
{
    internal sealed class TypedModuleCode
    {
        private readonly ModulePlan _module;
        private readonly TypedFunctionCode[] _generic;
        private readonly TypedFunctionCode[] _direct;
        private readonly DirectParameterType[][] _directParameters;

        private TypedModuleCode(
            ModulePlan module,
            TypedFunctionCode[] generic,
            TypedFunctionCode[] direct,
            DirectParameterType[][] directParameters,
            TypedFunctionCode initializer)
        {
            _module = module;
            Initializer = initializer;
            _generic = generic;
            _direct = direct;
            _directParameters = directParameters;
        }

        public TypedFunctionCode Initializer { get; }

        public static TypedModuleCode Build(ModulePlan module)
        {
            return Build(
                module,
                new HostExportCatalog(Array.Empty<Type>()));
        }

        public static TypedModuleCode Build(
            ModulePlan module,
            HostExportCatalog hostExports,
            CallableReturnPredictions callableReturns = null)
        {
            ArgumentNullException.ThrowIfNull(module);
            ArgumentNullException.ThrowIfNull(hostExports);
            return (callableReturns ?? CallableReturnPredictions.Build(new[] { module }, hostExports)).GetModuleCode(module);
        }

        internal void ApplyPredictions(CallableReturnPredictions predictions)
        {
            predictions.Apply(_module, _generic, _direct, Initializer);
            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                var parameters = _module.GetContextualParameters(function.Id);
                if (parameters == null)
                {
                    continue;
                }
                _generic[function.ModuleIndex]
                    .ApplyContextualParameterPredictions(parameters);
                _direct[function.ModuleIndex]?
                    .ApplyContextualParameterPredictions(parameters);
            }
        }

        internal static TypedModuleCode Analyze(
            ModulePlan module, HostExportCatalog hostExports, CallableReturnPredictions callableReturns)
        {
            ArgumentNullException.ThrowIfNull(module);
            ArgumentNullException.ThrowIfNull(hostExports);
            var functions = new Dictionary<FunctionId, FunctionPlan>();
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                if (function.IsDirectCallCandidate) functions[function.Id] = function;
            }

            var size = module.Functions.Count;
            var generic = new TypedFunctionCode[size];
            var direct = new TypedFunctionCode[size];
            var directParameters = new DirectParameterType[size][];
            var bindings = callableReturns.Bindings;
            var initializerBinding = callableReturns.GetInitializerBinding(module);
            var returns = new Dictionary<FunctionId, FlowValueType>();
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                var binding = bindings[function.Id.Value];
                // None is the bottom value for the direct specialization lattice.
                // Starting at Dynamic would permanently poison recursive return
                // inference (Number | Dynamic == Dynamic), preventing an otherwise
                // pure numeric recursive graph from ever reaching the double ABI.
                if (function.IsDirectCallCandidate) returns[function.Id] = FlowValueType.None;
                generic[function.ModuleIndex] = TypedFunctionBuilder.Analyze(
                    binding,
                    hostExports,
                    directReturnTypes: returns,
                    directParameterTypes: directParameters);
            }
            var universalReturns = new Dictionary<FunctionId, FlowValueType>();
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                universalReturns[function.Id] = generic[function.ModuleIndex].ReturnType;
            }

            var capturedCells = callableReturns.GetCapturedCells(module);
            var upvalueTypes = capturedCells.Analyze(generic, module);
            var initializer = TypedFunctionBuilder.Analyze(
                initializerBinding, hostExports, directReturnTypes: returns,
                directParameterTypes: directParameters, universalReturnTypes: universalReturns);
            if (!initializerBinding.HasDirectFunctionReference && CanAnalyzeIndependently(module, bindings))
            {
                AnalyzeIndependentFunctions(
                    module,
                    hostExports,
                    bindings,
                    generic,
                    direct,
                    directParameters,
                    universalReturns,
                    upvalueTypes);
                return new TypedModuleCode(module, generic, direct, directParameters, initializer);
            }

            var converged = false;
            var passLimit = Math.Min(64, Math.Max(6, module.Functions.Count + 2));
            var evidence = new Dictionary<FunctionId, ParameterEvidence>();
            var callers = new List<int>[size];
            var genericDirty = new bool[size + 1];
            var directDirty = new bool[size + 1];
            for (var index = 0; index <= size; index++)
            {
                var binding = index == size ? initializerBinding : bindings[module.Functions[index].Id.Value];
                genericDirty[index] = binding.HasDirectFunctionReference || binding.HasUpvalueReference;
                foreach (var name in binding.Names.Values)
                {
                    if (!name.DirectFunction.IsValid) continue;
                    var callee = module.GetFunctionIndex(name.DirectFunction);
                    if (callee < 0) continue;
                    var dependents = callers[callee] ??= new();
                    if (dependents.Count == 0 || dependents[^1] != index) dependents.Add(index);
                }
            }
            for (var pass = 0; pass < passLimit; pass++)
            {
                // Rebuild from the current call sites, including the previous
                // direct graph. Superseded observations must not poison later facts.
                foreach (var item in evidence)
                {
                    item.Value.Reset();
                }
                CollectParameterEvidence(module, bindings, functions, generic, direct, evidence);
                if (functions.Count != 0)
                    CollectParameterEvidence(initializerBinding, initializer, functions, evidence);
                var parameterDemands = CollectNativeParameterDemands(
                    module,
                    generic,
                    direct,
                    directParameters);
                var nextReturns = new Dictionary<FunctionId, FlowValueType>(returns.Count);
                var changed = false;

                foreach (var function in functions.Values)
                {
                    var parameterTypes = NormalizeParameterTypes(
                        module,
                        function,
                        evidence,
                        parameterDemands[function.ModuleIndex]);
                    var oldParameterTypes = directParameters[function.ModuleIndex];
                    directParameters[function.ModuleIndex] = parameterTypes;
                    if (!SameTypes(oldParameterTypes, parameterTypes))
                        InvalidateCallers(function.ModuleIndex, callers, genericDirty, directDirty);
                    var code = direct[function.ModuleIndex];
                    if (code == null || directDirty[function.ModuleIndex] || !SameTypes(oldParameterTypes, parameterTypes))
                    {
                        code = AnalyzeDirect(bindings[function.Id.Value], hostExports,
                            directParameters, returns, universalReturns, upvalueTypes);
                        if (!SameTypes(parameterTypes, directParameters[function.ModuleIndex]))
                            InvalidateCallers(function.ModuleIndex, callers, genericDirty, directDirty);
                        parameterTypes = directParameters[function.ModuleIndex];
                    }
                    direct[function.ModuleIndex] = code;
                    directDirty[function.ModuleIndex] = false;
                    nextReturns[function.Id] = code.ReturnType;
                    if (!returns.TryGetValue(function.Id, out var oldReturn) || oldReturn != code.ReturnType)
                    {
                        changed = true;
                    }
                    if (!SameTypes(oldParameterTypes, parameterTypes))
                    {
                        changed = true;
                    }
                }

                foreach (var pair in nextReturns)
                    if (!returns.TryGetValue(pair.Key, out var previous) || previous != pair.Value)
                        InvalidateCallers(module.GetFunctionIndex(pair.Key), callers, genericDirty, directDirty);
                returns = nextReturns;
                var nextUniversalReturns =
                    new Dictionary<FunctionId, FlowValueType>(universalReturns.Count);
                for (var i = 0; i < module.Functions.Count; i++)
                {
                    var function = module.Functions[i];
                    if (genericDirty[function.ModuleIndex])
                    {
                        generic[function.ModuleIndex] = TypedFunctionBuilder.Analyze(
                            bindings[function.Id.Value],
                            hostExports,
                            directReturnTypes: returns,
                            directParameterTypes: directParameters,
                            universalReturnTypes: universalReturns,
                            upvalueTypes: upvalueTypes);
                        genericDirty[function.ModuleIndex] = false;
                    }
                    var universalReturn = generic[function.ModuleIndex].ReturnType;
                    nextUniversalReturns[function.Id] = universalReturn;
                    if (!universalReturns.TryGetValue(function.Id, out var oldUniversal) ||
                        oldUniversal != universalReturn)
                    {
                        changed = true;
                    }
                }
                foreach (var pair in nextUniversalReturns)
                    if (!universalReturns.TryGetValue(pair.Key, out var previous) || previous != pair.Value)
                        InvalidateCallers(module.GetFunctionIndex(pair.Key), callers, genericDirty, directDirty);
                universalReturns = nextUniversalReturns;
                if (genericDirty[size])
                {
                    initializer = TypedFunctionBuilder.Analyze(
                        initializerBinding, hostExports, directReturnTypes: returns,
                        directParameterTypes: directParameters, universalReturnTypes: universalReturns);
                    genericDirty[size] = false;
                }

                // A closure cell is typed from the declaring function's freshly
                // rebuilt code, so the fact only reaches the closure body on the
                // next pass.
                var nextUpvalueTypes = capturedCells.Analyze(generic, module);
                if (!CapturedCellTypes.SameTypes(upvalueTypes, nextUpvalueTypes))
                {
                    changed = true;
                    foreach (var pair in nextUpvalueTypes)
                    {
                        upvalueTypes.TryGetValue(pair.Key, out var previous);
                        if (!CapturedCellTypes.SameTypes(previous, pair.Value))
                        {
                            var index = module.GetFunctionIndex(pair.Key);
                            genericDirty[index] = directDirty[index] = true;
                        }
                    }
                }
                upvalueTypes = nextUpvalueTypes;

                if (!changed && pass > 0)
                {
                    converged = true;
                    break;
                }
            }

            if (!converged)
            {
                // Never let an optimistic bottom value escape into emission. A very
                // deep or unstable graph may lose native specialization here, but its
                // generic code remains semantically correct.
                var conservativeReturns = new Dictionary<FunctionId, FlowValueType>(returns.Count);
                for (var i = 0; i < module.Functions.Count; i++)
                {
                    conservativeReturns[module.Functions[i].Id] = FlowValueType.Dynamic;
                }

                initializer = TypedFunctionBuilder.Analyze(
                    initializerBinding, hostExports, directReturnTypes: conservativeReturns,
                    directParameterTypes: directParameters, universalReturnTypes: universalReturns);
                for (var i = 0; i < module.Functions.Count; i++)
                {
                    var function = module.Functions[i];
                    direct[function.ModuleIndex] = function.IsDirectCallCandidate ? TypedFunctionBuilder.Analyze(
                        bindings[function.Id.Value],
                        hostExports,
                        directParameters[function.ModuleIndex],
                        conservativeReturns,
                        directParameters,
                        universalReturns,
                        upvalueTypes) : null;
                    generic[function.ModuleIndex] = TypedFunctionBuilder.Analyze(
                        bindings[function.Id.Value],
                        hostExports,
                        directReturnTypes: conservativeReturns,
                        directParameterTypes: directParameters,
                        universalReturnTypes: universalReturns,
                        upvalueTypes: upvalueTypes);
                }
            }

            return new TypedModuleCode(module, generic, direct, directParameters, initializer);
        }

        private static void InvalidateCallers(int callee, List<int>[] callers, bool[] genericDirty, bool[] directDirty)
        {
            if (callers[callee] == null) return;
            foreach (var caller in callers[callee]) genericDirty[caller] = directDirty[caller] = true;
        }

        private static bool CanAnalyzeIndependently(
            ModulePlan module,
            TypedFunctionBuilder.FunctionBinding[] bindings)
        {
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var binding = bindings[module.Functions[i].Id.Value];
                if (binding.HasDirectFunctionReference ||
                    binding.HasUpvalueReference)
                {
                    return false;
                }
            }
            return true;
        }

        private static void AnalyzeIndependentFunctions(
            ModulePlan module,
            HostExportCatalog hostExports,
            TypedFunctionBuilder.FunctionBinding[] bindings,
            TypedFunctionCode[] generic,
            TypedFunctionCode[] direct,
            DirectParameterType[][] directParameters,
            IReadOnlyDictionary<FunctionId, FlowValueType> universalReturns,
            IReadOnlyDictionary<FunctionId, FlowValueType[]> upvalueTypes)
        {
            var demands = CollectNativeParameterDemands(
                module,
                generic,
                direct,
                directParameters);
            var noEvidence = new Dictionary<FunctionId, ParameterEvidence>();
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                if (!function.IsDirectCallCandidate) continue;
                var parameterTypes = NormalizeParameterTypes(
                    module,
                    function,
                    noEvidence,
                    demands[function.ModuleIndex]);
                directParameters[function.ModuleIndex] = parameterTypes;
                direct[function.ModuleIndex] = AnalyzeDirect(bindings[function.Id.Value], hostExports,
                    directParameters, universalReturns, universalReturns, upvalueTypes);
            }
        }

        private static TypedFunctionCode AnalyzeDirect(TypedFunctionBuilder.FunctionBinding binding,
            HostExportCatalog hostExports, DirectParameterType[][] directParameters,
            IReadOnlyDictionary<FunctionId, FlowValueType> returns,
            IReadOnlyDictionary<FunctionId, FlowValueType> universalReturns,
            IReadOnlyDictionary<FunctionId, FlowValueType[]> upvalueTypes)
        {
            var index = binding.Function.ModuleIndex;
            var parameters = directParameters[index];
            var code = TypedFunctionBuilder.Analyze(binding, hostExports, parameters,
                returns, directParameters, universalReturns, upvalueTypes);
            var validated = ValidateParameterTypes(binding.Function, code, parameters);
            if (SameTypes(parameters, validated)) return code;
            directParameters[index] = validated;
            return TypedFunctionBuilder.Analyze(binding, hostExports, validated,
                returns, directParameters, universalReturns, upvalueTypes);
        }

        private static bool SameTypes(
            DirectParameterType[] left,
            DirectParameterType[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }
            return true;
        }

        public TypedFunctionCode GetGeneric(FunctionId function) =>
            _module.GetFunctionIndex(function) is var index && index >= 0 ? _generic[index] : null;

        public TypedFunctionCode GetDirect(FunctionId function) =>
            _module.GetFunctionIndex(function) is var index && index >= 0 ? _direct[index] : null;

        public DirectParameterType[] GetDirectParameters(FunctionId function) =>
            _module.GetFunctionIndex(function) is var index && index >= 0 ? _directParameters[index] : null;

        private static void CollectParameterEvidence(
            ModulePlan module,
            TypedFunctionBuilder.FunctionBinding[] bindings,
            IReadOnlyDictionary<FunctionId, FunctionPlan> functions,
            TypedFunctionCode[] generic,
            TypedFunctionCode[] direct,
            Dictionary<FunctionId, ParameterEvidence> evidence)
        {
            if (functions.Count == 0) return;
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                if (function.Declaration?.Body == null) continue;

                // Generic flow records coercion boundaries visible from dynamic
                // callers. Direct flow contributes the more precise facts inside
                // a specialized call graph. Both views are required: selecting
                // only the direct graph can incorrectly classify a coercion-only
                // callee as exact and force generic callers through an adapter.
                var genericCode = generic[function.ModuleIndex];
                if (genericCode != null)
                {
                    CollectParameterEvidence(bindings[function.Id.Value], genericCode, functions, evidence);
                }

                var directCode = direct[function.ModuleIndex];
                if (directCode != null && !ReferenceEquals(directCode, genericCode))
                {
                    CollectParameterEvidence(bindings[function.Id.Value], directCode, functions, evidence);
                }
            }
        }

        private static DirectParameterType[] NormalizeParameterTypes(
            ModulePlan module,
            FunctionPlan function,
            IReadOnlyDictionary<FunctionId, ParameterEvidence> evidence,
            NativeCoercionKind[] parameterDemands)
        {
            var parameterCount = 0;
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                if (function.LocalSlots[i].IsParameter) parameterCount++;
            }
            if (parameterCount == 0) return Array.Empty<DirectParameterType>();

            evidence.TryGetValue(function.Id, out var observed);
            var result = new DirectParameterType[parameterCount];
            var parameterIndex = 0;
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                if (!function.LocalSlots[i].IsParameter) continue;
                var checkedType = function.LocalSlots[i].Declaration is
                    ParameterDeclaration parameter
                        ? TypeReferenceFacts.GetFlowType(
                            module.Declaration,
                            parameter.DeclaredType)
                        : FlowValueType.None;
                var demand = parameterDemands != null &&
                    parameterIndex < parameterDemands.Length
                        ? parameterDemands[parameterIndex]
                        : NativeCoercionKind.None;
                if (checkedType != FlowValueType.None)
                {
                    result[parameterIndex++] =
                        function.CallableType == null &&
                        checkedType == FlowValueType.Number &&
                        demand is NativeCoercionKind.Int32Bitwise or NativeCoercionKind.Int32Shift
                            ? DirectParameterType.FromCoercion(demand)
                            : new DirectParameterType(checkedType);
                    continue;
                }
                var type = observed != null && parameterIndex < observed.Types.Length
                    ? observed.Types[parameterIndex]
                    : FlowValueType.None;
                var sawNonNative = observed != null &&
                    parameterIndex < observed.SawNonNative.Length &&
                    observed.SawNonNative[parameterIndex];
                var exact = new DirectParameterType(type);
                if ((!FlowValueTypeFacts.IsNativeDirectParameter(exact) || sawNonNative) &&
                    demand is NativeCoercionKind.ArithmeticNumber or NativeCoercionKind.Boolean)
                {
                    result[parameterIndex] = DirectParameterType.FromCoercion(demand);
                }
                else if (FlowValueTypeFacts.IsNumberCompatible(type) &&
                    demand is NativeCoercionKind.Int32Bitwise or NativeCoercionKind.Int32Shift)
                {
                    result[parameterIndex] = DirectParameterType.FromCoercion(demand);
                }
                else
                {
                    result[parameterIndex] = FlowValueTypeFacts.IsNativeDirectParameter(exact)
                        ? exact
                        : new DirectParameterType(FlowValueType.Dynamic);
                }
                parameterIndex++;
            }
            return result;
        }

        private static DirectParameterType[] ValidateParameterTypes(
            FunctionPlan function,
            TypedFunctionCode code,
            DirectParameterType[] parameterTypes)
        {
            if (parameterTypes == null || parameterTypes.Length == 0 || code == null)
            {
                return parameterTypes ?? Array.Empty<DirectParameterType>();
            }

            DirectParameterType[] result = null;
            var parameterIndex = 0;
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                if (!function.LocalSlots[i].IsParameter) continue;
                var parameterType = parameterTypes[parameterIndex];
                if (FlowValueTypeFacts.IsNativeDirectParameter(parameterType) &&
                    code.LocalTypes[i] != FlowValueTypeFacts.GetDirectLocalType(parameterType))
                {
                    result ??= (DirectParameterType[])parameterTypes.Clone();
                    result[parameterIndex] =
                        code.LocalTypes[i] == FlowValueType.Number &&
                        (parameterType.IsInt32Coercion ||
                            FlowValueTypeFacts.IsNumberCompatible(parameterType.Type))
                            ? new DirectParameterType(FlowValueType.Number)
                            : new DirectParameterType(FlowValueType.Dynamic);
                }
                parameterIndex++;
            }
            return result ?? parameterTypes;
        }

        private static NativeCoercionKind[][] CollectNativeParameterDemands(
            ModulePlan module,
            TypedFunctionCode[] generic,
            TypedFunctionCode[] direct,
            DirectParameterType[][] directParameters)
        {
            var result = new NativeCoercionKind[directParameters.Length][];
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                if (!function.IsDirectCallCandidate) continue;
                var code = direct[function.ModuleIndex] ?? generic[function.ModuleIndex];
                result[function.ModuleIndex] = code == null
                    ? Array.Empty<NativeCoercionKind>()
                    : new NativeParameterDemandAnalyzer(
                        module,
                        function,
                        code,
                        directParameters).Analyze();
            }
            return result;
        }

        private sealed class NativeParameterDemandAnalyzer
        {
            private readonly ModulePlan _module;
            private readonly FunctionPlan _function;
            private readonly TypedFunctionCode _code;
            private readonly DirectParameterType[][] _directParameters;
            private readonly int[] _parameterByLocal;
            private readonly NativeCoercionKind[] _demands;
            private readonly bool[] _invalid;

            public NativeParameterDemandAnalyzer(
                ModulePlan module,
                FunctionPlan function,
                TypedFunctionCode code,
                DirectParameterType[][] directParameters)
            {
                _module = module;
                _function = function;
                _code = code;
                _directParameters = directParameters;
                _parameterByLocal = new int[function.LocalSlots.Length];
                Array.Fill(_parameterByLocal, -1);

                var parameterCount = 0;
                for (var i = 0; i < function.LocalSlots.Length; i++)
                {
                    if (!function.LocalSlots[i].IsParameter) continue;
                    _parameterByLocal[i] = parameterCount++;
                }
                _demands = new NativeCoercionKind[parameterCount];
                _invalid = new bool[parameterCount];
            }

            public NativeCoercionKind[] Analyze()
            {
                Visit(_function.Declaration?.Body);
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

                var visitor = new DemandChildVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private void RecordUse(NameExpression name)
            {
                if (name.Parent is AssignmentExpression assignment &&
                    ReferenceEquals(assignment.Left, name))
                {
                    return;
                }
                var binding = _code.GetName(name);
                if (!binding.IsLocal ||
                    (uint)binding.Local.Value >= (uint)_parameterByLocal.Length)
                {
                    return;
                }

                var parameterIndex = _parameterByLocal[binding.Local.Value];
                if (parameterIndex < 0 || _invalid[parameterIndex])
                {
                    return;
                }

                var demand = GetUseDemand(name);
                if (demand == NativeCoercionKind.None)
                {
                    _invalid[parameterIndex] = true;
                    return;
                }

                var current = _demands[parameterIndex];
                if (current == NativeCoercionKind.None)
                {
                    _demands[parameterIndex] = demand;
                }
                else if (current is NativeCoercionKind.Int32Bitwise or NativeCoercionKind.Int32Shift &&
                    demand is NativeCoercionKind.Int32Bitwise or NativeCoercionKind.Int32Shift)
                {
                    _demands[parameterIndex] = NativeCoercionKind.Int32Bitwise;
                }
                else if (current != demand)
                {
                    _invalid[parameterIndex] = true;
                }
            }

            private NativeCoercionKind GetUseDemand(NameExpression name)
            {
                AstNode current = name;
                while (current.Parent is GroupExpression group &&
                    group.Expressions.Count == 1 &&
                    ReferenceEquals(group.Expression, current))
                {
                    current = group;
                }

                if (current.Parent is BinaryExpression binary &&
                    (ReferenceEquals(binary.Left, current) ||
                        ReferenceEquals(binary.Right, current)))
                {
                    if (binary.Operator == Operator.Add ||
                        binary.Operator == Operator.Subtract ||
                        binary.Operator == Operator.Multiply ||
                        binary.Operator == Operator.Divide ||
                        binary.Operator == Operator.Modulo)
                    {
                        var coercion = GetContainingInt32Coercion(binary);
                        if (coercion != NativeCoercionKind.None)
                        {
                            return coercion;
                        }
                        return NativeCoercionKind.ArithmeticNumber;
                    }
                    if (binary.Operator == Operator.BitwiseAnd ||
                        binary.Operator == Operator.BitwiseOr ||
                        binary.Operator == Operator.BitwiseXor)
                    {
                        return NativeCoercionKind.Int32Bitwise;
                    }
                    if (binary.Operator == Operator.LeftShift ||
                        binary.Operator == Operator.SignedRightShift ||
                        binary.Operator == Operator.UnSignedRightShift)
                    {
                        return NativeCoercionKind.Int32Shift;
                    }
                    return NativeCoercionKind.None;
                }

                if (current.Parent is UnaryExpression unary &&
                    ReferenceEquals(unary.Expression, current))
                {
                    if (unary.Operator == Operator.BitwiseNot)
                    {
                        return NativeCoercionKind.Int32Bitwise;
                    }
                    if (unary.Operator == Operator.Negate)
                    {
                        return NativeCoercionKind.ArithmeticNumber;
                    }
                    return unary.Operator == Operator.LogicalNot
                        ? NativeCoercionKind.Boolean
                        : NativeCoercionKind.None;
                }

                if (current.Parent is IfStatement @if &&
                    ReferenceEquals(@if.Condition, current))
                {
                    return NativeCoercionKind.Boolean;
                }
                if (current.Parent is WhileStatement @while &&
                    ReferenceEquals(@while.Condition, current))
                {
                    return NativeCoercionKind.Boolean;
                }
                if (current.Parent is ForStatement @for &&
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
                    if (argumentIndex < 0 || call.Target is not NameExpression target)
                    {
                        return NativeCoercionKind.None;
                    }

                    var targetFunction = _code.GetName(target).DirectFunction;
                    var targetIndex = _module.GetFunctionIndex(targetFunction);
                    if ((uint)targetIndex >= (uint)_directParameters.Length)
                    {
                        return NativeCoercionKind.None;
                    }
                    var parameters = _directParameters[targetIndex];
                    if (parameters == null || argumentIndex >= parameters.Length)
                    {
                        return NativeCoercionKind.None;
                    }
                    var parameter = parameters[argumentIndex];
                    return parameter.IsCoercion
                        ? parameter.Coercion
                        : NativeCoercionKind.None;
                }

                return NativeCoercionKind.None;
            }

            private static NativeCoercionKind GetContainingInt32Coercion(
                Expression expression)
            {
                AstNode current = expression;
                while (current.Parent is GroupExpression group &&
                    group.Expressions.Count == 1 &&
                    ReferenceEquals(group.Expression, current))
                {
                    current = group;
                }
                if (current.Parent is not BinaryExpression binary ||
                    (!ReferenceEquals(binary.Left, current) &&
                        !ReferenceEquals(binary.Right, current)))
                {
                    return NativeCoercionKind.None;
                }
                var op = binary.Operator;
                if (op == Operator.BitwiseAnd ||
                    op == Operator.BitwiseOr ||
                    op == Operator.BitwiseXor)
                {
                    return NativeCoercionKind.Int32Bitwise;
                }
                return op == Operator.LeftShift ||
                    op == Operator.SignedRightShift ||
                    op == Operator.UnSignedRightShift
                        ? NativeCoercionKind.Int32Shift
                        : NativeCoercionKind.None;
            }

            private readonly struct DemandChildVisitor : IAstChildVisitor
            {
                private readonly NativeParameterDemandAnalyzer _owner;

                public DemandChildVisitor(NativeParameterDemandAnalyzer owner)
                {
                    _owner = owner;
                }

                public void Visit(AstNode node)
                {
                    _owner.Visit(node);
                }
            }
        }

        private static void CollectParameterEvidence(TypedFunctionBuilder.FunctionBinding functionBinding,
            TypedFunctionCode code, IReadOnlyDictionary<FunctionId, FunctionPlan> functions,
            Dictionary<FunctionId, ParameterEvidence> evidence)
        {
            for (var i = functionBinding.BodyCallStart; i < functionBinding.Calls.Count; i++)
            {
                var call = functionBinding.Calls[i];
                if (call.Target is not NameExpression name) continue;
                var binding = code.GetName(name);
                if (binding.DirectFunction.IsValid &&
                    functions.TryGetValue(binding.DirectFunction, out var target) &&
                    target.IsDirectCallCandidate)
                {
                    AddEvidence(call, target, code, evidence);
                }
            }

        }

        private static void AddEvidence(FunctionCallExpression call, FunctionPlan target,
            TypedFunctionCode code, Dictionary<FunctionId, ParameterEvidence> evidenceByFunction)
        {
            if (!evidenceByFunction.TryGetValue(target.Id, out var evidence))
            {
                evidence = new ParameterEvidence(target.Declaration.Parameters.Count);
                evidenceByFunction[target.Id] = evidence;
            }
            for (var i = 0; i < evidence.Types.Length; i++)
            {
                var argumentType = i < call.Arguments.Count
                    ? code.GetExpressionType(call.Arguments[i])
                    : FlowValueType.Null;
                // Native parameters are optional specializations; incompatible
                // call sites continue through the generic adapter. Exact evidence
                // is therefore allowed to replace an earlier dynamic graph pass.
                // Two different exact native kinds, however, disable specialization
                // deterministically instead of depending on visitation order.
                if (FlowValueTypeFacts.IsNativeDirectParameter(
                    new DirectParameterType(argumentType)))
                {
                    if (evidence.NativeConflict[i]) continue;
                    var current = evidence.Types[i];
                    if (FlowValueTypeFacts.IsNativeDirectParameter(
                            new DirectParameterType(current)) &&
                        current != argumentType)
                    {
                        if (FlowValueTypeFacts.IsNumberCompatible(current) &&
                            FlowValueTypeFacts.IsNumberCompatible(argumentType))
                        {
                            evidence.Types[i] = FlowValueTypeFacts.Merge(
                                current,
                                argumentType);
                        }
                        else
                        {
                            evidence.NativeConflict[i] = true;
                            evidence.Types[i] = FlowValueType.Dynamic;
                        }
                    }
                    else
                    {
                        evidence.Types[i] = argumentType;
                    }
                }
                else if (!FlowValueTypeFacts.IsNativeDirectParameter(
                        new DirectParameterType(evidence.Types[i])) &&
                    !evidence.NativeConflict[i])
                {
                    evidence.SawNonNative[i] = true;
                    evidence.Types[i] |= argumentType;
                }
                else if (!FlowValueTypeFacts.IsNativeDirectParameter(
                    new DirectParameterType(argumentType)))
                {
                    evidence.SawNonNative[i] = true;
                }
            }
        }


        private sealed class ParameterEvidence
        {
            public ParameterEvidence(int count)
            {
                Types = new FlowValueType[count];
                NativeConflict = new bool[count];
                SawNonNative = new bool[count];
            }

            public FlowValueType[] Types { get; }
            public bool[] NativeConflict { get; }
            public bool[] SawNonNative { get; }

            public void Reset()
            {
                Array.Clear(Types);
                Array.Clear(NativeConflict);
                Array.Clear(SawNonNative);
            }
        }
    }
}
