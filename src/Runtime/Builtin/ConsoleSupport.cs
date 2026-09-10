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
    [NativeType("console")]
    public sealed partial class ConsoleSupport : ScriptObject
    {
        private sealed class TimerState
        {
            internal readonly Stopwatch Stopwatch = Stopwatch.StartNew();
            internal readonly Dictionary<string, long> Times = new();
        }

        private static readonly ConditionalWeakTable<AuroraEngine, TimerState> TimerStates = new();

        /// <summary>Writes a native string using script formatting.</summary>
        [Export("log", MatchFailure.Throw, DynamicAdapter = nameof(LOG))]
        public static void LogCore(ScriptContext ctx, string value) => ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine(value ?? "null");

        /// <summary>Writes a native int using script formatting.</summary>
        [Export("log", MatchFailure.Throw, DynamicAdapter = nameof(LOG))]
        public static void LogCore(ScriptContext ctx, int value) => ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine(value.ToString(System.Globalization.CultureInfo.CurrentCulture));

        /// <summary>Writes a native double using script formatting.</summary>
        [Export("log", MatchFailure.Throw, DynamicAdapter = nameof(LOG))]
        public static void LogCore(ScriptContext ctx, double value) => ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine(value.ToString(System.Globalization.CultureInfo.CurrentCulture));

        /// <summary>Writes a native bool using script formatting.</summary>
        [Export("log", MatchFailure.Throw, DynamicAdapter = nameof(LOG))]
        public static void LogCore(ScriptContext ctx, bool value) => ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine(value.ToString());

        /// <summary>Writes a native long using script formatting.</summary>
        [Export("log", MatchFailure.Throw, DynamicAdapter = nameof(LOG))]
        public static void LogCore(ScriptContext ctx, long value) => ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>Writes a native ulong using script formatting.</summary>
        [Export("log", MatchFailure.Throw, DynamicAdapter = nameof(LOG))]
        public static void LogCore(ScriptContext ctx, ulong value) => ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>Writes a native ulong using script formatting.</summary>
        [Export("log", MatchFailure.Throw, DynamicAdapter = nameof(LOG))]
        public static void LogCore(ScriptContext ctx, ScriptDatum value1) => ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine(value1.ToString());


        /// <summary>Writes a native ulong using script formatting.</summary>
        [Export("log", MatchFailure.Throw, DynamicAdapter = nameof(LOG))]
        public static void LogCore(ScriptContext ctx, ScriptDatum value1, ScriptDatum value2) => ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine(FormatArguments(ctx, [value1, value2]));





        /// <summary>Writes a native string using script formatting.</summary>
        [Export("error", MatchFailure.Throw, DynamicAdapter = nameof(ERROR))]
        public static void ErrorCore(ScriptContext ctx, string value) =>
            ctx.Engine.Options.Runtime.ConsoleErrorOut?.WriteLine(value ?? "null");

        /// <summary>Writes a native int using script formatting.</summary>
        [Export("error", MatchFailure.Throw, DynamicAdapter = nameof(ERROR))]
        public static void ErrorCore(ScriptContext ctx, int value) =>
            ctx.Engine.Options.Runtime.ConsoleErrorOut?.WriteLine(value.ToString(System.Globalization.CultureInfo.CurrentCulture));

        /// <summary>Writes a native double using script formatting.</summary>
        [Export("error", MatchFailure.Throw, DynamicAdapter = nameof(ERROR))]
        public static void ErrorCore(ScriptContext ctx, double value) =>
            ctx.Engine.Options.Runtime.ConsoleErrorOut?.WriteLine(value.ToString(System.Globalization.CultureInfo.CurrentCulture));

        /// <summary>Writes a native bool using script formatting.</summary>
        [Export("error", MatchFailure.Throw, DynamicAdapter = nameof(ERROR))]
        public static void ErrorCore(ScriptContext ctx, bool value) =>
            ctx.Engine.Options.Runtime.ConsoleErrorOut?.WriteLine(value.ToString());

        /// <summary>Writes a native long using script formatting.</summary>
        [Export("error", MatchFailure.Throw, DynamicAdapter = nameof(ERROR))]
        public static void ErrorCore(ScriptContext ctx, long value) =>
            ctx.Engine.Options.Runtime.ConsoleErrorOut?.WriteLine(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>Writes a native ulong using script formatting.</summary>
        [Export("error", MatchFailure.Throw, DynamicAdapter = nameof(ERROR))]
        public static void ErrorCore(ScriptContext ctx, ulong value) =>
            ctx.Engine.Options.Runtime.ConsoleErrorOut?.WriteLine(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        private static void LOG(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length > 0)
            {
                ctx.Engine.Options.Runtime.ConsoleStdOut?.WriteLine(FormatArguments(ctx, args));
            }
        }

        private static void ERROR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length > 0)
            {
                ctx.Engine.Options.Runtime.ConsoleErrorOut?.WriteLine(FormatArguments(ctx, args));
            }
        }

        /// <summary>Preserves the empty-call no-op behavior.</summary>
        [Export("log", MatchFailure.Throw, DynamicAdapter = nameof(LOG))]
        public static void LogCore(ScriptContext ctx) { }

        /// <summary>Preserves the empty-call no-op behavior.</summary>
        [Export("error", MatchFailure.Throw, DynamicAdapter = nameof(ERROR))]
        public static void ErrorCore(ScriptContext ctx) { }

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
        [Export("time", MatchFailure.Throw, DynamicAdapter = nameof(TIME))]
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
        [Export("timeEnd", MatchFailure.Throw, DynamicAdapter = nameof(TIME_END))]
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
