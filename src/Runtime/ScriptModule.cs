using AuroraScript.Runtime.Types;
using AuroraScript.Runtime.Property;
using AuroraScript.Core;
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
        /// Defines a compiler-emitted exported module member.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void DefineExport(
            string name,
            ScriptDatum value,
            bool writable,
            bool nativeFunction,
            bool force)
        {
            DefineMember(name, value, exported: true, writable, nativeFunction, force);
        }

        /// <summary>
        /// Defines a compiler-emitted internal module member.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void DefineInternal(
            string name,
            ScriptDatum value,
            bool writable,
            bool nativeFunction,
            bool force)
        {
            DefineMember(name, value, exported: false, writable, nativeFunction, force);
        }

        /// <summary>
        /// Reports whether the module contains a compiler-emitted native function.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsNativeFunction(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                hiddenClass.TryGet(name, out var meta) &&
                meta.NativeFunction;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal string[] GetOwnMemberNames()
        {
            var properties = OwnProperties;
            var names = new string[properties.Length];
            for (var i = 0; i < properties.Length; i++)
            {
                names[i] = properties[i].Name;
            }
            return names;
        }

        private void DefineMember(
            string name,
            ScriptDatum value,
            bool exported,
            bool writable,
            bool nativeFunction,
            bool force)
        {
            var propertyFlags = (writable ? PropertyFlags.Writable : 0)
                | (exported
                    ? PropertyFlags.Enumerable | PropertyFlags.ModuleExport
                    : 0)
                | (nativeFunction ? PropertyFlags.NativeFunction : 0);
            InternalDefine(name, value, propertyFlags, force);
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
