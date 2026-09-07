using AuroraScript.Runtime.Pool;
using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents the native 'Regex' constructor function in AuroraScript.
    /// Used for creating regular expression objects from patterns and flags.
    /// </summary>
    public sealed partial class ScriptRegex
    {
        /// <summary>
        /// Native implementation for constructing a new Regex object.
        /// Handles pattern strings and optional flags.
        /// </summary>
        internal static void CREATE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length == 0)
            {
                throw new AuroraRuntimeException("A pattern must be specified for Regex constructor.");
            }

            var flags = "";
            var pattern = "";

            if (args.Length == 1)
            {
                if (args[0].Kind == ValueKind.String)
                {
                    pattern = args[0].StringText;
                }
                else if (args[0].Kind == ValueKind.Regex)
                {
                    // If first arg is already a regex, return it as is (JS behavior).
                    result = args[0];
                    return;
                }
            }
            if (args.Length == 2 && args[1].Kind == ValueKind.String)
            {
                flags = args[1].StringText;
            }
            ScriptDatum.WriteAsRegex(ref result, RegexManager.Resolve(pattern, flags));
        }
    }
}
