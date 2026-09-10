# CompileBlock 导入已有模块

`CompileBlock` 保持函数体编译和直接执行模型。它不创建、注册或初始化运行时模块，不建立依赖图，不读取依赖源码，也不保留依赖的 AST、编译会话或编译缓存。

```csharp
using var block = engine.CompileBlock(
    "import lib from './lib'; return lib.calculate(value);",
    new CompileBlockOptions
    {
        Domain = domain,
        Parameters = ["value"],
        SourceName = "blocks/calculate.as"
    });

var result = block.Invoke(ScriptDatum.FromNumber(42));
```

## 导入和生命周期

- `Domain` 必须由同一引擎创建，依赖必须已经加载。缺失依赖在编译时报告错误。
- 使用引擎现有的 `ResolveAsync` 解析路径，只调用解析接口，不调用 `GetSourceAsync`。相对路径以 `BaseDirectory` 和 `SourceName` 组合出的代码块位置为准；`BaseDirectory` 默认使用解析器根目录。
- 系统包沿用现有配置及命名空间。例如 `import fs from 'fs'` 导入已启用的系统包，`import user from './fs'` 导入用户模块。
- import 只允许出现在代码块开头。导入别名是隐式只读参数，嵌套函数通过已有的 upvalue 捕获它。别名和显式参数总数最多 255。
- 有导入的代码块绑定到指定域，`Invoke()` 使用该域。传入其他域、依赖被移除或替换、已绑定的静态成员改变时，调用报错并要求重新编译。
- 没有 import 的代码块保持原有调用方式。`Dispose()` 注销代码块自身的动态委托并释放绑定引用，不注销依赖模块的委托。

## 调用选择

| 成员 | 处理方式 |
| --- | --- |
| 导出的 native 函数 | 从运行时闭包取得真实 CLR 入口、默认值及签名完整性信息；类型匹配时复用现有参数适配和返回类型推导 |
| 已启用系统包的方法 | 复用现有系统包导出描述和 native 核心入口 |
| 导出的 const 基础值 | 从实际导出值生成常量，不分析依赖源码 |
| 导出的 const 闭包 | 直接调用实际闭包入口，保留所属模块和捕获环境 |
| 普通可变成员 | 每次调用读取当前属性，保持动态调用 |
| 未导出的成员 | 外部读取及成员调用返回脚本 `null`，函数体不执行，调用参数仍正常求值 |

不能从 CLR 签名完整恢复的参数约束，以及不能证明兼容的参数或展开参数，保留 datum 检查入口。例如结构类型、部分数组类型、当前 host 调用适配器未支持的 CLR 类型，不会跳过检查强行调用 native 入口。普通 `null()` 仍按原有规则报错。

native 直调使用一个小的 CLR 适配方法进入被调用闭包的上下文，并在异常时恢复上下文。参数适配和返回类型推导复用现有逻辑；同一个已加载 native 闭包在一次编译中只创建一次描述和适配方法。这些编译期描述不进入运行时缓存。

模块已经存在导出标志和值，因此不增加另一套全局导出表。新增的长期信息仅附着在 native 闭包上：实际 `MethodInfo`、折叠后的可选参数默认值和签名是否完整的标记。代码块仅保留其依赖引用及实际使用的静态成员校验值。

动态编译复用既有委托注册项传递 native 入口，不新增全局注册表，也不重新注册代码块引用的依赖委托。

## 边界

静态绑定校验发生在代码块入口，不提供并发热更新事务；执行期间通过宿主强制改写 const/native 成员不属于支持的调用方式。代码块返回的闭包沿用现有闭包生命周期规则；保持代码块存活，直到这些闭包不再使用。

回归覆盖位于 `CompileBlockImportTests`、`ModuleVisibilityTests` 和现有的 `CompileBlockTests`、`NativeFunctionTests`、`BuiltInModuleTests`。包括依赖源码零读取、初始化不重复、域绑定、可变函数替换、native 返回类型传播、闭包捕获、未导出成员的点调用/下标调用/展开参数调用，以及三种模块编译模式。
