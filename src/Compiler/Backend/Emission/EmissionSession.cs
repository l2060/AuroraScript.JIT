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
        private readonly Dictionary<DynamicDelegateKey, int> _dynamicDelegateIds = new();
        private readonly List<DynamicDelegateKey> _pendingDynamicDelegates = new();

        public EmissionSession(
            CompileSession compileSession,
            AbstractCILBuilder builder,
            bool emitExecutableSkeletons = false,
            bool forceModuleDefinitions = false)
        {
            CompileSession = compileSession ?? throw new ArgumentNullException(nameof(compileSession));
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            EmitExecutableSkeletons = emitExecutableSkeletons;
            ForceModuleDefinitions = forceModuleDefinitions;
        }

        public CompileSession CompileSession { get; }
        public AbstractCILBuilder Builder { get; }
        public bool EmitExecutableSkeletons { get; }
        public bool ForceModuleDefinitions { get; }
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

        internal int GetDynamicDelegateId(DynamicMethod method, FunctionCallConvention convention)
        {
            var key = new DynamicDelegateKey(method, convention);
            if (_dynamicDelegateIds.TryGetValue(key, out var id))
            {
                return id;
            }

            id = DynamicMethodRegistry.Reserve();
            _dynamicDelegateIds.Add(key, id);
            _pendingDynamicDelegates.Add(key);
            return id;
        }

        private void CompleteDynamicDelegates()
        {
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
