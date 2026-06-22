using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ScriptDatumTests
{
    [Fact]
    public void FactoryMethodsPreserveKindAndPayload()
    {
        var nullValue = ScriptDatum.Null;
        var boolean = ScriptDatum.FromBoolean(true);
        var number = ScriptDatum.FromNumber(42.5);
        var text = ScriptDatum.FromString("Aurora");
        var array = ScriptDatum.FromArray(new ScriptArray());

        Assert.Equal(ValueKind.Null, nullValue.Kind);
        Assert.Equal(ValueKind.Boolean, boolean.Kind);
        Assert.True(boolean.Boolean);
        Assert.Equal(ValueKind.Number, number.Kind);
        Assert.Equal(42.5, number.Number);
        Assert.Equal(ValueKind.String, text.Kind);
        Assert.Equal("Aurora", text.String.Value);
        Assert.Equal(ValueKind.Array, array.Kind);
    }

    [Fact]
    public void EqualityHandlesSameKindsAndNumericCoercion()
    {
        Assert.Equal(ScriptDatum.FromNumber(1), ScriptDatum.FromNumber(1));
        Assert.NotEqual(ScriptDatum.FromNumber(1), ScriptDatum.FromNumber(2));
        Assert.Equal(ScriptDatum.FromNumber(1), ScriptDatum.FromBoolean(true));
        Assert.Equal(ScriptDatum.FromNumber(2), ScriptDatum.FromString("2"));
        Assert.NotEqual(ScriptDatum.FromString("2x"), ScriptDatum.FromNumber(2));
    }

    [Fact]
    public void MarshallerConvertsPrimitiveCollectionsAndDictionaries()
    {
        ScriptDatum number = ClrMarshaller.ToDatum(42);
        ScriptDatum text = ClrMarshaller.ToDatum("Aurora");
        ScriptDatum array = ClrMarshaller.ToDatum(new[] { 1, 2, 3 });
        ScriptDatum map = ClrMarshaller.ToDatum(new Dictionary<string, object> { ["value"] = 42 });

        ScriptAssert.Equal(42, number);
        ScriptAssert.Equal("Aurora", text);
        ScriptAssert.Equal(new object?[] { 1, 2, 3 }, array);
        Assert.Equal("42", map.Object.GetPropertyValue("value").ToString());
    }

    [Fact]
    public void SpanConversionHelpersHandleSuccessFailureAndBounds()
    {
        Span<ScriptDatum> values =
        [
            ScriptDatum.FromNumber(42),
            ScriptDatum.FromString("12.5"),
            ScriptDatum.FromBoolean(true),
            ScriptDatum.Null
        ];

        Assert.True(values.TryGetNumber(0, out var number));
        Assert.Equal(42, number);
        Assert.True(values.TryGetNumber(1, out var parsed));
        Assert.Equal(12.5, parsed);
        Assert.True(values.TryGetBoolean(2, out var boolean));
        Assert.True(boolean);
        Assert.True(values.TryGetString(3, out var nullText));
        Assert.Equal("null", nullText);
        Assert.False(values.TryGetNumber(99, out _));
    }
}
