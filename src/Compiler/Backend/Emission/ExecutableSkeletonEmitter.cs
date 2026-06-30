using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Lowering;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tokens;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class ExecutableSkeletonEmitter
    {
        private static readonly Type[] s_standardParameterTypes = [typeof(ScriptContext), typeof(Span<ScriptDatum>)];
        private static readonly Type[][] s_fastParameterTypes =
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
        private readonly ExecutableMethod[] _methodsByFunction;
        private readonly Stack<Label> _breakLabels = new();
        private readonly Stack<Label> _continueLabels = new();
        private FunctionPlan[] _functionsById;
        private bool _prepared;
        private bool _forceAllExecutable;
        private FunctionPlan _function;
        private ILGenerator _il;
        private LocalBuilder[] _locals;
        private LocalBuilder _capturedUpvalues;
        private LocalBuilder _contextUpvalues;
        private Dictionary<int, int> _capturedLocalByLocalSlot;
        private ParameterLoadPlan[] _parameterLoadPlans;
        private int _cilLocalCount;
        private long _lastLocation;
        private int _lastLocationEndOffset;

        public ExecutableSkeletonEmitter(EmissionSession session, ModulePlan module)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _module = module ?? throw new ArgumentNullException(nameof(module));

            var maxFunctionId = -1;
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var id = module.Functions[i].Id.Value;
                if (id > maxFunctionId)
                {
                    maxFunctionId = id;
                }
            }

            _methodsByFunction = maxFunctionId >= 0 ? new ExecutableMethod[maxFunctionId + 1] : Array.Empty<ExecutableMethod>();
        }

        public void Prepare()
        {
            Prepare(forceAllExecutable: false);
        }

        public void Prepare(bool forceAllExecutable)
        {
            if (_prepared)
            {
                return;
            }

            _prepared = true;
            _forceAllExecutable = forceAllExecutable;
            var executable = !forceAllExecutable && _session.CollectDiagnostics ? BuildExecutableSet() : null;
            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                if (executable != null && !executable.Contains(function.Id))
                {
                    continue;
                }
                if (executable == null && !forceAllExecutable && !CanEverEmit(function))
                {
                    continue;
                }

                var convention = SelectCallConvention(function);
                var parameterTypes = GetParameterTypes(convention);
                var name = string.IsNullOrEmpty(function.Name)
                    ? "lambda_" + function.Id.Value
                    : function.Name;
                var methodName = convention == FunctionCallConvention.Span
                    ? name
                    : name + "$fast" + GetFastArity(convention);
                var (method, il) = _session.Builder.DefineMethod(_module.Name, methodName, typeof(ScriptDatum), parameterTypes);
                function.CallConvention = convention;
                function.Method = method;
                function.DynamicDelegateId = 0;
                _methodsByFunction[function.Id.Value] = new ExecutableMethod(method, il, convention);
            }
        }

        public bool TryEmit(FunctionPlan function, out MethodInfo method, out int localCount)
        {
            method = null;
            localCount = 0;
            if (!_prepared)
            {
                Prepare(_forceAllExecutable);
            }

            if ((uint)function.Id.Value >= (uint)_methodsByFunction.Length)
            {
                return false;
            }

            ref var executable = ref _methodsByFunction[function.Id.Value];
            if (!executable.IsDefined || executable.Emitted)
            {
                return false;
            }

            executable.Emitted = true;
            method = executable.Method;
            _function = function;
            _il = executable.IL;
            _parameterLoadPlans = BuildParameterLoadPlans(function, executable.Convention);
            _cilLocalCount = 0;
            _locals = DeclareLocals(function);
            _capturedLocalByLocalSlot = BuildCapturedLocalMap(function);
            _capturedUpvalues = null;
            _contextUpvalues = null;
            try
            {
                InitializeCapturedLocals(function);
                _session.Builder.SetDebuggerMetadata(method, CreateDebuggerMetadata(function, executable.Convention));
                InitializeParameters(function, executable.Convention);
                EmitLocation(function.Body);
                EmitStatement(function.Body);
                LoadNull();
                _il.Emit(OpCodes.Ret);
                localCount = _cilLocalCount;
                return true;
            }
            finally
            {
                ReturnLocals(function);
                _function = null;
                _locals = null;
                _capturedLocalByLocalSlot = null;
                _parameterLoadPlans = null;
                _capturedUpvalues = null;
                _contextUpvalues = null;
            }
        }

        private HashSet<FunctionId> BuildExecutableSet()
        {
            var executable = new HashSet<FunctionId>();
            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                if (CanEverEmit(function))
                {
                    executable.Add(function.Id);
                }
            }

            var changed = true;
            while (changed)
            {
                changed = false;
                for (var i = 0; i < _module.Functions.Count; i++)
                {
                    var function = _module.Functions[i];
                    if (executable.Contains(function.Id) && !CanEmit(function, executable))
                    {
                        executable.Remove(function.Id);
                        changed = true;
                    }
                }
            }

            return executable;
        }

        private static bool CanEverEmit(FunctionPlan function)
        {
            return function != null &&
                function.Body != null;
        }

        private bool CanEmit(FunctionPlan function, IReadOnlySet<FunctionId> executableFunctions)
        {
            return CanEverEmit(function) &&
                CanEmitParameterDefaults(function, executableFunctions) &&
                CanEmitStatement(function.Body, executableFunctions);
        }

        private LocalBuilder[] DeclareLocals(FunctionPlan function)
        {
            if (function.LocalSlots.Length == 0)
            {
                return Array.Empty<LocalBuilder>();
            }

            var locals = ArrayPool<LocalBuilder>.Shared.Rent(function.LocalSlots.Length);
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                if (CanLoadParameterDirectly(function.LocalSlots[i].Id))
                {
                    locals[i] = null;
                    continue;
                }

                locals[i] = _il.DeclareLocal(typeof(ScriptDatum));
                _cilLocalCount++;
                _session.Builder.SetLocalSymInfo(locals[i], function.LocalSlots[i].Name);
            }
            return locals;
        }

        private void ReturnLocals(FunctionPlan function)
        {
            if (_locals == null || function.LocalSlots.Length == 0)
            {
                return;
            }

            ArrayPool<LocalBuilder>.Shared.Return(_locals, clearArray: true);
        }

        private void InitializeParameters(FunctionPlan function, FunctionCallConvention convention)
        {
            var parameterIndex = 0;
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                var slot = function.LocalSlots[i];
                if (!slot.IsParameter)
                {
                    continue;
                }

                var currentParameterIndex = parameterIndex;
                parameterIndex++;
                if (CanLoadParameterDirectly(slot.Id))
                {
                    continue;
                }

                void EmitParameterValue()
                {
                    if (convention == FunctionCallConvention.Span)
                    {
                        _il.Emit(OpCodes.Ldarg_1);
                        _il.Emit(OpCodes.Ldc_I4, currentParameterIndex);
                        var defaultValue = GetParameterDefault(function, currentParameterIndex);
                        if (defaultValue != null)
                        {
                            EmitExpression(defaultValue);
                            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_TryGetArg);
                        }
                        else
                        {
                            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetArg);
                        }
                    }
                    else
                    {
                        _il.Emit(OpCodes.Ldarg, currentParameterIndex + 1);
                    }
                }

                EmitStoreLocalFromProducer(slot.Id, EmitParameterValue);
            }
        }

        private bool CanLoadParameterDirectly(LocalSlotId slot)
        {
            return slot.IsValid &&
                _parameterLoadPlans != null &&
                (uint)slot.Value < (uint)_parameterLoadPlans.Length &&
                _parameterLoadPlans[slot.Value].CanLoadDirectly;
        }

        private void EmitDirectParameterLoad(LocalSlotId slot)
        {
            var plan = _parameterLoadPlans[slot.Value];
            if (plan.Convention == FunctionCallConvention.Span)
            {
                _il.Emit(OpCodes.Ldarg_1);
                _il.Emit(OpCodes.Ldc_I4, plan.ParameterIndex);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetArg);
                return;
            }

            _il.Emit(OpCodes.Ldarg, plan.ParameterIndex + 1);
        }

        private static LoweredExpression GetParameterDefault(FunctionPlan function, int parameterIndex)
        {
            return function.ParameterDefaults != null &&
                parameterIndex >= 0 &&
                parameterIndex < function.ParameterDefaults.Length
                ? function.ParameterDefaults[parameterIndex]
                : null;
        }

        private static ParameterLoadPlan[] BuildParameterLoadPlans(FunctionPlan function, FunctionCallConvention convention)
        {
            if (function.LocalSlots.Length == 0)
            {
                return Array.Empty<ParameterLoadPlan>();
            }

            var usage = LocalUsageAnalyzer.Analyze(function);
            var plans = new ParameterLoadPlan[function.LocalSlots.Length];
            var parameterIndex = 0;
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                var slot = function.LocalSlots[i];
                if (!slot.IsParameter)
                {
                    continue;
                }

                var direct = convention != FunctionCallConvention.Span &&
                    !function.UsesArgumentsObject &&
                    function.CapturedLocalSlots.Length == 0 &&
                    GetParameterDefault(function, parameterIndex) == null &&
                    usage.Reads[i] == 1 &&
                    usage.Writes[i] == 0 &&
                    usage.Addresses[i] == 0;

                plans[i] = new ParameterLoadPlan(direct, parameterIndex, convention);
                parameterIndex++;
            }

            return plans;
        }

        private static Dictionary<int, int> BuildCapturedLocalMap(FunctionPlan function)
        {
            if (function.CapturedLocalSlots.Length == 0)
            {
                return null;
            }

            var map = new Dictionary<int, int>(function.CapturedLocalSlots.Length);
            for (var i = 0; i < function.CapturedLocalSlots.Length; i++)
            {
                var slot = function.CapturedLocalSlots[i];
                if (slot.SourceLocal.IsValid)
                {
                    map[slot.SourceLocal.Value] = slot.Id.Value;
                }
            }

            return map;
        }

        private void InitializeCapturedLocals(FunctionPlan function)
        {
            if (function.CapturedLocalSlots.Length == 0)
            {
                return;
            }

            _capturedUpvalues = DeclareLocal(typeof(Upvalue[]));
            _il.Emit(OpCodes.Ldc_I4, function.CapturedLocalSlots.Length);
            _il.Emit(OpCodes.Newarr, typeof(Upvalue));
            _il.Emit(OpCodes.Stloc, _capturedUpvalues);

            for (var i = 0; i < function.CapturedLocalSlots.Length; i++)
            {
                _il.Emit(OpCodes.Ldloc, _capturedUpvalues);
                _il.Emit(OpCodes.Ldc_I4, i);
                _il.Emit(OpCodes.Newobj, RuntimeMetadata.Upvalue_CtorEmpty);
                _il.Emit(OpCodes.Stelem_Ref);
            }

        }

        private void InitializeCapturedLocal(int index)
        {
            _il.Emit(OpCodes.Ldloc, _capturedUpvalues);
            _il.Emit(OpCodes.Ldc_I4, index);
            _il.Emit(OpCodes.Newobj, RuntimeMetadata.Upvalue_CtorEmpty);
            _il.Emit(OpCodes.Stelem_Ref);
        }

        private bool TryGetCapturedLocalIndex(LocalSlotId slot, out int index)
        {
            if (slot.IsValid &&
                _capturedLocalByLocalSlot != null &&
                _capturedLocalByLocalSlot.TryGetValue(slot.Value, out index))
            {
                return true;
            }

            index = -1;
            return false;
        }

        private void EmitLoadLocal(LocalSlotId slot)
        {
            if (CanLoadParameterDirectly(slot))
            {
                EmitDirectParameterLoad(slot);
                return;
            }

            if (TryGetCapturedLocalIndex(slot, out var index))
            {
                EmitLoadCapturedUpvalue(index);
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.Upvalue_Value);
                return;
            }

            _il.Emit(OpCodes.Ldloc, _locals[slot.Value]);
        }

        private void EmitStoreLocalFromStack(LocalSlotId slot)
        {
            if (CanLoadParameterDirectly(slot))
            {
                throw new InvalidOperationException("Direct parameter load slot cannot be written.");
            }

            if (TryGetCapturedLocalIndex(slot, out var index))
            {
                var value = DeclareTemp();
                _il.Emit(OpCodes.Stloc, value);
                EmitLoadCapturedUpvalue(index);
                _il.Emit(OpCodes.Ldloc, value);
                _il.Emit(OpCodes.Stfld, RuntimeMetadata.Upvalue_Value);
                return;
            }

            _il.Emit(OpCodes.Stloc, _locals[slot.Value]);
        }

        private void EmitStoreLocalFromProducer(LocalSlotId slot, Action emitValue)
        {
            if (emitValue == null) throw new ArgumentNullException(nameof(emitValue));

            if (CanLoadParameterDirectly(slot))
            {
                throw new InvalidOperationException("Direct parameter load slot cannot be written.");
            }

            if (TryGetCapturedLocalIndex(slot, out var index))
            {
                EmitLoadCapturedUpvalue(index);
                emitValue();
                _il.Emit(OpCodes.Stfld, RuntimeMetadata.Upvalue_Value);
                return;
            }

            emitValue();
            _il.Emit(OpCodes.Stloc, _locals[slot.Value]);
        }

        private void EmitLoadLocalAddress(LocalSlotId slot)
        {
            if (CanLoadParameterDirectly(slot))
            {
                throw new InvalidOperationException("Direct parameter load slot cannot be addressed.");
            }

            if (TryGetCapturedLocalIndex(slot, out var index))
            {
                EmitLoadCapturedUpvalue(index);
                _il.Emit(OpCodes.Ldflda, RuntimeMetadata.Upvalue_Value);
                return;
            }

            _il.Emit(OpCodes.Ldloca, _locals[slot.Value]);
        }

        private void EmitLoadNameAddress(LoweredNameExpression name)
        {
            if (name.LocalSlot.IsValid && !name.ModuleSymbol.IsValid)
            {
                EmitLoadLocalAddress(name.LocalSlot);
                return;
            }

            if (name.UpvalueSlot.IsValid && !name.ModuleSymbol.IsValid)
            {
                EmitLoadContextUpvalue(name.UpvalueSlot.Value);
                _il.Emit(OpCodes.Ldflda, RuntimeMetadata.Upvalue_Value);
                return;
            }

            throw new NotSupportedException("Name cannot be addressed by reference.");
        }

        private void EmitLoadCapturedUpvalue(int index)
        {
            _il.Emit(OpCodes.Ldloc, _capturedUpvalues);
            _il.Emit(OpCodes.Ldc_I4, index);
            _il.Emit(OpCodes.Ldelem_Ref);
        }

        private void EmitLoadContextUpvalue(int index)
        {
            EmitLoadContextUpvalues();
            _il.Emit(OpCodes.Ldc_I4, index);
            _il.Emit(OpCodes.Ldelem_Ref);
        }

        private void EmitLoadContextUpvalues()
        {
            if (_contextUpvalues == null)
            {
                _contextUpvalues = DeclareLocal(typeof(Upvalue[]));
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Upvalues);
                _il.Emit(OpCodes.Stloc, _contextUpvalues);
            }

            _il.Emit(OpCodes.Ldloc, _contextUpvalues);
        }

        private void EmitClosureUpvalue(UpvalueSlot slot)
        {
            if (slot.IsInherited)
            {
                EmitLoadContextUpvalue(slot.SourceUpvalue.Value);
                return;
            }

            if (TryGetCapturedLocalIndex(slot.SourceLocal, out var index))
            {
                EmitLoadCapturedUpvalue(index);
                return;
            }

            throw new NotSupportedException("Unresolved closure upvalue '" + slot.Name + "'.");
        }

        private void EmitStatement(LoweredStatement statement)
        {
            MarkStatementSequencePoint(statement);
            switch (statement)
            {
                case null:
                    return;
                case LoweredNoOpStatement:
                    return;
                case LoweredBlockStatement block:
                    EmitBlock(block);
                    return;
                case LoweredReturnStatement returnStatement:
                    EmitExpressionOrNull(returnStatement.Expression);
                    _il.Emit(OpCodes.Ret);
                    return;
                case LoweredVariableDeclarationStatement variable:
                    EmitVariableDeclaration(variable);
                    return;
                case LoweredObjectDestructuringDeclarationStatement objectDestructuring:
                    EmitObjectDestructuringDeclaration(objectDestructuring);
                    return;
                case LoweredArrayDestructuringDeclarationStatement arrayDestructuring:
                    EmitArrayDestructuringDeclaration(arrayDestructuring);
                    return;
                case LoweredFunctionDeclarationStatement function:
                    EmitFunctionDeclaration(function);
                    return;
                case LoweredExpressionStatement expressionStatement:
                    EmitExpressionDiscarded(expressionStatement.Expression);
                    return;
                case LoweredIfStatement ifStatement:
                    EmitIf(ifStatement);
                    return;
                case LoweredWhileStatement whileStatement:
                    EmitWhile(whileStatement);
                    return;
                case LoweredForStatement forStatement:
                    EmitFor(forStatement);
                    return;
                case LoweredForInStatement forInStatement:
                    EmitForIn(forInStatement);
                    return;
                case LoweredTryStatement tryStatement:
                    EmitTry(tryStatement);
                    return;
                case LoweredThrowStatement throwStatement:
                    EmitThrow(throwStatement);
                    return;
                case LoweredDeleteStatement deleteStatement:
                    EmitDelete(deleteStatement);
                    return;
                case LoweredDebuggerStatement:
                    EmitDebugger();
                    return;
                case LoweredBreakStatement:
                    _il.Emit(OpCodes.Br, _breakLabels.Peek());
                    return;
                case LoweredContinueStatement:
                    _il.Emit(OpCodes.Br, _continueLabels.Peek());
                    return;
                case LoweredUnsupportedStatement unsupported:
                    throw CreateUnsupported(unsupported.Source, isExpression: false);
                default:
                    throw new NotSupportedException(statement.GetType().Name);
            }
        }

        private void MarkStatementSequencePoint(LoweredStatement statement)
        {
            if (statement == null ||
                statement is LoweredNoOpStatement ||
                statement is LoweredBlockStatement)
            {
                return;
            }

            EmitLocation(statement);
            _session.Builder.MarkSequencePoint(statement.Range, _il);
        }

        private void EmitIf(LoweredIfStatement statement)
        {
            var elseLabel = _il.DefineLabel();
            var endLabel = _il.DefineLabel();

            EmitCondition(statement.Condition);
            _il.Emit(OpCodes.Brfalse, elseLabel);
            EmitStatement(statement.Body);
            if (statement.Else != null)
            {
                _il.Emit(OpCodes.Br, endLabel);
            }

            _il.MarkLabel(elseLabel);
            EmitStatement(statement.Else);
            if (statement.Else != null)
            {
                _il.MarkLabel(endLabel);
            }
        }

        private void EmitBlock(LoweredBlockStatement block)
        {
            if (!ReferenceEquals(block, _function.Body))
            {
                InitializeCapturedLocalsForBlock(block);
            }

            for (var i = 0; i < block.Statements.Length; i++)
            {
                EmitStatement(block.Statements[i]);
            }
        }

        private void InitializeCapturedLocalsForBlock(LoweredBlockStatement block)
        {
            for (var i = 0; i < block.Statements.Length; i++)
            {
                switch (block.Statements[i])
                {
                    case LoweredVariableDeclarationStatement variable:
                        InitializeCapturedLocalForSlot(variable.Slot);
                        break;
                    case LoweredObjectDestructuringDeclarationStatement objectDestructuring:
                        for (var bindingIndex = 0; bindingIndex < objectDestructuring.Bindings.Length; bindingIndex++)
                        {
                            InitializeCapturedLocalForSlot(objectDestructuring.Bindings[bindingIndex].Slot);
                        }
                        break;
                    case LoweredArrayDestructuringDeclarationStatement arrayDestructuring:
                        for (var bindingIndex = 0; bindingIndex < arrayDestructuring.Bindings.Length; bindingIndex++)
                        {
                            InitializeCapturedLocalForSlot(arrayDestructuring.Bindings[bindingIndex].Slot);
                        }
                        break;
                }
            }
        }

        private void InitializeCapturedLocalForSlot(LocalSlotId slot)
        {
            if (TryGetCapturedLocalIndex(slot, out var capturedIndex))
            {
                InitializeCapturedLocal(capturedIndex);
            }
        }

        private void EmitFunctionDeclaration(LoweredFunctionDeclarationStatement statement)
        {
            var function = GetFunction(statement.Function);
            EmitStoreLocalFromProducer(statement.LocalSlot, () =>
            {
                ClosureMaterializer.EmitClosure(_session, _il, function, EmitClosureUpvalue);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
            });
        }

        private void EmitVariableDeclaration(LoweredVariableDeclarationStatement statement)
        {
            EmitStoreLocalFromProducer(statement.Slot, () => EmitExpressionOrNull(statement.Initializer));
        }

        private void EmitObjectDestructuringDeclaration(LoweredObjectDestructuringDeclarationStatement statement)
        {
            var targetLocal = DeclareLocal(typeof(ScriptObject));
            EmitExpression(statement.Initializer);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Stloc, targetLocal);

            for (var i = 0; i < statement.Bindings.Length; i++)
            {
                var binding = statement.Bindings[i];
                EmitStoreLocalFromProducer(binding.Slot, () =>
                {
                    _il.Emit(OpCodes.Ldloc, targetLocal);
                    _il.Emit(OpCodes.Ldarg_0);
                    _session.Builder.LoadStringConstant(_il, binding.Property.Value);
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_GetPropertyDatum);
                });
            }
        }

        private void EmitArrayDestructuringDeclaration(LoweredArrayDestructuringDeclarationStatement statement)
        {
            var arrayLocal = DeclareLocal(typeof(ScriptArray));
            EmitExpression(statement.Initializer);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Castclass, typeof(ScriptArray));
            _il.Emit(OpCodes.Stloc, arrayLocal);

            for (var i = 0; i < statement.Bindings.Length; i++)
            {
                var binding = statement.Bindings[i];
                if (binding.IsRest)
                {
                    _il.Emit(OpCodes.Ldloc, arrayLocal);
                    _il.Emit(OpCodes.Ldc_I4, binding.Index);
                    _il.Emit(OpCodes.Ldloc, arrayLocal);
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_get_Length);
                    _il.Emit(OpCodes.Ldc_I4, binding.TrailingCount);
                    _il.Emit(OpCodes.Sub);
                    EmitLoadLocalAddress(binding.Slot);
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_SliceTo);
                    continue;
                }

                EmitStoreLocalFromProducer(binding.Slot, () =>
                {
                    _il.Emit(OpCodes.Ldloc, arrayLocal);
                    if (binding.TrailingCount > 0)
                    {
                        _il.Emit(OpCodes.Ldloc, arrayLocal);
                        _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_get_Length);
                        _il.Emit(OpCodes.Ldc_I4, binding.TrailingCount);
                        _il.Emit(OpCodes.Sub);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Ldc_I4, binding.Index);
                    }
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_Get);
                });
            }
        }

        private void EmitWhile(LoweredWhileStatement statement)
        {
            var beginLabel = _il.DefineLabel();
            var endLabel = _il.DefineLabel();

            _continueLabels.Push(beginLabel);
            _breakLabels.Push(endLabel);

            _il.MarkLabel(beginLabel);
            EmitCondition(statement.Condition);
            _il.Emit(OpCodes.Brfalse, endLabel);
            EmitStatement(statement.Body);
            _il.Emit(OpCodes.Br, beginLabel);
            _il.MarkLabel(endLabel);

            _breakLabels.Pop();
            _continueLabels.Pop();
        }

        private void EmitFor(LoweredForStatement statement)
        {
            var conditionLabel = _il.DefineLabel();
            var incrementLabel = _il.DefineLabel();
            var endLabel = _il.DefineLabel();

            _continueLabels.Push(incrementLabel);
            _breakLabels.Push(endLabel);

            EmitStatement(statement.Initializer);
            _il.MarkLabel(conditionLabel);
            if (statement.Condition != null)
            {
                EmitCondition(statement.Condition);
                _il.Emit(OpCodes.Brfalse, endLabel);
            }

            EmitStatement(statement.Body);
            _il.MarkLabel(incrementLabel);
            if (statement.Incrementor != null)
            {
                EmitExpressionDiscarded(statement.Incrementor);
            }
            _il.Emit(OpCodes.Br, conditionLabel);
            _il.MarkLabel(endLabel);

            _breakLabels.Pop();
            _continueLabels.Pop();
        }

        private void EmitForIn(LoweredForInStatement statement)
        {
            var conditionLabel = _il.DefineLabel();
            var incrementLabel = _il.DefineLabel();
            var endLabel = _il.DefineLabel();

            _continueLabels.Push(incrementLabel);
            _breakLabels.Push(endLabel);

            EmitStatement(statement.Initializer);
            EmitExpression(statement.Iterator.Right);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_GetIterator);

            var iterator = DeclareLocal(typeof(ScriptEnumerator));
            _il.Emit(OpCodes.Stloc, iterator);

            _il.MarkLabel(conditionLabel);
            _il.Emit(OpCodes.Ldloc, iterator);
            EmitLoadNameAddress((LoweredNameExpression)statement.Iterator.Left);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptEnumerator_NextValue);
            _il.Emit(OpCodes.Brfalse, endLabel);

            EmitStatement(statement.Body);

            _il.MarkLabel(incrementLabel);
            _il.Emit(OpCodes.Br, conditionLabel);
            _il.MarkLabel(endLabel);

            _breakLabels.Pop();
            _continueLabels.Pop();
        }

        private void EmitTry(LoweredTryStatement statement)
        {
            if (!HasExceptionHandler(statement))
            {
                EmitStatement(statement.Body);
                return;
            }

            _il.BeginExceptionBlock();
            EmitStatement(statement.Body);

            if (HasCatch(statement))
            {
                _il.BeginCatchBlock(typeof(Exception));
                if (statement.CatchSlot.IsValid)
                {
                    if (TryGetCapturedLocalIndex(statement.CatchSlot, out var capturedIndex))
                    {
                        InitializeCapturedLocal(capturedIndex);
                    }
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ExceptionToErrorWithContext);
                    EmitStoreLocalFromStack(statement.CatchSlot);
                }
                else
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ExceptionToErrorWithContext);
                    _il.Emit(OpCodes.Pop);
                }

                EmitStatement(statement.CatchBody);
            }
            else
            {
                _il.BeginCatchBlock(typeof(Exception));
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ExceptionToErrorWithContext);
                _il.Emit(OpCodes.Pop);
            }

            if (statement.FinallyBody != null)
            {
                _il.BeginFinallyBlock();
                EmitStatement(statement.FinallyBody);
            }

            _il.EndExceptionBlock();
        }

        private void EmitThrow(LoweredThrowStatement statement)
        {
            EmitExpressionOrNull(statement.Expression);
            EmitLocation(statement);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_Throw);
        }

        private void EmitDelete(LoweredDeleteStatement statement)
        {
            switch (statement.Expression)
            {
                case LoweredGetPropertyExpression property:
                    EmitDeleteProperty(property);
                    return;
                case LoweredGetElementExpression element:
                    EmitDeleteElement(element);
                    return;
                default:
                    throw new NotSupportedException("Delete " + statement.Expression?.GetType().Name);
            }
        }

        private void EmitDeleteProperty(LoweredGetPropertyExpression expression)
        {
            if (!TryGetStaticPropertyName(expression, out var name))
            {
                throw new NotSupportedException("Dynamic property delete");
            }

            _il.Emit(OpCodes.Ldarg_0);
            EmitExpression(expression.Instance);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_DeleteProperty);
        }

        private void EmitDeleteElement(LoweredGetElementExpression expression)
        {
            _il.Emit(OpCodes.Ldarg_0);
            EmitExpression(expression.Instance);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            EmitExpression(expression.Index);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_DeleteElement);
        }

        private void EmitDebugger()
        {
            if (
#if NET9_0_OR_GREATER
                _session.Builder is PersistedBuilder &&
#endif
                _session.Options.Optimization.Level == OptimizeOptions.Debug)
            {
                _il.Emit(OpCodes.Break);
            }
        }

        private void EmitCondition(LoweredExpression expression)
        {
            if (TryEmitCondition(expression))
            {
                return;
            }

            EmitExpressionOrNull(expression);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ToBoolean);
        }

        private void EmitExpressionOrNull(LoweredExpression expression)
        {
            if (expression == null)
            {
                LoadNull();
                return;
            }

            EmitExpression(expression);
        }

        private void EmitExpressionDiscarded(LoweredExpression expression)
        {
            if (expression == null)
            {
                return;
            }

            if (expression is LoweredUnaryExpression unary &&
                GetMutationVoidMethod(unary.Operator) != null)
            {
                EmitMutationUnaryDiscarded(unary);
                return;
            }

            switch (expression)
            {
                case LoweredAssignmentExpression assignment:
                    EmitAssignmentDiscarded(assignment);
                    return;
                case LoweredCompoundExpression compound:
                    EmitCompoundDiscarded(compound);
                    return;
                case LoweredSetPropertyExpression property:
                    EmitSetPropertyDiscarded(property);
                    return;
                case LoweredSetElementExpression element:
                    EmitSetElementDiscarded(element);
                    return;
            }

            EmitExpression(expression);
            _il.Emit(OpCodes.Pop);
        }

        private void EmitExpression(LoweredExpression expression)
        {
            switch (expression)
            {
                case LoweredLiteralExpression literal:
                    EmitLiteral(literal);
                    return;
                case LoweredNameExpression name:
                    EmitName(name);
                    return;
                case LoweredBinaryExpression binary:
                    EmitBinary(binary);
                    return;
                case LoweredTemplateStringExpression template:
                    EmitTemplateString(template);
                    return;
                case LoweredAssignmentExpression assignment:
                    EmitAssignment(assignment);
                    return;
                case LoweredCompoundExpression compound:
                    EmitCompound(compound);
                    return;
                case LoweredUnaryExpression unary:
                    EmitUnary(unary);
                    return;
                case LoweredInExpression inExpression:
                    EmitIn(inExpression);
                    return;
                case LoweredGetPropertyExpression property:
                    EmitGetProperty(property);
                    return;
                case LoweredSetPropertyExpression property:
                    EmitSetProperty(property);
                    return;
                case LoweredGetElementExpression element:
                    EmitGetElement(element);
                    return;
                case LoweredSetElementExpression element:
                    EmitSetElement(element);
                    return;
                case LoweredArrayLiteralExpression array:
                    EmitArrayLiteral(array);
                    return;
                case LoweredMapExpression map:
                    EmitMap(map);
                    return;
                case LoweredLambdaExpression lambda:
                    EmitLambda(lambda);
                    return;
                case LoweredNewExpression @new:
                    EmitNew(@new);
                    return;
                case LoweredCallExpression call when TryResolveDirectCallTarget(call, out var target):
                    EmitDirectCall(call, target);
                    return;
                case LoweredCallExpression call when IsPropertyCall(call):
                    EmitPropertyCall(call);
                    return;
                case LoweredCallExpression call:
                    EmitRegularCall(call);
                    return;
                case LoweredUnsupportedExpression unsupported:
                    throw CreateUnsupported(unsupported.Source, isExpression: true);
                default:
                    throw new NotSupportedException(expression.GetType().Name);
            }
        }

        private void EmitBinary(LoweredBinaryExpression expression)
        {
            if (expression.Operator == Operator.LogicalAnd)
            {
                EmitLogical(expression, branchWhenTrue: false);
                return;
            }
            if (expression.Operator == Operator.LogicalOr)
            {
                EmitLogical(expression, branchWhenTrue: true);
                return;
            }
            if (TryEmitStringAddition(expression))
            {
                return;
            }

            EmitExpression(expression.Left);
            EmitExpression(expression.Right);
            _il.Emit(OpCodes.Call, GetBinaryMethod(expression.Operator));
        }

        private bool TryEmitCondition(LoweredExpression expression)
        {
            switch (expression)
            {
                case null:
                    _il.Emit(OpCodes.Ldc_I4_0);
                    return true;
                case LoweredLiteralExpression literal:
                    return TryEmitLiteralCondition(literal);
                case LoweredBinaryExpression binary:
                    return TryEmitBinaryCondition(binary);
                case LoweredUnaryExpression unary when unary.Operator == Operator.LogicalNot:
                    EmitCondition(unary.Expression);
                    _il.Emit(OpCodes.Ldc_I4_0);
                    _il.Emit(OpCodes.Ceq);
                    return true;
                case LoweredInExpression inExpression:
                    EmitExpression(inExpression.Right);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
                    EmitExpression(inExpression.Left);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_IncludedBool);
                    return true;
                default:
                    return false;
            }
        }

        private bool TryEmitLiteralCondition(LoweredLiteralExpression expression)
        {
            switch (expression.Token)
            {
                case BooleanToken boolean:
                    _il.Emit(boolean.BoolValue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    return true;
                case NullToken:
                    _il.Emit(OpCodes.Ldc_I4_0);
                    return true;
                case NumberToken number:
                    _il.Emit(number.NumberValue != 0 && !double.IsNaN(number.NumberValue) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    return true;
                case StringToken stringToken:
                    _il.Emit(string.IsNullOrEmpty(stringToken.Value) ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);
                    return true;
                default:
                    return false;
            }
        }

        private bool TryEmitBinaryCondition(LoweredBinaryExpression expression)
        {
            if (expression.Operator == Operator.LogicalAnd)
            {
                var falseLabel = _il.DefineLabel();
                var endLabel = _il.DefineLabel();

                EmitCondition(expression.Left);
                _il.Emit(OpCodes.Brfalse, falseLabel);
                EmitCondition(expression.Right);
                _il.Emit(OpCodes.Br, endLabel);
                _il.MarkLabel(falseLabel);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.MarkLabel(endLabel);
                return true;
            }

            if (expression.Operator == Operator.LogicalOr)
            {
                var trueLabel = _il.DefineLabel();
                var endLabel = _il.DefineLabel();

                EmitCondition(expression.Left);
                _il.Emit(OpCodes.Brtrue, trueLabel);
                EmitCondition(expression.Right);
                _il.Emit(OpCodes.Br, endLabel);
                _il.MarkLabel(trueLabel);
                _il.Emit(OpCodes.Ldc_I4_1);
                _il.MarkLabel(endLabel);
                return true;
            }

            var conditionMethod = GetBinaryConditionMethod(expression.Operator);
            if (conditionMethod == null)
            {
                return false;
            }

            EmitExpression(expression.Left);
            EmitExpression(expression.Right);
            _il.Emit(OpCodes.Call, conditionMethod);
            return true;
        }

        private bool TryEmitStringAddition(LoweredBinaryExpression expression)
        {
            if (expression.Operator != Operator.Add)
            {
                return false;
            }

            if (expression.Left is LoweredBinaryExpression leftBinary &&
                leftBinary.Operator == Operator.Add &&
                TryGetStringLiteral(leftBinary.Right, out var middle))
            {
                EmitExpression(leftBinary.Left);
                _session.Builder.LoadStringConstant(_il, middle);
                EmitExpression(expression.Right);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_AddStringMiddle);
                return true;
            }

            if (TryGetStringLiteral(expression.Right, out var right))
            {
                EmitExpression(expression.Left);
                _session.Builder.LoadStringConstant(_il, right);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_AddStringRight);
                return true;
            }

            if (TryGetStringLiteral(expression.Left, out var left))
            {
                _session.Builder.LoadStringConstant(_il, left);
                EmitExpression(expression.Right);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_AddStringLeft);
                return true;
            }

            return false;
        }

        private void EmitTemplateString(LoweredTemplateStringExpression expression)
        {
            var elementCount = CountTemplateStringElements(expression.Parts);
            if (elementCount == 0)
            {
                _session.Builder.LoadStringConstant(_il, string.Empty);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromString);
                return;
            }

            if (elementCount <= 4)
            {
                EmitTemplateStringConcat(expression.Parts, elementCount);
                return;
            }

            EmitTemplateStringBuilder(expression.Parts);
        }

        private static int CountTemplateStringElements(LoweredTemplateStringPart[] parts)
        {
            var count = 0;
            for (var i = 0; i < parts.Length; i++)
            {
                if (!parts[i].IsLiteral || parts[i].Literal.Length != 0)
                {
                    count++;
                }
            }

            return count;
        }

        private void EmitTemplateStringConcat(LoweredTemplateStringPart[] parts, int elementCount)
        {
            for (var i = 0; i < parts.Length; i++)
            {
                EmitTemplateStringElement(parts[i]);
            }

            switch (elementCount)
            {
                case 1:
                    break;
                case 2:
                    _il.Emit(OpCodes.Call, RuntimeMetadata.String_Concat2);
                    break;
                case 3:
                    _il.Emit(OpCodes.Call, RuntimeMetadata.String_Concat3);
                    break;
                case 4:
                    _il.Emit(OpCodes.Call, RuntimeMetadata.String_Concat4);
                    break;
                default:
                    throw new NotSupportedException("Template string concat element count " + elementCount);
            }

            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromString);
        }

        private void EmitTemplateStringElement(LoweredTemplateStringPart part)
        {
            if (part.IsLiteral)
            {
                if (part.Literal.Length != 0)
                {
                    _session.Builder.LoadStringConstant(_il, part.Literal);
                }
                return;
            }

            EmitExpression(part.Expression);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToString);
        }

        private void EmitTemplateStringBuilder(LoweredTemplateStringPart[] parts)
        {
            var dynamicStrings = ArrayPool<LocalBuilder>.Shared.Rent(parts.Length);
            try
            {
                var literalLength = 0;
                for (var i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (part.IsLiteral)
                    {
                        literalLength += part.Literal.Length;
                        dynamicStrings[i] = null;
                        continue;
                    }

                    EmitExpression(part.Expression);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToString);
                    var local = DeclareLocal(typeof(string));
                    _il.Emit(OpCodes.Stloc, local);
                    dynamicStrings[i] = local;
                }

                _il.Emit(OpCodes.Ldc_I4, literalLength);
                for (var i = 0; i < parts.Length; i++)
                {
                    var local = dynamicStrings[i];
                    if (local == null)
                    {
                        continue;
                    }

                    _il.Emit(OpCodes.Ldloc, local);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetStringLength);
                    _il.Emit(OpCodes.Add);
                }

                _il.Emit(OpCodes.Newobj, RuntimeMetadata.StringBuilder_CtorCapacity);
                for (var i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (part.IsLiteral)
                    {
                        if (part.Literal.Length == 0)
                        {
                            continue;
                        }

                        _session.Builder.LoadStringConstant(_il, part.Literal);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Ldloc, dynamicStrings[i]);
                    }

                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.StringBuilder_AppendString);
                }

                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.StringBuilder_ToString);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromString);
            }
            finally
            {
                ArrayPool<LocalBuilder>.Shared.Return(dynamicStrings, clearArray: true);
            }
        }

        private static bool TryGetStringLiteral(LoweredExpression expression, out string value)
        {
            if (expression is LoweredLiteralExpression { Token: StringToken token })
            {
                value = token.Value;
                return true;
            }

            value = null;
            return false;
        }

        private UnsupportedEmissionException CreateUnsupported(AstNode source, bool isExpression)
        {
            return new UnsupportedEmissionException(_function, new LoweredUnsupportedNode(
                source?.GetType().Name ?? "<null>",
                source?.Range ?? SourceSpan.None,
                isExpression));
        }

        private void EmitLogical(LoweredBinaryExpression expression, bool branchWhenTrue)
        {
            var endLabel = _il.DefineLabel();
            EmitExpression(expression.Left);
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ToBoolean);
            _il.Emit(branchWhenTrue ? OpCodes.Brtrue : OpCodes.Brfalse, endLabel);
            _il.Emit(OpCodes.Pop);
            EmitExpression(expression.Right);
            _il.MarkLabel(endLabel);
        }

        private void EmitAssignment(LoweredAssignmentExpression expression)
        {
            var name = (LoweredNameExpression)expression.Left;
            EmitExpression(expression.Right);
            _il.Emit(OpCodes.Dup);
            EmitStoreNameFromStack(name);
        }

        private void EmitAssignmentDiscarded(LoweredAssignmentExpression expression)
        {
            var name = (LoweredNameExpression)expression.Left;
            EmitExpression(expression.Right);
            EmitStoreNameFromStack(name);
        }

        private void EmitCompound(LoweredCompoundExpression expression)
        {
            if (expression.Left is LoweredGetElementExpression element && expression.Operator.SimplerOperator == Operator.Add)
            {
                EmitExpression(element.Instance);
                EmitExpression(element.Index);
                EmitExpression(expression.Right);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_CompoundAddElementDatum);
                return;
            }

            var name = (LoweredNameExpression)expression.Left;
            EmitName(name);
            EmitExpression(expression.Right);
            _il.Emit(OpCodes.Call, GetBinaryMethod(expression.Operator.SimplerOperator));
            _il.Emit(OpCodes.Dup);
            EmitStoreNameFromStack(name);
        }

        private void EmitCompoundDiscarded(LoweredCompoundExpression expression)
        {
            if (expression.Left is LoweredGetElementExpression element && expression.Operator.SimplerOperator == Operator.Add)
            {
                EmitExpression(element.Instance);
                EmitExpression(element.Index);
                EmitExpression(expression.Right);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_CompoundAddElementDatum);
                _il.Emit(OpCodes.Pop);
                return;
            }

            var name = (LoweredNameExpression)expression.Left;
            EmitName(name);
            EmitExpression(expression.Right);
            _il.Emit(OpCodes.Call, GetBinaryMethod(expression.Operator.SimplerOperator));
            EmitStoreNameFromStack(name);
        }

        private void EmitUnary(LoweredUnaryExpression expression)
        {
            var incrementMethod = GetIncrementMethod(expression.Operator);
            if (incrementMethod != null)
            {
                EmitMutationUnary(expression);
                return;
            }

            EmitExpression(expression.Expression);
            _il.Emit(OpCodes.Call, GetUnaryMethod(expression.Operator));
        }

        private void EmitMutationUnary(LoweredUnaryExpression expression)
        {
            if (expression.Expression is LoweredNameExpression name)
            {
                if ((name.LocalSlot.IsValid || name.UpvalueSlot.IsValid) && !name.ModuleSymbol.IsValid)
                {
                    EmitLoadNameAddress(name);
                    _il.Emit(OpCodes.Call, GetIncrementMethod(expression.Operator));
                    return;
                }

                EmitStoreTargetObject(name);
                _session.Builder.LoadStringConstant(_il, name.Name);
                _il.Emit(OpCodes.Call, GetPropertyMutationMethod(expression.Operator));
                return;
            }

            if (expression.Expression is LoweredGetElementExpression element)
            {
                EmitExpression(element.Instance);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
                EmitExpression(element.Index);
                _il.Emit(OpCodes.Call, GetElementMutationMethod(expression.Operator));
                return;
            }

            var property = (LoweredGetPropertyExpression)expression.Expression;
            if (!TryGetStaticPropertyName(property, out var propertyName))
            {
                throw new NotSupportedException("Dynamic property mutation");
            }
            EmitExpression(property.Instance);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _session.Builder.LoadStringConstant(_il, propertyName);
            _il.Emit(OpCodes.Call, GetPropertyMutationMethod(expression.Operator));
        }

        private void EmitMutationUnaryDiscarded(LoweredUnaryExpression expression)
        {
            if (expression.Expression is LoweredNameExpression name)
            {
                if ((name.LocalSlot.IsValid || name.UpvalueSlot.IsValid) && !name.ModuleSymbol.IsValid)
                {
                    EmitLoadNameAddress(name);
                    _il.Emit(OpCodes.Call, GetMutationVoidMethod(expression.Operator));
                    return;
                }

                EmitStoreTargetObject(name);
                _session.Builder.LoadStringConstant(_il, name.Name);
                _il.Emit(OpCodes.Call, GetPropertyMutationVoidMethod(expression.Operator));
                return;
            }

            if (expression.Expression is LoweredGetElementExpression element)
            {
                EmitExpression(element.Instance);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
                EmitExpression(element.Index);
                _il.Emit(OpCodes.Call, GetElementMutationVoidMethod(expression.Operator));
                return;
            }

            var property = (LoweredGetPropertyExpression)expression.Expression;
            if (!TryGetStaticPropertyName(property, out var propertyName))
            {
                throw new NotSupportedException("Dynamic property mutation");
            }
            EmitExpression(property.Instance);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _session.Builder.LoadStringConstant(_il, propertyName);
            _il.Emit(OpCodes.Call, GetPropertyMutationVoidMethod(expression.Operator));
        }

        private void EmitIn(LoweredInExpression expression)
        {
            EmitExpression(expression.Right);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            EmitExpression(expression.Left);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_Included);
        }

        private void EmitGetProperty(LoweredGetPropertyExpression expression)
        {
            if (!TryGetStaticPropertyName(expression, out var name))
            {
                throw new NotSupportedException("Dynamic property name");
            }

            EmitExpression(expression.Instance);
            if (StringComparer.Ordinal.Equals(name, "length"))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetLengthDatum);
                return;
            }

            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetProperty);
        }

        private void EmitSetProperty(LoweredSetPropertyExpression expression)
        {
            if (!TryGetStaticPropertyName(expression, out var name))
            {
                throw new NotSupportedException("Dynamic property name");
            }

            EmitExpression(expression.Instance);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            EmitExpression(expression.Value);
            var valueLocal = DeclareTemp();
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, valueLocal);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_SetPropertyDatum);
            _il.Emit(OpCodes.Ldloc, valueLocal);
        }

        private void EmitSetPropertyDiscarded(LoweredSetPropertyExpression expression)
        {
            if (!TryGetStaticPropertyName(expression, out var name))
            {
                throw new NotSupportedException("Dynamic property name");
            }

            EmitExpression(expression.Instance);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            EmitExpression(expression.Value);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_SetPropertyDatum);
        }

        private void EmitGetElement(LoweredGetElementExpression expression)
        {
            if (TryEmitGetElementFastPath(expression))
            {
                return;
            }

            EmitExpression(expression.Instance);
            EmitExpression(expression.Index);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetElementDatum);
        }

        private bool TryEmitGetElementFastPath(LoweredGetElementExpression expression)
        {
            if (TryGetNumberLiteral(expression.Index, out var index))
            {
                EmitExpression(expression.Instance);
                EmitRawNumber(index);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetElementNumber);
                return true;
            }

            if (expression.Index is LoweredBinaryExpression binary &&
                binary.Operator == Operator.Add)
            {
                if (TryGetNumberLiteral(binary.Right, out var right))
                {
                    EmitExpression(expression.Instance);
                    EmitExpression(binary.Left);
                    EmitRawNumber(right);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetElementAddNumberRight);
                    return true;
                }

                if (TryGetNumberLiteral(binary.Left, out var left))
                {
                    EmitExpression(expression.Instance);
                    EmitRawNumber(left);
                    EmitExpression(binary.Right);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetElementAddNumberLeft);
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetNumberLiteral(LoweredExpression expression, out double value)
        {
            if (expression is LoweredLiteralExpression { Token: NumberToken token })
            {
                value = token.NumberValue;
                return true;
            }

            value = default;
            return false;
        }

        private void EmitRawNumber(double value)
        {
            _il.Emit(OpCodes.Ldc_R8, value);
        }

        private void EmitSetElement(LoweredSetElementExpression expression)
        {
            EmitExpression(expression.Instance);
            EmitExpression(expression.Index);
            EmitExpression(expression.Value);
            var valueLocal = DeclareTemp();
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, valueLocal);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_SetElementDatum);
            _il.Emit(OpCodes.Ldloc, valueLocal);
        }

        private void EmitSetElementDiscarded(LoweredSetElementExpression expression)
        {
            EmitExpression(expression.Instance);
            EmitExpression(expression.Index);
            EmitExpression(expression.Value);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_SetElementDatum);
        }

        private void EmitArrayLiteral(LoweredArrayLiteralExpression expression)
        {
            if (HasSpread(expression.Elements))
            {
                EmitSpreadArrayLiteral(expression);
                return;
            }

            _il.Emit(OpCodes.Ldc_I4, expression.Elements.Length);
            _il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptArray_CtorCapacity);
            for (var i = 0; i < expression.Elements.Length; i++)
            {
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Ldc_I4, i);
                EmitExpressionOrNull(expression.Elements[i]);
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_SetElementValue);
            }
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
        }

        private void EmitSpreadArrayLiteral(LoweredArrayLiteralExpression expression)
        {
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptArray_CtorCapacity);
            for (var i = 0; i < expression.Elements.Length; i++)
            {
                _il.Emit(OpCodes.Dup);
                if (expression.Elements[i] is LoweredSpreadExpression spread)
                {
                    EmitExpression(spread.Expression);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_SpreadInto);
                }
                else
                {
                    EmitExpressionOrNull(expression.Elements[i]);
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_Push);
                }
            }
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
        }

        private void EmitMap(LoweredMapExpression expression)
        {
            if (TryGetFastObject3(expression, out var first, out var second, out var third))
            {
                EmitMapEntryForCreateObject(first);
                EmitMapEntryForCreateObject(second);
                EmitMapEntryForCreateObject(third);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_CreateObject3);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
                return;
            }

            _il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptObject_Ctor);
            for (var i = 0; i < expression.Entries.Length; i++)
            {
                EmitMapEntry(expression.Entries[i]);
            }
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
        }

        private void EmitMapEntry(LoweredMapEntry entry)
        {
            _il.Emit(OpCodes.Dup);
            if (entry.Value is LoweredSpreadExpression spread)
            {
                EmitExpression(spread.Expression);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_CopyPropertysFrom);
                return;
            }

            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, GetMapEntryKey(entry));
            EmitExpression(entry.Value);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_SetPropertyDatum);
        }

        private void EmitLambda(LoweredLambdaExpression expression)
        {
            var function = GetFunction(expression.Function);
            ClosureMaterializer.EmitClosure(_session, _il, function, EmitClosureUpvalue);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
        }

        private void EmitNew(LoweredNewExpression expression)
        {
            var call = expression.Expression;
            if (HasSpread(call.Arguments) || call.Arguments.Length > 2)
            {
                EmitNewMany(call);
                return;
            }

            EmitLocation(expression);
            EmitExpression(call.Target);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Ldarg_0);
            for (var i = 0; i < call.Arguments.Length; i++)
            {
                EmitExpression(call.Arguments[i]);
            }
            _il.Emit(OpCodes.Call, GetNewMethod(call.Arguments.Length));
        }

        private void EmitNewMany(LoweredCallExpression call)
        {
            var typeLocal = DeclareLocal(typeof(ScriptObject));
            var argsLocal = DeclareLocal(typeof(ScriptDatum[]));

            EmitLocation(call);
            EmitExpression(call.Target);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Stloc, typeLocal);

            var countLocal = EmitArgumentsToBuffer(call.Arguments, argsLocal);

            _il.Emit(OpCodes.Ldloc, typeLocal);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldloc, argsLocal);
            _il.Emit(OpCodes.Ldloc, countLocal);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_NewMany);
        }

        private void EmitMapEntryForCreateObject(LoweredMapEntry entry)
        {
            _session.Builder.LoadStringConstant(_il, entry.Key.Value);
            EmitExpression(entry.Value);
        }

        private void EmitDirectCall(LoweredCallExpression call, FunctionPlan target)
        {
            var arity = GetFastArity(target.CallConvention);
            var argumentLocals = EmitDirectCallArguments(call.Arguments, arity, out var deferredArguments);
            var directContext = DeclareLocal(typeof(ScriptContext));
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, target.Name);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_EnterDirect);
            _il.Emit(OpCodes.Stloc, directContext);

            EmitLocation(call);
            _il.Emit(OpCodes.Ldloc, directContext);
            _il.Emit(OpCodes.Ldloc, directContext);
            for (var i = 0; i < arity; i++)
            {
                if (i >= argumentLocals.Length)
                {
                    LoadNull();
                }
                else if (deferredArguments[i])
                {
                    EmitExpression(call.Arguments[i]);
                }
                else
                {
                    _il.Emit(OpCodes.Ldloc, argumentLocals[i]);
                }
            }

            _il.Emit(OpCodes.Call, target.Method);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_LeaveDirect);
        }

        private LocalBuilder[] EmitDirectCallArguments(LoweredExpression[] arguments, int arity, out bool[] deferredArguments)
        {
            if (arguments.Length == 0 || arity == 0)
            {
                for (var i = 0; i < arguments.Length; i++)
                {
                    EmitExpressionDiscarded(arguments[i]);
                }

                deferredArguments = Array.Empty<bool>();
                return Array.Empty<LocalBuilder>();
            }

            var count = Math.Min(arguments.Length, arity);
            var locals = new LocalBuilder[count];
            deferredArguments = new bool[count];
            var lastPreEvaluated = GetLastDirectCallPreEvaluationIndex(arguments, arity);
            for (var i = 0; i < arguments.Length; i++)
            {
                if (i < count)
                {
                    if (i > lastPreEvaluated && CanDeferDirectCallArgument(arguments[i]))
                    {
                        deferredArguments[i] = true;
                        continue;
                    }

                    EmitExpression(arguments[i]);
                    var local = DeclareTemp();
                    _il.Emit(OpCodes.Stloc, local);
                    locals[i] = local;
                }
                else
                {
                    EmitExpressionDiscarded(arguments[i]);
                }
            }

            return locals;
        }

        private static int GetLastDirectCallPreEvaluationIndex(LoweredExpression[] arguments, int arity)
        {
            var last = -1;
            for (var i = 0; i < arguments.Length; i++)
            {
                if (i >= arity || !CanDeferDirectCallArgument(arguments[i]))
                {
                    last = i;
                }
            }

            return last;
        }

        private static bool CanDeferDirectCallArgument(LoweredExpression expression)
        {
            return expression switch
            {
                LoweredLiteralExpression literal => literal.Token is NumberToken or BooleanToken or NullToken,
                LoweredNameExpression name => IsLocalName(name) || (name.UpvalueSlot.IsValid && !name.ModuleSymbol.IsValid),
                _ => false
            };
        }

        private void EmitRegularCall(LoweredCallExpression call)
        {
            if (HasSpread(call.Arguments) || call.Arguments.Length > 7)
            {
                EmitRegularCallMany(call);
                return;
            }

            EmitLocation(call);
            EmitExpression(call.Target);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Ldarg_0);
            for (var i = 0; i < call.Arguments.Length; i++)
            {
                EmitExpression(call.Arguments[i]);
            }
            _il.Emit(OpCodes.Call, GetInvokeMethod(call.Arguments.Length));
        }

        private void EmitPropertyCall(LoweredCallExpression call)
        {
            var property = (LoweredGetPropertyExpression)call.Target;
            if (!TryGetStaticPropertyName(property, out var name))
            {
                throw new NotSupportedException("Dynamic property call");
            }

            if (HasSpread(call.Arguments) || call.Arguments.Length > 7)
            {
                EmitPropertyCallMany(call, property, name);
                return;
            }

            EmitLocation(call);
            EmitExpression(property.Instance);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            for (var i = 0; i < call.Arguments.Length; i++)
            {
                EmitExpression(call.Arguments[i]);
            }
            _il.Emit(OpCodes.Call, GetInvokePropertyMethod(call.Arguments.Length));
        }

        private void EmitPropertyCallMany(LoweredCallExpression call, LoweredGetPropertyExpression property, string name)
        {
            var receiverLocal = DeclareLocal(typeof(ScriptObject));
            var argsLocal = DeclareLocal(typeof(ScriptDatum[]));

            EmitLocation(call);
            EmitExpression(property.Instance);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Stloc, receiverLocal);
            var countLocal = EmitArgumentsToBuffer(call.Arguments, argsLocal);

            _il.Emit(OpCodes.Ldloc, receiverLocal);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Ldloc, argsLocal);
            _il.Emit(OpCodes.Ldloc, countLocal);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_InvokePropertyMany);
        }

        private void EmitRegularCallMany(LoweredCallExpression call)
        {
            var functionLocal = DeclareLocal(typeof(ScriptObject));
            var argsLocal = DeclareLocal(typeof(ScriptDatum[]));

            EmitLocation(call);
            EmitExpression(call.Target);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Stloc, functionLocal);
            var countLocal = EmitArgumentsToBuffer(call.Arguments, argsLocal);

            _il.Emit(OpCodes.Ldloc, functionLocal);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldloc, argsLocal);
            _il.Emit(OpCodes.Ldloc, countLocal);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_InvokeMany);
        }

        private LocalBuilder EmitArgumentsToBuffer(LoweredExpression[] arguments, LocalBuilder argsLocal)
        {
            var countLocal = DeclareLocal(typeof(int));
            _il.Emit(OpCodes.Ldc_I4, Math.Max(arguments.Length, 1));
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_RentArguments);
            _il.Emit(OpCodes.Stloc, argsLocal);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Stloc, countLocal);

            for (var i = 0; i < arguments.Length; i++)
            {
                _il.Emit(OpCodes.Ldloc, argsLocal);
                _il.Emit(OpCodes.Ldloca, countLocal);
                if (arguments[i] is LoweredSpreadExpression spread)
                {
                    EmitExpression(spread.Expression);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_SpreadIntoArguments);
                }
                else
                {
                    EmitExpression(arguments[i]);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_AddArgument);
                }
                _il.Emit(OpCodes.Stloc, argsLocal);
            }

            return countLocal;
        }

        private LocalBuilder[] EmitArgumentsToLocals(LoweredExpression[] arguments)
        {
            var locals = new LocalBuilder[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
            {
                var local = DeclareTemp();
                EmitExpression(arguments[i]);
                _il.Emit(OpCodes.Stloc, local);
                locals[i] = local;
            }

            return locals;
        }

        private void EmitName(LoweredNameExpression name)
        {
            if (StringComparer.Ordinal.Equals(name.Name, "$args"))
            {
                _il.Emit(OpCodes.Ldarg_1);
                _il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptArray_SpanCtor);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
                return;
            }
            if (StringComparer.Ordinal.Equals(name.Name, "$state"))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_UserState);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
                return;
            }
            if (StringComparer.Ordinal.Equals(name.Name, "global"))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
                return;
            }

            if (IsLocalName(name))
            {
                EmitLoadLocal(name.LocalSlot);
                return;
            }

            if (name.UpvalueSlot.IsValid && !name.ModuleSymbol.IsValid)
            {
                EmitLoadContextUpvalue(name.UpvalueSlot.Value);
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.Upvalue_Value);
                return;
            }

            if (TryResolveMaterializedFunction(name.ModuleSymbol, out var function))
            {
                EmitModuleFunctionLoad(function);
                return;
            }

            if (name.ModuleSymbol.IsValid)
            {
                if (IsDeclaredOnlyModuleSymbol(name.ModuleSymbol))
                {
                    EmitGlobalPropertyLoad(name.Name);
                }
                else
                {
                    EmitModulePropertyLoad(name.Name);
                }
                return;
            }

            EmitGlobalPropertyLoad(name.Name);
        }

        private void EmitModuleFunctionLoad(FunctionPlan function)
        {
            EmitModulePropertyLoad(function.Name);
        }

        private void EmitModulePropertyLoad(string name)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_GetPropertyDatum);
        }

        private void EmitStoreNameFromStack(LoweredNameExpression name)
        {
            if (IsLocalName(name))
            {
                EmitStoreLocalFromStack(name.LocalSlot);
                return;
            }

            if (name.UpvalueSlot.IsValid && !name.ModuleSymbol.IsValid)
            {
                var value = DeclareTemp();
                _il.Emit(OpCodes.Stloc, value);
                EmitLoadContextUpvalue(name.UpvalueSlot.Value);
                _il.Emit(OpCodes.Ldloc, value);
                _il.Emit(OpCodes.Stfld, RuntimeMetadata.Upvalue_Value);
                return;
            }

            var valueLocal = DeclareTemp();
            _il.Emit(OpCodes.Stloc, valueLocal);
            EmitStoreTargetObject(name);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name.Name);
            _il.Emit(OpCodes.Ldloc, valueLocal);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_SetPropertyDatum);
        }

        private void EmitStoreTargetObject(LoweredNameExpression name)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, name.ModuleSymbol.IsValid && !IsDeclaredOnlyModuleSymbol(name.ModuleSymbol)
                ? RuntimeMetadata.CILContext_Module
                : RuntimeMetadata.CILContext_Global);
        }

        private bool IsDeclaredOnlyModuleSymbol(SymbolId symbol)
        {
            return symbol.IsValid &&
                _session.CompileSession.Symbols[symbol].HasFlag(BackendSymbolFlags.DeclaredOnly);
        }

        private void EmitGlobalPropertyLoad(string name)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_GetPropertyDatum);
        }

        private LocalBuilder DeclareTemp()
        {
            return DeclareLocal(typeof(ScriptDatum));
        }

        private LocalBuilder DeclareLocal(Type type)
        {
            _cilLocalCount++;
            return _il.DeclareLocal(type);
        }

        private void EmitLiteral(LoweredLiteralExpression expression)
        {
            switch (expression.Token)
            {
                case NumberToken number:
                    if (_session.Builder.LoadNumber(_il, number.NumberValue) == LoadState.Constant)
                    {
                        _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromNumber);
                    }
                    return;
                case StringToken stringToken:
                    if (_session.Builder.LoadString(_il, stringToken.Value) == LoadState.Constant)
                    {
                        _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromString);
                    }
                    return;
                case RegexToken regex:
                    _session.Builder.LoadStringConstant(_il, regex.Pattern);
                    _session.Builder.LoadStringConstant(_il, regex.Flags);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.RegexManager_LoadRegex);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
                    return;
                case BooleanToken boolean:
                    if (_session.Builder.LoadBoolean(_il, boolean.BoolValue) == LoadState.Constant)
                    {
                        _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromBoolean);
                    }
                    return;
                case NullToken:
                    LoadNull();
                    return;
                default:
                    throw new NotSupportedException(expression.Token?.GetType().Name ?? "<null>");
            }
        }

        private void LoadNull()
        {
            _session.Builder.LoadNull(_il);
        }

        private void EmitLocation(LoweredNode node)
        {
            if (!ShouldEmitLocation() || node == null || node.Range.StartLine <= 0)
            {
                return;
            }

            var location = ((long)_module.PathHash & 0xffffffffL) |
                ((long)node.Range.StartLine << 32);
            if (_lastLocation == location && _lastLocationEndOffset == _il.ILOffset)
            {
                return;
            }

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldc_I8, location);
            _il.Emit(OpCodes.Stfld, RuntimeMetadata.CILContext_Location);
            _lastLocation = location;
            _lastLocationEndOffset = _il.ILOffset;
        }

        private bool ShouldEmitLocation()
        {
            return _session.Options.Optimization.StackTrace ||
                _session.Options.Optimization.Level == OptimizeOptions.Debug;
        }

        private bool CanEmitStatement(LoweredStatement statement, IReadOnlySet<FunctionId> executableFunctions)
        {
            return statement switch
            {
                null => true,
                LoweredNoOpStatement => true,
                LoweredBlockStatement block => CanEmitBlock(block, executableFunctions),
                LoweredReturnStatement returnStatement => CanEmitExpression(returnStatement.Expression, executableFunctions),
                LoweredVariableDeclarationStatement variable => variable.Slot.IsValid && CanEmitExpression(variable.Initializer, executableFunctions),
                LoweredObjectDestructuringDeclarationStatement objectDestructuring => CanEmitObjectDestructuringDeclaration(objectDestructuring, executableFunctions),
                LoweredArrayDestructuringDeclarationStatement arrayDestructuring => CanEmitArrayDestructuringDeclaration(arrayDestructuring, executableFunctions),
                LoweredFunctionDeclarationStatement function => CanEmitFunctionDeclaration(function, executableFunctions),
                LoweredExpressionStatement expressionStatement => CanEmitExpression(expressionStatement.Expression, executableFunctions),
                LoweredIfStatement ifStatement => CanEmitExpression(ifStatement.Condition, executableFunctions) &&
                    CanEmitStatement(ifStatement.Body, executableFunctions) &&
                    CanEmitStatement(ifStatement.Else, executableFunctions),
                LoweredWhileStatement whileStatement => CanEmitExpression(whileStatement.Condition, executableFunctions) &&
                    CanEmitStatement(whileStatement.Body, executableFunctions),
                LoweredForStatement forStatement => CanEmitStatement(forStatement.Initializer, executableFunctions) &&
                    CanEmitExpression(forStatement.Condition, executableFunctions) &&
                    CanEmitExpression(forStatement.Incrementor, executableFunctions) &&
                    CanEmitStatement(forStatement.Body, executableFunctions),
                LoweredForInStatement forInStatement => CanEmitForIn(forInStatement, executableFunctions),
                LoweredTryStatement tryStatement => CanEmitTry(tryStatement, executableFunctions),
                LoweredThrowStatement throwStatement => CanEmitExpression(throwStatement.Expression, executableFunctions),
                LoweredDeleteStatement deleteStatement => CanEmitDelete(deleteStatement, executableFunctions),
                LoweredDebuggerStatement or LoweredBreakStatement or LoweredContinueStatement => true,
                _ => false
            };
        }

        private bool CanEmitForIn(LoweredForInStatement statement, IReadOnlySet<FunctionId> executableFunctions)
        {
            return statement.Iterator?.Left is LoweredNameExpression name &&
                IsLocalName(name) &&
                CanEmitStatement(statement.Initializer, executableFunctions) &&
                CanEmitExpression(statement.Iterator.Right, executableFunctions) &&
                CanEmitStatement(statement.Body, executableFunctions);
        }

        private bool CanEmitFunctionDeclaration(LoweredFunctionDeclarationStatement statement, IReadOnlySet<FunctionId> executableFunctions)
        {
            return statement.LocalSlot.IsValid &&
                statement.Function.IsValid &&
                executableFunctions.Contains(statement.Function) &&
                TryGetFunction(statement.Function, out var function) &&
                ClosureMaterializer.CanPlanMaterialize(function, requireName: true);
        }

        private bool CanEmitTry(LoweredTryStatement statement, IReadOnlySet<FunctionId> executableFunctions)
        {
            if (!HasExceptionHandler(statement))
            {
                return CanEmitStatement(statement.Body, executableFunctions);
            }

            return CanEmitProtectedStatement(statement.Body, executableFunctions) &&
                (!HasCatch(statement) || CanEmitProtectedStatement(statement.CatchBody, executableFunctions)) &&
                CanEmitProtectedStatement(statement.FinallyBody, executableFunctions);
        }

        private bool CanEmitProtectedStatement(LoweredStatement statement, IReadOnlySet<FunctionId> executableFunctions)
        {
            return statement switch
            {
                null => true,
                LoweredNoOpStatement => true,
                LoweredBlockStatement block => CanEmitProtectedBlock(block, executableFunctions),
                LoweredVariableDeclarationStatement variable => variable.Slot.IsValid && CanEmitExpression(variable.Initializer, executableFunctions),
                LoweredObjectDestructuringDeclarationStatement objectDestructuring => CanEmitObjectDestructuringDeclaration(objectDestructuring, executableFunctions),
                LoweredArrayDestructuringDeclarationStatement arrayDestructuring => CanEmitArrayDestructuringDeclaration(arrayDestructuring, executableFunctions),
                LoweredExpressionStatement expressionStatement => CanEmitExpression(expressionStatement.Expression, executableFunctions),
                LoweredIfStatement ifStatement => CanEmitExpression(ifStatement.Condition, executableFunctions) &&
                    CanEmitProtectedStatement(ifStatement.Body, executableFunctions) &&
                    CanEmitProtectedStatement(ifStatement.Else, executableFunctions),
                LoweredThrowStatement throwStatement => CanEmitExpression(throwStatement.Expression, executableFunctions),
                LoweredDeleteStatement deleteStatement => CanEmitDelete(deleteStatement, executableFunctions),
                LoweredDebuggerStatement => true,
                _ => false
            };
        }

        private bool CanEmitProtectedBlock(LoweredBlockStatement block, IReadOnlySet<FunctionId> executableFunctions)
        {
            for (var i = 0; i < block.Statements.Length; i++)
            {
                if (!CanEmitProtectedStatement(block.Statements[i], executableFunctions))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasExceptionHandler(LoweredTryStatement statement)
        {
            return HasCatch(statement) || statement.FinallyBody != null;
        }

        private static bool HasCatch(LoweredTryStatement statement)
        {
            return statement.CatchBody != null || !string.IsNullOrEmpty(statement.CatchVariable);
        }

        private bool CanEmitParameterDefaults(FunctionPlan function, IReadOnlySet<FunctionId> executableFunctions)
        {
            if (function.ParameterDefaults == null)
            {
                return true;
            }

            for (var i = 0; i < function.ParameterDefaults.Length; i++)
            {
                if (!CanEmitExpression(function.ParameterDefaults[i], executableFunctions))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanEmitBlock(LoweredBlockStatement block, IReadOnlySet<FunctionId> executableFunctions)
        {
            for (var i = 0; i < block.Statements.Length; i++)
            {
                if (!CanEmitStatement(block.Statements[i], executableFunctions))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanEmitObjectDestructuringDeclaration(
            LoweredObjectDestructuringDeclarationStatement statement,
            IReadOnlySet<FunctionId> executableFunctions)
        {
            if (!CanEmitExpression(statement.Initializer, executableFunctions))
            {
                return false;
            }

            for (var i = 0; i < statement.Bindings.Length; i++)
            {
                var binding = statement.Bindings[i];
                if (!binding.Slot.IsValid || binding.Property == null || string.IsNullOrEmpty(binding.Property.Value))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanEmitArrayDestructuringDeclaration(
            LoweredArrayDestructuringDeclarationStatement statement,
            IReadOnlySet<FunctionId> executableFunctions)
        {
            if (!CanEmitExpression(statement.Initializer, executableFunctions))
            {
                return false;
            }

            for (var i = 0; i < statement.Bindings.Length; i++)
            {
                if (!statement.Bindings[i].Slot.IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanEmitExpression(LoweredExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            return expression switch
            {
                null => true,
                LoweredLiteralExpression literal => literal.Token is NumberToken or StringToken or RegexToken or BooleanToken or NullToken,
                LoweredNameExpression name => CanEmitName(name, executableFunctions),
                LoweredBinaryExpression binary => CanEmitBinary(binary, executableFunctions),
                LoweredTemplateStringExpression template => CanEmitTemplateString(template, executableFunctions),
                LoweredAssignmentExpression assignment => assignment.Left is LoweredNameExpression name &&
                    CanEmitNameAssignmentTarget(name) &&
                    CanEmitExpression(assignment.Right, executableFunctions),
                LoweredCompoundExpression compound => CanEmitCompound(compound, executableFunctions),
                LoweredUnaryExpression unary => CanEmitUnary(unary, executableFunctions),
                LoweredInExpression inExpression => CanEmitIn(inExpression, executableFunctions),
                LoweredGetPropertyExpression property => CanEmitGetProperty(property, executableFunctions),
                LoweredSetPropertyExpression property => CanEmitSetProperty(property, executableFunctions),
                LoweredGetElementExpression element => CanEmitGetElement(element, executableFunctions),
                LoweredSetElementExpression element => CanEmitSetElement(element, executableFunctions),
                LoweredArrayLiteralExpression array => CanEmitArrayLiteral(array, executableFunctions),
                LoweredMapExpression map => CanEmitMap(map, executableFunctions),
                LoweredLambdaExpression lambda => CanEmitLambda(lambda, executableFunctions),
                LoweredNewExpression @new => CanEmitNew(@new, executableFunctions),
                LoweredCallExpression call => CanEmitDirectCall(call, executableFunctions) ||
                    CanEmitPropertyCall(call, executableFunctions) ||
                    CanEmitRegularCall(call, executableFunctions),
                _ => false
            };
        }

        private bool CanEmitBinary(LoweredBinaryExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            return (GetBinaryMethod(expression.Operator) != null ||
                    expression.Operator == Operator.LogicalAnd ||
                    expression.Operator == Operator.LogicalOr) &&
                CanEmitExpression(expression.Left, executableFunctions) &&
                CanEmitExpression(expression.Right, executableFunctions);
        }

        private bool CanEmitTemplateString(LoweredTemplateStringExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            for (var i = 0; i < expression.Parts.Length; i++)
            {
                var part = expression.Parts[i];
                if (!part.IsLiteral && !CanEmitExpression(part.Expression, executableFunctions))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanEmitUnary(LoweredUnaryExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            if (GetIncrementMethod(expression.Operator) != null)
            {
                if (expression.Expression is LoweredNameExpression name)
                {
                    return CanEmitNameAssignmentTarget(name);
                }

                if (expression.Expression is LoweredGetElementExpression element)
                {
                    return CanEmitGetElement(element, executableFunctions);
                }

                return expression.Expression is LoweredGetPropertyExpression property &&
                    TryGetStaticPropertyName(property, out _) &&
                    CanEmitExpression(property.Instance, executableFunctions);
            }

            return GetUnaryMethod(expression.Operator) != null &&
                CanEmitExpression(expression.Expression, executableFunctions);
        }

        private bool CanEmitCompound(LoweredCompoundExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            if (expression.Left is LoweredNameExpression name)
            {
                return CanEmitNameAssignmentTarget(name) &&
                    GetBinaryMethod(expression.Operator.SimplerOperator) != null &&
                    CanEmitExpression(expression.Right, executableFunctions);
            }

            if (expression.Left is LoweredGetElementExpression element)
            {
                return expression.Operator.SimplerOperator == Operator.Add &&
                    CanEmitGetElement(element, executableFunctions) &&
                    CanEmitExpression(expression.Right, executableFunctions);
            }

            return false;
        }

        private bool CanEmitIn(LoweredInExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            return CanEmitExpression(expression.Left, executableFunctions) &&
                CanEmitExpression(expression.Right, executableFunctions);
        }

        private bool CanEmitDelete(LoweredDeleteStatement statement, IReadOnlySet<FunctionId> executableFunctions)
        {
            return statement.Expression switch
            {
                LoweredGetPropertyExpression property => CanEmitGetPropertyReceiver(property, executableFunctions),
                LoweredGetElementExpression element => CanEmitGetElement(element, executableFunctions),
                _ => false
            };
        }

        private bool CanEmitDirectCall(LoweredCallExpression call, IReadOnlySet<FunctionId> executableFunctions)
        {
            if (HasSpread(call.Arguments))
            {
                return false;
            }

            if (!TryResolveDirectCallTarget(call, executableFunctions, out _))
            {
                return false;
            }

            for (var i = 0; i < call.Arguments.Length; i++)
            {
                if (!CanEmitArgument(call.Arguments[i], executableFunctions))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanEmitNew(LoweredNewExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            var call = expression.Expression;
            if (call == null || !CanEmitExpression(call.Target, executableFunctions))
            {
                return false;
            }

            for (var i = 0; i < call.Arguments.Length; i++)
            {
                if (!CanEmitArgument(call.Arguments[i], executableFunctions))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanEmitPropertyCall(LoweredCallExpression call, IReadOnlySet<FunctionId> executableFunctions)
        {
            if (call.Target is not LoweredGetPropertyExpression property ||
                !CanEmitGetPropertyReceiver(property, executableFunctions))
            {
                return false;
            }

            for (var i = 0; i < call.Arguments.Length; i++)
            {
                if (!CanEmitArgument(call.Arguments[i], executableFunctions))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanEmitName(LoweredNameExpression name, IReadOnlySet<FunctionId> executableFunctions)
        {
            return StringComparer.Ordinal.Equals(name.Name, "$args") ||
                StringComparer.Ordinal.Equals(name.Name, "$state") ||
                StringComparer.Ordinal.Equals(name.Name, "global") ||
                IsLocalName(name) ||
                (name.UpvalueSlot.IsValid && !name.ModuleSymbol.IsValid) ||
                name.ModuleSymbol.IsValid ||
                !name.UpvalueSlot.IsValid;
        }

        private static bool CanEmitNameAssignmentTarget(LoweredNameExpression name)
        {
            return name.LocalSlot.IsValid ||
                (name.UpvalueSlot.IsValid && !name.ModuleSymbol.IsValid) ||
                name.ModuleSymbol.IsValid ||
                !name.UpvalueSlot.IsValid;
        }

        private bool CanEmitGetProperty(LoweredGetPropertyExpression property, IReadOnlySet<FunctionId> executableFunctions)
        {
            return CanEmitGetPropertyReceiver(property, executableFunctions);
        }

        private bool CanEmitGetPropertyReceiver(LoweredGetPropertyExpression property, IReadOnlySet<FunctionId> executableFunctions)
        {
            return TryGetStaticPropertyName(property, out _) &&
                CanEmitExpression(property.Instance, executableFunctions);
        }

        private bool CanEmitSetProperty(LoweredSetPropertyExpression property, IReadOnlySet<FunctionId> executableFunctions)
        {
            return TryGetStaticPropertyName(property, out _) &&
                CanEmitExpression(property.Instance, executableFunctions) &&
                CanEmitExpression(property.Value, executableFunctions);
        }

        private bool CanEmitGetElement(LoweredGetElementExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            return CanEmitExpression(expression.Instance, executableFunctions) &&
                CanEmitExpression(expression.Index, executableFunctions);
        }

        private bool CanEmitSetElement(LoweredSetElementExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            return CanEmitExpression(expression.Instance, executableFunctions) &&
                CanEmitExpression(expression.Index, executableFunctions) &&
                CanEmitExpression(expression.Value, executableFunctions);
        }

        private bool CanEmitArrayLiteral(LoweredArrayLiteralExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            for (var i = 0; i < expression.Elements.Length; i++)
            {
                var element = expression.Elements[i];
                if (element is LoweredSpreadExpression spread)
                {
                    if (!CanEmitExpression(spread.Expression, executableFunctions))
                    {
                        return false;
                    }

                    continue;
                }

                if (!CanEmitExpression(element, executableFunctions))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanEmitMap(LoweredMapExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            for (var i = 0; i < expression.Entries.Length; i++)
            {
                var entry = expression.Entries[i];
                if (entry.Value is LoweredSpreadExpression spread)
                {
                    if (entry.Key != null || !CanEmitExpression(spread.Expression, executableFunctions))
                    {
                        return false;
                    }

                    continue;
                }

                if (!CanEmitMapEntryKey(entry) ||
                    !CanEmitExpression(entry.Value, executableFunctions))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanEmitLambda(LoweredLambdaExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            return expression.Function.IsValid &&
                executableFunctions.Contains(expression.Function) &&
                TryGetFunction(expression.Function, out var function) &&
                ClosureMaterializer.CanPlanMaterialize(function, requireName: false);
        }

        private bool CanEmitRegularCall(LoweredCallExpression call, IReadOnlySet<FunctionId> executableFunctions)
        {
            if (!CanEmitExpression(call.Target, executableFunctions))
            {
                return false;
            }

            for (var i = 0; i < call.Arguments.Length; i++)
            {
                if (!CanEmitArgument(call.Arguments[i], executableFunctions))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanEmitArgument(LoweredExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            return expression is LoweredSpreadExpression spread
                ? CanEmitExpression(spread.Expression, executableFunctions)
                : CanEmitExpression(expression, executableFunctions);
        }

        private bool TryResolveDirectCallTarget(LoweredCallExpression call, out FunctionPlan target)
        {
            target = null;
            if (!_session.CompileSession.Capabilities.CanUseModuleDirectCall ||
                !call.DirectFunction.IsValid ||
                !HasExecutableMethod(call.DirectFunction) ||
                !TryGetFunction(call.DirectFunction, out var function) ||
                !CanUseFastDirectSignature(function))
            {
                return false;
            }

            target = function;
            return true;
        }

        private bool TryResolveDirectCallTarget(
            LoweredCallExpression call,
            IReadOnlySet<FunctionId> executableFunctions,
            out FunctionPlan target)
        {
            target = null;
            if (!_session.CompileSession.Capabilities.CanUseModuleDirectCall ||
                !call.DirectFunction.IsValid ||
                !executableFunctions.Contains(call.DirectFunction) ||
                !TryGetFunction(call.DirectFunction, out var function) ||
                !CanUseFastDirectSignature(function))
            {
                return false;
            }

            target = function;
            return true;
        }

        private bool TryResolveMaterializedFunction(SymbolId symbolId, out FunctionPlan function)
        {
            function = null;
            return TryResolveMaterializedFunction(symbolId, null, out function) &&
                HasExecutableMethod(function.Id);
        }

        private bool HasExecutableMethod(FunctionId id)
        {
            return id.IsValid &&
                (uint)id.Value < (uint)_methodsByFunction.Length &&
                _methodsByFunction[id.Value].IsDefined;
        }

        private bool TryGetFunction(FunctionId id, out FunctionPlan function)
        {
            var functionsById = GetFunctionsById();
            if (id.IsValid &&
                (uint)id.Value < (uint)functionsById.Length)
            {
                function = functionsById[id.Value];
                return function != null;
            }

            function = null;
            return false;
        }

        private FunctionPlan GetFunction(FunctionId id)
        {
            var functionsById = GetFunctionsById();
            return functionsById[id.Value];
        }

        private FunctionPlan[] GetFunctionsById()
        {
            if (_functionsById != null)
            {
                return _functionsById;
            }

            if (_methodsByFunction.Length == 0)
            {
                _functionsById = Array.Empty<FunctionPlan>();
                return _functionsById;
            }

            var functionsById = new FunctionPlan[_methodsByFunction.Length];
            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                functionsById[function.Id.Value] = function;
            }

            _functionsById = functionsById;
            return functionsById;
        }

        private bool TryResolveMaterializedFunction(
            SymbolId symbolId,
            IReadOnlySet<FunctionId> executableFunctions,
            out FunctionPlan function)
        {
            function = null;
            if (!symbolId.IsValid)
            {
                return false;
            }

            var symbol = _session.CompileSession.Symbols[symbolId];
            if (symbol.Kind != BackendSymbolKind.Function || !symbol.Module.Equals(_module.Id))
            {
                return false;
            }

            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var candidate = _module.Functions[i];
                if (!ReferenceEquals(candidate.Declaration, symbol.Declaration))
                {
                    continue;
                }

                if (executableFunctions != null && !executableFunctions.Contains(candidate.Id))
                {
                    return false;
                }

                if (ClosureMaterializer.CanPlanMaterialize(candidate, requireName: true))
                {
                    function = candidate;
                    return true;
                }

                return false;
            }

            return false;
        }

        private static FunctionCallConvention SelectCallConvention(FunctionPlan function)
        {
            return CanUseFastDirectSignature(function)
                ? GetFastConvention(GetParameterCount(function))
                : FunctionCallConvention.Span;
        }

        private static bool CanUseFastDirectSignature(FunctionPlan function)
        {
            return function.IsDirectCallCandidate &&
                !function.HasDefaultParameters &&
                !function.UsesArgumentsObject &&
                GetParameterCount(function) <= 7;
        }

        private static int GetParameterCount(FunctionPlan function)
        {
            var count = 0;
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                if (function.LocalSlots[i].IsParameter)
                {
                    count++;
                }
            }

            return count;
        }

        private static Type[] GetParameterTypes(FunctionCallConvention convention)
        {
            return convention == FunctionCallConvention.Span
                ? s_standardParameterTypes
                : s_fastParameterTypes[GetFastArity(convention)];
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

        private string CreateDebuggerMetadata(FunctionPlan function, FunctionCallConvention convention)
        {
            if (_session.Options.Compiler.Mode != CompilationMode.Persistence ||
                _session.Options.Optimization.Level != OptimizeOptions.Debug)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.Append("v=1");
            builder.Append(";cc=");
            builder.Append(convention == FunctionCallConvention.Span ? "span" : "fast");
            builder.Append(";arity=");
            builder.Append(convention == FunctionCallConvention.Span ? GetParameterCount(function) : GetFastArity(convention));

            var parameterPlans = BuildParameterLoadPlans(function, convention);
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                var slot = function.LocalSlots[i];
                if (slot.Id.IsValid &&
                    _capturedLocalByLocalSlot != null &&
                    _capturedLocalByLocalSlot.ContainsKey(slot.Id.Value))
                {
                    continue;
                }

                if (slot.IsParameter)
                {
                    var plan = (uint)i < (uint)parameterPlans.Length ? parameterPlans[i] : default;
                    builder.Append(";p:");
                    AppendEscaped(builder, slot.Name);
                    builder.Append(':');
                    builder.Append(_locals != null && (uint)i < (uint)_locals.Length && _locals[i] != null ? _locals[i].LocalIndex : -1);
                    builder.Append(':');
                    builder.Append(plan.ParameterIndex);
                    builder.Append(':');
                    builder.Append(plan.CanLoadDirectly ? '1' : '0');
                    continue;
                }

                if (_locals == null || (uint)i >= (uint)_locals.Length || _locals[i] == null)
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
            if (!_module.ModuleScope.IsValid)
            {
                return;
            }

            var moduleScope = _session.CompileSession.Scopes[_module.ModuleScope];
            if (moduleScope.SymbolCount == 0)
            {
                return;
            }

            var symbols = _session.CompileSession.Symbols;
            for (var i = 0; i < moduleScope.SymbolCount; i++)
            {
                var symbol = symbols[new SymbolId(moduleScope.FirstSymbol.Value + i)];
                if (!CanShowModuleSymbolInDebugger(function, symbol))
                {
                    continue;
                }

                builder.Append(";m:");
                AppendEscaped(builder, symbol.Name);
            }
        }

        private static bool CanShowModuleSymbolInDebugger(FunctionPlan function, SymbolInfo symbol)
        {
            if (symbol.Kind is not (BackendSymbolKind.ModuleProperty or BackendSymbolKind.ImportAlias or BackendSymbolKind.Function or BackendSymbolKind.Enum))
            {
                return false;
            }

            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                if (string.Equals(function.LocalSlots[i].Name, symbol.Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            for (var i = 0; i < function.UpvalueSlots.Length; i++)
            {
                if (string.Equals(function.UpvalueSlots[i].Name, symbol.Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            for (var i = 0; i < function.CapturedLocalSlots.Length; i++)
            {
                if (string.Equals(function.CapturedLocalSlots[i].Name, symbol.Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AppendEscaped(StringBuilder builder, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch == '\\' || ch == ';' || ch == ':')
                {
                    builder.Append('\\');
                }

                builder.Append(ch);
            }
        }

        private static bool IsLocalName(LoweredNameExpression name)
        {
            return name.LocalSlot.IsValid && !name.UpvalueSlot.IsValid && !name.ModuleSymbol.IsValid;
        }

        private static bool HasSpread(LoweredExpression[] expressions)
        {
            for (var i = 0; i < expressions.Length; i++)
            {
                if (expressions[i] is LoweredSpreadExpression)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanEmitMapEntryKey(LoweredMapEntry entry)
        {
            return entry.Key != null ||
                entry.Value is LoweredNameExpression { Name: not null };
        }

        private static string GetMapEntryKey(LoweredMapEntry entry)
        {
            return entry.Key?.Value ?? ((LoweredNameExpression)entry.Value).Name;
        }

        private static MethodInfo GetBinaryMethod(Operator op)
        {
            if (op == Operator.Add) return RuntimeMetadata.CILHelper_Add;
            if (op == Operator.Subtract) return RuntimeMetadata.CILHelper_Subtract;
            if (op == Operator.Multiply) return RuntimeMetadata.CILHelper_Multiply;
            if (op == Operator.Divide) return RuntimeMetadata.CILHelper_Divide;
            if (op == Operator.Modulo) return RuntimeMetadata.CILHelper_Modulo;
            if (op == Operator.Equal) return RuntimeMetadata.CILHelper_Equal;
            if (op == Operator.NotEqual) return RuntimeMetadata.CILHelper_NotEqual;
            if (op == Operator.LessThan) return RuntimeMetadata.CILHelper_Less;
            if (op == Operator.LessThanOrEqual) return RuntimeMetadata.CILHelper_LessEqual;
            if (op == Operator.GreaterThan) return RuntimeMetadata.CILHelper_Greater;
            if (op == Operator.GreaterThanOrEqual) return RuntimeMetadata.CILHelper_GreaterEqual;
            if (op == Operator.BitwiseAnd) return RuntimeMetadata.CILHelper_BitwiseAnd;
            if (op == Operator.BitwiseOr) return RuntimeMetadata.CILHelper_BitwiseOr;
            if (op == Operator.BitwiseXor) return RuntimeMetadata.CILHelper_BitwiseXor;
            if (op == Operator.LeftShift) return RuntimeMetadata.CILHelper_LeftShift;
            if (op == Operator.SignedRightShift) return RuntimeMetadata.CILHelper_RightShift;
            if (op == Operator.UnSignedRightShift) return RuntimeMetadata.CILHelper_UnsignedRightShift;
            return null;
        }

        private static MethodInfo GetBinaryConditionMethod(Operator op)
        {
            if (op == Operator.Add) return RuntimeMetadata.CILHelper_AddBool;
            if (op == Operator.Subtract) return RuntimeMetadata.CILHelper_SubtractBool;
            if (op == Operator.Multiply) return RuntimeMetadata.CILHelper_MultiplyBool;
            if (op == Operator.Divide) return RuntimeMetadata.CILHelper_DivideBool;
            if (op == Operator.Modulo) return RuntimeMetadata.CILHelper_ModuloBool;
            if (op == Operator.Equal) return RuntimeMetadata.CILHelper_EqualBool;
            if (op == Operator.NotEqual) return RuntimeMetadata.CILHelper_NotEqualBool;
            if (op == Operator.LessThan) return RuntimeMetadata.CILHelper_LessBool;
            if (op == Operator.LessThanOrEqual) return RuntimeMetadata.CILHelper_LessEqualBool;
            if (op == Operator.GreaterThan) return RuntimeMetadata.CILHelper_GreaterBool;
            if (op == Operator.GreaterThanOrEqual) return RuntimeMetadata.CILHelper_GreaterEqualBool;
            if (op == Operator.BitwiseAnd) return RuntimeMetadata.CILHelper_BitwiseAndBool;
            if (op == Operator.BitwiseOr) return RuntimeMetadata.CILHelper_BitwiseOrBool;
            if (op == Operator.BitwiseXor) return RuntimeMetadata.CILHelper_BitwiseXorBool;
            if (op == Operator.LeftShift) return RuntimeMetadata.CILHelper_LeftShiftBool;
            if (op == Operator.SignedRightShift) return RuntimeMetadata.CILHelper_RightShiftBool;
            if (op == Operator.UnSignedRightShift) return RuntimeMetadata.CILHelper_UnsignedRightShiftBool;
            return null;
        }

        private static MethodInfo GetUnaryMethod(Operator op)
        {
            if (op == Operator.LogicalNot) return RuntimeMetadata.CILHelper_Not;
            if (op == Operator.BitwiseNot) return RuntimeMetadata.CILHelper_BitwiseNot;
            if (op == Operator.Negate) return RuntimeMetadata.CILHelper_Negate;
            if (op == Operator.TypeOf) return RuntimeMetadata.CILHelper_TypeOf;
            return null;
        }

        private static MethodInfo GetIncrementMethod(Operator op)
        {
            if (op == Operator.PreIncrement) return RuntimeMetadata.CILHelper_IncrementPrefix;
            if (op == Operator.PostIncrement) return RuntimeMetadata.CILHelper_IncrementPostfix;
            if (op == Operator.PreDecrement) return RuntimeMetadata.CILHelper_DecrementPrefix;
            if (op == Operator.PostDecrement) return RuntimeMetadata.CILHelper_DecrementPostfix;
            return null;
        }

        private static MethodInfo GetMutationVoidMethod(Operator op)
        {
            if (op == Operator.PreIncrement || op == Operator.PostIncrement) return RuntimeMetadata.CILHelper_IncrementVoid;
            if (op == Operator.PreDecrement || op == Operator.PostDecrement) return RuntimeMetadata.CILHelper_DecrementVoid;
            return null;
        }

        private static MethodInfo GetElementMutationMethod(Operator op)
        {
            if (op == Operator.PreIncrement) return RuntimeMetadata.CILHelper_IncrementElementPrefix;
            if (op == Operator.PostIncrement) return RuntimeMetadata.CILHelper_IncrementElementPostfix;
            if (op == Operator.PreDecrement) return RuntimeMetadata.CILHelper_DecrementElementPrefix;
            if (op == Operator.PostDecrement) return RuntimeMetadata.CILHelper_DecrementElementPostfix;
            return null;
        }

        private static MethodInfo GetElementMutationVoidMethod(Operator op)
        {
            if (op == Operator.PreIncrement || op == Operator.PostIncrement) return RuntimeMetadata.CILHelper_IncrementElementVoid;
            if (op == Operator.PreDecrement || op == Operator.PostDecrement) return RuntimeMetadata.CILHelper_DecrementElementVoid;
            return null;
        }

        private static MethodInfo GetPropertyMutationMethod(Operator op)
        {
            if (op == Operator.PreIncrement) return RuntimeMetadata.CILHelper_IncrementPropertyPrefix;
            if (op == Operator.PostIncrement) return RuntimeMetadata.CILHelper_IncrementPropertyPostfix;
            if (op == Operator.PreDecrement) return RuntimeMetadata.CILHelper_DecrementPropertyPrefix;
            if (op == Operator.PostDecrement) return RuntimeMetadata.CILHelper_DecrementPropertyPostfix;
            return null;
        }

        private static MethodInfo GetPropertyMutationVoidMethod(Operator op)
        {
            if (op == Operator.PreIncrement || op == Operator.PostIncrement) return RuntimeMetadata.CILHelper_IncrementPropertyVoid;
            if (op == Operator.PreDecrement || op == Operator.PostDecrement) return RuntimeMetadata.CILHelper_DecrementPropertyVoid;
            return null;
        }

        private static MethodInfo GetInvokeMethod(int argumentCount)
        {
            return argumentCount switch
            {
                0 => RuntimeMetadata.CILHelper_Invoke0,
                1 => RuntimeMetadata.CILHelper_Invoke1,
                2 => RuntimeMetadata.CILHelper_Invoke2,
                3 => RuntimeMetadata.CILHelper_Invoke3,
                4 => RuntimeMetadata.CILHelper_Invoke4,
                5 => RuntimeMetadata.CILHelper_Invoke5,
                6 => RuntimeMetadata.CILHelper_Invoke6,
                7 => RuntimeMetadata.CILHelper_Invoke7,
                _ => throw new NotSupportedException("Regular call arity " + argumentCount)
            };
        }

        private static MethodInfo GetInvokePropertyMethod(int argumentCount)
        {
            return argumentCount switch
            {
                0 => RuntimeMetadata.CILHelper_InvokeProperty0,
                1 => RuntimeMetadata.CILHelper_InvokeProperty1,
                2 => RuntimeMetadata.CILHelper_InvokeProperty2,
                3 => RuntimeMetadata.CILHelper_InvokeProperty3,
                4 => RuntimeMetadata.CILHelper_InvokeProperty4,
                5 => RuntimeMetadata.CILHelper_InvokeProperty5,
                6 => RuntimeMetadata.CILHelper_InvokeProperty6,
                7 => RuntimeMetadata.CILHelper_InvokeProperty7,
                _ => throw new NotSupportedException("Property call arity " + argumentCount)
            };
        }

        private static MethodInfo GetNewMethod(int argumentCount)
        {
            return argumentCount switch
            {
                0 => RuntimeMetadata.CILHelper_New0,
                1 => RuntimeMetadata.CILHelper_New1,
                2 => RuntimeMetadata.CILHelper_New2,
                _ => throw new NotSupportedException("Constructor arity " + argumentCount)
            };
        }

        private static bool IsPropertyCall(LoweredCallExpression call)
        {
            return call.Target is LoweredGetPropertyExpression property &&
                TryGetStaticPropertyName(property, out _);
        }

        private static bool TryGetStaticPropertyName(LoweredGetPropertyExpression property, out string name)
        {
            if (property.Property is LoweredNameExpression propertyName &&
                !string.IsNullOrEmpty(propertyName.Name))
            {
                name = propertyName.Name;
                return true;
            }

            name = null;
            return false;
        }

        private static bool TryGetStaticPropertyName(LoweredSetPropertyExpression property, out string name)
        {
            if (property.Property is LoweredNameExpression propertyName &&
                !string.IsNullOrEmpty(propertyName.Name))
            {
                name = propertyName.Name;
                return true;
            }

            name = null;
            return false;
        }

        private static bool TryGetFastObject3(
            LoweredMapExpression expression,
            out LoweredMapEntry first,
            out LoweredMapEntry second,
            out LoweredMapEntry third)
        {
            first = default;
            second = default;
            third = default;
            if (expression.Entries.Length != 3)
            {
                return false;
            }

            var firstEntry = expression.Entries[0];
            var secondEntry = expression.Entries[1];
            var thirdEntry = expression.Entries[2];
            if (firstEntry.Key == null ||
                secondEntry.Key == null ||
                thirdEntry.Key == null ||
                firstEntry.Value == null ||
                secondEntry.Value == null ||
                thirdEntry.Value == null)
            {
                return false;
            }

            var firstKey = firstEntry.Key.Value;
            var secondKey = secondEntry.Key.Value;
            var thirdKey = thirdEntry.Key.Value;
            if (StringComparer.Ordinal.Equals(firstKey, secondKey) ||
                StringComparer.Ordinal.Equals(firstKey, thirdKey) ||
                StringComparer.Ordinal.Equals(secondKey, thirdKey))
            {
                return false;
            }

            first = firstEntry;
            second = secondEntry;
            third = thirdEntry;
            return true;
        }

        private struct ExecutableMethod
        {
            public ExecutableMethod(MethodInfo method, ILGenerator il, FunctionCallConvention convention)
            {
                Method = method;
                IL = il;
                Convention = convention;
                Emitted = false;
            }

            public MethodInfo Method { get; }
            public ILGenerator IL { get; }
            public FunctionCallConvention Convention { get; }
            public bool Emitted { get; set; }
            public bool IsDefined => Method != null;
        }

        private readonly struct ParameterLoadPlan
        {
            public ParameterLoadPlan(bool canLoadDirectly, int parameterIndex, FunctionCallConvention convention)
            {
                CanLoadDirectly = canLoadDirectly;
                ParameterIndex = parameterIndex;
                Convention = convention;
            }

            public bool CanLoadDirectly { get; }
            public int ParameterIndex { get; }
            public FunctionCallConvention Convention { get; }
        }

        private readonly struct LocalUsage
        {
            public LocalUsage(int[] reads, int[] writes, int[] addresses)
            {
                Reads = reads;
                Writes = writes;
                Addresses = addresses;
            }

            public int[] Reads { get; }
            public int[] Writes { get; }
            public int[] Addresses { get; }
        }

        private sealed class LocalUsageAnalyzer
        {
            private readonly int[] _reads;
            private readonly int[] _writes;
            private readonly int[] _addresses;

            private LocalUsageAnalyzer(int localCount)
            {
                _reads = new int[localCount];
                _writes = new int[localCount];
                _addresses = new int[localCount];
            }

            public static LocalUsage Analyze(FunctionPlan function)
            {
                var analyzer = new LocalUsageAnalyzer(function.LocalSlots.Length);
                analyzer.VisitStatement(function.Body);
                return new LocalUsage(analyzer._reads, analyzer._writes, analyzer._addresses);
            }

            private void Read(LocalSlotId slot)
            {
                if (slot.IsValid && (uint)slot.Value < (uint)_reads.Length)
                {
                    _reads[slot.Value]++;
                }
            }

            private void Write(LocalSlotId slot)
            {
                if (slot.IsValid && (uint)slot.Value < (uint)_writes.Length)
                {
                    _writes[slot.Value]++;
                }
            }

            private void Address(LocalSlotId slot)
            {
                if (slot.IsValid && (uint)slot.Value < (uint)_addresses.Length)
                {
                    _addresses[slot.Value]++;
                }
            }

            private void VisitStatement(LoweredStatement statement)
            {
                switch (statement)
                {
                    case null:
                    case LoweredNoOpStatement:
                        return;
                    case LoweredBlockStatement block:
                        for (var i = 0; i < block.Statements.Length; i++)
                        {
                            VisitStatement(block.Statements[i]);
                        }
                        return;
                    case LoweredExpressionStatement expression:
                        VisitExpression(expression.Expression);
                        return;
                    case LoweredReturnStatement @return:
                        VisitExpression(@return.Expression);
                        return;
                    case LoweredVariableDeclarationStatement variable:
                        Write(variable.Slot);
                        VisitExpression(variable.Initializer);
                        return;
                    case LoweredObjectDestructuringDeclarationStatement destructuring:
                        VisitExpression(destructuring.Initializer);
                        for (var i = 0; i < destructuring.Bindings.Length; i++)
                        {
                            Write(destructuring.Bindings[i].Slot);
                        }
                        return;
                    case LoweredArrayDestructuringDeclarationStatement destructuring:
                        VisitExpression(destructuring.Initializer);
                        for (var i = 0; i < destructuring.Bindings.Length; i++)
                        {
                            Write(destructuring.Bindings[i].Slot);
                        }
                        return;
                    case LoweredFunctionDeclarationStatement function:
                        Write(function.LocalSlot);
                        return;
                    case LoweredIfStatement ifStatement:
                        VisitExpression(ifStatement.Condition);
                        VisitStatement(ifStatement.Body);
                        VisitStatement(ifStatement.Else);
                        return;
                    case LoweredWhileStatement whileStatement:
                        VisitExpression(whileStatement.Condition);
                        VisitStatement(whileStatement.Body);
                        return;
                    case LoweredForStatement forStatement:
                        VisitStatement(forStatement.Initializer);
                        VisitExpression(forStatement.Condition);
                        VisitStatement(forStatement.Body);
                        VisitExpression(forStatement.Incrementor);
                        return;
                    case LoweredForInStatement forInStatement:
                        VisitStatement(forInStatement.Initializer);
                        VisitExpression(forInStatement.Iterator?.Right);
                        if (forInStatement.Iterator?.Left is LoweredNameExpression iteratorName)
                        {
                            VisitWriteTarget(iteratorName);
                        }
                        VisitStatement(forInStatement.Body);
                        return;
                    case LoweredTryStatement tryStatement:
                        VisitStatement(tryStatement.Body);
                        Write(tryStatement.CatchSlot);
                        VisitStatement(tryStatement.CatchBody);
                        VisitStatement(tryStatement.FinallyBody);
                        return;
                    case LoweredThrowStatement throwStatement:
                        VisitExpression(throwStatement.Expression);
                        return;
                    case LoweredDeleteStatement deleteStatement:
                        VisitExpression(deleteStatement.Expression);
                        return;
                    case LoweredBreakStatement:
                    case LoweredContinueStatement:
                    case LoweredDebuggerStatement:
                    case LoweredUnsupportedStatement:
                        return;
                    default:
                        throw new NotSupportedException(statement.GetType().Name);
                }
            }

            private void VisitExpression(LoweredExpression expression)
            {
                switch (expression)
                {
                    case null:
                    case LoweredLiteralExpression:
                    case LoweredUnsupportedExpression:
                        return;
                    case LoweredNameExpression name:
                        Read(name.LocalSlot);
                        return;
                    case LoweredBinaryExpression binary:
                        VisitExpression(binary.Left);
                        VisitExpression(binary.Right);
                        return;
                    case LoweredTemplateStringExpression template:
                        for (var i = 0; i < template.Parts.Length; i++)
                        {
                            var part = template.Parts[i];
                            if (!part.IsLiteral)
                            {
                                VisitExpression(part.Expression);
                            }
                        }
                        return;
                    case LoweredCallExpression call:
                        VisitExpression(call.Target);
                        for (var i = 0; i < call.Arguments.Length; i++)
                        {
                            VisitExpression(call.Arguments[i]);
                        }
                        return;
                    case LoweredAssignmentExpression assignment:
                        VisitExpression(assignment.Right);
                        VisitWriteTarget(assignment.Left);
                        return;
                    case LoweredCompoundExpression compound:
                        VisitExpression(compound.Left);
                        VisitExpression(compound.Right);
                        VisitWriteTarget(compound.Left);
                        return;
                    case LoweredUnaryExpression unary:
                        if (GetIncrementMethod(unary.Operator) != null)
                        {
                            VisitExpression(unary.Expression);
                            VisitWriteTarget(unary.Expression);
                            return;
                        }
                        VisitExpression(unary.Expression);
                        return;
                    case LoweredInExpression inExpression:
                        VisitExpression(inExpression.Left);
                        VisitExpression(inExpression.Right);
                        return;
                    case LoweredGetPropertyExpression property:
                        VisitExpression(property.Instance);
                        VisitExpression(property.Property);
                        return;
                    case LoweredGetElementExpression element:
                        VisitExpression(element.Instance);
                        VisitExpression(element.Index);
                        return;
                    case LoweredSetPropertyExpression property:
                        VisitExpression(property.Instance);
                        VisitExpression(property.Property);
                        VisitExpression(property.Value);
                        return;
                    case LoweredSetElementExpression element:
                        VisitExpression(element.Instance);
                        VisitExpression(element.Index);
                        VisitExpression(element.Value);
                        return;
                    case LoweredArrayLiteralExpression array:
                        for (var i = 0; i < array.Elements.Length; i++)
                        {
                            VisitExpression(array.Elements[i]);
                        }
                        return;
                    case LoweredMapExpression map:
                        for (var i = 0; i < map.Entries.Length; i++)
                        {
                            VisitExpression(map.Entries[i].Value);
                        }
                        return;
                    case LoweredSpreadExpression spread:
                        VisitExpression(spread.Expression);
                        return;
                    case LoweredNewExpression @new:
                        VisitExpression(@new.Expression);
                        return;
                    case LoweredLambdaExpression:
                        return;
                    default:
                        throw new NotSupportedException(expression.GetType().Name);
                }
            }

            private void VisitWriteTarget(LoweredExpression expression)
            {
                switch (expression)
                {
                    case LoweredNameExpression name:
                        Write(name.LocalSlot);
                        if (name.LocalSlot.IsValid || name.UpvalueSlot.IsValid)
                        {
                            Address(name.LocalSlot);
                        }
                        return;
                    case LoweredGetPropertyExpression property:
                        VisitExpression(property.Instance);
                        VisitExpression(property.Property);
                        return;
                    case LoweredGetElementExpression element:
                        VisitExpression(element.Instance);
                        VisitExpression(element.Index);
                        return;
                    default:
                        VisitExpression(expression);
                        return;
                }
            }
        }
    }
}
