using System;
using System.Collections.Generic;
using System.Threading;

namespace AuroraScript.Runtime.Interop
{
    /// <summary>
    /// A host-side registry for CLR types, managing the mapping between script-visible aliases and actual .NET types.
    /// This allows AuroraScript to discover and interact with host-provided types and libraries.
    /// </summary>
    public sealed class ClrTypeRegistry : IDisposable
    {
        private readonly Dictionary<string, ClrType> _aliasMap = new(StringComparer.Ordinal);

        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
        private bool _disposed;

        /// <summary>
        /// Registers a .NET type in the registry with a specific alias and access permissions.
        /// </summary>
        /// <param name="type">The .NET <see cref="Type"/> to register.</param>
        /// <param name="alias">The alias name used to refer to this type in scripts. If null or empty, the type's simple name is used.</param>
        /// <param name="access">The access permissions (Constructor/Static) granted to the script.</param>
        public void RegisterType(Type type, string alias, TypeAccess access)
        {
            ArgumentNullException.ThrowIfNull(type);
            if (string.IsNullOrWhiteSpace(alias))
            {
                alias = type.Name;
            }
            alias = alias.Trim();

            _lock.EnterWriteLock();
            try
            {
                EnsureNotDisposed();
                ClrTypeResolver.ResolveType(type, out var typeDescriptor);
                var clrType = new ClrType(type, typeDescriptor, access);
                _aliasMap.Add(alias, clrType);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Unregisters a type from the registry using its alias.
        /// </summary>
        /// <param name="alias">The alias of the type to remove.</param>
        /// <returns>True if the type was found and removed; otherwise, false.</returns>
        public bool UnregisterType(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias)) return false;
            _lock.EnterWriteLock();
            try
            {
                EnsureNotDisposed();
                return _aliasMap.Remove(alias);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Attempts to retrieve a registered <see cref="ClrType"/> by its alias.
        /// </summary>
        /// <param name="alias">The alias name to look up.</param>
        /// <param name="descriptor">When this method returns, contains the associated <see cref="ClrType"/>, or null if not found.</param>
        /// <returns>True if the alias exists in the registry; otherwise, false.</returns>
        public bool TryGetClrType(string alias, out ClrType descriptor)
        {
            descriptor = null;
            if (string.IsNullOrWhiteSpace(alias)) return false;
            _lock.EnterReadLock();
            try
            {
                EnsureNotDisposed();
                return _aliasMap.TryGetValue(alias, out descriptor);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ClrTypeRegistry"/> and clears the alias map.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _lock.EnterWriteLock();
            try
            {
                _aliasMap.Clear();
                _disposed = true;
            }
            finally
            {
                _lock.ExitWriteLock();
                _lock.Dispose();
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ClrTypeRegistry));
            }
        }
    }
}

