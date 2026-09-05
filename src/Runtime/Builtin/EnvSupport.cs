using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System.Diagnostics;

namespace AuroraScript.Runtime.Builtin
{

    /// <summary>Provides monotonic elapsed time since the clock was initialized.</summary>
    [AuroraNativeType("Env")]
    public sealed partial class EnvSupport : ScriptObject
    {
        private static readonly long PerfClockStart = Stopwatch.GetTimestamp();

        /// <summary>Returns elapsed 100-nanosecond ticks.</summary>
        [AuroraExport("ticks")]
        public static long Ticks() => Stopwatch.GetElapsedTime(PerfClockStart).Ticks;

        /// <summary>Returns elapsed milliseconds, including the fractional part.</summary>
        [AuroraExport("elapsedMs")]
        public static double ElapsedMs() => Stopwatch.GetElapsedTime(PerfClockStart).TotalMilliseconds;



    }
}
