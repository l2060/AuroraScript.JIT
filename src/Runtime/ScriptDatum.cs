using AuroraScript.Runtime.Types;
using System;
using System.Runtime.InteropServices;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Represents a primary data unit (datum) in the AuroraScript runtime.
    /// This is a value type implemented as a tagged union using an explicit layout to minimize memory overhead.
    /// It can store primitive values (Number, Boolean, Null) or references to script objects.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct ScriptDatum : IEquatable<ScriptDatum>
    {
        /// <summary>
        /// The kind of value stored in this datum (e.g., Null, Boolean, Number, String, Object).
        /// </summary>
        [FieldOffset(0)]
        public ValueKind Kind;

        /// <summary>
        /// The double-precision floating-point number value. 
        /// Overlays with other 8-byte value fields.
        /// </summary>
        [FieldOffset(8)]
        public double Number;

        /// <summary>
        /// The boolean value stored in this datum. 
        /// Overlays with other 8-byte value fields.
        /// </summary>
        [FieldOffset(8)]
        public bool Boolean;

        /// <summary>
        /// The reference to a <see cref="ScriptObject"/> (including functions, arrays, and standard objects).
        /// </summary>
        [FieldOffset(16)]
        public ScriptObject Object;

        /// <summary>
        /// Gets or sets the datum value as a <see cref="StringValue"/>.
        /// This is a convenience property for accessing the underlying object as a string.
        /// </summary>
        public StringValue String
        {
            readonly get => Object as StringValue;
            set => Object = value;
        }

        /// <summary>
        /// Returns a string representation of the datum's value.
        /// </summary>
        /// <returns>A string representing the current value.</returns>
        public override readonly string ToString()
        {
            return ScriptDatum.ToString(this);
        }

        /// <summary>
        /// Serves as the default hash function for the <see cref="ScriptDatum"/>.
        /// </summary>
        /// <returns>A hash code for the current datum based on its kind and value.</returns>
        public override readonly int GetHashCode()
        {
            return Kind switch
            {
                ValueKind.Null => ScriptObject.Null.GetHashCode(),
                ValueKind.Boolean => Boolean.GetHashCode(),
                ValueKind.Number => Number.GetHashCode(),
                ValueKind.String => String.GetHashCode(),
                _ => Object.GetHashCode(),
            };
        }

        /// <summary>
        /// Determines whether the specified <see cref="ScriptDatum"/> is equal to the current instance.
        /// Supports type-aware comparison and loose numeric equality (if applicable).
        /// </summary>
        /// <param name="other">The datum to compare with the current instance.</param>
        /// <returns>True if the values are considered equal; otherwise, false.</returns>
        public readonly bool Equals(ScriptDatum other)
        {
            var a = other;
            var b = this;
            if (a.Kind == b.Kind)
            {
                return a.Kind switch
                {
                    ValueKind.Null => true,
                    ValueKind.Boolean => a.Boolean == b.Boolean,
                    ValueKind.Number => a.Number == b.Number,
                    ValueKind.String => a.String.Value == b.String.Value,
                    _ => ReferenceEquals(a.Object, b.Object),
                };
            }

            // Fallback to numeric comparison if both sides can be treated as numbers
            if (ScriptDatum.TryToNumber(a, out var na) && ScriptDatum.TryToNumber(b, out var nb))
            {
                return na == nb;
            }
            return false;
        }
    }
}
