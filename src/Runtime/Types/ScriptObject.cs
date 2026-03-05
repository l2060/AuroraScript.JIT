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

        internal PropertyDescriptor[] propertyValues = Array.Empty<PropertyDescriptor>();

        internal HiddenClass hiddenClass = HiddenClass.Root;

        private ObjectFlags flags = ObjectFlags.None;
        /// <summary>
        /// Gets a value indicating whether the object is frozen (read-only).
        /// </summary>
        public bool IsFrozen => (flags & ObjectFlags.Frozen) == ObjectFlags.Frozen;

        /// <summary>
        /// Gets a value indicating whether the object is immutable.
        /// </summary>
        public bool Immutable => (flags & ObjectFlags.Immutable) == ObjectFlags.Immutable;

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
        /// Invokes the object. Built-in objects throw an exception unless overridden (e.g., for functions).
        /// </summary>
        /// <param name="ctx">The script context.</param>
        /// <param name="args">The arguments for invocation.</param>
        /// <returns>The result of the invocation.</returns>
        /// <exception cref="AuroraRuntimeException">Thrown if the object is not a function.</exception>
        internal virtual ScriptDatum Invoke(ScriptContext ctx, params ScriptDatum[] args)
        {
            throw new AuroraRuntimeException("Object cannot be called.");
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
            return InternalGetProperty(key);
        }

        /// <summary>
        /// Internal implementation of property assignment.
        /// </summary>
        internal virtual void SetPropertyValue(ScriptContext ctx, string key, ScriptObject value)
        {
            InternalDefine(key, value);
        }

        /// <summary>
        /// Resolves a property by searching this object and its prototype chain.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <returns>The <see cref="PropertyDescriptor"/> if found; otherwise, null.</returns>
        internal PropertyDescriptor _resolveProperty(string key)
        {
            if (hiddenClass.TryGet(key, out var meta))
            {
                return propertyValues[meta.Slot];
            }
            if (prototype != null)
            {
                return prototype._resolveProperty(key);
            }
            return null;
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
        /// Directly updates or defines a property, potentially bypassing some safety checks.
        /// </summary>
        internal void Patch(string key, ScriptObject value, bool writeable = true, bool enumerable = true)
        {
            InternalDefine(key, value, writeable, enumerable, true);
        }

        internal void InternalDefine(string key, ScriptObject value, bool writeable = true, bool enumerable = true, bool force = false)
        {
            if (Immutable) return;
            if (IsFrozen) ThrowHelper.ThrowFrozen();
            if (hiddenClass.TryGet(key, out var meta))
            {
                if (!force && !meta.Writable) ThrowHelper.ThrowDisableWritable();
            }
            hiddenClass = hiddenClass.AddProperty(key, writeable, enumerable, false, out meta);
            //
            var values = propertyValues;
            if (meta.Slot >= values.Length) Resize();
            propertyValues[meta.Slot] = new PropertyDescriptor(null, null, value);
        }







        private void Resize()
        {
            var cap = Math.Max(propertyValues.Length, 2) * 2;
            Array.Resize(ref this.propertyValues, cap);
        }



        private ScriptObject InternalGetProperty(string key)
        {
            var property = _resolveProperty(key);
            if (property != null)
            {
                var value = property.Value;
                if (value is BondingGetter getter)
                {
                    ScriptDatum datum = default;
                    getter.Invoke(this, ref datum);
                    return ScriptDatum.ToObject(datum);
                }
                if (value is BondingFunction clrFunc)
                {
                    return clrFunc.Bind(this);
                }
                return value;
            }
            return ScriptObject.Null;
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

            var result = new List<ScriptDatum>();
            var current = this;
            while (current != null)
            {
                if (!current.Immutable)
                {
                    // TODO 检查
                    var hc = current.hiddenClass;
                    var propertyCount = hc.PropertyCount;
                    foreach (var item in hc._properties)
                    {
                        if (item.Value.Enumerable)
                        {
                            result.Add(ScriptDatum.FromString(item.Key));
                        }
                    }
                }
                current = current.prototype;
            }
            return new ScriptEnumerator(result);
        }

        /// <summary>
        /// Retrieves a list of all enumerable property keys from this object and its prototypes.
        /// </summary>
        /// <returns>A list of property keys.</returns>
        public List<string> EnumerationKeys()
        {
            if (Immutable) return new List<string>();
            var list = new List<string>(8);
            foreach (var item in hiddenClass._properties)
            {
                if (item.Value.Enumerable)
                {
                    list.Add(item.Key);
                }
            }
            if (prototype != null)
            {
                var result = prototype.EnumerationKeys();
                if (result.Count > 0) list.AddRange(result);
            }
            return list;
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
        public void CopyProperties(ScriptObject target, bool force = false)
        {
            if (Immutable) return;
            foreach (var item in hiddenClass._properties)
            {
                var key = item.Key;
                var meta = item.Value;
                var property = propertyValues[meta.Slot];
                if (target.hiddenClass.TryGet(key, out var targetMeta))
                {
                    if (targetMeta.Writable || force)
                    {
                        // We use InternalDefine to handle the value and basic flags, 
                        // but we also need to copy the getter/setter if they exist.
                        target.InternalDefine(key, property.Value, meta.Writable, meta.Enumerable, force);
                        var targetProperty = target.propertyValues[targetMeta.Slot];
                        targetProperty.Getter = property.Getter;
                        targetProperty.Setter = property.Setter;
                    }
                }
                else
                {
                    target.InternalDefine(key, property.Value, meta.Writable, meta.Enumerable, force);
                    if (target.hiddenClass.TryGet(key, out targetMeta))
                    {
                        var targetProperty = target.propertyValues[targetMeta.Slot];
                        targetProperty.Getter = property.Getter;
                        targetProperty.Setter = property.Setter;
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
                        var newArray = new ScriptArray(array.Length);
                        for (int i = 0; i < array.Length; i++)
                        {
                            newArray.SetElement(i, ScriptDatum.Clone(array.GetElement(i), true));
                        }
                        return newArray;
                    }
                default:
                    return DeepCloneObject();
            }
        }

        private ScriptObject DeepCloneObject()
        {
            var target = new ScriptObject(Prototype);
            foreach (var item in hiddenClass._properties)
            {
                var key = item.Key;
                var meta = item.Value;
                var property = propertyValues[meta.Slot];

                // Recursively clone the value
                var newObject = ScriptObject.Null;
                if (property.Value != null)
                {
                    newObject = property.Value.DeepClone();
                }
                target.InternalDefine(key, newObject, meta.Writable, meta.Enumerable, true);

                // Copy accessors if they exist
                if (target.hiddenClass.TryGet(key, out var targetMeta))
                {
                    var targetProp = target.propertyValues[targetMeta.Slot];
                    targetProp.Getter = property.Getter;
                    targetProp.Setter = property.Setter;
                }
            }
            return target;
        }

    }
}
