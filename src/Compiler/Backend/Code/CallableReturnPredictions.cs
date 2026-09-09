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
        }

        public static CallableReturnPredictions Build(IReadOnlyList<ModulePlan> modules, HostExportCatalog hostExports)
        {
            var analysis = new CallableReturnPredictions(modules);
            foreach (var module in modules)
                analysis._moduleCodes[module] = TypedModuleCode.Analyze(module, hostExports, analysis);
            var predictions = new TypedFunctionCode[analysis._predictions.Length];
            var converged = false;
            var upvalues = new Dictionary<FunctionId, FlowValueType[]>();
            CallableReturnPrediction[] previousReturns = null;
            Dictionary<FunctionId, FlowValueType[]> previousUpvalues = null;
            // Starting at bottom allows mutually recursive summaries to acquire
            // their base-case return types. These are hints, never ABI evidence.
            var passLimit = Math.Max(6, analysis._declarations.Count * 2 + 2);
            for (var pass = 0; pass < passLimit; pass++)
            {
                var next = new CallableReturnPrediction[analysis._returns.Length];
                var changed = false;
                foreach (var function in analysis._declarations.Values)
                {
                    var binding = analysis._bindings[function.Id.Value];
                    if (previousReturns != null &&
                        !analysis.InputsChanged(binding, previousReturns, previousUpvalues, upvalues))
                    {
                        next[function.Id.Value] = analysis._returns[function.Id.Value];
                        continue;
                    }
                    var prediction = analysis.CanReuseGeneric(binding)
                        ? analysis._moduleCodes[binding.Module].GetGeneric(function.Id)
                        : TypedFunctionBuilder.Analyze(binding, hostExports,
                            upvalueTypes: upvalues,
                            callableReturnPrediction: target => analysis.GetReturn(binding, target));
                    predictions[function.Id.Value] = prediction;
                    // Int32/UInt32 storage refinements are erased at the ordinary
                    // datum ABI. Guessing integer arithmetic from them could change
                    // overflow behavior; preserve the runtime Number kind instead.
                    var returnType = prediction.ReturnType is FlowValueType.Int32 or FlowValueType.UInt32
                        ? FlowValueType.Number : prediction.ReturnType;
                    TypeReferenceFacts.TryGetNativeObject(hostExports, function.Declaration.ReturnType, out var native);
                    native ??= GetNativeReturn(binding, prediction);
                    TypeReferenceFacts.TryGetCustomType(binding.Module.Declaration, function.Declaration.ReturnType, out var structural);
                    var summary = new CallableReturnPrediction(returnType, native, structural);
                    next[function.Id.Value] = summary;
                    changed |= analysis._returns[function.Id.Value] != summary;
                }
                previousReturns = analysis._returns;
                analysis._returns = next;
                var nextUpvalues = new Dictionary<FunctionId, FlowValueType[]>();
                foreach (var module in modules)
                    foreach (var pair in analysis._capturedCells[module].Analyze(predictions))
                        nextUpvalues[pair.Key] = pair.Value;
                changed |= !CapturedCellTypes.SameTypes(upvalues, nextUpvalues);
                previousUpvalues = upvalues;
                upvalues = nextUpvalues;
                if (!changed) { converged = true; break; }
            }
            // An unstable graph has no usable prediction. The original proven
            // analysis and all of its normal dynamic fallbacks remain intact.
            if (!converged)
            {
                return analysis;
            }
            foreach (var function in analysis._declarations.Values)
                analysis._predictions[function.Id.Value] = analysis.CanReuseGeneric(analysis._bindings[function.Id.Value])
                    ? new TypedFunctionCode.PredictionFacts(predictions[function.Id.Value].ReturnType, null, null)
                    : predictions[function.Id.Value].GetPredictionFacts();
            foreach (var pair in analysis._initializers)
                if (!analysis.CanReuseGeneric(pair.Value))
                    analysis._initializerPredictions[pair.Key] = TypedFunctionBuilder.Analyze(pair.Value, hostExports,
                        callableReturnPrediction: target => analysis.GetReturn(pair.Value, target)).GetPredictionFacts();
            foreach (var code in analysis._moduleCodes.Values) code.ApplyPredictions(analysis);
            return analysis;
        }

        private bool CanReuseGeneric(TypedFunctionBuilder.FunctionBinding binding)
        {
            if (binding.HasUpvalueReference || binding.HasDirectFunctionReference) return false;
            foreach (var call in binding.Calls)
                if (GetReturn(binding, call.Target) != null) return false;
            return true;
        }

        private bool InputsChanged(TypedFunctionBuilder.FunctionBinding binding,
            CallableReturnPrediction[] previousReturns,
            Dictionary<FunctionId, FlowValueType[]> previousUpvalues,
            Dictionary<FunctionId, FlowValueType[]> upvalues)
        {
            previousUpvalues.TryGetValue(binding.Function.Id, out var previousCells);
            upvalues.TryGetValue(binding.Function.Id, out var cells);
            if (!CapturedCellTypes.SameTypes(previousCells, cells)) return true;
            foreach (var call in binding.Calls)
                if (_resolvedCallables.TryGetValue(call.Target, out var target) && target != null &&
                    previousReturns[target.Id.Value] != _returns[target.Id.Value]) return true;
            return false;
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
            if (!_resolvedCallables.TryGetValue(target, out var function))
            {
                function = Resolve(binding, target, new HashSet<AstNode>(ReferenceEqualityComparer.Instance));
                _resolvedCallables[target] = function;
            }
            return function != null ? _returns[function.Id.Value] : null;
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
