using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class TypedDocumentLiteralTests
{
    [Fact]
    public async Task TDocLiteralBuildsObjectsArraysAndDynamicValues()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run(user) {
                var value = tdoc Object {
                    readonly String id $(user.id),
                    name "Aurora",
                    dynamic $(user.role),
                    tags ["a", $(user.role), 3],
                };
                return [value.id, value.name, value.dynamic, value.tags[1], value.tags.length];
            }
            """);

        var user = new ScriptObject();
        user.Define("id", ScriptDatum.FromString("u-1"));
        user.Define("role", ScriptDatum.FromString("admin"));
        ScriptAssert.Equal(
            new object?[] { "u-1", "Aurora", "admin", "admin", 3 },
            TestWorkspace.Execute(domain, "run", "TEST", ScriptDatum.FromObject(user)));
    }

    [Fact]
    public async Task TDocLiteralBuildsPackedArraysAndReadonlyProperties()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var value = tdoc Object {
                    readonly id "u-1",
                    Int8Array bytes [-2, 0, 3],
                    BooleanArray flags [true, false],
                };
                var failed = false;
                try { value.id = "changed"; } catch (e) { failed = true; }
                return [value.id, value.bytes[0], value.bytes[2], value.flags[0], failed];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { "u-1", -2, 3, true, true },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task TDocLiteralBuildsBuiltinTypedObjects()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var value = tdoc Object {
                    StringBuffer text "hello",
                    Path file "a/b.as",
                    Regex pattern { pattern "a+", flags "i" },
                    HashMap values [["x", 42]],
                };
                return [value.text.toString(), value.file.toString(), value.pattern.test("AAA"), value.values.get("x")];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { "hello", "a/b.as", true, 42 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task TDocLiteralWorksInModuleInitializers()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            const value = tdoc Object {
                readonly id "module",
                StringBuffer text "ok",
                Int32Array values [1, 2, 3],
            };
            export func run() { return [value.id, value.text.toString(), value.values[2]]; }
            """);

        ScriptAssert.Equal(
            new object?[] { "module", "ok", 3 },
            TestWorkspace.Execute(domain, "run"));
    }
}
