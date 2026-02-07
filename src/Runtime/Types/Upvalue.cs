namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a captured closure variable (an "Upvalue").
    /// This is used to share variables across different lexical scopes or closures.
    /// </summary>
    internal sealed class Upvalue
    {
        /// <summary>
        /// The current value of the captured variable.
        /// </summary>
        public ScriptDatum Value;
    }
}
