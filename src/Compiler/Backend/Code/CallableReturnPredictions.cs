using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
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
        private readonly Dictionary<ModuleDeclaration, ModulePlan> _modules = new(ReferenceEqualityComparer.Instance);
        private readonly TypedFunctionCode[] _predictions;
        private readonly Dictionary<ModulePlan, TypedFunctionCode> _initializerPredictions = new();
        private readonly Dictionary<FunctionDeclaration, FunctionPlan> _declarations =
            new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<SymbolId, AstNode> _symbols = new();
        private readonly Dictionary<Expression, FunctionPlan> _resolvedCallables = new(ReferenceEqualityComparer.Instance);
        private CallableReturnPrediction[] _returns;

        private CallableReturnPredictions(IReadOnlyList<ModulePlan> modules)
        {
            var maxId = -1;
            foreach (var module in modules)
                foreach (var function in module.Functions) maxId = Math.Max(maxId, function.Id.Value);
            _bindings = new TypedFunctionBuilder.FunctionBinding[maxId + 1];
            _predictions = new TypedFunctionCode[maxId + 1];
            _returns = new CallableReturnPrediction[maxId + 1];
            foreach (var module in modules)
            {
                _modules[module.Declaration] = module;
                var bindings = TypedFunctionBuilder.BindModule(module);
                _initializers[module] = TypedFunctionBuilder.Bind(module, module.InitializerFunction);
                foreach (var function in module.Functions)
                {
                    _bindings[function.Id.Value] = bindings[function.Id.Value];
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
            var converged = false;
            var upvalues = new Dictionary<FunctionId, FlowValueType[]>();
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
                    var prediction = TypedFunctionBuilder.Analyze(binding, hostExports,
                        upvalueTypes: upvalues,
                        callableReturnPrediction: target => analysis.GetReturn(binding, target));
                    analysis._predictions[function.Id.Value] = prediction;
                    // Int32/UInt32 storage refinements are erased at the ordinary
                    // datum ABI. Guessing integer arithmetic from them could change
                    // overflow behavior; preserve the runtime Number kind instead.
                    var returnType = prediction.ReturnType is FlowValueType.Int32 or FlowValueType.UInt32
                        ? FlowValueType.Number : prediction.ReturnType;
                    TypeReferenceFacts.TryGetNativeObject(hostExports, function.Declaration.ReturnType, out var native);
                    native ??= NativeReturnSummary.Analyze(prediction);
                    TypeReferenceFacts.TryGetCustomType(binding.Module.Declaration, function.Declaration.ReturnType, out var structural);
                    var summary = new CallableReturnPrediction(returnType, native, structural);
                    next[function.Id.Value] = summary;
                    changed |= analysis._returns[function.Id.Value] != summary;
                }
                analysis._returns = next;
                var nextUpvalues = new Dictionary<FunctionId, FlowValueType[]>();
                foreach (var module in modules)
                    foreach (var pair in CapturedCellTypes.Analyze(module, analysis._predictions))
                        nextUpvalues[pair.Key] = pair.Value;
                changed |= !CapturedCellTypes.SameTypes(upvalues, nextUpvalues);
                upvalues = nextUpvalues;
                if (!changed) { converged = true; break; }
            }
            // An unstable graph has no usable prediction. The original proven
            // analysis and all of its normal dynamic fallbacks remain intact.
            if (!converged)
            {
                Array.Clear(analysis._predictions);
                return analysis;
            }
            foreach (var pair in analysis._initializers)
                analysis._initializerPredictions[pair.Key] = TypedFunctionBuilder.Analyze(pair.Value, hostExports,
                    callableReturnPrediction: target => analysis.GetReturn(pair.Value, target));
            return analysis;
        }

        public void Apply(ModulePlan module, TypedFunctionCode[] generic,
            TypedFunctionCode[] direct, TypedFunctionCode initializer)
        {
            foreach (var function in module.Functions)
            {
                var prediction = _predictions[function.Id.Value];
                if (prediction == null) continue;
                generic[function.Id.Value].Prediction = prediction;
                if (direct[function.Id.Value] != null)
                    direct[function.Id.Value].Prediction = prediction;
            }
            if (_initializerPredictions.TryGetValue(module, out var initializerPrediction))
                initializer.Prediction = initializerPrediction;
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

        private sealed class NativeReturnSummary
        {
            private readonly TypedFunctionCode _code;
            private HostNativeObjectDescriptor _native;
            private bool _sawValue;
            private bool _mixed;

            private NativeReturnSummary(TypedFunctionCode code) { _code = code; }

            public static HostNativeObjectDescriptor Analyze(TypedFunctionCode code)
            {
                var scanner = new NativeReturnSummary(code);
                scanner.Visit(code.Function.Declaration.Body);
                return scanner._mixed ? null : scanner._native;
            }

            public void Visit(AstNode node)
            {
                if (node == null || node is FunctionDeclaration or LambdaExpression) return;
                if (node is ReturnStatement returned)
                {
                    if (returned.Expression == null || _code.GetExpressionType(returned.Expression) == FlowValueType.Null) return;
                    var native = _code.GetNativeObjectType(returned.Expression);
                    if (_sawValue && !ReferenceEquals(_native, native)) _mixed = true;
                    _native = native;
                    _sawValue = true;
                    return;
                }
                var visitor = new ChildVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private readonly struct ChildVisitor(NativeReturnSummary owner) : IAstChildVisitor
            {
                public void Visit(AstNode node) => owner.Visit(node);
            }
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
