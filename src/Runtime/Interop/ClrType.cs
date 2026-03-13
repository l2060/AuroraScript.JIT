using AuroraScript.Runtime.Types;
using System;
using System.Reflection;

namespace AuroraScript.Runtime.Interop
{
    /// <summary>
    /// Defines the access permissions for a CLR type exposed to AuroraScript.
    /// </summary>
    [Flags]
    public enum TypeAccess
    {
        /// <summary>
        /// Grants permission to invoke public instance constructors.
        /// </summary>
        Constructor = 1,
        /// <summary>
        /// Grants permission to access public static members (properties, fields, and methods).
        /// </summary>
        Static = 2,
        /// <summary>
        /// Grants full access to both constructors and static members.
        /// </summary>
        All = Constructor | Static
    }

    /// <summary>
    /// Represents a .NET type (CLR type) that has been exposed to the AuroraScript runtime.
    /// This class allows scripts to instantiate .NET objects and access their static members.
    /// </summary>
    public sealed class ClrType : ScriptType
    {
        internal readonly TypeAccess _access;
        internal readonly ClrTypeDescriptor _descriptor;
        internal readonly Lazy<ConstructorInfo[]> _constructors;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClrType"/> class.
        /// </summary>
        /// <param name="type">The underlying .NET <see cref="Type"/>.</param>
        /// <param name="descriptor">The descriptor containing metadata about the CLR type.</param>
        /// <param name="access">The access permissions for this type.</param>
        internal ClrType(Type type, ClrTypeDescriptor descriptor, TypeAccess access) : base(type.Name)
        {
            _constructors = new Lazy<ConstructorInfo[]>(() => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Frozen();
            _access = access;
        }

        /// <summary>
        /// Instantiates a new instance of the CLR type using the provided arguments.
        /// Performs constructor overload resolution based on the script arguments.
        /// </summary>
        /// <param name="ctx">The current script execution context.</param>
        /// <param name="args">The arguments passed to the constructor.</param>
        /// <param name="result">The resulting <see cref="ScriptDatum"/> containing the new <see cref="ClrInstanceObject"/>.</param>
        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var constructors = _constructors.Value;
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException($"CLR type '{_descriptor.Type.FullName}' does not expose public constructors.");
            }
            if ((_access & TypeAccess.Constructor) != TypeAccess.Constructor)
            {
                throw new InvalidOperationException($"CLR type '{_descriptor.Type.FullName}' does not have constructor access rights.");
            }

            foreach (var ctor in constructors)
            {
                if (!ClrMarshaller.TryBuildArguments(ctor, args, out var invokeArgs))
                {
                    continue;
                }
                var instance = ctor.Invoke(invokeArgs);
                ScriptDatum.WriteAsObject(ref result, new ClrInstanceObject(_descriptor, instance));
                return;
            }
            throw new InvalidOperationException($"No matching constructor found for '{_descriptor.Type.FullName}'.");
        }

        /// <summary>
        /// Retrieves a static property or method from the CLR type.
        /// </summary>
        /// <param name="ctx">The current script execution context.</param>
        /// <param name="key">The name of the static member to retrieve.</param>
        /// <returns>The script object representing the static member's value or a method binding.</returns>
        internal sealed override ScriptObject GetPropertyValue(ScriptContext ctx, String key)
        {
            if ((_access & TypeAccess.Static) != TypeAccess.Static)
            {
                throw new InvalidOperationException($"CLR type '{_descriptor.Type.FullName}' does not have static member access rights.");
            }
            var getter = _descriptor.GetGetter(key);
            if (getter != null)
            {
                var value = getter(null);
                return ClrMarshaller.ToScript(value);
            }
            var method = _descriptor.GetMethods(key, true);
            if (method != null)
            {
                return method;
            }
            ThrowHelper.ThrowNotfoundProperty(key);
            return ScriptObject.Null;
        }

        /// <summary>
        /// Sets a static property on the CLR type.
        /// </summary>
        /// <param name="ctx">The current script execution context.</param>
        /// <param name="key">The name of the static property to set.</param>
        /// <param name="value">The new value to assign to the property.</param>
        internal sealed override void SetPropertyValue(ScriptContext ctx, String key, ScriptObject value)
        {
            var setter = _descriptor.GetSetter(key);
            if (setter != null)
            {
                var targetType = setter.Type;
                if (targetType != null)
                {
                    if (!ClrMarshaller.TryConvertArgument(value, targetType, out var converted))
                    {
                        throw new InvalidOperationException($"Cannot convert script value to '{targetType.FullName}'.");
                    }
                    setter.Setter(null, converted);
                }
                return;
            }
            ThrowHelper.ThrowNotfoundProperty(key);
        }

        /// <summary>
        /// Returns a string representation of the CLR type.
        /// </summary>
        public override string ToString()
        {
            return $"[clr type {_descriptor.Type.FullName}]";
        }
    }
}

