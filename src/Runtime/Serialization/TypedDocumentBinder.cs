using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Shared runtime binding rules for native TDoc literals and standalone TDoc
    /// values.  The binder intentionally works on <see cref="ScriptDatum"/> so
    /// interpolation does not need an intermediate serialization tree.
    /// </summary>
    internal static class TypedDocumentBinder
    {
        public static ScriptDatum BindInterpolation(
            ScriptContext context,
            string typeName,
            ScriptDatum value)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return Bind(context.Engine, typeName, value, "$");
        }

        public static ScriptDatum BindInterpolationAtPath(
            ScriptContext context,
            string typeName,
            ScriptDatum value,
            string path)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return Bind(
                context.Engine,
                typeName,
                value,
                string.IsNullOrEmpty(path) ? "$" : path);
        }

        /// <summary>
        /// Validates one value against the concrete packed-array target and writes it
        /// directly to that target.  Native TDoc array literals use this instead of
        /// first materializing a <see cref="ScriptArray"/> of <see cref="ScriptDatum"/>
        /// values only to copy it into primitive storage.
        /// </summary>
        public static void SetPackedElement(
            ScriptPackedArray target,
            int index,
            ScriptDatum value,
            string path)
        {
            ArgumentNullException.ThrowIfNull(target);
            path = string.IsNullOrEmpty(path) ? "$" : path;

            // The compiler allocates the exact target length and emits only valid
            // indexes.  Let the CLR array store retain its inexpensive bounds check;
            // do not add another check to every valid literal element.
            switch (target)
            {
                case ScriptInt32Array int32:
                    int32._items[index] = ReadInt32(value, "Int32Array", path, index);
                    return;
                case ScriptInt8Array int8:
                    int8._items[index] = (sbyte)ReadInt64(
                        value,
                        sbyte.MinValue,
                        sbyte.MaxValue,
                        "Int8Array",
                        path,
                        index);
                    return;
                case ScriptFloat64Array float64:
                    if (value.Kind != ValueKind.Number || !double.IsFinite(value.Number))
                    {
                        throw Error("Float64Array elements must be finite numbers.", ElementPath(path, index));
                    }
                    float64._items[index] = value.Number;
                    return;
                case ScriptBooleanArray boolean:
                    if (!TryGetBooleanElement(value, out var booleanValue))
                    {
                        throw Error("BooleanArray elements must be true, false, 0, or 1.", ElementPath(path, index));
                    }
                    boolean._items[index] = booleanValue;
                    return;
                case ScriptUInt8Array uint8:
                    uint8._items[index] = (byte)ReadUInt64(value, byte.MaxValue, "UInt8Array", path, index);
                    return;
                case ScriptInt16Array int16:
                    int16._items[index] = (short)ReadInt64(
                        value,
                        short.MinValue,
                        short.MaxValue,
                        "Int16Array",
                        path,
                        index);
                    return;
                case ScriptUInt16Array uint16:
                    uint16._items[index] = (ushort)ReadUInt64(value, ushort.MaxValue, "UInt16Array", path, index);
                    return;
                case ScriptUInt32Array uint32:
                    uint32._items[index] = (uint)ReadUInt64(value, uint.MaxValue, "UInt32Array", path, index);
                    return;
                case ScriptInt64Array int64:
                    int64._items[index] = ReadInt64(value, long.MinValue, long.MaxValue, "Int64Array", path, index);
                    return;
                case ScriptUInt64Array uint64:
                    uint64._items[index] = ReadUInt64(value, ulong.MaxValue, "UInt64Array", path, index);
                    return;
                default:
                    throw new ArgumentException("Unknown packed-array target.", nameof(target));
            }
        }

        /// <summary>Applies one explicit TDoc type to a runtime datum.</summary>
        public static ScriptDatum Bind(
            AuroraEngine engine,
            string typeName,
            ScriptDatum value)
        {
            return Bind(engine, typeName, value, "$");
        }

        internal static ScriptDatum Bind(
            AuroraEngine engine,
            string typeName,
            ScriptDatum value,
            string path)
        {
            ArgumentNullException.ThrowIfNull(engine);
            if (string.IsNullOrEmpty(typeName)) return value;

            switch (typeName)
            {
                case "Null":
                    Require(value.Kind == ValueKind.Null, typeName, "requires null.", path);
                    return ScriptDatum.Null;
                case "String":
                    Require(value.Kind == ValueKind.String, typeName, "requires a string value.", path);
                    return value;
                case "Number":
                    Require(value.Kind == ValueKind.Number && double.IsFinite(value.Number), typeName, "requires a finite number.", path);
                    return value;
                case "Boolean":
                    Require(value.Kind == ValueKind.Boolean, typeName, "requires a boolean value.", path);
                    return value;
                case "Object":
                    Require(
                        value.Object != null && value.Object.GetType() == typeof(ScriptObject),
                        typeName,
                        "requires an object value.",
                        path);
                    return value;
                case "Array":
                    Require(value.Object is ScriptArray, typeName, "requires a regular array value.", path);
                    return value;
                case "Date":
                    return BindDate(engine, value, path);
                case "StringBuffer":
                    if (value.Object is StringBuffer) return value;
                    Require(value.Kind == ValueKind.String, typeName, "requires a string value.", path);
                    return ScriptDatum.FromObject(new StringBuffer(value.StringText));
                case "Path":
                    if (value.Object is ScriptPathValue) return value;
                    Require(value.Kind == ValueKind.String, typeName, "requires a string value.", path);
                    return ScriptDatum.FromObject(new ScriptPathValue(value.StringText));
                case "Regex":
                    Require(value.Object is ScriptRegex, typeName, "requires a regex value.", path);
                    return value;
                case "HashMap":
                    Require(value.Object is ScriptHashMap, typeName, "requires a hash-map value.", path);
                    return value;
                case "Int32Array":
                    return BindPacked(typeName, value, path);
                case "Int8Array":
                    return BindPacked(typeName, value, path);
                case "Float64Array":
                    return BindPacked(typeName, value, path);
                case "BooleanArray":
                    return BindPacked(typeName, value, path);
                case "UInt8Array":
                case "Int16Array":
                case "UInt16Array":
                case "UInt32Array":
                case "Int64Array":
                case "UInt64Array":
                    return BindPacked(typeName, value, path);
                default:
                    return BindClrAlias(engine, typeName, value, path);
            }
        }

        public static ClrInstanceObject CreateClrObject(
            ScriptContext context,
            string alias,
            string path)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return CreateClrObject(context.Engine, alias, path);
        }

        public static void SetClrObjectMember(
            ClrInstanceObject target,
            string alias,
            string name,
            bool readOnly,
            ScriptDatum value,
            string path)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            SetClrObjectMemberCore(target, alias, name, readOnly, value, path, true);
        }

        private static void SetClrObjectMemberCore(
            ClrInstanceObject target,
            string alias,
            string name,
            bool readOnly,
            ScriptDatum value,
            string path,
            bool pathIncludesMember)
        {
            if (readOnly)
            {
                throw Error(
                    "readonly is not supported by the default CLR object contract.",
                    ErrorPath(path, name, pathIncludesMember));
            }

            var contract = target.Descriptor.DataContract;
            if (!contract.TryGetMember(name, out var member))
            {
                throw Error(
                    $"Unknown field '{name}' for registered type '{alias}'.",
                    ErrorPath(path, name, pathIncludesMember));
            }
            AssignClrMember(
                target.Instance,
                member,
                name,
                value,
                path,
                pathIncludesMember);
        }

        private static ScriptDatum BindDate(AuroraEngine engine, ScriptDatum value, string path)
        {
            if (value.Object is ScriptDate) return value;

            if (value.Kind == ValueKind.Number)
            {
                var number = value.Number;
                if (!double.IsFinite(number) || Math.Truncate(number) != number ||
                    number < DateTimeOffset.MinValue.Ticks ||
                    number > DateTimeOffset.MaxValue.Ticks ||
                    (long)number < DateTimeOffset.MinValue.Ticks ||
                    (long)number > DateTimeOffset.MaxValue.Ticks ||
                    (long)number != number)
                {
                    throw Error("Date ticks must be an exactly representable integer in the range 0.." + DateTimeOffset.MaxValue.Ticks + ".", path);
                }
                return ScriptDatum.FromDate(new ScriptDate((long)number));
            }

            if (value.Kind != ValueKind.String)
            {
                throw Error("Type 'Date' requires a formatted string, an integer ticks value, or a ScriptDate.", path);
            }

            var format = engine.Options.Runtime.DateTimeFormat;
            if (string.IsNullOrEmpty(format))
            {
                throw Error("EngineOptions.Runtime.DateTimeFormat cannot be null or empty.", path);
            }

            try
            {
                if (TryParseDate(value.StringText, format, out var parsed))
                {
                    return ScriptDatum.FromDate(new ScriptDate(parsed));
                }
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                throw Error($"Invalid EngineOptions.Runtime.DateTimeFormat '{format}'.", path, exception);
            }

            throw Error($"Date value must match EngineOptions.Runtime.DateTimeFormat '{format}'.", path);
        }

        internal static bool TryParseDate(
            string text,
            string format,
            out DateTimeOffset value)
        {
            return DateTimeOffset.TryParseExact(
                text,
                format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out value);
        }

        private static ScriptDatum BindPacked(
            string typeName,
            ScriptDatum value,
            string path)
        {
            if (value.Object != null && IsMatchingPackedType(value.Object, typeName))
            {
                return value;
            }

            var source = value.Object as ScriptArray;
            if (source == null)
            {
                throw Error($"Type '{typeName}' requires an array value.", path);
            }

            var length = source.Length;
            var sourceItems = source._items;
            switch (typeName)
            {
                case "Int32Array":
                    var int32 = new int[length];
                    for (var i = 0; i < length; i++) int32[i] = ReadInt32(sourceItems[i], typeName, path, i);
                    return ScriptDatum.FromObject(new ScriptInt32Array(int32));
                case "Int8Array":
                    var int8 = new sbyte[length];
                    for (var i = 0; i < length; i++) int8[i] = (sbyte)ReadInt64(sourceItems[i], sbyte.MinValue, sbyte.MaxValue, typeName, path, i);
                    return ScriptDatum.FromObject(new ScriptInt8Array(int8));
                case "UInt8Array":
                    var uint8 = new byte[length];
                    for (var i = 0; i < length; i++) uint8[i] = (byte)ReadUInt64(sourceItems[i], byte.MaxValue, typeName, path, i);
                    return ScriptDatum.FromObject(new ScriptUInt8Array(uint8));
                case "Int16Array":
                    var int16 = new short[length];
                    for (var i = 0; i < length; i++) int16[i] = (short)ReadInt64(sourceItems[i], short.MinValue, short.MaxValue, typeName, path, i);
                    return ScriptDatum.FromObject(new ScriptInt16Array(int16));
                case "UInt16Array":
                    var uint16 = new ushort[length];
                    for (var i = 0; i < length; i++) uint16[i] = (ushort)ReadUInt64(sourceItems[i], ushort.MaxValue, typeName, path, i);
                    return ScriptDatum.FromObject(new ScriptUInt16Array(uint16));
                case "UInt32Array":
                    var uint32 = new uint[length];
                    for (var i = 0; i < length; i++) uint32[i] = (uint)ReadUInt64(sourceItems[i], uint.MaxValue, typeName, path, i);
                    return ScriptDatum.FromObject(new ScriptUInt32Array(uint32));
                case "Int64Array":
                    var int64 = new long[length];
                    for (var i = 0; i < length; i++) int64[i] = ReadInt64(sourceItems[i], long.MinValue, long.MaxValue, typeName, path, i);
                    return ScriptDatum.FromObject(new ScriptInt64Array(int64));
                case "UInt64Array":
                    var uint64 = new ulong[length];
                    for (var i = 0; i < length; i++) uint64[i] = ReadUInt64(sourceItems[i], ulong.MaxValue, typeName, path, i);
                    return ScriptDatum.FromObject(new ScriptUInt64Array(uint64));
                case "Float64Array":
                    var float64 = new double[length];
                    for (var i = 0; i < length; i++)
                    {
                        var element = sourceItems[i];
                        if (element.Kind != ValueKind.Number || !double.IsFinite(element.Number))
                        {
                            throw Error($"{typeName} elements must be finite numbers.", ElementPath(path, i));
                        }
                        float64[i] = element.Number;
                    }
                    return ScriptDatum.FromObject(new ScriptFloat64Array(float64));
                case "BooleanArray":
                    var boolean = new bool[length];
                    for (var i = 0; i < length; i++)
                    {
                        var element = sourceItems[i];
                        if (TryGetBooleanElement(element, out var booleanValue))
                        {
                            boolean[i] = booleanValue;
                        }
                        else
                        {
                            throw Error(
                                $"{typeName} elements must be true, false, 0, or 1.",
                                ElementPath(path, i));
                        }
                    }
                    return ScriptDatum.FromObject(new ScriptBooleanArray(boolean));
                default:
                    throw Error($"Unknown packed-array type '{typeName}'.", path);
            }
        }

        private static bool IsMatchingPackedType(ScriptObject value, string typeName)
        {
            return (typeName, value) switch
            {
                ("Int32Array", ScriptInt32Array) or
                ("Int8Array", ScriptInt8Array) or
                ("Float64Array", ScriptFloat64Array) or
                ("BooleanArray", ScriptBooleanArray) or
                ("UInt8Array", ScriptUInt8Array) or
                ("Int16Array", ScriptInt16Array) or
                ("UInt16Array", ScriptUInt16Array) or
                ("UInt32Array", ScriptUInt32Array) or
                ("Int64Array", ScriptInt64Array) or
                ("UInt64Array", ScriptUInt64Array) => true,
                _ => false
            };
        }

        internal static bool TryGetBooleanElement(ScriptDatum value, out bool result)
        {
            if (value.Kind == ValueKind.Boolean)
            {
                result = value.Boolean;
                return true;
            }
            if (value.Kind == ValueKind.Number && double.IsFinite(value.Number) &&
                (value.Number == 0d || value.Number == 1d))
            {
                result = value.Number == 1d;
                return true;
            }
            result = false;
            return false;
        }

        internal static bool TryGetFiniteInteger(ScriptDatum value, out double number)
        {
            number = value.Number;
            return value.Kind == ValueKind.Number &&
                double.IsFinite(number) && Math.Truncate(number) == number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsPackedRange(TypedDocumentPackedKind kind, double number)
        {
            return kind switch
            {
                TypedDocumentPackedKind.Float64 or TypedDocumentPackedKind.Boolean => true,
                TypedDocumentPackedKind.Int8 => number >= sbyte.MinValue && number <= sbyte.MaxValue,
                TypedDocumentPackedKind.UInt8 => number >= byte.MinValue && number <= byte.MaxValue,
                TypedDocumentPackedKind.Int16 => number >= short.MinValue && number <= short.MaxValue,
                TypedDocumentPackedKind.UInt16 => number >= ushort.MinValue && number <= ushort.MaxValue,
                TypedDocumentPackedKind.UInt32 => number >= uint.MinValue && number <= uint.MaxValue,
                TypedDocumentPackedKind.Int64 => number >= long.MinValue && number < 9223372036854775808d,
                TypedDocumentPackedKind.UInt64 => number >= 0d && number < 18446744073709551616d,
                _ => number >= int.MinValue && number <= int.MaxValue
            };
        }

        internal static bool TryGetPackedKind(string typeName, out TypedDocumentPackedKind kind)
        {
            kind = typeName switch
            {
                "Int32Array" => TypedDocumentPackedKind.Int32,
                "Int8Array" => TypedDocumentPackedKind.Int8,
                "Float64Array" => TypedDocumentPackedKind.Float64,
                "BooleanArray" => TypedDocumentPackedKind.Boolean,
                "UInt8Array" => TypedDocumentPackedKind.UInt8,
                "Int16Array" => TypedDocumentPackedKind.Int16,
                "UInt16Array" => TypedDocumentPackedKind.UInt16,
                "UInt32Array" => TypedDocumentPackedKind.UInt32,
                "Int64Array" => TypedDocumentPackedKind.Int64,
                "UInt64Array" => TypedDocumentPackedKind.UInt64,
                _ => (TypedDocumentPackedKind)byte.MaxValue
            };
            return kind != (TypedDocumentPackedKind)byte.MaxValue;
        }

        private static int ReadInt32(ScriptDatum value, string typeName, string path, int index)
        {
            var number = ReadFiniteInteger(value, typeName, path, index);
            if (number < int.MinValue || number > int.MaxValue)
            {
                throw Error($"{typeName} element is outside the range {int.MinValue}..{int.MaxValue}.", ElementPath(path, index));
            }
            return (int)number;
        }

        private static long ReadInt64(ScriptDatum value, long minimum, long maximum, string typeName, string path, int index)
        {
            var number = ReadFiniteInteger(value, typeName, path, index);
            if (number < minimum || number > maximum ||
                (maximum == long.MaxValue && number >= 9223372036854775808d) ||
                (long)number != number)
            {
                throw Error($"{typeName} element is outside the range {minimum}..{maximum}.", ElementPath(path, index));
            }
            return (long)number;
        }

        private static ulong ReadUInt64(ScriptDatum value, ulong maximum, string typeName, string path, int index)
        {
            var number = ReadFiniteInteger(value, typeName, path, index);
            if (number < 0d || number > maximum || number >= 18446744073709551616d ||
                (ulong)number != number)
            {
                throw Error($"{typeName} element is outside the range 0..{maximum}.", ElementPath(path, index));
            }
            return (ulong)number;
        }

        private static double ReadFiniteInteger(ScriptDatum value, string typeName, string path, int index)
        {
            if (!TryGetFiniteInteger(value, out var number))
            {
                throw Error($"{typeName} elements must be finite integers.", ElementPath(path, index));
            }
            return number;
        }

        private static ScriptDatum BindClrAlias(
            AuroraEngine engine,
            string alias,
            ScriptDatum value,
            string path)
        {
            if (!engine.ClrRegistry.TryGetClrType(alias, out var registration))
            {
                throw Error($"Unknown type '{alias}'.", path);
            }
            ValidateClrRegistration(registration, alias, path);

            var type = registration._descriptor.Type;
            if (value.Object is ClrInstanceObject existing &&
                existing.Instance != null && type.IsInstanceOfType(existing.Instance))
            {
                return value;
            }

            var source = value.Object;
            if (source == null || source.GetType() != typeof(ScriptObject))
            {
                throw Error($"Type '{alias}' requires an object value.", path);
            }

            var target = CreateClrObject(registration, alias, path);

            var properties = source.OwnProperties;
            for (var i = 0; i < properties.Length; i++)
            {
                ref readonly var metadata = ref properties[i];
                if (!metadata.Meta.Enumerable) continue;
                var descriptor = source.GetOwnProperty(metadata.Meta.Slot);
                if (descriptor.IsAccessor)
                {
                    throw Error(
                        $"CLR member '{metadata.Name}' is not writable.",
                        MemberPath(path, metadata.Name));
                }
                SetClrObjectMemberCore(
                    target,
                    alias,
                    metadata.Name,
                    readOnly: !metadata.Meta.Writable,
                    descriptor.Datum,
                    path,
                    false);
            }

            return ScriptDatum.FromObject(target);
        }

        internal static ClrInstanceObject CreateClrObject(
            AuroraEngine engine,
            string alias,
            string path)
        {
            if (!engine.ClrRegistry.TryGetClrType(alias, out var registration))
            {
                throw Error($"Unknown type '{alias}'.", path);
            }
            ValidateClrRegistration(registration, alias, path);
            return CreateClrObject(registration, alias, path);
        }

        private static ClrInstanceObject CreateClrObject(
            ClrType registration,
            string alias,
            string path)
        {
            object instance;
            try
            {
                instance = registration._descriptor.DataContract.Factory();
            }
            catch (Exception exception)
            {
                throw Error($"Could not construct registered type '{alias}'.", path, exception);
            }
            return new ClrInstanceObject(registration._descriptor, instance);
        }

        internal static void ValidateClrRegistration(
            ClrType registration,
            string alias,
            string path)
        {
            var message = GetClrRegistrationError(registration, alias);
            if (message != null) throw Error(message, path);
        }

        internal static string GetClrRegistrationError(ClrType registration, string alias)
        {
            var type = registration._descriptor.Type;
            if (!type.IsClass || type.IsAbstract || type.IsArray || type.ContainsGenericParameters ||
                typeof(Delegate).IsAssignableFrom(type))
            {
                return $"Registered type '{alias}' is not a constructible object type.";
            }
            if ((registration._access & TypeAccess.Constructor) == 0)
            {
                return $"Registered type '{alias}' does not allow construction.";
            }
            if (registration._descriptor.DataContract.Factory == null)
            {
                return $"Registered type '{alias}' requires a public parameterless constructor.";
            }
            return null;
        }

        internal static void AssignClrMember(
            object instance,
            ClrDataMember member,
            string name,
            ScriptDatum value,
            string path,
            bool pathIncludesMember)
        {
            if (member.Setter == null)
            {
                throw Error(
                    $"CLR member '{name}' is not writable.",
                    ErrorPath(path, name, pathIncludesMember));
            }
            if (!TryConvertClrValue(value, member.Type, out var converted))
            {
                throw Error(
                    $"Value cannot be converted to CLR type '{member.Type.FullName}'.",
                    ErrorPath(path, name, pathIncludesMember));
            }
            try
            {
                member.Setter.Setter(instance, converted);
            }
            catch (Exception exception)
            {
                throw Error(
                    "CLR member assignment failed.",
                    ErrorPath(path, name, pathIncludesMember),
                    exception);
            }
        }

        internal static bool TryConvertClrValue(ScriptDatum value, Type targetType, out object converted)
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

            var effective = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (effective.IsEnum)
            {
                if (value.Kind == ValueKind.Number &&
                    TryConvertClrNumber(value.Number, Enum.GetUnderlyingType(effective), out var underlying))
                {
                    converted = Enum.ToObject(effective, underlying);
                    return true;
                }
                converted = null;
                return false;
            }
            if (IsClrNumericType(effective))
            {
                if (value.Kind == ValueKind.Number)
                {
                    return TryConvertClrNumber(value.Number, effective, out converted);
                }
                converted = null;
                return false;
            }
            if (effective == typeof(bool))
            {
                if (value.Kind == ValueKind.Boolean)
                {
                    converted = value.Boolean;
                    return true;
                }
                converted = null;
                return false;
            }
            if (effective == typeof(char))
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
            return Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or
                TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or
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
                    converted = (sbyte)value; return true;
                case TypeCode.Byte when value >= byte.MinValue && value <= byte.MaxValue:
                    converted = (byte)value; return true;
                case TypeCode.Int16 when value >= short.MinValue && value <= short.MaxValue:
                    converted = (short)value; return true;
                case TypeCode.UInt16 when value >= ushort.MinValue && value <= ushort.MaxValue:
                    converted = (ushort)value; return true;
                case TypeCode.Int32 when value >= int.MinValue && value <= int.MaxValue:
                    converted = (int)value; return true;
                case TypeCode.UInt32 when value >= uint.MinValue && value <= uint.MaxValue:
                    converted = (uint)value; return true;
                case TypeCode.Int64 when value >= -9223372036854775808d && value < 9223372036854775808d:
                    converted = (long)value; return (long)converted == value;
                case TypeCode.UInt64 when value >= 0d && value < 18446744073709551616d:
                    converted = (ulong)value; return (ulong)converted == value;
                default:
                    converted = null; return false;
            }
        }

        private static string ElementPath(string path, int index) =>
            (string.IsNullOrEmpty(path) ? "$" : path) + "[" + index + "]";

        private static string MemberPath(string path, string name) =>
            (string.IsNullOrEmpty(path) ? "$" : path) + "." + name;

        private static string ErrorPath(string path, string name, bool pathIncludesMember) =>
            pathIncludesMember ? path : MemberPath(path, name);

        private static void Require(bool condition, string typeName, string message, string path)
        {
            if (!condition) throw Error($"Type '{typeName}' {message}", path);
        }

        private static TypedDocumentException Error(string message, string path, Exception inner = null) =>
            new(message, "<script>", 0, 0, path, inner);
    }
}
