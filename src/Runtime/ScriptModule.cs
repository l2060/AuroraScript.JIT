using AuroraScript.Runtime.Types;
using AuroraScript.Core;
using System.Collections.Generic;
using System.ComponentModel;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Represents a script module within the AuroraScript runtime.
    /// Modules serve as namespaces that contain their own sets of properties and variables.
    /// </summary>
    /// <remarks>
    /// <see cref="Source"/> identifies the module. <see cref="Name"/> is only the optional
    /// explicit lookup label used by host module APIs and script <c>global.getModule</c>.
    /// </remarks>
    public sealed class ScriptModule : ScriptObject
    {
        private HashSet<string> _nativeFunctions;

        /// <summary> Gets the explicit lookup name, or null when the module is anonymous. </summary>
        public readonly string Name;

        /// <summary> Gets the resolved source that identifies this module. </summary>
        public readonly ScriptSourceReference Source;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptModule"/> class.
        /// </summary>
        /// <param name="moduleName">The explicit module name, or null for an anonymous module.</param>
        /// <param name="source">The resolved source that identifies the module.</param>
        internal ScriptModule(string moduleName, ScriptSourceReference source)
        {
            Name = moduleName;
            Source = source;
        }

        /// <summary>
        /// Records a compiler-emitted native function that cannot be hot updated.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void RegisterNativeFunction(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                (_nativeFunctions ??= new HashSet<string>(
                    System.StringComparer.Ordinal)).Add(name);
            }
        }

        /// <summary>
        /// Reports whether the module contains a compiler-emitted native function.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsNativeFunction(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                _nativeFunctions?.Contains(name) == true;
        }

        /// <summary>
        /// Returns a string that represents the current module.
        /// </summary>
        /// <returns>A string in the format "module: [Name]".</returns>
        public override string ToString()
        {
            return $"module: {Name ?? Source.ModulePath}";
        }
    }
}
