using AuroraScript.Runtime.Pool;
using System;

namespace AuroraScript.Runtime.Types.TypeConstruct
{
    /// <summary>
    /// Represents the native 'Regex' constructor function in AuroraScript.
    /// Used for creating regular expression objects from patterns and flags.
    /// </summary>
    internal class ScriptRegexConstructor : ScriptType
    {
        /// <summary> The global singleton instance of the Regex constructor. </summary>
        internal static ScriptRegexConstructor INSTANCE = new ScriptRegexConstructor();

        internal ScriptRegexConstructor() : base("Regex")
        {

        }

        /// <summary>
        /// Native implementation for constructing a new Regex object.
        /// Handles pattern strings and optional flags.
        /// </summary>
        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)
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
