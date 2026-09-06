using AuroraScript.Hosting;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using System.Text.Json;

namespace AuroraScript.Runtime.Builtin
{
    /// <summary>Exposes JSON serialization through generated native exports.</summary>
    [AuroraNativeType("JSON")]
    public sealed partial class JsonSupport : ScriptObject
    {
        /// <summary>Deserializes JSON text.</summary>
        [AuroraExport("parse", MatchFailure.Throw)]
        public static ScriptDatum ParseCore(ScriptContext ctx, string text)
        {
            try
            {
                var serializer = ctx.Engine.Options.Runtime.JsonSerializer;
                return ScriptDatum.FromObject(serializer.Deserialize(text));
            }
            catch (JsonException ex)
            {
                throw new AuroraRuntimeException($"JSON.parse error: {ex.Message}");
            }
        }

        /// <summary>Serializes a script value as JSON text.</summary>
        [AuroraExport("stringify", MatchFailure.Throw)]
        public static string StringifyCore(ScriptContext ctx, ScriptDatum value, bool indented = false)
        {
            try
            {
                var serializer = ctx.Engine.Options.Runtime.JsonSerializer;
                return serializer.Serialize(value, ctx.Engine.Options, indented);
            }
            catch (TypedDocumentException exception)
            {
                throw new AuroraRuntimeException($"TDoc.stringify error: {exception.Message}");
            }
        }
    }
}
