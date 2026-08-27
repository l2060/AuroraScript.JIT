# AuroraScript MCP Server

AuroraScript 的 stdio MCP 服务器，为支持 MCP 的 AI 编码客户端提供脚本文档、运行时 API 元数据、示例、诊断、校验和执行能力。

> The AuroraScript stdio MCP server exposes script documentation, runtime API metadata, examples, diagnostics, validation, and execution to MCP-capable AI coding clients.

适用版本：**4.0.0**。

> Applies to **4.0.0**.

## 安装

```bash
dotnet tool install --global AuroraScript.Mcp --version 4.0.0
```

更新已有安装：

> Update an existing installation:

```bash
dotnet tool update --global AuroraScript.Mcp --version 4.0.0
```

工具命令为 `aurora-mcp`。

> The tool command is `aurora-mcp`.

## Codex 配置

```bash
codex mcp add aurora-script -- aurora-mcp
```

也可以手动加入 Codex 配置：

> You can also add it to the Codex configuration manually:

```toml
[mcp_servers.aurora-script]
type = "stdio"
command = "aurora-mcp"
startup_timeout_sec = 10
tool_timeout_sec = 60
enabled = true
```

## 完整文档

资源 URI、全部工具的 `Parameters:` / `Returns`、请求示例、工作区覆盖规则和故障排查见 [Wiki：MCP 工具](https://github.com/l2060/AuroraScript.JIT/wiki/Tooling-MCP)。

> See [Wiki: MCP Tooling](https://github.com/l2060/AuroraScript.JIT/wiki/Tooling-MCP) for resource URIs, every tool's `Parameters:` / `Returns`, request examples, workspace-overlay rules, and troubleshooting.

## 模块名称与工具执行

`aurora_check_script` 和 `aurora_check_file` 可以校验匿名模块；没有 `@module(NAME);` 时不会从文件名推导默认名称。`import` / `include` 始终通过 Source Resolver 路径解析，因此依赖文件通常可以保持匿名。

`aurora_run_script` 和 `aurora_run_file` 的模块模式通过宿主模块名执行，所以编译后的模块图中必须存在与 `moduleName` 相同的显式 `@module` 名称；通常由入口模块声明。`aurora_run_script` 省略 `moduleName` 时使用 `TEST`，此时模块图中应包含 `@module(TEST);`。

脚本需要按显式名称动态获取已加载模块时可使用 `global.getModule("NAME")`；未命名或不存在的模块返回 `null`。`global.modules` 仍只以解析后的 `FullPath` 为键，不会增加模块名别名。该 API 已包含在 MCP 的 `aurora://schema/runtime-api` 资源和运行时 API 查询工具中。

宿主侧用 C# 实现类型化脚本全局时，阅读 MCP 资源 `aurora://docs/host-integration` 与 `aurora://schema/host-api` 中的 `[AuroraBuiltinGlobal]` / `[AuroraExport]` 说明；不要猜测 Bonding 回调签名。

> `aurora_check_script` and `aurora_check_file` can validate anonymous modules; no default name is derived from a filename when `@module(NAME);` is absent. Imports and includes are resolved by Source Resolver path, so dependency files can normally remain anonymous.
>
> Module mode in `aurora_run_script` and `aurora_run_file` executes through the host module-name API. The compiled graph must contain the same explicit `@module` name as `moduleName`, normally on the entry module. When `aurora_run_script` omits `moduleName`, it defaults to `TEST`, so the graph should contain `@module(TEST);`.
>
> Scripts can use `global.getModule("NAME")` to look up an already loaded module by its explicit name; anonymous or missing modules return `null`. `global.modules` remains keyed only by resolved `FullPath`, with no module-name aliases. The API is available through the MCP `aurora://schema/runtime-api` resource and runtime API query tools.
>
> When generating C# host globals, read `aurora://docs/host-integration` and `aurora://schema/host-api` for `[AuroraBuiltinGlobal]` / `[AuroraExport]` rather than inventing Bonding callback signatures.

## 开发

```bash
dotnet build language-tools/AuroraScript.Mcp/AuroraScript.Mcp.csproj -c Release
dotnet pack language-tools/AuroraScript.Mcp/AuroraScript.Mcp.csproj -c Release -o artifacts/mcp-tool
```
