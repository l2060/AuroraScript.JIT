using AuroraScript.Runtime;
using System;
using System.Collections.Generic;

namespace AuroraScript.Core
{
    /// <summary>
    /// Immutable engine-scoped index shared by source resolution and runtime globals.
    /// </summary>
    internal sealed class NativePackageRegistry
    {
        private readonly NativePackageDefinition[] _definitions;
        private readonly Dictionary<string, NativePackageDefinition> _byName;
        private readonly Dictionary<string, NativePackageDefinition> _byModulePath;
        private readonly Dictionary<string, NativePackageDefinition> _byFullPath;
        private readonly Dictionary<string, NativePackageDefinition> _byTypeName;

        internal NativePackageRegistry(IReadOnlyList<NativePackageDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            _definitions = new NativePackageDefinition[definitions.Count];
            _byName = new Dictionary<string, NativePackageDefinition>(
                definitions.Count,
                StringComparer.Ordinal);
            _byModulePath = new Dictionary<string, NativePackageDefinition>(
                definitions.Count,
                StringComparer.Ordinal);
            _byFullPath = new Dictionary<string, NativePackageDefinition>(
                definitions.Count,
                ScriptPath.Comparer);
            _byTypeName = new Dictionary<string, NativePackageDefinition>(
                definitions.Count,
                StringComparer.Ordinal);

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i] ?? throw new ArgumentException(
                    "Native package definitions cannot contain null entries.",
                    nameof(definitions));

                if (!_byName.TryAdd(definition.Name, definition))
                {
                    throw new InvalidOperationException(
                        $"The native package name '{definition.Name}' is already configured.");
                }

                if (!_byModulePath.TryAdd(definition.ModulePath, definition))
                {
                    throw new InvalidOperationException(
                        $"The native package path '{definition.ModulePath}' is already configured.");
                }

                if (!_byFullPath.TryAdd(definition.Reference.FullPath, definition))
                {
                    throw new InvalidOperationException(
                        $"The native package source '{definition.Reference.FullPath}' is already configured.");
                }

                if (!_byTypeName.TryAdd(definition.TypeName, definition))
                {
                    throw new InvalidOperationException(
                        $"The native package type '{definition.TypeName}' is already configured.");
                }

                _definitions[i] = definition;
            }
        }

        internal int Count => _definitions.Length;

        internal IReadOnlyList<NativePackageDefinition> Definitions => _definitions;

        internal bool TryGetByName(string name, out NativePackageDefinition definition)
        {
            return _byName.TryGetValue(name, out definition);
        }

        internal bool TryResolve(string requestedPath, out NativePackageDefinition definition)
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

            if (!requestedPath.StartsWith(NativePackageDefinition.Root, StringComparison.Ordinal))
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
            out NativePackageDefinition definition)
        {
            definition = null;
            return ScriptPath.NormalizedRootsEqual(
                    reference.BaseDirectory,
                    NativePackageDefinition.Root) &&
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
