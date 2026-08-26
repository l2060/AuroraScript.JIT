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
        private readonly TypedFunctionCode[] _generic;
        private readonly TypedFunctionCode[] _direct;
        private readonly DirectParameterType[][] _directParameters;

        private TypedModuleCode(
            TypedFunctionCode[] generic,
            TypedFunctionCode[] direct,
            DirectParameterType[][] directParameters)
        {
            _generic = generic;
            _direct = direct;
            _directParameters = directParameters;
        }

        public static TypedModuleCode Build(
            ModulePlan module,
            HostExportCatalog hostExports)
        {
            ArgumentNullException.ThrowIfNull(module);
            ArgumentNullException.ThrowIfNull(hostExports);
            var maxId = -1;
            var functions = new Dictionary<FunctionId, FunctionPlan>();
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                maxId = Math.Max(maxId, function.Id.Value);
                functions[function.Id] = function;
            }

            var size = maxId + 1;
            var generic = new TypedFunctionCode[size];
            var direct = new TypedFunctionCode[size];
            var directParameters = new DirectParameterType[size][];
            var returns = new Dictionary<FunctionId, FlowValueType>();
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                // None is the bottom value for the direct specialization lattice.
                // Starting at Dynamic would permanently poison recursive return
                // inference (Number | Dynamic == Dynamic), preventing an otherwise
                // pure numeric recursive graph from ever reaching the double ABI.
                returns[function.Id] = FlowValueType.None;
                generic[function.Id.Value] = TypedFunctionBuilder.Build(
                    module,
                    function,
                    hostExports,
                    directReturnTypes: returns,
                    directParameterTypes: directParameters);
            }
            var universalReturns = new Dictionary<FunctionId, FlowValueType>();
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                universalReturns[function.Id] = generic[function.Id.Value].ReturnType;
            }

            var converged = false;
            var passLimit = Math.Min(64, Math.Max(6, module.Functions.Count + 2));
            var evidence = new Dictionary<FunctionId, ParameterEvidence>();
            for (var pass = 0; pass < passLimit; pass++)
            {
                // Exact call-site evidence is monotonic and bootstraps recursive
                // native graphs. Dynamic observations are transient because an
                // early imprecise pass must not permanently poison later facts.
                foreach (var item in evidence)
                {
                    item.Value.ResetTransientEvidence();
                }
                CollectParameterEvidence(module, functions, generic, direct, evidence);
                var parameterDemands = CollectNativeParameterDemands(
                    module,
                    generic,
                    direct,
                    directParameters);
                var nextReturns = new Dictionary<FunctionId, FlowValueType>(returns.Count);
                var changed = false;

                for (var i = 0; i < module.Functions.Count; i++)
                {
                    var function = module.Functions[i];
                    var parameterTypes = NormalizeParameterTypes(
                        module,
                        function,
                        evidence,
                        parameterDemands[function.Id.Value]);
                    var oldParameterTypes = directParameters[function.Id.Value];
                    directParameters[function.Id.Value] = parameterTypes;
                    var code = TypedFunctionBuilder.Build(
                        module,
                        function,
                        hostExports,
                        parameterTypes,
                        returns,
                        directParameters,
                        universalReturns);
                    var validatedParameterTypes = ValidateParameterTypes(function, code, parameterTypes);
                    if (!SameTypes(parameterTypes, validatedParameterTypes))
                    {
                        parameterTypes = validatedParameterTypes;
                        directParameters[function.Id.Value] = parameterTypes;
                        code = TypedFunctionBuilder.Build(
                            module,
                            function,
                            hostExports,
                            parameterTypes,
                            returns,
                            directParameters,
                            universalReturns);
                    }
                    direct[function.Id.Value] = code;
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

                returns = nextReturns;
                var nextUniversalReturns =
                    new Dictionary<FunctionId, FlowValueType>(universalReturns.Count);
                for (var i = 0; i < module.Functions.Count; i++)
                {
                    var function = module.Functions[i];
                    generic[function.Id.Value] = TypedFunctionBuilder.Build(
                        module,
                        function,
                        hostExports,
                        directReturnTypes: returns,
                        directParameterTypes: directParameters,
                        universalReturnTypes: universalReturns);
                    var universalReturn = generic[function.Id.Value].ReturnType;
                    nextUniversalReturns[function.Id] = universalReturn;
                    if (!universalReturns.TryGetValue(function.Id, out var oldUniversal) ||
                        oldUniversal != universalReturn)
                    {
                        changed = true;
                    }
                }
                universalReturns = nextUniversalReturns;

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

                for (var i = 0; i < module.Functions.Count; i++)
                {
                    var function = module.Functions[i];
                    direct[function.Id.Value] = TypedFunctionBuilder.Build(
                        module,
                        function,
                        hostExports,
                        directParameters[function.Id.Value],
                        conservativeReturns,
                        directParameters,
                        universalReturns);
                    generic[function.Id.Value] = TypedFunctionBuilder.Build(
                        module,
                        function,
                        hostExports,
                        directReturnTypes: conservativeReturns,
                        directParameterTypes: directParameters,
                        universalReturnTypes: universalReturns);
                }
            }

            return new TypedModuleCode(generic, direct, directParameters);
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

        public TypedFunctionCode GetGeneric(FunctionId function)
        {
            return function.IsValid && (uint)function.Value < (uint)_generic.Length
                ? _generic[function.Value]
                : null;
        }

        public TypedFunctionCode GetDirect(FunctionId function)
        {
            return function.IsValid && (uint)function.Value < (uint)_direct.Length
                ? _direct[function.Value]
                : null;
        }

        public DirectParameterType[] GetDirectParameters(FunctionId function)
        {
            return function.IsValid && (uint)function.Value < (uint)_directParameters.Length
                ? _directParameters[function.Value]
                : null;
        }

        private static void CollectParameterEvidence(
            ModulePlan module,
            IReadOnlyDictionary<FunctionId, FunctionPlan> functions,
            TypedFunctionCode[] generic,
            TypedFunctionCode[] direct,
            Dictionary<FunctionId, ParameterEvidence> evidence)
        {
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                if (function.Declaration?.Body == null) continue;

                // Generic flow records coercion boundaries visible from dynamic
                // callers. Direct flow contributes the more precise facts inside
                // a specialized call graph. Both views are required: selecting
                // only the direct graph can incorrectly classify a coercion-only
                // callee as exact and force generic callers through an adapter.
                var genericCode = generic[function.Id.Value];
                if (genericCode != null)
                {
                    var genericCollector = new DirectCallCollector(
                        genericCode,
                        functions,
                        evidence);
                    genericCollector.Visit(function.Declaration.Body);
                }

                var directCode = direct[function.Id.Value];
                if (directCode != null && !ReferenceEquals(directCode, genericCode))
                {
                    var directCollector = new DirectCallCollector(
                        directCode,
                        functions,
                        evidence);
                    directCollector.Visit(function.Declaration.Body);
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
                if (checkedType != FlowValueType.None)
                {
                    result[parameterIndex++] =
                        new DirectParameterType(checkedType);
                    continue;
                }
                var type = observed != null && parameterIndex < observed.Types.Length
                    ? observed.Types[parameterIndex]
                    : FlowValueType.None;
                var demand = parameterDemands != null &&
                    parameterIndex < parameterDemands.Length
                        ? parameterDemands[parameterIndex]
                        : NativeCoercionKind.None;
                var sawNonNative = observed != null &&
                    parameterIndex < observed.SawNonNative.Length &&
                    observed.SawNonNative[parameterIndex];
                var exact = new DirectParameterType(type);
                if ((!FlowValueTypeFacts.IsNativeDirectParameter(exact) || sawNonNative) &&
                    demand is NativeCoercionKind.ArithmeticNumber or NativeCoercionKind.Boolean)
                {
                    result[parameterIndex] = DirectParameterType.FromCoercion(demand);
                }
                else if (FlowValueTypeFacts.IsNumeric(type) &&
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
                    result[parameterIndex] = new DirectParameterType(FlowValueType.Dynamic);
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
                var code = direct[function.Id.Value] ?? generic[function.Id.Value];
                result[function.Id.Value] = code == null
                    ? Array.Empty<NativeCoercionKind>()
                    : new NativeParameterDemandAnalyzer(
                        function,
                        code,
                        directParameters).Analyze();
            }
            return result;
        }

        private sealed class NativeParameterDemandAnalyzer
        {
            private readonly FunctionPlan _function;
            private readonly TypedFunctionCode _code;
            private readonly DirectParameterType[][] _directParameters;
            private readonly int[] _parameterByLocal;
            private readonly NativeCoercionKind[] _demands;
            private readonly bool[] _invalid;

            public NativeParameterDemandAnalyzer(
                FunctionPlan function,
                TypedFunctionCode code,
                DirectParameterType[][] directParameters)
            {
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
                    if (binary.Operator == Operator.Subtract ||
                        binary.Operator == Operator.Multiply ||
                        binary.Operator == Operator.Divide ||
                        binary.Operator == Operator.Modulo)
                    {
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
                    if (!targetFunction.IsValid ||
                        (uint)targetFunction.Value >= (uint)_directParameters.Length)
                    {
                        return NativeCoercionKind.None;
                    }
                    var parameters = _directParameters[targetFunction.Value];
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

        private sealed class DirectCallCollector
        {
            private readonly TypedFunctionCode _code;
            private readonly IReadOnlyDictionary<FunctionId, FunctionPlan> _functions;
            private readonly Dictionary<FunctionId, ParameterEvidence> _evidence;

            public DirectCallCollector(
                TypedFunctionCode code,
                IReadOnlyDictionary<FunctionId, FunctionPlan> functions,
                Dictionary<FunctionId, ParameterEvidence> evidence)
            {
                _code = code;
                _functions = functions;
                _evidence = evidence;
            }

            public void Visit(AstNode node)
            {
                if (node == null || node is FunctionDeclaration || node is LambdaExpression) return;
                if (node is FunctionCallExpression call && call.Target is NameExpression name)
                {
                    var binding = _code.GetName(name);
                    if (binding.DirectFunction.IsValid &&
                        _functions.TryGetValue(binding.DirectFunction, out var target) &&
                        target.IsDirectCallCandidate)
                    {
                        AddEvidence(call, target);
                    }
                }

                var visitor = new ChildVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private void AddEvidence(FunctionCallExpression call, FunctionPlan target)
            {
                if (!_evidence.TryGetValue(target.Id, out var evidence))
                {
                    evidence = new ParameterEvidence(target.Declaration.Parameters.Count);
                    _evidence[target.Id] = evidence;
                }
                for (var i = 0; i < evidence.Types.Length; i++)
                {
                    var argumentType = i < call.Arguments.Count
                        ? _code.GetExpressionType(call.Arguments[i])
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
                            if (FlowValueTypeFacts.IsNumeric(current) &&
                                FlowValueTypeFacts.IsNumeric(argumentType))
                            {
                                evidence.Types[i] = FlowValueType.Number;
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

            private readonly struct ChildVisitor : IAstChildVisitor
            {
                private readonly DirectCallCollector _owner;

                public ChildVisitor(DirectCallCollector owner)
                {
                    _owner = owner;
                }

                public void Visit(AstNode node)
                {
                    _owner.Visit(node);
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

            public void ResetTransientEvidence()
            {
                for (var i = 0; i < Types.Length; i++)
                {
                    if (!FlowValueTypeFacts.IsNativeDirectParameter(
                        new DirectParameterType(Types[i])))
                    {
                        Types[i] = FlowValueType.None;
                    }
                    SawNonNative[i] = false;
                }
            }
        }
    }
}
