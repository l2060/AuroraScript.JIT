using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace AuroraScript.Runtime.Builtin
{

    /// <summary>
    /// Script console Type implemented through generated native exports.
    /// </summary>
    [AuroraNativeType("console")]
    public sealed partial class ConsoleSupport : ScriptObject
    {
        private sealed class TimerState
        {
            internal readonly Stopwatch Stopwatch = Stopwatch.StartNew();
            internal readonly Dictionary<string, long> Times = new();
        }

        private static readonly ConditionalWeakTable<AuroraEngine, TimerState> TimerStates = new();

        /// <summary>Writes one value to standard output.</summary>
        [AuroraExport("log", MatchFailure.Throw, DynamicAdapter = nameof(LOG))]
        public static void LogCore(ScriptContext ctx, ScriptDatum value)
        {
            ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine(DatumToString(ctx, value));
        }

        private static void LOG(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length > 0)
            {
                ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine(FormatArguments(ctx, args));
            }
        }

        /// <summary>Writes one value to standard error.</summary>
        [AuroraExport("error", MatchFailure.Throw, DynamicAdapter = nameof(ERROR))]
        public static void ErrorCore(ScriptContext ctx, ScriptDatum value)
        {
            ctx.Engine.Options.Runtime.ConsoleErrorOut?.WriteLine(DatumToString(ctx, value));
        }

        private static void ERROR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length > 0)
            {
                ctx.Engine.Options.Runtime.ConsoleErrorOut?.WriteLine(FormatArguments(ctx, args));
            }
        }

        private static string FormatArguments(ScriptContext ctx, ReadOnlySpan<ScriptDatum> args)
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

        private static string DatumToString(ScriptContext ctx, ScriptDatum datum)
        {
            if (ScriptDatum.TryGetError(in datum, out var error))
            {
                var sb = new StringBuilder();
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
                var jsonDocument = ctx.Engine.Options.Runtime.JsonSerializer.Serialize(datum, ctx.Engine.Options, false);
                return jsonDocument;
            }
            return ScriptDatum.ToString(datum);
        }

        /// <summary>Starts or resets a named timer.</summary>
        [AuroraExport("time", MatchFailure.Throw, DynamicAdapter = nameof(TIME))]
        public static void TimeCore(ScriptContext ctx, string label)
        {
            var state = TimerStates.GetValue(ctx.Engine, static _ => new TimerState());
            lock (state.Times)
            {
                state.Times[label] = state.Stopwatch.ElapsedMilliseconds;
            }
        }

        private static void TIME(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length > 0 && ScriptDatum.TryGetString(in args[0], out var label))
            {
                TimeCore(ctx, label.Value);
            }
        }

        /// <summary>Stops a named timer and writes its elapsed time.</summary>
        [AuroraExport("timeEnd", MatchFailure.Throw, DynamicAdapter = nameof(TIME_END))]
        public static void TimeEndCore(ScriptContext ctx, string label)
        {
            var state = TimerStates.GetValue(ctx.Engine, static _ => new TimerState());
            lock (state.Times)
            {
                if (!state.Times.TryGetValue(label, out var start)) return;
                var elapsed = state.Stopwatch.ElapsedMilliseconds - start;
                state.Times.Remove(label);
                ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine($"{label} Used {elapsed}ms");
            }
        }

        private static void TIME_END(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length > 0 && ScriptDatum.TryGetString(in args[0], out var label))
            {
                TimeEndCore(ctx, label.Value);
            }
        }
    }
}
