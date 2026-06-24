using AuroraScript.Runtime.Serialization;
using System;
using System.IO;

namespace AuroraScript
{
    /// <summary>
    /// Specifies the available modes for compiling and executing script code.
    /// These modes determine how the compiled output is managed, accessed, and optimized.
    /// </summary>
    public enum CompilationMode
    {
        /// <summary>
        /// The compiled assembly is persistently output to a disk file or stream.
        /// Supports saving as a DLL/PDB for later loading.
        /// </summary>
        Persistence,

        /// <summary>
        /// The code is compiled and executed in-memory only.
        /// It cannot be directly debugged via standard PDB, but its IL can be dumped.
        /// </summary>
        OnlyRun,

        /// <summary>
        /// Compiled into a dynamic method. The generated code is invisible in memory 
        /// and undebuggable, offering the best performance and highest level of concealment.
        /// </summary>
        Dynamic
    }

    /// <summary>
    /// Specifies optimization levels for the code generation process.
    /// </summary>
    public enum OptimizeOptions
    {
        /// <summary>
        /// Disables inlining and JIT optimizations. Persistent mode supports debugging with full symbol information.
        /// </summary>
        Debug,

        /// <summary>
        /// Enables inlining and JIT optimizations to achieve higher runtime performance.
        /// </summary>
        Release
    }

    /// <summary>
    /// Specifies how runtime string values are materialized when converting CLR strings
    /// into <see cref="Runtime.Types.StringValue"/>.
    /// </summary>
    public enum StringPoolingStrategy
    {
        /// <summary>
        /// Always allocate a new <see cref="Runtime.Types.StringValue"/> wrapper.
        /// </summary>
        None,

        /// <summary>
        /// Use runtime string pool with weak references for reusable wrappers.
        /// </summary>
        Intern,
    }

    /// <summary>
    /// Represents the configuration settings for an <see cref="AuroraEngine"/>.
    /// This record provides a fluent interface for configuring compilation, optimization, and environment behavior.
    /// </summary>
    public record EngineOptions
    {
        /// <summary>
        /// Provides a default set of options for the engine.
        /// </summary>
        public static readonly EngineOptions Default = new();

        /// <summary>
        /// Gets the base directory path used for resolving relative script file or resource locations.
        /// </summary>
        public string BaseDirectory { get; init; }

        /// <summary>
        /// Gets the compilation mode, determining how the engine processes script sources.
        /// </summary>
        public CompilationMode CompilationMode { get; init; } = CompilationMode.OnlyRun;

        /// <summary>
        /// Gets the optimization level used during code generation.
        /// </summary>
        public OptimizeOptions OptimizeOption { get; init; } = OptimizeOptions.Release;

        /// <summary>
        /// Gets a value indicating whether runtime hot reload and dynamic patching are enabled.
        /// </summary>
        public bool EnableHotReload { get; init; } = true;

        /// <summary>
        /// Gets a value indicating whether the compiler may automatically infer direct calls
        /// for proven same-module internal functions without an explicit script annotation.
        /// Explicit @directCall annotations are not controlled by this option.
        /// </summary>
        public bool EnableAutoModuleDirectCall { get; init; }

        /// <summary>
        /// Gets a value indicating whether obfuscation (confusion) is enabled.
        /// When enabled:
        /// 1. Null, Number, and Boolean constants are hidden.
        /// 2. Type, method, and variable names are obfuscated to prevent reverse engineering.
        /// 3. Structural transformations are applied to hamper decompilation.
        /// </summary>
        public bool EnableConfused { get; init; } = false;

        /// <summary>
        /// Gets the JSON serializer used for script data serialization.
        /// </summary>
        public ScriptJsonSerializer JsonSerializer { get; init; } = ScriptJsonSerializer.Default;

        /// <summary>
        /// Gets or sets the standard date and time format string used within the engine.
        /// </summary>
        public string DateTimeFormat { get; init; } = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Gets the <see cref="TextWriter"/> used for standard console output (e.g., from script console.log).
        /// </summary>
        public TextWriter ConsoleStdOut { get; init; } = Console.Out;

        /// <summary>
        /// Gets the <see cref="TextWriter"/> used for error output.
        /// </summary>
        public TextWriter ConsoleErrorOut { get; init; } = Console.Error;

        /// <summary>
        /// Gets the target path for the generated script assembly when using Persistence mode.
        /// </summary>
        public string AssemblyOut { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the script file extension. Defaults to ".as".
        /// </summary>
        public string ExtName { get; set; } = ".as";

        /// <summary>
        /// Gets the strategy for allocating script string wrapper objects.
        /// </summary>
        public StringPoolingStrategy StringPooling { get; init; } = StringPoolingStrategy.Intern;

        /// <summary>
        /// Gets the maximum number of modules that may be parsed concurrently.
        /// A value of zero selects <see cref="Environment.ProcessorCount"/>.
        /// </summary>
        public int MaxDegreeOfParallelism { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineOptions"/> record.
        /// </summary>
        public EngineOptions()
        {
        }

        /// <summary>
        /// Configures the base directory and returns a new options instance.
        /// </summary>
        /// <param name="value">The base directory path.</param>
        /// <returns>A new <see cref="EngineOptions"/> instance with the updated base directory.</returns>
        public EngineOptions WithBaseDirectory(string value)
        {
            var _baseDirectory = Path.GetFullPath(value);
            return this with { BaseDirectory = _baseDirectory };
        }

        /// <summary>
        /// Configures the compilation mode and returns a new options instance.
        /// </summary>
        /// <param name="value">The compilation mode to use.</param>
        /// <returns>A new <see cref="EngineOptions"/> instance with the updated compilation mode.</returns>
        public EngineOptions WithCompilationMode(CompilationMode value)
        {
            return this with { CompilationMode = value };
        }

        /// <summary>
        /// Configures the optimization options and returns a new options instance.
        /// </summary>
        /// <param name="value">The optimization option to use.</param>
        /// <returns>A new <see cref="EngineOptions"/> instance with the updated optimization option.</returns>
        public EngineOptions WithOptimizeOption(OptimizeOptions value)
        {
            return this with { OptimizeOption = value };
        }

        /// <summary>
        /// Sets whether runtime hot reload and dynamic patching are enabled and returns a new options instance.
        /// </summary>
        /// <param name="value">True to allow runtime hot patching; false to reject dynamic patches.</param>
        /// <returns>A new <see cref="EngineOptions"/> instance with the updated hot reload setting.</returns>
        public EngineOptions WithEnableHotReload(bool value)
        {
            return this with { EnableHotReload = value };
        }

        /// <summary>
        /// Sets whether same-module internal direct-call inference is enabled and returns a new options instance.
        /// Explicit @directCall annotations are not controlled by this option.
        /// </summary>
        public EngineOptions WithEnableAutoModuleDirectCall(bool value)
        {
            return this with { EnableAutoModuleDirectCall = value };
        }

        /// <summary>
        /// Configures the JSON serializer and returns a new options instance.
        /// </summary>
        /// <param name="value">The script JSON serializer to use.</param>
        /// <returns>A new <see cref="EngineOptions"/> instance with the updated serializer.</returns>
        /// <exception cref="AuroraException">Thrown if the <paramref name="value"/> is null.</exception>
        public EngineOptions WithJsonSerializer(ScriptJsonSerializer value)
        {
            if (value == null)
            {
                throw new AuroraException("Parameter value is not allowed to be empty");
            }
            return this with { JsonSerializer = value };
        }

        /// <summary>
        /// Configures the date time format and returns a new options instance.
        /// </summary>
        /// <param name="value">The date and time format string.</param>
        /// <returns>A new <see cref="EngineOptions"/> instance with the updated date format.</returns>
        public EngineOptions WithDateTimeFormat(string value)
        {
            return this with { DateTimeFormat = value };
        }

        /// <summary>
        /// Configures the standard output writer and returns a new options instance.
        /// </summary>
        /// <param name="value">The text writer for standard output.</param>
        /// <returns>A new <see cref="EngineOptions"/> instance with the updated output writer.</returns>
        public EngineOptions WithConsoleStdOut(TextWriter value)
        {
            return this with { ConsoleStdOut = value };
        }

        /// <summary>
        /// Configures the error output writer and returns a new options instance.
        /// </summary>
        /// <param name="value">The text writer for error output.</param>
        /// <returns>A new <see cref="EngineOptions"/> instance with the updated error writer.</returns>
        public EngineOptions WithConsoleErrorOut(TextWriter value)
        {
            return this with { ConsoleErrorOut = value };
        }

        /// <summary>
        /// Configures the assembly output path and returns a new options instance.
        /// </summary>
        /// <param name="value">The path where the persistent assembly will be saved.</param>
        /// <returns>A new <see cref="EngineOptions"/> instance with the updated assembly path.</returns>
        public EngineOptions WithAssemblyOut(string value)
        {
            return this with { AssemblyOut = value };
        }

        /// <summary>
        /// Sets whether obfuscation/confusion is enabled and returns a new options instance.
        /// </summary>
        /// <param name="value">True to enable obfuscation; otherwise, false.</param>
        /// <returns>A new <see cref="EngineOptions"/> instance with the updated confusion setting.</returns>
        public EngineOptions WithEnableConfused(bool value)
        {
            return this with { EnableConfused = value };
        }

        /// <summary>
        /// Configures the script file extension and returns a new options instance.
        /// </summary>
        /// <param name="value">The file extension (e.g., ".as").</param>
        /// <returns>A new <see cref="EngineOptions"/> instance with the updated extension.</returns>
        /// <exception cref="ArgumentException">Thrown if the extension is invalid.</exception>
        public EngineOptions WithExtName(string value)
        {
            if (!value.StartsWith('.')) value = "." + value;
            if (value.LastIndexOf('.') > 0)
            {
                throw new ArgumentException("Extensions can only start with \".\" or provide a sense of an extension", nameof(value));
            }
            return this with { ExtName = value };
        }

        /// <summary>
        /// Configures the string pooling strategy and returns a new options instance.
        /// </summary>
        /// <param name="value">The string pooling strategy.</param>
        /// <returns>A new <see cref="EngineOptions"/> instance with the updated setting.</returns>
        public EngineOptions WithStringPooling(StringPoolingStrategy value)
        {
            return this with { StringPooling = value };
        }

        /// <summary>
        /// Configures the maximum number of concurrently parsed modules.
        /// Use zero to select the processor count automatically.
        /// </summary>
        public EngineOptions WithMaxDegreeOfParallelism(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The maximum degree of parallelism cannot be negative.");
            }
            return this with { MaxDegreeOfParallelism = value };
        }
    }
}
