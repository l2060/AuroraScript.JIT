using AuroraScript.Runtime.Types;
using System;
using System.Diagnostics;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Represents a property descriptor within a <see cref="ScriptObject"/>.
    /// Encapsulates the property's value, name (key), and its attributes such as writability and enumerability.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplayValue,nq}", Name = "{Key,nq}", Type = "{DebuggerDisplayType,nq}")]
    internal sealed class ObjectProperty
    {
        /// <summary> The current value of the property. </summary>
        internal ScriptObject Value;

        /// <summary> Indicates whether the property is included in enumerations (e.g., for-in loops). </summary>
        internal Boolean Enumerable;

        /// <summary> Indicates whether the property's value can be changed. </summary>
        internal Boolean Writable;

        /// <summary> The name or key of the property. </summary>
        internal String Key;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectProperty"/> class with a key and attributes, but no initial value.
        /// </summary>
        internal ObjectProperty(String key, bool writeable, bool enumerable)
        {
            Key = key;
            Enumerable = enumerable;
            Writable = writeable;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectProperty"/> class with a key, value, and attributes.
        /// </summary>
        internal ObjectProperty(String key, ScriptObject value, bool writeable, bool enumerable)
        {
            Key = key;
            Value = value;
            Writable = writeable;
            Enumerable = enumerable;
        }


        /// <summary> Gets a string representation of the value for the debugger display. </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal string DebuggerDisplayValue
        {
            get
            {
                return Value.ToString();
            }
        }

        /// <summary>
        /// Creates a bitwise clone of this property descriptor.
        /// </summary>
        /// <returns>A new <see cref="ObjectProperty"/> instance with the same key, value, and attributes.</returns>
        internal ObjectProperty Clone()
        {
            return new ObjectProperty(Key, Value, Writable, Enumerable);
        }


    }
}
