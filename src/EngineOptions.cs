using AuroraScript.Runtime.Serialization;
using AuroraScript.Source;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.IO;
using AuroraScript.Core;

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
        private RuntimeOptions _runtime = RuntimeOptions.Default;
        private CompilerOptions _compiler = CompilerOptions.Default;
        private OptimizationOptions _optimization = OptimizationOptions.Default;
        private OutputOptions _output = OutputOptions.Default;
        private IReadOnlyList<BuiltInModuleDefinition> _builtIns = Array.Empty<BuiltInModuleDefinition>();

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
        /// Gets the native modules explicitly enabled for this engine.
        /// The default collection is empty.
        /// </summary>
        public IReadOnlyList<BuiltInModuleDefinition> BuiltIns
        {
            get => _builtIns;
            init => _builtIns = BuiltInModulesBuilder.CreateSnapshot(value);
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
        /// Configures the native modules available to the engine and returns a new
        /// immutable options instance.
        /// </summary>
        public EngineOptions WithBuiltIns(Action<BuiltInModulesBuilder> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var builder = new BuiltInModulesBuilder(BuiltIns);
            configure(builder);
            return this with { BuiltIns = builder.ToDefinitions() };
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

        /// <summary>
        /// Gets the resolver used to locate and open imported script sources.
        /// The default resolver reads from the file system.
        /// </summary>
        public IScriptSourceResolver SourceResolver { get; init; } = FileScriptSourceResolver.Instance;

        /// <summary>
        /// Gets additional assemblies whose source-generated host exports may be bound
        /// directly by compiled scripts. The engine assembly is always scanned, and
        /// runtime globals must still be registered separately.
        /// </summary>
        public IReadOnlyList<Assembly> HostExportAssemblies
        {
            get => _hostExportAssemblies;
            init => _hostExportAssemblies = CreateSnapshot(value);
        }

        private readonly IReadOnlyList<Assembly> _hostExportAssemblies = Array.Empty<Assembly>();

        internal static IReadOnlyList<Assembly> CreateSnapshot(IReadOnlyList<Assembly> value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Count == 0)
            {
                return Array.Empty<Assembly>();
            }

            var copy = new Assembly[value.Count];
            for (var i = 0; i < copy.Length; i++)
            {
                copy[i] = value[i] ??
                    throw new ArgumentException(
                        "Host export assemblies cannot contain null.",
                        nameof(value));
            }
            return new ReadOnlyCollection<Assembly>(copy);
        }
    }

    /// <summary>
    /// Mutable builder used by <see cref="EngineOptions.WithCompiler"/>.
    /// </summary>
    public sealed class CompilerOptionsBuilder
    {
        private string _extName;
        private int _maxDegreeOfParallelism;
        private IScriptSourceResolver _sourceResolver;
        private IReadOnlyList<Assembly> _hostExportAssemblies;

        /// <summary>
        /// Creates a mutable compiler-options builder from an immutable options snapshot.
        /// </summary>
        public CompilerOptionsBuilder(CompilerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            Mode = options.Mode;
            _extName = options.ExtName;
            _maxDegreeOfParallelism = options.MaxDegreeOfParallelism;
            _sourceResolver = options.SourceResolver ?? FileScriptSourceResolver.Instance;
            _hostExportAssemblies = options.HostExportAssemblies;
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
        /// Gets or sets the resolver used to locate and open imported script sources.
        /// </summary>
        public IScriptSourceResolver SourceResolver
        {
            get => _sourceResolver;
            set => _sourceResolver = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets additional assemblies whose source-generated host exports may be
        /// bound directly by compiled scripts.
        /// </summary>
        public IReadOnlyList<Assembly> HostExportAssemblies
        {
            get => _hostExportAssemblies;
            set => _hostExportAssemblies = CompilerOptions.CreateSnapshot(value);
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

        /// <summary>
        /// Sets the resolver used to locate and open imported script sources.
        /// </summary>
        public CompilerOptionsBuilder WithSourceResolver(IScriptSourceResolver value)
        {
            SourceResolver = value;
            return this;
        }

        /// <summary>
        /// Sets additional assemblies whose source-generated host exports may be bound
        /// directly by compiled scripts.
        /// </summary>
        public CompilerOptionsBuilder WithHostExportAssemblies(params Assembly[] value)
        {
            HostExportAssemblies = value;
            return this;
        }

        internal CompilerOptions ToOptions()
        {
            return new CompilerOptions
            {
                Mode = Mode,
                ExtName = ExtName,
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                SourceResolver = SourceResolver,
                HostExportAssemblies = HostExportAssemblies
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
        /// Gets a value indicating whether the compiler may inline proven module-level
        /// const values at same-module use sites. Only side-effect-free literal expressions are eligible.
        /// </summary>
        public bool EnableModuleConstInlining { get; init; }

        /// <summary>
        /// Gets a value indicating whether generated code records source line locations
        /// used to build script stack traces. Disabling this in release builds removes
        /// those runtime location writes. Debug builds always keep stack trace locations.
        /// </summary>
        public bool StackTrace { get; init; } = true;
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
            ModuleConstInlining = options.EnableModuleConstInlining;
            StackTrace = options.StackTrace;
        }

        /// <summary>
        /// Gets or sets the optimization level used during code generation.
        /// </summary>
        public OptimizeOptions Level { get; set; }

        /// <summary>
        /// Gets or sets whether eligible module-level const reads may be inlined.
        /// </summary>
        public bool ModuleConstInlining { get; set; }

        /// <summary>
        /// Gets or sets whether generated code records source line locations used to build script stack traces.
        /// Debug builds always keep stack trace locations even when this value is false.
        /// </summary>
        public bool StackTrace { get; set; } = true;

        /// <summary>
        /// Sets the optimization level used during code generation.
        /// </summary>
        public OptimizationOptionsBuilder WithLevel(OptimizeOptions value)
        {
            Level = value;
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

        /// <summary>
        /// Sets whether generated code records source line locations used to build script stack traces.
        /// </summary>
        public OptimizationOptionsBuilder WithStackTrace(bool value)
        {
            StackTrace = value;
            return this;
        }

        internal OptimizationOptions ToOptions()
        {
            return new OptimizationOptions
            {
                Level = Level,
                EnableModuleConstInlining = ModuleConstInlining,
                StackTrace = StackTrace
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
