using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Host;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace AuroraScript.Tests;

public sealed class TypedDocumentSerializationTests
{
    [Fact]
    public void DeserializesStandaloneDocumentWithoutMarkerAndPreservesCoreTypes()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        const string document = """
            // standalone documents start directly with their root value
            Object {
                readonly String id "u-001",
                String name "Hanks",
                Object meta {
                    phone "1526-255",
                    tags [String "A", "B", 100],
                },
                Int8Array signedBytes [-128, 0, 127],
                StringBuffer greeting "hello",
                Date createdAt "2024-11-23 15:26:33",
                Path scriptPath "a/b/c/d.as",
            }
            """;

        var datum = TypedDocumentSerializer.Deserialize(engine, document);
        var root = Assert.IsType<ScriptObject>(datum.Object);
        Assert.Equal("u-001", root.GetPropertyDatum(null, "id").StringText);
        Assert.Equal("Hanks", root.GetPropertyDatum(null, "name").StringText);

        var meta = Assert.IsType<ScriptObject>(root.GetPropertyDatum(null, "meta").Object);
        Assert.Equal("1526-255", meta.GetPropertyDatum(null, "phone").StringText);
        Assert.Equal(3, Assert.IsType<ScriptArray>(meta.GetPropertyDatum(null, "tags").Object).Length);

        var bytes = Assert.IsType<ScriptInt8Array>(root.GetPropertyDatum(null, "signedBytes").Object);
        Assert.Equal(-128, bytes.GetElement(0));
        Assert.Equal(127, bytes.GetElement(2));
        Assert.Equal("hello", Assert.IsType<StringBuffer>(root.GetPropertyDatum(null, "greeting").Object).ToString());
        Assert.IsType<ScriptDate>(root.GetPropertyDatum(null, "createdAt").Object);
        Assert.Equal("a/b/c/d.as", Assert.IsType<ScriptPathValue>(root.GetPropertyDatum(null, "scriptPath").Object).Value);

        Assert.ThrowsAny<AuroraException>(() => root.Define("id", ScriptDatum.FromString("changed")));
        meta.Define("phone", ScriptDatum.FromString("changed"));
        Assert.Equal("changed", meta.GetPropertyDatum(null, "phone").StringText);
    }

    [Fact]
    public void SerializesCanonicalDocumentAndRoundTripsReadonlyAndSpecialTypes()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var root = new ScriptObject();
        root.Define("id", ScriptDatum.FromString("u-001"), writeable: false);
        root.Define("bytes", ScriptDatum.FromObject(CreateBytes()));
        root.Define("createdAt", ScriptDatum.FromDate(new DateTimeOffset(2024, 11, 23, 15, 26, 33, TimeSpan.Zero)));
        root.Define("path", ScriptDatum.FromObject(new ScriptPathValue("a/b.as")));

        var text = TypedDocumentSerializer.Serialize(engine, ScriptDatum.FromObject(root));

        Assert.DoesNotContain("tdoc", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Object ", text, StringComparison.Ordinal);
        Assert.Contains("readonly id \"u-001\"", text, StringComparison.Ordinal);
        Assert.Contains("Int8Array bytes", text, StringComparison.Ordinal);
        Assert.Contains("Date createdAt \"2024-11-23 15:26:33\"", text, StringComparison.Ordinal);

        var restored = Assert.IsType<ScriptObject>(TypedDocumentSerializer.Deserialize(engine, text).Object);
        Assert.IsType<ScriptInt8Array>(restored.GetPropertyDatum(null, "bytes").Object);
        Assert.IsType<ScriptDate>(restored.GetPropertyDatum(null, "createdAt").Object);
        Assert.IsType<ScriptPathValue>(restored.GetPropertyDatum(null, "path").Object);
        Assert.ThrowsAny<AuroraException>(() => restored.Define("id", ScriptDatum.FromString("changed")));
    }

    [Fact]
    public void DefaultsToOnlyInferableTypeNames()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var values = new ScriptArray();
        values.Push(ScriptDatum.FromString("A"));
        values.Push(ScriptDatum.FromNumber(2));

        var root = new ScriptObject();
        root.Define("name", ScriptDatum.FromString("Aurora"));
        root.Define("enabled", ScriptDatum.True);
        root.Define("values", ScriptDatum.FromArray(values));
        root.Define("bytes", ScriptDatum.FromObject(CreateBytes()));
        root.Define("createdAt", ScriptDatum.FromDate(new DateTimeOffset(2024, 11, 23, 15, 26, 33, TimeSpan.Zero)));

        var text = TypedDocumentSerializer.Serialize(
            engine,
            ScriptDatum.FromObject(root),
            new TypedDocumentOptions { Indented = false });

        Assert.False(TypedDocumentOptions.Default.EmitTypeNames);
        Assert.StartsWith("{", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Object ", text, StringComparison.Ordinal);
        Assert.DoesNotContain("String name", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Boolean enabled", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Array values", text, StringComparison.Ordinal);
        Assert.Contains("Int8Array bytes", text, StringComparison.Ordinal);
        Assert.Contains("Date createdAt", text, StringComparison.Ordinal);

        var restored = Assert.IsType<ScriptObject>(TypedDocumentSerializer.Deserialize(engine, text).Object);
        Assert.Equal("Aurora", restored.GetPropertyDatum(null, "name").StringText);
        Assert.True(restored.GetPropertyDatum(null, "enabled").Boolean);
        Assert.IsType<ScriptArray>(restored.GetPropertyDatum(null, "values").Object);
        Assert.IsType<ScriptInt8Array>(restored.GetPropertyDatum(null, "bytes").Object);
        Assert.IsType<ScriptDate>(restored.GetPropertyDatum(null, "createdAt").Object);
    }

    [Fact]
    public void SerializesVisibleEnumerablePrototypePropertiesAsDocumentMembers()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var prototype = new ScriptObject();
        prototype.Define("name", ScriptDatum.FromString("Aurora"));
        prototype.Define("version", ScriptDatum.FromNumber(4), writeable: false);
        var wrapper = new ScriptObject(prototype);

        var text = TypedDocumentSerializer.Serialize(
            engine,
            ScriptDatum.FromObject(wrapper),
            new TypedDocumentOptions { Indented = false });

        Assert.Equal("{name \"Aurora\",readonly version 4}", text);

        var allTypes = TypedDocumentSerializer.Serialize(
            engine,
            ScriptDatum.FromObject(wrapper),
            new TypedDocumentOptions { Indented = false, EmitTypeNames = true });
        Assert.Equal("Object {String name \"Aurora\",readonly Number version 4}", allTypes);

        var restored = Assert.IsType<ScriptObject>(TypedDocumentSerializer.Deserialize(engine, allTypes).Object);
        Assert.Equal("Aurora", restored.GetPropertyDatum(null, "name").StringText);
        Assert.Equal(4, restored.GetPropertyDatum(null, "version").Number);
        Assert.ThrowsAny<AuroraException>(() => restored.Define("version", ScriptDatum.FromNumber(5)));
    }

    [Fact]
    public void AuroraTypedDocumentReadsAndWritesTextFilesAndStreams()
    {
        using var workspace = new TestWorkspace();
        var engine = new AuroraEngine(EngineOptions.Default);
        var tdoc = new AuroraTypedDocument(engine);
        var root = new ScriptObject();
        root.Define("name", ScriptDatum.FromString("Aurora"));
        root.Define("enabled", ScriptDatum.True);
        var value = ScriptDatum.FromObject(root);

        var text = tdoc.Serialize(value, new TypedDocumentOptions { Indented = false });
        var fromText = Assert.IsType<ScriptObject>(tdoc.Deserialize(text).Object);
        Assert.Equal("Aurora", fromText.GetPropertyDatum(null, "name").StringText);

        using var stream = new MemoryStream();
        tdoc.WriteStream(stream, value, new TypedDocumentOptions { Indented = false });
        Assert.True(stream.CanRead);
        stream.Position = 0;
        var fromStream = Assert.IsType<ScriptObject>(tdoc.ReadStream(stream).Object);
        Assert.True(stream.CanRead);
        Assert.True(fromStream.GetPropertyDatum(null, "enabled").Boolean);

        var path = Path.Combine(workspace.Root, "settings.tdoc");
        tdoc.WriteFile(path, value, new TypedDocumentOptions { Indented = false });
        Assert.True(File.Exists(path));
        var fromFile = Assert.IsType<ScriptObject>(tdoc.ReadFile(path).Object);
        Assert.Equal("Aurora", fromFile.GetPropertyDatum(null, "name").StringText);
    }

    [Fact]
    public void WidePackedArraysRoundTripThroughTextAndStreamWithoutNumberConversion()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var tdoc = new AuroraTypedDocument(engine);
        var signed = new ScriptInt64Array(3);
        signed.SetElement(0, long.MinValue);
        signed.SetElement(1, 9007199254740993L);
        signed.SetElement(2, long.MaxValue);
        var unsigned = new ScriptUInt64Array(3);
        unsigned.SetElement(0, 0UL);
        unsigned.SetElement(1, 9007199254740993UL);
        unsigned.SetElement(2, ulong.MaxValue);

        var root = new ScriptObject();
        root.Define("signed", ScriptDatum.FromObject(signed));
        root.Define("unsigned", ScriptDatum.FromObject(unsigned));
        var value = ScriptDatum.FromObject(root);
        var options = new TypedDocumentOptions { Indented = false };

        var text = tdoc.Serialize(value, options);
        Assert.Contains("9007199254740993", text, StringComparison.Ordinal);
        Assert.Contains("18446744073709551615", text, StringComparison.Ordinal);
        var fromText = Assert.IsType<ScriptObject>(tdoc.Deserialize(text).Object);
        Assert.Equal(long.MaxValue, Assert.IsType<ScriptInt64Array>(fromText.GetPropertyDatum(null, "signed").Object).GetElement(2));
        Assert.Equal(ulong.MaxValue, Assert.IsType<ScriptUInt64Array>(fromText.GetPropertyDatum(null, "unsigned").Object).GetElement(2));

        using var stream = new MemoryStream();
        tdoc.WriteStream(stream, value, options);
        stream.Position = 0;
        var fromStream = Assert.IsType<ScriptObject>(tdoc.ReadStream(stream, options).Object);
        Assert.Equal(9007199254740993L, Assert.IsType<ScriptInt64Array>(fromStream.GetPropertyDatum(null, "signed").Object).GetElement(1));
        Assert.Equal(9007199254740993UL, Assert.IsType<ScriptUInt64Array>(fromStream.GetPropertyDatum(null, "unsigned").Object).GetElement(1));
    }

    [Fact]
    public void EveryPackedArrayRoundTripsThroughNativeStorage()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        const string document = """
            Object {
                Int32Array int32 [-2147483648, 2147483647],
                Int8Array int8 [-128, 127],
                Float64Array float64 [1.25, -2.5e2],
                BooleanArray boolean [0, true],
                UInt8Array uint8 [0, 255],
                Int16Array int16 [-32768, 32767],
                UInt16Array uint16 [0, 65535],
                UInt32Array uint32 [0, 4294967295],
                Int64Array int64 [-9223372036854775808, 9007199254740993, 9223372036854775807],
                UInt64Array uint64 [0, 9007199254740993, 18446744073709551615],
            }
            """;

        var root = Assert.IsType<ScriptObject>(TypedDocumentSerializer.Deserialize(engine, document).Object);
        var text = TypedDocumentSerializer.Serialize(
            engine,
            ScriptDatum.FromObject(root),
            new TypedDocumentOptions { Indented = false });
        var restored = Assert.IsType<ScriptObject>(TypedDocumentSerializer.Deserialize(engine, text).Object);

        Assert.Equal(int.MinValue, Assert.IsType<ScriptInt32Array>(restored.GetPropertyDatum(null, "int32").Object).GetElement(0));
        Assert.Equal(sbyte.MaxValue, Assert.IsType<ScriptInt8Array>(restored.GetPropertyDatum(null, "int8").Object).GetElement(1));
        Assert.Equal(-250d, Assert.IsType<ScriptFloat64Array>(restored.GetPropertyDatum(null, "float64").Object).GetElement(1));
        Assert.True(Assert.IsType<ScriptBooleanArray>(restored.GetPropertyDatum(null, "boolean").Object).GetElement(1));
        Assert.Equal(byte.MaxValue, Assert.IsType<ScriptUInt8Array>(restored.GetPropertyDatum(null, "uint8").Object).GetElement(1));
        Assert.Equal(short.MinValue, Assert.IsType<ScriptInt16Array>(restored.GetPropertyDatum(null, "int16").Object).GetElement(0));
        Assert.Equal(ushort.MaxValue, Assert.IsType<ScriptUInt16Array>(restored.GetPropertyDatum(null, "uint16").Object).GetElement(1));
        Assert.Equal(uint.MaxValue, Assert.IsType<ScriptUInt32Array>(restored.GetPropertyDatum(null, "uint32").Object).GetElement(1));
        Assert.Equal(9007199254740993L, Assert.IsType<ScriptInt64Array>(restored.GetPropertyDatum(null, "int64").Object).GetElement(1));
        Assert.Equal(ulong.MaxValue, Assert.IsType<ScriptUInt64Array>(restored.GetPropertyDatum(null, "uint64").Object).GetElement(2));
    }

    [Fact]
    public void DateRoundTripUsesEngineDateTimeFormatForWritingAndReading()
    {
        var engine = new AuroraEngine(EngineOptions.Default.WithRuntime(runtime =>
            runtime.DateTimeFormat = "yyyy/MM/dd HH:mm:ss zzz"));
        var original = new ScriptDate(new DateTimeOffset(2026, 8, 19, 21, 8, 7, TimeSpan.FromHours(8)));

        var text = TypedDocumentSerializer.Serialize(engine, ScriptDatum.FromDate(original));
        Assert.Equal("Date \"2026/08/19 21:08:07 +08:00\"", text);

        var restored = Assert.IsType<ScriptDate>(TypedDocumentSerializer.Deserialize(engine, text).Object);
        Assert.Equal(original.DateTime, restored.DateTime);

        var mismatch = Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "Date \"2026-08-19 21:08:07\""));
        Assert.Equal("$", mismatch.DataPath);
        Assert.Contains("DateTimeFormat", mismatch.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NumericDateUsesTicksAndSerializesThroughDateTimeFormat()
    {
        const long ticks = 14425655658;
        var engine = new AuroraEngine(EngineOptions.Default.WithRuntime(runtime =>
            runtime.DateTimeFormat = "O"));

        var parsed = Assert.IsType<ScriptDate>(
            TypedDocumentSerializer.Deserialize(engine, $"Date {ticks}").Object);
        Assert.Equal(ticks, parsed.Ticks);
        Assert.Equal(TimeSpan.Zero, parsed.DateTime.Offset);

        var text = TypedDocumentSerializer.Serialize(engine, ScriptDatum.FromDate(parsed));
        Assert.StartsWith("Date \"", text, StringComparison.Ordinal);
        Assert.DoesNotContain(ticks.ToString(), text, StringComparison.Ordinal);

        var restored = Assert.IsType<ScriptDate>(TypedDocumentSerializer.Deserialize(engine, text).Object);
        Assert.Equal(ticks, restored.Ticks);
        Assert.Equal(TimeSpan.Zero, restored.DateTime.Offset);

        const long precisionSensitiveTicks = 638679147930000001;
        var precise = Assert.IsType<ScriptDate>(
            TypedDocumentSerializer.Deserialize(engine, $"Date {precisionSensitiveTicks}").Object);
        Assert.Equal(precisionSensitiveTicks, precise.Ticks);

        var maximum = Assert.IsType<ScriptDate>(
            TypedDocumentSerializer.Deserialize(engine, $"Date {DateTimeOffset.MaxValue.Ticks}").Object);
        Assert.Equal(DateTimeOffset.MaxValue.Ticks, maximum.Ticks);
    }

    [Theory]
    [InlineData("Date 1.5", "$")]
    [InlineData("Date -1", "$")]
    [InlineData("Date 3155378976000000000", "$")]
    [InlineData("Date true", "$")]
    [InlineData("Object { Date createdAt 1.5 }", "$.createdAt")]
    public void RejectsInvalidNumericDates(string document, string expectedPath)
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var error = Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, document));
        Assert.Equal(expectedPath, error.DataPath);
    }

    [Fact]
    public void DeserializesRegexAndHashMapWithoutLosingTheirIdentity()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        const string document = """
            Object {
                Regex expression {
                    pattern "ab+",
                    flags "gi",
                },
                HashMap values [
                    ["name", "Aurora"],
                    [1, true],
                ],
            }
            """;

        var root = Assert.IsType<ScriptObject>(TypedDocumentSerializer.Deserialize(engine, document).Object);
        var regex = Assert.IsType<ScriptRegex>(root.GetPropertyDatum(null, "expression").Object);
        Assert.Equal("ab+", regex.Pattern);
        Assert.Equal("gi", regex.Flags);

        var map = Assert.IsType<ScriptHashMap>(root.GetPropertyDatum(null, "values").Object);
        Assert.Equal("Aurora", map.Get(ScriptDatum.FromString("name")).StringText);
        Assert.True(map.Get(ScriptDatum.FromNumber(1)).Boolean);

        var roundTrip = TypedDocumentSerializer.Serialize(engine, ScriptDatum.FromObject(root));
        var restored = Assert.IsType<ScriptObject>(TypedDocumentSerializer.Deserialize(engine, roundTrip).Object);
        Assert.IsType<ScriptRegex>(restored.GetPropertyDatum(null, "expression").Object);
        Assert.IsType<ScriptHashMap>(restored.GetPropertyDatum(null, "values").Object);

        const string repeatedRegex = "Array [Regex { pattern \"x\", flags \"\" }, Regex { pattern \"x\", flags \"\" }]";
        var repeated = TypedDocumentSerializer.Deserialize(engine, repeatedRegex);
        var repeatedRoundTrip = TypedDocumentSerializer.Serialize(engine, repeated);
        Assert.IsType<ScriptArray>(TypedDocumentSerializer.Deserialize(engine, repeatedRoundTrip).Object);
    }

    [Fact]
    public void ReportsStructuredPathsForRangeAndSyntaxErrors()
    {
        using var workspace = new TestWorkspace();
        var engine = new AuroraEngine(EngineOptions.Default);
        var path = Path.Combine(workspace.Root, "settings.tdoc");
        File.WriteAllText(path, "Object { Int8Array bytes [0, 255] }");
        var tdoc = new AuroraTypedDocument(engine);

        var range = Assert.Throws<TypedDocumentException>(() => tdoc.ReadFile(path));
        Assert.Equal(path, range.SourceName);
        Assert.Equal("$.bytes[1]", range.DataPath);
        Assert.True(range.Line > 0);
        Assert.True(range.Column > 0);

        var duplicate = Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "Object { name \"a\", name \"b\" }"));
        Assert.Equal("$.name", duplicate.DataPath);

        Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "Object { name \"a\" age 1 }"));
        Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "tdoc Object { name \"a\" }"));
    }

    [Fact]
    public void SkipsCircularAndSharedReferences()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var circular = new ScriptObject();
        circular.Define("self", circular);
        Assert.Equal("{}", TypedDocumentSerializer.Serialize(engine, ScriptDatum.FromObject(circular)));

        var child = new ScriptObject();
        child.Define("value", ScriptDatum.FromNumber(1));
        var shared = new ScriptObject();
        shared.Define("left", child);
        shared.Define("right", child);
        var text = TypedDocumentSerializer.Serialize(engine, ScriptDatum.FromObject(shared));
        Assert.Contains("left", text, StringComparison.Ordinal);
        Assert.DoesNotContain("right", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ClrObjectsRequireCurrentHostRegistration()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        engine.RegisterType<HostProfile>("User");
        var profile = new HostProfile { Name = "Hanks", Age = 18 };
        var datum = ClrMarshaller.ToDatum(profile);

        var text = TypedDocumentSerializer.Serialize(engine, datum);
        Assert.StartsWith("User {", text, StringComparison.Ordinal);
        Assert.Contains("Age 18", text, StringComparison.Ordinal);
        Assert.Contains("Name \"Hanks\"", text, StringComparison.Ordinal);

        var restoredWrapper = Assert.IsType<ClrInstanceObject>(
            TypedDocumentSerializer.Deserialize(engine, text).Object);
        var restored = Assert.IsType<HostProfile>(restoredWrapper.Instance);
        Assert.Equal("Hanks", restored.Name);
        Assert.Equal(18, restored.Age);

        var otherEngine = new AuroraEngine(EngineOptions.Default);
        Assert.Equal("null", TypedDocumentSerializer.Serialize(otherEngine, datum));
        Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(otherEngine, "User { String Name \"Hanks\", Number Age 18 }"));

        Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "User { Number Age 18.5, String Name \"Hanks\" }"));
        Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "User { Number Age 2147483648, String Name \"Hanks\" }"));

        var allTypes = TypedDocumentSerializer.Serialize(
            engine,
            datum,
            new TypedDocumentOptions { Indented = false, EmitTypeNames = true });
        Assert.StartsWith("User {", allTypes, StringComparison.Ordinal);
        Assert.Contains("String Name", allTypes, StringComparison.Ordinal);
        Assert.Contains("Number Age", allTypes, StringComparison.Ordinal);
        var restoredWithAllTypes = Assert.IsType<ClrInstanceObject>(
            TypedDocumentSerializer.Deserialize(engine, allTypes).Object);
        Assert.IsType<HostProfile>(restoredWithAllTypes.Instance);
    }

    [Fact]
    public void NativeTypedDocumentsRoundTripWithoutClrWrappers()
    {
        var engine = new AuroraEngine(EngineOptions.Default.WithCompiler(compiler =>
            compiler.WithNativeTypes(typeof(Vec2))));
        var vector = new Vec2(3, 4);
        var text = TypedDocumentSerializer.Serialize(
            engine,
            ScriptDatum.FromObject(vector),
            new TypedDocumentOptions { Indented = false });
        Assert.Equal("Vec2 [3,4]", text);

        var restored = Assert.IsType<Vec2>(
            TypedDocumentSerializer.Deserialize(engine, text).Object);
        Assert.Equal(3d, restored.X);
        Assert.Equal(4d, restored.Y);
        Assert.Equal(5d, restored.LengthCore());

        var fromObject = Assert.IsType<Vec2>(
            TypedDocumentSerializer.Deserialize(engine, "Vec2 {x 3,y 4}").Object);
        Assert.Equal(3d, fromObject.X);
        Assert.Equal(4d, fromObject.Y);

        var nested = Assert.IsType<ScriptObject>(
            TypedDocumentSerializer.Deserialize(engine, "{Vec2 vec [1000,2000]}").Object);
        var nestedVector = Assert.IsType<Vec2>(nested.GetPropertyDatum(null, "vec").Object);
        Assert.Equal(1000d, nestedVector.X);
        Assert.Equal(2000d, nestedVector.Y);

        var otherEngine = new AuroraEngine(EngineOptions.Default);
        Assert.Equal("null", TypedDocumentSerializer.Serialize(otherEngine, ScriptDatum.FromObject(vector)));
        Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(otherEngine, "Vec2 { x 1, y 2 }"));
        Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "Vec2 { x 1, z 2 }"));
        Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "Vec2 [1, 2, 3]"));
    }

    [Fact]
    public void PrimitiveHotPathsDoNotBuildTokenOrNodeGraphs()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        for (var index = 0; index < 32; index++)
        {
            _ = TypedDocumentSerializer.Deserialize(engine, "Number 42.5");
            _ = TypedDocumentSerializer.Serialize(engine, ScriptDatum.FromNumber(42.5));
        }

        const int iterations = 1000;
        var sum = 0d;
        var beforeRead = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < iterations; index++)
        {
            var value = TypedDocumentSerializer.Deserialize(engine, "Number 42.5");
            sum += value.Number;
        }
        var readBytes = GC.GetAllocatedBytesForCurrentThread() - beforeRead;

        var beforeWrite = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < iterations; index++)
        {
            _ = TypedDocumentSerializer.Serialize(engine, ScriptDatum.FromNumber(42.5));
        }
        var writeBytes = GC.GetAllocatedBytesForCurrentThread() - beforeWrite;

        Assert.Equal(42.5 * iterations, sum);
        Assert.InRange(readBytes, 0, 64 * 1024);
        Assert.InRange(writeBytes, 0, 256 * 1024);
    }

    [Fact]
    public void HandlesEscapesPackedArraysAndCompactOutput()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        const string document = """
            Object {
                String text "line\nquote: \" slash: \\ unicode: \u4E2D hex: \x41",
                String multiline "first
            second",
                Number grouped 1_000.5,
                Int32Array ints [-2147483648, 0, 2147483647],
                Int8Array bytes [-128, 127],
                Float64Array doubles [1.25, -2.5e2],
                BooleanArray bits [true, false],
            }
            """;

        var root = Assert.IsType<ScriptObject>(TypedDocumentSerializer.Deserialize(engine, document).Object);
        Assert.Equal("line\nquote: \" slash: \\ unicode: 中 hex: A", root.GetPropertyDatum(null, "text").StringText);
        Assert.Equal("first\nsecond", root.GetPropertyDatum(null, "multiline").StringText);
        Assert.Equal(1000.5, root.GetPropertyDatum(null, "grouped").Number);
        Assert.Equal(int.MinValue, Assert.IsType<ScriptInt32Array>(root.GetPropertyDatum(null, "ints").Object).GetElement(0));
        Assert.Equal(sbyte.MaxValue, Assert.IsType<ScriptInt8Array>(root.GetPropertyDatum(null, "bytes").Object).GetElement(1));
        Assert.Equal(-250d, Assert.IsType<ScriptFloat64Array>(root.GetPropertyDatum(null, "doubles").Object).GetElement(1));
        Assert.False(Assert.IsType<ScriptBooleanArray>(root.GetPropertyDatum(null, "bits").Object).GetElement(1));

        var compact = TypedDocumentSerializer.Serialize(
            engine,
            ScriptDatum.FromObject(root),
            new TypedDocumentOptions { Indented = false });
        Assert.DoesNotContain('\n', compact);
        var restored = Assert.IsType<ScriptObject>(TypedDocumentSerializer.Deserialize(engine, compact).Object);
        Assert.IsType<ScriptBooleanArray>(restored.GetPropertyDatum(null, "bits").Object);

        Assert.Throws<TypedDocumentException>(() => TypedDocumentSerializer.Deserialize(engine, "Number 1__0"));
        Assert.Throws<TypedDocumentException>(() => TypedDocumentSerializer.Deserialize(engine, "Number 1_"));
    }

    [Fact]
    public void DeserializesCompactSingleDigitInt8RunsAndRetainsPackedValidation()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var bytes = Assert.IsType<ScriptInt8Array>(
            TypedDocumentSerializer.Deserialize(
                engine,
                "Int8Array [0,1,2,3,4,5,6,7,8,9, 10, -1, Number 2,]").Object);

        Assert.Equal(13, bytes.Length);
        Assert.Equal((sbyte)0, bytes.GetElement(0));
        Assert.Equal((sbyte)9, bytes.GetElement(9));
        Assert.Equal((sbyte)10, bytes.GetElement(10));
        Assert.Equal((sbyte)-1, bytes.GetElement(11));
        Assert.Equal((sbyte)2, bytes.GetElement(12));

        var root = Assert.IsType<ScriptObject>(
            TypedDocumentSerializer.Deserialize(engine, "Object { Int8Array bytes [0,1,2] }").Object);
        var nested = Assert.IsType<ScriptInt8Array>(root.GetPropertyDatum(null, "bytes").Object);
        Assert.Equal((sbyte)2, nested.GetElement(2));

        var trailing = Assert.IsType<ScriptInt8Array>(
            TypedDocumentSerializer.Deserialize(engine, "Int8Array [0,1,2,]").Object);
        Assert.Equal(3, trailing.Length);

        Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "Int8Array [0,1,128]"));

        var wideError = Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "Int64Array [Number 1.5]"));
        Assert.Equal("$[0]", wideError.DataPath);
    }

    [Fact]
    public void DeserializesCompactSingleDigitUInt8RunsWithoutChangingPackedValidation()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var bytes = Assert.IsType<ScriptUInt8Array>(
            TypedDocumentSerializer.Deserialize(
                engine,
                "UInt8Array [0,1,2,3,4,5,6,7,8,9, 10, 255, Number 2,]").Object);

        Assert.Equal(13, bytes.Length);
        Assert.Equal((byte)0, bytes.GetElement(0));
        Assert.Equal((byte)9, bytes.GetElement(9));
        Assert.Equal((byte)10, bytes.GetElement(10));
        Assert.Equal(byte.MaxValue, bytes.GetElement(11));
        Assert.Equal((byte)2, bytes.GetElement(12));

        var compact = Assert.IsType<ScriptUInt8Array>(
            TypedDocumentSerializer.Deserialize(engine, "UInt8Array [0,1,2,]").Object);
        Assert.Equal(3, compact.Length);

        Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "UInt8Array [0,1,256]"));
    }

    [Fact]
    public void DeserializesUnsignedAndFloatPackedRunsThroughNativeStorage()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        const string document = """
            Object {
                UInt8Array u8 [0, 255, 1],
                UInt16Array u16 [0, 65535, 42],
                UInt32Array u32 [0, 4294967295, 42],
                UInt64Array u64 [0, 9007199254740993, 18446744073709551615],
                Float64Array floats [
                    1.25,
                    -2.5e2,
                    3.0,
                ],
                String tail "ok",
            }
            """;

        var root = Assert.IsType<ScriptObject>(
            TypedDocumentSerializer.Deserialize(engine, document).Object);
        Assert.Equal((byte)255, Assert.IsType<ScriptUInt8Array>(root.GetPropertyDatum(null, "u8").Object).GetElement(1));
        Assert.Equal(ushort.MaxValue, Assert.IsType<ScriptUInt16Array>(root.GetPropertyDatum(null, "u16").Object).GetElement(1));
        Assert.Equal(uint.MaxValue, Assert.IsType<ScriptUInt32Array>(root.GetPropertyDatum(null, "u32").Object).GetElement(1));
        Assert.Equal(ulong.MaxValue, Assert.IsType<ScriptUInt64Array>(root.GetPropertyDatum(null, "u64").Object).GetElement(2));
        var floats = Assert.IsType<ScriptFloat64Array>(root.GetPropertyDatum(null, "floats").Object);
        Assert.Equal(1.25, floats.GetElement(0));
        Assert.Equal(-250d, floats.GetElement(1));
        Assert.Equal(3d, floats.GetElement(2));
        Assert.Equal("ok", root.GetPropertyDatum(null, "tail").StringText);
    }

    [Fact]
    public void CompactOutputHasNoFormattingLineBreaksButPreservesStringControlCharacters()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var root = new ScriptObject();
        root.Define("text", ScriptDatum.FromString("first\r\nsecond\fthird"));

        var compact = TypedDocumentSerializer.Serialize(
            engine,
            ScriptDatum.FromObject(root),
            new TypedDocumentOptions { Indented = false });

        Assert.DoesNotContain('\r', compact);
        Assert.DoesNotContain('\n', compact);
        Assert.Contains("\\r\\n", compact, StringComparison.Ordinal);
        Assert.Contains("\\f", compact, StringComparison.Ordinal);

        var restored = Assert.IsType<ScriptObject>(TypedDocumentSerializer.Deserialize(engine, compact).Object);
        Assert.Equal("first\r\nsecond\fthird", restored.GetPropertyDatum(null, "text").StringText);
    }

    [Theory]
    [InlineData("Object { id UX01 }")]
    [InlineData("Object { id UX01-03 }")]
    public void RequiresQuotesForStringValues(string document)
    {
        var engine = new AuroraEngine(EngineOptions.Default);

        Assert.Throws<TypedDocumentException>(() => TypedDocumentSerializer.Deserialize(engine, document));
    }

    [Fact]
    public void EnforcesConfiguredDepthAndClrConstructionBoundary()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var depthOptions = new TypedDocumentOptions { MaxDepth = 2 };
        var readError = Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "Array [Array [1]]", depthOptions));
        Assert.Equal("$[0][0]", readError.DataPath);

        var packedReadError = Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "Int8Array [1]", new TypedDocumentOptions { MaxDepth = 1 }));
        Assert.Equal("$[0]", packedReadError.DataPath);

        var inner = new ScriptArray();
        inner.Push(ScriptDatum.FromNumber(1));
        var outer = new ScriptArray();
        outer.Push(ScriptDatum.FromArray(inner));
        var writeError = Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Serialize(engine, ScriptDatum.FromArray(outer), depthOptions));
        Assert.Equal("$[0][0]", writeError.DataPath);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TypedDocumentSerializer.Deserialize(engine, "null", new TypedDocumentOptions { MaxDepth = 0 }));

        var staticOnly = new AuroraEngine(EngineOptions.Default);
        staticOnly.RegisterType<HostProfile>("User", TypeAccess.Static);
        var profile = ClrMarshaller.ToDatum(new HostProfile());
        Assert.Equal("null", TypedDocumentSerializer.Serialize(staticOnly, profile));
        Assert.Throws<TypedDocumentException>(() =>
            TypedDocumentSerializer.Deserialize(staticOnly, "User {}"));
    }

    public static IEnumerable<object[]> PackedArrayBoundaryCases()
    {
        yield return new object[] { "Int32Array", "-2147483648, 2147483647", new[] { "-2147483649", "2147483648", "1.5", "NaN", "Infinity" } };
        yield return new object[] { "Int8Array", "-128, 127", new[] { "-129", "128", "1.5", "NaN", "Infinity" } };
        yield return new object[] { "Float64Array", "-2.5e2, 1.5", new[] { "NaN", "Infinity" } };
        yield return new object[] { "BooleanArray", "0, 1, true, false", new[] { "-1", "2", "0.5", "NaN", "Infinity" } };
        yield return new object[] { "UInt8Array", "0, 255", new[] { "-1", "256", "1.5", "NaN", "Infinity" } };
        yield return new object[] { "Int16Array", "-32768, 32767", new[] { "-32769", "32768", "1.5", "NaN", "Infinity" } };
        yield return new object[] { "UInt16Array", "0, 65535", new[] { "-1", "65536", "1.5", "NaN", "Infinity" } };
        yield return new object[] { "UInt32Array", "0, 4294967295", new[] { "-1", "4294967296", "1.5", "NaN", "Infinity" } };
        yield return new object[] { "Int64Array", "-9223372036854775808, 9007199254740993, 9223372036854775807", new[] { "-9223372036854775809", "9223372036854775808", "1.5", "NaN", "Infinity" } };
        yield return new object[] { "UInt64Array", "0, 9007199254740993, 18446744073709551615", new[] { "-1", "18446744073709551616", "1.5", "NaN", "Infinity" } };
    }

    [Theory]
    [MemberData(nameof(PackedArrayBoundaryCases))]
    public void EveryPackedArrayReaderValidatesBoundsAndElementShape(
        string typeName,
        string validValues,
        string[] invalidValues)
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        Assert.IsAssignableFrom<ScriptPackedArray>(
            TypedDocumentSerializer.Deserialize(engine, $"{typeName} [{validValues}]").Object);

        foreach (var invalid in invalidValues)
        {
            Assert.Throws<TypedDocumentException>(() =>
                TypedDocumentSerializer.Deserialize(engine, $"{typeName} [{invalid}]"));
        }
    }

    private static ScriptInt8Array CreateBytes()
    {
        var result = new ScriptInt8Array(3);
        result.SetElement(0, -128);
        result.SetElement(1, 0);
        result.SetElement(2, 127);
        return result;
    }

    public sealed class HostProfile
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
