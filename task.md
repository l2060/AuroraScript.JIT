# TypedDocument 修复与扩展任务文档

  状态：待执行
  本轮仅定义任务，不修改代码。

  ## 一、任务目标

  在不引入 codec、版本迁移、注释保留和资源限制设计的前提下：

  1. 统一独立 TDoc reader、脚本 tdoc 字面量和编辑器验证的类型绑定规则。
  2. 修复所有 Packed Array 的静默溢出和隐式截断。
  3. 使 $(...) 结果严格服从外层显式类型。
  4. 使 TDoc Date 严格使用当前 DateTimeFormat。
  5. 让脚本字面量支持当前已注册 CLR alias。
  6. 增加六种 Packed Array。
  7. 让编辑器诊断覆盖内置 TDoc 的形状、范围和日期错误。
  8. 保持现有 EmitTypeNames 和注释策略不变。

  本轮明确不做：

  - codec、schema version、migration。
  - 注释/trivia 保留和安全写回。
  - 资源长度限制。
  - CLR alias 名称的编辑器诊断。
  - EmitTypeNames 行为调整。

  ## 二、总体架构

  ### 2.1 编译器生成 Typed Literal

  脚本字面量不再先构造普通 ScriptObject 再转换。

  编译流程改为：

  AuroraScript AST
      ↓
  TypedDocumentLiteral 结构/构建调用
      ↓
  统一 TypedDocumentBinder
      ↓
  直接创建目标类型

  编译器生成的内容应保留：

  - 根类型 alias。
  - 对象成员名。
  - 成员显式类型。
  - readonly 标志。
  - 数组元素类型。
  - 每个表达式的运行时结果。

  运行时使用 typed-literal builder 或等价的内部结构暂存这些成员，最后由 binder 直接创建：

  - ScriptObject
  - ScriptArray
  - Packed Array
  - ScriptDate、ScriptRegex 等特殊类型
  - 已注册 CLR 对象

  对于注册 CLR 对象，不得先构造普通 ScriptObject 再反射转换。

  ### 2.2 统一绑定层

  新增内部共享绑定层，例如：

  TypedDocumentBinder
  TypedDocumentTypeBinding
  TypedDocumentPackedRules

  职责：

  - 内置类型识别和验证。
  - 显式类型转换。
  - Packed Array 元素验证。
  - Date 格式解析。
  - CLR alias 运行时查找。
  - CLR descriptor contract 的构造和成员赋值。
  - 统一数据路径错误。

  调用关系：

  独立 TypedDocumentReader ─┐
  脚本 TypedCilEmitter     ├─> TypedDocumentBinder
  ModuleInitializerEmitter ┘
  编辑器 TDoc 验证器       ──> 纯验证入口

  独立 reader 仍保持流式读取；它可以直接调用 binder 的无中间树验证方法。脚本字面量则使用 typed-literal builder 调用同一 binder。

  ## 三、具体修复内容

  ### 3.1 Packed Array 溢出检查

  所有整数 Packed Array 必须按以下顺序处理：

  1. 确认是有限数值。
  2. 确认是整数。
  3. 检查上下界。
  4. 检查通过后再转换为目标 CLR 类型。

  禁止先执行 Conv_I1、Conv_I4 等窄化指令再检查。

  错误行为：

  tdoc Int8Array [255]

  必须抛出范围异常，不能得到 -1。

  动态插值也必须同样检查：

  tdoc Int8Array $([255])

  常量错误可以在编译阶段报告；动态值在运行时报告，并带有数组索引路径。

  ### 3.2 BooleanArray

  BooleanArray 只接受四种输入：

  true
  false
  0
  1

  语义为：

  false -> false
  true  -> true
  0     -> false
  1     -> true

  以下值必须拒绝：

  2、-1、0.5、NaN、Infinity、字符串、对象

  运行时数组最终只保存 bool，因此 writer 统一输出：

  BooleanArray [true, false]

  不保留输入是 0/1 还是布尔字面量。

  ### 3.3 外层显式类型绑定

  以下规则统一由 binder 执行：

  tdoc Date $("...")
  tdoc Object $(value)
  tdoc Array $(value)
  tdoc Int8Array $(value)
  tdoc User $(value)

  要求：

  - Date 插值必须得到可转换为 Date 的值。
  - Object 只能绑定对象。
  - Array 只能绑定普通数组。
  - Packed Array 必须逐元素验证。
  - CLR alias 必须通过当前 engine 的 ClrRegistry。
  - 不能因为是 $(...) 就跳过外层类型检查。

  无显式类型的插值仍保留原始值：

  tdoc $(value)

  独立 .tdoc 继续禁止 $(...)。

  ### 3.4 Date 格式

  TDoc Date 绑定必须使用：

  engine.Options.Runtime.DateTimeFormat

  规则：

  - 字符串使用严格格式匹配。
  - 不回退到其他 legacy 格式。
  - 数值形式按 ticks 严格验证。
  - 已经是 ScriptDate 的值可直接通过。
  - writer 使用同一份格式输出。

  普通脚本 Date(...) 构造器的既有兼容行为不在本任务内修改；只修 TDoc 路径。

  ### 3.5 已注册 CLR alias

  脚本：

  tdoc User {
      String name "Hanks",
  }

  编译器生成 typed literal 构建过程，运行时由 binder：

  1. 从 context.Engine.ClrRegistry 查找 User。
  2. 验证该注册项是否允许 TDoc 构造。
  3. 按现有 descriptor contract 创建 CLR 对象。
  4. 验证字段、重复字段和 CLR 类型转换。
  5. 返回 ClrInstanceObject。

  未知 alias：

  tdoc Missing {}

  由运行时抛出未知类型错误。

  编辑器不对 CLR alias 名称做诊断，因为编辑器没有可靠的宿主 registry 上下文。

  本轮不增加 codec，也不改变现有 CLR descriptor contract。

  ### 3.6 语法和编辑器诊断

  修正脚本 TDoc 与独立 TDoc 的语法差异：

  - 支持科学计数法。
  - 支持显式类型加引号属性名，例如：

    Object { String "display name" "x" }

  - 保持逗号和尾逗号规则一致。
  - 对内置类型执行值形状和范围验证。
  - 对 Date 常量执行当前 format 验证。
  - 对 Packed Array 常量执行逐元素验证。
  - 诊断包含文件、行、列和数据路径。

  不做：

  - CLR alias 未知名称诊断。
  - codec 执行。
  - 注释保留。

  ## 四、新增 Packed Array

  新增：

  UInt8Array
  Int16Array
  UInt16Array
  UInt32Array
  Int64Array
  UInt64Array

  需要同步实现：

  - ScriptPackedArray 子类。
  - 全局构造器和 AuroraEngine 注册。
  - reader/writer/binder。
  - 编译器 flow type。
  - CIL emitter 和 module initializer。
  - CLR marshaller。
  - 调试视图和枚举访问。
  - 编辑器类型识别。
  - round-trip 和错误测试。

  推荐底层存储：

   TDoc 类型      CLR 存储
  ━━━━━━━━━━━━━  ━━━━━━━━━━
   UInt8Array     byte[]
  ─────────────  ──────────
   Int16Array     short[]
  ─────────────  ──────────
   UInt16Array    ushort[]
  ─────────────  ──────────
   UInt32Array    uint[]
  ─────────────  ──────────
   Int64Array     long[]
  ─────────────  ──────────
   UInt64Array    ulong[]

  ScriptDatum.Number 当前是 double。因此 Int64Array/UInt64Array 对无法被 double 精确表示的动态值必须报错，不能静默截断。若未来需要完整的 64 位整数文本空间，需要另行设计整数词法或 BigInt，不纳入本任务。

  ## 五、注释和 EmitTypeNames

  ### 注释

  不实现注释保留。

  scanner 继续接受并跳过行注释、块注释。Deserialize 继续返回纯数据值，不增加语法树或安全写回模型。

  ### EmitTypeNames

  不处理。

  保留当前默认行为：

  TypedDocumentOptions.Default.EmitTypeNames == false

  本任务不改变默认输出、不修改相关 API，也不处理 /D:/Source Code/AuroraScript.JIT/next.md:443 的陈旧验收文字。

  ## 六、验证方案

  ### 6.1 运行时边界测试

  必须覆盖：

  tdoc Int8Array [127]       // 成功
  tdoc Int8Array [128]       // 范围异常
  tdoc Int8Array [-129]      // 范围异常
  tdoc Int8Array [1.5]       // 整数异常
  tdoc BooleanArray [0, 1, true, false] // 成功
  tdoc BooleanArray [2]      // 类型/范围异常

  新增数组分别测试最小值、最大值、越界值、小数、NaN 和 Infinity。

  ### 6.2 Date 测试

  - 默认 DateTimeFormat 成功解析。
  - 自定义 format 成功解析。
  - 不匹配格式拒绝。
  - 脚本 literal 和独立 Deserialize 结果一致。
  - writer/read round-trip 使用同一格式。

  ### 6.3 插值测试

  覆盖：

  tdoc Date $("...")
  tdoc Object $(42)          // 必须失败
  tdoc Array $(object)       // 必须失败
  tdoc Int8Array $(values)    // 逐元素验证
  tdoc User $(value)          // CLR alias 绑定

  同时测试根值、对象成员和数组元素位置。

  ### 6.4 CLR alias 测试

  - 已注册 alias 成功构造 CLR 对象。
  - 未注册 alias 在运行时拒绝。
  - 字段未知、重复、不可转换时拒绝。
  - 模块初始化字面量和普通函数字面量使用相同结果。
  - 验证没有先构造普通 ScriptObject 再转换为 CLR 对象。

  ### 6.5 编辑器测试

  - .tdoc 中 Packed Array 越界诊断。
  - .tdoc 中 Date format 错误诊断。
  - .tdoc 中显式类型和值形状错误诊断。
  - 科学计数法和引号属性名可解析。
  - CLR alias 名称不产生编辑器诊断。
  - 诊断包含正确源位置和数据路径。

  ### 6.6 回归测试

  完成后运行：

  TypedDocument 相关测试
  LanguageServices 测试
  LanguageServer 测试
  全解决方案测试
  net8 / net9 / net10

  验收标准：

  - 现有非 TDoc 行为无回归。
  - TDoc 独立 reader 与脚本 literal 对相同输入产生相同值或相同错误。
  - 不再出现 Int8Array [255] -> -1。
  - 不再出现显式 Date/Object 插值绕过外层类型。
  - 新增六种 Packed Array 全部完成 round-trip 和边界验证。