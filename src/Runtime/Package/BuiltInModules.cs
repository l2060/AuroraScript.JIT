using AuroraScript.Compiler;
using AuroraScript.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AuroraScript.Runtime.Package
{
    /// <summary>
    /// Describes a native module that may be enabled for an <see cref="AuroraEngine"/>.
    /// </summary>
    /// <remarks>
    /// A definition is immutable and may be shared by multiple option instances. A fresh
    /// <see cref="ScriptModule"/> is created for every script global so module state is
    /// isolated between engines and domains.
    /// </remarks>
    public sealed class BuiltInModuleDefinition
    {
        internal const string Root = "builtin://";

        private readonly Action<ScriptModule> _configure;

        /// <summary>
        /// Creates a built-in module whose import path is the same as its module name.
        /// </summary>
        /// <param name="name">The runtime module name and bare import path.</param>
        /// <param name="configure">Configures each newly-created runtime module instance.</param>
        public BuiltInModuleDefinition(string name, Action<ScriptModule> configure)
            : this(name, name, configure)
        {
        }

        /// <summary>
        /// Creates a built-in module with an explicit bare import path.
        /// </summary>
        /// <param name="name">The runtime module name.</param>
        /// <param name="modulePath">The bare path used by script imports.</param>
        /// <param name="configure">Configures each newly-created runtime module instance.</param>
        public BuiltInModuleDefinition(
            string name,
            string modulePath,
            Action<ScriptModule> configure)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A built-in module name is required.", nameof(name));
            }

            ValidateModuleName(name);
            Name = name;
            ModulePath = NormalizeModulePath(modulePath);
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
            Reference = new ScriptSourceReference(Root, Root + ModulePath, ModulePath);
            Source = $"@module({name});";
        }

        /// <summary>
        /// Gets the runtime module name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the bare path used by script imports.
        /// </summary>
        public string ModulePath { get; }

        internal ScriptSourceReference Reference { get; }

        internal string Source { get; }

        internal ScriptModule CreateModule()
        {
            var module = new ScriptModule(Name, Reference.ModulePath, Reference.FullPath);
            _configure(module);
            return module;
        }

        private static string NormalizeModulePath(string modulePath)
        {
            if (string.IsNullOrWhiteSpace(modulePath))
            {
                throw new ArgumentException("A built-in module path is required.", nameof(modulePath));
            }

            var normalized = modulePath.Trim().Replace('\\', '/');
            if (ScriptPath.IsPathRooted(normalized) ||
                normalized.StartsWith("/", StringComparison.Ordinal) ||
                normalized.EndsWith("/", StringComparison.Ordinal) ||
                normalized.Contains("//", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A built-in module path must be a normalized relative path.",
                    nameof(modulePath));
            }

            var segments = normalized.Split('/');
            for (var i = 0; i < segments.Length; i++)
            {
                if (segments[i] is "." or "..")
                {
                    throw new ArgumentException(
                        "A built-in module path cannot contain '.' or '..' segments.",
                        nameof(modulePath));
                }
            }

            return normalized;
        }

        private static void ValidateModuleName(string name)
        {
            if (!IsIdentifierStart(name[0]) || Symbols.FromString(name) != null)
            {
                throw new ArgumentException(
                    "A built-in module name must be a non-keyword AuroraScript identifier.",
                    nameof(name));
            }

            for (var i = 1; i < name.Length; i++)
            {
                if (!IsIdentifierPart(name[i]))
                {
                    throw new ArgumentException(
                        "A built-in module name must be a non-keyword AuroraScript identifier.",
                        nameof(name));
                }
            }
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
    /// Mutable builder used by <see cref="EngineOptions.WithBuiltIns"/>.
    /// </summary>
    public sealed class BuiltInModulesBuilder
    {
        private readonly List<BuiltInModuleDefinition> _definitions = new();

        /// <summary>
        /// Creates a builder initialized from an immutable options snapshot.
        /// </summary>
        public BuiltInModulesBuilder(IReadOnlyList<BuiltInModuleDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            for (var i = 0; i < definitions.Count; i++)
            {
                Add(definitions[i]);
            }
        }

        /// <summary>
        /// Adds a built-in module to the engine configuration.
        /// </summary>
        public BuiltInModulesBuilder Add(BuiltInModuleDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            for (var i = 0; i < _definitions.Count; i++)
            {
                var existing = _definitions[i];
                if (string.Equals(existing.Name, definition.Name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"The built-in module name '{definition.Name}' is already configured.");
                }

                if (ScriptPath.Comparer.Equals(existing.ModulePath, definition.ModulePath))
                {
                    throw new InvalidOperationException(
                        $"The built-in module path '{definition.ModulePath}' is already configured.");
                }
            }

            _definitions.Add(definition);
            return this;
        }

        /// <summary>
        /// Removes all built-in modules from the builder.
        /// </summary>
        public BuiltInModulesBuilder Clear()
        {
            _definitions.Clear();
            return this;
        }

        internal IReadOnlyList<BuiltInModuleDefinition> ToDefinitions()
        {
            return CreateSnapshot(_definitions);
        }

        internal static IReadOnlyList<BuiltInModuleDefinition> CreateSnapshot(
            IReadOnlyList<BuiltInModuleDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (definitions.Count == 0) return Array.Empty<BuiltInModuleDefinition>();

            var builder = new BuiltInModulesBuilder();
            for (var i = 0; i < definitions.Count; i++)
            {
                builder.Add(definitions[i]);
            }

            return new ReadOnlyCollection<BuiltInModuleDefinition>(builder._definitions.ToArray());
        }

        private BuiltInModulesBuilder()
        {
        }
    }

    /// <summary>
    /// Provides the built-in modules shipped with AuroraScript.
    /// </summary>
    public static class BuiltInModules
    {
        /// <summary>
        /// Gets file-system access through the <c>fs</c> module.
        /// </summary>
        public static BuiltInModuleDefinition FileSystem { get; } =
            new BuiltInModuleDefinition("fs", FileSystemModule.Configure);

        /// <summary>
        /// Gets synchronous and callback-based HTTP access through the <c>http</c> module.
        /// </summary>
        public static BuiltInModuleDefinition HttpClient { get; } =
            new BuiltInModuleDefinition("http", HttpClientModule.Configure);
    }
}
