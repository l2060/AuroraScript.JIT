using AuroraScript;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;
using System;
using System.Text;

namespace AuroraBenchmark
{
    /// <summary>
    /// Keeps the small scalar, ordinary document, packed-array, and wide-object TDoc
    /// paths visible in the standard performance suite.
    /// </summary>
    [MemoryDiagnoser]
    [ShortRunJob]
    [MarkdownExporter, JsonExporter, CsvExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn(NumeralSystem.Arabic)]
    [CategoriesColumn]
    public class TypedDocumentBenchmarks
    {
        private const string PrimitiveDocument = "Number 42.5";

        private AuroraEngine _engine;
        private string _objectDocument;
        private string _arrayDocument;
        private string _packedArrayDocument;
        private string _wideObjectDocument;
        private string _largePackedInt8Document;
        private string _largePackedUInt8Document;
        private AuroraTypedDocument _typedDocument;
        private ScriptDatum _objectValue;
        private ScriptDatum _packedArrayValue;

        [GlobalSetup]
        public void Setup()
        {
            _engine = new AuroraEngine(EngineOptions.Default);
            _typedDocument = new AuroraTypedDocument(_engine);
            _objectDocument = "Object { String name \"Aurora\", Number version 4, Boolean enabled true, Array tags [String \"jit\", String \"typed-document\", Number 4] }";
            _arrayDocument = CreateArrayDocument("Array", 1024, item => item.ToString());
            _packedArrayDocument = CreateArrayDocument("Int32Array", 1024, item => item.ToString());
            _wideObjectDocument = CreateWideObjectDocument(256);
            _largePackedInt8Document = CreateArrayDocument("Int8Array", 1_000_000, item => (item & 1).ToString());
            _largePackedUInt8Document = CreateArrayDocument("UInt8Array", 1_000_000, item => (item & 1).ToString());
            _objectValue = TypedDocumentSerializer.Deserialize(_engine, _objectDocument);
            _packedArrayValue = TypedDocumentSerializer.Deserialize(_engine, _packedArrayDocument);
        }

        [BenchmarkCategory("typed-document")]
        [Benchmark]
        public ScriptDatum DeserializePrimitive()
        {
            return TypedDocumentSerializer.Deserialize(_engine, PrimitiveDocument);
        }

        [BenchmarkCategory("typed-document")]
        [Benchmark]
        public string SerializePrimitive()
        {
            return TypedDocumentSerializer.Serialize(_engine, ScriptDatum.FromNumber(42.5));
        }

        [BenchmarkCategory("typed-document")]
        [Benchmark]
        public ScriptDatum DeserializeNestedObject()
        {
            return TypedDocumentSerializer.Deserialize(_engine, _objectDocument);
        }

        [BenchmarkCategory("typed-document")]
        [Benchmark]
        public string SerializeNestedObject()
        {
            return TypedDocumentSerializer.Serialize(_engine, _objectValue);
        }

        [BenchmarkCategory("typed-document")]
        [Benchmark]
        public ScriptDatum DeserializeGenericArray1024()
        {
            return TypedDocumentSerializer.Deserialize(_engine, _arrayDocument);
        }

        [BenchmarkCategory("typed-document")]
        [Benchmark]
        public ScriptDatum DeserializePackedInt32Array1024()
        {
            return TypedDocumentSerializer.Deserialize(_engine, _packedArrayDocument);
        }

        [BenchmarkCategory("typed-document")]
        [Benchmark]
        public string SerializePackedInt32Array1024()
        {
            return TypedDocumentSerializer.Serialize(_engine, _packedArrayValue);
        }

        [BenchmarkCategory("typed-document")]
        [Benchmark]
        public ScriptDatum DeserializeWideObject256()
        {
            return TypedDocumentSerializer.Deserialize(_engine, _wideObjectDocument);
        }

        [BenchmarkCategory("typed-document")]
        [Benchmark]
        public ScriptDatum DeserializePackedInt8Array1M()
        {
            return _typedDocument.Deserialize(_largePackedInt8Document);
        }

        [BenchmarkCategory("typed-document")]
        [Benchmark]
        public ScriptDatum DeserializePackedUInt8Array1M()
        {
            return _typedDocument.Deserialize(_largePackedUInt8Document);
        }

        private static string CreateArrayDocument(string typeName, int count, Func<int, string> format)
        {
            var builder = new StringBuilder(typeName.Length + (count * 4) + 4);
            builder.Append(typeName).Append(" [");
            for (var index = 0; index < count; index++)
            {
                if (index != 0) builder.Append(',');
                builder.Append(format(index));
            }
            return builder.Append(']').ToString();
        }

        private static string CreateWideObjectDocument(int count)
        {
            var builder = new StringBuilder(count * 20);
            builder.Append("Object {");
            for (var index = 0; index < count; index++)
            {
                builder.Append("Number p").Append(index).Append(' ').Append(index).Append(',');
            }
            return builder.Append('}').ToString();
        }
    }
}
