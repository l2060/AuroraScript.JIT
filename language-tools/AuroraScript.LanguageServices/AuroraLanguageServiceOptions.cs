using AuroraScript.Core;
using AuroraScript.LanguageServices.Builtins;
using System;
using System.IO;

namespace AuroraScript.LanguageServices;

/// <summary>
/// Configures the AuroraScript language service workspace, parser, and source resolution behavior.
/// </summary>
public sealed class AuroraLanguageServiceOptions
{
    private string _baseDirectory;
    private string _extension;
    private IScriptSourceResolver _sourceResolver;

    /// <summary>
    /// Creates language-service options with the required builtin API catalog.
    /// </summary>
    public AuroraLanguageServiceOptions(BuiltinApiCatalog builtins)
    {
        Builtins = builtins ?? throw new ArgumentNullException(nameof(builtins));
        _baseDirectory = Directory.GetCurrentDirectory();
        _extension = ".as";
        _sourceResolver = FileScriptSourceResolver.Instance;
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
    /// </summary>
    public IScriptSourceResolver SourceResolver
    {
        get => _sourceResolver;
        init => _sourceResolver = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal EngineOptions ToEngineOptions()
    {
        return EngineOptions.Default.WithCompiler(compiler =>
        {
            compiler.Directory = BaseDirectory;
            compiler.ExtName = Extension;
            compiler.SourceResolver = SourceResolver;
        });
    }
}
