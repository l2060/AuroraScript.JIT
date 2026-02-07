using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System.Text.Json;

namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Standard serializer for script use, designed to match the behavior of <c>JSON.stringify</c>.
    /// Follows JavaScript standards by skipping function properties in objects and 
    /// serializing them to null when they appear in arrays.
    /// </summary>
    public class StandardScriptJsonSerializer : ScriptJsonSerializer
    {
        /// <summary>
        /// Overrides the base datum writing to handle functions according to JS standards.
        /// Functions are serialized as null.
        /// </summary>
        protected override void WriteDatum(Utf8JsonWriter writer, in ScriptDatum datum, in ScriptSerializationContext context)
        {
            if (datum.Kind == ValueKind.Function || datum.Kind == ValueKind.ClrFunction || datum.Kind == ValueKind.ClrBonding)
            {
                writer.WriteNullValue();
                return;
            }
            base.WriteDatum(writer, datum, context);
        }


        /// <summary>
        /// Writes an empty object for closures, as per standard JSON serialization for functions.
        /// </summary>
        protected override void WriteClosure(Utf8JsonWriter writer, ClosureFunction closure, in ScriptSerializationContext context)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes a CLR instance object to the JSON stream without specialized type metadata.
        /// This provides a "cleaner" JSON suitable for standard interop.
        /// </summary>
        protected override void WriteClrInstanceObject(Utf8JsonWriter writer, ClrInstanceObject clrInstanceObject, in ScriptSerializationContext context)
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
        /// Circular references are treated as errors in standard JSON serialization.
        /// </summary>
        /// <exception cref="AuroraRuntimeException">Thrown when a circular reference is detected.</exception>
        protected override void WriteCircularReference(Utf8JsonWriter writer, ScriptObject value, in ScriptSerializationContext context)
        {
            // JS throws error on circular reference normally, but our JsonSupport catches the exception
            throw new AuroraRuntimeException("JSON.stringify cannot serialize circular references");
        }
    }
}
