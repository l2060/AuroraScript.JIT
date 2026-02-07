using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Emits
{
    /// <summary>
    /// Manages the virtual evaluation stack during CIL emission.
    /// It tracks the types of values currently on the .NET evaluation stack,
    /// enabling automatic type coercion and validation of stack operations.
    /// </summary>
    internal class CILStackManager
    {
        private readonly Stack<Type> _typeStack = new();

        /// <summary>
        /// Pushes a type onto the virtual stack.
        /// </summary>
        public void Push(Type type) => _typeStack.Push(type);

        /// <summary>
        /// Pops a type from the virtual stack.
        /// </summary>
        public Type Pop() => _typeStack.Pop();

        /// <summary>
        /// Peeks at the type on top of the virtual stack.
        /// </summary>
        public Type Peek() => _typeStack.Peek();

        /// <summary>
        /// Gets the number of items on the virtual stack.
        /// </summary>
        public int Count => _typeStack.Count;

        /// <summary>
        /// Clears the virtual stack.
        /// </summary>
        public void Clear() => _typeStack.Clear();

        /// <summary>
        /// Gets the current state of the stack as an array.
        /// Useful for branching and merging stack states.
        /// </summary>
        public Type[] GetState() => _typeStack.ToArray();

        /// <summary>
        /// Restores the stack state from a previously saved array.
        /// </summary>
        public void RestoreState(Type[] state)
        {
            _typeStack.Clear();
            if (state == null) return;
            // The state is returned from ToArray(), so we need to reverse it to restore correctly via Push.
            for (int i = state.Length - 1; i >= 0; i--)
            {
                _typeStack.Push(state[i]);
            }
        }

        /// <summary>
        /// Ensures that the top of the stack matches the target type.
        /// If types don't match, it emits the necessary CIL instructions for type conversion.
        /// </summary>
        /// <param name="il">The IL generator.</param>
        /// <param name="targetType">The expected type on top of the stack.</param>
        public void EnsureTop(ILGenerator il, Type targetType)
        {
            if (_typeStack.Count == 0) return;
            var currentType = _typeStack.Peek();
            if (targetType.IsAssignableFrom(currentType)) return;

            // Conversion to ScriptDatum (the common interchange type)
            if (targetType == typeof(ScriptDatum))
            {
                if (typeof(ScriptObject).IsAssignableFrom(currentType))
                {
                    il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromObject);
                }
                else if (currentType == typeof(string))
                {
                    il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromString);
                }
                else if (currentType == typeof(double))
                {
                    il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromNumber);
                }
                else if (currentType == typeof(bool))
                {
                    il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromBoolean);
                }
            }
            // Conversion to ScriptObject (the base object type)
            else if (targetType == typeof(ScriptObject))
            {
                if (currentType == typeof(ScriptDatum))
                {
                    il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
                }
                else if (currentType == typeof(string))
                {
                    il.Emit(OpCodes.Call, RuntimeMetadata.StringValue_Of);
                }
                else if (currentType == typeof(double))
                {
                    il.Emit(OpCodes.Call, RuntimeMetadata.NumberValue_Of);
                }
                else if (currentType == typeof(bool))
                {
                    il.Emit(OpCodes.Call, RuntimeMetadata.BooleanValue_Of);
                }
            }
            // Coercion to string
            else if (targetType == typeof(string))
            {
                if (currentType == typeof(ScriptDatum))
                {
                    il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToString);
                }
                else
                {
                    // Recursively ensure Top is ScriptDatum first, then call ScriptDatum.ToString()
                    EnsureTop(il, typeof(ScriptDatum));
                    EnsureTop(il, typeof(string));
                    return;
                }
            }
            // Coercion to bool
            else if (targetType == typeof(Boolean))
            {
                if (currentType == typeof(ScriptDatum))
                {
                    il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ToBoolean);
                }
                else
                {
                    // Recursively ensure Top is ScriptDatum first, then call CILHelper.ToBoolean()
                    EnsureTop(il, typeof(ScriptDatum));
                    EnsureTop(il, typeof(Boolean));
                    return;
                }
            }

            _typeStack.Pop();
            _typeStack.Push(targetType);
        }
    }
}
