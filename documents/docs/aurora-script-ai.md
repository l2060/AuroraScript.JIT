# AuroraScript AI Reference

Version target: AuroraScript.JIT 2.1.1

This file is the primary AI reference for AuroraScript. Prefer these rules over JavaScript assumptions.

## File And Module Model

- Script files normally use `.as`.
- A module may start with `@module(NAME);`. It must be the first effective statement when present.
- `include "path";` and `import Alias from "path";` must appear at the top of a module before ordinary declarations.
- `export` is only valid at module scope.
- `CompileBlock` accepts statement bodies only. It rejects module-only syntax such as `@module`, `import`, `include`, `export`, and `declare`.

## Statements

- Empty statement: `;`
- Block: `{ statement* }`
- Function: `func name(args) { ... }` or `function name(args) { ... }`
- External declaration: `declare func name(args);`
- Variable: `var name;`, `var name = expr;`, `const name = expr;`
- Destructuring: `var { a, b } = obj;`, `var [ first, ...rest ] = array;`
- Enum: `enum Name { A, B = 3, C }`
- Control flow: `if`, `else`, `while`, `for`, `for-in`, `break`, `continue`, `return`, `throw`, `try`, `catch`, `finally`, `delete`, `debugger`

Variable declarations are single-binding declarations. `var a = 1, b = 2;` is not the current form.

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
- `Error`, `HashMap`, `Regex`, `Proxy`, `StringBuffer`
- `console`, `JSON`, `Math`, `HotPatch`

Common APIs:

- `console.log(...values)`, `console.error(...values)`, `console.time(label)`, `console.timeEnd(label)`
- `JSON.parse(text)`, `JSON.stringify(value, indented = false)`
- `Math.PI`, `Math.E`, `Math.Tau`, `Math.abs`, `Math.max`, `Math.min`, `Math.random`, `Math.log`, `Math.pow`, `Math.exp`, `Math.cos`, `Math.sin`, `Math.tan`, `Math.acos`, `Math.asin`, `Math.atan`, `Math.floor`, `Math.round`
- Array: `length`, `push`, `pop`, `sort`, `join`, `slice`, `reverse`, `unshift`, `shift`, `concat`, `find`, `findIndex`, `findLast`, `findLastIndex`, `map`, `filter`, `some`, `every`, `flat`, `reduce`, `indexOf`, `lastIndexOf`, `has`
- String: `length`, `contains`, `indexOf`, `lastIndexOf`, `startsWith`, `endsWith`, `substring`, `split`, `match`, `matchAll`, `replace`, `padLeft`, `padRight`, `trim`, `trimLeft`, `trimRight`, `slice`, `toString`, `charCodeAt`, `toLowerCase`, `toUpperCase`
- StringBuffer: `append`, `insert`, `appendLine`, `clear`, `release`, `stringAndRelease`, `toString`

## Performance Rules

- Prefer `const` for compile-time constants.
- Enable module const inlining for modules with stable exported constants.
- Use normal template strings for small or medium formatting; the compiler already selects concat or builder paths.
- Use `StringBuffer` for long loops or many incremental appends.
- Use `@directCall` on helper functions that should remain directly callable when the compiler cannot infer it.
- Avoid `console.log` in hot paths.
- Avoid unnecessary closure captures in loops.
- Cache repeated dynamic property lookups in local variables when the same property is used many times.
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
2. If generating code, check examples in `examples/valid`.
3. If rejecting code, compare with `examples/invalid`.
4. Use the MCP tool `aurora_check_script` for final validation.

