using System;

namespace AuroraScript.Runtime.Interop
{
    // Shared semantics for CIL emitted against registered CLR types.
    internal static class ClrDirectOps
    {
        public static bool IsType(ScriptDatum value, Type type, TypeAccess access) =>
            value.Object is ClrType registration && registration._descriptor.Type == type &&
            (registration._access & access) == access;

        public static T GetInstance<T>(ScriptDatum value) where T : class =>
            value.Object is ClrInstanceObject wrapper && wrapper.Descriptor.Type == typeof(T) &&
            wrapper.Instance is T instance ? instance : null;

        public static T GetRequiredInstance<T>(ScriptDatum value)
        {
            if (value.Object is ClrInstanceObject wrapper &&
                typeof(T).IsAssignableFrom(wrapper.Descriptor.Type) &&
                wrapper.Instance is T instance)
            {
                return instance;
            }
            throw new AuroraRuntimeException(
                $"Expected a CLR instance assignable to '{typeof(T).FullName}'.");
        }

        public static ScriptDatum CheckInstance<T>(ScriptDatum value)
        {
            _ = GetRequiredInstance<T>(value);
            return value;
        }

        public static bool TryConvert<T>(ScriptDatum value, out T result)
        {
            if (ClrMarshaller.TryConvertArgument(in value, typeof(T), out var converted))
            {
                result = (T)converted;
                return true;
            }
            result = default;
            return false;
        }

        public static ScriptDatum WrapFrozenConstruction<T>(
            ScriptContext context,
            T instance)
        {
            if (!context.Engine.ClrRegistry.TryGetClrType(
                    typeof(T),
                    out _,
                    out var registration))
            {
                throw new AuroraRuntimeException(
                    $"Frozen CLR type '{typeof(T).FullName}' is not registered.");
            }
            return ScriptDatum.FromObject(
                new ClrInstanceObject(registration._descriptor, instance));
        }
    }
}
