using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Base class for fixed-length, primitive-backed script arrays. The backing CLR
    /// array is zero-initialized by the runtime and no <see cref="ScriptDatum"/>
    /// buffer is allocated.
    /// </summary>
    public abstract class ScriptPackedArray : ScriptObject
    {
        /// <summary>Initializes an instance using the shared packed-array prototype.</summary>
        protected ScriptPackedArray() : base(Prototypes.ScriptPackedArrayPrototype)
        {
        }

        /// <summary>Gets the immutable number of elements in this array.</summary>
        public abstract int Length { get; }

        /// <inheritdoc />
        protected internal abstract override ScriptDatum TypeOfValue { get; }

        internal abstract ScriptDatum GetElementDatumUnchecked(int index);

        internal abstract void SetElementDatumUnchecked(int index, ScriptDatum value);

        internal abstract void FillDatum(ScriptDatum value);

        internal abstract ScriptPackedArray ClonePackedArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ScriptDatum GetElementDatum(int index)
        {
            // Dynamic element access has already established that the receiver is a
            // packed array. Dispatch once here so a read does not pay two abstract
            // calls (Length and GetElementDatumUnchecked) on every iteration.
            switch (this)
            {
                case ScriptInt32Array int32:
                    if ((uint)index >= (uint)int32._items.Length)
                    {
                        ThrowIndexOutOfRange(index, int32._items.Length);
                    }
                    return ScriptDatum.FromNumber(int32._items[index]);
                case ScriptInt8Array int8:
                    if ((uint)index >= (uint)int8._items.Length)
                    {
                        ThrowIndexOutOfRange(index, int8._items.Length);
                    }
                    return ScriptDatum.FromNumber(int8._items[index]);
                case ScriptFloat32Array float32:
                    if ((uint)index >= (uint)float32._items.Length)
                    {
                        ThrowIndexOutOfRange(index, float32._items.Length);
                    }
                    return ScriptDatum.FromNumber(float32._items[index]);
                case ScriptFloat64Array float64:
                    if ((uint)index >= (uint)float64._items.Length)
                    {
                        ThrowIndexOutOfRange(index, float64._items.Length);
                    }
                    return ScriptDatum.FromNumber(float64._items[index]);
                case ScriptBooleanArray boolean:
                    if ((uint)index >= (uint)boolean._items.Length)
                    {
                        ThrowIndexOutOfRange(index, boolean._items.Length);
                    }
                    return ScriptDatum.FromBoolean(boolean._items[index]);
                case ScriptUInt8Array uint8:
                    if ((uint)index >= (uint)uint8._items.Length)
                    {
                        ThrowIndexOutOfRange(index, uint8._items.Length);
                    }
                    return ScriptDatum.FromNumber(uint8._items[index]);
                case ScriptInt16Array int16:
                    if ((uint)index >= (uint)int16._items.Length)
                    {
                        ThrowIndexOutOfRange(index, int16._items.Length);
                    }
                    return ScriptDatum.FromNumber(int16._items[index]);
                case ScriptUInt16Array uint16:
                    if ((uint)index >= (uint)uint16._items.Length)
                    {
                        ThrowIndexOutOfRange(index, uint16._items.Length);
                    }
                    return ScriptDatum.FromNumber(uint16._items[index]);
                case ScriptUInt32Array uint32:
                    if ((uint)index >= (uint)uint32._items.Length)
                    {
                        ThrowIndexOutOfRange(index, uint32._items.Length);
                    }
                    return ScriptDatum.FromNumber(uint32._items[index]);
                case ScriptInt64Array int64:
                    if ((uint)index >= (uint)int64._items.Length)
                    {
                        ThrowIndexOutOfRange(index, int64._items.Length);
                    }
                    return ScriptDatum.FromNumber(ToExactNumber(int64._items[index], "Int64Array", index));
                case ScriptUInt64Array uint64:
                    if ((uint)index >= (uint)uint64._items.Length)
                    {
                        ThrowIndexOutOfRange(index, uint64._items.Length);
                    }
                    return ScriptDatum.FromNumber(ToExactNumber(uint64._items[index], "UInt64Array", index));
                default:
                    ValidateIndex(index);
                    return GetElementDatumUnchecked(index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetElementDatum(int index, ScriptDatum value)
        {
            switch (this)
            {
                case ScriptInt32Array int32:
                    if ((uint)index >= (uint)int32._items.Length)
                    {
                        ThrowIndexOutOfRange(index, int32._items.Length);
                    }
                    int32._items[index] = unchecked((int)ValueOps.ToArithmeticNumber(value));
                    return;
                case ScriptInt8Array int8:
                    if ((uint)index >= (uint)int8._items.Length)
                    {
                        ThrowIndexOutOfRange(index, int8._items.Length);
                    }
                    int8._items[index] = unchecked((sbyte)(int)ValueOps.ToArithmeticNumber(value));
                    return;
                case ScriptFloat32Array float32:
                    if ((uint)index >= (uint)float32._items.Length)
                    {
                        ThrowIndexOutOfRange(index, float32._items.Length);
                    }
                    float32._items[index] = (float)ValueOps.ToArithmeticNumber(value);
                    return;
                case ScriptFloat64Array float64:
                    if ((uint)index >= (uint)float64._items.Length)
                    {
                        ThrowIndexOutOfRange(index, float64._items.Length);
                    }
                    float64._items[index] = ValueOps.ToArithmeticNumber(value);
                    return;
                case ScriptBooleanArray boolean:
                    if ((uint)index >= (uint)boolean._items.Length)
                    {
                        ThrowIndexOutOfRange(index, boolean._items.Length);
                    }
                    boolean._items[index] = ValueOps.ToBoolean(value);
                    return;
                case ScriptUInt8Array uint8:
                    if ((uint)index >= (uint)uint8._items.Length)
                    {
                        ThrowIndexOutOfRange(index, uint8._items.Length);
                    }
                    uint8._items[index] = unchecked((byte)(int)ValueOps.ToArithmeticNumber(value));
                    return;
                case ScriptInt16Array int16:
                    if ((uint)index >= (uint)int16._items.Length)
                    {
                        ThrowIndexOutOfRange(index, int16._items.Length);
                    }
                    int16._items[index] = unchecked((short)(int)ValueOps.ToArithmeticNumber(value));
                    return;
                case ScriptUInt16Array uint16:
                    if ((uint)index >= (uint)uint16._items.Length)
                    {
                        ThrowIndexOutOfRange(index, uint16._items.Length);
                    }
                    uint16._items[index] = unchecked((ushort)(int)ValueOps.ToArithmeticNumber(value));
                    return;
                case ScriptUInt32Array uint32:
                    if ((uint)index >= (uint)uint32._items.Length)
                    {
                        ThrowIndexOutOfRange(index, uint32._items.Length);
                    }
                    uint32._items[index] = unchecked((uint)ValueOps.ToArithmeticNumber(value));
                    return;
                case ScriptInt64Array int64:
                    if ((uint)index >= (uint)int64._items.Length)
                    {
                        ThrowIndexOutOfRange(index, int64._items.Length);
                    }
                    int64._items[index] = unchecked((long)ValueOps.ToArithmeticNumber(value));
                    return;
                case ScriptUInt64Array uint64:
                    if ((uint)index >= (uint)uint64._items.Length)
                    {
                        ThrowIndexOutOfRange(index, uint64._items.Length);
                    }
                    uint64._items[index] = unchecked((ulong)ValueOps.ToArithmeticNumber(value));
                    return;
                default:
                    ValidateIndex(index);
                    SetElementDatumUnchecked(index, value);
                    return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ValidateIndex(int index)
        {
            if ((uint)index >= (uint)Length)
            {
                ThrowIndexOutOfRange(index, Length);
            }
        }

        /// <summary>
        /// Converts a script array length to a CLR array length. This follows the
        /// useful TypedArray length behavior: NaN/null become zero, fractional
        /// values are truncated, and negative/infinite/oversized lengths fail.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValidateLength(double value)
        {
            if (double.IsNaN(value) || value == 0d)
            {
                return 0;
            }
            if (value < 0d || double.IsInfinity(value) || value > Array.MaxLength)
            {
                ThrowInvalidLength(value);
            }
            return (int)Math.Truncate(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ValidateLength(ScriptDatum value)
        {
            return ValidateLength(ValueOps.ToArithmeticNumber(value));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidLength(double value)
        {
            throw new AuroraRuntimeException("Invalid packed-array length: " + value + ".");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRange(int index, int length)
        {
            throw new AuroraRuntimeException(
                "Packed-array index " + index + " is outside the valid range [0, " + length + ").");
        }

        internal static double ToExactNumber(long value, string typeName, int index)
        {
            var number = (double)value;
            // Casting a double at 2^63 saturates to long.MaxValue on the CLR,
            // so the round-trip comparison alone would incorrectly accept that
            // boundary value.  Keep the exclusive upper bound explicit.
            if (!double.IsFinite(number) || number >= 9223372036854775808d ||
                (long)number != value)
            {
                throw new AuroraRuntimeException(
                    $"{typeName} element at index {index} cannot be represented exactly as a Number.");
            }
            return number;
        }

        internal static double ToExactNumber(ulong value, string typeName, int index)
        {
            var number = (double)value;
            if (number >= 18446744073709551616d || (ulong)number != value)
            {
                throw new AuroraRuntimeException(
                    $"{typeName} element at index {index} cannot be represented exactly as a Number.");
            }
            return number;
        }

        /// <summary>Converts a signed 64-bit element to a script number without loss.</summary>
        public static double ToExactInt64Number(long value, int index) =>
            ToExactNumber(value, "Int64Array", index);

        /// <summary>Converts an unsigned 64-bit element to a script number without loss.</summary>
        public static double ToExactUInt64Number(ulong value, int index) =>
            ToExactNumber(value, "UInt64Array", index);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowDeleteNotSupported()
        {
            throw new AuroraRuntimeException("Elements cannot be deleted from a fixed-length packed array.");
        }

        internal new static void LENGTH(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptPackedArray array)
            {
                ScriptDatum.WriteAsNumber(ref result, array.Length);
            }
        }

        internal static void FILL(
            ScriptContext context,
            ScriptObject thisObject,
            Span<ScriptDatum> arguments,
            ref ScriptDatum result)
        {
            if (thisObject is not ScriptPackedArray array)
            {
                throw new AuroraRuntimeException("fill requires a packed-array receiver.");
            }

            array.FillDatum(arguments.Length == 0 ? default : arguments[0]);
            ScriptDatum.WriteAsObject(ref result, array);
        }

        /// <inheritdoc />
        protected internal override void SetPropertyDatum(
            ScriptContext context,
            string key,
            ScriptDatum value)
        {
            if (StringComparer.Ordinal.Equals(key, "length"))
            {
                throw new AuroraRuntimeException("The length of a packed array is read-only.");
            }
            base.SetPropertyDatum(context, key, value);
        }

        /// <inheritdoc />
        public sealed override ScriptEnumerator GetEnumerator()
        {
            return new ScriptEnumerator(this);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (Length == 0) return "[]";

            var builder = new StringBuilder();
            builder.Append('[');
            for (var i = 0; i < Length; i++)
            {
                if (i != 0) builder.Append(", ");
                builder.Append(ScriptDatum.ToString(GetElementDatumUnchecked(i)));
            }
            builder.Append(']');
            return builder.ToString();
        }
    }

    /// <summary>A fixed-length array backed by a CLR <see cref="int"/> array.</summary>
    public sealed class ScriptInt32Array : ScriptPackedArray
    {
        internal readonly int[] _items;

        /// <summary>Creates a zero-initialized array with the supplied length.</summary>
        public ScriptInt32Array(int length)
        {
            _items = new int[length];
        }

        internal ScriptInt32Array(int[] items)
        {
            _items = items;
        }

        /// <inheritdoc />
        public override int Length => _items.Length;

        /// <summary>Gets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetElement(int index) => _items[index];

        /// <summary>Sets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetElement(int index, int value) => _items[index] = value;

        internal override ScriptDatum GetElementDatumUnchecked(int index) =>
            ScriptDatum.FromNumber(_items[index]);

        internal override void SetElementDatumUnchecked(int index, ScriptDatum value) =>
            _items[index] = unchecked((int)ValueOps.ToArithmeticNumber(value));

        internal override void FillDatum(ScriptDatum value) =>
            Array.Fill(_items, unchecked((int)ValueOps.ToArithmeticNumber(value)));

        internal override ScriptPackedArray ClonePackedArray() =>
            new ScriptInt32Array((int[])_items.Clone());

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Int32Array;
    }

    /// <summary>A fixed-length array backed by a CLR signed-byte array.</summary>
    public sealed class ScriptInt8Array : ScriptPackedArray
    {
        internal readonly sbyte[] _items;

        /// <summary>Creates a zero-initialized array with the supplied length.</summary>
        public ScriptInt8Array(int length)
        {
            _items = new sbyte[length];
        }

        internal ScriptInt8Array(sbyte[] items)
        {
            _items = items;
        }

        /// <inheritdoc />
        public override int Length => _items.Length;

        /// <summary>Gets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte GetElement(int index) => _items[index];

        /// <summary>Sets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetElement(int index, sbyte value) => _items[index] = value;

        internal override ScriptDatum GetElementDatumUnchecked(int index) =>
            ScriptDatum.FromNumber(_items[index]);

        internal override void SetElementDatumUnchecked(int index, ScriptDatum value) =>
            _items[index] = unchecked((sbyte)(int)ValueOps.ToArithmeticNumber(value));

        internal override void FillDatum(ScriptDatum value) =>
            Array.Fill(_items, unchecked((sbyte)(int)ValueOps.ToArithmeticNumber(value)));

        internal override ScriptPackedArray ClonePackedArray() =>
            new ScriptInt8Array((sbyte[])_items.Clone());

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Int8Array;
    }

    /// <summary>A fixed-length array backed by a CLR <see cref="float"/> array.</summary>
    public sealed class ScriptFloat32Array : ScriptPackedArray
    {
        internal readonly float[] _items;

        /// <summary>Creates a zero-initialized array with the supplied length.</summary>
        public ScriptFloat32Array(int length)
        {
            _items = new float[length];
        }

        internal ScriptFloat32Array(float[] items)
        {
            _items = items;
        }

        /// <inheritdoc />
        public override int Length => _items.Length;

        /// <summary>Gets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetElement(int index) => _items[index];

        /// <summary>Sets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetElement(int index, float value) => _items[index] = value;

        internal override ScriptDatum GetElementDatumUnchecked(int index) =>
            ScriptDatum.FromNumber(_items[index]);

        internal override void SetElementDatumUnchecked(int index, ScriptDatum value) =>
            _items[index] = (float)ValueOps.ToArithmeticNumber(value);

        internal override void FillDatum(ScriptDatum value) =>
            Array.Fill(_items, (float)ValueOps.ToArithmeticNumber(value));

        internal override ScriptPackedArray ClonePackedArray() =>
            new ScriptFloat32Array((float[])_items.Clone());

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Float32Array;
    }

    /// <summary>A fixed-length array backed by a CLR <see cref="double"/> array.</summary>
    public sealed class ScriptFloat64Array : ScriptPackedArray
    {
        internal readonly double[] _items;

        /// <summary>Creates a zero-initialized array with the supplied length.</summary>
        public ScriptFloat64Array(int length)
        {
            _items = new double[length];
        }

        internal ScriptFloat64Array(double[] items)
        {
            _items = items;
        }

        /// <inheritdoc />
        public override int Length => _items.Length;

        /// <summary>Gets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetElement(int index) => _items[index];

        /// <summary>Sets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetElement(int index, double value) => _items[index] = value;

        internal override ScriptDatum GetElementDatumUnchecked(int index) =>
            ScriptDatum.FromNumber(_items[index]);

        internal override void SetElementDatumUnchecked(int index, ScriptDatum value) =>
            _items[index] = ValueOps.ToArithmeticNumber(value);

        internal override void FillDatum(ScriptDatum value) =>
            Array.Fill(_items, ValueOps.ToArithmeticNumber(value));

        internal override ScriptPackedArray ClonePackedArray() =>
            new ScriptFloat64Array((double[])_items.Clone());

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Float64Array;
    }

    /// <summary>A fixed-length array backed by a CLR <see cref="bool"/> array.</summary>
    public sealed class ScriptBooleanArray : ScriptPackedArray
    {
        internal readonly bool[] _items;

        /// <summary>Creates a zero-initialized array with the supplied length.</summary>
        public ScriptBooleanArray(int length)
        {
            _items = new bool[length];
        }

        internal ScriptBooleanArray(bool[] items)
        {
            _items = items;
        }

        /// <inheritdoc />
        public override int Length => _items.Length;

        /// <summary>Gets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetElement(int index) => _items[index];

        /// <summary>Sets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetElement(int index, bool value) => _items[index] = value;

        internal override ScriptDatum GetElementDatumUnchecked(int index) =>
            ScriptDatum.FromBoolean(_items[index]);

        internal override void SetElementDatumUnchecked(int index, ScriptDatum value) =>
            _items[index] = ValueOps.ToBoolean(value);

        internal override void FillDatum(ScriptDatum value) =>
            Array.Fill(_items, ValueOps.ToBoolean(value));

        internal override ScriptPackedArray ClonePackedArray() =>
            new ScriptBooleanArray((bool[])_items.Clone());

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.BooleanArray;
    }

    /// <summary>A fixed-length array backed by a CLR <see cref="byte"/> array.</summary>
    public sealed class ScriptUInt8Array : ScriptPackedArray
    {
        internal readonly byte[] _items;

        /// <summary>Creates a zero-initialized array with the supplied length.</summary>
        public ScriptUInt8Array(int length) => _items = new byte[length];

        internal ScriptUInt8Array(byte[] items) => _items = items;

        /// <inheritdoc />
        public override int Length => _items.Length;

        /// <summary>Gets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetElement(int index) => _items[index];

        /// <summary>Sets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetElement(int index, byte value) => _items[index] = value;

        internal override ScriptDatum GetElementDatumUnchecked(int index) =>
            ScriptDatum.FromNumber(_items[index]);

        internal override void SetElementDatumUnchecked(int index, ScriptDatum value) =>
            _items[index] = unchecked((byte)(int)ValueOps.ToArithmeticNumber(value));

        internal override void FillDatum(ScriptDatum value) =>
            Array.Fill(_items, unchecked((byte)(int)ValueOps.ToArithmeticNumber(value)));

        internal override ScriptPackedArray ClonePackedArray() =>
            new ScriptUInt8Array((byte[])_items.Clone());

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.UInt8Array;
    }

    /// <summary>A fixed-length array backed by a CLR <see cref="short"/> array.</summary>
    public sealed class ScriptInt16Array : ScriptPackedArray
    {
        internal readonly short[] _items;

        /// <summary>Creates a zero-initialized array with the supplied length.</summary>
        public ScriptInt16Array(int length) => _items = new short[length];

        internal ScriptInt16Array(short[] items) => _items = items;

        /// <inheritdoc />
        public override int Length => _items.Length;

        /// <summary>Gets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetElement(int index) => _items[index];

        /// <summary>Sets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetElement(int index, short value) => _items[index] = value;

        internal override ScriptDatum GetElementDatumUnchecked(int index) =>
            ScriptDatum.FromNumber(_items[index]);

        internal override void SetElementDatumUnchecked(int index, ScriptDatum value) =>
            _items[index] = unchecked((short)(int)ValueOps.ToArithmeticNumber(value));

        internal override void FillDatum(ScriptDatum value) =>
            Array.Fill(_items, unchecked((short)(int)ValueOps.ToArithmeticNumber(value)));

        internal override ScriptPackedArray ClonePackedArray() =>
            new ScriptInt16Array((short[])_items.Clone());

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Int16Array;
    }

    /// <summary>A fixed-length array backed by a CLR <see cref="ushort"/> array.</summary>
    public sealed class ScriptUInt16Array : ScriptPackedArray
    {
        internal readonly ushort[] _items;

        /// <summary>Creates a zero-initialized array with the supplied length.</summary>
        public ScriptUInt16Array(int length) => _items = new ushort[length];

        internal ScriptUInt16Array(ushort[] items) => _items = items;

        /// <inheritdoc />
        public override int Length => _items.Length;

        /// <summary>Gets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetElement(int index) => _items[index];

        /// <summary>Sets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetElement(int index, ushort value) => _items[index] = value;

        internal override ScriptDatum GetElementDatumUnchecked(int index) =>
            ScriptDatum.FromNumber(_items[index]);

        internal override void SetElementDatumUnchecked(int index, ScriptDatum value) =>
            _items[index] = unchecked((ushort)(int)ValueOps.ToArithmeticNumber(value));

        internal override void FillDatum(ScriptDatum value) =>
            Array.Fill(_items, unchecked((ushort)(int)ValueOps.ToArithmeticNumber(value)));

        internal override ScriptPackedArray ClonePackedArray() =>
            new ScriptUInt16Array((ushort[])_items.Clone());

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.UInt16Array;
    }

    /// <summary>A fixed-length array backed by a CLR <see cref="uint"/> array.</summary>
    public sealed class ScriptUInt32Array : ScriptPackedArray
    {
        internal readonly uint[] _items;

        /// <summary>Creates a zero-initialized array with the supplied length.</summary>
        public ScriptUInt32Array(int length) => _items = new uint[length];

        internal ScriptUInt32Array(uint[] items) => _items = items;

        /// <inheritdoc />
        public override int Length => _items.Length;

        /// <summary>Gets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetElement(int index) => _items[index];

        /// <summary>Sets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetElement(int index, uint value) => _items[index] = value;

        internal override ScriptDatum GetElementDatumUnchecked(int index) =>
            ScriptDatum.FromNumber(_items[index]);

        internal override void SetElementDatumUnchecked(int index, ScriptDatum value) =>
            _items[index] = unchecked((uint)ValueOps.ToArithmeticNumber(value));

        internal override void FillDatum(ScriptDatum value) =>
            Array.Fill(_items, unchecked((uint)ValueOps.ToArithmeticNumber(value)));

        internal override ScriptPackedArray ClonePackedArray() =>
            new ScriptUInt32Array((uint[])_items.Clone());

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.UInt32Array;
    }

    /// <summary>A fixed-length array backed by a CLR <see cref="long"/> array.</summary>
    public sealed class ScriptInt64Array : ScriptPackedArray
    {
        internal readonly long[] _items;

        /// <summary>Creates a zero-initialized array with the supplied length.</summary>
        public ScriptInt64Array(int length) => _items = new long[length];

        internal ScriptInt64Array(long[] items) => _items = items;

        /// <inheritdoc />
        public override int Length => _items.Length;

        /// <summary>Gets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetElement(int index) => _items[index];

        /// <summary>Sets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetElement(int index, long value) => _items[index] = value;

        internal override ScriptDatum GetElementDatumUnchecked(int index) =>
            ScriptDatum.FromNumber(ToExactNumber(_items[index], "Int64Array", index));

        internal override void SetElementDatumUnchecked(int index, ScriptDatum value) =>
            _items[index] = unchecked((long)ValueOps.ToArithmeticNumber(value));

        internal override void FillDatum(ScriptDatum value) =>
            Array.Fill(_items, unchecked((long)ValueOps.ToArithmeticNumber(value)));

        internal override ScriptPackedArray ClonePackedArray() =>
            new ScriptInt64Array((long[])_items.Clone());

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Int64Array;
    }

    /// <summary>A fixed-length array backed by a CLR <see cref="ulong"/> array.</summary>
    public sealed class ScriptUInt64Array : ScriptPackedArray
    {
        internal readonly ulong[] _items;

        /// <summary>Creates a zero-initialized array with the supplied length.</summary>
        public ScriptUInt64Array(int length) => _items = new ulong[length];

        internal ScriptUInt64Array(ulong[] items) => _items = items;

        /// <inheritdoc />
        public override int Length => _items.Length;

        /// <summary>Gets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetElement(int index) => _items[index];

        /// <summary>Sets an element without dynamic value conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetElement(int index, ulong value) => _items[index] = value;

        internal override ScriptDatum GetElementDatumUnchecked(int index) =>
            ScriptDatum.FromNumber(ToExactNumber(_items[index], "UInt64Array", index));

        internal override void SetElementDatumUnchecked(int index, ScriptDatum value) =>
            _items[index] = unchecked((ulong)ValueOps.ToArithmeticNumber(value));

        internal override void FillDatum(ScriptDatum value) =>
            Array.Fill(_items, unchecked((ulong)ValueOps.ToArithmeticNumber(value)));

        internal override ScriptPackedArray ClonePackedArray() =>
            new ScriptUInt64Array((ulong[])_items.Clone());

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.UInt64Array;
    }
}
