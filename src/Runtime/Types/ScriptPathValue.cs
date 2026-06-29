using System;
using System.Collections.Generic;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a mutable script-side path value without adding a dedicated ValueKind.
    /// </summary>
    public sealed partial class ScriptPathValue : ScriptObject
    {
        private string _value;

        internal ScriptPathValue(string root, Span<ScriptDatum> segments, int segmentStart = 0)
            : base(Prototypes.PathPrototype)
        {
            EnableValueEquality();
            _value = PathText.Build(root, segments, segmentStart);
        }

        internal ScriptPathValue(string value)
            : base(Prototypes.PathPrototype)
        {
            EnableValueEquality();
            _value = PathText.Normalize(value);
        }

        /// <summary>
        /// Gets the normalized path text held by this value.
        /// </summary>
        public string Value => _value ?? string.Empty;

        internal void Reset(string root, Span<ScriptDatum> segments, int segmentStart = 0)
        {
            _value = PathText.Build(root, segments, segmentStart);
        }

        internal void Append(Span<ScriptDatum> segments, int segmentStart = 0)
        {
            _value = PathText.Build(Value, segments, segmentStart);
        }

        internal void EnsureExtension(string extension)
        {
            _value = PathText.EnsureExtension(Value, extension);
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
            return other is ScriptPathValue path && PathText.AreEqual(Value, path.Value);
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

        internal static void ENSURE_EXTENSION(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                path.EnsureExtension(GetPathString(args, 0));
                ScriptDatum.WriteAsObject(ref result, path);
            }
        }

        internal static void DIRECTORY_NAME(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                ScriptDatum.WriteAsString(ref result, PathText.GetDirectoryName(path.Value));
            }
        }

        internal static void FILE_NAME(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                ScriptDatum.WriteAsString(ref result, PathText.GetFileName(path.Value));
            }
        }

        internal static void PROTOCOL(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptPathValue path)
            {
                ScriptDatum.WriteAsString(ref result, PathText.GetProtocol(path.Value));
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

    internal static class PathText
    {
        public static string Build(string root, Span<ScriptDatum> segments, int segmentStart)
        {
            var result = Normalize(root);
            for (var i = segmentStart; i < segments.Length; i++)
            {
                if (ScriptPathValue.TryGetPathString(segments, i, out var segment))
                {
                    result = Combine(result, segment);
                }
            }

            return result;
        }

        public static string Combine(string basePath, string segment)
        {
            basePath = Normalize(basePath);
            segment = NormalizeSeparators(segment);
            if (string.IsNullOrEmpty(segment))
            {
                return basePath;
            }

            if (IsRooted(segment))
            {
                if (segment[0] == '/' && !HasUriScheme(segment) && TryGetRoot(basePath, out var baseRoot) && baseRoot.HasRoot)
                {
                    return Normalize(baseRoot.Prefix + segment);
                }

                return Normalize(segment);
            }

            if (string.IsNullOrEmpty(basePath))
            {
                return Normalize(segment);
            }

            return Normalize(basePath.EndsWith("/", StringComparison.Ordinal)
                ? basePath + segment
                : basePath + "/" + segment);
        }

        public static string Normalize(string path)
        {
            path = NormalizeSeparators(path);
            if (path.Length == 0)
            {
                return string.Empty;
            }

            var root = ParseRoot(path);
            var parts = new List<string>();
            var start = root.PathStart;
            while (start <= path.Length)
            {
                var slash = path.IndexOf('/', start);
                var end = slash < 0 ? path.Length : slash;
                if (end > start)
                {
                    var part = path.Substring(start, end - start);
                    if (part == ".")
                    {
                    }
                    else if (part == "..")
                    {
                        if (parts.Count > 0 && parts[parts.Count - 1] != "..")
                        {
                            parts.RemoveAt(parts.Count - 1);
                        }
                        else if (!root.HasRoot)
                        {
                            parts.Add(part);
                        }
                    }
                    else
                    {
                        parts.Add(part);
                    }
                }

                if (slash < 0)
                {
                    break;
                }

                start = slash + 1;
            }

            var suffix = string.Join("/", parts);
            if (root.HasScheme)
            {
                return suffix.Length == 0 ? root.Prefix : root.Prefix + "/" + suffix;
            }

            if (root.HasDrive)
            {
                return suffix.Length == 0 ? root.Prefix : root.Prefix + "/" + suffix;
            }

            if (root.HasLeadingSlash)
            {
                return suffix.Length == 0 ? "/" : "/" + suffix;
            }

            return suffix;
        }

        public static string EnsureExtension(string path, string extension)
        {
            path = Normalize(path);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return path;
            }

            extension = NormalizeSeparators(extension);
            if (extension.Length > 0 && extension[0] != '.')
            {
                extension = "." + extension;
            }

            var root = ParseRoot(path);
            var lastSlash = path.LastIndexOf('/');
            if (lastSlash < root.PathStart - 1)
            {
                lastSlash = root.PathStart - 1;
            }

            var lastDot = path.LastIndexOf('.');
            if (lastDot > lastSlash)
            {
                var current = path.Substring(lastDot);
                var comparison = root.HasScheme || !OperatingSystem.IsWindows()
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;
                if (string.Equals(current, extension, comparison))
                {
                    return path;
                }

                return path.Substring(0, lastDot) + extension;
            }

            return path + extension;
        }

        public static string GetDirectoryName(string path)
        {
            path = Normalize(path);
            if (path.Length == 0)
            {
                return string.Empty;
            }

            var root = ParseRoot(path);
            if (root.PathStart >= path.Length)
            {
                return root.Prefix;
            }

            var lastSlash = path.LastIndexOf('/');
            if (lastSlash < root.PathStart)
            {
                return root.Prefix;
            }

            if (lastSlash == 0 && root.HasLeadingSlash)
            {
                return "/";
            }

            return path.Substring(0, lastSlash);
        }

        public static string GetFileName(string path)
        {
            path = Normalize(path);
            if (path.Length == 0)
            {
                return string.Empty;
            }

            var root = ParseRoot(path);
            if (root.PathStart >= path.Length)
            {
                return string.Empty;
            }

            var lastSlash = path.LastIndexOf('/');
            return lastSlash >= root.PathStart ? path.Substring(lastSlash + 1) : path.Substring(root.PathStart);
        }

        public static bool IsRooted(string path)
        {
            path = NormalizeSeparators(path);
            return path.Length > 0 && (path[0] == '/' || HasUriScheme(path) || IsWindowsDrivePath(path));
        }

        public static string GetProtocol(string path)
        {
            path = NormalizeSeparators(path);
            var colon = path.IndexOf(':');
            return colon > 1 && HasUriScheme(path) ? path.Substring(0, colon) : string.Empty;
        }

        public static bool AreEqual(string left, string right)
        {
            left ??= string.Empty;
            right ??= string.Empty;
            var comparison = UsesOrdinalIgnoreCase(left, right)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return string.Equals(left, right, comparison);
        }

        public static bool IsUnderRoot(string root, string path)
        {
            root = TrimTrailingSlash(Normalize(root));
            path = Normalize(path);
            if (root.Length == 0 || path.Length == 0)
            {
                return false;
            }

            var comparison = UsesOrdinalIgnoreCase(root, path)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!path.StartsWith(root, comparison))
            {
                return false;
            }

            return path.Length == root.Length || path[root.Length] == '/';
        }

        private static bool TryGetRoot(string path, out RootInfo root)
        {
            root = ParseRoot(path);
            return root.HasRoot;
        }

        private static string TrimTrailingSlash(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var root = ParseRoot(path);
            var minLength = root.Prefix?.Length ?? 0;
            while (path.Length > minLength && path[path.Length - 1] == '/')
            {
                path = path.Substring(0, path.Length - 1);
            }

            return path;
        }

        private static bool UsesOrdinalIgnoreCase(string left, string right)
        {
            return OperatingSystem.IsWindows() && !HasUriScheme(left) && !HasUriScheme(right);
        }

        private static string NormalizeSeparators(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace('\\', '/');
        }

        private static bool HasUriScheme(string path)
        {
            var colon = path.IndexOf(':');
            if (colon <= 1)
            {
                return false;
            }

            if (!char.IsLetter(path[0]))
            {
                return false;
            }

            for (var i = 1; i < colon; i++)
            {
                var c = path[i];
                if (!char.IsLetterOrDigit(c) && c != '+' && c != '-' && c != '.')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsWindowsDrivePath(string path)
        {
            return path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':';
        }

        private static RootInfo ParseRoot(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return default;
            }

            var colon = path.IndexOf(':');
            if (colon > 1 && HasUriScheme(path))
            {
                if (colon + 2 < path.Length && path[colon + 1] == '/' && path[colon + 2] == '/')
                {
                    var authorityStart = colon + 3;
                    var nextSlash = path.IndexOf('/', authorityStart);
                    if (nextSlash < 0)
                    {
                        return new RootInfo(path, path.Length, hasRoot: true, hasScheme: true, hasDrive: false, hasLeadingSlash: false);
                    }

                    return new RootInfo(path.Substring(0, nextSlash), nextSlash + 1, hasRoot: true, hasScheme: true, hasDrive: false, hasLeadingSlash: false);
                }

                return new RootInfo(path.Substring(0, colon + 1), colon + 1, hasRoot: true, hasScheme: true, hasDrive: false, hasLeadingSlash: false);
            }

            if (IsWindowsDrivePath(path))
            {
                var pathStart = path.Length > 2 && path[2] == '/' ? 3 : 2;
                return new RootInfo(path.Substring(0, 2), pathStart, hasRoot: true, hasScheme: false, hasDrive: true, hasLeadingSlash: false);
            }

            if (path[0] == '/')
            {
                return new RootInfo(string.Empty, 1, hasRoot: true, hasScheme: false, hasDrive: false, hasLeadingSlash: true);
            }

            return new RootInfo(string.Empty, 0, hasRoot: false, hasScheme: false, hasDrive: false, hasLeadingSlash: false);
        }

        private readonly struct RootInfo
        {
            public RootInfo(string prefix, int pathStart, bool hasRoot, bool hasScheme, bool hasDrive, bool hasLeadingSlash)
            {
                Prefix = prefix;
                PathStart = pathStart;
                HasRoot = hasRoot;
                HasScheme = hasScheme;
                HasDrive = hasDrive;
                HasLeadingSlash = hasLeadingSlash;
            }

            public string Prefix { get; }
            public int PathStart { get; }
            public bool HasRoot { get; }
            public bool HasScheme { get; }
            public bool HasDrive { get; }
            public bool HasLeadingSlash { get; }
        }
    }
}
