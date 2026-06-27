# AuroraScript 当前进度交接

更新时间：2026-06-27

## 当前目标

当前主线是让 AuroraScript 支持更完整的语言工具链与可扩展源码加载机制：

- 脚本引擎支持通过 `IScriptSourceResolver` 从文件系统、内存、虚拟文件系统等来源加载脚本。
- 语言服务和 LSP 使用 workspace-first 模型，支持打开文档优先于磁盘文件。
- 为后续 Visual Studio 2026 插件提供 definition/references/rename/semantic tokens/diagnostics 等基础能力。
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

`TextDocumentStore` 已移除，LSP 文档状态统一走 `AuroraLanguageService.Workspace`。

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

### 2. 整理 SourceResolver 用户文档

建议把 `CustomSourceResolverUsageTests` 中的 `VirtualFileSystemSourceResolver` 精简成 NuGet 用户文档示例：

- 如何配置 `EngineOptions.WithCompiler(compiler => compiler.SourceResolver = resolver)`。
- 如何设置虚拟 `BaseDirectory`。
- 如何打开入口脚本。
- import/include 路径解析规范。
- resolver 的线程安全要求。

### 3. 完善语言服务诊断策略

当前 diagnostics 主要是当前文档 parse/semantic diagnostics。

后续需要明确：

- import/include 解析失败是否发布到所有受影响打开文档。
- 依赖文件 parse failure 是否跟随引用者提示。
- LSP 是否维护诊断队列并在 `didChange` 后刷新依赖图。

### 4. 继续优化 Workspace Index

当前模块级缓存是正确性优先的可靠版本。

可继续优化：

- 增加 resolver source stamp/version 接口，避免每次都读取完整磁盘/虚拟文本。
- 建 dependency graph，局部失效依赖链。
- 给 `AuroraWorkspaceIndexCache` 增加测试 hook 或 benchmark，量化缓存命中收益。

### 5. Visual Studio 2026 插件方向

下一阶段建议先稳定 LSP，再做 VS 插件外壳：

- 首选 VS 插件作为 LSP client，启动 `AuroraScript.LanguageServer`。
- 插件层只处理 VS 文档/项目系统/调试 UI 适配。
- 语言语义能力继续集中在 `AuroraScript.LanguageServices`。

### 6. NuGet 发布前 API 检查

需要检查 public API：

- `IScriptSourceResolver`
- `ScriptSourceReference`
- `MemoryScriptSource`
- `CompilerOptions.SourceResolver`
- `CompilerOptionsBuilder.WithSourceResolver`
- `AuroraLanguageServiceOptions.SourceResolver`

确认 XML docs、命名、异常行为和跨平台路径行为符合 NuGet 用户预期。
