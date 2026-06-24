<p align="center">
  <img src="icon.png" width="128" alt="AuroraScript Logo" />
</p>

<p align="center">
  <a href="./README.md">简体中文</a> | <a href="./README_EN.md">English</a>
</p>

# AuroraScript

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-2.0.0-orange.svg)](src/AuroraScript.csproj)
[![Target](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blueviolet.svg)](src/AuroraScript.csproj)

AuroraScript 是一个面向 .NET 宿主程序的轻量脚本引擎。脚本会被编译为 CIL，并通过 .NET 运行时执行，适合把规则、业务逻辑、配置化流程、热修复逻辑和小型表达式嵌入到 C# 应用中。

AuroraScript 的语法借鉴了 JavaScript 的表达式、对象、数组、闭包和模块写法，但它不是 ECMAScript 实现，也不承诺兼容浏览器或 Node.js 语义。README 中列出的能力均以当前源码和测试覆盖为准。

> [!NOTE]
> 项目仍在持续开发中。公开 API、性能和语义边界仍可能调整；用于生产前建议固定 NuGet 版本并跑自己的回归测试。

## 支持平台

NuGet 包名：`AuroraScript.JIT`

当前 `2.0.0` 包发布为多目标框架：

| 目标框架 | 支持情况 |
|---|---|
| `net8.0` | 支持 `Dynamic` / `OnlyRun`；不支持 `CompilationMode.Persistence` |
| `net9.0` | 支持 `Dynamic` / `OnlyRun` / `Persistence` |
| `net10.0` | 支持 `Dynamic` / `OnlyRun` / `Persistence` |

平台目标为 `AnyCPU`。运行时没有 native 依赖，`Dynamic` 和 `OnlyRun` 模式理论上可在 x64 与 ARM64 上运行。`Persistence` 模式生成 IL-only 程序集；已避免 x64 PE 标记，但仍建议在目标 ARM64 系统上执行完整测试后再正式声明支持。

## 引擎特色

- **CIL/JIT 执行**：脚本编译为 .NET IL，运行时走 CLR/JIT，不依赖解释器循环执行主逻辑。
- **嵌入简单**：通过 `AuroraEngine` 编译脚本，通过 `ScriptDomain` 隔离运行环境并执行模块函数。
- **三种编译模式**：可在动态执行、可检查内存程序集、持久化 DLL/PDB 之间取舍。
- **CLR 互操作**：支持注册 CLR 类型，脚本可调用构造函数、属性、字段、实例方法、静态方法、重载、可选参数和 `params` 参数。
- **运行时热更新**：支持宿主侧 `DynamicPatch` 和脚本侧 `HotPatch.replace` / `HotPatch.incremental`。
- **显式性能注解**：支持 `@directCall` 标记模块内热点函数，让可优化的调用点走更直接的执行路径。
- **模块与作用域隔离**：支持 `@module`、`import`、`include`，每个 `ScriptDomain` 拥有独立 global 和模块实例。
- **CompileBlock**：可编译不进入模块系统的小段脚本，适合公式、过滤器、规则判断等高频小逻辑。
- **内置标准对象**：包含 `Object`、`Array`、`String`、`Date`、`Regex`、`HashMap`、`StringBuffer`、`JSON`、`Math`、`console`、`Proxy`、`HotPatch`。
- **测试覆盖广**：测试覆盖词法、语法、表达式、语句、模块、编译模式、CLR 互操作、JSON、热重载、并发和回归场景。

## 安装

```bash
dotnet add package AuroraScript.JIT
```

源码构建：

```bash
git clone https://github.com/l2060/AuroraScript.git
cd AuroraScript
dotnet build src/AuroraScript.csproj -c Release
```

## 快速使用

### 宿主代码

```csharp
using AuroraScript;
using AuroraScript.Runtime;
using System.Text;

var options = EngineOptions.Default
    .WithBaseDirectory("./scripts")
    .WithCompilationMode(CompilationMode.Dynamic)
    .WithOptimizeOption(OptimizeOptions.Release);

var engine = new AuroraEngine(options);
engine.RegisterType(typeof(Math), "Math2");

await engine.BuildAsync(engine.SearchAllFileSource(Encoding.UTF8));

var domain = engine.CreateDomain();
var result = domain.Execute("MAIN", "main", ScriptDatum.FromNumber(20));
Console.WriteLine(result);
```

### 脚本代码

```javascript
@module(MAIN);

export func main(value) {
    var total = Math2.Abs(-value);
    var items = [1, 2, 3];

    for (var item in items) {
        total = total + item;
    }

    return total;
}
```

### 注入宿主状态

```csharp
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;

public sealed class MyState : ScriptObject
{
    public MyState()
    {
        Define("Name", StringValue.Of("Aurora"));
        Define("Count", NumberValue.Of(3));
    }
}

var userState = new MyState();
var domain = engine.CreateDomain(userState: userState);
```

`userState` 必须继承 `ScriptObject`。脚本中可通过 `$state` 访问该对象：

如果值已经是运行时表示，可直接调用 `Define(string, ScriptDatum, ...)`，避免先装箱成 `ScriptObject` 再转换回 `ScriptDatum`。

```javascript
@module(MAIN);

export func name() {
    return $state.Name;
}
```

### 配置全局变量或函数

```csharp
var domain = engine.CreateDomain(global =>
{
    global.Define("HOST_ADD", (Func<int, int, int>)((a, b) => a + b));
    global.Define("HOST_NAME", "Aurora");
    global.Define("HOST_COUNT", ScriptDatum.FromNumber(3));
});
```

```javascript
@module(MAIN);

export declare func HOST_ADD(left, right);

export func run() {
    return HOST_NAME + ":" + HOST_ADD(20, 22);
}
```

## CompileBlock

`CompileBlock` 会把源码当作匿名函数体编译，不创建模块，也不参与热重载。它适合公式、规则、过滤器和需要频繁调用的小段逻辑。

```csharp
var engine = new AuroraEngine(EngineOptions.Default.WithBaseDirectory("."));

var block = engine.CompileBlock("""
func clamp(v, min, max) {
    if (v < min) return min;
    if (v > max) return max;
    return v;
}

return clamp(x, 0, 100);
""", new CompileBlockOptions
{
    Parameters = ["x"],
    SourceName = "rules/clamp.as"
});

var result = block.Invoke(ScriptDatum.FromNumber(125));
```

参数名通过 `CompileBlockOptions.Parameters` 声明，运行时按位置传入。参数名不能为空、不能重复，且不能使用 `global`、`$args`、`$state`。

`CompiledBlock` 无引用后会通过 GC finalizer 自动释放编译期间注册的动态 delegate；需要确定性释放时也可以显式调用 `Dispose()`。

## 编译模式

| 模式 | 说明 | 适用场景 |
|---|---|---|
| `Dynamic` | 使用 `DynamicMethod` 发射 CIL，不产生可持久化程序集 | 高性能运行、规则执行、小型脚本 |
| `OnlyRun` | 使用可回收动态程序集在内存中运行 | 需要更容易被运行时工具观察的内存执行 |
| `Persistence` | 生成可保存的 DLL，并可带调试符号 | 调试、诊断、需要落盘程序集的场景 |

限制：

- `Persistence` 需要 `net9.0` 或更高版本；`net8.0` 下调用会抛出 `PlatformNotSupportedException`。
- Visual Studio 源码级脚本调试依赖 `Persistence` 模式和 Debug 优化设置。

## 热修复

热修复由 `EngineOptions.EnableHotReload` 控制，默认启用。关闭后会禁用宿主侧 `DynamicPatch` 和脚本侧 `HotPatch`。

宿主侧：

```csharp
domain.DynamicPatch(
    engine.MemorySource("main.as", "@module(MAIN); export func run() { return 42; }"),
    HotPatchType.Replace);
```

脚本侧：

```javascript
HotPatch.replace("main.as", "@module(MAIN); export func run() { return 42; }");
HotPatch.incremental("main.as", "export func added() { return 1; }");
```

注意：

- `Replace` 会清空目标模块现有成员后应用新代码。
- `Incremental` 会保留现有成员，并添加或更新补丁中声明的成员。
- 是否使用 `Replace` 或 `Incremental` 由宿主或脚本作者根据补丁范围决定；修改一组互相调用的函数时，通常更适合使用 `Replace` 保持模块内容一致。
- 补丁源码中的顶层代码会在应用补丁时执行。

## 模块与函数注解

模块名通过 `@module(NAME);` 声明。`@module` 必须出现在模块的第一条有效语句位置；它前面只能有空白或注释。

```javascript
// 模块声明必须放在第一条有效语句
@module(MAIN);

export func run() {
    return 42;
}
```

函数注解写在函数声明前。当前支持的函数注解是 `@directCall`。

```javascript
@module(MAIN);

@directCall
func addOne(value) {
    return value + 1;
}

export func run(value) {
    return addOne(value);
}
```

`@directCall` 和 `@directCall()` 等价，用于显式启用该函数的模块内直接调用优化，同时保留模块上的函数对象。因此函数仍可被导出、读取或传给宿主侧调用。

```javascript
@module(MAIN);

@directCall()
export func checksum(value) {
    return value + 100;
}
```

如果启用了自动模块内直接调用推断，可以用 `@directCall(false)` 为单个函数关闭该优化：

```javascript
@module(MAIN);

@directCall(false)
func normalCall(value) {
    return value;
}
```

宿主侧可以通过 `EngineOptions.WithEnableAutoModuleDirectCall(true)` 开启自动推断。显式 `@directCall` 不依赖该选项；即使没有开启自动推断，被标记的函数仍会按可优化规则尝试使用直接调用。

适用建议：

- `@directCall` 适合参数固定、逻辑稳定、被模块内高频调用的普通函数。
- 使用默认参数、`$args`、捕获外部变量等动态调用形态时，编译器会继续使用普通函数调用路径。
- 被 `@directCall` 标记的函数名不应在模块内被重新赋值。

## 语言能力

当前测试覆盖和示例中使用的语法包括：

- `var` / `const` / `func` / `function` / `return`
- `@module`、`@directCall`
- `if` / `else` / `for` / `for-in` / `while` / `break` / `continue`
- `try` / `catch` / `finally` / `throw`
- `enum`
- 闭包、递归、默认参数、`$args`
- 表达式 Lambda：`(a, b) => a + b`
- 块 Lambda：`(value) => { return value * 2; }`
- 对象字面量、数组字面量、稀疏数组
- 对象/数组解构
- 对象 shorthand 和对象/数组 spread
- 模板字符串和嵌套模板
- 正则字面量与除法语义区分
- `typeof`、`in`、`delete`、自增自减、复合赋值

## 内置 API

### 全局对象

- `Array`
- `String`
- `Boolean`
- `Object`
- `Number`
- `Date`
- `Error`
- `HashMap`
- `Regex`
- `Proxy`
- `StringBuffer`
- `console`
- `JSON`
- `Math`
- `HotPatch`
- `global`
- `$state`
- `$args`

### Object

静态成员：

- `Object.equal$(a, b)`
- `Object.equal(a, b)`
- `Object.deepEqual(a, b)`
- `Object.assign(target, ...sources)`
- `Object.keys(obj)`
- `Object.clone(obj)`
- `Object.deepClone(obj)`
- `Object.freeze(obj)`

实例成员：

- `obj.length`
- `obj.toString()`

> `Object.extends` 当前在构造器中注册，但实现体未返回有效结果，因此 README 不把它列为可用 API。

### Array

静态成员：

- `Array.from(iterable, [mapCallback])`
- `Array.isArray(value)`
- `Array.of(...items)`
- `Array.withCapacity(capacity)`

`Array.withCapacity(capacity)` 创建 `length` 为 0 的数组并预留底层容量；不同于 `new Array(n)`，它不会创建 `n` 个空/null 槽位，适合已知追加规模的 `push` 场景。

实例成员：

- `length`
- `has(value)`
- `indexOf(value)`
- `lastIndexOf(value)`
- `push(...items)`
- `pop()`
- `sort([compare])`
- `join([separator])`
- `slice(start, [end])`
- `reverse()`
- `unshift(...items)`
- `shift()`
- `concat(...items)`
- `find(callback)`
- `findIndex(callback)`
- `findLast(callback)`
- `findLastIndex(callback)`
- `map(callback)`
- `filter(callback)`
- `some(callback)`
- `every(callback)`
- `flat([depth])`
- `reduce(callback)`

### String

静态成员：

- `String.fromCharCode(code)`
- `String.valueOf(value)`
- `String.compare(a, b)`

实例成员：

- `length`
- `contains(text)`
- `indexOf(text)`
- `lastIndexOf(text)`
- `startsWith(text)`
- `endsWith(text)`
- `substring(start, [end])`
- `slice(start, [end])`
- `split(separator)`
- `match(regex)`
- `matchAll(regex)`
- `replace(search, replacement)`
- `padLeft(width, [char])`
- `padRight(width, [char])`
- `trim()`
- `trimLeft()`
- `trimRight()`
- `toString()`
- `charCodeAt(index)`
- `toLowerCase()`
- `toUpperCase()`

### Date

静态成员：

- `Date.now()`
- `Date.utcNow()`
- `Date.parse(value)`

实例成员：

- `year`
- `month`
- `day`
- `hour`
- `minute`
- `second`
- `millisecond`
- `dayOfWeek`
- `dayOfYear`
- `ticks`
- `toString([format])`

### HashMap

- `set(key, value)`
- `get(key)`
- `getOrInsert(key, valueOrCallback)`
- `has(key)`
- `delete(key)`
- `clear()`
- `keys`
- `values`
- `size`

### Regex

- `test(text)`

字符串还提供 `match(regex)` 和 `matchAll(regex)`。

### StringBuffer

- `append(...items)`
- `appendLine(...items)`
- `insert(index, value)`
- `clear()`
- `toString()`
- `release()`
- `stringAndRelease()`

### JSON

- `JSON.parse(text)`
- `JSON.stringify(value, [indented])`

JSON 支持脚本基础值、对象、数组、HashMap 等类型；循环引用会抛出运行时异常。

### Math

常量和函数以当前 `MathSupport` 为准，测试覆盖常见数学函数、随机数和错误路径。常用成员包括：

- `Math.PI`
- `Math.E`
- `Math.abs(x)`
- `Math.max(...values)`
- `Math.min(...values)`
- `Math.random()`
- `Math.floor(x)`
- `Math.round(x)`
- `Math.pow(x, y)`
- `Math.log(x)`
- `Math.exp(x)`
- `Math.sin(x)`
- `Math.cos(x)`
- `Math.tan(x)`

### console

- `console.log(...args)`
- `console.error(...args)`
- `console.time(label)`
- `console.timeEnd(label)`

输出目标可通过 `EngineOptions.WithConsoleStdOut` 和 `WithConsoleErrorOut` 配置。

### Proxy

```javascript
var proxy = new Proxy(target, {
    get: (obj, key) => obj[key],
    set: (obj, key, value) => { obj[key] = value; return value; }
});
```

构造 `Proxy` 时必须提供 `get` 和 `set` handler。测试覆盖了属性读取、写入和删除相关路径。

## CLR 互操作

注册 CLR 类型：

```csharp
engine.RegisterType<HostCalculator>("Calculator");
```

脚本中使用：

```javascript
@module(MAIN);

export func run() {
    var host = new Calculator(5);
    host.Value = 7;
    host.Field = 3;
    return [host.Add(2), Calculator.Multiply(3, 4), host.Value, host.Field];
}
```

已测试能力：

- 构造函数
- 属性和字段读写
- 实例方法和静态方法
- 重载选择
- 可选参数
- `params` 参数
- CLR 集合和委托的全局注入
- 类型访问限制：`TypeAccess.All`、`Constructor`、`Static`
- 注册表重复 alias 和释放后的错误处理

## 测试

测试项目：`tests/AuroraScript.Tests`

当前 `net10.0` test discovery 共发现 **395** 个测试用例。按测试类统计：

| 测试类 | 用例数 | 覆盖重点 |
|---|---:|---|
| `LexerTests` | 37 | 词法、数字/字符串/正则、注释、错误 token |
| `ParserSyntaxTests` | 85 | 语法分支、模块声明、import/include/export、函数注解、错误语法诊断 |
| `ExpressionExecutionTests` | 38 | 表达式、运算符、成员/索引访问、spread、赋值 |
| `StatementExecutionTests` | 7 | 控制流、循环、闭包、递归、异常、Domain 隔离 |
| `LanguageFeatureExecutionTests` | 16 | enum、Lambda、稀疏数组、truthiness、模板、赋值语义 |
| `ModuleCompilationTests` | 12 | 模块依赖、并行编译、循环依赖、错误聚合、取消 |
| `CompilerBackendPlanTests` | 80 | backend plan、direct call、闭包/upvalue、slot/lowering、控制流和常量折叠计划 |
| `CompileBlockTests` | 23 | CompileBlock 参数、调用方式、错误输入、诊断和动态 delegate 生命周期 |
| `CompilationModeTests` | 6 | Dynamic/OnlyRun/Persistence 行为和热重载开关 |
| `RuntimeApiAndErrorTests` | 11 | 运行时 API、错误路径、`$state`、释放后行为 |
| `BuiltInLibraryTests` | 12 | Math、String、Array、JSON、HashMap、Regex、StringBuffer、Console |
| `AdvancedRuntimeTypeTests` | 6 | 构造器、Object、freeze、Date、Proxy |
| `ClrInteropTests` | 9 | CLR 构造/属性/字段/方法/重载/访问限制 |
| `SerializationTests` | 9 | JSON 序列化/反序列化、循环引用、异常 JSON |
| `ScriptDatumTests` | 4 | Datum payload、相等性、CLR 集合转换、Span helper |
| `HotReloadTests` | 4 | 热重载禁用、增量补丁、替换补丁、Domain 隔离 |
| `ConcurrentRuntimeTests` | 3 | 同域/多域并发、detached closure 并发 |
| `ReleaseRegressionTests` | 9 | Release 直连调用、闭包槽位、栈平衡、混淆、空模块 |
| `ClosureFunctionContextTests` | 3 | 上下文池生命周期和 detached 调用 |
| `EngineOptionsAndSourceTests` | 10 | EngineOptions、Source 路径、扩展名、并行度、空输入 |
| `CoreSemanticsRegressionTests` | 11 | 基础语义、null 加法、coercion、短路逻辑、数组容量/索引、对象属性、闭包/循环 |

覆盖范围摘要：

- Lexer：关键字、标识符、Unicode、运算符、数字、字符串、正则、注释、CRLF 位置、错误 token。
- Parser：模块元数据、import/include/export、声明、表达式、Lambda、解构、控制流、异常、模板、正则、错误诊断。
- CompileBlock：参数校验、局部函数、domain/no-domain 调用、模块语法拒绝、source name、空输入、动态 delegate 释放。
- 表达式/语句：优先级、算术、位运算、比较、逻辑、成员访问、spread、赋值、循环、异常、闭包、递归。
- 基础语义回归：null 数值加法、字符串参与加法、truthiness、短路返回值、数组容量与 `new Array(n)` 语义、对象属性和闭包循环。
- 模块编译：相对路径、菱形依赖、重复根、并行依赖图、循环依赖、错误聚合、取消、并发 build。
- 编译后端计划：direct call、函数注解、slot/upvalue 绑定、lowering、控制流、常量折叠和 runtime helper 调用计划。
- 编译模式：Dynamic、OnlyRun、Persistence 的行为一致性；net8 下 Persistence 限制。
- 运行时 API 和错误：未 Build 使用、缺失模块/方法、脚本堆栈、const 写入、`$state`、释放。
- 内置库：Math、String、Array、JSON、HashMap、Regex、StringBuffer、Console、Date、Proxy。
- CLR 互操作、序列化、ScriptDatum、热重载、并发运行、Release 回归。

运行测试：

```bash
dotnet test tests/AuroraScript.Tests.csproj
```

当前测试项目多目标到 `net8.0;net9.0;net10.0`。运行对应测试需要本机安装相应 .NET runtime。

## Benchmark

统一 Benchmark 项目位于 `benchmark/`，包含运行时指标和编译器 pipeline 指标。

快速 smoke：

```bash
dotnet run --project benchmark/Benchmark.csproj -c Release -- --smoke
```

输出简易 CSV 对比：

```bash
dotnet run --project benchmark/Benchmark.csproj -c Release -- --compare
```

运行 BenchmarkDotNet：

```bash
dotnet run --project benchmark/Benchmark.csproj -c Release
```

当前重点指标包括：

- Domain 创建、空调用、函数调用、模块调用、闭包调用
- 对象、数组、HashMap、字符串、JSON、Regex、CLR 互操作
- Lexer、Parser、Emitter、单模块/多模块编译、CompileBlock

最近一次摘要结果来自 2026-06-22 在 Release/net10.0 下执行的快速对比命令：

```bash
dotnet run --project benchmark/Benchmark.csproj -c Release -- --compare
```

旧的 `ScriptBenchmark` 报告是历史文件，已不作为当前指标参考。完整 BenchmarkDotNet 报告可通过不带 `--compare` 的 benchmark 命令重新生成。

测试环境：

- BenchmarkDotNet `0.15.8`
- Windows 11 `10.0.26200.8655`
- Intel Core i7-13700KF
- .NET SDK `10.0.301`
- Runtime `.NET 10.0.9`
- Job `ShortRun`

运行时核心结果：

| 指标 | 规模 | Mean | Allocated | 观察 |
|---|---:|---:|---:|---|
| `EmptyCall` | 1 call | 0.001 ms | 279 B | 宿主到脚本空调用开销低 |
| `CreateDomain` | 1 domain | 0.017 ms | 5.55 KB | Domain 创建较轻量 |
| `NumericLoop` | 1,000 | 0.009 ms | 411 B | 数值循环接近零分配 |
| `FunctionCallLoop` | 1,000 | 0.302 ms | 327 B | 局部函数调用分配很低 |
| `ModuleCallLoop` | 1,000 | 0.364 ms | 327 B | 模块调用略慢于局部调用 |
| `ClosureInvoke` | 1,000 | 0.067 ms | 487 B | 闭包调用分配稳定 |
| `ObjectCreateSetGet` | 1,000 | 0.611 ms | 195.72 KB | 对象创建/属性写入线性分配 |
| `ArrayPushIndex` | 1,000 | 0.288 ms | 48.49 KB | 数组 push/index 路径分配较低 |
| `ArrayLiteralIndex` | 1,000 | 0.119 ms | 172.15 KB | 数组字面量会按次数分配数组对象 |
| `HashMapSetGet` | 1,000 | 0.781 ms | 199.77 KB | 已使用容量构造，主要成本来自动态字符串 key |
| `JsonStringify` | 1,000 | 1.821 ms | 774.43 KB | JSON 序列化分配较高 |
| `JsonParse` | 1,000 | 7.606 ms | 875.43 KB | JSON 解析仍是较重路径 |
| `JsonRoundTrip` | 1,000 | 9.930 ms | 1.61 MB | parse + stringify 组合成本较高 |
| `RegexMatchAll` | 1,000 | 5.697 ms | 3.34 MB | 当前最重的常规运行时路径之一 |
| `StringBufferAppend` | 1,000 | 0.369 ms | 39.35 KB | 明显优于直接字符串拼接 |
| `StringConcat` | 1,000 | 0.512 ms | 3.73 MB | 直接字符串拼接分配很高 |
| `ClrPropertyGetSet` | 1,000 | 0.100 ms | 23.87 KB | CLR 属性访问较轻量 |
| `ClrArrayArgument` | 1,000 | 0.884 ms | 250.39 KB | 剩余分配主要来自脚本数组字面量和必要的 CLR 数组 |
| `ClrInstanceMethod` | 1,000 | 0.177 ms | 23.88 KB | DynamicMethod invoker 后实例方法开销明显降低 |
| `ClrStaticMethod` | 1,000 | 0.560 ms | 31.61 KB | 静态方法调用包装已降低，主要剩余为返回字符串包装 |

编译器 pipeline 结果：

| 指标 | Mean | Allocated | 观察 |
|---|---:|---:|---|
| `CompileBlock` | 0.047 ms | 18.35 KB | 小段脚本编译开销较低 |
| `FullCompile_MultiModule` | 0.281 ms | 65.63 KB | 当前多模块样例较小，结果健康 |
| `FullCompile_SingleModule` | 10.413 ms | 2.81 MB | 大模块完整编译主要成本 |
| `EmitOnly_ParsedLargeModule` | 4.598 ms | 1.26 MB | Emitter 是大模块编译主要热点 |
| `LexerOnly_Large` | 2.640 ms | 21.49 KB | 大源码词法阶段分配较低 |
| `ParseOnly_Large` | 5.421 ms | 1.53 MB | AST 构建带来主要分配 |
| `ParseOnly_TemplateInterpolation` | 0.949 ms | 413.08 KB | 模板插值解析分配偏高 |

异常点分析：

- `StringConcat` 的 1,000 次场景分配约 `3.73 MB`，属于预期但非常重的用法问题；性能敏感场景应使用 `StringBuffer`。
- `RegexMatchAll` 每 1,000 次分配约 `3.34 MB`，说明当前 match 结果对象构造成本高，适合后续优化结果数组、capture/group 对象分配。
- JSON round-trip 每 1,000 次分配约 `1.61 MB`，解析和序列化仍是运行时分配热点。
- `HashMapSetGet` 已移除字典扩容作为主要变量；当前剩余成本主要来自 benchmark 中 `"k" + i` 的动态字符串 key 构造。
- CLR 互操作已通过 DynamicMethod invoker 和数组转换快路径降低调用包装成本；`ClrArrayArgument` 的剩余分配主要来自脚本数组字面量和必要的 CLR 数组创建。
- 编译器侧 `ParseOnly_TemplateInterpolation` 分配相对源码规模偏高，模板解析可作为专项优化点。

## 示例

- [examples/tests/main.as](examples/tests/main.as)：模块加载和脚本入口示例。
- [examples/tests/unit.as](examples/tests/unit.as)：内置类型、标准库和语言特性示例。
- [tests/AuroraScript.Tests](tests/AuroraScript.Tests)：推荐作为行为规格参考。
- [benchmark/scripts/runtime.as](benchmark/scripts/runtime.as)：运行时性能指标脚本。

## VS Code 插件

`vscode-extension` 目录包含 VS Code 插件工程，当前主要提供语法高亮和基础编辑体验。可进入该目录执行：

```bash
npm install
npm run package
```

然后安装生成的 `.vsix`。

---

Made by [l2060](https://github.com/l2060)
