using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.IO;
using System.Security;

namespace AuroraScript.Runtime.Package
{
    [NativeType("fs")]
    [NativePackage("fs")]
    public sealed partial class FileSystemSupport : ScriptObject
    {
        [Export("readText", DynamicAdapter = nameof(READ_TEXT))]
        public static string ReadTextCore(string path) => ReadAllText(RequireNonEmptyPath(path, "readText", "path"));

        [Export("readText", DynamicAdapter = nameof(READ_TEXT))]
        public static string ReadTextCore(ScriptPathValue path) => ReadTextCore(path.Value);

        [Export("readBytes", DynamicAdapter = nameof(READ_BYTES))]
        public static ScriptUInt8Array ReadBytesCore(string path) => new ScriptUInt8Array(ReadAllBytes(RequireNonEmptyPath(path, "readBytes", "path")));

        [Export("readBytes", DynamicAdapter = nameof(READ_BYTES))]
        public static ScriptUInt8Array ReadBytesCore(ScriptPathValue path) => ReadBytesCore(path.Value);

        [Export("writeText", DynamicAdapter = nameof(WRITE_TEXT))]
        public static bool WriteTextCore(string path, string text)
        {
            WriteAllText(RequireNonEmptyPath(path, "writeText", "path"), text ?? string.Empty);
            return true;
        }

        [Export("writeText", DynamicAdapter = nameof(WRITE_TEXT))]
        public static bool WriteTextCore(ScriptPathValue path, string text) => WriteTextCore(path.Value, text);

        [Export("writeBytes", DynamicAdapter = nameof(WRITE_BYTES))]
        public static bool WriteBytesCore(string path, ScriptUInt8Array bytes)
        {
            WriteAllBytes(RequireNonEmptyPath(path, "writeBytes", "path"), RequireBytes(bytes, "writeBytes"));
            return true;
        }

        [Export("writeBytes", DynamicAdapter = nameof(WRITE_BYTES))]
        public static bool WriteBytesCore(ScriptPathValue path, ScriptUInt8Array bytes) => WriteBytesCore(path.Value, bytes);

        [Export("appendText", DynamicAdapter = nameof(APPEND_TEXT))]
        public static bool AppendTextCore(string path, string text)
        {
            AppendAllText(RequireNonEmptyPath(path, "appendText", "path"), text ?? string.Empty);
            return true;
        }

        [Export("appendText", DynamicAdapter = nameof(APPEND_TEXT))]
        public static bool AppendTextCore(ScriptPathValue path, string text) => AppendTextCore(path.Value, text);

        [Export("appendBytes", DynamicAdapter = nameof(APPEND_BYTES))]
        public static bool AppendBytesCore(string path, ScriptUInt8Array bytes)
        {
            AppendAllBytes(RequireNonEmptyPath(path, "appendBytes", "path"), RequireBytes(bytes, "appendBytes"));
            return true;
        }

        [Export("appendBytes", DynamicAdapter = nameof(APPEND_BYTES))]
        public static bool AppendBytesCore(ScriptPathValue path, ScriptUInt8Array bytes) => AppendBytesCore(path.Value, bytes);

        [Export("exist", DynamicAdapter = nameof(EXIST))]
        public static bool ExistCore(string path)
        {
            path = RequireNonEmptyPath(path, "exist", "path");
            return File.Exists(path) || Directory.Exists(path);
        }

        [Export("exist", DynamicAdapter = nameof(EXIST))]
        public static bool ExistCore(ScriptPathValue path) => ExistCore(path.Value);

        [Export("isFile", DynamicAdapter = nameof(IS_FILE))]
        public static bool IsFileCore(string path) => File.Exists(RequireNonEmptyPath(path, "isFile", "path"));

        [Export("isFile", DynamicAdapter = nameof(IS_FILE))]
        public static bool IsFileCore(ScriptPathValue path) => IsFileCore(path.Value);

        [Export("isDir", DynamicAdapter = nameof(IS_DIR))]
        public static bool IsDirCore(string path) => Directory.Exists(RequireNonEmptyPath(path, "isDir", "path"));

        [Export("isDir", DynamicAdapter = nameof(IS_DIR))]
        public static bool IsDirCore(ScriptPathValue path) => IsDirCore(path.Value);

        [Export("size", DynamicAdapter = nameof(SIZE))]
        public static double SizeCore(string path) => GetFileLength(RequireNonEmptyPath(path, "size", "path"));

        [Export("size", DynamicAdapter = nameof(SIZE))]
        public static double SizeCore(ScriptPathValue path) => SizeCore(path.Value);

        [Export("mkDir", DynamicAdapter = nameof(MK_DIR))]
        public static bool MkDirCore(string path)
        {
            CreateDirectory(RequireNonEmptyPath(path, "mkDir", "path"));
            return true;
        }

        [Export("mkDir", DynamicAdapter = nameof(MK_DIR))]
        public static bool MkDirCore(ScriptPathValue path) => MkDirCore(path.Value);

        [Export("dir", DynamicAdapter = nameof(DIR))]
        public static ScriptArray DirCore(string path) => ListDirectory(RequireNonEmptyPath(path, "dir", "path"));

        [Export("dir", DynamicAdapter = nameof(DIR))]
        public static ScriptArray DirCore(ScriptPathValue path) => DirCore(path.Value);

        [Export("copy", DynamicAdapter = nameof(COPY))]
        public static bool CopyCore(string source, string destination, bool overwrite = false)
        {
            CopyPath(
                RequireNonEmptyPath(source, "copy", "source"),
                RequireNonEmptyPath(destination, "copy", "destination"),
                overwrite);
            return true;
        }

        [Export("copy", DynamicAdapter = nameof(COPY))]
        public static bool CopyCore(ScriptPathValue source, ScriptPathValue destination, bool overwrite = false)
            => CopyCore(source.Value, destination.Value, overwrite);

        [Export("move", DynamicAdapter = nameof(MOVE))]
        public static bool MoveCore(string source, string destination, bool overwrite = false)
        {
            MovePath(
                RequireNonEmptyPath(source, "move", "source"),
                RequireNonEmptyPath(destination, "move", "destination"),
                overwrite);
            return true;
        }

        [Export("move", DynamicAdapter = nameof(MOVE))]
        public static bool MoveCore(ScriptPathValue source, ScriptPathValue destination, bool overwrite = false)
            => MoveCore(source.Value, destination.Value, overwrite);

        [Export("delete", DynamicAdapter = nameof(DELETE))]
        public static bool DeleteCore(string path, bool recursive = false)
            => DeletePath(RequireNonEmptyPath(path, "delete", "path"), recursive);

        [Export("delete", DynamicAdapter = nameof(DELETE))]
        public static bool DeleteCore(ScriptPathValue path, bool recursive = false)
            => DeleteCore(path.Value, recursive);

        public static void READ_TEXT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsString(ref result, ReadTextCore(RequirePath(args, 0, "readText", "path")));
        }

        public static void READ_BYTES(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsObject(ref result, ReadBytesCore(RequirePath(args, 0, "readBytes", "path")));
        }

        public static void WRITE_TEXT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, WriteTextCore(
                RequirePath(args, 0, "writeText", "path"),
                RequireString(args, 1, "writeText", "text")));
        }

        public static void WRITE_BYTES(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, WriteBytesCore(
                RequirePath(args, 0, "writeBytes", "path"),
                RequireBytes(args, 1, "writeBytes")));
        }

        public static void APPEND_TEXT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, AppendTextCore(
                RequirePath(args, 0, "appendText", "path"),
                RequireString(args, 1, "appendText", "text")));
        }

        public static void APPEND_BYTES(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, AppendBytesCore(
                RequirePath(args, 0, "appendBytes", "path"),
                RequireBytes(args, 1, "appendBytes")));
        }

        public static void EXIST(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, ExistCore(RequirePath(args, 0, "exist", "path")));
        }

        public static void IS_FILE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, IsFileCore(RequirePath(args, 0, "isFile", "path")));
        }

        public static void IS_DIR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, IsDirCore(RequirePath(args, 0, "isDir", "path")));
        }

        public static void SIZE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsNumber(ref result, SizeCore(RequirePath(args, 0, "size", "path")));
        }

        public static void MK_DIR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, MkDirCore(RequirePath(args, 0, "mkDir", "path")));
        }

        public static void DIR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsArray(ref result, DirCore(RequirePath(args, 0, "dir", "path")));
        }

        public static void COPY(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, CopyCore(
                RequirePath(args, 0, "copy", "source"),
                RequirePath(args, 1, "copy", "destination"),
                GetOptionalBoolean(args, 2, "copy", "overwrite")));
        }

        public static void MOVE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, MoveCore(
                RequirePath(args, 0, "move", "source"),
                RequirePath(args, 1, "move", "destination"),
                GetOptionalBoolean(args, 2, "move", "overwrite")));
        }

        public static void DELETE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, DeleteCore(
                RequirePath(args, 0, "delete", "path"),
                GetOptionalBoolean(args, 1, "delete", "recursive")));
        }

        private static string ReadAllText(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("readText", path, exception);
            }
        }

        private static byte[] ReadAllBytes(string path)
        {
            try
            {
                return File.ReadAllBytes(path);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("readBytes", path, exception);
            }
        }

        private static void WriteAllText(string path, string text)
        {
            try
            {
                File.WriteAllText(path, text);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("writeText", path, exception);
            }
        }

        private static void WriteAllBytes(string path, ScriptUInt8Array bytes)
        {
            try
            {
                File.WriteAllBytes(path, bytes._items);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("writeBytes", path, exception);
            }
        }

        private static void AppendAllText(string path, string text)
        {
            try
            {
                File.AppendAllText(path, text);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("appendText", path, exception);
            }
        }

        private static void AppendAllBytes(string path, ScriptUInt8Array bytes)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                stream.Write(bytes._items, 0, bytes._items.Length);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("appendBytes", path, exception);
            }
        }

        private static double GetFileLength(string path)
        {
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

                return file.Length;
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("size", path, exception);
            }
        }

        private static void CreateDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("mkDir", path, exception);
            }
        }

        private static ScriptArray ListDirectory(string path)
        {
            try
            {
                var entries = Directory.GetFileSystemEntries(path);
                Array.Sort(entries, StringComparer.Ordinal);
                var names = ScriptArray.CreateEmptyWithCapacity(entries.Length);
                for (var i = 0; i < entries.Length; i++)
                {
                    names.Push(ScriptDatum.FromString(Path.GetFileName(entries[i])));
                }

                return names;
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("dir", path, exception);
            }
        }

        private static void CopyPath(string source, string destination, bool overwrite)
        {
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
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("copy", source, destination, exception);
            }
        }

        private static void MovePath(string source, string destination, bool overwrite)
        {
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
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                throw CreateFileSystemError("move", source, destination, exception);
            }
        }

        private static bool DeletePath(string path, bool recursive)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }

                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive);
                    return true;
                }

                return false;
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

            return RequireNonEmptyPath(path, method, parameter);
        }

        private static string RequireNonEmptyPath(string path, string method, string parameter)
        {
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

        private static ScriptUInt8Array RequireBytes(ScriptUInt8Array bytes, string method)
        {
            if (bytes == null)
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
