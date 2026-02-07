using AuroraScript.Runtime.Types;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Represents a script module within the AuroraScript runtime.
    /// Modules serve as namespaces that contain their own sets of properties and variables.
    /// </summary>
    public sealed class ScriptModule : ScriptObject
    {
        /// <summary> Gets the name of the module. </summary>
        public readonly string Name;

        /// <summary> Gets the unique relative path that identifies this module. </summary>
        public readonly string ModulePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptModule"/> class.
        /// </summary>
        /// <param name="moduleName">The name of the module.</param>
        /// <param name="modulePath">The identifying path of the module.</param>
        internal ScriptModule(string moduleName, string modulePath)
        {
            Name = moduleName;
            ModulePath = modulePath;
        }

        /// <summary>
        /// Returns a string that represents the current module.
        /// </summary>
        /// <returns>A string in the format "module: [Name]".</returns>
        public override string ToString()
        {
            return $"module: {Name}";
        }
    }
}
