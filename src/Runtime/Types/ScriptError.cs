using System;

using AuroraScript.Hosting;

namespace AuroraScript.Runtime.Types
{


    /// <summary>
    /// Represents a script-level error object, including a message and a stack trace.
    /// This is the script-side representation of an exception.
    /// </summary>
    [NativeType("Error")]
    public partial class ScriptError : ScriptObject
    {
        internal static void CREATE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetString(0, out var message))
                ScriptDatum.WriteAsError(ref result, new ScriptError(message, ctx.CallStack()));
        }

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Error;

        [Export(DynamicAdapter = nameof(CREATE))]
        internal ScriptError(string errMsg, AuroraStackTrace[] stackTrace)
        {
            Message = errMsg;
            StackTrace = stackTrace;
            InternalDefine("message", ScriptDatum.FromString(errMsg), writeable: false, enumerable: true);
        }

        /// <summary>
        /// The error message describing the error.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The stack trace at the point where the error was created.
        /// </summary>
        public AuroraStackTrace[] StackTrace { get; }

        /// <summary>Returns the script error name and message.</summary>
        public override string ToString()
        {
            return $"Error: {Message}";
        }

    }
}
