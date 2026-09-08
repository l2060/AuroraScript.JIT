using AuroraScript.Core;
using AuroraScript.Hosting;
using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a mutable script-side path value without adding a dedicated ValueKind.
    /// </summary>
    [NativeType("Path")]
    public sealed partial class ScriptPathValue : ScriptObject
    {
        private string _value;

        internal ScriptPathValue(string root, Span<ScriptDatum> segments, int segmentStart = 0)
            : base(NativePrototype)
        {
            EnableValueEquality();
            _value = BuildPathText(root, segments, segmentStart);
        }

        internal ScriptPathValue(string value)
            : base(NativePrototype)
        {
            EnableValueEquality();
            _value = ScriptPath.NormalizeText(value);
        }

        /// <summary>Creates a normalized path from a root and optional path segments.</summary>
        [Export]
        public ScriptPathValue(params ScriptDatum[] segments)
            : base(NativePrototype)
        {
            EnableValueEquality();
            var values = segments.AsSpan();
            _value = BuildPathText(GetPathString(values, 0), values, 1);
        }

        /// <summary>
        /// Gets the normalized path text held by this value.
        /// </summary>
        public string Value => _value ?? string.Empty;

        internal void Reset(string root, Span<ScriptDatum> segments, int segmentStart = 0)
        {
            _value = BuildPathText(root, segments, segmentStart);
        }

        internal void Append(Span<ScriptDatum> segments, int segmentStart = 0)
        {
            _value = AppendPathText(Value, segments, segmentStart);
        }

        private void Append(string segment)
        {
            if (segment == null)
            {
                return;
            }
            var builder = new ScriptPath.PathTextBuilder(Value);
            try
            {
                builder.Append(segment);
                _value = builder.ToStringAndReturn();
            }
            finally
            {
                builder.Dispose();
            }
        }

        internal void ChangeExt(string extension)
        {
            _value = ScriptPath.EnsureExtensionNormalizedText(Value, extension);
        }

        internal ScriptPathValue Clone()
        {
            return new ScriptPathValue(Value);
        }

        /// <summary>
        /// Returns the normalized path text.
        /// </summary>
        public override string ToString()
        {
            return Value;
        }

        internal override bool ValueEquals(ScriptObject other)
        {
            return other is ScriptPathValue path && ScriptPath.PathTextEqualsNormalized(Value, path.Value);
        }

        internal static bool TryGetPathString(Span<ScriptDatum> args, int index, out string value)
        {
            if ((uint)index < (uint)args.Length && args[index].Object is ScriptPathValue path)
            {
                value = path.Value;
                return true;
            }

            return args.TryGetString(index, out value);
        }

        internal static string GetPathString(Span<ScriptDatum> args, int index)
        {
            return TryGetPathString(args, index, out var value) ? value : string.Empty;
        }

        private static string GetPathString(ScriptDatum value)
        {
            if (value.Object is ScriptPathValue path)
            {
                return path.Value;
            }

            return value.Kind == ValueKind.String ? value.StringText : string.Empty;
        }

        internal static string BuildPathText(string root, Span<ScriptDatum> segments, int segmentStart = 0)
        {
            return AppendPathText(ScriptPath.NormalizeText(root), segments, segmentStart);
        }

        internal static string AppendPathText(string normalizedBasePath, Span<ScriptDatum> segments, int segmentStart = 0)
        {
            if (segments.Length <= segmentStart)
            {
                return normalizedBasePath ?? string.Empty;
            }

            if (TryAppendSimplePathText(normalizedBasePath, segments, segmentStart, out var simplePath))
            {
                return simplePath;
            }

            var builder = new ScriptPath.PathTextBuilder(normalizedBasePath);
            try
            {
                for (var i = segmentStart; i < segments.Length; i++)
                {
                    if (TryGetPathString(segments, i, out var segment))
                    {
                        builder.Append(segment);
                    }
                }

                return builder.ToStringAndReturn();
            }
            finally
            {
                builder.Dispose();
            }
        }

        private static bool TryAppendSimplePathText(string normalizedBasePath, Span<ScriptDatum> segments, int segmentStart, out string result)
        {
            if (segments.Length - segmentStart > ScriptPath.SimplePathJoinBuilder.Capacity)
            {
                result = null;
                return false;
            }

            var builder = new ScriptPath.SimplePathJoinBuilder();
            for (var i = segmentStart; i < segments.Length; i++)
            {
                if (!TryGetStrictPathString(segments, i, out var segment))
                {
                    result = null;
                    return false;
                }

                if (!ScriptPath.TryAddSimpleRelativeTextSegment(ref builder, segment))
                {
                    result = null;
                    return false;
                }
            }

            result = builder.ToStringAndReturn(normalizedBasePath ?? string.Empty);
            return true;
        }

        private static bool TryGetStrictPathString(Span<ScriptDatum> args, int index, out string value)
        {
            if ((uint)index >= (uint)args.Length)
            {
                value = string.Empty;
                return false;
            }

            ref readonly var datum = ref args[index];
            if (datum.Object is ScriptPathValue path)
            {
                value = path.Value;
                return true;
            }

            if (datum.Kind == ValueKind.String)
            {
                value = datum.StringText;
                return true;
            }

            value = string.Empty;
            return false;
        }

        /// <summary>Returns the normalized path text.</summary>
        [Export("toString")]
        public string ToStringCore() => Value;

        /// <summary>Appends one segment to this path.</summary>
        [Export("append", DynamicAdapter = nameof(APPEND))]
        public ScriptPathValue AppendCore(string segment0)
        {
            Append(segment0);
            return this;
        }

        /// <summary>Appends one segment to this path.</summary>
        [Export("append", DynamicAdapter = nameof(APPEND))]
        public ScriptPathValue AppendCore(ScriptPathValue segment0)
        {
            Append(segment0._value);
            return this;
        }


        /// <summary>Appends two segments to this path.</summary>
        [Export("append", DynamicAdapter = nameof(APPEND))]
        public ScriptPathValue AppendCore(string segment0, string segment1)
        {
            Append(segment0);
            Append(segment1);
            return this;
        }

        /// <summary>Appends three segments to this path.</summary>
        [Export("append", DynamicAdapter = nameof(APPEND))]
        public ScriptPathValue AppendCore(string segment0, string segment1, string segment2)
        {
            Append(segment0);
            Append(segment1);
            Append(segment2);
            return this;
        }

        /// <summary>Replaces this path with a new root and optional segments.</summary>
        [Export("reset", DynamicAdapter = nameof(RESET))]
        public ScriptPathValue ResetCore(string root = null, string segment1 = null, string segment2 = null)
        {
            _value = ScriptPath.NormalizeText(root ?? string.Empty);
            Append(segment1);
            Append(segment2);
            return this;
        }

        /// <summary>Changes this path's extension.</summary>
        [Export("changeExt", DynamicAdapter = nameof(CHANGEEXT_DYNAMIC))]
        public ScriptPathValue ChangeExtCore(string extension = null)
        {
            ChangeExt(extension);
            return this;
        }

        private static void CHANGEEXT_DYNAMIC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
                result = ScriptDatum.FromObject(path.ChangeExtCore(GetPathString(args.Length > 0 ? args[0] : default)));
        }

        /// <summary>Returns this path's normalized extension.</summary>
        [Export("extName")]
        public string ExtNameCore() => ScriptPath.GetExtNameNormalizedText(Value);

        /// <summary>Returns this path's normalized directory name.</summary>
        [Export("directoryName")]
        public string DirectoryNameCore() => ScriptPath.GetDirectoryNameNormalizedText(Value);

        /// <summary>Returns this path's normalized file name.</summary>
        [Export("fileName")]
        public string FileNameCore() => ScriptPath.GetFileNameNormalizedText(Value);

        /// <summary>Returns this path's protocol name.</summary>
        [Export("protocol")]
        public string ProtocolCore() => ScriptPath.GetProtocolText(Value);

        /// <summary>Creates an independent copy of this path.</summary>
        [Export("clone")]
        public ScriptPathValue CloneCore() => Clone();

        /// <summary>Creates a path from a root and optional segments.</summary>
        [Export("of", DynamicAdapter = nameof(OF))]
        public static ScriptPathValue OfCore(string root = null, string segment1 = null, string segment2 = null)
        {
            var result = new ScriptPathValue(root ?? string.Empty);
            result.Append(segment1);
            result.Append(segment2);
            return result;
        }

        /// <summary>Dynamic adapter for variadic Path.of calls.</summary>
        public static void OF(ScriptContext context, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            result = ScriptDatum.FromObject(new ScriptPathValue(
                GetPathString(args, 0),
                args,
                1));
        }

        /// <summary>Dynamic adapter for variadic Path append calls.</summary>
        public static void APPEND(ScriptContext context, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not ScriptPathValue path)
            {
                result = ScriptDatum.Null;
                return;
            }
            path.Append(args);
            result = ScriptDatum.FromObject(path);
        }

        /// <summary>Dynamic adapter for variadic Path reset calls.</summary>
        public static void RESET(ScriptContext context, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not ScriptPathValue path)
            {
                result = ScriptDatum.Null;
                return;
            }
            path.Reset(GetPathString(args, 0), args, 1);
            result = ScriptDatum.FromObject(path);
        }

        /// <summary>Returns whether the supplied value is a Path.</summary>
        [Export("isPath")]
        public static bool IsPathCore(ScriptDatum value = default) => value.Object is ScriptPathValue;

        /// <summary>Joins and normalizes path segments.</summary>
        [Export("join", DynamicAdapter = nameof(JOIN))]
        public static string JoinCore(string root = null, string segment1 = null, string segment2 = null)
        {
            return JoinStrings(root, segment1, segment2);
        }

        private static void JOIN(ScriptContext context, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            result = ScriptDatum.FromString(BuildPathText(GetPathString(args, 0), args, 1));
        }

        /// <summary>Resolves segments relative to the current module directory.</summary>
        [Export("baseModule", DynamicAdapter = nameof(BASE_MODULE))]
        public static string BaseModuleCore(ScriptContext context, string segment0 = null, string segment1 = null, string segment2 = null)
        {
            var fullPath = context?.Module?.Source.FullPath;
            return string.IsNullOrEmpty(fullPath) ? null : JoinStrings(
                ScriptPath.GetDirectoryNameNormalizedText(fullPath), segment0, segment1, segment2);
        }

        private static void BASE_MODULE(ScriptContext context, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var fullPath = context?.Module?.Source.FullPath;
            result = string.IsNullOrEmpty(fullPath) ? ScriptDatum.Null : ScriptDatum.FromString(
                AppendPathText(ScriptPath.GetDirectoryNameNormalizedText(fullPath), args));
        }

        private static string JoinStrings(string root, string segment1, string segment2, string segment3 = null)
        {
            var path = ScriptPath.NormalizeText(root);
            path = ScriptPath.CombineNormalizedText(path, segment1);
            path = ScriptPath.CombineNormalizedText(path, segment2);
            return ScriptPath.CombineNormalizedText(path, segment3);
        }

        /// <summary>Normalizes path text.</summary>
        [Export("normalize", DynamicAdapter = nameof(NORMALIZE_DYNAMIC))]
        public static string NormalizeCore(string value = null) => ScriptPath.NormalizeText(value);

        /// <summary>Accepts a native Path without a datum conversion.</summary>
        [Export("normalize", DynamicAdapter = nameof(NORMALIZE_DYNAMIC))]
        public static string NormalizeCore(ScriptPathValue value) => NormalizeCore(value?.Value);

        private static void NORMALIZE_DYNAMIC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result) =>
            result = ScriptDatum.FromString(NormalizeCore(GetPathString(args.Length > 0 ? args[0] : default)));

        /// <summary>Returns the directory portion of a path.</summary>
        [Export("directoryName", DynamicAdapter = nameof(DIRECTORYNAME_DYNAMIC))]
        public static string DirectoryNameCore(string value = null) => ScriptPath.GetDirectoryNameText(value);

        /// <summary>Accepts a native Path without a datum conversion.</summary>
        [Export("directoryName", DynamicAdapter = nameof(DIRECTORYNAME_DYNAMIC))]
        public static string DirectoryNameCore(ScriptPathValue value) => DirectoryNameCore(value?.Value);

        private static void DIRECTORYNAME_DYNAMIC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result) =>
            result = ScriptDatum.FromString(DirectoryNameCore(GetPathString(args.Length > 0 ? args[0] : default)));

        /// <summary>Returns the file-name portion of a path.</summary>
        [Export("fileName", DynamicAdapter = nameof(FILENAME_DYNAMIC))]
        public static string FileNameCore(string value = null) => ScriptPath.GetFileNameText(value);

        /// <summary>Accepts a native Path without a datum conversion.</summary>
        [Export("fileName", DynamicAdapter = nameof(FILENAME_DYNAMIC))]
        public static string FileNameCore(ScriptPathValue value) => FileNameCore(value?.Value);

        private static void FILENAME_DYNAMIC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result) =>
            result = ScriptDatum.FromString(FileNameCore(GetPathString(args.Length > 0 ? args[0] : default)));

        /// <summary>Returns the extension portion of a path.</summary>
        [Export("extName", DynamicAdapter = nameof(EXTNAME_DYNAMIC))]
        public static string ExtNameCore(string value = null) => ScriptPath.GetExtNameText(value);

        /// <summary>Accepts a native Path without a datum conversion.</summary>
        [Export("extName", DynamicAdapter = nameof(EXTNAME_DYNAMIC))]
        public static string ExtNameCore(ScriptPathValue value) => ExtNameCore(value?.Value);

        private static void EXTNAME_DYNAMIC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result) =>
            result = ScriptDatum.FromString(ExtNameCore(GetPathString(args.Length > 0 ? args[0] : default)));

        /// <summary>Returns the protocol portion of a path.</summary>
        [Export("protocol", DynamicAdapter = nameof(PROTOCOL_DYNAMIC))]
        public static string ProtocolCore(string value = null) => ScriptPath.GetProtocolText(value);

        /// <summary>Accepts a native Path without a datum conversion.</summary>
        [Export("protocol", DynamicAdapter = nameof(PROTOCOL_DYNAMIC))]
        public static string ProtocolCore(ScriptPathValue value) => ProtocolCore(value?.Value);

        private static void PROTOCOL_DYNAMIC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result) =>
            result = ScriptDatum.FromString(ProtocolCore(GetPathString(args.Length > 0 ? args[0] : default)));

        /// <summary>Changes a path's extension.</summary>
        [Export("changeExt", DynamicAdapter = nameof(CHANGEEXT_STATIC_DYNAMIC))]
        public static string ChangeExtCore(string path = null, string extension = null) => ScriptPath.EnsureExtensionText(path, extension);

        private static void CHANGEEXT_STATIC_DYNAMIC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result) =>
            result = ScriptDatum.FromString(ChangeExtCore(GetPathString(args.Length > 0 ? args[0] : default),
                GetPathString(args.Length > 1 ? args[1] : default)));

        /// <summary>Returns whether a path is rooted.</summary>
        [Export("isRooted", DynamicAdapter = nameof(ISROOTED_DYNAMIC))]
        public static bool IsRootedCore(string value = null) => ScriptPath.IsRootedText(value);

        /// <summary>Accepts a native Path without a datum conversion.</summary>
        [Export("isRooted", DynamicAdapter = nameof(ISROOTED_DYNAMIC))]
        public static bool IsRootedCore(ScriptPathValue value) => IsRootedCore(value?.Value);

        private static void ISROOTED_DYNAMIC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result) =>
            result = ScriptDatum.FromBoolean(IsRootedCore(GetPathString(args.Length > 0 ? args[0] : default)));

        /// <summary>Returns whether a path is under the supplied root.</summary>
        [Export("isUnderRoot", DynamicAdapter = nameof(ISUNDERROOT_STATIC_DYNAMIC))]
        public static bool IsUnderRootCore(string root = null, string path = null) => ScriptPath.IsUnderRootText(root, path);

        private static void ISUNDERROOT_STATIC_DYNAMIC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result) =>
            result = ScriptDatum.FromBoolean(IsUnderRootCore(GetPathString(args.Length > 0 ? args[0] : default),
                GetPathString(args.Length > 1 ? args[1] : default)));

        /// <summary>Returns the current module file, or null outside a module.</summary>
        [Export("currentFile")]
        public static ScriptDatum CurrentFileCore(ScriptContext context)
        {
            var fullPath = context?.Module?.Source.FullPath;
            return string.IsNullOrEmpty(fullPath) ? ScriptDatum.Null : ScriptDatum.FromString(fullPath);
        }

        /// <summary>Returns the current module directory, or null outside a module.</summary>
        [Export("currentDirectory")]
        public static ScriptDatum CurrentDirectoryCore(ScriptContext context)
        {
            var fullPath = context?.Module?.Source.FullPath;
            return string.IsNullOrEmpty(fullPath)
                ? ScriptDatum.Null
                : ScriptDatum.FromString(ScriptPath.GetDirectoryNameNormalizedText(fullPath));
        }
    }
}
