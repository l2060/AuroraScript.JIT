using AuroraScript.Runtime.Property;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AuroraScript.Runtime.Debugging
{
    internal sealed class ScriptObjectDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ScriptObject value;

        public ScriptObjectDebugView(ScriptObject value)
        {
            this.value = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ScriptDebugProperty[] Properties => ScriptDebugView.GetProperties(value);
    }

    internal sealed class ScriptDatumDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ScriptDatum value;

        public ScriptDatumDebugView(ScriptDatum value)
        {
            this.value = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ScriptDebugProperty[] Properties => ScriptDebugView.GetProperties(value);
    }

    [DebuggerDisplay("{DisplayValue,nq}", Name = "{Name,nq}", Type = "{DisplayType,nq}")]
    internal sealed class ScriptDebugProperty
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ScriptDatum value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string displayType;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string displayValue;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ScriptDebugProperty[] properties;

        public ScriptDebugProperty(string name, ScriptDatum value)
        {
            Name = name;
            this.value = value;
        }

        public ScriptDebugProperty(string name, string displayValue, string displayType = null, ScriptDebugProperty[] properties = null)
        {
            Name = name;
            this.displayValue = displayValue ?? string.Empty;
            this.displayType = displayType ?? string.Empty;
            this.properties = properties;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string Name { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string DisplayType => displayType ?? ScriptDebugView.GetTypeName(value);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string DisplayValue => displayValue ?? ScriptDebugView.FormatValue(value);

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ScriptDebugProperty[] Properties => properties ?? ScriptDebugView.GetProperties(value);
    }

    internal static class ScriptDebugView
    {
        private const int MaxArrayPreviewItems = 32;

        public static ScriptDebugProperty[] GetProperties(ScriptDatum datum)
        {
            return TryGetObject(datum, out var obj)
                ? GetProperties(obj)
                : Array.Empty<ScriptDebugProperty>();
        }

        public static ScriptDebugProperty[] GetProperties(ScriptObject obj)
        {
            if (obj == null || ReferenceEquals(obj, ScriptObject.Null))
            {
                return Array.Empty<ScriptDebugProperty>();
            }

            var properties = obj switch
            {
                ScriptArray array => GetArrayProperties(array),
                ScriptHashMap hashMap => GetHashMapProperties(hashMap),
                _ => GetObjectProperties(obj)
            };

            return properties.Length == 0
                ? new[] { new ScriptDebugProperty("(empty)", string.Empty) }
                : properties;
        }

        public static string GetTypeName(ScriptDatum datum)
        {
            return TryGetObject(datum, out var obj)
                ? GetTypeName(obj)
                : ScriptDatum.GetTypeName(datum);
        }

        public static string GetTypeName(ScriptObject obj)
        {
            if (obj == null || ReferenceEquals(obj, ScriptObject.Null))
            {
                return "null";
            }

            return obj switch
            {
                ScriptArray => "array",
                StringValue => "string",
                NumberValue => "number",
                BooleanValue => "boolean",
                ScriptDate => "date",
                ScriptRegex => "regex",
                ClosureFunction => "function",
                ScriptType => "type",
                ScriptError => "error",
                _ => obj.GetType() == typeof(ScriptObject) ? "object" : obj.GetType().Name
            };
        }

        public static string FormatValue(ScriptDatum datum)
        {
            return datum.Kind switch
            {
                ValueKind.Null => "null",
                ValueKind.Boolean => datum.Boolean ? "true" : "false",
                ValueKind.Number => datum.Number.ToString(),
                ValueKind.String => Quote(datum.String?.Value ?? string.Empty),
                ValueKind.Array => FormatArray((ScriptArray)datum.Object),
                _ => FormatObject(datum.Object)
            };
        }

        public static string FormatValue(ScriptObject obj)
        {
            return obj switch
            {
                null => "null",
                NullValue => "null",
                BooleanValue boolean => boolean.Value ? "true" : "false",
                NumberValue number => number.DoubleValue.ToString(),
                StringValue text => Quote(text.Value ?? string.Empty),
                ScriptArray array => FormatArray(array),
                _ => FormatObject(obj)
            };
        }

        private static string FormatObject(ScriptObject obj)
        {
            if (obj == null || ReferenceEquals(obj, ScriptObject.Null))
            {
                return "null";
            }

            return GetTypeName(obj);
        }

        private static string FormatArray(ScriptArray array)
        {
            if (array == null)
            {
                return "null";
            }

            var length = array.Length;
            if (length == 0)
            {
                return "[]";
            }

            var builder = new StringBuilder();
            builder.Append('[');
            var previewCount = Math.Min(length, MaxArrayPreviewItems);
            for (var i = 0; i < previewCount; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(FormatArrayElement(array.GetElement(i)));
            }

            if (previewCount < length)
            {
                builder.Append(", ...");
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static string FormatArrayElement(ScriptDatum datum)
        {
            return datum.Kind == ValueKind.Array || TryGetObject(datum, out _)
                ? FormatObject(datum.Object)
                : FormatValue(datum);
        }

        private static ScriptDebugProperty[] GetArrayProperties(ScriptArray array)
        {
            var objectProperties = GetObjectProperties(array);
            var result = new ScriptDebugProperty[array.Length + objectProperties.Length];
            for (var i = 0; i < array.Length; i++)
            {
                result[i] = new ScriptDebugProperty("[" + i + "]", array.GetElement(i));
            }

            Array.Copy(objectProperties, 0, result, array.Length, objectProperties.Length);
            return result;
        }

        private static ScriptDebugProperty[] GetHashMapProperties(ScriptHashMap hashMap)
        {
            var objectProperties = GetObjectProperties(hashMap);
            var result = new ScriptDebugProperty[hashMap.DebugEntries.Count + objectProperties.Length];
            var index = 0;
            foreach (var item in hashMap.DebugEntries)
            {
                result[index++] = new ScriptDebugProperty(FormatKey(item.Key), item.Value);
            }

            Array.Copy(objectProperties, 0, result, index, objectProperties.Length);
            return result;
        }

        private static ScriptDebugProperty[] GetObjectProperties(ScriptObject obj)
        {
            var count = CountDisplayProperties(obj);
            if (count == 0)
            {
                return Array.Empty<ScriptDebugProperty>();
            }

            var result = new ScriptDebugProperty[count];
            FillDisplayProperties(obj, result);
            return result;
        }

        private static int CountDisplayProperties(ScriptObject obj)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var count = 0;
            var current = obj;
            var includeNonEnumerable = true;
            while (current != null)
            {
                foreach (var property in current.hiddenClass.Properties)
                {
                    if ((includeNonEnumerable || property.Meta.Enumerable) &&
                        seen.Add(property.Name) &&
                        TryGetOwnPropertyDatum(current, property, out _))
                    {
                        count++;
                    }
                }

                current = current.Prototype;
                includeNonEnumerable = false;
            }

            return count;
        }

        private static void FillDisplayProperties(ScriptObject obj, ScriptDebugProperty[] result)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var offset = 0;
            var current = obj;
            var includeNonEnumerable = true;
            while (current != null)
            {
                foreach (var property in current.hiddenClass.Properties)
                {
                    if ((includeNonEnumerable || property.Meta.Enumerable) &&
                        seen.Add(property.Name) &&
                        TryGetOwnPropertyDatum(current, property, out var datum))
                    {
                        result[offset++] = new ScriptDebugProperty(property.Name, datum);
                    }
                }

                current = current.Prototype;
                includeNonEnumerable = false;
            }
        }

        private static bool TryGetOwnPropertyDatum(ScriptObject obj, HiddenProperty property, out ScriptDatum datum)
        {
            var slot = property.Meta.Slot;
            if ((uint)slot >= (uint)obj.propertyValues.Length)
            {
                datum = default;
                return false;
            }

            var descriptor = obj.propertyValues[slot];
            if (!descriptor.IsDefined)
            {
                datum = default;
                return false;
            }

            datum = descriptor.IsAccessor
                ? ScriptDatum.FromString("<accessor>")
                : descriptor.Datum;
            return true;
        }

        private static bool TryGetObject(ScriptDatum datum, out ScriptObject obj)
        {
            if ((datum.Kind & ValueKind.Object) == ValueKind.Object && datum.Object != null)
            {
                obj = datum.Object;
                return true;
            }

            obj = null;
            return false;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
        }

        private static string FormatKey(ScriptDatum key)
        {
            return key.Kind == ValueKind.String
                ? key.String?.Value ?? string.Empty
                : FormatValue(key);
        }
    }
}
