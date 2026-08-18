using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Reflection;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class CompileBlockEmitter
    {
        private readonly EmissionSession _session;
        private readonly CompileBlockPlan _plan;

        public CompileBlockEmitter(EmissionSession session, CompileBlockPlan plan)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        }

        public MethodInfo Emit()
        {
            var module = _plan.Module ?? throw new InvalidOperationException("CompileBlock module plan is missing.");
            var function = _plan.Function ?? throw new InvalidOperationException("CompileBlock function plan is missing.");
            var typed = new TypedCilEmitter(_session, module);
            typed.Prepare(forceAllExecutable: true);
            MethodInfo entryMethod = null;
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var current = module.Functions[i];
                var emitted = typed.TryEmit(current, out var method, out _);
                if (emitted &&
                    current.Id.Equals(function.Id))
                {
                    entryMethod = method;
                }
            }

            if (entryMethod == null)
            {
                throw new AuroraException("The compiler did not produce a compiled block entry point.");
            }

            _session.CompleteDynamicDelegates();
            return entryMethod;
        }
    }
}
