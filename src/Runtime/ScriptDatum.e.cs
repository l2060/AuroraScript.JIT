using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Partial implementation of <see cref="ScriptDatum"/> providing static factories and 
    /// destination-writing (WriteAs) methods for efficient value manipulation.
    /// </summary>
    public partial struct ScriptDatum
    {
        /// <summary> A reference to a null script datum. </summary>
        public static readonly ScriptDatum Null = default;
        /// <summary> A reference to a NaN script datum. </summary>
        public static readonly ScriptDatum NaN = ScriptDatum.FromNumber(Double.NaN);
        /// <summary> A reference to a true script datum. </summary>
        public static readonly ScriptDatum True = ScriptDatum.FromBoolean(true);
        /// <summary> A reference to a false script datum. </summary>
        public static readonly ScriptDatum False = ScriptDatum.FromBoolean(false);

        /// <summary>
        /// Gets the CLR <see cref="ScriptObject"/> representation of any script value,
        /// materializing immutable primitive wrappers when necessary.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetScriptObject(
            in ScriptDatum value,
            out ScriptObject result)
        {
            switch (value.Kind)
            {
                case ValueKind.Null:
                    result = NullValue.Instance;
                    return true;
                case ValueKind.Boolean:
                    result = BooleanValue.Of(value.Boolean);
                    return true;
                case ValueKind.Number:
                    result = NumberValue.Of(value.Number);
                    return true;
                case ValueKind.Int64:
                    result = new Int64Value(value.Int64);
                    return true;
                case ValueKind.UInt64:
                    result = new UInt64Value(value.UInt64);
                    return true;
                case ValueKind.String:
                    result = StringValue.Of(value.StringText);
                    return true;
                default:
                    result = value.Object;
                    return result != null;
            }
        }


        /// <summary>
        /// Marks the given datum as Null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarkAsNull(ref ScriptDatum dst)
        {
            dst.SetNull();
        }

        /// <summary> Creates a new datum from a boolean value. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromBoolean(bool value)
        {
            return CreateBoolean(value);
        }

        /// <summary> Writes a boolean value into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsBoolean(ref ScriptDatum dst, bool value)
        {
            dst.SetBoolean(value);
        }

        /// <summary> Writes a numeric value (keeping existing Kind) into the destination. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteNumber(ref ScriptDatum dst, double value)
        {
            dst.SetNumber(value);
        }

        /// <summary> Writes a numeric value and sets the Kind to Number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsNumber(ref ScriptDatum dst, double value)
        {
            dst.SetNumber(value);
        }

        /// <summary> Creates a new datum from a double-precision number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromNumber(double value)
        {
            return CreateNumber(value);
        }

        /// <summary> Creates a new numeric datum from an Int32 without widening at the call site. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromNumber(int value)
        {
            return CreateNumber(value);
        }

        /// <summary> Creates a new numeric datum from a UInt32 without signed widening. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromNumber(uint value)
        {
            return CreateNumber(value);
        }

        /// <summary> Creates a new numeric datum from an Int64 without widening at the call site. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromNumber(long value)
        {
            return CreateNumber(value);
        }

        /// <summary>Creates a datum containing an exact signed 64-bit integer.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromInt64(long value)
        {
            return CreateInt64(value);
        }

        /// <summary>Writes an exact signed 64-bit integer into the destination datum.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsInt64(ref ScriptDatum dst, long value)
        {
            dst.SetInt64(value);
        }

        /// <summary>Creates a datum containing an exact unsigned 64-bit integer.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromUInt64(ulong value)
        {
            return CreateUInt64(value);
        }

        /// <summary>Writes an exact unsigned 64-bit integer into the destination datum.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsUInt64(ref ScriptDatum dst, ulong value)
        {
            dst.SetUInt64(value);
        }

        /// <summary> Creates a new datum from an existing <see cref="StringValue"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromString(StringValue value)
        {
            return CreateString(value?.Value);
        }

        /// <summary> Creates a new datum from a .NET string. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromString(String value)
        {
            return CreateString(value);
        }

        /// <summary> Writes a .NET string into the destination datum as a StringValue. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsString(ref ScriptDatum dst, String value)
        {
            dst.SetString(value);
        }

        /// <summary> Writes an existing StringValue into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsString(ref ScriptDatum dst, StringValue value)
        {
            dst.SetString(value?.Value);
        }

        /// <summary> Creates a new datum from a script array. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromArray(ScriptArray value)
        {
            return CreateReference(ValueKind.Array, value);
        }

        /// <summary> Writes a script array into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsArray(ref ScriptDatum dst, ScriptArray value)
        {
            dst.SetReference(ValueKind.Array, value);
        }

        /// <summary> Creates a new datum from a script date. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromDate(ScriptDate date)
        {
            return CreateReference(ValueKind.Date, date);
        }

        /// <summary> Writes a script date into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsDate(ref ScriptDatum dst, ScriptDate value)
        {
            dst.SetReference(ValueKind.Date, value);
        }

        /// <summary> Writes a .NET DateTimeOffset into the destination as a script date. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsDate(ref ScriptDatum dst, DateTimeOffset value)
        {
            dst.SetReference(ValueKind.Date, new ScriptDate(value));
        }

        /// <summary> Writes a .NET DateTime into the destination as a script date. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsDate(ref ScriptDatum dst, DateTime value)
        {
            dst.SetReference(ValueKind.Date, new ScriptDate(value));
        }

        /// <summary> Creates a new datum from a .NET DateTime. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromDate(DateTime date)
        {
            return CreateReference(ValueKind.Date, new ScriptDate(date));
        }

        /// <summary> Creates a new datum from a .NET DateTimeOffset. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromDate(DateTimeOffset date)
        {
            return CreateReference(ValueKind.Date, new ScriptDate(date));
        }

        /// <summary> Creates a new datum from a script regex. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromRegex(ScriptRegex value)
        {
            return CreateReference(ValueKind.Regex, value);
        }

        /// <summary> Writes a script regex into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsRegex(ref ScriptDatum dst, ScriptRegex value)
        {
            dst.SetReference(ValueKind.Regex, value);
        }

        /// <summary> Creates a new datum from a script function (closure). </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromFunction(ClosureFunction value)
        {
            return CreateReference(ValueKind.Function, value);
        }

        /// <summary> Writes a script function into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsFunction(ref ScriptDatum dst, ClosureFunction value)
        {
            dst.SetReference(ValueKind.Function, value);
        }

        /// <summary> Creates a new datum from a CLR method binding. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromClrFunction(ClrMethodBinding value)
        {
            return CreateReference(ValueKind.ClrFunction, value);
        }

        /// <summary> Creates a new datum from a script type. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromType(ScriptType value)
        {
            return CreateReference(ValueKind.Type, value);
        }

        /// <summary> Writes a script type into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsType(ref ScriptDatum dst, ScriptType value)
        {
            dst.SetReference(ValueKind.Type, value);
        }

        /// <summary> Creates a new datum from a CLR bonding function. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromBonding(BondingFunction value)
        {
            return CreateReference(ValueKind.ClrBonding, value);
        }

        /// <summary> Writes a CLR bonding function into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsClrBonding(ref ScriptDatum dst, BondingFunction value)
        {
            dst.SetReference(ValueKind.ClrBonding, value);
        }

        /// <summary> Writes a native delegate into the destination datum as a bonding function. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsClrBonding(ref ScriptDatum dst, ClrDatumDelegate value)
        {
            dst.SetReference(ValueKind.ClrBonding, new BondingFunction(value));
        }

        /// <summary> Creates a new datum from a native delegate. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromBonding(ClrDatumDelegate value)
        {
            return CreateReference(ValueKind.ClrBonding, new BondingFunction(value));
        }


        /// <summary> Creates a new datum from a native getter delegate. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromBondingGetter(ClrGetterDelegate callback)
        {
            return CreateReference(ValueKind.ClrBonding, new BondingGetter(callback));
        }

        /// <summary> Writes a generic script object into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsObject(ref ScriptDatum dst, ScriptObject value)
        {
            dst.SetReference(ValueKind.Object, value);
        }


        /// <summary> Creates a new datum from a ERROR. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromError(ScriptError value)
        {
            return CreateReference(ValueKind.Error, value);
        }


        /// <summary> Writes a generic script object into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsError(ref ScriptDatum dst, ScriptError value)
        {
            dst.SetReference(ValueKind.Error, value);
        }

        /// <summary>
        /// Writes an existing <see cref="ScriptObject"/> into the destination datum,
        /// automatically determining the correct <see cref="ValueKind"/> based on the operand's concrete type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteObject(ref ScriptDatum dst, ScriptObject value)
        {
            switch (value)
            {
                case null:
                case NullValue:
                    dst.SetNull();
                    return;
                case NumberValue numberValue:
                    dst.SetNumber(numberValue.DoubleValue);
                    return;

                case Int64Value int64Value:
                    dst.SetInt64(int64Value.Value);
                    return;

                case UInt64Value uint64Value:
                    dst.SetUInt64(uint64Value.Value);
                    return;

                case BooleanValue booleanValue:
                    dst.SetBoolean(booleanValue.Value);
                    return;

                case StringValue stringValue:
                    dst.SetString(stringValue.Value);
                    return;

                case ScriptArray:
                    dst.SetReference(ValueKind.Array, value);
                    return;

                case ScriptDate:
                    dst.SetReference(ValueKind.Date, value);
                    return;

                case ScriptRegex:
                    dst.SetReference(ValueKind.Regex, value);
                    return;

                case ClrMethodBinding:
                    dst.SetReference(ValueKind.ClrFunction, value);
                    return;

                case ClosureFunction:
                    dst.SetReference(ValueKind.Function, value);
                    return;

                case ScriptType:
                    dst.SetReference(ValueKind.Type, value);
                    return;

                case BondingFunction:
                    dst.SetReference(ValueKind.ClrBonding, value);
                    return;

                case ScriptError:
                    dst.SetReference(ValueKind.Error, value);
                    return;

                default:
                    dst.SetReference(ValueKind.Object, value);
                    return;
            }
        }

        /// <summary>
        /// Creates a new datum from an existing <see cref="ScriptObject"/>,
        /// automatically determining the correct <see cref="ValueKind"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromObject(ScriptObject value)
        {
            switch (value)
            {
                case null:
                case NullValue:
                    return default;

                case NumberValue numberValue:
                    return FromNumber(numberValue.DoubleValue);

                case Int64Value int64Value:
                    return FromInt64(int64Value.Value);

                case UInt64Value uint64Value:
                    return FromUInt64(uint64Value.Value);

                case BooleanValue booleanValue:
                    return FromBoolean(booleanValue.Value);

                case StringValue stringValue:
                    return FromString(stringValue);

                case ScriptArray scriptArray:
                    return FromArray(scriptArray);

                case ScriptDate scriptDate:
                    return FromDate(scriptDate);

                case ScriptRegex scriptRegex:
                    return FromRegex(scriptRegex);

                case ClosureFunction closureFunction:
                    return FromFunction(closureFunction);

                case ScriptError scriptError:
                    return FromError(scriptError);

                case ClrMethodBinding clrMethodBinding:
                    return FromClrFunction(clrMethodBinding);

                case ScriptType clrTypeObject:
                    return FromType(clrTypeObject);

                case BondingFunction bonding:
                    return FromBonding(bonding);

                default:
                    return CreateReference(ValueKind.Object, value);
            }

        }

        /// <summary>
        /// Boxes the script datum into a <see cref="ScriptObject"/> wrapper if necessary.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptObject ToObject(ScriptDatum d)
        {
            switch (d.Kind)
            {
                case ValueKind.Null:
                    return ScriptObject.Null;
                case ValueKind.Boolean:
                    return BooleanValue.Of(d.Boolean);
                case ValueKind.Number:
                    return NumberValue.Of(d.Number);
                case ValueKind.Int64:
                    return new Int64Value(d.Int64);
                case ValueKind.UInt64:
                    return new UInt64Value(d.UInt64);
                case ValueKind.String:
                    return d.String;
                default:
                    return d.Object;
            }
        }

        /// <summary>
        /// Evaluates the truthiness of the datum according to JavaScript-like rules.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTrue(ScriptDatum d)
        {
            switch (d.Kind)
            {
                case ValueKind.Null:
                    return false;
                case ValueKind.Boolean:
                    return d.Boolean;
                case ValueKind.Number:
                    var num = d.Number;
                    return num != 0 && !double.IsNaN(num);
                case ValueKind.Int64:
                    return d.Int64 != 0;
                case ValueKind.UInt64:
                    return d.UInt64 != 0;
                case ValueKind.String:
                    return !string.IsNullOrEmpty(d.StringText);
                default:
                    return d.Object != ScriptObject.Null;
            }
        }

        /// <summary>
        /// Evaluates the falsiness of the datum.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFalse(ScriptDatum d)
        {
            switch (d.Kind)
            {
                case ValueKind.Null:
                    return true;
                case ValueKind.Boolean:
                    return !d.Boolean;
                case ValueKind.Number:
                    var num = d.Number;
                    return num == 0 || double.IsNaN(num);
                case ValueKind.Int64:
                    return d.Int64 == 0;
                case ValueKind.UInt64:
                    return d.UInt64 == 0;
                case ValueKind.String:
                    return string.IsNullOrEmpty(d.StringText);
                default:
                    return d.Object == ScriptObject.Null;
            }
        }

        /// <summary>
        /// Returns the script type name of the datum.
        /// </summary>
        public static string GetTypeName(ScriptDatum d)
        {
            ScriptDatum datum = TypeOf(d);
            return datum.StringText;
        }

        /// <summary>
        /// Returns a datum representing the type of the given datum.
        /// Primitive kinds are identified from <see cref="ValueKind"/>; object
        /// identity comes from <see cref="ScriptObject.TypeOfValue"/>.
        /// </summary>
        public static ScriptDatum TypeOf(ScriptDatum d)
        {
            if (d.Reference is ScriptObject scriptObject)
            {
                return scriptObject.TypeOfValue;
            }

            switch (d.Kind)
            {
                case ValueKind.Boolean:
                    return TypeNames.Boolean;
                case ValueKind.Number:
                    return TypeNames.Number;
                case ValueKind.Int64:
                    return TypeNames.Int64;
                case ValueKind.UInt64:
                    return TypeNames.UInt64;
                case ValueKind.String:
                    return TypeNames.String;
                default:
                    return TypeNames.Null;
            }
        }

        /// <summary>
        /// Converts the datum to its string representation.
        /// </summary>
        public static string ToString(ScriptDatum d)
        {
            switch (d.Kind)
            {
                case ValueKind.Null:
                    return "null";
                case ValueKind.Boolean:
                    return d.Boolean.ToString();
                case ValueKind.Number:
                    return d.Number.ToString();
                case ValueKind.Int64:
                    return d.Int64.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case ValueKind.UInt64:
                    return d.UInt64.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case ValueKind.String:
                    return d.StringText;
                default:
                    return d.Object.ToString();
            }
        }
    }




}
