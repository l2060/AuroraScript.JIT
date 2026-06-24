using AuroraScript.Core;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Runtime.Extensions
{
    internal class HotPatchSupport : ScriptObject
    {
        internal static HotPatchSupport INSTANCE = new HotPatchSupport();
        public HotPatchSupport()
        {
            Define("replace", ScriptDatum.FromBonding(REPLACE), writeable: false, enumerable: false);
            Define("incremental", ScriptDatum.FromBonding(INCREMENTAL), writeable: false, enumerable: false);
            Frozen();
        }



        internal static void REPLACE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (!args.TryGetString(0, out var modulePath))
            {
                ThrowHelper.ThrowInvalidHotPatchParam(nameof(modulePath));
            }
            if (!args.TryGetString(1, out var script))
            {
                ThrowHelper.ThrowInvalidHotPatchParam(nameof(script));
            }
            HotPatchType patchType = HotPatchType.Replace;
            if (args.TryGetBoolean(2, out var ignoreDepends) && ignoreDepends)
            {
                patchType = patchType | HotPatchType.IgnoreDepends;
            }
            var engine = ctx.Engine;
            ctx.Domain.DynamicPatch(engine.MemorySource(modulePath, script), patchType);
        }



        internal static void INCREMENTAL(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (!args.TryGetString(0, out var modulePath))
            {
                ThrowHelper.ThrowInvalidHotPatchParam(nameof(modulePath));
            }
            if (!args.TryGetString(1, out var script))
            {
                ThrowHelper.ThrowInvalidHotPatchParam(nameof(script));
            }
            HotPatchType patchType = HotPatchType.Incremental;
            if (args.TryGetBoolean(2, out var ignoreDepends) && ignoreDepends)
            {
                patchType = patchType | HotPatchType.IgnoreDepends;
            }
            var engine = ctx.Engine;
            ctx.Domain.DynamicPatch(engine.MemorySource(modulePath, script), patchType);
        }

    }
}
