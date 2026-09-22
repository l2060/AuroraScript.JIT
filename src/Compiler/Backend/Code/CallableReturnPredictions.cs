using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Code
{
    internal readonly record struct CallableReturnPrediction(FlowValueType Type,
        HostNativeObjectDescriptor NativeObject = null, TypeDeclaration StructuralType = null);

    /// <summary>
    /// Return summaries for dynamic callables, independent of native ABI eligibility.
    /// These facts describe the declared/initial callable, not an immutable binding.
    /// Consumers must guard actual values before using the resulting predictions.
    /// </summary>
    internal sealed class CallableReturnPredictions
    {
        private readonly TypedFunctionBuilder.FunctionBinding[] _bindings;
        private readonly Dictionary<ModulePlan, TypedFunctionBuilder.FunctionBinding> _initializers = new();
        private readonly Dictionary<ModulePlan, CapturedCellTypes> _capturedCells = new();
        private readonly Dictionary<ModulePlan, TypedModuleCode> _moduleCodes = new();
        private readonly Dictionary<ModuleDeclaration, ModulePlan> _modules = new(ReferenceEqualityComparer.Instance);
        private readonly TypedFunctionCode.PredictionFacts?[] _predictions;
        private readonly Dictionary<ModulePlan, TypedFunctionCode.PredictionFacts> _initializerPredictions = new();
        private readonly Dictionary<FunctionDeclaration, FunctionPlan> _declarations =
            new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<SymbolId, AstNode> _symbols = new();
        private readonly Dictionary<Expression, FunctionPlan> _resolvedCallables = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<AstNode> _resolveVisited = new(ReferenceEqualityComparer.Instance);
        private readonly FunctionPlan[] _functions;
        private readonly List<int>[] _dependents;
        private readonly bool[] _requiresPrediction;
        private readonly Dictionary<ModulePlan, bool> _initializerRequiresPrediction = new();
        private CallableReturnPrediction[] _returns;

        internal TypedFunctionBuilder.FunctionBinding[] Bindings => _bindings;
        internal TypedFunctionBuilder.FunctionBinding GetInitializerBinding(ModulePlan module) => _initializers[module];
        internal CapturedCellTypes GetCapturedCells(ModulePlan module) => _capturedCells[module];
        internal TypedModuleCode GetModuleCode(ModulePlan module) => _moduleCodes[module];

        private CallableReturnPredictions(IReadOnlyList<ModulePlan> modules)
        {
            var maxId = -1;
            foreach (var module in modules)
                foreach (var function in module.Functions) maxId = Math.Max(maxId, function.Id.Value);
            _bindings = new TypedFunctionBuilder.FunctionBinding[maxId + 1];
            _predictions = new TypedFunctionCode.PredictionFacts?[maxId + 1];
            _returns = new CallableReturnPrediction[maxId + 1];
            foreach (var module in modules)
            {
                _modules[module.Declaration] = module;
                _initializers[module] = TypedFunctionBuilder.BindModule(module, _bindings);
                _capturedCells[module] = new CapturedCellTypes(module, _bindings);
                foreach (var function in module.Functions)
                {
                    _declarations[function.Declaration] = function;
                    if (function.IsModuleFunction && module.TryGetSymbol(function.Name, out var symbol))
                        _symbols[symbol] = function.Declaration;
                }
                foreach (var statement in module.Declaration.Statements)
                    if (statement is VariableDeclaration variable && variable.Name != null &&
                        module.TryGetSymbol(variable.Name.Value, out var symbol))
                        _symbols[symbol] = variable;
            }

            _functions = new FunctionPlan[maxId + 1];
            _dependents = new List<int>[maxId + 1];
            _requiresPrediction = new bool[maxId + 1];
            for (var i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                for (var functionIndex = 0; functionIndex < module.Functions.Count; functionIndex++)
                {
                    var function = module.Functions[functionIndex];
                    _functions[function.Id.Value] = function;
                }
            }

            BuildDependencies(modules);
        }

        public static CallableReturnPredictions Build(IReadOnlyList<ModulePlan> modules, HostExportCatalog hostExports)
        {
            var analysis = new CallableReturnPredictions(modules);
            foreach (var module in modules)
                analysis._moduleCodes[module] = TypedModuleCode.Analyze(module, hostExports, analysis);
            var predictions = new TypedFunctionCode[analysis._predictions.Length];
            var upvalues = new Dictionary<FunctionId, FlowValueType[]>();
            var queue = new Queue<int>();
            var queued = new bool[analysis._functions.Length];
            var iterations = new byte[analysis._functions.Length];

            // Seed the lattice with the already computed generic code. Recursive
            // functions deliberately keep the bottom value until the worklist
            // analyzes them, preserving the old recursive inference behavior.
            for (var i = 0; i < analysis._functions.Length; i++)
            {
                var function = analysis._functions[i];
                if (function == null)
                {
                    continue;
                }

                var binding = analysis._bindings[i];
                var generic = analysis._moduleCodes[binding.Module].GetGeneric(function.Id);
                predictions[i] = generic;
                if (!analysis._requiresPrediction[i])
                {
                    analysis._returns[i] = analysis.CreateSummary(binding, generic, hostExports);
                }
                else
                {
                    analysis.Enqueue(i, queue, queued);
                }
            }

            var converged = analysis.DrainWorklist(
                hostExports,
                predictions,
                upvalues,
                queue,
                queued,
                iterations);

            while (converged)
            {
                foreach (var module in modules)
                {
                    analysis._capturedCells[module].Update(
                        predictions,
                        upvalues,
                        onChanged: functionId => analysis.Enqueue(functionId.Value, queue, queued));
                }

                if (queue.Count == 0)
                {
                    break;
                }

                converged = analysis.DrainWorklist(
                    hostExports,
                    predictions,
                    upvalues,
                    queue,
                    queued,
                    iterations);
            }

            // An unstable graph has no usable prediction. The original proven
            // analysis and all of its normal dynamic fallbacks remain intact.
            if (!converged)
            {
                return analysis;
            }
            for (var i = 0; i < analysis._functions.Length; i++)
            {
                var function = analysis._functions[i];
                if (function == null)
                {
                    continue;
                }

                analysis._predictions[i] = analysis._requiresPrediction[i]
                    ? predictions[i].GetPredictionFacts()
                    : new TypedFunctionCode.PredictionFacts(predictions[i].ReturnType, null, null);
            }
            foreach (var pair in analysis._initializers)
                if (analysis._initializerRequiresPrediction[pair.Key])
                    analysis._initializerPredictions[pair.Key] = TypedFunctionBuilder.Analyze(pair.Value, hostExports,
                        callableReturnPrediction: target => analysis.GetReturn(pair.Value, target)).GetPredictionFacts();
            foreach (var code in analysis._moduleCodes.Values) code.ApplyPredictions(analysis);
            return analysis;
        }

        private void BuildDependencies(IReadOnlyList<ModulePlan> modules)
        {
            for (var i = 0; i < _functions.Length; i++)
            {
                var function = _functions[i];
                if (function == null)
                {
                    continue;
                }

                var binding = _bindings[i];
                var requiresPrediction = binding.HasUpvalueReference;
                for (var callIndex = 0; callIndex < binding.Calls.Count; callIndex++)
                {
                    var call = binding.Calls[callIndex];
                    var target = ResolveCallable(binding, call.Target);
                    if (target == null || !NeedsCallablePrediction(binding, call.Target))
                    {
                        continue;
                    }

                    requiresPrediction = true;
                    var dependents = _dependents[target.Id.Value] ??= new List<int>();
                    if (dependents.Count == 0 || dependents[^1] != i)
                    {
                        dependents.Add(i);
                    }
                }

                _requiresPrediction[i] = requiresPrediction;
            }

            for (var i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                _initializerRequiresPrediction[module] = RequiresPrediction(_initializers[module]);
            }
        }

        private bool RequiresPrediction(TypedFunctionBuilder.FunctionBinding binding)
        {
            if (binding.HasUpvalueReference)
            {
                return true;
            }

            foreach (var call in binding.Calls)
            {
                if (ResolveCallable(binding, call.Target) != null &&
                    NeedsCallablePrediction(binding, call.Target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool NeedsCallablePrediction(
            TypedFunctionBuilder.FunctionBinding binding,
            Expression target)
        {
            // A direct name call is already typed by TypedModuleCode's direct and
            // universal return lattices. The separate callable graph is only
            // needed once the target has crossed a dynamic value boundary.
            return target is not NameExpression name ||
                !binding.Names.TryGetValue(name, out var value) ||
                !value.DirectFunction.IsValid;
        }

        private void Enqueue(int functionId, Queue<int> queue, bool[] queued)
        {
            if ((uint)functionId >= (uint)_requiresPrediction.Length ||
                !_requiresPrediction[functionId] || queued[functionId])
            {
                return;
            }

            queued[functionId] = true;
            queue.Enqueue(functionId);
        }

        private bool DrainWorklist(
            HostExportCatalog hostExports,
            TypedFunctionCode[] predictions,
            Dictionary<FunctionId, FlowValueType[]> upvalues,
            Queue<int> queue,
            bool[] queued,
            byte[] iterations)
        {
            while (queue.Count != 0)
            {
                var functionId = queue.Dequeue();
                queued[functionId] = false;
                if (++iterations[functionId] > 32)
                {
                    return false;
                }

                var binding = _bindings[functionId];
                var prediction = TypedFunctionBuilder.Analyze(
                    binding,
                    hostExports,
                    upvalueTypes: upvalues,
                    callableReturnPrediction: target => GetReturn(binding, target));
                predictions[functionId] = prediction;
                var summary = CreateSummary(binding, prediction, hostExports);
                if (_returns[functionId] == summary)
                {
                    continue;
                }

                _returns[functionId] = summary;
                var dependents = _dependents[functionId];
                if (dependents == null)
                {
                    continue;
                }

                for (var i = 0; i < dependents.Count; i++)
                {
                    Enqueue(dependents[i], queue, queued);
                }
            }

            return true;
        }

        private CallableReturnPrediction CreateSummary(
            TypedFunctionBuilder.FunctionBinding binding,
            TypedFunctionCode prediction,
            HostExportCatalog hostExports)
        {
            // Int32/UInt32 storage refinements are erased at the ordinary datum
            // ABI. Guessing integer arithmetic from them could change overflow
            // behavior; preserve the runtime Number kind instead.
            var returnType = prediction.ReturnType is FlowValueType.Int32 or FlowValueType.UInt32
                ? FlowValueType.Number : prediction.ReturnType;
            TypeReferenceFacts.TryGetNativeObject(hostExports, binding.Function.Declaration.ReturnType, out var native);
            native ??= GetNativeReturn(binding, prediction);
            TypeReferenceFacts.TryGetCustomType(binding.Module.Declaration, binding.Function.Declaration.ReturnType, out var structural);
            return new CallableReturnPrediction(returnType, native, structural);
        }

        public void Apply(ModulePlan module, TypedFunctionCode[] generic,
            TypedFunctionCode[] direct, TypedFunctionCode initializer)
        {
            foreach (var function in module.Functions)
            {
                if (_predictions[function.Id.Value] is not { } prediction) continue;
                prediction = prediction.KeepDifferences(generic[function.ModuleIndex], direct[function.ModuleIndex]);
                _predictions[function.Id.Value] = prediction;
                generic[function.ModuleIndex].Prediction = prediction;
                if (direct[function.ModuleIndex] != null)
                    direct[function.ModuleIndex].Prediction = prediction;
            }
            if (_initializerPredictions.TryGetValue(module, out var initializerPrediction))
            {
                initializerPrediction = initializerPrediction.KeepDifferences(initializer, null);
                _initializerPredictions[module] = initializerPrediction;
                initializer.Prediction = initializerPrediction;
            }
        }

        private CallableReturnPrediction? GetReturn(TypedFunctionBuilder.FunctionBinding binding, Expression target)
        {
            var function = ResolveCallable(binding, target);
            return function != null ? _returns[function.Id.Value] : null;
        }

        private FunctionPlan ResolveCallable(
            TypedFunctionBuilder.FunctionBinding binding,
            Expression target)
        {
            if (_resolvedCallables.TryGetValue(target, out var function))
            {
                return function;
            }

            _resolveVisited.Clear();
            function = Resolve(binding, target, _resolveVisited);
            _resolvedCallables[target] = function;
            return function;
        }

        private static HostNativeObjectDescriptor GetNativeReturn(
            TypedFunctionBuilder.FunctionBinding binding, TypedFunctionCode code)
        {
            HostNativeObjectDescriptor result = null;
            var sawValue = false;
            foreach (var returned in binding.Returns)
            {
                if (returned.Expression == null || code.GetExpressionType(returned.Expression) == FlowValueType.Null) continue;
                var native = code.GetNativeObjectType(returned.Expression);
                if (sawValue && !ReferenceEquals(result, native)) return null;
                result = native;
                sawValue = true;
            }
            return result;
        }

        private FunctionPlan Resolve(TypedFunctionBuilder.FunctionBinding binding, AstNode node,
            HashSet<AstNode> visited)
        {
            if (node == null || !visited.Add(node)) return null;
            switch (node)
            {
                case FunctionDeclaration function:
                    return _declarations.TryGetValue(function, out var plan) ? plan : null;
                case LambdaExpression lambda:
                    return Resolve(binding, lambda.Function, visited);
                case GroupExpression group when group.Expressions.Count > 0:
                    return Resolve(binding, group.Expressions[^1], visited);
                case VariableDeclaration variable:
                    return Resolve(binding, variable.Initializer, visited);
                case NameExpression name when binding.Names.TryGetValue(name, out var value):
                    if (value.IsLocal)
                        return Resolve(binding, binding.Function.LocalSlots[value.Local.Value].Declaration, visited);
                    if (value.Upvalue.IsValid)
                    {
                        var cell = binding.Function.UpvalueSlots[value.Upvalue.Value];
                        while ((uint)cell.SourceFunction.Value < (uint)_bindings.Length &&
                            _bindings[cell.SourceFunction.Value] is { } ownerBinding)
                        {
                            var owner = ownerBinding.Function;
                            if (!cell.IsInherited)
                                return Resolve(ownerBinding,
                                    owner.LocalSlots[cell.SourceLocal.Value].Declaration, visited);
                            cell = owner.UpvalueSlots[cell.SourceUpvalue.Value];
                        }
                    }
                    if (value.ModuleSymbol.IsValid && _symbols.TryGetValue(value.ModuleSymbol, out var declaration))
                        return Resolve(_initializers[binding.Module], declaration, visited);
                    return null;
                case GetPropertyExpression { Object: NameExpression ownerName, Property: NameExpression member }
                    when binding.Names.TryGetValue(ownerName, out var ownerBinding) &&
                        !ownerBinding.IsLocal && !ownerBinding.Upvalue.IsValid:
                    foreach (var import in binding.Module.Declaration.Imports)
                    {
                        if (import.Include || import.Name?.Value != ownerName.Identifier?.Value ||
                            import.Module == null || !_modules.TryGetValue(import.Module, out var imported)) continue;
                        foreach (var function in imported.Functions)
                            if (function.IsModuleFunction && function.Visibility == FunctionVisibility.Exported &&
                                function.Name == member.Identifier?.Value) return function;
                    }
                    return null;
                default:
                    return null;
            }
        }
    }
}
