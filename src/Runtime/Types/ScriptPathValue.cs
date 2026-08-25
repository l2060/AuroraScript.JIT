using AuroraScript.Core;
using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a mutable script-side path value without adding a dedicated ValueKind.
    /// </summary>
    public sealed partial class ScriptPathValue : ScriptObject
    {
        private string _value;

        internal override ScriptDatum TypeOfValue => TypeNames.Path;

        internal ScriptPathValue(string root, Span<ScriptDatum> segments, int segmentStart = 0)
            : base(Prototypes.PathPrototype)
        {
            EnableValueEquality();
            _value = BuildPathText(root, segments, segmentStart);
        }

        internal ScriptPathValue(string value)
            : base(Prototypes.PathPrototype)
        {
            EnableValueEquality();
            _value = ScriptPath.NormalizeText(value);
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

        internal static void TO_STRING(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                ScriptDatum.WriteAsString(ref result, path.Value);
            }
        }

        internal static void APPEND(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                path.Append(args);
                ScriptDatum.WriteAsObject(ref result, path);
            }
        }

        internal static void RESET(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                var root = GetPathString(args, 0);
                path.Reset(root, args, 1);
                ScriptDatum.WriteAsObject(ref result, path);
            }
        }

        internal static void CHANGE_EXT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                path.ChangeExt(GetPathString(args, 0));
                ScriptDatum.WriteAsObject(ref result, path);
            }
        }

        internal static void EXT_NAME(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                ScriptDatum.WriteAsString(ref result, ScriptPath.GetExtNameNormalizedText(path.Value));
            }
        }

        internal static void DIRECTORY_NAME(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                ScriptDatum.WriteAsString(ref result, ScriptPath.GetDirectoryNameNormalizedText(path.Value));
            }
        }

        internal static void FILE_NAME(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                ScriptDatum.WriteAsString(ref result, ScriptPath.GetFileNameNormalizedText(path.Value));
            }
        }

        internal static void PROTOCOL(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                ScriptDatum.WriteAsString(ref result, ScriptPath.GetProtocolText(path.Value));
            }
        }

        internal static void CLONE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                ScriptDatum.WriteAsObject(ref result, path.Clone());
            }
        }
    }
}
