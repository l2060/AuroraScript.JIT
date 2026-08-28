using System;

namespace AuroraScript.Runtime.Types
{


    internal class ScriptErrorConstructor : ScriptType
    {
        internal readonly static ScriptErrorConstructor INSTANCE = new ScriptErrorConstructor();

        internal ScriptErrorConstructor() : base("Error")
        {

        }

        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetString(0, out var errString))
            {
                ScriptError error = new ScriptError(errString, ctx.CallStack());
                ScriptDatum.WriteAsError(ref result, error);
            }
        }
    }



    /// <summary>
    /// Represents a script-level error object, including a message and a stack trace.
    /// This is the script-side representation of an exception.
    /// </summary>
    public class ScriptError : ScriptObject
    {
        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Error;

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
