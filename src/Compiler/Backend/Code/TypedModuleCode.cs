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
            for (var pass = 0; pass < passLimit; pass++)
            {
                var evidence = CollectParameterEvidence(module, functions, generic, direct);
                var nextReturns = new Dictionary<FunctionId, FlowValueType>(returns.Count);
                var changed = false;

                for (var i = 0; i < module.Functions.Count; i++)
                {
                    var function = module.Functions[i];
                    var parameterTypes = NormalizeParameterTypes(function, generic[function.Id.Value], evidence);
                    var oldParameterTypes = directParameters[function.Id.Value];
                    directParameters[function.Id.Value] = parameterTypes;
                    var code = TypedFunctionBuilder.Build(
                        module,
                        function,
                        parameterTypes,
                        returns,
                        directParameters);
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

        private static Dictionary<FunctionId, FlowValueType[]> CollectParameterEvidence(
            ModulePlan module,
            IReadOnlyDictionary<FunctionId, FunctionPlan> functions,
            TypedFunctionCode[] generic,
            TypedFunctionCode[] direct)
        {
            var evidence = new Dictionary<FunctionId, FlowValueType[]>();
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                var code = direct[function.Id.Value] ?? generic[function.Id.Value];
                if (code == null || function.Declaration?.Body == null) continue;
                var collector = new DirectCallCollector(code, functions, evidence);
                collector.Visit(function.Declaration.Body);
            }
            return evidence;
        }

        private static FlowValueType[] NormalizeParameterTypes(
            FunctionPlan function,
            TypedFunctionCode genericCode,
            IReadOnlyDictionary<FunctionId, FlowValueType[]> evidence)
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
                var type = observed != null && parameterIndex < observed.Length
                    ? observed[parameterIndex]
                    : FlowValueType.None;
                result[parameterIndex] = type == FlowValueType.Number &&
                    genericCode != null &&
                    !genericCode.WrittenLocals[i]
                        ? FlowValueType.Number
                        : FlowValueType.Dynamic;
                parameterIndex++;
            }
            return result;
        }

        private sealed class DirectCallCollector
        {
            private readonly TypedFunctionCode _code;
            private readonly IReadOnlyDictionary<FunctionId, FunctionPlan> _functions;
            private readonly Dictionary<FunctionId, FlowValueType[]> _evidence;

            public DirectCallCollector(
                TypedFunctionCode code,
                IReadOnlyDictionary<FunctionId, FunctionPlan> functions,
                Dictionary<FunctionId, FlowValueType[]> evidence)
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
                if (!_evidence.TryGetValue(target.Id, out var types))
                {
                    types = new FlowValueType[target.Declaration.Parameters.Count];
                    _evidence[target.Id] = types;
                }
                for (var i = 0; i < types.Length; i++)
                {
                    var argumentType = i < call.Arguments.Count
                        ? _code.GetExpressionType(call.Arguments[i])
                        : FlowValueType.Null;
                    // A native numeric method is a specialization, not the function's
                    // only entry point. Keep exact numeric evidence even when another
                    // call site is dynamic or passes a different kind: compatible calls
                    // use the double ABI and all other calls retain the generic adapter.
                    // This also lets numeric evidence enter a recursive call graph instead
                    // of being widened away by the graph's initial dynamic pass.
                    if (argumentType == FlowValueType.Number)
                    {
                        types[i] = FlowValueType.Number;
                    }
                    else if (types[i] != FlowValueType.Number)
                    {
                        types[i] |= argumentType;
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
    }
}
