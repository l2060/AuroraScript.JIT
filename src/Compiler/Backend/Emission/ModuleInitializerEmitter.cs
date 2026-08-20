using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Analysis;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Code;
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
        private bool _hasArgumentBufferCleanup;
        private List<(LocalBuilder Arguments, LocalBuilder Count)> _argumentBuffers;

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
            _hasArgumentBufferCleanup = PooledArgumentCallDetector.Contains(_module.Declaration);
            if (_hasArgumentBufferCleanup)
            {
                _argumentBuffers = new List<(LocalBuilder Arguments, LocalBuilder Count)>();
                _il.BeginExceptionBlock();
            }
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

            for (var i = 0; i < _module.Declaration.Statements.Count; i++)
            {
                EmitModuleStatement(_module.Declaration.Statements[i]);
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

            if (_hasArgumentBufferCleanup)
            {
                _il.BeginFinallyBlock();
                for (var i = 0; i < _argumentBuffers.Count; i++)
                {
                    _il.Emit(OpCodes.Ldloc, _argumentBuffers[i].Arguments);
                    _il.Emit(OpCodes.Ldloc, _argumentBuffers[i].Count);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ReturnArguments);
                }
                _il.EndExceptionBlock();
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
            il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextModule);
            _session.Builder.LoadStringConstant(il, function.Name);
            ClosureMaterializer.EmitClosure(_session, il, function);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Callvirt, _session.ForceModuleDefinitions
                ? TypedRuntimeMetadata.ScriptObjectPatchObject
                : TypedRuntimeMetadata.ScriptObjectDefineObject);
        }

        private void EmitImportAlias(ImportDeclaration import)
        {
            if (import.Name == null || import.ModuleName == null)
            {
                return;
            }

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextModule);
            _session.Builder.LoadStringConstant(_il, import.Name.Value);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextGlobal);
            _session.Builder.LoadStringConstant(_il, import.ModuleName);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptGlobalGetModule);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptObjectDefineObject);
        }

        private void EmitInclude(ImportDeclaration import)
        {
            if (import.ModuleName == null)
            {
                return;
            }

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextModule);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextGlobal);
            _session.Builder.LoadStringConstant(_il, import.ModuleName);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptGlobalGetModule);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptObjectCopyEnumerableProperties);
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
                    if (variable.IsDeclare)
                    {
                        return;
                    }
                    MarkSequencePoint(variable);
                    EmitVariableDeclaration(variable);
                    return;
                case EnumDeclaration enumDeclaration:
                    MarkSequencePoint(enumDeclaration);
                    EmitEnum(enumDeclaration);
                    return;
                case ExpressionStatement expressionStatement:
                    MarkSequencePoint(expressionStatement);
                    EmitExpressionDiscarded(expressionStatement.Expression);
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
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextModule);
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
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ScriptObjectInternalDefineDatum);
        }

        private void EmitEnum(EnumDeclaration enumDeclaration)
        {
            if (enumDeclaration.Identifier == null)
            {
                return;
            }

            var enumLocal = _il.DeclareLocal(typeof(ScriptObject));
            _session.Builder.SetLocalSymInfo(enumLocal, enumDeclaration.Identifier.Value);
            _il.Emit(OpCodes.Newobj, TypedRuntimeMetadata.ScriptObjectConstructor);
            _il.Emit(OpCodes.Stloc, enumLocal);

            for (var i = 0; i < enumDeclaration.Elements.Count; i++)
            {
                var element = enumDeclaration.Elements[i];
                _il.Emit(OpCodes.Ldloc, enumLocal);
                _session.Builder.LoadStringConstant(_il, element.Name.Value);
                _il.Emit(OpCodes.Ldc_R8, (double)element.Value);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromNumber);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ldc_I4_1);
                _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptObjectDefineDatum);
            }

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextModule);
            _session.Builder.LoadStringConstant(_il, enumDeclaration.Identifier.Value);
            _il.Emit(OpCodes.Ldloc, enumLocal);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(_session.ForceModuleDefinitions ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ScriptObjectInternalDefineDatum);
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

        private void EmitExpressionDiscarded(Expression expression)
        {
            if (expression == null)
            {
                return;
            }

            if (expression is GroupExpression group)
            {
                EmitExpressionDiscarded(group.Expression);
                return;
            }

            if (expression is UnaryExpression unary && IsMutation(unary.Operator))
            {
                EmitUnaryDiscarded(unary);
                return;
            }

            switch (expression)
            {
                case AssignmentExpression assignment:
                    EmitAssignmentDiscarded(assignment);
                    return;
                case CompoundExpression compound:
                    EmitCompoundDiscarded(compound);
                    return;
                case SetPropertyExpression property:
                    EmitSetPropertyDiscarded(property);
                    return;
                case SetElementExpression element:
                    EmitSetElementDiscarded(element);
                    return;
            }

            EmitExpression(expression);
            _il.Emit(OpCodes.Pop);
        }

        private void EmitExpression(Expression expression)
        {
            switch (expression)
            {
                case TypedDocumentExpression tdoc:
                    EmitTypedDocument(tdoc);
                    return;
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
                case TemplateStringExpression template:
                    EmitTemplateString(template);
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

        private void EmitCondition(Expression expression)
        {
            if (TryEmitCondition(expression))
            {
                return;
            }

            EmitExpressionOrNull(expression);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToBooleanDatum);
        }

        private void EmitLiteral(LiteralExpression expression)
        {
            switch (expression.Token)
            {
                case NumberToken number:
                    if (_session.Builder.LoadNumber(_il, number.NumberValue) == LoadState.Constant)
                    {
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromNumber);
                    }
                    return;
                case StringToken stringToken:
                    if (_session.Builder.LoadString(_il, stringToken.Value) == LoadState.Constant)
                    {
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromString);
                    }
                    return;
                case RegexToken regex:
                    _session.Builder.LoadStringConstant(_il, regex.Pattern);
                    _session.Builder.LoadStringConstant(_il, regex.Flags);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ResolveRegex);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
                    return;
                case BooleanToken boolean:
                    if (_session.Builder.LoadBoolean(_il, boolean.BoolValue) == LoadState.Constant)
                    {
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromBoolean);
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
                _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextUserState);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
                return;
            }
            if (StringComparer.Ordinal.Equals(name, "global"))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextGlobal);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
                return;
            }

            if (IsDefinedModuleSymbol(name))
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

        private bool TryEmitCondition(Expression expression)
        {
            switch (expression)
            {
                case null:
                    _il.Emit(OpCodes.Ldc_I4_0);
                    return true;
                case GroupExpression group:
                    return TryEmitCondition(group.Expression);
                case LiteralExpression literal:
                    return TryEmitLiteralCondition(literal);
                case BinaryExpression binary:
                    return TryEmitBinaryCondition(binary);
                case UnaryExpression unary when unary.Operator == Operator.LogicalNot:
                    EmitCondition(unary.Expression);
                    _il.Emit(OpCodes.Ldc_I4_0);
                    _il.Emit(OpCodes.Ceq);
                    return true;
                case IncludedExpression included:
                    EmitExpression(included.Right);
                    EmitExpression(included.Left);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.Includes);
                    return true;
                case InExpression inExpression:
                    EmitExpression(inExpression.Right);
                    EmitExpression(inExpression.Left);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.Includes);
                    return true;
                default:
                    return false;
            }
        }

        private bool TryEmitLiteralCondition(LiteralExpression expression)
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

        private bool TryEmitBinaryCondition(BinaryExpression expression)
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
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.AddStringMiddle);
                return true;
            }

            if (TryGetStringLiteral(expression.Right, out var right))
            {
                EmitExpression(expression.Left);
                _session.Builder.LoadStringConstant(_il, right);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.AddStringRight);
                return true;
            }

            if (TryGetStringLiteral(expression.Left, out var left))
            {
                _session.Builder.LoadStringConstant(_il, left);
                EmitExpression(expression.Right);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.AddStringLeft);
                return true;
            }

            return false;
        }

        private void EmitTemplateString(TemplateStringExpression expression)
        {
            var elementCount = CountTemplateStringElements(expression);
            if (elementCount == 0)
            {
                _session.Builder.LoadStringConstant(_il, string.Empty);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromString);
                return;
            }

            if (elementCount <= 4)
            {
                EmitTemplateStringConcat(expression, elementCount);
                return;
            }

            EmitTemplateStringBuilder(expression);
        }

        private static int CountTemplateStringElements(TemplateStringExpression expression)
        {
            var count = 0;
            for (var i = 0; i < expression.PartCount; i++)
            {
                var part = expression.Parts[i];
                if (!part.IsLiteral || part.Literal.Length != 0)
                {
                    count++;
                }
            }

            return count;
        }

        private void EmitTemplateStringConcat(TemplateStringExpression expression, int elementCount)
        {
            for (var i = 0; i < expression.PartCount; i++)
            {
                EmitTemplateStringElement(expression.Parts[i]);
            }

            switch (elementCount)
            {
                case 1:
                    break;
                case 2:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.StringConcat2);
                    break;
                case 3:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.StringConcat3);
                    break;
                case 4:
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.StringConcat4);
                    break;
                default:
                    throw new NotSupportedException("Template string concat element count " + elementCount);
            }

            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromString);
        }

        private void EmitTemplateStringElement(TemplateStringPart part)
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
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToString);
        }

        private void EmitTemplateStringBuilder(TemplateStringExpression expression)
        {
            var dynamicStrings = new LocalBuilder[expression.PartCount];
            var literalLength = 0;
            for (var i = 0; i < expression.PartCount; i++)
            {
                var part = expression.Parts[i];
                if (part.IsLiteral)
                {
                    literalLength += part.Literal.Length;
                    continue;
                }

                EmitExpression(part.Expression);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToString);
                var local = DeclareLocal(typeof(string));
                _il.Emit(OpCodes.Stloc, local);
                dynamicStrings[i] = local;
            }

            _il.Emit(OpCodes.Ldc_I4, literalLength);
            for (var i = 0; i < dynamicStrings.Length; i++)
            {
                var local = dynamicStrings[i];
                if (local == null)
                {
                    continue;
                }

                _il.Emit(OpCodes.Ldloc, local);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.StringLength);
                _il.Emit(OpCodes.Add);
            }

            _il.Emit(OpCodes.Newobj, TypedRuntimeMetadata.StringBuilderCapacity);
            for (var i = 0; i < expression.PartCount; i++)
            {
                var part = expression.Parts[i];
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

                _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.StringBuilderAppend);
            }

            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.StringBuilderToString);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromString);
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
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToBooleanDatum);
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

        private void EmitAssignmentDiscarded(AssignmentExpression expression)
        {
            if (expression.Left is NameExpression name)
            {
                EmitExpression(expression.Right);
                EmitStoreNameFromStack(name.Identifier.Value);
                return;
            }

            if (expression.Left is GetPropertyExpression property)
            {
                EmitSetPropertyDiscarded(new SetPropertyExpression(property.Object, property.Property, expression.Right));
                return;
            }

            if (expression.Left is GetElementExpression element)
            {
                EmitSetElementDiscarded(new SetElementExpression(element.Object, element.Index, expression.Right));
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
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.CompoundAddElement);
                return;
            }

            throw new NotSupportedException("Module compound target " + expression.Left?.GetType().Name);
        }

        private void EmitCompoundDiscarded(CompoundExpression expression)
        {
            if (expression.Left is NameExpression name)
            {
                EmitName(name);
                EmitExpression(expression.Right);
                _il.Emit(OpCodes.Call, GetBinaryMethod(expression.Operator.SimplerOperator));
                EmitStoreNameFromStack(name.Identifier.Value);
                return;
            }

            if (expression.Left is GetElementExpression element && expression.Operator.SimplerOperator == Operator.Add)
            {
                EmitExpression(element.Object);
                EmitExpression(element.Index);
                EmitExpression(expression.Right);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.CompoundAddElement);
                _il.Emit(OpCodes.Pop);
                return;
            }

            throw new NotSupportedException("Module compound target " + expression.Left?.GetType().Name);
        }

        private void EmitUnary(UnaryExpression expression)
        {
            if (!IsMutation(expression.Operator))
            {
                EmitExpression(expression.Expression);
                _il.Emit(OpCodes.Call, GetUnaryMethod(expression.Operator));
                return;
            }

            if (expression.Expression is NameExpression name)
            {
                EmitStoreTargetObject(name.Identifier.Value);
                _il.Emit(OpCodes.Ldarg_0);
                _session.Builder.LoadStringConstant(_il, name.Identifier.Value);
                EmitMutationArguments(expression.Operator);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ChangeObjectProperty);
                return;
            }

            if (expression.Expression is GetElementExpression element)
            {
                EmitExpression(element.Object);
                EmitExpression(element.Index);
                EmitMutationArguments(expression.Operator);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ChangeElement);
                return;
            }

            if (expression.Expression is GetPropertyExpression property && TryGetStaticPropertyName(property, out var propertyName))
            {
                EmitExpression(property.Object);
                _il.Emit(OpCodes.Ldarg_0);
                _session.Builder.LoadStringConstant(_il, propertyName);
                EmitMutationArguments(expression.Operator);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ChangeDatumProperty);
                return;
            }

            throw new NotSupportedException("Module unary target " + expression.Expression?.GetType().Name);
        }

        private void EmitUnaryDiscarded(UnaryExpression expression)
        {
            EmitUnary(expression);
            _il.Emit(OpCodes.Pop);
        }

        private void EmitMutationArguments(Operator op)
        {
            _il.Emit(OpCodes.Ldc_R8,
                op == Operator.PreIncrement || op == Operator.PostIncrement ? 1d : -1d);
            _il.Emit(op == Operator.PostIncrement || op == Operator.PostDecrement
                ? OpCodes.Ldc_I4_1
                : OpCodes.Ldc_I4_0);
        }

        private void EmitIncluded(Expression left, Expression right)
        {
            EmitExpression(right);
            EmitExpression(left);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.Includes);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromBoolean);
        }

        private void EmitGetProperty(GetPropertyExpression expression)
        {
            if (!TryGetStaticPropertyName(expression, out var name))
            {
                throw new NotSupportedException("Dynamic module property name");
            }

            EmitExpression(expression.Object);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetProperty);
        }

        private void EmitSetProperty(SetPropertyExpression expression)
        {
            if (!TryGetStaticPropertyName(expression, out var name))
            {
                throw new NotSupportedException("Dynamic module property name");
            }

            EmitExpression(expression.Object);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            EmitExpression(expression.Value);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.SetProperty);
        }

        private void EmitSetPropertyDiscarded(SetPropertyExpression expression)
        {
            if (!TryGetStaticPropertyName(expression, out var name))
            {
                throw new NotSupportedException("Dynamic module property name");
            }

            EmitExpression(expression.Object);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            EmitExpression(expression.Value);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.SetProperty);
            _il.Emit(OpCodes.Pop);
        }

        private void EmitGetElement(GetElementExpression expression)
        {
            if (TryEmitGetElementFastPath(expression))
            {
                return;
            }

            EmitExpression(expression.Object);
            EmitExpression(expression.Index);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetElement);
        }

        private bool TryEmitGetElementFastPath(GetElementExpression expression)
        {
            if (TryGetNumberLiteral(expression.Index, out var index))
            {
                EmitExpression(expression.Object);
                EmitRawNumber(index);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetElementNumber);
                return true;
            }

            return false;
        }

        private static bool TryGetNumberLiteral(Expression expression, out double value)
        {
            if (expression is LiteralExpression { Token: NumberToken token })
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

        private void EmitSetElement(SetElementExpression expression)
        {
            EmitExpression(expression.Object);
            EmitExpression(expression.Index);
            EmitExpression(expression.Value);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.SetElement);
        }

        private void EmitSetElementDiscarded(SetElementExpression expression)
        {
            EmitExpression(expression.Object);
            EmitExpression(expression.Index);
            EmitExpression(expression.Value);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.SetElement);
            _il.Emit(OpCodes.Pop);
        }

        private void EmitArrayLiteral(ArrayLiteralExpression expression)
        {
            var hasSpread = HasSpread(expression.Elements);
            _il.Emit(OpCodes.Ldc_I4, hasSpread ? 0 : expression.Elements.Count);
            _il.Emit(OpCodes.Newobj, TypedRuntimeMetadata.ScriptArrayCapacity);
            for (var i = 0; i < expression.Elements.Count; i++)
            {
                _il.Emit(OpCodes.Dup);
                var element = expression.Elements[i];
                if (element is SpreadExpression spread)
                {
                    EmitExpression(spread.Expression);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.SpreadIntoArray);
                }
                else if (hasSpread)
                {
                    EmitExpressionOrNull(element);
                    _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArrayPush);
                }
                else
                {
                    _il.Emit(OpCodes.Ldc_I4, i);
                    EmitExpressionOrNull(element);
                    _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptArraySetElement);
                }
            }
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
        }

        private void EmitMap(MapExpression expression)
        {
            _il.Emit(OpCodes.Newobj, TypedRuntimeMetadata.ScriptObjectConstructor);
            for (var i = 0; i < expression.Entries.Count; i++)
            {
                _il.Emit(OpCodes.Dup);
                var mapEntry = expression.Entries[i];
                if (mapEntry is MapKeyValueExpression entry)
                {
                    if (entry.ReadOnly)
                    {
                        _session.Builder.LoadStringConstant(_il, entry.Key.Value);
                        EmitExpressionOrNull(entry.Value);
                        _il.Emit(OpCodes.Ldc_I4_0);
                        _il.Emit(OpCodes.Ldc_I4_1);
                        _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptObjectDefineDatum);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Ldarg_0);
                        _session.Builder.LoadStringConstant(_il, entry.Key.Value);
                        EmitExpressionOrNull(entry.Value);
                        _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptObjectSetProperty);
                    }
                    continue;
                }

                if (mapEntry is SpreadExpression spread)
                {
                    EmitExpression(spread.Expression);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.CopyProperties);
                    continue;
                }

                if (mapEntry is NameExpression name)
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _session.Builder.LoadStringConstant(_il, name.Identifier.Value);
                    EmitName(name);
                    _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptObjectSetProperty);
                    continue;
                }

                throw new NotSupportedException("Module map entry " + mapEntry?.GetType().Name);
            }
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
        }

        private void EmitTypedDocument(TypedDocumentExpression expression)
        {
            switch (expression.TypeName)
            {
                case null:
                case "":
                case "String":
                case "Number":
                case "Boolean":
                case "Object":
                case "Array":
                    EmitExpression(expression.Value);
                    return;
                case "Null":
                    _session.Builder.LoadNull(_il);
                    return;
                case "Int32Array":
                    EmitTypedPackedDocument(expression.Value, typeof(int), TypedRuntimeMetadata.ScriptInt32ArrayConstructor, TypedRuntimeMetadata.ScriptInt32ArrayItems, OpCodes.Stelem_I4);
                    return;
                case "Int8Array":
                    EmitTypedPackedDocument(expression.Value, typeof(sbyte), TypedRuntimeMetadata.ScriptInt8ArrayConstructor, TypedRuntimeMetadata.ScriptInt8ArrayItems, OpCodes.Stelem_I1);
                    return;
                case "Float64Array":
                    EmitTypedPackedDocument(expression.Value, typeof(double), TypedRuntimeMetadata.ScriptFloat64ArrayConstructor, TypedRuntimeMetadata.ScriptFloat64ArrayItems, OpCodes.Stelem_R8);
                    return;
                case "BooleanArray":
                    EmitTypedPackedDocument(expression.Value, typeof(bool), TypedRuntimeMetadata.ScriptBooleanArrayConstructor, TypedRuntimeMetadata.ScriptBooleanArrayItems, OpCodes.Stelem_I1);
                    return;
                case "StringBuffer":
                case "Date":
                case "Path":
                    EmitTypedGlobalConstructor(expression.TypeName, expression.Value);
                    return;
                case "Regex":
                    EmitTypedRegex(expression.Value);
                    return;
                case "HashMap":
                    EmitTypedHashMap(expression.Value);
                    return;
                default:
                    throw new NotSupportedException("Module TDoc type " + expression.TypeName);
            }
        }

        private void EmitTypedGlobalConstructor(string typeName, Expression value)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, typeName);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetGlobal);
            _il.Emit(OpCodes.Ldarg_0);
            EmitExpression(value);
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
            EmitExpression(pattern);
            if (flags == null) _session.Builder.LoadNull(_il);
            else EmitExpression(flags);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.New2);
        }

        private void EmitTypedHashMap(Expression value)
        {
            if (value is not ArrayLiteralExpression entries)
            {
                throw new NotSupportedException("TDoc HashMap requires an array value.");
            }
            _il.Emit(OpCodes.Ldc_I4, entries.Elements.Count);
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
                EmitExpression(pairArray.Elements[0]);
                EmitExpression(pairArray.Elements[1]);
                _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptHashMapPut);
            }
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
        }

        private void EmitTypedPackedDocument(
            Expression value,
            Type elementType,
            ConstructorInfo constructor,
            FieldInfo itemsField,
            OpCode storeOpcode)
        {
            if (value is not ArrayLiteralExpression array)
            {
                throw new NotSupportedException("TDoc packed array requires an array value.");
            }

            _il.Emit(OpCodes.Ldc_I4, array.Elements.Count);
            _il.Emit(OpCodes.Newobj, constructor);
            for (var i = 0; i < array.Elements.Count; i++)
            {
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Ldfld, itemsField);
                _il.Emit(OpCodes.Ldc_I4, i);
                EmitExpression(array.Elements[i]);
                if (elementType == typeof(double))
                {
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToArithmeticNumber);
                }
                else if (elementType == typeof(bool))
                {
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToBooleanDatum);
                }
                else
                {
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ToArithmeticNumber);
                    _il.Emit(OpCodes.Conv_I4);
                    if (elementType == typeof(sbyte)) _il.Emit(OpCodes.Conv_I1);
                }
                _il.Emit(storeOpcode);
            }
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
        }

        private void EmitLambda(LambdaExpression expression)
        {
            if (!GetFunctionsByDeclaration().TryGetValue(expression.Function, out var function) ||
                !ClosureMaterializer.CanMaterialize(function, requireName: false))
            {
                throw new NotSupportedException("Module lambda closure");
            }

            ClosureMaterializer.EmitClosure(_session, _il, function);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
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
            var argumentLocals = EmitDirectCallArguments(call.Arguments, arity, out var deferredArguments);
            _il.Emit(OpCodes.Ldarg_0);
            for (var i = 0; i < arity; i++)
            {
                if (i >= argumentLocals.Length)
                {
                    _session.Builder.LoadNull(_il);
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

            _il.Emit(OpCodes.Call, target.DirectEntryMethod);
        }

        private LocalBuilder[] EmitDirectCallArguments(IReadOnlyList<Expression> arguments, int arity, out bool[] deferredArguments)
        {
            if (arguments.Count == 0 || arity == 0)
            {
                for (var i = 0; i < arguments.Count; i++)
                {
                    EmitExpressionDiscarded(arguments[i]);
                }

                deferredArguments = Array.Empty<bool>();
                return Array.Empty<LocalBuilder>();
            }

            var count = Math.Min(arguments.Count, arity);
            var locals = new LocalBuilder[count];
            deferredArguments = new bool[count];
            var lastPreEvaluated = GetLastDirectCallPreEvaluationIndex(arguments, arity);
            for (var i = 0; i < arguments.Count; i++)
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

        private static int GetLastDirectCallPreEvaluationIndex(IReadOnlyList<Expression> arguments, int arity)
        {
            var last = -1;
            for (var i = 0; i < arguments.Count; i++)
            {
                if (i >= arity || !CanDeferDirectCallArgument(arguments[i]))
                {
                    last = i;
                }
            }

            return last;
        }

        private static bool CanDeferDirectCallArgument(Expression expression)
        {
            return expression switch
            {
                GroupExpression group => CanDeferDirectCallArgument(group.Expression),
                LiteralExpression literal => literal.Token is NumberToken or BooleanToken or NullToken,
                _ => false
            };
        }

        private void EmitPropertyCall(FunctionCallExpression call, GetPropertyExpression property, string name)
        {
            if (HasSpread(call.Arguments) || call.Arguments.Count > 7)
            {
                EmitPropertyCallMany(call, property, name);
                return;
            }

            EmitExpression(property.Object);
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
            var typeLocal = _il.DeclareLocal(typeof(ScriptDatum));
            var argsLocal = _il.DeclareLocal(typeof(ScriptDatum[]));
            var countLocal = _il.DeclareLocal(typeof(int));
            var resultLocal = _il.DeclareLocal(typeof(ScriptDatum));

            EmitExpression(call.Target);
            _il.Emit(OpCodes.Stloc, typeLocal);
            InitializeArgumentBuffer(argsLocal, countLocal);
            EmitArgumentsToBuffer(call.Arguments, argsLocal, countLocal);

            _il.Emit(OpCodes.Ldloc, typeLocal);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldloc, argsLocal);
            _il.Emit(OpCodes.Ldloc, countLocal);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.NewMany);
            _il.Emit(OpCodes.Stloc, resultLocal);
            ReleaseArgumentBuffer(argsLocal, countLocal);
            _il.Emit(OpCodes.Ldloc, resultLocal);
        }

        private void EmitRegularCallMany(FunctionCallExpression call)
        {
            var functionLocal = _il.DeclareLocal(typeof(ScriptDatum));
            var argsLocal = _il.DeclareLocal(typeof(ScriptDatum[]));
            var countLocal = _il.DeclareLocal(typeof(int));
            var resultLocal = _il.DeclareLocal(typeof(ScriptDatum));

            EmitExpression(call.Target);
            _il.Emit(OpCodes.Stloc, functionLocal);
            InitializeArgumentBuffer(argsLocal, countLocal);
            EmitArgumentsToBuffer(call.Arguments, argsLocal, countLocal);

            _il.Emit(OpCodes.Ldloc, functionLocal);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldloc, argsLocal);
            _il.Emit(OpCodes.Ldloc, countLocal);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.InvokeMany);
            _il.Emit(OpCodes.Stloc, resultLocal);
            ReleaseArgumentBuffer(argsLocal, countLocal);
            _il.Emit(OpCodes.Ldloc, resultLocal);
        }

        private void EmitPropertyCallMany(FunctionCallExpression call, GetPropertyExpression property, string name)
        {
            var receiverLocal = _il.DeclareLocal(typeof(ScriptDatum));
            var argsLocal = _il.DeclareLocal(typeof(ScriptDatum[]));
            var countLocal = _il.DeclareLocal(typeof(int));
            var resultLocal = _il.DeclareLocal(typeof(ScriptDatum));

            EmitExpression(property.Object);
            _il.Emit(OpCodes.Stloc, receiverLocal);
            InitializeArgumentBuffer(argsLocal, countLocal);
            EmitArgumentsToBuffer(call.Arguments, argsLocal, countLocal);

            _il.Emit(OpCodes.Ldloc, receiverLocal);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Ldloc, argsLocal);
            _il.Emit(OpCodes.Ldloc, countLocal);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.InvokePropertyMany);
            _il.Emit(OpCodes.Stloc, resultLocal);
            ReleaseArgumentBuffer(argsLocal, countLocal);
            _il.Emit(OpCodes.Ldloc, resultLocal);
        }

        private void EmitArgumentsToBuffer(
            IReadOnlyList<Expression> arguments,
            LocalBuilder argsLocal,
            LocalBuilder countLocal)
        {
            _il.Emit(OpCodes.Ldc_I4, Math.Max(arguments.Count, 1));
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.RentArguments);
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
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.AppendSpread);
                }
                else
                {
                    EmitExpression(arguments[i]);
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.AppendArgument);
                }
                _il.Emit(OpCodes.Stloc, argsLocal);
            }
        }

        private void InitializeArgumentBuffer(LocalBuilder argsLocal, LocalBuilder countLocal)
        {
            if (!_hasArgumentBufferCleanup || _argumentBuffers == null)
            {
                throw new InvalidOperationException("Missing module-level argument-buffer cleanup region.");
            }
            _argumentBuffers.Add((argsLocal, countLocal));

            // A module-level script catch may swallow a failure from this call site.
            // Drain the buffer retained by that failed attempt before a loop or later
            // execution overwrites the local; the module cleanup handles final exit.
            _il.Emit(OpCodes.Ldloc, argsLocal);
            _il.Emit(OpCodes.Ldloc, countLocal);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ReturnArguments);
            _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Stloc, argsLocal);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Stloc, countLocal);
        }

        private void ReleaseArgumentBuffer(LocalBuilder argsLocal, LocalBuilder countLocal)
        {
            _il.Emit(OpCodes.Ldloc, argsLocal);
            _il.Emit(OpCodes.Ldloc, countLocal);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ReturnArguments);
            _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Stloc, argsLocal);
        }

        private void EmitModulePropertyLoad(string name)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetModule);
        }

        private void EmitGlobalPropertyLoad(string name)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetGlobal);
        }

        private void EmitStoreNameFromStack(string name)
        {
            var valueLocal = DeclareTemp();
            _il.Emit(OpCodes.Stloc, valueLocal);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Ldloc, valueLocal);
            _il.Emit(OpCodes.Call, IsDefinedModuleSymbol(name)
                ? TypedRuntimeMetadata.SetModule
                : TypedRuntimeMetadata.SetGlobal);
            _il.Emit(OpCodes.Pop);
        }

        private void EmitStoreTargetObject(string name)
        {
            _il.Emit(OpCodes.Ldarg_0);
            var target = IsDefinedModuleSymbol(name)
                ? TypedRuntimeMetadata.ContextModule
                : TypedRuntimeMetadata.ContextGlobal;
            _il.Emit(OpCodes.Ldfld, target);
        }

        private bool IsDefinedModuleSymbol(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                _module.TryGetSymbol(name, out var symbolId) &&
                !_session.CompileSession.Symbols[symbolId].HasFlag(BackendSymbolFlags.DeclaredOnly);
        }

        private LocalBuilder DeclareTemp()
        {
            return _il.DeclareLocal(typeof(ScriptDatum));
        }

        private LocalBuilder DeclareLocal(Type type)
        {
            return _il.DeclareLocal(type);
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
            if (op == Operator.Add) return TypedRuntimeMetadata.Add;
            if (op == Operator.Subtract) return TypedRuntimeMetadata.Subtract;
            if (op == Operator.Multiply) return TypedRuntimeMetadata.Multiply;
            if (op == Operator.Divide) return TypedRuntimeMetadata.Divide;
            if (op == Operator.Modulo) return TypedRuntimeMetadata.Modulo;
            if (op == Operator.Equal) return TypedRuntimeMetadata.Equal;
            if (op == Operator.NotEqual) return TypedRuntimeMetadata.NotEqual;
            if (op == Operator.LessThan) return TypedRuntimeMetadata.Less;
            if (op == Operator.LessThanOrEqual) return TypedRuntimeMetadata.LessEqual;
            if (op == Operator.GreaterThan) return TypedRuntimeMetadata.Greater;
            if (op == Operator.GreaterThanOrEqual) return TypedRuntimeMetadata.GreaterEqual;
            if (op == Operator.BitwiseAnd) return TypedRuntimeMetadata.BitwiseAnd;
            if (op == Operator.BitwiseOr) return TypedRuntimeMetadata.BitwiseOr;
            if (op == Operator.BitwiseXor) return TypedRuntimeMetadata.BitwiseXor;
            if (op == Operator.LeftShift) return TypedRuntimeMetadata.LeftShift;
            if (op == Operator.SignedRightShift) return TypedRuntimeMetadata.RightShift;
            if (op == Operator.UnSignedRightShift) return TypedRuntimeMetadata.UnsignedRightShift;
            return null;
        }

        private static MethodInfo GetBinaryConditionMethod(Operator op)
        {
            if (op == Operator.Add) return TypedRuntimeMetadata.AddBoolean;
            if (op == Operator.Subtract) return TypedRuntimeMetadata.SubtractBoolean;
            if (op == Operator.Multiply) return TypedRuntimeMetadata.MultiplyBoolean;
            if (op == Operator.Divide) return TypedRuntimeMetadata.DivideBoolean;
            if (op == Operator.Modulo) return TypedRuntimeMetadata.ModuloBoolean;
            if (op == Operator.Equal) return TypedRuntimeMetadata.EqualBoolean;
            if (op == Operator.NotEqual) return TypedRuntimeMetadata.NotEqualBoolean;
            if (op == Operator.LessThan) return TypedRuntimeMetadata.LessBoolean;
            if (op == Operator.LessThanOrEqual) return TypedRuntimeMetadata.LessEqualBoolean;
            if (op == Operator.GreaterThan) return TypedRuntimeMetadata.GreaterBoolean;
            if (op == Operator.GreaterThanOrEqual) return TypedRuntimeMetadata.GreaterEqualBoolean;
            if (op == Operator.BitwiseAnd) return TypedRuntimeMetadata.BitwiseAndBoolean;
            if (op == Operator.BitwiseOr) return TypedRuntimeMetadata.BitwiseOrBoolean;
            if (op == Operator.BitwiseXor) return TypedRuntimeMetadata.BitwiseXorBoolean;
            if (op == Operator.LeftShift) return TypedRuntimeMetadata.LeftShiftBoolean;
            if (op == Operator.SignedRightShift) return TypedRuntimeMetadata.RightShiftBoolean;
            if (op == Operator.UnSignedRightShift) return TypedRuntimeMetadata.UnsignedRightShiftBoolean;
            return null;
        }

        private static MethodInfo GetUnaryMethod(Operator op)
        {
            if (op == Operator.LogicalNot) return TypedRuntimeMetadata.Not;
            if (op == Operator.BitwiseNot) return TypedRuntimeMetadata.BitwiseNot;
            if (op == Operator.Negate) return TypedRuntimeMetadata.Negate;
            if (op == Operator.TypeOf) return TypedRuntimeMetadata.TypeOf;
            return null;
        }

        private static bool IsMutation(Operator op) =>
            op == Operator.PreIncrement || op == Operator.PostIncrement ||
            op == Operator.PreDecrement || op == Operator.PostDecrement;

        private static MethodInfo GetInvokeMethod(int argumentCount)
        {
            return (uint)argumentCount < (uint)TypedRuntimeMetadata.Invoke.Length
                ? TypedRuntimeMetadata.Invoke[argumentCount]
                : throw new NotSupportedException("Regular call arity " + argumentCount);
        }

        private static MethodInfo GetInvokePropertyMethod(int argumentCount)
        {
            return (uint)argumentCount < (uint)TypedRuntimeMetadata.InvokeProperty.Length
                ? TypedRuntimeMetadata.InvokeProperty[argumentCount]
                : throw new NotSupportedException("Property call arity " + argumentCount);
        }

        private static MethodInfo GetNewMethod(int argumentCount)
        {
            return argumentCount switch
            {
                0 => TypedRuntimeMetadata.New0,
                1 => TypedRuntimeMetadata.New1,
                2 => TypedRuntimeMetadata.New2,
                _ => throw new NotSupportedException("Constructor arity " + argumentCount)
            };
        }

        private static bool CanUseFastDirectSignature(FunctionPlan function)
        {
            return function != null &&
                function.Method != null &&
                function.DirectEntryMethod != null &&
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
