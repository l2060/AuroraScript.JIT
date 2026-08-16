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
                case ScriptBooleanArray boolean:
                    if ((uint)index >= (uint)boolean._items.Length)
                    {
                        ThrowIndexOutOfRange(index, boolean._items.Length);
                    }
                    return ScriptDatum.FromBoolean(boolean._items[index]);
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
                case ScriptBooleanArray boolean:
                    if ((uint)index >= (uint)boolean._items.Length)
                    {
                        ThrowIndexOutOfRange(index, boolean._items.Length);
                    }
                    boolean._items[index] = ValueOps.ToBoolean(value);
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

        internal override void SetPropertyDatum(
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
    }
}
