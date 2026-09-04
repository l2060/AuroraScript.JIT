using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class TypeCheckTests
{
    [Fact]
    public void DedicatedTypeChecksAreInlineCandidates()
    {
        foreach (var type in Enum.GetValues<CheckedType>())
        {
            var method = typeof(TypeCheckOps).GetMethod("Check" + type);
            Assert.NotNull(method);
            Assert.True(method.MethodImplementationFlags.HasFlag(
                MethodImplAttributes.AggressiveInlining));
        }

        Assert.True(typeof(TypeCheckOps)
            .GetMethod(nameof(TypeCheckOps.Check))!
            .MethodImplementationFlags
            .HasFlag(MethodImplAttributes.NoInlining));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistenceEmitsDedicatedTypeCheckCalls()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func check(value) {
                return [
                    value as Null,
                    value as Boolean,
                    value as Number,
                    value as int32,
                    value as uint32,
                    value as String,
                    value as Object,
                    value as Array,
                    value as Int32Array,
                    value as Int8Array,
                    value as Float32Array,
                    value as Float64Array,
                    value as BooleanArray,
                    value as UInt8Array,
                    value as Int16Array,
                    value as UInt16Array,
                    value as UInt32Array,
                    value as Int64Array,
                    value as UInt64Array
                ];
            }
            """,
            CompilationMode.Persistence);

        using var stream = File.OpenRead(
            Path.Combine(workspace.Root, "test-output.dll"));
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var calls = reader.MemberReferences
            .Where(handle =>
            {
                var member = reader.GetMemberReference(handle);
                if (member.Parent.Kind != HandleKind.TypeReference) return false;
                var parent = reader.GetTypeReference(
                    (TypeReferenceHandle)member.Parent);
                return reader.GetString(parent.Name) == nameof(TypeCheckOps);
            })
            .Select(handle => reader.GetString(
                reader.GetMemberReference(handle).Name))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var type in Enum.GetValues<CheckedType>())
        {
            // Scalar integer assertions validate and return the CLR value in
            // one step instead of handing the datum back.
            Assert.Contains(
                type == CheckedType.Int32
                    ? nameof(TypeCheckOps.CheckInt32Value)
                    : type == CheckedType.UInt32
                        ? nameof(TypeCheckOps.CheckUInt32Value)
                    : "Check" + type,
                calls);
        }
        Assert.DoesNotContain(nameof(TypeCheckOps.Check), calls);
    }
#endif

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CheckExpressionsAndParametersAssertExactTypes(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func add(Number a, Number b) {
                return a + b;
            }
            export func increment(value) {
                var number = value as Number;
                return number + 1;
            }
            export func first(value) {
                var values = value as Float64Array;
                values[0] = values[0] + 1;
                return values[0];
            }
            export func identity(Number) {
                return Number;
            }
            """,
            mode);

        ScriptAssert.Equal(
            5,
            TestWorkspace.Execute(
                domain,
                "add",
                arguments: [
                    ScriptDatum.FromNumber(2),
                    ScriptDatum.FromNumber(3)]));
        ScriptAssert.Equal(
            8,
            TestWorkspace.Execute(
                domain,
                "increment",
                arguments: [ScriptDatum.FromNumber(7)]));
        ScriptAssert.Equal(
            "not a type declaration",
            TestWorkspace.Execute(
                domain,
                "identity",
                arguments: [ScriptDatum.FromString("not a type declaration")]));

        var values = new ScriptFloat64Array(1);
        values._items[0] = 4;
        ScriptAssert.Equal(
            5,
            TestWorkspace.Execute(
                domain,
                "first",
                arguments: [ScriptDatum.FromObject(values)]));

        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "add",
                arguments: [
                    ScriptDatum.FromString("2"),
                    ScriptDatum.FromNumber(3)]));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "first",
                arguments: [ScriptDatum.FromObject(new ScriptInt32Array(1))]));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task DeclaredReturnTypesAssertDynamicValuesAndPreserveNativeValues(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func add(Number a, Number b) Number {
                return a + b;
            }
            export func identity(value) Number {
                return value;
            }
            export func missing() Number {
                return;
            }
            """,
            mode);

        ScriptAssert.Equal(
            5,
            TestWorkspace.Execute(
                domain,
                "add",
                arguments: [
                    ScriptDatum.FromNumber(2),
                    ScriptDatum.FromNumber(3)]));
        ScriptAssert.Equal(
            7,
            TestWorkspace.Execute(
                domain,
                "identity",
                arguments: [ScriptDatum.FromNumber(7)]));

        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "identity",
                arguments: [ScriptDatum.FromString("7")]));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(domain, "missing"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CustomTypesGrantCompileTimeNativeFieldFacts(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export type Point {
                Number x;
                Number y;
            }
            var topLevel = { x: 2, y: 3 } as Point;
            export func add(Point p) Number {
                return p.x + p.y;
            }
            export func grant(value) Number {
                var p = value as Point;
                return p.x + p.y;
            }
            export func assignGrant(value) Number {
                var p;
                p = value as Point;
                return p.x + p.y;
            }
            func sumPoint(Point p) Number {
                return p.x + p.y;
            }
            export func fromLiteral() Number {
                return sumPoint({ x: 2, y: 3 });
            }
            export func assignLiteral(Point p) Number {
                p = { x: 4, y: 5 };
                return p.x + p.y;
            }
            export func bothBranches(flag, Point a, Point b) Number {
                var p;
                if (flag) p = a;
                else p = b;
                return p.x + p.y;
            }
            export func mixedBranches(flag, Point a, value) Number {
                var p;
                if (flag) p = a;
                else p = value;
                return p.x + p.y;
            }
            export func origin() Point {
                return { x: 1, y: 2 };
            }
            export func originSum() Number {
                var p = origin();
                return p.x + p.y;
            }
            export func valid() Point {
                return { x: 1, y: null };
            }
            export func topLevelValue() {
                return topLevel;
            }
            export func acceptWrongShape() {
                return add({ x: "wrong" });
            }
            export func acceptMissingFields() {
                return add({});
            }
            export func nonObject() {
                return 1 as Point;
            }
            export func dynamic(value) Point {
                return value;
            }
            """,
            mode);

        var point = TestWorkspace.Execute(domain, "valid");
        Assert.Equal(ValueKind.Object, point.Kind);
        Assert.Equal(
            ValueKind.Object,
            TestWorkspace.Execute(domain, "topLevelValue").Kind);
        ScriptAssert.Equal(
            5,
            TestWorkspace.Execute(
                domain,
                "add",
                arguments: [
                    ScriptDatum.FromObject(CreatePoint(2, 3))]));
        ScriptAssert.Equal(
            9,
            TestWorkspace.Execute(domain, "grant", arguments: [
                ScriptDatum.FromObject(CreatePoint(4, 5))]));
        ScriptAssert.Equal(
            11,
            TestWorkspace.Execute(domain, "assignGrant", arguments: [
                ScriptDatum.FromObject(CreatePoint(6, 5))]));
        ScriptAssert.Equal(5, TestWorkspace.Execute(domain, "fromLiteral"));
        ScriptAssert.Equal(
            9,
            TestWorkspace.Execute(
                domain,
                "assignLiteral",
                arguments: [ScriptDatum.FromObject(CreatePoint(0, 0))]));
        ScriptAssert.Equal(
            10,
            TestWorkspace.Execute(
                domain,
                "bothBranches",
                arguments: [
                    ScriptDatum.True,
                    ScriptDatum.FromObject(CreatePoint(4, 6)),
                    ScriptDatum.FromObject(CreatePoint(1, 2))]));
        ScriptAssert.Equal(
            3,
            TestWorkspace.Execute(
                domain,
                "bothBranches",
                arguments: [
                    ScriptDatum.False,
                    ScriptDatum.FromObject(CreatePoint(4, 6)),
                    ScriptDatum.FromObject(CreatePoint(1, 2))]));
        ScriptAssert.Equal(
            5,
            TestWorkspace.Execute(
                domain,
                "mixedBranches",
                arguments: [
                    ScriptDatum.True,
                    ScriptDatum.FromObject(CreatePoint(2, 3)),
                    ScriptDatum.FromObject(CreatePoint(8, 1))]));
        ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "originSum"));
        Assert.Equal(
            ValueKind.Number,
            TestWorkspace.Execute(domain, "nonObject").Kind);
        Assert.Equal(
            ValueKind.Number,
            TestWorkspace.Execute(
                domain,
                "dynamic",
                arguments: [ScriptDatum.FromNumber(1)]).Kind);
        Assert.True(double.IsNaN(
            TestWorkspace.Execute(domain, "acceptWrongShape").Number));
        ScriptAssert.Equal(
            0,
            TestWorkspace.Execute(domain, "acceptMissingFields"));
    }

    private static ScriptObject CreatePoint(double x, double y)
    {
        var point = new ScriptObject();
        point.Define("x", ScriptDatum.FromNumber(x));
        point.Define("y", ScriptDatum.FromNumber(y));
        return point;
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ExportedTypesFlowThroughQualifiedModuleImports(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "models.as",
            """
            @module(MODELS);
            export type Point {
                Number x;
                Number y;
            }
            type InternalPoint {
                Number x;
            }
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import models from './models';
            func add(models.Point p) Number {
                return p.x + p.y;
            }
            export func run(value) Number {
                var p = value as models.Point;
                return add(p);
            }
            export func create() models.Point {
                return { x: 4, y: 5 };
            }
            """);

        var engine = workspace.CreateEngine(
            mode,
            assemblyOut: mode == CompilationMode.Persistence
                ? Path.Combine(workspace.Root, "test-output.dll")
                : null);
        await engine.BuildAsync(["main.as"]);
        var domain = engine.CreateDomain();

        ScriptAssert.Equal(
            7,
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.FromObject(CreatePoint(3, 4))]));
        Assert.Equal(ValueKind.Object, TestWorkspace.Execute(domain, "create").Kind);
    }

    [Fact]
    public async Task ImportedTypesMustExistAndBeExported()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "models.as",
            """
            @module(MODELS);
            type InternalPoint {
                Number x;
            }
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import models from './models';
            export func run(models.InternalPoint value) {
                return value;
            }
            """);

        var engine = workspace.CreateEngine();
        var error = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => engine.BuildAsync(["main.as"]));
        Assert.Contains(
            "Unknown or inaccessible type 'models.InternalPoint'",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportedTypesCannotBeUsedAsValues()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "models.as",
            """
            @module(MODELS);
            export type Point {
                Number x;
                Number y;
            }
            export func add(Point p) Number {
                return p.x + p.y;
            }
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import models from './models';
            export func run() {
                return models.Point;
            }
            """);

        var engine = workspace.CreateEngine();
        var error = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => engine.BuildAsync(["main.as"]));
        Assert.Contains(
            "Type 'models.Point' is compile-time only and cannot be used as a value",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportedModuleValueExportsRemainReadable()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "models.as",
            """
            @module(MODELS);
            export type Point {
                Number x;
                Number y;
            }
            export const originX = 1;
            export func add(Point p) Number {
                return p.x + p.y;
            }
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import models from './models';
            export func run(models.Point p) Number {
                return models.add(p) + models.originX;
            }
            """);

        var engine = workspace.CreateEngine();
        await engine.BuildAsync(["main.as"]);
        var domain = engine.CreateDomain();
        ScriptAssert.Equal(
            6,
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.FromObject(CreatePoint(2, 3))]));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PointFieldArithmeticEmitsNativeKernel()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export type Point {
                Number x;
                Number y;
            }
            native func sumPoint(Point p) Number {
                return p.x + p.y;
            }
            export func run() Number {
                return sumPoint({ x: 2, y: 3 });
            }
            """,
            CompilationMode.Persistence);

        using var stream = File.OpenRead(Path.Combine(workspace.Root, "test-output.dll"));
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        foreach (var handle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(handle);
            if (string.Equals(
                reader.GetString(method.Name),
                "sumPoint$native",
                StringComparison.Ordinal))
            {
                return;
            }
        }
        Assert.Fail("Persisted method not found: sumPoint$native");
    }
#endif

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NestedShapesDeriveNativeFieldsThroughObjectMembers(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export type Point {
                Number x;
                Number y;
            }
            export type Rect {
                Point origin;
                Number width;
                Number height;
            }
            export func area(Rect rect) Number {
                return rect.origin.x * rect.width;
            }
            export func fromLiteral() Number {
                return area({
                    origin: { x: 2, y: 0 },
                    width: 4,
                    height: 1
                });
            }
            """,
            mode);

        var rect = new ScriptObject();
        rect.Define("origin", ScriptDatum.FromObject(CreatePoint(3, 9)));
        rect.Define("width", ScriptDatum.FromNumber(5));
        rect.Define("height", ScriptDatum.FromNumber(2));
        ScriptAssert.Equal(
            15,
            TestWorkspace.Execute(
                domain,
                "area",
                arguments: [ScriptDatum.FromObject(rect)]));
        ScriptAssert.Equal(8, TestWorkspace.Execute(domain, "fromLiteral"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NestedShapesAcceptQualifiedImportedFieldTypes(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "models.as",
            """
            @module(MODELS);
            export type Point {
                Number x;
                Number y;
            }
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import models from './models';
            export type Rect {
                models.Point origin;
                Number width;
            }
            export func left(Rect rect) Number {
                return rect.origin.x + rect.width;
            }
            """);

        var engine = workspace.CreateEngine(
            mode,
            assemblyOut: mode == CompilationMode.Persistence
                ? Path.Combine(workspace.Root, "test-output.dll")
                : null);
        await engine.BuildAsync(["main.as"]);
        var domain = engine.CreateDomain();
        var rect = new ScriptObject();
        rect.Define("origin", ScriptDatum.FromObject(CreatePoint(1, 2)));
        rect.Define("width", ScriptDatum.FromNumber(6));
        ScriptAssert.Equal(
            7,
            TestWorkspace.Execute(
                domain,
                "left",
                arguments: [ScriptDatum.FromObject(rect)]));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CyclicShapesDeriveNativeFieldsThroughLinks(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export type Node {
                Number value;
                Node next;
            }
            export func head(Node node) Number {
                return node.value;
            }
            export func tailValue(Node node) Number {
                return node.next.value;
            }
            export func walk(Number total, Node node) Number {
                if (node == null) return total;
                return walk(total + node.value, node.next);
            }
            """,
            mode);

        var tail = new ScriptObject();
        tail.Define("value", ScriptDatum.FromNumber(2));
        tail.Define("next", ScriptDatum.Null);
        var head = new ScriptObject();
        head.Define("value", ScriptDatum.FromNumber(1));
        head.Define("next", ScriptDatum.FromObject(tail));

        ScriptAssert.Equal(
            1,
            TestWorkspace.Execute(
                domain,
                "head",
                arguments: [ScriptDatum.FromObject(head)]));
        ScriptAssert.Equal(
            2,
            TestWorkspace.Execute(
                domain,
                "tailValue",
                arguments: [ScriptDatum.FromObject(head)]));
        ScriptAssert.Equal(
            3,
            TestWorkspace.Execute(
                domain,
                "walk",
                arguments: [
                    ScriptDatum.FromNumber(0),
                    ScriptDatum.FromObject(head)]));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task MutuallyRecursiveShapesDeriveNativeFields(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export type Left {
                Number value;
                Right other;
            }
            export type Right {
                Number value;
                Left other;
            }
            export func leftScore(Left value) Number {
                return value.value + value.other.value;
            }
            """,
            mode);

        var right = new ScriptObject();
        right.Define("value", ScriptDatum.FromNumber(20));
        right.Define("other", ScriptDatum.Null);
        var left = new ScriptObject();
        left.Define("value", ScriptDatum.FromNumber(1));
        left.Define("other", ScriptDatum.FromObject(right));

        ScriptAssert.Equal(
            21,
            TestWorkspace.Execute(
                domain,
                "leftScore",
                arguments: [ScriptDatum.FromObject(left)]));
    }

    [Fact]
    public async Task CheckRejectsUnsupportedTypeNamesAtCompileTime()
    {
        using var workspace = new TestWorkspace();
        await Assert.ThrowsAsync<AuroraCompilationException>(() =>
            workspace.CompileModuleAsync(
                """
                @module(TEST);
                export func run(value) {
                    return value as MissingType;
                }
                """));
    }
}
