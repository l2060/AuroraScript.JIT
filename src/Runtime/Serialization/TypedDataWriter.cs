using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace AuroraScript.Runtime.Serialization
{
    internal ref struct TypedDataWriter
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
        private readonly int _maxDepth;
        private StringBuilder _builder;
        private HashSet<object> _visited;
        private TypedDataPath _path;
        private int _depth;
        private int _valueDepth;

        internal TypedDataWriter(AuroraEngine engine, TypedDataOptions options)
        {
            _engine = engine;
            _sourceName = string.IsNullOrWhiteSpace(options.SourceName) ? "<atd>" : options.SourceName;
            _indented = options.Indented;
            _maxDepth = options.MaxDepth;
            _builder = RentBuilder();
            _visited = RentVisited();
            _path = new TypedDataPath(16);
            _depth = 0;
            _valueDepth = 0;
        }

        internal string Write(ScriptDatum value)
        {
            WriteTypedValue(value);
            return _builder.ToString();
        }

        internal void Dispose()
        {
            _path.Dispose();

            var visited = _visited;
            _visited = null;
            if (visited != null)
            {
                var visitedCount = visited.Count;
                visited.Clear();
                if (visitedCount <= MaxRetainedVisitedCount && s_cachedVisited == null)
                {
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

        private void WriteTypedValue(ScriptDatum value)
        {
            EnterValue();
            try
            {
                WriteTypedValueCore(value);
            }
            finally
            {
                _valueDepth--;
            }
        }

        private void WriteTypedValueCore(ScriptDatum value)
        {
            var typeName = ResolveTypeName(value, out var clrRegistration);
            if (typeName == null)
            {
                _builder.Append("null");
                return;
            }

            _builder.Append(typeName).Append(' ');
            WriteRawValue(value, clrRegistration);
        }

        private void WriteMember(string name, ScriptDatum value, bool writable)
        {
            WriteIndent();
            _path.PushProperty(name);
            try
            {
                EnterValue();
                try
                {
                    if (!writable) _builder.Append("readonly ");
                    var typeName = ResolveTypeName(value, out var clrRegistration);
                    if (typeName != null) _builder.Append(typeName).Append(' ');
                    WritePropertyName(name);
                    _builder.Append(' ');
                    if (typeName == null) _builder.Append("null");
                    else WriteRawValue(value, clrRegistration);
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
                case ValueKind.String:
                    WriteString(value.StringText);
                    return;
            }

            var scriptObject = value.Object;
            if (scriptObject == null)
            {
                throw Error("Object-backed datum has no value.");
            }

            if (scriptObject is not (ScriptDate or ScriptRegex))
            {
                TrackReference(scriptObject is ClrInstanceObject clr ? clr.Instance : scriptObject);
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
                case ScriptFloat64Array float64:
                    WritePackedArray(float64);
                    return;
                case ScriptBooleanArray boolean:
                    WritePackedArray(boolean);
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
                default:
                    if (scriptObject.GetType() == typeof(ScriptObject))
                    {
                        WriteObject(scriptObject);
                        return;
                    }
                    throw Error($"Runtime value '{scriptObject.GetType().FullName}' is not supported by ATD.");
            }
        }

        private void WriteObject(ScriptObject value)
        {
            var properties = value.OwnProperties;
            var count = 0;
            for (var index = 0; index < properties.Length; index++)
            {
                if (properties[index].Meta.Enumerable) count++;
            }

            BeginComposite('{', count);
            for (var index = 0; index < properties.Length; index++)
            {
                ref readonly var metadata = ref properties[index];
                if (!metadata.Meta.Enumerable) continue;
                var property = value.GetOwnProperty(metadata.Meta.Slot);
                if (property.IsAccessor)
                {
                    _path.PushProperty(metadata.Name);
                    try
                    {
                        throw Error("Accessor properties cannot be serialized as ATD data.");
                    }
                    finally
                    {
                        _path.Pop();
                    }
                }
                WriteMember(metadata.Name, property.Datum, metadata.Meta.Writable);
            }
            EndComposite('}', count);
        }

        private void WriteArray(ScriptArray value)
        {
            BeginComposite('[', value.Length);
            for (var index = 0; index < value.Length; index++)
            {
                WriteIndent();
                _path.PushIndex(index);
                try
                {
                    WriteTypedValue(value.GetElement(index));
                }
                finally
                {
                    _path.Pop();
                }
                WriteItemEnd();
            }
            EndComposite(']', value.Length);
        }

        private void WritePackedArray(ScriptPackedArray value)
        {
            BeginComposite('[', value.Length);
            for (var index = 0; index < value.Length; index++)
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
            EndComposite(']', value.Length);
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
            WriteMember("pattern", ScriptDatum.FromString(value.Pattern), writable: true);
            WriteMember("flags", ScriptDatum.FromString(value.Flags), writable: true);
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
                _builder.Append("Array ");
                BeginComposite('[', 2);

                _path.PushIndex(entryIndex);
                try
                {
                    _path.PushIndex(0);
                    try
                    {
                        WriteIndent();
                        WriteTypedValue(entry.Key);
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
                        WriteTypedValue(entry.Value);
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
            BeginComposite('{', members.Length);
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
                            throw Error($"CLR member '{member.Name}' is not readable.");
                        }
                        clrValue = getter(value.Instance);
                    }
                    catch (TypedDataException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        throw Error($"CLR member '{member.Name}' could not be read.", exception);
                    }

                    try
                    {
                        memberValue = ConvertClrMemberValue(clrValue);
                    }
                    catch (Exception exception)
                    {
                        throw Error(
                            $"CLR member '{member.Name}' is not supported by the ATD contract.",
                            exception);
                    }
                }
                finally
                {
                    _path.Pop();
                }
                WriteMember(member.Name, memberValue, writable: true);
            }
            EndComposite('}', members.Length);
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
                var integer = (long)value;
                var number = (double)integer;
                if (number >= 9223372036854775808d || (long)number != integer)
                {
                    throw Error("CLR integer value cannot be represented exactly as an ATD Number.");
                }
                return ScriptDatum.FromNumber(number);
            }
            if (type == typeof(ulong))
            {
                var integer = (ulong)value;
                var number = (double)integer;
                if (number >= 18446744073709551616d || (ulong)number != integer)
                {
                    throw Error("CLR integer value cannot be represented exactly as an ATD Number.");
                }
                return ScriptDatum.FromNumber(number);
            }
            if (type == typeof(decimal))
            {
                var decimalValue = (decimal)value;
                var number = (double)decimalValue;
                if (!double.IsFinite(number) || (decimal)number != decimalValue)
                {
                    throw Error("CLR decimal value cannot be represented exactly as an ATD Number.");
                }
                return ScriptDatum.FromNumber(number);
            }
            return ClrMarshaller.ToDatum(value);
        }

        private string ResolveTypeName(ScriptDatum value, out ClrType clrRegistration)
        {
            clrRegistration = null;
            switch (value.Kind)
            {
                case ValueKind.Null: return null;
                case ValueKind.Boolean: return "Boolean";
                case ValueKind.Number:
                    if (!double.IsFinite(value.Number))
                    {
                        throw Error("ATD cannot serialize NaN or infinity.");
                    }
                    return "Number";
                case ValueKind.String: return "String";
                case ValueKind.Array:
                    if (value.Object is ScriptArray) return "Array";
                    break;
                case ValueKind.Date:
                    if (value.Object is ScriptDate) return "Date";
                    break;
                case ValueKind.Regex:
                    if (value.Object is ScriptRegex) return "Regex";
                    break;
                case ValueKind.Function:
                case ValueKind.Type:
                case ValueKind.ClrFunction:
                case ValueKind.ClrBonding:
                case ValueKind.Error:
                    throw Error($"ATD does not support values of kind '{value.Kind}'.");
            }

            var scriptObject = value.Object;
            switch (scriptObject)
            {
                case ScriptInt32Array: return "Int32Array";
                case ScriptInt8Array: return "Int8Array";
                case ScriptFloat64Array: return "Float64Array";
                case ScriptBooleanArray: return "BooleanArray";
                case ScriptArray: return "Array";
                case ScriptDate: return "Date";
                case ScriptRegex: return "Regex";
                case StringBuffer: return "StringBuffer";
                case ScriptPathValue: return "Path";
                case ScriptHashMap: return "HashMap";
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
                        throw Error(
                            $"CLR object type '{actualType?.FullName ?? "<null>"}' was not registered by the host.");
                    }
                    if (!IsUsableTypeAlias(alias))
                    {
                        throw Error($"Host alias '{alias}' cannot be represented as an ATD type name.");
                    }
                    return alias;
                case ScriptObject when scriptObject.GetType() == typeof(ScriptObject):
                    return "Object";
                default:
                    throw Error(
                        $"Runtime value '{scriptObject?.GetType().FullName ?? value.Kind.ToString()}' is not supported by ATD.");
            }
        }

        private void TrackReference(object value)
        {
            if (value == null) throw Error("ATD cannot serialize a null object reference.");
            if (!_visited.Add(value))
            {
                throw Error("ATD does not support circular or shared object references.");
            }
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
                _depth--;
                WriteIndent();
            }
            _builder.Append(closing);
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
            if (TypedDataPath.IsIdentifier(value) &&
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
            if (!double.IsFinite(value)) throw Error("ATD cannot serialize NaN or infinity.");
            Span<char> buffer = stackalloc char[32];
            if (value.TryFormat(buffer, out var written, "R", CultureInfo.InvariantCulture))
            {
                _builder.Append(buffer[..written]);
                return;
            }
            _builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private TypedDataException Error(string message, Exception innerException = null)
        {
            return new TypedDataException(
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
                throw Error($"ATD value depth exceeds the configured limit of {_maxDepth}.");
            }
            _valueDepth++;
        }

        private static bool IsBuiltInTypeName(string value)
        {
            return value is "Object" or "Array" or "String" or "Number" or "Boolean" or
                "StringBuffer" or "Date" or "Regex" or "Path" or "HashMap" or
                "Int32Array" or "Int8Array" or "Float64Array" or "BooleanArray";
        }

        private static bool IsUsableTypeAlias(string value)
        {
            return TypedDataPath.IsIdentifier(value) &&
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
