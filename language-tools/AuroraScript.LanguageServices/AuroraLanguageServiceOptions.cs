using AuroraScript.Core;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.Source;
using System;
using System.Globalization;
using System.IO;

namespace AuroraScript.LanguageServices;

/// <summary>
/// Configures the AuroraScript language service workspace, parser, and source resolution behavior.
/// </summary>
public sealed class AuroraLanguageServiceOptions
{
    private string _baseDirectory;
    private string _extension;
    private IScriptSourceResolver? _sourceResolver;
    private int _maxWorkspaceIndexFiles;
    private string _documentationLocale;

    /// <summary>
    /// Creates language-service options with the required builtin API catalog.
    /// </summary>
    public AuroraLanguageServiceOptions(BuiltinApiCatalog builtins)
    {
        Builtins = builtins ?? throw new ArgumentNullException(nameof(builtins));
        _baseDirectory = Directory.GetCurrentDirectory();
        _extension = ".as";
        _maxWorkspaceIndexFiles = 2000;
        _documentationLocale = GetDefaultDocumentationLocale();
    }

    /// <summary>
    /// Gets the builtin API catalog used by hover, completion, signature help, and semantic diagnostics.
    /// </summary>
    public BuiltinApiCatalog Builtins { get; }

    /// <summary>
    /// Gets the workspace root used for relative document paths and import resolution.
    /// </summary>
    public string BaseDirectory
    {
        get => _baseDirectory;
        init => _baseDirectory = string.IsNullOrWhiteSpace(value)
            ? Directory.GetCurrentDirectory()
            : ScriptPath.NormalizeBaseDirectory(value);
    }

    /// <summary>
    /// Gets the script file extension used when import/include statements omit an extension.
    /// </summary>
    public string Extension
    {
        get => _extension;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Script extension is required.", nameof(value));
            }

            _extension = value[0] == '.' ? value : "." + value;
        }
    }

    /// <summary>
    /// Gets the resolver used to load files that are not open in the workspace.
    /// Defaults to a file-system resolver rooted at <see cref="BaseDirectory"/>.
    /// </summary>
    public IScriptSourceResolver SourceResolver
    {
        get => _sourceResolver ?? ScriptSources.FileSystem(BaseDirectory);
        init => _sourceResolver = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets whether disk files under <see cref="BaseDirectory"/> are indexed for workspace-wide references and rename.
    /// </summary>
    public bool IndexWorkspaceFiles { get; init; }

    /// <summary>
    /// Gets the maximum number of workspace script files indexed from disk.
    /// </summary>
    public int MaxWorkspaceIndexFiles
    {
        get => _maxWorkspaceIndexFiles;
        init
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Workspace file index limit must be positive.");
            }

            _maxWorkspaceIndexFiles = value;
        }
    }

    /// <summary>
    /// Gets the locale used for builtin API and annotation documentation. Chinese locales use Chinese notes; other locales use English.
    /// </summary>
    public string DocumentationLocale
    {
        get => _documentationLocale;
        init => _documentationLocale = NormalizeDocumentationLocale(value);
    }

    internal bool HasExplicitSourceResolver => _sourceResolver != null;

    internal EngineOptions ToEngineOptions(string? baseDirectory = null)
    {
        return EngineOptions.Default.WithCompiler(compiler =>
        {
            compiler.ExtName = Extension;
            var sourceResolver = _sourceResolver ?? ScriptSources.FileSystem(baseDirectory ?? BaseDirectory);
            compiler.SourceResolver = Builtins.Modules.Count == 0
                ? sourceResolver
                : new BuiltinApiSourceResolver(Builtins, sourceResolver);
        });
    }

    internal static string NormalizeDocumentationLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return GetDefaultDocumentationLocale();
        }

        return locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? "zh-CN"
            : "en";
    }

    private static string GetDefaultDocumentationLocale()
    {
        return CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? "zh-CN"
            : "en";
    }
}
