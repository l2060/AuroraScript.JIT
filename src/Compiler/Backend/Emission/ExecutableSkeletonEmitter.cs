using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Lowering;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Emits;
using AuroraScript.Compiler.Emits.Builders;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
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
        private readonly Dictionary<FunctionId, FunctionPlan> _functionsById = new();
        private readonly Dictionary<FunctionId, ExecutableMethod> _methodsByFunction = new();
        private readonly Stack<Label> _breakLabels = new();
        private readonly Stack<Label> _continueLabels = new();
        private bool _prepared;
        private ILGenerator _il;
        private LocalBuilder[] _locals;
        private int _cilLocalCount;

        public ExecutableSkeletonEmitter(EmissionSession session, ModulePlan module)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _module = module ?? throw new ArgumentNullException(nameof(module));

            for (var i = 0; i < module.Functions.Count; i++)
            {
                _functionsById[module.Functions[i].Id] = module.Functions[i];
            }
        }

        public void Prepare()
        {
            if (_prepared)
            {
                return;
            }

            _prepared = true;
            var executable = BuildExecutableSet();
            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                if (!executable.Contains(function.Id))
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
                _methodsByFunction.Add(function.Id, new ExecutableMethod(function, method, il, convention));
            }
        }

        public bool TryEmit(FunctionPlan function, out MethodInfo method, out int localCount)
        {
            method = null;
            localCount = 0;
            if (!_prepared)
            {
                Prepare();
            }

            if (!_methodsByFunction.TryGetValue(function.Id, out var executable) || executable.Emitted)
            {
                return false;
            }

            executable.Emitted = true;
            method = executable.Method;
            _il = executable.IL;
            _locals = DeclareLocals(function);
            _cilLocalCount = _locals.Length;
            InitializeParameters(function, executable.Convention);
            EmitStatement(function.Body);
            LoadNull();
            _il.Emit(OpCodes.Ret);
            localCount = _cilLocalCount;
            return true;
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
                function.Body != null &&
                function.UpvalueSlots.Length == 0 &&
                function.CapturedLocalSlots.Length == 0;
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

            var locals = new LocalBuilder[function.LocalSlots.Length];
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                locals[i] = _il.DeclareLocal(typeof(ScriptDatum));
                _session.Builder.SetLocalSymInfo(locals[i], function.LocalSlots[i].Name);
            }
            return locals;
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

                if (convention == FunctionCallConvention.Span)
                {
                    _il.Emit(OpCodes.Ldarg_1);
                    _il.Emit(OpCodes.Ldc_I4, parameterIndex);
                    var defaultValue = GetParameterDefault(function, parameterIndex);
                    parameterIndex++;
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
                    _il.Emit(OpCodes.Ldarg, parameterIndex + 1);
                    parameterIndex++;
                }

                _il.Emit(OpCodes.Stloc, _locals[slot.Id.Value]);
            }
        }

        private static LoweredExpression GetParameterDefault(FunctionPlan function, int parameterIndex)
        {
            return function.ParameterDefaults != null &&
                parameterIndex >= 0 &&
                parameterIndex < function.ParameterDefaults.Length
                ? function.ParameterDefaults[parameterIndex]
                : null;
        }

        private void EmitStatement(LoweredStatement statement)
        {
            switch (statement)
            {
                case null:
                    return;
                case LoweredBlockStatement block:
                    for (var i = 0; i < block.Statements.Length; i++)
                    {
                        EmitStatement(block.Statements[i]);
                    }
                    return;
                case LoweredReturnStatement returnStatement:
                    EmitExpressionOrNull(returnStatement.Expression);
                    _il.Emit(OpCodes.Ret);
                    return;
                case LoweredVariableDeclarationStatement variable:
                    EmitExpressionOrNull(variable.Initializer);
                    _il.Emit(OpCodes.Stloc, _locals[variable.Slot.Value]);
                    return;
                case LoweredExpressionStatement expressionStatement:
                    EmitExpressionOrNull(expressionStatement.Expression);
                    _il.Emit(OpCodes.Pop);
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
                default:
                    throw new NotSupportedException(statement.GetType().Name);
            }
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
                EmitExpression(statement.Incrementor);
                _il.Emit(OpCodes.Pop);
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
            _il.Emit(OpCodes.Ldloca, _locals[((LoweredNameExpression)statement.Iterator.Left).LocalSlot.Value]);
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
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ExceptionToError);
                    _il.Emit(OpCodes.Stloc, _locals[statement.CatchSlot.Value]);
                }
                else
                {
                    _il.Emit(OpCodes.Pop);
                }

                EmitStatement(statement.CatchBody);
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
                _session.Options.OptimizeOption == OptimizeOptions.Debug)
            {
                _il.Emit(OpCodes.Break);
            }
        }

        private void EmitCondition(LoweredExpression expression)
        {
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

            EmitExpression(expression.Left);
            EmitExpression(expression.Right);
            _il.Emit(OpCodes.Call, GetBinaryMethod(expression.Operator));
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
            _il.Emit(OpCodes.Stloc, _locals[name.LocalSlot.Value]);
        }

        private void EmitCompound(LoweredCompoundExpression expression)
        {
            var name = (LoweredNameExpression)expression.Left;
            _il.Emit(OpCodes.Ldloc, _locals[name.LocalSlot.Value]);
            EmitExpression(expression.Right);
            _il.Emit(OpCodes.Call, GetBinaryMethod(expression.Operator.SimplerOperator));
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, _locals[name.LocalSlot.Value]);
        }

        private void EmitUnary(LoweredUnaryExpression expression)
        {
            var incrementMethod = GetIncrementMethod(expression.Operator);
            if (incrementMethod != null)
            {
                var name = (LoweredNameExpression)expression.Expression;
                _il.Emit(OpCodes.Ldloca, _locals[name.LocalSlot.Value]);
                _il.Emit(OpCodes.Call, incrementMethod);
                return;
            }

            EmitExpression(expression.Expression);
            _il.Emit(OpCodes.Call, GetUnaryMethod(expression.Operator));
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

        private void EmitGetElement(LoweredGetElementExpression expression)
        {
            EmitExpression(expression.Instance);
            EmitExpression(expression.Index);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetElementDatum);
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
            var function = _functionsById[expression.Function];
            ClosureMaterializer.EmitClosure(_session, _il, function);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
        }

        private void EmitNew(LoweredNewExpression expression)
        {
            var call = expression.Expression;
            if (call.Arguments.Length > 2)
            {
                EmitNewMany(call);
                return;
            }

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

            EmitExpression(call.Target);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Stloc, typeLocal);
            var argumentLocals = EmitArgumentsToLocals(call.Arguments);

            _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_RentArguments);
            _il.Emit(OpCodes.Stloc, argsLocal);

            for (var i = 0; i < argumentLocals.Length; i++)
            {
                _il.Emit(OpCodes.Ldloc, argsLocal);
                _il.Emit(OpCodes.Ldc_I4, i);
                _il.Emit(OpCodes.Ldloc, argumentLocals[i]);
                _il.Emit(OpCodes.Stelem, typeof(ScriptDatum));
            }

            _il.Emit(OpCodes.Ldloc, typeLocal);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldloc, argsLocal);
            _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
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
            if (call.Arguments.Length <= arity)
            {
                _il.Emit(OpCodes.Ldarg_0);
                for (var i = 0; i < arity; i++)
                {
                    if (i < call.Arguments.Length)
                    {
                        EmitExpression(call.Arguments[i]);
                    }
                    else
                    {
                        LoadNull();
                    }
                }

                _il.Emit(OpCodes.Call, target.Method);
                return;
            }

            var temps = arity == 0 ? Array.Empty<LocalBuilder>() : new LocalBuilder[arity];
            for (var i = 0; i < arity; i++)
            {
                temps[i] = DeclareTemp();
            }

            for (var i = 0; i < call.Arguments.Length; i++)
            {
                EmitExpression(call.Arguments[i]);
                if (i < arity)
                {
                    _il.Emit(OpCodes.Stloc, temps[i]);
                }
                else
                {
                    _il.Emit(OpCodes.Pop);
                }
            }

            _il.Emit(OpCodes.Ldarg_0);
            for (var i = 0; i < arity; i++)
            {
                _il.Emit(OpCodes.Ldloc, temps[i]);
            }
            _il.Emit(OpCodes.Call, target.Method);
        }

        private void EmitRegularCall(LoweredCallExpression call)
        {
            if (call.Arguments.Length > 7)
            {
                EmitRegularCallMany(call);
                return;
            }

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

            if (call.Arguments.Length > 7)
            {
                EmitPropertyCallMany(call, property, name);
                return;
            }

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

            EmitExpression(property.Instance);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Stloc, receiverLocal);
            var argumentLocals = EmitArgumentsToLocals(call.Arguments);

            _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_RentArguments);
            _il.Emit(OpCodes.Stloc, argsLocal);

            for (var i = 0; i < argumentLocals.Length; i++)
            {
                _il.Emit(OpCodes.Ldloc, argsLocal);
                _il.Emit(OpCodes.Ldc_I4, i);
                _il.Emit(OpCodes.Ldloc, argumentLocals[i]);
                _il.Emit(OpCodes.Stelem, typeof(ScriptDatum));
            }

            _il.Emit(OpCodes.Ldloc, receiverLocal);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Ldloc, argsLocal);
            _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_InvokePropertyMany);
        }

        private void EmitRegularCallMany(LoweredCallExpression call)
        {
            var functionLocal = DeclareLocal(typeof(ScriptObject));
            var argsLocal = DeclareLocal(typeof(ScriptDatum[]));

            EmitExpression(call.Target);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Stloc, functionLocal);
            var argumentLocals = EmitArgumentsToLocals(call.Arguments);

            _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_RentArguments);
            _il.Emit(OpCodes.Stloc, argsLocal);

            for (var i = 0; i < argumentLocals.Length; i++)
            {
                _il.Emit(OpCodes.Ldloc, argsLocal);
                _il.Emit(OpCodes.Ldc_I4, i);
                _il.Emit(OpCodes.Ldloc, argumentLocals[i]);
                _il.Emit(OpCodes.Stelem, typeof(ScriptDatum));
            }

            _il.Emit(OpCodes.Ldloc, functionLocal);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldloc, argsLocal);
            _il.Emit(OpCodes.Ldc_I4, call.Arguments.Length);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_InvokeMany);
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
            if (IsLocalName(name))
            {
                _il.Emit(OpCodes.Ldloc, _locals[name.LocalSlot.Value]);
                return;
            }

            if (TryResolveMaterializedFunction(name.ModuleSymbol, out var function))
            {
                EmitModuleFunctionLoad(function);
                return;
            }

            throw new NotSupportedException("Name " + (name.Name ?? "<null>"));
        }

        private void EmitModuleFunctionLoad(FunctionPlan function)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, function.Name);
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

        private bool CanEmitStatement(LoweredStatement statement, IReadOnlySet<FunctionId> executableFunctions)
        {
            return statement switch
            {
                null => true,
                LoweredBlockStatement block => CanEmitBlock(block, executableFunctions),
                LoweredReturnStatement returnStatement => CanEmitExpression(returnStatement.Expression, executableFunctions),
                LoweredVariableDeclarationStatement variable => variable.Slot.IsValid && CanEmitExpression(variable.Initializer, executableFunctions),
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
                LoweredBlockStatement block => CanEmitProtectedBlock(block, executableFunctions),
                LoweredVariableDeclarationStatement variable => variable.Slot.IsValid && CanEmitExpression(variable.Initializer, executableFunctions),
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

        private bool CanEmitExpression(LoweredExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            return expression switch
            {
                null => true,
                LoweredLiteralExpression literal => literal.Token is NumberToken or StringToken or BooleanToken or NullToken,
                LoweredNameExpression name => CanEmitName(name, executableFunctions),
                LoweredBinaryExpression binary => CanEmitBinary(binary, executableFunctions),
                LoweredAssignmentExpression assignment => assignment.Left is LoweredNameExpression name &&
                    IsLocalName(name) &&
                    CanEmitExpression(assignment.Right, executableFunctions),
                LoweredCompoundExpression compound => compound.Left is LoweredNameExpression name &&
                    IsLocalName(name) &&
                    GetBinaryMethod(compound.Operator.SimplerOperator) != null &&
                    CanEmitExpression(compound.Right, executableFunctions),
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

        private bool CanEmitUnary(LoweredUnaryExpression expression, IReadOnlySet<FunctionId> executableFunctions)
        {
            if (GetIncrementMethod(expression.Operator) != null)
            {
                return expression.Expression is LoweredNameExpression name &&
                    IsLocalName(name);
            }

            return GetUnaryMethod(expression.Operator) != null &&
                CanEmitExpression(expression.Expression, executableFunctions);
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
            if (!TryResolveDirectCallTarget(call, executableFunctions, out _))
            {
                return false;
            }

            for (var i = 0; i < call.Arguments.Length; i++)
            {
                if (!CanEmitExpression(call.Arguments[i], executableFunctions))
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
                if (!CanEmitExpression(call.Arguments[i], executableFunctions))
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
                if (!CanEmitExpression(call.Arguments[i], executableFunctions))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanEmitName(LoweredNameExpression name, IReadOnlySet<FunctionId> executableFunctions)
        {
            return IsLocalName(name) || TryResolveMaterializedFunction(name.ModuleSymbol, executableFunctions, out _);
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
                _functionsById.TryGetValue(expression.Function, out var function) &&
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
                if (!CanEmitExpression(call.Arguments[i], executableFunctions))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryResolveDirectCallTarget(LoweredCallExpression call, out FunctionPlan target)
        {
            target = null;
            if (!_session.CompileSession.Capabilities.CanUseModuleDirectCall ||
                !call.DirectFunction.IsValid ||
                !_methodsByFunction.ContainsKey(call.DirectFunction) ||
                !_functionsById.TryGetValue(call.DirectFunction, out var function) ||
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
                !_functionsById.TryGetValue(call.DirectFunction, out var function) ||
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
                _methodsByFunction.ContainsKey(function.Id);
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
                function.Visibility == FunctionVisibility.InternalOnly &&
                !function.RequiresClosureObject &&
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

        private sealed class ExecutableMethod
        {
            public ExecutableMethod(FunctionPlan function, MethodInfo method, ILGenerator il, FunctionCallConvention convention)
            {
                Function = function;
                Method = method;
                IL = il;
                Convention = convention;
            }

            public FunctionPlan Function { get; }
            public MethodInfo Method { get; }
            public ILGenerator IL { get; }
            public FunctionCallConvention Convention { get; }
            public bool Emitted { get; set; }
        }
    }
}
