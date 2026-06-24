using AuroraScript.Compiler;
using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Backend;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Emission;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Extensions;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using AuroraScript.Runtime.Types.TypeConstruct;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("AuroraScript.Generated")]
[assembly: InternalsVisibleTo("Benchmark")]
[assembly: InternalsVisibleTo("AuroraScript.Tests")]

namespace AuroraScript
{
    /// <summary>
    /// The main entry point for the AuroraScript engine.
    /// Responsible for initializing the compiler, runtime environment, and providing interfaces for script execution.
    /// </summary>
    public class AuroraEngine
    {
        /// <summary>
        /// The global object that stores global variables and functions accessible to the script.
        /// </summary>
        public readonly ScriptGlobal Global;

        /// <summary>
        /// Registry for CLR types, allowing the host to expose accessible aliases to the script environment.
        /// </summary>
        public readonly ClrTypeRegistry ClrRegistry = new();

        /// <summary>
        /// Configuration options for the engine. These cannot be changed after initialization.
        /// </summary>
        internal readonly EngineOptions Options;

        /// <summary>
        /// Initializes static members of the <see cref="AuroraEngine"/> class by preloading prototypes.
        /// </summary>
        static AuroraEngine()
        {
            Prototypes.Preload();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraEngine"/> class.
        /// </summary>
        /// <param name="options">The engine configuration options, including the base directory and other settings.</param>
        /// <exception cref="AuroraException">Thrown if the <paramref name="options"/> parameter is null.</exception>
        public AuroraEngine(EngineOptions options)
        {
            Options = options ?? throw new AuroraException("the parameter \"options\" cannot be empty");
            StringValue.ConfigurePooling(Options.StringPooling);
            Global = new ScriptGlobal(this);

            // register standard types
            Global.Define("Array", ArrayConstructor.INSTANCE, writeable: false, enumerable: false);
            Global.Define("String", StringConstructor.INSTANCE, writeable: false, enumerable: false);
            Global.Define("Boolean", BooleanConstructor.INSTANCE, writeable: false, enumerable: false);
            Global.Define("Object", ScriptObjectConstructor.INSTANCE, writeable: false, enumerable: false);
            Global.Define("Number", NumberConstructor.INSTANCE, writeable: false, enumerable: false);
            Global.Define("Date", ScriptDateConstructor.INSTANCE, writeable: false, enumerable: false);

            // register advanced type
            Global.Define("Error", ScriptErrorConstructor.INSTANCE, writeable: false, enumerable: false);
            Global.Define("HashMap", ScriptHashMapConstructor.INSTANCE, writeable: false, enumerable: false);
            Global.Define("Regex", ScriptRegexConstructor.INSTANCE, writeable: false, enumerable: false);
            Global.Define("Proxy", ScriptProxyConstructor.INSTANCE, writeable: false, enumerable: false);
            Global.Define("StringBuffer", StringBufferConstructor.INSTANCE, writeable: false, enumerable: false);

            // Optional standard libraries
            Global.Define("console", new ConsoleSupport(), writeable: false, enumerable: false);
            Global.Define("JSON", new JsonSupport(), writeable: false, enumerable: false);
            Global.Define("Math", new MathSupport(), writeable: false, enumerable: false);

            // Hot patch support
            Global.Define("HotPatch", HotPatchSupport.INSTANCE, writeable: false, enumerable: false);
        }

        /// <summary>
        /// Registers a CLR type into the script environment.
        /// </summary>
        /// <typeparam name="T">The type to register.</typeparam>
        /// <param name="alias">The alias for the type in the script. If null, the type name is used.</param>
        /// <param name="access">The access level for members of this type. Defaults to <see cref="TypeAccess.All"/>.</param>
        public void RegisterType<T>(string alias = null, TypeAccess access = TypeAccess.All)
        {
            ClrRegistry.RegisterType(typeof(T), alias, access);
        }

        /// <summary>
        /// Registers a CLR type into the script environment.
        /// </summary>
        /// <param name="type">The type to register.</param>
        /// <param name="alias">The alias for the type in the script. If null, the type name is used.</param>
        /// <param name="access">The access level for members of this type. Defaults to <see cref="TypeAccess.All"/>.</param>
        public void RegisterType(Type type, string alias = null, TypeAccess access = TypeAccess.All)
        {
            ClrRegistry.RegisterType(type, alias, access);
        }

        /// <summary>
        /// Creates a script source from a string in memory.
        /// </summary>
        /// <param name="file">The filename or virtual path for the source.</param>
        /// <param name="code">The script code string.</param>
        /// <returns>A <see cref="ScriptSource"/> representing the memory-based content.</returns>
        public ScriptSource MemorySource(string file, string code)
        {
            if (!Path.IsPathRooted(file))
            {
                file = Path.Join(Options.BaseDirectory, file);
            }
            return new TextSource(Options.BaseDirectory, file, code);
        }

        /// <summary>
        /// Creates a script source from a file on disk.
        /// </summary>
        /// <param name="file">The path to the script file.</param>
        /// <param name="encoding">The file encoding.</param>
        /// <returns>A <see cref="ScriptSource"/> representing the file-based content.</returns>
        public ScriptSource FileSource(string file, Encoding encoding)
        {
            if (!Path.IsPathRooted(file))
            {
                file = Path.Join(Options.BaseDirectory, file);
            }
            return new FileSource(Options.BaseDirectory, file, encoding);
        }

        /// <summary>
        /// Searches all script files in the base directory and returns them as an array of script sources.
        /// </summary>
        /// <param name="encoding">The file encoding to use.</param>
        /// <returns>An array of <see cref="ScriptSource"/> objects.</returns>
        /// <exception cref="AuroraException">Thrown if the base directory is invalid.</exception>
        public ScriptSource[] SearchAllFileSource(Encoding encoding)
        {
            if (string.IsNullOrEmpty(Options.BaseDirectory) || !Directory.Exists(Options.BaseDirectory))
            {
                throw new AuroraException($"The BaseDirectory “{Options.BaseDirectory}” field of the parameter options is not a valid directory");
            }
            var files = Directory.GetFiles(Options.BaseDirectory, "*" + Options.ExtName, SearchOption.AllDirectories);
            return files
                .Select(file => new FileSource(Options.BaseDirectory, file, encoding))
                .OfType<ScriptSource>().ToArray();
        }

        /// <summary>
        /// Compiles and builds the provided script sources into an executable assembly.
        /// </summary>
        /// <param name="sources">An array of script sources to compile.</param>
        /// <returns>A task representing the asynchronous build operation.</returns>
        /// <exception cref="AuroraException">Thrown if the base directory is invalid.</exception>
        /// <exception cref="NotImplementedException">Thrown if the compilation mode is not supported.</exception>
        public Task BuildAsync(params ScriptSource[] sources)
        {
            return BuildAsync(CancellationToken.None, sources);
        }

        /// <summary>
        /// Compiles and builds the provided script sources into an executable assembly.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel source compilation or output persistence.</param>
        /// <param name="sources">The script sources to compile.</param>
        /// <returns>A task representing the asynchronous build operation.</returns>
        public async Task BuildAsync(CancellationToken cancellationToken, params ScriptSource[] sources)
        {
            await _buildLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var baseDirectory = Path.GetFullPath(Options.BaseDirectory);
                if (string.IsNullOrEmpty(baseDirectory) || !Directory.Exists(baseDirectory))
                {
                    throw new AuroraException($"The BaseDirectory “{baseDirectory}” field of the parameter options is not a valid directory");
                }
                AbstractCILBuilder builder = Options.CompilationMode switch
                {
                    CompilationMode.Persistence => new PersistedBuilder(Options),
                    CompilationMode.OnlyRun => new OnlyRunBuilder(Options),
                    CompilationMode.Dynamic => new DynamicBuilder(Options),
                    _ => throw new NotImplementedException()
                };
                var compiler = new ScriptCompiler(Options);
                var modules = await compiler.BuildModuleGraphAsync(sources, cancellationToken).ConfigureAwait(false);

                var backend = new BackendCompiler(builder, Options);
                var compileSession = backend.CreateModulePlans(modules, cancellationToken);
                new BackendBuildEmitter(new EmissionSession(compileSession, builder, emitExecutableSkeletons: true)).Emit();

                Assembly scriptAssembly = null;
                MethodInfo entryPoint;
                if (builder is PersistedBuilder persisted)
                {
                    var peImage = persisted.Serialize();
                    if (!string.IsNullOrEmpty(Options.AssemblyOut))
                    {
                        await File.WriteAllBytesAsync(Options.AssemblyOut, peImage, cancellationToken).ConfigureAwait(false);
                    }
                    scriptAssembly = Assembly.Load(peImage);
                    var type = scriptAssembly.GetType(AbstractCILBuilder.EntryPointTypeName);
                    entryPoint = type?.GetMethod(AbstractCILBuilder.EntryPointMethodName);
                }
                else
                {
                    entryPoint = builder.GetRuntimeEntryPoint();
                }

                if (entryPoint == null)
                {
                    throw new AuroraException("The compiler did not produce a runtime entry point.");
                }

                var entryPointDelegate = entryPoint.CreateDelegate<ScriptFunctionDelegate>();
                ScriptAssembly = scriptAssembly;
                _entryPointDelegate = entryPointDelegate;
            }
            finally
            {
                _buildLock.Release();
            }
        }

        /// <summary>
        /// Compiles a lightweight script block as an anonymous function body.
        /// </summary>
        /// <param name="source">The script block source.</param>
        /// <param name="options">The block compilation options.</param>
        /// <returns>A compiled block that can be invoked directly.</returns>
        public CompiledBlock CompileBlock(string source, CompileBlockOptions options = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            options ??= new CompileBlockOptions();
            ValidateCompileBlockParameters(options.Parameters);
            var sourceName = string.IsNullOrWhiteSpace(options.SourceName) ? "__compile_block__.as" : options.SourceName;
            var scriptSource = MemorySource(sourceName, source);
            var lexer = new AuroraLexer(Options.BaseDirectory, scriptSource);
            var parser = new AuroraParser(lexer, Options);
            var block = parser.ParseBlockBody();

            var builderOptions = Options with
            {
                CompilationMode = CompilationMode.Dynamic,
                EnableHotReload = false,
                OptimizeOption = OptimizeOptions.Release
            };
            var builder = new DynamicBuilder(builderOptions);
            var backend = new BackendCompiler(builder, builderOptions);
            var blockPlan = backend.CreateCompileBlockPlan(block, options.Parameters, sourceName);
            var emissionSession = new EmissionSession(blockPlan.Session, builder, emitExecutableSkeletons: true);
            var method = new CompileBlockEmitter(
                emissionSession,
                blockPlan).Emit();
            if (method == null)
            {
                throw new AuroraException("The compiler did not produce a compiled block entry point.");
            }
            var target = method.CreateDelegate<ScriptFunctionDelegate>();
            return new CompiledBlock(this, target, emissionSession.RegisteredDynamicDelegateIds);
        }

        private static void ValidateCompileBlockParameters(IReadOnlyList<string> parameters)
        {
            if (parameters == null)
            {
                return;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < parameters.Count; i++)
            {
                var name = parameters[i];
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("CompileBlock parameter names cannot be empty.", nameof(parameters));
                }

                if (!IsValidCompileBlockParameterName(name))
                {
                    throw new ArgumentException($"Invalid CompileBlock parameter name '{name}'.", nameof(parameters));
                }

                if (!names.Add(name))
                {
                    throw new ArgumentException($"Duplicate CompileBlock parameter name '{name}'.", nameof(parameters));
                }
            }
        }

        private static bool IsValidCompileBlockParameterName(string name)
        {
            if (name == "global" || name == "$args" || name == "$state")
            {
                return false;
            }

            if (name.Length == 0)
            {
                return false;
            }

            if (!IsIdentifierStart(name[0]))
            {
                return false;
            }

            for (var i = 1; i < name.Length; i++)
            {
                if (!IsIdentifierPart(name[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsIdentifierStart(char c)
        {
            return c == '_' || c == '$' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= 0x4e00 && c <= 0x9fbb);
        }

        private static bool IsIdentifierPart(char c)
        {
            return IsIdentifierStart(c) || (c >= '0' && c <= '9');
        }

        /// <summary>
        /// The entry point method of the compiled script assembly.
        /// </summary>
        private readonly SemaphoreSlim _buildLock = new(1, 1);

        private ScriptFunctionDelegate _entryPointDelegate;

        /// <summary>
        /// The compiled script assembly. Only populated in Persistence mode.
        /// </summary>
        private Assembly ScriptAssembly = null;

        /// <summary>
        /// Creates a new global environment based on the current engine's global object.
        /// </summary>
        /// <returns>A new <see cref="ScriptGlobal"/> instance.</returns>
        public ScriptGlobal NewEnvironment()
        {
            var environment = ScriptGlobal.With(this, Global);
            return environment;
        }

        /// <summary>
        /// Creates an empty script domain with optional custom global configuration and user state.
        /// </summary>
        /// <param name="globalConfiguration">A callback to configure the global environment for the domain.</param>
        /// <param name="userState">Optional script object state to pass to the domain context.</param>
        /// <returns>A new <see cref="ScriptDomain"/> instance.</returns>
        public ScriptDomain CreateEmptyDomain(Action<ScriptGlobal> globalConfiguration, ScriptObject userState = null)
        {
            var domainGlobal = ScriptGlobal.With(this, Global);
            globalConfiguration?.Invoke(domainGlobal);
            var stateObject = userState ?? ScriptObject.Null;
            return new ScriptDomain(this, domainGlobal, stateObject);
        }

        /// <summary>
        /// Creates a script domain and initializes it by executing the script entry point.
        /// </summary>
        /// <param name="globalConfiguration">A callback to configure the global environment for the domain.</param>
        /// <param name="userState">Optional script object state to pass to the domain context.</param>
        /// <returns>A new <see cref="ScriptDomain"/> instance.</returns>
        /// <exception cref="Exception">Thrown if assembly initialization fails.</exception>
        public ScriptDomain CreateDomain(Action<ScriptGlobal> globalConfiguration, ScriptObject userState = null)
        {
            var domainGlobal = ScriptGlobal.With(this, Global);
            globalConfiguration?.Invoke(domainGlobal);
            return CreateDomain(domainGlobal, userState);
        }

        /// <summary>
        /// Creates a script domain and initializes it by executing the script entry point.
        /// Each script domain has its own global object but shares prototypes.
        /// </summary>
        /// <param name="domainGlobal">The global environment to use for the domain. If null, a new one is created inheriting from the engine's global.</param>
        /// <param name="userState">Optional script object state to pass to the domain context.</param>
        /// <returns>A new <see cref="ScriptDomain"/> instance after initialization.</returns>
        /// <exception cref="Exception">Thrown if assembly initialization fails.</exception>
        public ScriptDomain CreateDomain(ScriptGlobal domainGlobal = null, ScriptObject userState = null)
        {
            domainGlobal ??= ScriptGlobal.With(this, Global);
            ScriptObject stateObject = userState ?? ScriptObject.Null;
            var domain = new ScriptDomain(this, domainGlobal, stateObject);
            var ctx = new ScriptContext(domain);
            var entryPoint = _entryPointDelegate ?? throw new AuroraException("The engine has not been built.");
            entryPoint(ctx, Span<ScriptDatum>.Empty);
            return domain;
        }
    }
}
