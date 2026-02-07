using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AuroraScript.Runtime.Extensions
{
    internal class ConsoleSupport : ScriptObject
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly Dictionary<string, long> _times = new();

        public ConsoleSupport()
        {
            Define("log", new BondingFunction(LOG), writeable: false, enumerable: false);
            Define("error", new BondingFunction(ERROR), writeable: false, enumerable: false);
            Define("time", new BondingFunction(TIME), writeable: false, enumerable: false);
            Define("timeEnd", new BondingFunction(TIMEEND), writeable: false, enumerable: false);
        }

        public static void LOG(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length > 0)
            {
                ctx.Domain.Engine.Options.ConsoleStdOut?.WriteLine(String.Join(", ", args.ToArray().Select(e => DatumToString(ctx, e))));
            }
        }


        public static void ERROR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length > 0)
            {
                ctx.Engine.Options.ConsoleErrorOut?.WriteLine(String.Join(", ", args.ToArray().Select(e => DatumToString(ctx, e))));
            }
        }


        private static String DatumToString(ScriptContext ctx, ScriptDatum datum)
        {
            if (ScriptDatum.TryGetError(in datum, out var error))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Error: ");
                sb.AppendLine(error.Message);
                foreach (var frame in error.StackTrace)
                {
                    sb.AppendLine(frame.ToString());
                }
                return sb.ToString();
            }

            if (datum.Kind.Include(ValueKind.Object))
            {
                var jsonDocument = ctx.Engine.Options.JsonSerializer.Serialize(datum, ctx.Engine.Options, false);
                return jsonDocument;
            }
            return ScriptDatum.ToString(datum);
        }


        public static void TIME(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetString(0, out var label))
            {
                var console = (ConsoleSupport)thisObject;
                console._times[label] = console._stopwatch.ElapsedMilliseconds;
            }
        }

        public static void TIMEEND(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var console = (ConsoleSupport)thisObject;
            if (args.TryGetString(0, out var label) && console._times.TryGetValue(label, out var start))
            {

                var elapsed = console._stopwatch.ElapsedMilliseconds - start;
                console._times.Remove(label);
                ctx.Engine.Options.ConsoleStdOut?.WriteLine($"{label} Used {elapsed}ms");
            }
        }
    }
}

