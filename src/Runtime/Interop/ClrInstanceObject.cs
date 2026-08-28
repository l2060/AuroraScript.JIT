using AuroraScript.Runtime.Types;
using System;
using System.Diagnostics;

namespace AuroraScript.Runtime.Interop
{
    /// <summary>
    /// Represents a specific instance of a .NET class (CLR object) within AuroraScript.
    /// This class acts as a proxy, allowing script code to interact with the properties, fields, and methods 
    /// of the underlying .NET instance.
    /// </summary>
    public sealed class ClrInstanceObject : ScriptObject
    {
        /// <summary> Gets the metadata descriptor for the underlying .NET type. </summary>
        internal readonly ClrTypeDescriptor Descriptor;

        /// <summary> Gets the actual .NET object instance being wrapped. </summary>
        public readonly object Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClrInstanceObject"/> class.
        /// </summary>
        /// <param name="descriptor">The descriptor for the .NET type.</param>
        /// <param name="instance">The .NET instance to wrap.</param>
        internal ClrInstanceObject(ClrTypeDescriptor descriptor, object instance)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Instance = instance;
        }

        /// <summary>
        /// Retrieves the value of a property or a bound method from the .NET instance.
        /// </summary>
        /// <param name="ctx">The current script execution context.</param>
        /// <param name="key">The name of the property or method to retrieve.</param>
        /// <returns>The result of the member access, converted to a script object.</returns>
        internal sealed override ScriptObject GetPropertyValue(ScriptContext ctx, String key)
        {
            return ScriptDatum.ToObject(GetPropertyDatum(ctx, key));
        }

        /// <inheritdoc />
        protected internal sealed override ScriptDatum GetPropertyDatum(ScriptContext ctx, String key)
        {
            var getter = Descriptor.GetGetter(key);
            if (getter != null)
            {
                var value = getter(Instance);
                return ScriptDatum.FromObject(ClrMarshaller.ToScript(value));
            }
            var method = Descriptor.GetMethods(key, false);
            if (method != null)
            {
                return ScriptDatum.FromObject(method.Bound(this));
            }

            Trace.TraceWarning($"CLR Instance ({Descriptor.Type.Name}): Property or method '{key}' not found.");
            return ScriptDatum.Null;
        }

        /// <summary>
        /// Sets the value of a property on the .NET instance.
        /// </summary>
        /// <param name="ctx">The current script execution context.</param>
        /// <param name="key">The name of the property to set.</param>
        /// <param name="value">The new script value to assign.</param>
        internal sealed override void SetPropertyValue(ScriptContext ctx, String key, ScriptObject value)
        {
            SetPropertyDatum(ctx, key, ScriptDatum.FromObject(value));
        }

        /// <inheritdoc />
        protected internal sealed override void SetPropertyDatum(ScriptContext ctx, String key, ScriptDatum value)
        {
            var setter = Descriptor.GetSetter(key);
            if (setter != null)
            {
                var targetType = setter.Type;
                if (targetType != null)
                {
                    if (!ClrMarshaller.TryConvertArgument(in value, targetType, out var converted))
                    {
                        throw new InvalidOperationException($"Cannot convert script value to '{targetType.FullName}'.");
                    }
                    setter.Setter(Instance, converted);
                }
                return;
            }

            Trace.TraceWarning($"CLR Instance ({Descriptor.Type.Name}): Property '{key}' not found or is read-only.");
        }

        /// <summary>
        /// Returns the string representation of the underlying .NET instance.
        /// </summary>
        public override string ToString()
        {
            return Instance.ToString() ?? base.ToString();
        }
    }
}

