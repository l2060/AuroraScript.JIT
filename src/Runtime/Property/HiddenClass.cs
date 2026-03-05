using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AuroraScript.Runtime.Property
{
    internal enum PropertyFlags : ushort
    {
        None = 0,
        Writable = 1 << 0,     // 0b001
        Enumerable = 1 << 1,   // 0b010
        Configurable = 1 << 2, // 0b100
        FullAccess = Writable | Enumerable | Configurable

    }


    internal sealed class HiddenClass
    {
        internal readonly ImmutableDictionary<string, PropertyMeta> _properties;
        private readonly Dictionary<TransitionKey, WeakReference<HiddenClass>> _addTransitions;
        private readonly Dictionary<TransitionKey, WeakReference<HiddenClass>> _delTransitions;


        public int PropertyCount => _properties.Count;

        public static readonly HiddenClass Root = new();

        private HiddenClass()
        {
            _properties = ImmutableDictionary<string, PropertyMeta>.Empty;
            _addTransitions = new(4);
            _delTransitions = new(4);
        }

        private HiddenClass(ImmutableDictionary<string, PropertyMeta> props)
        {
            _properties = props;
            _addTransitions = new(4);
            _delTransitions = new(4);
        }


        public HiddenClass RemoveProperty(string name)
        {
            if (!_properties.ContainsKey(name)) return this;

            var key = new TransitionKey(name, PropertyFlags.None);
            if (_delTransitions.TryGetValue(key, out var existing) && existing.TryGetTarget(out var existingShape))
            {
                return existingShape;
            }

            var newProps = _properties.Remove(name);
            var newShape = new HiddenClass(newProps);
            _delTransitions[key] = new WeakReference<HiddenClass>(newShape);

            return newShape;
        }


        public bool TryGet(string name, out PropertyMeta meta) => _properties.TryGetValue(name, out meta!);

        public HiddenClass AddProperty(string name, bool writable, bool enumerable, bool configurable, out PropertyMeta meta)
        {
            PropertyFlags flags = (writable ? PropertyFlags.Writable : 0) | (enumerable ? PropertyFlags.Enumerable : 0) | (configurable ? PropertyFlags.Configurable : 0);

            // Check if property already exists with the same flags
            if (_properties.TryGetValue(name, out var oldMeta) && oldMeta.Flags == flags)
            {
                meta = oldMeta;
                return this;
            }

            var key = new TransitionKey(name, flags);
            if (_addTransitions.TryGetValue(key, out var existing) && existing.TryGetTarget(out var hiddenClass))
            {
                meta = hiddenClass._properties[name];
                return hiddenClass;
            }

            // Reuse slot if redefining, otherwise use next available slot
            ushort slot = oldMeta.Slot;
            if (!_properties.ContainsKey(name))
            {
                slot = (ushort)_properties.Count;
            }
            meta = new PropertyMeta(slot, flags);
            // Use SetItem to handle both add and update (avoiding exceptions)
            var newProps = _properties.SetItem(name, meta);
            var newShape = new HiddenClass(newProps);
            _addTransitions[key] = new WeakReference<HiddenClass>(newShape);

            return newShape;
        }

        public IEnumerable<string> Keys => _properties.Keys;
    }
}
