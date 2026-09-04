# AuroraScript Language Reference

> This document is the machine-readable companion to the English wiki Language Guide; use it for parser and runtime details, while the wiki page provides the author tutorial.

This document summarizes language features supported by the current parser, binder, compiler, and runtime.

## Author Tutorial

AuroraScript is a dynamic script language with a compiler-inferred native path. A source declaration does not carry a C#-style type annotation; the compiler keeps a value in a native representation only when its flow is stable and doing so preserves script semantics. The sections below show the normal authoring workflow before the formal grammar details.

### 1. Create a module

Put `@module(NAME);` at the beginning of a module when host APIs or script `global.getModule` need a stable lookup name. Comments and blank lines may precede it, but no other effective statement may come first.

```as
// math.as
@module(MATH);

func square(value) {
    return value * value;
}

export func add(left, right) {
    return left + right;
}
```

The host builds the source through its configured `SourceResolver`, then calls an exported function by module name and export name. A source without `@module` is anonymous: it can still be imported by path, but it is not available to host `GetModule`, `GetMethod`, or `Execute` calls or to script `global.getModule` by name. The compiler never derives a default name from the source filename or path.

The explicit name is a lookup label, not module identity. Each resolved source is identified by its normalized absolute file path or virtual `ScriptSourceReference.FullPath`; imports, dependency ordering, generated module access, and runtime registration use that path. Consequently, anonymous files with the same filename in different directories can be compiled together. Name-conflict checks apply only to explicit `@module` names.

Scripts normally use `import` for dependencies. When a module must be selected dynamically by its explicit name, `global.getModule("NAME")` returns the already loaded module object or `null`. This lookup does not import a source and does not add a name key to the path-keyed `global.modules` registry.

### 2. Import or include dependencies

```as
// main.as
@module(MAIN);
import math from "./math";

const OFFSET = 2;

export func run(value) {
    return math.add(math.square(value), OFFSET);
}
```

`import Alias from "path";` binds one local name to the dependency's exported module object. `include "path";` instead merges another source file into the current module and exposes its private declarations directly. Both declarations must be at the top of the module, before ordinary declarations, and paths are resolved by the host resolver. The language does not provide named-import braces, default exports, or wildcard imports.

The host may opt in to native modules through `EngineOptions.BuiltIns`. Shipped modules use bare imports such as `import fs from "fs";` and `import http from "http";`; they are not global objects and are unavailable when the host has not enabled them. A relative path such as `./fs` remains a project-source import even when the `fs` native module is enabled.

### 3. Bind execution context

`context name;` and `context name as NativeType;` alias the current `UserState` object for this module. They are not properties of a session bag. Multiple names are allowed; every name still refers to that same instance, and a function that uses several names caches each one in its own local.

```as
context bag;
context player as UserState;

export func tick() Number {
    return player.x;
}

export func dump() {
    return bag.name;
}

export func both() Number {
    return player.x + bag.y;
}

export func current() UserState {
    return player;
}
```

A typed context is loaded once at the start of each function that uses it (`ldfld UserState` plus one `castclass`), then native field and method access applies. An untyped context stays dynamic. Host NativeType names are valid function return contracts. An `export native func player() UserState` entry returns the CLR `UserState` instance on the `$native` path; proven call sites invoke members without wrapping through `ScriptDatum`. Context names cannot be exported. A parameter or local of the same name shadows the context. The type listed after `as` must be a public `[AuroraNativeType]` included in `WithNativeTypes`.

### 4. Declare variables and constants

```as
var total = 0;
var pending;       // null when no initializer is supplied
const limit = 100;

total += 1;
if (total < limit) pending = total;
```

`var` is reassignable; `const` prevents assignment, compound assignment, and increment/decrement after declaration. `const` fixes the binding, not the referenced object:

```as
const settings = { retries: 2 };
settings.retries = 3; // allowed
// settings = {};     // compilation error
```

Simple declarations bind one name. Destructuring requires an initializer:

```as
var { name, age } = person;
var [first, second, ...rest] = values;
```

Duplicate declarations in one scope are rejected. A child block may shadow an outer `var`, but may not redeclare a visible outer `const`. `let` and JavaScript-style comma declarations are not supported.

### 5. Define functions and methods

`func` and `function` are equivalent. Types remain optional. A function
without a return contract keeps the normal weakly typed behavior; a type name
after the parameter list adds an exact boundary contract:

```as
func clamp(value, minimum, maximum) {
    if (value < minimum) return minimum;
    if (value > maximum) return maximum;
    return value;
}

func sum(first, ...rest) {
    var total = first;
    for (var item in rest) total += item;
    return total;
}

func add(Number left, Number right) Number {
    return left + right;
}

export native func distance(Number dx, Number dy) Number {
    return Math.sqrt(dx * dx + dy * dy);
}
```

Default parameters are written `name = expression`; a rest parameter starts with `...`, must be last, and cannot have a default. Functions are values. An object “method” is a function-valued property, not a separate declaration form:

```as
var operations = {
    add: (left, right) => left + right,
    format: value => `#${value}`
};
return operations.format(operations.add(20, 22));
```

Lambdas can have an expression body or a block body and can capture outer bindings.

`native func` is an explicit module-scope ABI contract. It emits a
`ScriptContext`-aware CLR-native entry for declared numeric, boolean, array,
packed-array, and host NativeType parameters and returns, while retaining
`ScriptDatum` for dynamic slots. A declared NativeType return is the CLR type
itself (`UserState player$native(...)`), not `ScriptDatum`; only the `$typed`
shell converts at a dynamic boundary. The host is responsible for supplying a
matching `UserState` object. Dynamic expressions inside the body remain valid
and cross a Datum boundary locally; they do not disable native storage in the
rest of the function.
Exported functions and private functions used as values also receive a
Datum-compatible closure shell. A qualified call to an imported exported
native function calls its native entry directly when every native argument is
proven compatible; otherwise it uses that shell. Native functions require a
return contract. Use the contextual return type `void` for procedures:

```as
native func clear(Cache cache) void {
    cache.size = 0;
}
```

The CLR native entry then returns `void`; falling through and bare `return;`
are valid, while `return expression;` is rejected. A direct native call used
as a statement does not materialize a result. Dynamic, exported, or
value-producing calls observe `null`, matching a host `[AuroraExport]` method
whose CLR return type is `void`. `void` is not an alias for `Null` and is not
valid on ordinary functions, parameters, fields, or assertions.

Native functions may use trailing primitive defaults that the compiler can evaluate as constants,
but the default must exactly match an explicit `Number`, `Boolean`, `String`, or
`Null` parameter type. They cannot use rest parameters and cannot be assigned. Apply
them only through a normal build: neither incremental nor replacement hot
patches may add, replace, or redefine a native function. The emitted native
method contains the function body directly; it does not enter a script frame
or add an exception wrapper. Runtime error conversion derives native function
names from the CLR exception stack and combines them with recorded source
locations.

### 6. Choose a value type

The source-level primitive and collection forms are:

| Type | Script spelling and construction | Notes |
| --- | --- | --- |
| `number` | `42`, `3.14`, `6e2`, `10000D`, `100_00` | Double-precision. Unsuffixed integers that fit `Int32` stay `Int32`; wider exact integers stay `Int64`. Suffix `D`/`d` forces `Number`. |
| `int32` constraint | `func index(int32 value) int32` | Compile-time/ABI constraint for an exact signed 32-bit integer. The runtime value remains a number and `typeof` remains `"number"`; `new int32` is invalid. |
| integer hex | `0xFFFF`, `0x100000000L` | Hexadecimal literals default to integer (`Int32` if they fit, otherwise `Int64`). Suffix `L`/`l` forces `Int64`; `I`/`i` forces `Int32`. `D` is a hex digit, so it is not a hex suffix. |
| `string` | `'text'`, `"text"`, `` `value=${expr}` ``, `|> line` | Immutable UTF-16 text; templates interpolate expressions and block strings preserve physical newlines. |
| `boolean` | `true`, `false` | Boolean value. |
| `null` | `null` | Missing value. |
| general `array` | `[1, "two", null]`, `new Array(n)` | Growable and heterogeneous; `new Array(n)` creates `n` null slots. |
| plain `object` | `{ name: "Aurora", ...other }` | Mutable property map. |
| `function` | `func f() {}`, `x => x` | Callable value/closure. |
| `regex` | `/pattern/flags`, `new Regex(pattern, flags)` | Literal flags are `g`, `i`, `m`, `u`, and `y`. |
| `enum` | `enum Mode { Read, Write = 4 }` | Object whose members are 32-bit integer numbers. |

Named `type` declarations are compile-time shapes. They do not change the
weakly typed object model and do not insert runtime structure scans:

```as
export type Point {
    Number x;
    Number y;
}

func add(Point p) Number {
    return p.x + p.y;
}
```

After a value is granted as `Point` (`Point p`, `value as Point`, or a
declared `Point` return), the compiler treats `p.x` and `p.y` as `Number` so
arithmetic can use native code. Object literals in those same positions are
also granted the shape, so `return { x: 1, y: 2 }` and `sum(Point p)` of
`{ x: 1, y: 2 }` do not need `as Point`. A local keeps the shape only when
every branch agrees. Missing or ill-typed fields still follow ordinary weak
coercion (for example arithmetic becomes `NaN`); they are not rejected as a
`Point` mismatch. Runtime exact checks remain only on builtin native types
(`Number`, `Boolean`, packed arrays, and the other `CheckedType` names) and
the lowercase `int32` numeric constraint at typed parameters, declared
returns that are not already proven, and assertions such as
`value as Number` or `value as int32`. `int32` accepts only finite integral
numbers in `-2147483648..2147483647` and rejects negative zero because native
integer storage cannot preserve its sign. Checked boundaries do not truncate
or wrap. A local whose every assignment is an integer keeps 32-bit storage:
integer literals, `int32` parameters, fields, and returns, `as int32` values,
`Int32Array` elements, signed bitwise results, and `+`, `-`, `*`, `%` over
those. Such a local is never conservatively widened to `Number`; it wraps like
CLR `int` instead, so `var max = 2147483647; max + 1` is `-2147483648`.
Expressions built from those locals wrap the same way (`currentX - 1` stays
`int32`). Write `2147483647D` or any fractional assignment to opt a local back
into `Number`. Integer `%` likewise cannot represent the negative zero or `NaN`
a `Number` remainder would produce, so `-14 % 7` is `0` and a zero divisor
raises a runtime error; give either operand `Number` storage to keep those
values. `/` is script number division. When the quotient is an exact integer,
assert it with `((current - currentX) / width) as int32`. Parentheses are
required because `as` binds tighter than `/`; `Math.floor` is not a substitute
because it returns `Number` and would hide a non-integral quotient.
`int32` does not add a runtime type identity, introduce a
global constructor, or act as a TDoc type name. Packed-array checks are nullable:
`null as Float64Array` returns `null`, and a `null` argument is inferred from
a declared `Float64Array` parameter without requiring `as`. Non-null values
must still match the exact packed-array type.

Exported shapes can be referenced through an imported module alias:

```as
import models from "./models";

func distance(models.Point point) Number {
    return point.x * point.x + point.y * point.y;
}
```

A shape field may itself be another shape. Nested members stay objects at
runtime; reading through them continues Native derivation:

```as
export type Rect {
    Point origin;
    Number width;
    Number height;
}

func left(Rect rect) Number {
    return rect.origin.x;
}
```

Shapes may also reference themselves or other shapes cyclically. Cycles
remain compile-time metadata only; the compiler does not validate custom
shape legality at runtime:

```as
export type Node {
    Number value;
    Node next;
}

func tailValue(Node node) Number {
    return node.next.value;
}
```

Mutually recursive shapes are also allowed:

```as
export type Left {
    Number value;
    Right other;
}
export type Right {
    Number value;
    Left other;
}
```

Nested and cyclic shapes do not add runtime object scans.

Only `export type` declarations are visible through the alias. Qualified
shape references remain compile-time metadata and do not add runtime module
properties or object checks. Using an imported type as a value
(`models.Point` in an expression) is rejected at compile time. The alias
itself is still the module instance: `models.add` is a function, `models.Point`
is not a property.

There is no separate `Unit`/`void` value type in the script language.

Common object-like built-ins are `Date`, `Error`, `HashMap`, `Path`, `Proxy`, `Regex`, and `StringBuffer`. Construct them with `new` and use the members documented in `schema/runtime-api.json` and the Script API pages. `TDoc` additionally provides the compiler-recognized `tdoc` expression for typed document values.

### 7. Use packed arrays for homogeneous data

The runtime exposes eleven fixed-length packed arrays:

```as
var signedBytes = new Int8Array(size);
var bytes = new UInt8Array(size);
var shorts = new Int16Array(size);
var unsignedShorts = new UInt16Array(size);
var ints = new Int32Array(size);
var unsignedInts = new UInt32Array(size);
var longs = new Int64Array(size);
var unsignedLongs = new UInt64Array(size);
var singles = new Float32Array(size);
var fractions = new Float64Array(size);
var flags = new BooleanArray(size);
```

Each constructor accepts an optional non-negative length and zero-initializes contiguous primitive storage. `length` is read-only; `push`, `pop`, and element deletion are not supported. Use a general `Array` when the collection must grow or contain mixed values. Script numbers are doubles, so values read from `Int64Array` and `UInt64Array` must be exactly representable as a script number; use TDoc typed values when exact 64-bit persistence is required.

`Conv8` reads and writes scalars and UTF-8 text on a `UInt8Array` only. Multi-byte integers and floats take `littleEndian` (default `true`). `typeof Conv8` is `"type"`; `new Conv8()` fails. There is no script `Encoding` global.

`typeof` reports the constructor name for these packed arrays (`"Int8Array"`, `"UInt8Array"`, and so on). They remain object-backed `ScriptDatum` values; they do not consume a dedicated `ValueKind` bit. `value as Int8Array` is the exact assertion used by the typed backend, while `typeof value == "Int8Array"` is the dynamic name check.

### 8. Understand the strong-typing path

The compiler performs flow analysis rather than requiring explicit annotations. Stable numbers, booleans, integer loop variables, and known packed-array references can be emitted as native CIL locals and direct array accesses. When a value is put in a dynamic object, read through an unknown property, passed to an unknown host callback, assigned an unrelated kind, or otherwise escapes the proven flow, the compiler boxes it into the normal `ScriptDatum` representation.

This path exists for three practical reasons:

1. Native locals avoid temporary `ScriptDatum` construction and boxing in numeric loops.
2. Packed arrays use contiguous primitive storage instead of one dynamic slot per element.
3. Explicit `native func` calls can avoid generic property lookup and dynamic dispatch.

It is therefore an optimization and a semantic boundary, not a second statically typed source language. Dynamic scripts remain valid, and unsupported local flow safely falls back to the dynamic path. To help inference, keep hot locals single-kind, cache `length`, keep packed arrays in locals, and avoid unknown callbacks in the inner loop. Function-call ABI optimization is explicit: use `native func` when a callable native ABI is required.

### 9. Handle host globals and compile blocks

Host globals can be described for tooling in a separate declaration file:

```as
@global();

declare const APP_NAME;
declare var ONLINE_TOTAL;
declare func HOST_LOG(message);
```

Native host types use `declare type`. A type containing only static members is
a non-constructible Type; adding a `constructor` describes a Type that supports
`new`. These names are for editor assistance only; they do not change compiler
inference. Do not use structural `type` declarations for host types.

```as
@global();

declare type Stats {
    static const Number PI;
    static func mean(Number a, Number b) Number;
}

declare type Vec2 {
    constructor(Number x, Number y);
    Number x;
    func length() Number;
    static const Number DIMENSIONS;
    static func from(Number x, Number y) Vec2;
}
```

An `@global()` file may contain only `declare` statements and cannot also contain `@module`, imports, includes, or exports. Declarations do not create runtime values; the host must define them on the script domain. `CompileBlock` accepts only a function body with host-supplied parameters and rejects all module-only syntax.

> **Authoring rule of thumb:** use a full module for imports, exports, shared helpers, and host-called entry points; use a `CompileBlock` for a small one-off body; use packed arrays and stable locals for numeric hot paths; use ordinary arrays/objects for flexible application data.

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
export native func add(Number left, Number right) Number { return left + right; }
```

Rules:

- `@module(NAME);` is metadata, not a function annotation.
- `@module` must be first when present.
- Omitting `@module` leaves the module anonymous; no filename- or path-derived name is created.
- An explicit name is used by host name-based module APIs and script `global.getModule`, and must be unique across explicitly named project and enabled built-in modules.
- Imports resolve source paths and do not depend on the imported module's explicit name.
- `include` and `import` must be at the top of the module.
- A host-enabled native module is imported by its bare module path. Native modules do not become globals, and relative paths continue to resolve as project sources.
- `export` may only appear at module scope.
- `native` is contextual: it modifies a module-scope `func`/`function`
  declaration and remains a valid identifier elsewhere.
- `declare` is not valid in modules. Use a separate `@global()` declaration file for host globals.
- Duplicate module-scope names are rejected.

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

declare type Stats {
    static func mean(Number a, Number b) Number;
}
```

`declare` is compile-time only and is only valid inside `@global()` files. A global declaration file must start with `@global();` after only comments or blank lines, cannot also use `@module`, cannot be imported or included, and is not compiled as a module. The compiler scans resolver-visible project `.as` files and loads these optional declarations before module analysis when they exist. Host-provided globals still work at runtime without an `@global()` file; the file exists to improve editor assistance and static diagnostics.

Global declarations create compiler symbols for binding, duplicate checks, and `const` assignment checks, but do not emit module initialization code or create runtime module properties. `declare var/const` must use one simple name and cannot have an initializer or destructuring pattern. Reads and writes of declared external variables resolve through the script domain `global` unless a local variable shadows the name. Duplicate global names across `@global()` files are rejected; functions do not support overloads. `declare type` member signatures are not used for type inference.

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
        Flag enabled false,
    };
}
```

The `tdoc` prefix is valid only in script expressions. It accepts optional explicit type names, `readonly` object members, arrays, objects, scalars (`null` / boolean / number / string), and `$(expression)` in value positions. Property names and type names are static. Standalone `.tdoc` documents start directly with the root value and do not allow the prefix or interpolation. Use `TDoc.parse` and `TDoc.stringify` to convert between text and runtime values.

Host NativeTypes participate when the CLR class implements `INativeTypedDocument` and is listed in `WithNativeTypes`. Then `tdoc Vec2 { x 3, y 4 }`, `tdoc Vec2 [3, 4]`, and `tdoc Flag false` construct the native instance directly. `WriteTypedDocument` chooses the canonical stored shape (object members, array elements, or a scalar null/boolean/number/string); deserialize and literals accept the shapes that type implements.

Lambdas:

```as
var one = x => x + 1;
var two = (x, y = 1) => x + y;
var block = () => { return 1; };
```

`typeof` returns interned type-name strings. Primitive and privileged object kinds stay lowercase; native objects that share `ValueKind.Object` storage report their constructor name:

```as
typeof 1;                      // "number"
typeof [];                     // "array"
typeof {};                     // "object"
typeof Math;                   // "type"
typeof Conv8;                  // "type"
typeof console;                // "type"
typeof new Int8Array(2);       // "Int8Array"
typeof new StringBuffer("");   // "StringBuffer"
typeof new HashMap();          // "HashMap"
typeof new Path("mem://app");  // "Path"
```

`value as Number` (and other builtin native type names) is a runtime assertion and is not a substitute for `typeof`. `value as Point` is a compile-time grant that unlocks native field facts; it does not scan the object. Host code should call `ScriptDatum.TypeOf` / `GetTypeName` for the same names; `ValueKind` is only the datum storage tag.

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
- hash map (`typeof` → `"HashMap"`)
- string buffer (`typeof` → `"StringBuffer"`)
- path (`typeof` → `"Path"`)
- packed arrays (`typeof` → `"Int8Array"`, `"UInt8Array"`, `"Int16Array"`, `"UInt16Array"`, `"Int32Array"`, `"UInt32Array"`, `"Int64Array"`, `"UInt64Array"`, `"Float32Array"`, `"Float64Array"`, `"BooleanArray"`)
- infrastructure and host NativeTypes (`typeof` → `"type"`): `Math`, `JSON`, `TDoc`, `console`, `Conv8`, `HotPatch`, and types selected with `WithNativeTypes`
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

