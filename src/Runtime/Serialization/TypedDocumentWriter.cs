using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace AuroraScript.Runtime.Serialization
{
    internal struct TypedDocumentWriter
    {
        private const int MaxRetainedCapacity = 1024 * 1024;
        private const int MaxRetainedVisitedCount = 4096;
        private const string HexDigits = "0123456789ABCDEF";

        [ThreadStatic]
        private static StringBuilder s_cachedBuilder;

        [ThreadStatic]
        private static HashSet<object> s_cachedVisited;

        private readonly AuroraEngine _engine;
        private readonly string _sourceName;
        private readonly bool _indented;
        private readonly bool _emitTypeNames;
        private readonly int _maxDepth;
        private StringBuilder _builder;
        private HashSet<object> _visited;
        private TypedDocumentPath _path;
        private int _depth;
        private int _valueDepth;

        internal TypedDocumentWriter(AuroraEngine engine, TypedDocumentOptions options, string sourceName)
        {
            _engine = engine;
            _sourceName = string.IsNullOrWhiteSpace(sourceName) ? "<tdoc>" : sourceName;
            _indented = options.Indented;
            _emitTypeNames = options.EmitTypeNames;
            _maxDepth = options.MaxDepth;
            _builder = RentBuilder();
            _visited = RentVisited();
            _path = new TypedDocumentPath(16);
            _depth = 0;
            _valueDepth = 0;
        }

        internal string Write(ScriptDatum value)
        {
            WriteCore(value);
            return _builder.ToString();
        }

        internal void WriteTo(TextWriter writer, ScriptDatum value)
        {
            ArgumentNullException.ThrowIfNull(writer);
            WriteCore(value);
            foreach (var chunk in _builder.GetChunks())
            {
                writer.Write(chunk.Span);
            }
        }

        private void WriteCore(ScriptDatum value)
        {
            if (!TryWriteTypedValue(value))
            {
                // A root value has no surrounding member to omit. Keep the document
                // valid and make the loss explicit as null instead of failing a
                // snapshot merely because it contains a runtime-only value.
                _builder.Append("null");
            }
        }

        internal void Dispose()
        {
            _path.Dispose();

            var visited = _visited;
            _visited = null;
            if (visited != null)
            {
                var visitedCount = visited.Count;
                if (visitedCount <= MaxRetainedVisitedCount && s_cachedVisited == null)
                {
                    visited.Clear();
                    s_cachedVisited = visited;
                }
            }

            var builder = _builder;
            _builder = null;
            if (builder == null) return;
            if (builder.Capacity <= MaxRetainedCapacity && s_cachedBuilder == null)
            {
                builder.Clear();
                s_cachedBuilder = builder;
            }
        }

        private bool TryWriteTypedValue(ScriptDatum value)
        {
            EnterValue();
            try
            {
                return TryWriteTypedValueCore(value);
            }
            finally
            {
                _valueDepth--;
            }
        }

        private bool TryWriteTypedValueCore(ScriptDatum value)
        {
            if (!TryResolveTypeName(value, out var typeName, out var clrRegistration) ||
                !TryTrackReference(value))
            {
                return false;
            }

            if (typeName != null && ShouldWriteTypeName(typeName)) _builder.Append(typeName).Append(' ');
            WriteRawValue(value, clrRegistration);
            return true;
        }

        private bool TryWriteMember(string name, ScriptDatum value, bool writable, bool writeLeadingLine = false)
        {
            _path.PushProperty(name);
            try
            {
                EnterValue();
                try
                {
                    if (!TryResolveTypeName(value, out var typeName, out var clrRegistration) ||
                        !TryTrackReference(value))
                    {
                        return false;
                    }

                    if (writeLeadingLine) WriteNewLine();
                    WriteIndent();
                    if (!writable) _builder.Append("readonly ");
                    if (typeName != null && ShouldWriteTypeName(typeName)) _builder.Append(typeName).Append(' ');
                    WritePropertyName(name);
                    _builder.Append(' ');
                    WriteRawValue(value, clrRegistration);
                    WriteItemEnd();
                    return true;
                }
                finally
                {
                    _valueDepth--;
                }
            }
            finally
            {
                _path.Pop();
            }
        }

        private void WriteRawValue(ScriptDatum value, ClrType clrRegistration)
        {
            switch (value.Kind)
            {
                case ValueKind.Null:
                    _builder.Append("null");
                    return;
                case ValueKind.Boolean:
                    _builder.Append(value.Boolean ? "true" : "false");
                    return;
                case ValueKind.Number:
                    WriteNumber(value.Number);
                    return;
                case ValueKind.Int64:
                    WriteInt64(value.Int64);
                    return;
                case ValueKind.UInt64:
                    WriteUInt64(value.UInt64);
                    return;
                case ValueKind.String:
                    WriteString(value.StringText);
                    return;
            }

            var scriptObject = value.Object;
            if (scriptObject == null)
            {
                throw Error("Object-backed datum has no value.");
            }

            switch (scriptObject)
            {
                case ScriptArray array:
                    WriteArray(array);
                    return;
                case ScriptInt32Array int32:
                    WritePackedArray(int32);
                    return;
                case ScriptInt8Array int8:
                    WritePackedArray(int8);
                    return;
                case ScriptFloat32Array float32:
                    WritePackedArray(float32);
                    return;
                case ScriptFloat64Array float64:
                    WritePackedArray(float64);
                    return;
                case ScriptBooleanArray boolean:
                    WritePackedArray(boolean);
                    return;
                case ScriptUInt8Array uint8:
                    WritePackedArray(uint8);
                    return;
                case ScriptInt16Array int16:
                    WritePackedArray(int16);
                    return;
                case ScriptUInt16Array uint16:
                    WritePackedArray(uint16);
                    return;
                case ScriptUInt32Array uint32:
                    WritePackedArray(uint32);
                    return;
                case ScriptInt64Array int64:
                    WritePackedArray(int64);
                    return;
                case ScriptUInt64Array uint64:
                    WritePackedArray(uint64);
                    return;
                case ScriptDate date:
                    WriteDate(date);
                    return;
                case ScriptRegex regex:
                    WriteRegex(regex);
                    return;
                case StringBuffer buffer:
                    WriteString(buffer);
                    return;
                case ScriptPathValue path:
                    WriteString(path.Value);
                    return;
                case ScriptHashMap hashMap:
                    WriteHashMap(hashMap);
                    return;
                case ClrInstanceObject clrInstance:
                    WriteClrObject(clrInstance, clrRegistration);
                    return;
                case INativeTypedDocument typedDocument:
                    WriteNativeTypedDocument(typedDocument);
                    return;
                default:
                    if (scriptObject.GetType() == typeof(ScriptObject))
                    {
                        WriteObject(scriptObject);
                        return;
                    }
                    throw Error($"Runtime value '{scriptObject.GetType().FullName}' is not supported by TDoc.");
            }
        }

        private void WriteObject(ScriptObject value)
        {
            if (value.HasEnumerablePrototypeProperties())
            {
                WriteObjectWithEnumerablePrototypeProperties(value);
                return;
            }

            var properties = value.OwnProperties;
            _builder.Append('{');
            _depth++;
            var writtenCount = 0;
            for (var index = 0; index < properties.Length; index++)
            {
                ref readonly var metadata = ref properties[index];
                if (!metadata.Meta.Enumerable) continue;
                var property = value.GetOwnProperty(metadata.Meta.Slot);
                if (property.IsAccessor) continue;
                if (TryWriteMember(metadata.Name, property.Datum, metadata.Meta.Writable, writtenCount == 0))
                {
                    writtenCount++;
                }
            }
            _depth--;
            if (writtenCount != 0)
            {
                RemoveTrailingComma();
                WriteIndent();
            }
            _builder.Append('}');
        }

        private void WriteObjectWithEnumerablePrototypeProperties(ScriptObject value)
        {
            // This fallback is only used for objects that expose enumerable properties
            // through a prototype. It matches script enumeration semantics by taking
            // the closest property for each name, then materializes the visible shape
            // as ordinary TDoc object members.
            var seenNames = new HashSet<string>(StringComparer.Ordinal);
            _builder.Append('{');
            _depth++;
            var writtenCount = 0;
            for (var current = value; current != null; current = current.Prototype)
            {
                if (current.Immutable) continue;

                var properties = current.OwnProperties;
                for (var index = 0; index < properties.Length; index++)
                {
                    ref readonly var metadata = ref properties[index];
                    if (!metadata.Meta.Enumerable || !seenNames.Add(metadata.Name)) continue;

                    var property = current.GetOwnProperty(metadata.Meta.Slot);
                    if (property.IsAccessor) continue;
                    if (TryWriteMember(metadata.Name, property.Datum, metadata.Meta.Writable, writtenCount == 0))
                    {
                        writtenCount++;
                    }
                }
            }
            _depth--;
            if (writtenCount != 0)
            {
                RemoveTrailingComma();
                WriteIndent();
            }
            _builder.Append('}');
        }

        internal bool TryWriteNativeMember(string name, ScriptDatum value, bool writable, bool writeLeadingLine)
        {
            return TryWriteMember(name, value, writable, writeLeadingLine);
        }

        internal bool TryWriteNativeElement(ScriptDatum value, int index, bool writeLeadingLine)
        {
            _path.PushIndex(index);
            try
            {
                EnterValue();
                try
                {
                    if (!TryResolveTypeName(value, out var typeName, out var clrRegistration) ||
                        !TryTrackReference(value))
                    {
                        return false;
                    }

                    if (writeLeadingLine) WriteNewLine();
                    WriteIndent();
                    if (typeName != null && ShouldWriteTypeName(typeName)) _builder.Append(typeName).Append(' ');
                    WriteRawValue(value, clrRegistration);
                    WriteItemEnd();
                    return true;
                }
                finally
                {
                    _valueDepth--;
                }
            }
            finally
            {
                _path.Pop();
            }
        }

        internal void BeginNativeBody(char opening)
        {
            _builder.Append(opening);
            _depth++;
        }

        internal void EndNativeBody(char closing, int writtenCount)
        {
            _depth--;
            if (writtenCount != 0)
            {
                RemoveTrailingComma();
                WriteIndent();
            }
            _builder.Append(closing);
        }

        internal void WriteEmptyNativeObject()
        {
            _builder.Append("{}");
        }

        internal void WriteNativeScalar(ScriptDatum value)
        {
            WriteRawValue(value, clrRegistration: default);
        }

        private void WriteNativeTypedDocument(INativeTypedDocument document)
        {
            var output = new TypedDocumentOutput(ref this);
            document.WriteTypedDocument(ref output);
            output.Complete();
        }

        internal int WriteNativeDynamicMembers(ScriptObject value, int writtenCount)
        {
            var properties = value.OwnProperties;
            for (var index = 0; index < properties.Length; index++)
            {
                ref readonly var metadata = ref properties[index];
                if (!metadata.Meta.Enumerable) continue;
                var property = value.GetOwnProperty(metadata.Meta.Slot);
                if (property.IsAccessor) continue;
                if (TryWriteNativeMember(
                    metadata.Name,
                    property.Datum,
                    metadata.Meta.Writable,
                    writtenCount == 0))
                {
                    writtenCount++;
                }
            }

            return writtenCount;
        }

        private void WriteArray(ScriptArray value)
        {
            var length = value.Length;
            var items = value._items;
            BeginComposite('[', length);
            for (var index = 0; index < length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    if (!TryWriteTypedValue(items[index])) _builder.Append("null");
                }
                finally
                {
                    _path.Pop();
                }
                WriteItemEnd();
            }
            EndComposite(']', length);
        }

        private void WritePackedArray(ScriptPackedArray value)
        {
            var length = value.Length;
            BeginComposite('[', length);

            // Every built-in packed array is primitive-backed.  Read its backing
            // storage directly: materializing a ScriptDatum for each item is
            // unnecessary work, and is impossible for some Int64/UInt64 values.
            switch (value)
            {
                case ScriptInt32Array int32:
                    WritePackedInt32Elements(int32._items);
                    break;
                case ScriptInt8Array int8:
                    WritePackedInt8Elements(int8._items);
                    break;
                case ScriptFloat32Array float32:
                    WritePackedFloat32Elements(float32._items);
                    break;
                case ScriptFloat64Array float64:
                    WritePackedFloat64Elements(float64._items);
                    break;
                case ScriptBooleanArray boolean:
                    WritePackedBooleanElements(boolean._items);
                    break;
                case ScriptUInt8Array uint8:
                    WritePackedUInt8Elements(uint8._items);
                    break;
                case ScriptInt16Array int16:
                    WritePackedInt16Elements(int16._items);
                    break;
                case ScriptUInt16Array uint16:
                    WritePackedUInt16Elements(uint16._items);
                    break;
                case ScriptUInt32Array uint32:
                    WritePackedUInt32Elements(uint32._items);
                    break;
                case ScriptInt64Array int64:
                    WritePackedInt64Elements(int64._items);
                    break;
                case ScriptUInt64Array uint64:
                    WritePackedUInt64Elements(uint64._items);
                    break;
                default:
                    WriteUnknownPackedElements(value, length);
                    break;
            }
            EndComposite(']', length);
        }

        private void WritePackedInt32Elements(int[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    EnterValue();
                    try { WriteInt32(values[index]); }
                    finally { _valueDepth--; }
                }
                finally { _path.Pop(); }
                WriteItemEnd();
            }
        }

        private void WritePackedInt8Elements(sbyte[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    EnterValue();
                    try { WriteInt32(values[index]); }
                    finally { _valueDepth--; }
                }
                finally { _path.Pop(); }
                WriteItemEnd();
            }
        }

        private void WritePackedFloat32Elements(float[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    EnterValue();
                    try { WriteNumber(values[index]); }
                    finally { _valueDepth--; }
                }
                finally { _path.Pop(); }
                WriteItemEnd();
            }
        }

        private void WritePackedFloat64Elements(double[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    EnterValue();
                    try { WriteNumber(values[index]); }
                    finally { _valueDepth--; }
                }
                finally { _path.Pop(); }
                WriteItemEnd();
            }
        }

        private void WritePackedBooleanElements(bool[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    EnterValue();
                    try { _builder.Append(values[index] ? "true" : "false"); }
                    finally { _valueDepth--; }
                }
                finally { _path.Pop(); }
                WriteItemEnd();
            }
        }

        private void WritePackedUInt8Elements(byte[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    EnterValue();
                    try { WriteInt32(values[index]); }
                    finally { _valueDepth--; }
                }
                finally { _path.Pop(); }
                WriteItemEnd();
            }
        }

        private void WritePackedInt16Elements(short[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    EnterValue();
                    try { WriteInt32(values[index]); }
                    finally { _valueDepth--; }
                }
                finally { _path.Pop(); }
                WriteItemEnd();
            }
        }

        private void WritePackedUInt16Elements(ushort[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    EnterValue();
                    try { WriteInt32(values[index]); }
                    finally { _valueDepth--; }
                }
                finally { _path.Pop(); }
                WriteItemEnd();
            }
        }

        private void WritePackedUInt32Elements(uint[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    EnterValue();
                    try { WriteUInt32(values[index]); }
                    finally { _valueDepth--; }
                }
                finally { _path.Pop(); }
                WriteItemEnd();
            }
        }

        private void WritePackedInt64Elements(long[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    EnterValue();
                    try { WriteInt64(values[index]); }
                    finally { _valueDepth--; }
                }
                finally { _path.Pop(); }
                WriteItemEnd();
            }
        }

        private void WritePackedUInt64Elements(ulong[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    EnterValue();
                    try { WriteUInt64(values[index]); }
                    finally { _valueDepth--; }
                }
                finally { _path.Pop(); }
                WriteItemEnd();
            }
        }

        private void WriteUnknownPackedElements(ScriptPackedArray value, int length)
        {
            for (var index = 0; index < length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    EnterValue();
                    try
                    {
                        var element = value.GetElementDatumUnchecked(index);
                        if (element.Kind == ValueKind.Boolean)
                        {
                            _builder.Append(element.Boolean ? "true" : "false");
                        }
                        else
                        {
                            WriteNumber(element.Number);
                        }
                    }
                    finally
                    {
                        _valueDepth--;
                    }
                }
                finally
                {
                    _path.Pop();
                }
                WriteItemEnd();
            }
        }

        private void WriteDate(ScriptDate value)
        {
            var format = _engine.Options.Runtime.DateTimeFormat;
            if (string.IsNullOrEmpty(format))
            {
                throw Error("EngineOptions.Runtime.DateTimeFormat cannot be null or empty.");
            }
            Span<char> buffer = stackalloc char[128];
            try
            {
                if (value.DateTime.TryFormat(buffer, out var written, format, CultureInfo.InvariantCulture))
                {
                    WriteString(buffer[..written]);
                    return;
                }
                WriteString(value.DateTime.ToString(format, CultureInfo.InvariantCulture));
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                throw Error($"Invalid EngineOptions.Runtime.DateTimeFormat '{format}'.", exception);
            }
        }

        private void WriteRegex(ScriptRegex value)
        {
            BeginComposite('{', 2);
            _ = TryWriteMember("pattern", ScriptDatum.FromString(value.Pattern), writable: true);
            _ = TryWriteMember("flags", ScriptDatum.FromString(value.Flags), writable: true);
            EndComposite('}', 2);
        }

        private void WriteHashMap(ScriptHashMap value)
        {
            var count = value.Entries.Count;
            BeginComposite('[', count);
            var entryIndex = 0;
            foreach (var entry in value.Entries)
            {
                WriteIndent();
                if (ShouldWriteTypeName("Array")) _builder.Append("Array ");
                BeginComposite('[', 2);

                _path.PushIndex(entryIndex);
                try
                {
                    _path.PushIndex(0);
                    try
                    {
                        WriteIndent();
                        if (!TryWriteTypedValue(entry.Key)) _builder.Append("null");
                        WriteItemEnd();
                    }
                    finally
                    {
                        _path.Pop();
                    }

                    _path.PushIndex(1);
                    try
                    {
                        WriteIndent();
                        if (!TryWriteTypedValue(entry.Value)) _builder.Append("null");
                        WriteItemEnd();
                    }
                    finally
                    {
                        _path.Pop();
                    }
                }
                finally
                {
                    _path.Pop();
                }

                EndComposite(']', 2);
                WriteItemEnd();
                entryIndex++;
            }
            EndComposite(']', count);
        }

        private void WriteClrObject(ClrInstanceObject value, ClrType registration)
        {
            if (registration == null)
            {
                throw Error("CLR object has no host registration.");
            }
            if ((registration._access & TypeAccess.Constructor) == 0)
            {
                throw Error("The host registration does not allow CLR object construction.");
            }
            var descriptor = registration._descriptor;
            var contract = descriptor.DataContract;
            var members = contract.Members;
            if (contract.Factory == null)
            {
                throw Error("The registered CLR object requires a public parameterless constructor.");
            }
            _builder.Append('{');
            _depth++;
            var writtenCount = 0;
            foreach (var member in members)
            {
                ScriptDatum memberValue;
                _path.PushProperty(member.Name);
                try
                {
                    object clrValue;
                    try
                    {
                        var getter = member.Getter;
                        if (getter == null)
                        {
                            continue;
                        }
                        clrValue = getter(value.Instance);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    try
                    {
                        memberValue = ConvertClrMemberValue(clrValue);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
                finally
                {
                    _path.Pop();
                }
                if (TryWriteMember(member.Name, memberValue, writable: true, writtenCount == 0))
                {
                    writtenCount++;
                }
            }
            _depth--;
            if (writtenCount != 0)
            {
                RemoveTrailingComma();
                WriteIndent();
            }
            _builder.Append('}');
        }

        private ScriptDatum ConvertClrMemberValue(object value)
        {
            if (value == null) return ScriptDatum.Null;

            var type = value.GetType();
            if (type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
                value = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }

            if (type == typeof(long))
            {
                return ScriptDatum.FromInt64((long)value);
            }
            if (type == typeof(ulong))
            {
                return ScriptDatum.FromUInt64((ulong)value);
            }
            if (type == typeof(decimal))
            {
                var decimalValue = (decimal)value;
                var number = (double)decimalValue;
                if (!double.IsFinite(number) || (decimal)number != decimalValue)
                {
                    throw Error("CLR decimal value cannot be represented exactly as a TDoc Number.");
                }
                return ScriptDatum.FromNumber(number);
            }
            return ClrMarshaller.ToDatum(value);
        }

        private bool TryResolveTypeName(ScriptDatum value, out string typeName, out ClrType clrRegistration)
        {
            typeName = null;
            clrRegistration = null;
            switch (value.Kind)
            {
                case ValueKind.Null: return true;
                case ValueKind.Boolean:
                    typeName = "Boolean";
                    return true;
                case ValueKind.Number:
                    if (!double.IsFinite(value.Number))
                    {
                        return false;
                    }
                    typeName = "Number";
                    return true;
                case ValueKind.Int64:
                    typeName = "Int64";
                    return true;
                case ValueKind.UInt64:
                    typeName = "UInt64";
                    return true;
                case ValueKind.String:
                    typeName = "String";
                    return true;
                case ValueKind.Array:
                    if (value.Object is ScriptArray)
                    {
                        typeName = "Array";
                        return true;
                    }
                    break;
                case ValueKind.Date:
                    if (value.Object is ScriptDate)
                    {
                        typeName = "Date";
                        return true;
                    }
                    break;
                case ValueKind.Regex:
                    if (value.Object is ScriptRegex)
                    {
                        typeName = "Regex";
                        return true;
                    }
                    break;
                case ValueKind.Function:
                case ValueKind.Type:
                case ValueKind.ClrFunction:
                case ValueKind.ClrBonding:
                case ValueKind.Error:
                    return false;
            }

            var scriptObject = value.Object;
            switch (scriptObject)
            {
                case ScriptInt32Array:
                    typeName = "Int32Array";
                    return true;
                case ScriptInt8Array:
                    typeName = "Int8Array";
                    return true;
                case ScriptFloat32Array:
                    typeName = "Float32Array";
                    return true;
                case ScriptFloat64Array:
                    typeName = "Float64Array";
                    return true;
                case ScriptBooleanArray:
                    typeName = "BooleanArray";
                    return true;
                case ScriptUInt8Array:
                    typeName = "UInt8Array";
                    return true;
                case ScriptInt16Array:
                    typeName = "Int16Array";
                    return true;
                case ScriptUInt16Array:
                    typeName = "UInt16Array";
                    return true;
                case ScriptUInt32Array:
                    typeName = "UInt32Array";
                    return true;
                case ScriptInt64Array:
                    typeName = "Int64Array";
                    return true;
                case ScriptUInt64Array:
                    typeName = "UInt64Array";
                    return true;
                case ScriptArray:
                    typeName = "Array";
                    return true;
                case ScriptDate:
                    typeName = "Date";
                    return true;
                case ScriptRegex:
                    typeName = "Regex";
                    return true;
                case StringBuffer:
                    typeName = "StringBuffer";
                    return true;
                case ScriptPathValue:
                    typeName = "Path";
                    return true;
                case ScriptHashMap:
                    typeName = "HashMap";
                    return true;
                case ClrInstanceObject clrInstance:
                    var actualType = clrInstance.Instance?.GetType();
                    if (actualType == null ||
                        !actualType.IsClass ||
                        actualType.IsAbstract ||
                        actualType.IsArray ||
                        actualType.ContainsGenericParameters ||
                        typeof(Delegate).IsAssignableFrom(actualType) ||
                        !_engine.ClrRegistry.TryGetClrType(actualType, out var alias, out clrRegistration))
                    {
                        return false;
                    }
                    if (!IsUsableTypeAlias(alias) ||
                        (clrRegistration._access & TypeAccess.Constructor) == 0 ||
                        clrRegistration._descriptor.DataContract.Factory == null)
                    {
                        return false;
                    }
                    typeName = alias;
                    return true;
                case INativeTypedDocument typedDocument
                    when _engine.TypedDocuments.TryGet(typedDocument.GetType(), out var native):
                    typeName = native.TypeName;
                    return true;
                case ScriptObject when scriptObject.GetType() == typeof(ScriptObject):
                    typeName = "Object";
                    return true;
                default:
                    return false;
            }
        }

        private bool TryTrackReference(ScriptDatum value)
        {
            if (value.Kind is ValueKind.Null or ValueKind.Boolean or ValueKind.Number or
                ValueKind.Int64 or ValueKind.UInt64 or ValueKind.String)
            {
                return true;
            }

            var scriptObject = value.Object;
            if (scriptObject == null) return false;
            if (scriptObject is ScriptDate or ScriptRegex) return true;

            var identity = scriptObject is ClrInstanceObject clr ? clr.Instance : scriptObject;
            return identity != null && _visited.Add(identity);
        }

        private void BeginComposite(char opening, int count)
        {
            _builder.Append(opening);
            if (count == 0) return;
            _depth++;
            WriteNewLine();
        }

        private void EndComposite(char closing, int count)
        {
            if (count != 0)
            {
                RemoveTrailingComma();
                _depth--;
                WriteIndent();
            }
            _builder.Append(closing);
        }

        private void RemoveTrailingComma()
        {
            var index = _builder.Length - 1;
            while (index >= 0 && char.IsWhiteSpace(_builder[index])) index--;
            if (index >= 0 && _builder[index] == ',') _builder.Remove(index, 1);
        }

        private void WriteItemEnd()
        {
            _builder.Append(',');
            WriteNewLine();
        }

        private void WriteIndent()
        {
            if (!_indented) return;
            _builder.Append(' ', _depth * 4);
        }

        private void WriteNewLine()
        {
            if (_indented) _builder.AppendLine();
        }

        private void WritePropertyName(string value)
        {
            if (TypedDocumentPath.IsIdentifier(value) &&
                value is not ("null" or "true" or "false" or "readonly") &&
                !IsBuiltInTypeName(value) &&
                !_engine.ClrRegistry.ContainsAlias(value))
            {
                _builder.Append(value);
                return;
            }
            WriteString(value);
        }

        private void WriteString(string value)
        {
            WriteString((value ?? string.Empty).AsSpan());
        }

        private void WriteString(StringBuffer value)
        {
            _builder.Append('"');
            var source = value.Builder;
            if (source != null)
            {
                foreach (var chunk in source.GetChunks())
                {
                    WriteEscapedStringContent(chunk.Span);
                }
            }
            _builder.Append('"');
        }

        private void WriteString(scoped ReadOnlySpan<char> value)
        {
            _builder.Append('"');
            WriteEscapedStringContent(value);
            _builder.Append('"');
        }

        private void WriteEscapedStringContent(scoped ReadOnlySpan<char> value)
        {
            var chunkStart = 0;
            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];
                string escape = current switch
                {
                    '"' => "\\\"",
                    '\\' => "\\\\",
                    '\b' => "\\b",
                    '\f' => "\\f",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    _ => null
                };
                if (escape == null && !char.IsControl(current)) continue;

                if (index > chunkStart) _builder.Append(value[chunkStart..index]);
                if (escape != null)
                {
                    _builder.Append(escape);
                }
                else
                {
                    _builder.Append("\\u")
                        .Append(HexDigits[(current >> 12) & 0xF])
                        .Append(HexDigits[(current >> 8) & 0xF])
                        .Append(HexDigits[(current >> 4) & 0xF])
                        .Append(HexDigits[current & 0xF]);
                }
                chunkStart = index + 1;
            }
            if (chunkStart < value.Length) _builder.Append(value[chunkStart..]);
        }

        private void WriteNumber(double value)
        {
            if (!double.IsFinite(value)) throw Error("TDoc cannot serialize NaN or infinity.");
            Span<char> buffer = stackalloc char[32];
            if (value.TryFormat(buffer, out var written, "R", CultureInfo.InvariantCulture))
            {
                _builder.Append(buffer[..written]);
                return;
            }
            _builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private void WriteInt32(int value)
        {
            Span<char> buffer = stackalloc char[11];
            if (!value.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture))
            {
                throw Error("Could not format packed integer element.");
            }
            _builder.Append(buffer[..written]);
        }

        private void WriteUInt32(uint value)
        {
            Span<char> buffer = stackalloc char[10];
            if (!value.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture))
            {
                throw Error("Could not format packed integer element.");
            }
            _builder.Append(buffer[..written]);
        }

        private void WriteInt64(long value)
        {
            Span<char> buffer = stackalloc char[20];
            if (!value.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture))
            {
                throw Error("Could not format Int64Array element.");
            }
            _builder.Append(buffer[..written]);
        }

        private void WriteUInt64(ulong value)
        {
            Span<char> buffer = stackalloc char[20];
            if (!value.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture))
            {
                throw Error("Could not format UInt64Array element.");
            }
            _builder.Append(buffer[..written]);
        }

        private bool ShouldWriteTypeName(string typeName)
        {
            return _emitTypeNames || typeName is not ("Boolean" or "Number" or "String" or "Object" or "Array");
        }

        private TypedDocumentException Error(string message, Exception innerException = null)
        {
            return new TypedDocumentException(
                message,
                _sourceName,
                0,
                0,
                _path.Format(),
                innerException);
        }

        private void EnterValue()
        {
            if (_valueDepth >= _maxDepth)
            {
                throw Error($"TDoc value depth exceeds the configured limit of {_maxDepth}.");
            }
            _valueDepth++;
        }

        private static bool IsBuiltInTypeName(string value)
        {
            return value is "Object" or "Array" or "String" or "Number" or "Boolean" or
                "StringBuffer" or "Date" or "Regex" or "Path" or "HashMap" or
                "Int32Array" or "Int8Array" or "Float32Array" or "Float64Array" or "BooleanArray" or
                "UInt8Array" or "Int16Array" or "UInt16Array" or "UInt32Array" or
                "Int64Array" or "UInt64Array";
        }

        private static bool IsUsableTypeAlias(string value)
        {
            return TypedDocumentPath.IsIdentifier(value) &&
                value is not ("null" or "true" or "false" or "readonly") &&
                !IsBuiltInTypeName(value);
        }

        private static StringBuilder RentBuilder()
        {
            var builder = s_cachedBuilder;
            if (builder == null) return new StringBuilder(256);
            s_cachedBuilder = null;
            builder.Clear();
            return builder;
        }

        private static HashSet<object> RentVisited()
        {
            var visited = s_cachedVisited;
            if (visited == null) return new HashSet<object>(ReferenceObjectComparer.Instance);
            s_cachedVisited = null;
            visited.Clear();
            return visited;
        }

        private sealed class ReferenceObjectComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceObjectComparer Instance = new();

            public new bool Equals(object left, object right) => ReferenceEquals(left, right);

            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
