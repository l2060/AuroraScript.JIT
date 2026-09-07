using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Marks a static-only <see cref="NativeTypeAttribute"/> as an opt-in importable
    /// package. The type is never registered on the script global; scripts access it through
    /// <c>import alias from "importPath"</c> after the host enables it with
    /// <see cref="EngineOptions.WithPackages"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class NativePackageAttribute : Attribute
    {
        /// <summary>Declares the bare import path, for example <c>fs</c>.</summary>
        public NativePackageAttribute(string importPath)
        {
            ImportPath = importPath ?? throw new ArgumentNullException(nameof(importPath));
        }

        /// <summary>Bare path used by script imports.</summary>
        public string ImportPath { get; }
    }
}
