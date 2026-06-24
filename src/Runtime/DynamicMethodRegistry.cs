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
        private static readonly ConcurrentDictionary<int, Delegate> _registry = new();

        internal static int Count => _registry.Count;

        public static int Reserve()
        {
            return Interlocked.Increment(ref _nextId);
        }

        public static void RegisterReserved(int id, String methodName, Delegate del)
        {
            _registry[id] = del;
        }

        /// <summary>
        /// Registers a delegate and returns a unique ID.
        /// </summary>
        public static int Register(String methodName, ScriptFunctionDelegate del)
        {
            return Register(methodName, (Delegate)del);
        }

        public static int Register(String methodName, ScriptFunctionDelegate0 del)
        {
            return Register(methodName, (Delegate)del);
        }

        public static int Register(String methodName, ScriptFunctionDelegate1 del)
        {
            return Register(methodName, (Delegate)del);
        }

        public static int Register(String methodName, ScriptFunctionDelegate2 del)
        {
            return Register(methodName, (Delegate)del);
        }

        public static int Register(String methodName, ScriptFunctionDelegate3 del)
        {
            return Register(methodName, (Delegate)del);
        }

        public static int Register(String methodName, ScriptFunctionDelegate4 del)
        {
            return Register(methodName, (Delegate)del);
        }

        public static int Register(String methodName, ScriptFunctionDelegate5 del)
        {
            return Register(methodName, (Delegate)del);
        }

        public static int Register(String methodName, ScriptFunctionDelegate6 del)
        {
            return Register(methodName, (Delegate)del);
        }

        public static int Register(String methodName, ScriptFunctionDelegate7 del)
        {
            return Register(methodName, (Delegate)del);
        }

        private static int Register(String methodName, Delegate del)
        {
            int id = Reserve();
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
                return (ScriptFunctionDelegate)del;
            }
            throw new InvalidOperationException($"Delegate with ID {id} not found in registry.");
        }

        public static ScriptFunctionDelegate0 Resolve0(int id)
        {
            if (_registry.TryGetValue(id, out var del))
            {
                return (ScriptFunctionDelegate0)del;
            }
            throw new InvalidOperationException($"Delegate with ID {id} not found in registry.");
        }

        public static ScriptFunctionDelegate1 Resolve1(int id)
        {
            if (_registry.TryGetValue(id, out var del))
            {
                return (ScriptFunctionDelegate1)del;
            }
            throw new InvalidOperationException($"Delegate with ID {id} not found in registry.");
        }

        public static ScriptFunctionDelegate2 Resolve2(int id)
        {
            if (_registry.TryGetValue(id, out var del))
            {
                return (ScriptFunctionDelegate2)del;
            }
            throw new InvalidOperationException($"Delegate with ID {id} not found in registry.");
        }

        public static ScriptFunctionDelegate3 Resolve3(int id)
        {
            if (_registry.TryGetValue(id, out var del))
            {
                return (ScriptFunctionDelegate3)del;
            }
            throw new InvalidOperationException($"Delegate with ID {id} not found in registry.");
        }

        public static ScriptFunctionDelegate4 Resolve4(int id)
        {
            if (_registry.TryGetValue(id, out var del))
            {
                return (ScriptFunctionDelegate4)del;
            }
            throw new InvalidOperationException($"Delegate with ID {id} not found in registry.");
        }

        public static ScriptFunctionDelegate5 Resolve5(int id)
        {
            if (_registry.TryGetValue(id, out var del))
            {
                return (ScriptFunctionDelegate5)del;
            }
            throw new InvalidOperationException($"Delegate with ID {id} not found in registry.");
        }

        public static ScriptFunctionDelegate6 Resolve6(int id)
        {
            if (_registry.TryGetValue(id, out var del))
            {
                return (ScriptFunctionDelegate6)del;
            }
            throw new InvalidOperationException($"Delegate with ID {id} not found in registry.");
        }

        public static ScriptFunctionDelegate7 Resolve7(int id)
        {
            if (_registry.TryGetValue(id, out var del))
            {
                return (ScriptFunctionDelegate7)del;
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

        internal static bool Contains(int id)
        {
            return _registry.ContainsKey(id);
        }
    }
}
