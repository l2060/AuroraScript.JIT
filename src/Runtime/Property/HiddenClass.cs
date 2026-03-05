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

        internal readonly ushort _maxSlot;
        private readonly HiddenClass _parent;
        private readonly string _transitionName;


        public int PropertyCount => _properties.Count;

        public static readonly HiddenClass Root = new();

        private HiddenClass()
        {
            _properties = ImmutableDictionary<string, PropertyMeta>.Empty;
            _addTransitions = new(4);
            _delTransitions = new(4);
            _maxSlot = 0;
            _parent = null!;
            _transitionName = null!;
        }

        private HiddenClass(ImmutableDictionary<string, PropertyMeta> props, ushort maxSlot, HiddenClass parent, string transitionName)
        {
            _properties = props;
            _addTransitions = new(4);
            _delTransitions = new(4);
            _maxSlot = maxSlot;
            _parent = parent;
            _transitionName = transitionName;
        }


        public HiddenClass RemoveProperty(string name)
        {
            if (!_properties.ContainsKey(name)) return this;

            // Optimization: If this is the most recently added property, we can go back to the parent.
            if (name == _transitionName) return _parent;

            var key = new TransitionKey(name, PropertyFlags.None);
            if (_delTransitions.TryGetValue(key, out var existing) && existing.TryGetTarget(out var existingShape))
            {
                return existingShape;
            }

            var newProps = _properties.Remove(name);
            var newShape = new HiddenClass(newProps, _maxSlot, null!, null!);
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

            // Reuse slot if redefining, otherwise use next available slot tracked by _maxSlot
            ushort slot = _maxSlot;
            ushort nextMaxSlot = (ushort)(_maxSlot + 1);

            if (oldMeta.Slot != 0 || _properties.ContainsKey(name))
            {
                slot = oldMeta.Slot;
                nextMaxSlot = _maxSlot;
            }

            meta = new PropertyMeta(slot, flags);
            // Use SetItem to handle both add and update (avoiding exceptions)
            var newProps = _properties.SetItem(name, meta);
            var newShape = new HiddenClass(newProps, nextMaxSlot, this, name);
            _addTransitions[key] = new WeakReference<HiddenClass>(newShape);

            return newShape;
        }

        public IEnumerable<string> Keys => _properties.Keys;
    }
}
