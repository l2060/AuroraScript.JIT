<p align="center">
  <img src="icon.png" width="128" alt="AuroraScript Logo" />
</p>

<p align="center">
  <a href="./README.md">简体中文</a> | <a href="./README_EN.md">English</a>
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
在 `Persistence` 模式下并开启Debug编译优化，您可以直接在 Visual Studio 中调试脚本：
- **断点 (Breakpoints)**：在 `.as` 文件中自由设置断点。
- **单步执行**: 支持逐语句 (F11)、逐过程 (F10) 和跳出 (Shift+F11)。
- **变量查看**: 支持查看本地变量、对象成员及代码堆栈。
- **调试指令**: 支持 `debugger` 指令进行编程式断点。

### 2. 启动调试
1.  在 C# 宿主程序中设置好断点。
2.  运行宿主程序。
3.  命中断点后即可在 Visual Studio 中进行调试。


![Debugger Demo](documents/debugger.png)

### 3. VS Code 插件
目前的 VS Code 插件主要用于提供**代码着色**（语法高亮）能力，提升编写体验。
- 安装方式：打开 `vscode-extension`，运行 `npm install` 与 `npm run package`，随后安装生成的 `.vsix` 文件。


*内置类型定义文件提供智能提示支持：*
![Type Definitions](documents/lib.d.as.png)

## 📚 内置 API (Built-in API)

AuroraScript 运行时提供了一套核心的标准库支持。

### 核心类型 (Core Types)

#### **Object**
基础对象类型，所有对象的基类。
- **静态方法**:
  - `equal$(a, b)`: 严格相等比较（引用一致性）。
  - `equal(a, b)`: 基本值相等比较。
  - `deepEqual(a, b)`: 深度递归比较对象内容。
  - `assign(target, ...sources)`: 将源对象的所有可枚举属性复制到目标对象。
  - `keys(obj)`: 返回包含对象所有自身可枚举属性名称的数组。
  - `clone(obj)` / `deepClone(obj)`: 对对象进行浅拷贝或深拷贝。
  - `extends(proto, [target])`: 创建一个以 `proto` 为原型的新对象，可选传入 `target` 进行初始化。
  - `freeze(obj)`: 冻结对象，防止添加、删除或修改属性。
- **实例成员**:
  - `length`: [只读] 返回对象拥有的属性数量。
  - `toString()`: 返回对象的字符串表示。

#### **Array**
有序集合类型，支持动态扩容与常用链式操作。
- **静态方法**:
  - `from(iterable)`: 从类数组或可迭代对象创建一个新数组。
  - `isArray(obj)`: 检查对象是否为数组类型。
  - `of(...items)`: 根据提供的参数创建一个新数组。
- **实例成员**:
  - `length`: [属性] 获取或设置数组长度。
  - `push(...items)` / `pop()`: 在末尾添加或移除元素。
  - `shift()` / `unshift(...items)`: 在开头移除或添加元素。
  - `slice(start, [end])`: 提取数组的一部分，不改变原数组。
  - `join([sep])`: 用指定分隔符（默认 `,`）将元素连接成字符串。
  - `reverse()` / `sort([cmp])`: 反转数组或按指定比较器排序。
  - `indexOf(val)` / `lastIndexOf(val)` / `has(val)`: 元素查找与存在性检查。
  - `map(cb)` / `filter(cb)` / `reduce(cb)` / `flat([depth])`: 闭包驱动的迭代与转换操作。
  - `some(cb)` / `every(cb)`: 逻辑谓词检查。

#### **String**
不可变的文本序列。
- **静态方法**:
  - `fromCharCode(...codes)`: 从一组 UTF-16 代码单元创建字符串。
  - `compare(a, b)`: 返回两个字符串的比较结果。
- **实例成员**:
  - `length`: [属性] 获取字符串字符数。
  - `substring(start, [end])` / `slice(start, [end])`: 截取子字符串。
  - `indexOf(sub)` / `lastIndexOf(sub)` / `contains(sub)`: 子串查找与匹配。
  - `startsWith(sub)` / `endsWith(sub)`: 前后缀检查。
  - `split(sep)`: 按分隔符拆分为字符串数组。
  - `replace(search, repl)`: 替换匹配项。支持正则匹配及回调函数处理。
  - `match(regex)` / `matchAll(regex)`: 结合正则进行模式检索。
  - `trim()` / `trimLeft()` / `trimRight()`: 去除空格或指定空白字符。
  - `toLowerCase()` / `toUpperCase()`: 大小写风格转换。
  - `charCodeAt(index)`: 获取指定位置字符的编码值。

#### **Date**
日期与时间处理。
- **静态方法**:
  - `now()` / `utcNow()`: 获取当前本地或 UTC 时间。
  - `parse(str)`: 解析日期字符串。
- **实例属性**:
  - `year` / `month` / `day` / `hour` / `minute` / `second` / `millisecond`: 获取时间各分量（只读）。
  - `dayOfWeek` / `dayOfYear` / `ticks`: 获取星期、年内天数或原始计时周期。

#### **HashMap**
高性能、线程安全的键值对集合，底层基于 `ConcurrentDictionary`。
- **实例成员**:
  - `size`: [属性] 获取集合内元素数量。
  - `set(key, val)` / `get(key)`: 存取键值对。支持任意类型作为键。
  - `has(key)` / `delete(key)`: 检查存在性或删除特定成员。
  - `getOrInsert(key, defaultVal/cb)`: 获取键值，若不存在则原子性地插入默认值或回调结果。
  - `keys` / `values`: [属性] 获取所有键或值的迭代集合。
  - `clear()`: 清空整个集合。

#### **Regex**
正则表达式对象。
- `test(str)`: 检查字符串是否匹配定义的模式。

---

### 标准库 (Standard Library)

#### **console**
提供标准输入输出与性能调试功能。
- `log(...args)`: 在控制台打印普通信息。多参数将自动以逗号分隔。
- `error(...args)`: 在控制台打印错误信息，支持输出调用堆栈。
- `time(label)`: 开始一个以 `label` 命名的计时器。
- `timeEnd(label)`: 停止计时器并在控制台输出经过的时间（毫秒）。

#### **Math**
提供常用数学常量与科学计算函数。
- **常量**: `PI`, `E`, `Tau`, `DEG_PER_RAD`。
- **方法**:
  - `abs(x)`: 返回 x 的绝对值。
  - `max(...args)` / `min(...args)`: 返回参数序列中的极大值或极小值。
  - `random()`: 返回 [0, 1) 之间的伪随机数。
  - `floor(x)` / `round(x)`: 向下取整或常规四舍五入。
  - `pow(x, y)` / `log(x)` / `exp(x)`: 幂、自然对数及指数函数。
  - `sin(x)` / `cos(x)` / `tan(x)`: 标准三角函数（弧度制）。

#### **JSON**
JSON 数据的序列化与反序列化工具。
- `parse(text)`: 将符合规范的 JSON 字符串解析为脚本对象。
- `stringify(obj, [indented])`: 将脚本对象序列化为文本。`indented` 为真时启用美化缩进。

#### **StringBuffer**
专为高性能大文本拼接设计的构建器。
- `append(...args)`: 向末尾追加一个或多个连接项。
- `appendLine(...args)`: 追加内容并附加平台相关的换行符。
- `insert(index, str)`: 在指定索引偏移处插入字符串。
- `clear()`: 重置缓冲区。
- `toString()`: 输出构建完成的完整字符串。

#### **Proxy**
拦截并定义对象基本操作的自定义代理行为。
- `new Proxy(target, handlers)`:
  - **注意事项**: 必须提供完整的 `handlers` 对象，目前支持拦截 `get`, `set` 和 `unset`（即 `delete`）操作。

#### **HotPatch**
提供运行时的模块动态补丁与修复能力。
- `replace(modulePath, script, [ignoreDeps])`: 完全替换指定路径的模块逻辑。
- `incremental(modulePath, script, [ignoreDeps])`: 增量添加或更新模块成员。
  - **注意事项**: 补丁脚本中的顶层代码（如变量初始化）会在应用时立即重新执行。

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
|  |  |  |  |
|  |  |  |  |
|  |  |  |  |
|  |  |  |  |


> 测试环境: Intel Core i7-13700KF, .NET 10.0.1.

## 📂 示例

- [**Basic Tests**](examples/tests/main.as): 语法结构和模块加载示例。
- [**Benchmarks**](benchmark/scripts/unit.as): 性能测试脚本。

---

Made with ❤️ by [l2060](https://github.com/l2060)
