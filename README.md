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

热修复由 `EngineOptions.EnableHotReload` 控制，默认启用。关闭后会禁用 `DynamicPatch` 和脚本侧 `HotPatch`，并允许编译器使用更激进的模块内直接调用优化。

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
- 补丁源码中的顶层代码会在应用补丁时执行。

## 语言能力

当前测试覆盖和示例中使用的语法包括：

- `var` / `const` / `func` / `function` / `return`
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

当前 `net10.0` test discovery 共发现 **282** 个测试用例。按测试类统计：

| 测试类 | 用例数 | 覆盖重点 |
|---|---:|---|
| `LexerTests` | 37 | 词法、数字/字符串/正则、注释、错误 token |
| `ParserSyntaxTests` | 79 | 语法分支、模块声明、import/include/export、错误语法诊断 |
| `ExpressionExecutionTests` | 35 | 表达式、运算符、成员/索引访问、spread、赋值 |
| `StatementExecutionTests` | 7 | 控制流、循环、闭包、递归、异常、Domain 隔离 |
| `LanguageFeatureExecutionTests` | 16 | enum、Lambda、稀疏数组、truthiness、模板、赋值语义 |
| `ModuleCompilationTests` | 12 | 模块依赖、并行编译、循环依赖、错误聚合、取消 |
| `CompileBlockTests` | 21 | CompileBlock 参数、调用方式、错误输入和诊断 |
| `CompilationModeTests` | 5 | Dynamic/OnlyRun/Persistence 行为和热重载开关 |
| `RuntimeApiAndErrorTests` | 9 | 运行时 API、错误路径、`$state`、释放后行为 |
| `BuiltInLibraryTests` | 8 | Math、String、Array、JSON、HashMap、Regex、StringBuffer、Console |
| `AdvancedRuntimeTypeTests` | 6 | 构造器、Object、freeze、Date、Proxy |
| `ClrInteropTests` | 5 | CLR 构造/属性/字段/方法/重载/访问限制 |
| `SerializationTests` | 9 | JSON 序列化/反序列化、循环引用、异常 JSON |
| `ScriptDatumTests` | 4 | Datum payload、相等性、CLR 集合转换、Span helper |
| `HotReloadTests` | 4 | 热重载禁用、增量补丁、替换补丁、Domain 隔离 |
| `ConcurrentRuntimeTests` | 3 | 同域/多域并发、detached closure 并发 |
| `ReleaseRegressionTests` | 9 | Release 直连调用、闭包槽位、栈平衡、混淆、空模块 |
| `ClosureFunctionContextTests` | 3 | 上下文池生命周期和 detached 调用 |
| `EngineOptionsAndSourceTests` | 10 | EngineOptions、Source 路径、扩展名、并行度、空输入 |

覆盖范围摘要：

- Lexer：关键字、标识符、Unicode、运算符、数字、字符串、正则、注释、CRLF 位置、错误 token。
- Parser：模块元数据、import/include/export、声明、表达式、Lambda、解构、控制流、异常、模板、正则、错误诊断。
- CompileBlock：参数校验、局部函数、domain/no-domain 调用、模块语法拒绝、source name、空输入。
- 表达式/语句：优先级、算术、位运算、比较、逻辑、成员访问、spread、赋值、循环、异常、闭包、递归。
- 模块编译：相对路径、菱形依赖、重复根、并行依赖图、循环依赖、错误聚合、取消、并发 build。
- 编译模式：Dynamic、OnlyRun、Persistence 的行为一致性；net8 下 Persistence 限制。
- 运行时 API 和错误：未 Build 使用、缺失模块/方法、脚本堆栈、const 写入、`$state`、释放。
- 内置库：Math、String、Array、JSON、HashMap、Regex、StringBuffer、Console、Date、Proxy。
- CLR 互操作、序列化、ScriptDatum、热重载、并发运行、Release 回归。

运行测试：

```bash
dotnet test tests/AuroraScript.Tests/AuroraScript.Tests.csproj
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

最近一次结果来自 `benchmark/bin/Release/net10.0/BenchmarkDotNet.Artifacts/results/` 下 2026-06-22 生成的 `RuntimeBenchmarks` 和 `CompilerPipelineBenchmarks` 报告。旧的 `ScriptBenchmark` 报告是历史文件，已不作为当前指标参考。

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
| `EmptyCall` | 1 call | 152 ns | 0 B | 宿主到脚本空调用开销低且无分配 |
| `CreateDomain` | 1 domain | 2.8 us | 5.32 KB | Domain 创建较轻量 |
| `NumericLoop` | 10,000 | 78.0 us | 48 B | 数值循环接近零分配 |
| `FunctionCallLoop` | 10,000 | 1.10 ms | 48 B | 局部函数调用基本无分配 |
| `ModuleCallLoop` | 10,000 | 1.32 ms | 48 B | 模块调用比局部调用慢约 20% |
| `ClosureInvoke` | 10,000 | 660 us | 208 B | 闭包调用分配稳定 |
| `ObjectCreateSetGet` | 10,000 | 1.97 ms | 1.91 MB | 对象创建/属性写入有线性分配 |
| `ArrayPushIndex` | 10,000 | 930 us | 768 KB | 出现 Gen2，需关注数组增长/大对象路径 |
| `HashMapSetGet` | 10,000 | 7.10 ms | 4.51 MB | 分配偏高，且触发 Gen2 |
| `JsonStringify` | 10,000 | 7.11 ms | 7.55 MB | JSON 序列化分配较高 |
| `JsonParse` | 10,000 | 10.76 ms | 8.54 MB | JSON 解析分配较高 |
| `RegexMatchAll` | 10,000 | 30.67 ms | 32.58 MB | 当前最重的常规运行时路径之一 |
| `StringBufferAppend` | 10,000 | 1.33 ms | 408 KB | 明显优于直接字符串拼接 |
| `StringConcat` | 10,000 | 55.71 ms | 457.79 MB | 异常重，展示了应优先使用 `StringBuffer` 的场景 |
| `ClrPropertyGetSet` | 10,000 | 592 us | 234 KB | CLR 属性访问相对健康 |
| `ClrArrayArgument` | 10,000 | 6.89 ms | 4.04 MB | 数组参数转换仍有较高分配 |
| `ClrInstanceMethod` | 10,000 | 8.31 ms | 2.90 MB | 实例方法调用仍有优化空间 |
| `ClrStaticMethod` | 10,000 | 11.59 ms | 4.04 MB | 静态方法绑定/参数转换是明显热点 |

编译器 pipeline 结果：

| 指标 | Mean | Allocated | 观察 |
|---|---:|---:|---|
| `CompileBlock` | 28.8 us | 17.85 KB | 小段脚本编译开销较低 |
| `FullCompile_MultiModule` | 358 us | 64.5 KB | 当前多模块样例较小，结果健康 |
| `FullCompile_SingleModule` | 7.25 ms | 2.78 MB | 大模块完整编译主要成本 |
| `EmitOnly_ParsedLargeModule` | 4.31 ms | 1.24 MB | Emitter 是大模块编译主要热点 |
| `LexerOnly_Large` | 579 us | 21.26 KB | 大源码词法阶段分配较低 |
| `ParseOnly_Large` | 845 us | 1.53 MB | AST 构建带来主要分配 |
| `ParseOnly_TemplateInterpolation` | 166 us | 412.59 KB | 模板插值解析分配偏高 |

异常点分析：

- `StringConcat` 的 10,000 次场景分配约 `457.79 MB`，属于预期但非常重的用法问题；性能敏感场景应使用 `StringBuffer`。
- `RegexMatchAll` 每 10,000 次分配约 `32.58 MB`，说明当前 match 结果对象构造成本高，适合后续优化结果数组、capture/group 对象分配。
- CLR 互操作中 `ClrStaticMethod`、`ClrArrayArgument` 分配约 `4.04 MB/10,000`，静态调用和脚本数组到 CLR 数组转换仍是热点。
- `HashMapSetGet` 10,000 次触发 Gen2，可能来自字符串 key 构造和字典扩容，应作为集合路径的后续优化点。
- `ArrayPushIndex` 10,000 次也出现 Gen2，建议后续检查数组扩容策略和 benchmark 是否应预设容量。
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
