using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using System;
using Xunit;

namespace AuroraScript.Tests;

public sealed class TypedDataSerializationTests
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

        var datum = TypedDataSerializer.Deserialize(engine, document);
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

        var text = TypedDataSerializer.Serialize(engine, ScriptDatum.FromObject(root));

        Assert.DoesNotContain("@data", text, StringComparison.Ordinal);
        Assert.Contains("readonly String id \"u-001\"", text, StringComparison.Ordinal);
        Assert.Contains("Int8Array bytes", text, StringComparison.Ordinal);
        Assert.Contains("Date createdAt \"2024-11-23 15:26:33\"", text, StringComparison.Ordinal);

        var restored = Assert.IsType<ScriptObject>(TypedDataSerializer.Deserialize(engine, text).Object);
        Assert.IsType<ScriptInt8Array>(restored.GetPropertyDatum(null, "bytes").Object);
        Assert.IsType<ScriptDate>(restored.GetPropertyDatum(null, "createdAt").Object);
        Assert.IsType<ScriptPathValue>(restored.GetPropertyDatum(null, "path").Object);
        Assert.ThrowsAny<AuroraException>(() => restored.Define("id", ScriptDatum.FromString("changed")));
    }

    [Fact]
    public void DateRoundTripUsesEngineDateTimeFormatForWritingAndReading()
    {
        var engine = new AuroraEngine(EngineOptions.Default.WithRuntime(runtime =>
            runtime.DateTimeFormat = "yyyy/MM/dd HH:mm:ss zzz"));
        var original = new ScriptDate(new DateTimeOffset(2026, 8, 19, 21, 8, 7, TimeSpan.FromHours(8)));

        var text = TypedDataSerializer.Serialize(engine, ScriptDatum.FromDate(original));
        Assert.Equal("Date \"2026/08/19 21:08:07 +08:00\"", text);

        var restored = Assert.IsType<ScriptDate>(TypedDataSerializer.Deserialize(engine, text).Object);
        Assert.Equal(original.DateTime, restored.DateTime);

        var mismatch = Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Deserialize(engine, "Date \"2026-08-19 21:08:07\""));
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
            TypedDataSerializer.Deserialize(engine, $"Date {ticks}").Object);
        Assert.Equal(ticks, parsed.Ticks);
        Assert.Equal(TimeSpan.Zero, parsed.DateTime.Offset);

        var text = TypedDataSerializer.Serialize(engine, ScriptDatum.FromDate(parsed));
        Assert.StartsWith("Date \"", text, StringComparison.Ordinal);
        Assert.DoesNotContain(ticks.ToString(), text, StringComparison.Ordinal);

        var restored = Assert.IsType<ScriptDate>(TypedDataSerializer.Deserialize(engine, text).Object);
        Assert.Equal(ticks, restored.Ticks);
        Assert.Equal(TimeSpan.Zero, restored.DateTime.Offset);

        const long precisionSensitiveTicks = 638679147930000001;
        var precise = Assert.IsType<ScriptDate>(
            TypedDataSerializer.Deserialize(engine, $"Date {precisionSensitiveTicks}").Object);
        Assert.Equal(precisionSensitiveTicks, precise.Ticks);

        var maximum = Assert.IsType<ScriptDate>(
            TypedDataSerializer.Deserialize(engine, $"Date {DateTimeOffset.MaxValue.Ticks}").Object);
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
        var error = Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Deserialize(engine, document));
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

        var root = Assert.IsType<ScriptObject>(TypedDataSerializer.Deserialize(engine, document).Object);
        var regex = Assert.IsType<ScriptRegex>(root.GetPropertyDatum(null, "expression").Object);
        Assert.Equal("ab+", regex.Pattern);
        Assert.Equal("gi", regex.Flags);

        var map = Assert.IsType<ScriptHashMap>(root.GetPropertyDatum(null, "values").Object);
        Assert.Equal("Aurora", map.Get(ScriptDatum.FromString("name")).StringText);
        Assert.True(map.Get(ScriptDatum.FromNumber(1)).Boolean);

        var roundTrip = TypedDataSerializer.Serialize(engine, ScriptDatum.FromObject(root));
        var restored = Assert.IsType<ScriptObject>(TypedDataSerializer.Deserialize(engine, roundTrip).Object);
        Assert.IsType<ScriptRegex>(restored.GetPropertyDatum(null, "expression").Object);
        Assert.IsType<ScriptHashMap>(restored.GetPropertyDatum(null, "values").Object);

        const string repeatedRegex = "Array [Regex { pattern \"x\", flags \"\" }, Regex { pattern \"x\", flags \"\" }]";
        var repeated = TypedDataSerializer.Deserialize(engine, repeatedRegex);
        var repeatedRoundTrip = TypedDataSerializer.Serialize(engine, repeated);
        Assert.IsType<ScriptArray>(TypedDataSerializer.Deserialize(engine, repeatedRoundTrip).Object);
    }

    [Fact]
    public void ReportsStructuredPathsForRangeAndSyntaxErrors()
    {
        var engine = new AuroraEngine(EngineOptions.Default);

        var range = Assert.Throws<TypedDataException>(() => TypedDataSerializer.Deserialize(
            engine,
            "Object { Int8Array bytes [0, 255] }",
            new TypedDataOptions { SourceName = "settings.atd" }));
        Assert.Equal("settings.atd", range.SourceName);
        Assert.Equal("$.bytes[1]", range.DataPath);
        Assert.True(range.Line > 0);
        Assert.True(range.Column > 0);

        var duplicate = Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Deserialize(engine, "Object { name \"a\", name \"b\" }"));
        Assert.Equal("$.name", duplicate.DataPath);

        Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Deserialize(engine, "Object { name \"a\" age 1 }"));
        Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Deserialize(engine, "@data Object { name \"a\" }"));
    }

    [Fact]
    public void RejectsCircularAndSharedReferences()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var circular = new ScriptObject();
        circular.Define("self", circular);
        Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Serialize(engine, ScriptDatum.FromObject(circular)));

        var child = new ScriptObject();
        child.Define("value", ScriptDatum.FromNumber(1));
        var shared = new ScriptObject();
        shared.Define("left", child);
        shared.Define("right", child);
        var error = Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Serialize(engine, ScriptDatum.FromObject(shared)));
        Assert.Equal("$.right", error.DataPath);
    }

    [Fact]
    public void ClrObjectsRequireCurrentHostRegistration()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        engine.RegisterType<HostProfile>("User");
        var profile = new HostProfile { Name = "Hanks", Age = 18 };
        var datum = ClrMarshaller.ToDatum(profile);

        var text = TypedDataSerializer.Serialize(engine, datum);
        Assert.StartsWith("User {", text, StringComparison.Ordinal);
        Assert.Contains("Number Age 18", text, StringComparison.Ordinal);
        Assert.Contains("String Name \"Hanks\"", text, StringComparison.Ordinal);

        var restoredWrapper = Assert.IsType<ClrInstanceObject>(
            TypedDataSerializer.Deserialize(engine, text).Object);
        var restored = Assert.IsType<HostProfile>(restoredWrapper.Instance);
        Assert.Equal("Hanks", restored.Name);
        Assert.Equal(18, restored.Age);

        var otherEngine = new AuroraEngine(EngineOptions.Default);
        Assert.Throws<TypedDataException>(() => TypedDataSerializer.Serialize(otherEngine, datum));
        Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Deserialize(otherEngine, "User { String Name \"Hanks\", Number Age 18 }"));

        Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Deserialize(engine, "User { Number Age 18.5, String Name \"Hanks\" }"));
        Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Deserialize(engine, "User { Number Age 2147483648, String Name \"Hanks\" }"));
    }

    [Fact]
    public void PrimitiveHotPathsDoNotBuildTokenOrNodeGraphs()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        for (var index = 0; index < 32; index++)
        {
            _ = TypedDataSerializer.Deserialize(engine, "Number 42.5");
            _ = TypedDataSerializer.Serialize(engine, ScriptDatum.FromNumber(42.5));
        }

        const int iterations = 1000;
        var sum = 0d;
        var beforeRead = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < iterations; index++)
        {
            var value = TypedDataSerializer.Deserialize(engine, "Number 42.5");
            sum += value.Number;
        }
        var readBytes = GC.GetAllocatedBytesForCurrentThread() - beforeRead;

        var beforeWrite = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < iterations; index++)
        {
            _ = TypedDataSerializer.Serialize(engine, ScriptDatum.FromNumber(42.5));
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

        var root = Assert.IsType<ScriptObject>(TypedDataSerializer.Deserialize(engine, document).Object);
        Assert.Equal("line\nquote: \" slash: \\ unicode: 中 hex: A", root.GetPropertyDatum(null, "text").StringText);
        Assert.Equal("first\nsecond", root.GetPropertyDatum(null, "multiline").StringText);
        Assert.Equal(1000.5, root.GetPropertyDatum(null, "grouped").Number);
        Assert.Equal(int.MinValue, Assert.IsType<ScriptInt32Array>(root.GetPropertyDatum(null, "ints").Object).GetElement(0));
        Assert.Equal(sbyte.MaxValue, Assert.IsType<ScriptInt8Array>(root.GetPropertyDatum(null, "bytes").Object).GetElement(1));
        Assert.Equal(-250d, Assert.IsType<ScriptFloat64Array>(root.GetPropertyDatum(null, "doubles").Object).GetElement(1));
        Assert.False(Assert.IsType<ScriptBooleanArray>(root.GetPropertyDatum(null, "bits").Object).GetElement(1));

        var compact = TypedDataSerializer.Serialize(
            engine,
            ScriptDatum.FromObject(root),
            new TypedDataOptions { Indented = false });
        Assert.DoesNotContain('\n', compact);
        var restored = Assert.IsType<ScriptObject>(TypedDataSerializer.Deserialize(engine, compact).Object);
        Assert.IsType<ScriptBooleanArray>(restored.GetPropertyDatum(null, "bits").Object);

        Assert.Throws<TypedDataException>(() => TypedDataSerializer.Deserialize(engine, "Number 1__0"));
        Assert.Throws<TypedDataException>(() => TypedDataSerializer.Deserialize(engine, "Number 1_"));
    }

    [Fact]
    public void EnforcesConfiguredDepthAndClrConstructionBoundary()
    {
        var engine = new AuroraEngine(EngineOptions.Default);
        var depthOptions = new TypedDataOptions { MaxDepth = 2 };
        var readError = Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Deserialize(engine, "Array [Array [1]]", depthOptions));
        Assert.Equal("$[0][0]", readError.DataPath);

        var inner = new ScriptArray();
        inner.Push(ScriptDatum.FromNumber(1));
        var outer = new ScriptArray();
        outer.Push(ScriptDatum.FromArray(inner));
        var writeError = Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Serialize(engine, ScriptDatum.FromArray(outer), depthOptions));
        Assert.Equal("$[0][0]", writeError.DataPath);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TypedDataSerializer.Deserialize(engine, "null", new TypedDataOptions { MaxDepth = 0 }));

        var staticOnly = new AuroraEngine(EngineOptions.Default);
        staticOnly.RegisterType<HostProfile>("User", TypeAccess.Static);
        var profile = ClrMarshaller.ToDatum(new HostProfile());
        Assert.Throws<TypedDataException>(() => TypedDataSerializer.Serialize(staticOnly, profile));
        Assert.Throws<TypedDataException>(() =>
            TypedDataSerializer.Deserialize(staticOnly, "User {}"));
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
