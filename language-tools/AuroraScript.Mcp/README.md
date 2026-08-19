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

## 开发

```bash
dotnet build language-tools/AuroraScript.Mcp/AuroraScript.Mcp.csproj -c Release
dotnet pack language-tools/AuroraScript.Mcp/AuroraScript.Mcp.csproj -c Release -o artifacts/mcp-tool
```
