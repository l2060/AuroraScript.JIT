using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Analysis;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class ModuleInitializerEmitter
    {
        private readonly EmissionSession _session;
        private readonly ModulePlan _module;
        private Dictionary<FunctionDeclaration, FunctionPlan> _functionsByDeclaration;
        private Dictionary<string, FunctionPlan> _directFunctionsByName;
        private MethodInfo _initializer;
        private ILGenerator _il;
        private bool _defined;
        private bool _emitted;

        public ModuleInitializerEmitter(EmissionSession session, ModulePlan module)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _module = module ?? throw new ArgumentNullException(nameof(module));
        }

        public void Define()
        {
            if (_defined)
            {
                return;
            }

            var method = _session.Builder.DefineModuleInitMethod(_module.Declaration);
            _initializer = method.Method;
            _il = method.IL;
            _module.Initializer = _initializer;
            _defined = true;
        }

        public bool TryEmit(out MethodInfo initializer)
        {
            initializer = null;
            if (_emitted)
            {
                initializer = _initializer;
                return true;
            }

            Define();
            for (var i = 0; i < _module.Declaration.Imports.Count; i++)
            {
                var import = _module.Declaration.Imports[i];
                if (!import.Include)
                {
                    MarkSequencePoint(import);
                    EmitImportAlias(import);
                }
            }

            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                if (!CanMaterialize(function))
                {
                    continue;
                }

                EmitDefineFunction(_il, function);
            }

            for (var i = 0; i < _module.Declaration.Length; i++)
            {
                EmitModuleStatement(_module.Declaration[i]);
            }

            for (var i = 0; i < _module.Declaration.Imports.Count; i++)
            {
                var import = _module.Declaration.Imports[i];
                if (import.Include)
                {
                    MarkSequencePoint(import);
                    EmitInclude(import);
                }
            }

            _il.Emit(OpCodes.Ret);
            _emitted = true;

            initializer = _initializer;
            return true;
        }

        private static bool CanMaterialize(FunctionPlan function)
        {
            return function.IsModuleFunction &&
                function.UpvalueSlots.Length == 0 &&
                ClosureMaterializer.CanMaterialize(function, requireName: true);
        }

        private void EmitDefineFunction(ILGenerator il, FunctionPlan function)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);
            _session.Builder.LoadStringConstant(il, function.Name);
            ClosureMaterializer.EmitClosure(_session, il, function);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Callvirt, _session.ForceModuleDefinitions
                ? RuntimeMetadata.ScriptObject_Patch
                : RuntimeMetadata.ScriptObject_Define);
        }

        private void EmitImportAlias(ImportDeclaration import)
        {
            if (import.Name == null || import.ModuleName == null)
            {
                return;
            }

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);
            _session.Builder.LoadStringConstant(_il, import.Name.Value);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
            _session.Builder.LoadStringConstant(_il, import.ModuleName);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptGlobal_GetModule);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_Define);
        }

        private void EmitInclude(ImportDeclaration import)
        {
            if (import.ModuleName == null)
            {
                return;
            }

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
            _session.Builder.LoadStringConstant(_il, import.ModuleName);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptGlobal_GetModule);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_CopyEnumerablePropertysFrom);
        }

        private void EmitModuleStatement(AstNode node)
        {
            switch (node)
            {
                case null:
                case ModuleMetaStatement:
                case FunctionDeclaration:
                case ImportDeclaration:
                    return;
                case VariableDeclaration variable:
                    MarkSequencePoint(variable);
                    EmitVariableDeclaration(variable);
                    return;
                case EnumDeclaration enumDeclaration:
                    MarkSequencePoint(enumDeclaration);
                    EmitEnum(enumDeclaration);
                    return;
                case ExpressionStatement expressionStatement:
                    MarkSequencePoint(expressionStatement);
                    EmitExpressionOrNull(expressionStatement.Expression);
                    _il.Emit(OpCodes.Pop);
                    return;
                default:
                    throw new NotSupportedException("Module initializer statement " + node.GetType().Name);
            }
        }

        private void MarkSequencePoint(AstNode node)
        {
            if (node == null)
            {
                return;
            }

            _session.Builder.MarkSequencePoint(node.Range, _il);
        }

        private void EmitVariableDeclaration(VariableDeclaration variable)
        {
            if (variable.Name != null)
            {
                EmitDefineDatum(variable, variable.Name.Value, variable.Initializer, writable: !variable.IsConst);
                return;
            }

            throw new NotSupportedException("Module destructuring declaration");
        }

        private void EmitDefineDatum(VariableDeclaration declaration, string name, Expression initializer, bool writable)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);
            _session.Builder.LoadStringConstant(_il, name);
            if (_module.TryGetSymbol(name, out var symbolId) &&
                ReferenceEquals(_session.CompileSession.Symbols[symbolId].Declaration, declaration) &&
                _module.TryGetInlineConstant(symbolId, out var constant))
            {
                EmitLiteral(ModuleConstInliningAnalyzer.CreateLiteralExpression(constant, SourceSpan.None));
            }
            else
            {
                EmitExpressionOrNull(initializer);
            }
            _il.Emit(writable ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(_session.ForceModuleDefinitions ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptObject_InternalDefineDatum);
        }

        private void EmitEnum(EnumDeclaration enumDeclaration)
        {
            if (enumDeclaration.Identifier == null)
            {
                return;
            }

            var enumLocal = _il.DeclareLocal(typeof(ScriptObject));
            _session.Builder.SetLocalSymInfo(enumLocal, enumDeclaration.Identifier.Value);
            _il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptObject_Ctor);
            _il.Emit(OpCodes.Stloc, enumLocal);

            for (var i = 0; i < enumDeclaration.Elements.Count; i++)
            {
                var element = enumDeclaration.Elements[i];
                _il.Emit(OpCodes.Ldloc, enumLocal);
                _session.Builder.LoadStringConstant(_il, element.Name.Value);
                _il.Emit(OpCodes.Ldc_R8, (double)element.Value);
                _il.Emit(OpCodes.Call, RuntimeMetadata.NumberValue_Of);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ldc_I4_1);
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_Define);
            }

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);
            _session.Builder.LoadStringConstant(_il, enumDeclaration.Identifier.Value);
            _il.Emit(OpCodes.Ldloc, enumLocal);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(_session.ForceModuleDefinitions ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptObject_InternalDefineDatum);
        }

        private void EmitExpressionOrNull(Expression expression)
        {
            if (expression == null)
            {
                _session.Builder.LoadNull(_il);
                return;
            }

            EmitExpression(expression);
        }

        private void EmitExpression(Expression expression)
        {
            switch (expression)
            {
                case GroupExpression group:
                    EmitExpressionOrNull(group.Expression);
                    return;
                case LiteralExpression literal:
                    EmitLiteral(literal);
                    return;
                case NameExpression name:
                    EmitName(name);
                    return;
                case BinaryExpression binary:
                    EmitBinary(binary);
                    return;
                case AssignmentExpression assignment:
                    EmitAssignment(assignment);
                    return;
                case CompoundExpression compound:
                    EmitCompound(compound);
                    return;
                case UnaryExpression unary:
                    EmitUnary(unary);
                    return;
                case IncludedExpression included:
                    EmitIncluded(included.Left, included.Right);
                    return;
                case InExpression inExpression:
                    EmitIncluded(inExpression.Left, inExpression.Right);
                    return;
                case GetPropertyExpression property:
                    EmitGetProperty(property);
                    return;
                case GetElementExpression element:
                    EmitGetElement(element);
                    return;
                case SetPropertyExpression property:
                    EmitSetProperty(property);
                    return;
                case SetElementExpression element:
                    EmitSetElement(element);
                    return;
                case ArrayLiteralExpression array:
                    EmitArrayLiteral(array);
                    return;
                case MapExpression map:
                    EmitMap(map);
                    return;
                case LambdaExpression lambda:
                    EmitLambda(lambda);
                    return;
                case NewExpression @new:
                    EmitNew(@new.Expression);
                    return;
                case FunctionCallExpression call:
                    EmitCall(call);
                    return;
                default:
                    throw new NotSupportedException("Module initializer expression " + expression.GetType().Name);
            }
        }

        private void EmitLiteral(LiteralExpression expression)
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
                    _session.Builder.LoadNull(_il);
                    return;
                default:
                    throw new NotSupportedException(expression.Token?.GetType().Name ?? "<null>");
            }
        }

        private void EmitName(NameExpression expression)
        {
            var name = expression.Identifier?.Value;
            if (StringComparer.Ordinal.Equals(name, "$state"))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_UserState);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
                return;
            }
            if (StringComparer.Ordinal.Equals(name, "global"))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
                return;
            }

            if (!string.IsNullOrEmpty(name) && _module.TryGetSymbol(name, out _))
            {
                EmitModulePropertyLoad(name);
                return;
            }

            EmitGlobalPropertyLoad(name);
        }

        private void EmitBinary(BinaryExpression expression)
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

        private bool TryEmitStringAddition(BinaryExpression expression)
        {
            if (expression.Operator != Operator.Add)
            {
                return false;
            }

            if (expression.Left is BinaryExpression leftBinary &&
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

        private static bool TryGetStringLiteral(Expression expression, out string value)
        {
            if (expression is LiteralExpression { Token: StringToken token })
            {
                value = token.Value;
                return true;
            }

            value = null;
            return false;
        }

        private void EmitLogical(BinaryExpression expression, bool branchWhenTrue)
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

        private void EmitAssignment(AssignmentExpression expression)
        {
            if (expression.Left is NameExpression name)
            {
                EmitExpression(expression.Right);
                _il.Emit(OpCodes.Dup);
                EmitStoreNameFromStack(name.Identifier.Value);
                return;
            }

            if (expression.Left is GetPropertyExpression property)
            {
                EmitSetProperty(new SetPropertyExpression(property.Object, property.Property, expression.Right));
                return;
            }

            if (expression.Left is GetElementExpression element)
            {
                EmitSetElement(new SetElementExpression(element.Object, element.Index, expression.Right));
                return;
            }

            throw new NotSupportedException("Module assignment target " + expression.Left?.GetType().Name);
        }

        private void EmitCompound(CompoundExpression expression)
        {
            if (expression.Left is NameExpression name)
            {
                EmitName(name);
                EmitExpression(expression.Right);
                _il.Emit(OpCodes.Call, GetBinaryMethod(expression.Operator.SimplerOperator));
                _il.Emit(OpCodes.Dup);
                EmitStoreNameFromStack(name.Identifier.Value);
                return;
            }

            if (expression.Left is GetElementExpression element && expression.Operator.SimplerOperator == Operator.Add)
            {
                EmitExpression(element.Object);
                EmitExpression(element.Index);
                EmitExpression(expression.Right);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_CompoundAddElementDatum);
                return;
            }

            throw new NotSupportedException("Module compound target " + expression.Left?.GetType().Name);
        }

        private void EmitUnary(UnaryExpression expression)
        {
            var incrementMethod = GetIncrementMethod(expression.Operator);
            if (incrementMethod == null)
            {
                EmitExpression(expression.Expression);
                _il.Emit(OpCodes.Call, GetUnaryMethod(expression.Operator));
                return;
            }

            if (expression.Expression is NameExpression name)
            {
                EmitStoreTargetObject(name.Identifier.Value);
                _session.Builder.LoadStringConstant(_il, name.Identifier.Value);
                _il.Emit(OpCodes.Call, GetPropertyMutationMethod(expression.Operator));
                return;
            }

            if (expression.Expression is GetElementExpression element)
            {
                EmitExpression(element.Object);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
                EmitExpression(element.Index);
                _il.Emit(OpCodes.Call, GetElementMutationMethod(expression.Operator));
                return;
            }

            if (expression.Expression is GetPropertyExpression property && TryGetStaticPropertyName(property, out var propertyName))
            {
                EmitExpression(property.Object);
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
                _session.Builder.LoadStringConstant(_il, propertyName);
                _il.Emit(OpCodes.Call, GetPropertyMutationMethod(expression.Operator));
                return;
            }

            throw new NotSupportedException("Module unary target " + expression.Expression?.GetType().Name);
        }

        private void EmitIncluded(Expression left, Expression right)
        {
            EmitExpression(right);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            EmitExpression(left);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_Included);
        }

        private void EmitGetProperty(GetPropertyExpression expression)
        {
            if (!TryGetStaticPropertyName(expression, out var name))
            {
                throw new NotSupportedException("Dynamic module property name");
            }

            EmitExpression(expression.Object);
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

        private void EmitSetProperty(SetPropertyExpression expression)
        {
            if (!TryGetStaticPropertyName(expression, out var name))
            {
                throw new NotSupportedException("Dynamic module property name");
            }

            EmitExpression(expression.Object);
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

        private void EmitGetElement(GetElementExpression expression)
        {
            EmitExpression(expression.Object);
            EmitExpression(expression.Index);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetElementDatum);
        }

        private void EmitSetElement(SetElementExpression expression)
        {
            EmitExpression(expression.Object);
            EmitExpression(expression.Index);
            EmitExpression(expression.Value);
            var valueLocal = DeclareTemp();
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, valueLocal);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_SetElementDatum);
            _il.Emit(OpCodes.Ldloc, valueLocal);
        }

        private void EmitArrayLiteral(ArrayLiteralExpression expression)
        {
            _il.Emit(OpCodes.Ldc_I4, expression.Length);
            _il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptArray_CtorCapacity);
            for (var i = 0; i < expression.Length; i++)
            {
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Ldc_I4, i);
                if (expression[i] is SpreadExpression spread)
                {
                    _il.Emit(OpCodes.Pop);
                    EmitExpression(spread.Expression);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_SpreadInto);
                }
                else
                {
                    EmitExpressionOrNull(expression[i] as Expression);
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_SetElementValue);
                }
            }
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
        }

        private void EmitMap(MapExpression expression)
        {
            _il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptObject_Ctor);
            for (var i = 0; i < expression.Length; i++)
            {
                _il.Emit(OpCodes.Dup);
                if (expression[i] is MapKeyValueExpression entry)
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _session.Builder.LoadStringConstant(_il, entry.Key.Value);
                    EmitExpressionOrNull(entry.Value);
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_SetPropertyDatum);
                    continue;
                }

                if (expression[i] is SpreadExpression spread)
                {
                    EmitExpression(spread.Expression);
                    _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
                    _il.Emit(OpCodes.Ldc_I4_0);
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_CopyPropertysFrom);
                    continue;
                }

                if (expression[i] is NameExpression name)
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _session.Builder.LoadStringConstant(_il, name.Identifier.Value);
                    EmitName(name);
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_SetPropertyDatum);
                    continue;
                }

                throw new NotSupportedException("Module map entry " + expression[i]?.GetType().Name);
            }
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
        }

        private void EmitLambda(LambdaExpression expression)
        {
            if (!GetFunctionsByDeclaration().TryGetValue(expression.Function, out var function) ||
                !ClosureMaterializer.CanMaterialize(function, requireName: false))
            {
                throw new NotSupportedException("Module lambda closure");
            }

            ClosureMaterializer.EmitClosure(_session, _il, function);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
        }

        private Dictionary<FunctionDeclaration, FunctionPlan> GetFunctionsByDeclaration()
        {
            if (_functionsByDeclaration != null)
            {
                return _functionsByDeclaration;
            }

            var map = new Dictionary<FunctionDeclaration, FunctionPlan>(_module.Functions.Count, ReferenceEqualityComparer.Instance);
            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                if (function.Declaration != null)
                {
                    map[function.Declaration] = function;
                }
            }

            _functionsByDeclaration = map;
            return map;
        }

        private void EmitNew(FunctionCallExpression call)
        {
            if (call == null)
            {
                throw new NotSupportedException("Empty constructor call");
            }

            if (HasSpread(call.Arguments) || call.Arguments.Count > 2)
            {
                EmitNewMany(call);
                return;
            }

            EmitExpression(call.Target);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Ldarg_0);
            for (var i = 0; i < call.Arguments.Count; i++)
            {
                EmitExpression(call.Arguments[i]);
            }
            _il.Emit(OpCodes.Call, GetNewMethod(call.Arguments.Count));
        }

        private void EmitCall(FunctionCallExpression call)
        {
            if (TryEmitDirectCall(call))
            {
                return;
            }

            if (call.Target is GetPropertyExpression property && TryGetStaticPropertyName(property, out var name))
            {
                EmitPropertyCall(call, property, name);
                return;
            }

            if (HasSpread(call.Arguments) || call.Arguments.Count > 7)
            {
                EmitRegularCallMany(call);
                return;
            }

            EmitExpression(call.Target);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Ldarg_0);
            for (var i = 0; i < call.Arguments.Count; i++)
            {
                EmitExpression(call.Arguments[i]);
            }
            _il.Emit(OpCodes.Call, GetInvokeMethod(call.Arguments.Count));
        }

        private bool TryEmitDirectCall(FunctionCallExpression call)
        {
            if (call.Target is not NameExpression target ||
                HasSpread(call.Arguments) ||
                !TryResolveDirectCallTarget(target, out var function))
            {
                return false;
            }

            EmitDirectCall(call, function);
            return true;
        }

        private bool TryResolveDirectCallTarget(NameExpression target, out FunctionPlan function)
        {
            function = null;
            var name = target.Identifier?.Value;
            if (string.IsNullOrEmpty(name) ||
                !_session.CompileSession.Capabilities.CanUseModuleDirectCall)
            {
                return false;
            }

            var functions = GetDirectFunctionsByName();
            return functions.TryGetValue(name, out function) &&
                CanUseFastDirectSignature(function);
        }

        private Dictionary<string, FunctionPlan> GetDirectFunctionsByName()
        {
            if (_directFunctionsByName != null)
            {
                return _directFunctionsByName;
            }

            var map = new Dictionary<string, FunctionPlan>(StringComparer.Ordinal);
            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                if (!string.IsNullOrEmpty(function.Name) &&
                    function.IsDirectCallCandidate)
                {
                    map[function.Name] = function;
                }
            }

            _directFunctionsByName = map;
            return map;
        }

        private void EmitDirectCall(FunctionCallExpression call, FunctionPlan target)
        {
            var arity = GetFastArity(target.CallConvention);
            var argumentLocals = EmitDirectCallArguments(call.Arguments, arity);
            var directContext = _il.DeclareLocal(typeof(ScriptContext));
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, target.Name);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_EnterDirect);
            _il.Emit(OpCodes.Stloc, directContext);

            _il.Emit(OpCodes.Ldloc, directContext);
            for (var i = 0; i < arity; i++)
            {
                if (i < argumentLocals.Length)
                {
                    _il.Emit(OpCodes.Ldloc, argumentLocals[i]);
                }
                else
                {
                    _session.Builder.LoadNull(_il);
                }
            }

            _il.Emit(OpCodes.Call, target.Method);
            EmitLeaveDirect(directContext);
        }

        private LocalBuilder[] EmitDirectCallArguments(IReadOnlyList<Expression> arguments, int arity)
        {
            if (arguments.Count == 0 || arity == 0)
            {
                for (var i = 0; i < arguments.Count; i++)
                {
                    EmitExpression(arguments[i]);
                    _il.Emit(OpCodes.Pop);
                }

                return Array.Empty<LocalBuilder>();
            }

            var count = Math.Min(arguments.Count, arity);
            var locals = new LocalBuilder[count];
            for (var i = 0; i < arguments.Count; i++)
            {
                EmitExpression(arguments[i]);
                if (i < count)
                {
                    var local = DeclareTemp();
                    _il.Emit(OpCodes.Stloc, local);
                    locals[i] = local;
                }
                else
                {
                    _il.Emit(OpCodes.Pop);
                }
            }

            return locals;
        }

        private void EmitLeaveDirect(LocalBuilder directContext)
        {
            var result = DeclareTemp();
            _il.Emit(OpCodes.Stloc, result);
            _il.Emit(OpCodes.Ldloc, directContext);
            _il.Emit(OpCodes.Ldloc, result);
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_LeaveDirect);
        }

        private void EmitPropertyCall(FunctionCallExpression call, GetPropertyExpression property, string name)
        {
            if (HasSpread(call.Arguments) || call.Arguments.Count > 7)
            {
                EmitPropertyCallMany(call, property, name);
                return;
            }

            EmitExpression(property.Object);
            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            for (var i = 0; i < call.Arguments.Count; i++)
            {
                EmitExpression(call.Arguments[i]);
            }
            _il.Emit(OpCodes.Call, GetInvokePropertyMethod(call.Arguments.Count));
        }

        private void EmitNewMany(FunctionCallExpression call)
        {
            var typeLocal = _il.DeclareLocal(typeof(ScriptObject));
            var argsLocal = _il.DeclareLocal(typeof(ScriptDatum[]));

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

        private void EmitRegularCallMany(FunctionCallExpression call)
        {
            var functionLocal = _il.DeclareLocal(typeof(ScriptObject));
            var argsLocal = _il.DeclareLocal(typeof(ScriptDatum[]));

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

        private void EmitPropertyCallMany(FunctionCallExpression call, GetPropertyExpression property, string name)
        {
            var receiverLocal = _il.DeclareLocal(typeof(ScriptObject));
            var argsLocal = _il.DeclareLocal(typeof(ScriptDatum[]));

            EmitExpression(property.Object);
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

        private LocalBuilder EmitArgumentsToBuffer(IReadOnlyList<Expression> arguments, LocalBuilder argsLocal)
        {
            var countLocal = _il.DeclareLocal(typeof(int));
            _il.Emit(OpCodes.Ldc_I4, Math.Max(arguments.Count, 1));
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_RentArguments);
            _il.Emit(OpCodes.Stloc, argsLocal);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Stloc, countLocal);

            for (var i = 0; i < arguments.Count; i++)
            {
                _il.Emit(OpCodes.Ldloc, argsLocal);
                _il.Emit(OpCodes.Ldloca, countLocal);
                if (arguments[i] is SpreadExpression spread)
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

        private void EmitModulePropertyLoad(string name)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_GetPropertyDatum);
        }

        private void EmitGlobalPropertyLoad(string name)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_GetPropertyDatum);
        }

        private void EmitStoreNameFromStack(string name)
        {
            var valueLocal = DeclareTemp();
            _il.Emit(OpCodes.Stloc, valueLocal);
            EmitStoreTargetObject(name);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Ldloc, valueLocal);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_SetPropertyDatum);
        }

        private void EmitStoreTargetObject(string name)
        {
            _il.Emit(OpCodes.Ldarg_0);
            var target = _module.TryGetSymbol(name, out _)
                ? RuntimeMetadata.CILContext_Module
                : RuntimeMetadata.CILContext_Global;
            _il.Emit(OpCodes.Ldfld, target);
        }

        private LocalBuilder DeclareTemp()
        {
            return _il.DeclareLocal(typeof(ScriptDatum));
        }

        private static bool HasSpread(IReadOnlyList<Expression> expressions)
        {
            for (var i = 0; i < expressions.Count; i++)
            {
                if (expressions[i] is SpreadExpression)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetStaticPropertyName(GetPropertyExpression property, out string name)
        {
            if (property.Property is NameExpression propertyName &&
                !string.IsNullOrEmpty(propertyName.Identifier?.Value))
            {
                name = propertyName.Identifier.Value;
                return true;
            }

            name = null;
            return false;
        }

        private static bool TryGetStaticPropertyName(SetPropertyExpression property, out string name)
        {
            if (property.Property is NameExpression propertyName &&
                !string.IsNullOrEmpty(propertyName.Identifier?.Value))
            {
                name = propertyName.Identifier.Value;
                return true;
            }

            name = null;
            return false;
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

        private static MethodInfo GetElementMutationMethod(Operator op)
        {
            if (op == Operator.PreIncrement) return RuntimeMetadata.CILHelper_IncrementElementPrefix;
            if (op == Operator.PostIncrement) return RuntimeMetadata.CILHelper_IncrementElementPostfix;
            if (op == Operator.PreDecrement) return RuntimeMetadata.CILHelper_DecrementElementPrefix;
            if (op == Operator.PostDecrement) return RuntimeMetadata.CILHelper_DecrementElementPostfix;
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

        private static bool CanUseFastDirectSignature(FunctionPlan function)
        {
            return function != null &&
                function.Method != null &&
                function.IsDirectCallCandidate &&
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
    }
}
