using AuroraScript.Runtime;
using AuroraScript.Runtime.Debugging;
using AuroraScript.Runtime.Types;
using System.Linq;
using Xunit;

namespace AuroraScript.Tests;

public sealed class DebuggerViewTests
{
    [Fact]
    public void ScriptObjectDebugViewShowsScriptProperties()
    {
        var obj = new ScriptObject();
        obj.Define("Prop1", ScriptDatum.FromNumber(42));
        obj.Define("Prop2", ScriptDatum.FromString("text"));

        var view = new ScriptObjectDebugView(obj);

        Assert.Equal("object", ScriptDebugView.GetTypeName(obj));
        Assert.Equal("object", ScriptDebugView.FormatValue(obj));
        Assert.Equal(new[] { "Prop1", "Prop2" }, view.Properties.Select(property => property.Name).ToArray());
        Assert.Equal(new[] { "number", "string" }, view.Properties.Select(property => property.DisplayType).ToArray());
        Assert.Equal(new[] { "42", "\"text\"" }, view.Properties.Select(property => property.DisplayValue).ToArray());
    }

    [Fact]
    public void ScriptDatumDebugViewShowsObjectTypeAndProperties()
    {
        var obj = new ScriptObject();
        obj.Define("Prop1", ScriptDatum.FromBoolean(true));
        var datum = ScriptDatum.FromObject(obj);

        var view = new ScriptDatumDebugView(datum);

        Assert.Equal("object", ScriptDebugView.GetTypeName(datum));
        Assert.Equal("object", ScriptDebugView.FormatValue(datum));
        var property = Assert.Single(view.Properties);
        Assert.Equal("Prop1", property.Name);
        Assert.Equal("boolean", property.DisplayType);
        Assert.Equal("true", property.DisplayValue);
    }

    [Fact]
    public void ScriptDatumDebugViewShowsArrayPreviewAndElements()
    {
        var array = new ScriptArray();
        array.Push(ScriptDatum.FromNumber(1));
        array.Push(ScriptDatum.FromString("two"));
        array.Define("Prop1", ScriptDatum.FromBoolean(false));

        var view = new ScriptDatumDebugView(ScriptDatum.FromArray(array));

        Assert.Equal("array", ScriptDebugView.GetTypeName(ScriptDatum.FromArray(array)));
        Assert.Equal("[1, \"two\"]", ScriptDebugView.FormatValue(ScriptDatum.FromArray(array)));
        Assert.Equal(new[] { "[0]", "[1]", "Prop1" }, view.Properties.Select(property => property.Name).ToArray());
        Assert.Equal(new[] { "number", "string", "boolean" }, view.Properties.Select(property => property.DisplayType).ToArray());
        Assert.Equal(new[] { "1", "\"two\"", "false" }, view.Properties.Select(property => property.DisplayValue).ToArray());
    }

    [Fact]
    public void ScriptDatumDebugViewShowsNestedObjectAsTypeAndAllowsExpansion()
    {
        var child = new ScriptObject();
        child.Define("Name", ScriptDatum.FromString("inner"));
        var parent = new ScriptObject();
        parent.Define("Child", ScriptDatum.FromObject(child));

        var property = Assert.Single(new ScriptDatumDebugView(ScriptDatum.FromObject(parent)).Properties);

        Assert.Equal("Child", property.Name);
        Assert.Equal("object", property.DisplayType);
        Assert.Equal("object", property.DisplayValue);
        var childProperty = Assert.Single(property.Properties);
        Assert.Equal("Name", childProperty.Name);
        Assert.Equal("\"inner\"", childProperty.DisplayValue);
    }

    [Fact]
    public void ScriptDatumDebugViewShowsSpecialObjectsAsObjects()
    {
        var buffer = new StringBuffer("abc");
        buffer.Define("Prop1", ScriptDatum.FromNumber(1));
        var datum = ScriptDatum.FromObject(buffer);

        var view = new ScriptDatumDebugView(datum);

        Assert.Equal("StringBuffer", ScriptDebugView.GetTypeName(datum));
        Assert.Equal("StringBuffer", ScriptDebugView.FormatValue(datum));
        var property = Assert.Single(view.Properties);
        Assert.Equal("Prop1", property.Name);
        Assert.Equal("1", property.DisplayValue);
    }

    [Fact]
    public void ScriptObjectDebugViewShowsPrototypePropertiesWhenObjectHasNoOwnProperties()
    {
        var timer = new ScriptObject();
        timer.Define("timeId", ScriptDatum.FromNumber(1));
        timer.Define("count", ScriptDatum.FromNumber(50));
        var result = new ScriptObject(timer);

        var view = new ScriptObjectDebugView(result);

        Assert.Equal("object", ScriptDebugView.FormatValue(result));
        Assert.Equal(new[] { "timeId", "count" }, view.Properties.Select(property => property.Name).ToArray());
        Assert.Equal(new[] { "1", "50" }, view.Properties.Select(property => property.DisplayValue).ToArray());
    }

    [Fact]
    public void ScriptObjectDebugViewShowsEmptyNodeInsteadOfRawView()
    {
        var view = new ScriptObjectDebugView(new ScriptObject());

        var property = Assert.Single(view.Properties);
        Assert.Equal("(empty)", property.Name);
        Assert.Equal(string.Empty, property.DisplayValue);
    }

    [Fact]
    public void ScriptObjectDebugViewShowsHashMapEntriesAsKeyValueRows()
    {
        var map = new ScriptHashMap();
        map.Put(ScriptDatum.FromString("first"), ScriptDatum.FromNumber(1));
        map.Put(ScriptDatum.FromNumber(2), ScriptDatum.FromString("second"));

        var properties = new ScriptObjectDebugView(map).Properties;

        Assert.Equal(new[] { "first", "2" }, properties.Select(property => property.Name).ToArray());
        Assert.Equal(new[] { "1", "\"second\"" }, properties.Select(property => property.DisplayValue).ToArray());
        Assert.Equal(new[] { "number", "string" }, properties.Select(property => property.DisplayType).ToArray());
    }

    [Fact]
    public void ScriptDatumDebugViewIgnoresStaleObjectForPrimitiveKinds()
    {
        var obj = new ScriptObject();
        obj.Define("Prop1", ScriptDatum.FromNumber(42));
        var datum = ScriptDatum.FromObject(obj);
        ScriptDatum.WriteAsNumber(ref datum, 1);

        var view = new ScriptDatumDebugView(datum);

        Assert.Equal("number", ScriptDebugView.GetTypeName(datum));
        Assert.Equal("1", ScriptDebugView.FormatValue(datum));
        Assert.Empty(view.Properties);
    }
}
