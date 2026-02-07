<p align="center">
  <img src="icon.png" width="128" alt="AuroraScript Logo" />
</p>

<p align="center">
  <a href="./README_CN.md">简体中文</a> | <a href="./README.md">English</a>
</p>

# AuroraScript

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/l2060/AuroraScript)
[![Version](https://img.shields.io/badge/version-1.0.0-orange.svg)](package.json)

AuroraScript 是一个基于 .NET 构建的轻量级、弱类型脚本执行引擎。它将脚本直接编译为 CIL（通用中间语言），并通过 .NET JIT 编译器执行。旨在提供极致的性能、易于嵌入且高度可扩展。

虽然在语法和机制上借鉴了 JavaScript，但 AuroraScript 是一门独立的语言，拥有自己的优化和特性，并不遵守 ECMA 规范。它充分利用了原生 .NET 基础设施进行执行、互操作和调试。

> [!NOTE]
> 🚧 **Work in Progress**: 本项目仍处于开发阶段，性能和 API 稳定性正在持续改进中。我们非常欢迎大家提交 **PR** 和 **Issue** 来共同壮大 AuroraScript！

## ✨ 特性

- **高性能**：无第三方依赖，编译为原生 CIL/MSIL，利用 .NET JIT 编译器执行。
- **弱类型系统**：类似 JavaScript 的灵活变量类型。
- **原生互操作**：可无缝注册和在脚本中使用 .NET (CLR) 类型和函数。
- **调试支持**：目前仅支持 **Visual Studio** 调试（断点、步进、变量查看等）。
- **模块化系统**：
  - 支持 `import xxx from 'xxx'` 导入模块导出项。
  - 支持 `include 'xxx.as'` 直接嵌入脚本文件。
  - 支持 `@module("MODULENAME")` 自定义模块名称。
- **高级控制流**：
  - 支持 `debugger` 指令进行编程式断点。
  - 支持宿主控制的中断（Interruption）与继续（Continue）机制。
  - 增强的 `where` / `for` loop 支持。
- **编译模式 (Compilation Modes)**：
  - `Persistence`：持久化程序集模式。编译为包含 PDB 符号的持久化 DLL。支持源码级调试和编程式断点。完全可检索、可被外部进程 Dump。
  - `OnlyRun`：临时内存编译模式。内存中即时编译执行。无托管调试映射关系。对外部性能分析器和 Dump 工具透明，代码驻留在可读内存段。
  - `Dynamic`：高性能动态执行模式。通过 `DynamicMethod` 发射 CIL。无元数据开销，提供极致性能。黑盒执行：不可检索也不可被外部进程 Dump。
- **热修复 (Hot-fix)**：支持在不丢失状态的情况下动态更新脚本逻辑。提供 `Replace` 和 `Incremental` 两种模式，支持 .NET API 或脚本 API 调用。
- **混淆支持 (Obfuscation)**：内置对比特、成员名和代码结构的混淆功能。
- **现代语法支持**：
  - 支持闭包（Closures）、Lambda 表达式和函数指针。
  - 对象解构：`var { a, b } = obj;`。
  - 数组解构：`var [ a, ...b ] = arr;`。
  - 展开运算符（Spread Operator）：`...` 支持数组和对象展开。
  - 文本模板：支持多行文本模板（`` ` `` 或 `|>` 语法）。
- **标准库**：内置 `Math`, `JSON`, `Date`, `Regex`, `HashMap`, `Proxy`, `StringBuffer` 等实用对象。

## 🚀 快速开始

### NuGet 安装

您可以通过 NuGet 快速安装 AuroraScript 引擎：

```bash
dotnet add package AuroraScript.JIT
```

### 手动安装

克隆仓库到本地：

```bash
git clone https://github.com/l2060/AuroraScript.git
cd AuroraScript
```

### 编译项目

构建核心引擎库：

```bash
dotnet build src/AuroraScript.csproj -c Release
```

## 📖 使用指南

### 运行脚本 (宿主程序)

在您的 .NET 应用程序中托管引擎：

```csharp
using AuroraScript;
using AuroraScript.Runtime;

// 1. 初始化引擎配置
var options = EngineOptions.Default.WithBaseDirectory("./scripts/");
var engine = new AuroraEngine(options);

// 2. 注册 CLR 类型或函数 (可选)
engine.RegisterType<Math>("Math2");

// 3. 编译脚本 (搜索并构建基础目录下所有 .as 文件)
await engine.BuildAsync(engine.SearchAllFileSource(Encoding.UTF8));

// 4. 创建 Domain 并执行
var domain = engine.CreateDomain();
// 执行 'MAIN' 模块中的 'main' 函数
domain.Execute("MAIN", "main");
```

### 编写脚本

语法示例：

```javascript
@module("MAIN");

func main() {
    console.log("你好, AuroraScript!");

    // 调用注册的 CLR 库
    var res = Math2.Abs(-100);
    console.log("Abs 结果: " + res);

    var list = [1, 2, 3, 4, 5];
    for (var item in list) {
        if (item % 2 == 0) {
            console.log("偶数: " + item);
        }
    }
}
```

## 🐞 脚本调试

AuroraScript 支持功能完备的 **Visual Studio** 调试体验。

### 1. Visual Studio 调试
在 `Persistence` 或 `OnlyRun` 模式下，您可以直接在 Visual Studio 中调试脚本：
- **断点 (Breakpoints)**：在 `.as` 文件中自由设置断点。
- **单步执行**: 支持逐语句 (F11)、逐过程 (F10) 和跳出 (Shift+F11)。
- **变量查看**: 支持查看本地变量、对象成员及代码堆栈。

### 2. VS Code 插件
目前的 VS Code 插件主要用于提供**代码着色**（语法高亮）能力，提升编写体验。
- 安装方式：打开 `vscode-extension`，运行 `npm install` 与 `npm run package`，随后安装生成的 `.vsix` 文件。

### 3. 启动调试
1.  在 C# 宿主程序中设置好断点。
2.  运行宿主程序。
3.  命中断点后即可在 Visual Studio 中进行调试。

![Debugger Demo](documents/debugger.png)

*内置类型定义文件提供智能提示支持：*
![Type Definitions](documents/lib.d.as.png)

## 📚 内置 API (Built-in API)

AuroraScript 运行时提供了一套核心的标准库支持。

### 核心类型 (Core Types)

| 类型 | 描述 | 常用方法 |
| :--- | :--- | :--- |
| **Object** | 基础对象类型 | `keys()`, `values()`, `assign()`, `toString()` |
| **String** | 字符串 | `length`, `substring`, `indexOf`, `split`, `replace`, `trim` |
| **Number** | 数值 | `toFixed`, `toString`, `isNaN` |
| **Boolean** | 布尔值 | `toString`, `valueOf` |
| **Array** | 数组 | `push`, `pop`, `shift`, `slice`, `splice`, `join`, `map` |
| **Function** | 函数 | `call`, `apply`, `bind` |

### 标准库 (Standard Library)

| 对象 | 描述 | 常用方法 |
| :--- | :--- | :--- |
| **console** | 终端输入输出 | `log(msg)`, `error(msg)`, `warn(msg)` |
| **Math** | 数学库 | `sin`, `cos`, `tan`, `sqrt`, `pow`, `random`, `PI`, `E` |
| **JSON** | JSON 序列化 | `parse(string)`, `stringify(object)` |
| **Date** | 日期时间 | `now()`, `parse(string)`, 构造函数 `new Date()` |
| **Regex** | 正则表达式 | 构造函数 `new Regex(pattern)`, `match(str)`, `replace(str, repl)` |
| **HashMap** | 哈希表 | `set(key, val)`, `get(key)`, `has(key)`, `delete(key)`, `clear()` |
| **Proxy** | 代理对象 | 构造函数 `new Proxy(target, handler)`, 拦截 get/set 等操作 |
| **StringBuffer** | 字符串构建器 | `append(str)`, `toString()`, 高性能字符串拼接 |

### 全局上下文
- `global`: 指向当前 Domain 的全局作用域。
- `$state`: 访问用户注入的状态对象 (通过 C# `ExecuteOptions.WithUserState` 传入)。
- `$args`: 当前函数的入参数组。

## 🔥 热修复 (Hot-fix)

AuroraScript 提供了强大的热修复能力，允许您在不重启应用程序或丢失运行时状态的情况下，动态更新正在运行的 `ScriptDomain` 中的脚本逻辑。

### 1. .NET API (宿主侧)
通过 `domain.DynamicPatch` 方法从宿主程序应用补丁：

```csharp
// 应用替换式补丁 (Replace)
domain.DynamicPatch(engine.MemorySource("module.as", "func newFunc() { ... }"), HotPatchType.Replace);

// 应用增量式补丁 (Incremental)
domain.DynamicPatch(engine.MemorySource("module.as", "var newVar = 1;"), HotPatchType.Incremental);
```

### 2. 脚本 API (脚本侧)
全局 `HotPatch` 对象允许脚本自行或为其他模块应用补丁：

```javascript
// 替换 'MAIN' 模块的所有成员
HotPatch.replace("MAIN", "func main() { console.log('已修复!'); }");

// 增量更新 'UTILS' 模块的成员
HotPatch.incremental("UTILS", "func helper() { return 42; }");
```

### 3. 工作原理
热修复通过 `IncrementalCompiler` 实现局部 JIT 编译。它将新代码链接到现有的 `ScriptGlobal` 环境中，并更新代表目标模块的 `ScriptObject` 实例。

### 4. 注意事项
- **顶层代码执行**：补丁模块应用时，其顶层代码（变量初始化等）会重新执行。
- **函数签名**：确保新函数的参数签名与现有调用处保持一致，以维持兼容性。
- **替换 vs 增量**：
    - `Replace` 模式：具有破坏性。在应用新代码前会清空模块的所有现有属性。
    - `Incremental` 模式：安全。保留现有属性，仅更新或添加新成员。
- **状态持久化**：如果补丁代码中包含模块级变量定义，这些变量会被重新初始化。

## 📊 性能基准 (Benchmark)

我们在性能优化上投入了大量精力，但仍有提升空间。欢迎社区贡献代码！

| 方法 | 平均耗时 (Mean) | 标准差 (StdDev) | 内存分配 |
| :--- | :---: | :---: | :---: |
| **TestIfTrue** | 225.1 ns | 0.19 ns | - |
| **TestAssuming** | 227.0 ns | 0.51 ns | - |
| **TestAddVar** | 240.6 ns | 0.50 ns | - |
| **TestGetVar** | 243.5 ns | 0.37 ns | - |
| **TestSetVar** | 250.7 ns | 0.39 ns | - |
| **TestSetProperty** | 257.0 ns | 1.05 ns | 48 B |
| **TestGetProperty** | 269.3 ns | 0.44 ns | - |
| **TestClone** | 515.6 ns | 1.34 ns | 1072 B |
| **TestIterator** | 822.1 ns | 2.09 ns | 1056 B |
| **TestJson** | 1,907.2 ns | 6.53 ns | 4520 B |
| **TestRegex** | 3,604.4 ns | 7.77 ns | 8696 B |
| **TestStrings** | 979.7 $\mu$s | 3.27 $\mu$s | 3.48 MB |
| **TestClosure** | 1.00 ms | 2.27 $\mu$s | 496 B |
| **TestObjects** | 1.38 ms | 5.06 $\mu$s | 5.14 MB |
| **TestArrays** | 1.40 ms | 15.13 $\mu$s | 2.22 MB |
| **TestFor100W** | 10.25 ms | 112.7 $\mu$s | - |

> 测试环境: Intel Core i7-13700KF, .NET 10.0.1.

## 📂 示例

- [**Basic Tests**](examples/tests/main.as): 语法结构和模块加载示例。
- [**Benchmarks**](benchmark/scripts/unit.as): 性能测试脚本。

---

Made with ❤️ by [l2060](https://github.com/l2060)
