using AuroraScript.Compiler;
using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Emission;
using AuroraScript.Compiler.GlobalDeclarations;
using AuroraScript.Core;
using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Builtin;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using AuroraScript.Runtime.Types.TypeConstruct;
using AuroraScript.Source;
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
[assembly: InternalsVisibleTo("AuroraScript.VisualStudio")]
[assembly: InternalsVisibleTo("AuroraScript.LanguageServer")]
[assembly: InternalsVisibleTo("AuroraScript.LanguageServices")]

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
        /// Native types that opted into TDoc through <see cref="INativeTypedDocument"/>.
        /// </summary>
        internal readonly TypedDocumentNativeCatalog TypedDocuments;

        /// <summary>
        /// Engine-scoped index of the native modules selected through <see cref="EngineOptions.BuiltIns"/>.
        /// </summary>
        internal readonly BuiltinModuleRegistry BuiltInRegistry;

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
            if (options == null)
            {
                throw new AuroraException("the parameter \"options\" cannot be empty");
            }

            BuiltInRegistry = new BuiltinModuleRegistry(options.BuiltIns);
            var sourceResolver = options.Compiler.SourceResolver ?? FileScriptSourceResolver.Instance;
            Options = BuiltInRegistry.Count == 0
                ? options
                : options.WithCompiler(compiler => compiler.SourceResolver =
                    new BuiltinScriptSourceResolver(sourceResolver, BuiltInRegistry));
            TypedDocuments = new TypedDocumentNativeCatalog(Options.Compiler.NativeTypes);
            Global = new ScriptGlobal(this);

            // register standard types
            Global.Define("Array", ArrayConstructor.INSTANCE, writeable: false, enumerable: false);
            Global.Define("Int32Array", PackedArrayConstructor.Int32, writeable: false, enumerable: false);
            Global.Define("Int8Array", PackedArrayConstructor.Int8, writeable: false, enumerable: false);
            Global.Define("Float32Array", PackedArrayConstructor.Float32, writeable: false, enumerable: false);
            Global.Define("Float64Array", PackedArrayConstructor.Float64, writeable: false, enumerable: false);
            Global.Define("BooleanArray", PackedArrayConstructor.Boolean, writeable: false, enumerable: false);
            Global.Define("UInt8Array", PackedArrayConstructor.UInt8, writeable: false, enumerable: false);
            Global.Define("Int16Array", PackedArrayConstructor.Int16, writeable: false, enumerable: false);
            Global.Define("UInt16Array", PackedArrayConstructor.UInt16, writeable: false, enumerable: false);
            Global.Define("UInt32Array", PackedArrayConstructor.UInt32, writeable: false, enumerable: false);
            Global.Define("Int64Array", PackedArrayConstructor.Int64, writeable: false, enumerable: false);
            Global.Define("UInt64Array", PackedArrayConstructor.UInt64, writeable: false, enumerable: false);
            StringValue.Register(Global);
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
            Global.Define("Path", PathConstructor.INSTANCE, writeable: false, enumerable: false);

            // Built-in infrastructure
            ConsoleSupport.Register(Global);
            JsonSupport.Register(Global);
            TDocSupport.Register(Global);
            MathSupport.Register(Global);
            Conv8Support.Register(Global);
            EnvSupport.Register(Global);
            HotPatchSupport.Register(Global);
            RegisterNativeTypes(Options.Compiler.NativeTypes);
        }

        private void RegisterNativeTypes(IReadOnlyList<Type> nativeTypes)
        {
            for (var i = 0; i < nativeTypes.Count; i++)
            {
                var nativeType = nativeTypes[i];
                if (nativeType.Assembly == typeof(AuroraEngine).Assembly)
                {
                    continue;
                }

                var attribute = nativeType.GetCustomAttribute<AuroraNativeTypeAttribute>();
                if (attribute == null)
                {
                    throw new ArgumentException(
                        $"Type '{nativeType.FullName}' is not marked with AuroraNativeTypeAttribute.",
                        nameof(nativeTypes));
                }

                if (attribute.NativeReceiverType != null)
                {
                    throw new ArgumentException(
                        $"Native value receiver '{nativeType.FullName}' cannot replace an engine-owned immutable prototype.",
                        nameof(nativeTypes));
                }

                var register = nativeType.GetMethod(
                    "Register",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof(ScriptObject), typeof(bool), typeof(bool) },
                    modifiers: null);
                if (register == null)
                {
                    throw new InvalidOperationException(
                        $"Native type '{nativeType.FullName}' does not expose its generated Register method.");
                }

                register.Invoke(null, new object[] { Global, false, false });
            }
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
        /// Enumerates all script sources from the configured source resolver.
        /// </summary>
        private async Task<ScriptSource[]> SearchAllSourceAsync(Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            var query = new ScriptSourceQuery(Options.Compiler.ExtName, encoding ?? Encoding.UTF8);
            var sources = new List<ScriptSource>();
            await foreach (var source in Options.Compiler.SourceResolver.GetAllSourcesAsync(query, cancellationToken).ConfigureAwait(false))
            {
                if (!IsProjectSource(source))
                {
                    continue;
                }

                sources.Add(source);
            }
            return sources.ToArray();
        }

        /// <summary>
        /// Compiles and builds the provided script sources into an executable assembly.
        /// </summary>
        /// <param name="sources">An array of script sources to compile.</param>
        /// <returns>A task representing the asynchronous build operation.</returns>
        /// <exception cref="AuroraException">Thrown if the base directory is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the compilation mode is not supported.</exception>
        public Task BuildAsync(params ScriptSource[] sources)
        {
            return BuildAsync(CancellationToken.None, sources);
        }

        /// <summary>
        /// Compiles all sources exposed by the configured source resolver.
        /// </summary>
        public async Task BuildAsync(CancellationToken cancellationToken = default)
        {
            var sources = await SearchAllSourceAsync(Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            await BuildAsync(cancellationToken, sources).ConfigureAwait(false);
        }

        /// <summary>
        /// Compiles one entry source and its imports/includes through the configured source resolver.
        /// </summary>
        public Task BuildAsync(string entryPath, CancellationToken cancellationToken = default)
        {
            return BuildAsync([entryPath], cancellationToken);
        }

        /// <summary>
        /// Compiles entry sources and their imports/includes through the configured source resolver.
        /// </summary>
        public async Task BuildAsync(IEnumerable<string> entryPaths, CancellationToken cancellationToken = default)
        {
            if (entryPaths == null)
            {
                throw new ArgumentNullException(nameof(entryPaths));
            }

            var context = new ScriptResolveContext(Options.Compiler.ExtName, Encoding.UTF8);
            var sources = new List<ScriptSource>();
            foreach (var entryPath in entryPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(entryPath))
                {
                    throw new ArgumentException("Entry source paths cannot be empty.", nameof(entryPaths));
                }

                var reference = await Options.Compiler.SourceResolver
                    .ResolveAsync(null, entryPath, context, cancellationToken)
                    .ConfigureAwait(false);
                if (reference == null)
                {
                    throw new FileNotFoundException("Entry script source not found.", entryPath);
                }

                sources.Add(await Options.Compiler.SourceResolver
                    .GetSourceAsync(reference.Value, cancellationToken)
                    .ConfigureAwait(false));
            }

            await BuildAsync(cancellationToken, sources.ToArray()).ConfigureAwait(false);
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
                AbstractCILBuilder builder = Options.Compiler.Mode switch
                {
                    CompilationMode.Persistence => new PersistedBuilder(Options),
                    CompilationMode.OnlyRun => new OnlyRunBuilder(Options),
                    CompilationMode.Dynamic => new DynamicBuilder(Options),
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(Options.Compiler.Mode),
                        Options.Compiler.Mode,
                        "Unsupported compilation mode.")
                };
                var compiler = new ScriptCompiler(Options);
                var modules = await compiler.BuildModuleGraphAsync(sources, cancellationToken).ConfigureAwait(false);
                ValidateBuiltInModuleConflicts(modules);

                EmitProgram(builder, modules, compiler.GlobalDeclarations, cancellationToken);

                Assembly scriptAssembly = null;
                MethodInfo entryPoint;
                if (builder is PersistedBuilder persisted)
                {
                    var peImage = persisted.Serialize();
                    if (!string.IsNullOrEmpty(Options.Output.AssemblyFile))
                    {
                        await File.WriteAllBytesAsync(Options.Output.AssemblyFile, peImage, cancellationToken).ConfigureAwait(false);
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
        /// <param name="parameters">Names of positional arguments exposed as local variables in the compiled block.</param>
        /// <returns>A compiled block that can be invoked directly.</returns>
        public CompiledBlock CompileBlock(string source, string[] parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            return CompileBlock(source, new CompileBlockOptions { Parameters = parameters });
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
            var scriptSource = new MemorySource("mem://compile-block/", sourceName, source);
            try
            {
                var lexer = new AuroraLexer(scriptSource.BaseDirectory, scriptSource);
                var parser = new AuroraParser(lexer, Options);
                var block = parser.ParseBlockBody();

                var builderOptions = Options
                    .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
                    .WithRuntime(runtime => runtime.HotReload = false)
                    .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release);
                var builder = new DynamicBuilder(builderOptions);
                var backend = new BackendCompiler(builder, builderOptions);
                var blockPlan = backend.CreateCompileBlockPlan(block, options.Parameters, sourceName);
                var emissionSession = new EmissionSession(blockPlan.Session, builder, emitExecutableCode: true);
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
            catch (Exception ex) when (IsCompilationPipelineException(ex))
            {
                throw CreateCompilationException(ex, AuroraCompilationStage.Parsing);
            }
        }

        private void ValidateBuiltInModuleConflicts(IReadOnlyList<ModuleDeclaration> modules)
        {
            for (var i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                if (string.IsNullOrEmpty(module.ModuleName) ||
                    !BuiltInRegistry.TryGetByName(module.ModuleName, out var builtIn) ||
                    ScriptPath.PathTextEqualsNormalized(module.Source.FullPath, builtIn.Reference.FullPath))
                {
                    continue;
                }

                throw new AuroraCompilationException(
                    AuroraCompilationStage.Linking,
                    module.Source.FullPath,
                    1,
                    1,
                    $"Module '{module.ModuleName}' conflicts with the enabled built-in module '{builtIn.ModulePath}'.");
            }
        }

        private void EmitProgram(
            AbstractCILBuilder builder,
            Compiler.Ast.ModuleDeclaration[] modules,
            GlobalDeclarationIndex globalDeclarations,
            CancellationToken cancellationToken)
        {
            try
            {
                var backend = new BackendCompiler(builder, Options, globalDeclarations);
                var compileSession = backend.CreateModulePlans(modules, cancellationToken);
                new BackendBuildEmitter(new EmissionSession(compileSession, builder, emitExecutableCode: true)).Emit();
            }
            catch (Exception ex) when (IsCompilationPipelineException(ex))
            {
                throw CreateCompilationException(ex, AuroraCompilationStage.Emission);
            }
        }

        private static bool IsProjectSource(ScriptSource source)
        {
            if (source == null)
            {
                return false;
            }

            return GlobalDeclarationScanner.IsProjectSource(
                ScriptPath.NormalizeBaseDirectory(source.BaseDirectory),
                source.FullPath);
        }

        internal static bool IsCompilationPipelineException(Exception exception)
        {
            if (exception is AuroraCompilationException or NotSupportedException)
            {
                return true;
            }

            if (exception is AggregateException aggregate)
            {
                var errors = aggregate.Flatten().InnerExceptions;
                return errors.Count > 0 && errors.All(IsCompilationPipelineException);
            }

            return false;
        }

        internal static AuroraCompilationException CreateCompilationException(
            Exception exception,
            AuroraCompilationStage fallbackStage)
        {
            return exception is AuroraCompilationException compilation
                ? compilation
                : AuroraCompilationException.FromException(exception, fallbackStage);
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
            if (name == "global")
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
