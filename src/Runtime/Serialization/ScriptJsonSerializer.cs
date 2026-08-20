using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Base class for script data serialization to and from JSON.
    /// Provides a extensible architecture for serializing complex script object graphs,
    /// with built-in support for circular reference detection and custom formatting.
    /// </summary>
    public class ScriptJsonSerializer
    {
        [ThreadStatic]
        private static ArrayBufferWriter<byte> s_bufferWriter;

        [ThreadStatic]
        private static HashSet<ScriptObject> s_visited;

        private const int MaxRetainedBufferSize = 1024 * 1024;

        /// <summary>
        /// Contextual information maintained during a serialization or deserialization operation.
        /// </summary>
        public readonly ref struct ScriptSerializationContext
        {
            /// <summary> The engine options used for configuration (e.g., recursion limits). </summary>
            public readonly EngineOptions Options;
            /// <summary> A set of objects already visited, used to detect circular references. </summary>
            public readonly HashSet<ScriptObject> Visited;

            /// <summary>
            /// Initializes a new serialization context.
            /// </summary>
            public ScriptSerializationContext(EngineOptions options, HashSet<ScriptObject> visited = null)
            {
                Options = options ?? EngineOptions.Default;
                Visited = visited;
            }
        }

        /// <summary> A singleton instance of the standard script JSON serializer. </summary>
        public static readonly ScriptJsonSerializer Default = new StandardScriptJsonSerializer();


        /// <summary>
        /// Serializes a <see cref="ScriptDatum"/> to a JSON string.
        /// </summary>
        /// <param name="datum">The script value to serialize.</param>
        /// <param name="options">Optional engine options.</param>
        /// <param name="indented">Whether to format the JSON output with indentation.</param>
        /// <returns>A JSON-formatted string.</returns>
        public String Serialize(ScriptDatum datum, EngineOptions options = null, Boolean indented = false)
        {
            var bufferWriter = RentBufferWriter();
            using var jsonWriter = new Utf8JsonWriter(bufferWriter, new JsonWriterOptions
            {
                Indented = indented,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            var visited = RentVisitedSet();
            var context = new ScriptSerializationContext(options, visited);
            try
            {
                WriteDatum(jsonWriter, in datum, context);
                jsonWriter.Flush();
                return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
            }
            finally
            {
                visited.Clear();
                if (bufferWriter.Capacity <= MaxRetainedBufferSize)
                {
                    bufferWriter.Clear();
                }
                else
                {
                    s_bufferWriter = null;
                }
            }
        }

        private static ArrayBufferWriter<byte> RentBufferWriter()
        {
            var writer = s_bufferWriter;
            if (writer == null)
            {
                writer = new ArrayBufferWriter<byte>();
                s_bufferWriter = writer;
            }
            else
            {
                writer.Clear();
            }
            return writer;
        }

        private static HashSet<ScriptObject> RentVisitedSet()
        {
            var visited = s_visited;
            if (visited == null)
            {
                visited = new HashSet<ScriptObject>(ScriptJsonSerializer.ReferenceComparer.Instance);
                s_visited = visited;
            }
            else
            {
                visited.Clear();
            }
            return visited;
        }

        /// <summary>
        /// Deserializes a JSON string into a script object.
        /// </summary>
        /// <param name="jsonText">The JSON text to parse.</param>
        /// <param name="options">Optional engine options.</param>
        /// <returns>The resulting <see cref="ScriptObject"/>.</returns>
        public ScriptObject Deserialize(String jsonText, EngineOptions options = null)
        {
            using var document = JsonDocument.Parse(jsonText);
            var context = new ScriptSerializationContext(options);
            return ReadElement(document.RootElement, context);
        }

        /// <summary>
        /// Reads a JSON element directly into a datum, avoiding transient boxed script objects for primitives.
        /// </summary>
        protected virtual ScriptDatum ReadDatum(JsonElement element, in ScriptSerializationContext context)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    return ScriptDatum.FromObject(ReadElement(element, context));
                case JsonValueKind.String:
                    return ScriptDatum.FromString(element.GetString());
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var longValue))
                    {
                        return ScriptDatum.FromNumber(longValue);
                    }
                    if (element.TryGetDouble(out var doubleValue))
                    {
                        return ScriptDatum.FromNumber(doubleValue);
                    }
                    return ScriptDatum.Null;
                case JsonValueKind.True:
                    return ScriptDatum.True;
                case JsonValueKind.False:
                    return ScriptDatum.False;
                default:
                    return ScriptDatum.Null;
            }
        }


        /// <summary>
        /// Reads a JSON element and converts it to a corresponding script object.
        /// </summary>
        /// <param name="element">The JSON element to read.</param>
        /// <param name="context">The current serialization context.</param>
        /// <returns>A <see cref="ScriptObject"/> representing the JSON value.</returns>
        protected virtual ScriptObject ReadElement(JsonElement element, in ScriptSerializationContext context)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => ReadScriptObject(element, context),
                JsonValueKind.Array => ReadScriptArray(element, context),
                JsonValueKind.String => ReadScriptString(element, context),
                JsonValueKind.Number => ReadScriptNumber(element, context),
                JsonValueKind.True => BooleanValue.True,
                JsonValueKind.False => BooleanValue.False,
                _ => ScriptObject.Null,
            };
        }

        /// <summary>
        /// Deserializes a JSON object into a script object.
        /// </summary>
        protected virtual ScriptObject ReadScriptObject(JsonElement element, in ScriptSerializationContext context)
        {
            var obj = new ScriptObject();
            foreach (var property in element.EnumerateObject())
            {
                obj.InternalDefine(property.Name, ReadDatum(property.Value, context));
            }
            return obj;
        }

        /// <summary>
        /// Deserializes a JSON array into a script array.
        /// </summary>
        protected virtual ScriptObject ReadScriptArray(JsonElement element, in ScriptSerializationContext context)
        {
            var array = new ScriptArray(element.GetArrayLength());
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                array.SetElement(index++, ReadDatum(item, context));
            }
            return array;
        }

        /// <summary>
        /// Deserializes a JSON number into a numeric script object.
        /// Supports both integers and floating-point numbers.
        /// </summary>
        protected virtual ScriptObject ReadScriptNumber(JsonElement element, in ScriptSerializationContext context)
        {
            if (element.TryGetInt64(out var longValue))
            {
                return NumberValue.Of(longValue);
            }
            if (element.TryGetDouble(out var doubleValue))
            {
                return NumberValue.Of(doubleValue);
            }
            return ScriptObject.Null;
        }

        /// <summary>
        /// Deserializes a JSON string into a script string object.
        /// </summary>
        protected virtual ScriptObject ReadScriptString(JsonElement element, in ScriptSerializationContext context)
        {
            return StringValue.Of(element.GetString());
        }


        /// <summary>
        /// Writes a <see cref="ScriptDatum"/> to the JSON stream.
        /// This is the main entry point for recursive serialization.
        /// </summary>
        protected virtual void WriteDatum(Utf8JsonWriter writer, in ScriptDatum datum, in ScriptSerializationContext context)
        {
            switch (datum.Kind)
            {
                case ValueKind.Null:
                    WriteNull(writer, context);
                    return;
                case ValueKind.Boolean:
                    WriteBooleanValue(writer, datum.Boolean, context);
                    return;
                case ValueKind.Number:
                    WriteNumberValue(writer, datum.Number, context);
                    return;
                case ValueKind.String:
                case ValueKind.Date:
                case ValueKind.Regex:
                case ValueKind.Array:
                case ValueKind.Object:
                case ValueKind.Function:
                case ValueKind.Type:
                case ValueKind.ClrFunction:
                case ValueKind.ClrBonding:
                case ValueKind.Error:
                    WriteScriptObject(writer, datum.Object, context);
                    return;
                default:
                    writer.WriteStringValue(ScriptDatum.ToString(datum));
                    return;
            }
        }

        /// <summary>
        /// Writes a script object (complex type) to the JSON stream.
        /// Uses a switch to delegate to specific writers for known types (Module, Global, etc.).
        /// Handles circular references if a visited set is provided.
        /// </summary>
        protected virtual void WriteScriptObject(Utf8JsonWriter writer, ScriptObject value, in ScriptSerializationContext context)
        {
            if (value == null || value == ScriptObject.Null)
            {
                WriteNull(writer, context);
                return;
            }

            switch (value)
            {
                case ScriptModule module:
                    WriteModule(writer, module, context);
                    break;
                case ScriptGlobal global:
                    WriteGlobal(writer, global, context);
                    break;
                case StringValue stringValue:
                    WriteString(writer, stringValue, context);
                    break;
                case NumberValue numberValue:
                    WriteNumber(writer, numberValue, context);
                    break;
                case BooleanValue boolValue:
                    WriteBoolean(writer, boolValue, context);
                    break;
                case ClosureFunction closure:
                    WriteClosure(writer, closure, context);
                    break;
                case ClrInstanceObject clrInstanceObject:
                    WriteClrInstanceObject(writer, clrInstanceObject, context);
                    break;
                case ClrMethodBinding clrMethod:
                    WriteClrMethodBinding(writer, clrMethod, context);
                    break;
                case BondingFunction bonding:
                    WriteBondingFunction(writer, bonding, context);
                    break;
                case ScriptArray array:
                    WriteArray(writer, array, context);
                    break;
                case ScriptPackedArray packedArray:
                    WritePackedArray(writer, packedArray, context);
                    break;
                case ScriptDate date:
                    WriteDate(writer, date, context);
                    break;
                case ScriptRegex regex:
                    WriteRegex(writer, regex, context);
                    break;
                case ScriptType clrType:
                    WriteType(writer, clrType, context);
                    break;
                case ScriptHashMap hashMap:
                    WriteHashMap(writer, hashMap, context);
                    break;
                case ScriptError error:
                    WriteError(writer, error, context);
                    break;

                default:
                    if (context.Visited != null && !context.Visited.Add(value))
                    {
                        WriteCircularReference(writer, value, context);
                        return;
                    }
                    writer.WriteStartObject();
                    SerializeObjectProperties(writer, value, context);
                    writer.WriteEndObject();
                    context.Visited?.Remove(value);
                    break;
            }

        }





        /// <summary>
        /// Serializes the properties of a script object.
        /// </summary>
        protected virtual void SerializeObjectProperties(Utf8JsonWriter writer, ScriptObject value, in ScriptSerializationContext context)
        {
            var keys = value.EnumerationKeys();
            for (int i = 0; i < keys.Count; i++)
            {
                var propertyName = keys[i];
                writer.WritePropertyName(propertyName);
                var propDatum = value.GetPropertyDatum(null, propertyName);
                WriteDatum(writer, in propDatum, context);
            }
        }

        /// <summary> Writes a placeholder for a circular reference to the JSON stream. </summary>
        protected virtual void WriteCircularReference(Utf8JsonWriter writer, ScriptObject value, in ScriptSerializationContext context)
        {
            writer.WriteStringValue("<circular>");
        }

        /// <summary> Writes a null value to the JSON stream. </summary>
        protected virtual void WriteError(Utf8JsonWriter writer, ScriptError value, in ScriptSerializationContext context)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("message");
            writer.WriteStringValue(value.Message);
            writer.WritePropertyName("stackTrace");
            writer.WriteStartArray();

            foreach (var frame in value.StackTrace)
            {
                writer.WriteStringValue(frame.ToString());
            }


            writer.WriteEndArray();
            writer.WriteEndObject();
        }


        /// <summary> Writes a null value to the JSON stream. </summary>
        protected virtual void WriteNull(Utf8JsonWriter writer, in ScriptSerializationContext context)
        {
            writer.WriteNullValue();
        }


        /// <summary> Writes a numeric value to the JSON stream. Handles NaN and Infinity by writing null. </summary>
        protected virtual void WriteNumber(Utf8JsonWriter writer, NumberValue numberValue, in ScriptSerializationContext context)
        {
            WriteNumberValue(writer, numberValue.DoubleValue, context);
        }

        /// <summary> Writes a raw numeric value to the JSON stream. Handles NaN and Infinity by writing null. </summary>
        protected virtual void WriteNumberValue(Utf8JsonWriter writer, double doubleValue, in ScriptSerializationContext context)
        {
            if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue(doubleValue);
            }
        }

        /// <summary> Writes a boolean value to the JSON stream. </summary>
        protected virtual void WriteBoolean(Utf8JsonWriter writer, BooleanValue boolValue, in ScriptSerializationContext context)
        {
            WriteBooleanValue(writer, boolValue.Value, context);
        }

        /// <summary> Writes a raw boolean value to the JSON stream. </summary>
        protected virtual void WriteBooleanValue(Utf8JsonWriter writer, bool value, in ScriptSerializationContext context)
        {
            writer.WriteBooleanValue(value);
        }

        /// <summary> Writes a string value to the JSON stream. </summary>
        protected virtual void WriteString(Utf8JsonWriter writer, StringValue stringValue, in ScriptSerializationContext context)
        {
            writer.WriteStringValue(stringValue.Value);
        }


        /// <summary> Writes a date value to the JSON stream using the configured format. </summary>
        protected virtual void WriteDate(Utf8JsonWriter writer, ScriptDate date, in ScriptSerializationContext context)
        {
            writer.WriteStringValue(date.Format(context.Options.Runtime.DateTimeFormat));
        }

        /// <summary> Writes a regular expression to the JSON stream. </summary>
        protected virtual void WriteRegex(Utf8JsonWriter writer, ScriptRegex regex, in ScriptSerializationContext context)
        {
            writer.WriteStringValue(regex.ToString());
        }


        /// <summary> Writes a script array to the JSON stream. Handles circular references. </summary>
        protected virtual void WriteArray(Utf8JsonWriter writer, ScriptArray array, in ScriptSerializationContext context)
        {
            if (array == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (context.Visited != null && !context.Visited.Add(array))
            {
                WriteCircularReference(writer, array, context);
                return;
            }

            writer.WriteStartArray();
            for (int i = 0; i < array.Length; i++)
            {
                WriteDatum(writer, array.GetElement(i), context);
            }
            writer.WriteEndArray();

            context.Visited?.Remove(array);
        }

        /// <summary>Writes a fixed-length primitive array as a JSON array.</summary>
        protected virtual void WritePackedArray(
            Utf8JsonWriter writer,
            ScriptPackedArray array,
            in ScriptSerializationContext context)
        {
            if (array == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            if (array is ScriptInt64Array int64)
            {
                for (var i = 0; i < int64._items.Length; i++) writer.WriteNumberValue(int64._items[i]);
                writer.WriteEndArray();
                return;
            }
            if (array is ScriptUInt64Array uint64)
            {
                for (var i = 0; i < uint64._items.Length; i++) writer.WriteNumberValue(uint64._items[i]);
                writer.WriteEndArray();
                return;
            }
            for (var i = 0; i < array.Length; i++)
            {
                var value = array.GetElementDatumUnchecked(i);
                WriteDatum(writer, in value, context);
            }
            writer.WriteEndArray();
        }

        /// <summary> Writes a closure function to the JSON stream, including its name. </summary>
        protected virtual void WriteClosure(Utf8JsonWriter writer, ClosureFunction closure, in ScriptSerializationContext context)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("__kind__");
            writer.WriteStringValue("function");
            writer.WritePropertyName("__name__");
            writer.WriteStringValue(string.IsNullOrEmpty(closure.FuncName) ? "anonymous" : closure.FuncName);
            writer.WriteEndObject();
        }

        /// <summary> Writes a script type definition to the JSON stream. </summary>
        protected virtual void WriteType(Utf8JsonWriter writer, ScriptType type, in ScriptSerializationContext context)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        /// <summary> Writes a hash map (dictionary) to the JSON stream. </summary>
        protected virtual void WriteHashMap(Utf8JsonWriter writer, ScriptHashMap hashMap, in ScriptSerializationContext context)
        {
            writer.WriteStartObject();
            var keys = hashMap.Keys();
            foreach (var key in keys)
            {
                var value = hashMap.Get(key);
                writer.WritePropertyName(key.ToString());
                WriteDatum(writer, in value, context);
            }
            writer.WriteEndObject();
        }

        /// <summary> Writes a CLR method binding to the JSON stream. </summary>
        protected virtual void WriteClrMethodBinding(Utf8JsonWriter writer, ClrMethodBinding clrMethod, in ScriptSerializationContext context)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        /// <summary> Writes a bonding function to the JSON stream. </summary>
        protected virtual void WriteBondingFunction(Utf8JsonWriter writer, BondingFunction bonding, in ScriptSerializationContext context)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        /// <summary> Writes the global context object to the JSON stream. </summary>
        protected virtual void WriteGlobal(Utf8JsonWriter writer, ScriptGlobal global, in ScriptSerializationContext context)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        /// <summary> Writes a script module to the JSON stream. </summary>
        protected virtual void WriteModule(Utf8JsonWriter writer, ScriptModule module, in ScriptSerializationContext context)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes a CLR instance object to the JSON stream.
        /// Serializes public instance properties and fields that are readable.
        /// </summary>
        protected virtual void WriteClrInstanceObject(Utf8JsonWriter writer, ClrInstanceObject clrInstanceObject, in ScriptSerializationContext context)
        {
            if (clrInstanceObject == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (context.Visited != null && !context.Visited.Add(clrInstanceObject))
            {
                WriteCircularReference(writer, clrInstanceObject, context);
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("__kind__");
            writer.WriteStringValue("clr:instance");
            writer.WritePropertyName("__type__");
            writer.WriteStringValue(clrInstanceObject.Descriptor.Type.FullName);
            // Serialize public properties
            foreach (var prop in clrInstanceObject.Descriptor.Type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                try
                {
                    if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                    {
                        var val = prop.GetValue(clrInstanceObject.Instance);
                        writer.WritePropertyName(prop.Name);
                        WriteDatum(writer, ClrMarshaller.ToDatum(val), context);
                    }
                }
                catch { }
            }

            // Serialize public fields
            foreach (var field in clrInstanceObject.Descriptor.Type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                try
                {
                    var val = field.GetValue(clrInstanceObject.Instance);
                    writer.WritePropertyName(field.Name);
                    WriteDatum(writer, ClrMarshaller.ToDatum(val), context);
                }
                catch { }
            }

            writer.WriteEndObject();
            context.Visited?.Remove(clrInstanceObject);
        }

        /// <summary>
        /// An equality comparer for script objects that uses reference equality.
        /// Essential for correctly detecting circular references in object graphs.
        /// </summary>
        internal sealed class ReferenceComparer : IEqualityComparer<ScriptObject>
        {
            /// <summary> A singleton instance of the reference comparer. </summary>
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            /// <summary> Compares two objects for reference equality. </summary>
            public bool Equals(ScriptObject x, ScriptObject y) => ReferenceEquals(x, y);
            /// <summary> Gets the hash code for an object based on its reference. </summary>
            public int GetHashCode(ScriptObject obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
