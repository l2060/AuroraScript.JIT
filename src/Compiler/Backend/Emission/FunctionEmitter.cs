using AuroraScript.Compiler.Backend.Plans;
using System;

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

        public FunctionEmitter(EmissionSession session, ModulePlan module, ExecutableSkeletonEmitter skeleton)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _locals = new LocalEmitter();
            _expressions = new ExpressionEmitter(_locals);
            _controlFlow = new ControlFlowEmitter(_locals, _expressions);
            _skeleton = skeleton;
        }

        public FunctionEmissionResult Emit(FunctionPlan function)
        {
            ArgumentNullException.ThrowIfNull(function);

            var context = new FunctionEmissionContext(_session.CompileSession, _module, function);
            _controlFlow.Emit(context, function.Body);
            if (_skeleton != null && _skeleton.TryEmit(function, out var method, out var localCount))
            {
                context.SetExecutableSkeleton(method, localCount);
            }
            return context.ToResult();
        }
    }
}
