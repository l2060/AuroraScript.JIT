<p align="center">
  <img src="icon.png" width="128" alt="AuroraScript Logo" />
</p>

<p align="center">
  <a href="./README.md">简体中文</a> | <a href="./README_EN.md">English</a>
</p>

# AuroraScript

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-4.0.0-orange.svg)](src/AuroraScript.csproj)
[![Target](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blueviolet.svg)](src/AuroraScript.csproj)

AuroraScript 是面向 .NET 宿主程序的轻量脚本引擎。它将脚本编译为 CIL 并交给 CLR/JIT 执行，适合嵌入规则、业务逻辑、配置化流程、热修复和小型表达式。

它借鉴 JavaScript 的表达式、对象、数组、闭包和模块写法，但不是 ECMAScript 实现，也不承诺兼容浏览器或 Node.js 语义。

## 特性

- CIL/JIT 执行、模块化脚本和独立 `ScriptDomain`。
- `Dynamic`、`OnlyRun`、`Persistence` 三种编译模式。
- CLR 类型注册与受控互操作。
- 内存、文件系统和组合脚本来源。
- 热更新、`CompileBlock`、标准对象和语言工具支持。

## 安装

```bash
dotnet add package AuroraScript.JIT --version 4.0.0
```

包支持 `net8.0`、`net9.0` 和 `net10.0`。`Dynamic` 与 `OnlyRun` 支持全部目标；`Persistence` 需要 `net9.0` 或更高版本。

## 快速开始

创建 `scripts/main.as`：

```javascript
@module(MAIN);

export func main(value) {
    return value + 22;
}
```

在宿主中编译并执行：

```csharp
using AuroraScript;
using AuroraScript.Core;
using AuroraScript.Runtime;

var options = EngineOptions.Default.WithCompiler(compiler =>
{
    compiler.SourceResolver = ScriptSources.FileSystem("./scripts");
    compiler.Mode = CompilationMode.Dynamic;
});

var engine = new AuroraEngine(options);
await engine.BuildAsync("main.as");

using var domain = engine.CreateDomain();
var result = domain.Execute("MAIN", "main", ScriptDatum.FromNumber(20));
Console.WriteLine(result); // 42
```

这里的 `@module(MAIN);` 为入口提供显式查询名称，使宿主可以通过 `Execute("MAIN", ...)`、`GetMethod` 或 `GetModule` 访问它，脚本也可以通过 `global.getModule("MAIN")` 动态查询它。仅被其他脚本 `import` / `include` 的文件可以省略 `@module`；省略后模块保持匿名，编译器不会根据文件名推导默认模块名。

## 文档

详细介绍、教程、逐对象 API Reference、性能基准、MCP、语言服务器和 Visual Studio 扩展已迁移到 Wiki：

- [Wiki 首页](https://github.com/l2060/AuroraScript.JIT/wiki)
- [快速开始](https://github.com/l2060/AuroraScript.JIT/wiki/Getting-Started)
- [语言指南](https://github.com/l2060/AuroraScript.JIT/wiki/Language-Guide)
- [Script API Reference](https://github.com/l2060/AuroraScript.JIT/wiki/API-Script-Reference)
- [.NET Host API Reference](https://github.com/l2060/AuroraScript.JIT/wiki/API-Host-Reference)
- [宿主原生导出](https://github.com/l2060/AuroraScript.JIT/wiki/Host-Native-Exports)
- [MCP、LSP 与 VSIX](https://github.com/l2060/AuroraScript.JIT/wiki/Tooling)
- [性能与 Benchmark](https://github.com/l2060/AuroraScript.JIT/wiki/Performance-and-Benchmarks)


## 从源码构建

```bash
git clone https://github.com/l2060/AuroraScript.JIT.git
cd AuroraScript.JIT
dotnet build src/AuroraScript.csproj -c Release
```

## 许可

本项目基于 [MIT License](LICENSE) 发布。
