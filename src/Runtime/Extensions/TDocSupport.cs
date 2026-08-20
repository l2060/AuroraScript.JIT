using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Runtime.Extensions
{
    /// <summary>
    /// Exposes TDoc serialization to AuroraScript code.
    /// </summary>
    internal sealed class TDocSupport : ScriptObject
    {
        internal TDocSupport()
        {
            Define("parse", ScriptDatum.FromBonding(PARSE), writeable: false, enumerable: false);
            Define("stringify", ScriptDatum.FromBonding(STRINGIFY), writeable: false, enumerable: false);
        }

        internal static void PARSE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (!args.TryGetString(0, out var text))
            {
                throw new AuroraRuntimeException("TDoc.parse requires text.");
            }

            try
            {
                result = TypedDocumentSerializer.Deserialize(ctx.Engine, text);
            }
            catch (TypedDocumentException exception)
            {
                throw new AuroraRuntimeException($"TDoc.parse error: {exception.Message}");
            }
        }

        internal static void STRINGIFY(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (!args.TryGetRef(0, ref result))
            {
                throw new AuroraRuntimeException("TDoc.stringify requires a value.");
            }

            var indented = true;
            if (args.Length > 1 && args.TryGetBoolean(1, out var specifiedIndented))
            {
                indented = specifiedIndented;
            }

            var emitTypeNames = false;
            if (args.Length > 2 && args.TryGetBoolean(2, out var specifiedEmitTypeNames))
            {
                emitTypeNames = specifiedEmitTypeNames;
            }

            try
            {
                var options = TypedDocumentOptions.GetFormattingOptions(indented, emitTypeNames);
                ScriptDatum.WriteAsString(ref result, TypedDocumentSerializer.Serialize(ctx.Engine, result, options));
            }
            catch (TypedDocumentException exception)
            {
                throw new AuroraRuntimeException($"TDoc.stringify error: {exception.Message}");
            }
        }
    }
}
