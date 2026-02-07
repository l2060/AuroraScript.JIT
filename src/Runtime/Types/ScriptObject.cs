using AuroraScript.Runtime.Util;
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
        internal ScriptObject _prototype;
        internal Dictionary<string, ObjectProperty> _properties;
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
            _prototype = prototype;
            flags = ObjectFlags.Immutable;
        }

        /// <summary>
        /// Initializes an internal object with a specific prototype.
        /// </summary>
        /// <param name="prototype">The prototype object.</param>
        internal ScriptObject(ScriptObject prototype)
        {
            _prototype = prototype;
            _properties = new Dictionary<string, ObjectProperty>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptObject"/> class with the default object prototype.
        /// </summary>
        public ScriptObject()
        {
            _prototype = Prototypes.ObjectPrototype;
            _properties = new Dictionary<string, ObjectProperty>();
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
        /// <returns>The <see cref="ObjectProperty"/> if found; otherwise, null.</returns>
        internal ObjectProperty _resolveProperty(string key)
        {
            if (_properties != null && _properties.TryGetValue(key, out var value))
            {
                return value;
            }
            if (_prototype != null)
            {
                return _prototype._resolveProperty(key);
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
            RuntimeHelper.CopyProperties(scriptObject, this, force);
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

        private void InternalDefine(string key, ScriptObject value, bool writeable = true, bool enumerable = true, bool force = false)
        {
            if (Immutable) return;
            if (IsFrozen) ThrowHelper.ThrowFrozen();
            if (!_properties.TryGetValue(key, out var existValue))
            {
                existValue = new ObjectProperty(key, writeable, enumerable);
                _properties[key] = existValue;
            }
            else
            {
                if (!force && !existValue.Writable) ThrowHelper.ThrowDisableWritable();
            }
            existValue.Value = value;
            existValue.Writable = writeable;
            existValue.Enumerable = enumerable;
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
            if (_properties != null && _properties.TryGetValue(key, out var value))
            {
                if (!value.Writable) ThrowHelper.ThrowDisableWritable();
                _properties.Remove(key);
                return true;
            }
            if (_prototype != null)
            {
                return _prototype.DeletePropertyValue(key);
            }
            return false;
        }

        /// <summary>
        /// Clears all properties from the object if it is not immutable or frozen.
        /// </summary>
        internal void ClearProperties()
        {
            if (Immutable || IsFrozen) return;
            _properties?.Clear();
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
                    foreach (var item in current._properties)
                    {
                        if (item.Value.Enumerable)
                        {
                            result.Add(ScriptDatum.FromString(item.Value.Key));
                        }
                    }
                }
                current = current._prototype;
            }
            return new ScriptEnumerator(result.ToArray());
        }

        /// <summary>
        /// Retrieves a list of all enumerable property keys from this object and its prototypes.
        /// </summary>
        /// <returns>A list of property keys.</returns>
        public List<string> EnumerationKeys()
        {
            if (Immutable) return new List<string>();
            var list = new List<string>(8);
            if (_properties != null)
            {
                foreach (var item in _properties)
                {
                    if (item.Value.Enumerable)
                    {
                        list.Add(item.Key);
                    }
                }
            }
            if (_prototype != null)
            {
                var result = _prototype.EnumerationKeys();
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
    }
}
