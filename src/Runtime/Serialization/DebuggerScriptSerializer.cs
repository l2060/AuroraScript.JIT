using AuroraScript.Runtime.Types;
using System.Text.Json;

namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Serializer specialized for debugger use.
    /// Extends <see cref="ScriptJsonSerializer"/> to provide rich metadata for script objects,
    /// such as entry points, source file locations, and line/column information for functions.
    /// </summary>
    public class DebuggerScriptSerializer : ScriptJsonSerializer
    {
        /// <summary>
        /// Writes a closure function with additional metadata for debugging.
        /// Currently, this writes an empty object placeholder, but is designed to 
        /// include detailed source and location information.
        /// </summary>
        protected override void WriteClosure(Utf8JsonWriter writer, ClosureFunction closure, in ScriptSerializationContext context)
        {
            writer.WriteStartObject();
            // NOTE: Detailed debugger metadata is currently disabled to prevent overhead during routine inspection.
            // Future enhancements may re-enable source location and entry point reporting here.
            writer.WriteEndObject();
        }
    }
}
