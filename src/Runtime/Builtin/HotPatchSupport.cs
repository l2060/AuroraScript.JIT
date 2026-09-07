using AuroraScript.Core;
using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Runtime.Builtin
{
    /// <summary>
    /// Script hot-patch Type implemented through generated native exports.
    /// </summary>
    [NativeType("HotPatch")]
    public sealed partial class HotPatchSupport : ScriptObject
    {
        /// <summary>Applies a replacement patch addressed by string path.</summary>
        [Export("replace", MatchFailure.Throw, DynamicAdapter = nameof(REPLACE))]
        public static void ReplaceCore(ScriptContext ctx, string modulePath, string script, bool ignoreDepends = false)
        {
            ApplyPatch(ctx, ResolveModulePath(ctx, modulePath), script, HotPatchType.Replace, ignoreDepends);
        }

        /// <summary>Applies a replacement patch addressed by a native Path value.</summary>
        [Export("replace", MatchFailure.Throw, DynamicAdapter = nameof(REPLACE))]
        public static void ReplaceCore(ScriptContext ctx, ScriptPathValue modulePath, string script, bool ignoreDepends = false)
        {
            ApplyPatch(ctx, ResolveModulePath(ctx, modulePath.Value), script, HotPatchType.Replace, ignoreDepends);
        }

        private static void REPLACE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ReadPatchArguments(ctx, args, out var modulePath, out var script, out var ignoreDepends);
            ApplyPatch(ctx, modulePath, script, HotPatchType.Replace, ignoreDepends);
        }

        /// <summary>Applies an incremental patch addressed by string path.</summary>
        [Export("incremental", MatchFailure.Throw, DynamicAdapter = nameof(INCREMENTAL))]
        public static void IncrementalCore(ScriptContext ctx, string modulePath, string script, bool ignoreDepends = false)
        {
            ApplyIncrementalPatch(ctx, ResolveModulePath(ctx, modulePath), script, ignoreDepends);
        }

        /// <summary>Applies an incremental patch addressed by a native Path value.</summary>
        [Export("incremental", MatchFailure.Throw, DynamicAdapter = nameof(INCREMENTAL))]
        public static void IncrementalCore(ScriptContext ctx, ScriptPathValue modulePath, string script, bool ignoreDepends = false)
        {
            ApplyIncrementalPatch(ctx, ResolveModulePath(ctx, modulePath.Value), script, ignoreDepends);
        }

        private static void INCREMENTAL(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ReadPatchArguments(ctx, args, out var modulePath, out var script, out var ignoreDepends);
            ApplyIncrementalPatch(ctx, modulePath, script, ignoreDepends);
        }

        private static void ApplyIncrementalPatch(ScriptContext ctx, string modulePath, string script, bool ignoreDepends)
        {
            ApplyPatch(ctx, modulePath, script, HotPatchType.Incremental, ignoreDepends);
        }

        private static void ApplyPatch(ScriptContext ctx, string modulePath, string script, HotPatchType patchType, bool ignoreDepends)
        {
            if (ignoreDepends)
            {
                patchType |= HotPatchType.IgnoreDepends;
            }
            ctx.Domain.DynamicPatch(modulePath, script, patchType);
        }

        private static void ReadPatchArguments(ScriptContext ctx, Span<ScriptDatum> args, out string modulePath, out string script, out bool ignoreDepends)
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

            if (!ScriptPathValue.TryGetPathString(args, 0, out modulePath))
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
