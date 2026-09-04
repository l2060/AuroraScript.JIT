using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Pool;
using AuroraScript.Runtime.Types;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime.Serialization
{
    internal enum TypedDocumentPackedKind : byte
    {
        Int32,
        Int8,
        Float32,
        Float64,
        Boolean,
        UInt8,
        Int16,
        UInt16,
        UInt32,
        Int64,
        UInt64
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
            if (TypeEquals(typeToken, "Null"))
            {
                if (!Match(TypedDocumentTokenKind.Null))
                {
                    throw Error(Current(), "Type 'Null' requires a null value.");
                }
                return ScriptDatum.Null;
            }
            if (TypeEquals(typeToken, "Object")) return ReadObject();
            if (TypeEquals(typeToken, "Array")) return ReadArray();
            if (TypeEquals(typeToken, "String")) return ReadStringValue("String");
            if (TypeEquals(typeToken, "Number")) return ReadNumberValue("Number");
            if (TypeEquals(typeToken, "Int64")) return ReadInt64Value();
            if (TypeEquals(typeToken, "UInt64")) return ReadUInt64Value();
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
            if (TypeEquals(typeToken, "Float32Array")) return ReadPackedArray(TypedDocumentPackedKind.Float32);
            if (TypeEquals(typeToken, "Float64Array")) return ReadPackedArray(TypedDocumentPackedKind.Float64);
            if (TypeEquals(typeToken, "BooleanArray")) return ReadPackedArray(TypedDocumentPackedKind.Boolean);
            if (TypeEquals(typeToken, "UInt8Array")) return ReadPackedArray(TypedDocumentPackedKind.UInt8);
            if (TypeEquals(typeToken, "Int16Array")) return ReadPackedArray(TypedDocumentPackedKind.Int16);
            if (TypeEquals(typeToken, "UInt16Array")) return ReadPackedArray(TypedDocumentPackedKind.UInt16);
            if (TypeEquals(typeToken, "UInt32Array")) return ReadPackedArray(TypedDocumentPackedKind.UInt32);
            if (TypeEquals(typeToken, "Int64Array")) return ReadPackedArray(TypedDocumentPackedKind.Int64);
            if (TypeEquals(typeToken, "UInt64Array")) return ReadPackedArray(TypedDocumentPackedKind.UInt64);

            var alias = _scanner.GetIdentifier(typeToken);
            if (_engine.TypedDocuments.TryGet(alias, out var native))
            {
                return ReadNativeTypedDocument(alias, native, typeToken);
            }
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

        private ScriptDatum ReadInt64Value()
        {
            var token = Current();
            if (token.Kind != TypedDocumentTokenKind.Number ||
                !_scanner.TryGetInt64Exact(token, out var value))
            {
                throw Error(token, "Type 'Int64' requires a signed 64-bit integer value.");
            }
            Advance();
            return ScriptDatum.FromInt64(value);
        }

        private ScriptDatum ReadUInt64Value()
        {
            var token = Current();
            if (token.Kind != TypedDocumentTokenKind.Number ||
                !_scanner.TryGetUInt64Exact(token, out var value))
            {
                throw Error(token, "Type 'UInt64' requires an unsigned 64-bit integer value.");
            }
            Advance();
            return ScriptDatum.FromUInt64(value);
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
                if (TypedDocumentBinder.TryParseDate(text, format, out var value))
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
                TypedDocumentPackedKind.Float32 => ReadFloat32PackedArray(),
                TypedDocumentPackedKind.Float64 => ReadFloat64PackedArray(),
                TypedDocumentPackedKind.Boolean => ReadBooleanPackedArray(),
                TypedDocumentPackedKind.UInt8 => ReadUInt8PackedArray(),
                TypedDocumentPackedKind.Int16 => ReadInt16PackedArray(),
                TypedDocumentPackedKind.UInt16 => ReadUInt16PackedArray(),
                TypedDocumentPackedKind.UInt32 => ReadUInt32PackedArray(),
                TypedDocumentPackedKind.Int64 => ReadInt64PackedArray(),
                TypedDocumentPackedKind.UInt64 => ReadUInt64PackedArray(),
                _ => throw new InvalidOperationException("Unknown TDoc packed-array kind.")
            };
        }

        private ScriptDatum ReadInt32PackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'Int32Array' requires an array value.");
            if (Current().Kind != TypedDocumentTokenKind.RightBracket)
            {
                var first = CurrentPacked(0);
                EnsurePackedElementDepth(first, 0);
                if (!_hasLookahead &&
                    first.Kind == TypedDocumentTokenKind.Number &&
                    _scanner.TryReadEntireSimpleInt32Array(first, out var directItems))
                {
                    AdvancePacked();
                    Expect(TypedDocumentTokenKind.RightBracket, "Expected ']'.");
                    return ScriptDatum.FromObject(new ScriptInt32Array(directItems));
                }
            }
            var buffer = new TypedDocumentPooledBuffer<int>(8);
            try
            {
                if (!Match(TypedDocumentTokenKind.RightBracket))
                {
                    EnsurePackedElementDepth(CurrentPacked(0), 0);
                    var index = 0;
                    while (true)
                    {
                        if (TryReadRawPackedNumber(index, out var location))
                        {
                            buffer.Add((int)ReadRawPackedNumber(
                                TypedDocumentPackedKind.Int32,
                                index,
                                location));
                        }
                        else
                        {
                            _path.PushIndex(index);
                            try
                            {
                                location = Current();
                                var value = ReadTypedValue();
                                ValidatePackedElement(TypedDocumentPackedKind.Int32, value, location);
                                TryGetPackedInt64(value, out var integer);
                                buffer.Add((int)integer);
                            }
                            finally
                            {
                                _path.Pop();
                            }
                        }
                        index++;
                        if (ReadPackedArraySeparator()) break;
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
            if (Match(TypedDocumentTokenKind.RightBracket))
            {
                return ScriptDatum.FromObject(new ScriptInt8Array(Array.Empty<sbyte>()));
            }

            EnsurePackedElementDepth(CurrentPacked(0), 0);
            var first = CurrentPacked(0);
            if (!_hasLookahead &&
                first.Kind == TypedDocumentTokenKind.Number &&
                first.Length == 1 &&
                // Verify the complete compact tail before allocating so the
                // returned packed array is the final owned storage.
                _scanner.TryReadEntireCompactInt8Array((sbyte)first.Number, out var compactItems))
            {
                AdvancePacked();
                Expect(TypedDocumentTokenKind.RightBracket, "Expected ']'.");
                return ScriptDatum.FromObject(new ScriptInt8Array(compactItems));
            }

            var buffer = new TypedDocumentPooledBuffer<sbyte>(8);
            try
            {
                var index = 0;
                while (true)
                {
                    // A large packed array is overwhelmingly made up of raw numeric
                    // tokens.  Avoid materializing a ScriptDatum and entering the
                    // general value dispatcher for that hot path.  The fallback
                    // still accepts all legal TDoc element forms (including
                    // `Number n`) and retains the full validation/error behavior.
                    var token = CurrentPacked(index);
                    if (!_hasLookahead &&
                        token.Kind == TypedDocumentTokenKind.Number &&
                        token.Length == 1)
                    {
                        buffer.Add((sbyte)token.Number);
                        index++;
                        while (_scanner.TryReadCompactSingleDigitAfterComma(out var element))
                        {
                            buffer.Add(element);
                            index++;
                        }
                        AdvancePacked();
                        if (ReadPackedArraySeparator()) break;
                        continue;
                    }
                    if (TryReadRawInt8Element(index, out var rawElement))
                    {
                        buffer.Add(rawElement);
                    }
                    else
                    {
                        _path.PushIndex(index);
                        try
                        {
                            var location = Current();
                            var value = ReadTypedValue();
                            ValidatePackedElement(TypedDocumentPackedKind.Int8, value, location);
                            TryGetPackedInt64(value, out var integer);
                            buffer.Add((sbyte)integer);
                        }
                        finally
                        {
                            _path.Pop();
                        }
                    }
                    index++;
                    if (ReadPackedArraySeparator()) break;
                }

                return ScriptDatum.FromObject(new ScriptInt8Array(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryReadRawInt8Element(int index, out sbyte value)
        {
            if (!TryReadRawPackedNumber(index, out var token))
            {
                value = 0;
                return false;
            }

            if (token.Length == 1)
            {
                AdvancePacked();
                value = (sbyte)token.Number;
                return true;
            }

            value = (sbyte)ReadRawPackedNumber(TypedDocumentPackedKind.Int8, index, token);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryReadRawPackedNumber(int index, out TypedDocumentToken token)
        {
            token = CurrentPacked(index);
            if (token.Kind == TypedDocumentTokenKind.Identifier && TypeEquals(token, "Number"))
            {
                AdvancePacked();
                token = CurrentPacked(index);
                if (token.Kind != TypedDocumentTokenKind.Number)
                {
                    throw PackedElementError(token, index, "Type 'Number' requires a number value.");
                }
                return true;
            }
            return token.Kind == TypedDocumentTokenKind.Number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double ReadRawPackedNumber(
            TypedDocumentPackedKind kind,
            int index,
            TypedDocumentToken token)
        {
            var number = token.Number;
            // A one-character numeric token can only be a decimal digit.  It is
            // therefore finite, integral, and within every packed numeric range.
            // Keep the checks for multi-character forms (signs, decimals, and
            // wide values) where they are actually needed.
            if (token.Length == 1)
            {
                AdvancePacked();
                return number;
            }
            if (!double.IsFinite(number))
            {
                throw PackedElementError(token, index, $"{PackedTypeName(kind)} elements must be finite numbers.");
            }
            if (kind is not (TypedDocumentPackedKind.Float32 or TypedDocumentPackedKind.Float64) && Math.Truncate(number) != number)
            {
                throw PackedElementError(token, index, $"{PackedTypeName(kind)} elements must be integers.");
            }
            if (!TypedDocumentBinder.IsPackedRange(kind, number))
            {
                throw PackedElementError(token, index, $"{PackedTypeName(kind)} value is outside its supported range.");
            }
            AdvancePacked();
            return number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryReadRawBooleanElement(int index, out bool value)
        {
            var token = CurrentPacked(index);
            if (token.Kind == TypedDocumentTokenKind.True)
            {
                AdvancePacked();
                value = true;
                return true;
            }
            if (token.Kind == TypedDocumentTokenKind.False)
            {
                AdvancePacked();
                value = false;
                return true;
            }
            if (!TryReadRawPackedNumber(index, out token))
            {
                value = false;
                return false;
            }

            var number = token.Number;
            if (!double.IsFinite(number) || (number != 0d && number != 1d))
            {
                throw PackedElementError(
                    token,
                    index,
                    "BooleanArray elements must be true, false, 0, or 1.");
            }
            AdvancePacked();
            value = number == 1d;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TypedDocumentToken CurrentPacked(int index)
        {
            if (_current.Kind != TypedDocumentTokenKind.Bad)
            {
                return _current;
            }

            _path.PushIndex(index);
            try
            {
                return Current();
            }
            finally
            {
                _path.Pop();
            }
        }

        private void EnsurePackedElementDepth(TypedDocumentToken token, int index)
        {
            if (_depth >= _maxDepth)
            {
                throw PackedElementError(
                    token,
                    index,
                    $"TDoc value depth exceeds the configured limit of {_maxDepth}.");
            }
        }

        private TypedDocumentException PackedElementError(
            TypedDocumentToken token,
            int index,
            string message)
        {
            _path.PushIndex(index);
            try
            {
                return Error(token, message);
            }
            finally
            {
                _path.Pop();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ReadPackedArraySeparator()
        {
            var token = Current();
            if (token.Kind == TypedDocumentTokenKind.Comma)
            {
                AdvancePacked();
                if (_current.Kind == TypedDocumentTokenKind.Bad)
                {
                    throw ScanError(_current);
                }
                if (_current.Kind == TypedDocumentTokenKind.RightBracket)
                {
                    AdvancePacked();
                    return true;
                }
                return false;
            }
            if (token.Kind == TypedDocumentTokenKind.RightBracket)
            {
                AdvancePacked();
                return true;
            }
            throw Error(token, "Expected ',' or ']'.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AdvancePacked()
        {
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

        private ScriptDatum ReadFloat32PackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'Float32Array' requires an array value.");
            if (Current().Kind != TypedDocumentTokenKind.RightBracket)
            {
                var first = CurrentPacked(0);
                EnsurePackedElementDepth(first, 0);
                if (!_hasLookahead &&
                    first.Kind == TypedDocumentTokenKind.Number &&
                    _scanner.TryReadEntireSimpleFloat64Array(first, out var directItems))
                {
                    AdvancePacked();
                    Expect(TypedDocumentTokenKind.RightBracket, "Expected ']'.");
                    var converted = new float[directItems.Length];
                    for (var i = 0; i < directItems.Length; i++)
                    {
                        converted[i] = (float)directItems[i];
                    }
                    return ScriptDatum.FromObject(new ScriptFloat32Array(converted));
                }
            }
            var buffer = new TypedDocumentPooledBuffer<float>(8);
            try
            {
                if (!Match(TypedDocumentTokenKind.RightBracket))
                {
                    EnsurePackedElementDepth(CurrentPacked(0), 0);
                    var index = 0;
                    while (true)
                    {
                        if (TryReadRawPackedNumber(index, out var location))
                        {
                            buffer.Add((float)ReadRawPackedNumber(
                                TypedDocumentPackedKind.Float32,
                                index,
                                location));
                        }
                        else
                        {
                            _path.PushIndex(index);
                            try
                            {
                                location = Current();
                                var value = ReadTypedValue();
                                ValidatePackedElement(TypedDocumentPackedKind.Float32, value, location);
                                buffer.Add((float)value.Number);
                            }
                            finally
                            {
                                _path.Pop();
                            }
                        }
                        index++;
                        if (ReadPackedArraySeparator()) break;
                    }
                }

                return ScriptDatum.FromObject(new ScriptFloat32Array(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private ScriptDatum ReadFloat64PackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'Float64Array' requires an array value.");
            if (Current().Kind != TypedDocumentTokenKind.RightBracket)
            {
                var first = CurrentPacked(0);
                EnsurePackedElementDepth(first, 0);
                if (!_hasLookahead &&
                    first.Kind == TypedDocumentTokenKind.Number &&
                    _scanner.TryReadEntireSimpleFloat64Array(first, out var directItems))
                {
                    AdvancePacked();
                    Expect(TypedDocumentTokenKind.RightBracket, "Expected ']'.");
                    return ScriptDatum.FromObject(new ScriptFloat64Array(directItems));
                }
            }
            var buffer = new TypedDocumentPooledBuffer<double>(8);
            try
            {
                if (!Match(TypedDocumentTokenKind.RightBracket))
                {
                    EnsurePackedElementDepth(CurrentPacked(0), 0);
                    var index = 0;
                    while (true)
                    {
                        if (TryReadRawPackedNumber(index, out var location))
                        {
                            buffer.Add(ReadRawPackedNumber(
                                TypedDocumentPackedKind.Float64,
                                index,
                                location));
                        }
                        else
                        {
                            _path.PushIndex(index);
                            try
                            {
                                location = Current();
                                var value = ReadTypedValue();
                                ValidatePackedElement(TypedDocumentPackedKind.Float64, value, location);
                                buffer.Add(value.Number);
                            }
                            finally
                            {
                                _path.Pop();
                            }
                        }
                        index++;
                        if (ReadPackedArraySeparator()) break;
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
                    EnsurePackedElementDepth(CurrentPacked(0), 0);
                    var index = 0;
                    while (true)
                    {
                        if (TryReadRawBooleanElement(index, out var element))
                        {
                            buffer.Add(element);
                        }
                        else
                        {
                            _path.PushIndex(index);
                            try
                            {
                                var location = Current();
                                var value = ReadTypedValue();
                                ValidatePackedElement(TypedDocumentPackedKind.Boolean, value, location);
                                buffer.Add(value.Kind == ValueKind.Boolean ? value.Boolean : value.Number == 1d);
                            }
                            finally
                            {
                                _path.Pop();
                            }
                        }
                        index++;
                        if (ReadPackedArraySeparator()) break;
                    }
                }

                return ScriptDatum.FromObject(new ScriptBooleanArray(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private ScriptDatum ReadUInt8PackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'UInt8Array' requires an array value.");
            if (Match(TypedDocumentTokenKind.RightBracket))
            {
                return ScriptDatum.FromObject(new ScriptUInt8Array(Array.Empty<byte>()));
            }

            EnsurePackedElementDepth(CurrentPacked(0), 0);
            var first = CurrentPacked(0);
            if (!_hasLookahead &&
                first.Kind == TypedDocumentTokenKind.Number &&
                first.Length == 1 &&
                // UInt8Array is the format used by the large map documents.  For
                // a compact digit run, make the result array directly instead of
                // renting a staging buffer and copying it a second time.
                _scanner.TryReadEntireCompactUInt8Array((byte)first.Number, out var compactItems))
            {
                AdvancePacked();
                Expect(TypedDocumentTokenKind.RightBracket, "Expected ']'.");
                return ScriptDatum.FromObject(new ScriptUInt8Array(compactItems));
            }

            if (!_hasLookahead &&
                first.Kind == TypedDocumentTokenKind.Number &&
                _scanner.TryReadEntireSimpleUInt8Array(first, out var simpleItems))
            {
                AdvancePacked();
                Expect(TypedDocumentTokenKind.RightBracket, "Expected ']'.");
                return ScriptDatum.FromObject(new ScriptUInt8Array(simpleItems));
            }

            var buffer = new TypedDocumentPooledBuffer<byte>(8);
            try
            {
                var index = 0;
                while (true)
                {
                    if (TryReadRawPackedNumber(index, out var location))
                    {
                        buffer.Add((byte)ReadRawPackedNumber(
                            TypedDocumentPackedKind.UInt8,
                            index,
                            location));
                    }
                    else
                    {
                        _path.PushIndex(index);
                        try
                        {
                            location = Current();
                            var value = ReadTypedValue();
                            ValidatePackedElement(TypedDocumentPackedKind.UInt8, value, location);
                            TryGetPackedUInt64(value, out var integer);
                            buffer.Add((byte)integer);
                        }
                        finally
                        {
                            _path.Pop();
                        }
                    }
                    index++;
                    if (ReadPackedArraySeparator()) break;
                }
                return ScriptDatum.FromObject(new ScriptUInt8Array(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private ScriptDatum ReadInt16PackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'Int16Array' requires an array value.");
            if (Current().Kind != TypedDocumentTokenKind.RightBracket)
            {
                var first = CurrentPacked(0);
                EnsurePackedElementDepth(first, 0);
                if (!_hasLookahead &&
                    first.Kind == TypedDocumentTokenKind.Number &&
                    _scanner.TryReadEntireSimpleInt16Array(first, out var directItems))
                {
                    AdvancePacked();
                    Expect(TypedDocumentTokenKind.RightBracket, "Expected ']'.");
                    return ScriptDatum.FromObject(new ScriptInt16Array(directItems));
                }
            }
            var buffer = new TypedDocumentPooledBuffer<short>(8);
            try
            {
                if (!Match(TypedDocumentTokenKind.RightBracket))
                {
                    EnsurePackedElementDepth(CurrentPacked(0), 0);
                    var index = 0;
                    while (true)
                    {
                        if (TryReadRawPackedNumber(index, out var location))
                        {
                            buffer.Add((short)ReadRawPackedNumber(
                                TypedDocumentPackedKind.Int16,
                                index,
                                location));
                        }
                        else
                        {
                            _path.PushIndex(index);
                            try
                            {
                                location = Current();
                                var value = ReadTypedValue();
                                ValidatePackedElement(TypedDocumentPackedKind.Int16, value, location);
                                TryGetPackedInt64(value, out var integer);
                                buffer.Add((short)integer);
                            }
                            finally
                            {
                                _path.Pop();
                            }
                        }
                        index++;
                        if (ReadPackedArraySeparator()) break;
                    }
                }
                return ScriptDatum.FromObject(new ScriptInt16Array(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private ScriptDatum ReadUInt16PackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'UInt16Array' requires an array value.");
            if (Current().Kind != TypedDocumentTokenKind.RightBracket)
            {
                var first = CurrentPacked(0);
                EnsurePackedElementDepth(first, 0);
                if (!_hasLookahead &&
                    first.Kind == TypedDocumentTokenKind.Number &&
                    _scanner.TryReadEntireSimpleUInt16Array(first, out var directItems))
                {
                    AdvancePacked();
                    Expect(TypedDocumentTokenKind.RightBracket, "Expected ']'.");
                    return ScriptDatum.FromObject(new ScriptUInt16Array(directItems));
                }
            }
            var buffer = new TypedDocumentPooledBuffer<ushort>(8);
            try
            {
                if (!Match(TypedDocumentTokenKind.RightBracket))
                {
                    EnsurePackedElementDepth(CurrentPacked(0), 0);
                    var index = 0;
                    while (true)
                    {
                        if (TryReadRawPackedNumber(index, out var location))
                        {
                            buffer.Add((ushort)ReadRawPackedNumber(
                                TypedDocumentPackedKind.UInt16,
                                index,
                                location));
                        }
                        else
                        {
                            _path.PushIndex(index);
                            try
                            {
                                location = Current();
                                var value = ReadTypedValue();
                                ValidatePackedElement(TypedDocumentPackedKind.UInt16, value, location);
                                TryGetPackedUInt64(value, out var integer);
                                buffer.Add((ushort)integer);
                            }
                            finally
                            {
                                _path.Pop();
                            }
                        }
                        index++;
                        if (ReadPackedArraySeparator()) break;
                    }
                }
                return ScriptDatum.FromObject(new ScriptUInt16Array(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private ScriptDatum ReadUInt32PackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'UInt32Array' requires an array value.");
            if (Current().Kind != TypedDocumentTokenKind.RightBracket)
            {
                var first = CurrentPacked(0);
                EnsurePackedElementDepth(first, 0);
                if (!_hasLookahead &&
                    first.Kind == TypedDocumentTokenKind.Number &&
                    _scanner.TryReadEntireSimpleUInt32Array(first, out var directItems))
                {
                    AdvancePacked();
                    Expect(TypedDocumentTokenKind.RightBracket, "Expected ']'.");
                    return ScriptDatum.FromObject(new ScriptUInt32Array(directItems));
                }
            }
            var buffer = new TypedDocumentPooledBuffer<uint>(8);
            try
            {
                if (!Match(TypedDocumentTokenKind.RightBracket))
                {
                    EnsurePackedElementDepth(CurrentPacked(0), 0);
                    var index = 0;
                    while (true)
                    {
                        if (TryReadRawPackedNumber(index, out var location))
                        {
                            buffer.Add((uint)ReadRawPackedNumber(
                                TypedDocumentPackedKind.UInt32,
                                index,
                                location));
                        }
                        else
                        {
                            _path.PushIndex(index);
                            try
                            {
                                location = Current();
                                var value = ReadTypedValue();
                                ValidatePackedElement(TypedDocumentPackedKind.UInt32, value, location);
                                TryGetPackedUInt64(value, out var integer);
                                buffer.Add((uint)integer);
                            }
                            finally
                            {
                                _path.Pop();
                            }
                        }
                        index++;
                        if (ReadPackedArraySeparator()) break;
                    }
                }
                return ScriptDatum.FromObject(new ScriptUInt32Array(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private ScriptDatum ReadInt64PackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'Int64Array' requires an array value.");
            if (Current().Kind != TypedDocumentTokenKind.RightBracket)
            {
                var first = CurrentPacked(0);
                EnsurePackedElementDepth(first, 0);
                if (!_hasLookahead &&
                    first.Kind == TypedDocumentTokenKind.Number &&
                    _scanner.TryReadEntireSimpleInt64Array(first, out var directItems))
                {
                    AdvancePacked();
                    Expect(TypedDocumentTokenKind.RightBracket, "Expected ']'.");
                    return ScriptDatum.FromObject(new ScriptInt64Array(directItems));
                }
            }
            var buffer = new TypedDocumentPooledBuffer<long>(8);
            try
            {
                if (!Match(TypedDocumentTokenKind.RightBracket))
                {
                    EnsurePackedElementDepth(CurrentPacked(0), 0);
                    var index = 0;
                    while (true)
                    {
                        if (TryReadExactInt64(index, out var exact))
                        {
                            buffer.Add(exact);
                        }
                        else
                        {
                            _path.PushIndex(index);
                            try
                            {
                                var location = Current();
                                var value = ReadTypedValue();
                                ValidatePackedElement(TypedDocumentPackedKind.Int64, value, location);
                                TryGetPackedInt64(value, out exact);
                                buffer.Add(exact);
                            }
                            finally
                            {
                                _path.Pop();
                            }
                        }
                        index++;
                        if (ReadPackedArraySeparator()) break;
                    }
                }
                return ScriptDatum.FromObject(new ScriptInt64Array(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private ScriptDatum ReadUInt64PackedArray()
        {
            Expect(TypedDocumentTokenKind.LeftBracket, "Type 'UInt64Array' requires an array value.");
            if (Current().Kind != TypedDocumentTokenKind.RightBracket)
            {
                var first = CurrentPacked(0);
                EnsurePackedElementDepth(first, 0);
                if (!_hasLookahead &&
                    first.Kind == TypedDocumentTokenKind.Number &&
                    _scanner.TryReadEntireSimpleUInt64Array(first, out var directItems))
                {
                    AdvancePacked();
                    Expect(TypedDocumentTokenKind.RightBracket, "Expected ']'.");
                    return ScriptDatum.FromObject(new ScriptUInt64Array(directItems));
                }
            }
            var buffer = new TypedDocumentPooledBuffer<ulong>(8);
            try
            {
                if (!Match(TypedDocumentTokenKind.RightBracket))
                {
                    EnsurePackedElementDepth(CurrentPacked(0), 0);
                    var index = 0;
                    while (true)
                    {
                        if (TryReadExactUInt64(index, out var exact))
                        {
                            buffer.Add(exact);
                        }
                        else
                        {
                            _path.PushIndex(index);
                            try
                            {
                                var location = Current();
                                var value = ReadTypedValue();
                                ValidatePackedElement(TypedDocumentPackedKind.UInt64, value, location);
                                TryGetPackedUInt64(value, out exact);
                                buffer.Add(exact);
                            }
                            finally
                            {
                                _path.Pop();
                            }
                        }
                        index++;
                        if (ReadPackedArraySeparator()) break;
                    }
                }
                return ScriptDatum.FromObject(new ScriptUInt64Array(buffer.ToArray()));
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private bool TryReadExactInt64(int index, out long value)
        {
            var token = CurrentPacked(index);
            if (token.Kind == TypedDocumentTokenKind.Number)
            {
                if (!_scanner.TryGetInt64Exact(token, out value)) return false;
                Advance();
                return true;
            }
            if (token.Kind == TypedDocumentTokenKind.Identifier && TypeEquals(token, "Number"))
            {
                Advance();
                token = CurrentPacked(index);
                if (token.Kind != TypedDocumentTokenKind.Number)
                {
                    throw PackedElementError(token, index, "Type 'Number' requires a number value.");
                }
                if (!_scanner.TryGetInt64Exact(token, out value))
                {
                    Advance();
                    throw PackedElementError(
                        token,
                        index,
                        "Int64Array elements must be exactly representable integers.");
                }
                Advance();
                return true;
            }
            value = 0;
            return false;
        }

        private bool TryReadExactUInt64(int index, out ulong value)
        {
            var token = CurrentPacked(index);
            if (token.Kind == TypedDocumentTokenKind.Number)
            {
                if (!_scanner.TryGetUInt64Exact(token, out value)) return false;
                Advance();
                return true;
            }
            if (token.Kind == TypedDocumentTokenKind.Identifier && TypeEquals(token, "Number"))
            {
                Advance();
                token = CurrentPacked(index);
                if (token.Kind != TypedDocumentTokenKind.Number)
                {
                    throw PackedElementError(token, index, "Type 'Number' requires a number value.");
                }
                if (!_scanner.TryGetUInt64Exact(token, out value))
                {
                    Advance();
                    throw PackedElementError(
                        token,
                        index,
                        "UInt64Array elements must be exactly representable integers.");
                }
                Advance();
                return true;
            }
            value = 0;
            return false;
        }

        private ScriptDatum ReadRegisteredObject(
            string alias,
            ClrType registration,
            TypedDocumentToken typeToken)
        {
            var registrationError = TypedDocumentBinder.GetClrRegistrationError(registration, alias);
            if (registrationError != null)
            {
                throw Error(typeToken, registrationError);
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

        private ScriptDatum ReadNativeTypedDocument(
            string alias,
            TypedDocumentNativeCatalog.Entry entry,
            TypedDocumentToken typeToken)
        {
            INativeTypedDocument document;
            try
            {
                document = entry.Create();
            }
            catch (Exception exception)
            {
                throw Error(typeToken, $"Could not construct native type '{alias}'.", exception);
            }

            if (document is not ScriptObject scriptObject)
            {
                throw Error(typeToken, $"Native type '{alias}' must construct a ScriptObject.");
            }

            if (Match(TypedDocumentTokenKind.LeftBracket))
            {
                ReadNativeTypedDocumentArray(alias, document, typeToken);
                return ScriptDatum.FromObject(scriptObject);
            }

            if (Match(TypedDocumentTokenKind.LeftBrace))
            {
                if (!Match(TypedDocumentTokenKind.RightBrace))
                {
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    while (true)
                    {
                        var header = ReadMemberHeader();
                        _path.PushProperty(header.Name);
                        try
                        {
                            if (!seen.Add(header.Name))
                            {
                                throw Error(header.NameToken, $"Duplicate property '{header.Name}'.");
                            }

                            var memberValue = ReadMemberValue(header);
                            try
                            {
                                var input = new TypedDocumentInput(
                                    header.Name,
                                    -1,
                                    header.ReadOnly,
                                    memberValue,
                                    _path.Format());
                                document.ReadTypedDocument(ref input);
                            }
                            catch (TypedDocumentException)
                            {
                                throw;
                            }
                            catch (Exception exception)
                            {
                                throw Error(
                                    header.NameToken,
                                    $"Native TDoc member '{header.Name}' read failed.",
                                    exception);
                            }
                        }
                        finally
                        {
                            _path.Pop();
                        }

                        if (ReadObjectSeparator()) break;
                    }
                }

                return ScriptDatum.FromObject(scriptObject);
            }

            var scalar = ReadTypedValue();
            try
            {
                var input = new TypedDocumentInput(null, -1, false, scalar, _path.Format());
                document.ReadTypedDocument(ref input);
            }
            catch (TypedDocumentException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw Error(typeToken, $"Native TDoc value read failed.", exception);
            }

            return ScriptDatum.FromObject(scriptObject);
        }

        private void ReadNativeTypedDocumentArray(
            string alias,
            INativeTypedDocument document,
            TypedDocumentToken typeToken)
        {
            if (Match(TypedDocumentTokenKind.RightBracket))
            {
                return;
            }

            var index = 0;
            while (true)
            {
                _path.PushIndex(index);
                try
                {
                    var value = ReadTypedValue();
                    try
                    {
                        var input = new TypedDocumentInput(
                            null,
                            index,
                            false,
                            value,
                            _path.Format());
                        document.ReadTypedDocument(ref input);
                    }
                    catch (TypedDocumentException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        throw Error(
                            typeToken,
                            $"Native TDoc element [{index}] read failed.",
                            exception);
                    }
                }
                finally
                {
                    _path.Pop();
                }

                index++;
                if (ReadArraySeparator()) break;
            }
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
            if (!TypedDocumentBinder.TryConvertClrValue(value, member.Type, out var converted))
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
                if (TypedDocumentBinder.TryGetBooleanElement(value, out _))
                {
                    return;
                }
                throw Error(location, "BooleanArray elements must be true, false, 0, or 1.");
            }
            if (kind is TypedDocumentPackedKind.Float32 or TypedDocumentPackedKind.Float64)
            {
                if (value.Kind == ValueKind.Number && double.IsFinite(value.Number))
                {
                    return;
                }
                throw Error(location, $"{PackedTypeName(kind)} elements must be finite numbers.");
            }

            if (kind is TypedDocumentPackedKind.Int32 or
                TypedDocumentPackedKind.Int8 or
                TypedDocumentPackedKind.Int16 or
                TypedDocumentPackedKind.Int64)
            {
                if (!TryGetPackedInt64(value, out var integer))
                {
                    throw Error(location, $"{PackedTypeName(kind)} elements must be finite integers.");
                }

                var inRange = kind switch
                {
                    TypedDocumentPackedKind.Int8 => integer >= sbyte.MinValue && integer <= sbyte.MaxValue,
                    TypedDocumentPackedKind.Int16 => integer >= short.MinValue && integer <= short.MaxValue,
                    TypedDocumentPackedKind.Int32 => integer >= int.MinValue && integer <= int.MaxValue,
                    _ => true
                };
                if (inRange) return;
            }
            else if (TryGetPackedUInt64(value, out var integer))
            {
                var inRange = kind switch
                {
                    TypedDocumentPackedKind.UInt8 => integer <= byte.MaxValue,
                    TypedDocumentPackedKind.UInt16 => integer <= ushort.MaxValue,
                    TypedDocumentPackedKind.UInt32 => integer <= uint.MaxValue,
                    _ => true
                };
                if (inRange) return;
            }

            throw Error(location, $"{PackedTypeName(kind)} value is outside its supported range.");
        }

        private static bool TryGetPackedInt64(ScriptDatum value, out long integer)
        {
            switch (value.Kind)
            {
                case ValueKind.Int64:
                    integer = value.Int64;
                    return true;
                case ValueKind.UInt64 when value.UInt64 <= long.MaxValue:
                    integer = (long)value.UInt64;
                    return true;
                case ValueKind.Number when TypeCheckOps.IsInt64(value.Number):
                    integer = (long)value.Number;
                    return true;
                default:
                    integer = 0;
                    return false;
            }
        }

        private static bool TryGetPackedUInt64(ScriptDatum value, out ulong integer)
        {
            switch (value.Kind)
            {
                case ValueKind.UInt64:
                    integer = value.UInt64;
                    return true;
                case ValueKind.Int64 when value.Int64 >= 0:
                    integer = (ulong)value.Int64;
                    return true;
                case ValueKind.Number when TypeCheckOps.IsUInt64(value.Number):
                    integer = (ulong)value.Number;
                    return true;
                default:
                    integer = 0;
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
                TypedDocumentPackedKind.Float32 => "Float32Array",
                TypedDocumentPackedKind.Float64 => "Float64Array",
                TypedDocumentPackedKind.Boolean => "BooleanArray",
                TypedDocumentPackedKind.UInt8 => "UInt8Array",
                TypedDocumentPackedKind.Int16 => "Int16Array",
                TypedDocumentPackedKind.UInt16 => "UInt16Array",
                TypedDocumentPackedKind.UInt32 => "UInt32Array",
                TypedDocumentPackedKind.Int64 => "Int64Array",
                TypedDocumentPackedKind.UInt64 => "UInt64Array",
                _ => "PackedArray"
            };
        }
    }
}
