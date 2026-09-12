using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class EmissionSession
    {
        private readonly int _dynamicDelegateCapacity;
        private CallableReturnPredictions _callableReturns;
        private readonly Dictionary<(string Module, ConstructorInfo Constructor), MethodInfo> _clrConstructors = new();

        internal MethodInfo GetClrConstructor(string module, ConstructorInfo constructor)
        {
            if (_clrConstructors.TryGetValue((module, constructor), out var cached)) return cached;
            var parameters = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
            var (method, il) = Builder.DefineMethod(module, "__clr_construct_" + _clrConstructors.Count,
                constructor.DeclaringType, parameters);
            var result = il.DeclareLocal(constructor.DeclaringType);
            il.BeginExceptionBlock();
            for (var i = 0; i < parameters.Length; i++) il.Emit(OpCodes.Ldarg, i);
            il.Emit(OpCodes.Newobj, constructor);
            il.Emit(OpCodes.Stloc, result);
            il.BeginCatchBlock(typeof(Exception));
            il.Emit(OpCodes.Newobj, typeof(TargetInvocationException).GetConstructor([typeof(Exception)]));
            il.Emit(OpCodes.Throw);
            il.EndExceptionBlock();
            il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ret);
            _clrConstructors.Add((module, constructor), method);
            return method;
        }

        internal CallableReturnPredictions CallableReturns => _callableReturns ??=
            CallableReturnPredictions.Build(CompileSession.Modules, CompileSession.HostExports);
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
            AddPendingDynamicDelegate(new PendingDynamicDelegate(id, method, function.CallConvention, function.NativeEntryMethod));
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
                    pending.Convention, pending.NativeEntry);
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
            public PendingDynamicDelegate(int id, DynamicMethod method, FunctionCallConvention convention, System.Reflection.MethodInfo nativeEntry)
            {
                Id = id;
                Method = method;
                Convention = convention;
                NativeEntry = nativeEntry;
            }

            public System.Reflection.MethodInfo NativeEntry { get; }
            public int Id { get; }
            public DynamicMethod Method { get; }
            public FunctionCallConvention Convention { get; }
        }
    }
}
