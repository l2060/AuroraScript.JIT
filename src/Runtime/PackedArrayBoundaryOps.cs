using AuroraScript.Runtime.Types;
using System;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Converts packed-array wrappers to native storage and back at dynamic
    /// boundaries. Native code keeps using the raw CLR arrays between boundaries.
    /// </summary>
    public static class PackedArrayBoundaryOps
    {
        private static readonly ConditionalWeakTable<Array, ScriptPackedArray> s_wrappers = new();

        /// <summary>Converts a nullable datum to signed 32-bit storage.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] ToInt32Storage(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : ToInt32Storage((ScriptInt32Array)value.Object);

        /// <summary>Converts a nullable datum to signed 8-bit storage.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte[] ToInt8Storage(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : ToInt8Storage((ScriptInt8Array)value.Object);

        /// <summary>Converts a nullable datum to double-precision storage.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double[] ToFloat64Storage(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : ToFloat64Storage((ScriptFloat64Array)value.Object);

        /// <summary>Converts a nullable datum to Boolean storage.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool[] ToBooleanStorage(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : ToBooleanStorage((ScriptBooleanArray)value.Object);

        /// <summary>Converts a nullable datum to unsigned 8-bit storage.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToUInt8Storage(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : ToUInt8Storage((ScriptUInt8Array)value.Object);

        /// <summary>Converts a nullable datum to signed 16-bit storage.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short[] ToInt16Storage(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : ToInt16Storage((ScriptInt16Array)value.Object);

        /// <summary>Converts a nullable datum to unsigned 16-bit storage.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort[] ToUInt16Storage(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : ToUInt16Storage((ScriptUInt16Array)value.Object);

        /// <summary>Converts a nullable datum to unsigned 32-bit storage.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint[] ToUInt32Storage(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : ToUInt32Storage((ScriptUInt32Array)value.Object);

        /// <summary>Converts a nullable datum to signed 64-bit storage.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long[] ToInt64Storage(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : ToInt64Storage((ScriptInt64Array)value.Object);

        /// <summary>Converts a nullable datum to unsigned 64-bit storage.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong[] ToUInt64Storage(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : ToUInt64Storage((ScriptUInt64Array)value.Object);

        /// <summary>Converts a nullable datum to an Int32Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptInt32Array ToInt32Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : (ScriptInt32Array)TypeCheckOps.CheckInt32Array(value).Object;

        /// <summary>Converts a nullable datum to an Int8Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptInt8Array ToInt8Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : (ScriptInt8Array)TypeCheckOps.CheckInt8Array(value).Object;

        /// <summary>Converts a nullable datum to a Float64Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFloat64Array ToFloat64Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : (ScriptFloat64Array)TypeCheckOps.CheckFloat64Array(value).Object;

        /// <summary>Converts a nullable datum to a BooleanArray wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptBooleanArray ToBooleanArray(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : (ScriptBooleanArray)TypeCheckOps.CheckBooleanArray(value).Object;

        /// <summary>Converts a nullable datum to a UInt8Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptUInt8Array ToUInt8Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : (ScriptUInt8Array)TypeCheckOps.CheckUInt8Array(value).Object;

        /// <summary>Converts a nullable datum to an Int16Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptInt16Array ToInt16Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : (ScriptInt16Array)TypeCheckOps.CheckInt16Array(value).Object;

        /// <summary>Converts a nullable datum to a UInt16Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptUInt16Array ToUInt16Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : (ScriptUInt16Array)TypeCheckOps.CheckUInt16Array(value).Object;

        /// <summary>Converts a nullable datum to a UInt32Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptUInt32Array ToUInt32Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : (ScriptUInt32Array)TypeCheckOps.CheckUInt32Array(value).Object;

        /// <summary>Converts a nullable datum to an Int64Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptInt64Array ToInt64Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : (ScriptInt64Array)TypeCheckOps.CheckInt64Array(value).Object;

        /// <summary>Converts a nullable datum to a UInt64Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptUInt64Array ToUInt64Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? null
                : (ScriptUInt64Array)TypeCheckOps.CheckUInt64Array(value).Object;

        /// <summary>Narrows a nullable object to an Int32Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptInt32Array ToInt32Array(ScriptObject value) =>
            value as ScriptInt32Array ??
                (ScriptInt32Array)Reject(value, CheckedType.Int32Array);

        /// <summary>Narrows a nullable object to an Int8Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptInt8Array ToInt8Array(ScriptObject value) =>
            value as ScriptInt8Array ??
                (ScriptInt8Array)Reject(value, CheckedType.Int8Array);

        /// <summary>Narrows a nullable object to a Float64Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFloat64Array ToFloat64Array(ScriptObject value) =>
            value as ScriptFloat64Array ??
                (ScriptFloat64Array)Reject(value, CheckedType.Float64Array);

        /// <summary>Narrows a nullable object to a BooleanArray wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptBooleanArray ToBooleanArray(ScriptObject value) =>
            value as ScriptBooleanArray ??
                (ScriptBooleanArray)Reject(value, CheckedType.BooleanArray);

        /// <summary>Narrows a nullable object to a UInt8Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptUInt8Array ToUInt8Array(ScriptObject value) =>
            value as ScriptUInt8Array ??
                (ScriptUInt8Array)Reject(value, CheckedType.UInt8Array);

        /// <summary>Narrows a nullable object to an Int16Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptInt16Array ToInt16Array(ScriptObject value) =>
            value as ScriptInt16Array ??
                (ScriptInt16Array)Reject(value, CheckedType.Int16Array);

        /// <summary>Narrows a nullable object to a UInt16Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptUInt16Array ToUInt16Array(ScriptObject value) =>
            value as ScriptUInt16Array ??
                (ScriptUInt16Array)Reject(value, CheckedType.UInt16Array);

        /// <summary>Narrows a nullable object to a UInt32Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptUInt32Array ToUInt32Array(ScriptObject value) =>
            value as ScriptUInt32Array ??
                (ScriptUInt32Array)Reject(value, CheckedType.UInt32Array);

        /// <summary>Narrows a nullable object to an Int64Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptInt64Array ToInt64Array(ScriptObject value) =>
            value as ScriptInt64Array ??
                (ScriptInt64Array)Reject(value, CheckedType.Int64Array);

        /// <summary>Narrows a nullable object to a UInt64Array wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptUInt64Array ToUInt64Array(ScriptObject value) =>
            value as ScriptUInt64Array ??
                (ScriptUInt64Array)Reject(value, CheckedType.UInt64Array);

        /// <summary>Extracts signed 32-bit storage and preserves wrapper identity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] ToInt32Storage(ScriptInt32Array value) =>
            Remember(value, value?._items);

        /// <summary>Extracts signed 8-bit storage and preserves wrapper identity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte[] ToInt8Storage(ScriptInt8Array value) =>
            Remember(value, value?._items);

        /// <summary>Extracts double storage and preserves wrapper identity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double[] ToFloat64Storage(ScriptFloat64Array value) =>
            Remember(value, value?._items);

        /// <summary>Extracts Boolean storage and preserves wrapper identity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool[] ToBooleanStorage(ScriptBooleanArray value) =>
            Remember(value, value?._items);

        /// <summary>Extracts unsigned 8-bit storage and preserves wrapper identity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToUInt8Storage(ScriptUInt8Array value) =>
            Remember(value, value?._items);

        /// <summary>Extracts signed 16-bit storage and preserves wrapper identity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short[] ToInt16Storage(ScriptInt16Array value) =>
            Remember(value, value?._items);

        /// <summary>Extracts unsigned 16-bit storage and preserves wrapper identity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort[] ToUInt16Storage(ScriptUInt16Array value) =>
            Remember(value, value?._items);

        /// <summary>Extracts unsigned 32-bit storage and preserves wrapper identity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint[] ToUInt32Storage(ScriptUInt32Array value) =>
            Remember(value, value?._items);

        /// <summary>Extracts signed 64-bit storage and preserves wrapper identity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long[] ToInt64Storage(ScriptInt64Array value) =>
            Remember(value, value?._items);

        /// <summary>Extracts unsigned 64-bit storage and preserves wrapper identity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong[] ToUInt64Storage(ScriptUInt64Array value) =>
            Remember(value, value?._items);

        /// <summary>Boxes nullable signed 32-bit storage without copying it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromInt32Storage(int[] value) =>
            Box(value, static storage => new ScriptInt32Array(storage));

        /// <summary>Boxes nullable signed 8-bit storage without copying it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromInt8Storage(sbyte[] value) =>
            Box(value, static storage => new ScriptInt8Array(storage));

        /// <summary>Boxes nullable double storage without copying it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromFloat64Storage(double[] value) =>
            Box(value, static storage => new ScriptFloat64Array(storage));

        /// <summary>Boxes nullable Boolean storage without copying it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromBooleanStorage(bool[] value) =>
            Box(value, static storage => new ScriptBooleanArray(storage));

        /// <summary>Boxes nullable unsigned 8-bit storage without copying it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromUInt8Storage(byte[] value) =>
            Box(value, static storage => new ScriptUInt8Array(storage));

        /// <summary>Boxes nullable signed 16-bit storage without copying it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromInt16Storage(short[] value) =>
            Box(value, static storage => new ScriptInt16Array(storage));

        /// <summary>Boxes nullable unsigned 16-bit storage without copying it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromUInt16Storage(ushort[] value) =>
            Box(value, static storage => new ScriptUInt16Array(storage));

        /// <summary>Boxes nullable unsigned 32-bit storage without copying it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromUInt32Storage(uint[] value) =>
            Box(value, static storage => new ScriptUInt32Array(storage));

        /// <summary>Boxes nullable signed 64-bit storage without copying it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromInt64Storage(long[] value) =>
            Box(value, static storage => new ScriptInt64Array(storage));

        /// <summary>Boxes nullable unsigned 64-bit storage without copying it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromUInt64Storage(ulong[] value) =>
            Box(value, static storage => new ScriptUInt64Array(storage));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ScriptPackedArray Reject(ScriptObject value, CheckedType expected)
        {
            if (value is null or NullValue) return null;
            TypeCheckOps.Check(ScriptDatum.FromObject(value), expected);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TStorage[] Remember<TStorage>(
            ScriptPackedArray wrapper,
            TStorage[] storage)
        {
            if (storage != null &&
                !s_wrappers.TryGetValue(storage, out _))
            {
                s_wrappers.GetValue(storage, _ => wrapper);
            }
            return storage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ScriptDatum Box<TStorage, TWrapper>(
            TStorage[] storage,
            Func<TStorage[], TWrapper> factory)
            where TWrapper : ScriptPackedArray
        {
            if (storage == null)
            {
                return ScriptDatum.Null;
            }
            if (s_wrappers.TryGetValue(storage, out var existing))
            {
                return ScriptDatum.FromObject(existing);
            }
            var created = factory(storage);
            var wrapper = s_wrappers.GetValue(storage, _ => created);
            return ScriptDatum.FromObject(wrapper);
        }
    }
}
