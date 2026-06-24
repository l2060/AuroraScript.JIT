using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class FunctionEmitter
    {
        private readonly EmissionSession _session;
        private readonly ModulePlan _module;
        private readonly LocalEmitter _locals;
        private readonly ExpressionEmitter _expressions;
        private readonly ControlFlowEmitter _controlFlow;
        private readonly ExecutableSkeletonEmitter _skeleton;
        private readonly HashSet<SymbolId> _directCallCandidateSymbols;

        public FunctionEmitter(EmissionSession session, ModulePlan module, ExecutableSkeletonEmitter skeleton)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _skeleton = skeleton;
            if (session.CollectDiagnostics || skeleton == null)
            {
                _locals = new LocalEmitter();
                _expressions = new ExpressionEmitter(_locals);
                _controlFlow = new ControlFlowEmitter(_locals, _expressions);
            }

            _directCallCandidateSymbols = session.CollectDiagnostics
                ? BuildDirectCallCandidateSymbols(session, module)
                : null;
        }

        public FunctionEmissionResult Emit(FunctionPlan function)
        {
            ArgumentNullException.ThrowIfNull(function);

            var context = new FunctionEmissionContext(_session.CompileSession, _module, function, _directCallCandidateSymbols);
            if (_session.CollectDiagnostics || _skeleton == null)
            {
                _controlFlow.Emit(context, function.Body);
            }

            if (_skeleton != null && _skeleton.TryEmit(function, out var method, out var localCount))
            {
                context.SetExecutableSkeleton(method, localCount);
            }
            return context.ToResult();
        }

        public void EmitWithoutResult(FunctionPlan function)
        {
            ArgumentNullException.ThrowIfNull(function);

            if (_session.CollectDiagnostics || _skeleton == null)
            {
                var context = new FunctionEmissionContext(_session.CompileSession, _module, function, _directCallCandidateSymbols);
                _controlFlow.Emit(context, function.Body);
                if (_skeleton != null && _skeleton.TryEmit(function, out var diagnosticMethod, out var diagnosticLocalCount))
                {
                    context.SetExecutableSkeleton(diagnosticMethod, diagnosticLocalCount);
                }
                return;
            }

            if (_skeleton != null && _skeleton.TryEmit(function, out var method, out _))
            {
                function.Method = method;
            }
        }

        private static HashSet<SymbolId> BuildDirectCallCandidateSymbols(EmissionSession session, ModulePlan module)
        {
            HashSet<SymbolId> result = null;
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                if (!function.IsDirectCallCandidate ||
                    string.IsNullOrEmpty(function.Name) ||
                    !module.TryGetSymbol(function.Name, out var symbolId))
                {
                    continue;
                }

                var symbol = session.CompileSession.Symbols[symbolId];
                if (symbol.Kind != BackendSymbolKind.Function ||
                    !ReferenceEquals(symbol.Declaration, function.Declaration))
                {
                    continue;
                }

                result ??= new HashSet<SymbolId>();
                result.Add(symbolId);
            }

            return result;
        }
    }
}
