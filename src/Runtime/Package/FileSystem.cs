using AuroraScript.Runtime.Types;
using System;
using System.IO;
using System.Security;

namespace AuroraScript.Runtime.Package
{
    internal static class FileSystemModule
    {
        internal static void Configure(ScriptModule module)
        {
            module.Define("readText", ScriptDatum.FromBonding(READ_TEXT), writeable: false, enumerable: false);
            module.Define("readBytes", ScriptDatum.FromBonding(READ_BYTES), writeable: false, enumerable: false);
            module.Define("writeText", ScriptDatum.FromBonding(WRITE_TEXT), writeable: false, enumerable: false);
            module.Define("writeBytes", ScriptDatum.FromBonding(WRITE_BYTES), writeable: false, enumerable: false);
            module.Define("appendText", ScriptDatum.FromBonding(APPEND_TEXT), writeable: false, enumerable: false);
            module.Define("appendBytes", ScriptDatum.FromBonding(APPEND_BYTES), writeable: false, enumerable: false);
            module.Define("exist", ScriptDatum.FromBonding(EXIST), writeable: false, enumerable: false);
            module.Define("isFile", ScriptDatum.FromBonding(IS_FILE), writeable: false, enumerable: false);
            module.Define("isDir", ScriptDatum.FromBonding(IS_DIR), writeable: false, enumerable: false);
            module.Define("size", ScriptDatum.FromBonding(SIZE), writeable: false, enumerable: false);
            module.Define("mkDir", ScriptDatum.FromBonding(MK_DIR), writeable: false, enumerable: false);
            module.Define("dir", ScriptDatum.FromBonding(DIR), writeable: false, enumerable: false);
            module.Define("copy", ScriptDatum.FromBonding(COPY), writeable: false, enumerable: false);
            module.Define("move", ScriptDatum.FromBonding(MOVE), writeable: false, enumerable: false);
            module.Define("delete", ScriptDatum.FromBonding(DELETE), writeable: false, enumerable: false);
        }

        public static void READ_TEXT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "readText", "path");

            try
            {
                ScriptDatum.WriteAsString(ref result, File.ReadAllText(path));
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("readText", path, exception);
            }
        }

        public static void READ_BYTES(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "readBytes", "path");

            try
            {
                ScriptDatum.WriteAsObject(ref result, new ScriptUInt8Array(File.ReadAllBytes(path)));
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("readBytes", path, exception);
            }
        }

        public static void WRITE_TEXT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "writeText", "path");
            var text = RequireString(args, 1, "writeText", "text");

            try
            {
                File.WriteAllText(path, text);
                ScriptDatum.WriteAsBoolean(ref result, true);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("writeText", path, exception);
            }
        }

        public static void WRITE_BYTES(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "writeBytes", "path");
            var bytes = RequireBytes(args, 1, "writeBytes");

            try
            {
                File.WriteAllBytes(path, bytes._items);
                ScriptDatum.WriteAsBoolean(ref result, true);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("writeBytes", path, exception);
            }
        }

        public static void APPEND_TEXT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "appendText", "path");
            var text = RequireString(args, 1, "appendText", "text");

            try
            {
                File.AppendAllText(path, text);
                ScriptDatum.WriteAsBoolean(ref result, true);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("appendText", path, exception);
            }
        }

        public static void APPEND_BYTES(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "appendBytes", "path");
            var bytes = RequireBytes(args, 1, "appendBytes");

            try
            {
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                stream.Write(bytes._items, 0, bytes._items.Length);
                ScriptDatum.WriteAsBoolean(ref result, true);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("appendBytes", path, exception);
            }
        }

        public static void EXIST(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "exist", "path");
            ScriptDatum.WriteAsBoolean(ref result, File.Exists(path) || Directory.Exists(path));
        }

        public static void IS_FILE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "isFile", "path");
            ScriptDatum.WriteAsBoolean(ref result, File.Exists(path));
        }

        public static void IS_DIR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "isDir", "path");
            ScriptDatum.WriteAsBoolean(ref result, Directory.Exists(path));
        }

        public static void SIZE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "size", "path");

            try
            {
                if (Directory.Exists(path))
                {
                    throw new IOException("The path is a directory, not a file.");
                }

                var file = new FileInfo(path);
                if (!file.Exists)
                {
                    throw new FileNotFoundException("The file does not exist.", path);
                }

                ScriptDatum.WriteAsNumber(ref result, file.Length);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("size", path, exception);
            }
        }

        public static void MK_DIR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "mkDir", "path");

            try
            {
                Directory.CreateDirectory(path);
                ScriptDatum.WriteAsBoolean(ref result, true);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("mkDir", path, exception);
            }
        }

        public static void DIR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "dir", "path");

            try
            {
                var entries = Directory.GetFileSystemEntries(path);
                Array.Sort(entries, StringComparer.Ordinal);
                var names = ScriptArray.CreateEmptyWithCapacity(entries.Length);
                for (var i = 0; i < entries.Length; i++)
                {
                    names.Push(ScriptDatum.FromString(Path.GetFileName(entries[i])));
                }

                ScriptDatum.WriteAsArray(ref result, names);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("dir", path, exception);
            }
        }

        public static void COPY(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var source = RequirePath(args, 0, "copy", "source");
            var destination = RequirePath(args, 1, "copy", "destination");
            var overwrite = GetOptionalBoolean(args, 2, "copy", "overwrite");

            try
            {
                if (File.Exists(source))
                {
                    EnsureDifferentPaths(source, destination);
                    File.Copy(source, destination, overwrite);
                }
                else if (Directory.Exists(source))
                {
                    CopyDirectory(source, destination, overwrite);
                }
                else
                {
                    throw new FileNotFoundException("The source path does not exist.", source);
                }

                ScriptDatum.WriteAsBoolean(ref result, true);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("copy", source, destination, exception);
            }
        }

        public static void MOVE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var source = RequirePath(args, 0, "move", "source");
            var destination = RequirePath(args, 1, "move", "destination");
            var overwrite = GetOptionalBoolean(args, 2, "move", "overwrite");

            try
            {
                if (File.Exists(source))
                {
                    EnsureDifferentPaths(source, destination);
                    File.Move(source, destination, overwrite);
                }
                else if (Directory.Exists(source))
                {
                    MoveDirectory(source, destination, overwrite);
                }
                else
                {
                    throw new FileNotFoundException("The source path does not exist.", source);
                }

                ScriptDatum.WriteAsBoolean(ref result, true);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("move", source, destination, exception);
            }
        }

        public static void DELETE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var path = RequirePath(args, 0, "delete", "path");
            var recursive = GetOptionalBoolean(args, 1, "delete", "recursive");

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    ScriptDatum.WriteAsBoolean(ref result, true);
                    return;
                }

                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive);
                    ScriptDatum.WriteAsBoolean(ref result, true);
                    return;
                }

                ScriptDatum.WriteAsBoolean(ref result, false);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("delete", path, exception);
            }
        }

        private static string RequirePath(
            Span<ScriptDatum> args,
            int index,
            string method,
            string parameter)
        {
            string path;
            if ((uint)index < (uint)args.Length && args[index].Object is ScriptPathValue pathValue)
            {
                path = pathValue.Value;
            }
            else if ((uint)index < (uint)args.Length && args[index].Kind == ValueKind.String)
            {
                path = args[index].StringText;
            }
            else
            {
                throw new AuroraRuntimeException(
                    $"fs.{method} requires '{parameter}' to be a non-empty string or Path.");
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new AuroraRuntimeException(
                    $"fs.{method} requires '{parameter}' to be a non-empty string or Path.");
            }

            return path;
        }

        private static string RequireString(
            Span<ScriptDatum> args,
            int index,
            string method,
            string parameter)
        {
            if ((uint)index >= (uint)args.Length || args[index].Kind != ValueKind.String)
            {
                throw new AuroraRuntimeException(
                    $"fs.{method} requires '{parameter}' to be a string.");
            }

            return args[index].StringText;
        }

        private static ScriptUInt8Array RequireBytes(
            Span<ScriptDatum> args,
            int index,
            string method)
        {
            if ((uint)index >= (uint)args.Length || args[index].Object is not ScriptUInt8Array bytes)
            {
                throw new AuroraRuntimeException(
                    $"fs.{method} requires 'bytes' to be a UInt8Array.");
            }

            return bytes;
        }

        private static bool GetOptionalBoolean(
            Span<ScriptDatum> args,
            int index,
            string method,
            string parameter)
        {
            if ((uint)index >= (uint)args.Length)
            {
                return false;
            }

            if (args[index].Kind != ValueKind.Boolean)
            {
                throw new AuroraRuntimeException(
                    $"fs.{method} requires '{parameter}' to be a boolean when provided.");
            }

            return args[index].Boolean;
        }

        private static void CopyDirectory(string source, string destination, bool overwrite)
        {
            EnsureDirectoryDestinationOutsideSource(source, destination);

            if (File.Exists(destination))
            {
                throw new IOException("The destination path is an existing file.");
            }

            if (Directory.Exists(destination) && !overwrite)
            {
                throw new IOException("The destination directory already exists.");
            }

            CopyDirectoryContents(new DirectoryInfo(source), destination, overwrite);
        }

        private static void CopyDirectoryContents(
            DirectoryInfo source,
            string destination,
            bool overwrite)
        {
            if ((source.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new NotSupportedException(
                    $"Copying the directory link '{source.FullName}' is not supported.");
            }

            Directory.CreateDirectory(destination);
            foreach (var entry in source.EnumerateFileSystemInfos())
            {
                var target = Path.Combine(destination, entry.Name);
                if (entry is DirectoryInfo directory)
                {
                    CopyDirectoryContents(directory, target, overwrite);
                }
                else if (entry is FileInfo file)
                {
                    file.CopyTo(target, overwrite);
                }
            }
        }

        private static void MoveDirectory(string source, string destination, bool overwrite)
        {
            EnsureDirectoryDestinationOutsideSource(source, destination);

            if (File.Exists(destination))
            {
                throw new IOException("The destination path is an existing file.");
            }

            if (Directory.Exists(destination))
            {
                if (!overwrite)
                {
                    throw new IOException("The destination directory already exists.");
                }

                Directory.Delete(destination, recursive: true);
            }

            Directory.Move(source, destination);
        }

        private static void EnsureDifferentPaths(string source, string destination)
        {
            var sourceFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
            var destinationFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destination));
            if (string.Equals(sourceFullPath, destinationFullPath, GetPathComparison()))
            {
                throw new IOException("The source and destination refer to the same path.");
            }
        }

        private static void EnsureDirectoryDestinationOutsideSource(string source, string destination)
        {
            var sourceFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
            var destinationFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destination));
            var comparison = GetPathComparison();
            if (string.Equals(sourceFullPath, destinationFullPath, comparison))
            {
                throw new IOException("The source and destination refer to the same path.");
            }

            var sourcePrefix = Path.EndsInDirectorySeparator(sourceFullPath)
                ? sourceFullPath
                : sourceFullPath + Path.DirectorySeparatorChar;
            if (destinationFullPath.StartsWith(sourcePrefix, comparison))
            {
                throw new IOException("The destination directory cannot be inside the source directory.");
            }
        }

        private static StringComparison GetPathComparison()
        {
            return OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
        }

        private static bool IsFileSystemException(Exception exception)
        {
            return exception is IOException or
                UnauthorizedAccessException or
                ArgumentException or
                NotSupportedException or
                SecurityException;
        }

        private static AuroraRuntimeException CreateFileSystemError(
            string method,
            string path,
            Exception exception)
        {
            return new AuroraRuntimeException(
                $"fs.{method} failed for '{path}': {exception.Message}");
        }

        private static AuroraRuntimeException CreateFileSystemError(
            string method,
            string source,
            string destination,
            Exception exception)
        {
            return new AuroraRuntimeException(
                $"fs.{method} failed for '{source}' -> '{destination}': {exception.Message}");
        }
    }
}
