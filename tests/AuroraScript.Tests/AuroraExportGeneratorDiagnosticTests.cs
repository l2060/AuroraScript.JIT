using AuroraScript.Hosting.Generators;
using AuroraScript.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace AuroraScript.Tests;

public sealed class AuroraExportGeneratorDiagnosticTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UnmarkedStaticExportsAlwaysBelongToType(bool primitive)
    {
        var source = ValueReceiverSource("""
            [AuroraExport("echo")]
            public static string EchoCore(string value) => value;
            """, annotateReceivers: false);
        if (!primitive) source = source.Replace(", NativeReceiverType = typeof(string)", "", StringComparison.Ordinal);
        var updated = RunCore(source, out var diagnostics);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(updated.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, updated.SyntaxTrees.Skip(1));
        Assert.Contains("Define(\"echo\", ScriptDatum.FromBonding(__Static_ECHO)", generated);
        Assert.DoesNotContain("prototype.Define(\"echo\"", generated);
        Assert.Contains("EchoCore(arg0)", generated);
    }

    [Theory]
    [InlineData("[AuroraNativeType(\"String\", NativeReceiverType = null)]")]
    [InlineData("[AuroraNativeType(\"String\", NativeReceiverType = typeof(int))]")]
    [InlineData("[AuroraNativeType(\"String\")]")]
    public void InstanceTargetRequiresAValidNativeReceiverType(string typeAttribute)
    {
        var source = ValueReceiverSource("""
            [AuroraExport("echo")]
            public static string EchoCore(string value) => value;
            """).Replace(
                "[AuroraNativeType(\"String\", NativeReceiverType = typeof(string))]",
                typeAttribute,
                StringComparison.Ordinal);
        Assert.Contains(Run(source), d => d.Id is "AURORAEXP001" or "AURORAEXP002");
    }

    [Theory]
    [InlineData("[AuroraExport(\"echo\", Target = AuroraExportTarget.Instance)] public string EchoCore(string value) => value;")]
    [InlineData("[AuroraExport(\"echo\", Target = (AuroraExportTarget)99)] public static string EchoCore(string value) => value;")]
    public void InstanceTargetRejectsInvalidContracts(string member)
    {
        Assert.Contains(Run(ValueReceiverSource(member, annotateReceivers: false)), d => d.Id == "AURORAEXP002");
    }

    [Fact]
    public void PrimitiveFactoryReusesStaticExportCatalogAndGeneratedType()
    {
        var source = ValueReceiverSource("""
            [AuroraExport("valueOf")]
            public static string CreateCore(string value = "") => value;
            [AuroraExport("compare", DynamicAdapter = nameof(Call))]
            public static int CompareCore(string left, string right) => 1;
            [AuroraExport("toString", Target = AuroraExportTarget.Instance)]
            public static string TextCore(string value) => value;
            """, annotateReceivers: false).Replace("NativeReceiverType = typeof(string)", "NativeReceiverType = typeof(string), NativeConstructor = nameof(CreateCore)", StringComparison.Ordinal);
        var updated = RunCore(source, out var diagnostics);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(updated.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, updated.SyntaxTrees.Skip(1));
        Assert.Contains("FactoryMemberName = \"valueOf\"", generated);
        Assert.Contains("AuroraGeneratedExportAttribute", generated);
        Assert.Contains("public static readonly ScriptType Type", generated);
        Assert.Contains("void Register(ScriptObject target", generated);
        Assert.Contains("Define(\"compare\", ScriptDatum.FromBonding(Call)", generated);
        Assert.Contains("public static void __Static_VALUEOF", generated);
        Assert.Contains("__Static_VALUEOF(ctx, this, args, ref result);", generated);
        Assert.DoesNotContain("IAuroraNativeInstance", generated);
        Assert.DoesNotContain("prototype.Define(\"valueOf\"", generated);
    }

    [Theory]
    [InlineData("Missing", "[AuroraExport(\"valueOf\")] public static string CreateCore(string value) => value;")]
    [InlineData("CreateCore", "[AuroraExport(\"valueOf\", Target = AuroraExportTarget.Instance)] public static string CreateCore(string value) => value;")]
    [InlineData("CreateCore", "[AuroraExport(\"valueOf\")] public static int CreateCore(string value) => 1;")]
    [InlineData("CreateCore", "[AuroraExport(\"valueOf\", IsGetter = true)] public static string CreateCore() => \"\";")]
    [InlineData("CreateCore", "[AuroraExport(\"valueOf\", DynamicAdapter = \"Missing\")] public static string CreateCore(string value) => value;")]
    [InlineData("CreateCore", "[AuroraExport(\"valueOf\")] private static string CreateCore(string value) => value;")]
    [InlineData("CreateCore", "[AuroraExport(\"valueOf\")] public string CreateCore(string value) => value;")]
    [InlineData("CreateCore", "[AuroraExport(\"valueOf\")] public static string CreateCore(params ScriptDatum[] args) => \"\";")]
    public void PrimitiveFactoryRejectsInvalidContracts(string factory, string members)
    {
        var source = ValueReceiverSource(members, annotateReceivers: false).Replace("AuroraNativeType(\"String\", NativeReceiverType = typeof(string))",
            "AuroraNativeType(\"String\", NativeReceiverType = typeof(string), NativeConstructor = \"" + factory + "\")", StringComparison.Ordinal);
        Assert.Contains(Run(source), diagnostic => diagnostic.Id == "AURORAEXP002");
    }

    [Fact]
    public void ValueReceiverUsesExistingNativeCatalogAndRegistersEachAdapterOnce()
    {
        var updated = RunCore(ValueReceiverSource(
            """
            [AuroraExport("slice", DynamicAdapter = nameof(Call))]
            public static string SliceCore(string value, int index) => value.Substring(index);
            [AuroraExport("slice", DynamicAdapter = nameof(Call))]
            public static string SliceCore(string value, double index) => value.Substring((int)index);
            [AuroraExport("length", IsGetter = true, DynamicAdapter = nameof(Get))]
            public static int LengthCore(string value) => value.Length;
            [AuroraExport("code", RequiresIndexProof = true, DynamicAdapter = nameof(Call))]
            public static int CodeCore(string value, int index) => value[index];
            """), out var diagnostics);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(updated.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, updated.SyntaxTrees.Skip(1).Select(tree => tree.ToString()));
        Assert.Contains("AuroraGeneratedNativeObjectAttribute", generated);
        Assert.Contains("ReceiverType = typeof(string)", generated);
        Assert.Contains("AuroraGeneratedNativeMethodAttribute", generated);
        Assert.Contains("IsGetter = true", generated);
        Assert.Contains("RequiresIndexProof = true", generated);
        Assert.Contains("RegisterNativeMembers(ScriptObject prototype)", generated);
        Assert.Equal(1, generated.Split("prototype.Define(\"slice\"").Length - 1);
        Assert.Contains("ScriptDatum.FromBondingGetter(Get)", generated);
        Assert.DoesNotContain("IAuroraNativeInstance", generated);
        Assert.DoesNotContain("void Register(", generated);
        Assert.DoesNotContain("ScriptType Type", generated);
    }

    [Theory]
    [InlineData("public string Core(string value, int index) => value;")]
    [InlineData("private static string Core(string value, int index) => value;")]
    [InlineData("public static string Core(int value, int index) => value.ToString();")]
    [InlineData("public static string Core(ref string value, int index) => value;")]
    [InlineData("public static string Core(string value = null) => value;")]
    [InlineData("public static string Core(string value, int index = 0) => value;")]
    [InlineData("public static string Core(string value, params ScriptDatum[] args) => value;")]
    [InlineData("public static string Core(ref ScriptContext context, string value) => value;")]
    public void ValueReceiverRejectsUnsupportedCoreSignatures(string method)
    {
        var diagnostics = Run(ValueReceiverSource(
            "[AuroraExport(\"value\", DynamicAdapter = nameof(Call))] " + method));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "AURORAEXP002");
    }

    [Theory]
    [InlineData("[AuroraExport(\"value\", RequiresIndexProof = true)] public static int Core(string value, int index) => 0;")]
    [InlineData("[AuroraExport(\"value\", DynamicAdapter = \"Missing\")] public static int Core(string value) => 0;")]
    [InlineData("[AuroraExport(\"value\", DynamicAdapter = nameof(Get))] public static int Core(string value) => 0;")]
    [InlineData("[AuroraExport(\"value\", DynamicAdapter = nameof(Call), IsGetter = true)] public static int Core(string value) => 0;")]
    [InlineData("[AuroraExport(\"value\", DynamicAdapter = nameof(Get), IsGetter = true)] public static void Core(string value) { }")]
    [InlineData("[AuroraExport(\"value\", DynamicAdapter = nameof(Get), IsGetter = true)] public static int Core(string value, int index) => 0;")]
    [InlineData("[AuroraExport(\"value\", DynamicAdapter = nameof(Call), RequiresIndexProof = true)] public static int Core(string value, double index) => 0;")]
    [InlineData("[AuroraExport(\"value\", DynamicAdapter = nameof(Call), RequiresIndexProof = true)] public static double Core(string value, int index) => 0;")]
    [InlineData("[AuroraExport] public ValueMembers() { }")]
    public void ValueReceiverRejectsInvalidAdapterAndProofContracts(string members)
    {
        Assert.Contains(Run(ValueReceiverSource(members)), diagnostic => diagnostic.Id == "AURORAEXP002");
    }

    [Theory]
    [InlineData("int", "Call", false)]
    [InlineData("double", "OtherCall", false)]
    [InlineData("", "Get", true)]
    public void ValueReceiverRejectsConflictingOverloads(string parameter, string adapter, bool getter)
    {
        var secondParameter = parameter.Length == 0 ? "" : ", " + parameter + " index";
        var diagnostics = Run(ValueReceiverSource($$"""
            [AuroraExport("value", DynamicAdapter = nameof(Call))]
            public static int First(string value, int index) => 0;
            [AuroraExport("value", DynamicAdapter = nameof({{adapter}}), IsGetter = {{getter.ToString().ToLowerInvariant()}})]
            public static int Second(string value{{secondParameter}}) => 0;
            private static void OtherCall(ScriptContext context, ScriptObject receiver, Span<ScriptDatum> args, ref ScriptDatum result) { }
            """));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "AURORAEXP003");
    }

    [Fact]
    public void ValueReceiverRejectsUnsupportedClrReceiver()
    {
        var diagnostics = Run(ValueReceiverSource("").Replace("typeof(string)", "typeof(int)", StringComparison.Ordinal));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "AURORAEXP001");
    }

    [Fact]
    public void ValueReceiverGeneratesDefaultAdaptersUsingSharedCoercionAndInvocation()
    {
        var compilation = RunCore(ValueReceiverSource("""
            [AuroraExport("trim")]
            public static string TrimCore(string value) => value.Trim();
            [AuroraExport("length", IsGetter = true)]
            public static int LengthCore(string value) => value.Length;
            [AuroraExport("has")]
            public static bool HasCore(string value, string search) => value.Contains(search);
            """), out var diagnostics);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, compilation.SyntaxTrees.Skip(1).Select(tree => tree.ToString()));
        Assert.Contains("ScriptDatum.FromBonding(__Value_TRIM)", generated);
        Assert.Contains("ScriptDatum.FromBondingGetter(__Value_LENGTH)", generated);
        Assert.Contains("TrimCore(self.Value)", generated);
        Assert.Contains("args.TryGetString(0, out var arg0)", generated);
        Assert.Contains("HasCore(self.Value, arg0)", generated);
    }

    [Fact]
    public void ValueReceiverRequiresExplicitAdapterForOverloads()
    {
        Assert.Contains(Run(ValueReceiverSource("""
            [AuroraExport("value")]
            public static int First(string value, int index) => 0;
            [AuroraExport("value")]
            public static int Second(string value, double index) => 0;
            """)), diagnostic => diagnostic.Id == "AURORAEXP003");
    }

    private static string ValueReceiverSource(string members, bool annotateReceivers = true) => $$"""
        using System;
        using AuroraScript.Hosting;
        using AuroraScript.Runtime;
        using AuroraScript.Runtime.Types;
        namespace Test;
        [AuroraNativeType("String", NativeReceiverType = typeof(string))]
        public sealed partial class ValueMembers
        {
            {{(annotateReceivers ? members.Replace(")]", ", Target = AuroraExportTarget.Instance)]", StringComparison.Ordinal) : members)}}
            private static void Call(ScriptContext context, ScriptObject receiver, Span<ScriptDatum> args, ref ScriptDatum result) { }
            private static void Get(ScriptObject receiver, ref ScriptDatum result) { }
        }
        """;

    [Theory]
    [InlineData("double", "NumberValue", "DoubleValue")]
    [InlineData("long", "Int64Value", "Value")]
    [InlineData("ulong", "UInt64Value", "Value")]
    public void NumericReceiverGeneratesDefaultAdapter(string receiver, string wrapper, string property)
    {
        var source = ValueReceiverSource($$"""
            [AuroraExport("format")]
            public static string FormatCore({{receiver}} value) => value.ToString();
            """).Replace("typeof(string)", $"typeof({receiver})", StringComparison.Ordinal);
        var compilation = RunCore(source, out var diagnostics);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, compilation.SyntaxTrees.Skip(1).Select(t => t.ToString()));
        Assert.Contains($"thisObject is not {wrapper} self", generated);
        Assert.Contains($"FormatCore(self.{property})", generated);
    }

    [Fact]
    public void NumberReceiverOverloadsRetainTheirClrReceiverInMetadata()
    {
        var source = ValueReceiverSource("""
            [AuroraExport("format", DynamicAdapter = nameof(Call))]
            public static string FormatCore(double value, int radix) => value.ToString();
            [AuroraExport("format", DynamicAdapter = nameof(Call))]
            public static string FormatCore(int value, int radix) => value.ToString();
            [AuroraExport("format", DynamicAdapter = nameof(Call))]
            public static string FormatCore(uint value, int radix) => value.ToString();
            """).Replace("typeof(string)", "typeof(double)", StringComparison.Ordinal);
        var compilation = RunCore(source, out var diagnostics);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, compilation.SyntaxTrees.Skip(1).Select(t => t.ToString()));
        Assert.Contains("ReceiverType = typeof(int)", generated);
        Assert.Contains("ReceiverType = typeof(uint)", generated);
        Assert.Equal(1, generated.Split("prototype.Define(\"format\"").Length - 1);
    }

    [Fact]
    public void NumberReceiverSpecializationNeedsAnExplicitDynamicAdapter()
    {
        var source = ValueReceiverSource("""
            [AuroraExport("format")]
            public static string FormatCore(int value) => value.ToString();
            """).Replace("typeof(string)", "typeof(double)", StringComparison.Ordinal);
        Assert.Contains(Run(source), d => d.Id == "AURORAEXP002");
    }

    [Fact]
    public void ReportsNonPartialBuiltinGlobal()
    {
        var diagnostics = Run(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [AuroraNativeType("Bad")]
            public sealed class Bad : ScriptObject
            {
                [AuroraExport("value")]
                public static double Value() => 1;
            }
            """);

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Id == "AURORAEXP001" &&
            diagnostic.GetMessage().Contains("partial", StringComparison.Ordinal));
    }

    [Fact]
    public void ReportsUnsupportedExportSignature()
    {
        var diagnostics = Run(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            using System;
            namespace Test;

            [AuroraNativeType("Bad")]
            public sealed partial class Bad : ScriptObject
            {
                [AuroraExport("value")]
                public static DateTime Value(DateTime value) => value;
            }
            """);

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Id == "AURORAEXP002");
    }

    [Fact]
    public void ReportsDuplicateScriptMemberNames()
    {
        var diagnostics = Run(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [AuroraNativeType("Bad")]
            public sealed partial class Bad : ScriptObject
            {
                [AuroraExport("value")]
                public static double First() => 1;

                [AuroraExport("value")]
                public static double Second() => 2;
            }
            """);

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Id == "AURORAEXP003");
    }

    [Fact]
    public void ReportsDuplicateGlobalNames()
    {
        var diagnostics = Run(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [AuroraNativeType("Same")]
            public sealed partial class First : ScriptObject
            {
                [AuroraExport("first")]
                public static double Value() => 1;
            }

            [AuroraNativeType("Same")]
            public sealed partial class Second : ScriptObject
            {
                [AuroraExport("second")]
                public static double Value() => 2;
            }
            """);

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Id == "AURORAEXP001" &&
            diagnostic.GetMessage().Contains(
                "more than",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ReportsNativeInstanceMustDeriveScriptObject()
    {
        var diagnostics = Run(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [AuroraNativeType("Bad")]
            public sealed partial class Bad
            {
                [AuroraExport("x")]
                public double X;
            }
            """);

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Id == "AURORAEXP001" &&
            diagnostic.GetMessage().Contains(
                "ScriptObject",
                StringComparison.Ordinal));
    }

    [Fact]
    public void NativeObjectCompilesWithoutInternalsVisibleTo()
    {
        var updated = RunCore(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            using System;
            namespace Test;

            [AuroraNativeType("Vec2")]
            public sealed partial class Vec2 : ScriptObject
            {
                [AuroraExport("x")]
                public double X;

                [AuroraExport]
                public Vec2(double x)
                {
                    X = x;
                }

                [AuroraExport("length")]
                public double LengthCore() => Math.Abs(X);
            }
            """,
            out _);

        Assert.Contains(updated.SyntaxTrees, tree =>
            tree.FilePath.EndsWith(
                "Test.Vec2.AuroraNativeType.g.cs",
                StringComparison.Ordinal));
        Assert.DoesNotContain(updated.GetDiagnostics(), diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void NativeObjectGeneratesStaticRuntimeAndCompilerExports()
    {
        var updated = RunCore(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [AuroraNativeType("Widget")]
            public sealed partial class Widget : ScriptObject
            {
                [AuroraExport("value")]
                public double ValueCore() => 1;

                [AuroraExport("value")]
                public static double StaticValueCore() => 2;

                [AuroraExport("COUNT")]
                public static readonly double Count = 3;
            }
            """,
            out var diagnostics);

        Assert.DoesNotContain(diagnostics, diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(updated.GetDiagnostics(), diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(
            Environment.NewLine,
            updated.SyntaxTrees.Select(tree => tree.ToString()));
        Assert.Contains("Define(\"value\", ScriptDatum.FromBonding(__Static_VALUE)", generated);
        Assert.Contains("Define(\"COUNT\", ScriptDatum.FromNumber(Count)", generated);
        Assert.Contains("AuroraGeneratedExportAttribute(\"Widget\", \"value\"", generated);
        Assert.Contains("AuroraGeneratedConstantAttribute(\"Widget\", \"COUNT\"", generated);
    }

    [Fact]
    public void ReportsInvalidNativeObjectStaticConstant()
    {
        var diagnostics = Run(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [AuroraNativeType("Bad")]
            public sealed partial class Bad : ScriptObject
            {
                [AuroraExport("COUNT")]
                public static int Count = 3;
            }
            """);

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Id == "AURORAEXP002" &&
            diagnostic.GetMessage().Contains(
                "public static readonly double",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ReportsMultipleExportedConstructors()
    {
        var diagnostics = Run(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [AuroraNativeType("Bad")]
            public sealed partial class Bad : ScriptObject
            {
                [AuroraExport]
                public Bad() { }

                [AuroraExport]
                public Bad(double value) { }
            }
            """);

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Id == "AURORAEXP003" &&
            diagnostic.GetMessage().Contains(
                "constructor",
                StringComparison.Ordinal));
    }

    [Fact]
    public void EmitsTypedDocumentFactoryWhenUserConstructorExists()
    {
        var compilation = RunCore(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime;
            using AuroraScript.Runtime.Serialization;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [AuroraNativeType("Vec2")]
            public sealed partial class Vec2 : ScriptObject, INativeTypedDocument
            {
                [AuroraExport("x")] public double X;
                [AuroraExport("y")] public double Y;

                [AuroraExport]
                public Vec2(double x, double y)
                {
                    X = x;
                    Y = y;
                }

                public void WriteTypedDocument(ref TypedDocumentOutput output)
                {
                    output.WriteElement(X);
                    output.WriteElement(Y);
                }

                public void ReadTypedDocument(ref TypedDocumentInput input) { }
            }
            """,
            out var diagnostics);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(
            Environment.NewLine,
            compilation.SyntaxTrees.Select(tree => tree.ToString()));
        Assert.Contains(
            "CreateTypedDocument() => new Vec2(default(__AuroraTypedDocumentConstruction))",
            generated);
    }

    private static ImmutableArray<Diagnostic> Run(string source)
    {
        RunCore(source, out var diagnostics);
        return diagnostics;
    }

    private static Compilation RunCore(
        string source,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        var references = new List<MetadataReference>();
        var trustedAssemblies = (string?)AppContext.GetData(
            "TRUSTED_PLATFORM_ASSEMBLIES");
        Assert.NotNull(trustedAssemblies);
        foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
        {
            references.Add(MetadataReference.CreateFromFile(path));
        }
        references.Add(MetadataReference.CreateFromFile(
            typeof(AuroraExportAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "GeneratorDiagnostics",
            [CSharpSyntaxTree.ParseText(source)],
            references.DistinctBy(reference => reference.Display),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new AuroraExportGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var updated,
            out var generatorDiagnostics);

        diagnostics = generatorDiagnostics.AddRange(
            driver.GetRunResult().Diagnostics);
        return updated;
    }
}
