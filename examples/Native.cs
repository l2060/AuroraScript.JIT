using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using System;

namespace Examples
{
    [AuroraNativeType("Vec2")]
    public sealed partial class Vec2 : ScriptObject, INativeTypedDocument
    {
        [AuroraExport("x")] public double X;
        [AuroraExport("y")] public double Y;
        [AuroraExport("DIMENSIONS")]
        public static readonly double Dimensions = 2;

        [AuroraExport]
        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public void WriteTypedDocument(ref TypedDocumentOutput output)
        {
            output.WriteMember("x", X);
            output.WriteMember("y", Y);
            output.WriteDynamicMembers(this);
        }

        public void ReadTypedDocument(ref TypedDocumentInput input)
        {
            if (input.IsElement)
            {
                throw input.Error("Not a valid format for Vec2 type documentation value.");
            }

            switch (input.MemberName)
            {
                case "x":
                    X = ReadFiniteNumber(ref input);
                    return;
                case "y":
                    Y = ReadFiniteNumber(ref input);
                    return;
                default:
                    input.DefineDynamicMember(this);
                    return;
            }
        }

        private static double ReadFiniteNumber(ref TypedDocumentInput input)
        {
            var value = input.Value;
            if (value.Kind != ValueKind.Number || !double.IsFinite(value.Number))
            {
                throw input.Error("Vec2 values require a finite number.");
            }

            return value.Number;
        }
        [AuroraExport("length")]
        public double LengthCore() => Math.Sqrt((X * X) + (Y * Y));

        [AuroraExport("length")]
        public static double StaticLengthCore(double x, double y) => Math.Sqrt((x * x) + (y * y));

        [AuroraExport("from")]
        public static Vec2 FromCore(double x, double y) => new Vec2(x, y);

    }



    /// <summary>
    /// Experimental script global used to validate <see cref="AuroraExportAttribute"/> source generation.
    /// </summary>
    [AuroraNativeType("Stats")]
    public sealed partial class StatsSupport : ScriptObject
    {

        /// <summary>Circle constant pi.</summary>
        [AuroraExport("PI")]
        public static readonly double PI = Math.PI;

        /// <summary>Returns the arithmetic mean of two numbers.</summary>
        [AuroraExport("mean", MatchFailure.ReturnNaN)]
        public static double MeanCore(double a, double b) => (a + b) / 2D;

        /// <summary>Returns the sum of two exact numbers.</summary>
        [AuroraExport("sumExact", MatchFailure.Throw)]
        public static double SumExactCore(
            [AuroraParam(MatchLevel.Exact)] double a,
            [AuroraParam(MatchLevel.Exact)] double b)
            => a + b;

        /// <summary>Returns an exact object argument unchanged.</summary>
        [AuroraExport("identity", MatchFailure.Throw)]
        public static ScriptObject IdentityCore(
            [AuroraParam(MatchLevel.Exact)] ScriptObject value)
            => value;

        /// <summary>Concatenates a string and an exact Int32 value.</summary>
        [AuroraExport("chat", MatchFailure.Throw)]
        public static string Chat(
            string value,
            [AuroraParam(MatchLevel.Exact)] int piece)
            => value + piece;

        /// <summary>Returns a script value unchanged.</summary>
        [AuroraExport("echo", MatchFailure.Throw)]
        public static ScriptDatum EchoCore(ScriptDatum value) => value;

        /// <summary>Returns whether the call has an engine-bound context.</summary>
        [AuroraExport("hasEngine", MatchFailure.Throw)]
        public static bool HasEngineCore(ScriptContext ctx) => ctx?.Engine != null;

        /// <summary>Returns whether the argument is the same object as this.</summary>
        [AuroraExport("sameThis", MatchFailure.Throw)]
        public static bool SameThisCore(
            ScriptContext ctx,
            ScriptObject thisObject,
            ScriptObject other)
            => ReferenceEquals(thisObject, other);

        /// <summary>Counts arbitrary trailing script values.</summary>
        [AuroraExport("restCount", MatchFailure.Throw)]
        public static double RestCountCore(params ScriptDatum[] values)
            => values.Length;

        /// <summary>Combines a fixed prefix with an arbitrary script-value tail.</summary>
        [AuroraExport("restAfter", MatchFailure.Throw)]
        public static double RestAfterCore(
            double first,
            params ScriptDatum[] values)
            => first + values.Length;

        /// <summary>Returns a script array unchanged.</summary>
        [AuroraExport("array", MatchFailure.Throw)]
        public static ScriptArray ArrayCore(ScriptArray value) => value;

        /// <summary>Returns a packed array unchanged.</summary>
        [AuroraExport("packed", MatchFailure.Throw)]
        public static ScriptPackedArray PackedCore(ScriptPackedArray value) => value;

        /// <summary>Returns a concrete packed array unchanged.</summary>
        [AuroraExport("int8Array", MatchFailure.Throw)]
        public static ScriptInt8Array Int8ArrayCore(ScriptInt8Array value) => value;

        /// <summary>Returns a Path unchanged.</summary>
        [AuroraExport("path", MatchFailure.Throw)]
        public static ScriptPathValue PathCore(ScriptPathValue value) => value;

        /// <summary>Returns a StringBuffer unchanged.</summary>
        [AuroraExport("stringBuffer", MatchFailure.Throw)]
        public static StringBuffer StringBufferCore(StringBuffer value) => value;

        /// <summary>Returns a Proxy unchanged.</summary>
        [AuroraExport("proxy", MatchFailure.Throw)]
        public static ScriptProxy ProxyCore(ScriptProxy value) => value;

        /// <summary>Returns a Regex unchanged.</summary>
        [AuroraExport("regex", MatchFailure.Throw)]
        public static ScriptRegex RegexCore(ScriptRegex value) => value;

        /// <summary>Returns a Date unchanged.</summary>
        [AuroraExport("date", MatchFailure.Throw)]
        public static ScriptDate DateCore(ScriptDate value) => value;

        /// <summary>Returns a HashMap unchanged.</summary>
        [AuroraExport("hashMap", MatchFailure.Throw)]
        public static ScriptHashMap HashMapCore(ScriptHashMap value) => value;

        /// <summary>Returns an Error unchanged.</summary>
        [AuroraExport("error", MatchFailure.Throw)]
        public static ScriptError ErrorCore(ScriptError value) => value;

        /// <summary>Returns a script function unchanged.</summary>
        [AuroraExport("function", MatchFailure.Throw)]
        public static ClosureFunction FunctionCore(ClosureFunction value) => value;

        /// <summary>Returns a boxed immutable primitive unchanged.</summary>
        [AuroraExport("immutable", MatchFailure.Throw)]
        public static ScriptImmutable ImmutableCore(ScriptImmutable value) => value;

        /// <summary>Returns the null object wrapper unchanged.</summary>
        [AuroraExport("nullValue", MatchFailure.Throw)]
        public static NullValue NullValueCore(NullValue value) => value;
    }
}
