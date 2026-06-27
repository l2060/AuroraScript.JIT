using AuroraScript.Compiler;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Core;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Pool;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Represents a delegate for script methods that can be invoked from the host or other script components.
    /// </summary>
    /// <param name="userState">Optional user-defined state to pass to the method.</param>
    /// <param name="arguments">An array of arguments to pass to the method.</param>
    /// <returns>A <see cref="ScriptDatum"/> representing the result of the method execution.</returns>
    public delegate ScriptDatum ScriptMethodDelegate(ScriptObject userState = null, params ScriptDatum[] arguments);

    /// <summary>
    /// Represents an isolated script execution domain.
    /// Each domain has its own global object and module registry but shares the base prototype chain.
    /// </summary>
    public sealed class ScriptDomain : IDisposable
    {
        internal readonly ScriptContextPool ContextPool = new();
        /// <summary>
        /// The global environment for this script domain.
        /// </summary>
        public readonly ScriptGlobal Global;

        /// <summary>
        /// The engine instance associated with this domain.
        /// </summary>
        public readonly AuroraEngine Engine;

        /// <summary>
        /// The default user state object associated with this domain.
        /// </summary>
        public readonly ScriptObject UserState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptDomain"/> class.
        /// </summary>
        /// <param name="engine">The engine instance.</param>
        /// <param name="domainGlobal">The global object for this domain.</param>
        /// <param name="userState">Initial user state for the domain.</param>
        internal ScriptDomain(AuroraEngine engine, ScriptGlobal domainGlobal, ScriptObject userState)
        {
            UserState = userState;
            Global = domainGlobal;
            Engine = engine;
        }

        /// <summary>
        /// Disposes of the script domain, clearing all registered modules and properties.
        /// </summary>
        public void Dispose()
        {
            Global.Modules.ClearProperties();
            Global.ClearProperties();
        }

        /// <summary>
        /// Executes a specified method in a module using the default user state and no arguments.
        /// </summary>
        /// <param name="moduleName">The name of the script module.</param>
        /// <param name="methodName">The name of the method to execute.</param>
        /// <returns>The result of the execution as a <see cref="ScriptDatum"/>.</returns>
        /// <exception cref="AuroraException">Thrown if the module or method is not found.</exception>
        public ScriptDatum Execute(string moduleName, string methodName)
        {
            return Execute(moduleName, methodName, UserState, Array.Empty<ScriptDatum>());
        }

        /// <summary>
        /// Executes a specified method in a module with arguments and default user state.
        /// </summary>
        /// <param name="moduleName">The name of the script module.</param>
        /// <param name="methodName">The name of the method to execute.</param>
        /// <param name="arguments">The arguments to pass to the method.</param>
        /// <returns>The result of the execution as a <see cref="ScriptDatum"/>.</returns>
        /// <exception cref="AuroraException">Thrown if the module or method is not found.</exception>
        public ScriptDatum Execute(string moduleName, string methodName, params ScriptObject[] arguments)
        {
            return Execute(moduleName, methodName, UserState, ClrMarshaller.ToDatums(arguments));
        }

        /// <summary>
        /// Executes a specified method in a module with raw <see cref="ScriptDatum"/> arguments and default user state.
        /// </summary>
        /// <param name="moduleName">The name of the script module.</param>
        /// <param name="methodName">The name of the method to execute.</param>
        /// <param name="arguments">The raw datum arguments.</param>
        /// <returns>The result of the execution as a <see cref="ScriptDatum"/>.</returns>
        /// <exception cref="AuroraException">Thrown if the module or method is not found.</exception>
        public ScriptDatum Execute(string moduleName, string methodName, params ScriptDatum[] arguments)
        {
            return Execute(moduleName, methodName, UserState, arguments);
        }

        /// <summary>
        /// Executes a specified method in a module with custom user state and script object arguments.
        /// </summary>
        /// <param name="moduleName">The name of the script module.</param>
        /// <param name="methodName">The name of the method to execute.</param>
        /// <param name="userState">The custom user state for this execution context.</param>
        /// <param name="arguments">The script object arguments.</param>
        /// <returns>The result of the execution as a <see cref="ScriptDatum"/>.</returns>
        /// <exception cref="AuroraException">Thrown if the module or method is not found.</exception>
        public ScriptDatum Execute(string moduleName, string methodName, ScriptObject userState, params ScriptObject[] arguments)
        {
            return Execute(moduleName, methodName, userState, ClrMarshaller.ToDatums(arguments));
        }

        /// <summary>
        /// The primary internal method for executing script methods. 
        /// Looks up the specified module and method, creates a context, and invokes the closure.
        /// </summary>
        /// <param name="moduleName">The name of the script module. Modules are internally prefixed with '@'.</param>
        /// <param name="methodName">The name of the method to invoke within the module.</param>
        /// <param name="userState">The user state passed to the script execution context.</param>
        /// <param name="arguments">The raw datum arguments passed to the script method.</param>
        /// <returns>The result of the execution as a <see cref="ScriptDatum"/>.</returns>
        /// <exception cref="AuroraException">Thrown if the module/method is missing or if the target is not a valid closure.</exception>
        public ScriptDatum Execute(string moduleName, string methodName, ScriptObject userState, params ScriptDatum[] arguments)
        {
            if (!Global.TryGetModule(moduleName, out var module))
            {
                throw new AuroraException($"The module named {moduleName} was not found");
            }

            var method = module.GetPropertyValue(methodName);
            if (method == ScriptObject.Null)
            {
                throw new AuroraException($"The method {methodName} of the module {moduleName} does not exist");
            }

            if (method is not ClosureFunction closure)
            {
                throw new AuroraException($"{methodName} is not a valid script method");
            }

            var ctx = ContextPool.Rent(this, userState, module, null);
            try
            {
                return closure.InvokeClr(ctx, arguments);
            }
            finally
            {
                ctx.Release();
            }
        }

        /// <summary>
        /// Retrieves a script closure by its module and method name.
        /// </summary>
        /// <param name="moduleName">The name of the module.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <returns>The <see cref="ClosureFunction"/> if found and valid; otherwise, null.</returns>
        public ClosureFunction GetMethod(string moduleName, string methodName)
        {
            if (!Global.TryGetModule(moduleName, out var module))
            {
                return null;
            }
            var method = module.GetPropertyValue(methodName);
            if (method == ScriptObject.Null || method is not ClosureFunction closure)
            {
                return null;
            }
            return closure;
        }

        /// <summary>
        /// Dynamically applies a hot patch to the script domain in memory.
        /// </summary>
        /// <param name="source">The script source containing the patch code.</param>
        /// <param name="patchType">The type of hot patch to apply (e.g., Replace, Append).</param>
        public void DynamicPatch(ScriptSource source, HotPatchType patchType)
        {
            EngineOptions ExeOptions = Engine.Options;
            if (!ExeOptions.Runtime.EnableHotReload)
            {
                throw new AuroraException("Dynamic patching is disabled by EngineOptions.Runtime.HotReload.");
            }
            DynamicBuilder builder = new DynamicBuilder(ExeOptions);
            var compiler = new IncrementalCompiler(this, ExeOptions, builder);
            DynamicCallMethod invoker;
            try
            {
                invoker = compiler.BuildPatch(source, patchType);
            }
            catch (Exception ex) when (AuroraEngine.IsCompilationPipelineException(ex))
            {
                throw AuroraEngine.CreateCompilationException(ex, AuroraCompilationStage.Emission);
            }
            var ctx = new ScriptContext(this);
            _ = invoker(ctx, []);
        }

        /// <summary>
        /// Retrieves a module object by its name.
        /// </summary>
        /// <param name="moduleName">The name of the module to retrieve.</param>
        /// <returns>The module represented as a <see cref="ScriptObject"/>, or <see cref="ScriptObject.Null"/> if not found.</returns>
        public ScriptObject GetModule(string moduleName)
        {
            if (Global.TryGetModule(moduleName, out var module))
            {
                return module;
            }
            return ScriptObject.Null;
        }
    }
}
