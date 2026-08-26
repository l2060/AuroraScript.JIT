using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Runtime.Extensions
{
    /// <summary>
    /// Experimental script global used to validate <see cref="AuroraExportAttribute"/> source generation.
    /// </summary>
    [AuroraBuiltinGlobal("Stats")]
    public sealed partial class StatsSupport : ScriptObject
    {
        /// <summary>Creates the generated Stats global object.</summary>
        public StatsSupport()
        {
            RegisterAuroraExports();
        }

        /// <summary>Returns the arithmetic mean of two numbers.</summary>
        [AuroraExport("mean", Failure = AuroraExportFailure.ReturnNaN)]
        public static double MeanCore(
            [AuroraParam(Coercion = AuroraParamCoercion.Weak)] double a,
            [AuroraParam(Coercion = AuroraParamCoercion.Weak)] double b)
            => (a + b) / 2D;

        /// <summary>Returns the sum of two exact numbers.</summary>
        [AuroraExport("sumExact", Failure = AuroraExportFailure.Throw)]
        public static double SumExactCore(
            [AuroraParam(Coercion = AuroraParamCoercion.Exact)] double a,
            [AuroraParam(Coercion = AuroraParamCoercion.Exact)] double b)
            => a + b;

        /// <summary>Returns an exact object argument unchanged.</summary>
        [AuroraExport("identity", Failure = AuroraExportFailure.Throw)]
        public static ScriptObject IdentityCore(
            [AuroraParam(Coercion = AuroraParamCoercion.Exact)] ScriptObject value)
            => value;


        /// <summary>Concatenates a string and an exact Int32 value.</summary>
        [AuroraExport("chat", Failure = AuroraExportFailure.Throw)]
        public static string Chat(
            [AuroraParam(Coercion = AuroraParamCoercion.Weak)] string value,
            [AuroraParam(Coercion = AuroraParamCoercion.Weak)] Double pice)
            => value + pice;
    }
}
