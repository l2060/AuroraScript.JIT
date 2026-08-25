当前 StringBuffer、Int8Array数组 等Native对象都是存到 Datum的reference中，ValueKind的类型定义已经将近满状态。
需要 将 typeof 关键字支持真实的Int8Array类型而不是Object


native func fo(Number a, Number b) Number{
}

export native func fo(Number a, Number b) Number{
}

使用native关键字可将func编译为本地原生方法  static double fo(ScriptContext A_0, double a, double b);
如果方法被标记了export则需要生成方法的Datum版本兼容壳   static ScriptDatum fo(ScriptContext A_0, Span<ScriptDatum> A_1); 由壳内调用原生方法，供外部模块调用。
未标注native的方法不做本地原生参数优化，保持static ScriptDatum fo(ScriptContext A_0, Span<ScriptDatum> A_1);函数声明，方法内计算指令仍根据变量类型推断选择性编译为Native
标注为native的方法不允许直接赋值修改，不允许HotPatch的增量修复仅允许替换修复。

1. 方法内脚本计算指令编译为Native优化是必做的，
2. 方法签名的Native本地原生化必须依赖native关键字指定。
3. 对外暴漏的方法Native时必须生成ScriptDatum版本外壳。
4. 模块间调用必须经过GetProperty获取壳函数。
5. 当前 direct对typed方法的封装是必要的么
6. 优化native不应该丢弃调用堆栈信息
7. 任何对方法引用造成可能逃逸的情况都应该回归到GetProperty获取壳函数。