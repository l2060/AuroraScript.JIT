using AuroraScript.Compiler;
using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Package;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace AuroraScript.Core
{
    /// <summary>
    /// Describes an opt-in native package that may be enabled for an <see cref="AuroraEngine"/>.
    /// </summary>
    /// <remarks>
    /// A definition is immutable and may be shared by multiple option instances. A fresh
    /// <see cref="ScriptModule"/> is created for every script global so package state is
    /// isolated between engines and domains. Packages are not registered on the script global.
    /// </remarks>
    public sealed class NativePackageDefinition
    {
        internal const string Root = "package://";

        /// <summary>
        /// Creates a package definition from a generated <see cref="NativePackageAttribute"/> type.
        /// </summary>
        public NativePackageDefinition(Type nativeType)
        {
            var native = NativeExportType.RequireNativePackageType(
                nativeType,
                nameof(nativeType));
            var package = nativeType.GetCustomAttribute<NativePackageAttribute>()!;

            NativeType = nativeType;
            TypeName = native.TypeName;
            ModulePath = NormalizeModulePath(package.ImportPath);
            Name = ResolveName(ModulePath, TypeName);
            Reference = new ScriptSourceReference(Root, Root + ModulePath, ModulePath);
            Source = $"@module({Name});";
            RegisterPackage = nativeType.GetMethod(
                "RegisterPackage",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(ScriptObject) },
                modifiers: null)!;
        }

        /// <summary>
        /// Gets the explicit name used by host module APIs and script <c>global.getModule</c>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the bare path used by script imports.
        /// </summary>
        public string ModulePath { get; }

        /// <summary>
        /// Gets the NativeType catalog name used by compiler direct calls.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Gets the generated package type.
        /// </summary>
        public Type NativeType { get; }

        internal ScriptSourceReference Reference { get; }

        internal string Source { get; }

        internal MethodInfo RegisterPackage { get; }

        internal ScriptModule CreateModule()
        {
            var module = new ScriptModule(Name, Reference) { IsNativePackage = true };
            RegisterPackage.Invoke(null, new object[] { module });
            return module;
        }

        internal static string NormalizeModulePath(string modulePath)
        {
            if (string.IsNullOrWhiteSpace(modulePath))
            {
                throw new ArgumentException("A native package path is required.", nameof(modulePath));
            }

            var normalized = modulePath.Trim().Replace('\\', '/');
            if (ScriptPath.IsPathRooted(normalized) ||
                normalized.StartsWith("/", StringComparison.Ordinal) ||
                normalized.EndsWith("/", StringComparison.Ordinal) ||
                normalized.Contains("//", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A native package path must be a normalized relative path.",
                    nameof(modulePath));
            }

            var segments = normalized.Split('/');
            for (var i = 0; i < segments.Length; i++)
            {
                if (segments[i] is "." or "..")
                {
                    throw new ArgumentException(
                        "A native package path cannot contain '.' or '..' segments.",
                        nameof(modulePath));
                }
            }

            return normalized;
        }

        internal static string ResolveName(string modulePath, string typeName)
        {
            if (IsIdentifier(modulePath))
            {
                return modulePath;
            }

            if (!IsIdentifier(typeName))
            {
                throw new ArgumentException(
                    "A native package name must be a non-keyword AuroraScript identifier.",
                    nameof(typeName));
            }

            return typeName;
        }

        internal static bool IsIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name) ||
                !IsIdentifierStart(name[0]) ||
                Symbols.FromString(name) != null)
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

        private static bool IsIdentifierStart(char value)
        {
            return (value >= 'a' && value <= 'z') ||
                (value >= 'A' && value <= 'Z') ||
                value is '_' or '$' ||
                (value >= '\u4e00' && value <= '\u9fbb');
        }

        private static bool IsIdentifierPart(char value)
        {
            return IsIdentifierStart(value) || (value >= '0' && value <= '9');
        }
    }

    /// <summary>
    /// Mutable builder used by <see cref="EngineOptions.WithPackages"/>.
    /// </summary>
    public sealed class NativePackagesBuilder
    {
        private readonly List<NativePackageDefinition> _definitions = new();

        /// <summary>
        /// Creates a builder initialized from an immutable options snapshot.
        /// </summary>
        public NativePackagesBuilder(IReadOnlyList<NativePackageDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            for (var i = 0; i < definitions.Count; i++)
            {
                Add(definitions[i]);
            }
        }

        /// <summary>
        /// Adds a native package to the engine configuration.
        /// </summary>
        public NativePackagesBuilder Add(NativePackageDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            for (var i = 0; i < _definitions.Count; i++)
            {
                var existing = _definitions[i];
                if (string.Equals(existing.Name, definition.Name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"The native package name '{definition.Name}' is already configured.");
                }

                if (ScriptPath.Comparer.Equals(existing.ModulePath, definition.ModulePath))
                {
                    throw new InvalidOperationException(
                        $"The native package path '{definition.ModulePath}' is already configured.");
                }

                if (string.Equals(existing.TypeName, definition.TypeName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"The native package type '{definition.TypeName}' is already configured.");
                }
            }

            _definitions.Add(definition);
            return this;
        }

        /// <summary>
        /// Adds a generated package type to the engine configuration.
        /// </summary>
        public NativePackagesBuilder Add(Type nativeType)
        {
            return Add(new NativePackageDefinition(nativeType));
        }

        /// <summary>
        /// Adds a generated package type to the engine configuration.
        /// </summary>
        public NativePackagesBuilder Add<T>()
            where T : ScriptObject
        {
            return Add(typeof(T));
        }

        /// <summary>
        /// Removes all native packages from the builder.
        /// </summary>
        public NativePackagesBuilder Clear()
        {
            _definitions.Clear();
            return this;
        }

        internal IReadOnlyList<NativePackageDefinition> ToDefinitions()
        {
            return CreateSnapshot(_definitions);
        }

        internal static IReadOnlyList<NativePackageDefinition> CreateSnapshot(
            IReadOnlyList<NativePackageDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (definitions.Count == 0) return Array.Empty<NativePackageDefinition>();

            var builder = new NativePackagesBuilder();
            for (var i = 0; i < definitions.Count; i++)
            {
                builder.Add(definitions[i]);
            }

            return new ReadOnlyCollection<NativePackageDefinition>(builder._definitions.ToArray());
        }

        private NativePackagesBuilder()
        {
        }
    }

    /// <summary>
    /// Provides the native packages shipped with AuroraScript.
    /// </summary>
    public static class NativePackages
    {
        /// <summary>
        /// Gets file-system access through the <c>fs</c> package.
        /// </summary>
        public static readonly NativePackageDefinition FileSystem = new(typeof(FileSystemSupport));

        /// <summary>
        /// Gets synchronous and callback-based HTTP access through the <c>http</c> package.
        /// </summary>
        public static readonly NativePackageDefinition HttpClient = new(typeof(HttpClientSupport));
    }
}
