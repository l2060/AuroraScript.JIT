using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace AuroraScript.Compiler.Backend.Emission
{
    /// <summary>
    /// Emits typed locals and native primitive CIL. Dynamic values are created only
    /// when an expression crosses a dynamic runtime boundary.
    /// </summary>
    internal sealed class TypedCilEmitter
    {
        private static readonly Type[] s_spanParameters = [typeof(ScriptContext), typeof(Span<ScriptDatum>)];
        private static readonly Type[][] s_fastParameters =
        [
            [typeof(ScriptContext)],
            [typeof(ScriptContext), typeof(ScriptDatum)],
            [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum)],
            [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)],
            [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)],
            [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)],
            [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)],
            [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]
        ];

        private readonly EmissionSession _session;
        private readonly ModulePlan _module;
        private readonly PreparedMethod[] _methods;
        private readonly PreparedDirectMethod[] _directMethods;
        private readonly Dictionary<FunctionDeclaration, FunctionPlan> _functionsByDeclaration;
        private readonly Stack<LoopTarget> _breakLabels = new();
        private readonly Stack<LoopTarget> _continueLabels = new();
        private FunctionPlan _function;
        private TypedFunctionCode _code;
        private ILGenerator _il;
        private LocalBuilder[] _locals;
        private LocalBuilder[] _parameterNumbers;
        private LocalBuilder[] _parameterNumberValid;
        private LocalBuilder[] _parameterBooleans;
        private int _localCount;
        private bool _prepared;
        private bool _directMode;
        private StackValueKind _methodReturnKind;
        private FlowValueType[] _directParameterTypes;
        private TypedModuleCode _moduleCode;
        private FunctionCallConvention _convention;
        private Label _returnLabel;
        private LocalBuilder _returnValue;
        private int _protectedRegionDepth;
        private int _finallyDepth;
        private bool _usesReturnEpilogue;
        private bool _handlesFinallyReturn;
        private Dictionary<int, int> _capturedLocalBySlot;
        private LocalBuilder _capturedUpvalues;
        private LocalBuilder _contextUpvalues;
        private bool[] _numericCacheNeeded;
        private bool[] _booleanCacheNeeded;
        private bool _hasArgumentBufferCleanup;
        private List<(LocalBuilder Arguments, LocalBuilder Count)> _argumentBuffers;
        private string _typedDocumentPath;

        public TypedCilEmitter(EmissionSession session, ModulePlan module)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            var maxId = -1;
            for (var i = 0; i < module.Functions.Count; i++)
            {
                maxId = Math.Max(maxId, module.Functions[i].Id.Value);
            }
            _methods = maxId < 0 ? Array.Empty<PreparedMethod>() : new PreparedMethod[maxId + 1];
            _directMethods = maxId < 0 ? Array.Empty<PreparedDirectMethod>() : new PreparedDirectMethod[maxId + 1];
            _functionsByDeclaration = new Dictionary<FunctionDeclaration, FunctionPlan>(ReferenceEqualityComparer.Instance);
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var declaration = module.Functions[i].Declaration;
                if (declaration != null) _functionsByDeclaration[declaration] = module.Functions[i];
            }
        }

        public void Prepare(bool forceAllExecutable = false)
        {
            if (_prepared) return;
            _prepared = true;

            if (_session.CollectDiagnostics)
            {
                return;
            }

            _moduleCode = TypedModuleCode.Build(_module);
            PrepareDirectMethods();
            var genericCandidates = BuildGenericCandidates();
            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                if (function.Method != null) continue;
                if (!genericCandidates[function.Id.Value]) continue;

                var code = _moduleCode.GetGeneric(function.Id);

                var convention = SelectConvention(function);
                var name = string.IsNullOrEmpty(function.Name)
                    ? "lambda_" + function.Id.Value
                    : function.Name;
                var methodName = convention == FunctionCallConvention.Span
                    ? name + "$typed"
                    : name + "$typed" + GetFastArity(convention);
                var (method, il) = _session.Builder.DefineMethod(
                    _module.Name,
                    methodName,
                    typeof(ScriptDatum),
                    GetParameterTypes(convention));

                function.CallConvention = convention;
                function.Method = method;
                function.DynamicDelegateId = 0;
                _methods[function.Id.Value] = new PreparedMethod(method, il, convention, code);
            }


            PrepareGenericDirectAdapters();
            EmitDirectMethods();
        }

        private bool[] BuildGenericCandidates()
        {
            var candidates = new bool[_methods.Length];
            for (var i = 0; i < _module.Functions.Count; i++)
            {
                candidates[_module.Functions[i].Id.Value] = true;
            }

            var changed = true;
            while (changed)
            {
                changed = false;
                for (var i = 0; i < _module.Functions.Count; i++)
                {
                    var function = _module.Functions[i];
                    if (!candidates[function.Id.Value]) continue;
                    var code = _moduleCode.GetGeneric(function.Id);
                    if (TypedSubsetValidator.CanEmit(
                        code,
                        id => HasDirectMethod(id) ||
                            (id.IsValid && (uint)id.Value < (uint)candidates.Length && candidates[id.Value]),
                        directMode: false,
                        requireNativeLocal: false))
                    {
                        continue;
                    }
                    candidates[function.Id.Value] = false;
                    changed = true;
                }
            }
            return candidates;
        }

        private void PrepareDirectMethods()
        {
            var candidates = new bool[_directMethods.Length];
            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                if (!function.IsDirectCallCandidate ||
                    function.HasDefaultParameters ||
                    function.UsesArgumentsObject)
                {
                    continue;
                }

                var code = _moduleCode.GetDirect(function.Id);
                if (code == null ||
                    code.ReturnType == FlowValueType.None ||
                    FlowValueTypeFacts.ContainsPackedArray(code.ReturnType) ||
                    !ReturnsOnAllPaths(function.Declaration.Body as Statement))
                {
                    continue;
                }

                candidates[function.Id.Value] = true;
            }

            // Calls form a graph, so validate against the complete candidate set and
            // monotonically remove functions whose callees cannot use the native ABI.
            // This also permits recursion without depending on declaration order.
            var changed = true;
            while (changed)
            {
                changed = false;
                for (var i = 0; i < _module.Functions.Count; i++)
                {
                    var function = _module.Functions[i];
                    if (!candidates[function.Id.Value]) continue;
                    var code = _moduleCode.GetDirect(function.Id);
                    if (TypedSubsetValidator.CanEmit(
                        code,
                        id => id.IsValid &&
                            (uint)id.Value < (uint)candidates.Length &&
                            candidates[id.Value],
                        directMode: true,
                        requireNativeLocal: false) &&
                        NativeDirectCallSignatureValidator.CanEmit(
                            code,
                            id => id.IsValid &&
                                (uint)id.Value < (uint)candidates.Length &&
                                candidates[id.Value],
                            id => _moduleCode.GetDirectParameters(id)))
                    {
                        continue;
                    }

                    candidates[function.Id.Value] = false;
                    changed = true;
                }
            }

            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                if (!candidates[function.Id.Value]) continue;
                var code = _moduleCode.GetDirect(function.Id);
                var parameterTypes = _moduleCode.GetDirectParameters(function.Id);

                var nativeParameters = new Type[parameterTypes.Length];
                for (var parameterIndex = 0; parameterIndex < parameterTypes.Length; parameterIndex++)
                {
                    nativeParameters[parameterIndex] = GetNativeParameterType(parameterTypes[parameterIndex]);
                }

                var name = string.IsNullOrEmpty(function.Name)
                    ? "lambda_" + function.Id.Value
                    : function.Name;
                var (method, il) = _session.Builder.DefineMethod(
                    _module.Name,
                    name + "$native",
                    code.ReturnType switch
                    {
                        FlowValueType.Int32 => typeof(int),
                        FlowValueType.Boolean => typeof(bool),
                        FlowValueType.Number => typeof(double),
                        _ => typeof(ScriptDatum)
                    },
                    nativeParameters);
                _directMethods[function.Id.Value] = new PreparedDirectMethod(
                    method,
                    il,
                    code,
                    parameterTypes,
                    code.ReturnType == FlowValueType.Int32
                        ? StackValueKind.Int32
                        : code.ReturnType == FlowValueType.Boolean
                            ? StackValueKind.Boolean
                        : code.ReturnType == FlowValueType.Number
                            ? StackValueKind.Number
                            : StackValueKind.Datum);
            }
        }

        private bool HasDirectMethod(FunctionId function)
        {
            return function.IsValid &&
                (uint)function.Value < (uint)_directMethods.Length &&
                _directMethods[function.Value].IsDefined;
        }

        private bool HasGenericDirectMethod(FunctionId function)
        {
            return function.IsValid &&
                (uint)function.Value < (uint)_methods.Length &&
                _methods[function.Value].DirectMethod != null;
        }

        private void PrepareGenericDirectAdapters()
        {
            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                ref var prepared = ref _methods[function.Id.Value];
                if (!function.IsDirectCallCandidate ||
                    !prepared.IsDefined ||
                    prepared.Convention == FunctionCallConvention.Span)
                {
                    continue;
                }

                var name = string.IsNullOrEmpty(function.Name)
                    ? "lambda_" + function.Id.Value
                    : function.Name;
                var parameterTypes = GetParameterTypes(prepared.Convention);
                var (adapter, il) = _session.Builder.DefineMethod(
                    _module.Name,
                    name + "$direct" + GetFastArity(prepared.Convention),
                    typeof(ScriptDatum),
                    parameterTypes);
                EmitGenericDirectAdapter(il, prepared.Method, parameterTypes.Length, name);
                prepared.DirectMethod = adapter;
                function.DirectEntryMethod = adapter;
            }
        }

        private void EmitGenericDirectAdapter(
            ILGenerator il,
            MethodInfo target,
            int parameterCount,
            string functionName)
        {
            var frame = il.DeclareLocal(typeof(int));
            var result = il.DeclareLocal(typeof(ScriptDatum));
            il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(il, functionName);
            il.Emit(OpCodes.Call, TypedRuntimeMetadata.EnterDirectFrame);
            il.Emit(OpCodes.Stloc, frame);

            il.BeginExceptionBlock();
            for (var i = 0; i < parameterCount; i++) il.Emit(OpCodes.Ldarg, i);
            il.Emit(OpCodes.Call, target);
            il.Emit(OpCodes.Stloc, result);
            il.BeginCatchBlock(typeof(Exception));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, TypedRuntimeMetadata.CaptureExceptionFrame);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, frame);
            il.Emit(OpCodes.Call, TypedRuntimeMetadata.LeaveFrame);
            il.Emit(OpCodes.Rethrow);
            il.EndExceptionBlock();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, frame);
            il.Emit(OpCodes.Call, TypedRuntimeMetadata.LeaveFrame);
            il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ret);
        }

        private void EmitDirectMethods()
        {
            for (var i = 0; i < _directMethods.Length; i++)
            {
                ref var direct = ref _directMethods[i];
                if (!direct.IsDefined || direct.Emitted) continue;
                var function = direct.Code.Function;
                EmitMethodBody(
                    function,
                    direct.Code,
                    direct.IL,
                    FunctionCallConvention.Span,
                    directMode: true,
                    direct.ParameterTypes,
                    direct.ReturnKind,
                    out _);
                direct.Emitted = true;
            }
        }

        public bool TryEmit(FunctionPlan function, out MethodInfo method, out int localCount)
        {
            method = null;
            localCount = 0;
            if (!_prepared) Prepare();
            if (function == null || (uint)function.Id.Value >= (uint)_methods.Length) return false;

            ref var prepared = ref _methods[function.Id.Value];
            if (!prepared.IsDefined || prepared.Emitted) return false;
            prepared.Emitted = true;

            EmitMethodBody(
                function,
                prepared.Code,
                prepared.IL,
                prepared.Convention,
                directMode: false,
                directParameterTypes: null,
                StackValueKind.Datum,
                out localCount);
            method = prepared.Method;
            return true;
        }

        private void EmitMethodBody(
            FunctionPlan function,
            TypedFunctionCode code,
            ILGenerator il,
            FunctionCallConvention convention,
            bool directMode,
            FlowValueType[] directParameterTypes,
            StackValueKind returnKind,
            out int localCount)
        {
            _function = function;
            _code = code;
            _il = il;
            _directMode = directMode;
            _convention = convention;
            _directParameterTypes = directParameterTypes;
            _methodReturnKind = returnKind;
            _localCount = 0;
            _capturedLocalBySlot = BuildCapturedLocalMap(function);
            FindParameterCaches(code, out _numericCacheNeeded, out _booleanCacheNeeded);
            _locals = DeclareLocals();
            try
            {
                var body = function.Declaration.Body as Statement;
                _handlesFinallyReturn = !directMode && ContainsReturnInFinally(body);
                _hasArgumentBufferCleanup = !directMode &&
                    PooledArgumentCallDetector.Contains(function.Declaration);
                _argumentBuffers = _hasArgumentBufferCleanup
                    ? new List<(LocalBuilder Arguments, LocalBuilder Count)>()
                    : null;
                _usesReturnEpilogue = _handlesFinallyReturn ||
                    _hasArgumentBufferCleanup ||
                    ContainsProtectedRegion(body);
                if (_usesReturnEpilogue)
                {
                    _returnLabel = _il.DefineLabel();
                    _returnValue = DeclareLocal(returnKind switch
                    {
                        StackValueKind.Int32 => typeof(int),
                        StackValueKind.Number => typeof(double),
                        StackValueKind.Boolean => typeof(bool),
                        _ => typeof(ScriptDatum)
                    });
                }
                if (_handlesFinallyReturn)
                {
                    _il.BeginExceptionBlock();
                    _protectedRegionDepth++;
                }
                if (_hasArgumentBufferCleanup)
                {
                    _il.BeginExceptionBlock();
                    _protectedRegionDepth++;
                }
                if (directMode) InitializeDirectParameters();
                else
                {
                    InitializeCapturedLocals();
                    _session.Builder.SetDebuggerMetadata(
                        function.Method,
                        CreateDebuggerMetadata(function, convention));
                    InitializeParameters(convention);
                }
                EmitStatement(body);
                if (returnKind is StackValueKind.Int32 or StackValueKind.Boolean) _il.Emit(OpCodes.Ldc_I4_0);
                else if (returnKind == StackValueKind.Number) _il.Emit(OpCodes.Ldc_R8, double.NaN);
                else EmitNull();
                if (_usesReturnEpilogue)
                {
                    _il.Emit(OpCodes.Stloc, _returnValue);
                }
                if (_hasArgumentBufferCleanup)
                {
                    _protectedRegionDepth--;
                    _il.BeginFinallyBlock();
                    for (var i = 0; i < _argumentBuffers.Count; i++)
                    {
                        _il.Emit(OpCodes.Ldloc, _argumentBuffers[i].Arguments);
                        _il.Emit(OpCodes.Ldloc, _argumentBuffers[i].Count);
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ReturnArguments);
                    }
                    _il.EndExceptionBlock();
                }
                if (_handlesFinallyReturn)
                {
                    _il.Emit(OpCodes.Leave, _returnLabel);
                    _protectedRegionDepth--;
                    _il.BeginCatchBlock(TypedRuntimeMetadata.ReturnSignalType);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetReturnValue);
                    _il.Emit(OpCodes.Stloc, _returnValue);
                    _il.EndExceptionBlock();
                    _il.MarkLabel(_returnLabel);
                    _il.Emit(OpCodes.Ldloc, _returnValue);
                }
                else if (_usesReturnEpilogue)
                {
                    _il.MarkLabel(_returnLabel);
                    _il.Emit(OpCodes.Ldloc, _returnValue);
                }
                _il.Emit(OpCodes.Ret);
                localCount = _localCount;
            }
            finally
            {
                _function = null;
                _code = null;
                _il = null;
                _locals = null;
                _parameterNumbers = null;
                _parameterNumberValid = null;
                _parameterBooleans = null;
                _directParameterTypes = null;
                _directMode = false;
                _convention = FunctionCallConvention.Span;
                _returnValue = null;
                _protectedRegionDepth = 0;
                _finallyDepth = 0;
                _usesReturnEpilogue = false;
                _handlesFinallyReturn = false;
                _capturedLocalBySlot = null;
                _capturedUpvalues = null;
                _contextUpvalues = null;
                _numericCacheNeeded = null;
                _booleanCacheNeeded = null;
                _hasArgumentBufferCleanup = false;
                _argumentBuffers = null;
                _typedDocumentPath = null;
                _methodReturnKind = StackValueKind.Datum;
                _breakLabels.Clear();
                _continueLabels.Clear();
            }
        }

        private LocalBuilder[] DeclareLocals()
        {
            var result = new LocalBuilder[_function.LocalSlots.Length];
            _parameterNumbers = new LocalBuilder[result.Length];
            _parameterNumberValid = new LocalBuilder[result.Length];
            _parameterBooleans = new LocalBuilder[result.Length];
            for (var i = 0; i < result.Length; i++)
            {
                var type = _code.LocalTypes[i] switch
                {
                    FlowValueType.Int32 => typeof(int),
                    FlowValueType.Number => typeof(double),
                    FlowValueType.Boolean => typeof(bool),
                    FlowValueType.String => typeof(string),
                    FlowValueType.Array => typeof(ScriptArray),
                    FlowValueType.Int32Array => GetPackedLocalClrType(FlowValueType.Int32Array),
                    FlowValueType.Int8Array => GetPackedLocalClrType(FlowValueType.Int8Array),
                    FlowValueType.Float64Array => GetPackedLocalClrType(FlowValueType.Float64Array),
                    FlowValueType.BooleanArray => GetPackedLocalClrType(FlowValueType.BooleanArray),
                    FlowValueType.UInt8Array => GetPackedLocalClrType(FlowValueType.UInt8Array),
                    FlowValueType.Int16Array => GetPackedLocalClrType(FlowValueType.Int16Array),
                    FlowValueType.UInt16Array => GetPackedLocalClrType(FlowValueType.UInt16Array),
                    FlowValueType.UInt32Array => GetPackedLocalClrType(FlowValueType.UInt32Array),
                    FlowValueType.Int64Array => GetPackedLocalClrType(FlowValueType.Int64Array),
                    FlowValueType.UInt64Array => GetPackedLocalClrType(FlowValueType.UInt64Array),
                    _ => typeof(ScriptDatum),
                };
                result[i] = DeclareLocal(type);
                _session.Builder.SetLocalSymInfo(result[i], _function.LocalSlots[i].Name);
                if (!_directMode &&
                    _function.LocalSlots[i].IsParameter &&
                    _numericCacheNeeded[i] &&
                    (_capturedLocalBySlot == null || !_capturedLocalBySlot.ContainsKey(i)) &&
                    !_code.WrittenLocals[i])
                {
                    _parameterNumbers[i] = DeclareLocal(typeof(double));
                    _parameterNumberValid[i] = DeclareLocal(typeof(bool));
                }
                if (!_directMode &&
                    _function.LocalSlots[i].IsParameter &&
                    _booleanCacheNeeded[i] &&
                    (_capturedLocalBySlot == null || !_capturedLocalBySlot.ContainsKey(i)) &&
                    !_code.WrittenLocals[i])
                {
                    _parameterBooleans[i] = DeclareLocal(typeof(bool));
                }
            }
            return result;
        }

        private static void FindParameterCaches(
            TypedFunctionCode code,
            out bool[] numeric,
            out bool[] boolean)
        {
            numeric = new bool[code.LocalTypes.Length];
            boolean = new bool[code.LocalTypes.Length];
            var collector = new ParameterCacheCollector(code, numeric, boolean);
            collector.Visit(code.Function.Declaration?.Body);
        }

        private static Dictionary<int, int> BuildCapturedLocalMap(FunctionPlan function)
        {
            if (function.CapturedLocalSlots.Length == 0) return null;
            var result = new Dictionary<int, int>(function.CapturedLocalSlots.Length);
            for (var i = 0; i < function.CapturedLocalSlots.Length; i++)
            {
                var slot = function.CapturedLocalSlots[i];
                if (slot.SourceLocal.IsValid) result[slot.SourceLocal.Value] = slot.Id.Value;
            }
            return result;
        }

        private bool TryGetCapturedIndex(LocalSlotId slot, out int index)
        {
            if (slot.IsValid && _capturedLocalBySlot != null &&
                _capturedLocalBySlot.TryGetValue(slot.Value, out index))
            {
                return true;
            }
            index = -1;
            return false;
        }

        private void InitializeCapturedLocals()
        {
            if (_function.CapturedLocalSlots.Length == 0) return;
            _capturedUpvalues = DeclareLocal(typeof(Upvalue[]));
            EmitInt32(_function.CapturedLocalSlots.Length);
            _il.Emit(OpCodes.Newarr, typeof(Upvalue));
            _il.Emit(OpCodes.Stloc, _capturedUpvalues);
            for (var i = 0; i < _function.CapturedLocalSlots.Length; i++) InitializeCapturedCell(i);
        }

        private void InitializeCapturedCell(int index)
        {
            _il.Emit(OpCodes.Ldloc, _capturedUpvalues);
            EmitInt32(index);
            _il.Emit(OpCodes.Newobj, TypedRuntimeMetadata.UpvalueConstructor);
            _il.Emit(OpCodes.Stelem_Ref);
        }

        private void InitializeBlockCapturedLocals(BlockStatement block)
        {
            if (_capturedLocalBySlot == null || ReferenceEquals(block, _function.Declaration.Body)) return;
            for (var i = 0; i < _function.LocalSlots.Length; i++)
            {
                var local = _function.LocalSlots[i];
                if (!ReferenceEquals(local.Declaration?.Parent, block) &&
                    !ReferenceEquals(local.Declaration, block))
                {
                    continue;
                }
                if (TryGetCapturedIndex(local.Id, out var captured)) InitializeCapturedCell(captured);
            }
        }

        private void EmitLoadCapturedCell(int index)
        {
            _il.Emit(OpCodes.Ldloc, _capturedUpvalues);
            EmitInt32(index);
            _il.Emit(OpCodes.Ldelem_Ref);
        }

        private void EmitLoadContextUpvalues()
        {
            if (_contextUpvalues == null)
            {
                _contextUpvalues = DeclareLocal(typeof(Upvalue[]));
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextUpvalues);
                _il.Emit(OpCodes.Stloc, _contextUpvalues);
            }
            _il.Emit(OpCodes.Ldloc, _contextUpvalues);
        }

        private void EmitLoadContextCell(int index)
        {
            EmitLoadContextUpvalues();
            EmitInt32(index);
            _il.Emit(OpCodes.Ldelem_Ref);
        }

        private void EmitLoadLocal(LocalSlotId slot)
        {
            if (TryGetCapturedIndex(slot, out var captured))
            {
                EmitLoadCapturedCell(captured);
                _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.UpvalueValue);
                return;
            }
            _il.Emit(OpCodes.Ldloc, _locals[slot.Value]);
        }

        private void EmitStoreLocalFromStack(LocalSlotId slot)
        {
            if (TryGetCapturedIndex(slot, out var captured))
            {
                var value = DeclareLocal(typeof(ScriptDatum));
                _il.Emit(OpCodes.Stloc, value);
                EmitLoadCapturedCell(captured);
                _il.Emit(OpCodes.Ldloc, value);
                _il.Emit(OpCodes.Stfld, TypedRuntimeMetadata.UpvalueValue);
                return;
            }
            _il.Emit(OpCodes.Stloc, _locals[slot.Value]);
        }

        private void EmitLoadUpvalue(UpvalueSlotId slot)
        {
            EmitLoadContextCell(slot.Value);
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.UpvalueValue);
        }

        private void EmitStoreUpvalue(UpvalueSlotId slot, LocalBuilder value)
        {
            EmitLoadContextCell(slot.Value);
            _il.Emit(OpCodes.Ldloc, value);
            _il.Emit(OpCodes.Stfld, TypedRuntimeMetadata.UpvalueValue);
        }

        private void EmitClosureUpvalue(UpvalueSlot slot)
        {
            if (slot.IsInherited)
            {
                EmitLoadContextCell(slot.SourceUpvalue.Value);
                return;
            }
            if (TryGetCapturedIndex(slot.SourceLocal, out var captured))
            {
                EmitLoadCapturedCell(captured);
                return;
            }
            throw new NotSupportedException("Unresolved closure upvalue '" + slot.Name + "'.");
        }

        private void InitializeDirectParameters()
        {
            var parameterIndex = 0;
            for (var i = 0; i < _function.LocalSlots.Length; i++)
            {
                var slot = _function.LocalSlots[i];
                if (!slot.IsParameter) continue;
                _il.Emit(OpCodes.Ldarg, parameterIndex);
                EmitStoreLocalFromStack(slot.Id);
                parameterIndex++;
            }
        }

        private void InitializeParameters(FunctionCallConvention convention)
        {
            var parameterIndex = 0;
            for (var i = 0; i < _function.LocalSlots.Length; i++)
            {
                var slot = _function.LocalSlots[i];
                if (!slot.IsParameter) continue;

                if (convention == FunctionCallConvention.Span)
                {
                    _il.Emit(OpCodes.Ldarg_1);
                    EmitInt32(parameterIndex);
                    var defaultValue = _function.Declaration.Parameters[parameterIndex].Initializer;
                    if (defaultValue == null)
                    {
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetArgument);
                    }
                    else
                    {
                        EmitDatum(defaultValue);
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetArgumentOrDefault);
                    }
                }
                else
                {
                    _il.Emit(OpCodes.Ldarg, parameterIndex + 1);
                }

                EmitStoreLocalFromStack(slot.Id);
                if (_parameterNumbers[slot.Id.Value] != null)
                {
                    EmitLoadLocal(slot.Id);
                    _il.Emit(OpCodes.Ldloca, _parameterNumbers[slot.Id.Value]);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.TryToNumber);
                    _il.Emit(OpCodes.Stloc, _parameterNumberValid[slot.Id.Value]);

                    var numericReady = _il.DefineLabel();
                    _il.Emit(OpCodes.Ldloc, _parameterNumberValid[slot.Id.Value]);
                    _il.Emit(OpCodes.Brtrue, numericReady);
                    EmitLoadLocal(slot.Id);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToArithmeticNumber);
                    _il.Emit(OpCodes.Stloc, _parameterNumbers[slot.Id.Value]);
                    _il.MarkLabel(numericReady);
                }
                if (_parameterBooleans[slot.Id.Value] != null)
                {
                    EmitLoadLocal(slot.Id);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToBooleanDatum);
                    _il.Emit(OpCodes.Stloc, _parameterBooleans[slot.Id.Value]);
                }
                parameterIndex++;
            }
        }

        private void EmitStatement(Statement statement)
        {
            if (statement is not null and not BlockStatement)
            {
                EmitLocation(statement);
                _session.Builder.MarkSequencePoint(statement.Range, _il);
            }
            switch (statement)
            {
                case null:
                    return;
                case BlockStatement block:
                    InitializeBlockCapturedLocals(block);
                    for (var i = 0; i < block.Functions.Count; i++) EmitFunctionDeclaration(block.Functions[i]);
                    for (var i = 0; i < block.Statements.Count; i++) EmitStatement(block.Statements[i]);
                    return;
                case FunctionDeclaration function:
                    EmitFunctionDeclaration(function);
                    return;
                case VariableDeclaration variable:
                    EmitVariable(variable);
                    return;
                case ExpressionStatement expression:
                    if (expression.Expression != null)
                    {
                        EmitExpression(expression.Expression);
                        _il.Emit(OpCodes.Pop);
                    }
                    return;
                case ReturnStatement @return:
                    if (_methodReturnKind == StackValueKind.Int32)
                    {
                        if (@return.Expression == null) _il.Emit(OpCodes.Ldc_I4_0);
                        else EmitInt32Value(@return.Expression);
                    }
                    else if (_methodReturnKind == StackValueKind.Number)
                    {
                        if (@return.Expression == null) _il.Emit(OpCodes.Ldc_R8, double.NaN);
                        else EmitNumber(@return.Expression);
                    }
                    else if (_methodReturnKind == StackValueKind.Boolean)
                    {
                        if (@return.Expression == null) _il.Emit(OpCodes.Ldc_I4_0);
                        else EmitCondition(@return.Expression);
                    }
                    else
                    {
                        if (@return.Expression == null) EmitNull();
                        else EmitDatum(@return.Expression);
                    }
                    if (!_usesReturnEpilogue)
                    {
                        _il.Emit(OpCodes.Ret);
                        return;
                    }
                    if (_finallyDepth != 0)
                    {
                        if (_methodReturnKind != StackValueKind.Datum || !_handlesFinallyReturn)
                        {
                            throw new NotSupportedException("Return from finally requires the generic typed ABI.");
                        }
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ReturnFromFinally);
                        return;
                    }
                    _il.Emit(OpCodes.Stloc, _returnValue);
                    _il.Emit(_protectedRegionDepth == 0 ? OpCodes.Br : OpCodes.Leave, _returnLabel);
                    return;
                case IfStatement @if:
                    EmitIf(@if);
                    return;
                case WhileStatement @while:
                    EmitWhile(@while);
                    return;
                case ForStatement @for:
                    EmitFor(@for);
                    return;
                case ForInStatement forIn:
                    EmitForIn(forIn);
                    return;
                case TryStatement @try:
                    EmitTry(@try);
                    return;
                case ThrowStatement @throw:
                    EmitDatumOrNull(@throw.Expression);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.Throw);
                    return;
                case DeleteStatement delete:
                    EmitDelete(delete);
                    return;
                case BreakStatement:
                    EmitLoopTransfer(_breakLabels.Peek(), isContinue: false);
                    return;
                case ContinueStatement:
                    EmitLoopTransfer(_continueLabels.Peek(), isContinue: true);
                    return;
                case DebuggerStatement:
                    EmitDebugger();
                    return;
                default:
                    throw new NotSupportedException("Typed statement: " + statement.GetType().Name);
            }
        }

        private void EmitDebugger()
        {
#if NET9_0_OR_GREATER
            if (_session.Builder is Builders.PersistedBuilder &&
                _session.Options.Optimization.Level == OptimizeOptions.Debug)
            {
                _il.Emit(OpCodes.Break);
            }
#endif
        }

        private void EmitVariable(VariableDeclaration variable)
        {
            if (variable.IsDeclare) return;
            if (variable.Pattern is ObjectDestructuringPattern objectPattern)
            {
                EmitObjectDestructuring(variable, objectPattern);
                return;
            }
            if (variable.Pattern is ArrayDestructuringPattern arrayPattern)
            {
                EmitArrayDestructuring(variable, arrayPattern);
                return;
            }
            var slot = _code.GetDeclarationSlot(variable);
            if (!slot.IsValid) throw new InvalidOperationException("Unbound local declaration.");

            switch (_code.GetLocalType(slot))
            {
                case FlowValueType.Int32:
                    if (variable.Initializer == null) _il.Emit(OpCodes.Ldc_I4_0);
                    else EmitInt32Value(variable.Initializer);
                    break;
                case FlowValueType.Number:
                    if (variable.Initializer == null) _il.Emit(OpCodes.Ldc_R8, 0d);
                    else EmitNumber(variable.Initializer);
                    break;
                case FlowValueType.Boolean:
                    if (variable.Initializer == null) _il.Emit(OpCodes.Ldc_I4_0);
                    else EmitCondition(variable.Initializer);
                    break;
                case FlowValueType.String:
                    if (variable.Initializer == null) _session.Builder.LoadStringConstant(_il, string.Empty);
                    else EmitString(variable.Initializer);
                    break;
                case FlowValueType.Array:
                    if (variable.Initializer == null) _il.Emit(OpCodes.Ldnull);
                    else EmitArrayReference(variable.Initializer);
                    break;
                case FlowValueType.Int32Array:
                case FlowValueType.Int8Array:
                case FlowValueType.Float64Array:
                case FlowValueType.BooleanArray:
                case FlowValueType.UInt8Array:
                case FlowValueType.Int16Array:
                case FlowValueType.UInt16Array:
                case FlowValueType.UInt32Array:
                case FlowValueType.Int64Array:
                case FlowValueType.UInt64Array:
                    if (variable.Initializer == null)
                    {
                        _il.Emit(OpCodes.Ldnull);
                    }
                    else
                    {
                        EmitPackedArrayReference(variable.Initializer, _code.GetLocalType(slot));
                    }
                    break;
                default:
                    if (variable.Initializer == null) EmitNull();
                    else EmitDatum(variable.Initializer);
                    break;
            }
            EmitStoreLocalFromStack(slot);
        }

        private void EmitObjectDestructuring(
            VariableDeclaration declaration,
            ObjectDestructuringPattern pattern)
        {
            var source = DeclareLocal(typeof(ScriptDatum));
            EmitDatumOrNull(declaration.Initializer);
            _il.Emit(OpCodes.Stloc, source);
            for (var i = 0; i < pattern.Properties.Count; i++)
            {
                var name = pattern.Properties[i].Value;
                var slot = FindLocal(name, declaration);
                if (!slot.IsValid) throw new InvalidOperationException("Unbound destructuring name '" + name + "'.");
                _il.Emit(OpCodes.Ldloc, source);
                _il.Emit(OpCodes.Ldarg_0);
                _session.Builder.LoadStringConstant(_il, name);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetProperty);
                EmitStoreLocalFromStack(slot);
            }
        }

        private void EmitArrayDestructuring(
            VariableDeclaration declaration,
            ArrayDestructuringPattern pattern)
        {
            var source = DeclareLocal(typeof(ScriptDatum));
            EmitDatumOrNull(declaration.Initializer);
            _il.Emit(OpCodes.Stloc, source);
            var restIndex = -1;
            for (var i = 0; i < pattern.Elements.Count; i++)
            {
                if (pattern.Elements[i] is SpreadExpression)
                {
                    restIndex = i;
                    break;
                }
            }
            var restTrailing = restIndex >= 0 ? pattern.Elements.Count - restIndex - 1 : 0;
            for (var i = 0; i < pattern.Elements.Count; i++)
            {
                var element = pattern.Elements[i];
                if (element == null) continue;
                var isRest = element is SpreadExpression;
                var name = element switch
                {
                    NameExpression direct => direct.Identifier?.Value,
                    SpreadExpression { Expression: NameExpression spread } => spread.Identifier?.Value,
                    _ => null
                };
                if (string.IsNullOrEmpty(name)) throw new NotSupportedException("Array destructuring target.");
                var slot = FindLocal(name, declaration);
                if (!slot.IsValid) throw new InvalidOperationException("Unbound destructuring name '" + name + "'.");
                _il.Emit(OpCodes.Ldloc, source);
                EmitInt32(i);
                EmitInt32(isRest ? restTrailing : restIndex >= 0 && i > restIndex ? pattern.Elements.Count - i : 0);
                _il.Emit(OpCodes.Call, isRest
                    ? TypedRuntimeMetadata.SliceDestructureArray
                    : TypedRuntimeMetadata.GetDestructureElement);
                EmitStoreLocalFromStack(slot);
            }
        }

        private LocalSlotId FindLocal(string name, AstNode node)
        {
            var scope = 0;
            var current = node;
            while (current != null)
            {
                if (_function.LocalScopeByNode != null &&
                    _function.LocalScopeByNode.TryGetValue(current, out scope))
                {
                    break;
                }
                current = current.Parent;
            }
            while (scope >= 0)
            {
                for (var i = _function.LocalSlots.Length - 1; i >= 0; i--)
                {
                    if (_function.LocalSlots[i].ScopeId == scope &&
                        StringComparer.Ordinal.Equals(_function.LocalSlots[i].Name, name))
                    {
                        return _function.LocalSlots[i].Id;
                    }
                }
                scope = (uint)scope < (uint)_function.LocalScopes.Length
                    ? _function.LocalScopes[scope].ParentId
                    : -1;
            }
            return LocalSlotId.Invalid;
        }

        private void EmitIf(IfStatement statement)
        {
            var elseLabel = _il.DefineLabel();
            var endLabel = _il.DefineLabel();
            EmitCondition(statement.Condition);
            _il.Emit(OpCodes.Brfalse, elseLabel);
            EmitStatement(statement.Body);
            if (statement.Else != null) _il.Emit(OpCodes.Br, endLabel);
            _il.MarkLabel(elseLabel);
            EmitStatement(statement.Else);
            if (statement.Else != null) _il.MarkLabel(endLabel);
        }

        private void EmitWhile(WhileStatement statement)
        {
            var conditionLabel = _il.DefineLabel();
            var endLabel = _il.DefineLabel();
            _continueLabels.Push(new LoopTarget(conditionLabel, _protectedRegionDepth, _finallyDepth));
            _breakLabels.Push(new LoopTarget(endLabel, _protectedRegionDepth, _finallyDepth));
            try
            {
                _il.MarkLabel(conditionLabel);
                EmitCondition(statement.Condition);
                _il.Emit(OpCodes.Brfalse, endLabel);
                EmitLoopBody(
                    statement.Body,
                    conditionLabel,
                    endLabel,
                    ContainsFinallyTransferToCurrentLoop(statement.Body));
                _il.Emit(OpCodes.Br, conditionLabel);
                _il.MarkLabel(endLabel);
            }
            finally
            {
                _breakLabels.Pop();
                _continueLabels.Pop();
            }
        }

        private void EmitFor(ForStatement statement)
        {
            if (statement.Initializer is Statement initializerStatement)
            {
                EmitStatement(initializerStatement);
            }
            else if (statement.Initializer is Expression initializerExpression)
            {
                EmitExpression(initializerExpression);
                _il.Emit(OpCodes.Pop);
            }

            var conditionLabel = _il.DefineLabel();
            var incrementLabel = _il.DefineLabel();
            var endLabel = _il.DefineLabel();
            _continueLabels.Push(new LoopTarget(incrementLabel, _protectedRegionDepth, _finallyDepth));
            _breakLabels.Push(new LoopTarget(endLabel, _protectedRegionDepth, _finallyDepth));
            try
            {
                _il.MarkLabel(conditionLabel);
                if (statement.Condition != null)
                {
                    EmitCondition(statement.Condition);
                    _il.Emit(OpCodes.Brfalse, endLabel);
                }
                EmitLoopBody(
                    statement.Body,
                    incrementLabel,
                    endLabel,
                    ContainsFinallyTransferToCurrentLoop(statement.Body));
                _il.MarkLabel(incrementLabel);
                if (statement.Incrementor != null)
                {
                    EmitExpression(statement.Incrementor);
                    _il.Emit(OpCodes.Pop);
                }
                _il.Emit(OpCodes.Br, conditionLabel);
                _il.MarkLabel(endLabel);
            }
            finally
            {
                _breakLabels.Pop();
                _continueLabels.Pop();
            }
        }

        private void EmitForIn(ForInStatement statement)
        {
            EmitStatement(statement.Initializer);
            EmitDatum(statement.Iterator.Right);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetEnumerator);
            var iterator = DeclareLocal(typeof(ScriptEnumerator));
            _il.Emit(OpCodes.Stloc, iterator);
            var current = DeclareLocal(typeof(ScriptDatum));

            var conditionLabel = _il.DefineLabel();
            var continueLabel = _il.DefineLabel();
            var endLabel = _il.DefineLabel();
            _continueLabels.Push(new LoopTarget(continueLabel, _protectedRegionDepth, _finallyDepth));
            _breakLabels.Push(new LoopTarget(endLabel, _protectedRegionDepth, _finallyDepth));
            try
            {
                _il.MarkLabel(conditionLabel);
                _il.Emit(OpCodes.Ldloc, iterator);
                _il.Emit(OpCodes.Ldloca, current);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.MoveNext);
                _il.Emit(OpCodes.Brfalse, endLabel);
                var binding = _code.GetName(statement.Iterator.Left);
                EmitStoreBoundName(binding, current);
                EmitLoopBody(
                    statement.Body,
                    continueLabel,
                    endLabel,
                    ContainsFinallyTransferToCurrentLoop(statement.Body));
                _il.MarkLabel(continueLabel);
                _il.Emit(OpCodes.Br, conditionLabel);
                _il.MarkLabel(endLabel);
            }
            finally
            {
                _breakLabels.Pop();
                _continueLabels.Pop();
            }
        }

        private void EmitTry(TryStatement statement)
        {
            var hasCatch = statement.CatchBody != null || !string.IsNullOrEmpty(statement.CatchVariable);
            if (!hasCatch && statement.FinallyBody == null)
            {
                EmitStatement(statement.Body);
                return;
            }

            _il.BeginExceptionBlock();
            _protectedRegionDepth++;
            try
            {
                EmitStatement(statement.Body);
            }
            finally
            {
                _protectedRegionDepth--;
            }

            if (hasCatch)
            {
                _il.BeginCatchBlock(typeof(Exception));
                _protectedRegionDepth++;
                try
                {
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.PrepareCatch);
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToScriptError);
                    if (!string.IsNullOrEmpty(statement.CatchVariable))
                    {
                        var slot = FindLocal(statement.CatchVariable, statement.CatchBody ?? statement);
                        if (!slot.IsValid) throw new InvalidOperationException("Unbound catch variable.");
                        EmitStoreLocalFromStack(slot);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Pop);
                    }
                    EmitStatement(statement.CatchBody);
                }
                finally
                {
                    _protectedRegionDepth--;
                }
            }
            else if (statement.FinallyBody != null)
            {
                // AuroraScript's established try/finally semantics consume the script
                // exception before running finally, even without an explicit catch.
                _il.BeginCatchBlock(typeof(Exception));
                _protectedRegionDepth++;
                try
                {
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.PrepareCatch);
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToScriptError);
                    _il.Emit(OpCodes.Pop);
                }
                finally
                {
                    _protectedRegionDepth--;
                }
            }

            if (statement.FinallyBody != null)
            {
                _il.BeginFinallyBlock();
                _protectedRegionDepth++;
                _finallyDepth++;
                try
                {
                    EmitStatement(statement.FinallyBody);
                }
                finally
                {
                    _finallyDepth--;
                    _protectedRegionDepth--;
                }
            }
            _il.EndExceptionBlock();
        }

        private void EmitLoopBody(
            Statement body,
            Label continueLabel,
            Label breakLabel,
            bool catchesFinallyTransfer)
        {
            if (!catchesFinallyTransfer)
            {
                EmitStatement(body);
                return;
            }

            _il.BeginExceptionBlock();
            _protectedRegionDepth++;
            try
            {
                EmitStatement(body);
            }
            finally
            {
                _protectedRegionDepth--;
            }

            _il.BeginCatchBlock(TypedRuntimeMetadata.LoopTransferSignalType);
            var breakTransfer = _il.DefineLabel();
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.IsContinue);
            _il.Emit(OpCodes.Brfalse, breakTransfer);
            _il.Emit(OpCodes.Leave, continueLabel);
            _il.MarkLabel(breakTransfer);
            _il.Emit(OpCodes.Leave, breakLabel);
            _il.EndExceptionBlock();
        }

        private void EmitLoopTransfer(LoopTarget target, bool isContinue)
        {
            if (_finallyDepth > target.FinallyDepth)
            {
                _il.Emit(
                    OpCodes.Call,
                    isContinue
                        ? TypedRuntimeMetadata.ContinueFromFinally
                        : TypedRuntimeMetadata.BreakFromFinally);
                return;
            }
            _il.Emit(
                _protectedRegionDepth > target.ProtectedDepth ? OpCodes.Leave : OpCodes.Br,
                target.Label);
        }

        private void EmitDelete(DeleteStatement statement)
        {
            if (statement.Expression is GetPropertyExpression property &&
                TryGetStaticPropertyName(property.Property, out var name))
            {
                _il.Emit(OpCodes.Ldarg_0);
                EmitDatum(property.Object);
                _session.Builder.LoadStringConstant(_il, name);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DeleteProperty);
                return;
            }
            if (statement.Expression is GetElementExpression element)
            {
                _il.Emit(OpCodes.Ldarg_0);
                EmitDatum(element.Object);
                EmitDatum(element.Index);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DeleteElement);
                return;
            }
            throw new NotSupportedException("Typed delete target.");
        }

        private StackValueKind EmitExpression(Expression expression)
        {
            switch (expression)
            {
                case TypedDocumentExpression tdoc:
                    return EmitTypedDocument(tdoc);
                case LiteralExpression literal:
                    return EmitLiteral(literal);
                case NameExpression name:
                    return EmitName(name);
                case BinaryExpression binary:
                    return EmitBinary(binary);
                case AssignmentExpression assignment:
                    return EmitAssignment(assignment);
                case CompoundExpression compound:
                    return EmitCompound(compound);
                case UnaryExpression unary:
                    return EmitUnary(unary);
                case FunctionCallExpression call:
                    if (TryGetDirectCall(call, out _)) return EmitDirectCall(call);
                    if (TryGetGenericDirectCall(call, out _)) return EmitGenericDirectCall(call);
                    return EmitCall(call);
                case GetPropertyExpression property:
                    return EmitGetProperty(property);
                case SetPropertyExpression property:
                    return EmitSetProperty(property);
                case GetElementExpression element:
                    return EmitGetElement(element);
                case SetElementExpression element:
                    return EmitSetElement(element);
                case ArrayLiteralExpression array:
                    return EmitArrayLiteral(array);
                case MapExpression map:
                    return EmitMap(map);
                case IncludedExpression included:
                    return EmitIncluded(included.Left, included.Right);
                case InExpression @in:
                    return EmitIncluded(@in.Left, @in.Right);
                case TemplateStringExpression template:
                    return EmitTemplateString(template);
                case NewExpression @new:
                    return EmitNew(@new);
                case LambdaExpression lambda:
                    return EmitLambda(lambda);
                case GroupExpression group:
                    if (group.Expressions.Count == 0)
                    {
                        EmitNull();
                        return StackValueKind.Datum;
                    }
                    for (var i = 0; i < group.Expressions.Count - 1; i++)
                    {
                        EmitExpression(group.Expressions[i]);
                        _il.Emit(OpCodes.Pop);
                    }
                    return EmitExpression(group.Expressions[^1]);
                default:
                    throw new NotSupportedException("Typed expression: " + expression.GetType().Name);
            }
        }

        private StackValueKind EmitGetProperty(GetPropertyExpression expression)
        {
            if (!TryGetStaticPropertyName(expression.Property, out var name))
            {
                throw new NotSupportedException("Dynamic dot-property name.");
            }
            var receiverType = _code.GetExpressionType(expression.Object);
            if (receiverType == FlowValueType.Array &&
                StringComparer.Ordinal.Equals(name, "length"))
            {
                EmitArrayReference(expression.Object);
                _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArrayLength);
                return StackValueKind.Int32;
            }
            if (FlowValueTypeFacts.IsPackedArray(receiverType) &&
                StringComparer.Ordinal.Equals(name, "length"))
            {
                EmitPackedArrayStorage(expression.Object, receiverType);
                _il.Emit(OpCodes.Ldlen);
                return StackValueKind.Int32;
            }
            EmitDatum(expression.Object);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetProperty);
            return StackValueKind.Datum;
        }

        private StackValueKind EmitSetProperty(SetPropertyExpression expression)
        {
            if (!TryGetStaticPropertyName(expression.Property, out var name))
            {
                throw new NotSupportedException("Dynamic dot-property name.");
            }
            EmitDatum(expression.Object);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            EmitDatum(expression.Value);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.SetProperty);
            return StackValueKind.Datum;
        }

        private StackValueKind EmitGetElement(GetElementExpression expression)
        {
            var receiverType = _code.GetExpressionType(expression.Object);
            if (receiverType == FlowValueType.Array &&
                FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(expression.Index)))
            {
                EmitArrayReference(expression.Object);
                EmitInt32Value(expression.Index);
                _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArrayGetElement);
                return StackValueKind.Datum;
            }
            if (FlowValueTypeFacts.IsPackedArray(receiverType) &&
                FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(expression.Index)))
            {
                return EmitPackedGetElement(expression.Object, expression.Index, receiverType);
            }

            EmitDatum(expression.Object);
            if (FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(expression.Index)))
            {
                EmitNumber(expression.Index);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetElementNumber);
            }
            else
            {
                EmitDatum(expression.Index);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetElement);
            }
            return StackValueKind.Datum;
        }

        private StackValueKind EmitSetElement(SetElementExpression expression)
        {
            var receiverType = _code.GetExpressionType(expression.Object);
            if (receiverType == FlowValueType.Array &&
                FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(expression.Index)))
            {
                var receiver = DeclareLocal(typeof(ScriptArray));
                var index = DeclareLocal(typeof(int));
                var value = DeclareLocal(typeof(ScriptDatum));
                EmitArrayReference(expression.Object);
                _il.Emit(OpCodes.Stloc, receiver);
                EmitInt32Value(expression.Index);
                _il.Emit(OpCodes.Stloc, index);
                EmitDatum(expression.Value);
                _il.Emit(OpCodes.Stloc, value);
                _il.Emit(OpCodes.Ldloc, receiver);
                _il.Emit(OpCodes.Ldloc, index);
                _il.Emit(OpCodes.Ldloc, value);
                _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArraySetElement);
                _il.Emit(OpCodes.Ldloc, value);
                return StackValueKind.Datum;
            }
            if (FlowValueTypeFacts.IsPackedArray(receiverType) &&
                FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(expression.Index)))
            {
                return EmitPackedSetElement(expression, receiverType);
            }

            EmitDatum(expression.Object);
            var numberIndex = FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(expression.Index));
            if (numberIndex) EmitNumber(expression.Index);
            else EmitDatum(expression.Index);
            EmitDatum(expression.Value);
            _il.Emit(OpCodes.Call, numberIndex
                ? TypedRuntimeMetadata.SetElementNumber
                : TypedRuntimeMetadata.SetElement);
            return StackValueKind.Datum;
        }

        private StackValueKind EmitPackedGetElement(
            Expression receiver,
            Expression index,
            FlowValueType arrayType)
        {
            EmitPackedArrayStorage(receiver, arrayType);
            LocalBuilder indexLocal = null;
            if (arrayType is FlowValueType.Int64Array or FlowValueType.UInt64Array)
            {
                indexLocal = DeclareLocal(typeof(int));
                EmitInt32Value(index);
                _il.Emit(OpCodes.Stloc, indexLocal);
                _il.Emit(OpCodes.Ldloc, indexLocal);
            }
            else
            {
                EmitInt32Value(index);
            }
            switch (arrayType)
            {
                case FlowValueType.Int32Array:
                    _il.Emit(OpCodes.Ldelem_I4);
                    return StackValueKind.Int32;
                case FlowValueType.Int8Array:
                    _il.Emit(OpCodes.Ldelem_I1);
                    return StackValueKind.Int32;
                case FlowValueType.Float64Array:
                    _il.Emit(OpCodes.Ldelem_R8);
                    return StackValueKind.Number;
                case FlowValueType.BooleanArray:
                    _il.Emit(OpCodes.Ldelem_U1);
                    return StackValueKind.Boolean;
                case FlowValueType.UInt8Array:
                    _il.Emit(OpCodes.Ldelem_U1);
                    _il.Emit(OpCodes.Conv_R8);
                    return StackValueKind.Number;
                case FlowValueType.Int16Array:
                    _il.Emit(OpCodes.Ldelem_I2);
                    _il.Emit(OpCodes.Conv_R8);
                    return StackValueKind.Number;
                case FlowValueType.UInt16Array:
                    _il.Emit(OpCodes.Ldelem_U2);
                    _il.Emit(OpCodes.Conv_R8);
                    return StackValueKind.Number;
                case FlowValueType.UInt32Array:
                    _il.Emit(OpCodes.Ldelem_U4);
                    _il.Emit(OpCodes.Conv_R8);
                    return StackValueKind.Number;
                case FlowValueType.Int64Array:
                    _il.Emit(OpCodes.Ldelem_I8);
                    _il.Emit(OpCodes.Ldloc, indexLocal);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToExactInt64Number);
                    return StackValueKind.Number;
                case FlowValueType.UInt64Array:
                    _il.Emit(OpCodes.Ldelem_I8);
                    _il.Emit(OpCodes.Conv_U8);
                    _il.Emit(OpCodes.Ldloc, indexLocal);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToExactUInt64Number);
                    return StackValueKind.Number;
                default:
                    throw new NotSupportedException("Unknown packed-array element type.");
            }
        }

        private StackValueKind EmitPackedSetElement(
            SetElementExpression expression,
            FlowValueType arrayType)
        {
            var receiver = DeclareLocal(GetPackedStorageType(arrayType));
            EmitPackedArrayStorage(expression.Object, arrayType);
            _il.Emit(OpCodes.Stloc, receiver);

            var index = DeclareLocal(typeof(int));
            EmitInt32Value(expression.Index);
            _il.Emit(OpCodes.Stloc, index);

            var valueKind = EmitExpression(expression.Value);
            var value = DeclareLocal(GetStackClrType(valueKind));
            _il.Emit(OpCodes.Stloc, value);

            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldloc, index);
            _il.Emit(OpCodes.Ldloc, value);
            if (arrayType == FlowValueType.Float64Array)
            {
                ConvertStackToNumber(valueKind);
                _il.Emit(OpCodes.Stelem_R8);
            }
            else if (arrayType == FlowValueType.BooleanArray)
            {
                ConvertStackToBoolean(valueKind);
                _il.Emit(OpCodes.Stelem_I1);
            }
            else if (arrayType is FlowValueType.UInt8Array or FlowValueType.Int8Array)
            {
                ConvertStackToNumber(valueKind);
                _il.Emit(OpCodes.Conv_I1);
                _il.Emit(OpCodes.Stelem_I1);
            }
            else if (arrayType is FlowValueType.Int16Array or FlowValueType.UInt16Array)
            {
                ConvertStackToNumber(valueKind);
                _il.Emit(OpCodes.Conv_I2);
                _il.Emit(OpCodes.Stelem_I2);
            }
            else if (arrayType == FlowValueType.UInt32Array)
            {
                ConvertStackToNumber(valueKind);
                _il.Emit(OpCodes.Conv_U4);
                _il.Emit(OpCodes.Stelem_I4);
            }
            else if (arrayType is FlowValueType.Int64Array or FlowValueType.UInt64Array)
            {
                ConvertStackToNumber(valueKind);
                _il.Emit(arrayType == FlowValueType.UInt64Array ? OpCodes.Conv_U8 : OpCodes.Conv_I8);
                _il.Emit(OpCodes.Stelem_I8);
            }
            else
            {
                ConvertStackToInt32(valueKind, truncateThroughInt64: false);
                if (arrayType == FlowValueType.Int8Array) _il.Emit(OpCodes.Conv_I1);
                _il.Emit(arrayType == FlowValueType.Int8Array
                    ? OpCodes.Stelem_I1
                    : OpCodes.Stelem_I4);
            }

            _il.Emit(OpCodes.Ldloc, value);
            return valueKind;
        }

        private StackValueKind EmitArrayLiteral(ArrayLiteralExpression expression)
        {
            var hasSpread = false;
            for (var i = 0; i < expression.Elements.Count; i++)
            {
                if (expression.Elements[i] is SpreadExpression)
                {
                    hasSpread = true;
                    break;
                }
            }

            EmitInt32(hasSpread ? 0 : expression.Elements.Count);
            _il.Emit(OpCodes.Newobj, TypedRuntimeMetadata.ScriptArrayCapacity);
            for (var i = 0; i < expression.Elements.Count; i++)
            {
                _il.Emit(OpCodes.Dup);
                if (expression.Elements[i] is SpreadExpression spread)
                {
                    EmitDatumAtTDocIndex(spread.Expression, i);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.SpreadIntoArray);
                }
                else if (hasSpread)
                {
                    EmitDatumOrNullAtTDocIndex(expression.Elements[i], i);
                    _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArrayPush);
                }
                else
                {
                    EmitInt32(i);
                    EmitDatumOrNullAtTDocIndex(expression.Elements[i], i);
                    _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArraySetElement);
                }
            }
            return StackValueKind.Array;
        }

        private StackValueKind EmitMap(MapExpression expression)
        {
            var hasReadOnly = false;
            for (var i = 0; i < expression.Entries.Count; i++)
            {
                if (expression.Entries[i] is MapKeyValueExpression { ReadOnly: true })
                {
                    hasReadOnly = true;
                    break;
                }
            }

            if (!hasReadOnly && TryGetFastObject3(expression, out var first, out var second, out var third))
            {
                EmitFastMapEntryAtTDocPath(first);
                EmitFastMapEntryAtTDocPath(second);
                EmitFastMapEntryAtTDocPath(third);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.CreateObject3);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
                return StackValueKind.Datum;
            }

            _il.Emit(OpCodes.Newobj, TypedRuntimeMetadata.ScriptObjectConstructor);
            for (var i = 0; i < expression.Entries.Count; i++)
            {
                var entry = expression.Entries[i];
                _il.Emit(OpCodes.Dup);
                if (entry is SpreadExpression spread)
                {
                    EmitDatum(spread.Expression);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.CopyProperties);
                    continue;
                }

                if (entry is MapKeyValueExpression { ReadOnly: true })
                {
                    _session.Builder.LoadStringConstant(_il, GetMapEntryKey(entry));
                    EmitDatumAtTDocProperty(GetMapEntryValue(entry), GetMapEntryKey(entry));
                    _il.Emit(OpCodes.Ldc_I4_0);
                    _il.Emit(OpCodes.Ldc_I4_1);
                    _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptObjectDefineDatum);
                }
                else
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _session.Builder.LoadStringConstant(_il, GetMapEntryKey(entry));
                    EmitDatumAtTDocProperty(GetMapEntryValue(entry), GetMapEntryKey(entry));
                    _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptObjectSetProperty);
                }
            }
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
            return StackValueKind.Datum;
        }

        private StackValueKind EmitTypedDocument(TypedDocumentExpression expression)
        {
            var ownsPath = _typedDocumentPath == null;
            if (ownsPath) _typedDocumentPath = "$";
            try
            {
                return EmitTypedDocumentCore(expression);
            }
            finally
            {
                if (ownsPath) _typedDocumentPath = null;
            }
        }

        private StackValueKind EmitTypedDocumentCore(TypedDocumentExpression expression)
        {
            var typeName = expression.TypeName;
            if (string.IsNullOrEmpty(typeName))
            {
                return EmitExpression(expression.Value);
            }

            // Interpolated TDoc values always pass through the shared binder.  In
            // particular, an interpolation must not bypass the outer explicit type
            // (Object/Array/PackedArray/Date/CLR alias).
            if (expression.IsInterpolation)
            {
                EmitBoundTypedLiteral(typeName, expression.Value);
                return StackValueKind.Datum;
            }

            // A registered CLR alias is built member-by-member.  This avoids
            // allocating a temporary ScriptObject only to immediately reflect
            // it back into the CLR instance.
            if (!IsBuiltInTDocTypeName(typeName) && expression.Value is MapExpression clrObject)
            {
                EmitTypedClrObject(typeName, clrObject);
                return StackValueKind.Datum;
            }

            // A nested interpolation makes the value dynamic.  Build the ordinary
            // datum shape and let the binder validate every member/element at the
            // same runtime boundary as a root interpolation.  This also prevents
            // CIL Conv_* instructions from narrowing an unchecked value first.
            if (ContainsTDocInterpolation(expression.Value) &&
                typeName is not ("Regex" or "HashMap"))
            {
                EmitBoundTypedLiteral(typeName, expression.Value);
                return StackValueKind.Datum;
            }

            switch (typeName)
            {
                case "Null":
                    EmitNull();
                    return StackValueKind.Datum;
                case "String":
                    return EmitTypedString(expression.Value);
                case "Number":
                    EmitNumber(expression.Value);
                    return StackValueKind.Number;
                case "Boolean":
                    EmitCondition(expression.Value);
                    return StackValueKind.Boolean;
                case "Object":
                    if (expression.Value is not MapExpression objectValue)
                    {
                        throw new NotSupportedException("TDoc Object requires an object value.");
                    }
                    return EmitMap(objectValue);
                case "Array":
                    if (expression.Value is not ArrayLiteralExpression arrayValue)
                    {
                        throw new NotSupportedException("TDoc Array requires an array value.");
                    }
                    return EmitArrayLiteral(arrayValue);
                case "Int32Array":
                    return EmitTypedPackedArray(expression.Value, FlowValueType.Int32Array);
                case "Int8Array":
                    return EmitTypedPackedArray(expression.Value, FlowValueType.Int8Array);
                case "Float64Array":
                    return EmitTypedPackedArray(expression.Value, FlowValueType.Float64Array);
                case "BooleanArray":
                    return EmitTypedPackedArray(expression.Value, FlowValueType.BooleanArray);
                case "UInt8Array":
                    return EmitTypedPackedArray(expression.Value, FlowValueType.UInt8Array);
                case "Int16Array":
                    return EmitTypedPackedArray(expression.Value, FlowValueType.Int16Array);
                case "UInt16Array":
                    return EmitTypedPackedArray(expression.Value, FlowValueType.UInt16Array);
                case "UInt32Array":
                    return EmitTypedPackedArray(expression.Value, FlowValueType.UInt32Array);
                case "Int64Array":
                    return EmitTypedPackedArray(expression.Value, FlowValueType.Int64Array);
                case "UInt64Array":
                    return EmitTypedPackedArray(expression.Value, FlowValueType.UInt64Array);
                case "StringBuffer":
                case "Path":
                    if (expression.IsInterpolation)
                    {
                        EmitDatum(expression.Value);
                        return StackValueKind.Datum;
                    }
                    EmitTypedGlobalConstructor(typeName, expression.Value);
                    return StackValueKind.Datum;
                case "Date":
                    EmitBoundTypedLiteral(typeName, expression.Value);
                    return StackValueKind.Datum;
                case "Regex":
                    if (expression.IsInterpolation)
                    {
                        EmitDatum(expression.Value);
                        return StackValueKind.Datum;
                    }
                    EmitTypedRegex(expression.Value);
                    return StackValueKind.Datum;
                case "HashMap":
                    EmitTypedHashMap(expression.Value);
                    return StackValueKind.Datum;
                default:
                    // Host aliases use the same binding contract as standalone
                    // TDoc and are resolved against the current engine registry.
                    EmitBoundTypedLiteral(typeName, expression.Value);
                    return StackValueKind.Datum;
            }
        }

        private void EmitBoundTypedLiteral(string typeName, Expression value)
        {
            var path = _typedDocumentPath ?? "$";
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, typeName);
            EmitDatum(value);
            _session.Builder.LoadStringConstant(_il, path);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.BindTypedDocumentAtPath);
        }

        private void EmitTypedClrObject(string alias, MapExpression expression)
        {
            var target = DeclareLocal(typeof(ClrInstanceObject));
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, alias);
            _session.Builder.LoadStringConstant(_il, _typedDocumentPath ?? "$");
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.CreateTypedDocumentClrObject);
            _il.Emit(OpCodes.Stloc, target);

            for (var i = 0; i < expression.Entries.Count; i++)
            {
                if (expression.Entries[i] is not MapKeyValueExpression entry)
                {
                    throw new NotSupportedException("TDoc CLR aliases require named members.");
                }

                _il.Emit(OpCodes.Ldloc, target);
                _session.Builder.LoadStringConstant(_il, alias);
                _session.Builder.LoadStringConstant(_il, entry.Key.Value);
                _il.Emit(entry.ReadOnly ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                EmitDatumAtTDocProperty(entry.Value, entry.Key.Value);
                _session.Builder.LoadStringConstant(
                    _il,
                    AppendTDocPropertyPath(_typedDocumentPath ?? "$", entry.Key.Value));
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.SetTypedDocumentClrMember);
            }

            _il.Emit(OpCodes.Ldloc, target);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
        }

        private static bool IsBuiltInTDocTypeName(string typeName)
        {
            return typeName is "Null" or "Object" or "Array" or "String" or "Number" or
                "Boolean" or "StringBuffer" or "Date" or "Regex" or "Path" or "HashMap" or
                "Int32Array" or "Int8Array" or "Float64Array" or "BooleanArray" or
                "UInt8Array" or "Int16Array" or "UInt16Array" or "UInt32Array" or
                "Int64Array" or "UInt64Array";
        }

        private static bool ContainsTDocInterpolation(Expression value)
        {
            if (value is TypedDocumentExpression tdoc)
            {
                return tdoc.IsInterpolation || ContainsTDocInterpolation(tdoc.Value);
            }
            if (value is ArrayLiteralExpression array)
            {
                for (var i = 0; i < array.Elements.Count; i++)
                {
                    if (ContainsTDocInterpolation(array.Elements[i])) return true;
                }
            }
            else if (value is MapExpression map)
            {
                for (var i = 0; i < map.Entries.Count; i++)
                {
                    if (ContainsTDocInterpolation(map.Entries[i])) return true;
                }
            }
            else if (value is MapKeyValueExpression entry)
            {
                return ContainsTDocInterpolation(entry.Value);
            }
            else if (value is SpreadExpression spread)
            {
                return ContainsTDocInterpolation(spread.Expression);
            }
            return false;
        }

        private StackValueKind EmitTypedString(Expression expression)
        {
            var kind = EmitExpression(expression);
            if (kind == StackValueKind.String) return kind;
            ConvertToDatum(kind);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToString);
            return StackValueKind.String;
        }

        private void EmitTypedGlobalConstructor(string typeName, Expression value)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, typeName);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetGlobal);
            _il.Emit(OpCodes.Ldarg_0);
            EmitDatum(value);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.New1);
        }

        private void EmitTypedRegex(Expression value)
        {
            if (value is not MapExpression map)
            {
                throw new NotSupportedException("TDoc Regex requires an object value.");
            }

            Expression pattern = null;
            Expression flags = null;
            for (var i = 0; i < map.Entries.Count; i++)
            {
                if (map.Entries[i] is not MapKeyValueExpression entry) continue;
                if (StringComparer.Ordinal.Equals(entry.Key.Value, "pattern")) pattern = entry.Value;
                else if (StringComparer.Ordinal.Equals(entry.Key.Value, "flags")) flags = entry.Value;
            }
            if (pattern == null) throw new NotSupportedException("TDoc Regex requires 'pattern'.");

            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, "Regex");
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetGlobal);
            _il.Emit(OpCodes.Ldarg_0);
            EmitDatum(pattern);
            if (flags == null) EmitNull();
            else EmitDatum(flags);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.New2);
        }

        private void EmitTypedHashMap(Expression value)
        {
            if (value is not ArrayLiteralExpression entries)
            {
                throw new NotSupportedException("TDoc HashMap requires an array value.");
            }

            EmitInt32(entries.Elements.Count);
            _il.Emit(OpCodes.Newobj, TypedRuntimeMetadata.ScriptHashMapConstructor);
            for (var i = 0; i < entries.Elements.Count; i++)
            {
                var pair = entries.Elements[i] is TypedDocumentExpression pairTDoc
                    ? pairTDoc.Value
                    : entries.Elements[i];
                if (pair is not ArrayLiteralExpression pairArray || pairArray.Elements.Count != 2)
                {
                    throw new NotSupportedException("TDoc HashMap entries must contain two values.");
                }
                _il.Emit(OpCodes.Dup);
                EmitDatum(pairArray.Elements[0]);
                EmitDatum(pairArray.Elements[1]);
                _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptHashMapPut);
            }
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
        }

        private StackValueKind EmitTypedPackedArray(Expression expression, FlowValueType arrayType)
        {
            if (expression is not ArrayLiteralExpression array)
            {
                throw new NotSupportedException("TDoc packed array requires an array value.");
            }

            var elementType = GetPackedStorageType(arrayType).GetElementType();
            EmitInt32(array.Elements.Count);
            if (_directMode)
            {
                _il.Emit(OpCodes.Newarr, elementType);
            }
            else
            {
                _il.Emit(OpCodes.Newobj, GetPackedConstructor(arrayType));
            }

            for (var i = 0; i < array.Elements.Count; i++)
            {
                _il.Emit(OpCodes.Dup);
                if (!_directMode)
                {
                    _il.Emit(OpCodes.Ldfld, GetPackedItemsField(arrayType));
                }
                EmitInt32(i);
                var value = array.Elements[i];
                if (arrayType == FlowValueType.BooleanArray)
                {
                    EmitCondition(value);
                    _il.Emit(OpCodes.Stelem_I1);
                }
                else if (arrayType == FlowValueType.Float64Array)
                {
                    EmitNumber(value);
                    _il.Emit(OpCodes.Stelem_R8);
                }
                else if (arrayType == FlowValueType.UInt8Array)
                {
                    EmitInt32Value(value);
                    _il.Emit(OpCodes.Conv_U1);
                    _il.Emit(OpCodes.Stelem_I1);
                }
                else if (arrayType == FlowValueType.Int16Array)
                {
                    EmitInt32Value(value);
                    _il.Emit(OpCodes.Conv_I2);
                    _il.Emit(OpCodes.Stelem_I2);
                }
                else if (arrayType == FlowValueType.UInt16Array)
                {
                    EmitInt32Value(value);
                    _il.Emit(OpCodes.Conv_U2);
                    _il.Emit(OpCodes.Stelem_I2);
                }
                else if (arrayType == FlowValueType.UInt32Array)
                {
                    EmitNumber(value);
                    _il.Emit(OpCodes.Conv_U4);
                    _il.Emit(OpCodes.Stelem_I4);
                }
                else if (arrayType == FlowValueType.Int64Array)
                {
                    EmitNumber(value);
                    _il.Emit(OpCodes.Conv_I8);
                    _il.Emit(OpCodes.Stelem_I8);
                }
                else if (arrayType == FlowValueType.UInt64Array)
                {
                    EmitNumber(value);
                    _il.Emit(OpCodes.Conv_U8);
                    _il.Emit(OpCodes.Stelem_I8);
                }
                else
                {
                    EmitInt32Value(value);
                    if (arrayType == FlowValueType.Int8Array) _il.Emit(OpCodes.Conv_I1);
                    _il.Emit(arrayType == FlowValueType.Int8Array
                        ? OpCodes.Stelem_I1
                        : OpCodes.Stelem_I4);
                }
            }
            return GetPackedLocalStackKind(arrayType);
        }

        private void EmitFastMapEntryAtTDocPath(MapKeyValueExpression entry)
        {
            _session.Builder.LoadStringConstant(_il, entry.Key.Value);
            EmitDatumAtTDocProperty(entry.Value, entry.Key.Value);
        }

        private void EmitDatumAtTDocProperty(Expression expression, string propertyName)
        {
            if (_typedDocumentPath == null)
            {
                EmitDatum(expression);
                return;
            }

            var previous = _typedDocumentPath;
            _typedDocumentPath = AppendTDocPropertyPath(previous, propertyName);
            try { EmitDatum(expression); }
            finally { _typedDocumentPath = previous; }
        }

        private void EmitDatumAtTDocIndex(Expression expression, int index)
        {
            if (_typedDocumentPath == null)
            {
                EmitDatum(expression);
                return;
            }

            var previous = _typedDocumentPath;
            _typedDocumentPath = previous + "[" + index + "]";
            try { EmitDatum(expression); }
            finally { _typedDocumentPath = previous; }
        }

        private void EmitDatumOrNullAtTDocIndex(Expression expression, int index)
        {
            if (expression == null)
            {
                EmitNull();
                return;
            }
            EmitDatumAtTDocIndex(expression, index);
        }

        private static string AppendTDocPropertyPath(string path, string propertyName)
        {
            if (TypedDocumentPath.IsIdentifier(propertyName))
            {
                return path + "." + propertyName;
            }

            var builder = new StringBuilder(path.Length + (propertyName?.Length ?? 0) + 4);
            builder.Append(path).Append("[\"");
            foreach (var value in propertyName ?? string.Empty)
            {
                if (value is '\\' or '"') builder.Append('\\');
                builder.Append(value);
            }
            return builder.Append("\"]").ToString();
        }

        private StackValueKind EmitIncluded(Expression value, Expression collection)
        {
            EmitDatum(collection);
            EmitDatum(value);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.Includes);
            return StackValueKind.Boolean;
        }

        private StackValueKind EmitTemplateString(TemplateStringExpression expression)
        {
            var elementCount = 0;
            for (var i = 0; i < expression.Parts.Count; i++)
            {
                var part = expression.Parts[i];
                if (!part.IsLiteral || part.Literal.Length != 0) elementCount++;
            }

            if (elementCount == 0)
            {
                _session.Builder.LoadStringConstant(_il, string.Empty);
                return StackValueKind.String;
            }
            if (elementCount <= 4)
            {
                for (var i = 0; i < expression.Parts.Count; i++)
                {
                    EmitTemplatePart(expression.Parts[i]);
                }
                if (elementCount == 2) _il.Emit(OpCodes.Call, TypedRuntimeMetadata.StringConcat2);
                else if (elementCount == 3) _il.Emit(OpCodes.Call, TypedRuntimeMetadata.StringConcat3);
                else if (elementCount == 4) _il.Emit(OpCodes.Call, TypedRuntimeMetadata.StringConcat4);
                return StackValueKind.String;
            }

            var values = new LocalBuilder[expression.Parts.Count];
            var literalLength = 0;
            for (var i = 0; i < expression.Parts.Count; i++)
            {
                var part = expression.Parts[i];
                if (part.IsLiteral)
                {
                    literalLength += part.Literal.Length;
                    continue;
                }
                EmitString(part.Expression);
                values[i] = DeclareLocal(typeof(string));
                _il.Emit(OpCodes.Stloc, values[i]);
            }

            EmitInt32(literalLength);
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] == null) continue;
                _il.Emit(OpCodes.Ldloc, values[i]);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.StringLength);
                _il.Emit(OpCodes.Add);
            }
            _il.Emit(OpCodes.Newobj, TypedRuntimeMetadata.StringBuilderCapacity);
            for (var i = 0; i < expression.Parts.Count; i++)
            {
                var part = expression.Parts[i];
                if (part.IsLiteral)
                {
                    if (part.Literal.Length == 0) continue;
                    _session.Builder.LoadStringConstant(_il, part.Literal);
                }
                else
                {
                    _il.Emit(OpCodes.Ldloc, values[i]);
                }
                _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.StringBuilderAppend);
            }
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.StringBuilderToString);
            return StackValueKind.String;
        }

        private void EmitTemplatePart(TemplateStringPart part)
        {
            if (part.IsLiteral)
            {
                if (part.Literal.Length != 0) _session.Builder.LoadStringConstant(_il, part.Literal);
                return;
            }
            EmitString(part.Expression);
        }

        private void EmitString(Expression expression)
        {
            var kind = EmitExpression(expression);
            if (kind == StackValueKind.String) return;
            ConvertToDatum(kind);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToString);
        }

        private void EmitDatumOrNull(Expression expression)
        {
            if (expression == null) EmitNull();
            else EmitDatum(expression);
        }

        private StackValueKind EmitDirectCall(FunctionCallExpression call)
        {
            if (call.Target is not NameExpression target || HasSpread(call.Arguments))
            {
                throw new NotSupportedException("Typed indirect call.");
            }

            var function = _code.GetName(target).DirectFunction;
            if (!HasDirectMethod(function))
            {
                throw new NotSupportedException("Typed direct call target.");
            }

            ref var prepared = ref _directMethods[function.Value];
            var parameterCount = prepared.ParameterTypes.Length;
            var argumentCount = call.Arguments.Count;
            var commonCount = Math.Min(parameterCount, argumentCount);
            for (var i = 0; i < commonCount; i++)
            {
                if (prepared.ParameterTypes[i] == FlowValueType.Int32Bitwise)
                {
                    EmitInt32Operand(call.Arguments[i], truncateThroughInt64: true);
                }
                else if (prepared.ParameterTypes[i] == FlowValueType.Int32Shift)
                {
                    EmitInt32Operand(call.Arguments[i], truncateThroughInt64: false);
                }
                else if (prepared.ParameterTypes[i] == FlowValueType.Int32)
                {
                    EmitInt32Value(call.Arguments[i]);
                }
                else if (prepared.ParameterTypes[i] == FlowValueType.Number)
                {
                    EmitNumber(call.Arguments[i]);
                }
                else if (prepared.ParameterTypes[i] == FlowValueType.Array)
                {
                    EmitArrayReference(call.Arguments[i]);
                }
                else if (prepared.ParameterTypes[i] == FlowValueType.Boolean)
                {
                    EmitCondition(call.Arguments[i]);
                }
                else if (FlowValueTypeFacts.IsPackedArray(prepared.ParameterTypes[i]))
                {
                    EmitPackedArrayStorage(call.Arguments[i], prepared.ParameterTypes[i]);
                }
                else
                {
                    EmitDatum(call.Arguments[i]);
                }
            }

            for (var i = commonCount; i < parameterCount; i++)
            {
                // Missing script arguments are null. A parameter inferred as native
                // Number cannot reach this path because missing-call evidence widens it.
                EmitNull();
            }

            // Script calls still evaluate surplus arguments, in source order, even
            // though the statically bound callee does not receive them.
            for (var i = parameterCount; i < argumentCount; i++)
            {
                EmitExpression(call.Arguments[i]);
                _il.Emit(OpCodes.Pop);
            }

            _il.Emit(OpCodes.Call, prepared.Method);
            return prepared.ReturnKind;
        }

        private bool TryGetDirectCall(FunctionCallExpression call, out FunctionId function)
        {
            if (call?.Target is NameExpression target && !HasSpread(call.Arguments))
            {
                function = _code.GetName(target).DirectFunction;
                return HasDirectMethod(function) && CanUseNativeDirectSignature(call, function);
            }
            function = FunctionId.Invalid;
            return false;
        }

        private bool CanUseNativeDirectSignature(FunctionCallExpression call, FunctionId function)
        {
            ref var prepared = ref _directMethods[function.Value];
            for (var i = 0; i < prepared.ParameterTypes.Length; i++)
            {
                if (FlowValueTypeFacts.IsNativeDirectParameter(prepared.ParameterTypes[i]) &&
                    (i >= call.Arguments.Count ||
                        !FlowValueTypeFacts.CanPassNativeArgument(
                            prepared.ParameterTypes[i],
                            _code.GetExpressionType(call.Arguments[i]))))
                {
                    return false;
                }
            }
            return true;
        }

        private bool TryGetGenericDirectCall(FunctionCallExpression call, out FunctionId function)
        {
            if (!_directMode && call?.Target is NameExpression target && !HasSpread(call.Arguments))
            {
                function = _code.GetName(target).DirectFunction;
                return HasGenericDirectMethod(function);
            }
            function = FunctionId.Invalid;
            return false;
        }

        private StackValueKind EmitGenericDirectCall(FunctionCallExpression call)
        {
            var targetName = (NameExpression)call.Target;
            var function = _code.GetName(targetName).DirectFunction;
            ref var prepared = ref _methods[function.Value];
            var arity = GetFastArity(prepared.Convention);
            if (arity < 0)
            {
                throw new NotSupportedException("Direct span call.");
            }

            _il.Emit(OpCodes.Ldarg_0);
            var common = Math.Min(arity, call.Arguments.Count);
            for (var i = 0; i < common; i++) EmitDatum(call.Arguments[i]);
            for (var i = common; i < arity; i++) EmitNull();
            for (var i = arity; i < call.Arguments.Count; i++)
            {
                EmitExpression(call.Arguments[i]);
                _il.Emit(OpCodes.Pop);
            }
            _il.Emit(OpCodes.Call, prepared.DirectMethod);
            return StackValueKind.Datum;
        }

        private StackValueKind EmitCall(FunctionCallExpression call)
        {
            if (TryEmitArrayFactoryCall(call, out var arrayFactoryResult))
            {
                return arrayFactoryResult;
            }
            if (_directMode)
            {
                throw new NotSupportedException("Native direct code cannot cross a dynamic call boundary.");
            }

            if (call.Target is GetPropertyExpression property &&
                TryGetStaticPropertyName(property.Property, out var name))
            {
                return EmitPropertyCall(call, property.Object, name);
            }

            var hasSpread = HasSpread(call.Arguments);
            if (!hasSpread && call.Arguments.Count <= 7)
            {
                EmitDatum(call.Target);
                _il.Emit(OpCodes.Ldarg_0);
                for (var i = 0; i < call.Arguments.Count; i++) EmitDatum(call.Arguments[i]);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.Invoke[call.Arguments.Count]);
                return StackValueKind.Datum;
            }

            var target = DeclareLocal(typeof(ScriptDatum));
            EmitDatum(call.Target);
            _il.Emit(OpCodes.Stloc, target);
            var arguments = DeclareLocal(typeof(ScriptDatum[]));
            var count = DeclareLocal(typeof(int));
            var result = DeclareLocal(typeof(ScriptDatum));
            InitializeArgumentBuffer(arguments, count);
            EmitArgumentBuffer(call.Arguments, arguments, count);
            _il.Emit(OpCodes.Ldloc, target);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldloc, arguments);
            _il.Emit(OpCodes.Ldloc, count);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.InvokeMany);
            _il.Emit(OpCodes.Stloc, result);
            ReleaseArgumentBuffer(arguments, count);
            _il.Emit(OpCodes.Ldloc, result);
            return StackValueKind.Datum;
        }

        private StackValueKind EmitPropertyCall(
            FunctionCallExpression call,
            Expression receiver,
            string name)
        {
            if (_code.GetExpressionType(receiver) == FlowValueType.Array &&
                StringComparer.Ordinal.Equals(name, "push") &&
                !HasSpread(call.Arguments) &&
                call.Arguments.Count <= 7)
            {
                return EmitArrayPushCall(call, receiver);
            }

            var hasSpread = HasSpread(call.Arguments);
            if (!hasSpread && call.Arguments.Count <= 7)
            {
                EmitDatum(receiver);
                _il.Emit(OpCodes.Ldarg_0);
                _session.Builder.LoadStringConstant(_il, name);
                for (var i = 0; i < call.Arguments.Count; i++) EmitDatum(call.Arguments[i]);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.InvokeProperty[call.Arguments.Count]);
                return StackValueKind.Datum;
            }

            var receiverLocal = DeclareLocal(typeof(ScriptDatum));
            EmitDatum(receiver);
            _il.Emit(OpCodes.Stloc, receiverLocal);
            var arguments = DeclareLocal(typeof(ScriptDatum[]));
            var count = DeclareLocal(typeof(int));
            var result = DeclareLocal(typeof(ScriptDatum));
            InitializeArgumentBuffer(arguments, count);
            EmitArgumentBuffer(call.Arguments, arguments, count);
            _il.Emit(OpCodes.Ldloc, receiverLocal);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Ldloc, arguments);
            _il.Emit(OpCodes.Ldloc, count);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.InvokePropertyMany);
            _il.Emit(OpCodes.Stloc, result);
            ReleaseArgumentBuffer(arguments, count);
            _il.Emit(OpCodes.Ldloc, result);
            return StackValueKind.Datum;
        }

        private StackValueKind EmitArrayPushCall(
            FunctionCallExpression call,
            Expression receiverExpression)
        {
            var receiver = DeclareLocal(typeof(ScriptArray));
            EmitArrayReference(receiverExpression);
            _il.Emit(OpCodes.Stloc, receiver);

            var arguments = new LocalBuilder[call.Arguments.Count];
            for (var i = 0; i < arguments.Length; i++)
            {
                arguments[i] = DeclareLocal(typeof(ScriptDatum));
                EmitDatum(call.Arguments[i]);
                _il.Emit(OpCodes.Stloc, arguments[i]);
            }

            var fallback = _il.DefineLabel();
            var end = _il.DefineLabel();
            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArrayHasOwnPushProperty);
            _il.Emit(OpCodes.Brtrue, fallback);

            for (var i = 0; i < arguments.Length; i++)
            {
                _il.Emit(OpCodes.Ldloc, receiver);
                _il.Emit(OpCodes.Ldloc, arguments[i]);
                _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArrayPush);
            }
            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArrayLength);
            _il.Emit(OpCodes.Conv_R8);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromNumber);
            _il.Emit(OpCodes.Br, end);

            _il.MarkLabel(fallback);
            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, "push");
            for (var i = 0; i < arguments.Length; i++)
            {
                _il.Emit(OpCodes.Ldloc, arguments[i]);
            }
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.InvokeProperty[arguments.Length]);
            _il.MarkLabel(end);
            return StackValueKind.Datum;
        }

        private bool TryEmitArrayFactoryCall(
            FunctionCallExpression call,
            out StackValueKind result)
        {
            result = StackValueKind.Datum;
            if (!IsArrayFactoryCall(_code, call) || HasSpread(call.Arguments))
            {
                return false;
            }

            LocalBuilder capacity = null;
            if (call.Arguments.Count > 0)
            {
                capacity = DeclareLocal(typeof(ScriptDatum));
                EmitArrayFactoryCapacityDatum(call.Arguments[0]);
                _il.Emit(OpCodes.Stloc, capacity);
            }
            for (var i = 1; i < call.Arguments.Count; i++)
            {
                EmitExpression(call.Arguments[i]);
                _il.Emit(OpCodes.Pop);
            }

            if (capacity == null) EmitNull();
            else _il.Emit(OpCodes.Ldloc, capacity);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ScriptArrayCreateEmptyWithCapacity);
            result = StackValueKind.Array;
            return true;
        }

        private void EmitArgumentBuffer(
            IReadOnlyList<Expression> source,
            LocalBuilder arguments,
            LocalBuilder count)
        {
            EmitInt32(source.Count);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.RentArguments);
            _il.Emit(OpCodes.Stloc, arguments);
            EmitInt32(0);
            _il.Emit(OpCodes.Stloc, count);

            for (var i = 0; i < source.Count; i++)
            {
                _il.Emit(OpCodes.Ldloc, arguments);
                _il.Emit(OpCodes.Ldloca, count);
                if (source[i] is SpreadExpression spread)
                {
                    EmitDatum(spread.Expression);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.AppendSpread);
                }
                else
                {
                    EmitDatum(source[i]);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.AppendArgument);
                }
                _il.Emit(OpCodes.Stloc, arguments);
            }
        }

        private void InitializeArgumentBuffer(LocalBuilder arguments, LocalBuilder count)
        {
            if (!_hasArgumentBufferCleanup || _argumentBuffers == null)
            {
                throw new InvalidOperationException("Missing function-level argument-buffer cleanup region.");
            }
            _argumentBuffers.Add((arguments, count));

            // A script catch can consume an exception raised while this call site
            // owns a buffer. If the site is entered again (for example in a loop),
            // return that retained buffer before replacing the local. The function
            // cleanup region remains the backstop when control leaves immediately.
            _il.Emit(OpCodes.Ldloc, arguments);
            _il.Emit(OpCodes.Ldloc, count);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ReturnArguments);
            _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Stloc, arguments);
            EmitInt32(0);
            _il.Emit(OpCodes.Stloc, count);
        }

        private void ReleaseArgumentBuffer(LocalBuilder arguments, LocalBuilder count)
        {
            _il.Emit(OpCodes.Ldloc, arguments);
            _il.Emit(OpCodes.Ldloc, count);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ReturnArguments);
            _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Stloc, arguments);
        }

        private StackValueKind EmitNew(NewExpression expression)
        {
            var call = expression.Expression;
            var resultType = _code.GetExpressionType(expression);
            if (FlowValueTypeFacts.IsPackedArray(resultType) &&
                call.Arguments.Count <= 1 &&
                !HasSpread(call.Arguments))
            {
                if (call.Arguments.Count == 0) _il.Emit(OpCodes.Ldc_R8, 0d);
                else EmitNumber(call.Arguments[0]);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ValidatePackedArrayLength);
                if (_directMode)
                {
                    _il.Emit(OpCodes.Newarr, GetPackedStorageType(resultType).GetElementType());
                }
                else
                {
                    _il.Emit(OpCodes.Newobj, GetPackedConstructor(resultType));
                }
                return GetPackedLocalStackKind(resultType);
            }

            if (resultType == FlowValueType.Array &&
                IsArrayConstruction(_code, expression) &&
                !HasSpread(call.Arguments))
            {
                if (call.Arguments.Count == 1)
                {
                    EmitArrayLengthDatum(call.Arguments[0]);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ScriptArrayCreateWithLength);
                }
                else
                {
                    for (var i = 0; i < call.Arguments.Count; i++)
                    {
                        EmitExpression(call.Arguments[i]);
                        _il.Emit(OpCodes.Pop);
                    }
                    EmitInt32(0);
                    _il.Emit(OpCodes.Newobj, TypedRuntimeMetadata.ScriptArrayCapacity);
                }
                return StackValueKind.Array;
            }

            var hasSpread = HasSpread(call.Arguments);
            if (!hasSpread && call.Arguments.Count <= 2)
            {
                EmitDatum(call.Target);
                _il.Emit(OpCodes.Ldarg_0);
                for (var i = 0; i < call.Arguments.Count; i++) EmitDatum(call.Arguments[i]);
                _il.Emit(OpCodes.Call, call.Arguments.Count == 0
                    ? TypedRuntimeMetadata.New0
                    : call.Arguments.Count == 1
                        ? TypedRuntimeMetadata.New1
                        : TypedRuntimeMetadata.New2);
                return StackValueKind.Datum;
            }

            var constructor = DeclareLocal(typeof(ScriptDatum));
            EmitDatum(call.Target);
            _il.Emit(OpCodes.Stloc, constructor);
            var arguments = DeclareLocal(typeof(ScriptDatum[]));
            var count = DeclareLocal(typeof(int));
            var result = DeclareLocal(typeof(ScriptDatum));
            InitializeArgumentBuffer(arguments, count);
            EmitArgumentBuffer(call.Arguments, arguments, count);
            _il.Emit(OpCodes.Ldloc, constructor);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldloc, arguments);
            _il.Emit(OpCodes.Ldloc, count);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.NewMany);
            _il.Emit(OpCodes.Stloc, result);
            ReleaseArgumentBuffer(arguments, count);
            _il.Emit(OpCodes.Ldloc, result);
            return StackValueKind.Datum;
        }

        private void EmitArrayLengthDatum(Expression expression)
        {
            var type = _code.GetExpressionType(expression);
            if ((type & (FlowValueType.Int32 | FlowValueType.Number)) != 0 ||
                type == FlowValueType.Dynamic)
            {
                EmitDatum(expression);
                return;
            }

            EmitExpression(expression);
            _il.Emit(OpCodes.Pop);
            EmitNull();
        }

        private void EmitArrayFactoryCapacityDatum(Expression expression)
        {
            if (_directMode &&
                FlowValueTypeFacts.IsPackedArray(_code.GetExpressionType(expression)))
            {
                EmitExpression(expression);
                _il.Emit(OpCodes.Pop);
                EmitNull();
                return;
            }
            EmitDatum(expression);
        }

        private void EmitFunctionDeclaration(FunctionDeclaration declaration)
        {
            if (declaration == null || declaration.Flags == FunctionFlags.Declare) return;
            if (!_functionsByDeclaration.TryGetValue(declaration, out var function) ||
                !ClosureMaterializer.CanMaterialize(function, requireName: false))
            {
                throw new NotSupportedException("Nested function is not materialized.");
            }
            var slot = FindLocal(declaration.Name?.Value, declaration);
            if (!slot.IsValid) throw new InvalidOperationException("Unbound nested function.");
            ClosureMaterializer.EmitClosure(_session, _il, function, EmitClosureUpvalue);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
            EmitStoreLocalFromStack(slot);
        }

        private StackValueKind EmitLambda(LambdaExpression expression)
        {
            if (expression?.Function == null ||
                !_functionsByDeclaration.TryGetValue(expression.Function, out var function) ||
                !ClosureMaterializer.CanMaterialize(function, requireName: false))
            {
                throw new NotSupportedException("Lambda is not materialized.");
            }
            ClosureMaterializer.EmitClosure(_session, _il, function, EmitClosureUpvalue);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
            return StackValueKind.Datum;
        }

        private static bool HasSpread(IReadOnlyList<Expression> expressions)
        {
            for (var i = 0; i < expressions.Count; i++)
            {
                if (expressions[i] is SpreadExpression) return true;
            }
            return false;
        }

        private static bool IsArrayConstruction(TypedFunctionCode code, NewExpression expression)
        {
            if (expression?.Expression?.Target is not NameExpression name)
            {
                return false;
            }
            var binding = code.GetName(name);
            return binding.IsUnshadowedGlobal &&
                StringComparer.Ordinal.Equals(binding.Name, "Array");
        }

        private static bool IsArrayFactoryCall(TypedFunctionCode code, FunctionCallExpression expression)
        {
            if (expression?.Target is not GetPropertyExpression property ||
                !TryGetStaticPropertyName(property.Property, out var propertyName) ||
                !StringComparer.Ordinal.Equals(propertyName, "withCapacity") ||
                property.Object is not NameExpression name)
            {
                return false;
            }
            var binding = code.GetName(name);
            return binding.IsUnshadowedGlobal &&
                StringComparer.Ordinal.Equals(binding.Name, "Array");
        }

        private StackValueKind EmitLiteral(LiteralExpression literal)
        {
            switch (literal.Token)
            {
                case NumberToken number:
                    if (_code.GetExpressionType(literal) == FlowValueType.Int32)
                    {
                        EmitInt32((int)number.NumberValue);
                        return StackValueKind.Int32;
                    }
                    _il.Emit(OpCodes.Ldc_R8, number.NumberValue);
                    return StackValueKind.Number;
                case BooleanToken boolean:
                    _il.Emit(boolean.BoolValue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    return StackValueKind.Boolean;
                case StringToken text:
                    _session.Builder.LoadStringConstant(_il, text.Value ?? string.Empty);
                    return StackValueKind.String;
                case NullToken:
                    EmitNull();
                    return StackValueKind.Datum;
                case RegexToken regex:
                    _session.Builder.LoadStringConstant(_il, regex.Pattern);
                    _session.Builder.LoadStringConstant(_il, regex.Flags);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ResolveRegex);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
                    return StackValueKind.Datum;
                default:
                    throw new NotSupportedException("Typed literal: " + literal.Token.GetType().Name);
            }
        }

        private StackValueKind EmitName(NameExpression expression)
        {
            var binding = _code.GetName(expression);
            if (binding.HasConstant)
            {
                return EmitConstant(binding.Constant, _code.GetExpressionType(expression));
            }
            if (!binding.IsLocal)
            {
                if (StringComparer.Ordinal.Equals(binding.Name, "$args"))
                {
                    if (_convention != FunctionCallConvention.Span)
                    {
                        throw new InvalidOperationException("$args requires the span convention.");
                    }
                    _il.Emit(OpCodes.Ldarg_1);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetArgumentsArray);
                    return StackValueKind.Datum;
                }
                if (StringComparer.Ordinal.Equals(binding.Name, "$state"))
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetUserState);
                    return StackValueKind.Datum;
                }
                if (StringComparer.Ordinal.Equals(binding.Name, "global"))
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetGlobalObject);
                    return StackValueKind.Datum;
                }
                if (binding.Upvalue.IsValid)
                {
                    EmitLoadUpvalue(binding.Upvalue);
                    return StackValueKind.Datum;
                }

                _il.Emit(OpCodes.Ldarg_0);
                _session.Builder.LoadStringConstant(_il, binding.Name);
                _il.Emit(OpCodes.Call, IsModuleBinding(binding)
                    ? TypedRuntimeMetadata.GetModule
                    : TypedRuntimeMetadata.GetGlobal);
                return StackValueKind.Datum;
            }

            EmitLoadLocal(binding.Local);
            return _code.GetLocalType(binding.Local) switch
            {
                FlowValueType.Int32 => StackValueKind.Int32,
                FlowValueType.Number => StackValueKind.Number,
                FlowValueType.Boolean => StackValueKind.Boolean,
                FlowValueType.String => StackValueKind.String,
                FlowValueType.Array => StackValueKind.Array,
                FlowValueType.Int32Array => GetPackedLocalStackKind(FlowValueType.Int32Array),
                FlowValueType.Int8Array => GetPackedLocalStackKind(FlowValueType.Int8Array),
                FlowValueType.Float64Array => GetPackedLocalStackKind(FlowValueType.Float64Array),
                FlowValueType.BooleanArray => GetPackedLocalStackKind(FlowValueType.BooleanArray),
                FlowValueType.UInt8Array => GetPackedLocalStackKind(FlowValueType.UInt8Array),
                FlowValueType.Int16Array => GetPackedLocalStackKind(FlowValueType.Int16Array),
                FlowValueType.UInt16Array => GetPackedLocalStackKind(FlowValueType.UInt16Array),
                FlowValueType.UInt32Array => GetPackedLocalStackKind(FlowValueType.UInt32Array),
                FlowValueType.Int64Array => GetPackedLocalStackKind(FlowValueType.Int64Array),
                FlowValueType.UInt64Array => GetPackedLocalStackKind(FlowValueType.UInt64Array),
                _ => StackValueKind.Datum,
            };
        }

        private StackValueKind EmitConstant(ScriptDatum value, FlowValueType type)
        {
            switch (value.Kind)
            {
                case ValueKind.Null:
                    EmitNull();
                    return StackValueKind.Datum;
                case ValueKind.Boolean:
                    _il.Emit(value.Boolean ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    return StackValueKind.Boolean;
                case ValueKind.Number:
                    if (type == FlowValueType.Int32)
                    {
                        EmitInt32((int)value.Number);
                        return StackValueKind.Int32;
                    }
                    _il.Emit(OpCodes.Ldc_R8, value.Number);
                    return StackValueKind.Number;
                case ValueKind.String:
                    _session.Builder.LoadStringConstant(_il, value.StringText ?? string.Empty);
                    return StackValueKind.String;
                default:
                    throw new NotSupportedException("Object constants require a runtime slot.");
            }
        }

        private StackValueKind EmitBinary(BinaryExpression binary)
        {
            var op = binary.Operator;
            if (op == Operator.LogicalAnd || op == Operator.LogicalOr)
            {
                return EmitLogical(binary, op == Operator.LogicalOr);
            }

            if (op == Operator.Add && TryEmitStringAddition(binary))
            {
                return StackValueKind.Datum;
            }

            if ((op == Operator.Add || op == Operator.Subtract || op == Operator.Multiply) &&
                _code.GetExpressionType(binary) == FlowValueType.Int32)
            {
                EmitInt32Value(binary.Left);
                EmitInt32Value(binary.Right);
                _il.Emit(op == Operator.Add ? OpCodes.Add :
                    op == Operator.Subtract ? OpCodes.Sub : OpCodes.Mul);
                return StackValueKind.Int32;
            }

            if (op == Operator.Add && _code.GetExpressionType(binary) == FlowValueType.Number)
            {
                EmitArithmeticNumber(binary.Left);
                EmitArithmeticNumber(binary.Right);
                _il.Emit(OpCodes.Add);
                return StackValueKind.Number;
            }

            if (op == Operator.Subtract || op == Operator.Multiply ||
                op == Operator.Divide || op == Operator.Modulo)
            {
                EmitArithmeticNumber(binary.Left);
                EmitArithmeticNumber(binary.Right);
                _il.Emit(op == Operator.Subtract ? OpCodes.Sub :
                    op == Operator.Multiply ? OpCodes.Mul :
                    op == Operator.Divide ? OpCodes.Div : OpCodes.Rem);
                return StackValueKind.Number;
            }

            if (IsComparison(op))
            {
                EmitComparison(binary);
                return StackValueKind.Boolean;
            }

            if (IsBitwise(op))
            {
                return EmitBitwise(binary);
            }

            EmitDatum(binary.Left);
            EmitDatum(binary.Right);
            _il.Emit(OpCodes.Call, GetDynamicBinary(op));
            return StackValueKind.Datum;
        }

        private bool TryEmitStringAddition(BinaryExpression expression)
        {
            if (expression.Left is BinaryExpression left && left.Operator == Operator.Add &&
                TryGetStringLiteral(left.Right, out var middle))
            {
                EmitDatum(left.Left);
                _session.Builder.LoadStringConstant(_il, middle);
                EmitDatum(expression.Right);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.AddStringMiddle);
                return true;
            }
            if (TryGetStringLiteral(expression.Right, out var right))
            {
                EmitDatum(expression.Left);
                _session.Builder.LoadStringConstant(_il, right);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.AddStringRight);
                return true;
            }
            if (TryGetStringLiteral(expression.Left, out var leftText))
            {
                _session.Builder.LoadStringConstant(_il, leftText);
                EmitDatum(expression.Right);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.AddStringLeft);
                return true;
            }
            return false;
        }

        private static bool TryGetStringLiteral(Expression expression, out string value)
        {
            if (expression is LiteralExpression { Token: StringToken text })
            {
                value = text.Value ?? string.Empty;
                return true;
            }
            value = null;
            return false;
        }

        private StackValueKind EmitLogical(BinaryExpression binary, bool branchWhenTrue)
        {
            var result = DeclareLocal(typeof(ScriptDatum));
            var end = _il.DefineLabel();
            EmitDatum(binary.Left);
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, result);
            if (TryGetCachedBoolean(binary.Left, out var cachedBoolean))
            {
                _il.Emit(OpCodes.Pop);
                _il.Emit(OpCodes.Ldloc, cachedBoolean);
            }
            else
            {
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToBooleanDatum);
            }
            _il.Emit(branchWhenTrue ? OpCodes.Brtrue : OpCodes.Brfalse, end);
            EmitDatum(binary.Right);
            _il.Emit(OpCodes.Stloc, result);
            _il.MarkLabel(end);
            _il.Emit(OpCodes.Ldloc, result);
            return StackValueKind.Datum;
        }

        private void EmitComparison(BinaryExpression binary)
        {
            var op = binary.Operator;
            if (TryEmitCachedParameterComparison(binary))
            {
                return;
            }
            var leftType = _code.GetExpressionType(binary.Left);
            var rightType = _code.GetExpressionType(binary.Right);
            if (leftType == FlowValueType.Int32 && rightType == FlowValueType.Int32)
            {
                EmitInt32Value(binary.Left);
                EmitInt32Value(binary.Right);
                EmitNativeInt32Comparison(op);
                return;
            }
            if (FlowValueTypeFacts.IsNumeric(leftType) &&
                FlowValueTypeFacts.IsNumeric(rightType))
            {
                EmitNumber(binary.Left);
                EmitNumber(binary.Right);
                EmitNativeNumberComparison(op);
                return;
            }

            EmitDatum(binary.Left);
            EmitDatum(binary.Right);
            _il.Emit(OpCodes.Call, GetComparison(op));
        }

        private bool TryEmitCachedParameterComparison(BinaryExpression binary)
        {
            var leftCached = TryGetCachedParameter(binary.Left, out var leftValue, out var leftValid);
            var rightCached = TryGetCachedParameter(binary.Right, out var rightValue, out var rightValid);
            if ((binary.Operator == Operator.Equal || binary.Operator == Operator.NotEqual) &&
                leftCached && rightCached)
            {
                // Same-kind strings use ordinal equality, so two dynamically typed
                // parameters cannot be reduced to their numeric coercions alone.
                return false;
            }
            if ((!leftCached && !FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(binary.Left))) ||
                (!rightCached && !FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(binary.Right))) ||
                (!leftCached && !rightCached))
            {
                return false;
            }

            var leftTemp = DeclareLocal(typeof(double));
            var rightTemp = DeclareLocal(typeof(double));
            if (leftCached) _il.Emit(OpCodes.Ldloc, leftValue);
            else EmitNumber(binary.Left);
            _il.Emit(OpCodes.Stloc, leftTemp);
            if (rightCached) _il.Emit(OpCodes.Ldloc, rightValue);
            else EmitNumber(binary.Right);
            _il.Emit(OpCodes.Stloc, rightTemp);

            var invalid = _il.DefineLabel();
            var end = _il.DefineLabel();
            if (leftCached)
            {
                _il.Emit(OpCodes.Ldloc, leftValid);
                _il.Emit(OpCodes.Brfalse, invalid);
            }
            if (rightCached)
            {
                _il.Emit(OpCodes.Ldloc, rightValid);
                _il.Emit(OpCodes.Brfalse, invalid);
            }
            _il.Emit(OpCodes.Ldloc, leftTemp);
            _il.Emit(OpCodes.Ldloc, rightTemp);
            EmitNativeNumberComparison(binary.Operator);
            _il.Emit(OpCodes.Br, end);
            _il.MarkLabel(invalid);
            _il.Emit(binary.Operator == Operator.NotEqual ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            _il.MarkLabel(end);
            return true;
        }

        private bool TryGetCachedParameter(
            Expression expression,
            out LocalBuilder value,
            out LocalBuilder valid)
        {
            if (expression is NameExpression name)
            {
                var binding = _code.GetName(name);
                if (binding.IsLocal &&
                    (uint)binding.Local.Value < (uint)_parameterNumbers.Length &&
                    _parameterNumbers[binding.Local.Value] != null)
                {
                    value = _parameterNumbers[binding.Local.Value];
                    valid = _parameterNumberValid[binding.Local.Value];
                    return true;
                }
            }

            value = null;
            valid = null;
            return false;
        }

        private bool TryEmitCachedNumber(Expression expression)
        {
            if (!TryGetCachedParameter(expression, out var value, out _))
            {
                return false;
            }
            _il.Emit(OpCodes.Ldloc, value);
            return true;
        }

        private bool TryGetCachedBoolean(Expression expression, out LocalBuilder value)
        {
            if (expression is NameExpression name)
            {
                var binding = _code.GetName(name);
                if (binding.IsLocal &&
                    (uint)binding.Local.Value < (uint)_parameterBooleans.Length &&
                    _parameterBooleans[binding.Local.Value] != null)
                {
                    value = _parameterBooleans[binding.Local.Value];
                    return true;
                }
            }
            value = null;
            return false;
        }

        private void EmitNativeNumberComparison(Operator op)
        {
            if (op == Operator.Equal)
            {
                _il.Emit(OpCodes.Ceq);
            }
            else if (op == Operator.NotEqual)
            {
                _il.Emit(OpCodes.Ceq);
                EmitBooleanNot();
            }
            else if (op == Operator.LessThan)
            {
                _il.Emit(OpCodes.Clt);
            }
            else if (op == Operator.GreaterThan)
            {
                _il.Emit(OpCodes.Cgt);
            }
            else if (op == Operator.LessThanOrEqual)
            {
                _il.Emit(OpCodes.Cgt_Un);
                EmitBooleanNot();
            }
            else
            {
                _il.Emit(OpCodes.Clt_Un);
                EmitBooleanNot();
            }
        }

        private void EmitNativeInt32Comparison(Operator op)
        {
            if (op == Operator.Equal)
            {
                _il.Emit(OpCodes.Ceq);
            }
            else if (op == Operator.NotEqual)
            {
                _il.Emit(OpCodes.Ceq);
                EmitBooleanNot();
            }
            else if (op == Operator.LessThan)
            {
                _il.Emit(OpCodes.Clt);
            }
            else if (op == Operator.GreaterThan)
            {
                _il.Emit(OpCodes.Cgt);
            }
            else if (op == Operator.LessThanOrEqual)
            {
                _il.Emit(OpCodes.Cgt);
                EmitBooleanNot();
            }
            else
            {
                _il.Emit(OpCodes.Clt);
                EmitBooleanNot();
            }
        }

        private StackValueKind EmitBitwise(BinaryExpression binary)
        {
            var op = binary.Operator;
            if (FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(binary.Left)) &&
                FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(binary.Right)))
            {
                var truncateThroughInt64 = op == Operator.BitwiseAnd ||
                    op == Operator.BitwiseOr || op == Operator.BitwiseXor;
                EmitInt32Operand(binary.Left, truncateThroughInt64);
                EmitInt32Operand(binary.Right, truncateThroughInt64);
                _il.Emit(op == Operator.BitwiseAnd ? OpCodes.And :
                    op == Operator.BitwiseOr ? OpCodes.Or :
                    op == Operator.BitwiseXor ? OpCodes.Xor :
                    op == Operator.LeftShift ? OpCodes.Shl :
                    op == Operator.SignedRightShift ? OpCodes.Shr : OpCodes.Shr_Un);
                if (op == Operator.UnSignedRightShift)
                {
                    _il.Emit(OpCodes.Conv_U4);
                    _il.Emit(OpCodes.Conv_R_Un);
                    return StackValueKind.Number;
                }
                if (_code.GetExpressionType(binary) == FlowValueType.Int32)
                {
                    return StackValueKind.Int32;
                }
                _il.Emit(OpCodes.Conv_R8);
                return StackValueKind.Number;
            }

            EmitDatum(binary.Left);
            EmitDatum(binary.Right);
            _il.Emit(OpCodes.Call, GetDynamicBinary(op));
            if (_code.GetExpressionType(binary) == FlowValueType.Int32)
            {
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToArithmeticNumber);
                _il.Emit(OpCodes.Conv_I8);
                _il.Emit(OpCodes.Conv_I4);
                return StackValueKind.Int32;
            }
            if (_code.GetExpressionType(binary) == FlowValueType.Number)
            {
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToArithmeticNumber);
                return StackValueKind.Number;
            }
            return StackValueKind.Datum;
        }

        private void EmitInt32Operand(Expression expression, bool truncateThroughInt64)
        {
            if (TryEmitCachedNumber(expression))
            {
                ConvertStackToInt32(StackValueKind.Number, truncateThroughInt64);
                return;
            }
            var kind = EmitExpression(expression);
            ConvertStackToInt32(kind, truncateThroughInt64);
        }

        private StackValueKind EmitAssignment(AssignmentExpression assignment)
        {
            if (assignment.Left is not NameExpression name)
            {
                throw new NotSupportedException("Typed assignment target.");
            }
            var binding = _code.GetName(name);
            if (!binding.IsLocal)
            {
                if (binding.Upvalue.IsValid)
                {
                    var value = DeclareLocal(typeof(ScriptDatum));
                    EmitDatum(assignment.Right);
                    _il.Emit(OpCodes.Stloc, value);
                    EmitStoreUpvalue(binding.Upvalue, value);
                    _il.Emit(OpCodes.Ldloc, value);
                    return StackValueKind.Datum;
                }
                _il.Emit(OpCodes.Ldarg_0);
                _session.Builder.LoadStringConstant(_il, binding.Name);
                EmitDatum(assignment.Right);
                _il.Emit(OpCodes.Call, IsModuleBinding(binding)
                    ? TypedRuntimeMetadata.SetModule
                    : TypedRuntimeMetadata.SetGlobal);
                return StackValueKind.Datum;
            }

            var localType = _code.GetLocalType(binding.Local);
            if (localType == FlowValueType.Int32)
            {
                EmitInt32Value(assignment.Right);
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Stloc, _locals[binding.Local.Value]);
                return StackValueKind.Int32;
            }
            if (localType == FlowValueType.Number)
            {
                EmitNumber(assignment.Right);
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Stloc, _locals[binding.Local.Value]);
                return StackValueKind.Number;
            }
            if (localType == FlowValueType.Boolean)
            {
                EmitCondition(assignment.Right);
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Stloc, _locals[binding.Local.Value]);
                return StackValueKind.Boolean;
            }
            if (localType == FlowValueType.String)
            {
                EmitString(assignment.Right);
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Stloc, _locals[binding.Local.Value]);
                return StackValueKind.String;
            }
            if (localType == FlowValueType.Array)
            {
                EmitArrayReference(assignment.Right);
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Stloc, _locals[binding.Local.Value]);
                return StackValueKind.Array;
            }
            if (FlowValueTypeFacts.IsPackedArray(localType))
            {
                EmitPackedArrayReference(assignment.Right, localType);
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Stloc, _locals[binding.Local.Value]);
                return GetPackedLocalStackKind(localType);
            }

            EmitDatum(assignment.Right);
            _il.Emit(OpCodes.Dup);
            EmitStoreLocalFromStack(binding.Local);
            return StackValueKind.Datum;
        }

        private StackValueKind EmitCompound(CompoundExpression expression)
        {
            var op = expression.Operator.SimplerOperator;
            if (expression.Left is NameExpression name)
            {
                var binding = _code.GetName(name);
                if (binding.IsLocal &&
                    _code.GetLocalType(binding.Local) == FlowValueType.Int32 &&
                    _code.GetExpressionType(expression) == FlowValueType.Int32)
                {
                    EmitInt32Binary(op, name, expression.Right);
                    _il.Emit(OpCodes.Dup);
                    _il.Emit(OpCodes.Stloc, _locals[binding.Local.Value]);
                    return StackValueKind.Int32;
                }
                if (binding.IsLocal && _code.GetLocalType(binding.Local) == FlowValueType.Number)
                {
                    EmitNumericBinary(op, name, expression.Right);
                    _il.Emit(OpCodes.Dup);
                    _il.Emit(OpCodes.Stloc, _locals[binding.Local.Value]);
                    return StackValueKind.Number;
                }

                var result = DeclareLocal(typeof(ScriptDatum));
                EmitDatum(name);
                EmitDatum(expression.Right);
                _il.Emit(OpCodes.Call, GetDynamicBinary(op));
                _il.Emit(OpCodes.Stloc, result);
                EmitStoreBoundName(binding, result);
                _il.Emit(OpCodes.Ldloc, result);
                return StackValueKind.Datum;
            }

            if (expression.Left is GetPropertyExpression property &&
                TryGetStaticPropertyName(property.Property, out var propertyName))
            {
                var receiver = DeclareLocal(typeof(ScriptDatum));
                EmitDatum(property.Object);
                _il.Emit(OpCodes.Stloc, receiver);
                _il.Emit(OpCodes.Ldloc, receiver);
                _il.Emit(OpCodes.Ldarg_0);
                _session.Builder.LoadStringConstant(_il, propertyName);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetProperty);
                EmitDatum(expression.Right);
                _il.Emit(OpCodes.Call, GetDynamicBinary(op));
                var result = DeclareLocal(typeof(ScriptDatum));
                _il.Emit(OpCodes.Stloc, result);
                _il.Emit(OpCodes.Ldloc, receiver);
                _il.Emit(OpCodes.Ldarg_0);
                _session.Builder.LoadStringConstant(_il, propertyName);
                _il.Emit(OpCodes.Ldloc, result);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.SetProperty);
                return StackValueKind.Datum;
            }

            if (expression.Left is GetElementExpression element)
            {
                var arrayType = _code.GetExpressionType(element.Object);
                if (arrayType == FlowValueType.Array &&
                    FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(element.Index)))
                {
                    return EmitArrayCompound(expression, element, op);
                }
                if (FlowValueTypeFacts.IsPackedArray(arrayType) &&
                    FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(element.Index)) &&
                    FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(expression)))
                {
                    return EmitPackedNumericCompound(expression, element, arrayType, op);
                }

                var receiver = DeclareLocal(typeof(ScriptDatum));
                var numberIndex = FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(element.Index));
                var index = DeclareLocal(numberIndex ? typeof(double) : typeof(ScriptDatum));
                EmitDatum(element.Object);
                _il.Emit(OpCodes.Stloc, receiver);
                if (numberIndex) EmitNumber(element.Index);
                else EmitDatum(element.Index);
                _il.Emit(OpCodes.Stloc, index);
                _il.Emit(OpCodes.Ldloc, receiver);
                _il.Emit(OpCodes.Ldloc, index);
                _il.Emit(OpCodes.Call, numberIndex
                    ? TypedRuntimeMetadata.GetElementNumber
                    : TypedRuntimeMetadata.GetElement);
                EmitDatum(expression.Right);
                _il.Emit(OpCodes.Call, GetDynamicBinary(op));
                var result = DeclareLocal(typeof(ScriptDatum));
                _il.Emit(OpCodes.Stloc, result);
                _il.Emit(OpCodes.Ldloc, receiver);
                _il.Emit(OpCodes.Ldloc, index);
                _il.Emit(OpCodes.Ldloc, result);
                _il.Emit(OpCodes.Call, numberIndex
                    ? TypedRuntimeMetadata.SetElementNumber
                    : TypedRuntimeMetadata.SetElement);
                return StackValueKind.Datum;
            }

            throw new NotSupportedException("Typed compound target.");
        }

        private StackValueKind EmitArrayCompound(
            CompoundExpression expression,
            GetElementExpression element,
            Operator op)
        {
            var receiver = DeclareLocal(typeof(ScriptArray));
            EmitArrayReference(element.Object);
            _il.Emit(OpCodes.Stloc, receiver);

            var index = DeclareLocal(typeof(int));
            EmitInt32Value(element.Index);
            _il.Emit(OpCodes.Stloc, index);

            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldloc, index);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArrayGetElement);
            EmitDatum(expression.Right);
            _il.Emit(OpCodes.Call, GetDynamicBinary(op));
            var result = DeclareLocal(typeof(ScriptDatum));
            _il.Emit(OpCodes.Stloc, result);

            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldloc, index);
            _il.Emit(OpCodes.Ldloc, result);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArraySetElement);
            _il.Emit(OpCodes.Ldloc, result);
            return StackValueKind.Datum;
        }

        private StackValueKind EmitPackedNumericCompound(
            CompoundExpression expression,
            GetElementExpression element,
            FlowValueType arrayType,
            Operator op)
        {
            var receiver = DeclareLocal(GetPackedStorageType(arrayType));
            EmitPackedArrayStorage(element.Object, arrayType);
            _il.Emit(OpCodes.Stloc, receiver);

            var index = DeclareLocal(typeof(int));
            EmitInt32Value(element.Index);
            _il.Emit(OpCodes.Stloc, index);

            EmitPackedNumericElement(receiver, index, arrayType);
            EmitNumericBinaryRight(op, expression.Right);
            var result = DeclareLocal(typeof(double));
            _il.Emit(OpCodes.Stloc, result);

            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldloc, index);
            _il.Emit(OpCodes.Ldloc, result);
            EmitPackedNumberStore(arrayType);

            _il.Emit(OpCodes.Ldloc, result);
            return StackValueKind.Number;
        }

        private void EmitPackedNumericElement(
            LocalBuilder receiver,
            LocalBuilder index,
            FlowValueType arrayType)
        {
            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldloc, index);
            _il.Emit(arrayType switch
            {
                FlowValueType.Int32Array => OpCodes.Ldelem_I4,
                FlowValueType.Int8Array => OpCodes.Ldelem_I1,
                FlowValueType.Float64Array => OpCodes.Ldelem_R8,
                FlowValueType.BooleanArray => OpCodes.Ldelem_U1,
                FlowValueType.UInt8Array => OpCodes.Ldelem_U1,
                FlowValueType.Int16Array => OpCodes.Ldelem_I2,
                FlowValueType.UInt16Array => OpCodes.Ldelem_U2,
                FlowValueType.UInt32Array => OpCodes.Ldelem_U4,
                FlowValueType.Int64Array => OpCodes.Ldelem_I8,
                FlowValueType.UInt64Array => OpCodes.Ldelem_I8,
                _ => throw new NotSupportedException("Unknown packed-array element type.")
            });
            if (arrayType == FlowValueType.Int64Array)
            {
                _il.Emit(OpCodes.Ldloc, index);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToExactInt64Number);
            }
            else if (arrayType == FlowValueType.UInt64Array)
            {
                _il.Emit(OpCodes.Conv_U8);
                _il.Emit(OpCodes.Ldloc, index);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToExactUInt64Number);
            }
            else if (arrayType != FlowValueType.Float64Array)
            {
                _il.Emit(OpCodes.Conv_R8);
            }
        }

        private void EmitNumericBinaryRight(Operator op, Expression right)
        {
            if (op == Operator.Add || op == Operator.Subtract || op == Operator.Multiply ||
                op == Operator.Divide || op == Operator.Modulo)
            {
                EmitNumber(right);
                _il.Emit(op == Operator.Add ? OpCodes.Add :
                    op == Operator.Subtract ? OpCodes.Sub :
                    op == Operator.Multiply ? OpCodes.Mul :
                    op == Operator.Divide ? OpCodes.Div : OpCodes.Rem);
                return;
            }
            if (IsBitwise(op))
            {
                var truncateThroughInt64 = op == Operator.BitwiseAnd ||
                    op == Operator.BitwiseOr || op == Operator.BitwiseXor;
                if (truncateThroughInt64) _il.Emit(OpCodes.Conv_I8);
                _il.Emit(OpCodes.Conv_I4);
                EmitInt32Operand(right, truncateThroughInt64);
                _il.Emit(op == Operator.BitwiseAnd ? OpCodes.And :
                    op == Operator.BitwiseOr ? OpCodes.Or :
                    op == Operator.BitwiseXor ? OpCodes.Xor :
                    op == Operator.LeftShift ? OpCodes.Shl :
                    op == Operator.SignedRightShift ? OpCodes.Shr : OpCodes.Shr_Un);
                if (op == Operator.UnSignedRightShift)
                {
                    _il.Emit(OpCodes.Conv_U4);
                    _il.Emit(OpCodes.Conv_R_Un);
                }
                else
                {
                    _il.Emit(OpCodes.Conv_R8);
                }
                return;
            }
            throw new NotSupportedException("Native packed-array compound operator.");
        }

        private void EmitNumericBinary(Operator op, Expression left, Expression right)
        {
            if (op == Operator.Add || op == Operator.Subtract || op == Operator.Multiply ||
                op == Operator.Divide || op == Operator.Modulo)
            {
                EmitNumber(left);
                EmitNumber(right);
                _il.Emit(op == Operator.Add ? OpCodes.Add :
                    op == Operator.Subtract ? OpCodes.Sub :
                    op == Operator.Multiply ? OpCodes.Mul :
                    op == Operator.Divide ? OpCodes.Div : OpCodes.Rem);
                return;
            }
            if (IsBitwise(op))
            {
                var truncateThroughInt64 = op == Operator.BitwiseAnd ||
                    op == Operator.BitwiseOr || op == Operator.BitwiseXor;
                EmitInt32Operand(left, truncateThroughInt64);
                EmitInt32Operand(right, truncateThroughInt64);
                _il.Emit(op == Operator.BitwiseAnd ? OpCodes.And :
                    op == Operator.BitwiseOr ? OpCodes.Or :
                    op == Operator.BitwiseXor ? OpCodes.Xor :
                    op == Operator.LeftShift ? OpCodes.Shl :
                    op == Operator.SignedRightShift ? OpCodes.Shr : OpCodes.Shr_Un);
                if (op == Operator.UnSignedRightShift)
                {
                    _il.Emit(OpCodes.Conv_U4);
                    _il.Emit(OpCodes.Conv_R_Un);
                }
                else
                {
                    _il.Emit(OpCodes.Conv_R8);
                }
                return;
            }
            throw new NotSupportedException("Native compound operator.");
        }

        private void EmitInt32Binary(Operator op, Expression left, Expression right)
        {
            if (op == Operator.Add || op == Operator.Subtract || op == Operator.Multiply)
            {
                EmitInt32Value(left);
                EmitInt32Value(right);
                _il.Emit(op == Operator.Add ? OpCodes.Add :
                    op == Operator.Subtract ? OpCodes.Sub : OpCodes.Mul);
                return;
            }
            if (op == Operator.BitwiseAnd || op == Operator.BitwiseOr ||
                op == Operator.BitwiseXor || op == Operator.LeftShift ||
                op == Operator.SignedRightShift)
            {
                EmitInt32Operand(left, truncateThroughInt64: false);
                EmitInt32Operand(right, truncateThroughInt64: false);
                _il.Emit(op == Operator.BitwiseAnd ? OpCodes.And :
                    op == Operator.BitwiseOr ? OpCodes.Or :
                    op == Operator.BitwiseXor ? OpCodes.Xor :
                    op == Operator.LeftShift ? OpCodes.Shl : OpCodes.Shr);
                return;
            }
            throw new NotSupportedException("Native Int32 compound operator.");
        }

        private void EmitStoreBoundName(BoundName binding, LocalBuilder value)
        {
            if (binding.IsLocal)
            {
                _il.Emit(OpCodes.Ldloc, value);
                switch (_code.GetLocalType(binding.Local))
                {
                    case FlowValueType.Int32:
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToArithmeticNumber);
                        _il.Emit(OpCodes.Conv_I4);
                        break;
                    case FlowValueType.Number:
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToArithmeticNumber);
                        break;
                    case FlowValueType.Boolean:
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToBooleanDatum);
                        break;
                    case FlowValueType.String:
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToString);
                        break;
                    case FlowValueType.Array:
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToObject);
                        _il.Emit(OpCodes.Castclass, typeof(ScriptArray));
                        break;
                    case FlowValueType.Int32Array:
                    case FlowValueType.Int8Array:
                    case FlowValueType.Float64Array:
                    case FlowValueType.BooleanArray:
                    case FlowValueType.UInt8Array:
                    case FlowValueType.Int16Array:
                    case FlowValueType.UInt16Array:
                    case FlowValueType.UInt32Array:
                    case FlowValueType.Int64Array:
                    case FlowValueType.UInt64Array:
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToObject);
                        _il.Emit(OpCodes.Castclass, GetPackedClrType(_code.GetLocalType(binding.Local)));
                        break;
                }
                EmitStoreLocalFromStack(binding.Local);
                return;
            }
            if (binding.Upvalue.IsValid)
            {
                EmitStoreUpvalue(binding.Upvalue, value);
                return;
            }
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, binding.Name);
            _il.Emit(OpCodes.Ldloc, value);
            _il.Emit(OpCodes.Call, IsModuleBinding(binding)
                ? TypedRuntimeMetadata.SetModule
                : TypedRuntimeMetadata.SetGlobal);
            _il.Emit(OpCodes.Pop);
        }

        private bool IsModuleBinding(BoundName binding)
        {
            return binding.ModuleSymbol.IsValid &&
                !_session.CompileSession.Symbols[binding.ModuleSymbol].HasFlag(BackendSymbolFlags.DeclaredOnly);
        }

        private StackValueKind EmitUnary(UnaryExpression unary)
        {
            var op = unary.Operator;
            if (op == Operator.PreIncrement || op == Operator.PostIncrement ||
                op == Operator.PreDecrement || op == Operator.PostDecrement)
            {
                return EmitNumericMutation(unary);
            }
            if (op == Operator.Negate)
            {
                if (_code.GetExpressionType(unary) == FlowValueType.Int32)
                {
                    EmitInt32Value(unary.Expression);
                    _il.Emit(OpCodes.Neg);
                    return StackValueKind.Int32;
                }
                EmitArithmeticNumber(unary.Expression);
                _il.Emit(OpCodes.Neg);
                return StackValueKind.Number;
            }
            if (op == Operator.LogicalNot)
            {
                EmitCondition(unary.Expression);
                EmitBooleanNot();
                return StackValueKind.Boolean;
            }
            if (op == Operator.BitwiseNot)
            {
                if (FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(unary.Expression)))
                {
                    EmitInt32Operand(unary.Expression, truncateThroughInt64: true);
                    _il.Emit(OpCodes.Not);
                }
                else
                {
                    EmitDatum(unary.Expression);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.BitwiseNot);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToArithmeticNumber);
                    _il.Emit(OpCodes.Conv_I8);
                    _il.Emit(OpCodes.Conv_I4);
                }
                return StackValueKind.Int32;
            }
            if (op == Operator.TypeOf)
            {
                EmitDatum(unary.Expression);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.TypeOf);
                return StackValueKind.Datum;
            }
            throw new NotSupportedException("Typed unary operator.");
        }

        private StackValueKind EmitNumericMutation(UnaryExpression unary)
        {
            if (unary.Expression is GetElementExpression element)
            {
                var arrayType = _code.GetExpressionType(element.Object);
                if (arrayType == FlowValueType.Array &&
                    FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(element.Index)))
                {
                    return EmitArrayNumericMutation(unary, element);
                }
                if (FlowValueTypeFacts.IsPackedArray(arrayType) &&
                    FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(element.Index)))
                {
                    return EmitPackedNumericMutation(unary, element, arrayType);
                }
                return EmitDynamicMutation(unary);
            }
            if (unary.Expression is not NameExpression name)
            {
                return EmitDynamicMutation(unary);
            }
            var binding = _code.GetName(name);
            if (!binding.IsLocal)
            {
                return EmitDynamicMutation(unary);
            }

            var increment = unary.Operator == Operator.PreIncrement || unary.Operator == Operator.PostIncrement;
            var postfix = unary.Operator == Operator.PostIncrement || unary.Operator == Operator.PostDecrement;
            if (_code.GetLocalType(binding.Local) == FlowValueType.Int32 &&
                _code.GetExpressionType(unary) == FlowValueType.Int32)
            {
                _il.Emit(OpCodes.Ldloc, _locals[binding.Local.Value]);
                if (postfix) _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Ldc_I4_1);
                _il.Emit(increment ? OpCodes.Add : OpCodes.Sub);
                if (!postfix) _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Stloc, _locals[binding.Local.Value]);
                return StackValueKind.Int32;
            }
            if (_code.GetLocalType(binding.Local) != FlowValueType.Number)
            {
                return EmitDynamicMutation(unary);
            }
            _il.Emit(OpCodes.Ldloc, _locals[binding.Local.Value]);
            if (postfix) _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_R8, 1d);
            _il.Emit(increment ? OpCodes.Add : OpCodes.Sub);
            if (!postfix) _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, _locals[binding.Local.Value]);
            return StackValueKind.Number;
        }

        private StackValueKind EmitPackedNumericMutation(
            UnaryExpression unary,
            GetElementExpression element,
            FlowValueType arrayType)
        {
            var receiver = DeclareLocal(GetPackedStorageType(arrayType));
            EmitPackedArrayStorage(element.Object, arrayType);
            _il.Emit(OpCodes.Stloc, receiver);

            var index = DeclareLocal(typeof(int));
            EmitInt32Value(element.Index);
            _il.Emit(OpCodes.Stloc, index);

            var previous = DeclareLocal(typeof(double));
            EmitPackedNumericElement(receiver, index, arrayType);
            _il.Emit(OpCodes.Stloc, previous);

            var current = DeclareLocal(typeof(double));
            _il.Emit(OpCodes.Ldloc, previous);
            _il.Emit(OpCodes.Ldc_R8,
                unary.Operator == Operator.PreIncrement || unary.Operator == Operator.PostIncrement
                    ? 1d
                    : -1d);
            _il.Emit(OpCodes.Add);
            _il.Emit(OpCodes.Stloc, current);

            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldloc, index);
            _il.Emit(OpCodes.Ldloc, current);
            EmitPackedNumberStore(arrayType);

            var postfix = unary.Operator == Operator.PostIncrement ||
                unary.Operator == Operator.PostDecrement;
            _il.Emit(OpCodes.Ldloc, postfix ? previous : current);
            return StackValueKind.Number;
        }

        private void EmitPackedNumberStore(FlowValueType arrayType)
        {
            switch (arrayType)
            {
                case FlowValueType.Float64Array:
                    _il.Emit(OpCodes.Stelem_R8);
                    return;
                case FlowValueType.BooleanArray:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToBooleanNumber);
                    _il.Emit(OpCodes.Stelem_I1);
                    return;
                case FlowValueType.Int8Array:
                    _il.Emit(OpCodes.Conv_I1);
                    _il.Emit(OpCodes.Stelem_I1);
                    return;
                case FlowValueType.UInt8Array:
                    _il.Emit(OpCodes.Conv_U1);
                    _il.Emit(OpCodes.Stelem_I1);
                    return;
                case FlowValueType.Int16Array:
                    _il.Emit(OpCodes.Conv_I2);
                    _il.Emit(OpCodes.Stelem_I2);
                    return;
                case FlowValueType.UInt16Array:
                    _il.Emit(OpCodes.Conv_U2);
                    _il.Emit(OpCodes.Stelem_I2);
                    return;
                case FlowValueType.UInt32Array:
                    _il.Emit(OpCodes.Conv_U4);
                    _il.Emit(OpCodes.Stelem_I4);
                    return;
                case FlowValueType.Int64Array:
                    _il.Emit(OpCodes.Conv_I8);
                    _il.Emit(OpCodes.Stelem_I8);
                    return;
                case FlowValueType.UInt64Array:
                    _il.Emit(OpCodes.Conv_U8);
                    _il.Emit(OpCodes.Stelem_I8);
                    return;
                case FlowValueType.Int32Array:
                    _il.Emit(OpCodes.Conv_I4);
                    _il.Emit(OpCodes.Stelem_I4);
                    return;
                default:
                    throw new NotSupportedException("Unknown packed-array storage type.");
            }
        }

        private StackValueKind EmitArrayNumericMutation(
            UnaryExpression unary,
            GetElementExpression element)
        {
            var receiver = DeclareLocal(typeof(ScriptArray));
            EmitArrayReference(element.Object);
            _il.Emit(OpCodes.Stloc, receiver);

            var index = DeclareLocal(typeof(int));
            EmitInt32Value(element.Index);
            _il.Emit(OpCodes.Stloc, index);

            var previous = DeclareLocal(typeof(ScriptDatum));
            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldloc, index);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArrayGetElement);
            _il.Emit(OpCodes.Stloc, previous);

            var current = DeclareLocal(typeof(ScriptDatum));
            _il.Emit(OpCodes.Ldloc, previous);
            _il.Emit(OpCodes.Ldc_R8,
                unary.Operator == Operator.PreIncrement || unary.Operator == Operator.PostIncrement
                    ? 1d
                    : -1d);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ChangeByOne);
            _il.Emit(OpCodes.Stloc, current);

            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldloc, index);
            _il.Emit(OpCodes.Ldloc, current);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArraySetElement);

            var postfix = unary.Operator == Operator.PostIncrement ||
                unary.Operator == Operator.PostDecrement;
            _il.Emit(OpCodes.Ldloc, postfix ? previous : current);
            return StackValueKind.Datum;
        }

        private StackValueKind EmitDynamicMutation(UnaryExpression unary)
        {
            var delta = unary.Operator == Operator.PreIncrement || unary.Operator == Operator.PostIncrement
                ? 1d
                : -1d;
            var postfix = unary.Operator == Operator.PostIncrement || unary.Operator == Operator.PostDecrement;
            var oldValue = DeclareLocal(typeof(ScriptDatum));
            var newValue = DeclareLocal(typeof(ScriptDatum));

            if (unary.Expression is NameExpression name)
            {
                var binding = _code.GetName(name);
                EmitDatum(name);
                _il.Emit(OpCodes.Stloc, oldValue);
                EmitChangedValue(oldValue, newValue, delta);
                EmitStoreBoundName(binding, newValue);
            }
            else if (unary.Expression is GetPropertyExpression property &&
                TryGetStaticPropertyName(property.Property, out var propertyName))
            {
                var receiver = DeclareLocal(typeof(ScriptDatum));
                EmitDatum(property.Object);
                _il.Emit(OpCodes.Stloc, receiver);
                _il.Emit(OpCodes.Ldloc, receiver);
                _il.Emit(OpCodes.Ldarg_0);
                _session.Builder.LoadStringConstant(_il, propertyName);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetProperty);
                _il.Emit(OpCodes.Stloc, oldValue);
                EmitChangedValue(oldValue, newValue, delta);
                _il.Emit(OpCodes.Ldloc, receiver);
                _il.Emit(OpCodes.Ldarg_0);
                _session.Builder.LoadStringConstant(_il, propertyName);
                _il.Emit(OpCodes.Ldloc, newValue);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.SetProperty);
                _il.Emit(OpCodes.Pop);
            }
            else if (unary.Expression is GetElementExpression element)
            {
                var receiver = DeclareLocal(typeof(ScriptDatum));
                var numberIndex = FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(element.Index));
                var index = DeclareLocal(numberIndex ? typeof(double) : typeof(ScriptDatum));
                EmitDatum(element.Object);
                _il.Emit(OpCodes.Stloc, receiver);
                if (numberIndex) EmitNumber(element.Index);
                else EmitDatum(element.Index);
                _il.Emit(OpCodes.Stloc, index);
                _il.Emit(OpCodes.Ldloc, receiver);
                _il.Emit(OpCodes.Ldloc, index);
                _il.Emit(OpCodes.Call, numberIndex
                    ? TypedRuntimeMetadata.GetElementNumber
                    : TypedRuntimeMetadata.GetElement);
                _il.Emit(OpCodes.Stloc, oldValue);
                EmitChangedValue(oldValue, newValue, delta);
                _il.Emit(OpCodes.Ldloc, receiver);
                _il.Emit(OpCodes.Ldloc, index);
                _il.Emit(OpCodes.Ldloc, newValue);
                _il.Emit(OpCodes.Call, numberIndex
                    ? TypedRuntimeMetadata.SetElementNumber
                    : TypedRuntimeMetadata.SetElement);
                _il.Emit(OpCodes.Pop);
            }
            else
            {
                throw new NotSupportedException("Typed mutation target.");
            }

            _il.Emit(OpCodes.Ldloc, postfix ? oldValue : newValue);
            return StackValueKind.Datum;
        }

        private void EmitChangedValue(LocalBuilder oldValue, LocalBuilder newValue, double delta)
        {
            _il.Emit(OpCodes.Ldloc, oldValue);
            _il.Emit(OpCodes.Ldc_R8, delta);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ChangeByOne);
            _il.Emit(OpCodes.Stloc, newValue);
        }

        private void EmitDatum(Expression expression)
        {
            var kind = EmitExpression(expression);
            ConvertToDatum(kind);
        }

        private void EmitPackedArrayReference(Expression expression, FlowValueType expectedType)
        {
            if (_directMode)
            {
                EmitPackedArrayStorage(expression, expectedType);
                return;
            }

            var kind = EmitExpression(expression);
            if (kind == GetPackedStackKind(expectedType)) return;

            if (kind == StackValueKind.Datum)
            {
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToObject);
            }
            else if (kind != StackValueKind.Object && !IsPackedStackKind(kind))
            {
                ConvertToDatum(kind);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToObject);
            }
            _il.Emit(OpCodes.Castclass, GetPackedClrType(expectedType));
        }

        private void EmitArrayReference(Expression expression)
        {
            var kind = EmitExpression(expression);
            if (kind == StackValueKind.Array) return;
            if (kind == StackValueKind.Datum)
            {
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToObject);
            }
            else if (kind != StackValueKind.Object)
            {
                ConvertToDatum(kind);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToObject);
            }
            _il.Emit(OpCodes.Castclass, typeof(ScriptArray));
        }

        private void EmitPackedArrayStorage(Expression expression, FlowValueType expectedType)
        {
            var kind = EmitExpression(expression);
            if (kind == GetPackedStorageStackKind(expectedType))
            {
                return;
            }
            if (kind == GetPackedStackKind(expectedType))
            {
                _il.Emit(OpCodes.Ldfld, GetPackedItemsField(expectedType));
                return;
            }
            if (kind == StackValueKind.Datum)
            {
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToObject);
            }
            else if (kind != StackValueKind.Object && !IsPackedStackKind(kind))
            {
                ConvertToDatum(kind);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToObject);
            }
            _il.Emit(OpCodes.Castclass, GetPackedClrType(expectedType));
            _il.Emit(OpCodes.Ldfld, GetPackedItemsField(expectedType));
        }

        private void ConvertToDatum(StackValueKind kind)
        {
            switch (kind)
            {
                case StackValueKind.Datum:
                    return;
                case StackValueKind.Int32:
                    _il.Emit(OpCodes.Conv_R8);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromNumber);
                    return;
                case StackValueKind.Number:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromNumber);
                    return;
                case StackValueKind.Boolean:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromBoolean);
                    return;
                case StackValueKind.String:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromString);
                    return;
                case StackValueKind.Object:
                case StackValueKind.Array:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
                    return;
                case StackValueKind.Int32Array:
                case StackValueKind.Int8Array:
                case StackValueKind.Float64Array:
                case StackValueKind.BooleanArray:
                case StackValueKind.UInt8Array:
                case StackValueKind.Int16Array:
                case StackValueKind.UInt16Array:
                case StackValueKind.UInt32Array:
                case StackValueKind.Int64Array:
                case StackValueKind.UInt64Array:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
                    return;
                case StackValueKind.Int32Buffer:
                case StackValueKind.Int8Buffer:
                case StackValueKind.Float64Buffer:
                case StackValueKind.BooleanBuffer:
                case StackValueKind.UInt8Buffer:
                case StackValueKind.Int16Buffer:
                case StackValueKind.UInt16Buffer:
                case StackValueKind.UInt32Buffer:
                case StackValueKind.Int64Buffer:
                case StackValueKind.UInt64Buffer:
                    throw new NotSupportedException("Native packed-array storage cannot cross a dynamic value boundary.");
            }
        }

        private void EmitNumber(Expression expression)
        {
            if (TryEmitCachedNumber(expression)) return;
            var kind = EmitExpression(expression);
            ConvertStackToNumber(kind);
        }

        private void ConvertStackToNumber(StackValueKind kind)
        {
            switch (kind)
            {
                case StackValueKind.Number:
                    return;
                case StackValueKind.Int32:
                    _il.Emit(OpCodes.Conv_R8);
                    return;
                case StackValueKind.Boolean:
                    _il.Emit(OpCodes.Conv_R8);
                    return;
                case StackValueKind.Datum:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToArithmeticNumber);
                    return;
                default:
                    ConvertToDatum(kind);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToArithmeticNumber);
                    return;
            }
        }

        private void EmitInt32Value(Expression expression)
        {
            if (TryEmitCachedNumber(expression))
            {
                ConvertStackToInt32(StackValueKind.Number, truncateThroughInt64: false);
                return;
            }
            var kind = EmitExpression(expression);
            ConvertStackToInt32(kind, truncateThroughInt64: false);
        }

        private void ConvertStackToInt32(StackValueKind kind, bool truncateThroughInt64)
        {
            switch (kind)
            {
                case StackValueKind.Int32:
                    return;
                case StackValueKind.Boolean:
                    return;
                case StackValueKind.Number:
                    if (truncateThroughInt64) _il.Emit(OpCodes.Conv_I8);
                    _il.Emit(OpCodes.Conv_I4);
                    return;
                case StackValueKind.Datum:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToArithmeticNumber);
                    if (truncateThroughInt64) _il.Emit(OpCodes.Conv_I8);
                    _il.Emit(OpCodes.Conv_I4);
                    return;
                default:
                    ConvertToDatum(kind);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToArithmeticNumber);
                    if (truncateThroughInt64) _il.Emit(OpCodes.Conv_I8);
                    _il.Emit(OpCodes.Conv_I4);
                    return;
            }
        }

        private void EmitLocation(AstNode node)
        {
            if (_directMode || node == null || node.Range.StartLine <= 0 ||
                (!_session.Options.Optimization.StackTrace &&
                    _session.Options.Optimization.Level != OptimizeOptions.Debug))
            {
                return;
            }
            var location = ((long)_module.PathHash & 0xffffffffL) |
                ((long)node.Range.StartLine << 32);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldc_I8, location);
            _il.Emit(OpCodes.Stfld, TypedRuntimeMetadata.ContextLocation);
        }

        private void EmitArithmeticNumber(Expression expression)
        {
            EmitNumber(expression);
        }

        private void EmitCondition(Expression expression)
        {
            if (TryGetCachedBoolean(expression, out var value))
            {
                _il.Emit(OpCodes.Ldloc, value);
                return;
            }
            var kind = EmitExpression(expression);
            ConvertStackToBoolean(kind);
        }

        private void ConvertStackToBoolean(StackValueKind kind)
        {
            switch (kind)
            {
                case StackValueKind.Boolean:
                    return;
                case StackValueKind.Int32:
                    _il.Emit(OpCodes.Ldc_I4_0);
                    _il.Emit(OpCodes.Ceq);
                    EmitBooleanNot();
                    return;
                case StackValueKind.Number:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToBooleanNumber);
                    return;
                case StackValueKind.String:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.IsNullOrEmpty);
                    EmitBooleanNot();
                    return;
                case StackValueKind.Object:
                case StackValueKind.Array:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToBooleanObject);
                    return;
                case StackValueKind.Int32Array:
                case StackValueKind.Int8Array:
                case StackValueKind.Float64Array:
                case StackValueKind.BooleanArray:
                case StackValueKind.UInt8Array:
                case StackValueKind.Int16Array:
                case StackValueKind.UInt16Array:
                case StackValueKind.UInt32Array:
                case StackValueKind.Int64Array:
                case StackValueKind.UInt64Array:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToBooleanObject);
                    return;
                case StackValueKind.Int32Buffer:
                case StackValueKind.Int8Buffer:
                case StackValueKind.Float64Buffer:
                case StackValueKind.BooleanBuffer:
                case StackValueKind.UInt8Buffer:
                case StackValueKind.Int16Buffer:
                case StackValueKind.UInt16Buffer:
                case StackValueKind.UInt32Buffer:
                case StackValueKind.Int64Buffer:
                case StackValueKind.UInt64Buffer:
                    _il.Emit(OpCodes.Ldnull);
                    _il.Emit(OpCodes.Ceq);
                    EmitBooleanNot();
                    return;
                case StackValueKind.Datum:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToBooleanDatum);
                    return;
            }
        }

        private static bool IsPackedStackKind(StackValueKind kind)
        {
            return kind is StackValueKind.Int32Array or
                StackValueKind.Int8Array or
                StackValueKind.Float64Array or
                StackValueKind.BooleanArray or
                StackValueKind.UInt8Array or
                StackValueKind.Int16Array or
                StackValueKind.UInt16Array or
                StackValueKind.UInt32Array or
                StackValueKind.Int64Array or
                StackValueKind.UInt64Array or
                StackValueKind.Int32Buffer or
                StackValueKind.Int8Buffer or
                StackValueKind.Float64Buffer or
                StackValueKind.BooleanBuffer or
                StackValueKind.UInt8Buffer or
                StackValueKind.Int16Buffer or
                StackValueKind.UInt16Buffer or
                StackValueKind.UInt32Buffer or
                StackValueKind.Int64Buffer or
                StackValueKind.UInt64Buffer;
        }

        private static StackValueKind GetPackedStackKind(FlowValueType type)
        {
            return type switch
            {
                FlowValueType.Int32Array => StackValueKind.Int32Array,
                FlowValueType.Int8Array => StackValueKind.Int8Array,
                FlowValueType.Float64Array => StackValueKind.Float64Array,
                FlowValueType.BooleanArray => StackValueKind.BooleanArray,
                FlowValueType.UInt8Array => StackValueKind.UInt8Array,
                FlowValueType.Int16Array => StackValueKind.Int16Array,
                FlowValueType.UInt16Array => StackValueKind.UInt16Array,
                FlowValueType.UInt32Array => StackValueKind.UInt32Array,
                FlowValueType.Int64Array => StackValueKind.Int64Array,
                FlowValueType.UInt64Array => StackValueKind.UInt64Array,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private static StackValueKind GetPackedStorageStackKind(FlowValueType type)
        {
            return type switch
            {
                FlowValueType.Int32Array => StackValueKind.Int32Buffer,
                FlowValueType.Int8Array => StackValueKind.Int8Buffer,
                FlowValueType.Float64Array => StackValueKind.Float64Buffer,
                FlowValueType.BooleanArray => StackValueKind.BooleanBuffer,
                FlowValueType.UInt8Array => StackValueKind.UInt8Buffer,
                FlowValueType.Int16Array => StackValueKind.Int16Buffer,
                FlowValueType.UInt16Array => StackValueKind.UInt16Buffer,
                FlowValueType.UInt32Array => StackValueKind.UInt32Buffer,
                FlowValueType.Int64Array => StackValueKind.Int64Buffer,
                FlowValueType.UInt64Array => StackValueKind.UInt64Buffer,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private StackValueKind GetPackedLocalStackKind(FlowValueType type)
        {
            return _directMode
                ? GetPackedStorageStackKind(type)
                : GetPackedStackKind(type);
        }

        private static Type GetPackedClrType(FlowValueType type)
        {
            return type switch
            {
                FlowValueType.Int32Array => typeof(ScriptInt32Array),
                FlowValueType.Int8Array => typeof(ScriptInt8Array),
                FlowValueType.Float64Array => typeof(ScriptFloat64Array),
                FlowValueType.BooleanArray => typeof(ScriptBooleanArray),
                FlowValueType.UInt8Array => typeof(ScriptUInt8Array),
                FlowValueType.Int16Array => typeof(ScriptInt16Array),
                FlowValueType.UInt16Array => typeof(ScriptUInt16Array),
                FlowValueType.UInt32Array => typeof(ScriptUInt32Array),
                FlowValueType.Int64Array => typeof(ScriptInt64Array),
                FlowValueType.UInt64Array => typeof(ScriptUInt64Array),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private static Type GetPackedStorageType(FlowValueType type)
        {
            return type switch
            {
                FlowValueType.Int32Array => typeof(int[]),
                FlowValueType.Int8Array => typeof(sbyte[]),
                FlowValueType.Float64Array => typeof(double[]),
                FlowValueType.BooleanArray => typeof(bool[]),
                FlowValueType.UInt8Array => typeof(byte[]),
                FlowValueType.Int16Array => typeof(short[]),
                FlowValueType.UInt16Array => typeof(ushort[]),
                FlowValueType.UInt32Array => typeof(uint[]),
                FlowValueType.Int64Array => typeof(long[]),
                FlowValueType.UInt64Array => typeof(ulong[]),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private Type GetPackedLocalClrType(FlowValueType type)
        {
            return _directMode ? GetPackedStorageType(type) : GetPackedClrType(type);
        }

        private static Type GetNativeParameterType(FlowValueType type)
        {
            if (type == FlowValueType.Int32 || FlowValueTypeFacts.IsInt32Coercion(type))
            {
                return typeof(int);
            }
            if (type == FlowValueType.Number) return typeof(double);
            if (type == FlowValueType.Boolean) return typeof(bool);
            if (type == FlowValueType.Array) return typeof(ScriptArray);
            if (FlowValueTypeFacts.IsPackedArray(type)) return GetPackedStorageType(type);
            return typeof(ScriptDatum);
        }

        private static Type GetStackClrType(StackValueKind kind)
        {
            return kind switch
            {
                StackValueKind.Datum => typeof(ScriptDatum),
                StackValueKind.Int32 => typeof(int),
                StackValueKind.Number => typeof(double),
                StackValueKind.Boolean => typeof(bool),
                StackValueKind.String => typeof(string),
                StackValueKind.Object => typeof(ScriptObject),
                StackValueKind.Array => typeof(ScriptArray),
                StackValueKind.Int32Array => typeof(ScriptInt32Array),
                StackValueKind.Int8Array => typeof(ScriptInt8Array),
                StackValueKind.Float64Array => typeof(ScriptFloat64Array),
                StackValueKind.BooleanArray => typeof(ScriptBooleanArray),
                StackValueKind.UInt8Array => typeof(ScriptUInt8Array),
                StackValueKind.Int16Array => typeof(ScriptInt16Array),
                StackValueKind.UInt16Array => typeof(ScriptUInt16Array),
                StackValueKind.UInt32Array => typeof(ScriptUInt32Array),
                StackValueKind.Int64Array => typeof(ScriptInt64Array),
                StackValueKind.UInt64Array => typeof(ScriptUInt64Array),
                StackValueKind.Int32Buffer => typeof(int[]),
                StackValueKind.Int8Buffer => typeof(sbyte[]),
                StackValueKind.Float64Buffer => typeof(double[]),
                StackValueKind.BooleanBuffer => typeof(bool[]),
                StackValueKind.UInt8Buffer => typeof(byte[]),
                StackValueKind.Int16Buffer => typeof(short[]),
                StackValueKind.UInt16Buffer => typeof(ushort[]),
                StackValueKind.UInt32Buffer => typeof(uint[]),
                StackValueKind.Int64Buffer => typeof(long[]),
                StackValueKind.UInt64Buffer => typeof(ulong[]),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        private static FieldInfo GetPackedItemsField(FlowValueType type)
        {
            return type switch
            {
                FlowValueType.Int32Array => TypedRuntimeMetadata.ScriptInt32ArrayItems,
                FlowValueType.Int8Array => TypedRuntimeMetadata.ScriptInt8ArrayItems,
                FlowValueType.Float64Array => TypedRuntimeMetadata.ScriptFloat64ArrayItems,
                FlowValueType.BooleanArray => TypedRuntimeMetadata.ScriptBooleanArrayItems,
                FlowValueType.UInt8Array => TypedRuntimeMetadata.ScriptUInt8ArrayItems,
                FlowValueType.Int16Array => TypedRuntimeMetadata.ScriptInt16ArrayItems,
                FlowValueType.UInt16Array => TypedRuntimeMetadata.ScriptUInt16ArrayItems,
                FlowValueType.UInt32Array => TypedRuntimeMetadata.ScriptUInt32ArrayItems,
                FlowValueType.Int64Array => TypedRuntimeMetadata.ScriptInt64ArrayItems,
                FlowValueType.UInt64Array => TypedRuntimeMetadata.ScriptUInt64ArrayItems,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private static ConstructorInfo GetPackedConstructor(FlowValueType type)
        {
            return type switch
            {
                FlowValueType.Int32Array => TypedRuntimeMetadata.ScriptInt32ArrayConstructor,
                FlowValueType.Int8Array => TypedRuntimeMetadata.ScriptInt8ArrayConstructor,
                FlowValueType.Float64Array => TypedRuntimeMetadata.ScriptFloat64ArrayConstructor,
                FlowValueType.BooleanArray => TypedRuntimeMetadata.ScriptBooleanArrayConstructor,
                FlowValueType.UInt8Array => TypedRuntimeMetadata.ScriptUInt8ArrayConstructor,
                FlowValueType.Int16Array => TypedRuntimeMetadata.ScriptInt16ArrayConstructor,
                FlowValueType.UInt16Array => TypedRuntimeMetadata.ScriptUInt16ArrayConstructor,
                FlowValueType.UInt32Array => TypedRuntimeMetadata.ScriptUInt32ArrayConstructor,
                FlowValueType.Int64Array => TypedRuntimeMetadata.ScriptInt64ArrayConstructor,
                FlowValueType.UInt64Array => TypedRuntimeMetadata.ScriptUInt64ArrayConstructor,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private void EmitNull()
        {
            _il.Emit(OpCodes.Ldsfld, TypedRuntimeMetadata.DatumNull);
        }

        private void EmitBooleanNot()
        {
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Ceq);
        }

        private LocalBuilder DeclareLocal(Type type)
        {
            _localCount++;
            return _il.DeclareLocal(type);
        }

        private static bool IsComparison(Operator op)
        {
            return op == Operator.Equal || op == Operator.NotEqual ||
                op == Operator.LessThan || op == Operator.LessThanOrEqual ||
                op == Operator.GreaterThan || op == Operator.GreaterThanOrEqual;
        }

        private static bool IsBitwise(Operator op)
        {
            return op == Operator.BitwiseAnd || op == Operator.BitwiseOr ||
                op == Operator.BitwiseXor || op == Operator.LeftShift ||
                op == Operator.SignedRightShift || op == Operator.UnSignedRightShift;
        }

        private static bool TryGetStaticPropertyName(Expression expression, out string name)
        {
            if (expression is NameExpression property && !string.IsNullOrEmpty(property.Identifier?.Value))
            {
                name = property.Identifier.Value;
                return true;
            }
            if (expression is LiteralExpression { Token: StringToken text })
            {
                name = text.Value;
                return true;
            }
            name = null;
            return false;
        }

        private static bool TryGetFastObject3(
            MapExpression expression,
            out MapKeyValueExpression first,
            out MapKeyValueExpression second,
            out MapKeyValueExpression third)
        {
            first = null;
            second = null;
            third = null;
            if (expression.Entries.Count != 3 ||
                expression.Entries[0] is not MapKeyValueExpression firstEntry ||
                expression.Entries[1] is not MapKeyValueExpression secondEntry ||
                expression.Entries[2] is not MapKeyValueExpression thirdEntry ||
                firstEntry.Key == null || secondEntry.Key == null || thirdEntry.Key == null ||
                firstEntry.Value == null || secondEntry.Value == null || thirdEntry.Value == null)
            {
                return false;
            }

            var firstName = firstEntry.Key.Value;
            var secondName = secondEntry.Key.Value;
            var thirdName = thirdEntry.Key.Value;
            if (StringComparer.Ordinal.Equals(firstName, secondName) ||
                StringComparer.Ordinal.Equals(firstName, thirdName) ||
                StringComparer.Ordinal.Equals(secondName, thirdName))
            {
                return false;
            }
            first = firstEntry;
            second = secondEntry;
            third = thirdEntry;
            return true;
        }

        private static string GetMapEntryKey(Expression entry)
        {
            if (entry is MapKeyValueExpression keyValue) return keyValue.Key.Value;
            if (entry is NameExpression name) return name.Identifier?.Value;
            throw new NotSupportedException("Object literal entry key.");
        }

        private static Expression GetMapEntryValue(Expression entry)
        {
            return entry is MapKeyValueExpression keyValue ? keyValue.Value : entry;
        }

        private static MethodInfo GetDynamicBinary(Operator op)
        {
            if (op == Operator.Add) return TypedRuntimeMetadata.Add;
            if (op == Operator.Subtract) return TypedRuntimeMetadata.Subtract;
            if (op == Operator.Multiply) return TypedRuntimeMetadata.Multiply;
            if (op == Operator.Divide) return TypedRuntimeMetadata.Divide;
            if (op == Operator.Modulo) return TypedRuntimeMetadata.Modulo;
            if (op == Operator.BitwiseAnd) return TypedRuntimeMetadata.BitwiseAnd;
            if (op == Operator.BitwiseOr) return TypedRuntimeMetadata.BitwiseOr;
            if (op == Operator.BitwiseXor) return TypedRuntimeMetadata.BitwiseXor;
            if (op == Operator.LeftShift) return TypedRuntimeMetadata.LeftShift;
            if (op == Operator.SignedRightShift) return TypedRuntimeMetadata.RightShift;
            if (op == Operator.UnSignedRightShift) return TypedRuntimeMetadata.UnsignedRightShift;
            throw new NotSupportedException("Dynamic binary operator.");
        }

        private static MethodInfo GetComparison(Operator op)
        {
            if (op == Operator.Equal) return TypedRuntimeMetadata.EqualBoolean;
            if (op == Operator.NotEqual) return TypedRuntimeMetadata.NotEqualBoolean;
            if (op == Operator.LessThan) return TypedRuntimeMetadata.LessBoolean;
            if (op == Operator.LessThanOrEqual) return TypedRuntimeMetadata.LessEqualBoolean;
            if (op == Operator.GreaterThan) return TypedRuntimeMetadata.GreaterBoolean;
            if (op == Operator.GreaterThanOrEqual) return TypedRuntimeMetadata.GreaterEqualBoolean;
            throw new NotSupportedException("Comparison operator.");
        }

        private static FunctionCallConvention SelectConvention(FunctionPlan function)
        {
            return function.IsDirectCallCandidate &&
                !function.HasDefaultParameters &&
                !function.UsesArgumentsObject &&
                GetParameterCount(function) <= 7
                    ? GetFastConvention(GetParameterCount(function))
                    : FunctionCallConvention.Span;
        }

        private static int GetParameterCount(FunctionPlan function)
        {
            var count = 0;
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                if (function.LocalSlots[i].IsParameter) count++;
            }
            return count;
        }

        private static Type[] GetParameterTypes(FunctionCallConvention convention)
        {
            return convention == FunctionCallConvention.Span
                ? s_spanParameters
                : s_fastParameters[GetFastArity(convention)];
        }

        private static FunctionCallConvention GetFastConvention(int arity)
        {
            return arity switch
            {
                0 => FunctionCallConvention.Fast0,
                1 => FunctionCallConvention.Fast1,
                2 => FunctionCallConvention.Fast2,
                3 => FunctionCallConvention.Fast3,
                4 => FunctionCallConvention.Fast4,
                5 => FunctionCallConvention.Fast5,
                6 => FunctionCallConvention.Fast6,
                7 => FunctionCallConvention.Fast7,
                _ => FunctionCallConvention.Span
            };
        }

        private static int GetFastArity(FunctionCallConvention convention)
        {
            return convention switch
            {
                FunctionCallConvention.Fast0 => 0,
                FunctionCallConvention.Fast1 => 1,
                FunctionCallConvention.Fast2 => 2,
                FunctionCallConvention.Fast3 => 3,
                FunctionCallConvention.Fast4 => 4,
                FunctionCallConvention.Fast5 => 5,
                FunctionCallConvention.Fast6 => 6,
                FunctionCallConvention.Fast7 => 7,
                _ => -1
            };
        }

        private string CreateDebuggerMetadata(
            FunctionPlan function,
            FunctionCallConvention convention)
        {
            if (_session.Options.Compiler.Mode != CompilationMode.Persistence ||
                _session.Options.Optimization.Level != OptimizeOptions.Debug)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.Append("v=1;cc=");
            builder.Append(convention == FunctionCallConvention.Span ? "span" : "fast");
            builder.Append(";arity=");
            builder.Append(convention == FunctionCallConvention.Span
                ? GetParameterCount(function)
                : GetFastArity(convention));

            var parameterIndex = 0;
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                var slot = function.LocalSlots[i];
                if (slot.Id.IsValid &&
                    _capturedLocalBySlot != null &&
                    _capturedLocalBySlot.ContainsKey(slot.Id.Value))
                {
                    if (slot.IsParameter) parameterIndex++;
                    continue;
                }

                if (slot.IsParameter)
                {
                    builder.Append(";p:");
                    AppendEscaped(builder, slot.Name);
                    builder.Append(':');
                    builder.Append((uint)i < (uint)_locals.Length ? _locals[i].LocalIndex : -1);
                    builder.Append(':');
                    builder.Append(parameterIndex++);
                    // Typed emission always copies parameters into their inferred CIL local.
                    builder.Append(":0");
                    continue;
                }

                if ((uint)i >= (uint)_locals.Length || _locals[i] == null)
                {
                    continue;
                }

                builder.Append(";l:");
                AppendEscaped(builder, slot.Name);
                builder.Append(':');
                builder.Append(_locals[i].LocalIndex);
            }

            for (var i = 0; i < function.UpvalueSlots.Length; i++)
            {
                builder.Append(";u:");
                AppendEscaped(builder, function.UpvalueSlots[i].Name);
                builder.Append(':');
                builder.Append(i);
            }

            for (var i = 0; i < function.CapturedLocalSlots.Length; i++)
            {
                builder.Append(";c:");
                AppendEscaped(builder, function.CapturedLocalSlots[i].Name);
                builder.Append(':');
                builder.Append(i);
                builder.Append(':');
                builder.Append(_capturedUpvalues?.LocalIndex ?? -1);
            }

            AppendModuleSymbols(builder, function);
            return builder.ToString();
        }

        private void AppendModuleSymbols(StringBuilder builder, FunctionPlan function)
        {
            if (!_module.ModuleScope.IsValid) return;
            var moduleScope = _session.CompileSession.Scopes[_module.ModuleScope];
            var symbols = _session.CompileSession.Symbols;
            for (var i = 0; i < moduleScope.SymbolCount; i++)
            {
                var symbol = symbols[new SymbolId(moduleScope.FirstSymbol.Value + i)];
                if (!CanShowModuleSymbolInDebugger(function, symbol)) continue;
                builder.Append(";m:");
                AppendEscaped(builder, symbol.Name);
            }
        }

        private static bool CanShowModuleSymbolInDebugger(FunctionPlan function, SymbolInfo symbol)
        {
            if (symbol.Kind is not (BackendSymbolKind.ModuleProperty or
                BackendSymbolKind.ImportAlias or
                BackendSymbolKind.Function or
                BackendSymbolKind.Enum))
            {
                return false;
            }

            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                if (string.Equals(function.LocalSlots[i].Name, symbol.Name, StringComparison.Ordinal)) return false;
            }
            for (var i = 0; i < function.UpvalueSlots.Length; i++)
            {
                if (string.Equals(function.UpvalueSlots[i].Name, symbol.Name, StringComparison.Ordinal)) return false;
            }
            for (var i = 0; i < function.CapturedLocalSlots.Length; i++)
            {
                if (string.Equals(function.CapturedLocalSlots[i].Name, symbol.Name, StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private static void AppendEscaped(StringBuilder builder, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch is '\\' or ';' or ':') builder.Append('\\');
                builder.Append(ch);
            }
        }

        private static bool ReturnsOnAllPaths(Statement statement)
        {
            switch (statement)
            {
                case ReturnStatement:
                    return true;
                case BlockStatement block:
                    for (var i = 0; i < block.Statements.Count; i++)
                    {
                        if (ReturnsOnAllPaths(block.Statements[i])) return true;
                    }
                    return false;
                case IfStatement @if:
                    return @if.Else != null &&
                        ReturnsOnAllPaths(@if.Body) &&
                        ReturnsOnAllPaths(@if.Else);
                default:
                    return false;
            }
        }

        private static bool ContainsReturnInFinally(Statement statement, bool insideFinally = false)
        {
            switch (statement)
            {
                case null:
                case FunctionDeclaration:
                    return false;
                case ReturnStatement:
                    return insideFinally;
                case BlockStatement block:
                    for (var i = 0; i < block.Statements.Count; i++)
                    {
                        if (ContainsReturnInFinally(block.Statements[i], insideFinally)) return true;
                    }
                    return false;
                case IfStatement @if:
                    return ContainsReturnInFinally(@if.Body, insideFinally) ||
                        ContainsReturnInFinally(@if.Else, insideFinally);
                case WhileStatement @while:
                    return ContainsReturnInFinally(@while.Body, insideFinally);
                case ForStatement @for:
                    return ContainsReturnInFinally(@for.Initializer as Statement, insideFinally) ||
                        ContainsReturnInFinally(@for.Body, insideFinally);
                case ForInStatement forIn:
                    return ContainsReturnInFinally(forIn.Initializer, insideFinally) ||
                        ContainsReturnInFinally(forIn.Body, insideFinally);
                case TryStatement @try:
                    return ContainsReturnInFinally(@try.Body, insideFinally) ||
                        ContainsReturnInFinally(@try.CatchBody, insideFinally) ||
                        ContainsReturnInFinally(@try.FinallyBody, insideFinally: true);
                default:
                    return false;
            }
        }

        private static bool ContainsFinallyTransferToCurrentLoop(
            Statement statement,
            int nestedLoopDepth = 0,
            int finallyDepth = 0)
        {
            switch (statement)
            {
                case null:
                case FunctionDeclaration:
                    return false;
                case BreakStatement:
                case ContinueStatement:
                    return nestedLoopDepth == 0 && finallyDepth != 0;
                case BlockStatement block:
                    for (var i = 0; i < block.Statements.Count; i++)
                    {
                        if (ContainsFinallyTransferToCurrentLoop(
                            block.Statements[i],
                            nestedLoopDepth,
                            finallyDepth))
                        {
                            return true;
                        }
                    }
                    return false;
                case IfStatement @if:
                    return ContainsFinallyTransferToCurrentLoop(
                            @if.Body,
                            nestedLoopDepth,
                            finallyDepth) ||
                        ContainsFinallyTransferToCurrentLoop(
                            @if.Else,
                            nestedLoopDepth,
                            finallyDepth);
                case WhileStatement @while:
                    return ContainsFinallyTransferToCurrentLoop(
                        @while.Body,
                        nestedLoopDepth + 1,
                        finallyDepth);
                case ForStatement @for:
                    return ContainsFinallyTransferToCurrentLoop(
                            @for.Initializer as Statement,
                            nestedLoopDepth,
                            finallyDepth) ||
                        ContainsFinallyTransferToCurrentLoop(
                            @for.Body,
                            nestedLoopDepth + 1,
                            finallyDepth);
                case ForInStatement forIn:
                    return ContainsFinallyTransferToCurrentLoop(
                            forIn.Initializer,
                            nestedLoopDepth,
                            finallyDepth) ||
                        ContainsFinallyTransferToCurrentLoop(
                            forIn.Body,
                            nestedLoopDepth + 1,
                            finallyDepth);
                case TryStatement @try:
                    return ContainsFinallyTransferToCurrentLoop(
                            @try.Body,
                            nestedLoopDepth,
                            finallyDepth) ||
                        ContainsFinallyTransferToCurrentLoop(
                            @try.CatchBody,
                            nestedLoopDepth,
                            finallyDepth) ||
                        ContainsFinallyTransferToCurrentLoop(
                            @try.FinallyBody,
                            nestedLoopDepth,
                            finallyDepth + 1);
                default:
                    return false;
            }
        }

        private static bool ContainsProtectedRegion(Statement statement)
        {
            switch (statement)
            {
                case null:
                    return false;
                case TryStatement:
                    return true;
                case BlockStatement block:
                    for (var i = 0; i < block.Statements.Count; i++)
                    {
                        if (ContainsProtectedRegion(block.Statements[i])) return true;
                    }
                    return false;
                case IfStatement @if:
                    return ContainsProtectedRegion(@if.Body) || ContainsProtectedRegion(@if.Else);
                case WhileStatement @while:
                    return ContainsProtectedRegion(@while.Body);
                case ForStatement @for:
                    return ContainsProtectedRegion(@for.Initializer as Statement) ||
                        ContainsProtectedRegion(@for.Body);
                case ForInStatement forIn:
                    return ContainsProtectedRegion(forIn.Initializer) ||
                        ContainsProtectedRegion(forIn.Body);
                default:
                    return false;
            }
        }

        private static void EmitInt32(int value, ILGenerator il)
        {
            il.Emit(OpCodes.Ldc_I4, value);
        }

        private void EmitInt32(int value)
        {
            EmitInt32(value, _il);
        }

        private enum StackValueKind : byte
        {
            Datum,
            Int32,
            Number,
            Boolean,
            String,
            Object,
            Array,
            Int32Array,
            Int8Array,
            Float64Array,
            BooleanArray,
            UInt8Array,
            Int16Array,
            UInt16Array,
            UInt32Array,
            Int64Array,
            UInt64Array,
            Int32Buffer,
            Int8Buffer,
            Float64Buffer,
            BooleanBuffer,
            UInt8Buffer,
            Int16Buffer,
            UInt16Buffer,
            UInt32Buffer,
            Int64Buffer,
            UInt64Buffer
        }

        private readonly struct LoopTarget
        {
            public LoopTarget(Label label, int protectedDepth, int finallyDepth)
            {
                Label = label;
                ProtectedDepth = protectedDepth;
                FinallyDepth = finallyDepth;
            }

            public Label Label { get; }
            public int ProtectedDepth { get; }
            public int FinallyDepth { get; }
        }

        private sealed class ParameterCacheCollector
        {
            private readonly TypedFunctionCode _code;
            private readonly bool[] _numeric;
            private readonly bool[] _boolean;

            public ParameterCacheCollector(
                TypedFunctionCode code,
                bool[] numeric,
                bool[] boolean)
            {
                _code = code;
                _numeric = numeric;
                _boolean = boolean;
            }

            public void Visit(AstNode node)
            {
                if (node == null || node is FunctionDeclaration || node is LambdaExpression) return;
                if (node is BinaryExpression binary)
                {
                    if (IsComparison(binary.Operator) || IsNumericShadowOperator(binary.Operator))
                    {
                        MarkNumeric(binary.Left);
                        MarkNumeric(binary.Right);
                    }
                    else if (binary.Operator == Operator.LogicalAnd ||
                        binary.Operator == Operator.LogicalOr)
                    {
                        MarkBoolean(binary.Left);
                    }
                }
                else if (node is UnaryExpression unary)
                {
                    if (unary.Operator == Operator.Negate)
                    {
                        MarkNumeric(unary.Expression);
                    }
                    else if (unary.Operator == Operator.LogicalNot)
                    {
                        MarkBoolean(unary.Expression);
                    }
                }
                else if (node is IfStatement @if)
                {
                    MarkBoolean(@if.Condition);
                }
                else if (node is WhileStatement @while)
                {
                    MarkBoolean(@while.Condition);
                }
                else if (node is ForStatement @for)
                {
                    MarkBoolean(@for.Condition);
                }
                var visitor = new ChildVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private static bool IsNumericShadowOperator(Operator op)
            {
                return op == Operator.Subtract || op == Operator.Multiply ||
                    op == Operator.Divide || op == Operator.Modulo;
            }

            private void MarkNumeric(Expression expression)
            {
                Mark(expression, _numeric);
            }

            private void MarkBoolean(Expression expression)
            {
                if (expression is UnaryExpression unary &&
                    unary.Operator == Operator.LogicalNot)
                {
                    MarkBoolean(unary.Expression);
                    return;
                }
                if (expression is BinaryExpression binary &&
                    (binary.Operator == Operator.LogicalAnd ||
                        binary.Operator == Operator.LogicalOr))
                {
                    MarkBoolean(binary.Left);
                    MarkBoolean(binary.Right);
                    return;
                }
                Mark(expression, _boolean);
            }

            private void Mark(Expression expression, bool[] result)
            {
                if (expression is not NameExpression name) return;
                var binding = _code.GetName(name);
                if (!binding.IsLocal || (uint)binding.Local.Value >= (uint)result.Length) return;
                if (_code.Function.LocalSlots[binding.Local.Value].IsParameter)
                {
                    result[binding.Local.Value] = true;
                }
            }

            private readonly struct ChildVisitor : IAstChildVisitor
            {
                private readonly ParameterCacheCollector _owner;

                public ChildVisitor(ParameterCacheCollector owner)
                {
                    _owner = owner;
                }

                public void Visit(AstNode node)
                {
                    _owner.Visit(node);
                }
            }
        }

        private struct PreparedMethod
        {
            public PreparedMethod(MethodInfo method, ILGenerator il, FunctionCallConvention convention, TypedFunctionCode code)
            {
                Method = method;
                DirectMethod = null;
                IL = il;
                Convention = convention;
                Code = code;
                Emitted = false;
            }

            public MethodInfo Method;
            public MethodInfo DirectMethod;
            public ILGenerator IL;
            public FunctionCallConvention Convention;
            public TypedFunctionCode Code;
            public bool Emitted;
            public bool IsDefined => Method != null;
        }

        private struct PreparedDirectMethod
        {
            public PreparedDirectMethod(
                MethodInfo method,
                ILGenerator il,
                TypedFunctionCode code,
                FlowValueType[] parameterTypes,
                StackValueKind returnKind)
            {
                Method = method;
                IL = il;
                Code = code;
                ParameterTypes = parameterTypes;
                ReturnKind = returnKind;
                Emitted = false;
            }

            public MethodInfo Method;
            public ILGenerator IL;
            public TypedFunctionCode Code;
            public FlowValueType[] ParameterTypes;
            public StackValueKind ReturnKind;
            public bool Emitted;
            public bool IsDefined => Method != null;
        }

        private static class NativeDirectCallSignatureValidator
        {
            public static bool CanEmit(
                TypedFunctionCode code,
                Func<FunctionId, bool> canDirectCall,
                Func<FunctionId, FlowValueType[]> getParameterTypes)
            {
                var visitor = new Visitor(code, canDirectCall, getParameterTypes);
                visitor.VisitRoot(code.Function.Declaration?.Body);
                return visitor.Valid;
            }

            private struct Visitor : IAstChildVisitor
            {
                private readonly TypedFunctionCode _code;
                private readonly Func<FunctionId, bool> _canDirectCall;
                private readonly Func<FunctionId, FlowValueType[]> _getParameterTypes;

                public Visitor(
                    TypedFunctionCode code,
                    Func<FunctionId, bool> canDirectCall,
                    Func<FunctionId, FlowValueType[]> getParameterTypes)
                {
                    _code = code;
                    _canDirectCall = canDirectCall;
                    _getParameterTypes = getParameterTypes;
                    Valid = true;
                }

                public bool Valid;

                public void VisitRoot(AstNode node)
                {
                    VisitNode(node, isRoot: true);
                }

                public void Visit(AstNode node)
                {
                    VisitNode(node, isRoot: false);
                }

                private void VisitNode(AstNode node, bool isRoot)
                {
                    if (!Valid || node == null)
                    {
                        return;
                    }
                    if (!isRoot && node is FunctionDeclaration or LambdaExpression)
                    {
                        return;
                    }

                    if (node is BinaryExpression binary &&
                        (FlowValueTypeFacts.IsPackedArray(_code.GetExpressionType(binary.Left)) ||
                            FlowValueTypeFacts.IsPackedArray(_code.GetExpressionType(binary.Right))))
                    {
                        Valid = false;
                        return;
                    }
                    if (node is UnaryExpression unary &&
                        unary.Operator != Operator.LogicalNot &&
                        FlowValueTypeFacts.IsPackedArray(_code.GetExpressionType(unary.Expression)))
                    {
                        Valid = false;
                        return;
                    }

                    if (node is FunctionCallExpression call)
                    {
                        if (call.Parent is NewExpression packedNew &&
                            (FlowValueTypeFacts.IsPackedArray(_code.GetExpressionType(packedNew)) ||
                                IsArrayConstruction(_code, packedNew)))
                        {
                            AstTraversal.VisitChildren(node, ref this);
                            return;
                        }
                        if (IsArrayFactoryCall(_code, call) && !HasSpread(call.Arguments))
                        {
                            AstTraversal.VisitChildren(node, ref this);
                            return;
                        }
                        if (HasSpread(call.Arguments) || call.Target is not NameExpression target)
                        {
                            Valid = false;
                            return;
                        }

                        var function = _code.GetName(target).DirectFunction;
                        var parameters = _canDirectCall(function)
                            ? _getParameterTypes(function)
                            : null;
                        if (parameters == null)
                        {
                            Valid = false;
                            return;
                        }

                        for (var i = 0; i < parameters.Length; i++)
                        {
                            if (FlowValueTypeFacts.IsNativeDirectParameter(parameters[i]) &&
                                (i >= call.Arguments.Count ||
                                    !FlowValueTypeFacts.CanPassNativeArgument(
                                        parameters[i],
                                        _code.GetExpressionType(call.Arguments[i]))))
                            {
                                Valid = false;
                                return;
                            }
                        }
                    }

                    AstTraversal.VisitChildren(node, ref this);
                }
            }
        }

        private static class TypedSubsetValidator
        {
            public static bool CanEmit(
                TypedFunctionCode code,
                Func<FunctionId, bool> canDirectCall,
                bool directMode,
                bool requireNativeLocal)
            {
                if (code == null || canDirectCall == null) return false;
                var function = code.Function;
                if (function?.Declaration?.Body is not Statement body)
                {
                    return false;
                }
                if (directMode &&
                    (function.UpvalueSlots.Length != 0 ||
                        function.CapturedLocalSlots.Length != 0 ||
                        function.NestedFunctions.Length != 0 ||
                        function.UsesArgumentsObject))
                {
                    return false;
                }


                var hasNativeLocal = false;
                for (var i = 0; i < code.LocalTypes.Length; i++)
                {
                    if ((FlowValueTypeFacts.IsNumeric(code.LocalTypes[i]) ||
                        FlowValueTypeFacts.IsPackedArray(code.LocalTypes[i]) ||
                        code.LocalTypes[i] == FlowValueType.Array) &&
                        !function.LocalSlots[i].IsParameter)
                    {
                        hasNativeLocal = true;
                        break;
                    }
                }
                if (requireNativeLocal && !hasNativeLocal)
                {
                    return false;
                }

                for (var i = 0; i < function.Declaration.Parameters.Count; i++)
                {
                    if (!CanEmitExpression(code, function.Declaration.Parameters[i].Initializer, canDirectCall, !directMode)) return false;
                }
                return CanEmitStatement(code, body, loopDepth: 0, canDirectCall, !directMode);
            }

            private static bool CanEmitStatement(
                TypedFunctionCode code,
                Statement statement,
                int loopDepth,
                Func<FunctionId, bool> canDirectCall,
                bool allowRuntimeBoundary)
            {
                switch (statement)
                {
                    case null:
                        return true;
                    case BlockStatement block:
                        for (var i = 0; i < block.Functions.Count; i++)
                        {
                            if (!CanEmitStatement(code, block.Functions[i], loopDepth, canDirectCall, allowRuntimeBoundary)) return false;
                        }
                        for (var i = 0; i < block.Statements.Count; i++)
                        {
                            if (!CanEmitStatement(code, block.Statements[i], loopDepth, canDirectCall, allowRuntimeBoundary)) return false;
                        }
                        return true;
                    case FunctionDeclaration functionDeclaration:
                        return allowRuntimeBoundary && functionDeclaration.Flags != FunctionFlags.Declare
                            ? functionDeclaration.Body != null
                            : true;
                    case VariableDeclaration variable:
                        if (variable.IsDeclare) return true;
                        if (!CanEmitExpression(code, variable.Initializer, canDirectCall, allowRuntimeBoundary)) return false;
                        if (variable.Pattern is ObjectDestructuringPattern objectPattern)
                        {
                            return allowRuntimeBoundary && objectPattern.Properties.Count != 0;
                        }
                        if (variable.Pattern is ArrayDestructuringPattern arrayPattern)
                        {
                            if (!allowRuntimeBoundary) return false;
                            for (var i = 0; i < arrayPattern.Elements.Count; i++)
                            {
                                if (arrayPattern.Elements[i] != null &&
                                    arrayPattern.Elements[i] is not NameExpression &&
                                    arrayPattern.Elements[i] is not SpreadExpression { Expression: NameExpression })
                                {
                                    return false;
                                }
                            }
                            return true;
                        }
                        return variable.Pattern == null && code.GetDeclarationSlot(variable).IsValid;
                    case ExpressionStatement expression:
                        return CanEmitExpression(code, expression.Expression, canDirectCall, allowRuntimeBoundary);
                    case ReturnStatement @return:
                        return CanEmitExpression(code, @return.Expression, canDirectCall, allowRuntimeBoundary);
                    case IfStatement @if:
                        return CanEmitExpression(code, @if.Condition, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitStatement(code, @if.Body, loopDepth, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitStatement(code, @if.Else, loopDepth, canDirectCall, allowRuntimeBoundary);
                    case WhileStatement @while:
                        return CanEmitExpression(code, @while.Condition, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitStatement(code, @while.Body, loopDepth + 1, canDirectCall, allowRuntimeBoundary);
                    case ForStatement @for:
                        var initializerSupported = @for.Initializer == null ||
                            (@for.Initializer is Statement statementInitializer && CanEmitStatement(code, statementInitializer, loopDepth, canDirectCall, allowRuntimeBoundary)) ||
                            (@for.Initializer is Expression expressionInitializer && CanEmitExpression(code, expressionInitializer, canDirectCall, allowRuntimeBoundary));
                        return initializerSupported &&
                            CanEmitExpression(code, @for.Condition, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitExpression(code, @for.Incrementor, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitStatement(code, @for.Body, loopDepth + 1, canDirectCall, allowRuntimeBoundary);
                    case ForInStatement forIn:
                        return allowRuntimeBoundary &&
                            CanEmitStatement(code, forIn.Initializer, loopDepth, canDirectCall, allowRuntimeBoundary) &&
                            forIn.Iterator?.Left != null &&
                            CanEmitAssignmentTarget(code, forIn.Iterator.Left, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitExpression(code, forIn.Iterator.Right, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitStatement(code, forIn.Body, loopDepth + 1, canDirectCall, allowRuntimeBoundary);
                    case TryStatement @try:
                        return allowRuntimeBoundary &&
                            CanEmitStatement(code, @try.Body, loopDepth, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitStatement(code, @try.CatchBody, loopDepth, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitStatement(code, @try.FinallyBody, loopDepth, canDirectCall, allowRuntimeBoundary);
                    case ThrowStatement @throw:
                        return allowRuntimeBoundary &&
                            CanEmitExpression(code, @throw.Expression, canDirectCall, allowRuntimeBoundary);
                    case DeleteStatement delete:
                        if (!allowRuntimeBoundary) return false;
                        if (delete.Expression is GetPropertyExpression deleteProperty)
                        {
                            return TryGetStaticPropertyName(deleteProperty.Property, out _) &&
                                CanEmitExpression(code, deleteProperty.Object, canDirectCall, allowRuntimeBoundary);
                        }
                        return delete.Expression is GetElementExpression deleteElement &&
                            CanEmitExpression(code, deleteElement.Object, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitExpression(code, deleteElement.Index, canDirectCall, allowRuntimeBoundary);
                    case BreakStatement:
                    case ContinueStatement:
                        return loopDepth > 0;
                    case DebuggerStatement:
                        return true;
                    default:
                        return false;
                }
            }

            private static bool CanEmitExpression(
                TypedFunctionCode code,
                Expression expression,
                Func<FunctionId, bool> canDirectCall,
                bool allowRuntimeBoundary)
            {
                switch (expression)
                {
                    case null:
                        return true;
                    case TypedDocumentExpression tdoc:
                        if (string.IsNullOrEmpty(tdoc.TypeName))
                        {
                            return CanEmitExpression(code, tdoc.Value, canDirectCall, allowRuntimeBoundary);
                        }
                        if (tdoc.TypeName is "StringBuffer" or "Date" or "Regex" or "Path" or "HashMap")
                        {
                            return allowRuntimeBoundary &&
                                CanEmitExpression(code, tdoc.Value, canDirectCall, allowRuntimeBoundary);
                        }
                        if (tdoc.TypeName is "Object" or "Array")
                        {
                            return allowRuntimeBoundary &&
                                CanEmitExpression(code, tdoc.Value, canDirectCall, allowRuntimeBoundary);
                        }
                        return CanEmitExpression(code, tdoc.Value, canDirectCall, allowRuntimeBoundary);
                    case LiteralExpression literal:
                        return literal.Token is NullToken or BooleanToken or NumberToken or StringToken ||
                            (allowRuntimeBoundary && literal.Token is RegexToken);
                    case NameExpression name:
                        var binding = code.GetName(name);
                        return binding.IsLocal ||
                            (binding.HasConstant && (binding.Constant.Kind == ValueKind.Null ||
                                binding.Constant.Kind == ValueKind.Boolean ||
                                binding.Constant.Kind == ValueKind.Number ||
                                binding.Constant.Kind == ValueKind.String)) ||
                            (allowRuntimeBoundary && !string.IsNullOrEmpty(binding.Name));
                    case BinaryExpression binary:
                        return GetDynamicBinaryOrComparisonSupported(binary.Operator) &&
                            CanEmitExpression(code, binary.Left, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitExpression(code, binary.Right, canDirectCall, allowRuntimeBoundary);
                    case AssignmentExpression assignment:
                        return assignment.Left is NameExpression target &&
                            (code.GetName(target).IsLocal ||
                                (allowRuntimeBoundary && !string.IsNullOrEmpty(code.GetName(target).Name))) &&
                            CanEmitExpression(code, assignment.Right, canDirectCall, allowRuntimeBoundary);
                    case CompoundExpression compound:
                        return CanEmitAssignmentTarget(code, compound.Left, canDirectCall, allowRuntimeBoundary) &&
                            GetDynamicBinaryOrComparisonSupported(compound.Operator.SimplerOperator) &&
                            CanEmitExpression(code, compound.Right, canDirectCall, allowRuntimeBoundary);
                    case UnaryExpression unary:
                        if (unary.Operator == Operator.PreIncrement || unary.Operator == Operator.PostIncrement ||
                            unary.Operator == Operator.PreDecrement || unary.Operator == Operator.PostDecrement)
                        {
                            if (unary.Expression is NameExpression mutation &&
                                code.GetName(mutation).IsLocal &&
                                FlowValueTypeFacts.IsNumeric(code.GetLocalType(code.GetName(mutation).Local)))
                            {
                                return true;
                            }
                            if (unary.Expression is GetElementExpression elementMutation)
                            {
                                var receiverType = code.GetExpressionType(elementMutation.Object);
                                var nativeMutation = FlowValueTypeFacts.IsPackedArray(receiverType) ||
                                    receiverType == FlowValueType.Array;
                                if (nativeMutation &&
                                    FlowValueTypeFacts.IsNumeric(code.GetExpressionType(elementMutation.Index)))
                                {
                                    return CanEmitExpression(
                                            code,
                                            elementMutation.Object,
                                            canDirectCall,
                                            allowRuntimeBoundary) &&
                                        CanEmitExpression(
                                            code,
                                            elementMutation.Index,
                                            canDirectCall,
                                            allowRuntimeBoundary);
                                }
                            }
                            return allowRuntimeBoundary &&
                                CanEmitAssignmentTarget(code, unary.Expression, canDirectCall, allowRuntimeBoundary);
                        }
                        return (unary.Operator == Operator.Negate || unary.Operator == Operator.LogicalNot ||
                            unary.Operator == Operator.BitwiseNot || unary.Operator == Operator.TypeOf) &&
                            CanEmitExpression(code, unary.Expression, canDirectCall, allowRuntimeBoundary);
                    case FunctionCallExpression call:
                        if (IsArrayFactoryCall(code, call) && !HasSpread(call.Arguments))
                        {
                            for (var i = 0; i < call.Arguments.Count; i++)
                            {
                                if (!CanEmitExpression(
                                    code,
                                    call.Arguments[i],
                                    canDirectCall,
                                    allowRuntimeBoundary))
                                {
                                    return false;
                                }
                            }
                            return true;
                        }
                        var hasSpread = HasSpread(call.Arguments);
                        var isDirect = !hasSpread &&
                            call.Target is NameExpression callTarget &&
                            canDirectCall(code.GetName(callTarget).DirectFunction);
                        if (!isDirect && (!allowRuntimeBoundary ||
                            !CanEmitExpression(code, call.Target, canDirectCall, allowRuntimeBoundary)))
                        {
                            return false;
                        }
                        for (var i = 0; i < call.Arguments.Count; i++)
                        {
                            var argument = call.Arguments[i] is SpreadExpression callSpread
                                ? callSpread.Expression
                                : call.Arguments[i];
                            if (!CanEmitExpression(code, argument, canDirectCall, allowRuntimeBoundary)) return false;
                        }
                        return true;
                    case NewExpression @new:
                        if (FlowValueTypeFacts.IsPackedArray(code.GetExpressionType(@new)) &&
                            !HasSpread(@new.Expression.Arguments) &&
                            @new.Expression.Arguments.Count <= 1)
                        {
                            return @new.Expression.Arguments.Count == 0 ||
                                CanEmitExpression(
                                    code,
                                    @new.Expression.Arguments[0],
                                    canDirectCall,
                                    allowRuntimeBoundary);
                        }
                        if (IsArrayConstruction(code, @new) &&
                            !HasSpread(@new.Expression.Arguments))
                        {
                            for (var i = 0; i < @new.Expression.Arguments.Count; i++)
                            {
                                if (!CanEmitExpression(
                                    code,
                                    @new.Expression.Arguments[i],
                                    canDirectCall,
                                    allowRuntimeBoundary))
                                {
                                    return false;
                                }
                            }
                            return true;
                        }
                        return allowRuntimeBoundary &&
                            CanEmitExpression(code, @new.Expression, canDirectCall, allowRuntimeBoundary);
                    case LambdaExpression lambda:
                        return allowRuntimeBoundary && lambda.Function?.Body != null;
                    case GetPropertyExpression property:
                        var propertySupported = TryGetStaticPropertyName(property.Property, out var propertyName) &&
                            CanEmitExpression(code, property.Object, canDirectCall, allowRuntimeBoundary);
                        return propertySupported &&
                            (allowRuntimeBoundary ||
                                ((FlowValueTypeFacts.IsPackedArray(code.GetExpressionType(property.Object)) ||
                                    code.GetExpressionType(property.Object) == FlowValueType.Array) &&
                                    StringComparer.Ordinal.Equals(propertyName, "length")));
                    case SetPropertyExpression property:
                        return allowRuntimeBoundary &&
                            TryGetStaticPropertyName(property.Property, out _) &&
                            CanEmitExpression(code, property.Object, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitExpression(code, property.Value, canDirectCall, allowRuntimeBoundary);
                    case GetElementExpression element:
                        var nativeGet = (FlowValueTypeFacts.IsPackedArray(code.GetExpressionType(element.Object)) ||
                                code.GetExpressionType(element.Object) == FlowValueType.Array) &&
                            FlowValueTypeFacts.IsNumeric(code.GetExpressionType(element.Index));
                        return (allowRuntimeBoundary || nativeGet) &&
                            CanEmitExpression(code, element.Object, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitExpression(code, element.Index, canDirectCall, allowRuntimeBoundary);
                    case SetElementExpression element:
                        var nativeSet = (FlowValueTypeFacts.IsPackedArray(code.GetExpressionType(element.Object)) ||
                                code.GetExpressionType(element.Object) == FlowValueType.Array) &&
                            FlowValueTypeFacts.IsNumeric(code.GetExpressionType(element.Index));
                        return (allowRuntimeBoundary || nativeSet) &&
                            CanEmitExpression(code, element.Object, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitExpression(code, element.Index, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitExpression(code, element.Value, canDirectCall, allowRuntimeBoundary);
                    case ArrayLiteralExpression array:
                        if (!allowRuntimeBoundary) return false;
                        for (var i = 0; i < array.Elements.Count; i++)
                        {
                            var item = array.Elements[i] is SpreadExpression spread ? spread.Expression : array.Elements[i];
                            if (!CanEmitExpression(code, item, canDirectCall, allowRuntimeBoundary)) return false;
                        }
                        return true;
                    case MapExpression map:
                        if (!allowRuntimeBoundary) return false;
                        for (var i = 0; i < map.Entries.Count; i++)
                        {
                            var entry = map.Entries[i];
                            if (entry is SpreadExpression spread)
                            {
                                if (!CanEmitExpression(code, spread.Expression, canDirectCall, allowRuntimeBoundary)) return false;
                            }
                            else if ((entry is not MapKeyValueExpression { Key: not null } && entry is not NameExpression) ||
                                !CanEmitExpression(code, GetMapEntryValue(entry), canDirectCall, allowRuntimeBoundary))
                            {
                                return false;
                            }
                        }
                        return true;
                    case IncludedExpression included:
                        return allowRuntimeBoundary &&
                            CanEmitExpression(code, included.Left, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitExpression(code, included.Right, canDirectCall, allowRuntimeBoundary);
                    case InExpression @in:
                        return allowRuntimeBoundary &&
                            CanEmitExpression(code, @in.Left, canDirectCall, allowRuntimeBoundary) &&
                            CanEmitExpression(code, @in.Right, canDirectCall, allowRuntimeBoundary);
                    case TemplateStringExpression template:
                        if (!allowRuntimeBoundary) return false;
                        for (var i = 0; i < template.Parts.Count; i++)
                        {
                            if (!template.Parts[i].IsLiteral &&
                                !CanEmitExpression(code, template.Parts[i].Expression, canDirectCall, allowRuntimeBoundary))
                            {
                                return false;
                            }
                        }
                        return true;
                    case GroupExpression group:
                        for (var i = 0; i < group.Expressions.Count; i++)
                        {
                            if (!CanEmitExpression(code, group.Expressions[i], canDirectCall, allowRuntimeBoundary)) return false;
                        }
                        return true;
                    default:
                        return false;
                }
            }

            private static bool CanEmitAssignmentTarget(
                TypedFunctionCode code,
                Expression target,
                Func<FunctionId, bool> canDirectCall,
                bool allowRuntimeBoundary)
            {
                if (target is NameExpression name)
                {
                    var binding = code.GetName(name);
                    return binding.IsLocal ||
                        (allowRuntimeBoundary && !string.IsNullOrEmpty(binding.Name));
                }
                if (target is GetPropertyExpression property)
                {
                    return allowRuntimeBoundary &&
                        TryGetStaticPropertyName(property.Property, out _) &&
                        CanEmitExpression(code, property.Object, canDirectCall, allowRuntimeBoundary);
                }
                if (target is GetElementExpression element)
                {
                    var nativeTarget = (FlowValueTypeFacts.IsPackedArray(code.GetExpressionType(element.Object)) ||
                            code.GetExpressionType(element.Object) == FlowValueType.Array) &&
                        FlowValueTypeFacts.IsNumeric(code.GetExpressionType(element.Index));
                    return (allowRuntimeBoundary || nativeTarget) &&
                        CanEmitExpression(code, element.Object, canDirectCall, allowRuntimeBoundary) &&
                        CanEmitExpression(code, element.Index, canDirectCall, allowRuntimeBoundary);
                }
                return false;
            }

            private static bool GetDynamicBinaryOrComparisonSupported(Operator op)
            {
                return op == Operator.Add || op == Operator.Subtract || op == Operator.Multiply ||
                    op == Operator.Divide || op == Operator.Modulo || op == Operator.Equal ||
                    op == Operator.NotEqual || op == Operator.LessThan || op == Operator.LessThanOrEqual ||
                    op == Operator.GreaterThan || op == Operator.GreaterThanOrEqual ||
                    op == Operator.BitwiseAnd || op == Operator.BitwiseOr || op == Operator.BitwiseXor ||
                    op == Operator.LeftShift || op == Operator.SignedRightShift ||
                    op == Operator.UnSignedRightShift || op == Operator.LogicalAnd || op == Operator.LogicalOr;
            }
        }
    }
}
