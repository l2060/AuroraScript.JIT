# Benchmark 内存申请路径调查

基于当前仓库代码与脚本，内存与 Gen0 的主要来源如下。

## 1) `TestCreateDomain` (13,152 B)

`CreateDomain` 每次调用都会创建一个新的运行域对象图：

- 新建 `ScriptGlobal`（内部又新建 `Modules` 对象、`Dictionary`，并定义 `modules` 属性）。
- `userState` 通过 `ClrMarshaller.ToScript` 包装为脚本对象。
- 新建 `ScriptDomain`。
- 新建 `ScriptContext`。
- `EntryPoint.Invoke` 反射调用时创建参数数组（`[ctx, Array.Empty<ScriptDatum>()]`）与反射调用开销。

这些对象基本都是短命对象，因此会集中进入 Gen0。

## 2) `testMD5` / `testMD5_100` (102,784 B / 10,253,848 B)

`testMD5` 调用 `md5.MD5("12345")`，`testMD5_100` 在循环中调用 100 次。其内存放大几乎线性，说明是“每次调用内部产生大量短命对象”。

脚本层热点：

- `ConvertToWordArray` 中创建新数组 `Array(lNumberOfWords - 1)`。
- `Utf8Encode`、`WordToHex`、最终拼接 `WordToHex(a)+...` 都在进行字符串拼接，产生大量中间字符串。

运行时层热点：

- `ScriptDatum.WriteAsString` / `FromString(string)` 每次都会 `new StringValue(value)`（包装字符串对象），不会复用。
- `StringValue.Of(string)` 直接分配新对象。

因此 MD5 场景不是单一大对象，而是**大量小对象（字符串 + StringValue 包装 + 数组对象）**，表现为高 Gen0 次数。

## 3) `testDraw` (53,280 B)

此函数执行脚本逻辑并伴随常规对象/字符串操作，内存级别介于 `CreateDomain` 与 `MD5` 之间，符合“有业务对象创建但字符串密度低于 MD5”特征。

## 4) `testFor1E` (192 B, Gen0≈0)

`testFor1E` 主要是纯数值循环，几乎不构造对象；仅保留了每次 `Execute` 的固定运行时开销：

- `Execute` 内部新建 `ScriptContext`。
- 其它极少量调用框架开销。

这与结果中“超长耗时但几乎无分配”一致。


## 5) 关于统一 `ScriptDatum[]` 传参是否影响分配

会有影响，但要分场景看：

- **当前这组 benchmark（无参调用）影响很小**：
  - `Execute(module, method)` 直接传 `Array.Empty<ScriptDatum>()`，不会为参数额外新建数组。
- **有参调用时会产生额外 Gen0**：
  - 走 `params ScriptDatum[]` 且调用点不是现成数组时，C# 会在调用端构造参数数组。
  - 走 `Execute(..., params ScriptObject[] arguments)` 时，会进入 `ClrMarshaller.ToDatums(arguments)`，这里会 `new ScriptDatum[arguments.Length]`。
  - 走 `ToDatums(object[])` 也同理会新建 `ScriptDatum[]`。

另外，`ClosureFunction.InvokeClr(ctx, params ScriptDatum[] args)` 只是接收数组并向下传递，不会再复制一份；真正的数组分配主要发生在**调用端的 params 打包**或 `ToDatums` 转换阶段。

### 对这次结果的含义

- `testFor1E` 几乎无分配，说明其关键路径没有参数数组放大量创建（与无参调用一致）。
- `testMD5/testMD5_100` 的主因仍是字符串/对象创建，`ScriptDatum[]` 仅是次要项。

## 结论

1. **MD5 的高内存与高 Gen0 属于预期行为**：脚本实现大量字符串拼接 + 运行时字符串包装对象分配叠加导致。
2. **CreateDomain 的分配主要来自生命周期初始化对象图**，不是泄漏。
3. **For 循环基准证明运行时基础开销很低**，问题集中在“字符串/对象密集型脚本”。

## 可落地优化方向

1. 在运行时把 `ScriptDatum.WriteAsString(string)` 改为可选 intern/池化策略（已有 `StringValue.Intern` 但当前路径未使用）。
2. 为脚本层字符串拼接提供 `StringBuffer` 风格替代，避免 `a+b+c` 产生大量临时字符串。
3. 为热点脚本（如 MD5）提供原生 CLR 实现并绑定到脚本，以减少解释层对象创建。
4. 如果频繁创建域，考虑域对象池或复用 `ScriptDomain`（视隔离语义允许范围）。
