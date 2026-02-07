using System;
using System.Collections.Concurrent;
using System.Threading;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// A registry to store and retrieve delegates by ID.
    /// This is used to safely pass managed delegates to compiled JIT code without using raw pointers.
    /// </summary>
    internal static class DynamicMethodRegistry
    {
        private static int _nextId = 0;
        private static readonly ConcurrentDictionary<int, ScriptFunctionDelegate> _registry = new();

        /// <summary>
        /// Registers a delegate and returns a unique ID.
        /// </summary>
        public static int Register(String methodName, ScriptFunctionDelegate del)
        {
            int id = Interlocked.Increment(ref _nextId);
            _registry[id] = del;
            return id;
        }

        /// <summary>
        /// Resolves a delegate by its ID.
        /// </summary>
        public static ScriptFunctionDelegate Resolve(int id)
        {
            if (_registry.TryGetValue(id, out var del))
            {
                return del;
            }
            throw new InvalidOperationException($"Delegate with ID {id} not found in registry.");
        }

        /// <summary>
        /// Removes a delegate from the registry.
        /// </summary>
        public static void Unregister(int id)
        {
            _registry.TryRemove(id, out _);
        }
    }
}
