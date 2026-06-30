using AuroraScript.Core;
using AuroraScript.Source;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.Compiler.GlobalDeclarations
{
    internal enum AuroraScriptFileKind
    {
        Unknown,
        Module,
        Global
    }

    internal enum GlobalDeclarationKind
    {
        Const,
        Var,
        Function
    }

    internal sealed class GlobalDeclarationInfo
    {
        public GlobalDeclarationInfo(
            string name,
            GlobalDeclarationKind kind,
            string filePath,
            SourceSpan nameRange,
            SourceSpan declarationRange)
        {
            Name = name;
            Kind = kind;
            FilePath = filePath;
            NameRange = nameRange;
            DeclarationRange = declarationRange;
        }

        public string Name { get; }
        public GlobalDeclarationKind Kind { get; }
        public string FilePath { get; }
        public SourceSpan NameRange { get; }
        public SourceSpan DeclarationRange { get; }
    }

    internal sealed class GlobalDeclarationIndex
    {
        public static readonly GlobalDeclarationIndex Empty = new(
            new Dictionary<string, GlobalDeclarationInfo>(StringComparer.Ordinal),
            Array.Empty<AuroraCompilationDiagnostic>());

        public GlobalDeclarationIndex(
            IReadOnlyDictionary<string, GlobalDeclarationInfo> declarations,
            IReadOnlyList<AuroraCompilationDiagnostic> diagnostics)
        {
            Declarations = declarations ?? new Dictionary<string, GlobalDeclarationInfo>(StringComparer.Ordinal);
            Diagnostics = diagnostics ?? Array.Empty<AuroraCompilationDiagnostic>();
        }

        public IReadOnlyDictionary<string, GlobalDeclarationInfo> Declarations { get; }
        public IReadOnlyList<AuroraCompilationDiagnostic> Diagnostics { get; }

        public bool TryGet(string name, out GlobalDeclarationInfo declaration)
        {
            return Declarations.TryGetValue(name, out declaration);
        }
    }

    internal sealed class GlobalDeclarationWorkspaceIndexBuilder
    {
        private readonly Dictionary<string, GlobalDeclarationInfo> _declarations = new(StringComparer.Ordinal);
        private readonly List<AuroraCompilationDiagnostic> _diagnostics = new();
        private readonly HashSet<string> _files = new(ScriptPath.Comparer);

        public IReadOnlyDictionary<string, GlobalDeclarationInfo> Declarations => _declarations;
        public IReadOnlyList<AuroraCompilationDiagnostic> Diagnostics => _diagnostics;

        public void AddFile(string filePath, string text)
        {
            if (string.IsNullOrWhiteSpace(filePath) ||
                !_files.Add(ScriptPath.NormalizeFullPath(filePath)))
            {
                return;
            }

            var result = GlobalDeclarationScanner.Scan(filePath, text);
            if (result.Kind != AuroraScriptFileKind.Global)
            {
                return;
            }

            AddDiagnostics(result.Diagnostics);
            for (var i = 0; i < result.Declarations.Count; i++)
            {
                AddDeclaration(result.Declarations[i]);
            }
        }

        public GlobalDeclarationIndex ToIndex()
        {
            return new GlobalDeclarationIndex(
                new Dictionary<string, GlobalDeclarationInfo>(_declarations, StringComparer.Ordinal),
                _diagnostics.ToArray());
        }

        private void AddDeclaration(GlobalDeclarationInfo declaration)
        {
            if (!_declarations.TryGetValue(declaration.Name, out var existing))
            {
                _declarations.Add(declaration.Name, declaration);
                return;
            }

            _diagnostics.Add(GlobalDeclarationScanner.CreateDiagnostic(
                AuroraCompilationStage.Binding,
                declaration.NameRange,
                $"Duplicate global declaration '{declaration.Name}'. First declared in {existing.FilePath}."));
        }

        private void AddDiagnostics(IReadOnlyList<AuroraCompilationDiagnostic> diagnostics)
        {
            for (var i = 0; i < diagnostics.Count; i++)
            {
                _diagnostics.Add(diagnostics[i]);
            }
        }
    }

    internal sealed class GlobalDeclarationScanResult
    {
        public GlobalDeclarationScanResult(
            AuroraScriptFileKind kind,
            SourceSpan headerRange,
            IReadOnlyList<GlobalDeclarationInfo> declarations,
            IReadOnlyList<AuroraCompilationDiagnostic> diagnostics)
        {
            Kind = kind;
            HeaderRange = headerRange;
            Declarations = declarations ?? Array.Empty<GlobalDeclarationInfo>();
            Diagnostics = diagnostics ?? Array.Empty<AuroraCompilationDiagnostic>();
        }

        public AuroraScriptFileKind Kind { get; }
        public SourceSpan HeaderRange { get; }
        public IReadOnlyList<GlobalDeclarationInfo> Declarations { get; }
        public IReadOnlyList<AuroraCompilationDiagnostic> Diagnostics { get; }
    }

    internal static class GlobalDeclarationScanner
    {
        public static AuroraScriptFileKind DetectKind(string text)
        {
            return DetectKind(string.Empty, text).Kind;
        }

        public static GlobalDeclarationScanResult DetectKind(string filePath, string text)
        {
            text ??= string.Empty;
            var scanner = new Scanner(filePath, text);
            return scanner.DetectKindOnly();
        }

        public static bool IsGlobalFile(string text)
        {
            return DetectKind(text) == AuroraScriptFileKind.Global;
        }

        public static bool IsGlobalFile(ScriptSource source)
        {
            if (source == null)
            {
                return false;
            }

            return IsGlobalFile(source.ReadSource());
        }

        public static bool TryReadGlobalDeclarationSource(ScriptSource source, out string text)
        {
            text = string.Empty;
            if (source == null)
            {
                return false;
            }

            if (source is FileSource fileSource &&
                DetectFileHeaderKind(fileSource.FullPath, fileSource.Encoding) != AuroraScriptFileKind.Global)
            {
                return false;
            }

            text = source.ReadSource();
            return IsGlobalFile(text);
        }

        public static GlobalDeclarationScanResult Scan(string filePath, string text)
        {
            text ??= string.Empty;
            var scanner = new Scanner(filePath, text);
            return scanner.Scan();
        }

        public static GlobalDeclarationIndex BuildIndex(IEnumerable<(string Path, string Text)> documents)
        {
            var builder = new GlobalDeclarationWorkspaceIndexBuilder();
            foreach (var document in documents)
            {
                builder.AddFile(document.Path, document.Text);
            }

            return builder.ToIndex();
        }

        public static async Task<GlobalDeclarationIndex> BuildIndexAsync(
            IScriptSourceResolver resolver,
            string extension,
            CancellationToken cancellationToken = default)
        {
            if (resolver == null)
            {
                return GlobalDeclarationIndex.Empty;
            }

            var builder = new GlobalDeclarationWorkspaceIndexBuilder();
            var resolverRoot = ScriptPath.NormalizeBaseDirectory(resolver.Root);
            var query = new ScriptSourceQuery(extension, Encoding.UTF8);
            await foreach (var source in resolver.GetAllSourcesAsync(query, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourceRoot = string.IsNullOrWhiteSpace(source.BaseDirectory)
                    ? resolverRoot
                    : ScriptPath.NormalizeBaseDirectory(source.BaseDirectory);
                if (!IsProjectSource(sourceRoot, source.FullPath))
                {
                    continue;
                }

                try
                {
                    if (TryReadGlobalDeclarationSource(source, out var text))
                    {
                        builder.AddFile(source.FullPath, text);
                    }
                }
                catch (Exception ex) when (IsSourceReadFailure(ex))
                {
                }
            }

            return builder.ToIndex();
        }

        public static IEnumerable<string> EnumerateWorkspaceScriptFiles(string baseDirectory, string extension)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory) ||
                !Directory.Exists(baseDirectory))
            {
                yield break;
            }

            baseDirectory = ScriptPath.NormalizeBaseDirectory(baseDirectory);
            extension = ScriptResolveContext.NormalizeExtension(extension);
            var pending = new Queue<string>();
            pending.Enqueue(baseDirectory);
            while (pending.Count != 0)
            {
                var directory = pending.Dequeue();
                IEnumerable<string> childDirectories;
                try
                {
                    childDirectories = Directory.EnumerateDirectories(directory);
                }
                catch (Exception ex) when (IsSourceReadFailure(ex))
                {
                    continue;
                }

                foreach (var child in childDirectories)
                {
                    pending.Enqueue(child);
                }

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(directory, "*" + extension, SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex) when (IsSourceReadFailure(ex))
                {
                    continue;
                }

                foreach (var file in files)
                {
                    var normalized = ScriptPath.NormalizeFullPath(file);
                    if (IsProjectSource(baseDirectory, normalized))
                    {
                        yield return normalized;
                    }
                }
            }
        }

        public static bool IsProjectSource(string normalizedRoot, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(normalizedRoot) ||
                string.IsNullOrWhiteSpace(fullPath))
            {
                return false;
            }

            fullPath = ScriptPath.NormalizeFullPath(fullPath);
            normalizedRoot = ScriptPath.NormalizeBaseDirectory(normalizedRoot);
            return ScriptPath.IsWithinNormalizedRoot(normalizedRoot, fullPath);
        }

        public static AuroraCompilationDiagnostic CreateDiagnostic(
            AuroraCompilationStage stage,
            SourceSpan range,
            string message)
        {
            return new AuroraCompilationDiagnostic(stage, message, range);
        }

        private static bool IsSourceReadFailure(Exception exception)
        {
            return exception is FileNotFoundException
                or DirectoryNotFoundException
                or IOException
                or UnauthorizedAccessException
                or ArgumentException
                or NotSupportedException
                or KeyNotFoundException;
        }

        private static AuroraScriptFileKind DetectFileHeaderKind(string path, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return AuroraScriptFileKind.Unknown;
            }

            using var reader = new StreamReader(path, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            SkipReaderTrivia(reader, appendTo: null);
            if (reader.Peek() != '@')
            {
                return AuroraScriptFileKind.Unknown;
            }

            var header = new StringBuilder(64);
            header.Append((char)reader.Read());
            if (!TryReadReaderIdentifier(reader, header, out _))
            {
                return AuroraScriptFileKind.Unknown;
            }

            SkipReaderTrivia(reader, header);
            if (reader.Peek() != '(')
            {
                return AuroraScriptFileKind.Unknown;
            }

            header.Append((char)reader.Read());
            if (!ReadBalancedReaderParentheses(reader, header))
            {
                return AuroraScriptFileKind.Unknown;
            }

            SkipReaderTrivia(reader, header);
            if (reader.Peek() != ';')
            {
                return AuroraScriptFileKind.Unknown;
            }

            header.Append((char)reader.Read());
            return DetectKind(path, header.ToString()).Kind;
        }

        private static void SkipReaderTrivia(TextReader reader, StringBuilder appendTo)
        {
            while (true)
            {
                var current = reader.Peek();
                if (current < 0)
                {
                    return;
                }

                var value = (char)current;
                if (char.IsWhiteSpace(value))
                {
                    appendTo?.Append((char)reader.Read());
                    if (appendTo == null)
                    {
                        reader.Read();
                    }
                    continue;
                }

                if (value != '/')
                {
                    return;
                }

                var first = (char)reader.Read();
                var next = reader.Peek();
                if (next == '/')
                {
                    appendTo?.Append(first).Append((char)reader.Read());
                    if (appendTo == null)
                    {
                        reader.Read();
                    }
                    SkipReaderLineComment(reader, appendTo);
                    continue;
                }

                if (next == '*')
                {
                    appendTo?.Append(first).Append((char)reader.Read());
                    if (appendTo == null)
                    {
                        reader.Read();
                    }
                    SkipReaderBlockComment(reader, appendTo);
                    continue;
                }

                return;
            }
        }

        private static bool TryReadReaderIdentifier(TextReader reader, StringBuilder appendTo, out string value)
        {
            value = string.Empty;
            var current = reader.Peek();
            if (current < 0 || !IsIdentifierStart((char)current))
            {
                return false;
            }

            var start = appendTo.Length;
            appendTo.Append((char)reader.Read());
            while (reader.Peek() is var next && next >= 0 && IsIdentifierPart((char)next))
            {
                appendTo.Append((char)reader.Read());
            }

            value = appendTo.ToString(start, appendTo.Length - start);
            return true;
        }

        private static bool ReadBalancedReaderParentheses(TextReader reader, StringBuilder appendTo)
        {
            var depth = 1;
            while (true)
            {
                var current = reader.Peek();
                if (current < 0)
                {
                    return false;
                }

                var value = (char)reader.Read();
                appendTo.Append(value);
                if (value == '/' && reader.Peek() == '/')
                {
                    appendTo.Append((char)reader.Read());
                    SkipReaderLineComment(reader, appendTo);
                    continue;
                }

                if (value == '/' && reader.Peek() == '*')
                {
                    appendTo.Append((char)reader.Read());
                    SkipReaderBlockComment(reader, appendTo);
                    continue;
                }

                if (value == '"' || value == '\'' || value == '`')
                {
                    SkipReaderString(reader, appendTo, value);
                    continue;
                }

                if (value == '(')
                {
                    depth++;
                }
                else if (value == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return true;
                    }
                }
            }
        }

        private static void SkipReaderLineComment(TextReader reader, StringBuilder appendTo)
        {
            while (reader.Peek() is var current && current >= 0)
            {
                var value = (char)reader.Read();
                appendTo?.Append(value);
                if (value == '\r' || value == '\n')
                {
                    return;
                }
            }
        }

        private static void SkipReaderBlockComment(TextReader reader, StringBuilder appendTo)
        {
            var previous = '\0';
            while (reader.Peek() is var current && current >= 0)
            {
                var value = (char)reader.Read();
                appendTo?.Append(value);
                if (previous == '*' && value == '/')
                {
                    return;
                }

                previous = value;
            }
        }

        private static void SkipReaderString(TextReader reader, StringBuilder appendTo, char quote)
        {
            while (reader.Peek() is var current && current >= 0)
            {
                var value = (char)reader.Read();
                appendTo.Append(value);
                if (value == '\\' && reader.Peek() >= 0)
                {
                    appendTo.Append((char)reader.Read());
                    continue;
                }

                if (value == quote)
                {
                    return;
                }

                if (quote != '`' && (value == '\r' || value == '\n'))
                {
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIdentifierStart(char value)
        {
            return (value >= 'a' && value <= 'z') ||
                (value >= 'A' && value <= 'Z') ||
                value == '_' ||
                value == '$' ||
                (value >= 0x4e00 && value <= 0x9fbb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIdentifierPart(char value)
        {
            return IsIdentifierStart(value) || (value >= '0' && value <= '9');
        }

        private ref struct Scanner
        {
            private readonly string _filePath;
            private readonly string _text;
            private readonly List<AuroraCompilationDiagnostic> _diagnostics;
            private readonly List<GlobalDeclarationInfo> _declarations;
            private int _offset;
            private int _line;
            private int _column;

            public Scanner(string filePath, string text)
            {
                _filePath = filePath ?? string.Empty;
                _text = text ?? string.Empty;
                _diagnostics = new List<AuroraCompilationDiagnostic>();
                _declarations = new List<GlobalDeclarationInfo>();
                _offset = 0;
                _line = 1;
                _column = 1;
            }

            public GlobalDeclarationScanResult DetectKindOnly()
            {
                SkipTrivia();
                if (!TryReadAnnotationHeader(out var kind, out var headerRange, out _))
                {
                    return new GlobalDeclarationScanResult(
                        AuroraScriptFileKind.Unknown,
                        SourceSpan.None,
                        Array.Empty<GlobalDeclarationInfo>(),
                        Array.Empty<AuroraCompilationDiagnostic>());
                }

                return new GlobalDeclarationScanResult(
                    kind,
                    headerRange,
                    Array.Empty<GlobalDeclarationInfo>(),
                    Array.Empty<AuroraCompilationDiagnostic>());
            }

            public GlobalDeclarationScanResult Scan()
            {
                SkipTrivia();
                if (!TryReadAnnotationHeader(out var kind, out var headerRange, out var headerNameRange) ||
                    kind != AuroraScriptFileKind.Global)
                {
                    return new GlobalDeclarationScanResult(
                        kind,
                        headerRange,
                        Array.Empty<GlobalDeclarationInfo>(),
                        Array.Empty<AuroraCompilationDiagnostic>());
                }

                while (true)
                {
                    SkipTrivia();
                    if (IsAtEnd)
                    {
                        break;
                    }

                    if (Peek() == '@')
                    {
                        if (TryPeekAnnotationName(out var annotationName, out var annotationNameRange) &&
                            string.Equals(annotationName, "module", StringComparison.Ordinal))
                        {
                            AddDiagnostic(annotationNameRange, "@global() declaration files cannot also declare @module metadata.");
                            SkipStatement();
                            continue;
                        }

                        AddDiagnostic(CurrentSpan(1), "@global() declaration files only allow declare statements.");
                        SkipStatement();
                        continue;
                    }

                    var statementStart = CurrentPosition();
                    if (TryReadIdentifier(out var keyword, out var keywordRange))
                    {
                        if (string.Equals(keyword, "declare", StringComparison.Ordinal))
                        {
                            ParseDeclare(statementStart);
                            continue;
                        }

                        if (string.Equals(keyword, "export", StringComparison.Ordinal))
                        {
                            SkipTrivia();
                            if (TryPeekIdentifier("declare"))
                            {
                                AddDiagnostic(keywordRange, "export declare is not supported. Use declare inside an @global() file.");
                            }
                            else
                            {
                                AddDiagnostic(keywordRange, "@global() declaration files only allow declare statements.");
                            }

                            SkipStatement();
                            continue;
                        }

                        AddDiagnostic(keywordRange, "@global() declaration files only allow declare statements.");
                        SkipStatement();
                        continue;
                    }

                    AddDiagnostic(CurrentSpan(1), "@global() declaration files only allow declare statements.");
                    SkipStatement();
                }

                _ = headerNameRange;
                return new GlobalDeclarationScanResult(
                    AuroraScriptFileKind.Global,
                    headerRange,
                    _declarations.ToArray(),
                    _diagnostics.ToArray());
            }

            private void ParseDeclare(SourcePosition statementStart)
            {
                SkipTrivia();
                if (!TryReadIdentifier(out var kindText, out var kindRange))
                {
                    AddDiagnostic(CurrentSpan(1), "declare must be followed by const, var, or func.");
                    SkipStatement();
                    return;
                }

                if (string.Equals(kindText, "func", StringComparison.Ordinal) ||
                    string.Equals(kindText, "function", StringComparison.Ordinal))
                {
                    ParseDeclareFunction(statementStart);
                    return;
                }

                if (string.Equals(kindText, "const", StringComparison.Ordinal) ||
                    string.Equals(kindText, "var", StringComparison.Ordinal))
                {
                    ParseDeclareVariable(statementStart, kindText);
                    return;
                }

                AddDiagnostic(kindRange, "declare must be followed by const, var, or func.");
                SkipStatement();
            }

            private void ParseDeclareFunction(SourcePosition statementStart)
            {
                SkipTrivia();
                if (!TryReadIdentifier(out var name, out var nameRange))
                {
                    AddDiagnostic(CurrentSpan(1), "declare func requires a function name.");
                    SkipStatement();
                    return;
                }

                SkipTrivia();
                if (!ConsumeIf('('))
                {
                    AddDiagnostic(CurrentSpan(1), "declare func requires a parameter list.");
                    SkipStatement();
                    return;
                }

                if (!SkipBalancedParentheses())
                {
                    AddDiagnostic(CurrentSpan(1), "declare func parameter list must be closed with ')'.");
                    return;
                }

                SkipTrivia();
                if (!ConsumeIf(';'))
                {
                    AddDiagnostic(CurrentSpan(1), "declare func must end with ';'.");
                    SkipStatement();
                    return;
                }

                var declarationRange = SpanFrom(statementStart, PreviousPosition());
                _declarations.Add(new GlobalDeclarationInfo(
                    name,
                    GlobalDeclarationKind.Function,
                    _filePath,
                    nameRange,
                    declarationRange));
            }

            private void ParseDeclareVariable(SourcePosition statementStart, string kindText)
            {
                SkipTrivia();
                if (!TryReadIdentifier(out var name, out var nameRange))
                {
                    AddDiagnostic(CurrentSpan(1), "declare const/var requires a variable name.");
                    SkipStatement();
                    return;
                }

                SkipTrivia();
                if (!ConsumeIf(';'))
                {
                    AddDiagnostic(CurrentSpan(1), "declare const/var must contain a single name and end with ';'.");
                    SkipStatement();
                    return;
                }

                var declarationRange = SpanFrom(statementStart, PreviousPosition());
                _declarations.Add(new GlobalDeclarationInfo(
                    name,
                    string.Equals(kindText, "const", StringComparison.Ordinal)
                        ? GlobalDeclarationKind.Const
                        : GlobalDeclarationKind.Var,
                    _filePath,
                    nameRange,
                    declarationRange));
            }

            private bool TryReadAnnotationHeader(
                out AuroraScriptFileKind kind,
                out SourceSpan headerRange,
                out SourceSpan nameRange)
            {
                kind = AuroraScriptFileKind.Unknown;
                headerRange = SourceSpan.None;
                nameRange = SourceSpan.None;
                if (!ConsumeIf('@'))
                {
                    return false;
                }

                var start = PreviousPosition();
                if (!TryReadIdentifier(out var name, out nameRange))
                {
                    return false;
                }

                SkipTrivia();
                if (!ConsumeIf('('))
                {
                    return false;
                }

                var argumentStartOffset = _offset;
                var argumentLine = _line;
                var argumentColumn = _column;
                if (!SkipBalancedParentheses())
                {
                    return false;
                }

                var closeOffset = Math.Max(argumentStartOffset, _offset - 1);
                var closePosition = CurrentPosition();
                SkipTrivia();
                if (!ConsumeIf(';'))
                {
                    return false;
                }

                headerRange = SpanFrom(start, PreviousPosition());
                if (string.Equals(name, "global", StringComparison.Ordinal))
                {
                    kind = AuroraScriptFileKind.Global;
                    if (ContainsNonTrivia(argumentStartOffset, closeOffset))
                    {
                        var argumentRange = new SourceSpan
                        {
                            FileName = _filePath,
                            StartLine = argumentLine,
                            StartColumn = argumentColumn,
                            EndLine = closePosition.Line,
                            EndColumn = closePosition.Column,
                            Offset = argumentStartOffset,
                            Length = Math.Max(0, closeOffset - argumentStartOffset)
                        };
                        AddDiagnostic(argumentRange, "@global() does not accept arguments.");
                    }

                    return true;
                }

                if (string.Equals(name, "module", StringComparison.Ordinal))
                {
                    kind = AuroraScriptFileKind.Module;
                    return true;
                }

                return true;
            }

            private readonly bool ContainsNonTrivia(int start, int end)
            {
                while (start < end)
                {
                    var current = _text[start];
                    var next = start + 1 < end ? _text[start + 1] : '\0';
                    if (char.IsWhiteSpace(current))
                    {
                        start++;
                        continue;
                    }

                    if (current == '/' && next == '/')
                    {
                        start += 2;
                        while (start < end && _text[start] != '\r' && _text[start] != '\n')
                        {
                            start++;
                        }
                        continue;
                    }

                    if (current == '/' && next == '*')
                    {
                        start += 2;
                        while (start + 1 < end)
                        {
                            if (_text[start] == '*' && _text[start + 1] == '/')
                            {
                                start += 2;
                                break;
                            }

                            start++;
                        }
                        continue;
                    }

                    return true;
                }

                return false;
            }

            private bool TryPeekAnnotationName(out string name, out SourceSpan nameRange)
            {
                var savedOffset = _offset;
                var savedLine = _line;
                var savedColumn = _column;
                name = string.Empty;
                nameRange = SourceSpan.None;
                if (!ConsumeIf('@'))
                {
                    Restore(savedOffset, savedLine, savedColumn);
                    return false;
                }

                var result = TryReadIdentifier(out name, out nameRange);
                Restore(savedOffset, savedLine, savedColumn);
                return result;
            }

            private bool TryPeekIdentifier(string expected)
            {
                var savedOffset = _offset;
                var savedLine = _line;
                var savedColumn = _column;
                var result = TryReadIdentifier(out var value, out _) &&
                    string.Equals(value, expected, StringComparison.Ordinal);
                Restore(savedOffset, savedLine, savedColumn);
                return result;
            }

            private bool TryReadIdentifier(out string value, out SourceSpan range)
            {
                value = string.Empty;
                range = SourceSpan.None;
                if (IsAtEnd || !IsIdentifierStart(Peek()))
                {
                    return false;
                }

                var start = CurrentPosition();
                var startOffset = _offset;
                Advance();
                while (!IsAtEnd && IsIdentifierPart(Peek()))
                {
                    Advance();
                }

                value = _text.Substring(startOffset, _offset - startOffset);
                range = SpanFrom(start, CurrentPosition());
                return true;
            }

            private bool SkipBalancedParentheses()
            {
                var depth = 1;
                while (!IsAtEnd)
                {
                    var current = Peek();
                    var next = Peek(1);
                    if (current == '/' && next == '/')
                    {
                        SkipLineComment();
                        continue;
                    }

                    if (current == '/' && next == '*')
                    {
                        SkipBlockComment();
                        continue;
                    }

                    if (current == '"' || current == '\'' || current == '`')
                    {
                        SkipString(current);
                        continue;
                    }

                    Advance();
                    if (current == '(')
                    {
                        depth++;
                    }
                    else if (current == ')')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private void SkipStatement()
            {
                while (!IsAtEnd)
                {
                    var current = Peek();
                    var next = Peek(1);
                    if (current == '/' && next == '/')
                    {
                        SkipLineComment();
                        continue;
                    }

                    if (current == '/' && next == '*')
                    {
                        SkipBlockComment();
                        continue;
                    }

                    if (current == '"' || current == '\'' || current == '`')
                    {
                        SkipString(current);
                        continue;
                    }

                    Advance();
                    if (current == ';')
                    {
                        break;
                    }
                }
            }

            private void SkipTrivia()
            {
                while (!IsAtEnd)
                {
                    var current = Peek();
                    var next = Peek(1);
                    if (char.IsWhiteSpace(current))
                    {
                        Advance();
                        continue;
                    }

                    if (current == '/' && next == '/')
                    {
                        SkipLineComment();
                        continue;
                    }

                    if (current == '/' && next == '*')
                    {
                        SkipBlockComment();
                        continue;
                    }

                    break;
                }
            }

            private void SkipLineComment()
            {
                while (!IsAtEnd && Peek() != '\r' && Peek() != '\n')
                {
                    Advance();
                }
            }

            private void SkipBlockComment()
            {
                Advance();
                Advance();
                while (!IsAtEnd)
                {
                    if (Peek() == '*' && Peek(1) == '/')
                    {
                        Advance();
                        Advance();
                        return;
                    }

                    Advance();
                }
            }

            private void SkipString(char quote)
            {
                Advance();
                while (!IsAtEnd)
                {
                    var current = Peek();
                    if (current == '\\')
                    {
                        Advance();
                        if (!IsAtEnd)
                        {
                            Advance();
                        }
                        continue;
                    }

                    Advance();
                    if (current == quote)
                    {
                        return;
                    }

                    if (quote != '`' && (current == '\r' || current == '\n'))
                    {
                        return;
                    }
                }
            }

            private bool ConsumeIf(char value)
            {
                if (!IsAtEnd && Peek() == value)
                {
                    Advance();
                    return true;
                }

                return false;
            }

            private void AddDiagnostic(SourceSpan range, string message)
            {
                _diagnostics.Add(CreateDiagnostic(AuroraCompilationStage.Parsing, range, message));
            }

            private SourceSpan CurrentSpan(int length)
            {
                var start = CurrentPosition();
                return new SourceSpan
                {
                    FileName = _filePath,
                    StartLine = start.Line,
                    StartColumn = start.Column,
                    EndLine = start.Line,
                    EndColumn = start.Column + Math.Max(length, 0),
                    Offset = start.Offset,
                    Length = Math.Max(length, 0)
                };
            }

            private SourceSpan SpanFrom(SourcePosition start, SourcePosition end)
            {
                return new SourceSpan
                {
                    FileName = _filePath,
                    StartLine = start.Line,
                    StartColumn = start.Column,
                    EndLine = end.Line,
                    EndColumn = end.Column,
                    Offset = start.Offset,
                    Length = Math.Max(0, end.Offset - start.Offset)
                };
            }

            private SourcePosition CurrentPosition()
            {
                return new SourcePosition(_offset, _line, _column);
            }

            private SourcePosition PreviousPosition()
            {
                if (_offset <= 0)
                {
                    return CurrentPosition();
                }

                return new SourcePosition(_offset, _line, _column);
            }

            private void Restore(int offset, int line, int column)
            {
                _offset = offset;
                _line = line;
                _column = column;
            }

            private char Peek(int lookahead = 0)
            {
                var index = _offset + lookahead;
                return index >= 0 && index < _text.Length ? _text[index] : '\0';
            }

            private void Advance()
            {
                if (IsAtEnd)
                {
                    return;
                }

                var current = _text[_offset++];
                if (current == '\r')
                {
                    if (_offset < _text.Length && _text[_offset] == '\n')
                    {
                        _offset++;
                    }

                    _line++;
                    _column = 1;
                }
                else if (current == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
            }

            private bool IsAtEnd => _offset >= _text.Length;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsIdentifierStart(char value)
            {
                return (value >= 'a' && value <= 'z') ||
                    (value >= 'A' && value <= 'Z') ||
                    value == '_' ||
                    value == '$' ||
                    (value >= 0x4e00 && value <= 0x9fbb);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsIdentifierPart(char value)
            {
                return IsIdentifierStart(value) || (value >= '0' && value <= '9');
            }
        }

        private readonly struct SourcePosition
        {
            public SourcePosition(int offset, int line, int column)
            {
                Offset = offset;
                Line = line;
                Column = column;
            }

            public int Offset { get; }
            public int Line { get; }
            public int Column { get; }
        }
    }
}
