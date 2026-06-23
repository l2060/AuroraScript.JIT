using AuroraScript.Compiler.Backend.Plans;
using System;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class ModuleEmitter
    {
        private readonly EmissionSession _session;

        public ModuleEmitter(EmissionSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public ModuleEmissionResult Emit(ModulePlan module)
        {
            ArgumentNullException.ThrowIfNull(module);

            ExecutableSkeletonEmitter skeleton = null;
            ModuleInitializerEmitter initializerEmitter = null;
            if (_session.EmitExecutableSkeletons)
            {
                initializerEmitter = new ModuleInitializerEmitter(_session, module);
                initializerEmitter.Define();
                skeleton = new ExecutableSkeletonEmitter(_session, module);
                skeleton.Prepare();
            }

            var functionEmitter = new FunctionEmitter(_session, module, skeleton);
            var functions = new FunctionEmissionResult[module.Functions.Count];
            for (var i = 0; i < module.Functions.Count; i++)
            {
                _session.CompileSession.CancellationToken.ThrowIfCancellationRequested();
                functions[i] = functionEmitter.Emit(module.Functions[i]);
            }

            var initializer = default(System.Reflection.MethodInfo);
            if (initializerEmitter != null)
            {
                initializerEmitter.TryEmit(out initializer);
            }

            return new ModuleEmissionResult(module.Id, module.Name, functions, initializer);
        }
    }
}
