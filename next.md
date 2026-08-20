
# AuroraScript Typed Document（TDoc）设计草案

> 状态：TDoc 4.0 已实现独立文本的序列化、反序列化、`readonly`、类型输出控制、脚本 `TDoc` API、宿主文本/文件/流 API，以及脚本 `tdoc` 字面量和 `.tdoc` 编辑器渲染。独立文档仍禁止 `$(...)`；语法树/注释保留属于后续设计。

## 1. 目标

1. 序列化/反序列化后不丢失对象、容器及已注册类型的身份。
2. 支持独立配置文档和脚本内原生 `tdoc` 字面量。
3. 支持默认类型自动推断与显式类型声明。
4. 支持 AuroraScript 内置类型，以及宿主通过 `AuroraEngine.RegisterType` 显式注册的 CLR/CIL 对象类型。
5. 支持行注释、块注释、`readonly` 对象属性和脚本内联表达式。


## 2. 原始语法示例
```
Array [
	StringBuffer "hello",
	String "world",
	Number 123.45,
	Boolean false,
    Date "2024-11-23 15:26:33",
    Date 14425655658,
    Path "a/b/c/d.as"
]

```


### 示例 2：对象、嵌套对象与推断

```aurorascript

Object {
	String name "Hanks",
	age 18, // 省略类型自动推断为 Number
	Object meta {
		String address "Ho Yang",
		phone "1526-255",  // 省略类型 自动推断为String
		tags [ String "A",  "B", 100 ] // 自动推断为 Array
	}
}

```

### 示例 3：压缩数组

```aurorascript
Int8Array [1, 2, 3, 4, 5, 6, 0x7F]
```

> 上述示例只展示根值。独立 `.tdoc` 文档直接以根值开始，不需要 `tdoc` 前缀；只有作为 AuroraScript 表达式时，根值之前才必须加 `tdoc`。

## 3. 总体边界

TDoc 是数据格式，不是第二套通用脚本语言。

- 普通 AuroraScript 继续使用现有对象字面量、数组字面量、函数调用和下标访问。
- TDoc 只在独立 `.tdoc` 文件或 `tdoc` 后启用。
- JSON 保持面向外部互操作的 JSON 语义；TDoc 才负责保留 `Date`、`Path`、压缩数组、`StringBuffer`、已注册 CIL 对象类型等身份。
- TDoc 的纯数据部分不执行代码。只有脚本内显式 `$(...)` 才能求值。
- TDoc 面向树形配置，不保留循环引用、共享身份或 prototype 链；写入时按可跳过值规则截断这些运行时结构。

采用“一套语义、两个入口”的最小设计：

| 入口 | 用途 | 内联表达式 |
| --- | --- | --- |
| `.tdoc` 独立文档 | 配置、持久化数据、宿主输入输出 | 禁止 |
| `tdoc` 脚本字面量 | 在 `.as` 中创建带类型数据 | 允许 `$(...)` |

## 4. 文档入口与脚本标记

### 4.1 独立 `.tdoc` 文件

```aurorascript
Object {
    String name "Aurora",
    version 4,
}
```

- 独立文档由 `.tdoc` 文件入口或反序列化 API 确定语法模式，第一个非 trivia token 就是根值，不要求也不输出 `tdoc` 标记。
- 当前规范版本是 TDoc 1；版本由 API/实现选择，而不是写入数据文档。
- 一个文件只允许一个根值。
- 根值前后不允许普通 AuroraScript 语句、函数、导入、变量声明或 `$(...)`。

### 4.2 脚本内 `tdoc` 字面量

```aurorascript
const profile = tdoc Object {
    String name "Hanks",
    age 18,
};
```

`tdoc` 是明确的语法边界。因此现有语义不会改变：

```aurorascript
var item = Array[index]; // 仍是下标访问，不是 TDoc 数组构造
const values = tdoc Array [1, 2, 3]; // 只有此处按 TDoc 规则解析
```

## 5. 规范语法

`identifier`、字符串和数值字面量沿用 AuroraScript 词法规则；类型别名区分大小写。

```ebnf
standalone-document = typed-value EOF ;
embedded-data       = "tdoc" typed-value ;

typed-value         = [ type-ref ] raw-value ;
raw-value           = "null"
                    | boolean
                    | number
                    | string
                    | array
                    | object
                    | interpolation ;

array               = "[" [ array-element { "," array-element } [ "," ] ] "]" ;
array-element       = typed-value ;

object              = "{" [ object-member { "," object-member } [ "," ] ] "}" ;
object-member       = [ "readonly" ] [ type-ref ] property-name raw-value ;
property-name       = identifier | string ;
type-ref            = identifier ;

interpolation       = "$" "(" aurora-expression ")" ;
```

### 5.1 对象成员的定位规则

对象成员沿用 `next.md` 的“类型 + 属性名 + 值”风格，而非强制 `key: value`：

| 文本 | 结果 |
| --- | --- |
| `name "Hanks"` | 属性 `name`，`String` 自动推断 |
| `age 18` | 属性 `age`，`Number` 自动推断 |
| `String name "Hanks"` | 显式 `String` 属性 `name` |
| `Object meta { ... }` | 显式 `Object` 属性 `meta` |
| `readonly String id "u-1"` | 显式只读 `String` 属性 `id` |
| `"display name" "Hanks"` | 属性名可使用字符串 |

判定只依赖位置：对象成员中有两个连续的名称位置时，第一个是类型名、第二个是属性名；只有一个名称位置时，它就是属性名。解析器无需预先知道该类型是否已经注册。

字符串值必须有单引号或双引号；不引入裸字符串。`Object { id UX01 }` 中的 `UX01` 处于标识符/类型判定位置，`Object { id UX01-03 }` 中的 `-` 也不属于标识符，二者都非法。正确写法是 `Object { id "UX01" }` 或 `Object { id "UX01-03" }`。这避免裸词同时充当类型名、属性名和字符串值的歧义。

若属性名本身等于内置或宿主注册类型名，为保持清晰，应引用该属性名：

```aurorascript
Object {
    "String" "this is a property named String",
    "User" "this is a property named User",
}
```

数组没有属性名，所以 `String "A"`、`Date "2024-11-23 15:26:33"`、`Date 14425655658`、`Object { ... }`、`Int8Array [1, 2]` 的第一个名称直接是元素类型。

### 5.2 分隔、注释与诊断

- 同级数组元素和对象属性用逗号分隔，允许尾逗号。
- 支持 `//` 行注释及非嵌套 `/* ... */` 块注释，注释可位于任何允许空白的位置。
- 同一 `Object` 内的重复属性名是错误，不采用“后者覆盖前者”。
- 缺少逗号、未闭合字符串/注释、未知类型、值形状错误和类型范围错误都必须给出文件名、行、列和数据路径，例如 `$.meta.tags[2]`。

## 6. 类型模型

### 6.1 自动推断

| 文本 | 推断类型 |
| --- | --- |
| `null` | `Null` |
| `true` / `false` | `Boolean` |
| `123`、`123.45`、`0xFF` | `Number` |
| `'text'` / `"text"` | `String` |
| `[ ... ]` | `Array` |
| `{ ... }` | `Object` |

裸数值始终是 AuroraScript `Number`，即当前运行时的 `double` 语义。TDoc 1 不会把裸 `18` 自动变为 `Int32`、`Int64`、`Decimal` 或 `Byte`，也不借助 CIL 类型注册保留这些 CLR 标量身份；CIL 扩展仅适用于宿主已注册的对象类型。

### 6.2 内置类型

| 类型 | 示例 | 反序列化目标 |
| --- | --- | --- |
| `Object` | `Object { name "Aurora" }` | `ScriptObject` |
| `Array` | `Array [1, "x"]` | `ScriptArray` |
| `String` | `String "Aurora"` | `String` |
| `Number` | `Number 123.45` | `Number` |
| `Boolean` | `Boolean false` | `Boolean` |
| `StringBuffer` | `StringBuffer "hello"` | `StringBuffer` |
| `Date` | `Date "2024-11-23 15:26:33"` / `Date 14425655658` | 字符串按 `EngineOptions.Runtime.DateTimeFormat` 解析；整数按 .NET ticks 构造 `ScriptDate` |
| `Regex` | `Regex { pattern "ab+", flags "gi" }` | `ScriptRegex` |
| `Path` | `Path "a/b/c/d.as"` | `ScriptPathValue` |
| `HashMap` | `HashMap [["name", "Aurora"], [1, true]]` | `ScriptHashMap` |
| `Int32Array` | `Int32Array [1, 2, 3]` | `ScriptInt32Array` |
| `Int8Array` | `Int8Array [-128, 0, 127]` | `ScriptInt8Array` |
| `Float64Array` | `Float64Array [1.5, 2.5]` | `ScriptFloat64Array` |
| `BooleanArray` | `BooleanArray [true, false]` | `ScriptBooleanArray` |



### 6.3 严格范围

TDoc 是持久化数据格式，应比普通运行时赋值更严格：

- `Int8Array` 每个元素只能是 `-128..127` 的整数。
- `0xFF` 的数值是 `255`，因此不能写入 `Int8Array`；读取时必须报越界错误，不能静默变为 `-1`。
- 如果业务需要 `0..255`，应单独引入 `UInt8Array`/`ByteArray`，不要复用 `Int8Array`。
- `Int32Array`、`Float64Array` 和 `BooleanArray` 也必须逐元素验证，不允许依赖隐式截断。
- `Date` 的数值形式必须是 `0..DateTimeOffset.MaxValue.Ticks` 范围内的整数，不允许小数、负数、溢出或隐式截断。

## 7. `readonly` 属性

`readonly` 是对象属性描述符，不是类型：

```aurorascript
Object {
    readonly String id "user-001",
    readonly name "Hanks",
    readonly Object options {
        retries 3,
    },
}
```

规范语义：

1. 构造对象时先写入初始值，然后将该属性标记为 `writeable: false`。
2. 后续给 `object.id` 或 `object.options` 或 `object.name` 赋值必须失败并产生运行时错误。
3. `readonly` 是浅只读：`options.retries = 4` 仍可执行。深度不可变应由未来的 `freeze` 功能单独定义。
4. 默认属性可枚举；`readonly` 不改变枚举性。
5. TDoc writer 必须读取属性描述符并重新输出 `readonly`，否则 round-trip 会丢失只读语义。
6. 该规则直接适用于普通 `Object`。宿主已注册的 CLR/CIL 对象是否能映射为只读成员，由其注册描述符或与注册项绑定的 codec 决定。

`const config = ...` 只限制变量绑定；`readonly` 才限制 `config.id` 这样的对象属性。两者可同时使用。

## 8. 脚本内联变量与表达式

仅 `tdoc` 字面量允许 `$(...)`。括号中是普通 AuroraScript 表达式：

```aurorascript
export func createProfile(user, baseAge) {
    const profile = tdoc Object {
        readonly String id $(user.id),
        String name $(user.name),
        age $(baseAge + 1),
        String message $("Hello, " + user.name),
        tags ["system", $(user.role)],
    };

    return profile;
}
```

规则：

1. `$(...)` 是唯一的动态逃逸口；未包裹的内容一律按纯数据处理，不会被当作变量求值。
2. 内联结果先按普通脚本规则求值，再由外层显式类型验证或转换。例如 `String name $(user.name)` 的结果必须可写入 `String`。
3. 根值、对象属性和数组元素都可以使用 `$(...)`。
4. `.tdoc` 独立文档禁止 `$(...)`，因为它没有脚本作用域。若需要宿主替换，应设计显式的绑定字典 API，不能让配置文件执行任意代码。
5. 不支持隐式裸变量，例如 `name userName`；这会与“类型 + 属性名 + 值”产生歧义，也不利于配置可复现。

## 9. CLR/CIL 宿主注册对象类型

TDoc 对 CLR/CIL 的支持只覆盖宿主通过当前 `AuroraEngine` 的 `RegisterType<T>` 或 `RegisterType(Type, ...)` 显式注册的对象类型。该引擎的 CLR 类型注册表是唯一的准入列表和别名来源；类型仅仅存在于已加载程序集、具有公共构造函数或公共成员，并不表示 TDoc 可以使用它。

例如宿主先注册 `User`：

```csharp
var engine = new AuroraEngine(options);
engine.RegisterType<User>("User");
```

之后才可以在文档中使用该注册别名：

```aurorascript
Object {
    User profile {
        String name "Hanks",
        age 18,
    },
}
```

`User` 的解析链必须是：

```text
TDoc 文本别名 User
    → 当前 AuroraEngine.ClrRegistry 中的注册项
    → 注册项的类型描述符或与其绑定的 codec
    → 已注册的 CLR User 对象实例
```

约束如下：

- 序列化必须用对象实例的实际 CLR 类型反查当前引擎的注册项；未注册对象不能降级为普通 `Object` 或自动反射输出，而是按可跳过值规则省略成员或写为 `null`。
- 反序列化只能用文档中的别名查询当前引擎的注册表；未知或未注册别名必须报错，不能从类型名或程序集名猜测 CLR 类型。
- 仅支持对象实例；CLR 标量、枚举、委托、静态类型、方法绑定及开放泛型不属于 TDoc 的 CIL 对象类型范围。
- 注册项必须定义可持久化的成员形状以及构造/填充规则。若使用自定义 codec，codec 必须绑定到该注册项，不能绕过 `RegisterType` 单独开放类型。
- 注册别名是文档中的类型身份；重命名、未知字段、默认值和版本迁移策略由宿主的注册契约负责。

不得通过 `AssemblyQualifiedName`、`Type.GetType`、任意 `Activator.CreateInstance` 或未受注册项约束的公共成员反射还原类型。配置内容可能来自不可信来源，这些路径会绕过宿主准入边界，并产生构造副作用和版本脆弱性。

因此，TDoc 不承诺支持所有 CLR/CIL 类型；只有具有稳定数据语义且已由当前宿主注册的对象类型才有资格参与序列化和反序列化。

以下对象不是可持久化值；写入时不会自动反射或执行它们，而是按 10.2 的可跳过值规则处理：

- 任意未注册 CLR/CIL 对象；
- 闭包、脚本函数、委托、静态类型和 CLR 方法绑定；
- `Proxy` 及其处理器；
- `ScriptGlobal`、模块和类型对象；
- 循环引用、共享身份或外部资源句柄对象图。

## 10. 序列化与反序列化

### 10.1 独立于 JSON 的接口

当前运行时 API 是独立能力：

```csharp
TypedDocumentSerializer.Serialize(AuroraEngine engine, ScriptDatum value, TypedDocumentOptions options); // string
TypedDocumentSerializer.Deserialize(AuroraEngine engine, string text, TypedDocumentOptions options);      // ScriptDatum

var tdoc = new AuroraTypedDocument(engine, options);
tdoc.Serialize(value);                             // string
tdoc.Deserialize(text);                            // ScriptDatum
tdoc.WriteFile(path, value);                       // void
tdoc.ReadFile(path);                               // ScriptDatum
tdoc.WriteStream(stream, value);                   // void
tdoc.ReadStream(stream);                           // ScriptDatum
```

脚本侧使用 `TDoc.parse(text)` 和 `TDoc.stringify(value, indented = true, emitTypes = false)`。`TDoc` 只处理文本和值；文件与流 I/O 只由 CIL/宿主 API 提供。

`TypedDocumentOptions.EmitTypeNames` 默认是 `false`。默认只输出无法由原始字面量唯一推断的类型名：`StringBuffer`、`Date`、`Regex`、`Path`、`HashMap`、全部 Packed Array 和已注册 CLR/CIL 类型；`Object`、`Array`、`String`、`Number`、`Boolean` 省略类型名。设为 `true` 可强制输出所有可用类型名，用于严格评审、稳定快照或显式数据契约。

`AuroraEngine` 是必需上下文：它同时提供 `EngineOptions.Runtime.DateTimeFormat` 和只允许宿主注册对象类型的 `ClrRegistry`。反序列化根值必须使用 `ScriptDatum`，而不是只返回 `ScriptObject`。这使根级 `null`、`Boolean`、`Number` 与 `String` 都能保持原始值种类。

### 10.2 写入规则

1. 先由内置类型表或当前引擎的 CLR 注册表判定运行时类型；未注册 CLR/CIL 对象不会自动反射，按可跳过值规则处理。
2. 默认省略基础可推断值的类型名；特殊类型、压缩数组和已注册 CIL 对象类型必须输出显式类型名。`EmitTypeNames = true` 可强制基础值也输出显式类型名。
3. `Object` 输出脚本可见的可枚举属性顺序与 `readonly` 描述符：先自身属性，再取 prototype 链中未被遮蔽的属性；文档读取后只恢复扁平 own properties，不恢复 prototype 身份。
4. `HashMap` 必须输出键值对列表，不能降级为普通对象，否则非字符串键会丢失。
5. `Date` 必须输出字符串，并严格使用当前引擎的 `EngineOptions.Runtime.DateTimeFormat`；不得写死 ISO 8601、改用 ticks 或在格式化失败时静默回退。
6. `Regex` 必须输出 pattern 和 flags，不能只输出 `ToString()`。
7. 独立文档直接输出根值，不输出 `tdoc` 或版本标记。
8. 默认输出 UTF-8、规范缩进和尾逗号；可提供紧凑输出选项。
9. 对函数、代理、访问器、未注册 CLR/CIL 对象、非有限 `Number`、循环/共享引用等不可表示的运行时值，`stringify` 不报类型错误：对象成员省略；数组元素及 `HashMap` 条目中的不可表示键/值写为 `null`；根值写为 `null`。这是一份数据快照，不是完整对象图克隆。

### 10.3 读取规则

```text
文本
  → 基于索引/Span 的流式扫描与单 token 前瞻
  → 内置类型表/当前 AuroraEngine.ClrRegistry 绑定
  → ScriptDatum / ScriptObject / 宿主已注册的 CLR 对象
```

读取顺序：

1. 独立文档从第一个非 trivia token 直接读取根值，不要求 `tdoc` 标记。
2. 词法阶段识别注释、字符串、数值和源位置。
3. `Deserialize` 热路径使用单 token 前瞻并直接绑定/构造目标值，不建立 token 列表、中间 AST 或 `DataNode`。
4. 绑定阶段先解析内置别名，再通过当前引擎的 CLR 注册表解析 CIL 对象别名；反射契约和访问器按注册类型缓存。
5. `Date` 字符串必须严格按照当前引擎的 `EngineOptions.Runtime.DateTimeFormat` 解析；格式不匹配时在对应数据路径报错，不得回退尝试其他日期格式。`Date` 数值是独立、显式的 .NET ticks 形式，而不是字符串解析失败后的回退路径。
6. 构造阶段创建 `ScriptDatum`、脚本对象或宿主已注册的 CLR 对象。
7. 通过范围、类型、只读、字段和资源限制验证后才返回结果。

未来面向编辑器和安全写回的 `TypedDocument.Parse` 路径才建立保留 trivia 与源范围的语法节点；它不进入运行时 `Deserialize` 热路径。正常成功路径不得为 token 图、诊断路径或中间数据树分配对象，诊断路径只在失败时按需构造。

TDoc 1 不保留循环引用和共享身份。写入时首次可表示的出现会写入，之后的出现按可跳过值规则处理；读取文本时仍对未知/不支持类型、范围和结构错误报告完整数据路径，例如 `$.meta.tags[2]`。

`Date` writer 始终输出字符串并使用当前引擎选项，不输出 ticks。字符串形式的读取必须使用同一份 `DateTimeFormat`；数值形式则明确表示从 `0001-01-01T00:00:00+00:00` 起的 .NET ticks。若数值 Date 在规范化为字符串后仍需无损 round-trip，必须把 `DateTimeFormat` 配置为包含完整小数秒和时区信息的格式（例如 `O`）；TDoc 不会自行补充格式中缺失的信息。

### 10.4 注释保留

- `Deserialize` 必须接受不带 `tdoc` 标记的根值及其中的注释，并返回纯数据值。
- 未来的 `TypedDocument.Parse` 应保留注释、空白和源范围，供编辑器、格式化及“读取后保存”使用。
- 从 `ScriptDatum` 新建的规范化文档没有原始注释；只有从未来的语法文档对象修改后写回，才可能保留已有注释。

## 11. 与现有 AuroraScript 的兼容性

1. 在 `.as` 源码中，`tdoc` 之后才进入 TDoc 子语法，不改变普通对象字面量、数组字面量、函数调用或下标访问；独立文档入口由 API 决定，因此不需要该标记。
2. 现有 `Array` 构造器接受容量，现有压缩数组构造器接受长度；TDoc 的 `Array [ ... ]` 和 `Int8Array [ ... ]` 是新数据字面量能力，不能伪装为现有构造器调用。
3. 现有 JSON 序列化会将特殊类型降级为字符串、普通对象或普通数组；TDoc 是新增保真格式，不能改变 JSON 输出。
4. 现有 AuroraScript 词法器已识别行/块注释；TDoc 复用其词法约定，并使用可保留 trivia 的扫描路径实现文档级注释保留。

## 12. 实施顺序与验收

### 阶段 1：核心数据文档

- 不带 `tdoc` 标记的 `.tdoc` 根值、数组、对象、逗号、注释和结构化诊断；
- 基础推断、显式类型、`readonly` 和严格数值范围；
- `Object`、`Array`、四种压缩数组、`StringBuffer`、`Date`、`Regex`、`Path`、`HashMap`；
- 自定义 `DateTimeFormat` 下的 `Date` round-trip，以及格式不匹配时的结构化诊断；
- 数值 ticks `Date` 的读取、严格范围验证，以及按 `DateTimeFormat` 规范化为字符串；
- 序列化/反序列化 round-trip、循环/共享引用的可跳过值降级，以及可枚举 prototype 属性扁平化。

### 阶段 2：原生脚本

- `tdoc` 字面量；
- `$(...)` 内联表达式；
- 与普通 CIL 编译、作用域和运行时错误一致的行为；
- 保证 TDoc 的纯数据部分不执行任意代码。

### 阶段 3：宿主注册类型与工具

- 与 `AuroraEngine.RegisterType`/`ClrRegistry` 集成，并支持绑定到注册项的 codec、类型版本和迁移；
- 已注册 CIL 对象的 round-trip、未注册对象写入降级，以及未知别名读取拒绝用例；
- Language Server/VSIX 的 `.tdoc` 高亮、格式化和诊断（已接入）；
- `TypedDocument` 的注释保留与安全保存；
- 如确有需求，再评估 `UInt8Array`、带类型标量、对象图引用和深冻结。

## 13. 最小验收文档

```aurorascript
Object {
    readonly String id "u-001",
    String name "Hanks",
    age 18,
    Object meta {
        String address "Ho Yang",
        phone "1526-255",
        tags [String "A", "B", 100],
    },
    Int8Array signedBytes [-128, 0, 127],
    StringBuffer greeting "hello",
    Date createdAt "2024-11-23 15:26:33",
    Path scriptPath "a/b/c/d.as",
}
```

该文档反序列化后必须满足：

- 文档无需 `tdoc` 标记即可读取；
- 根值和 `meta` 为 `ScriptObject`；
- `tags` 为 `ScriptArray`；
- `signedBytes` 为 `ScriptInt8Array`，保留 `sbyte[]` 身份；
- `greeting` 为 `StringBuffer`，`createdAt` 为 `ScriptDate`，`scriptPath` 为 `ScriptPathValue`；
- `id` 不可重新赋值，`meta.phone` 仍可修改；
- 在默认 `DateTimeFormat`（`yyyy-MM-dd HH:mm:ss`）下，`createdAt` 按该格式读取并再次写出；
- 再次序列化后仍输出 `readonly String id`、`Int8Array`、`StringBuffer`、`Date` 和 `Path` 类型信息，且不添加 `tdoc` 标记。
- 增加 UInt8Array/Int16Array/UInt16Array/UInt32Array/Int64Array/UInt64Array 实现
