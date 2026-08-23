using AuroraScript.Core;
using System;
using System.Collections.Generic;

namespace AuroraScript.Runtime.Package
{
    /// <summary>
    /// Immutable engine-scoped index shared by source resolution and runtime globals.
    /// </summary>
    internal sealed class BuiltinModuleRegistry
    {
        private readonly BuiltInModuleDefinition[] _definitions;
        private readonly Dictionary<string, BuiltInModuleDefinition> _byName;
        private readonly Dictionary<string, BuiltInModuleDefinition> _byModulePath;
        private readonly Dictionary<string, BuiltInModuleDefinition> _byFullPath;

        internal BuiltinModuleRegistry(IReadOnlyList<BuiltInModuleDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            _definitions = new BuiltInModuleDefinition[definitions.Count];
            _byName = new Dictionary<string, BuiltInModuleDefinition>(
                definitions.Count,
                StringComparer.Ordinal);
            _byModulePath = new Dictionary<string, BuiltInModuleDefinition>(
                definitions.Count,
                StringComparer.Ordinal);
            _byFullPath = new Dictionary<string, BuiltInModuleDefinition>(
                definitions.Count,
                ScriptPath.Comparer);

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i] ?? throw new ArgumentException(
                    "Built-in module definitions cannot contain null entries.",
                    nameof(definitions));

                if (!_byName.TryAdd(definition.Name, definition))
                {
                    throw new InvalidOperationException(
                        $"The built-in module name '{definition.Name}' is already configured.");
                }

                if (!_byModulePath.TryAdd(definition.ModulePath, definition))
                {
                    throw new InvalidOperationException(
                        $"The built-in module path '{definition.ModulePath}' is already configured.");
                }

                if (!_byFullPath.TryAdd(definition.Reference.FullPath, definition))
                {
                    throw new InvalidOperationException(
                        $"The built-in module source '{definition.Reference.FullPath}' is already configured.");
                }

                _definitions[i] = definition;
            }
        }

        internal int Count => _definitions.Length;

        internal bool TryGetByName(string name, out BuiltInModuleDefinition definition)
        {
            return _byName.TryGetValue(name, out definition);
        }

        internal bool TryResolve(string requestedPath, out BuiltInModuleDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(requestedPath))
            {
                return false;
            }

            if (_byModulePath.TryGetValue(requestedPath, out definition))
            {
                return true;
            }

            if (!requestedPath.StartsWith(BuiltInModuleDefinition.Root, StringComparison.Ordinal))
            {
                return false;
            }

            string normalizedPath;
            try
            {
                normalizedPath = ScriptPath.NormalizeFullPath(requestedPath);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return _byFullPath.TryGetValue(normalizedPath, out definition);
        }

        internal bool TryGet(
            ScriptSourceReference reference,
            out BuiltInModuleDefinition definition)
        {
            definition = null;
            return ScriptPath.NormalizedRootsEqual(
                    reference.BaseDirectory,
                    BuiltInModuleDefinition.Root) &&
                _byFullPath.TryGetValue(reference.FullPath, out definition);
        }

        internal void RegisterModules(ScriptGlobal global)
        {
            if (global == null) throw new ArgumentNullException(nameof(global));

            for (var i = 0; i < _definitions.Length; i++)
            {
                var definition = _definitions[i];
                var module = definition.CreateModule();
                global.RegisterModule(
                    definition.Reference.FullPath.GetHashCode(),
                    module);
            }
        }
    }
}
