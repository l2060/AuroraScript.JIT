using System;
using System.Buffers;
using System.IO;

namespace AuroraScript.Core
{
    internal static class ScriptPath
    {
        public static readonly StringComparer Comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        public static string NormalizeBaseDirectory(string baseDirectory)
        {
            var normalized = string.IsNullOrWhiteSpace(baseDirectory)
                ? Directory.GetCurrentDirectory()
                : NormalizeFullPath(baseDirectory);
            return TrimTrailingSeparators(normalized);
        }

        public static string GetFullPath(string baseDirectory, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Script source paths cannot be empty.", nameof(path));
            }

            if (IsRooted(path))
            {
                return NormalizeFullPath(path);
            }

            return NormalizeFullPath(Combine(NormalizeBaseDirectory(baseDirectory), path));
        }

        public static string NormalizeFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Script source paths cannot be empty.", nameof(path));
            }

            return HasUriScheme(path)
                ? NormalizeLogicalPath(path)
                : NormalizeFileSystemPath(Path.GetFullPath(path));
        }

        public static string Combine(string basePath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Script source paths cannot be empty.", nameof(relativePath));
            }

            if (IsRooted(relativePath))
            {
                return NormalizeFullPath(relativePath);
            }

            basePath = NormalizeBaseDirectory(basePath);
            if (!HasUriScheme(basePath))
            {
                return NormalizeFileSystemPath(Path.Combine(basePath, relativePath));
            }

            var uriBase = basePath.EndsWith("/", StringComparison.Ordinal)
                ? basePath
                : basePath + "/";
            if (Uri.TryCreate(uriBase, UriKind.Absolute, out var baseUri) &&
                Uri.TryCreate(baseUri, NormalizeSeparators(relativePath), out var resolvedUri))
            {
                return NormalizeLogicalPath(resolvedUri.ToString());
            }

            return NormalizeLogicalPath(uriBase + NormalizeSeparators(relativePath));
        }

        public static string GetDirectoryName(string path)
        {
            path = NormalizeFullPath(path);
            if (!HasUriScheme(path))
            {
                return NormalizeFileSystemPath(Path.GetDirectoryName(path) ?? string.Empty);
            }

            var lastSlash = path.LastIndexOf('/');
            if (lastSlash < 0)
            {
                return path;
            }

            var schemeSeparator = path.IndexOf("://", StringComparison.Ordinal);
            if (schemeSeparator >= 0 && lastSlash <= schemeSeparator + 2)
            {
                return path;
            }

            return path.Substring(0, lastSlash);
        }

        public static string GetFileName(string path)
        {
            path = NormalizeFullPath(path);
            if (!HasUriScheme(path))
            {
                return Path.GetFileName(path);
            }

            var lastSlash = path.LastIndexOf('/');
            return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
        }

        public static string EnsureExtension(string path, string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return NormalizeFullPath(path);
            }

            if (extension[0] != '.')
            {
                extension = "." + extension;
            }

            path = NormalizeFullPath(path);
            var currentExtension = GetExtension(path);
            return string.Equals(currentExtension, extension, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
                ? path
                : ChangeExtension(path, extension);
        }

        public static string GetModulePath(string baseDirectory, string fullPath)
        {
            baseDirectory = NormalizeBaseDirectory(baseDirectory);
            fullPath = NormalizeFullPath(fullPath);

            string modulePath;
            if (HasUriScheme(baseDirectory) || HasUriScheme(fullPath))
            {
                modulePath = StartsWithPath(fullPath, baseDirectory)
                    ? fullPath.Substring(baseDirectory.Length)
                    : fullPath;
            }
            else
            {
                modulePath = Path.GetRelativePath(baseDirectory, fullPath);
            }

            modulePath = modulePath.Replace('\\', '/');
            while (modulePath.StartsWith("/", StringComparison.Ordinal))
            {
                modulePath = modulePath.Substring(1);
            }
            return modulePath;
        }

        public static bool StartsWithPath(string path, string basePath)
        {
            path = NormalizeFullPath(path);
            basePath = TrimTrailingSeparators(NormalizeBaseDirectory(basePath));

            return IsWithinNormalizedRoot(basePath, path);
        }

        public static bool IsWithinNormalizedRoot(string normalizedRoot, string normalizedPath)
        {
            if (string.IsNullOrEmpty(normalizedRoot) || string.IsNullOrEmpty(normalizedPath))
            {
                return false;
            }

            if (!normalizedPath.StartsWith(normalizedRoot, PathComparison))
            {
                return false;
            }

            if (normalizedPath.Length == normalizedRoot.Length)
            {
                return true;
            }

            var next = normalizedPath[normalizedRoot.Length];
            return next == '/' || next == '\\';
        }

        public static bool NormalizedRootsEqual(string left, string right)
        {
            return Comparer.Equals(left, right);
        }

        public static bool IsPathRooted(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && IsRooted(path);
        }

        public static bool RootsEqual(string left, string right)
        {
            return Comparer.Equals(NormalizeBaseDirectory(left), NormalizeBaseDirectory(right));
        }

        public static string NormalizeText(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            if (IsNormalizedText(path))
            {
                return path;
            }

            var builder = new PathTextBuilder(string.Empty);
            try
            {
                builder.Append(path);
                return builder.ToStringAndReturn();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static string JoinText(string root, ReadOnlySpan<string> segments)
        {
            return JoinNormalizedText(NormalizeText(root), segments);
        }

        public static string CombineText(string basePath, string segment)
        {
            return CombineNormalizedText(NormalizeText(basePath), segment);
        }

        public static string JoinNormalizedText(string normalizedBasePath, ReadOnlySpan<string> segments)
        {
            normalizedBasePath ??= string.Empty;
            if (segments.Length == 0)
            {
                return normalizedBasePath;
            }

            if (segments.Length == 1)
            {
                return CombineNormalizedText(normalizedBasePath, segments[0]);
            }

            if (TryJoinSimpleRelativeSegments(normalizedBasePath, segments, out var joined))
            {
                return joined;
            }

            var builder = new PathTextBuilder(normalizedBasePath);
            try
            {
                for (var i = 0; i < segments.Length; i++)
                {
                    builder.Append(segments[i]);
                }

                return builder.ToStringAndReturn();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static string CombineNormalizedText(string normalizedBasePath, string segment)
        {
            normalizedBasePath ??= string.Empty;
            if (string.IsNullOrEmpty(segment))
            {
                return normalizedBasePath;
            }

            if (normalizedBasePath.Length == 0)
            {
                var emptyBaseKind = ClassifyTextSegment(segment);
                return emptyBaseKind == TextSegmentKind.SimpleRelative ? segment : NormalizeText(segment);
            }

            var kind = ClassifyTextSegment(segment);
            if (kind == TextSegmentKind.CurrentRelative)
            {
                return normalizedBasePath;
            }

            if (kind == TextSegmentKind.SimpleRelative)
            {
                return normalizedBasePath.EndsWith("/", StringComparison.Ordinal)
                    ? string.Concat(normalizedBasePath.AsSpan(), segment.AsSpan())
                    : string.Concat(normalizedBasePath.AsSpan(), "/".AsSpan(), segment.AsSpan());
            }

            var builder = new PathTextBuilder(normalizedBasePath);
            try
            {
                builder.Append(segment);
                return builder.ToStringAndReturn();
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static string EnsureExtensionText(string path, string extension)
        {
            return EnsureExtensionNormalizedText(NormalizeText(path), extension);
        }

        public static string EnsureExtensionNormalizedText(string normalizedPath, string extension)
        {
            normalizedPath ??= string.Empty;
            if (string.IsNullOrWhiteSpace(extension))
            {
                return normalizedPath;
            }

            extension = NormalizeSeparators(extension);
            if (extension.Length > 0 && extension[0] != '.')
            {
                extension = "." + extension;
            }

            var root = ParseRootText(normalizedPath.AsSpan());
            var extensionStart = GetExtensionStartNormalizedText(normalizedPath, root);
            if (extensionStart >= 0)
            {
                var comparison = root.HasScheme || !OperatingSystem.IsWindows()
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;
                if (normalizedPath.AsSpan(extensionStart).Equals(extension.AsSpan(), comparison))
                {
                    return normalizedPath;
                }

                return string.Concat(normalizedPath.AsSpan(0, extensionStart), extension.AsSpan());
            }

            return normalizedPath + extension;
        }

        public static string GetExtNameText(string path)
        {
            return GetExtNameNormalizedText(NormalizeText(path));
        }

        public static string GetExtNameNormalizedText(string normalizedPath)
        {
            normalizedPath ??= string.Empty;
            if (normalizedPath.Length == 0)
            {
                return string.Empty;
            }

            var root = ParseRootText(normalizedPath.AsSpan());
            var extensionStart = GetExtensionStartNormalizedText(normalizedPath, root);
            return extensionStart >= 0 ? normalizedPath.Substring(extensionStart) : string.Empty;
        }

        public static string GetDirectoryNameText(string path)
        {
            return GetDirectoryNameNormalizedText(NormalizeText(path));
        }

        public static string GetDirectoryNameNormalizedText(string normalizedPath)
        {
            if (string.IsNullOrEmpty(normalizedPath))
            {
                return string.Empty;
            }

            var root = ParseRootText(normalizedPath.AsSpan());
            if (root.PathStart >= normalizedPath.Length)
            {
                return GetRootPrefix(normalizedPath, root);
            }

            var lastSlash = normalizedPath.LastIndexOf('/');
            if (lastSlash < root.PathStart)
            {
                return GetRootPrefix(normalizedPath, root);
            }

            if (lastSlash == 0 && root.HasLeadingSlash)
            {
                return "/";
            }

            return normalizedPath.Substring(0, lastSlash);
        }

        public static string GetFileNameText(string path)
        {
            return GetFileNameNormalizedText(NormalizeText(path));
        }

        public static string GetFileNameNormalizedText(string normalizedPath)
        {
            if (string.IsNullOrEmpty(normalizedPath))
            {
                return string.Empty;
            }

            var root = ParseRootText(normalizedPath.AsSpan());
            if (root.PathStart >= normalizedPath.Length)
            {
                return string.Empty;
            }

            var lastSlash = normalizedPath.LastIndexOf('/');
            return lastSlash >= root.PathStart
                ? normalizedPath.Substring(lastSlash + 1)
                : normalizedPath.Substring(root.PathStart);
        }

        public static string GetProtocolText(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            if (!char.IsLetter(path[0]))
            {
                return string.Empty;
            }

            for (var i = 1; i < path.Length; i++)
            {
                var c = path[i];
                if (c == ':')
                {
                    return i > 1 ? path.Substring(0, i) : string.Empty;
                }

                if (!char.IsLetterOrDigit(c) && c != '+' && c != '-' && c != '.')
                {
                    return string.Empty;
                }
            }

            return string.Empty;
        }

        public static bool IsRootedText(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var first = path[0];
            if (IsSlash(first))
            {
                return true;
            }

            if (!char.IsLetter(first))
            {
                return false;
            }

            if (path.Length >= 2 && path[1] == ':')
            {
                return true;
            }

            for (var i = 1; i < path.Length; i++)
            {
                var c = path[i];
                if (c == '/')
                {
                    return false;
                }

                if (c == ':')
                {
                    return i > 1;
                }

                if (!char.IsLetterOrDigit(c) && c != '+' && c != '-' && c != '.')
                {
                    return false;
                }
            }

            return false;
        }

        public static bool IsUnderRootText(string root, string path)
        {
            root = TrimTrailingSlashNormalizedText(NormalizeText(root));
            path = NormalizeText(path);
            if (root.Length == 0 || path.Length == 0)
            {
                return false;
            }

            var comparison = UsesOrdinalIgnoreCaseText(root, path)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!path.StartsWith(root, comparison))
            {
                return false;
            }

            return path.Length == root.Length || path[root.Length] == '/';
        }

        public static bool PathTextEqualsNormalized(string left, string right)
        {
            left ??= string.Empty;
            right ??= string.Empty;
            var comparison = UsesOrdinalIgnoreCaseText(left, right)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return string.Equals(left, right, comparison);
        }

        private static string GetExtension(string path)
        {
            if (!HasUriScheme(path))
            {
                return Path.GetExtension(path);
            }

            var lastSlash = path.LastIndexOf('/');
            var lastDot = path.LastIndexOf('.');
            return lastDot > lastSlash ? path.Substring(lastDot) : string.Empty;
        }

        private static string ChangeExtension(string path, string extension)
        {
            if (!HasUriScheme(path))
            {
                return NormalizeFileSystemPath(Path.ChangeExtension(path, extension));
            }

            var lastSlash = path.LastIndexOf('/');
            var lastDot = path.LastIndexOf('.');
            if (lastDot > lastSlash)
            {
                return path.Substring(0, lastDot) + extension;
            }
            return path + extension;
        }

        private static bool IsRooted(string path)
        {
            return HasUriScheme(path) || Path.IsPathRooted(path);
        }

        private static string NormalizeLogicalPath(string path)
        {
            return NormalizeSeparators(path);
        }

        private static string NormalizeFileSystemPath(string path)
        {
            return NormalizeSeparators(path);
        }

        private static bool HasUriScheme(string path)
        {
            return !string.IsNullOrEmpty(path) && HasUriScheme(path.AsSpan());
        }

        private static bool HasUriScheme(ReadOnlySpan<char> path)
        {
            var colon = path.IndexOf(':');
            return colon > 1 && HasUriScheme(path, colon);
        }

        private static bool HasUriScheme(ReadOnlySpan<char> path, int colon)
        {
            if (colon <= 1 || !char.IsLetter(path[0]))
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

        private static string TrimTrailingSeparators(string path)
        {
            var minLength = 0;
            if (HasUriScheme(path))
            {
                var schemeSeparator = path.IndexOf("://", StringComparison.Ordinal);
                if (schemeSeparator >= 0)
                {
                    minLength = schemeSeparator + 3;
                }
            }
            else
            {
                var root = Path.GetPathRoot(path);
                minLength = string.IsNullOrEmpty(root) ? 0 : root.Length;
            }

            while (path.Length > minLength && IsSlash(path[path.Length - 1]))
            {
                path = path.Substring(0, path.Length - 1);
            }

            return path;
        }

        private static bool IsNormalizedText(string path)
        {
            if (path.Length == 0)
            {
                return true;
            }

            var root = ParseRootText(path.AsSpan());
            if (ContainsBackslash(path, 0, root.PrefixLength))
            {
                return false;
            }

            if (root.HasRoot &&
                !root.HasLeadingSlash &&
                root.PathStart > root.PrefixLength &&
                root.PathStart >= path.Length)
            {
                return false;
            }

            if (root.PathStart >= path.Length)
            {
                return true;
            }

            var seenNormalSegment = false;
            var segmentStart = root.PathStart;
            for (var i = root.PathStart; i <= path.Length; i++)
            {
                if (i < path.Length && path[i] != '/')
                {
                    if (path[i] == '\\')
                    {
                        return false;
                    }

                    continue;
                }

                var length = i - segmentStart;
                if (length == 0)
                {
                    return false;
                }

                if (IsCurrentSegment(path, segmentStart, length))
                {
                    return false;
                }

                if (IsParentSegment(path, segmentStart, length))
                {
                    if (root.HasRoot || seenNormalSegment)
                    {
                        return false;
                    }
                }
                else
                {
                    seenNormalSegment = true;
                }

                if (i == path.Length)
                {
                    break;
                }

                segmentStart = i + 1;
            }

            return true;
        }

        internal struct PathTextBuilder : IDisposable
        {
            private string _rootSource;
            private RootInfo _root;
            private SegmentPiece[] _segments;
            private InlineSegments _inlineSegments;
            private int _segmentCount;
            private int _segmentLength;
            private bool _disposed;

            public PathTextBuilder(string normalizedBasePath)
            {
                normalizedBasePath ??= string.Empty;
                _rootSource = normalizedBasePath;
                _root = ParseRootText(normalizedBasePath.AsSpan());
                _segments = null;
                _inlineSegments = default;
                if (_root.PathStart < normalizedBasePath.Length)
                {
                    AddNormalizedSegments(normalizedBasePath, _root.PathStart);
                }
            }

            public void Append(string segment)
            {
                if (string.IsNullOrEmpty(segment))
                {
                    return;
                }

                var kind = ClassifyTextSegment(segment);
                if (kind == TextSegmentKind.CurrentRelative)
                {
                    return;
                }

                if (kind == TextSegmentKind.SimpleRelative)
                {
                    AddSegment(segment, 0, segment.Length);
                    return;
                }

                if (kind == TextSegmentKind.RootCandidate)
                {
                    var segmentRoot = ParseRootText(segment.AsSpan());
                    if (segmentRoot.HasRoot)
                    {
                        if (segmentRoot.HasLeadingSlash && !HasUriScheme(segment) && _root.HasRoot)
                        {
                            ClearSegments();
                            AddRawSegments(segment, segmentRoot.PathStart);
                            return;
                        }

                        ResetRoot(segment, segmentRoot);
                        AddRawSegments(segment, segmentRoot.PathStart);
                        return;
                    }
                }

                AddRawSegments(segment, 0);
            }

            public string ToStringAndReturn()
            {
                if (_segmentCount == 0)
                {
                    return GetRootPrefix(_rootSource, _root);
                }

                var length = _segmentLength + _segmentCount - 1;
                if (_root.HasLeadingSlash)
                {
                    length++;
                }
                else if (_root.HasScheme || _root.HasDrive)
                {
                    length += _root.PrefixLength + 1;
                }

                return string.Create(length, this, static (destination, builder) =>
                {
                    var position = 0;
                    if (builder._root.HasLeadingSlash)
                    {
                        destination[position++] = '/';
                    }
                    else if (builder._root.HasScheme || builder._root.HasDrive)
                    {
                        CopyNormalized(builder._rootSource.AsSpan(0, builder._root.PrefixLength), destination.Slice(position, builder._root.PrefixLength));
                        position += builder._root.PrefixLength;
                        destination[position++] = '/';
                    }

                    for (var i = 0; i < builder._segmentCount; i++)
                    {
                        if (i > 0)
                        {
                            destination[position++] = '/';
                        }

                        var segment = builder.GetSegment(i);
                        segment.Text.AsSpan(segment.Start, segment.Length).CopyTo(destination.Slice(position, segment.Length));
                        position += segment.Length;
                    }
                });
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                if (_segments != null)
                {
                    Array.Clear(_segments, 0, _segmentCount);
                    ArrayPool<SegmentPiece>.Shared.Return(_segments);
                }
            }

            private void ResetRoot(string rootSource, RootInfo root)
            {
                _rootSource = rootSource;
                _root = root;
                ClearSegments();
            }

            private void ClearSegments()
            {
                if (_segments != null)
                {
                    Array.Clear(_segments, 0, _segmentCount);
                }

                _inlineSegments = default;
                _segmentCount = 0;
                _segmentLength = 0;
            }

            private void AddNormalizedSegments(string path, int start)
            {
                while (start <= path.Length)
                {
                    var slash = path.IndexOf('/', start);
                    var end = slash < 0 ? path.Length : slash;
                    if (end > start)
                    {
                        AddSegment(path, start, end - start);
                    }

                    if (slash < 0)
                    {
                        return;
                    }

                    start = slash + 1;
                }
            }

            private void AddRawSegments(string path, int start)
            {
                while (start <= path.Length)
                {
                    var slash = IndexOfSlash(path, start);
                    var end = slash < 0 ? path.Length : slash;
                    if (end > start)
                    {
                        var length = end - start;
                        if (IsCurrentSegment(path, start, length))
                        {
                        }
                        else if (ScriptPath.IsParentSegment(path, start, length))
                        {
                            if (_segmentCount > 0 && !IsParentSegment(GetSegment(_segmentCount - 1)))
                            {
                                _segmentLength -= GetSegment(--_segmentCount).Length;
                                ClearSegment(_segmentCount);
                            }
                            else if (!_root.HasRoot)
                            {
                                AddSegment(path, start, length);
                            }
                        }
                        else
                        {
                            AddSegment(path, start, length);
                        }
                    }

                    if (slash < 0)
                    {
                        return;
                    }

                    start = slash + 1;
                }
            }

            private void AddSegment(string text, int start, int length)
            {
                EnsureSegmentCapacity(_segmentCount + 1);
                SetSegment(_segmentCount, new SegmentPiece(text, start, length));
                _segmentCount++;
                _segmentLength += length;
            }

            private void EnsureSegmentCapacity(int capacity)
            {
                if (capacity <= InlineSegments.Capacity)
                {
                    return;
                }

                if (_segments == null)
                {
                    _segments = ArrayPool<SegmentPiece>.Shared.Rent(Math.Max(16, capacity));
                    for (var i = 0; i < _segmentCount; i++)
                    {
                        _segments[i] = _inlineSegments[i];
                    }

                    _inlineSegments = default;
                    return;
                }

                if (capacity > _segments.Length)
                {
                    var current = _segments;
                    _segments = ArrayPool<SegmentPiece>.Shared.Rent(Math.Max(current.Length * 2, capacity));
                    Array.Copy(current, _segments, _segmentCount);
                    Array.Clear(current, 0, _segmentCount);
                    ArrayPool<SegmentPiece>.Shared.Return(current);
                }
            }

            private SegmentPiece GetSegment(int index)
            {
                return _segments == null ? _inlineSegments[index] : _segments[index];
            }

            private void SetSegment(int index, SegmentPiece segment)
            {
                if (_segments == null)
                {
                    _inlineSegments[index] = segment;
                    return;
                }

                _segments[index] = segment;
            }

            private void ClearSegment(int index)
            {
                if (_segments == null)
                {
                    _inlineSegments[index] = default;
                    return;
                }

                _segments[index] = default;
            }

            private static bool IsParentSegment(SegmentPiece segment)
            {
                return ScriptPath.IsParentSegment(segment.Text, segment.Start, segment.Length);
            }
        }

        private static string NormalizeSeparators(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.IndexOf('\\') < 0 ? value : value.Replace('\\', '/');
        }

        private static string TrimTrailingSlashNormalizedText(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var root = ParseRootText(path.AsSpan());
            var minLength = root.HasLeadingSlash ? 1 : root.PrefixLength;
            var end = path.Length;
            while (end > minLength && path[end - 1] == '/')
            {
                end--;
            }

            return end == path.Length ? path : path.Substring(0, end);
        }

        private static bool UsesOrdinalIgnoreCaseText(string left, string right)
        {
            return OperatingSystem.IsWindows() && !HasUriScheme(left) && !HasUriScheme(right);
        }

        private static RootInfo ParseRootText(ReadOnlySpan<char> path)
        {
            if (path.Length == 0)
            {
                return default;
            }

            var colon = path.IndexOf(':');
            if (colon > 1 && HasUriScheme(path, colon))
            {
                if (colon + 2 < path.Length && IsSlash(path[colon + 1]) && IsSlash(path[colon + 2]))
                {
                    var authorityStart = colon + 3;
                    var relativeSlash = IndexOfSlash(path.Slice(authorityStart));
                    if (relativeSlash < 0)
                    {
                        return new RootInfo(path.Length, path.Length, hasRoot: true, hasScheme: true, hasDrive: false, hasLeadingSlash: false);
                    }

                    var nextSlash = authorityStart + relativeSlash;
                    return new RootInfo(nextSlash, nextSlash + 1, hasRoot: true, hasScheme: true, hasDrive: false, hasLeadingSlash: false);
                }

                return new RootInfo(colon + 1, colon + 1, hasRoot: true, hasScheme: true, hasDrive: false, hasLeadingSlash: false);
            }

            if (IsWindowsDrivePath(path))
            {
                var pathStart = path.Length > 2 && IsSlash(path[2]) ? 3 : 2;
                return new RootInfo(2, pathStart, hasRoot: true, hasScheme: false, hasDrive: true, hasLeadingSlash: false);
            }

            if (IsSlash(path[0]))
            {
                return new RootInfo(0, 1, hasRoot: true, hasScheme: false, hasDrive: false, hasLeadingSlash: true);
            }

            return default;
        }

        private static string GetRootPrefix(string path, RootInfo root)
        {
            if (root.HasLeadingSlash)
            {
                return "/";
            }

            if (root.PrefixLength == 0)
            {
                return string.Empty;
            }

            if (!ContainsBackslash(path, 0, root.PrefixLength))
            {
                return path.Substring(0, root.PrefixLength);
            }

            return string.Create(root.PrefixLength, path, static (destination, state) =>
            {
                CopyNormalized(state.AsSpan(0, destination.Length), destination);
            });
        }

        private static int GetExtensionStartNormalizedText(string normalizedPath, RootInfo root)
        {
            if (root.PathStart >= normalizedPath.Length)
            {
                return -1;
            }

            var lastSlash = normalizedPath.LastIndexOf('/');
            if (lastSlash < root.PathStart - 1)
            {
                lastSlash = root.PathStart - 1;
            }

            var lastDot = normalizedPath.LastIndexOf('.');
            return lastDot > lastSlash ? lastDot : -1;
        }

        private static int IndexOfSlash(string value, int start)
        {
            for (var i = start; i < value.Length; i++)
            {
                if (IsSlash(value[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int IndexOfSlash(ReadOnlySpan<char> value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (IsSlash(value[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool ContainsBackslash(string value, int start, int length)
        {
            return value.AsSpan(start, length).IndexOf('\\') >= 0;
        }

        private static void CopyNormalized(ReadOnlySpan<char> source, Span<char> destination)
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                destination[i] = c == '\\' ? '/' : c;
            }
        }

        private static bool IsCurrentSegment(string path, int start, int length)
        {
            return length == 1 && path[start] == '.';
        }

        private static bool IsParentSegment(string path, int start, int length)
        {
            return length == 2 && path[start] == '.' && path[start + 1] == '.';
        }

        private static bool TryJoinSimpleRelativeSegments(string normalizedBasePath, ReadOnlySpan<string> segments, out string result)
        {
            result = null;
            if (segments.Length > InlineSegments.Capacity)
            {
                return false;
            }

            var builder = new SimplePathJoinBuilder();
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (!TryAddSimpleRelativeTextSegment(ref builder, segment))
                {
                    return false;
                }
            }

            result = builder.ToStringAndReturn(normalizedBasePath);
            return true;
        }

        internal static bool TryAddSimpleRelativeTextSegment(ref SimplePathJoinBuilder builder, string segment)
        {
            if (string.IsNullOrEmpty(segment))
            {
                return true;
            }

            var kind = ClassifyTextSegment(segment);
            if (kind == TextSegmentKind.CurrentRelative)
            {
                return true;
            }

            return kind == TextSegmentKind.SimpleRelative && builder.TryAdd(segment);
        }

        private static TextSegmentKind ClassifyTextSegment(string segment)
        {
            if (segment.Length == 0)
            {
                return TextSegmentKind.CurrentRelative;
            }

            if (IsCurrentSegment(segment, 0, segment.Length))
            {
                return TextSegmentKind.CurrentRelative;
            }

            if (IsParentSegment(segment, 0, segment.Length))
            {
                return TextSegmentKind.RawRelative;
            }

            var first = segment[0];
            if (IsSlash(first) || (segment.Length >= 2 && segment[1] == ':'))
            {
                return TextSegmentKind.RootCandidate;
            }

            var canStartScheme = char.IsLetter(first);
            for (var i = 1; i < segment.Length; i++)
            {
                var c = segment[i];
                if (IsSlash(c))
                {
                    return TextSegmentKind.RawRelative;
                }

                if (canStartScheme && c == ':' && i > 1)
                {
                    return TextSegmentKind.RootCandidate;
                }
            }

            return TextSegmentKind.SimpleRelative;
        }

        private static bool IsWindowsDrivePath(ReadOnlySpan<char> path)
        {
            return path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':';
        }

        private static bool IsSlash(char value)
        {
            return value == '/' || value == '\\';
        }

        private static StringComparison PathComparison => OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        private readonly struct RootInfo
        {
            public RootInfo(int prefixLength, int pathStart, bool hasRoot, bool hasScheme, bool hasDrive, bool hasLeadingSlash)
            {
                PrefixLength = prefixLength;
                PathStart = pathStart;
                HasRoot = hasRoot;
                HasScheme = hasScheme;
                HasDrive = hasDrive;
                HasLeadingSlash = hasLeadingSlash;
            }

            public int PrefixLength { get; }
            public int PathStart { get; }
            public bool HasRoot { get; }
            public bool HasScheme { get; }
            public bool HasDrive { get; }
            public bool HasLeadingSlash { get; }
        }

        private readonly struct SegmentPiece
        {
            public SegmentPiece(string text, int start, int length)
            {
                Text = text;
                Start = start;
                Length = length;
            }

            public string Text { get; }
            public int Start { get; }
            public int Length { get; }
        }

        internal struct SimplePathJoinBuilder
        {
            public const int Capacity = InlineSegments.Capacity;

            private InlineSegments _segments;
            private int _segmentCount;
            private int _segmentLength;

            public SimplePathJoinBuilder()
            {
                _segments = default;
                _segmentCount = 0;
                _segmentLength = 0;
            }

            public bool TryAdd(string segment)
            {
                if (_segmentCount >= Capacity)
                {
                    return false;
                }

                _segments[_segmentCount] = new SegmentPiece(segment, 0, segment.Length);
                _segmentCount++;
                _segmentLength += segment.Length;
                return true;
            }

            public readonly string ToStringAndReturn(string normalizedBasePath)
            {
                if (_segmentCount == 0)
                {
                    return normalizedBasePath;
                }

                var length = normalizedBasePath.Length + _segmentLength + _segmentCount - 1;
                if (normalizedBasePath.Length > 0 && normalizedBasePath[normalizedBasePath.Length - 1] != '/')
                {
                    length++;
                }

                return string.Create(length, (normalizedBasePath, this), static (destination, state) =>
                {
                    var (basePath, builder) = state;
                    var position = 0;
                    if (basePath.Length > 0)
                    {
                        basePath.AsSpan().CopyTo(destination);
                        position = basePath.Length;
                    }

                    for (var i = 0; i < builder._segmentCount; i++)
                    {
                        if (i > 0 || (position > 0 && destination[position - 1] != '/'))
                        {
                            destination[position++] = '/';
                        }

                        var segment = builder._segments[i];
                        segment.Text.AsSpan(0, segment.Length).CopyTo(destination.Slice(position, segment.Length));
                        position += segment.Length;
                    }
                });
            }
        }

        private enum TextSegmentKind
        {
            CurrentRelative,
            SimpleRelative,
            RawRelative,
            RootCandidate
        }

        private struct InlineSegments
        {
            public const int Capacity = 8;

            private SegmentPiece _0;
            private SegmentPiece _1;
            private SegmentPiece _2;
            private SegmentPiece _3;
            private SegmentPiece _4;
            private SegmentPiece _5;
            private SegmentPiece _6;
            private SegmentPiece _7;

            public SegmentPiece this[int index]
            {
                readonly get
                {
                    return index switch
                    {
                        0 => _0,
                        1 => _1,
                        2 => _2,
                        3 => _3,
                        4 => _4,
                        5 => _5,
                        6 => _6,
                        7 => _7,
                        _ => throw new IndexOutOfRangeException()
                    };
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            _0 = value;
                            return;
                        case 1:
                            _1 = value;
                            return;
                        case 2:
                            _2 = value;
                            return;
                        case 3:
                            _3 = value;
                            return;
                        case 4:
                            _4 = value;
                            return;
                        case 5:
                            _5 = value;
                            return;
                        case 6:
                            _6 = value;
                            return;
                        case 7:
                            _7 = value;
                            return;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
            }
        }
    }
}
