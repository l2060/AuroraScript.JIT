using AuroraScript.Runtime;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using System;
using Xunit;

namespace AuroraScript.Tests;

public sealed class SerializationTests
{
    [Fact]
    public void SerializesPrimitiveDatumsWithInvariantJsonRepresentation()
    {
        var serializer = ScriptJsonSerializer.Default;

        Assert.Equal("null", serializer.Serialize(ScriptDatum.Null));
        Assert.Equal("true", serializer.Serialize(ScriptDatum.FromBoolean(true)));
        Assert.Equal("42.5", serializer.Serialize(ScriptDatum.FromNumber(42.5)));
        Assert.Equal("\"Aurora\\nScript\"", serializer.Serialize(ScriptDatum.FromString("Aurora\nScript")));
    }

    [Fact]
    public void SerializesNestedArraysAndObjectsAndSupportsIndentation()
    {
        var value = new ScriptObject();
        value.Define("name", StringValue.Of("Aurora"));
        value.Define("items", new ScriptArray([ScriptDatum.FromNumber(1), ScriptDatum.FromBoolean(true), ScriptDatum.Null]));

        var compact = ScriptJsonSerializer.Default.Serialize(ScriptDatum.FromObject(value));
        var indented = ScriptJsonSerializer.Default.Serialize(ScriptDatum.FromObject(value), indented: true);

        Assert.Equal("{\"name\":\"Aurora\",\"items\":[1,true,null]}", compact);
        Assert.Contains(Environment.NewLine, indented);
    }

    [Fact]
    public void DeserializesEveryJsonValueKind()
    {
        var value = ScriptJsonSerializer.Default.Deserialize(
            "{\"text\":\"x\",\"number\":2.5,\"yes\":true,\"no\":false,\"none\":null,\"items\":[1,2]}");

        Assert.Equal("x", value.GetPropertyValue("text").ToString());
        Assert.Equal("2.5", value.GetPropertyValue("number").ToString());
        Assert.Same(BooleanValue.True, value.GetPropertyValue("yes"));
        Assert.Same(BooleanValue.False, value.GetPropertyValue("no"));
        Assert.Same(ScriptObject.Null, value.GetPropertyValue("none"));
        Assert.Equal(2, Assert.IsType<ScriptArray>(value.GetPropertyValue("items")).Length);
    }

    [Fact]
    public void StandardSerializerRejectsCircularObjectAndArrayGraphs()
    {
        var objectValue = new ScriptObject();
        objectValue.Define("self", objectValue);
        var arrayValue = new ScriptArray(1);
        arrayValue.SetElement(0, ScriptDatum.FromObject(arrayValue));

        Assert.Throws<AuroraRuntimeException>(() => ScriptJsonSerializer.Default.Serialize(ScriptDatum.FromObject(objectValue)));
        Assert.Throws<AuroraRuntimeException>(() => ScriptJsonSerializer.Default.Serialize(ScriptDatum.FromObject(arrayValue)));
    }

    [Fact]
    public void SerializesNonFiniteNumbersAsNull()
    {
        var serializer = ScriptJsonSerializer.Default;

        Assert.Equal("null", serializer.Serialize(ScriptDatum.FromNumber(double.NaN)));
        Assert.Equal("null", serializer.Serialize(ScriptDatum.FromNumber(double.PositiveInfinity)));
        Assert.Equal("null", serializer.Serialize(ScriptDatum.FromNumber(double.NegativeInfinity)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("not-json")]
    [InlineData("[1,]")]
    public void RejectsMalformedJson(string json)
    {
        Assert.ThrowsAny<Exception>(() => ScriptJsonSerializer.Default.Deserialize(json));
    }
}
