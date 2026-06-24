using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AuroraScript.Runtime.Extensions
{
    internal class ConsoleSupport : ScriptObject
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly Dictionary<string, long> _times = new();

        public ConsoleSupport()
        {
            Define("log", ScriptDatum.FromBonding(LOG), writeable: false, enumerable: false);
            Define("error", ScriptDatum.FromBonding(ERROR), writeable: false, enumerable: false);
            Define("time", ScriptDatum.FromBonding(TIME), writeable: false, enumerable: false);
            Define("timeEnd", ScriptDatum.FromBonding(TIMEEND), writeable: false, enumerable: false);
        }

        public static void LOG(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length > 0)
            {
                ctx.Domain.Engine.Options.ConsoleStdOut?.WriteLine(FormatArguments(ctx, args));
            }
        }


        public static void ERROR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length > 0)
            {
                ctx.Engine.Options.ConsoleErrorOut?.WriteLine(FormatArguments(ctx, args));
            }
        }

        private static string FormatArguments(ScriptContext ctx, Span<ScriptDatum> args)
        {
            if (args.Length == 1)
            {
                return DatumToString(ctx, args[0]);
            }

            var builder = new StringBuilder();
            for (var i = 0; i < args.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(DatumToString(ctx, args[i]));
            }
            return builder.ToString();
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

