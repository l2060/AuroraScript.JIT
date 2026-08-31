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
