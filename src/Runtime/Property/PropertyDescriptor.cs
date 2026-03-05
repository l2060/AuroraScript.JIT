using AuroraScript.Runtime.Types;
using System.Diagnostics;

namespace AuroraScript.Runtime.Property
{
    /// <summary>
    /// Represents a property descriptor within a <see cref="ScriptObject"/>.
    /// Encapsulates the property's value, name (key), and its attributes such as writability and enumerability.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplayValue,nq}", Name = "{Key,nq}", Type = "{DebuggerDisplayType,nq}")]
    internal sealed class PropertyDescriptor
    {
        /// <summary> The current value of the property. </summary>
        public ScriptObject Value;
        /// <summary> The Getter Function of the property. </summary>
        public ClosureFunction Getter;
        /// <summary> The Setter Function of the property. </summary>
        public ClosureFunction Setter;
        /// <summary> Gets a value indicating whether this property is an accessor (has a getter or setter). </summary>
        public bool IsAccessor => Getter != null || Setter != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDescriptor"/> class with a key, value, and attributes.
        /// </summary>
        internal PropertyDescriptor(ClosureFunction getter, ClosureFunction setter, ScriptObject value)
        {
            Getter = getter;
            Setter = setter;
            Value = value;
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

    }
}
