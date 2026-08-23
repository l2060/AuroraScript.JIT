using AuroraScript.Core;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Runtime.Types.TypeConstruct
{
    internal sealed class PathConstructor : ScriptType
    {
        internal static readonly PathConstructor INSTANCE = new PathConstructor();

        private PathConstructor() : base("Path")
        {
            Define("of", ScriptDatum.FromBonding(OF), writeable: false, enumerable: false);
            Define("isPath", ScriptDatum.FromBonding(IS_PATH), writeable: false, enumerable: false);
            Define("join", ScriptDatum.FromBonding(JOIN), writeable: false, enumerable: false);
            Define("baseModule", ScriptDatum.FromBonding(BASE_MODULE), writeable: false, enumerable: false);
            Define("normalize", ScriptDatum.FromBonding(NORMALIZE), writeable: false, enumerable: false);
            Define("directoryName", ScriptDatum.FromBonding(DIRECTORY_NAME), writeable: false, enumerable: false);
            Define("fileName", ScriptDatum.FromBonding(FILE_NAME), writeable: false, enumerable: false);
            Define("extName", ScriptDatum.FromBonding(EXT_NAME), writeable: false, enumerable: false);
            Define("protocol", ScriptDatum.FromBonding(PROTOCOL), writeable: false, enumerable: false);
            Define("changeExt", ScriptDatum.FromBonding(CHANGE_EXT), writeable: false, enumerable: false);
            Define("isRooted", ScriptDatum.FromBonding(IS_ROOTED), writeable: false, enumerable: false);
            Define("isUnderRoot", ScriptDatum.FromBonding(IS_UNDER_ROOT), writeable: false, enumerable: false);
            Define("currentFile", ScriptDatum.FromBonding(CURRENT_FILE), writeable: false, enumerable: false);
            Define("currentDirectory", ScriptDatum.FromBonding(CURRENT_DIRECTORY), writeable: false, enumerable: false);
            Frozen();
        }

        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsObject(ref result, CreatePath(args));
        }

        internal static void OF(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsObject(ref result, CreatePath(args));
        }

        private static ScriptPathValue CreatePath(Span<ScriptDatum> args)
        {
            var root = ScriptPathValue.GetPathString(args, 0);
            return new ScriptPathValue(root, args, 1);
        }

        internal static void IS_PATH(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, args.Length > 0 && args[0].Object is ScriptPathValue);
        }

        internal static void JOIN(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var root = ScriptPathValue.GetPathString(args, 0);
            ScriptDatum.WriteAsString(ref result, ScriptPathValue.BuildPathText(root, args, 1));
        }

        internal static void BASE_MODULE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var fullPath = ctx?.Module?.Source.FullPath;
            if (string.IsNullOrEmpty(fullPath))
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }

            var directory = ScriptPath.GetDirectoryNameNormalizedText(fullPath);
            ScriptDatum.WriteAsString(ref result, ScriptPathValue.AppendPathText(directory, args));
        }

        internal static void NORMALIZE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsString(ref result, ScriptPath.NormalizeText(ScriptPathValue.GetPathString(args, 0)));
        }

        internal static void DIRECTORY_NAME(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsString(ref result, ScriptPath.GetDirectoryNameText(ScriptPathValue.GetPathString(args, 0)));
        }

        internal static void FILE_NAME(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsString(ref result, ScriptPath.GetFileNameText(ScriptPathValue.GetPathString(args, 0)));
        }

        internal static void EXT_NAME(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsString(ref result, ScriptPath.GetExtNameText(ScriptPathValue.GetPathString(args, 0)));
        }

        internal static void PROTOCOL(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsString(ref result, ScriptPath.GetProtocolText(ScriptPathValue.GetPathString(args, 0)));
        }

        internal static void CHANGE_EXT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = ScriptPathValue.GetPathString(args, 0);
            var extension = ScriptPathValue.GetPathString(args, 1);
            ScriptDatum.WriteAsString(ref result, ScriptPath.EnsureExtensionText(path, extension));
        }

        internal static void IS_ROOTED(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, ScriptPath.IsRootedText(ScriptPathValue.GetPathString(args, 0)));
        }

        internal static void IS_UNDER_ROOT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var root = ScriptPathValue.GetPathString(args, 0);
            var path = ScriptPathValue.GetPathString(args, 1);
            ScriptDatum.WriteAsBoolean(ref result, ScriptPath.IsUnderRootText(root, path));
        }

        internal static void CURRENT_FILE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var fullPath = ctx?.Module?.Source.FullPath;
            if (string.IsNullOrEmpty(fullPath))
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }

            ScriptDatum.WriteAsString(ref result, fullPath);
        }

        internal static void CURRENT_DIRECTORY(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var fullPath = ctx?.Module?.Source.FullPath;
            if (string.IsNullOrEmpty(fullPath))
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }

            ScriptDatum.WriteAsString(ref result, ScriptPath.GetDirectoryNameNormalizedText(fullPath));
        }
    }
}
