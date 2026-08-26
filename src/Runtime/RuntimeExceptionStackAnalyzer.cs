using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AuroraScript.Runtime
{
    internal static class RuntimeExceptionStackAnalyzer
    {
        private const string NativeSuffix = "$native";

        public static AuroraStackTrace[] MergeNativeFrames(
            Exception exception,
            IReadOnlyList<AuroraStackTrace> scriptTrace)
        {
            ArgumentNullException.ThrowIfNull(exception);

            var frames = new StackTrace(exception, false).GetFrames();
            if (frames == null || frames.Length == 0)
            {
                return Copy(scriptTrace);
            }

            var result = new List<AuroraStackTrace>(frames.Length + (scriptTrace?.Count ?? 0));
            var fallbackPath = scriptTrace != null && scriptTrace.Count > 0
                ? scriptTrace[0].FullPath
                : null;
            var fallbackLine = scriptTrace != null && scriptTrace.Count > 0
                ? scriptTrace[0].Line
                : 0;

            for (var i = 0; i < frames.Length; i++)
            {
                var methodName = frames[i].GetMethod()?.Name;
                if (string.IsNullOrEmpty(methodName) ||
                    !methodName.EndsWith(NativeSuffix, StringComparison.Ordinal))
                {
                    continue;
                }

                var scriptName = methodName[..^NativeSuffix.Length];
                if (ContainsMethod(result, scriptName))
                {
                    continue;
                }

                var existing = FindMethod(scriptTrace, scriptName);
                result.Add(existing ?? new AuroraStackTrace(
                    fallbackPath,
                    scriptName,
                    result.Count == 0 ? fallbackLine : 0));
            }

            if (scriptTrace != null)
            {
                for (var i = 0; i < scriptTrace.Count; i++)
                {
                    var frame = scriptTrace[i];
                    if (!ContainsMethod(result, frame.Method))
                    {
                        result.Add(frame);
                    }
                }
            }

            return result.ToArray();
        }

        private static AuroraStackTrace FindMethod(
            IReadOnlyList<AuroraStackTrace> trace,
            string method)
        {
            if (trace == null)
            {
                return null;
            }

            for (var i = 0; i < trace.Count; i++)
            {
                if (StringComparer.Ordinal.Equals(trace[i].Method, method))
                {
                    return trace[i];
                }
            }
            return null;
        }

        private static bool ContainsMethod(
            List<AuroraStackTrace> trace,
            string method)
        {
            for (var i = 0; i < trace.Count; i++)
            {
                if (StringComparer.Ordinal.Equals(trace[i].Method, method))
                {
                    return true;
                }
            }
            return false;
        }

        private static AuroraStackTrace[] Copy(
            IReadOnlyList<AuroraStackTrace> trace)
        {
            if (trace == null || trace.Count == 0)
            {
                return Array.Empty<AuroraStackTrace>();
            }

            var result = new AuroraStackTrace[trace.Count];
            for (var i = 0; i < trace.Count; i++)
            {
                result[i] = trace[i];
            }
            return result;
        }
    }
}
