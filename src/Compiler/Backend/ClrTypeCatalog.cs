using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AuroraScript.Compiler.Backend
{
    // Metadata only. Runtime values must still pass a guard before using these bindings.
    internal sealed class ClrTypeCatalog
    {
        private readonly Dictionary<string, (Type Type, TypeAccess Access)> _aliases = new(StringComparer.Ordinal);
        private readonly HashSet<Type> _types = new();
        private readonly Dictionary<(Type Type, string Name, bool Static), MethodBase[]> _methods = new();

        public ClrTypeCatalog(RuntimeOptions options)
        {
            foreach (var entry in options.CLRTypes)
            {
                if (!IsAccessible(entry.Type)) continue;
                _aliases.Add(entry.Alias, (entry.Type, entry.Access));
                _types.Add(entry.Type);
            }
        }

        public bool TryGetType(string alias, TypeAccess access, out Type type)
        {
            type = null;
            if (alias == null || !_aliases.TryGetValue(alias, out var entry) || (entry.Access & access) != access) return false;
            type = entry.Type;
            return true;
        }

        public bool Contains(Type type) => type != null && _types.Contains(type);

        public bool TryGetType(Type type, out Type registered)
        {
            registered = Contains(type) ? type : null;
            return registered != null;
        }

        internal static bool IsAccessible(Type type) => type.IsVisible && !type.ContainsGenericParameters &&
            !type.IsByRef && !type.IsPointer && !type.IsByRefLike && !type.IsFunctionPointer;

        public MethodBase Bind(Type type, string name, bool isStatic, FunctionCallExpression call, TypedFunctionCode code)
        {
            return Bind(type, name, isStatic, call, code.GetExpressionType, code.GetClrType);
        }

        public MethodBase Bind(
            Type type,
            string name,
            bool isStatic,
            FunctionCallExpression call,
            Func<Expression, FlowValueType> getFlowType,
            Func<Expression, Type> getClrType)
        {
            if (call.Arguments.Any(argument => argument is SpreadExpression)) return null;
            var key = (type, name, isStatic);
            if (!_methods.TryGetValue(key, out var methods))
            {
                methods = name == null
                    ? type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    : type.GetMember(name, MemberTypes.Method, BindingFlags.Public |
                        (isStatic ? BindingFlags.Static : BindingFlags.Instance)).Cast<MethodBase>().ToArray();
                _methods.Add(key, methods);
            }

            MethodBase selected = null;
            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                // Only fully proven, exact-arity signatures enter the direct path.
                // Optional, variadic, generic, and unknown conversions retain the
                // existing dynamic binder and its overload semantics.
                if (!AcceptsCount(parameters, call.Arguments.Count) ||
                    parameters.Length != call.Arguments.Count ||
                    parameters.Any(p => p.IsDefined(typeof(ParamArrayAttribute))) ||
                    method.ContainsGenericParameters || !IsAccessible(method.DeclaringType) ||
                    parameters.Any(p => !IsAccessible(p.ParameterType)) ||
                    parameters.Any(p => Nullable.GetUnderlyingType(p.ParameterType) != null) ||
                    method is MethodInfo info && !IsAccessible(info.ReturnType))
                {
                    continue;
                }

                var matches = true;
                for (var i = 0; i < parameters.Length; i++)
                {
                    var flowType = getFlowType(call.Arguments[i]);
                    var match = MatchesPrimitive(flowType, parameters[i].ParameterType);
                    var argumentClrType = getClrType(call.Arguments[i]);
                    if ((match != true || !IsRangeSafeDirectConversion(
                            flowType,
                            parameters[i].ParameterType)) &&
                        (argumentClrType == null ||
                            !parameters[i].ParameterType.IsAssignableFrom(argumentClrType)))
                    {
                        matches = false;
                        break;
                    }
                }
                if (!matches) continue;
                if (selected != null) return null;
                selected = method;
            }
            return selected;
        }

        public MemberInfo BindMember(Type owner, string name, bool isStatic, bool write)
        {
            if (owner == null || string.IsNullOrEmpty(name) || !Contains(owner) && !IsAccessible(owner))
                return null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            try
            {
                var property = owner.GetProperty(name, flags);
                if (property != null)
                {
                    var accessor = write ? property.SetMethod : property.GetMethod;
                    return property.GetIndexParameters().Length == 0 &&
                        accessor?.IsPublic == true &&
                        accessor.IsStatic == isStatic &&
                        IsAccessible(accessor.DeclaringType) &&
                        IsAccessible(property.PropertyType)
                            ? property
                            : null;
                }
                var field = owner.GetField(name, flags);
                return field != null &&
                    field.IsStatic == isStatic &&
                    (!write || !field.IsInitOnly && !field.IsLiteral) &&
                    field.GetRequiredCustomModifiers().Length == 0 &&
                    IsAccessible(field.DeclaringType) &&
                    IsAccessible(field.FieldType)
                        ? field
                        : null;
            }
            catch (AmbiguousMatchException)
            {
                return null;
            }
        }

        public bool HasReadableMember(Type owner, string name, bool isStatic)
        {
            if (owner == null || string.IsNullOrEmpty(name))
            {
                return false;
            }
            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            return owner.GetMember(name, flags).Any(member => member switch
            {
                FieldInfo field => field.IsStatic == isStatic,
                PropertyInfo property => property.GetMethod?.IsPublic == true &&
                    property.GetMethod.IsStatic == isStatic &&
                    property.GetIndexParameters().Length == 0,
                MethodInfo method => method.IsStatic == isStatic,
                _ => false
            });
        }

        public bool HasWritableMember(Type owner, string name, bool isStatic)
        {
            if (owner == null || string.IsNullOrEmpty(name))
            {
                return false;
            }
            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            return owner.GetMember(name, flags).Any(member => member switch
            {
                FieldInfo field => field.IsStatic == isStatic &&
                    !field.IsInitOnly && !field.IsLiteral,
                PropertyInfo property => property.SetMethod?.IsPublic == true &&
                    property.SetMethod.IsStatic == isStatic &&
                    property.GetIndexParameters().Length == 0,
                _ => false
            });
        }

        private static bool AcceptsCount(ParameterInfo[] parameters, int count)
        {
            var variadic = parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute));
            return count >= parameters.Count(p => !p.HasDefaultValue && !p.IsDefined(typeof(ParamArrayAttribute))) &&
                (variadic || count <= parameters.Length);
        }

        private static bool? MatchesPrimitive(FlowValueType source, Type target)
        {
            if (source == FlowValueType.Null) return false;
            target = Nullable.GetUnderlyingType(target) ?? target;
            if (target.IsEnum || target == typeof(decimal)) return null;
            ScriptDatum sample;
            if (source is FlowValueType.Number or FlowValueType.Int32 or FlowValueType.UInt32) sample = ScriptDatum.FromNumber(0);
            else if (source == FlowValueType.Int64) sample = ScriptDatum.FromInt64(0);
            else if (source == FlowValueType.UInt64) sample = ScriptDatum.FromUInt64(0);
            else if (source == FlowValueType.Boolean) sample = ScriptDatum.FromBoolean(false);
            else if (source == FlowValueType.String) sample = ScriptDatum.FromString("");
            else return null;
            return ClrMarshaller.TryConvertArgument(in sample, target, out _);
        }

        public static FlowValueType GetFlowType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(void)) return FlowValueType.Null;
            if (type == typeof(bool)) return FlowValueType.Boolean;
            if (type == typeof(string)) return FlowValueType.String;
            if (type == typeof(double) || type == typeof(float))
                return FlowValueType.Number;
            if (type == typeof(int) || type == typeof(short) || type == typeof(sbyte) ||
                type == typeof(byte) || type == typeof(ushort))
                return FlowValueType.Int32;
            if (type == typeof(uint)) return FlowValueType.UInt32;
            if (type == typeof(long)) return FlowValueType.Int64;
            if (type == typeof(ulong)) return FlowValueType.UInt64;
            return FlowValueType.Object;
        }

        public static bool CanPassPrimitive(FlowValueType source, Type target)
        {
            return MatchesPrimitive(source, target) == true &&
                IsRangeSafeDirectConversion(source, target) &&
                Nullable.GetUnderlyingType(target) == null;
        }

        private static bool IsRangeSafeDirectConversion(
            FlowValueType source,
            Type target)
        {
            target = Nullable.GetUnderlyingType(target) ?? target;
            if (source == FlowValueType.Int64)
                return target == typeof(long) ||
                    target == typeof(double) ||
                    target == typeof(float);
            if (source == FlowValueType.UInt64)
                return target == typeof(ulong) ||
                    target == typeof(double) ||
                    target == typeof(float);
            return true;
        }
    }
}
