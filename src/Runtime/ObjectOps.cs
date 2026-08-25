using AuroraScript.Runtime.Property;
using AuroraScript.Runtime.Types;
using System;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Small dynamic boundary for generated property, element, and collection code.
    /// Primitive expressions stay in native CIL and enter here only for object semantics.
    /// </summary>
    internal static class ObjectOps
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetProperty(ScriptDatum receiver, string name)
        {
            return GetProperty(receiver, null, name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetProperty(ScriptDatum receiver, ScriptContext context, string name)
        {
            if (StringComparer.Ordinal.Equals(name, "length"))
            {
                if (receiver.Kind == ValueKind.String)
                {
                    return ScriptDatum.FromNumber(receiver.StringText?.Length ?? 0);
                }
                if (receiver.Kind == ValueKind.Array && receiver.Object is ScriptArray array)
                {
                    return ScriptDatum.FromNumber(array.Length);
                }
                if (receiver.Reference is ScriptPackedArray packedArray)
                {
                    return ScriptDatum.FromNumber(packedArray.Length);
                }
            }

            return ScriptDatum.ToObject(receiver).GetPropertyDatum(context, name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum SetProperty(
            ScriptDatum receiver,
            ScriptContext context,
            string name,
            ScriptDatum value)
        {
            ScriptDatum.ToObject(receiver).SetPropertyDatum(context, name, value);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetElement(ScriptDatum receiver, ScriptDatum index)
        {
            if (receiver.Kind == ValueKind.Array && receiver.Object is ScriptArray array &&
                ScriptDatum.TryToInteger(in index, out var numericIndex))
            {
                var value = default(ScriptDatum);
                array.GetElement((int)numericIndex, ref value);
                return value;
            }

            if (receiver.Reference is ScriptPackedArray packedArray &&
                ScriptDatum.TryToInteger(in index, out numericIndex))
            {
                return packedArray.GetElementDatum((int)numericIndex);
            }

            var instance = ScriptDatum.ToObject(receiver);
            if (instance is ScriptArray fallbackArray && ScriptDatum.TryToInteger(in index, out numericIndex))
            {
                var value = default(ScriptDatum);
                fallbackArray.GetElement((int)numericIndex, ref value);
                return value;
            }
            return instance.GetPropertyDatum(null, ScriptDatum.ToString(index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetElementNumber(ScriptDatum receiver, double index)
        {
            if (receiver.Kind == ValueKind.Array && receiver.Object is ScriptArray array)
            {
                var value = default(ScriptDatum);
                array.GetElement((int)index, ref value);
                return value;
            }
            if (receiver.Reference is ScriptPackedArray packedArray)
            {
                return packedArray.GetElementDatum((int)index);
            }
            return GetElement(receiver, ScriptDatum.FromNumber(index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum SetElement(ScriptDatum receiver, ScriptDatum index, ScriptDatum value)
        {
            var instance = ScriptDatum.ToObject(receiver);
            if (instance is ScriptArray array && ScriptDatum.TryToInteger(in index, out var numericIndex))
            {
                array.SetElement((int)numericIndex, in value);
            }
            else if (instance is ScriptPackedArray packedArray &&
                ScriptDatum.TryToInteger(in index, out numericIndex))
            {
                packedArray.SetElementDatum((int)numericIndex, value);
            }
            else
            {
                instance.SetPropertyDatum(null, ScriptDatum.ToString(index), value);
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum SetElementNumber(ScriptDatum receiver, double index, ScriptDatum value)
        {
            if (receiver.Kind == ValueKind.Array && receiver.Object is ScriptArray array)
            {
                array.SetElement((int)index, in value);
                return value;
            }
            if (receiver.Reference is ScriptPackedArray packedArray)
            {
                packedArray.SetElementDatum((int)index, value);
                return value;
            }
            return SetElement(receiver, ScriptDatum.FromNumber(index), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CompoundAddElement(
            ScriptDatum receiver,
            ScriptDatum index,
            ScriptDatum value)
        {
            var result = ValueOps.Add(GetElement(receiver, index), value);
            SetElement(receiver, index, result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CompoundAddElementNumber(
            ScriptDatum receiver,
            double index,
            ScriptDatum value)
        {
            var result = ValueOps.Add(GetElementNumber(receiver, index), value);
            SetElementNumber(receiver, index, result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum ChangeElement(
            ScriptDatum receiver,
            ScriptDatum index,
            double delta,
            bool postfix)
        {
            var previous = GetElement(receiver, index);
            var current = ValueOps.ChangeByOne(previous, delta);
            SetElement(receiver, index, current);
            return postfix ? previous : current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum ChangeElementNumber(
            ScriptDatum receiver,
            double index,
            double delta,
            bool postfix)
        {
            var previous = GetElementNumber(receiver, index);
            var current = ValueOps.ChangeByOne(previous, delta);
            SetElementNumber(receiver, index, current);
            return postfix ? previous : current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum ChangeProperty(
            ScriptDatum receiver,
            ScriptContext context,
            string name,
            double delta,
            bool postfix)
        {
            return ChangeProperty(ScriptDatum.ToObject(receiver), context, name, delta, postfix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum ChangeProperty(
            ScriptObject receiver,
            ScriptContext context,
            string name,
            double delta,
            bool postfix)
        {
            var previous = receiver.GetPropertyDatum(context, name);
            var current = ValueOps.ChangeByOne(previous, delta);
            receiver.SetPropertyDatum(context, name, current);
            return postfix ? previous : current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptObject CreateObject3(
            string name0,
            ScriptDatum value0,
            string name1,
            ScriptDatum value1,
            string name2,
            ScriptDatum value2)
        {
            var shape = HiddenClass.GetLiteralShape(name0, name1, name2);
            var values = new ScriptDatum[Math.Max(shape._maxSlot, (ushort)4)];
            values[0] = value0;
            values[1] = value1;
            values[2] = value2;
            return new ScriptObject(shape, values);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SpreadInto(ScriptArray target, ScriptDatum source)
        {
            if (source.Kind == ValueKind.Array && source.Object is ScriptArray array)
            {
                for (var i = 0; i < array.Length; i++) target.Push(array.GetElement(i));
                return;
            }
            if (source.Reference is ScriptPackedArray packedArray)
            {
                for (var i = 0; i < packedArray.Length; i++)
                {
                    target.Push(packedArray.GetElementDatumUnchecked(i));
                }
                return;
            }
            target.Push(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyProperties(ScriptObject target, ScriptDatum source)
        {
            target.CopyPropertysFrom(ScriptDatum.ToObject(source), force: false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Includes(ScriptDatum collection, ScriptDatum value)
        {
            if (collection.Kind == ValueKind.String)
            {
                var text = collection.StringText ?? string.Empty;
                if (value.Kind == ValueKind.String)
                {
                    var needle = value.StringText ?? string.Empty;
                    if (needle.Length > 1)
                    {
                        return text.IndexOf(needle, StringComparison.Ordinal) >= 0;
                    }
                }

                for (var i = 0; i < text.Length; i++)
                {
                    var current = ScriptDatum.FromString(StringValue.TextFromChar(text[i]));
                    if (current.Equals(value)) return true;
                }
                return false;
            }

            var instance = ScriptDatum.ToObject(collection);
            var enumerator = instance.GetEnumerator();
            while (enumerator.NextValue(out var current))
            {
                if (current.Equals(value)) return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DeleteProperty(ScriptContext context, ScriptDatum receiver, string name)
        {
            var instance = ScriptDatum.ToObject(receiver);
            if (instance is ScriptArray array && int.TryParse(name, out var index)) array.Remove(index);
            else if (instance is ScriptPackedArray && int.TryParse(name, out _))
                ScriptPackedArray.ThrowDeleteNotSupported();
            else instance.DeletePropertyValue(context, name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DeleteElement(ScriptContext context, ScriptDatum receiver, ScriptDatum index)
        {
            var instance = ScriptDatum.ToObject(receiver);
            if (instance is ScriptArray array && ScriptDatum.TryToInteger(in index, out var numericIndex))
            {
                array.Remove((int)numericIndex);
            }
            else if (instance is ScriptPackedArray && ScriptDatum.TryToInteger(in index, out _))
            {
                ScriptPackedArray.ThrowDeleteNotSupported();
            }
            else
            {
                instance.DeletePropertyValue(context, ScriptDatum.ToString(index));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetDestructureElement(ScriptDatum source, int index, int trailingCount)
        {
            var instance = ScriptDatum.ToObject(source);
            if (instance is ScriptArray array)
            {
                return array.GetElement(trailingCount > 0 ? array.Length - trailingCount : index);
            }
            if (instance is ScriptPackedArray packedArray)
            {
                return packedArray.GetElementDatum(
                    trailingCount > 0 ? packedArray.Length - trailingCount : index);
            }
            throw new AuroraRuntimeException("Array destructuring requires an array.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum SliceDestructureArray(ScriptDatum source, int start, int trailingCount)
        {
            var instance = ScriptDatum.ToObject(source);
            if (instance is ScriptArray array)
            {
                var result = default(ScriptDatum);
                array.SliceTo(start, array.Length - trailingCount, ref result);
                return result;
            }
            if (instance is ScriptPackedArray packedArray)
            {
                var end = packedArray.Length - trailingCount;
                var length = Math.Max(0, end - start);
                var result = ScriptArray.CreateWithCapacity(length);
                for (var i = start; i < end; i++)
                {
                    result.Push(packedArray.GetElementDatum(i));
                }
                return ScriptDatum.FromArray(result);
            }
            throw new AuroraRuntimeException("Array destructuring requires an array.");
        }
    }
}
