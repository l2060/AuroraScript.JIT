using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;

namespace AuroraScript.Tests.Host;

/// <summary>
/// Script global used to validate <see cref="ExportAttribute"/> source generation.
/// </summary>
[NativeType("Stats")]
public sealed partial class StatsSupport : ScriptObject
{
    /// <summary>Returns the arithmetic mean of two numbers.</summary>
    [Export("mean", MatchFailure.ReturnNaN)]
    public static double MeanCore(double a, double b) => (a + b) / 2D;

    /// <summary>Returns the sum of two exact numbers.</summary>
    [Export("sumExact", MatchFailure.Throw)]
    public static double SumExactCore(
        [AuroraParam(MatchLevel.Exact)] double a,
        [AuroraParam(MatchLevel.Exact)] double b)
        => a + b;

    /// <summary>Returns an exact object argument unchanged.</summary>
    [Export("identity", MatchFailure.Throw)]
    public static ScriptObject IdentityCore(
        [AuroraParam(MatchLevel.Exact)] ScriptObject value)
        => value;

    /// <summary>Concatenates a string and an exact Int32 value.</summary>
    [Export("chat", MatchFailure.Throw)]
    public static string Chat(
        string value,
        [AuroraParam(MatchLevel.Exact)] int piece)
        => value + piece;

    /// <summary>Returns a script value unchanged.</summary>
    [Export("echo", MatchFailure.Throw)]
    public static ScriptDatum EchoCore(ScriptDatum value) => value;

    /// <summary>Returns whether the call has an engine-bound context.</summary>
    [Export("hasEngine", MatchFailure.Throw)]
    public static bool HasEngineCore(ScriptContext ctx) => ctx?.Engine != null;

    /// <summary>Returns whether the argument is the same object as this.</summary>
    [Export("sameThis", MatchFailure.Throw)]
    public static bool SameThisCore(
        ScriptContext ctx,
        ScriptObject thisObject,
        ScriptObject other)
        => ReferenceEquals(thisObject, other);

    /// <summary>Counts arbitrary trailing script values.</summary>
    [Export("restCount", MatchFailure.Throw)]
    public static double RestCountCore(params ScriptDatum[] values)
        => values.Length;

    /// <summary>Combines a fixed prefix with an arbitrary script-value tail.</summary>
    [Export("restAfter", MatchFailure.Throw)]
    public static double RestAfterCore(
        double first,
        params ScriptDatum[] values)
        => first + values.Length;

    /// <summary>Returns a script array unchanged.</summary>
    [Export("array", MatchFailure.Throw)]
    public static ScriptArray ArrayCore(ScriptArray value) => value;

    /// <summary>Returns a packed array unchanged.</summary>
    [Export("packed", MatchFailure.Throw)]
    public static ScriptPackedArray PackedCore(ScriptPackedArray value) => value;

    /// <summary>Returns a concrete packed array unchanged.</summary>
    [Export("int8Array", MatchFailure.Throw)]
    public static ScriptInt8Array Int8ArrayCore(ScriptInt8Array value) => value;

    /// <summary>Returns a Path unchanged.</summary>
    [Export("path", MatchFailure.Throw)]
    public static ScriptPathValue PathCore(ScriptPathValue value) => value;

    /// <summary>Returns a StringBuffer unchanged.</summary>
    [Export("stringBuffer", MatchFailure.Throw)]
    public static StringBuffer StringBufferCore(StringBuffer value) => value;

    /// <summary>Returns a Proxy unchanged.</summary>
    [Export("proxy", MatchFailure.Throw)]
    public static ScriptProxy ProxyCore(ScriptProxy value) => value;

    /// <summary>Returns a Regex unchanged.</summary>
    [Export("regex", MatchFailure.Throw)]
    public static ScriptRegex RegexCore(ScriptRegex value) => value;

    /// <summary>Returns a Date unchanged.</summary>
    [Export("date", MatchFailure.Throw)]
    public static ScriptDate DateCore(ScriptDate value) => value;

    /// <summary>Returns a HashMap unchanged.</summary>
    [Export("hashMap", MatchFailure.Throw)]
    public static ScriptHashMap HashMapCore(ScriptHashMap value) => value;

    /// <summary>Returns an Error unchanged.</summary>
    [Export("error", MatchFailure.Throw)]
    public static ScriptError ErrorCore(ScriptError value) => value;

    /// <summary>Returns a script function unchanged.</summary>
    [Export("function", MatchFailure.Throw)]
    public static ClosureFunction FunctionCore(ClosureFunction value) => value;

    /// <summary>Returns a boxed immutable primitive unchanged.</summary>
    [Export("immutable", MatchFailure.Throw)]
    public static ScriptImmutable ImmutableCore(ScriptImmutable value) => value;

    /// <summary>Returns the null object wrapper unchanged.</summary>
    [Export("nullValue", MatchFailure.Throw)]
    public static NullValue NullValueCore(NullValue value) => value;
}
