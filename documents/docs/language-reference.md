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
declare func external(value);
enum Mode { Read, Write = 4, Append }
```

Destructuring:

```as
var { name, age } = person;
var [ first, ...rest ] = values;
```

Only one simple name is declared by a simple `var` or `const` statement.

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
- CLR interop objects exposed by the host

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

