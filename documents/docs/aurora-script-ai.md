# AuroraScript AI Reference

Version target: AuroraScript.JIT 4.0.0

This file is the primary AI reference for AuroraScript. Prefer these rules over JavaScript assumptions.
For default code generation style, also read `docs/script-authoring-best-practices.md`.

## Authoring Defaults For AI

- Generate a full module unless the user explicitly asks for a `CompileBlock` body.
- Use `@module(NAME);` only for modules that need explicit-name lookup through host APIs or script `global.getModule`. Other modules are anonymous and are imported by path.
- For `CompileBlock`, do not use `@module`, `@global()`, `include`, `import`, `export`, or `declare`.
- Prefer `const` for values that are never reassigned and `var` for values that change.
- Return plain data that hosts can serialize: number, string, boolean, null, arrays, and objects.
- Check `schema/runtime-api.json` before using runtime APIs that look like JavaScript built-ins.
- Use `aurora_search_runtime_api` or `aurora_get_runtime_api` before generating calls to runtime APIs.
- Validate generated script with `aurora_check_script`; use `aurora_run_script` when behavior can be verified.
- Use `aurora_check_file` or `aurora_run_file` for real `.as` files on disk with import/include dependencies.

## File And Module Model

- Script files normally use `.as`.
- A module may start with `@module(NAME);`. It must be the first effective statement when present.
- Omitting `@module` creates an anonymous module. Never derive a default module name from its filename, relative path, or absolute path.
- `@module(NAME)` supplies the explicit name used by host APIs such as `ScriptDomain.Execute`, `GetMethod`, and `GetModule`, and by script `global.getModule`. It is not the identity used by `import` or `include`.
- Only explicit module names must be unique. Anonymous files with the same filename in different directories can coexist because resolved source identity is based on normalized `ScriptSourceReference.FullPath`.
- A global declaration file starts with `@global();`. Before it only comments and blank lines are allowed.
- `@global()` files contain only `declare` statements, cannot also use `@module`, cannot be imported/included, and are not compiled as modules.
- `include "path";` and `import Alias from "path";` must appear at the top of a module before ordinary declarations.
- `export` is only valid at module scope.
- `CompileBlock` accepts statement bodies only. It rejects file/module-only syntax such as `@module`, `@global()`, `import`, `include`, `export`, and `declare`.
- Import/include paths are raw script text until the module graph asks the configured resolver to resolve them.
- Relative imports are resolved from the importing file's full path, not from a global compiler directory.
- After resolution, compilation, dependency ordering, module initialization, and runtime import binding use the resolved absolute or virtual `FullPath`. `ModulePath` remains resolver-relative source information, not a module name or registry identity.
- The runtime `global.modules` registry has one module-object entry per resolved `FullPath`; it does not duplicate entries under explicit names or resolver-relative paths. Use `global.getModule(name)` only for dynamic lookup of an already loaded, explicitly named module; ordinary dependencies should still use `import`.
- Entry files are resolved from the resolver root. Do not assume the old `compiler.Directory` or `BaseDirectory` input model.
- Use `/` in generated script paths and tool overlay paths, even on Windows.
- Use `Path.baseModule(...segments)` or `Path.currentDirectory()` when generated script code needs paths relative to the current module.
- In MCP `sources` overlays, keys are paths relative to the tool root/source root. They override disk or later resolver sources only when the resolved target path falls under the overlay root.
- A parent memory overlay can override a child file-system dependency. For example, if memory is rooted at `d:/a/b/c` and disk is rooted at `d:/a/b/c/d`, a disk script importing `../test` can resolve to memory source `d:/a/b/c/test.as`.
- Different protocols or non-overlapping roots are isolated script namespaces, such as `mem://overlay/` versus `d:/project/scripts/`.
- Host-side `DynamicPatch` / `ReplacePatch` / `IncrementalPatch` string overloads require an absolute file path or virtual full path under the current resolver root.
- Script-side `HotPatch.replace` and `HotPatch.incremental` should pass only `script` when patching the current module. If a module path is supplied, relative paths resolve from the current module full path.
- Hot patch targets are matched by resolved full path, never by `@module` name. A different path cannot target a loaded module by reusing its name, and an explicit patch name cannot rename a module already loaded at that path. An anonymous patch at the same path preserves the loaded module's explicit name. A new path creates a new named or anonymous module, subject to explicit-name conflict checks.
- Native modules are opt-in host capabilities. `EngineOptions.Default.BuiltIns` is empty; the host must add `BuiltInModules.FileSystem` and/or `BuiltInModules.HttpClient` before constructing the engine.
- Import enabled native modules by bare path (`import fs from "fs";`, `import http from "http";`). Bare native paths take priority over the project resolver, while `./fs`, `../fs`, and other relative imports remain project sources.

## Statements

- Empty statement: `;`
- Block: `{ statement* }`
- Function: `func name(args) { ... }` or `function name(args) { ... }`
- Execution context: `context name;` or `context name as NativeType;` at module scope. Each name aliases `ScriptContext.UserState`. Typed names require a public `[AuroraNativeType]` listed in `WithNativeTypes`. Do not generate `$state`.
- External declaration in an `@global()` file: `declare func name(args);`, `declare var name;`, `declare const name;`, `declare type Name { ... }`
- Variable: `var name;`, `var name = expr;`, `const name = expr;`
- Destructuring: `var { a, b } = obj;`, `var [ first, ...rest ] = array;`
- Enum: `enum Name { A, B = 3, C }`
- Control flow: `if`, `else`, `while`, `for`, `for-in`, `break`, `continue`, `return`, `throw`, `try`, `catch`, `finally`, `delete`, `debugger`

Variable declarations are single-binding declarations. `var a = 1, b = 2;` is not the current form.

External `declare` declarations are optional compile-time declarations for host-defined globals. They improve semantic coloring, go-to-definition, and static diagnostics, but host globals still work at runtime without them. They do not create module properties, do not assign `null`, and do not emit runtime initialization code. Put them in one or more `@global()` files when a .NET host defines values or services on `global` and the project wants that compile-time contract:

```as
@global();

declare const APP_VERSION;
declare var ONLINE_TOTAL;
declare func HOST_LOG(message);

declare type Stats {
    static func mean(Number a, Number b) Number;
}

declare type Vec2 {
    constructor(Number x, Number y);
    Number x;
    func length() Number;
}
```

Rules:

- Plain `declare` is only valid inside `@global()` files. `export declare` is invalid.
- The compiler scans resolver-visible project `.as` files and loads `@global()` files before module analysis when they exist.
- Duplicate global declarations across `@global()` files are rejected. Function overloads are not allowed.
- `declare var/const` must declare one simple name and must not have an initializer or destructuring pattern.
- `declare const` participates in compile-time const assignment checks, but reads still resolve from host-defined `global`.
- `declare var` reads and writes resolve through `global` unless shadowed by a local variable.
- Do not use `export const HOST_VALUE;` for host-provided values; that emits a module property initialized to `null` and can hide the host global.
- `declare type` is the script contract for an `[AuroraNativeType]`. Static-only
  types omit `constructor` and mark members `static`; constructible types include
  one constructor. Application native types must be selected explicitly with
  `EngineOptions.WithCompiler(compiler => compiler.WithNativeTypes(...))`; that
  selection applies to every domain of the engine. Contracts are editor-only
  and must not be treated as structural `type` declarations or as compiler
  inference input. Generating `.as` from host assemblies is deferred.

## Functions

```as
func add(a, b = 1) {
    return a + b;
}

func collect(first, ...rest) {
    return rest.length;
}
```

Rules:

- Parameter names must be unique.
- A rest parameter must be last.
- A rest parameter cannot have a default value.
- Function declarations inside a block are local to that block.
- Lambdas use `=>` and may have expression or block bodies.
- `native func` requires a return contract. Use `void` for a procedure; it is not an alias for `Null`.
- A NativeType return on `native func` is the CLR type on `$native`. Proven callees keep that instance; only `$typed` converts to `ScriptDatum`.

```as
var inc = x => x + 1;
var add = (a, b = 1) => a + b;
var run = () => { return add(1, 2); };
```

## Scope And Declaration Rules

AuroraScript uses module, function, and block scopes.

- Same scope cannot declare duplicate names.
- Function parameters live in the function root scope.
- A child block may shadow an outer `var`.
- A child block may not redeclare a visible outer `const`.
- `const` cannot be assigned after declaration.
- `const` cannot be mutated by `=`, compound assignment, `++`, or `--`.

Valid:

```as
var a = 123;
{
    var a = 123456;
    console.log(a);
}
```

Invalid:

```as
const a = 123;
{
    var a = 123456;
}
```

Invalid:

```as
const a = 123;
a = { b: 1234 };
```

## Expressions

Literals:

- number: `1`, `1.5`, `10000D` (force `Number`), `1L` (force `Int64`), `0xD76AA478u` (force `UInt32`), `100000` (inferred `Int32` when it fits), hexadecimal `0xFFFF` (integer by default)
- integer constraint: lowercase `int32` is allowed on parameters, returns,
  shape fields, and `value as int32`. It requires an exact signed 32-bit
  integer at checked boundaries but keeps script `Number` identity
  (`typeof` is `"number"`). A local whose every assignment is an integer keeps
  32-bit storage and wraps like CLR `int` instead of widening to `Number`, so
  it cannot hold negative zero or `NaN`: `-14 % 7` is `0` and `x % 0` raises.
  Expressions from those locals wrap too (`currentX - 1`). `/` stays `Number`;
  an exact integer quotient uses `((a - b) / c) as int32` (parentheses required).
- unsigned integer constraint: lowercase `uint32` is allowed on parameters,
  returns, shape fields, and `value as uint32`. Checked boundaries require an
  exact value in `0..4294967295` and reject negative zero. Suffix `U`/`u`
  selects `UInt32` literal storage without changing unsuffixed inference.
  Arithmetic wraps modulo 2^32, bitwise results stay unsigned, `>>` is logical,
  and runtime identity remains Number (`typeof` is `"number"`).
- string: `'text'`, `"text"`
- template string: `` `value=${expr}` ``
- regex literal
- boolean: `true`, `false`
- null: `null`

Collections:

```as
var array = [1, 2, null];
var object = { name: "Aurora", count: 1 };
var shorthand = { object };
var merged = { ...object, count: 2 };
```

Access and calls:

```as
object.name;
object["name"];
array[0];
fn(1, 2);
fn(...array);
new StringBuffer("");
```

Operators, high to low:

1. grouping, array/object literal, member access, index, function call
2. `new`
3. postfix `++`, `--`
4. prefix `...`, `++`, `--`, `!`, `~`, unary `-`
5. `typeof`, `in`, relational `< <= > >=`
6. `* / %`
7. `+ -`
8. `<< >> >>>`
9. `== !=`
10. `&`, `^`, `|`
11. `&&`, `||`
12. `= += -= *= /= %=`, `=>`

Assignments are right-associative.

`typeof` results:

- lowercase for primitives and privileged kinds: `"null"`, `"boolean"`, `"number"`, `"string"`, `"object"`, `"array"`, `"date"`, `"regex"`, `"function"`, `"type"`, `"error"`, `"clr:function"`, `"clr:bonding"`
- `"type"` for infrastructure NativeTypes (`Math`, `JSON`, `TDoc`, `console`, `Conv8`, `HotPatch`) and host types registered with `WithNativeTypes`
- constructor names for native objects stored as `ValueKind.Object`: `"Int8Array"`, `"UInt8Array"`, `"Int16Array"`, `"UInt16Array"`, `"Int32Array"`, `"UInt32Array"`, `"Int64Array"`, `"UInt64Array"`, `"Float32Array"`, `"Float64Array"`, `"BooleanArray"`, `"StringBuffer"`, `"HashMap"`, `"Path"`
- Do not assume JavaScript `typeof new Int8Array() === "object"`. Use `typeof bytes == "Int8Array"` or `check Int8Array bytes`.
- Do not add a new `ValueKind` member for a native type; identity lives on the object (`TypeOfValue`).

## Template Strings

```as
return `[ ${0 + 10} - ${1 + 10} ]`;
```

Semantics:

- Interpolation uses `${ expression }`.
- Parts evaluate left to right.
- The compiler emits small templates through concat-style code and larger templates through `StringBuilder`-style code.
- Module-level const template strings may be inlined when module const inlining is enabled and every part is compile-time evaluable.

## Runtime Globals

Constructors and globals:

- `Array`, `String`, `Boolean`, `Object`, `Number`, `Date`
- `Error`, `HashMap`, `Regex`, `Proxy`, `StringBuffer`, `Path`
- Packed arrays: `Int8Array`, `UInt8Array`, `Int16Array`, `UInt16Array`, `Int32Array`, `UInt32Array`, `Int64Array`, `UInt64Array`, `Float32Array`, `Float64Array`, `BooleanArray`
- Infrastructure Types (`typeof` is `"type"`; `new` fails): `console`, `JSON`, `TDoc`, `Math`, `Conv8`, `HotPatch`

Common APIs:

- `console.log(...values)`, `console.error(...values)`, `console.time(label)`, `console.timeEnd(label)`
- `JSON.parse(text)`, `JSON.stringify(value, indented = false)`
- `JSON.stringify` enumerates NativeType instances normally: exported enumerable Native fields and enumerable dynamic properties are included; Native methods are not.
- `TDoc.parse(text)`, `TDoc.stringify(value, indented = false, emitTypes = false)`；`emitTypes = true` 强制输出所有可用类型名
- Native TDoc literals use `tdoc [TypeName] value`, for example `const value = tdoc Object { readonly String id $(user.id), enabled true, };`. Host NativeTypes that implement `INativeTypedDocument` can be named directly (`tdoc Vec2 { x 3, y 4 }`, `tdoc Vec2 [3, 4]`, `tdoc Flag false`, `tdoc User "a,b"`). `WriteTypedDocument` chooses the canonical object, array, or scalar body. Only value positions may use `$(expression)`; property names and type names are static. Standalone `.tdoc` documents omit the `tdoc` prefix and do not allow interpolation.
- `Math.PI`, `Math.E`, `Math.Tau`, `Math.abs`, `Math.max`, `Math.min`, `Math.random`, `Math.log`, `Math.pow`, `Math.exp`, `Math.cos`, `Math.sin`, `Math.tan`, `Math.acos`, `Math.asin`, `Math.atan`, `Math.floor`, `Math.round`
- `Conv8` on `UInt8Array` only: `BYTES1`/`BYTES2`/`BYTES4`/`BYTES8`; `get`/`set` for bool, int8–64, uint8–64, float32/64 (`littleEndian` default `true`); `getString(buffer, offset, byteLength)` / `setString(buffer, offset, value)` as UTF-8. `int64`/`uint64` round-trip through `Number`.
- Array: `length`, `push`, `pop`, `sort`, `join`, `slice`, `reverse`, `unshift`, `shift`, `concat`, `find`, `findIndex`, `findLast`, `findLastIndex`, `map`, `filter`, `some`, `every`, `flat`, `reduce`, `indexOf`, `lastIndexOf`, `has`
- String: `length`, `contains`, `indexOf`, `lastIndexOf`, `startsWith`, `endsWith`, `substring`, `split`, `match`, `matchAll`, `replace`, `padLeft`, `padRight`, `trim`, `trimLeft`, `trimRight`, `slice`, `toString`, `charCodeAt`, `toLowerCase`, `toUpperCase`
- StringBuffer: `append`, `insert`, `appendLine`, `clear`, `release`, `stringAndRelease`, `toString`
- Path constructor/static: `new Path(root, ...segments)`, `Path.of(root, ...segments)`, `Path.isPath(value)`, `Path.join(root, ...segments)`, `Path.baseModule(...segments)`, `Path.normalize(path)`, `Path.directoryName(path)`, `Path.fileName(path)`, `Path.extName(path)`, `Path.protocol(path)`, `Path.changeExt(path, extension)`, `Path.isRooted(path)`, `Path.isUnderRoot(root, path)`, `Path.currentFile()`, `Path.currentDirectory()`
- Path instance: `append(...segments)`, `reset(root, ...segments)`, `changeExt(extension)`, `directoryName()`, `fileName()`, `extName()`, `protocol()`, `clone()`, `toString()`

Constructor signatures are also available in `schema/runtime-api.json` under each constructor global's `constructors` array:

- `new Array(capacity?: number): array`
- `new String(value?: any): string`
- `new Boolean(value?: any): boolean`
- `new Object(prototype?: object): object`
- `new Number(value?: any): number`
- `new Date(value: number|string): date`; use `Date.now()` or `Date.utcNow()` for the current time.
- `new Error(message: string): Error`
- `new HashMap(capacity?: number): HashMap`
- `new Regex(pattern: string|Regex, flags?: string): Regex`
- `new Proxy(target: object, options: object): Proxy`; `options` must provide `get` and `set`.
- `new StringBuffer(initialValue?: string): StringBuffer`
- `new Path(root?: string|Path, ...segments: string|Path): Path`

Path rules:

- `Path` normalizes text with `/` separators and supports protocol roots such as `mem://app` and `asset://pkg`.
- `Path.join`, `Path.baseModule`, `Path.currentFile`, and `Path.currentDirectory` return strings.
- `Path.extName(path)` and `path.extName()` return the extension including the leading dot, or an empty string when absent.
- `new Path(...)` and `Path.of(...)` return mutable `Path` objects; `append`, `reset`, and `changeExt` mutate and return the same `Path`.
- `Path` objects support `==` by normalized path text value.

## Opt-In Native Modules

These modules are not runtime globals. Generated code may use them only when the host contract explicitly enables the corresponding `BuiltInModules` definition.

File-system module:

```as
import fs from "fs";
```

- Every path argument accepts `string|Path`.
- Reads: `readText(path): string`, `readBytes(path): UInt8Array`.
- Writes: `writeText(path, text)`, `writeBytes(path, bytes)`, `appendText(path, text)`, `appendBytes(path, bytes)` return `boolean`.
- Inspection: `exist(path)`, `isFile(path)`, `isDir(path)` return `boolean`; `size(path)` returns file bytes and rejects directories.
- Directories: `mkDir(path): boolean`; `dir(path): string[]` returns ordinal-sorted top-level names.
- Changes: `copy(source, destination, overwrite?)`, `move(source, destination, overwrite?)`, and `delete(path, recursive?)` return `boolean`. Overwrite and recursive behavior must be enabled explicitly.
- File-system failures become `AuroraRuntimeException`; recursive directory copy does not follow directory links.

HTTP client module:

```as
import http from "http";
```

- Synchronous: `request(method, url, options?)`, `get/delete/head(url, options?)`, and `post/put/patch(url, body?, options?)` return a buffered response.
- Callback versions use the `Async` suffix and require the callback as the final argument: `requestAsync`, `getAsync`, `postAsync`, `putAsync`, `patchAsync`, `deleteAsync`, and `headAsync`.
- Async scheduling returns `true`. Success calls `callback(null, response)`; transport, timeout, and response-reading failures call `callback(error, null)`.
- Options: `headers` is an object of string or string-array values; `body` is `string|UInt8Array`; `contentType` is a non-empty string; `timeout` is a positive integer in milliseconds.
- Response fields: `status`, `statusText`, `ok`, final `url`, lowercase-key `headers`, decoded `body` and `text`, and raw `bytes: UInt8Array`.
- HTTP 404/500 remain normal responses with `ok = false`. Redirects and common decompression are automatic; cookies are not retained.

## Performance Rules

- Prefer `const` for compile-time constants.
- When module const inlining is enabled, primitive module constants and enum members
  can be folded through import aliases without runtime module/property reads.
- Use normal template strings for small or medium formatting; the compiler already selects concat or builder paths.
- Use `StringBuffer` for long loops or many incremental appends.
- Use `typeof value == "Int8Array"` (or the matching constructor name) to distinguish packed arrays; they are not `"object"`.
- Packed-array parameters accept `null`; a literal `null` is inferred from the
  declared parameter type and does not require `as Float64Array`. Non-null
  values still require the exact packed-array type.
- Use `native func` only when a stable native ABI and direct-call behavior are required. Its defaults must be trailing compiler-foldable primitive constants; when a parameter has an explicit type, the default type must match it exactly.
- Use lowercase `int32` for indices, lengths, counters, and IDs that must stay
  in signed 32-bit range. It is a checked compile/ABI constraint, not a
  constructor or runtime type; do not spell it `Int32`. Integer locals wrap;
  script `/` is not integer division — write `((a - b) / c) as int32` for an
  exact quotient, not `Math.floor`.
- Use lowercase `uint32`, `UInt32Array`, and `U`/`u` constants for unsigned
  32-bit hash/checksum words. It is a checked CLR `uint` ABI constraint, not a
  constructor; do not spell the scalar type `UInt32`.
- Use `native func work(...) void { ... }` for a procedure. Falling through
  and `return;` are valid; returning an expression is invalid. Direct native
  statement calls use a CLR `void` ABI, while dynamic calls evaluate to `null`.
- `export type` declares compile-time shapes for native field derivation. Shapes may nest or form cycles (for example `Node { Number value; Node next; }`). They are not runtime contracts; missing or mismatched fields are not rejected as shape errors.
- Avoid `console.log` in hot paths.
- Avoid unnecessary closure captures in loops.
- Cache repeated dynamic property lookups in local variables when the same property is used many times.
- Cache loop bounds such as `items.length` in local variables before index loops; do not put dynamic property reads directly in loop conditions.
- Use `CompilationMode.Dynamic` for fastest in-memory code, `OnlyRun` for collectible in-memory assemblies, and `Persistence` when a DLL/PDB output is required.

## Diagnostics Pattern

Compilation errors are reported as `AuroraCompilationException`.
Each diagnostic has:

- `message`
- `fileName`
- `lineNumber`
- `columnNumber`

Typical diagnostics:

- `Duplicate declaration 'name' ...`
- `Cannot assign to constant 'name'`
- `Import file not found: path`
- `The Import statement must be placed at the top of the module.`
- `continue statement must be inside a loop.`
- `break statement must be inside a loop.`

## Recommended AI Workflow

1. Read this file.
2. If generating script code, read `docs/script-authoring-best-practices.md`.
3. If generating host-side C# integration code, read `docs/host-integration.md` and `schema/host-api.json`. For typed script globals implemented in C#, use `[AuroraNativeType]` / `[AuroraExport]` rather than `BondingFunction` unless you need a raw `ScriptDatum` span callback.
4. Check examples in `examples/valid` for accepted syntax.
5. If rejecting code, compare with `examples/invalid`.
6. Use `aurora_search_runtime_api` or `aurora_get_runtime_api` before using runtime APIs that may be confused with JavaScript built-ins.
7. Use `aurora_validate_best_practices` to catch AI authoring mistakes such as dynamic loop bounds.
8. Use `aurora_check_script` for generated in-memory source.
9. Use `aurora_run_script` when a small runnable in-memory example can verify behavior.
10. Use `aurora_check_file` or `aurora_run_file` when checking an existing `.as` file and its resolver-loaded dependencies.

