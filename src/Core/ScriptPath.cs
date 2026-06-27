using System;
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
            return string.IsNullOrWhiteSpace(baseDirectory)
                ? Directory.GetCurrentDirectory()
                : NormalizeFullPath(baseDirectory);
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
                : Path.GetFullPath(path);
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
                return Path.Combine(basePath, relativePath);
            }

            var uriBase = basePath.EndsWith("/", StringComparison.Ordinal)
                ? basePath
                : basePath + "/";
            if (Uri.TryCreate(uriBase, UriKind.Absolute, out var baseUri) &&
                Uri.TryCreate(baseUri, relativePath.Replace('\\', '/'), out var resolvedUri))
            {
                return NormalizeLogicalPath(resolvedUri.ToString());
            }

            return NormalizeLogicalPath(uriBase + relativePath.Replace('\\', '/'));
        }

        public static string GetDirectoryName(string path)
        {
            path = NormalizeFullPath(path);
            if (!HasUriScheme(path))
            {
                return Path.GetDirectoryName(path) ?? string.Empty;
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

            if (!path.StartsWith(basePath, PathComparison))
            {
                return false;
            }

            if (path.Length == basePath.Length)
            {
                return true;
            }

            var next = path[basePath.Length];
            return next == '/' || next == '\\';
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
                return Path.ChangeExtension(path, extension);
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
            return path.Replace('\\', '/');
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

            while (path.Length > minLength && (path[path.Length - 1] == '/' || path[path.Length - 1] == '\\'))
            {
                path = path.Substring(0, path.Length - 1);
            }

            return path;
        }

        private static StringComparison PathComparison => OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }
}
