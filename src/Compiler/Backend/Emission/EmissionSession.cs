using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using System;
using System.Buffers;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class EmissionSession
    {
        private readonly int _dynamicDelegateCapacity;
        private PendingDynamicDelegate[] _pendingDynamicDelegates;
        private int _pendingDynamicDelegateCount;
        private int[] _registeredDynamicDelegateIds = Array.Empty<int>();

        public EmissionSession(
            CompileSession compileSession,
            AbstractCILBuilder builder,
            bool emitExecutableCode = false,
            bool forceModuleDefinitions = false,
            bool collectDiagnostics = false)
        {
            CompileSession = compileSession ?? throw new ArgumentNullException(nameof(compileSession));
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            EmitExecutableCode = emitExecutableCode;
            ForceModuleDefinitions = forceModuleDefinitions;
            CollectDiagnostics = collectDiagnostics;
            _dynamicDelegateCapacity = CountFunctions(compileSession);
        }

        public CompileSession CompileSession { get; }
        public AbstractCILBuilder Builder { get; }
        public bool EmitExecutableCode { get; }
        public bool ForceModuleDefinitions { get; }
        public bool CollectDiagnostics { get; }
        public EngineOptions Options => CompileSession.Options;
        internal int[] RegisteredDynamicDelegateIds => _registeredDynamicDelegateIds;

        public EmissionReport Emit()
        {
            var modules = CompileSession.Modules ?? Array.Empty<ModulePlan>();
            var results = new ModuleEmissionResult[modules.Length];
            var moduleEmitter = new ModuleEmitter(this);
            var states = new ModuleEmitter.ModuleEmissionState[modules.Length];
            for (var i = 0; i < modules.Length; i++)
            {
                CompileSession.CancellationToken.ThrowIfCancellationRequested();
                states[i] = moduleEmitter.Prepare(modules[i]);
            }
            for (var i = 0; i < modules.Length; i++)
            {
                CompileSession.CancellationToken.ThrowIfCancellationRequested();
                results[i] = moduleEmitter.Emit(states[i]);
            }

            CompleteDynamicDelegates();
            return new EmissionReport(results);
        }

        public void EmitAll()
        {
            var modules = CompileSession.Modules ?? Array.Empty<ModulePlan>();
            var moduleEmitter = new ModuleEmitter(this);
            var states = new ModuleEmitter.ModuleEmissionState[modules.Length];
            for (var i = 0; i < modules.Length; i++)
            {
                CompileSession.CancellationToken.ThrowIfCancellationRequested();
                states[i] = moduleEmitter.Prepare(modules[i]);
            }
            for (var i = 0; i < modules.Length; i++)
            {
                CompileSession.CancellationToken.ThrowIfCancellationRequested();
                moduleEmitter.EmitWithoutReport(states[i]);
            }

            CompleteDynamicDelegates();
        }

        internal int GetDynamicDelegateId(FunctionPlan function, DynamicMethod method)
        {
            if (function.DynamicDelegateId != 0)
            {
                return function.DynamicDelegateId;
            }

            var id = DynamicMethodRegistry.Reserve();
            function.DynamicDelegateId = id;
            AddPendingDynamicDelegate(new PendingDynamicDelegate(id, method, function.CallConvention));
            return id;
        }

        private void AddPendingDynamicDelegate(PendingDynamicDelegate pending)
        {
            var delegates = _pendingDynamicDelegates;
            if (delegates == null)
            {
                _pendingDynamicDelegates = ArrayPool<PendingDynamicDelegate>.Shared.Rent(Math.Max(4, _dynamicDelegateCapacity));
                delegates = _pendingDynamicDelegates;
            }
            else if (_pendingDynamicDelegateCount == delegates.Length)
            {
                var replacement = ArrayPool<PendingDynamicDelegate>.Shared.Rent(delegates.Length * 2);
                Array.Copy(delegates, replacement, delegates.Length);
                ArrayPool<PendingDynamicDelegate>.Shared.Return(delegates, clearArray: true);
                _pendingDynamicDelegates = replacement;
                delegates = replacement;
            }

            delegates[_pendingDynamicDelegateCount++] = pending;
        }

        internal void CompleteDynamicDelegates()
        {
            if (_pendingDynamicDelegates == null)
            {
                return;
            }

            var registeredIds = new int[_pendingDynamicDelegateCount];
            for (var i = 0; i < _pendingDynamicDelegateCount; i++)
            {
                var pending = _pendingDynamicDelegates[i];
                ClosureMaterializer.RegisterDynamicDelegate(
                    pending.Id,
                    pending.Method,
                    pending.Convention);
                registeredIds[i] = pending.Id;
            }

            _registeredDynamicDelegateIds = registeredIds;
            ArrayPool<PendingDynamicDelegate>.Shared.Return(_pendingDynamicDelegates, clearArray: true);
            _pendingDynamicDelegates = null;
            _pendingDynamicDelegateCount = 0;
        }

        private static int CountFunctions(CompileSession compileSession)
        {
            var modules = compileSession.Modules;
            var count = 0;
            for (var moduleIndex = 0; moduleIndex < modules.Length; moduleIndex++)
            {
                count += modules[moduleIndex].Functions.Count;
            }

            return count;
        }

        private readonly struct PendingDynamicDelegate
        {
            public PendingDynamicDelegate(int id, DynamicMethod method, FunctionCallConvention convention)
            {
                Id = id;
                Method = method;
                Convention = convention;
            }

            public int Id { get; }
            public DynamicMethod Method { get; }
            public FunctionCallConvention Convention { get; }
        }
    }
}
