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
        /// Marks the given datum as Null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarkAsNull(ref ScriptDatum dst)
        {
            dst.Kind = ValueKind.Null;
            dst.Object = null;
        }

        /// <summary> Creates a new datum from a boolean value. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromBoolean(bool value)
        {
            return new ScriptDatum { Kind = ValueKind.Boolean, Boolean = value };
        }

        /// <summary> Writes a boolean value into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsBoolean(ref ScriptDatum dst, bool value)
        {
            dst.Kind = ValueKind.Boolean;
            dst.Boolean = value;
            dst.Object = null;
        }

        /// <summary> Writes a numeric value (keeping existing Kind) into the destination. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteNumber(ref ScriptDatum dst, double value)
        {
            dst.Number = value;
            dst.Object = null;
        }

        /// <summary> Writes a numeric value and sets the Kind to Number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsNumber(ref ScriptDatum dst, double value)
        {
            dst.Kind = ValueKind.Number;
            dst.Number = value;
        }

        /// <summary> Creates a new datum from a double-precision number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromNumber(double value)
        {
            return new ScriptDatum { Kind = ValueKind.Number, Number = value };
        }

        /// <summary> Creates a new datum from an existing <see cref="StringValue"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromString(StringValue value)
        {
            return new ScriptDatum { Kind = ValueKind.String, String = value };
        }

        /// <summary> Creates a new datum from a .NET string. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromString(String value)
        {
            return new ScriptDatum { Kind = ValueKind.String, String = StringValue.Of(value) };
        }

        /// <summary> Writes a .NET string into the destination datum as a StringValue. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsString(ref ScriptDatum dst, String value)
        {
            dst.Kind = ValueKind.String;
            dst.String = StringValue.Of(value);
        }

        /// <summary> Writes an existing StringValue into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsString(ref ScriptDatum dst, StringValue value)
        {
            dst.Kind = ValueKind.String;
            dst.String = value;
        }

        /// <summary> Creates a new datum from a script array. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromArray(ScriptArray value)
        {
            return new ScriptDatum { Kind = ValueKind.Array, Object = value };
        }

        /// <summary> Writes a script array into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsArray(ref ScriptDatum dst, ScriptArray value)
        {
            dst.Kind = ValueKind.Array;
            dst.Object = value;
        }

        /// <summary> Creates a new datum from a script date. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromDate(ScriptDate date)
        {
            return new ScriptDatum { Kind = ValueKind.Date, Object = date };
        }

        /// <summary> Writes a script date into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsDate(ref ScriptDatum dst, ScriptDate value)
        {
            dst.Kind = ValueKind.Date;
            dst.Object = value;
        }

        /// <summary> Writes a .NET DateTimeOffset into the destination as a script date. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsDate(ref ScriptDatum dst, DateTimeOffset value)
        {
            dst.Kind = ValueKind.Date;
            dst.Object = new ScriptDate(value);
        }

        /// <summary> Writes a .NET DateTime into the destination as a script date. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsDate(ref ScriptDatum dst, DateTime value)
        {
            dst.Kind = ValueKind.Date;
            dst.Object = new ScriptDate(value);
        }

        /// <summary> Creates a new datum from a .NET DateTime. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromDate(DateTime date)
        {
            return new ScriptDatum { Kind = ValueKind.Date, Object = new ScriptDate(date) };
        }

        /// <summary> Creates a new datum from a .NET DateTimeOffset. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromDate(DateTimeOffset date)
        {
            return new ScriptDatum { Kind = ValueKind.Date, Object = new ScriptDate(date) };
        }

        /// <summary> Creates a new datum from a script regex. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromRegex(ScriptRegex value)
        {
            return new ScriptDatum { Kind = ValueKind.Regex, Object = value };
        }

        /// <summary> Writes a script regex into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsRegex(ref ScriptDatum dst, ScriptRegex value)
        {
            dst.Kind = ValueKind.Regex;
            dst.Object = value;
        }

        /// <summary> Creates a new datum from a script function (closure). </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromFunction(ClosureFunction value)
        {
            return new ScriptDatum { Kind = ValueKind.Function, Object = value };
        }

        /// <summary> Writes a script function into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsFunction(ref ScriptDatum dst, ClosureFunction value)
        {
            dst.Kind = ValueKind.Function;
            dst.Object = value;
        }

        /// <summary> Creates a new datum from a CLR method binding. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromClrFunction(ClrMethodBinding value)
        {
            return new ScriptDatum { Kind = ValueKind.ClrFunction, Object = value };
        }

        /// <summary> Creates a new datum from a script type. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromType(ScriptType value)
        {
            return new ScriptDatum { Kind = ValueKind.Type, Object = value };
        }

        /// <summary> Writes a script type into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsType(ref ScriptDatum dst, ScriptType value)
        {
            dst.Kind = ValueKind.Type;
            dst.Object = value;
        }

        /// <summary> Creates a new datum from a CLR bonding function. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromBonding(BondingFunction value)
        {
            return new ScriptDatum { Kind = ValueKind.ClrBonding, Object = value };
        }

        /// <summary> Writes a CLR bonding function into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsClrBonding(ref ScriptDatum dst, BondingFunction value)
        {
            dst.Kind = ValueKind.ClrBonding;
            dst.Object = value;
        }

        /// <summary> Writes a native delegate into the destination datum as a bonding function. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsClrBonding(ref ScriptDatum dst, ClrDatumDelegate value)
        {
            dst.Kind = ValueKind.ClrBonding;
            dst.Object = new BondingFunction(value);
        }

        /// <summary> Creates a new datum from a native delegate. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromBonding(ClrDatumDelegate value)
        {
            return new ScriptDatum { Kind = ValueKind.ClrBonding, Object = new BondingFunction(value) };
        }


        /// <summary> Creates a new datum from a native getter delegate. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromBondingGetter(ClrGetterDelegate callback)
        {
            return new ScriptDatum { Kind = ValueKind.ClrBonding, Object = new BondingGetter(callback) };
        }

        /// <summary> Writes a generic script object into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsObject(ref ScriptDatum dst, ScriptObject value)
        {
            dst.Kind = ValueKind.Object;
            dst.Object = value;
        }


        /// <summary> Creates a new datum from a ERROR. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromError(ScriptError value)
        {
            return new ScriptDatum { Kind = ValueKind.Error, Object = value };
        }


        /// <summary> Writes a generic script object into the destination datum. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsError(ref ScriptDatum dst, ScriptError value)
        {
            dst.Kind = ValueKind.Error;
            dst.Object = value;
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
                    dst.Kind = ValueKind.Null;
                    return;
                case NumberValue numberValue:
                    dst.Kind = ValueKind.Number;
                    dst.Number = numberValue.DoubleValue;
                    return;

                case BooleanValue booleanValue:
                    dst.Kind = ValueKind.Boolean;
                    dst.Boolean = booleanValue.Value;
                    return;

                case StringValue:
                    dst.Kind = ValueKind.String;
                    dst.Object = value;
                    return;

                case ScriptArray:
                    dst.Kind = ValueKind.Array;
                    dst.Object = value;
                    return;

                case ScriptDate:
                    dst.Kind = ValueKind.Date;
                    dst.Object = value;
                    return;

                case ScriptRegex:
                    dst.Kind = ValueKind.Regex;
                    dst.Object = value;
                    return;

                case ClrMethodBinding:
                    dst.Kind = ValueKind.ClrFunction;
                    dst.Object = value;
                    return;

                case ClosureFunction:
                    dst.Kind = ValueKind.Function;
                    dst.Object = value;
                    return;

                case ScriptType:
                    dst.Kind = ValueKind.Type;
                    dst.Object = value;
                    return;

                case BondingFunction:
                    dst.Kind = ValueKind.ClrBonding;
                    dst.Object = value;
                    return;

                case ScriptError:
                    dst.Kind = ValueKind.Error;
                    dst.Object = value;
                    return;

                default:
                    dst.Kind = ValueKind.Object;
                    dst.Object = value;
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

                case ClrMethodBinding clrMethodBinding:
                    return FromClrFunction(clrMethodBinding);

                case ScriptType clrTypeObject:
                    return FromType(clrTypeObject);

                case BondingFunction bonding:
                    return FromBonding(bonding);

                default:
                    return new ScriptDatum { Kind = ValueKind.Object, Object = value };
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
                case ValueKind.String:
                    return !string.IsNullOrEmpty(d.String.Value);
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
                case ValueKind.String:
                    return string.IsNullOrEmpty(d.String.Value);
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
            return datum.String.Value;
        }

        /// <summary>
        /// Returns a datum representing the type of the given datum.
        /// </summary>
        public static ScriptDatum TypeOf(ScriptDatum d)
        {
            switch (d.Kind)
            {
                case ValueKind.Null:
                    return TypeNames.Null;
                case ValueKind.Boolean:
                    return TypeNames.Boolean;
                case ValueKind.Number:
                    return TypeNames.Number;
                case ValueKind.String:
                    return TypeNames.String;
                case ValueKind.Object:
                    return TypeNames.Object;
                case ValueKind.Date:
                    return TypeNames.Date;
                case ValueKind.Array:
                    return TypeNames.Array;
                case ValueKind.Regex:
                    return TypeNames.Regex;
                case ValueKind.Function:
                    return TypeNames.Function;
                case ValueKind.Type:
                    return TypeNames.Type;
                case ValueKind.ClrFunction:
                    return TypeNames.ClrFunction;
                case ValueKind.ClrBonding:
                    return TypeNames.ClrBonding;
                case ValueKind.Error:
                    return TypeNames.Error;

                default:
                    return TypeNames.Object;
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
                case ValueKind.String:
                    return d.String.Value;
                default:
                    return d.Object.ToString();
            }
        }
    }




}
