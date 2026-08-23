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
            ReadPatchArguments(ctx, args, out var modulePath, out var script, out var ignoreDepends);
            HotPatchType patchType = HotPatchType.Replace;
            if (ignoreDepends)
            {
                patchType = patchType | HotPatchType.IgnoreDepends;
            }
            ctx.Domain.DynamicPatch(modulePath, script, patchType);
        }



        internal static void INCREMENTAL(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ReadPatchArguments(ctx, args, out var modulePath, out var script, out var ignoreDepends);
            HotPatchType patchType = HotPatchType.Incremental;
            if (ignoreDepends)
            {
                patchType = patchType | HotPatchType.IgnoreDepends;
            }
            ctx.Domain.DynamicPatch(modulePath, script, patchType);
        }

        private static void ReadPatchArguments(
            ScriptContext ctx,
            Span<ScriptDatum> args,
            out string modulePath,
            out string script,
            out bool ignoreDepends)
        {
            if (args.Length == 0)
            {
                ThrowHelper.ThrowInvalidHotPatchParam(nameof(script));
            }

            if (args.Length == 1 || args[1].Kind == ValueKind.Boolean)
            {
                modulePath = GetCurrentModuleFullPath(ctx);
                if (!args.TryGetString(0, out script))
                {
                    ThrowHelper.ThrowInvalidHotPatchParam(nameof(script));
                }
                ignoreDepends = args.TryGetBoolean(1, out var currentIgnoreDepends) && currentIgnoreDepends;
                return;
            }

            if (!args.TryGetString(0, out modulePath))
            {
                ThrowHelper.ThrowInvalidHotPatchParam(nameof(modulePath));
            }
            if (!args.TryGetString(1, out script))
            {
                ThrowHelper.ThrowInvalidHotPatchParam(nameof(script));
            }

            modulePath = ResolveModulePath(ctx, modulePath);
            ignoreDepends = args.TryGetBoolean(2, out var explicitIgnoreDepends) && explicitIgnoreDepends;
        }

        private static string GetCurrentModuleFullPath(ScriptContext ctx)
        {
            var fullPath = ctx?.Module?.Source.FullPath;
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                throw new AuroraRuntimeException("HotPatch requires a current module full path.");
            }
            return fullPath;
        }

        private static string ResolveModulePath(ScriptContext ctx, string modulePath)
        {
            if (ScriptPath.IsPathRooted(modulePath))
            {
                return modulePath;
            }

            var currentFullPath = GetCurrentModuleFullPath(ctx);
            var currentDirectory = ScriptPath.GetDirectoryName(currentFullPath);
            return ScriptPath.Combine(currentDirectory, modulePath);
        }

    }
}
