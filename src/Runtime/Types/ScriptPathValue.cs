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
        [Export("changeExt")]
        public ScriptPathValue ChangeExtCore(ScriptDatum extension = default)
        {
            ChangeExt(GetPathString(extension));
            return this;
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
        [Export("normalize")]
        public static string NormalizeCore(ScriptDatum value = default) => ScriptPath.NormalizeText(GetPathString(value));

        /// <summary>Returns the directory portion of a path.</summary>
        [Export("directoryName")]
        public static string DirectoryNameCore(ScriptDatum value = default) => ScriptPath.GetDirectoryNameText(GetPathString(value));

        /// <summary>Returns the file-name portion of a path.</summary>
        [Export("fileName")]
        public static string FileNameCore(ScriptDatum value = default) => ScriptPath.GetFileNameText(GetPathString(value));

        /// <summary>Returns the extension portion of a path.</summary>
        [Export("extName")]
        public static string ExtNameCore(ScriptDatum value = default) => ScriptPath.GetExtNameText(GetPathString(value));

        /// <summary>Returns the protocol portion of a path.</summary>
        [Export("protocol")]
        public static string ProtocolCore(ScriptDatum value = default) => ScriptPath.GetProtocolText(GetPathString(value));

        /// <summary>Changes a path's extension.</summary>
        [Export("changeExt")]
        public static string ChangeExtCore(ScriptDatum path = default, ScriptDatum extension = default) =>
            ScriptPath.EnsureExtensionText(GetPathString(path), GetPathString(extension));

        /// <summary>Returns whether a path is rooted.</summary>
        [Export("isRooted")]
        public static bool IsRootedCore(ScriptDatum value = default) => ScriptPath.IsRootedText(GetPathString(value));

        /// <summary>Returns whether a path is under the supplied root.</summary>
        [Export("isUnderRoot")]
        public static bool IsUnderRootCore(ScriptDatum root = default, ScriptDatum path = default) =>
            ScriptPath.IsUnderRootText(GetPathString(root), GetPathString(path));

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
