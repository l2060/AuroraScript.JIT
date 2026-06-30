# AuroraScript Host Integration Guide

Version target: AuroraScript.JIT 3.0.0

This document describes the .NET host-side API used to embed AuroraScript, load scripts from custom stores, expose CLR services, run modules and blocks, and apply runtime patches.

Use `schema/host-api.json` for a machine-readable API index. Use `schema/runtime-api.json` for APIs visible inside AuroraScript code.

## Engine Setup

Create an `AuroraEngine` with immutable `EngineOptions`.

```csharp
using AuroraScript;
using AuroraScript.Core;

var options = EngineOptions.Default
    .WithCompiler(compiler =>
    {
        compiler.Mode = CompilationMode.Dynamic;
        compiler.SourceResolver = ScriptSources.FileSystem("scripts");
    })
    .WithRuntime(runtime => runtime.HotReload = false)
    .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release);

var engine = new AuroraEngine(options);
```

Common compilation modes:

- `CompilationMode.Dynamic`: fastest in-memory emission. Best for services that do not need persisted DLL/PDB output.
- `CompilationMode.OnlyRun`: in-memory execution with collectible assembly behavior.
- `CompilationMode.Persistence`: produces a loadable assembly image and optionally writes `Output.AssemblyFile`.

Common build entry points:

- `await engine.BuildAsync("main.as")`: resolves one entry file and its imports/includes through the configured resolver.
- `await engine.BuildAsync(["main.as", "tools/job.as"])`: builds multiple explicit entries.
- `await engine.BuildAsync()`: enumerates all resolver-visible sources through `GetAllSourcesAsync`.
- `await engine.BuildAsync(new MemorySource(root, path, source))`: builds already materialized source objects.

After a successful build, create a domain and execute exported functions:

```csharp
await engine.BuildAsync("main.as");

using var domain = engine.CreateDomain();
var result = domain.Execute("MAIN", "run");
```

## CompileBlock

`CompileBlock` compiles a function body, not a module. Do not pass `@module`, `@global()`, `import`, `include`, `export`, or `declare` syntax.

```csharp
using var block = engine.CompileBlock(
    "return left + right;",
    ["left", "right"]);

var result = block.Invoke(
    ScriptDatum.FromNumber(20),
    ScriptDatum.FromNumber(22));
```

Use `CompileBlockOptions.SourceName` to improve diagnostics:

```csharp
using var block = engine.CompileBlock(
    "return value * 2;",
    new CompileBlockOptions
    {
        SourceName = "rules/double.as",
        Parameters = ["value"]
    });
```

## Source Resolvers

`IScriptSourceResolver` is the extension point for loading scripts from files, memory, virtual directories, databases, embedded resources, object storage, or tenant-specific stores.

The resolver has three responsibilities:

- `ResolveAsync(importer, requestedPath, context)`: map a raw import/include path to a stable `ScriptSourceReference`.
- `GetSourceAsync(reference)`: return the source text for a resolved reference.
- `GetAllSourcesAsync(query)`: enumerate sources visible to `BuildAsync()`.

The resolver does not decide whether a source is "open" in an editor. It only defines the compiler's source universe.

### Built-In Resolvers

```csharp
var fileSystem = ScriptSources.FileSystem("scripts");

var memory = ScriptSources.Memory("mem://app/")
    .Add("main.as", "@module(MAIN); export func run() { return 42; }");

var composite = ScriptSources.Composite(
    memory,
    fileSystem);
```

In `CompositeScriptSourceResolver`, earlier resolvers win when the resolved import/include target falls under their root. Different protocols or non-overlapping roots remain isolated script namespaces. Use `/` for script paths stored inside resolvers.

### Resolution Rules

The compiler pipeline treats parsing and loading as separate steps:

1. The parser keeps the raw text from `import` and `include`, such as `./lib` or `../shared`.
2. Module graph construction calls `ResolveAsync(importer, requestedPath, context)`.
3. If a resolver returns a `ScriptSourceReference`, the compiler calls `GetSourceAsync(reference)` to load the source.

Resolver contracts:

- `ResolveAsync(null, entryPath, ...)` resolves an explicit build entry from the resolver `Root`.
- `ResolveAsync(importer, requestedPath, ...)` resolves from the directory of `importer.FullPath`, not from a global compiler directory.
- `ScriptSourceReference.BaseDirectory` identifies the resolver root that should later read the source. `CompositeScriptSourceResolver.GetSourceAsync` routes by exact normalized `BaseDirectory`.
- Resolver implementations should normalize roots and source keys when they are constructed or added, use `/` separators internally, and avoid repeated normalization inside hot comparisons.
- `GetAllSourcesAsync` defines what `BuildAsync()` compiles without an explicit entry. Composite enumeration de-duplicates by normalized `ScriptSource.FullPath`, so earlier resolvers hide later sources with the same identity.

Built-in resolver details:

- `MemorySourceResolver` only resolves targets under its root that exist in its in-memory table.
- `FileSystemScriptSourceResolver` enumerates files under its root for `BuildAsync()`. For imports/includes, it follows the importer path and may resolve a file outside its root if the relative path points there and the file exists; the returned reference still has the file-system resolver root as `BaseDirectory`.
- `CompositeScriptSourceResolver` tries resolvers in the order they were added. The first resolver that can resolve the target wins.

## Memory Overlay Over File System

Use this when the main project lives on disk but the host needs generated scripts, unsaved editor buffers, tenant customizations, or test-only dependencies.

```csharp
var scriptRoot = Path.GetFullPath("scripts");

var overlay = ScriptSources.Memory(scriptRoot)
    .Add("generated/config.as", """
        @module(CONFIG);
        export const value = 42;
        """);

var resolver = ScriptSources.Composite(
    overlay,
    ScriptSources.FileSystem(scriptRoot));

var options = EngineOptions.Default
    .WithCompiler(compiler =>
    {
        compiler.SourceResolver = resolver;
        compiler.Mode = CompilationMode.Dynamic;
    });

var engine = new AuroraEngine(options);
await engine.BuildAsync("main.as");
```

If `main.as` imports `./generated/config`, the memory source is used before checking the file system because the resolved target falls under the memory root. A parent memory root can also override a child file-system root; for example `Memory("d:/a/b/c")` can override `FileSystem("d:/a/b/c/d")` when a script imports `../test`. A separate virtual root such as `mem://overlay/` remains isolated from file-system paths.

## Custom Virtual Resolver

The following resolver loads scripts from an in-memory virtual path table. The same pattern works for database rows or remote stores if `GetSourceAsync` fetches by the resolved path.

```csharp
using AuroraScript.Core;
using AuroraScript.Source;
using System.Runtime.CompilerServices;

public sealed class VirtualSourceResolver : IScriptSourceResolver
{
    private readonly Dictionary<string, string> _sources = new(StringComparer.Ordinal);

    public VirtualSourceResolver(string root)
    {
        Root = NormalizeRoot(root);
    }

    public string Root { get; }

    public VirtualSourceResolver Add(string path, string source)
    {
        _sources[ResolveFromDirectory(Root, path)] = source ?? string.Empty;
        return this;
    }

    public ValueTask<ScriptSourceReference?> ResolveAsync(
        ScriptSourceReference? importer,
        string requestedPath,
        ScriptResolveContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentPath = importer?.FullPath ?? Root;
        var currentDirectory = importer == null ? Root : GetDirectory(currentPath);
        var fullPath = EnsureExtension(
            ResolveFromDirectory(currentDirectory, requestedPath),
            context.Extension);

        return IsUnderRoot(Root, fullPath) && _sources.ContainsKey(fullPath)
            ? new ValueTask<ScriptSourceReference?>(
                new ScriptSourceReference(Root, fullPath))
            : new ValueTask<ScriptSourceReference?>((ScriptSourceReference?)null);
    }

    public ValueTask<ScriptSource> GetSourceAsync(
        ScriptSourceReference reference,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!RootsEqual(reference.BaseDirectory, Root))
        {
            throw new FileNotFoundException("Virtual script source not found.", reference.FullPath);
        }

        if (!_sources.TryGetValue(reference.FullPath, out var text))
        {
            throw new FileNotFoundException("Virtual script source not found.", reference.FullPath);
        }

        return new ValueTask<ScriptSource>(
            new MemorySource(reference.BaseDirectory, reference.FullPath, text));
    }

    public async IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
        ScriptSourceQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var pair in _sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            if (pair.Key.EndsWith(query.Extension, StringComparison.Ordinal))
            {
                yield return new MemorySource(Root, pair.Key, pair.Value);
            }
        }
    }

    private static string NormalizeRoot(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("A virtual root is required.", nameof(root));
        }

        var trimmed = TrimRoot(root);
        return trimmed.EndsWith("://", StringComparison.Ordinal) ? trimmed : trimmed + "/";
    }

    private static bool IsUnderRoot(string root, string path)
    {
        root = TrimRoot(root);
        path = path.Replace('\\', '/');
        if (root.EndsWith("://", StringComparison.Ordinal))
        {
            return path.StartsWith(root, StringComparison.Ordinal);
        }

        return path.Equals(root, StringComparison.Ordinal) ||
            path.StartsWith(root + "/", StringComparison.Ordinal);
    }

    private static bool RootsEqual(string left, string right)
    {
        return string.Equals(TrimRoot(left), TrimRoot(right), StringComparison.Ordinal);
    }

    private static string TrimRoot(string root)
    {
        root = root.Replace('\\', '/');
        var minLength = 0;
        var schemeSeparator = root.IndexOf("://", StringComparison.Ordinal);
        if (schemeSeparator >= 0)
        {
            minLength = schemeSeparator + 3;
        }

        while (root.Length > minLength && root[root.Length - 1] == '/')
        {
            root = root.Substring(0, root.Length - 1);
        }

        return root;
    }

    private static string ResolveFromDirectory(string directory, string path)
    {
        path = path.Replace('\\', '/');
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        return new Uri(new Uri(directory, UriKind.Absolute), path).ToString();
    }

    private static string GetDirectory(string path)
    {
        path = path.Replace('\\', '/');
        var slash = path.LastIndexOf('/');
        return slash < 0 ? path : path.Substring(0, slash + 1);
    }

    private static string EnsureExtension(string path, string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return path;
        }

        if (extension[0] != '.')
        {
            extension = "." + extension;
        }

        var slash = path.LastIndexOf('/');
        var dot = path.LastIndexOf('.');
        return dot > slash ? path : path + extension;
    }
}
```

Usage:

```csharp
var resolver = new VirtualSourceResolver("vfs://tenant-a/app/")
    .Add("main.as", """
        @module(MAIN);
        import lib from './lib/math';
        export func run() { return lib.add(40, 2); }
        """)
    .Add("lib/math.as", """
        @module(MATH);
        export func add(left, right) { return left + right; }
        """);

var engine = new AuroraEngine(
    EngineOptions.Default.WithCompiler(compiler =>
    {
        compiler.SourceResolver = resolver;
        compiler.Mode = CompilationMode.Dynamic;
    }));

await engine.BuildAsync("main.as");
using var domain = engine.CreateDomain();
var result = domain.Execute("MAIN", "run");
```

## Database Resolver Pattern

For database-backed scripts:

- Use a stable virtual root such as `db://tenant-id/scripts/`.
- Store scripts by normalized full virtual path, or map virtual paths to record ids in `ResolveAsync`.
- Keep `ResolveAsync` cheap. It should identify existence and return a reference.
- Do expensive text loading in `GetSourceAsync`.
- Implement `GetAllSourcesAsync` only for the subset that should compile when `BuildAsync()` is called.
- Pass `CancellationToken` into database calls.

Performance guidance for custom resolvers:

- Normalize `Root` once in the constructor.
- Normalize full source keys when sources are added or indexed.
- Store lookup keys in the same canonical form returned in `ScriptSourceReference.FullPath`.
- Use direct dictionary lookups for `ResolveAsync`; avoid scanning all sources for each import.
- Compare already-normalized roots and paths in `ResolveAsync` and `GetSourceAsync`.

## CLR Interop

Expose a CLR type with `RegisterType`:

```csharp
engine.RegisterType<MyService>("Service", TypeAccess.All);
```

Expose a host object or delegate to one domain:

```csharp
using var domain = engine.CreateDomain(global =>
{
    global.Define("tenantId", "tenant-a", writeable: false);
    global.Define("hostLog", (Action<string>)(message => Console.WriteLine(message)), writeable: false);
});
```

Host-provided global values do not require script declarations to work at runtime. When modules or AI tools need a compile-time contract for editor assistance and static diagnostics, declare those values in a resolver-visible `@global()` file:

```as
@global();

declare const tenantId;
declare func hostLog(message);
```

For mutable host-provided values, the optional contract uses `declare var NAME;` in the same kind of file:

```as
@global();

declare var ONLINE_TOTAL;
```

`declare` is compile-time only and is only valid in `@global()` files. These files cannot be imported or included, are not compiled as modules, and are loaded by scanning resolver-visible project `.as` files before module analysis. If no `@global()` file exists, the host globals still work at runtime; the project simply lacks those optional compile-time symbols. Do not write `export declare`; it is invalid. Reads and writes go to the domain `global` unless a local variable shadows the name. Do not model host globals as `export const NAME;` or `export var NAME;`; those forms create module properties and can hide the host-defined value.

`ClrMarshaller` converts common values:

- CLR to script: `null`, numbers, `bool`, `string`, `DateTime`, `DateTimeOffset`, `Enum`, `Delegate`, `IDictionary`, `IEnumerable`, `ScriptObject`, `ScriptDatum`, registered CLR objects.
- Script to CLR: strings, numbers, booleans, arrays, `ScriptObject`, CLR instance wrappers.

Use `ScriptDatum` when you need exact runtime values and minimum conversion overhead.

## Domains And State

An engine owns compiled code and shared prototypes. A `ScriptDomain` owns one isolated global/module registry.

```csharp
using var first = engine.CreateDomain(userState: ScriptObject.Null);
using var second = engine.CreateDomain(userState: ScriptObject.Null);
```

Use separate domains for isolated executions over the same compiled assembly.

## Hot Patch

Enable hot reload in runtime options, then patch a loaded domain:

```csharp
var mainPath = Path.GetFullPath(Path.Combine(scriptRoot, "main.as"));

await domain.ReplacePatchAsync(
    mainPath,
    "@module(MAIN); export func run() { return 43; }");
```

Host-side string overloads require an absolute file path or virtual full path. The path must fall under the current source resolver. In a composite resolver, the longest matching root is used. This is intentional: patching a non-existent script with a relative path would otherwise assign the patch to an arbitrary resolver namespace. Script-side `HotPatch.replace` / `HotPatch.incremental` may omit the path to patch the current module, or use a relative path resolved from the current module full path.

```csharp
await domain.ReplacePatchAsync(
    Path.GetFullPath(Path.Combine(scriptRoot, "main.as")),
    """
    @module(MAIN);
    import util from "./util";
    export func run() { return util.value; }
    """);
```

Patch types:

- `Replace`: replaces matching module members.
- `Incremental`: adds new members and updates matching members.
- `IgnoreDepends`: skips dependencies that are already loaded.

## MCP Workflow

For AI-assisted development:

1. Read `host-integration` for .NET host usage.
2. Read `host-api` for a structured API index.
3. Use `aurora_search_runtime_api` or `aurora_get_runtime_api` before using runtime APIs that look like JavaScript built-ins.
4. Use `aurora_check_script` to validate generated in-memory script text.
5. Use `aurora_run_script` to execute a small module or block and inspect `stdout`, `stderr`, and `result`.
6. Use `aurora_check_file` or `aurora_run_file` when the script exists on disk and imports/includes should be resolved by the file-system resolver.
7. Use `aurora_build_workspace` when the resolver should compile every visible `.as` file under a root.
