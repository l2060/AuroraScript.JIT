using AuroraScript.Hosting;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using System.Text.Json;

namespace AuroraScript.Runtime.Builtin
{

    [AuroraNativeType("JSON")]
    internal sealed partial class JsonSupport : ScriptObject
    {

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


        [AuroraExport("stringify", MatchFailure.Throw)]
        public static string StringifyCore(ScriptContext ctx, ScriptDatum value, bool indented = true)
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
