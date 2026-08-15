using AuroraScript.Runtime.Types;
using System.Diagnostics;

namespace AuroraScript.Runtime.Property
{
    /// <summary>
    /// Represents a property descriptor within a <see cref="ScriptObject"/>.
    /// Encapsulates the property's value, name (key), and its attributes such as writability and enumerability.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplayValue,nq}", Name = "{Key,nq}", Type = "{DebuggerDisplayType,nq}")]
    internal struct PropertyDescriptor
    {
        /// <summary> The current value of the property. </summary>
        public ScriptDatum Datum;
        public ScriptObject Value
        {
            readonly get => ScriptDatum.ToObject(Datum);
            set => Datum = ScriptDatum.FromObject(value);
        }
        private PropertyAccessor accessor;
        /// <summary> The Getter Function of the property. </summary>
        public ClosureFunction Getter
        {
            readonly get => accessor?.Getter;
            set
            {
                if (value == null)
                {
                    if (accessor != null) accessor.Getter = null;
                    return;
                }
                (accessor ??= new PropertyAccessor()).Getter = value;
            }
        }
        /// <summary> The Setter Function of the property. </summary>
        public ClosureFunction Setter
        {
            readonly get => accessor?.Setter;
            set
            {
                if (value == null)
                {
                    if (accessor != null) accessor.Setter = null;
                    return;
                }
                (accessor ??= new PropertyAccessor()).Setter = value;
            }
        }
        /// <summary> Gets a value indicating whether this property is an accessor (has a getter or setter). </summary>
        public readonly bool IsAccessor => accessor != null && (accessor.Getter != null || accessor.Setter != null);
        public readonly bool IsDefined => Datum.Kind != ValueKind.Null || Datum.Object != null || IsAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDescriptor"/> class with a key, value, and attributes.
        /// </summary>
        internal PropertyDescriptor(ClosureFunction getter, ClosureFunction setter, ScriptObject value)
        {
            Getter = getter;
            Setter = setter;
            Datum = ScriptDatum.FromObject(value);
        }

        internal PropertyDescriptor(ClosureFunction getter, ClosureFunction setter, ScriptDatum datum)
        {
            Getter = getter;
            Setter = setter;
            Datum = datum;
        }

        internal PropertyDescriptor(ScriptDatum datum, PropertyAccessor accessor)
        {
            Datum = datum;
            this.accessor = accessor;
        }


        /// <summary> Gets a string representation of the value for the debugger display. </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal string DebuggerDisplayValue
        {
            get
            {
                return IsDefined ? ScriptDatum.ToString(Datum) : "<empty>";
            }
        }

    }

    internal sealed class PropertyAccessor
    {
        public ClosureFunction Getter;
        public ClosureFunction Setter;
    }
}
