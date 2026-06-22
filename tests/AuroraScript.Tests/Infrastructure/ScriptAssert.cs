using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using Xunit;

namespace AuroraScript.Tests.Infrastructure;

internal static class ScriptAssert
{
    public static void Equal(object? expected, ScriptDatum actual)
    {
        switch (expected)
        {
            case null:
                Assert.Equal(ValueKind.Null, actual.Kind);
                return;
            case bool boolean:
                Assert.Equal(ValueKind.Boolean, actual.Kind);
                Assert.Equal(boolean, actual.Boolean);
                return;
            case string text:
                Assert.Equal(ValueKind.String, actual.Kind);
                Assert.NotNull(actual.String);
                Assert.Equal(text, actual.String.Value);
                return;
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                Assert.Equal(ValueKind.Number, actual.Kind);
                Assert.Equal(Convert.ToDouble(expected), actual.Number, precision: 10);
                return;
            case object?[] expectedArray:
                Assert.Equal(ValueKind.Array, actual.Kind);
                var array = Assert.IsType<ScriptArray>(actual.Object);
                Assert.Equal(expectedArray.Length, array.Length);
                for (var i = 0; i < expectedArray.Length; i++)
                {
                    Equal(expectedArray[i], array.GetElement(i));
                }
                return;
            default:
                throw new ArgumentException($"Unsupported expected script value type: {expected.GetType().FullName}", nameof(expected));
        }
    }
}
