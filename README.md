<p align="center">
  <img src="icon.png" width="128" alt="AuroraScript Logo" />
</p>

<p align="center">
  <a href="./README.md">简体中文</a> | <a href="./README_EN.md">English</a>
</p>

# AuroraScript

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-3.0.1-orange.svg)](src/AuroraScript.csproj)
[![Target](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blueviolet.svg)](src/AuroraScript.csproj)

AuroraScript 是一个面向 .NET 宿主程序的轻量脚本引擎。脚本会被编译为 CIL，并通过 .NET 运行时执行，适合把规则、业务逻辑、配置化流程、热修复逻辑和小型表达式嵌入到 C# 应用中。

AuroraScript 的语法借鉴了 JavaScript 的表达式、对象、数组、闭包和模块写法，但它不是 ECMAScript 实现，也不承诺兼容浏览器或 Node.js 语义。README 中列出的能力均以当前源码和测试覆盖为准。

> [!NOTE]
> 项目仍在持续开发中。公开 API、性能和语义边界仍可能调整；用于生产前建议固定 NuGet 版本并跑自己的回归测试。

## 支持平台

NuGet 包名：`AuroraScript.JIT`

当前 `3.0.1` 包发布为多目标框架：

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
- **内置标准对象**：包含 `Object`、`Array`、`Int32Array`、`Float64Array`、`Int8Array`、`BooleanArray`、`String`、`Date`、`Regex`、`HashMap`、`StringBuffer`、`Path`、`JSON`、`Math`、`console`、`Proxy`、`HotPatch`。
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
using AuroraScript.Core;
using AuroraScript.Runtime;

var options = EngineOptions.Default
    .WithCompiler(compiler =>
    {
        compiler.SourceResolver = ScriptSources.FileSystem("./scripts");
        compiler.Mode = CompilationMode.Dynamic;
    })
    .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release);

var engine = new AuroraEngine(options);
engine.RegisterType(typeof(Math), "Math2");

await engine.BuildAsync();

var domain = engine.CreateDomain();
var result = domain.Execute("MAIN", "main", ScriptDatum.FromNumber(20));
Console.WriteLine(result);
```

> 3.0 变更：脚本输入不再通过 `compiler.Directory`、`BaseDirectory` 或 `SearchAllFileSource` 配置。请通过 `compiler.SourceResolver` 提供脚本来源，并使用 `BuildAsync()`、`BuildAsync("main.as")` 或 `BuildAsync(params ScriptSource[])` 编译。

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

宿主注入的 `global` 成员不要求写 `@global()` 声明；没有声明时，运行时仍会从 domain 的 `global` 对象读取这些值。`@global()` 的作用是给宿主侧 API 提供可选的编译期契约，让编辑器获得语义着色、跳转定义和更好的静态诊断。它类似 TypeScript 的 `.d.ts`：只提供符号契约，不产生运行时代码。

`globals.as`：

```javascript
@global();

declare func HOST_ADD(left, right);
declare const HOST_NAME;
declare var HOST_COUNT;
```

普通模块中可以直接使用这些全局符号：

```javascript
@module(MAIN);

export func run() {
    return HOST_NAME + ":" + HOST_ADD(20, 22);
}
```

`@global()` 文件规则：

- `@global();` 必须是第一条有效语句；它前面只能有空行或注释。
- `@global()` 不能与 `@module` 同时存在。
- `@global()` 文件只允许 `declare` 声明，不能写 `export declare`。
- `declare` 不能写在 `@module` 文件中。
- `@global()` 文件不能被 `import` / `include`，也不会被编译为模块。
- 一个项目可以有多个 `@global()` 文件，但全局名称不能重复；函数不支持重载。

## Script Source Resolver

3.0 起，脚本加载统一通过 `IScriptSourceResolver` 扩展。Resolver 只负责三件事：把 import/include 的原始路径重定位为稳定引用、按引用读取源码、枚举所有源码。源码是否已经打开、来自文件系统、内存、数据库、对象存储或虚拟目录，都由 resolver 自己决定。

常用工厂位于 `AuroraScript.Core.ScriptSources`：

```csharp
using AuroraScript.Core;

var options = EngineOptions.Default.WithCompiler(compiler =>
{
    compiler.SourceResolver = ScriptSources.FileSystem("./scripts");
});
```

`BuildAsync()` 会从当前 resolver 的 `GetAllSourcesAsync` 枚举所有脚本；`BuildAsync("main.as")` 会先通过 `ResolveAsync(null, "main.as", ...)` 找到入口，再由模块图构建过程解析它的 `import` / `include` 依赖：

```csharp
await engine.BuildAsync();          // 编译 resolver 暴露的所有脚本
await engine.BuildAsync("main.as"); // 只从入口和依赖构建模块图
```

内存源码适合测试、动态生成脚本或插件脚本：

```csharp
var memory = ScriptSources.Memory("mem://app/")
    .Add("main.as", """
        @module(MAIN);
        import lib from './lib';
        export func run() { return lib.value; }
        """)
    .Add("lib.as", """
        @module(LIB);
        export const value = 42;
        """);

var engine = new AuroraEngine(
    EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = memory));

await engine.BuildAsync("main.as");
```

需要在文件系统基础上叠加内存脚本时，使用 `Composite`。顺序越靠前优先级越高；只要 import/include 解析后的目标路径落在前面 resolver 的 root 范围内，就可以被前面的 resolver 覆盖。不同协议或不相交 root 仍是隔离的脚本命名空间。脚本路径建议统一使用 `/`：

```csharp
var scriptRoot = Path.GetFullPath("./scripts");

var resolver = ScriptSources.Composite(
    ScriptSources.Memory(scriptRoot)
        .Add("generated.as", "@module(GEN); export const value = 42;"),
    ScriptSources.FileSystem(scriptRoot));

var options = EngineOptions.Default.WithCompiler(compiler =>
{
    compiler.SourceResolver = resolver;
});
```

如果 `Memory("d:/a/b/c")` 放在 `FileSystem("d:/a/b/c/d")` 前面，文件系统脚本中的 `import '../test'` 会先解析成 `d:/a/b/c/test.as`，因此可以由父级 Memory root 覆盖。自定义来源只需要实现 `IScriptSourceResolver`。`ResolveAsync` 应保留导入者上下文，让 `import lib from './lib';` 和 `include '../shared';` 相对导入者自身路径解析，而不是相对某个全局编译目录；随后再判断解析后的目标路径是否属于当前 resolver 的 root。

Resolver 规则：

- Parser 只保留脚本里写的原始路径；模块图构建阶段负责调用 `ResolveAsync`。
- `ResolveAsync(null, entryPath, ...)` 从 resolver 的 `Root` 解析入口；依赖解析从 `importer.FullPath` 所在目录解析。
- `Composite` 按 resolver 顺序解析，谁先返回引用谁优先，不需要额外的 `ProviderId`。
- `MemorySourceResolver` 只解析落在自身 root 内且已添加的源码；因此只有目标路径落在 Memory root 下时才能覆盖后面的 resolver。
- `FileSystemScriptSourceResolver` 的 `BuildAsync()` 枚举 root 下文件；依赖解析会跟随导入者路径，文件存在时可解析到 root 外，返回引用仍由该 FileSystem resolver 读取。
- `GetSourceAsync` 按 `ScriptSourceReference.BaseDirectory` 精确路由到对应 resolver，不会重新按目标路径搜索。
- `BuildAsync()` 通过 `GetAllSourcesAsync` 枚举源码；`Composite` 按规范化后的 `FullPath` 去重，前面的源码覆盖后面的同路径源码。
- 编译器会在模块分析前扫描 resolver 可见的项目 `.as` 文件；如果存在 `@global()` 声明文件，会加载这些可选声明。它们不依赖 `import` / `include`。
- Resolver 内部路径应在构建/添加源码时规范化，并统一使用 `/` 分隔，避免每次比较时重复归一化。

## CompileBlock

`CompileBlock` 会把源码当作匿名函数体编译，不创建模块，也不参与热重载。它适合公式、规则、过滤器和需要频繁调用的小段逻辑。

`CompileBlock` 只接受语句体，不支持 `@module`、`@global()`、`import`、`include`、`export` 或 `declare`。

```csharp
var engine = new AuroraEngine(EngineOptions.Default);

var block = engine.CompileBlock("""
func clamp(v, min, max) {
    if (v < min) return min;
    if (v > max) return max;
    return v;
}

return clamp(x, 0, 100);
""", ["x"]);

var result = block.Invoke(ScriptDatum.FromNumber(125));
```

参数名可以通过 `CompileBlock(source, string[] parameters)` 直接声明，运行时按位置传入。需要指定诊断源码名时，使用 `CompileBlockOptions.SourceName`。参数名不能为空、不能重复，且不能使用 `global`、`$args`、`$state`。

`CompiledBlock` 无引用后会通过 GC finalizer 自动释放编译期间注册的动态 delegate；需要确定性释放时也可以显式调用 `Dispose()`。

## 编译模式

| 模式 | 说明 | 适用场景 |
|---|---|---|
| `Dynamic` | 使用 `DynamicMethod` 发射 CIL，不产生可持久化程序集 | 高性能运行、规则执行、小型脚本 |
| `OnlyRun` | 使用可回收动态程序集在内存中运行 | 需要更容易被运行时工具观察的内存执行 |
| `Persistence` | 生成可保存的 DLL，并可带调试符号 | 调试、诊断、需要落盘程序集的场景 |

限制：

- `Persistence` 需要 `net9.0` 或更高版本；`net8.0` 下调用会抛出 `PlatformNotSupportedException`。
- `Persistence` 在 `Output.AssemblyFile` 为空时不会写 DLL，但仍会序列化为内存 PE 并通过 `Assembly.Load(byte[])` 加载，以保留 Debug 模式下的 embedded PDB/sequence point 语义。
- Visual Studio 源码级脚本调试依赖 `Persistence` 模式和 Debug 优化设置；`OnlyRun` 是可回收动态程序集运行模式，不提供同等源码级调试符号。

## 热修复

热修复由 `EngineOptions.Runtime.EnableHotReload` 控制，默认启用。关闭后会禁用宿主侧 `DynamicPatch` 和脚本侧 `HotPatch`。

宿主侧：

```csharp
var mainPath = Path.GetFullPath(Path.Combine(scriptRoot, "main.as"));
domain.ReplacePatch(mainPath, "@module(MAIN); export func run() { return 42; }");
domain.IncrementalPatch(mainPath, "export func added() { return 1; }");
```

宿主侧字符串重载要求传入绝对文件路径或虚拟 full path。路径必须落在当前 `SourceResolver` 的某个 root 下；`Composite` 会选择最长匹配 root。这样补丁里的 `import` / `include` 会继续按同一个 resolver 命名空间解析依赖，不会被放进错误的独立空间。

脚本侧：

```javascript
HotPatch.replace("@module(MAIN); export func run() { return 42; }");
HotPatch.incremental("./main", "export func added() { return 1; }");
```

脚本侧只传 `script` 时会修补当前模块；传入相对 `modulePath` 时，会从当前模块的 `FullPath` 所在目录解析。

注意：

- `Replace` 会清空目标模块现有成员后应用新代码。
- `Incremental` 会保留现有成员，并添加或更新补丁中声明的成员。
- 是否使用 `Replace` 或 `Incremental` 由宿主或脚本作者根据补丁范围决定；修改一组互相调用的函数时，通常更适合使用 `Replace` 保持模块内容一致。
- 补丁源码中的顶层代码会在应用补丁时执行。

## 模块与函数注解

模块名通过 `@module(NAME);` 声明。`@module` 必须出现在模块的第一条有效语句位置；它前面只能有空白或注释。

`@module` 文件是可编译模块，允许顶层 `import` / `include` / `export`。`@global()` 文件是全局声明文件，只用于编译期符号辅助，不能与 `@module` 混用，也不能被导入或包含。

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

宿主侧可以通过 `EngineOptions.WithOptimization(optimization => optimization.EnableAutoModuleDirectCall = true)` 开启自动推断。显式 `@directCall` 不依赖该选项；即使没有开启自动推断，被标记的函数仍会按可优化规则尝试使用直接调用。

适用建议：

- `@directCall` 适合参数固定、逻辑稳定、被模块内高频调用的普通函数。
- 使用默认参数、`$args`、捕获外部变量等动态调用形态时，编译器会继续使用普通函数调用路径。
- 被 `@directCall` 标记的函数名不应在模块内被重新赋值。

### directCall 的调用路径优化

`@directCall` 优化的是“调用一个已知模块函数”的路径，不是函数内联。目标仍是独立的 CIL 方法，异常堆栈和返回值语义保持不变。调用会在当前 `ScriptContext` 上使用轻量帧，不再分配子上下文；若整个调用图被证明为纯数值路径，还会直接调用使用原生 `int`、`double` 和 `bool` 签名的方法，图内不物化运行时值。

普通的模块函数调用大致会走下面的路径：

1. 读取函数名对应的模块成员，例如 `addOne` 会从当前 `ScriptContext.Module` 上按字符串 key 读取 `addOne`。
2. 模块成员读取会进入 `ScriptObject.GetPropertyDatum`：查 hidden class/property meta、检查属性描述符、必要时处理 getter、CLR 绑定函数、原型链或 CLR fallback。
3. 调用点把读取到的值转换成可调用对象。
4. 通过 `CallOps.Invoke0..Invoke7` 或 `InvokeMany` 进入紧凑调用边界。
5. helper 判断目标是不是 `ClosureFunction`，再进入 `ClosureFunction.Invoke*`。
6. `ClosureFunction` 在当前上下文上压入轻量帧，按参数个数选择固定参数 delegate 或 span delegate，调用后恢复帧。

开启 direct call 后，符合条件的调用点会改成更短的路径：

1. 编译期确认 `addOne(value)` 的目标就是同一模块里的某个函数，并把调用点绑定到该函数的 `FunctionId` / IL method。
2. 调用时仍按源码顺序计算参数；固定参数会放进临时 local，缺失参数补 `null`，多余参数会被求值后丢弃。
3. 通用 direct adapter 只在当前上下文上使用轻量帧，用于调用栈和异常定位，不分配子 `ScriptContext`。
4. 发射的 IL 直接 `call` 目标方法；若编译器证明函数签名和函数体是纯数值路径，参数、local、算术、比较和返回值都保持为原生 CIL 值（`int` / `double` / `bool`），直到确实需要动态边界。可证明安全的 32 位整数循环、位运算和专业数组下标值使用 `int`；需要 JavaScript number 语义或可能溢出时使用 `double`。

因此 direct call 主要省掉了：模块属性读取、函数对象转换、`CallOps.Invoke*` 分发、`ClosureFunction.Invoke*` 分发、delegate arity switch，以及动态路径上的许多分支。通用 direct call 保留轻量帧；编译调用图内部已证明安全的原生数值调用既不经过动态分发，也不转换 `ScriptDatum`。在循环里反复调用小型数值函数时收益最明显。

能够走 direct call 的调用点需要满足这些条件：

- 调用形式必须是同模块内的静态名字调用，例如 `addOne(x)`；`obj.addOne(x)`、`module.addOne(x)`、`var f = addOne; f(x)`、跨模块 import 调用都不会走这条路径。
- 调用参数不能使用 spread，例如 `addOne(...items)` 会回退到普通调用。
- 轻量通用 direct adapter 和自动 direct-call 推断当前覆盖不超过 7 个参数的函数。显式标记的 `@directCall` 函数可以超过 7 个参数；当调用图能证明其参数和函数体可原生专业化时，同模块静态调用点会直接调用原生方法，否则保留普通 closure/span 调用回退。默认参数、`$args`、闭包捕获或被内部子函数捕获的局部变量仍会阻止这条原生路径。
- 显式 `@directCall` 会保留模块上的函数对象，因此函数仍可被导出、读取或由宿主调用；它只是让模块内可证明的调用点使用直接路径。
- 自动推断只在 `EngineOptions.WithOptimization(optimization => optimization.EnableAutoModuleDirectCall = true)` 开启后生效，并且只会优化没有被赋值、没有被当作值读取、只作为直接调用目标出现的模块内函数。
- direct call 不会跨模块优化，也不会因为一个函数被标记就强制所有调用点都优化；不符合条件的调用点仍走普通路径。

### local 变量和 module 成员的访问成本

函数参数、函数体内 `var` / `const`、函数体内声明的局部函数都会被后端绑定到 local slot。能够证明类型的 number、Boolean 和 string 分别使用 CLR 原生 `double`、`bool` 和 `string` slot，只有动态值才使用 `ScriptDatum`。读取和写入是直接的 `ldloc` / `stloc`，只在对象、调用、作用域等动态运行时边界才进行转换。

模块顶层声明则不同。顶层 `var`、`const`、`func`、`enum` 和 import/export 相关符号都属于模块对象的成员。函数里访问一个模块成员时，编译器不能把它简单当作 local，因为模块对象是可观察的运行时对象：宿主可以 `GetModule` / `Execute`，脚本可以 import/export，热补丁可以替换模块成员，属性也可能带 getter/setter 或被重新定义。因此访问模块成员时通常要从 `ScriptContext.Module` 出发，按名字调用 `ScriptObject.GetPropertyDatum` 或 `SetPropertyDatum`。

这就是 local 明显更快的原因：

- local 是编译期确定的槽位编号，运行时直接按 slot 读写。
- module 成员是运行时属性，必须按字符串名字解析 property meta。
- module 读取需要处理属性描述符、getter、绑定函数、原型链和 CLR fallback 等动态语义。
- module 写入需要检查 setter、可写性和 hidden class/property slot 更新。
- 在循环中重复访问 module 成员会重复承担这些动态检查；local 不会。

性能敏感代码建议把循环计数器、累加器和重复读取的模块常量放进方法内 local：

```javascript
@module(MAIN);

const scale = 3;

export func fastSum(items) {
    var localScale = scale;
    var total = 0;

    for (var item in items) {
        total = total + item * localScale;
    }

    return total;
}
```

如果需要更新模块状态，可以在循环前读入 local，循环结束后再写回模块成员。这样只承担一次模块读写成本：

```javascript
@module(MAIN);

var counter = 0;

export func addMany(items) {
    var value = counter;

    for (var item in items) {
        value = value + item;
    }

    counter = value;
    return value;
}
```

闭包捕获的变量介于两者之间：它不再是单纯 `ldloc` / `stloc`，而是通过 upvalue 对象保存，但仍然避免了模块对象的字符串属性查找。热路径里优先使用参数和普通 local，其次是必要的闭包变量；模块成员更适合作为跨函数共享状态、导出 API 或宿主可观察数据。

### 模块级 const 的编译期内联

模块级 `const` 默认仍按模块成员处理；编译期内联需要显式开启 `EngineOptions.WithOptimization(optimization => optimization.EnableModuleConstInlining = true)`。这个开关独立于 `Runtime.EnableHotReload`：热更新是否开启不会自动禁止或启用该优化，最终只由这个编译参数决定。

开启后，编译器会在同一个模块内按源码顺序分析顶层 `const` / `export const`。如果初始化表达式能在编译期证明为无副作用的 primitive 值，后续同模块函数体里的读取会被替换成 literal，常量自身的模块初始化也会直接写入折叠后的值：

```javascript
@module(TEST);

export const a1 = 1;
export const a5 = a1 + 4; // 编译为 a5 = 5

export func test() {
    console.log(a5); // 编译为 console.log(5)
}
```

更复杂的 primitive 表达式也会按顺序折叠：

```javascript
export const NUM = 3.141592678987654321;
export const STR = 'this is string';
export const BOOL = true;
export const BASE = 10;
export const COMPLEX = BASE * NUM + 5;       // 36.41592678987654
export const TAG = BASE + '_' + 1;           // '10_1'
export const TEMPLATE = STR + BASE + '_' + TAG; // 'this is string10_10_1'
```

开启该参数后，模块初始化代码也应该直接写入折叠值，例如 `COMPLEX` 应发射为类似 `ScriptDatum.FromNumber(36.41592678987654)`，而不是重新执行 `GetPropertyDatum("BASE") * GetPropertyDatum("NUM") + 5`。

当前可内联的值和表达式包括：`null`、boolean、number、string，以及由更早声明的同模块可内联 const 组成的 `+`、`-`、`*`、`/`、`%`、`!`、`&&`、`||` 和括号表达式。折叠时会尽量沿用运行时 helper 的语义，例如字符串参与 `+` 时做拼接，`null` 在数值运算里按 `0` 处理，逻辑表达式保留短路返回值语义。

下面这些情况不会内联：

- 初始化表达式包含函数调用、构造、属性读取、下标读取、数组/对象字面量、lambda、赋值或其它可能有副作用的表达式。
- `export const fv = func();` 不会内联，因为 `func()` 的结果只能在运行时知道，而且调用本身可能有副作用。
- 前向引用不会内联，例如 `export const a5 = a1 + 4; export const a1 = 1;` 中的 `a5` 不会被折叠。
- import/include 得到的外部模块成员不会跨模块内联。
- 局部变量、参数或 upvalue 与模块 const 同名时，局部绑定优先，不会替换成模块常量。

这个优化只改变可证明常量的读取路径，不会删除模块成员本身。`export const` 仍会定义在模块对象上，宿主和其它模块仍可按原语义访问。但已经内联到同模块函数体里的读取不再走模块属性查询，因此不会观察到后续由宿主或热补丁强行替换该 const 成员后的值；需要这种动态可观察性时不要开启该优化。

## 语言能力

当前测试覆盖和示例中使用的语法包括：

- `var` / `const` / `func` / `function` / `return`
- `@module`、`@global()`、`@directCall`
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
- `Int32Array`
- `Float64Array`
- `Int8Array`
- `BooleanArray`
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
- `Path`
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

普通 `Array` 仍支持异构元素、动态属性和扩容。只要具体数组类型没有经过对象属性等动态边界，编译器会让 local 和 direct-call 参数保持为 `ScriptArray`，并为 `length`、数值下标读写和未覆写的 `push` 发射直接调用。若脚本给实例定义了自己的 `push`，调用会可靠地回退到动态语义。

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

### Int32Array / Float64Array / Int8Array / BooleanArray

专业数组使用 CLR 原生连续存储，适合寻路、图像、状态表和数值循环：

```as
var scores = new Int32Array(1000000);
var weights = new Float64Array(1000000);
var costs = new Int8Array(1000000);
var visited = new BooleanArray(1000000);

scores[10] = 42;
weights[10] = 1.25;
visited[10] = true;
scores.fill(0);
```

- 长度固定，创建后数值数组以 `0`、布尔数组以 `false` 初始化。
- 支持数值下标读写、只读 `length` 和 `fill(value)`；不支持 `push`、`pop` 或删除元素。
- 越界访问会抛出运行时异常；`Array.isArray(value)` 对专业数组返回 `false`。
- 编译器能确认具体数组类型时，下标访问直接发射 CLR 数组 CIL，数值仅在进入动态对象、脚本调用或宿主边界时转换为 `ScriptDatum`。
- 在原生 direct 调用图内，专业数组参数和 local 直接使用 `int[]`、`double[]`、`sbyte[]`、`bool[]` backing storage；原生函数之间传递数组不会重复读取 wrapper 字段或重新包装数组。
- 为保持最快路径，应让专业数组保存在局部变量中，或作为 `@directCall` 热点函数的参数传递；从动态对象属性重新读取后仍保持正确语义，但会经过动态访问边界。
- `Int32Array` 适合确定为 32 位有符号整数的索引、距离和队列表；`Float64Array` 适合小数、`NaN`、无穷值或需要通用 number 语义的数据；`Int8Array` 适合紧凑的有符号小值；`BooleanArray` 适合标记表。

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

### Path

`Path` 是脚本侧协议感知的路径对象。它保存规范化后的路径文本，统一使用 `/` 分隔，支持普通文件路径和 `mem://app/main.as`、`asset://pkg/textures/ui.png` 这类协议路径。`Path` 是对象类型，`==` 会按规范化路径文本做值比较。

构造和静态成员：

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

实例成员：

- `append(...segments)`
- `reset(root, ...segments)`
- `changeExt(extension)`
- `directoryName()`
- `fileName()`
- `extName()`
- `protocol()`
- `clone()`
- `toString()`

`Path.baseModule(...segments)` 从当前模块文件所在目录开始拼接，适合脚本内部生成与当前模块相邻的资源路径。`Path.currentFile()` 和 `Path.currentDirectory()` 返回当前模块的完整路径和目录；如果当前执行上下文没有模块，则返回 `null`。

```javascript
@module(MAIN);

export func run() {
    var config = Path.changeExt(Path.baseModule("../assets", "config"), "as");
    var path = new Path("mem://app/scripts", "../shared", "main");
    path.changeExt("as");
    return [
        path.toString(),
        path.extName(),
        path.protocol(),
        config,
        Path.join(Path.currentDirectory(), "generated", "out.as")
    ];
}
```

### JSON

- `JSON.parse(text)`
- `JSON.stringify(value, [indented])`

JSON 支持脚本基础值、对象、普通数组、专业数组和 HashMap 等类型；循环引用会抛出运行时异常。

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

输出目标可通过 `EngineOptions.WithRuntime(runtime => { runtime.ConsoleStdOut = ...; runtime.ConsoleErrorOut = ...; })` 配置。

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

当前 `net10.0` 测试覆盖矩阵：

| 测试类 | 覆盖重点 |
|---|---|
| `LexerTests` | 词法、数字/字符串/正则、注释、错误 token |
| `ParserSyntaxTests` | 语法分支、模块声明、import/include/export、函数注解、错误语法诊断 |
| `ExpressionExecutionTests` | 表达式、运算符、成员/索引访问、spread、赋值 |
| `StatementExecutionTests` | 控制流、循环、闭包、递归、异常、Domain 隔离 |
| `LanguageFeatureExecutionTests` | enum、Lambda、稀疏数组、truthiness、模板、赋值语义 |
| `ModuleCompilationTests` | 模块依赖、并行编译、循环依赖、错误聚合、取消 |
| `CompilerBackendPlanTests` | typed code、原生 CIL slot/signature、direct call、闭包/upvalue、控制流和常量折叠 |
| `CompileBlockTests` | CompileBlock 参数、调用方式、错误输入、诊断和动态 delegate 生命周期 |
| `CompilationModeTests` | Dynamic/OnlyRun/Persistence 行为和热重载开关 |
| `RuntimeApiAndErrorTests` | 运行时 API、错误路径、`$state`、释放后行为 |
| `BuiltInLibraryTests` | Math、String、Array、JSON、HashMap、Regex、StringBuffer、Console |
| `AdvancedRuntimeTypeTests` | 构造器、Object、freeze、Date、Proxy |
| `ClrInteropTests` | CLR 构造/属性/字段/方法/重载/访问限制 |
| `SerializationTests` | JSON 序列化/反序列化、循环引用、异常 JSON |
| `ScriptDatumTests` | Datum payload、相等性、CLR 集合转换、Span helper |
| `ValueOpsTests` | 动态边界上的算术、比较、位运算和类型转换语义 |
| `HotReloadTests` | 热重载禁用、增量补丁、替换补丁、Domain 隔离 |
| `ConcurrentRuntimeTests` | 同域/多域并发、detached closure 并发 |
| `ReleaseRegressionTests` | Release 直连调用、闭包槽位、栈平衡、混淆、空模块 |
| `ClosureFunctionContextTests` | 上下文池生命周期和 detached 调用 |
| `EngineOptionsAndSourceTests` | EngineOptions、SourceResolver、扩展名、并行度、空输入 |
| `CustomSourceResolverUsageTests` | 自定义 resolver、虚拟路径、内存/文件组合和入口解析 |
| `CoreSemanticsRegressionTests` | 基础语义、null 加法、coercion、短路逻辑、数组容量/索引、对象属性、闭包/循环 |

覆盖范围摘要：

- Lexer：关键字、标识符、Unicode、运算符、数字、字符串、正则、注释、CRLF 位置、错误 token。
- Parser：模块元数据、import/include/export、声明、表达式、Lambda、解构、控制流、异常、模板、正则、错误诊断。
- CompileBlock：参数校验、局部函数、domain/no-domain 调用、模块语法拒绝、source name、空输入、动态 delegate 释放。
- 表达式/语句：优先级、算术、位运算、比较、逻辑、成员访问、spread、赋值、循环、异常、闭包、递归。
- 基础语义回归：null 数值加法、字符串参与加法、truthiness、短路返回值、数组容量与 `new Array(n)` 语义、对象属性和闭包循环。
- 模块编译：相对路径、菱形依赖、重复根、并行依赖图、循环依赖、错误聚合、取消、并发 build。
- 编译后端计划：typed code、函数注解、slot/upvalue 绑定、原生 CIL signature/local、控制流、常量折叠和 runtime 边界调用。
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

最新的运行时热路径结果来自 2026-08-15 在 Release/net10.0 下执行的 BenchmarkDotNet `ShortRun`：

```bash
dotnet run --project benchmark/Benchmark.csproj -c Release -- \
  --filter "*FunctionCallLoop*" "*ModuleCallLoop*" "*NumericLoop*"
```

旧架构的 benchmark 数字不再作为当前指标参考。完整指标可通过不带 `--filter` 的命令重新生成。

测试环境：

- BenchmarkDotNet `0.15.8`
- Windows 11 `10.0.28000.2704`
- Intel Core i7-13700KF
- .NET SDK `10.0.400`
- Runtime `.NET 10.0.11`
- Job `ShortRun`

运行时核心热路径结果：

| 指标 | 规模 | Mean | Allocated | 观察 |
|---|---:|---:|---:|---|
| `NumericLoop` | 1,000 | 1.477 µs | 0 B | 数值 local 和算术保持为原生 CIL |
| `NumericLoop` | 10,000 | 13.483 µs | 0 B | 时间线性增长，无循环内分配 |
| `FunctionCallLoop` | 1,000 | 82.668 µs | 0 B | 同模块数值调用使用原生 specialization |
| `FunctionCallLoop` | 10,000 | 766.803 µs | 0 B | 调用帧复用，无逐次上下文分配 |
| `ModuleCallLoop` | 1,000 | 122.591 µs | 0 B | 跨模块通用调用边界零分配 |
| `ModuleCallLoop` | 10,000 | 1.186 ms | 0 B | 动态分发成本可控且无逐次分配 |

benchmark 会复用预先构造的宿主参数数组，因此 `Allocated` 只反映实际执行路径；以上六项均未检测到托管分配。`ShortRun` 适合回归验证，绝对耗时应在目标部署机器上重新测量。

## 示例

- [examples/tests/main.as](examples/tests/main.as)：模块加载和脚本入口示例。
- [examples/tests/unit.as](examples/tests/unit.as)：内置类型、标准库和语言特性示例。
- [tests/AuroraScript.Tests](tests/AuroraScript.Tests)：推荐作为行为规格参考。
- [benchmark/scripts/runtime.as](benchmark/scripts/runtime.as)：运行时性能指标脚本。

## AI / MCP 服务

`AuroraScript.Mcp` 是面向 AI 编程工具的 stdio MCP 服务器。它把 AuroraScript 的文档、schema、示例、运行时 API、宿主 API 和脚本校验能力暴露给支持 MCP 的客户端，让 AI 在生成脚本前可以先查询语言资料，并在生成后调用编译/运行工具验证结果。

MCP 服务适合这些场景：

- 让 Codex、Claude、VS Code MCP Client 等工具读取 AuroraScript 语法、最佳实践和运行时 API。
- 检查 AI 生成的 `.as` 脚本是否可编译。
- 运行单文件、带 import/include 依赖的脚本，捕获返回值、`stdout`、`stderr` 和诊断。
- 查询 `Array.push`、`String.replace`、`HashMap`、`console.log` 等运行时 API。
- 查询宿主侧 API 和高级集成资料，例如自定义 `IScriptSourceResolver`。

### 安装 MCP dotnet tool

NuGet tool 包名是 `AuroraScript.Mcp`，命令名是 `aurora-mcp`：

```bash
dotnet tool install -g AuroraScript.Mcp
```

更新：

```bash
dotnet tool update -g AuroraScript.Mcp
```

本地源码调试也可以直接运行项目：

```bash
dotnet run --project language-tools/AuroraScript.Mcp/AuroraScript.Mcp.csproj
```

### 命令行 smoke test

MCP 使用 JSON-RPC over stdin/stdout。安装 tool 后可以这样测试资源读取：

```powershell
'{"jsonrpc":"2.0","id":1,"method":"resources/read","params":{"uri":"aurora://schema/runtime-api"}}' | aurora-mcp
```

也可以调用工具查询文档：

```powershell
'{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"aurora_get_document","arguments":{"id":"script-best-practices"}}}' | aurora-mcp
```

### Codex 配置

推荐先安装全局 tool：

```bash
dotnet tool install -g AuroraScript.Mcp
```

然后在 Codex 中添加 stdio MCP 服务：

```bash
codex mcp add aurora-script -- aurora-mcp
```

也可以手动编辑 Codex 配置文件：

- Windows：`%USERPROFILE%\.codex\config.toml`
- macOS / Linux：`~/.codex/config.toml`

```toml
[mcp_servers.aurora-script]
type = "stdio"
command = "aurora-mcp"
startup_timeout_sec = 10
tool_timeout_sec = 60
enabled = true
```

如果使用本地编译出的 exe，可以把 `command` 改成绝对路径：

```toml
[mcp_servers.aurora-script]
type = "stdio"
command = "D:\\mcp\\AuroraScript.Mcp.exe"
cwd = "D:\\mcp"
startup_timeout_sec = 10
tool_timeout_sec = 60
enabled = true
```

修改 Codex MCP 配置后，需要重启 Codex 或新开一个 Codex 会话，让 MCP 服务重新加载。

### MCP 资源和工具

常用资源：

- `aurora://docs/ai`
- `aurora://docs/script-best-practices`
- `aurora://docs/language`
- `aurora://docs/performance`
- `aurora://docs/host-integration`
- `aurora://schema/runtime-api`
- `aurora://schema/host-api`
- `aurora://examples/manifest`

常用工具：

- `aurora_check_script`：检查内存脚本或 CompileBlock。
- `aurora_run_script`：运行内存脚本或 CompileBlock。
- `aurora_check_file`：检查磁盘 `.as` 文件及其模块依赖。
- `aurora_run_file`：运行磁盘 `.as` 文件及其模块依赖。
- `aurora_build_workspace`：编译指定目录下 resolver 可见的所有脚本。
- `aurora_search_runtime_api` / `aurora_get_runtime_api`：查询运行时 API。
- `aurora_get_document` / `aurora_list_documents`：读取内置文档。
- `aurora_validate_best_practices`：检查 AI 脚本生成的最佳实践问题。

## 语言工具

语言工具位于 `language-tools/`：

- `AuroraScript.LanguageServices`：解析、语义索引、诊断、定义跳转等共享服务。
- `AuroraScript.LanguageServer`：LSP 服务器。
- `AuroraScript.VisualStudio`：Visual Studio VSIX 工程，打包语言服务器和 TextMate 语法。
- `AuroraScript.Mcp`：面向 AI 工具的 MCP 服务器，提供 `documents` 文档、schema、示例和脚本校验能力。

---

Made by [l2060](https://github.com/l2060)
