using AuroraScript.Runtime.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime.Interop
{
    /// <summary>
    /// A central utility class for marshalling values between the AuroraScript runtime and the .NET CLR.
    /// It handles type conversion, argument preparation for method calls, and wrapping .NET objects/delegates
    /// to be consumed by script code.
    /// </summary>
    public static class ClrMarshaller
    {
        /// <summary>
        /// Attempts to convert a <see cref="ScriptObject"/> to the specified .NET <see cref="Type"/>.
        /// </summary>
        /// <param name="scriptValue">The script object to convert.</param>
        /// <param name="targetType">The desired .NET type.</param>
        /// <param name="result">When this method returns, contains the converted .NET object if successful.</param>
        /// <returns>True if the conversion was successful; otherwise, false.</returns>
        public static bool TryConvertArgument(ScriptObject scriptValue, Type targetType, out object result)
        {
            result = null;
            if (scriptValue != null && targetType.IsInstanceOfType(scriptValue))
            {
                result = scriptValue;
                return true;
            }
            if (targetType == typeof(ScriptObject) || targetType == typeof(ScriptObject[]))
            {
                result = scriptValue;
                return true;
            }

            if (scriptValue == ScriptObject.Null)
            {
                if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                {
                    result = null;
                    return true;
                }
                return false;
            }

            if (targetType == typeof(string))
            {
                if (scriptValue is StringValue str)
                {
                    result = str.Value;
                    return true;
                }
                result = scriptValue.ToString();
                return true;
            }

            if (IsNumericType(targetType))
            {
                if (scriptValue is NumberValue number)
                {
                    try
                    {
                        result = Convert.ChangeType(number.DoubleValue, Nullable.GetUnderlyingType(targetType) ?? targetType, CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                return false;
            }

            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                if (scriptValue is BooleanValue booleanValue)
                {
                    result = booleanValue.Value;
                    return true;
                }
                result = scriptValue.IsTrue();
                return true;
            }

            if (scriptValue is ScriptArray scriptArray)
            {
                if (TryConvertScriptArray(scriptArray, targetType, out result))
                {
                    return true;
                }
            }

            if (scriptValue is ScriptPackedArray packedArray &&
                TryConvertPackedArray(packedArray, targetType, out result))
            {
                return true;
            }

            if (targetType.IsAssignableFrom(typeof(ScriptObject)))
            {
                result = scriptValue;
                return true;
            }

            if (scriptValue is ClrInstanceObject clrInstance)
            {
                var instance = clrInstance.Instance;
                if (instance == null)
                {
                    result = null;
                    return !targetType.IsValueType;
                }
                if (targetType.IsInstanceOfType(instance))
                {
                    result = instance;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to convert a <see cref="ScriptDatum"/> to the specified .NET <see cref="Type"/>.
        /// This is an optimized version that avoids object boxing where possible.
        /// </summary>
        /// <param name="datum">The script datum to convert.</param>
        /// <param name="targetType">The desired .NET type.</param>
        /// <param name="result">When this method returns, contains the converted .NET object if successful.</param>
        /// <returns>True if the conversion was successful; otherwise, false.</returns>
        public static bool TryConvertArgument(in ScriptDatum datum, Type targetType, out object result)
        {
            switch (datum.Kind)
            {
                case ValueKind.Null:
                    if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                    {
                        result = null;
                        return true;
                    }
                    result = null;
                    return false;
                case ValueKind.Boolean:
                    if (targetType == typeof(bool) || targetType == typeof(bool?))
                    {
                        result = datum.Boolean;
                        return true;
                    }
                    if (IsNumericType(targetType))
                    {
                        return TryConvertNumber(datum.Boolean ? 1d : 0d, targetType, out result);
                    }
                    break;
                case ValueKind.Number:
                    if (IsNumericType(targetType))
                    {
                        return TryConvertNumber(datum.Number, targetType, out result);
                    }
                    if (targetType == typeof(bool) || targetType == typeof(bool?))
                    {
                        result = datum.Number != 0 && !double.IsNaN(datum.Number);
                        return true;
                    }
                    break;
                case ValueKind.String:
                    if (targetType == typeof(string))
                    {
                        result = datum.StringText;
                        return true;
                    }
                    if (targetType == typeof(ScriptObject))
                    {
                        result = datum.String;
                        return true;
                    }
                    break;
                case ValueKind.Object:
                    return TryConvertArgument(datum.Object, targetType, out result);

                case ValueKind.Array:
                    if (datum.Object is ScriptArray arrayValue && TryConvertScriptArray(arrayValue, targetType, out result))
                    {
                        return true;
                    }
                    break;


            }
            result = null;
            return false;
        }

        /// <summary>
        /// Converts a .NET object to a <see cref="ScriptDatum"/>.
        /// </summary>
        /// <param name="value">The .NET object to convert.</param>
        /// <returns>A script datum representing the provided value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum ToDatum(object value)
        {
            ScriptDatum datum = default;
            ClrMarshaller.WriteToDatum(ref datum, value);
            return datum;
        }

        /// <summary>
        /// Writes a .NET object value into the provided <see cref="ScriptDatum"/> reference.
        /// This method contains the core mapping logic for various .NET types.
        /// </summary>
        /// <param name="datum">The target script datum.</param>
        /// <param name="value">The .NET object to write.</param>
        public static void WriteToDatum(ref ScriptDatum datum, object value)
        {
            switch (value)
            {
                case null:
                    datum = default;
                    return;
                case ScriptDatum datum2:
                    datum = datum2;
                    return;

                case Char b:
                    ScriptDatum.WriteAsString(ref datum, b.ToString());
                    return;
                case Byte b:
                    ScriptDatum.WriteAsNumber(ref datum, b);
                    return;
                case SByte n:
                    ScriptDatum.WriteAsNumber(ref datum, n);
                    return;
                case Single n:
                    ScriptDatum.WriteAsNumber(ref datum, n);
                    return;
                case Double n:
                    ScriptDatum.WriteAsNumber(ref datum, n);
                    return;
                case Int16 n:
                    ScriptDatum.WriteAsNumber(ref datum, n);
                    return;
                case Int32 n:
                    ScriptDatum.WriteAsNumber(ref datum, n);
                    return;
                case Int64 n:
                    ScriptDatum.WriteAsNumber(ref datum, n);
                    return;
                case UInt16 n:
                    ScriptDatum.WriteAsNumber(ref datum, n);
                    return;
                case UInt32 n:
                    ScriptDatum.WriteAsNumber(ref datum, n);
                    return;
                case UInt64 n:
                    ScriptDatum.WriteAsNumber(ref datum, n);
                    return;
                case Decimal n:
                    ScriptDatum.WriteAsNumber(ref datum, (double)n);
                    return;

                case ScriptObject scriptObject:
                    ScriptDatum.WriteObject(ref datum, scriptObject);
                    return;

                case bool boolean:
                    ScriptDatum.WriteAsBoolean(ref datum, boolean);
                    return;

                case DateTime dateTime:
                    ScriptDatum.WriteAsDate(ref datum, dateTime);
                    return;

                case DateTimeOffset dateTime2:
                    ScriptDatum.WriteAsDate(ref datum, dateTime2);
                    return;

                case string str:
                    ScriptDatum.WriteAsString(ref datum, str);
                    return;

                case Enum enumValue:
                    ScriptDatum.WriteAsNumber(ref datum, Convert.ToInt32(enumValue, CultureInfo.InvariantCulture));
                    return;

                case Delegate handler:
                    ScriptDatum.WriteAsClrBonding(ref datum, WrapDelegate(handler));
                    return;

                case IDictionary dictionary:
                    ScriptDatum.WriteAsObject(ref datum, ConvertDictionary(dictionary));
                    return;

                case IEnumerable enumerable:
                    ScriptDatum.WriteAsArray(ref datum, ToDatumArray(enumerable));
                    return;
            }
            if (ClrTypeResolver.ResolveType(value.GetType(), out var descriptor))
            {
                ScriptDatum.WriteAsObject(ref datum, new ClrInstanceObject(descriptor, value));
                return;
            }
            throw new InvalidOperationException($"The return type '{value.GetType().FullName}' is not registered for CLR interop.");
        }

        /// <summary>
        /// Converts an array of .NET objects to an array of <see cref="ScriptDatum"/>.
        /// </summary>
        /// <param name="values">The .NET objects to convert.</param>
        /// <returns>An array of script datums.</returns>
        public static ScriptDatum[] ToDatums(object[] values)
        {
            if (values == null || values.Length == 0) return Array.Empty<ScriptDatum>();
            var result = new ScriptDatum[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                ClrMarshaller.WriteToDatum(ref result[i], values[i]);
            }
            return result;
        }

        /// <summary>
        /// Converts an array of <see cref="ScriptObject"/> to an array of <see cref="ScriptDatum"/>.
        /// </summary>
        /// <param name="arguments">The script objects to convert.</param>
        /// <returns>An array of script datums.</returns>
        public static ScriptDatum[] ToDatums(ScriptObject[] arguments)
        {
            if (arguments == null || arguments.Length == 0) return Array.Empty<ScriptDatum>();
            var result = new ScriptDatum[arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
            {
                ScriptDatum.WriteObject(ref result[i], arguments[i]);
            }
            return result;
        }

        /// <summary>
        /// Converts a .NET object to its corresponding <see cref="ScriptObject"/> representation.
        /// </summary>
        /// <param name="value">The .NET object to convert.</param>
        /// <returns>The script object representation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptObject ToScript(object value)
        {
            if (value is ScriptObject scriptObject)
            {
                return scriptObject;
            }
            ScriptDatum datum = default;
            ClrMarshaller.WriteToDatum(ref datum, value);
            return ScriptDatum.ToObject(datum);
        }

        /// <summary>
        /// Converts a .NET <see cref="IDictionary"/> to a <see cref="ScriptObject"/>.
        /// </summary>
        private static ScriptObject ConvertDictionary(IDictionary dictionary)
        {
            var obj = new ScriptObject();
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = entry.Key.ToString() ?? string.Empty;
                obj.SetPropertyValue(key, ToScript(entry.Value));
            }
            return obj;
        }

        /// <summary>
        /// Converts a .NET <see cref="IEnumerable"/> to a <see cref="ScriptArray"/>.
        /// </summary>
        private static ScriptArray ToDatumArray(IEnumerable values)
        {
            var array = new ScriptArray();
            if (values != null)
            {
                foreach (var item in values)
                {
                    ScriptDatum datum = default;
                    ClrMarshaller.WriteToDatum(ref datum, item);
                    array.Push(datum);
                }
            }
            return array;
        }

        /// <summary>
        /// Determines whether the specified type is a .NET numeric type.
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryConvertNumber(double value, Type targetType, out object result)
        {
            var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Double:
                    result = value;
                    return true;
                case TypeCode.Single:
                    result = (float)value;
                    return true;
                case TypeCode.Int32:
                    result = (int)value;
                    return true;
                case TypeCode.Int64:
                    result = (long)value;
                    return true;
                case TypeCode.Int16:
                    result = (short)value;
                    return true;
                case TypeCode.Byte:
                    result = (byte)value;
                    return true;
                case TypeCode.SByte:
                    result = (sbyte)value;
                    return true;
                case TypeCode.UInt16:
                    result = (ushort)value;
                    return true;
                case TypeCode.UInt32:
                    result = (uint)value;
                    return true;
                case TypeCode.UInt64:
                    result = (ulong)value;
                    return true;
                case TypeCode.Decimal:
                    result = (decimal)value;
                    return true;
                default:
                    result = null;
                    return false;
            }
        }

        /// <summary>
        /// Attempts to convert a <see cref="ScriptArray"/> to a .NET collection type (Array, List, etc.).
        /// </summary>
        private static bool TryConvertPackedArray(
            ScriptPackedArray array,
            Type targetType,
            out object result)
        {
            object storage = array switch
            {
                ScriptInt32Array int32 => int32._items,
                ScriptInt8Array int8 => int8._items,
                ScriptFloat32Array float32 => float32._items,
                ScriptFloat64Array float64 => float64._items,
                ScriptBooleanArray boolean => boolean._items,
                ScriptUInt8Array uint8 => uint8._items,
                ScriptInt16Array int16 => int16._items,
                ScriptUInt16Array uint16 => uint16._items,
                ScriptUInt32Array uint32 => uint32._items,
                ScriptInt64Array int64 => int64._items,
                ScriptUInt64Array uint64 => uint64._items,
                _ => null
            };
            if (storage != null && targetType.IsInstanceOfType(storage))
            {
                result = storage;
                return true;
            }

            result = null;
            return false;
        }

        private static bool TryConvertScriptArray(ScriptArray scriptArray, Type targetType, out object result)
        {
            if (targetType == typeof(ScriptArray) || typeof(ScriptArray).IsAssignableFrom(targetType))
            {
                result = scriptArray;
                return true;
            }

            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType() ?? typeof(object);
                return TryConvertToClrArray(scriptArray, elementType, out result);
            }

            if (TryGetGenericEnumerableElementType(targetType, out var element))
            {
                if (!TryConvertToTypedList(scriptArray, element, out var listObject))
                {
                    result = null;
                    return false;
                }

                if (targetType.IsInterface || targetType.IsAssignableFrom(listObject.GetType()))
                {
                    result = listObject;
                    return true;
                }

                var enumerableType = typeof(IEnumerable<>).MakeGenericType(element);
                var ctor = targetType.GetConstructor(new[] { enumerableType });
                if (ctor != null)
                {
                    result = ctor.Invoke(new[] { listObject });
                    return true;
                }

                if (!targetType.IsAbstract && typeof(IList).IsAssignableFrom(targetType))
                {
                    var concreteList = (IList)Activator.CreateInstance(targetType);
                    foreach (var item in (IEnumerable)listObject)
                    {
                        concreteList.Add(item);
                    }
                    result = concreteList;
                    return true;
                }

                result = null;
                return false;
            }

            if (typeof(IList).IsAssignableFrom(targetType))
            {
                IList listInstance;
                if (targetType.IsInterface || targetType.IsAbstract)
                {
                    listInstance = new ArrayList();
                }
                else
                {
                    listInstance = (IList)Activator.CreateInstance(targetType);
                }

                for (int i = 0; i < scriptArray.Length; i++)
                {
                    ScriptDatum datum = default;
                    scriptArray.GetElement(i, ref datum);
                    listInstance.Add(ScriptDatum.ToObject(datum));
                }

                result = listInstance;
                return true;
            }

            if (typeof(IEnumerable).IsAssignableFrom(targetType) && (targetType.IsInterface || targetType == typeof(IEnumerable)))
            {
                var arrayList = new ArrayList();
                for (int i = 0; i < scriptArray.Length; i++)
                {
                    ScriptDatum datum = default;
                    scriptArray.GetElement(i, ref datum);
                    arrayList.Add(ScriptDatum.ToObject(datum));
                }
                result = arrayList;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Converts a <see cref="ScriptArray"/> to a .NET array of the specified element type.
        /// </summary>
        private static bool TryConvertToClrArray(ScriptArray scriptArray, Type elementType, out object result)
        {
            var length = scriptArray.Length;
            var values = scriptArray.Values();

            if (elementType == typeof(string))
            {
                var strings = new string[length];
                for (int i = 0; i < length; i++)
                {
                    ref readonly var datum = ref values[i];
                    if (datum.Kind == ValueKind.String)
                    {
                        strings[i] = datum.StringText;
                        continue;
                    }
                    if (!TryConvertArgument(in datum, elementType, out var converted))
                    {
                        if (!TryFallbackArrayConversion(datum, elementType, out converted))
                        {
                            result = null;
                            return false;
                        }
                    }
                    strings[i] = (string)converted;
                }

                result = strings;
                return true;
            }

            if (elementType == typeof(int))
            {
                var numbers = new int[length];
                for (int i = 0; i < length; i++)
                {
                    ref readonly var datum = ref values[i];
                    if (datum.Kind == ValueKind.Number)
                    {
                        numbers[i] = (int)datum.Number;
                        continue;
                    }

                    if (!TryConvertArgument(in datum, elementType, out var converted))
                    {
                        result = null;
                        return false;
                    }

                    numbers[i] = (int)converted;
                }

                result = numbers;
                return true;
            }

            if (elementType == typeof(double))
            {
                var numbers = new double[length];
                for (int i = 0; i < length; i++)
                {
                    ref readonly var datum = ref values[i];
                    if (datum.Kind == ValueKind.Number)
                    {
                        numbers[i] = datum.Number;
                        continue;
                    }

                    if (!TryConvertArgument(in datum, elementType, out var converted))
                    {
                        result = null;
                        return false;
                    }

                    numbers[i] = (double)converted;
                }

                result = numbers;
                return true;
            }

            if (elementType == typeof(bool))
            {
                var booleans = new bool[length];
                for (int i = 0; i < length; i++)
                {
                    ref readonly var datum = ref values[i];
                    if (datum.Kind == ValueKind.Boolean)
                    {
                        booleans[i] = datum.Boolean;
                        continue;
                    }

                    if (!TryConvertArgument(in datum, elementType, out var converted))
                    {
                        result = null;
                        return false;
                    }

                    booleans[i] = (bool)converted;
                }

                result = booleans;
                return true;
            }

            if (elementType == typeof(ScriptDatum))
            {
                var datums = new ScriptDatum[length];
                values.CopyTo(datums);
                result = datums;
                return true;
            }

            if (elementType == typeof(object))
            {
                var objects = new object[length];
                for (int i = 0; i < length; i++)
                {
                    objects[i] = ScriptDatum.ToObject(values[i]);
                }

                result = objects;
                return true;
            }

            var arrayInstance = Array.CreateInstance(elementType, length);

            for (int i = 0; i < length; i++)
            {
                ref readonly var datum = ref values[i];
                if (!TryConvertArgument(in datum, elementType, out var converted))
                {
                    if (!TryFallbackArrayConversion(datum, elementType, out converted))
                    {
                        result = null;
                        return false;
                    }
                }
                arrayInstance.SetValue(converted, i);
            }

            result = arrayInstance;
            return true;
        }

        /// <summary>
        /// Converts a <see cref="ScriptArray"/> to a .NET <see cref="List{T}"/> of the specified element type.
        /// </summary>
        private static bool TryConvertToTypedList(ScriptArray scriptArray, Type elementType, out object listObject)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < scriptArray.Length; i++)
            {
                var datum = scriptArray.GetElement(i);
                if (!TryConvertArgument(in datum, elementType, out var converted))
                {
                    if (!TryFallbackArrayConversion(datum, elementType, out converted))
                    {
                        listObject = null;
                        return false;
                    }
                }
                list.Add(converted);
            }

            listObject = list;
            return true;
        }

        /// <summary>
        /// Performs a fallback conversion for array elements when direct conversion fails.
        /// Handles wrapping values in <see cref="ScriptObject"/> or <see cref="ScriptDatum"/>.
        /// </summary>
        private static bool TryFallbackArrayConversion(ScriptDatum datum, Type elementType, out object converted)
        {
            if (elementType == typeof(object) || elementType == typeof(ScriptObject))
            {
                converted = ScriptDatum.ToObject(datum);
                return true;
            }

            if (elementType == typeof(ScriptDatum))
            {
                converted = datum;
                return true;
            }

            converted = null;
            return false;
        }

        /// <summary>
        /// Attempts to extract the element type from a generic collection type.
        /// </summary>
        private static bool TryGetGenericEnumerableElementType(Type targetType, out Type elementType)
        {
            if (targetType.IsGenericType && IsSupportedGenericEnumerable(targetType.GetGenericTypeDefinition()))
            {
                elementType = targetType.GetGenericArguments()[0];
                return true;
            }

            foreach (var interfaceType in targetType.GetInterfaces())
            {
                if (interfaceType.IsGenericType && IsSupportedGenericEnumerable(interfaceType.GetGenericTypeDefinition()))
                {
                    elementType = interfaceType.GetGenericArguments()[0];
                    return true;
                }
            }

            elementType = null;
            return false;
        }

        /// <summary>
        /// Checks if the specified generic type definition is a supported enumerable interface.
        /// </summary>
        private static bool IsSupportedGenericEnumerable(Type genericDefinition)
        {
            return genericDefinition == typeof(IEnumerable<>) ||
                   genericDefinition == typeof(ICollection<>) ||
                   genericDefinition == typeof(IList<>) ||
                   genericDefinition == typeof(IReadOnlyCollection<>) ||
                   genericDefinition == typeof(IReadOnlyList<>);
        }





        /// <summary>
        /// Wraps a .NET <see cref="Delegate"/> into a <see cref="ClrDatumDelegate"/> that can be invoked from scripts.
        /// Handles the conversion of script arguments to the delegate's parameter types.
        /// </summary>
        private static ClrDatumDelegate WrapDelegate(Delegate handler)
        {
            if (handler is ClrDatumDelegate datumDelegate)
            {
                return datumDelegate;
            }
            return (context, thisObject, args, ref result) =>
            {
                var prepared = PrepareDelegateArguments(handler, args);
                var clrResult = handler.DynamicInvoke(prepared);
                ClrMarshaller.WriteToDatum(ref result, clrResult);
            };
        }

        /// <summary>
        /// Prepares the arguments for a delegate invocation, handling type conversion, 
        /// variadic parameters (params), and default values.
        /// </summary>
        private static object[] PrepareDelegateArguments(Delegate handler, Span<ScriptDatum> args)
        {
            var parameters = handler.Method.GetParameters();
            if (parameters.Length == 0)
            {
                return Array.Empty<object>();
            }

            var prepared = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (IsParamArray(parameters[i]))
                {
                    prepared[i] = ConvertParamArray(parameters[i], args, i);
                    continue;
                }

                if (i < args.Length)
                {
                    if (!ClrMarshaller.TryConvertArgument(in args[i], parameters[i].ParameterType, out var converted))
                    {
                        throw new InvalidOperationException($"Cannot convert script argument #{i} to '{parameters[i].ParameterType.FullName}' for delegate '{handler.Method.DeclaringType?.Name}.{handler.Method.Name}'.");
                    }
                    prepared[i] = converted;
                    continue;
                }

                if (parameters[i].HasDefaultValue)
                {
                    prepared[i] = parameters[i].DefaultValue;
                    continue;
                }

                prepared[i] = parameters[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(parameters[i].ParameterType) == null
                    ? Activator.CreateInstance(parameters[i].ParameterType)
                    : null;
            }

            return prepared;
        }

        /// <summary>
        /// Determines whether the specified parameter is marked with the <see cref="ParamArrayAttribute"/>.
        /// </summary>
        private static bool IsParamArray(ParameterInfo parameter)
        {
            return parameter.GetCustomAttribute<ParamArrayAttribute>() != null;
        }

        /// <summary>
        /// Converts the remaining script arguments into a .NET array for a variadic (params) parameter.
        /// </summary>
        private static object ConvertParamArray(ParameterInfo parameter, Span<ScriptDatum> args, int startIndex)
        {
            var elementType = parameter.ParameterType.GetElementType() ?? typeof(object);
            var available = Math.Max(0, args.Length - startIndex);
            var array = Array.CreateInstance(elementType, available);
            for (int offset = 0; offset < available; offset++)
            {
                if (!ClrMarshaller.TryConvertArgument(in args[startIndex + offset], elementType, out var converted))
                {
                    throw new InvalidOperationException($"Cannot convert variadic script argument #{startIndex + offset} to '{elementType.FullName}'.");
                }
                array.SetValue(converted, offset);
            }

            return array;
        }

        /// <summary>
        /// Attempts to build an array of .NET objects from script arguments that match 
        /// the signature of the specified method.
        /// </summary>
        internal static bool TryBuildArguments(MethodBase method, Span<ScriptDatum> args, out object[] invokeArgs)
        {
            return TryBuildArguments(method.GetParameters(), args, out invokeArgs);
        }

        internal static bool TryBuildArguments(ParameterInfo[] parameters, Span<ScriptDatum> args, out object[] invokeArgs)
        {
            var hasParamArray = parameters.Length > 0 && IsParamArray(parameters[^1]);
            var requiredCount = 0;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (IsParamArray(parameters[i]))
                {
                    break;
                }

                if (!parameters[i].HasDefaultValue)
                {
                    requiredCount++;
                }
            }

            if (args.Length < requiredCount || (!hasParamArray && args.Length > parameters.Length))
            {
                invokeArgs = null;
                return false;
            }

            invokeArgs = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (IsParamArray(parameters[i]))
                {
                    invokeArgs[i] = ConvertParamArray(parameters[i], args, i);
                    return true;
                }

                if (i >= args.Length)
                {
                    if (parameters[i].HasDefaultValue)
                    {
                        invokeArgs[i] = parameters[i].DefaultValue;
                        continue;
                    }

                    invokeArgs = null;
                    return false;
                }

                if (!ClrMarshaller.TryConvertArgument(in args[i], parameters[i].ParameterType, out var converted))
                {
                    invokeArgs = null;
                    return false;
                }
                invokeArgs[i] = converted;
            }
            return true;
        }

        internal static bool TryBuildArguments(ParameterInfo[] parameters, Span<ScriptDatum> args, object[] invokeArgs)
        {
            var hasParamArray = parameters.Length > 0 && IsParamArray(parameters[^1]);
            var requiredCount = 0;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (IsParamArray(parameters[i]))
                {
                    break;
                }

                if (!parameters[i].HasDefaultValue)
                {
                    requiredCount++;
                }
            }

            if (args.Length < requiredCount || (!hasParamArray && args.Length > parameters.Length))
            {
                return false;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (IsParamArray(parameters[i]))
                {
                    invokeArgs[i] = ConvertParamArray(parameters[i], args, i);
                    return true;
                }

                if (i >= args.Length)
                {
                    if (parameters[i].HasDefaultValue)
                    {
                        invokeArgs[i] = parameters[i].DefaultValue;
                        continue;
                    }

                    return false;
                }

                if (!ClrMarshaller.TryConvertArgument(in args[i], parameters[i].ParameterType, out var converted))
                {
                    return false;
                }
                invokeArgs[i] = converted;
            }
            return true;
        }



    }
}

