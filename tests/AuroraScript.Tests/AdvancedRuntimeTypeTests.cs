using AuroraScript.Tests.Infrastructure;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class AdvancedRuntimeTypeTests
{
    [Fact]
    public async Task ArrayStringNumberAndBooleanConstructorsExposeExpectedStaticOperations()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var generated = Array.of(1, 2, 3);
                var mapped = Array.from('abc', (value) => value.toUpperCase());
                return [Array.isArray(generated), generated.length, mapped.join(''), String.fromCharCode(65), String.valueOf(42), Number.parseInt('12.9'), Number.parseFloat('2.5'), Number.isInteger(2), Number.isNaN(Number.NaN), Number.isInfinity(Number.POSITIVE_INFINITY), new Boolean(1).toString()];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { true, 3, "ABC", "A", "42", 12, 2.5, true, true, true, "true" },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ObjectOperationsCoverKeysAssignCloneEqualityAndFreeze()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var original = { a: 1, nested: { value: 2 } };
                var assigned = Object.assign({}, original, { b: 3 });
                var clone = Object.clone(original);
                var deep = Object.deepClone(original);
                var keys = Object.keys(assigned);
                keys.sort();
                return [keys.join(','), assigned.a, assigned.b, Object.equal$(original, original), Object.equal(original, clone), Object.deepEqual(original, deep)];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { "a,b,nested", 1, 3, true, true, true },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task FrozenObjectsRejectMutation()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var value = { a: 1 };
                Object.freeze(value);
                value.a = 2;
            }
            """);

        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task DateParseAndConstructorExposeStableCalendarComponents()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var date = Date.parse('2024-02-29 12:34:56');
                return [date.year, date.month, date.day, date.hour, date.minute, date.second, date.toString('yyyy-MM-dd HH:mm:ss')];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { 2024, 2, 29, 12, 34, 56, "2024-02-29 12:34:56" },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ProxyInterceptsGetSetAndDelete()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var gets = 0;
                var sets = 0;
                var deletes = 0;
                var target = {};
                var proxy = new Proxy(target, {
                    get: (object, key) => { gets++; return object[key]; },
                    set: (object, key, value) => { sets++; object[key] = value; },
                    unset: (object, key) => { deletes++; delete object[key]; }
                });
                proxy.value = 42;
                var result = proxy.value;
                delete proxy.value;
                return [result, gets, sets, deletes, 'value' in target];
            }
            """);

        ScriptAssert.Equal(new object?[] { 42, 1, 1, 1, false }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ProxyRequiresGetAndSetHandlers()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func run() { return new Proxy({}, {}); }");

        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "run"));
    }
}
