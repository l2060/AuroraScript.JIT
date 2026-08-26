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
        private readonly FunctionReportCollector _reportCollector;
        private readonly TypedCilEmitter _typed;
        private readonly HashSet<SymbolId> _directCallCandidateSymbols;

        public FunctionEmitter(
            EmissionSession session,
            ModulePlan module,
            TypedCilEmitter typed)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _typed = typed;
            if (session.CollectDiagnostics)
            {
                _reportCollector = new FunctionReportCollector(
                    module,
                    session.CompileSession.HostExports);
            }

            _directCallCandidateSymbols = session.CollectDiagnostics
                ? BuildDirectCallCandidateSymbols(session, module)
                : null;
        }

        public FunctionEmissionResult Emit(FunctionPlan function)
        {
            ArgumentNullException.ThrowIfNull(function);

            var context = new FunctionEmissionContext(_session.CompileSession, _module, function, _directCallCandidateSymbols);
            if (_session.CollectDiagnostics)
            {
                _reportCollector.Collect(context);
            }

            if (_typed != null && _typed.TryEmit(function, out var typedMethod, out var typedLocalCount))
            {
                context.SetExecutableCode(typedMethod, typedLocalCount);
            }
            else if (_typed != null)
            {
                throw new NotSupportedException($"Typed CIL emission does not support function '{function.Name ?? "<anonymous>"}'.");
            }
            return context.ToResult();
        }

        public void EmitWithoutResult(FunctionPlan function)
        {
            ArgumentNullException.ThrowIfNull(function);

            if (_session.CollectDiagnostics)
            {
                var context = new FunctionEmissionContext(_session.CompileSession, _module, function, _directCallCandidateSymbols);
                _reportCollector.Collect(context);
                if (_typed != null && _typed.TryEmit(function, out var typedMethod, out var typedLocalCount))
                {
                    context.SetExecutableCode(typedMethod, typedLocalCount);
                }
                else if (_typed != null)
                {
                    throw new NotSupportedException($"Typed CIL emission does not support function '{function.Name ?? "<anonymous>"}'.");
                }
                return;
            }

            if (_typed != null && _typed.TryEmit(function, out var emittedTypedMethod, out _))
            {
                function.Method = emittedTypedMethod;
            }
            else if (_typed != null)
            {
                throw new NotSupportedException($"Typed CIL emission does not support function '{function.Name ?? "<anonymous>"}'.");
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
