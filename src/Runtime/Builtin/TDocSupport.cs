using AuroraScript.Hosting;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;

namespace AuroraScript.Runtime.Builtin
{
    /// <summary>
    /// Exposes TDoc serialization through generated native exports.
    /// </summary>
    [AuroraNativeType("TDoc")]
    public sealed partial class TDocSupport : ScriptObject
    {
        /// <summary>Deserializes TDoc text.</summary>
        [AuroraExport("parse", MatchFailure.Throw)]
        public static ScriptDatum ParseCore(ScriptContext ctx, string text)
        {
            try
            {
                return TypedDocumentSerializer.Deserialize(ctx.Engine, text);
            }
            catch (TypedDocumentException exception)
            {
                throw new AuroraRuntimeException($"TDoc.parse error: {exception.Message}");
            }
        }

        /// <summary>Serializes a script value as TDoc text.</summary>
        [AuroraExport("stringify", MatchFailure.Throw)]
        public static string StringifyCore(
            ScriptContext ctx,
            ScriptDatum value,
            bool indented = true,
            bool emitTypeNames = false)
        {
            try
            {
                var options = TypedDocumentOptions.GetFormattingOptions(indented, emitTypeNames);
                return TypedDocumentSerializer.Serialize(ctx.Engine, value, options);
            }
            catch (TypedDocumentException exception)
            {
                throw new AuroraRuntimeException($"TDoc.stringify error: {exception.Message}");
            }
        }
    }
}
