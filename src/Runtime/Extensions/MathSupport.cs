using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Runtime.Extensions
{
    /// <summary>
    /// Script Math global implemented through generated native exports.
    /// </summary>
    [AuroraNativeModule("Math")]
    public sealed partial class MathSupport : ScriptObject
    {
        /// <summary>Circle constant pi.</summary>
        [AuroraExport("PI")]
        public static readonly double PI = Math.PI;

        /// <summary>Euler number constant.</summary>
        [AuroraExport("E")]
        public static readonly double E = Math.E;

        /// <summary>Circle constant tau, equal to 2*pi.</summary>
        [AuroraExport("Tau")]
        public static readonly double Tau = Math.Tau;

        /// <summary>Degrees per radian conversion constant.</summary>
        [AuroraExport("DEG_PER_RAD")]
        public static readonly double DEG_PER_RAD = Math.PI / 180D;

        /// <summary>Returns the absolute value.</summary>
        [AuroraExport("abs", MatchFailure.ReturnNaN)]
        public static double AbsCore(double value) => Math.Abs(value);

        /// <summary>Returns the largest argument.</summary>
        [AuroraExport("max", MatchFailure.ReturnNaN)]
        public static double MaxCore(params double[] values)
        {
            var max = values[0];
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i] > max)
                {
                    max = values[i];
                }
            }

            return max;
        }

        /// <summary>Returns the smallest argument.</summary>
        [AuroraExport("min", MatchFailure.ReturnNaN)]
        public static double MinCore(params double[] values)
        {
            var min = values[0];
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i] < min)
                {
                    min = values[i];
                }
            }

            return min;
        }

        /// <summary>Returns a pseudo-random number.</summary>
        [AuroraExport("random", MatchFailure.ReturnNaN)]
        public static double RandomCore() => Random.Shared.NextDouble();

        /// <summary>Returns the natural logarithm.</summary>
        [AuroraExport("log", MatchFailure.ReturnNaN)]
        public static double LogCore(double value) => Math.Log(value);

        /// <summary>Returns a number raised to a power.</summary>
        [AuroraExport("pow", MatchFailure.ReturnNaN)]
        public static double PowCore(double x, double y) => Math.Pow(x, y);

        /// <summary>Returns e raised to a power.</summary>
        [AuroraExport("exp", MatchFailure.ReturnNaN)]
        public static double ExpCore(double value) => Math.Exp(value);

        /// <summary>Returns the cosine.</summary>
        [AuroraExport("cos", MatchFailure.ReturnNaN)]
        public static double CosCore(double value) => Math.Cos(value);

        /// <summary>Returns the sine.</summary>
        [AuroraExport("sin", MatchFailure.ReturnNaN)]
        public static double SinCore(double value) => Math.Sin(value);

        /// <summary>Returns the tangent.</summary>
        [AuroraExport("tan", MatchFailure.ReturnNaN)]
        public static double TanCore(double value) => Math.Tan(value);

        /// <summary>Returns the arccosine.</summary>
        [AuroraExport("acos", MatchFailure.ReturnNaN)]
        public static double AcosCore(double value) => Math.Acos(value);

        /// <summary>Returns the arcsine.</summary>
        [AuroraExport("asin", MatchFailure.ReturnNaN)]
        public static double AsinCore(double value) => Math.Asin(value);

        /// <summary>Returns the arctangent.</summary>
        [AuroraExport("atan", MatchFailure.ReturnNaN)]
        public static double AtanCore(double value) => Math.Atan(value);

        /// <summary>Returns the smallest integer greater than or equal to the value.</summary>
        [AuroraExport("ceil", MatchFailure.ReturnNaN)]
        public static double CeilCore(double value) => Math.Ceiling(value);

        /// <summary>Returns the largest integer less than or equal to the value.</summary>
        [AuroraExport("floor", MatchFailure.ReturnNaN)]
        public static double FloorCore(double value) => Math.Floor(value);

        /// <summary>Returns the nearest integer.</summary>
        [AuroraExport("round", MatchFailure.ReturnNaN)]
        public static double RoundCore(double value) => Math.Round(value);
    }
}
