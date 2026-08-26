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
            return Emit(Prepare(module));
        }

        internal ModuleEmissionState Prepare(ModulePlan module)
        {
            ArgumentNullException.ThrowIfNull(module);
            return CreateState(module);
        }

        internal ModuleEmissionResult Emit(ModuleEmissionState state)
        {
            var module = state.Module;
            var functions = new FunctionEmissionResult[module.Functions.Count];
            for (var i = 0; i < module.Functions.Count; i++)
            {
                _session.CompileSession.CancellationToken.ThrowIfCancellationRequested();
                functions[i] = state.FunctionEmitter.Emit(module.Functions[i]);
            }

            var initializer = default(System.Reflection.MethodInfo);
            if (state.InitializerEmitter != null)
            {
                state.InitializerEmitter.TryEmit(out initializer);
            }

            return new ModuleEmissionResult(module.Id, module.Name, functions, initializer);
        }

        public void EmitWithoutReport(ModulePlan module)
        {
            ArgumentNullException.ThrowIfNull(module);
            EmitWithoutReport(Prepare(module));
        }

        internal void EmitWithoutReport(ModuleEmissionState state)
        {
            var module = state.Module;
            for (var i = 0; i < module.Functions.Count; i++)
            {
                _session.CompileSession.CancellationToken.ThrowIfCancellationRequested();
                state.FunctionEmitter.EmitWithoutResult(module.Functions[i]);
            }

            state.InitializerEmitter?.TryEmit(out _);
        }

        private ModuleEmissionState CreateState(ModulePlan module)
        {
            TypedCilEmitter typed = null;
            ModuleInitializerEmitter initializerEmitter = null;
            if (_session.EmitExecutableCode)
            {
                initializerEmitter = new ModuleInitializerEmitter(_session, module);
                initializerEmitter.Define();
                typed = new TypedCilEmitter(_session, module);
                typed.Prepare();
            }

            var functionEmitter = new FunctionEmitter(_session, module, typed);
            return new ModuleEmissionState(
                module,
                functionEmitter,
                initializerEmitter);
        }

        internal readonly struct ModuleEmissionState
        {
            public ModuleEmissionState(
                ModulePlan module,
                FunctionEmitter functionEmitter,
                ModuleInitializerEmitter initializerEmitter)
            {
                Module = module;
                FunctionEmitter = functionEmitter;
                InitializerEmitter = initializerEmitter;
            }

            public ModulePlan Module { get; }
            public FunctionEmitter FunctionEmitter { get; }
            public ModuleInitializerEmitter InitializerEmitter { get; }
        }
    }
}
