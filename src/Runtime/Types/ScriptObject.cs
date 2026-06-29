using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Property;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Specifies flags for controlling object behavior, such as immutability and freezing.
    /// </summary>
    [Flags]
    public enum ObjectFlags
    {
        /// <summary> No flags set. </summary>
        None = 0,
        /// <summary> The object is frozen and no further modifications are allowed. </summary>
        Frozen = 1,
        /// <summary> The object is immutable (typically used for built-in constants). </summary>
        Immutable = 2,
        /// <summary> The object participates in custom value equality for == and Object.equal. </summary>
        ValueEquality = 4,
    }

    /// <summary>
    /// Represents the base object type in the AuroraScript runtime.
    /// Supports prototype-based inheritance and dynamic property management.
    /// </summary>
    public partial class ScriptObject
    {
        private ScriptObject prototype;
        /// <summary> Prototype object. </summary>
        public ScriptObject Prototype => prototype;

        private PropertyDescriptor[] propertyValues = Array.Empty<PropertyDescriptor>();

        private HiddenClass hiddenClass = HiddenClass.Root;

        private ObjectFlags flags = ObjectFlags.None;
        private ClrInstanceObject clrFallback;
        /// <summary>
        /// Gets a value indicating whether the object is frozen (read-only).
        /// </summary>
        public bool IsFrozen => (flags & ObjectFlags.Frozen) == ObjectFlags.Frozen;

        /// <summary>
        /// Gets a value indicating whether the object is immutable.
        /// </summary>
        public bool Immutable => (flags & ObjectFlags.Immutable) == ObjectFlags.Immutable;

        internal bool HasValueEquality => (flags & ObjectFlags.ValueEquality) == ObjectFlags.ValueEquality;

        /// <summary>
        /// Initializes an internal immutable object with a specific prototype.
        /// </summary>
        /// <param name="prototype">The prototype object.</param>
        /// <param name="placeholder">A placeholder flag to distinguish this constructor.</param>
        internal ScriptObject(ScriptObject prototype, bool placeholder)
        {
            this.prototype = prototype;
            flags = ObjectFlags.Immutable;
        }

        /// <summary>
        /// Initializes an internal object with a specific prototype.
        /// </summary>
        /// <param name="prototype">The prototype object.</param>
        internal ScriptObject(ScriptObject prototype)
        {
            this.prototype = prototype;
        }

        internal ScriptObject(HiddenClass hiddenClass, PropertyDescriptor[] propertyValues)
        {
            prototype = Prototypes.ObjectPrototype;
            this.hiddenClass = hiddenClass;
            this.propertyValues = propertyValues;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptObject"/> class with the default object prototype.
        /// </summary>
        public ScriptObject()
        {
            prototype = Prototypes.ObjectPrototype;
        }

        /// <summary>
        /// Freezes the object, preventing any further changes to its properties.
        /// </summary>
        [DebuggerHidden]
        public void Frozen()
        {
            flags |= ObjectFlags.Frozen;
        }

        /// <summary>
        /// Enables custom value equality for this object when compared by == and Object.equal.
        /// </summary>
        protected void EnableValueEquality()
        {
            flags |= ObjectFlags.ValueEquality;
        }

        internal virtual bool ValueEquals(ScriptObject other)
        {
            return false;
        }

        /// <summary>
        /// Invokes the object. Built-in objects throw an exception unless overridden (e.g., for functions).
        /// </summary>
        /// <param name="ctx">The script context.</param>
        /// <param name="args">The arguments for invocation.</param>
        /// <returns>The result of the invocation.</returns>
        /// <exception cref="AuroraRuntimeException">Thrown if the object is not a function.</exception>
        internal virtual ScriptDatum Invoke(ScriptContext ctx, Span<ScriptDatum> args)
        {
            throw new AuroraRuntimeException("Object cannot be called.");
        }



        internal virtual ScriptDatum Invoke(ScriptContext ctx)
        {
            return Invoke(ctx, Span<ScriptDatum>.Empty);
        }

        internal virtual ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1)
        {
            DatumBuffer1 buffer = default;
            buffer[0] = arg1;
            return Invoke(ctx, buffer);
        }

        internal virtual ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2)
        {
            DatumBuffer2 buffer = default;
            buffer[0] = arg1;
            buffer[1] = arg2;
            return Invoke(ctx, buffer);
        }

        internal virtual ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3)
        {
            DatumBuffer3 buffer = default;
            buffer[0] = arg1;
            buffer[1] = arg2;
            buffer[2] = arg3;
            return Invoke(ctx, buffer);
        }

        internal virtual ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4)
        {
            DatumBuffer4 buffer = default;
            buffer[0] = arg1;
            buffer[1] = arg2;
            buffer[2] = arg3;
            buffer[3] = arg4;
            return Invoke(ctx, buffer);
        }

        internal virtual ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5)
        {
            DatumBuffer5 buffer = default;
            buffer[0] = arg1;
            buffer[1] = arg2;
            buffer[2] = arg3;
            buffer[3] = arg4;
            buffer[4] = arg5;
            return Invoke(ctx, buffer);
        }

        internal virtual ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6)
        {
            DatumBuffer6 buffer = default;
            buffer[0] = arg1;
            buffer[1] = arg2;
            buffer[2] = arg3;
            buffer[3] = arg4;
            buffer[4] = arg5;
            buffer[5] = arg6;
            return Invoke(ctx, buffer);
        }

        internal virtual ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6, ScriptDatum arg7)
        {
            DatumBuffer7 buffer = default;
            buffer[0] = arg1;
            buffer[1] = arg2;
            buffer[2] = arg3;
            buffer[3] = arg4;
            buffer[4] = arg5;
            buffer[5] = arg6;
            buffer[6] = arg7;
            return Invoke(ctx, buffer);
        }

        internal virtual ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6, ScriptDatum arg7, ScriptDatum arg8)
        {
            DatumBuffer8 buffer = default;
            buffer[0] = arg1;
            buffer[1] = arg2;
            buffer[2] = arg3;
            buffer[3] = arg4;
            buffer[4] = arg5;
            buffer[5] = arg6;
            buffer[6] = arg7;
            buffer[7] = arg8;
            return Invoke(ctx, buffer);
        }

        /// <summary>
        /// Retrieves the value of a property by its key.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <returns>The property value, or Null if not found.</returns>
        public ScriptObject GetPropertyValue(string key)
        {
            return GetPropertyValue(null, key);
        }

        /// <summary>
        /// Sets the value of a property by its key.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The value to set.</param>
        public void SetPropertyValue(string key, ScriptObject value)
        {
            SetPropertyValue(null, key, value);
        }

        /// <summary>
        /// Deletes a property from the object.
        /// </summary>
        /// <param name="key">The property key to delete.</param>
        /// <returns>True if the property was found and deleted; otherwise, false.</returns>
        public bool DeletePropertyValue(string key)
        {
            return DeletePropertyValue(null, key);
        }

        /// <summary>
        /// Internal implementation of property deletion.
        /// </summary>
        internal virtual bool DeletePropertyValue(ScriptContext ctx, string key)
        {
            return InternalDeletePropertyValue(key);
        }

        /// <summary>
        /// Internal implementation of property retrieval, supporting inheritance through the prototype chain.
        /// </summary>
        internal virtual ScriptObject GetPropertyValue(ScriptContext ctx, string key)
        {
            return ScriptDatum.ToObject(GetPropertyDatum(ctx, key));
        }

        internal virtual ScriptDatum GetPropertyDatum(ScriptContext ctx, string key)
        {
            return InternalGetPropertyDatum(ctx, key);
        }

        internal virtual void SetPropertyValue(ScriptContext ctx, string key, ScriptObject value)
        {
            SetPropertyDatum(ctx, key, ScriptDatum.FromObject(value));
        }

        internal virtual void SetPropertyDatum(ScriptContext ctx, string key, ScriptDatum value)
        {
            if (TryResolveProperty(key, out var property) && property.Setter != null)
            {
                property.Setter.Invoke(ctx, value);
                return;
            }
            InternalDefine(key, value);
        }

        /// <summary>
        /// Resolves a property by searching this object and its prototype chain.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="property">The resolved property descriptor.</param>
        /// <returns>True if the property was found; otherwise, false.</returns>
        internal bool TryResolveProperty(string key, out PropertyDescriptor property)
        {
            if (hiddenClass.TryGet(key, out var meta))
            {
                property = propertyValues[meta.Slot];
                return property.IsDefined;
            }
            if (prototype != null)
            {
                return prototype.TryResolveProperty(key, out property);
            }
            property = default;
            return false;
        }

        /// <summary>
        /// Copies all properties from another object to this one.
        /// </summary>
        /// <param name="scriptObject">The source object.</param>
        /// <param name="force">If true, overrides read-only properties.</param>
        public void CopyPropertysFrom(ScriptObject scriptObject, bool force = false)
        {
            scriptObject.CopyProperties(this, force);
        }

        /// <summary>
        /// Copies enumerable own properties from another object to this one.
        /// </summary>
        /// <param name="scriptObject">The source object.</param>
        /// <param name="force">If true, overrides read-only properties.</param>
        public void CopyEnumerablePropertysFrom(ScriptObject scriptObject, bool force = false)
        {
            scriptObject.CopyProperties(this, force, enumerableOnly: true);
        }

        /// <summary>
        /// Defines an object property with specific attributes.
        /// Typically called from host (C#) code.
        /// </summary>
        /// <param name="key">The property name.</param>
        /// <param name="value">The property value.</param>
        /// <param name="writeable">Indicates if the property can be modified after definition.</param>
        /// <param name="enumerable">Indicates if the property appears in enumerations (e.g., for-in loops).</param>
        public virtual void Define(string key, ScriptObject value, bool writeable = true, bool enumerable = true)
        {
            InternalDefine(key, value, writeable, enumerable, false);
        }

        /// <summary>
        /// Defines a datum property with specific attributes.
        /// Typically called from host (C#) code when the value is already in script representation.
        /// </summary>
        /// <param name="key">The property name.</param>
        /// <param name="value">The property value.</param>
        /// <param name="writeable">Indicates if the property can be modified after definition.</param>
        /// <param name="enumerable">Indicates if the property appears in enumerations (e.g., for-in loops).</param>
        public virtual void Define(string key, ScriptDatum value, bool writeable = true, bool enumerable = true)
        {
            InternalDefine(key, value, writeable, enumerable, false);
        }

        /// <summary>
        /// Directly updates or defines a property, potentially bypassing some safety checks.
        /// </summary>
        internal void Patch(string key, ScriptObject value, bool writeable = true, bool enumerable = true)
        {
            InternalDefine(key, value, writeable, enumerable, true);
        }

        internal void InternalDefine(string key, ScriptObject value, bool writeable = true, bool enumerable = true, bool force = false)
        {
            InternalDefine(key, ScriptDatum.FromObject(value), writeable, enumerable, force);
        }

        internal void InternalDefine(string key, ScriptDatum value, bool writeable = true, bool enumerable = true, bool force = false)
        {
            if (Immutable) return;
            if (IsFrozen) ThrowHelper.ThrowFrozen();

            bool alreadyExists = hiddenClass.TryGet(key, out var meta);
            if (alreadyExists)
            {
                if (!force && !meta.Writable) ThrowHelper.ThrowDisableWritable();
            }

            hiddenClass = hiddenClass.AddProperty(key, writeable, enumerable, false, out meta);

            if (meta.Slot >= propertyValues.Length) Resize();

            // Optimization: If the property existed and we're just updating the value, reuse the descriptor
            if (alreadyExists && propertyValues[meta.Slot].IsDefined)
            {
                propertyValues[meta.Slot].Datum = value;
            }
            else
            {
                propertyValues[meta.Slot] = new PropertyDescriptor(null, null, value);
            }
        }







        private void Resize()
        {
            var cap = Math.Max(propertyValues.Length, 2) * 2;
            Array.Resize(ref this.propertyValues, cap);
        }



        private ScriptDatum InternalGetPropertyDatum(ScriptContext ctx, string key)
        {
            if (TryResolveProperty(key, out var property))
            {
                // ECMA-style Getter support
                if (property.Getter != null)
                {
                    return property.Getter.Invoke(ctx);
                }

                var value = property.Datum;
                var valueObject = value.Object;
                if (valueObject is BondingGetter getter)
                {
                    ScriptDatum datum = default;
                    getter.Invoke(this, ref datum);
                    return datum;
                }
                if (valueObject is BondingFunction clrFunc)
                {
                    return ScriptDatum.FromObject(clrFunc.Bind(this));
                }
                return value;
            }
            if (TryGetClrFallback(out var clrInstance))
            {
                var value = clrInstance.GetPropertyDatum(ctx, key);
                if (value.Kind != ValueKind.Null || value.Object != null)
                {
                    return value;
                }
            }
            return ScriptDatum.Null;
        }

        private bool TryGetClrFallback(out ClrInstanceObject clrInstance)
        {
            clrInstance = clrFallback;
            if (clrInstance != null)
            {
                return true;
            }

            var type = GetType();
            if (type == typeof(ScriptObject) || type.Assembly == typeof(ScriptObject).Assembly)
            {
                return false;
            }

            if (!ClrTypeResolver.ResolveType(type, out var descriptor))
            {
                return false;
            }

            clrInstance = new ClrInstanceObject(descriptor, this);
            clrFallback = clrInstance;
            return true;
        }

        private bool InternalDeletePropertyValue(string key)
        {
            if (hiddenClass.TryGet(key, out var meta))
            {
                if (!meta.Writable) ThrowHelper.ThrowDisableWritable();
                hiddenClass = hiddenClass.RemoveProperty(key);
                propertyValues[meta.Slot] = default;
                return true;
            }
            if (prototype != null)
            {
                return prototype.DeletePropertyValue(key);
            }
            return false;
        }

        /// <summary>
        /// Clears all properties from the object if it is not immutable or frozen.
        /// </summary>
        internal void ClearProperties()
        {
            if (Immutable || IsFrozen) return;
            hiddenClass = HiddenClass.Root;
            propertyValues = Array.Empty<PropertyDescriptor>();
        }

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        /// <returns>A string "[object]".</returns>
        public override string ToString()
        {
            return "[object]";
        }

        /// <summary>
        /// Determines if the object is considered "truthy" in conditional evaluations.
        /// </summary>
        /// <returns>Always returns true for standard objects.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsTrue()
        {
            return true;
        }

        /// <summary>
        /// Gets an enumerator for the object's properties, traversing the prototype chain.
        /// </summary>
        /// <returns>A <see cref="ScriptEnumerator"/> containing enumerable property keys.</returns>
        public virtual ScriptEnumerator GetEnumerator()
        {
            if (!Immutable && !HasEnumerablePrototypeProperties())
            {
                return new ScriptEnumerator(hiddenClass.EnumerableKeys);
            }

            var result = new List<ScriptDatum>();
            var seenKeys = new HashSet<string>();
            var current = this;
            while (current != null)
            {
                if (!current.Immutable)
                {
                    var hc = current.hiddenClass;
                    foreach (var item in hc.Properties)
                    {
                        if (item.Meta.Enumerable && seenKeys.Add(item.Name))
                        {
                            result.Add(ScriptDatum.FromString(item.Name));
                        }
                    }
                }
                current = current.prototype;
            }
            return new ScriptEnumerator(result);
        }

        private bool HasEnumerablePrototypeProperties()
        {
            var current = prototype;
            while (current != null)
            {
                if (!current.Immutable && current.hiddenClass.EnumerableCount > 0)
                {
                    return true;
                }
                current = current.prototype;
            }
            return false;
        }

        /// <summary>
        /// Retrieves a list of all enumerable property keys from this object and its prototypes.
        /// </summary>
        /// <returns>A list of property keys.</returns>
        public List<string> EnumerationKeys()
        {
            if (Immutable) return new List<string>();
            var list = new List<string>(8);
            CollectEnumerationKeys(list);
            return list;
        }

        private void CollectEnumerationKeys(List<string> list)
        {
            if (Immutable) return;
            foreach (var item in hiddenClass.Properties)
            {
                if (item.Meta.Enumerable)
                {
                    list.Add(item.Name);
                }
            }
            if (prototype != null)
            {
                prototype.CollectEnumerationKeys(list);
            }
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


        /// <summary>
        /// Copies all own properties from this object to a target object.
        /// </summary>
        /// <param name="target">The destination object to copy properties to.</param>
        /// <param name="force">If true, overrides target properties even if they are marked as non-writable.</param>
        /// <param name="enumerableOnly">If true, copies only enumerable properties.</param>
        public void CopyProperties(ScriptObject target, bool force = false, bool enumerableOnly = false)
        {
            if (Immutable) return;
            foreach (var item in hiddenClass.Properties)
            {
                var key = item.Name;
                var meta = item.Meta;
                if (enumerableOnly && !meta.Enumerable)
                {
                    continue;
                }
                var property = propertyValues[meta.Slot];
                if (target.hiddenClass.TryGet(key, out var targetMeta))
                {
                    if (targetMeta.Writable || force)
                    {
                        // We use InternalDefine to handle the value and basic flags, 
                        // but we also need to copy the getter/setter if they exist.
                        target.InternalDefine(key, property.Datum, meta.Writable, meta.Enumerable, force);
                        target.propertyValues[targetMeta.Slot].Getter = property.Getter;
                        target.propertyValues[targetMeta.Slot].Setter = property.Setter;
                    }
                }
                else
                {
                    target.InternalDefine(key, property.Datum, meta.Writable, meta.Enumerable, force);
                    if (target.hiddenClass.TryGet(key, out targetMeta))
                    {
                        target.propertyValues[targetMeta.Slot].Getter = property.Getter;
                        target.propertyValues[targetMeta.Slot].Setter = property.Setter;
                    }
                }
            }
        }
        /// <summary>
        /// Creates a deep copy of the current script object, including all its own properties and nested objects.
        /// </summary>
        /// <returns>A new <see cref="ScriptObject"/> that is a deep copy of this instance.</returns>
        public ScriptObject DeepClone()
        {
            switch (this)
            {
                case ScriptDate:
                case ClrInstanceObject:
                case ScriptRegex:
                case ClosureFunction:
                case ScriptType:
                case ClrMethodBinding:
                case BondingFunction:
                    return this;
                case ScriptArray array:
                    {
                        var newArray = ScriptArray.CreateWithCapacity(array.Length);
                        for (int i = 0; i < array.Length; i++)
                        {
                            newArray.SetElement(i, ScriptDatum.Clone(array.GetElement(i), true));
                        }
                        // Optimization: Ensure custom properties on the array object are also cloned
                        array.CopyProperties(newArray, true);
                        return newArray;
                    }
                default:
                    return DeepCloneObject();
            }
        }

        private ScriptObject DeepCloneObject()
        {
            var target = new ScriptObject(Prototype);

            // Optimization: Share the HiddenClass and pre-allocate the value array
            target.hiddenClass = this.hiddenClass;
            target.propertyValues = new PropertyDescriptor[this.propertyValues.Length];

            foreach (var item in hiddenClass.Properties)
            {
                var meta = item.Meta;
                var property = propertyValues[meta.Slot];

                if (!property.IsDefined) continue;

                // Recursively clone the value
                var newValue = ScriptDatum.Null;
                if (property.IsDefined)
                {
                    newValue = ScriptDatum.Clone(property.Datum, true);
                }

                target.propertyValues[meta.Slot] = new PropertyDescriptor(property.Getter, property.Setter, newValue);
            }
            return target;
        }

    }
}
