using AuroraScript.Core;
using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Runtime.Builtin
{
    /// <summary>
    /// Script hot-patch Type implemented through generated native exports.
    /// </summary>
    [AuroraNativeType("HotPatch")]
    internal sealed partial class HotPatchSupport : ScriptObject
    {
        [AuroraExport("replace", MatchFailure.Throw)]
        public static void ReplaceCore(ScriptContext ctx, params ScriptDatum[] args)
        {
            ReadPatchArguments(ctx, args.AsSpan(), out var modulePath, out var script, out var ignoreDepends);
            var patchType = HotPatchType.Replace;
            if (ignoreDepends)
            {
                patchType |= HotPatchType.IgnoreDepends;
            }
            ctx.Domain.DynamicPatch(modulePath, script, patchType);
        }

        [AuroraExport("incremental", MatchFailure.Throw)]
        public static void IncrementalCore(ScriptContext ctx, params ScriptDatum[] args)
        {
            ReadPatchArguments(ctx, args.AsSpan(), out var modulePath, out var script, out var ignoreDepends);
            var patchType = HotPatchType.Incremental;
            if (ignoreDepends)
            {
                patchType |= HotPatchType.IgnoreDepends;
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
