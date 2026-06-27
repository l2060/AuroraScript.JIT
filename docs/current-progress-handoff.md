# AuroraScript 当前进度交接

更新时间：2026-06-27

## 当前目标

当前主线是让 AuroraScript 支持 Visual Studio 2026 插件所需的完整语言工具链与可扩展源码加载机制：

- 脚本引擎支持通过 `IScriptSourceResolver` 从文件系统、内存、虚拟文件系统等来源加载脚本。
- 语言服务和 LSP 使用 workspace-first 模型，支持打开文档优先于磁盘文件。
- 为后续 Visual Studio 2026 插件提供 definition/references/rename/semantic tokens/diagnostics 等基础能力。
- VS2026 插件最终目标包括：脚本调试变量观察、脚本着色、查找引用、转到定义、自动完成、内置类型虚拟文档跳转。
- 保持实现偏性能优先，避免不必要的长期内存驻留和错误缓存。

## 已完成

### SourceResolver

已实现并接入源码解析抽象：

- `src/Core/ScriptSourceResolver.cs`
  - `IScriptSourceResolver`
  - `FileScriptSourceResolver`
- `src/Core/ScriptSourceReference.cs`
- `src/Core/MemoryScriptSource.cs`
- `src/Core/ScriptPath.cs`
- `src/EngineOptions.cs`
  - `CompilerOptions.SourceResolver`
  - `CompilerOptionsBuilder.SourceResolver`
  - `WithSourceResolver`

编译链路已改为通过 resolver 解析 import/include：

- `src/Compiler/Analyzer/AuroraParser.cs`
- `src/Compiler/ScriptCompiler.cs`
- `src/Compiler/IncrementalCompiler.cs`
- `src/AuroraEngine.cs`

`BuildAsync` 不再要求物理 `BaseDirectory` 必须存在；内存/虚拟脚本入口可以正常编译。

### 自定义 SourceResolver 示例测试

新增测试类：

- `tests/AuroraScript.Tests/CustomSourceResolverUsageTests.cs`

覆盖内容：

- 完整自定义 `VirtualFileSystemSourceResolver` 示例。
- 使用虚拟根目录 `vfs://...`。
- 入口脚本、`import`、`include` 全部从虚拟文件系统加载。
- 支持自定义脚本扩展名，例如 `.aurora`。
- 缺失虚拟依赖时返回编译异常。

这个测试类可以作为 NuGet 用户文档里的最小完整示例来源。

### LanguageServices / LanguageServer / MCP

语言工具项目已放到根目录下：

- `language-tools/AuroraScript.LanguageServices`
- `language-tools/AuroraScript.LanguageServer`
- `language-tools/AuroraScript.Mcp`

已弃用旧 `vscode-extension`，不要继续使用它。

LanguageServices 已具备：

- `AuroraLanguageService`
- `AuroraLanguageServiceOptions`
- `AuroraWorkspace`
- `AuroraWorkspaceSnapshot`
- `WorkspaceScriptSourceResolver`
- parse/diagnostics/hover/completion/signature help
- definition/references/rename
- semantic tokens
- builtin API catalog/readonly diagnostics

LanguageServer 已接入：

- `didOpen`
- `didChange`
- `didClose`
- hover/completion/signatureHelp/definition/references/rename/semanticTokens
- `aurora/builtinDocument` 自定义请求，用于读取内置类型虚拟文档。

`TextDocumentStore` 已移除，LSP 文档状态统一走 `AuroraLanguageService.Workspace`。

### VS2026 插件基础能力推进

本轮继续围绕 VS2026 插件目标补齐 LSP/LanguageServices 能力：

- LSP `didOpen` / `didChange` / `didClose` 后改为刷新所有打开文档 diagnostics。
  - 解决打开或关闭依赖文件后，引用者 import/include 诊断不更新的问题。
  - 当前实现是正确性优先的 workspace 全量刷新；后续可用 dependency graph 缩小刷新范围。
- definition 支持局部变量、参数、局部函数等本地符号跳转。
  - 防止内置符号 fallback 抢占脚本局部定义。
- definition 增加内置类型/成员虚拟文档 fallback。
  - 例如 `Math` / `Math.abs` 可跳转到 `aurora-builtin:/Math.as`。
  - 新增 `BuiltinDefinitionDocuments` 根据 `BuiltinApiCatalog` 生成只读声明虚拟文档。
  - 内置声明格式参考 `lib.d.as` 的类型写法，不继续开发旧 VSCode 扩展实现：
    - 方法显示参数类型和返回类型，例如 `export declare Math.abs(value: Number): Number;`。
    - 属性/常量显示值类型，例如 `export declare Math.PI: Number;`。
    - prototype 成员会合并到对应类型文档，例如 `export declare Array.prototype.push(...values: Object[]): Number;`。
    - JSDoc 中同步显示 `@param` / `@returns` 类型说明。
  - 虚拟文档内的内置类型名支持继续 definition 跳转，例如 `Math.as` 中的 `Number` 跳到 `aurora-builtin:/Number.as`。
  - 这些虚拟文档是声明文件用途，不再要求能被普通 `.as` 编译 parser 成功解析。
- LanguageServer 新增 `aurora/builtinDocument` 请求。
  - VS 插件可用 definition 返回的 `aurora-builtin:` URI 调该请求读取只读文档内容。

### 可结项判断

按当前实现和回归结果，以下语言工具基础任务可以结项：

- SourceResolver 抽象和虚拟源码加载链路。
- workspace-first LanguageServices/LSP 文档状态。
- hover/completion/signatureHelp/definition/references/rename/semanticTokens 基础能力。
- 打开/修改/关闭依赖文档后的打开文档 diagnostics 刷新。
- 局部变量、参数、局部函数 definition 优先级。
- 内置对象/成员虚拟文档跳转、`aurora/builtinDocument` 读取、声明文件类型展示、内置文档内类型跳转。

VS2026 插件外壳本轮已开始实现，可打包为 VSIX：

- 新增 `visualstudio-extension/AuroraScript.VisualStudio` VSIX 项目。
- 插件使用 VS LSP client 启动内嵌的 `AuroraScript.LanguageServer.exe`。
- `.as` 文件注册到 `aurorascript` content type。
- VSIX 内置 TextMate grammar 和 language-configuration，提供本地脚本着色、括号/注释/自动闭合配置。
  - grammar 已按当前 compiler/parser 支持的 AuroraScript 语法补齐，而不是沿用旧 VSCode 扩展：
    - 实际关键字：`declare/if/else/const/function/func/var/return/debugger/break/continue/enum/for/new/delete/while/try/catch/finally/throw/import/include/from/export/typeof/in`。
    - 注解/元数据：`@module(...)`、`@directCall`、多注解函数声明。
    - 字符串：单引号、双引号、反引号模板字符串、`${...}` 插值表达式、`|>` 字符串块续行。
    - 数字、regex literal、对象/数组字面量、解构、lambda `=>`、spread/rest `...`、复合赋值和位运算操作符。
- VSIX 打包时发布 `AuroraScript.LanguageServer` 为 `win-x64` self-contained trimmed single-file exe，放入 `Server/` 目录。
  - VSIX 只收录 `Server/AuroraScript.LanguageServer.exe`，不收录 side-by-side dll/json/pdb/runtime 依赖文件。
  - 发布目录构建前会清理，publish 后会删除非 exe sidecar 文件，并在 exe 缺失时让构建失败。
- definition 返回 `aurora-builtin:` 时，middle layer 请求 `aurora/builtinDocument`，把内置声明写到临时只读缓存文件并返回给 VS 打开。
- 内置缓存文件的 definition 请求会映射回原始 `aurora-builtin:` URI，支持内置文档内类型继续跳转。
- 内置缓存文件的 didOpen/didChange/didClose 被过滤，避免声明文件被当作普通脚本产生 diagnostics。

仍不能整体结项的 VS2026 插件目标：

- 尚未在真实 Visual Studio 2026 实例中做交互验收。
- VS Marketplace 发布元数据、签名、图标、许可页等发布资产尚未整理。
- 调试变量观察/运行时调试适配层尚未实现。
- dependency graph 级 diagnostics 局部刷新仍是优化项。
- VS TextMate grammar 已按 compiler 语法补齐，但仍需在真实 VS2026 编辑器中做视觉验收。

涉及文件：

- `language-tools/AuroraScript.LanguageServices/Features/Definition/BuiltinDocument.cs`
- `language-tools/AuroraScript.LanguageServices/Internal/BuiltinDefinitionDocuments.cs`
- `language-tools/AuroraScript.LanguageServices/AuroraLanguageService.cs`
- `language-tools/AuroraScript.LanguageServices/Internal/SymbolIndex/AuroraDefinitionResolver.cs`
- `language-tools/AuroraScript.LanguageServices/Internal/SymbolIndex/AuroraLocalSymbolIndex.cs`
- `language-tools/AuroraScript.LanguageServer/AuroraLanguageServer.cs`
- `language-tools/AuroraScript.LanguageServer/AuroraScript.LanguageServer.csproj`
- `visualstudio-extension/AuroraScript.VisualStudio/AuroraScript.VisualStudio.csproj`
- `visualstudio-extension/AuroraScript.VisualStudio/source.extension.vsixmanifest`
- `visualstudio-extension/AuroraScript.VisualStudio/Language/*`
- `visualstudio-extension/AuroraScript.VisualStudio/Grammars/AuroraScript.tmLanguage.json`
- `visualstudio-extension/AuroraScript.VisualStudio/language-configuration.json`

新增/调整回归：

- `DefinitionFeatureTests`
  - builtin member definition 跳转到虚拟文档。
  - builtin global definition 跳转到虚拟文档。
  - local shadow 不被 builtin fallback 覆盖。
  - builtin 虚拟文档使用 `lib.d.as` 风格参数/返回值类型声明。
  - builtin 虚拟文档内类型名可继续跳转到对应内置类型文档。
  - prototype 成员出现在对应内置类型文档。
- `AuroraLanguageServerTests`
  - 依赖文档打开/关闭后刷新引用者 diagnostics。
  - definition 返回 `aurora-builtin:` URI。
  - `aurora/builtinDocument` 返回虚拟文档文本。
  - `aurora-builtin:` 虚拟文档内部类型名可通过 LSP definition 继续跳转。
- VSIX 打包验证：
  - `dotnet build visualstudio-extension\AuroraScript.VisualStudio\AuroraScript.VisualStudio.csproj -c Release -m:1`
  - 产物：`visualstudio-extension\AuroraScript.VisualStudio\bin\Release\net472\AuroraScript.VisualStudio.vsix`
  - Release VSIX 当前约 32 MB。
  - 包内 `Server/` 目录只包含 `Server/AuroraScript.LanguageServer.exe`。
  - 包内包含 `Assets/icon.png`、`Assets/LICENSE.txt`、`Grammars/AuroraScript.tmLanguage.json`、`language-configuration.json`。
  - 单文件语言服务器已通过 stdio initialize + didOpen + completion smoke test。

### Workspace Index 缓存调整

已从整张 workspace index 缓存改为模块级索引缓存：

- `language-tools/AuroraScript.LanguageServices/Internal/SymbolIndex/AuroraWorkspaceIndex.cs`
- `language-tools/AuroraScript.LanguageServices/Internal/SymbolIndex/AuroraWorkspaceIndexCache.cs`

设计取舍：

- 不再按 `AuroraWorkspace.Version` 缓整张 index。
- 每次查询构建轻量 workspace index。
- 单个模块 AST/符号索引按路径和精确文本复用。
- 避免磁盘 import 文件变化但 workspace version 不变时误用旧 index。
- 长期缓存只用于 workspace-first API，纯文本一次性 API 仍无状态。

已补回归：

- workspace 文档变化后 definition 位置更新。
- 磁盘 import 文件变化后 definition 位置更新。
- LSP `didChange` 修改 import 文件后 definition 位置更新。

## 最近验证结果

已通过：

- `dotnet test tests\AuroraScript.Tests\AuroraScript.Tests.csproj --no-restore --filter CustomSourceResolverUsageTests`
  - net8.0：3 passed
  - net9.0：3 passed
  - net10.0：3 passed
  - 过程中出现一次 MSB3026 文件占用重试警告，重试后通过。
- `dotnet test tests\AuroraScript.Tests\AuroraScript.Tests.csproj --no-build`
  - net8.0：435 passed
  - net9.0：438 passed
  - net10.0：438 passed
- `dotnet test tests\AuroraScript.LanguageServices.Tests\AuroraScript.LanguageServices.Tests.csproj --no-restore`
  - 最近一次通过：31 passed
- `dotnet test tests\AuroraScript.LanguageServer.Tests\AuroraScript.LanguageServer.Tests.csproj --no-restore`
  - 最近一次通过：11 passed
- `dotnet build AuroraScript.sln --no-restore -m:1`
  - 在新增 `CustomSourceResolverUsageTests` 前通过：0 warnings, 0 errors
- 本轮 VS2026 插件基础能力验证：
  - `dotnet test tests\AuroraScript.LanguageServices.Tests\AuroraScript.LanguageServices.Tests.csproj --no-restore`
    - net10.0：37 passed
  - `dotnet test tests\AuroraScript.LanguageServer.Tests\AuroraScript.LanguageServer.Tests.csproj --no-restore`
    - net10.0：14 passed
  - `dotnet build AuroraScript.sln --no-restore -m:1`
    - 0 warnings, 0 errors
  - 并行跑 LanguageServices/LanguageServer 测试时出现一次 `AuroraScript.LanguageServices.dll` 文件占用重试警告，重试后通过；串行 solution build 无警告。
- 本轮 VS 插件外壳验证：
  - `dotnet build visualstudio-extension\AuroraScript.VisualStudio\AuroraScript.VisualStudio.csproj -m:1`
    - Debug VSIX 生成成功，0 warnings, 0 errors。
  - `dotnet build visualstudio-extension\AuroraScript.VisualStudio\AuroraScript.VisualStudio.csproj -c Release -m:1`
    - Release VSIX 生成成功，0 warnings, 0 errors。
    - Release 包内容检查通过：`Server/` 目录仅有 `AuroraScript.LanguageServer.exe`，无 dll/json/pdb/runtime sidecar。
    - VSIX 中 grammar/language-configuration 内容检查通过。
  - `visualstudio-extension\AuroraScript.VisualStudio\obj\Release\LanguageServer\win-x64\AuroraScript.LanguageServer.exe`
    - 单文件 stdio completion smoke test 通过。
  - `dotnet test tests\AuroraScript.LanguageServices.Tests\AuroraScript.LanguageServices.Tests.csproj --no-restore`
    - net10.0：37 passed
  - `dotnet test tests\AuroraScript.LanguageServer.Tests\AuroraScript.LanguageServer.Tests.csproj --no-restore`
    - net10.0：14 passed
  - `dotnet build AuroraScript.sln --no-restore -m:1`
    - 0 warnings, 0 errors
  - 并行跑 LanguageServices/LanguageServer 测试时出现一次 `AuroraScript.dll` 文件写入锁；LanguageServer 测试串行重跑后通过。

如果新上下文继续提交前，建议再跑一次 solution build。

## 当前工作区注意事项

当前 git worktree 有大量已添加/修改/重命名文件，很多来自前面语言工具和测试结构重构。不要随意 revert。

尤其注意：

- `.agent` 到 `.agents` 的重命名是已有状态。
- `tests` 项目已重组到：
  - `tests/AuroraScript.Tests`
  - `tests/AuroraScript.LanguageServices.Tests`
  - `tests/AuroraScript.LanguageServer.Tests`
- `language-tools` 下大量文件是新增项目内容。
- `visualstudio-extension/AuroraScript.VisualStudio` 是新增 VSIX 插件项目，已加入 `AuroraScript.sln`。
- `AuroraScript.VisualStudio.vsix` 是构建产物，位于 `bin\Debug` / `bin\Release`，不要手工提交二进制产物，除非明确做 release artifact。
- `src/Core/TextSource.cs` 已删除，使用 `MemoryScriptSource`。
- `vscode-extension` 已弃用，不要基于它继续开发。

## 下一步建议

### 1. 再跑完整验证

优先执行：

```powershell
dotnet test tests\AuroraScript.Tests\AuroraScript.Tests.csproj --no-restore
dotnet test tests\AuroraScript.LanguageServices.Tests\AuroraScript.LanguageServices.Tests.csproj --no-restore
dotnet test tests\AuroraScript.LanguageServer.Tests\AuroraScript.LanguageServer.Tests.csproj --no-restore
dotnet build AuroraScript.sln --no-restore -m:1
```

### 2. VS2026 插件实机验收和发布准备

不要更新 `README.md` / `README_EN.md`，不要继续旧 `vscode-extension` 实现。下一步优先做 VSIX 实机验收：

- 安装 `visualstudio-extension\AuroraScript.VisualStudio\bin\Release\net472\AuroraScript.VisualStudio.vsix` 到 Visual Studio 2026。
- 打开 `.as` 文件，确认 TextMate 着色、注释/括号配置生效。
  - 重点验收关键字、`@module(...)`/`@directCall` 注解、反引号模板字符串、`${...}` 插值、`|>` 字符串块、regex literal、解构、lambda、spread/rest、内置声明文档类型名。
- 验证 LSP 启动、diagnostics、completion、hover、signatureHelp、definition、references、rename、semantic tokens。
- 验证 `Math.abs` 跳到内置文档，内置文档里的 `Number` / `Object` / `Function` 继续跳转。
- 验证内置缓存文件不会产生普通脚本 diagnostics。
- 整理发布资产：图标、manifest 版本、publisher、license、release notes、签名策略。

### 3. 完善语言服务诊断策略

当前 LSP 已支持打开文档 diagnostics 全量刷新，但还没有依赖图级局部刷新。

后续需要明确：

- import/include 解析失败目前通过 workspace 全量刷新覆盖；后续可优化成只发布受影响打开文档。
- 依赖文件 parse failure 是否跟随引用者提示。
- LSP 是否维护诊断队列并在 `didChange` 后按依赖图局部刷新。

### 4. 继续优化 Workspace Index

当前模块级缓存是正确性优先的可靠版本。

可继续优化：

- 增加 resolver source stamp/version 接口，避免每次都读取完整磁盘/虚拟文本。
- 建 dependency graph，局部失效依赖链。
- 给 `AuroraWorkspaceIndexCache` 增加测试 hook 或 benchmark，量化缓存命中收益。

### 5. Visual Studio 2026 插件方向

当前已具备 VS 插件外壳和 VSIX 打包，下一阶段建议：

- 插件层继续只处理 VS 文档/项目系统/调试 UI 适配。
- 语言语义能力继续集中在 `AuroraScript.LanguageServices`。
- 插件侧优先补实机验收和发布细节。
- 后续调试变量观察需继续梳理 runtime/debugger 协议和 VS 调试适配层。

### 6. NuGet 发布前 API 检查

需要检查 public API：

- `IScriptSourceResolver`
- `ScriptSourceReference`
- `MemoryScriptSource`
- `CompilerOptions.SourceResolver`
- `CompilerOptionsBuilder.WithSourceResolver`
- `AuroraLanguageServiceOptions.SourceResolver`

确认 XML docs、命名、异常行为和跨平台路径行为符合 NuGet 用户预期。
