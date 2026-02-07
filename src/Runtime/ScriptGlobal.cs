using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Represents metadata for a script module, including its name and path.
    /// </summary>
    /// <param name="ModuleName">The name of the module.</param>
    /// <param name="ModulePath">The file system or virtual path of the module.</param>
    internal record ModuleMeta(string ModuleName, string ModulePath);

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
        /// A hash map connecting module path hashes to their metadata for quick lookup.
        /// </summary>
        internal Dictionary<int, ModuleMeta> modulePathHash = new Dictionary<int, ModuleMeta>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptGlobal"/> class.
        /// </summary>
        /// <param name="engine">The engine instance.</param>
        /// <param name="prototype">The prototype object to inherit from. Typically another <see cref="ScriptGlobal"/> or null.</param>
        internal ScriptGlobal(AuroraEngine engine, ScriptObject prototype = null)
        {
            Engine = engine;
            _prototype = prototype;
            // Define the 'modules' property on the global object, making it non-writable and non-enumerable.
            base.Define("modules", Modules, false, false);
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
            base.Define(key, ClrMarshaller.ToScript(value), true, true);
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
            base.Define(key, ClrMarshaller.ToScript(value), writeable, enumerable);
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
            var obj = base.GetPropertyValue(ctx, key);
            if (obj == Null && Engine.ClrRegistry.TryGetClrType(key, out var clrType))
            {
                return clrType;
            }
            return obj;
        }

        /// <summary>
        /// Ensures a module exists in the global scope. If it doesn't, a new one is created.
        /// </summary>
        /// <param name="name">The name of the module.</param>
        /// <param name="path">The path to the module.</param>
        /// <returns>The existing or newly created <see cref="ScriptModule"/>.</returns>
        internal ScriptModule EnsureModule(string name, string path)
        {
            var ext = Modules.GetPropertyValue(name);
            if (ext is ScriptModule mod)
            {
                return mod;
            }
            var newMod = new ScriptModule(name, path);
            Modules.Define(name, newMod, true, true);
            modulePathHash[path.GetHashCode()] = new ModuleMeta(name, path);
            return newMod;
        }

        /// <summary>
        /// Registers a module into the global module registry.
        /// </summary>
        /// <param name="name">The name of the module.</param>
        /// <param name="hash">The hash code of the module path.</param>
        /// <param name="module">The module instance to register.</param>
        /// <exception cref="AuroraRuntimeException">Thrown if a property with the same name exists but is not a module.</exception>
        internal void RegisterModule(string name, int hash, ScriptModule module)
        {
            var ext = Modules.GetPropertyValue(name);
            if (ext != Null)
            {
                // If it's already a module, we just keep it (for hot-fixing purposes)
                if (ext is ScriptModule) return;
                throw new AuroraRuntimeException(null, null);
            }
            Modules.Define(name, module, true, true);
            modulePathHash[hash] = new ModuleMeta(module.Name, module.ModulePath);
        }

        /// <summary>
        /// Tries to retrieve a module by its name.
        /// </summary>
        /// <param name="name">The name of the module.</param>
        /// <param name="module">The retrieved module if successful.</param>
        /// <returns>True if the module was found; otherwise, false.</returns>
        internal bool TryGetModule(string name, out ScriptModule module)
        {
            var ext = Modules.GetPropertyValue(name);
            if (ext is ScriptModule mod)
            {
                module = mod;
                return true;
            }
            module = null;
            return false;
        }

        /// <summary>
        /// Retrieves a module by its name.
        /// </summary>
        /// <param name="name">The name of the module.</param>
        /// <returns>The <see cref="ScriptModule"/> if found; otherwise, null.</returns>
        internal ScriptModule GetModule(string name)
        {
            var ext = Modules.GetPropertyValue(name);
            if (ext is ScriptModule mod)
            {
                return mod;
            }
            return null;
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
