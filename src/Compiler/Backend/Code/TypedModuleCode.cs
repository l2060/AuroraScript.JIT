using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
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
        private readonly FlowValueType[][] _directParameters;

        private TypedModuleCode(
            TypedFunctionCode[] generic,
            TypedFunctionCode[] direct,
            FlowValueType[][] directParameters)
        {
            _generic = generic;
            _direct = direct;
            _directParameters = directParameters;
        }

        public static TypedModuleCode Build(ModulePlan module)
        {
            ArgumentNullException.ThrowIfNull(module);
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
            var directParameters = new FlowValueType[size][];
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
                    directReturnTypes: returns,
                    directParameterTypes: directParameters);
            }

            var converged = false;
            var passLimit = Math.Min(64, Math.Max(6, module.Functions.Count + 2));
            var evidence = new Dictionary<FunctionId, ParameterEvidence>();
            for (var pass = 0; pass < passLimit; pass++)
            {
                CollectParameterEvidence(module, functions, generic, direct, evidence);
                var nextReturns = new Dictionary<FunctionId, FlowValueType>(returns.Count);
                var changed = false;

                for (var i = 0; i < module.Functions.Count; i++)
                {
                    var function = module.Functions[i];
                    var parameterTypes = NormalizeParameterTypes(function, evidence);
                    var oldParameterTypes = directParameters[function.Id.Value];
                    directParameters[function.Id.Value] = parameterTypes;
                    var code = TypedFunctionBuilder.Build(
                        module,
                        function,
                        parameterTypes,
                        returns,
                        directParameters);
                    var validatedParameterTypes = ValidateParameterTypes(function, code, parameterTypes);
                    if (!SameTypes(parameterTypes, validatedParameterTypes))
                    {
                        parameterTypes = validatedParameterTypes;
                        directParameters[function.Id.Value] = parameterTypes;
                        code = TypedFunctionBuilder.Build(
                            module,
                            function,
                            parameterTypes,
                            returns,
                            directParameters);
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
                for (var i = 0; i < module.Functions.Count; i++)
                {
                    var function = module.Functions[i];
                    generic[function.Id.Value] = TypedFunctionBuilder.Build(
                        module,
                        function,
                        directReturnTypes: returns,
                        directParameterTypes: directParameters);
                }

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
                        directParameters[function.Id.Value],
                        conservativeReturns,
                        directParameters);
                    generic[function.Id.Value] = TypedFunctionBuilder.Build(
                        module,
                        function,
                        directReturnTypes: conservativeReturns,
                        directParameterTypes: directParameters);
                }
            }

            return new TypedModuleCode(generic, direct, directParameters);
        }

        private static bool SameTypes(FlowValueType[] left, FlowValueType[] right)
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

        public FlowValueType[] GetDirectParameters(FunctionId function)
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
                var code = direct[function.Id.Value] ?? generic[function.Id.Value];
                if (code == null || function.Declaration?.Body == null) continue;
                var collector = new DirectCallCollector(code, functions, evidence);
                collector.Visit(function.Declaration.Body);
            }
        }

        private static FlowValueType[] NormalizeParameterTypes(
            FunctionPlan function,
            IReadOnlyDictionary<FunctionId, ParameterEvidence> evidence)
        {
            var parameterCount = 0;
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                if (function.LocalSlots[i].IsParameter) parameterCount++;
            }
            if (parameterCount == 0) return Array.Empty<FlowValueType>();

            evidence.TryGetValue(function.Id, out var observed);
            var result = new FlowValueType[parameterCount];
            var parameterIndex = 0;
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                if (!function.LocalSlots[i].IsParameter) continue;
                var type = observed != null && parameterIndex < observed.Types.Length
                    ? observed.Types[parameterIndex]
                    : FlowValueType.None;
                result[parameterIndex] = FlowValueTypeFacts.IsNativeDirectParameter(type)
                    ? type
                    : FlowValueType.Dynamic;
                parameterIndex++;
            }
            return result;
        }

        private static FlowValueType[] ValidateParameterTypes(
            FunctionPlan function,
            TypedFunctionCode code,
            FlowValueType[] parameterTypes)
        {
            if (parameterTypes == null || parameterTypes.Length == 0 || code == null)
            {
                return parameterTypes ?? Array.Empty<FlowValueType>();
            }

            FlowValueType[] result = null;
            var parameterIndex = 0;
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                if (!function.LocalSlots[i].IsParameter) continue;
                var parameterType = parameterTypes[parameterIndex];
                if (FlowValueTypeFacts.IsNativeDirectParameter(parameterType) &&
                    code.LocalTypes[i] != parameterType)
                {
                    result ??= (FlowValueType[])parameterTypes.Clone();
                    result[parameterIndex] = FlowValueType.Dynamic;
                }
                parameterIndex++;
            }
            return result ?? parameterTypes;
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
                    if (FlowValueTypeFacts.IsNativeDirectParameter(argumentType))
                    {
                        if (evidence.NativeConflict[i]) continue;
                        var current = evidence.Types[i];
                        if (FlowValueTypeFacts.IsNativeDirectParameter(current) &&
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
                    else if (!FlowValueTypeFacts.IsNativeDirectParameter(evidence.Types[i]) &&
                        !evidence.NativeConflict[i])
                    {
                        evidence.Types[i] |= argumentType;
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
            }

            public FlowValueType[] Types { get; }
            public bool[] NativeConflict { get; }
        }
    }
}
