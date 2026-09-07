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
            [Export("echo")]
            public static string EchoCore(string value) => value;
            """, annotateReceivers: false);
        if (!primitive) source = source.Replace("[NativeReceiver(typeof(string))]", "", StringComparison.Ordinal);
        var updated = RunCore(source, out var diagnostics);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(updated.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, updated.SyntaxTrees.Skip(1));
        Assert.Contains("Define(\"echo\", ScriptDatum.FromBonding(__Static_ECHO)", generated);
        Assert.DoesNotContain("prototype.Define(\"echo\"", generated);
        Assert.Contains("EchoCore(arg0)", generated);
    }

    [Theory]
    [InlineData("[NativeReceiver(null)]")]
    [InlineData("[NativeReceiver(typeof(int))]")]
    [InlineData("")]
    public void ReceiverExportRequiresAValidNativeReceiver(string typeAttribute)
    {
        var source = ValueReceiverSource("""
            [Export("echo")]
            public static string EchoCore(string value) => value;
            """).Replace(
                "[NativeReceiver(typeof(string))]",
                typeAttribute,
                StringComparison.Ordinal);
        Assert.Contains(Run(source), d => d.Id is "AURORAEXP001" or "AURORAEXP002");
    }

    [Theory]
    [InlineData("[ReceiverExport(\"echo\")] public string EchoCore(string value) => value;")]
    [InlineData("[ReceiverExport(\"echo\")] public static string EchoCore(int value) => value.ToString();")]
    public void ReceiverExportRejectsInvalidContracts(string member)
    {
        Assert.Contains(Run(ValueReceiverSource(member, annotateReceivers: false)), d => d.Id == "AURORAEXP002");
    }

    [Fact]
    public void PrimitiveFactoryReusesStaticExportCatalogAndGeneratedType()
    {
        var source = ValueReceiverSource("""
            [Export("valueOf")]
            public static string CreateCore(string value = "") => value;
            [Export("compare", DynamicAdapter = nameof(Call))]
            public static int CompareCore(string left, string right) => 1;
            [ReceiverExport("toString")]
            public static string TextCore(string value) => value;
            """, annotateReceivers: false).Replace("NativeReceiver(typeof(string))", "NativeReceiver(typeof(string), Constructor = nameof(CreateCore))", StringComparison.Ordinal);
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
    [InlineData("Missing", "[Export(\"valueOf\")] public static string CreateCore(string value) => value;")]
    [InlineData("CreateCore", "[ReceiverExport(\"valueOf\")] public static string CreateCore(string value) => value;")]
    [InlineData("CreateCore", "[Export(\"valueOf\")] public static int CreateCore(string value) => 1;")]
    [InlineData("CreateCore", "[Export(\"valueOf\", IsGetter = true)] public static string CreateCore() => \"\";")]
    [InlineData("CreateCore", "[Export(\"valueOf\", DynamicAdapter = \"Missing\")] public static string CreateCore(string value) => value;")]
    [InlineData("CreateCore", "[Export(\"valueOf\")] private static string CreateCore(string value) => value;")]
    [InlineData("CreateCore", "[Export(\"valueOf\")] public string CreateCore(string value) => value;")]
    [InlineData("CreateCore", "[Export(\"valueOf\")] public static string CreateCore(params ScriptDatum[] args) => \"\";")]
    public void PrimitiveFactoryRejectsInvalidContracts(string factory, string members)
    {
        var source = ValueReceiverSource(members, annotateReceivers: false).Replace("NativeReceiver(typeof(string))",
            "NativeReceiver(typeof(string), Constructor = \"" + factory + "\")", StringComparison.Ordinal);
        Assert.Contains(Run(source), diagnostic => diagnostic.Id == "AURORAEXP002");
    }

    [Fact]
    public void ValueReceiverUsesExistingNativeCatalogAndRegistersEachAdapterOnce()
    {
        var updated = RunCore(ValueReceiverSource(
            """
            [Export("slice", DynamicAdapter = nameof(Call))]
            public static string SliceCore(string value, int index) => value.Substring(index);
            [Export("slice", DynamicAdapter = nameof(Call))]
            public static string SliceCore(string value, double index) => value.Substring((int)index);
            [Export("length", IsGetter = true, DynamicAdapter = nameof(Get))]
            public static int LengthCore(string value) => value.Length;
            [ReceiverExport("code", DynamicAdapter = nameof(Call))]
            public static int CodeCore(string value, int index) => value[index];
            """), out var diagnostics);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(updated.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, updated.SyntaxTrees.Skip(1).Select(tree => tree.ToString()));
        Assert.Contains("AuroraGeneratedNativeObjectAttribute", generated);
        Assert.Contains("ReceiverType = typeof(string)", generated);
        Assert.Contains("AuroraGeneratedNativeMethodAttribute", generated);
        Assert.Contains("IsGetter = true", generated);
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
            "[ReceiverExport(\"value\", DynamicAdapter = nameof(Call))] " + method, annotateReceivers: false));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "AURORAEXP002");
    }

    [Theory]
    [InlineData("[ReceiverExport(\"value\", DynamicAdapter = \"Missing\")] public static int Core(string value) => 0;")]
    [InlineData("[ReceiverExport(\"value\", DynamicAdapter = nameof(Get))] public static int Core(string value) => 0;")]
    [InlineData("[ReceiverExport(\"value\", DynamicAdapter = nameof(Call), IsGetter = true)] public static int Core(string value) => 0;")]
    [InlineData("[ReceiverExport(\"value\", DynamicAdapter = nameof(Get), IsGetter = true)] public static void Core(string value) { }")]
    [InlineData("[ReceiverExport(\"value\", DynamicAdapter = nameof(Get), IsGetter = true)] public static int Core(string value, int index) => 0;")]
    [InlineData("[ReceiverExport(\"value\", DynamicAdapter = nameof(Get), IsGetter = true, Writable = true)] public static int Core(string value) => 0;")]
    [InlineData("[Export] public ValueMembers() { }")]
    public void ValueReceiverRejectsInvalidAdapterContracts(string members)
    {
        Assert.Contains(Run(ValueReceiverSource(members, annotateReceivers: false)), diagnostic => diagnostic.Id == "AURORAEXP002");
    }

    [Theory]
    [InlineData("int", "Call", false)]
    [InlineData("double", "OtherCall", false)]
    [InlineData("", "Get", true)]
    public void ValueReceiverRejectsConflictingOverloads(string parameter, string adapter, bool getter)
    {
        var secondParameter = parameter.Length == 0 ? "" : ", " + parameter + " index";
        var diagnostics = Run(ValueReceiverSource($$"""
            [Export("value", DynamicAdapter = nameof(Call))]
            public static int First(string value, int index) => 0;
            [Export("value", DynamicAdapter = nameof({{adapter}}), IsGetter = {{getter.ToString().ToLowerInvariant()}})]
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
            [Export("trim")]
            public static string TrimCore(string value) => value.Trim();
            [Export("length", IsGetter = true)]
            public static int LengthCore(string value) => value.Length;
            [Export("has")]
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
            [Export("value")]
            public static int First(string value, int index) => 0;
            [Export("value")]
            public static int Second(string value, double index) => 0;
            """)), diagnostic => diagnostic.Id == "AURORAEXP003");
    }

    private static string ValueReceiverSource(string members, bool annotateReceivers = true) => $$"""
        using System;
        using AuroraScript.Hosting;
        using AuroraScript.Runtime;
        using AuroraScript.Runtime.Types;
        namespace Test;
        [NativeType("String")]
        [NativeReceiver(typeof(string))]
        public sealed partial class ValueMembers
        {
            {{(annotateReceivers ? members.Replace("[Export", "[ReceiverExport", StringComparison.Ordinal) : members)}}
            private static void Call(ScriptContext context, ScriptObject receiver, Span<ScriptDatum> args, ref ScriptDatum result) { }
            private static void Get(ScriptObject receiver, ref ScriptDatum result) { }
        }
        """;

    [Theory]
    [InlineData("bool", "BooleanValue", "Value")]
    [InlineData("double", "NumberValue", "DoubleValue")]
    [InlineData("long", "Int64Value", "Value")]
    [InlineData("ulong", "UInt64Value", "Value")]
    public void PrimitiveReceiverGeneratesDefaultAdapter(string receiver, string wrapper, string property)
    {
        var source = ValueReceiverSource($$"""
            [Export("format")]
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
            [Export("format", DynamicAdapter = nameof(Call))]
            public static string FormatCore(double value, int radix) => value.ToString();
            [Export("format", DynamicAdapter = nameof(Call))]
            public static string FormatCore(int value, int radix) => value.ToString();
            [Export("format", DynamicAdapter = nameof(Call))]
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
            [Export("format")]
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

            [NativeType("Bad")]
            public sealed class Bad : ScriptObject
            {
                [Export("value")]
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

            [NativeType("Bad")]
            public sealed partial class Bad : ScriptObject
            {
                [Export("value")]
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

            [NativeType("Bad")]
            public sealed partial class Bad : ScriptObject
            {
                [Export("value")]
                public static double First() => 1;

                [Export("value")]
                public static double Second() => 2;
            }
            """);

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Id == "AURORAEXP003");
    }

    [Fact]
    public void NativeObjectGeneratesPairedPropertyAccessors()
    {
        var updated = RunCore(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [NativeType("Widget")]
            public sealed partial class Widget : ScriptObject
            {
                [Export("value", IsGetter = true)]
                public double GetValueCore() => 1;

                [Export("value", IsSetter = true)]
                public void SetValueCore(double value) { }
            }
            """,
            out var diagnostics);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(updated.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, updated.SyntaxTrees.Select(tree => tree.ToString()));
        Assert.Contains("__Get_VALUE(ctx, this, Span<ScriptDatum>.Empty, ref result)", generated);
        Assert.Contains("__Set_VALUE(ctx, this, MemoryMarshal.CreateSpan(ref value, 1), ref result)", generated);
        Assert.Contains("IsGetter = true", generated);
        Assert.Contains("IsSetter = true", generated);
    }

    [Theory]
    [InlineData("[Export(\"value\", IsGetter = true, IsSetter = true)] public double Core() => 0;")]
    [InlineData("[Export(\"value\", IsGetter = true)] public void Core() { }")]
    [InlineData("[Export(\"value\", IsGetter = true)] public double Core(double value) => value;")]
    [InlineData("[Export(\"value\", IsSetter = true)] public double Core(double value) => value;")]
    [InlineData("[Export(\"value\", IsSetter = true)] public void Core() { }")]
    [InlineData("[Export(\"value\", IsSetter = true)] public void Core(double value = 0) { }")]
    [InlineData("[Export(\"value\", IsSetter = true)] public double Value;")]
    public void NativeObjectRejectsInvalidAccessorContracts(string member)
    {
        var source = $$"""
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [NativeType("Widget")]
            public sealed partial class Widget : ScriptObject
            {
                {{member}}
            }
            """;

        Assert.Contains(Run(source), diagnostic => diagnostic.Id == "AURORAEXP002");
    }

    [Fact]
    public void ReportsDuplicateGlobalNames()
    {
        var diagnostics = Run(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [NativeType("Same")]
            public sealed partial class First : ScriptObject
            {
                [Export("first")]
                public static double Value() => 1;
            }

            [NativeType("Same")]
            public sealed partial class Second : ScriptObject
            {
                [Export("second")]
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

            [NativeType("Bad")]
            public sealed partial class Bad
            {
                [Export("x")]
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

            [NativeType("Vec2")]
            public sealed partial class Vec2 : ScriptObject
            {
                [Export("x")]
                public double X;

                [Export]
                public Vec2(double x) : base(NativePrototype)
                {
                    X = x;
                }

                [Export("length")]
                public double LengthCore() => Math.Abs(X);
            }
            """,
            out _);

        Assert.Contains(updated.SyntaxTrees, tree =>
            tree.FilePath.EndsWith(
                "Test.Vec2.NativeType.g.cs",
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

            [NativeType("Widget")]
            public sealed partial class Widget : ScriptObject
            {
                [Export("value")]
                public double ValueCore() => 1;

                [Export("value")]
                public static double StaticValueCore() => 2;

                [Export("COUNT")]
                public static readonly double Count = 3;
                [Export("ENABLED")]
                public static readonly bool Enabled = true;
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
        Assert.DoesNotContain("__Static_VALUEBonding", generated);
        Assert.Contains("RegisterNativeMembers(ScriptObject prototype)", generated);
        Assert.Contains("prototype.Define(\"value\", ScriptDatum.FromBonding(VALUE), writeable: false, enumerable: false)", generated);
        Assert.Contains("internal static ScriptObject NativePrototype => NativePrototypeHolder.Value", generated);
        Assert.DoesNotContain("case \"value\":", generated);
        Assert.Contains("Define(\"COUNT\", ScriptDatum.FromNumber(Count)", generated);
        Assert.Contains("Define(\"ENABLED\", ScriptDatum.FromBoolean(Enabled)", generated);
        Assert.Contains("AuroraGeneratedExportAttribute(\"Widget\", \"value\"", generated);
        Assert.Contains("AuroraGeneratedConstantAttribute(\"Widget\", \"COUNT\"", generated);
        Assert.Contains("AuroraGeneratedConstantAttribute(\"Widget\", \"ENABLED\"", generated);
    }

    [Fact]
    public void ObjectNativeTypeCanUseExplicitDynamicAdaptersForDirectCores()
    {
        var updated = RunCore(
            """
            using System;
            using AuroraScript.Hosting;
            using AuroraScript.Runtime;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [NativeType("Widget")]
            public sealed partial class Widget : ScriptObject
            {
                [Export("format", DynamicAdapter = nameof(FORMAT))]
                public string FormatCore(string value) => value;

                [Export("format", DynamicAdapter = nameof(FORMAT))]
                public string FormatCore(string left, string right) => left + right;

                [Export("format", DynamicAdapter = nameof(FORMAT))]
                public string FormatCore(params ScriptDatum[] values) => string.Empty;

                [Export("create", DynamicAdapter = nameof(CREATE))]
                public static Widget CreateCore(string value = null) => new Widget();

                [Export("create", DynamicAdapter = nameof(CREATE))]
                public static Widget CreateCore(ScriptObject value) => new Widget();

                public static void FORMAT(ScriptContext context, ScriptObject receiver,
                    Span<ScriptDatum> args, ref ScriptDatum result) { }
                public static void CREATE(ScriptContext context, ScriptObject receiver,
                    Span<ScriptDatum> args, ref ScriptDatum result) { }
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
        Assert.Contains("prototype.Define(\"format\", ScriptDatum.FromBonding(FORMAT)", generated);
        Assert.Contains("Define(\"create\", ScriptDatum.FromBonding(CREATE)", generated);
        Assert.Equal(1, generated.Split("public static void FORMAT(").Length - 1);
        Assert.Equal(4, generated.Split("UseDynamicForExtraArguments = true").Length - 1);
    }

    [Fact]
    public void ExportFlagsControlDescriptorsEnumerationAndNativeCatalogEligibility()
    {
        var updated = RunCore(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [NativeType("Widget")]
            public sealed partial class Widget : ScriptObject
            {
                [Export("stable")]
                public string StableCore() => "stable";

                [Export("replaceable", Writable = true, Enumerable = true)]
                public string ReplaceableCore() => "replaceable";

                [Export("visible", Enumerable = true)]
                public double Visible;
            }
            """,
            out var diagnostics);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(updated.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, updated.SyntaxTrees.Select(tree => tree.ToString()));
        Assert.Contains("prototype.Define(\"stable\", ScriptDatum.FromBonding(STABLE), writeable: false, enumerable: false)", generated);
        Assert.Contains("prototype.Define(\"replaceable\", ScriptDatum.FromBonding(REPLACEABLE), writeable: true, enumerable: true)", generated);
        Assert.Contains("AuroraGeneratedNativeMethodAttribute(\"Widget\", \"stable\"", generated);
        Assert.DoesNotContain("AuroraGeneratedNativeMethodAttribute(\"Widget\", \"replaceable\"", generated);
        Assert.Contains("AuroraGeneratedNativeFieldAttribute(\"Widget\", \"visible\"", generated);
        Assert.Contains("keys.Add(ScriptDatum.FromString(\"visible\"))", generated);
        Assert.DoesNotContain("prototype.Frozen()", generated);
    }

    [Fact]
    public void ReportsInvalidNativeObjectStaticConstant()
    {
        var diagnostics = Run(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [NativeType("Bad")]
            public sealed partial class Bad : ScriptObject
            {
                [Export("COUNT")]
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

            [NativeType("Bad")]
            public sealed partial class Bad : ScriptObject
            {
                [Export]
                public Bad() { }

                [Export]
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

            [NativeType("Vec2")]
            public sealed partial class Vec2 : ScriptObject, INativeTypedDocument
            {
                [Export("x")] public double X;
                [Export("y")] public double Y;

                [Export]
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

    [Fact]
    public void PackageTypeEmitsRegisterPackageWithoutGlobalRegister()
    {
        var updated = RunCore(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [NativeType("fs")]
            [NativePackage("fs")]
            public sealed partial class FileSystemSupport : ScriptObject
            {
                [Export("readText")]
                public static string ReadTextCore(string path) => path;
            }
            """,
            out var diagnostics);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(updated.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, updated.SyntaxTrees.Skip(1).Select(tree => tree.ToString()));
        Assert.Contains("public static void RegisterPackage(ScriptObject module)", generated);
        Assert.Contains("module.Define(\"readText\"", generated);
        Assert.DoesNotContain("public static void Register(ScriptObject target", generated);
        Assert.Contains("AuroraGeneratedExportAttribute(\"fs\", \"readText\"", generated);
    }

    [Fact]
    public void PackageTypeRejectsInstanceExportsAndReservedNames()
    {
        var instance = Run(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [NativeType("fs")]
            [NativePackage("fs")]
            public sealed partial class FileSystemSupport : ScriptObject
            {
                [Export("x")] public double X;
            }
            """);
        Assert.Contains(instance, diagnostic =>
            diagnostic.GetMessage().Contains("Native packages cannot export constructors", StringComparison.Ordinal));

        var reserved = Run(
            """
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;

            [NativeType("Math")]
            [NativePackage("math")]
            public sealed partial class MathPackage : ScriptObject
            {
                [Export("abs")]
                public static double AbsCore(double value) => value;
            }
            """);
        Assert.Contains(reserved, diagnostic =>
            diagnostic.GetMessage().Contains("conflicts with an engine infrastructure Type", StringComparison.Ordinal));
    }

    private static ImmutableArray<Diagnostic> Run(string source)
    {
        RunCore(source, out var diagnostics);
        return diagnostics;
    }

    [Fact]
    public void ExternalHostCannotUseReceiverAttributes()
    {
        var updated = RunCore(ValueReceiverSource("""
            [ReceiverExport("text")]
            public static string TextCore(string value) => value;
            """, annotateReceivers: false), out _, allowInternalReceivers: false);
        var errors = updated.GetDiagnostics();
        Assert.Contains(errors, d => d.Id == "CS0122" && d.GetMessage().Contains("NativeReceiver", StringComparison.Ordinal));
        Assert.Contains(errors, d => d.Id == "CS0122" && d.GetMessage().Contains("ReceiverExport", StringComparison.Ordinal));
    }

    [Fact]
    public void ConstructorCompatibilityAdapterUsesExistingExportAttribute()
    {
        var updated = RunCore("""
            using System;
            using AuroraScript.Hosting;
            using AuroraScript.Runtime;
            using AuroraScript.Runtime.Types;
            namespace Test;
            [NativeType("Custom")]
            public sealed partial class Custom : ScriptObject {
                [Export(DynamicAdapter = nameof(Create))]
                private Custom() { }
                private static void Create(ScriptContext ctx, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) {
                    result = ScriptDatum.FromObject(new Custom());
                }
            }
            """, out var diagnostics, allowInternalReceivers: false);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(updated.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, updated.SyntaxTrees.Skip(1));
        Assert.Contains("Create(ctx, this, args, ref result);", generated);
        Assert.Contains("base(\"Custom\", false)", generated);
    }

    [Fact]
    public void ClrGetterCanBeExportedWithoutAnExtraCoreWrapper()
    {
        var updated = RunCore("""
            using AuroraScript.Hosting;
            using AuroraScript.Runtime.Types;
            namespace Test;
            [NativeType("Meter")]
            public sealed partial class Meter : ScriptObject {
                public int Count { [Export("count", IsGetter = true)] get => 7; }
            }
            """, out var diagnostics, allowInternalReceivers: false);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(updated.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        var generated = string.Join(Environment.NewLine, updated.SyntaxTrees.Skip(1));
        Assert.Contains("self.@Count", generated);
        Assert.Contains("\"get_Count\"", generated);
        Assert.DoesNotContain("self.get_Count()", generated);
    }

    private static Compilation RunCore(
        string source,
        out ImmutableArray<Diagnostic> diagnostics,
        bool allowInternalReceivers = true)
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
            typeof(ExportAttribute).Assembly.Location));

        // Only primitive fixtures use the existing test friend assembly identity.
        // Ordinary host fixtures must continue to compile without internal access.
        var internalReceiver = allowInternalReceivers &&
            (source.Contains("[NativeReceiver(", StringComparison.Ordinal) ||
             source.Contains("[ReceiverExport(", StringComparison.Ordinal));
        var compilation = CSharpCompilation.Create(
            internalReceiver ? "AuroraScript.Tests" : "GeneratorDiagnostics",
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
