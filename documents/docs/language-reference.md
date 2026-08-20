# AuroraScript Language Reference

This document summarizes language features supported by the current parser, binder, compiler, and runtime.

## Lexical Elements

Identifiers may start with ASCII letters, `_`, `$`, or CJK characters in the `0x4e00..0x9fbb` range.
Identifier parts may also contain digits.

Value tokens include strings, template strings, regex literals, numbers, booleans, and `null`.

Line and block comments are accepted by the lexer.

## Module Syntax

```as
@module(TEST);
include "shared";
import UTIL from "util";

export const VALUE = 42;
export var mutable = 0;
export func run() { return VALUE; }
```

Rules:

- `@module(NAME);` is metadata, not a function annotation.
- `@module` must be first when present.
- `include` and `import` must be at the top of the module.
- `export` may only appear at module scope.
- `declare` is not valid in modules. Use a separate `@global()` declaration file for host globals.
- Duplicate module-scope names are rejected.

## Function Annotations

Function annotations use `@name` before a function declaration.

```as
@directCall
func helper(value) { return value + 1; }

@directCall(false)
func dynamicHelper(value) { return value + 1; }
```

Supported annotations:

- `@directCall`
- `@directCall(true)`
- `@directCall(false)`

Unsupported annotations are rejected during backend binding.

## Declarations

```as
var value;
var value2 = 1;
const name = "Aurora";
enum Mode { Read, Write = 4, Append }
```

Destructuring:

```as
var { name, age } = person;
var [ first, ...rest ] = values;
```

Only one simple name is declared by a simple `var` or `const` statement.

External declarations:

```as
@global();

declare func HOST_LOG(message);
declare const APP_VERSION;
declare var ONLINE_TOTAL;
```

`declare` is compile-time only and is only valid inside `@global()` files. A global declaration file must start with `@global();` after only comments or blank lines, cannot also use `@module`, cannot be imported or included, and is not compiled as a module. The compiler scans resolver-visible project `.as` files and loads these optional declarations before module analysis when they exist. Host-provided globals still work at runtime without an `@global()` file; the file exists to improve editor assistance and static diagnostics.

Global declarations create compiler symbols for binding, duplicate checks, and `const` assignment checks, but do not emit module initialization code or create runtime module properties. `declare var/const` must use one simple name and cannot have an initializer or destructuring pattern. Reads and writes of declared external variables resolve through the script domain `global` unless a local variable shadows the name. Duplicate global names across `@global()` files are rejected; functions do not support overloads.

## Scope

Scopes:

- module scope
- function root scope
- block scope
- catch scope

Rules:

- Duplicate declarations in the same scope are rejected.
- Function parameters and function-body root locals share the same function root scope.
- A child block may shadow an outer `var`.
- A child block may not redeclare a visible outer `const`.
- `const` cannot be assigned after declaration.

## Control Flow

```as
if (condition) statement else statement
while (condition) statement
for (var i = 0; i < 10; i++) statement
for (i = 0; i < 10; i++) statement
for (var item in values) statement
for (item in values) statement
try { ... } catch (error) { ... } finally { ... }
throw value;
return value;
break;
continue;
delete target;
debugger;
```

`break` and `continue` are only valid inside loops.

## Expressions

Primary expressions:

- identifiers
- literals
- grouping
- arrays
- objects
- templates
- lambdas

Objects:

```as
var value = 1;
var obj = {
    value,
    label: "item",
    1: "number key",
    true: "boolean key",
    null: "null key",
    ...other
};
```

Arrays:

```as
var values = [1, 2, 3];
var copy = [...values];
```

Native TDoc literals:

```as
func createProfile(user) {
    return tdoc Object {
        readonly String id $(user.id),
        name "Aurora",
        tags [String "system", Number 4],
    };
}
```

The `tdoc` prefix is valid only in script expressions. It accepts optional explicit type names, `readonly` object members, arrays, objects, and `$(expression)` in value positions. Property names and type names are static. Standalone `.tdoc` documents start directly with the root value and do not allow the prefix or interpolation. Use `TDoc.parse` and `TDoc.stringify` to convert between text and runtime values.

Lambdas:

```as
var one = x => x + 1;
var two = (x, y = 1) => x + y;
var block = () => { return 1; };
```

## Templates

```as
var value = `name=${name}, count=${count}`;
```

Interpolation is parsed as a normal expression and evaluates left to right.

## Runtime Types

Primitive values:

- number
- string
- boolean
- null

Object values:

- object
- array
- function
- error
- regex
- date
- hash map
- string buffer
- path
- CLR interop objects exposed by the host

## Runtime Constructors

Use `schema/runtime-api.json` for the complete machine-readable runtime API. Constructor globals expose structured signatures in their `constructors` arrays:

- `new Array(capacity?: number): array`
- `new String(value?: any): string`
- `new Boolean(value?: any): boolean`
- `new Object(prototype?: object): object`
- `new Number(value?: any): number`
- `new Date(value: number|string): date`
- `new Error(message: string): Error`
- `new HashMap(capacity?: number): HashMap`
- `new Regex(pattern: string|Regex, flags?: string): Regex`
- `new Proxy(target: object, options: object): Proxy`
- `new StringBuffer(initialValue?: string): StringBuffer`
- `new Path(root?: string|Path, ...segments: string|Path): Path`

## Path Runtime API

`Path` is a protocol-aware script object for path text manipulation. It normalizes separators to `/`, handles dot segments, and supports protocol roots such as `mem://app` and `asset://pkg`.

```as
var path = new Path("mem://app/scripts", "../shared", "main");
path.changeExt("as");

return [
    path.toString(),
    path.directoryName(),
    path.fileName(),
    path.extName(),
    path.protocol(),
    Path.join(Path.currentDirectory(), "generated", "out.as")
];
```

Constructor and static members:

- `new Path(root, ...segments)`
- `Path.of(root, ...segments)`
- `Path.isPath(value)`
- `Path.join(root, ...segments)`
- `Path.baseModule(...segments)`
- `Path.normalize(path)`
- `Path.directoryName(path)`
- `Path.fileName(path)`
- `Path.extName(path)`
- `Path.protocol(path)`
- `Path.changeExt(path, extension)`
- `Path.isRooted(path)`
- `Path.isUnderRoot(root, path)`
- `Path.currentFile()`
- `Path.currentDirectory()`

Instance members:

- `append(...segments)`
- `reset(root, ...segments)`
- `changeExt(extension)`
- `directoryName()`
- `fileName()`
- `extName()`
- `protocol()`
- `clone()`
- `toString()`

`Path` APIs accept string path text or existing `Path` values for path arguments. `Path.join`, `Path.baseModule`, `Path.currentFile`, and `Path.currentDirectory` return strings. `Path.extName(path)` and `path.extName()` return the extension including the leading dot, or an empty string when absent. `new Path(...)` and `Path.of(...)` return mutable `Path` objects. `Path` objects compare with `==` by normalized path text value.

## CompileBlock

`CompileBlock` compiles a function body, not a module.

Allowed:

```as
var a = 1;
return a + 1;
```

Rejected:

```as
@module(TEST);
export func run() { return 1; }
```

