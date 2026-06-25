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
    /// Represents all configuration for an <see cref="AuroraEngine"/>.
    /// Options are grouped by responsibility so runtime behavior, compiler behavior,
    /// optimization switches, and output settings do not share one flat namespace.
    /// </summary>
    public record EngineOptions
    {
        private const string LegacyApiMessage = "Use the grouped EngineOptions API: WithRuntime, WithCompiler, WithOptimization, or WithOutput.";

        private RuntimeOptions _runtime = RuntimeOptions.Default;
        private CompilerOptions _compiler = CompilerOptions.Default;
        private OptimizationOptions _optimization = OptimizationOptions.Default;
        private OutputOptions _output = OutputOptions.Default;

        /// <summary>
        /// Provides a default set of options for the engine.
        /// </summary>
        public static readonly EngineOptions Default = new();

        /// <summary>
        /// Runtime behavior used while scripts execute.
        /// </summary>
        public RuntimeOptions Runtime
        {
            get => _runtime;
            init => _runtime = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Compiler input and compilation mode settings.
        /// </summary>
        public CompilerOptions Compiler
        {
            get => _compiler;
            init => _compiler = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Compile-time optimization switches.
        /// </summary>
        public OptimizationOptions Optimization
        {
            get => _optimization;
            init => _optimization = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Generated output and emission settings.
        /// </summary>
        public OutputOptions Output
        {
            get => _output;
            init => _output = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets the base directory path used for resolving relative script files.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public string BaseDirectory
        {
            get => Compiler.BaseDirectory;
            init => _compiler = ConfigureCompiler(Compiler, compiler => compiler.Directory = value);
        }

        /// <summary>
        /// Gets the compilation mode, determining how the engine processes script sources.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public CompilationMode CompilationMode
        {
            get => Compiler.Mode;
            init => _compiler = Compiler with { Mode = value };
        }

        /// <summary>
        /// Gets the optimization level used during code generation.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public OptimizeOptions OptimizeOption
        {
            get => Optimization.Level;
            init => _optimization = Optimization with { Level = value };
        }

        /// <summary>
        /// Gets a value indicating whether runtime hot reload and dynamic patching are enabled.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public bool EnableHotReload
        {
            get => Runtime.EnableHotReload;
            init => _runtime = Runtime with { EnableHotReload = value };
        }

        /// <summary>
        /// Gets a value indicating whether same-module direct-call inference is enabled.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public bool EnableAutoModuleDirectCall
        {
            get => Optimization.EnableAutoModuleDirectCall;
            init => _optimization = Optimization with { EnableAutoModuleDirectCall = value };
        }

        /// <summary>
        /// Gets a value indicating whether eligible module-level const reads may be inlined.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public bool EnableModuleConstInlining
        {
            get => Optimization.EnableModuleConstInlining;
            init => _optimization = Optimization with { EnableModuleConstInlining = value };
        }

        /// <summary>
        /// Gets a value indicating whether obfuscation (confusion) is enabled.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public bool EnableConfused
        {
            get => Output.EnableConfused;
            init => _output = Output with { EnableConfused = value };
        }

        /// <summary>
        /// Gets the JSON serializer used for script data serialization.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public ScriptJsonSerializer JsonSerializer
        {
            get => Runtime.JsonSerializer;
            init => _runtime = ConfigureRuntime(Runtime, runtime => runtime.JsonSerializer = value);
        }

        /// <summary>
        /// Gets the standard date and time format string used within the engine.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public string DateTimeFormat
        {
            get => Runtime.DateTimeFormat;
            init => _runtime = ConfigureRuntime(Runtime, runtime => runtime.DateTimeFormat = value);
        }

        /// <summary>
        /// Gets the writer used for standard console output.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public TextWriter ConsoleStdOut
        {
            get => Runtime.ConsoleStdOut;
            init => _runtime = ConfigureRuntime(Runtime, runtime => runtime.ConsoleStdOut = value);
        }

        /// <summary>
        /// Gets the writer used for error console output.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public TextWriter ConsoleErrorOut
        {
            get => Runtime.ConsoleErrorOut;
            init => _runtime = ConfigureRuntime(Runtime, runtime => runtime.ConsoleErrorOut = value);
        }

        /// <summary>
        /// Gets the target path for the generated script assembly when using Persistence mode.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public string AssemblyOut
        {
            get => Output.AssemblyFile;
            init => _output = ConfigureOutput(Output, output => output.AssemblyFile = value);
        }

        /// <summary>
        /// Gets or sets the script file extension. Defaults to ".as".
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public string ExtName
        {
            get => Compiler.ExtName;
            set => _compiler = ConfigureCompiler(Compiler, compiler => compiler.ExtName = value);
        }

        /// <summary>
        /// Gets the strategy for allocating script string wrapper objects.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public StringPoolingStrategy StringPooling
        {
            get => Runtime.StringPooling;
            init => _runtime = Runtime with { StringPooling = value };
        }

        /// <summary>
        /// Gets the maximum number of modules that may be parsed concurrently.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public int MaxDegreeOfParallelism
        {
            get => Compiler.MaxDegreeOfParallelism;
            init => _compiler = ConfigureCompiler(Compiler, compiler => compiler.MaxDegreeOfParallelism = value);
        }

        /// <summary>
        /// Configures runtime behavior and returns a new immutable options instance.
        /// </summary>
        public EngineOptions WithRuntime(Action<RuntimeOptionsBuilder> configure)
        {
            return this with { Runtime = ConfigureRuntime(Runtime, configure) };
        }

        /// <summary>
        /// Configures compiler behavior and returns a new immutable options instance.
        /// </summary>
        public EngineOptions WithCompiler(Action<CompilerOptionsBuilder> configure)
        {
            return this with { Compiler = ConfigureCompiler(Compiler, configure) };
        }

        /// <summary>
        /// Configures compile-time optimizations and returns a new immutable options instance.
        /// </summary>
        public EngineOptions WithOptimization(Action<OptimizationOptionsBuilder> configure)
        {
            return this with { Optimization = ConfigureOptimization(Optimization, configure) };
        }

        /// <summary>
        /// Configures generated output behavior and returns a new immutable options instance.
        /// </summary>
        public EngineOptions WithOutput(Action<OutputOptionsBuilder> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            return this with { Output = ConfigureOutput(Output, configure) };
        }

        /// <summary>
        /// Configures the base directory and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithBaseDirectory(string value)
        {
            return WithCompiler(compiler => compiler.Directory = value);
        }

        /// <summary>
        /// Configures the compilation mode and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithCompilationMode(CompilationMode value)
        {
            return WithCompiler(compiler => compiler.Mode = value);
        }

        /// <summary>
        /// Configures the optimization level and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithOptimizeOption(OptimizeOptions value)
        {
            return WithOptimization(optimization => optimization.Level = value);
        }

        /// <summary>
        /// Sets whether runtime hot reload and dynamic patching are enabled and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithEnableHotReload(bool value)
        {
            return WithRuntime(runtime => runtime.HotReload = value);
        }

        /// <summary>
        /// Sets whether same-module direct-call inference is enabled and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithEnableAutoModuleDirectCall(bool value)
        {
            return WithOptimization(optimization => optimization.AutoModuleDirectCall = value);
        }

        /// <summary>
        /// Sets whether eligible module-level const reads may be inlined and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithEnableModuleConstInlining(bool value)
        {
            return WithOptimization(optimization => optimization.ModuleConstInlining = value);
        }

        /// <summary>
        /// Configures the JSON serializer and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithJsonSerializer(ScriptJsonSerializer value)
        {
            return WithRuntime(runtime => runtime.JsonSerializer = value);
        }

        /// <summary>
        /// Configures the date time format and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithDateTimeFormat(string value)
        {
            return WithRuntime(runtime => runtime.DateTimeFormat = value);
        }

        /// <summary>
        /// Configures the standard output writer and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithConsoleStdOut(TextWriter value)
        {
            return WithRuntime(runtime => runtime.ConsoleStdOut = value);
        }

        /// <summary>
        /// Configures the error output writer and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithConsoleErrorOut(TextWriter value)
        {
            return WithRuntime(runtime => runtime.ConsoleErrorOut = value);
        }

        /// <summary>
        /// Configures the assembly output path and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithAssemblyOut(string value)
        {
            return WithOutput(output => output.AssemblyFile = value);
        }

        /// <summary>
        /// Sets whether obfuscation/confusion is enabled and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithEnableConfused(bool value)
        {
            return WithOutput(output => output.Confused = value);
        }

        /// <summary>
        /// Configures the script file extension and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithExtName(string value)
        {
            return WithCompiler(compiler => compiler.ExtName = value);
        }

        /// <summary>
        /// Configures the string pooling strategy and returns a new options instance.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithStringPooling(StringPoolingStrategy value)
        {
            return WithRuntime(runtime => runtime.StringPooling = value);
        }

        /// <summary>
        /// Configures the maximum number of concurrently parsed modules.
        /// </summary>
        [Obsolete(LegacyApiMessage)]
        public EngineOptions WithMaxDegreeOfParallelism(int value)
        {
            return WithCompiler(compiler => compiler.MaxDegreeOfParallelism = value);
        }

        private static RuntimeOptions ConfigureRuntime(RuntimeOptions options, Action<RuntimeOptionsBuilder> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var builder = new RuntimeOptionsBuilder(options);
            configure(builder);
            return builder.ToOptions();
        }

        private static CompilerOptions ConfigureCompiler(CompilerOptions options, Action<CompilerOptionsBuilder> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var builder = new CompilerOptionsBuilder(options);
            configure(builder);
            return builder.ToOptions();
        }

        private static OptimizationOptions ConfigureOptimization(OptimizationOptions options, Action<OptimizationOptionsBuilder> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var builder = new OptimizationOptionsBuilder(options);
            configure(builder);
            return builder.ToOptions();
        }

        private static OutputOptions ConfigureOutput(OutputOptions options, Action<OutputOptionsBuilder> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var builder = new OutputOptionsBuilder(options);
            configure(builder);
            return builder.ToOptions();
        }
    }

    /// <summary>
    /// Runtime behavior used while scripts execute.
    /// </summary>
    public sealed record RuntimeOptions
    {
        /// <summary>
        /// Provides default runtime behavior.
        /// </summary>
        public static readonly RuntimeOptions Default = new();

        /// <summary>
        /// Gets a value indicating whether runtime hot reload and dynamic patching are enabled.
        /// </summary>
        public bool EnableHotReload { get; init; } = true;

        /// <summary>
        /// Gets the JSON serializer used for script data serialization.
        /// </summary>
        public ScriptJsonSerializer JsonSerializer { get; init; } = ScriptJsonSerializer.Default;

        /// <summary>
        /// Gets the standard date and time format string used within the engine.
        /// </summary>
        public string DateTimeFormat { get; init; } = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Gets the <see cref="TextWriter"/> used for standard console output.
        /// </summary>
        public TextWriter ConsoleStdOut { get; init; } = Console.Out;

        /// <summary>
        /// Gets the <see cref="TextWriter"/> used for error output.
        /// </summary>
        public TextWriter ConsoleErrorOut { get; init; } = Console.Error;

        /// <summary>
        /// Gets the strategy for allocating script string wrapper objects.
        /// </summary>
        public StringPoolingStrategy StringPooling { get; init; } = StringPoolingStrategy.Intern;
    }

    /// <summary>
    /// Mutable builder used by <see cref="EngineOptions.WithRuntime"/>.
    /// </summary>
    public sealed class RuntimeOptionsBuilder
    {
        private ScriptJsonSerializer _jsonSerializer;
        private string _dateTimeFormat;
        private TextWriter _consoleStdOut;
        private TextWriter _consoleErrorOut;

        /// <summary>
        /// Creates a mutable runtime-options builder from an immutable options snapshot.
        /// </summary>
        public RuntimeOptionsBuilder(RuntimeOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            HotReload = options.EnableHotReload;
            _jsonSerializer = options.JsonSerializer;
            _dateTimeFormat = options.DateTimeFormat;
            _consoleStdOut = options.ConsoleStdOut;
            _consoleErrorOut = options.ConsoleErrorOut;
            StringPooling = options.StringPooling;
        }

        /// <summary>
        /// Gets or sets whether runtime hot reload and dynamic patching are enabled.
        /// </summary>
        public bool HotReload { get; set; }

        /// <summary>
        /// Gets or sets the JSON serializer used for script data serialization.
        /// </summary>
        public ScriptJsonSerializer JsonSerializer
        {
            get => _jsonSerializer;
            set => _jsonSerializer = value ?? throw new AuroraException("Parameter value is not allowed to be empty");
        }

        /// <summary>
        /// Gets or sets the standard date and time format string used within the engine.
        /// </summary>
        public string DateTimeFormat
        {
            get => _dateTimeFormat;
            set => _dateTimeFormat = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the writer used for standard console output.
        /// </summary>
        public TextWriter ConsoleStdOut
        {
            get => _consoleStdOut;
            set => _consoleStdOut = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the writer used for error console output.
        /// </summary>
        public TextWriter ConsoleErrorOut
        {
            get => _consoleErrorOut;
            set => _consoleErrorOut = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the strategy for allocating script string wrapper objects.
        /// </summary>
        public StringPoolingStrategy StringPooling { get; set; }

        /// <summary>
        /// Sets whether runtime hot reload and dynamic patching are enabled.
        /// </summary>
        public RuntimeOptionsBuilder WithHotReload(bool value)
        {
            HotReload = value;
            return this;
        }

        /// <summary>
        /// Sets the JSON serializer used for script data serialization.
        /// </summary>
        public RuntimeOptionsBuilder WithJsonSerializer(ScriptJsonSerializer value)
        {
            JsonSerializer = value;
            return this;
        }

        /// <summary>
        /// Sets the standard date and time format string used within the engine.
        /// </summary>
        public RuntimeOptionsBuilder WithDateTimeFormat(string value)
        {
            DateTimeFormat = value;
            return this;
        }

        /// <summary>
        /// Sets the writer used for standard console output.
        /// </summary>
        public RuntimeOptionsBuilder WithConsoleStdOut(TextWriter value)
        {
            ConsoleStdOut = value;
            return this;
        }

        /// <summary>
        /// Sets the writer used for error console output.
        /// </summary>
        public RuntimeOptionsBuilder WithConsoleErrorOut(TextWriter value)
        {
            ConsoleErrorOut = value;
            return this;
        }

        /// <summary>
        /// Sets the strategy for allocating script string wrapper objects.
        /// </summary>
        public RuntimeOptionsBuilder WithStringPooling(StringPoolingStrategy value)
        {
            StringPooling = value;
            return this;
        }

        internal RuntimeOptions ToOptions()
        {
            return new RuntimeOptions
            {
                EnableHotReload = HotReload,
                JsonSerializer = JsonSerializer,
                DateTimeFormat = DateTimeFormat,
                ConsoleStdOut = ConsoleStdOut,
                ConsoleErrorOut = ConsoleErrorOut,
                StringPooling = StringPooling
            };
        }
    }

    /// <summary>
    /// Compiler input and compilation mode settings.
    /// </summary>
    public sealed record CompilerOptions
    {
        /// <summary>
        /// Provides default compiler behavior.
        /// </summary>
        public static readonly CompilerOptions Default = new();

        /// <summary>
        /// Gets the base directory path used for resolving relative script file or resource locations.
        /// </summary>
        public string BaseDirectory { get; init; } = string.Empty;

        /// <summary>
        /// Gets the compilation mode, determining how the engine processes script sources.
        /// </summary>
        public CompilationMode Mode { get; init; } = CompilationMode.OnlyRun;

        /// <summary>
        /// Gets the script file extension. Defaults to ".as".
        /// </summary>
        public string ExtName { get; init; } = ".as";

        /// <summary>
        /// Gets the maximum number of modules that may be parsed concurrently.
        /// A value of zero selects <see cref="Environment.ProcessorCount"/>.
        /// </summary>
        public int MaxDegreeOfParallelism { get; init; }
    }

    /// <summary>
    /// Mutable builder used by <see cref="EngineOptions.WithCompiler"/>.
    /// </summary>
    public sealed class CompilerOptionsBuilder
    {
        private string _baseDirectory;
        private string _extName;
        private int _maxDegreeOfParallelism;

        /// <summary>
        /// Creates a mutable compiler-options builder from an immutable options snapshot.
        /// </summary>
        public CompilerOptionsBuilder(CompilerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _baseDirectory = options.BaseDirectory;
            Mode = options.Mode;
            _extName = options.ExtName;
            _maxDegreeOfParallelism = options.MaxDegreeOfParallelism;
        }

        /// <summary>
        /// Gets or sets the base directory path used for resolving relative script files.
        /// </summary>
        public string Directory
        {
            get => _baseDirectory;
            set => _baseDirectory = Path.GetFullPath(value);
        }

        /// <summary>
        /// Gets or sets the compilation mode.
        /// </summary>
        public CompilationMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the script file extension.
        /// </summary>
        public string ExtName
        {
            get => _extName;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (!value.StartsWith('.')) value = "." + value;
                if (value.LastIndexOf('.') > 0)
                {
                    throw new ArgumentException("Extensions can only start with \".\" or provide a sense of an extension", nameof(value));
                }
                _extName = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of modules that may be parsed concurrently.
        /// </summary>
        public int MaxDegreeOfParallelism
        {
            get => _maxDegreeOfParallelism;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The maximum degree of parallelism cannot be negative.");
                }
                _maxDegreeOfParallelism = value;
            }
        }

        /// <summary>
        /// Sets the base directory path used for resolving relative script files.
        /// </summary>
        public CompilerOptionsBuilder WithDirectory(string value)
        {
            Directory = value;
            return this;
        }

        /// <summary>
        /// Sets the compilation mode.
        /// </summary>
        public CompilerOptionsBuilder WithMode(CompilationMode value)
        {
            Mode = value;
            return this;
        }

        /// <summary>
        /// Sets the script file extension.
        /// </summary>
        public CompilerOptionsBuilder WithExtName(string value)
        {
            ExtName = value;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of modules that may be parsed concurrently.
        /// </summary>
        public CompilerOptionsBuilder WithMaxDegreeOfParallelism(int value)
        {
            MaxDegreeOfParallelism = value;
            return this;
        }

        internal CompilerOptions ToOptions()
        {
            return new CompilerOptions
            {
                BaseDirectory = Directory,
                Mode = Mode,
                ExtName = ExtName,
                MaxDegreeOfParallelism = MaxDegreeOfParallelism
            };
        }
    }

    /// <summary>
    /// Compile-time optimization switches.
    /// </summary>
    public sealed record OptimizationOptions
    {
        /// <summary>
        /// Provides default compile-time optimization settings.
        /// </summary>
        public static readonly OptimizationOptions Default = new();

        /// <summary>
        /// Gets the optimization level used during code generation.
        /// </summary>
        public OptimizeOptions Level { get; init; } = OptimizeOptions.Release;

        /// <summary>
        /// Gets a value indicating whether the compiler may automatically infer direct calls
        /// for proven same-module internal functions without an explicit script annotation.
        /// Explicit @directCall annotations are not controlled by this option.
        /// </summary>
        public bool EnableAutoModuleDirectCall { get; init; }

        /// <summary>
        /// Gets a value indicating whether the compiler may inline proven module-level
        /// const values at same-module use sites. Only side-effect-free literal expressions are eligible.
        /// </summary>
        public bool EnableModuleConstInlining { get; init; }
    }

    /// <summary>
    /// Mutable builder used by <see cref="EngineOptions.WithOptimization"/>.
    /// </summary>
    public sealed class OptimizationOptionsBuilder
    {
        /// <summary>
        /// Creates a mutable optimization-options builder from an immutable options snapshot.
        /// </summary>
        public OptimizationOptionsBuilder(OptimizationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            Level = options.Level;
            AutoModuleDirectCall = options.EnableAutoModuleDirectCall;
            ModuleConstInlining = options.EnableModuleConstInlining;
        }

        /// <summary>
        /// Gets or sets the optimization level used during code generation.
        /// </summary>
        public OptimizeOptions Level { get; set; }

        /// <summary>
        /// Gets or sets whether same-module direct-call inference is enabled.
        /// </summary>
        public bool AutoModuleDirectCall { get; set; }

        /// <summary>
        /// Gets or sets whether eligible module-level const reads may be inlined.
        /// </summary>
        public bool ModuleConstInlining { get; set; }

        /// <summary>
        /// Sets the optimization level used during code generation.
        /// </summary>
        public OptimizationOptionsBuilder WithLevel(OptimizeOptions value)
        {
            Level = value;
            return this;
        }

        /// <summary>
        /// Sets whether same-module direct-call inference is enabled.
        /// </summary>
        public OptimizationOptionsBuilder WithAutoModuleDirectCall(bool value)
        {
            AutoModuleDirectCall = value;
            return this;
        }

        /// <summary>
        /// Sets whether eligible module-level const reads may be inlined.
        /// </summary>
        public OptimizationOptionsBuilder WithModuleConstInlining(bool value)
        {
            ModuleConstInlining = value;
            return this;
        }

        internal OptimizationOptions ToOptions()
        {
            return new OptimizationOptions
            {
                Level = Level,
                EnableAutoModuleDirectCall = AutoModuleDirectCall,
                EnableModuleConstInlining = ModuleConstInlining
            };
        }
    }

    /// <summary>
    /// Generated output and emission settings.
    /// </summary>
    public sealed record OutputOptions
    {
        /// <summary>
        /// Provides default output and emission settings.
        /// </summary>
        public static readonly OutputOptions Default = new();

        /// <summary>
        /// Gets the target path for the generated script assembly when using Persistence mode.
        /// </summary>
        public string AssemblyFile { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether obfuscation (confusion) is enabled.
        /// </summary>
        public bool EnableConfused { get; init; }
    }

    /// <summary>
    /// Mutable builder used by <see cref="EngineOptions.WithOutput"/>.
    /// </summary>
    public sealed class OutputOptionsBuilder
    {
        private string _assemblyOut;

        /// <summary>
        /// Creates a mutable output-options builder from an immutable options snapshot.
        /// </summary>
        public OutputOptionsBuilder(OutputOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _assemblyOut = options.AssemblyFile;
            Confused = options.EnableConfused;
        }

        /// <summary>
        /// Gets or sets the target path for the generated script assembly when using Persistence mode.
        /// </summary>
        public string AssemblyFile
        {
            get => _assemblyOut;
            set => _assemblyOut = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets whether obfuscation is enabled for generated output.
        /// </summary>
        public bool Confused { get; set; }

        /// <summary>
        /// Sets the target path for the generated script assembly when using Persistence mode.
        /// </summary>
        public OutputOptionsBuilder WithAssemblyFile(string value)
        {
            AssemblyFile = value;
            return this;
        }

        /// <summary>
        /// Sets whether obfuscation is enabled for generated output.
        /// </summary>
        public OutputOptionsBuilder WithConfused(bool value)
        {
            Confused = value;
            return this;
        }

        internal OutputOptions ToOptions()
        {
            return new OutputOptions
            {
                AssemblyFile = AssemblyFile,
                EnableConfused = Confused
            };
        }
    }
}
