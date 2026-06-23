using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Emits.Builders;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class EmissionSession
    {
        private Dictionary<DynamicDelegateKey, int> _dynamicDelegateIds;
        private List<DynamicDelegateKey> _pendingDynamicDelegates;

        public EmissionSession(
            CompileSession compileSession,
            AbstractCILBuilder builder,
            bool emitExecutableSkeletons = false,
            bool forceModuleDefinitions = false,
            bool collectDiagnostics = false)
        {
            CompileSession = compileSession ?? throw new ArgumentNullException(nameof(compileSession));
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            EmitExecutableSkeletons = emitExecutableSkeletons;
            ForceModuleDefinitions = forceModuleDefinitions;
            CollectDiagnostics = collectDiagnostics;
        }

        public CompileSession CompileSession { get; }
        public AbstractCILBuilder Builder { get; }
        public bool EmitExecutableSkeletons { get; }
        public bool ForceModuleDefinitions { get; }
        public bool CollectDiagnostics { get; }
        public EngineOptions Options => CompileSession.Options;

        public EmissionReport Emit()
        {
            var modules = CompileSession.Modules ?? Array.Empty<ModulePlan>();
            var results = new ModuleEmissionResult[modules.Length];
            var moduleEmitter = new ModuleEmitter(this);
            for (var i = 0; i < modules.Length; i++)
            {
                CompileSession.CancellationToken.ThrowIfCancellationRequested();
                results[i] = moduleEmitter.Emit(modules[i]);
            }

            CompleteDynamicDelegates();
            return new EmissionReport(results);
        }

        public void EmitAll()
        {
            var modules = CompileSession.Modules ?? Array.Empty<ModulePlan>();
            var moduleEmitter = new ModuleEmitter(this);
            for (var i = 0; i < modules.Length; i++)
            {
                CompileSession.CancellationToken.ThrowIfCancellationRequested();
                moduleEmitter.EmitWithoutReport(modules[i]);
            }

            CompleteDynamicDelegates();
        }

        internal int GetDynamicDelegateId(DynamicMethod method, FunctionCallConvention convention)
        {
            var key = new DynamicDelegateKey(method, convention);
            if (_dynamicDelegateIds != null && _dynamicDelegateIds.TryGetValue(key, out var id))
            {
                return id;
            }

            id = DynamicMethodRegistry.Reserve();
            (_dynamicDelegateIds ??= new Dictionary<DynamicDelegateKey, int>()).Add(key, id);
            (_pendingDynamicDelegates ??= new List<DynamicDelegateKey>()).Add(key);
            return id;
        }

        internal void CompleteDynamicDelegates()
        {
            if (_pendingDynamicDelegates == null)
            {
                return;
            }

            for (var i = 0; i < _pendingDynamicDelegates.Count; i++)
            {
                var key = _pendingDynamicDelegates[i];
                ClosureMaterializer.RegisterDynamicDelegate(
                    _dynamicDelegateIds[key],
                    key.Method,
                    key.Convention);
            }

            _pendingDynamicDelegates.Clear();
        }

        private readonly struct DynamicDelegateKey : IEquatable<DynamicDelegateKey>
        {
            public DynamicDelegateKey(DynamicMethod method, FunctionCallConvention convention)
            {
                Method = method;
                Convention = convention;
            }

            public DynamicMethod Method { get; }
            public FunctionCallConvention Convention { get; }

            public bool Equals(DynamicDelegateKey other)
            {
                return ReferenceEquals(Method, other.Method) && Convention == other.Convention;
            }

            public override bool Equals(object obj)
            {
                return obj is DynamicDelegateKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Method, Convention);
            }
        }
    }
}
