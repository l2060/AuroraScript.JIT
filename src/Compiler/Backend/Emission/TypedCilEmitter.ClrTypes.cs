using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed partial class TypedCilEmitter
    {
        private Type GetClrReceiverType(Expression expression, out bool isStatic)
        {
            isStatic = false;
            var owner = _code.GetClrType(expression);
            if (owner == null)
            {
                return null;
            }
            if (expression is NameExpression name)
            {
                var binding = _code.GetName(name);
                isStatic = binding.IsUnshadowedGlobal &&
                    _session.CompileSession.ClrTypes.TryGetType(
                        name.Identifier?.Value,
                        TypeAccess.Static,
                        out var registered) &&
                    registered == owner;
            }
            return owner;
        }

        private MemberInfo GetClrMember(Type owner, string name, bool isStatic, bool write)
        {
            if (!isStatic && owner.IsValueType) return null;
            return _session.CompileSession.ClrTypes.BindMember(owner, name, isStatic, write);
        }

        private bool TryGetClrCall(FunctionCallExpression call, Expression receiver, string name,
            out Type owner, out bool isStatic, out MethodBase method)
        {
            method = null;
            if (_code.TryGetClrCall(call, out method))
            {
                owner = method.DeclaringType;
                isStatic = method is ConstructorInfo ||
                    method is MethodInfo staticMethod && staticMethod.IsStatic;
                return !_directMode && owner != null;
            }
            owner = GetClrReceiverType(receiver, out isStatic);
            if (owner != null &&
                name != null &&
                !_session.CompileSession.ClrTypes.HasReadableMember(
                    owner,
                    name,
                    isStatic))
            {
                ReportUnknownClrMember(
                    call,
                    owner,
                    name,
                    isStatic,
                    write: false);
            }
            if (_directMode || owner == null || !isStatic && owner.IsValueType || HasSpread(call.Arguments)) return false;
            if (name == null && (!isStatic || owner.IsAbstract)) return false;
            if (name != null)
            {
                // Dynamic CLR member lookup gives getters and fields priority over methods.
                try
                {
                    const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
                    if (owner.GetProperty(name, flags) != null || owner.GetField(name, flags) != null) return false;
                }
                catch (AmbiguousMatchException) { return false; }
            }
            // CLR direct targets are selected during type analysis. Rebinding in
            // emission would bypass access checks and create a second inference path.
            return false;
        }

        private LocalBuilder SaveClrOperand(Expression expression)
        {
            var local = DeclareLocal(typeof(ScriptDatum));
            EmitDatum(expression);
            _il.Emit(OpCodes.Stloc, local);
            return local;
        }

        private LocalBuilder EmitClrReceiverGuard(LocalBuilder receiver, Type owner, bool isStatic,
            TypeAccess access, Label fallback)
        {
            _il.Emit(OpCodes.Ldloc, receiver);
            if (isStatic)
            {
                _il.Emit(OpCodes.Ldtoken, owner);
                _il.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
                EmitInt32((int)access);
                _il.Emit(OpCodes.Call, typeof(ClrDirectOps).GetMethod(nameof(ClrDirectOps.IsType)));
                _il.Emit(OpCodes.Brfalse, fallback);
                return null;
            }
            var instance = DeclareLocal(owner);
            _il.Emit(OpCodes.Call, typeof(ClrDirectOps).GetMethod(nameof(ClrDirectOps.GetInstance)).MakeGenericMethod(owner));
            _il.Emit(OpCodes.Stloc, instance);
            _il.Emit(OpCodes.Ldloc, instance);
            _il.Emit(OpCodes.Brfalse, fallback);
            return instance;
        }

        private LocalBuilder EmitClrConversion(LocalBuilder value, Type type, Label fallback, FlowValueType source = FlowValueType.Dynamic)
        {
            var converted = DeclareLocal(type);
            var sourceProperty = source switch
            {
                FlowValueType.Number or FlowValueType.Int32 or FlowValueType.UInt32 => nameof(ScriptDatum.Number),
                FlowValueType.Int64 => nameof(ScriptDatum.Int64),
                FlowValueType.UInt64 => nameof(ScriptDatum.UInt64),
                _ => null
            };
            var conversion = type == typeof(double) ? OpCodes.Conv_R8 : type == typeof(float) ? OpCodes.Conv_R4 :
                type == typeof(int) ? OpCodes.Conv_I4 : type == typeof(uint) ? OpCodes.Conv_U4 :
                type == typeof(long) ? OpCodes.Conv_I8 : type == typeof(ulong) ? OpCodes.Conv_U8 :
                type == typeof(short) ? OpCodes.Conv_I2 : type == typeof(ushort) ? OpCodes.Conv_U2 :
                type == typeof(byte) ? OpCodes.Conv_U1 : type == typeof(sbyte) ? OpCodes.Conv_I1 : default;
            var uncheckedConversion = source is FlowValueType.Number or FlowValueType.Int32 or FlowValueType.UInt32 ||
                type == typeof(double) || type == typeof(float) ||
                source == FlowValueType.Int64 && type == typeof(long) || source == FlowValueType.UInt64 && type == typeof(ulong);
            if (sourceProperty != null && conversion.Size != 0 && uncheckedConversion)
            {
                _il.Emit(OpCodes.Ldloca, value);
                _il.Emit(OpCodes.Call, typeof(ScriptDatum).GetProperty(sourceProperty).GetMethod);
                if (source == FlowValueType.UInt64 && (type == typeof(double) || type == typeof(float)))
                    _il.Emit(OpCodes.Conv_R_Un);
                _il.Emit(conversion);
                _il.Emit(OpCodes.Stloc, converted);
                return converted;
            }
            _il.Emit(OpCodes.Ldloc, value);
            _il.Emit(OpCodes.Ldloca, converted);
            _il.Emit(OpCodes.Call, typeof(ClrDirectOps).GetMethod(nameof(ClrDirectOps.TryConvert)).MakeGenericMethod(type));
            _il.Emit(OpCodes.Brfalse, fallback);
            return converted;
        }

        private StackValueKind EmitClrResult(Type type)
        {
            if (type == typeof(void)) { EmitNull(); return StackValueKind.Datum; }
            if (type == typeof(float))
            {
                _il.Emit(OpCodes.Conv_R8);
                return StackValueKind.Number;
            }
            if (type == typeof(double)) return StackValueKind.Number;
            if (type == typeof(int) || type == typeof(short) || type == typeof(ushort) ||
                type == typeof(byte) || type == typeof(sbyte)) return StackValueKind.Int32;
            if (type == typeof(uint)) return StackValueKind.UInt32;
            if (type == typeof(long)) return StackValueKind.Int64;
            if (type == typeof(ulong)) return StackValueKind.UInt64;
            if (type == typeof(bool)) return StackValueKind.Boolean;
            if (type == typeof(string))
            {
                _il.Emit(OpCodes.Call, typeof(ClrMarshaller)
                    .GetMethod(nameof(ClrMarshaller.ToDatum), [typeof(object)]));
                return StackValueKind.Datum;
            }
            if (type.IsValueType) _il.Emit(OpCodes.Box, type);
            _il.Emit(OpCodes.Call, typeof(ClrMarshaller).GetMethod(nameof(ClrMarshaller.ToDatum)));
            return StackValueKind.Datum;
        }

        private StackValueKind EmitClrCall(FunctionCallExpression call, Expression receiverExpression,
            string name, Type owner, bool isStatic, MethodBase method)
        {
            var parameters = method.GetParameters();
            if (method is ConstructorInfo constructor)
            {
                var converted = EmitClrArguments(call, parameters);
                // Keep the exception region in a typed helper: a new-expression may
                // itself be evaluated while its enclosing expression has values on the stack.
                _il.Emit(OpCodes.Ldarg_0);
                foreach (var value in converted) _il.Emit(OpCodes.Ldloc, value);
                _il.Emit(OpCodes.Call, _session.GetClrConstructor(_module.Source.FullPath, constructor));
                _il.Emit(OpCodes.Call, typeof(ClrDirectOps)
                    .GetMethod(nameof(ClrDirectOps.WrapFrozenConstruction))
                    .MakeGenericMethod(owner));
                return StackValueKind.Datum;
            }

            LocalBuilder instance = null;
            if (!isStatic)
            {
                instance = EmitRequiredClrInstance(receiverExpression, owner);
            }
            var arguments = EmitClrArguments(call, parameters);
            if (instance != null) _il.Emit(OpCodes.Ldloc, instance);
            foreach (var value in arguments) _il.Emit(OpCodes.Ldloc, value);
            var target = (MethodInfo)method;
            _il.Emit(isStatic ? OpCodes.Call : OpCodes.Callvirt, target);
            return EmitClrResult(target.ReturnType);
        }

        private LocalBuilder[] EmitClrArguments(
            FunctionCallExpression call,
            ParameterInfo[] parameters)
        {
            var values = new LocalBuilder[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                values[i] = DeclareLocal(parameters[i].ParameterType);
                EmitProvenClrValue(call.Arguments[i], parameters[i].ParameterType);
                _il.Emit(OpCodes.Stloc, values[i]);
            }
            return values;
        }

        private LocalBuilder EmitRequiredClrInstance(Expression expression, Type owner)
        {
            var instance = DeclareLocal(owner);
            EmitDatum(expression);
            _il.Emit(OpCodes.Call, typeof(ClrDirectOps)
                .GetMethod(nameof(ClrDirectOps.GetRequiredInstance))
                .MakeGenericMethod(owner));
            _il.Emit(OpCodes.Stloc, instance);
            return instance;
        }

        private void EmitProvenClrValue(Expression expression, Type target)
        {
            var source = _code.GetExpressionType(expression);
            if (target == typeof(string))
            {
                EmitString(expression);
                return;
            }
            if (target == typeof(bool))
            {
                EmitCondition(expression);
                return;
            }
            if (_code.GetClrType(expression) is { } clrType &&
                target.IsAssignableFrom(clrType))
            {
                var instance = EmitRequiredClrInstance(expression, clrType);
                _il.Emit(OpCodes.Ldloc, instance);
                if (clrType != target) _il.Emit(OpCodes.Castclass, target);
                return;
            }

            if (source == FlowValueType.Int64) EmitInt64Value(expression);
            else if (source == FlowValueType.UInt64) EmitUInt64Value(expression);
            else EmitNumber(expression);

            if (target == typeof(double))
            {
                if (source == FlowValueType.UInt64) _il.Emit(OpCodes.Conv_R_Un);
                else if (source == FlowValueType.Int64) _il.Emit(OpCodes.Conv_R8);
                return;
            }
            if (target == typeof(float))
            {
                _il.Emit(source == FlowValueType.UInt64 ? OpCodes.Conv_R_Un : OpCodes.Conv_R4);
                if (source == FlowValueType.UInt64) _il.Emit(OpCodes.Conv_R4);
            }
            else if (target == typeof(int)) _il.Emit(OpCodes.Conv_I4);
            else if (target == typeof(uint)) _il.Emit(OpCodes.Conv_U4);
            else if (target == typeof(long)) _il.Emit(OpCodes.Conv_I8);
            else if (target == typeof(ulong))
            {
                if (source != FlowValueType.UInt64) _il.Emit(OpCodes.Conv_U8);
            }
            else if (target == typeof(short)) _il.Emit(OpCodes.Conv_I2);
            else if (target == typeof(ushort)) _il.Emit(OpCodes.Conv_U2);
            else if (target == typeof(byte)) _il.Emit(OpCodes.Conv_U1);
            else if (target == typeof(sbyte)) _il.Emit(OpCodes.Conv_I1);
            else throw new NotSupportedException(
                $"Unsupported proven CLR argument type '{target}'.");
        }

        private bool TryGetClrMember(
            Expression receiver,
            string name,
            bool write,
            out Type owner,
            out bool isStatic,
            out MemberInfo member,
            AstNode diagnosticNode = null)
        {
            owner = GetClrReceiverType(receiver, out isStatic);
            member = !_directMode && owner != null ? GetClrMember(owner, name, isStatic, write) : null;
            if (owner != null &&
                !(write
                    ? _session.CompileSession.ClrTypes.HasWritableMember(
                        owner,
                        name,
                        isStatic)
                    : _session.CompileSession.ClrTypes.HasReadableMember(
                        owner,
                        name,
                        isStatic)))
            {
                ReportUnknownClrMember(
                    diagnosticNode ?? receiver,
                    owner,
                    name,
                    isStatic,
                    write);
            }
            return member != null;
        }

        private void ReportUnknownClrMember(
            AstNode node,
            Type owner,
            string name,
            bool isStatic,
            bool write)
        {
            var access = isStatic ? "static" : "instance";
            var requirement = write ? "writable" : "readable";
            var outcome = write
                ? "the assignment will have no effect"
                : "the dynamic fallback cannot resolve this member";
            _session.CompileSession.ReportWarning(
                node,
                $"CLR type '{owner.FullName}' does not contain an accessible {requirement} {access} member '{name}'; {outcome}.");
        }

        private StackValueKind EmitProvenClrMemberRead(
            Expression receiver,
            string name,
            Type owner,
            bool isStatic,
            MemberInfo member)
        {
            return EmitClrMemberRead(
                isStatic ? null : SaveClrOperand(receiver),
                name,
                owner,
                isStatic,
                member);
        }

        private StackValueKind EmitClrMemberRead(LocalBuilder receiver, string name, Type owner, bool isStatic, MemberInfo member)
        {
            LocalBuilder instance = null;
            if (!isStatic)
            {
                instance = DeclareLocal(owner);
                _il.Emit(OpCodes.Ldloc, receiver);
                _il.Emit(OpCodes.Call, typeof(ClrDirectOps)
                    .GetMethod(nameof(ClrDirectOps.GetRequiredInstance))
                    .MakeGenericMethod(owner));
                _il.Emit(OpCodes.Stloc, instance);
            }
            if (instance != null) _il.Emit(OpCodes.Ldloc, instance);
            Type resultType;
            if (member is FieldInfo field)
            {
                if (field.IsLiteral) EmitClrConstant(field.GetRawConstantValue(), field.FieldType);
                else _il.Emit(isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
                resultType = field.FieldType;
            }
            else
            {
                var property = (PropertyInfo)member;
                _il.Emit(isStatic ? OpCodes.Call : OpCodes.Callvirt, property.GetMethod);
                resultType = property.PropertyType;
            }
            return EmitClrResult(resultType);
        }

        private void EmitClrConstant(object value, Type type)
        {
            if (value == null) { _il.Emit(OpCodes.Ldnull); return; }
            if (type == typeof(string)) _session.Builder.LoadStringConstant(_il, (string)value);
            else if (type == typeof(float)) _il.Emit(OpCodes.Ldc_R4, (float)value);
            else if (type == typeof(double)) _il.Emit(OpCodes.Ldc_R8, (double)value);
            else if (type == typeof(long)) _il.Emit(OpCodes.Ldc_I8, Convert.ToInt64(value));
            else if (type == typeof(ulong)) _il.Emit(OpCodes.Ldc_I8, unchecked((long)Convert.ToUInt64(value)));
            else if (type == typeof(uint)) EmitInt32(unchecked((int)Convert.ToUInt32(value)));
            else if (type.IsEnum) EmitClrConstant(value, Enum.GetUnderlyingType(type));
            else EmitInt32(Convert.ToInt32(value));
        }

        private void EmitClrMemberWrite(LocalBuilder receiver, LocalBuilder value, string name,
            Type owner, bool isStatic, MemberInfo member)
        {
            var fallback = _il.DefineLabel();
            var done = _il.DefineLabel();
            var instance = EmitClrReceiverGuard(receiver, owner, isStatic, TypeAccess.Static, fallback);
            var type = member is FieldInfo fieldInfo ? fieldInfo.FieldType : ((PropertyInfo)member).PropertyType;
            var converted = EmitClrConversion(value, type, fallback);
            if (instance != null) _il.Emit(OpCodes.Ldloc, instance);
            _il.Emit(OpCodes.Ldloc, converted);
            if (member is FieldInfo field) _il.Emit(isStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
            else _il.Emit(isStatic ? OpCodes.Call : OpCodes.Callvirt, ((PropertyInfo)member).SetMethod);
            _il.Emit(OpCodes.Ldloc, value);
            _il.Emit(OpCodes.Br, done);
            _il.MarkLabel(fallback);
            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldarg_0);
            _session.Builder.LoadStringConstant(_il, name);
            _il.Emit(OpCodes.Ldloc, value);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.SetProperty);
            _il.MarkLabel(done);
        }

        private StackValueKind EmitProvenClrMemberWrite(
            Expression receiver,
            Expression value,
            Type owner,
            bool isStatic,
            MemberInfo member)
        {
            var instance = isStatic ? null : EmitRequiredClrInstance(receiver, owner);
            var valueType = member is FieldInfo fieldInfo
                ? fieldInfo.FieldType
                : ((PropertyInfo)member).PropertyType;
            var converted = DeclareLocal(valueType);
            EmitProvenClrValue(value, valueType);
            _il.Emit(OpCodes.Stloc, converted);

            if (instance != null) _il.Emit(OpCodes.Ldloc, instance);
            _il.Emit(OpCodes.Ldloc, converted);
            if (member is FieldInfo field)
                _il.Emit(isStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
            else
            {
                var setter = ((PropertyInfo)member).SetMethod;
                _il.Emit(isStatic ? OpCodes.Call : OpCodes.Callvirt, setter);
            }

            _il.Emit(OpCodes.Ldloc, converted);
            return EmitClrResult(valueType);
        }
    }
}
