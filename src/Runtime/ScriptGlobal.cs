using AuroraScript.Runtime.Interop;
using AuroraScript.Core;
using AuroraScript.Runtime.Types;
using System.Collections.Generic;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Identifies the source associated with emitted location metadata.
    /// </summary>
    /// <param name="Source">The source that identifies the module.</param>
    internal record ModuleMeta(ScriptSourceReference Source);

    /// <summary>
    /// Represents the global execution object in AuroraScript.
    /// It contains global variables, functions, and the registry of loaded modules.
    /// </summary>
    public sealed class ScriptGlobal : ScriptObject
    {
        /// <summary>
        /// The engine instance associated with this global object.
        /// </summary>
        public readonly AuroraEngine Engine;

        /// <summary>
        /// A collection of all modules loaded within this global scope.
        /// </summary>
        internal readonly ScriptObject Modules = new ScriptObject();

        /// <summary>
        /// A hash map connecting emitted source path hashes to their metadata for stack traces.
        /// </summary>
        internal readonly Dictionary<int, ModuleMeta> sourcePathHash = new Dictionary<int, ModuleMeta>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptGlobal"/> class.
        /// </summary>
        /// <param name="engine">The engine instance.</param>
        /// <param name="prototype">The prototype object to inherit from. Typically another <see cref="ScriptGlobal"/> or null.</param>
        internal ScriptGlobal(AuroraEngine engine, ScriptObject prototype = null) : base(prototype)
        {
            Engine = engine;
            // Define the 'modules' property on the global object, making it non-writable and non-enumerable.
            base.Define("modules", Modules, false, false);
            base.Define("getModule", ScriptDatum.FromBonding(GET_MODULE), false, false);
            engine.BuiltInRegistry.RegisterModules(this);
        }

        /// <summary>
        /// Gets a loaded module by its explicit <c>@module</c> name for script callers.
        /// </summary>
        /// <remarks>
        /// Module storage remains keyed only by resolved source full path. This method performs
        /// explicit-name lookup without adding another registry entry.
        /// </remarks>
        internal static void GET_MODULE(
            ScriptContext context,
            ScriptObject thisObject,
            System.Span<ScriptDatum> arguments,
            ref ScriptDatum result)
        {
            if (thisObject is ScriptGlobal global &&
                arguments.TryGetString(0, out var name) &&
                global.TryGetModule(name, out var module))
            {
                ScriptDatum.WriteAsObject(ref result, module);
            }
        }

        /// <summary>
        /// Creates a new <see cref="ScriptGlobal"/> instance inheriting from a specified prototype.
        /// </summary>
        /// <param name="engine">The engine instance.</param>
        /// <param name="prototype">The prototype global object.</param>
        /// <returns>A new <see cref="ScriptGlobal"/> instance.</returns>
        internal static ScriptGlobal With(AuroraEngine engine, ScriptObject prototype)
        {
            return new ScriptGlobal(engine, prototype);
        }

        /// <summary>
        /// Sets a global property value, converting the CLR value to a script object.
        /// </summary>
        /// <param name="key">The property name.</param>
        /// <param name="value">The value to set.</param>
        public void SetPropertyValue(string key, object value)
        {
            ScriptDatum datum = default;
            ClrMarshaller.WriteToDatum(ref datum, value);
            base.Define(key, datum, true, true);
        }

        /// <summary>
        /// Defines a property on the global object with specified attributes.
        /// </summary>
        /// <param name="key">The property name.</param>
        /// <param name="value">The value to associate with the property.</param>
        /// <param name="writeable">Whether the property value can be changed.</param>
        /// <param name="enumerable">Whether the property shows up in enumeration.</param>
        public void Define(string key, object value, bool writeable = true, bool enumerable = true)
        {
            ScriptDatum datum = default;
            ClrMarshaller.WriteToDatum(ref datum, value);
            base.Define(key, datum, writeable, enumerable);
        }

        /// <summary>
        /// Defines a global property when the value is already represented as a <see cref="ScriptDatum"/>.
        /// </summary>
        /// <param name="key">The property name.</param>
        /// <param name="value">The value to associate with the property.</param>
        /// <param name="writeable">Whether the property value can be changed.</param>
        /// <param name="enumerable">Whether the property shows up in enumeration.</param>
        public sealed override void Define(string key, ScriptDatum value, bool writeable = true, bool enumerable = true)
        {
            base.Define(key, value, writeable, enumerable);
        }

        /// <summary>
        /// Retrieves a property value from the global object. 
        /// If the property is not found locally, it attempts to resolve it via the engine's CLR type registry.
        /// </summary>
        /// <param name="ctx">The execution context.</param>
        /// <param name="key">The property name.</param>
        /// <returns>The property value if found; otherwise, <see cref="ScriptObject.Null"/>.</returns>
        internal sealed override ScriptObject GetPropertyValue(ScriptContext ctx, string key)
        {
            return ScriptDatum.ToObject(GetPropertyDatum(ctx, key));
        }

        internal sealed override ScriptDatum GetPropertyDatum(ScriptContext ctx, string key)
        {
            var datum = base.GetPropertyDatum(ctx, key);
            if (datum.Kind == ValueKind.Null && Engine.ClrRegistry.TryGetClrType(key, out var clrType))
            {
                return ScriptDatum.FromObject(clrType);
            }
            return datum;
        }

        /// <summary>
        /// Ensures a module exists in the global scope. If it doesn't, a new one is created.
        /// </summary>
        /// <param name="name">The explicit module name, or null for an anonymous module.</param>
        /// <param name="source">The resolved source that identifies the module.</param>
        /// <returns>The existing or newly created <see cref="ScriptModule"/>.</returns>
        internal ScriptModule EnsureModule(string name, ScriptSourceReference source)
        {
            if (TryGetModuleByPath(source.FullPath, out var module))
            {
                if (!string.IsNullOrEmpty(name) &&
                    !string.Equals(module.Name, name, System.StringComparison.Ordinal))
                {
                    throw new AuroraRuntimeException(
                        $"Module source '{source.FullPath}' is already loaded with a different explicit name.");
                }

                sourcePathHash[source.FullPath.GetHashCode()] = new ModuleMeta(module.Source);
                return module;
            }

            EnsureModuleNameAvailable(name, source.FullPath);
            module = new ScriptModule(name, source);
            Modules.Define(source.FullPath, module, true, true);
            sourcePathHash[source.FullPath.GetHashCode()] = new ModuleMeta(source);
            return module;
        }

        /// <summary>
        /// Registers a module into the global module registry.
        /// </summary>
        /// <param name="hash">The source path hash embedded in emitted locations.</param>
        /// <param name="module">The module instance to register.</param>
        /// <exception cref="AuroraRuntimeException">Thrown when the source or explicit name conflicts with a loaded module.</exception>
        internal void RegisterModule(int hash, ScriptModule module)
        {
            if (TryGetModuleByPath(module.Source.FullPath, out var existing))
            {
                if (!string.IsNullOrEmpty(module.Name) &&
                    !string.Equals(existing.Name, module.Name, System.StringComparison.Ordinal))
                {
                    throw new AuroraRuntimeException(
                        $"Module source '{module.Source.FullPath}' is already loaded with a different explicit name.");
                }

                sourcePathHash[hash] = new ModuleMeta(existing.Source);
                return;
            }

            EnsureModuleNameAvailable(module.Name, module.Source.FullPath);
            Modules.Define(module.Source.FullPath, module, true, true);
            sourcePathHash[hash] = new ModuleMeta(module.Source);
        }

        /// <summary>
        /// Tries to retrieve a module by its name.
        /// </summary>
        /// <param name="name">The explicit name of the module.</param>
        /// <param name="module">The retrieved module if successful.</param>
        /// <returns>True if the module was found; otherwise, false.</returns>
        internal bool TryGetModule(string name, out ScriptModule module)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var keys = Modules.EnumerationKeys();
                for (var i = 0; i < keys.Count; i++)
                {
                    if (Modules.GetPropertyValue(keys[i]) is ScriptModule candidate &&
                        string.Equals(candidate.Name, name, System.StringComparison.Ordinal))
                    {
                        module = candidate;
                        return true;
                    }
                }
            }

            module = null;
            return false;
        }

        /// <summary>
        /// Retrieves a module by its absolute source path.
        /// </summary>
        /// <param name="fullPath">The absolute source path of the module.</param>
        /// <returns>The <see cref="ScriptModule"/> if found; otherwise, null.</returns>
        internal ScriptModule GetModuleByPath(string fullPath)
        {
            return TryGetModuleByPath(fullPath, out var module) ? module : null;
        }

        internal bool TryGetModuleByPath(string fullPath, out ScriptModule module)
        {
            var ext = Modules.GetPropertyValue(fullPath);
            if (ext is ScriptModule exact)
            {
                module = exact;
                return true;
            }

            var keys = Modules.EnumerationKeys();
            for (var i = 0; i < keys.Count; i++)
            {
                if (ScriptPath.Comparer.Equals(keys[i], fullPath) &&
                    Modules.GetPropertyValue(keys[i]) is ScriptModule candidate)
                {
                    module = candidate;
                    return true;
                }
            }

            module = null;
            return false;
        }

        private void EnsureModuleNameAvailable(string name, string fullPath)
        {
            if (string.IsNullOrEmpty(name) || !TryGetModule(name, out var existing))
            {
                return;
            }

            throw new AuroraRuntimeException(
                $"Module name '{name}' is already used by source '{existing.Source.FullPath}', not '{fullPath}'.");
        }

        /// <summary>
        /// Returns a string representation of the global object.
        /// </summary>
        /// <returns>The string "global".</returns>
        public override string ToString()
        {
            return "global";
        }
    }
}
