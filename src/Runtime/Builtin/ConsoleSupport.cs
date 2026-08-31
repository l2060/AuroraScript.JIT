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
    internal sealed partial class ConsoleSupport : ScriptObject
    {
        private sealed class TimerState
        {
            internal readonly Stopwatch Stopwatch = Stopwatch.StartNew();
            internal readonly Dictionary<string, long> Times = new();
        }

        private static readonly ConditionalWeakTable<AuroraEngine, TimerState> TimerStates = new();

        [AuroraExport("log", MatchFailure.Throw)]
        public static void LogCore(ScriptContext ctx, params ScriptDatum[] args)
        {
            if (args.Length > 0)
            {
                ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine(FormatArguments(ctx, args));
            }
        }

        [AuroraExport("error", MatchFailure.Throw)]
        public static void ErrorCore(ScriptContext ctx, params ScriptDatum[] args)
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

        [AuroraExport("time", MatchFailure.Throw)]
        public static void TimeCore(ScriptContext ctx, params ScriptDatum[] args)
        {
            if (args.Length > 0 &&
                ScriptDatum.TryGetString(in args[0], out var label))
            {
                var state = TimerStates.GetValue(ctx.Engine, static _ => new TimerState());
                lock (state.Times)
                {
                    state.Times[label.Value] = state.Stopwatch.ElapsedMilliseconds;
                }
            }
        }

        [AuroraExport("timeEnd", MatchFailure.Throw)]
        public static void TimeEndCore(ScriptContext ctx, params ScriptDatum[] args)
        {
            if (args.Length == 0 ||
                !ScriptDatum.TryGetString(in args[0], out var label))
            {
                return;
            }

            var state = TimerStates.GetValue(ctx.Engine, static _ => new TimerState());
            lock (state.Times)
            {
                if (!state.Times.TryGetValue(label.Value, out var start))
                {
                    return;
                }
                var elapsed = state.Stopwatch.ElapsedMilliseconds - start;
                state.Times.Remove(label.Value);
                ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine($"{label} Used {elapsed}ms");
            }
        }
    }
}
