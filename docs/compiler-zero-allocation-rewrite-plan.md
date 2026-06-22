# 编译期低分配高性能重构方案

本文档用于执行 AuroraScript 编译链路的终极版性能重构。目标不是保持旧 API 兼容，而是在保证语义正确的前提下，重构 lexer、parser、AST/IR、emitter 相关模型，使编译期间尽量接近 0 临时分配，并最大化编译性能。

## 最终落地状态

本方案最初将完整 arena/handle AST 作为候选终态。基于实际 Benchmark，最终实现采用了更偏向 CPU 性能的混合架构：

- Lexer 使用首字符分派、手写 scanner、20 字节 packed `LexToken` 和 `ArrayPool<LexToken>` 分块缓冲。扫描不创建 token class，parser 完成后立即归还 token chunks。
- 标识符在 lexer 内按 span intern；关键字、操作符和标点只保存整数 symbol id。只有进入最终 AST/诊断边界时才物化 token class。
- Parser 保留 class AST 作为最终编译结果，但移除了仅用于 range 的关键字 token 物化，并将源码文件传播改为无 iterator 的复用 visitor。最终 AST 节点和字符串属于结果分配，不计为可消除的临时表示。
- Emitter 保留直接 AST 引用以避免 NodeId 间接访问的 CPU 成本；闭包分析器、常量分析器和声明 visitor 改为实例内复用，无嵌套闭包的 block 不创建 analyzer 或字典快照。
- DynamicMethod 参数签名数组静态复用，visitor 的只读列表遍历改为索引循环，移除 `$args` 检测中的 `yield` 状态机分配。

未采用完整 arena AST 的原因是性能优先级高于绝对分配数：当前 AST 是 emitter 直接消费的最终模型，改为 arena 会引入 NodeId 查表与大范围语义风险，而实测 Parser 分配已下降 61.01%、FullCompile 分配已下降 60.93%，同时 FullCompile 约加速 3.98 倍。剩余主要分配来自最终 AST、intern 后字符串及 Reflection.Emit 产物。

最终数据见 `docs/benchmarks/compiler-pipeline/compare.md`。下文保留初始目标架构和阶段计划，作为设计决策记录。

## 目标

- 建立改造前 Benchmark 基线，记录 CPU、Allocated、Gen0/Gen1/Gen2。
- 重构 lexer 为 streaming scanner，扫描热路径不创建 token 对象、不构造临时字符串。
- 重构 parser 消费轻量 `LexToken`，不再依赖 `Token class`、`Token.Value`、子类类型判断。
- 重构 AST 模型为 arena/handle 结构，避免大量 AST class 对象与 `List<AstNode>` 分配。
- 重构 emitter/scope/analysis 使用 `NameId` / `StringId`，避免重复字符串物化和字符串比较。
- 改造后用同一 Benchmark 项目和同一数据集复测，输出 before/after 对比结果。

## 非目标

- 不保持旧 `Token` / `ValueToken` / `IdentifierToken` API 兼容。
- 不为了兼容旧 AST visitor 模型保留 class AST。
- 不追求运行期 0 分配；本次聚焦编译期间。
- 不通过牺牲语义、错误定位、调试 range 来换取性能。

## 基线 Benchmark

现有 `benchmark` 主要测运行时执行，`optimization-benchmark` 也以调用、数值、对象、数组为主。改造前必须新增编译期专用 Benchmark，建议放在新项目：

```text
compiler-benchmark/
  CompilerBenchmark.csproj
  Program.cs
  CompilerPipelineBenchmarks.cs
  BenchmarkScripts/
```

### Benchmark 场景

必须覆盖以下维度：

- `LexerOnly_Small`：小文件，普通变量、函数、表达式。
- `LexerOnly_Large`：大文件，重复声明、长表达式。
- `LexerOnly_CommentsWhitespace`：大量空白、行注释、块注释。
- `LexerOnly_StringsTemplatesRegex`：字符串、模板字符串、正则字面量。
- `LexerOnly_UnicodeIdentifiers`：中文标识符和 ASCII 混合。
- `ParseOnly_Small`：lexer + parser，不进入 emitter。
- `ParseOnly_Large`：大模块 AST 构建。
- `ParseOnly_TemplateInterpolation`：模板字符串插值表达式。
- `EmitOnly_ParsedModule`：复用预解析结果，单独测 emitter。
- `FullCompile_SingleModule`：lexer + parser + emitter。
- `FullCompile_MultiModule`：多模块 import/include。
- `CompileBlock`：`AuroraEngine.CompileBlock` 热路径。

### 采集指标

BenchmarkDotNet 必须启用：

- `MemoryDiagnoser`
- `MarkdownExporter`
- `JsonExporter`
- `CsvExporter`
- `MinColumn`
- `MaxColumn`
- `MeanColumn`
- `MedianColumn`

每个 case 记录：

- `Mean`
- `Median`
- `Allocated`
- `Gen0`
- `Gen1`
- `Gen2`
- `Operations/Second`（如有）
- 源码长度、token 数、AST 节点数、模块数

### 运行命令

改造前：

```powershell
dotnet run -c Release --project compiler-benchmark -- --filter *CompilerPipelineBenchmarks*
```

快速迭代模式可以保留一个非 BDN 的手动测量入口，输出 CSV：

```text
Case,SourceBytes,TokenCount,NodeCount,ElapsedMs,AllocatedBytes,Gen0,Gen1,Gen2
```

基线结果保存到：

```text
docs/benchmarks/compiler-pipeline/before/
```

改造后同命令运行，保存到：

```text
docs/benchmarks/compiler-pipeline/after/
```

最终输出：

```text
docs/benchmarks/compiler-pipeline/compare.md
```

## 目标架构

### 编译上下文

新增 `CompilationSession`，统一承载编译期短生命周期状态：

```csharp
internal sealed class CompilationSession
{
    public SourceText Source;
    public NameTable Names;
    public StringTable Strings;
    public NodeArena Nodes;
    public DiagnosticSink Diagnostics;
}
```

该对象按模块或编译任务创建，编译结束后整体释放。内部可使用 `ArrayPool<T>` / 自定义可增长 buffer，避免分散小对象。

### SourceText

替代当前 `source.ReadSource().Replace("\r\n", "\n")`。

要求：

- 保留原始源码字符串，不复制整份源码。
- scanner 原生识别 `\n`、`\r\n`、`\r`。
- `SourceSpan` 不再在每个节点保存 `FileName string`，改保存 `SourceId` 或引用共享 `SourceText`。

建议模型：

```csharp
internal sealed class SourceText
{
    public int SourceId;
    public string Text;
    public string FullPath;
    public string FileName;
    public string BaseDirectory;
}
```

### Symbol 模型

当前 `Symbols` 是 class，并通过反射构建字典。终极版改为小整数 id：

```csharp
internal enum SymbolId : ushort
{
    None,
    KwDeclare,
    KwIf,
    KwElse,
    KwConst,
    KwFunction,
    KwFunc,
    KwVar,
    KwReturn,
    KwDebugger,
    KwBreak,
    KwYield,
    KwContinue,
    KwEnum,
    KwFor,
    KwNew,
    KwDelete,
    KwWhile,
    KwTry,
    KwCatch,
    KwFinally,
    KwThrow,
    KwImport,
    KwInclude,
    KwFrom,
    KwExport,
    OpTypeof,
    OpIn,
    // ...
}
```

配套静态表：

- `SymbolText[SymbolId]`
- `SymbolType[SymbolId]`
- `KeywordLookup`
- `PunctuatorLookup`
- `OperatorLookup`

禁止在 parser 热路径用字符串拼接 key。

### Token 模型

废弃 `Token class` 层级，替换为值类型：

```csharp
internal enum TokenKind : byte
{
    Eof,
    Identifier,
    Number,
    String,
    StringTemplate,
    Regex,
    Keyword,
    Operator,
    Punctuator,
    Null,
    Boolean
}

internal readonly struct LexToken
{
    public readonly TokenKind Kind;
    public readonly SymbolId Symbol;
    public readonly SourceSpan Range;
    public readonly int Offset;
    public readonly int Length;
    public readonly int NameId;
    public readonly int StringId;
    public readonly double NumberValue;
    public readonly bool BooleanValue;
}
```

原则：

- 操作符、标点、关键字不保存字符串。
- 标识符保存 `NameId`。
- 字符串保存 `StringId` 或 source slice。
- 数字保存 `double NumberValue`，必要时保留 raw slice 供诊断。
- 正则保存 pattern/flags 的 id 或 source slice，不构造 literal/pattern/flags 临时字符串。

### NameTable 和 StringTable

所有长期需要的字符串统一经过表管理：

```csharp
internal sealed class NameTable
{
    public int Intern(SourceText source, int offset, int length);
    public string GetString(int id);
}

internal sealed class StringTable
{
    public int InternDecoded(ReadOnlySpan<char> value);
    public int InternSlice(SourceText source, int offset, int length);
    public string GetString(int id);
}
```

要求：

- 同一个标识符只物化一次字符串。
- scope、closure、emitter 优先用 `int NameId` 做 key。
- 只有 emit metadata、IL string 常量、错误信息需要时才调用 `GetString`。

## Lexer 重构

### API

`AuroraLexer` 改为 streaming reader：

```csharp
internal ref struct TokenReader
{
    public LexToken Peek(int offset = 0);
    public LexToken Next();
    public bool Match(SymbolId symbol);
    public LexToken Expect(SymbolId symbol);
    public LexerSnapshot CreateSnapshot();
    public void RestoreSnapshot(LexerSnapshot snapshot);
}

internal readonly struct LexerSnapshot
{
    public readonly int Position;
    public readonly int Offset;
    public readonly int Line;
    public readonly int Column;
    public readonly LexToken PreviousSignificantToken;
}
```

`LexerSnapshot` 必须是 struct，不允许 class 分配。

### 扫描策略

使用首字符 `switch` + 专用扫描函数：

- `ScanIdentifierOrKeyword`
- `ScanNumber`
- `ScanString`
- `ScanStringTemplate`
- `ScanSlash`：行注释、块注释、正则、除法相关操作符。
- `ScanPunctuatorOrOperator`
- `SkipWhitespaceAndComments`

禁止：

- 每个 token 遍历规则对象。
- 虚方法规则匹配。
- whitespace/comment/newline 生成字符串。
- 扫描期 `Substring` / `Slice().ToString()`。
- 构造完整 token list。

### 正则字面量

只在当前字符为 `/` 时判断上下文。判断依据保存为 `PreviousSignificantToken`，不要每个 token 都调用。

### 数字解析

手写解析：

- 十进制整数/小数跳过 `_`。
- 十六进制直接累加整数值。
- 不再使用 `value.Replace("_", "")`。
- 解析失败时用 source slice 构建错误信息。

### 字符串解析

无转义字符串：

- 直接 intern slice 或延迟物化。

有转义字符串：

- 使用 `ValueStringBuilder` 或 `ArrayPool<char>` 解码。
- 只产生最终字符串，不产生中间字符串。

### 模板字符串

模板 token 只标记范围。parser 在同一 source 范围内处理 `${...}`：

- 不创建 `raw = Substring(...)`。
- 不创建 `exprText`。
- 不创建子 `TextSource`。
- 不创建子 `AuroraLexer` / `AuroraParser`。

使用 bounded parser：

```csharp
ParseExpressionUntilTemplateBrace(templateExpressionEndOffset)
```

## Parser 重构

### Parser 输入

Parser 直接消费 `TokenReader` / `LexToken`。

旧 API 替换：

- `NextOfKind(Symbols.X)` -> `Expect(SymbolId.X)`
- `TestNext(Symbols.X)` -> `Match(SymbolId.X)`
- `TestSymbol(Symbols.X)` -> `Peek().Symbol == SymbolId.X`
- `NextOfKind<IdentifierToken>()` -> `ExpectIdentifier()`
- `NextOfKind<ValueToken>()` -> `ExpectLiteral()`

### Operator 表

当前 `Operator.FromSymbols` 会构造字符串 key：

```csharp
symbols.Name + "," + hasLHSOperand
```

替换为数组查表：

```csharp
private static readonly OperatorInfo[] PrefixOperators;
private static readonly OperatorInfo[] InfixOperators;
private static readonly OperatorInfo[] PostfixOperators;
```

`GetPrecedence(Peek())` 变成 O(1) 数组访问，无字符串分配。

### AST/IR Arena

终极版不再创建 class AST 对象图。改为 arena + node handle：

```csharp
internal readonly struct NodeId
{
    public readonly int Value;
}

internal enum NodeKind : ushort
{
    Module,
    Block,
    Function,
    VariableDeclaration,
    Name,
    Literal,
    Binary,
    Unary,
    Assignment,
    Call,
    GetProperty,
    GetElement,
    SetProperty,
    SetElement,
    If,
    While,
    For,
    ForIn,
    Return,
    Throw,
    Try,
    Import,
    // ...
}
```

`NodeArena` 使用结构化数组或 pooled arrays：

```csharp
internal sealed class NodeArena
{
    public NodeId Add(NodeKind kind, SourceSpan range);
    public ref NodeData Get(NodeId id);
    public NodeListBuilder CreateListBuilder();
}
```

节点字段保存：

- `NameId`
- `StringId`
- `NumberValue`
- `SymbolId`
- `OperatorId`
- 子节点 `NodeId`
- 子列表 `NodeListId`

不要在节点上保存 `Token`。

### 列表分配

替代所有高频 `new List<T>()`：

- 函数参数列表
- block statements
- call arguments
- array elements
- object properties
- module functions/imports

使用 arena list：

```csharp
internal readonly struct NodeListId
{
    public readonly int Start;
    public readonly int Count;
}
```

构建期用 pooled builder，完成后 compact 到 arena。

### Range

保留 `SourceSpan`，但去掉 per-node `FileName string`。使用：

```csharp
public struct SourceSpan
{
    public int SourceId;
    public int StartLine;
    public int StartColumn;
    public int EndLine;
    public int EndColumn;
    public int Offset;
    public int Length;
}
```

删除 `SetSourceRecursive`，节点创建时 range 已完整。

### 需要顺手修正的 parser 逻辑

- 表达式 range 不再通过 `ChildNodes.Last()` 推断，直接使用刚解析的 right node range。
- for/in 回溯 snapshot 改 struct，避免分配。
- lambda 名称不再拼接 `"lambda_" + line + "_" + column"`，改用稳定 synthetic name id。
- 空数组缺项产生的 `NullToken` 改为 arena literal null node。
- 模板字符串插值用同一 parser bounded scope，避免子 parser。
- 错误信息按需从 source slice 生成 token text。

## Scope、Closure、Emitter 重构

### Scope

当前 `CodeScope` 使用 `string` 和线性 `FindByNameLocal`。改为 `NameId`：

```csharp
internal sealed class CodeScope
{
    private PooledDictionary<int, DeclareObject> _variables;
    public DeclareObject Declare(int nameId, DeclareType type, MemberAccess access, NodeId variableNode);
    public bool Resolve(int nameId, out DeclareObject value);
}
```

优势：

- scope lookup 从字符串比较变成 int hash/compare。
- 不重复物化相同 identifier string。
- closure 分析的 `HashSet<string>` 改为 `HashSet<int>`。

### Emitter 输入

`CILEmitter` 不再访问 `node.Name.Value`、`node.Identifier.Value`。统一：

```csharp
var name = session.Names.GetString(nameId);
```

仅在以下位置物化字符串：

- IL metadata 名称。
- `LoadStringConstant`。
- 错误信息。
- 对外 API 返回模块名、导入路径等。

### 常量池

常量池 key 优先使用结构化 key：

```csharp
internal readonly struct ConstantKey
{
    public readonly LiteralKind Kind;
    public readonly int StringId;
    public readonly double Number;
    public readonly bool Boolean;
}
```

避免 `Dictionary<object, LocalBuilder>` 的 boxing/装箱和 object hash。

### Visitor 替换

旧 `IAstVisitor` 遍历 class AST。终极版改为：

```csharp
internal abstract class NodeVisitor
{
    protected readonly CompilationSession Session;
    public void Visit(NodeId node);
}
```

或者在 emitter 中直接 `switch (node.Kind)`，避免虚调用和 `IEnumerable<AstNode>` 遍历。

## 需要删除或替换的旧模块

- `src/Compiler/ToKen.cs`
- `src/Compiler/Tokens/*`
- `src/Compiler/Scanning/TokenRules.cs`
- `AuroraLexer.ParseTokens`
- `List<Token> tokens`
- `Token.Value` 热路径
- `Symbols` class 反射初始化
- `Operator.FromSymbols` 字符串字典
- class AST 强引用 token 的字段

可保留兼容名字但内部重写的模块：

- `AuroraLexer`
- `AuroraParser`
- `CILEmitter`
- `SourceSpan`
- `AuroraParseException` / `AuroraLexicalException`

## 实施阶段

### 阶段 0：Benchmark 基线

1. 新增 `compiler-benchmark`。
2. 添加标准测试脚本数据。
3. 运行 Release Benchmark。
4. 保存 before 数据。
5. 记录当前 commit hash、SDK 版本、CPU 信息。

验收：

- `docs/benchmarks/compiler-pipeline/before/` 有完整输出。
- 覆盖 lexer/parser/emit/full compile。

### 阶段 1：核心基础设施

1. 引入 `SourceText`、`CompilationSession`。
2. 引入 `NameTable`、`StringTable`。
3. 引入 `SymbolId` 和静态 symbol/operator 表。
4. 引入新 `SourceSpan`。

验收：

- 不接入旧 parser，只保证基础类型测试通过。
- symbol/operator lookup 不分配。

### 阶段 2：Streaming Lexer

1. 实现 `TokenReader`。
2. 实现 `LexToken`。
3. 实现各类扫描函数。
4. 删除规则对象调度。
5. 添加 lexer-only 单测与 benchmark。

验收：

- lexer-only benchmark 分配接近 0，除必要 identifier/string intern。
- 所有现有脚本 token 序列语义一致。

### 阶段 3：Parser + Arena AST

1. Parser 改为消费 `LexToken`。
2. 引入 `NodeArena`、`NodeId`、`NodeKind`。
3. 重写表达式 Pratt parser。
4. 重写 statement/module parser。
5. 重写模板字符串 parser。

验收：

- parse-only benchmark 分配主要来自 arena buffer。
- 不再创建 token class、AST class、子 lexer/parser。

### 阶段 4：Analysis/Scope/Emitter

1. `CodeScope` 改 `NameId`。
2. Declaration/Closure/ConstantHoister 改 NodeArena visitor。
3. `CILEmitter` 改 `NodeId` 输入。
4. 常量池 key 结构化。
5. 只在必要边界物化字符串。

验收：

- full compile 通过所有现有脚本。
- emit-only benchmark 无明显回退。

### 阶段 5：清理旧模型

1. 删除旧 token classes。
2. 删除 `TokenRules`。
3. 删除旧 AST token 字段。
4. 删除临时 adapter。
5. 清理错误信息和诊断路径。

验收：

- 仓库无旧 token model 热路径引用。
- `rg "Token.Value|IdentifierToken|ValueToken|TokenRules"` 无编译主路径残留。

### 阶段 6：复测与对比

1. 运行同一 Benchmark 命令。
2. 保存 after 数据。
3. 生成 `compare.md`。
4. 标记未达标 case 并继续优化。

验收：

- 输出完整 before/after 表格。
- 对每个退化 case 给出解释或修正。

## 预期收益

实际结果以 Benchmark 为准。当前结构下，合理预期：

- Lexer allocated bytes 下降 80% 到 95%+。
- Lexer CPU 提升 2x 左右，大文件/注释多场景可能更高。
- Parser allocated bytes 下降 50% 到 80%，取决于 arena 化程度。
- Parser CPU 提升 1.3x 到 2x。
- Full compile allocated bytes 下降 30% 到 60%。
- Full compile CPU 提升 1.1x 到 1.6x。
- 小脚本收益较小，大脚本、批量编译、动态编译收益明显。

若 emitter 保持 string-heavy，整体收益会被明显削弱；因此 emitter/scope 必须同步迁移到 id 模型。

## 性能验收门槛

改造完成后，除非有明确语义修复导致额外成本，否则：

- `FullCompile_*` 任一 case 不允许 CPU 回退超过 5%。
- `LexerOnly_*` 分配至少下降 80%。
- `ParseOnly_*` 分配至少下降 50%。
- `FullCompile_*` 分配至少下降 30%。
- Gen0 次数必须显著下降。
- 所有现有 examples/scripts 编译结果一致。

## 风险与处理

### 风险：arena AST 改动面很大

处理：

- 先保持语义测试和 benchmark 同步。
- 每完成一个语法族就运行 compile benchmark 和现有脚本构建。

### 风险：错误信息需要 token text

处理：

- 正常路径不生成 token text。
- 异常路径通过 `SourceText.Text.AsSpan(offset, length).ToString()` 按需生成。

### 风险：模板字符串语义变化

处理：

- 专门建立模板字符串测试集。
- 覆盖嵌套 `{}`、转义、空插值、字符串中的 brace。

### 风险：NameId 影响对外 API

处理：

- 编译内部统一 id。
- 对外 API 边界统一 `NameTable.GetString(id)`。

### 风险：ArrayPool 使用错误导致数据污染

处理：

- `CompilationSession.Dispose()` 统一归还 buffer。
- Debug build 下开启 buffer ownership 检查。

## 最终交付物

- `compiler-benchmark` 项目。
- before benchmark 数据。
- after benchmark 数据。
- `compare.md` 对比报告。
- 新 lexer/parser/arena/emitter 实现。
- 删除旧 token/rule/class AST 热路径。
- 编译语义回归测试通过。

## 执行原则

- 性能优化以 benchmark 为准，不凭感觉判断。
- 热路径不为诊断、兼容、调试提前分配字符串。
- 编译结果对象可以分配，临时中间对象必须尽量消除。
- 发现旧架构不合理处直接修正，不保留兼容层。
- 每个阶段都要能独立验证，避免大爆炸式不可定位回归。
