using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Runtime.Extensions
{
    internal class MathSupport : ScriptObject
    {
        public MathSupport()
        {
            Define("PI", ScriptDatum.FromNumber(Math.PI), writeable: false, enumerable: false);
            Define("E", ScriptDatum.FromNumber(Math.E), writeable: false, enumerable: false);
            Define("Tau", ScriptDatum.FromNumber(Math.Tau), writeable: false, enumerable: false);
            Define("DEG_PER_RAD", ScriptDatum.FromNumber(Math.PI / 180D), writeable: false, enumerable: false);





            Define("abs", ScriptDatum.FromBonding(ABS), writeable: false, enumerable: false);
            Define("max", ScriptDatum.FromBonding(MAX), writeable: false, enumerable: false);
            Define("min", ScriptDatum.FromBonding(MIN), writeable: false, enumerable: false);

            Define("random", ScriptDatum.FromBonding(RANDOM), writeable: false, enumerable: false);
            Define("log", ScriptDatum.FromBonding(LOG), writeable: false, enumerable: false);
            Define("pow", ScriptDatum.FromBonding(POW), writeable: false, enumerable: false);
            Define("exp", ScriptDatum.FromBonding(EXP), writeable: false, enumerable: false);

            Define("cos", ScriptDatum.FromBonding(COS), writeable: false, enumerable: false);
            Define("sin", ScriptDatum.FromBonding(SIN), writeable: false, enumerable: false);
            Define("tan", ScriptDatum.FromBonding(TAN), writeable: false, enumerable: false);
            Define("acos", ScriptDatum.FromBonding(ACOS), writeable: false, enumerable: false);
            Define("asin", ScriptDatum.FromBonding(ASIN), writeable: false, enumerable: false);
            Define("atan", ScriptDatum.FromBonding(ATAN), writeable: false, enumerable: false);



            Define("floor", ScriptDatum.FromBonding(FLOOR), writeable: false, enumerable: false);
            Define("round", ScriptDatum.FromBonding(ROUND), writeable: false, enumerable: false);

        }


        public static void FLOOR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                ScriptDatum.WriteAsNumber(ref result, Math.Floor(num));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }

        public static void ROUND(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                ScriptDatum.WriteAsNumber(ref result, Math.Round(num));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }




        #region MyRegion

        public static void COS(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                ScriptDatum.WriteAsNumber(ref result, Math.Cos(num));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }
        public static void ACOS(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                ScriptDatum.WriteAsNumber(ref result, Math.Acos(num));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }

        public static void SIN(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                ScriptDatum.WriteAsNumber(ref result, Math.Sin(num));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }

        public static void ASIN(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                ScriptDatum.WriteAsNumber(ref result, Math.Asin(num));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }

        public static void TAN(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                ScriptDatum.WriteAsNumber(ref result, Math.Tan(num));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }

        public static void ATAN(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                ScriptDatum.WriteAsNumber(ref result, Math.Atan(num));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }





        #endregion






        public static void RANDOM(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsNumber(ref result, Random.Shared.NextDouble());
        }


        public static void POW(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num1) && args.TryGetNumber(1, out var num2))
            {
                ScriptDatum.WriteAsNumber(ref result, Math.Pow(num1, num2));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }

        public static void EXP(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                ScriptDatum.WriteAsNumber(ref result, Math.Exp(num));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }

        public static void LOG(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                ScriptDatum.WriteAsNumber(ref result, Math.Log(num));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }

        public static void ABS(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                ScriptDatum.WriteAsNumber(ref result, num < 0 ? -num : num);
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }


        public static void MAX(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                var index = 1;
                while (args.TryGetNumber(index++, out var num2))
                {
                    if (num2 > num) num = num2;
                }
                ScriptDatum.WriteAsNumber(ref result, num);
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }

        public static void MIN(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var num))
            {
                var index = 1;
                while (args.TryGetNumber(index++, out var num2))
                {
                    if (num2 < num) num = num2;
                }
                ScriptDatum.WriteAsNumber(ref result, num);
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, Double.NaN);
            }
        }


    }
}
