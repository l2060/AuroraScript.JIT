using AuroraScript.Runtime.Types;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    /// <summary>Allocation-free fixed-arity call boundaries used by generated CIL.</summary>
    internal static class CallOps
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke0(ScriptDatum function, ScriptContext context) =>
            ScriptDatum.ToObject(function).Invoke(context);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke1(ScriptDatum function, ScriptContext context, ScriptDatum arg0) =>
            ScriptDatum.ToObject(function).Invoke(context, arg0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke2(ScriptDatum function, ScriptContext context, ScriptDatum arg0, ScriptDatum arg1) =>
            ScriptDatum.ToObject(function).Invoke(context, arg0, arg1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke3(ScriptDatum function, ScriptContext context, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2) =>
            ScriptDatum.ToObject(function).Invoke(context, arg0, arg1, arg2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke4(ScriptDatum function, ScriptContext context, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3) =>
            ScriptDatum.ToObject(function).Invoke(context, arg0, arg1, arg2, arg3);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke5(ScriptDatum function, ScriptContext context, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4) =>
            ScriptDatum.ToObject(function).Invoke(context, arg0, arg1, arg2, arg3, arg4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke6(ScriptDatum function, ScriptContext context, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5) =>
            ScriptDatum.ToObject(function).Invoke(context, arg0, arg1, arg2, arg3, arg4, arg5);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke7(ScriptDatum function, ScriptContext context, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6) =>
            ScriptDatum.ToObject(function).Invoke(context, arg0, arg1, arg2, arg3, arg4, arg5, arg6);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty0(ScriptDatum receiver, ScriptContext context, string name)
        {
            var instance = ScriptDatum.ToObject(receiver);
            if (TryInvokeNative(instance, context, name, Span<ScriptDatum>.Empty, out var result)) return result;
            return instance.GetPropertyValue(context, name).Invoke(context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty1(ScriptDatum receiver, ScriptContext context, string name, ScriptDatum arg0)
        {
            var instance = ScriptDatum.ToObject(receiver);
            DatumBuffer1 args = default;
            args[0] = arg0;
            if (TryInvokeNative(instance, context, name, args, out var result)) return result;
            return instance.GetPropertyValue(context, name).Invoke(context, arg0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty2(ScriptDatum receiver, ScriptContext context, string name, ScriptDatum arg0, ScriptDatum arg1)
        {
            var instance = ScriptDatum.ToObject(receiver);
            DatumBuffer2 args = default;
            args[0] = arg0;
            args[1] = arg1;
            if (TryInvokeNative(instance, context, name, args, out var result)) return result;
            return instance.GetPropertyValue(context, name).Invoke(context, arg0, arg1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty3(ScriptDatum receiver, ScriptContext context, string name, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2)
        {
            var instance = ScriptDatum.ToObject(receiver);
            DatumBuffer3 args = default;
            args[0] = arg0;
            args[1] = arg1;
            args[2] = arg2;
            if (TryInvokeNative(instance, context, name, args, out var result)) return result;
            return instance.GetPropertyValue(context, name).Invoke(context, arg0, arg1, arg2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty4(ScriptDatum receiver, ScriptContext context, string name, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3)
        {
            var instance = ScriptDatum.ToObject(receiver);
            DatumBuffer4 args = default;
            args[0] = arg0;
            args[1] = arg1;
            args[2] = arg2;
            args[3] = arg3;
            if (TryInvokeNative(instance, context, name, args, out var result)) return result;
            return instance.GetPropertyValue(context, name).Invoke(context, arg0, arg1, arg2, arg3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty5(ScriptDatum receiver, ScriptContext context, string name, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4)
        {
            var instance = ScriptDatum.ToObject(receiver);
            DatumBuffer5 args = default;
            args[0] = arg0;
            args[1] = arg1;
            args[2] = arg2;
            args[3] = arg3;
            args[4] = arg4;
            if (TryInvokeNative(instance, context, name, args, out var result)) return result;
            return instance.GetPropertyValue(context, name).Invoke(context, arg0, arg1, arg2, arg3, arg4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty6(ScriptDatum receiver, ScriptContext context, string name, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5)
        {
            var instance = ScriptDatum.ToObject(receiver);
            DatumBuffer6 args = default;
            args[0] = arg0;
            args[1] = arg1;
            args[2] = arg2;
            args[3] = arg3;
            args[4] = arg4;
            args[5] = arg5;
            if (TryInvokeNative(instance, context, name, args, out var result)) return result;
            return instance.GetPropertyValue(context, name).Invoke(context, arg0, arg1, arg2, arg3, arg4, arg5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty7(ScriptDatum receiver, ScriptContext context, string name, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6)
        {
            var instance = ScriptDatum.ToObject(receiver);
            DatumBuffer7 args = default;
            args[0] = arg0;
            args[1] = arg1;
            args[2] = arg2;
            args[3] = arg3;
            args[4] = arg4;
            args[5] = arg5;
            args[6] = arg6;
            if (TryInvokeNative(instance, context, name, args, out var result)) return result;
            return instance.GetPropertyValue(context, name).Invoke(context, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryInvokeNative(
            ScriptObject receiver,
            ScriptContext context,
            string name,
            Span<ScriptDatum> arguments,
            out ScriptDatum result)
        {
            if (receiver.TryResolveProperty(name, out var property) &&
                property.Getter == null &&
                property.Value is BondingFunction { Target: null } native)
            {
                result = default;
                native.DatumMethod.Invoke(context, receiver, arguments, ref result);
                return true;
            }
            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum[] RentArguments(int capacity)
        {
            return ArrayPool<ScriptDatum>.Shared.Rent(Math.Max(1, capacity));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum[] AppendArgument(ScriptDatum[] arguments, ref int count, ScriptDatum value)
        {
            arguments = EnsureCapacity(arguments, count + 1, count);
            arguments[count++] = value;
            return arguments;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum[] AppendSpread(ScriptDatum[] arguments, ref int count, ScriptDatum value)
        {
            if (value.Kind == ValueKind.Array && value.Object is ScriptArray array)
            {
                arguments = EnsureCapacity(arguments, count + array.Length, count);
                for (var i = 0; i < array.Length; i++) arguments[count++] = array.GetElement(i);
                return arguments;
            }
            return AppendArgument(arguments, ref count, value);
        }

        public static ScriptDatum InvokeMany(ScriptDatum function, ScriptContext context, ScriptDatum[] arguments, int count)
        {
            return ScriptDatum.ToObject(function).Invoke(context, arguments.AsSpan(0, count));
        }

        public static ScriptDatum InvokePropertyMany(
            ScriptDatum receiver,
            ScriptContext context,
            string name,
            ScriptDatum[] arguments,
            int count)
        {
            var instance = ScriptDatum.ToObject(receiver);
            var span = arguments.AsSpan(0, count);
            if (TryInvokeNative(instance, context, name, span, out var result)) return result;
            return instance.GetPropertyValue(context, name).Invoke(context, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum New0(ScriptDatum constructor, ScriptContext context) =>
            Construct(ScriptDatum.ToObject(constructor), context, Span<ScriptDatum>.Empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum New1(ScriptDatum constructor, ScriptContext context, ScriptDatum arg0)
        {
            DatumBuffer1 args = default;
            args[0] = arg0;
            return Construct(ScriptDatum.ToObject(constructor), context, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum New2(ScriptDatum constructor, ScriptContext context, ScriptDatum arg0, ScriptDatum arg1)
        {
            DatumBuffer2 args = default;
            args[0] = arg0;
            args[1] = arg1;
            return Construct(ScriptDatum.ToObject(constructor), context, args);
        }

        public static ScriptDatum NewMany(ScriptDatum constructor, ScriptContext context, ScriptDatum[] arguments, int count)
        {
            return Construct(ScriptDatum.ToObject(constructor), context, arguments.AsSpan(0, count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ScriptDatum Construct(ScriptObject constructor, ScriptContext context, Span<ScriptDatum> arguments)
        {
            if (constructor is ScriptType type)
            {
                var result = default(ScriptDatum);
                type.Construct(context, arguments, ref result);
                return result;
            }
            ThrowHelper.ThrowNotConstructor(constructor?.ToString() ?? "null");
            return default;
        }

        private static ScriptDatum[] EnsureCapacity(ScriptDatum[] arguments, int capacity, int copyCount)
        {
            if (arguments.Length >= capacity) return arguments;
            var replacement = ArrayPool<ScriptDatum>.Shared.Rent(capacity);
            Array.Copy(arguments, replacement, copyCount);
            ReturnArguments(arguments, copyCount);
            return replacement;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReturnArguments(ScriptDatum[] arguments, int count)
        {
            if (arguments == null) return;
            if (count > 0) Array.Clear(arguments, 0, count);
            ArrayPool<ScriptDatum>.Shared.Return(arguments, clearArray: false);
        }
    }
}
