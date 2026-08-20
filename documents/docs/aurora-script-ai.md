# AuroraScript AI Reference

Version target: AuroraScript.JIT 4.0.0

This file is the primary AI reference for AuroraScript. Prefer these rules over JavaScript assumptions.
For default code generation style, also read `docs/script-authoring-best-practices.md`.

## Authoring Defaults For AI

- Generate a full module unless the user explicitly asks for a `CompileBlock` body.
- For modules, start with `@module(NAME);`, then top-level `include`/`import`, then declarations, then exported entry functions.
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
- A global declaration file starts with `@global();`. Before it only comments and blank lines are allowed.
- `@global()` files contain only `declare` statements, cannot also use `@module`, cannot be imported/included, and are not compiled as modules.
- `include "path";` and `import Alias from "path";` must appear at the top of a module before ordinary declarations.
- `export` is only valid at module scope.
- `CompileBlock` accepts statement bodies only. It rejects file/module-only syntax such as `@module`, `@global()`, `import`, `include`, `export`, and `declare`.
- Import/include paths are raw script text until the module graph asks the configured resolver to resolve them.
- Relative imports are resolved from the importing file's full path, not from a global compiler directory.
- Entry files are resolved from the resolver root. Do not assume the old `compiler.Directory` or `BaseDirectory` input model.
- Use `/` in generated script paths and tool overlay paths, even on Windows.
- Use `Path.baseModule(...segments)` or `Path.currentDirectory()` when generated script code needs paths relative to the current module.
- In MCP `sources` overlays, keys are paths relative to the tool root/source root. They override disk or later resolver sources only when the resolved target path falls under the overlay root.
- A parent memory overlay can override a child file-system dependency. For example, if memory is rooted at `d:/a/b/c` and disk is rooted at `d:/a/b/c/d`, a disk script importing `../test` can resolve to memory source `d:/a/b/c/test.as`.
- Different protocols or non-overlapping roots are isolated script namespaces, such as `mem://overlay/` versus `d:/project/scripts/`.
- Host-side `DynamicPatch` / `ReplacePatch` / `IncrementalPatch` string overloads require an absolute file path or virtual full path under the current resolver root.
- Script-side `HotPatch.replace` and `HotPatch.incremental` should pass only `script` when patching the current module. If a module path is supplied, relative paths resolve from the current module full path.

## Statements

- Empty statement: `;`
- Block: `{ statement* }`
- Function: `func name(args) { ... }` or `function name(args) { ... }`
- External declaration in an `@global()` file: `declare func name(args);`, `declare var name;`, `declare const name;`
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
```

Rules:

- Plain `declare` is only valid inside `@global()` files. `export declare` is invalid.
- The compiler scans resolver-visible project `.as` files and loads `@global()` files before module analysis when they exist.
- Duplicate global declarations across `@global()` files are rejected. Function overloads are not allowed.
- `declare var/const` must declare one simple name and must not have an initializer or destructuring pattern.
- `declare const` participates in compile-time const assignment checks, but reads still resolve from host-defined `global`.
- `declare var` reads and writes resolve through `global` unless shadowed by a local variable.
- Do not use `export const HOST_VALUE;` for host-provided values; that emits a module property initialized to `null` and can hide the host global.

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

- number: `1`, `1.5`, hexadecimal numeric literals are supported by the lexer
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
- `console`, `JSON`, `TDoc`, `Math`, `HotPatch`

Common APIs:

- `console.log(...values)`, `console.error(...values)`, `console.time(label)`, `console.timeEnd(label)`
- `JSON.parse(text)`, `JSON.stringify(value, indented = false)`
- `TDoc.parse(text)`, `TDoc.stringify(value, indented = true, emitTypes = false)`；`emitTypes = true` 强制输出所有可用类型名
- Native TDoc literals use `tdoc [TypeName] value`, for example `const value = tdoc Object { readonly String id $(user.id), enabled true, };`. Only value positions may use `$(expression)`; property names and type names are static. Standalone `.tdoc` documents omit the `tdoc` prefix and do not allow interpolation.
- `Math.PI`, `Math.E`, `Math.Tau`, `Math.abs`, `Math.max`, `Math.min`, `Math.random`, `Math.log`, `Math.pow`, `Math.exp`, `Math.cos`, `Math.sin`, `Math.tan`, `Math.acos`, `Math.asin`, `Math.atan`, `Math.floor`, `Math.round`
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

## Performance Rules

- Prefer `const` for compile-time constants.
- Enable module const inlining for modules with stable exported constants.
- Use normal template strings for small or medium formatting; the compiler already selects concat or builder paths.
- Use `StringBuffer` for long loops or many incremental appends.
- Use `@directCall` on helper functions that should remain directly callable when the compiler cannot infer it.
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
3. If generating host-side C# integration code, read `docs/host-integration.md` and `schema/host-api.json`.
4. Check examples in `examples/valid` for accepted syntax.
5. If rejecting code, compare with `examples/invalid`.
6. Use `aurora_search_runtime_api` or `aurora_get_runtime_api` before using runtime APIs that may be confused with JavaScript built-ins.
7. Use `aurora_validate_best_practices` to catch AI authoring mistakes such as dynamic loop bounds.
8. Use `aurora_check_script` for generated in-memory source.
9. Use `aurora_run_script` when a small runnable in-memory example can verify behavior.
10. Use `aurora_check_file` or `aurora_run_file` when checking an existing `.as` file and its resolver-loaded dependencies.

