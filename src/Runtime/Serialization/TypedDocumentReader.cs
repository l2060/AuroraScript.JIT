using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Pool;
using AuroraScript.Runtime.Types;
using System;
using System.Buffers;
using System.Globalization;

namespace AuroraScript.Runtime.Serialization
{
    internal enum TypedDocumentPackedKind : byte
    {
        Int32,
        Int8,
        Float64,
        Boolean
    }

    internal readonly struct TypedDocumentMemberHeader
    {
        internal TypedDocumentMemberHeader(
            string name,
            bool readOnly,
            bool hasType,
            TypedDocumentToken typeToken,
            TypedDocumentToken nameToken)
        {
            Name = name;
            ReadOnly = readOnly;
            HasType = hasType;
            TypeToken = typeToken;
            NameToken = nameToken;
        }

        internal string Name { get; }
        internal bool ReadOnly { get; }
        internal bool HasType { get; }
        internal TypedDocumentToken TypeToken { get; }
        internal TypedDocumentToken NameToken { get; }
    }

    /// <summary>
    /// A pooled staging buffer for a packed primitive array. It never becomes part of
    /// the result object; the parser copies exactly once into the result's owned array.
    /// </summary>
    internal ref struct TypedDocumentPooledBuffer<T> where T : unmanaged
    {
        private T[] _items;
        private int _count;

        internal TypedDocumentPooledBuffer(int initialCapacity)
        {
            _items = ArrayPool<T>.Shared.Rent(Math.Max(4, initialCapacity));
            _count = 0;
        }

        internal int Count => _count;

        internal void Add(T value)
        {
            if (_count == _items.Length) Grow();
            _items[_count++] = value;
        }

        internal T[] ToArray()
        {
            if (_count == 0) return Array.Empty<T>();
            var result = new T[_count];
            _items.AsSpan(0, _count).CopyTo(result);
            return result;
        }

        public void Dispose()
        {
            var items = _items;
            _items = null;
            _count = 0;
            if (items != null) ArrayPool<T>.Shared.Return(items, clearArray: false);
        }

        private void Grow()
        {
            var replacement = ArrayPool<T>.Shared.Rent(_items.Length * 2);
            _items.AsSpan(0, _count).CopyTo(replacement);
            ArrayPool<T>.Shared.Return(_items, clearArray: false);
            _items = replacement;
        }
    }

    internal ref struct TypedDocumentReader
    {
        private readonly AuroraEngine _engine;
        private readonly string _sourceName;
        private readonly int _maxDepth;
        private TypedDocumentScanner _scanner;
        private TypedDocumentToken _current;
        private TypedDocumentToken _lookahead;
        private bool _hasLookahead;
        private TypedDocumentPath _path;
        private int _depth;

        internal TypedDocumentReader(
            AuroraEngine engine,
            string text,
            TypedDocumentOptions options,
            string sourceName)
        {
            _engine = engine;
            _sourceName = string.IsNullOrWhiteSpace(sourceName) ? "<tdoc>" : sourceName;
            _maxDepth = options.MaxDepth;
            _scanner = new TypedDocumentScanner(text);
            _current = _scanner.Read();
            _lookahead = default;
            _hasLookahead = false;
            _path = new TypedDocumentPath(16);
            _depth = 0;
        }

        internal ScriptDatum ReadDocument()
        {
            var result = ReadTypedValue();
            var trailing = Current();
            if (trailing.Kind != TypedDocumentTokenKind.EndOfFile)
            {
                throw Error(trailing, "Only one root value is allowed.");
            }
            return result;
        }

        internal void Dispose()
        {
            _path.Dispose();
        }

        private ScriptDatum ReadTypedValue()
        {
            var token = Current();
            EnterValue(token);
            try
            {
                if (token.Kind != TypedDocumentTokenKind.Identifier)
                {
                    return ReadInferredValue();
                }

                Advance();
                return ReadExplicitValue(token);
            }
            finally
            {
                _depth--;
            }
        }

        private ScriptDatum ReadExplicitValue(TypedDocumentToken typeToken)
        {
            if (TypeEquals(typeToken, "Object")) return ReadObject();
            if (TypeEquals(typeToken, "Array")) return ReadArray();
            if (TypeEquals(typeToken, "String")) return ReadStringValue("String");
            if (TypeEquals(typeToken, "Number")) return ReadNumberValue("Number");
            if (TypeEquals(typeToken, "Boolean")) return ReadBooleanValue("Boolean");
            if (TypeEquals(typeToken, "StringBuffer"))
            {
                return ScriptDatum.FromObject(new StringBuffer(ReadRequiredString("StringBuffer")));
            }
            if (TypeEquals(typeToken, "Date")) return ReadDate();
            if (TypeEquals(typeToken, "Regex")) return ReadRegex();
            if (TypeEquals(typeToken, "Path"))
            {
                return ScriptDatum.FromObject(new ScriptPathValue(ReadRequiredString("Path")));
            }
            if (TypeEquals(typeToken, "HashMap")) return ReadHashMap();
            if (TypeEquals(typeToken, "Int32Array")) return ReadPackedArray(TypedDocumentPackedKind.Int32);
            if (TypeEquals(typeToken, "Int8Array")) return ReadPackedArray(TypedDocumentPackedKind.Int8);
            if (TypeEquals(typeToken, "Float64Array")) return ReadPackedArray(TypedDocumentPackedKind.Float64);
            if (TypeEquals(typeToken, "BooleanArray")) return ReadPackedArray(TypedDocumentPackedKind.Boolean);

            var alias = _scanner.GetIdentifier(typeToken);
            if (!_engine.ClrRegistry.TryGetClrType(alias, out var registration))
            {
                throw Error(typeToken, $"Unknown type '{alias}'.");
            }
            return ReadRegisteredObject(alias, registration, typeToken);
        }

        private ScriptDatum ReadInferredValue()
        {
            var token = Current();
            switch (token.Kind)
            {
                case TypedDocumentTokenKind.Null:
                    Advance();
                    return ScriptDatum.Null;
                case TypedDocumentTokenKind.True:
                    Advance();
                    return ScriptDatum.True;
                case TypedDocumentTokenKind.False:
                    Advance();
                    return ScriptDatum.False;
                case TypedDocumentTokenKind.Number:
                    Advance();
                    return ScriptDatum.FromNumber(token.Number);
                case TypedDocumentTokenKind.String:
                    var text = _scanner.GetString(token);
                    Advance();
                    return ScriptDatum.FromString(text);
                case TypedDocumentTokenKind.LeftBracket:
                    return ReadArray();
                case TypedDocumentTokenKind.LeftBrace:
                    return ReadObject();
                default:
                    throw Error(token, "Expected a data value.");
            }
        }

        private ScriptDatum ReadObject()
        {
            Expect(TypedDocumentTokenKind.LeftBrace, "Type 'Object' requires an object value.");
            var result = new ScriptObject();
            if (Match(TypedDocumentTokenKind.RightBrace))
            {
                return ScriptDatum.FromObject(result);
            }

            while (true)
            {
                var header = ReadMemberHeader();
                _path.PushProperty(header.Name);
                try
                {
                    if (result.HasOwnProperty(header.Name))
                    {
                        throw Error(header.NameToken, $"Duplicate property '{header.Name}'.");
                    }
                    var value = ReadMemberValue(header);
                    result.Define(header.Name, value, writeable: !header.ReadOnly, enumerable: true);
                }
                finally
                {
                    _path.Pop();
                }

                if (ReadObjectSeparator()) break;
            }
            return ScriptDatum.FromObject(result);
        }

        private ScriptDatum ReadArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'Array' requires an array value.");
            var result = new ScriptArray();
            if (Match(TypedDocumentTokenKind.RightBracket))
            {
                return ScriptDatum.FromArray(result);
            }

            var index = 0;
            while (true)
            {
                _path.PushIndex(index);
                try
                {
                    result.Push(ReadTypedValue());
                }
                finally
                {
                    _path.Pop();
                }
                index++;
                if (ReadArraySeparator()) break;
            }
            return ScriptDatum.FromArray(result);
        }

        private ScriptDatum ReadStringValue(string typeName)
        {
            return ScriptDatum.FromString(ReadRequiredString(typeName));
        }

        private ScriptDatum ReadNumberValue(string typeName)
        {
            var token = Current();
            if (token.Kind != TypedDocumentTokenKind.Number)
            {
                throw Error(token, $"Type '{typeName}' requires a number value.");
            }
            Advance();
            return ScriptDatum.FromNumber(token.Number);
        }

        private ScriptDatum ReadBooleanValue(string typeName)
        {
            var token = Current();
            if (token.Kind == TypedDocumentTokenKind.True)
            {
                Advance();
                return ScriptDatum.True;
            }
            if (token.Kind == TypedDocumentTokenKind.False)
            {
                Advance();
                return ScriptDatum.False;
            }
            throw Error(token, $"Type '{typeName}' requires a boolean value.");
        }

        private string ReadRequiredString(string typeName)
        {
            var token = Current();
            if (token.Kind != TypedDocumentTokenKind.String)
            {
                throw Error(token, $"Type '{typeName}' requires a string value.");
            }
            var value = _scanner.GetString(token);
            Advance();
            return value;
        }

        private ScriptDatum ReadDate()
        {
            var token = Current();
            if (token.Kind == TypedDocumentTokenKind.Number)
            {
                var numericTicks = token.Number;
                if (Math.Truncate(numericTicks) != numericTicks ||
                    !_scanner.TryGetInt64Exact(token, out var ticks) ||
                    ticks < DateTimeOffset.MinValue.Ticks ||
                    ticks > DateTimeOffset.MaxValue.Ticks)
                {
                    throw Error(
                        token,
                        $"Date ticks must be an integer in the range 0..{DateTimeOffset.MaxValue.Ticks}.");
                }

                Advance();
                return ScriptDatum.FromDate(new ScriptDate(ticks));
            }
            if (token.Kind != TypedDocumentTokenKind.String)
            {
                throw Error(token, "Type 'Date' requires a formatted string or an integer ticks value.");
            }

            var text = ReadRequiredString("Date");
            var format = _engine.Options.Runtime.DateTimeFormat;
            if (string.IsNullOrEmpty(format))
            {
                throw Error(token, "EngineOptions.Runtime.DateTimeFormat cannot be null or empty.");
            }
            try
            {
                if (DateTimeOffset.TryParseExact(
                        text,
                        format,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var value))
                {
                    return ScriptDatum.FromDate(new ScriptDate(value));
                }
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                throw Error(token, $"Invalid EngineOptions.Runtime.DateTimeFormat '{format}'.", exception);
            }
            throw Error(token, $"Date value must match EngineOptions.Runtime.DateTimeFormat '{format}'.");
        }

        private ScriptDatum ReadRegex()
        {
            Expect(TypedDocumentTokenKind.LeftBrace, "Type 'Regex' requires an object value.");
            string pattern = null;
            string flags = null;
            if (!Match(TypedDocumentTokenKind.RightBrace))
            {
                while (true)
                {
                    var header = ReadMemberHeader();
                    _path.PushProperty(header.Name);
                    try
                    {
                        if (header.ReadOnly)
                        {
                            throw Error(header.NameToken, "Regex fields cannot be readonly.");
                        }
                        if (header.Name is not ("pattern" or "flags"))
                        {
                            throw Error(header.NameToken, $"Unknown Regex field '{header.Name}'.");
                        }

                        var value = ReadMemberValue(header);
                        if (value.Kind != ValueKind.String)
                        {
                            throw Error(header.NameToken, "Regex fields must be strings.");
                        }
                        if (header.Name == "pattern")
                        {
                            if (pattern != null) throw Error(header.NameToken, "Duplicate property 'pattern'.");
                            pattern = value.StringText;
                        }
                        else
                        {
                            if (flags != null) throw Error(header.NameToken, "Duplicate property 'flags'.");
                            flags = value.StringText;
                        }
                    }
                    finally
                    {
                        _path.Pop();
                    }
                    if (ReadObjectSeparator()) break;
                }
            }

            if (pattern == null) throw Error(Current(), "Regex requires a 'pattern' field.");
            if (flags == null) throw Error(Current(), "Regex requires a 'flags' field.");
            try
            {
                return ScriptDatum.FromRegex(RegexManager.Resolve(pattern, flags));
            }
            catch (Exception exception) when (exception is ArgumentException or AuroraRuntimeException)
            {
                throw Error(Current(), exception.Message, exception);
            }
        }

        private ScriptDatum ReadHashMap()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'HashMap' requires an array value.");
            var result = new ScriptHashMap();
            if (Match(TypedDocumentTokenKind.RightBracket))
            {
                return ScriptDatum.FromObject(result);
            }

            var entryIndex = 0;
            while (true)
            {
                _path.PushIndex(entryIndex);
                try
                {
                    if (Current().Kind == TypedDocumentTokenKind.Identifier)
                    {
                        var pairType = Current();
                        if (!TypeEquals(pairType, "Array"))
                        {
                            throw Error(pairType, "Each HashMap entry must be a two-element array.");
                        }
                        Advance();
                    }
                    Expect(TypedDocumentTokenKind.LeftBracket, "Each HashMap entry must be a two-element array.");

                    ScriptDatum key;
                    ScriptDatum value;
                    _path.PushIndex(0);
                    try
                    {
                        key = ReadTypedValue();
                    }
                    finally
                    {
                        _path.Pop();
                    }
                    Expect(TypedDocumentTokenKind.Comma, "HashMap entries require a key and value.");
                    _path.PushIndex(1);
                    try
                    {
                        value = ReadTypedValue();
                    }
                    finally
                    {
                        _path.Pop();
                    }
                    if (Match(TypedDocumentTokenKind.Comma))
                    {
                        Expect(TypedDocumentTokenKind.RightBracket, "HashMap entries contain exactly two values.");
                    }
                    else
                    {
                        Expect(TypedDocumentTokenKind.RightBracket, "HashMap entries contain exactly two values.");
                    }
                    result.Put(key, value);
                }
                finally
                {
                    _path.Pop();
                }

                entryIndex++;
                if (ReadArraySeparator()) break;
            }
            return ScriptDatum.FromObject(result);
        }

        private ScriptDatum ReadPackedArray(TypedDocumentPackedKind kind)
        {
            return kind switch
            {
                TypedDocumentPackedKind.Int32 => ReadInt32PackedArray(),
                TypedDocumentPackedKind.Int8 => ReadInt8PackedArray(),
                TypedDocumentPackedKind.Float64 => ReadFloat64PackedArray(),
                _ => ReadBooleanPackedArray()
            };
        }

        private ScriptDatum ReadInt32PackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'Int32Array' requires an array value.");
            var buffer = new TypedDocumentPooledBuffer<int>(8);
            try
            {
                if (!Match(TypedDocumentTokenKind.RightBracket))
                {
                    while (true)
                    {
                        _path.PushIndex(buffer.Count);
                        try
                        {
                            var location = Current();
                            var value = ReadTypedValue();
                            ValidatePackedElement(TypedDocumentPackedKind.Int32, value, location);
                            buffer.Add((int)value.Number);
                        }
                        finally
                        {
                            _path.Pop();
                        }
                        if (ReadArraySeparator()) break;
                    }
                }

                return ScriptDatum.FromObject(new ScriptInt32Array(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private ScriptDatum ReadInt8PackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'Int8Array' requires an array value.");
            var buffer = new TypedDocumentPooledBuffer<sbyte>(8);
            try
            {
                if (!Match(TypedDocumentTokenKind.RightBracket))
                {
                    while (true)
                    {
                        _path.PushIndex(buffer.Count);
                        try
                        {
                            var location = Current();
                            var value = ReadTypedValue();
                            ValidatePackedElement(TypedDocumentPackedKind.Int8, value, location);
                            buffer.Add((sbyte)value.Number);
                        }
                        finally
                        {
                            _path.Pop();
                        }
                        if (ReadArraySeparator()) break;
                    }
                }

                return ScriptDatum.FromObject(new ScriptInt8Array(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private ScriptDatum ReadFloat64PackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'Float64Array' requires an array value.");
            var buffer = new TypedDocumentPooledBuffer<double>(8);
            try
            {
                if (!Match(TypedDocumentTokenKind.RightBracket))
                {
                    while (true)
                    {
                        _path.PushIndex(buffer.Count);
                        try
                        {
                            var location = Current();
                            var value = ReadTypedValue();
                            ValidatePackedElement(TypedDocumentPackedKind.Float64, value, location);
                            buffer.Add(value.Number);
                        }
                        finally
                        {
                            _path.Pop();
                        }
                        if (ReadArraySeparator()) break;
                    }
                }

                return ScriptDatum.FromObject(new ScriptFloat64Array(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private ScriptDatum ReadBooleanPackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'BooleanArray' requires an array value.");
            var buffer = new TypedDocumentPooledBuffer<bool>(8);
            try
            {
                if (!Match(TypedDocumentTokenKind.RightBracket))
                {
                    while (true)
                    {
                        _path.PushIndex(buffer.Count);
                        try
                        {
                            var location = Current();
                            var value = ReadTypedValue();
                            ValidatePackedElement(TypedDocumentPackedKind.Boolean, value, location);
                            buffer.Add(value.Boolean);
                        }
                        finally
                        {
                            _path.Pop();
                        }
                        if (ReadArraySeparator()) break;
                    }
                }

                return ScriptDatum.FromObject(new ScriptBooleanArray(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private ScriptDatum ReadRegisteredObject(
            string alias,
            ClrType registration,
            TypedDocumentToken typeToken)
        {
            var type = registration._descriptor.Type;
            if (!type.IsClass ||
                type.IsAbstract ||
                type.IsArray ||
                type.ContainsGenericParameters ||
                typeof(Delegate).IsAssignableFrom(type))
            {
                throw Error(typeToken, $"Registered type '{alias}' is not a constructible object type.");
            }
            if ((registration._access & TypeAccess.Constructor) == 0)
            {
                throw Error(typeToken, $"Registered type '{alias}' does not allow construction.");
            }
            var contract = registration._descriptor.DataContract;
            var factory = contract.Factory;
            if (factory == null)
            {
                throw Error(typeToken, $"Registered type '{alias}' requires a public parameterless constructor.");
            }

            Expect(TypedDocumentTokenKind.LeftBrace, $"Registered type '{alias}' requires an object value.");
            object instance;
            try
            {
                instance = factory();
            }
            catch (Exception exception)
            {
                throw Error(typeToken, $"Could not construct registered type '{alias}'.", exception);
            }

            if (!Match(TypedDocumentTokenKind.RightBrace))
            {
                var seenMask = 0UL;
                ulong[] rentedSeen = null;
                var seenWordCount = (contract.Members.Length + 63) >> 6;
                if (seenWordCount > 1)
                {
                    rentedSeen = ArrayPool<ulong>.Shared.Rent(seenWordCount);
                    Array.Clear(rentedSeen, 0, seenWordCount);
                }
                try
                {
                    while (true)
                    {
                        var header = ReadMemberHeader();
                        _path.PushProperty(header.Name);
                        try
                        {
                            if (!contract.TryGetMember(header.Name, out var member))
                            {
                                throw Error(
                                    header.NameToken,
                                    $"Unknown field '{header.Name}' for registered type '{alias}'.");
                            }

                            var duplicate = false;
                            if (rentedSeen == null)
                            {
                                var bit = 1UL << member.Index;
                                duplicate = (seenMask & bit) != 0;
                                seenMask |= bit;
                            }
                            else
                            {
                                var wordIndex = member.Index >> 6;
                                var bit = 1UL << (member.Index & 63);
                                duplicate = (rentedSeen[wordIndex] & bit) != 0;
                                rentedSeen[wordIndex] |= bit;
                            }
                            if (duplicate)
                            {
                                throw Error(header.NameToken, $"Duplicate property '{header.Name}'.");
                            }
                            if (header.ReadOnly)
                            {
                                throw Error(
                                    header.NameToken,
                                    "readonly is not supported by the default CLR object contract.");
                            }
                            var value = ReadMemberValue(header);
                            AssignRegisteredMember(instance, member, header, value);
                        }
                        finally
                        {
                            _path.Pop();
                        }
                        if (ReadObjectSeparator()) break;
                    }
                }
                finally
                {
                    if (rentedSeen != null)
                    {
                        Array.Clear(rentedSeen, 0, seenWordCount);
                        ArrayPool<ulong>.Shared.Return(rentedSeen);
                    }
                }
            }

            return ScriptDatum.FromObject(new ClrInstanceObject(registration._descriptor, instance));
        }

        private void AssignRegisteredMember(
            object instance,
            ClrDataMember member,
            TypedDocumentMemberHeader header,
            ScriptDatum value)
        {
            var setter = member.Setter;
            if (setter == null)
            {
                throw Error(header.NameToken, $"CLR member '{header.Name}' is not writable.");
            }
            if (!TryConvertClrValue(value, member.Type, out var converted))
            {
                throw Error(
                    header.NameToken,
                    $"Value cannot be converted to CLR type '{member.Type.FullName}'.");
            }
            try
            {
                setter.Setter(instance, converted);
            }
            catch (Exception exception)
            {
                throw Error(header.NameToken, "CLR member assignment failed.", exception);
            }
        }

        private ScriptDatum ReadMemberValue(TypedDocumentMemberHeader header)
        {
            var token = header.HasType ? header.TypeToken : Current();
            EnterValue(token);
            try
            {
                return header.HasType ? ReadExplicitValue(header.TypeToken) : ReadInferredValue();
            }
            finally
            {
                _depth--;
            }
        }

        private void EnterValue(TypedDocumentToken token)
        {
            if (_depth >= _maxDepth)
            {
                throw Error(token, $"TDoc value depth exceeds the configured limit of {_maxDepth}.");
            }
            _depth++;
        }

        private TypedDocumentMemberHeader ReadMemberHeader()
        {
            var readOnly = Match(TypedDocumentTokenKind.ReadOnly);
            var first = Current();
            if (first.Kind is not (TypedDocumentTokenKind.Identifier or TypedDocumentTokenKind.String))
            {
                throw Error(first, "Expected a property name.");
            }
            Advance();

            if (first.Kind == TypedDocumentTokenKind.Identifier &&
                Current().Kind == TypedDocumentTokenKind.Identifier)
            {
                var nameToken = Current();
                var name = _scanner.GetIdentifier(nameToken);
                Advance();
                return new TypedDocumentMemberHeader(name, readOnly, true, first, nameToken);
            }
            if (first.Kind == TypedDocumentTokenKind.Identifier &&
                Current().Kind == TypedDocumentTokenKind.String &&
                IsRawValueStart(PeekNextKind()))
            {
                var nameToken = Current();
                var name = _scanner.GetString(nameToken);
                Advance();
                return new TypedDocumentMemberHeader(name, readOnly, true, first, nameToken);
            }

            var propertyName = first.Kind == TypedDocumentTokenKind.Identifier
                ? _scanner.GetIdentifier(first)
                : _scanner.GetString(first);
            return new TypedDocumentMemberHeader(propertyName, readOnly, false, default, first);
        }

        private bool ReadObjectSeparator()
        {
            if (Match(TypedDocumentTokenKind.Comma))
            {
                return Match(TypedDocumentTokenKind.RightBrace);
            }
            if (Match(TypedDocumentTokenKind.RightBrace)) return true;
            throw Error(Current(), "Expected ',' or '}'.");
        }

        private bool ReadArraySeparator()
        {
            if (Match(TypedDocumentTokenKind.Comma))
            {
                return Match(TypedDocumentTokenKind.RightBracket);
            }
            if (Match(TypedDocumentTokenKind.RightBracket)) return true;
            throw Error(Current(), "Expected ',' or ']'.");
        }

        private void ValidatePackedElement(
            TypedDocumentPackedKind kind,
            ScriptDatum value,
            TypedDocumentToken location)
        {
            if (kind == TypedDocumentPackedKind.Boolean)
            {
                if (value.Kind != ValueKind.Boolean)
                {
                    throw Error(location, "BooleanArray elements must be booleans.");
                }
                return;
            }
            if (value.Kind != ValueKind.Number || !double.IsFinite(value.Number))
            {
                throw Error(location, $"{PackedTypeName(kind)} elements must be finite numbers.");
            }
            if (kind == TypedDocumentPackedKind.Float64) return;
            if (Math.Truncate(value.Number) != value.Number)
            {
                throw Error(location, $"{PackedTypeName(kind)} elements must be integers.");
            }

            var minimum = kind == TypedDocumentPackedKind.Int8 ? sbyte.MinValue : int.MinValue;
            var maximum = kind == TypedDocumentPackedKind.Int8 ? sbyte.MaxValue : int.MaxValue;
            if (value.Number < minimum || value.Number > maximum)
            {
                throw Error(location, $"Integer value must be in the range {minimum}..{maximum}.");
            }
        }

        private static bool TryConvertClrValue(ScriptDatum value, Type targetType, out object converted)
        {
            if (value.Object != null && targetType.IsInstanceOfType(value.Object))
            {
                converted = value.Object;
                return true;
            }
            if (value.Object is ScriptDate date)
            {
                if (targetType == typeof(DateTimeOffset) || targetType == typeof(DateTimeOffset?))
                {
                    converted = date.DateTime;
                    return true;
                }
                if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                {
                    converted = date.DateTime.DateTime;
                    return true;
                }
            }

            var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (effectiveType.IsEnum)
            {
                if (value.Kind == ValueKind.Number &&
                    TryConvertClrNumber(value.Number, Enum.GetUnderlyingType(effectiveType), out var underlying))
                {
                    converted = Enum.ToObject(effectiveType, underlying);
                    return true;
                }
                converted = null;
                return false;
            }
            if (IsClrNumericType(effectiveType))
            {
                if (value.Kind == ValueKind.Number)
                {
                    return TryConvertClrNumber(value.Number, effectiveType, out converted);
                }
                converted = null;
                return false;
            }
            if (effectiveType == typeof(bool))
            {
                if (value.Kind == ValueKind.Boolean)
                {
                    converted = value.Boolean;
                    return true;
                }
                converted = null;
                return false;
            }
            if (effectiveType == typeof(char))
            {
                if (value.Kind == ValueKind.String && value.StringText.Length == 1)
                {
                    converted = value.StringText[0];
                    return true;
                }
                converted = null;
                return false;
            }
            return ClrMarshaller.TryConvertArgument(in value, targetType, out converted);
        }

        private static bool IsClrNumericType(Type type)
        {
            return Type.GetTypeCode(type) is
                TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or
                TypeCode.UInt64 or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or
                TypeCode.Decimal or TypeCode.Double or TypeCode.Single;
        }

        private static bool TryConvertClrNumber(double value, Type type, out object converted)
        {
            if (!double.IsFinite(value))
            {
                converted = null;
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Double:
                    converted = value;
                    return true;
                case TypeCode.Single:
                    var single = (float)value;
                    converted = single;
                    return float.IsFinite(single);
                case TypeCode.Decimal:
                    try
                    {
                        converted = (decimal)value;
                        return true;
                    }
                    catch (OverflowException)
                    {
                        converted = null;
                        return false;
                    }
            }

            if (Math.Truncate(value) != value)
            {
                converted = null;
                return false;
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte when value >= sbyte.MinValue && value <= sbyte.MaxValue:
                    converted = (sbyte)value;
                    return true;
                case TypeCode.Byte when value >= byte.MinValue && value <= byte.MaxValue:
                    converted = (byte)value;
                    return true;
                case TypeCode.Int16 when value >= short.MinValue && value <= short.MaxValue:
                    converted = (short)value;
                    return true;
                case TypeCode.UInt16 when value >= ushort.MinValue && value <= ushort.MaxValue:
                    converted = (ushort)value;
                    return true;
                case TypeCode.Int32 when value >= int.MinValue && value <= int.MaxValue:
                    converted = (int)value;
                    return true;
                case TypeCode.UInt32 when value >= uint.MinValue && value <= uint.MaxValue:
                    converted = (uint)value;
                    return true;
                case TypeCode.Int64 when value >= -9223372036854775808d && value < 9223372036854775808d:
                    converted = (long)value;
                    return true;
                case TypeCode.UInt64 when value >= 0d && value < 18446744073709551616d:
                    converted = (ulong)value;
                    return true;
                default:
                    converted = null;
                    return false;
            }
        }

        private TypedDocumentToken Current()
        {
            if (_current.Kind == TypedDocumentTokenKind.Bad)
            {
                throw ScanError(_current);
            }
            return _current;
        }

        private TypedDocumentTokenKind PeekNextKind()
        {
            if (!_hasLookahead)
            {
                _lookahead = _scanner.Read();
                _hasLookahead = true;
            }
            return _lookahead.Kind;
        }

        private void Advance()
        {
            _ = Current();
            if (_hasLookahead)
            {
                _current = _lookahead;
                _lookahead = default;
                _hasLookahead = false;
            }
            else
            {
                _current = _scanner.Read();
            }
        }

        private bool Match(TypedDocumentTokenKind kind)
        {
            if (Current().Kind != kind) return false;
            Advance();
            return true;
        }

        private TypedDocumentToken Expect(TypedDocumentTokenKind kind, string message)
        {
            var token = Current();
            if (token.Kind != kind) throw Error(token, message);
            Advance();
            return token;
        }

        private bool TypeEquals(TypedDocumentToken token, string typeName)
        {
            return _scanner.TextEquals(token, typeName);
        }

        private TypedDocumentException ScanError(TypedDocumentToken token)
        {
            var message = token.Error switch
            {
                TypedDocumentScanError.UnexpectedCharacter => $"Unexpected character '{token.ErrorCharacter}'.",
                TypedDocumentScanError.ScriptMarkerNotAllowed => "Independent TDoc documents do not accept script markers.",
                TypedDocumentScanError.UnterminatedString => "Unterminated string literal.",
                TypedDocumentScanError.UnterminatedComment => "Unterminated block comment.",
                TypedDocumentScanError.InvalidEscape => $"Unsupported escape sequence '\\{token.ErrorCharacter}'.",
                TypedDocumentScanError.InvalidUnicodeEscape => "Invalid Unicode escape sequence.",
                TypedDocumentScanError.InvalidHexEscape => "Invalid hexadecimal escape sequence.",
                TypedDocumentScanError.InvalidNumber => "Invalid number.",
                _ => "Invalid TDoc token."
            };
            return Error(token, message);
        }

        private TypedDocumentException Error(
            TypedDocumentToken token,
            string message,
            Exception innerException = null)
        {
            return new TypedDocumentException(
                message,
                _sourceName,
                token.Line,
                token.Column,
                _path.Format(),
                innerException);
        }

        private static bool IsRawValueStart(TypedDocumentTokenKind kind)
        {
            return kind is TypedDocumentTokenKind.Null or
                TypedDocumentTokenKind.True or
                TypedDocumentTokenKind.False or
                TypedDocumentTokenKind.Number or
                TypedDocumentTokenKind.String or
                TypedDocumentTokenKind.LeftBracket or
                TypedDocumentTokenKind.LeftBrace;
        }

        private static string PackedTypeName(TypedDocumentPackedKind kind)
        {
            return kind switch
            {
                TypedDocumentPackedKind.Int32 => "Int32Array",
                TypedDocumentPackedKind.Int8 => "Int8Array",
                TypedDocumentPackedKind.Float64 => "Float64Array",
                _ => "BooleanArray"
            };
        }
    }
}
