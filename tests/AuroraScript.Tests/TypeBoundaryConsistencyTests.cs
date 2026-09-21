using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class TypeBoundaryConsistencyTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task RepresentationDoesNotChangeNumberSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export func overflow() { var n = 2147483647; return n + 1; }
            export func mutable() { var n = 1 as int32; n = 1.5; return n; }
            export func captured() { var n = 1 as int32; var f = () => n; n = 1.5; return f(); }
            export func modulo() { var n = 1; return n % 0; }
            export func unsigned() { var n = 4294967295u; return [n + 1, n >> 1, n | 0]; }
            export func cast(value) { return (int32)value; }
            export func castConstant() { return (int32)-123.5 + (int32)2.9 * 2; }
            export func cancelled(int32 x) int32 { return (x + 1) - 1; }
            export func invalidated(int32 x) { if (x > 0) { x = -2147483648; return x - 1; } return 0; }
            """, mode);
        using (domain)
        {
            ScriptAssert.Equal(2147483648d, TestWorkspace.Execute(domain, "overflow"));
            ScriptAssert.Equal(1.5, TestWorkspace.Execute(domain, "mutable"));
            ScriptAssert.Equal(1.5, TestWorkspace.Execute(domain, "captured"));
            Assert.True(double.IsNaN(TestWorkspace.Execute(domain, "modulo").Number));
            var values = (ScriptArray)TestWorkspace.Execute(domain, "unsigned").Reference;
            ScriptAssert.Equal(4294967296d, values.GetElement(0));
            ScriptAssert.Equal(-1, values.GetElement(1));
            ScriptAssert.Equal(-1, values.GetElement(2));
            ScriptAssert.Equal(-119, TestWorkspace.Execute(domain, "castConstant"));
            ScriptAssert.Equal(int.MaxValue, TestWorkspace.Execute(domain, "cancelled", arguments: [ScriptDatum.FromNumber(int.MaxValue)]));
            ScriptAssert.Equal(-2147483649d, TestWorkspace.Execute(domain, "invalidated", arguments: [ScriptDatum.FromNumber(1)]));
            ScriptAssert.Equal(123, TestWorkspace.Execute(domain, "cast", arguments: [ScriptDatum.FromNumber(123.9)]));
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "cast",
                arguments: [ScriptDatum.FromString("12")]));
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "cast",
                arguments: [ScriptDatum.Null]));
            ScriptAssert.Equal(0, TestWorkspace.Execute(domain, "cast",
                arguments: [ScriptDatum.FromNumber(double.NaN)]));
            ScriptAssert.Equal(int.MinValue, TestWorkspace.Execute(domain, "cast",
                arguments: [ScriptDatum.FromNumber(double.PositiveInfinity)]));
            ScriptAssert.Equal(int.MinValue, TestWorkspace.Execute(domain, "cast",
                arguments: [ScriptDatum.FromNumber(2147483648d)]));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task IntegerRangeProofsRespectEvaluationAndControlFlow(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func ordered() Number { var x = 2147483647; return x + (x = 1); }
            export native func condition(int32 x) Number {
                if (x > 0 && (x = -2147483648) < 0) return x - 1;
                return 0;
            }
            export native func branch(Boolean flag) Number {
                var x = 0; if (flag) x = 2147483647; return x + 1;
            }
            export native func loop() Number {
                var x = 0; var result = 0;
                for (var i = 0; i < 2; i++) { result = x + 1; x = 2147483647; }
                return result;
            }
            export native func continued(Boolean flag) Number {
                var x = 0; var n = 0; var result = 0;
                for (; n < 1; result = x + 1) {
                    n++;
                    if (flag) { x = 2147483647; continue; }
                    x = 0;
                }
                return result;
            }
            export func caught() {
                var x = 0;
                try { x = 2147483647; throw 'test'; } catch (e) { return x + 1; }
            }
            export native func wideModulo() Number { var x = 2147483647; x = x + 2; return x % 2; }
            export func zeros() { var zero = 0; return [0 * -1, -1 * 0, zero * -1, -2 % 2]; }
            export native func indexed(Int32Array values, uint32 x) int32 { return values[x + 1]; }
            export native func guarded(Int32Array a, Int32Array b, int32 parent) int32 {
                var left = parent * 2 + 1;
                if (left < 0 || left >= a.length) return -1;
                return a[left] ^ b[left];
            }
            export func runGuarded(int32 parent) {
                var a = new Int32Array(2); a[1] = 3;
                var b = new Int32Array(2); b[1] = 5;
                return guarded(a, b, parent);
            }
            export func invalidIndex() { return indexed(new Int32Array(1), 4294967295u); }
            export func mutableViews(int32 start) {
                var n = start + 1;
                var original = n;
                if (n > 100) n = 1;
                var first = n ^ (n << 1);
                n = 7;
                var assigned = n ^ (n << 1);
                n += 2;
                var compounded = n ^ (n << 1);
                var post = n++;
                var pre = ++n;
                var mutated = n ^ (n << 1);
                n = 4294967296;
                var wide = n;
                n = -0;
                return [original, first, assigned, compounded, post, pre, mutated, wide, n];
            }
            """, mode);
        using (domain)
        {
            foreach (var method in new[] { "ordered", "loop", "caught" })
                ScriptAssert.Equal(2147483648d, TestWorkspace.Execute(domain, method));
            foreach (var method in new[] { "branch", "continued" })
            {
                ScriptAssert.Equal(2147483648d, TestWorkspace.Execute(domain, method, arguments: [ScriptDatum.True]));
                ScriptAssert.Equal(1, TestWorkspace.Execute(domain, method, arguments: [ScriptDatum.False]));
            }
            ScriptAssert.Equal(-2147483649d, TestWorkspace.Execute(domain, "condition", arguments: [ScriptDatum.FromNumber(1)]));
            ScriptAssert.Equal(1, TestWorkspace.Execute(domain, "wideModulo"));
            var zeros = (ScriptArray)TestWorkspace.Execute(domain, "zeros").Reference;
            for (var i = 0; i < 4; i++) Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(zeros.GetElement(i).Number));
            ScriptAssert.Equal(6, TestWorkspace.Execute(domain, "runGuarded", arguments: [ScriptDatum.FromNumber(0)]));
            ScriptAssert.Equal(-1, TestWorkspace.Execute(domain, "runGuarded", arguments: [ScriptDatum.FromNumber(int.MaxValue)]));
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "invalidIndex"));
            var updated = (ScriptArray)TestWorkspace.Execute(domain, "mutableViews",
                arguments: [ScriptDatum.FromNumber(int.MaxValue)]).Reference;
            var expected = new double[] { 2147483648d, 3, 9, 27, 9, 11, 29, 4294967296d };
            for (var i = 0; i < expected.Length; i++) ScriptAssert.Equal(expected[i], updated.GetElement(i));
            Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(updated.GetElement(8).Number));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task InferredIntegersWidenWithoutChangingNumberSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export func widened(int32 start) {
                var n = start + 1;
                var original = n;
                var restored = n - 1;
                n += 2;
                var added = n;
                n %= 3;
                return [original, restored, added, n, typeof original];
            }
            export func steps() {
                var n = 2147483647;
                var before = n++;
                var after = ++n;
                return [before, after, n, typeof n];
            }
            export func precision() {
                var positive = 9007199254740991;
                var negative = -9007199254740991;
                for (var i = 0; i < 3; i++) { positive++; negative--; }
                var oldPositive = positive--;
                var oldNegative = negative++;
                var rounded = 9007199254740991 + 2;
                return [oldPositive, positive, oldNegative, negative, rounded - 9007199254740991];
            }
            export func product(int32 input) {
                if (input < 1) return [];
                var n = input * input;
                return [n, n + 1, typeof n];
            }
            export func invalidWideIndex() {
                var n = 2147483647 + 2147483647 + 2;
                return (new Int32Array(1))[n];
            }
            export func negativeIndex() {
                var a = [1, 2]; var i = -1;
                a[i] = 7;
                var last = a[1];
                i = -2147483648;
                a[i] = 9;
                i--;
                return [last, i, a.length];
            }
            export func changedIndex() {
                var a = new Int32Array(1); var i = 0;
                a[i] = (i = 2147483647);
                return ++i;
            }
            """, mode);
        using (domain)
        {
            ScriptAssert.Equal(new object?[] { 2147483648d, int.MaxValue, 2147483650d, 1, "number" },
                TestWorkspace.Execute(domain, "widened", arguments: [ScriptDatum.FromNumber(int.MaxValue)]));
            ScriptAssert.Equal(new object?[] { int.MaxValue, 2147483649d, 2147483649d, "number" },
                TestWorkspace.Execute(domain, "steps"));
            ScriptAssert.Equal(new object?[] { 9007199254740992d, 9007199254740991d, -9007199254740992d, -9007199254740991d, 1 },
                TestWorkspace.Execute(domain, "precision"));
            var product = (double)int.MaxValue * int.MaxValue;
            ScriptAssert.Equal(new object?[] { product, product + 1, "number" },
                TestWorkspace.Execute(domain, "product", arguments: [ScriptDatum.FromNumber(int.MaxValue)]));
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "invalidWideIndex"));
            ScriptAssert.Equal(new object?[] { 7, -2147483649d, 2 }, TestWorkspace.Execute(domain, "negativeIndex"));
            ScriptAssert.Equal(2147483648d, TestWorkspace.Execute(domain, "changedIndex"));
        }
    }

    [Theory]
    [InlineData("export func f(Number x) { x = 'abc'; }")]
    [InlineData("export func f(Number x) { var read = () => x; x = 'abc'; }")]
    [InlineData("export func f() Number { return 'abc'; }")]
    [InlineData("func f(Number x) {} export func run() { f('abc'); }")]
    [InlineData("export func f() { return 1 as Object; }")]
    [InlineData("export func f() { return 1.5 as int32; }")]
    [InlineData("export func f() { return (int32)'123'; }")]
    [InlineData("type F(int32 x) Number; export func f(F callback) { return callback(1.5); }")]
    [InlineData("type F(Number x) Number; export func f(F callback) { return callback(); }")]
    [InlineData("type F(Number x) Number; export func f(F callback) { return callback(1, 2); }")]
    [InlineData("type F(Number x) Number; func g(String x) Number { return 1; } func apply(F f) { return f(1); } export func run() { return apply(g); }")]
    public async Task KnownMismatchesAreCompilationErrors(string body)
    {
        using var workspace = new TestWorkspace();
        await Assert.ThrowsAsync<AuroraCompilationException>(() => workspace.CompileModuleAsync("@module(TEST); " + body));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task BoundariesSurviveAliasingClosuresAndNativeCalls(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            type Point { Number x; }
            type Node { Number x; Node next; }
            type F(int32 x) Number;
            type Weak(x);
            export native func nativePoint(Point p) Number { return p.x; }
            func raw(x) { return x; }
            func apply(F f, value) { return f(value); }
            func applyWeak(Weak f, value) { return f(value); }
            func capture(F f, value) { var g = () => f(value); return g(); }
            export func strong(value) { return apply(raw, value); }
            export func weak(value) { return applyWeak(raw, value); }
            export func captured(value) { return capture(raw, value); }
            export func point(raw) { var p = raw as Point; var n = p.x; return n; }
            export func alias(raw) { var p = raw as Point; raw.x = 'abc'; return p.x; }
            export func callback(raw) { var p = raw as Point; var mutate = () => raw.x = 'abc'; mutate(); return p.x; }
            export func native(raw) { return nativePoint(raw); }
            export func recursive(raw) { var p = raw as Node; return p.next.x; }
            export func assign(Number x, raw) { x = raw; return x; }
            export func assignCaptured(Number x, raw) { var f = () => x; x = raw; return f(); }
            export func increment(Point p) { p.x++; return p.x; }
            export func compound(Point p, value) { p.x += value; return p.x; }
            """, mode);
        using (domain)
        {
            foreach (var method in new[] { "strong", "captured" })
            {
                ScriptAssert.Equal(2, TestWorkspace.Execute(domain, method, arguments: [ScriptDatum.FromNumber(2)]));
                Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, method, arguments: [ScriptDatum.FromNumber(1.5)]));
            }
            ScriptAssert.Equal(1.5, TestWorkspace.Execute(domain, "weak", arguments: [ScriptDatum.FromNumber(1.5)]));
            foreach (var method in new[] { "point", "native", "alias", "callback" })
            {
                var obj = new ScriptObject();
                obj.Define("x", ScriptDatum.FromNumber(7));
                if (method is "alias" or "callback")
                    Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, method, arguments: [ScriptDatum.FromObject(obj)]));
                else
                {
                    ScriptAssert.Equal(7, TestWorkspace.Execute(domain, method, arguments: [ScriptDatum.FromObject(obj)]));
                    obj.SetPropertyValue("x", StringValue.Of("abc"));
                    Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, method, arguments: [ScriptDatum.FromObject(obj)]));
                }
            }
            var node = new ScriptObject();
            node.Define("x", ScriptDatum.FromNumber(9));
            node.Define("next", ScriptDatum.FromObject(node));
            ScriptAssert.Equal(9, TestWorkspace.Execute(domain, "recursive", arguments: [ScriptDatum.FromObject(node)]));
            foreach (var method in new[] { "assign", "assignCaptured" })
                Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, method,
                    arguments: [ScriptDatum.FromNumber(1), ScriptDatum.FromString("abc")]));
            var reads = 0;
            var accessor = new ScriptObject();
            accessor.Define("x", ScriptDatum.FromBondingGetter((ScriptObject _, ref ScriptDatum result) =>
                result = ScriptDatum.FromNumber(++reads)));
            ScriptAssert.Equal(2, TestWorkspace.Execute(domain, "nativePoint", arguments: [ScriptDatum.FromObject(accessor)]));
            Assert.Equal(2, reads); // One assertion and one actual getter read.
            reads = 0;
            accessor.Define("x", ScriptDatum.FromBondingGetter((ScriptObject _, ref ScriptDatum result) =>
                result = ++reads == 1 ? ScriptDatum.FromNumber(1) : ScriptDatum.FromString("abc")));
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "point", arguments: [ScriptDatum.FromObject(accessor)]));
            var writable = new ScriptObject();
            writable.Define("x", ScriptDatum.FromNumber(1));
            ScriptAssert.Equal(2, TestWorkspace.Execute(domain, "increment", arguments: [ScriptDatum.FromObject(writable)]));
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "compound",
                arguments: [ScriptDatum.FromObject(writable), ScriptDatum.FromString("abc")]));
            ScriptAssert.Equal(2, TestWorkspace.Execute(domain, "point", arguments: [ScriptDatum.FromObject(writable)]));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ReferenceNullIsPreservedAtBoundaries(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            type Point { Number x; }
            native func text(String x) String { return x; }
            export func textOnly(value) { return text(value); }
            export func run(value) { return [value as String, value as Array, value as Object, value as Point, value as Int32Array, text(value)]; }
            export func add(String x, String y) { return x + y; }
            export func concat(String x) { return 'a' + x; }
            export func number(value) { return value as Number; }
            """, mode);
        using (domain)
        {
            Assert.Equal(ValueKind.Null, TestWorkspace.Execute(domain, "textOnly", arguments: [ScriptDatum.Null]).Kind);
            var values = (ScriptArray)TestWorkspace.Execute(domain, "run", arguments: [ScriptDatum.Null]).Reference;
            for (var i = 0; i < 6; i++) Assert.True(values.GetElement(i).Kind == ValueKind.Null, $"Null result {i}: {values.GetElement(i).Kind}");
            ScriptAssert.Equal(0, TestWorkspace.Execute(domain, "add", arguments: [ScriptDatum.Null, ScriptDatum.Null]));
            ScriptAssert.Equal("anull", TestWorkspace.Execute(domain, "concat", arguments: [ScriptDatum.Null]));
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "number", arguments: [ScriptDatum.Null]));
        }
    }
}
