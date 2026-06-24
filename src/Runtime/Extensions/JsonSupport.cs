using AuroraScript.Runtime.Types;
using System;
using System.Text.Json;

namespace AuroraScript.Runtime.Extensions
{
    internal class JsonSupport : ScriptObject
    {
        public JsonSupport()
        {
            Define("parse", ScriptDatum.FromBonding(PARSE), writeable: false, enumerable: false);
            Define("stringify", ScriptDatum.FromBonding(STRINGIFY), writeable: false, enumerable: false);
        }

        public static void PARSE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            try
            {
                if (args.TryGetString(0, out var jsonText))
                {
                    var serializer = ctx.Engine.Options.JsonSerializer;
                    ScriptDatum.WriteAsObject(ref result, serializer.Deserialize(jsonText));
                }
            }
            catch (JsonException ex)
            {
                throw new AuroraRuntimeException($"JSON.parse error: {ex.Message}");
            }
        }

        public static void STRINGIFY(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var serializer = ctx.Engine.Options.JsonSerializer;
            args.TryGetBoolean(1, out var indented);
            if (args.TryGetRef(0, ref result))
            {
                ScriptDatum.WriteAsString(ref result, serializer.Serialize(result, ctx.Engine.Options, indented));
                return;
            }
            throw new AuroraRuntimeException($"JSON.stringify error.");
        }

    }
}
