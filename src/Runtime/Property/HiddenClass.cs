using System;
using System.Collections.Generic;
using AuroraScript.Runtime.Types;

namespace AuroraScript.Runtime.Property
{
    internal enum PropertyFlags : ushort
    {
        None = 0,
        Writable = 1 << 0,
        Enumerable = 1 << 1,
        Configurable = 1 << 2,
        FullAccess = Writable | Enumerable | Configurable
    }

    internal readonly struct HiddenProperty
    {
        public readonly string Name;
        public readonly PropertyMeta Meta;
        public readonly ScriptDatum Key;

        public HiddenProperty(string name, PropertyMeta meta)
        {
            Name = name;
            Meta = meta;
            Key = ScriptDatum.FromString(name);
        }
    }

    internal sealed class HiddenClass
    {
        private readonly HiddenProperty[] _properties;
        private Dictionary<TransitionKey, HiddenClass> _addTransitions;
        private Dictionary<TransitionKey, HiddenClass> _delTransitions;

        internal readonly ushort _maxSlot;
        private readonly HiddenClass _parent;
        private readonly string _transitionName;
        private readonly bool _transitionAddedProperty;
        private readonly ScriptDatum[] _enumerableKeys;
        public int PropertyCount => _properties.Length;
        public int EnumerableCount => _enumerableKeys.Length;
        public ScriptDatum[] EnumerableKeys => _enumerableKeys;
        public ReadOnlySpan<HiddenProperty> Properties => _properties;

        public static readonly HiddenClass Root = new();

        private HiddenClass()
        {
            _properties = Array.Empty<HiddenProperty>();
            _maxSlot = 0;
            _parent = null!;
            _transitionName = null!;
            _transitionAddedProperty = false;
            _enumerableKeys = Array.Empty<ScriptDatum>();
        }

        private HiddenClass(HiddenProperty[] properties, ushort maxSlot, HiddenClass parent, string transitionName, bool transitionAddedProperty)
        {
            _properties = properties;
            _maxSlot = maxSlot;
            _parent = parent;
            _transitionName = transitionName;
            _transitionAddedProperty = transitionAddedProperty;
            _enumerableKeys = CreateEnumerableKeys(properties);
        }

        public HiddenClass RemoveProperty(string name)
        {
            var index = IndexOf(name);
            if (index < 0) return this;

            if (_transitionAddedProperty && name == _transitionName && _parent != null) return _parent;

            var key = new TransitionKey(name, PropertyFlags.None);
            if (_delTransitions != null && _delTransitions.TryGetValue(key, out var existingShape))
            {
                return existingShape;
            }

            var newProps = new HiddenProperty[_properties.Length - 1];
            if (index > 0) Array.Copy(_properties, 0, newProps, 0, index);
            var afterCount = _properties.Length - index - 1;
            if (afterCount > 0) Array.Copy(_properties, index + 1, newProps, index, afterCount);

            var newShape = new HiddenClass(newProps, _maxSlot, null!, null!, false);
            _delTransitions ??= new(2);
            _delTransitions[key] = newShape;
            return newShape;
        }

        public bool TryGet(string name, out PropertyMeta meta)
        {
            var index = IndexOf(name);
            if (index >= 0)
            {
                meta = _properties[index].Meta;
                return true;
            }

            meta = default;
            return false;
        }

        public HiddenClass AddProperty(string name, bool writable, bool enumerable, bool configurable, out PropertyMeta meta)
        {
            var flags = (writable ? PropertyFlags.Writable : 0)
                | (enumerable ? PropertyFlags.Enumerable : 0)
                | (configurable ? PropertyFlags.Configurable : 0);

            var index = IndexOf(name);
            if (index >= 0)
            {
                var oldMeta = _properties[index].Meta;
                if (oldMeta.Flags == flags)
                {
                    meta = oldMeta;
                    return this;
                }
            }

            var key = new TransitionKey(name, flags);
            if (_addTransitions != null && _addTransitions.TryGetValue(key, out var hiddenClass))
            {
                hiddenClass.TryGet(name, out meta);
                return hiddenClass;
            }

            ushort slot;
            ushort nextMaxSlot;
            HiddenProperty[] newProps;

            if (index >= 0)
            {
                slot = _properties[index].Meta.Slot;
                nextMaxSlot = _maxSlot;
                meta = new PropertyMeta(slot, flags);
                newProps = (HiddenProperty[])_properties.Clone();
                newProps[index] = new HiddenProperty(name, meta);
            }
            else
            {
                slot = _maxSlot;
                nextMaxSlot = checked((ushort)(_maxSlot + 1));
                meta = new PropertyMeta(slot, flags);
                newProps = new HiddenProperty[_properties.Length + 1];
                if (_properties.Length > 0) Array.Copy(_properties, newProps, _properties.Length);
                newProps[^1] = new HiddenProperty(name, meta);
            }

            var newShape = new HiddenClass(newProps, nextMaxSlot, this, name, index < 0);
            _addTransitions ??= new(4);
            _addTransitions[key] = newShape;
            return newShape;
        }

        public static HiddenClass GetLiteralShape(string name0, string name1, string name2)
        {
            var shape = Root;
            shape = shape.AddProperty(name0, writable: true, enumerable: true, configurable: false, out _);
            shape = shape.AddProperty(name1, writable: true, enumerable: true, configurable: false, out _);
            shape = shape.AddProperty(name2, writable: true, enumerable: true, configurable: false, out _);
            return shape;
        }

        public static HiddenClass GetLiteralShape(params string[] names)
        {
            var shape = Root;
            foreach (var name in names)
            {
                shape = shape.AddProperty(name, writable: true, enumerable: true, configurable: false, out _);
            }
            return shape;
        }

        private int IndexOf(string name)
        {
            for (int i = _properties.Length - 1; i >= 0; i--)
            {
                if (StringComparer.Ordinal.Equals(_properties[i].Name, name)) return i;
            }
            return -1;
        }

        private static ScriptDatum[] CreateEnumerableKeys(HiddenProperty[] properties)
        {
            var count = 0;
            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i].Meta.Enumerable) count++;
            }
            if (count == 0) return Array.Empty<ScriptDatum>();

            var keys = new ScriptDatum[count];
            var index = 0;
            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i].Meta.Enumerable)
                {
                    keys[index++] = properties[i].Key;
                }
            }
            return keys;
        }
    }
}
