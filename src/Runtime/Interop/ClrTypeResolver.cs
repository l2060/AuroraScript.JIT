using System;
using System.Collections.Generic;

namespace AuroraScript.Runtime.Interop
{
    /// <summary>
    /// Provides a centralized registry and cache for <see cref="ClrTypeDescriptor"/> instances.
    /// This class ensuring that each .NET type is only analyzed and described once.
    /// </summary>
    internal static class ClrTypeResolver
    {
        private static readonly Dictionary<Type, ClrTypeDescriptor> _typeMap = new();

        /// <summary>
        /// Resolves the <see cref="ClrTypeDescriptor"/> for the specified .NET type, 
        /// creating and caching it if it doesn't already exist.
        /// </summary>
        /// <param name="type">The .NET type to resolve.</param>
        /// <param name="descriptor">When this method returns, contains the descriptor for the type.</param>
        /// <returns>True if the type was successfully resolved; otherwise, false.</returns>
        public static bool ResolveType(Type type, out ClrTypeDescriptor descriptor)
        {
            descriptor = null;
            if (type == null) return false;
            if (!_typeMap.TryGetValue(type, out descriptor))
            {
                descriptor = new ClrTypeDescriptor(type);
                _typeMap.Add(type, descriptor);
            }
            return true;
        }

        /// <summary>
        /// Clears the global type descriptor cache.
        /// </summary>
        public static void clean()
        {
            _typeMap.Clear();
        }
    }
}
