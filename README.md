CILEmitter.cs是一个非常干净的CIL发射器，接下来你的工作是帮助我完成这个发射器
它的目的是将脚本的语法树对象编译为CIL并输出DLL，目前输出DLL的方法我已经完成

## 脚本的对象
ScriptGlobal 是Engine的全局对象，当然每个Domain中也存在一个被隔离的，但是它们都继承自Engine的ScriptGlobal 
ScriptDomain 是脚本的隔离域，每个域是一个脚本的副本 它下面包含了很多个ScriptModule对象
ScriptModule 脚本的模块对象，每个模块对应一个脚本文件，模块中的root变量和func将作为模块的属性存在
ScriptObject  脚本的基础Map对象
ScriptDatum 脚本的内存数据对象，它可以包住所有ScriptObject的子类型。并且使用它存储 数值 bool null，有效提升计算性能

## 初始化Domain
 
CIL负责为脚本创建Domain初始化流程，创建动态类型AuroraScriptInitializer的方法InitializeDomain
InitializeDomain方法参数为ScriptDomain，负责创建所有模块实例并注册到Domain的Global中
随后为调用各模块的初始化方法进行初始化操作。
先注册全部模块后初始化模块的意义在于模块之间的引用。


## 初始化模块
每个as脚本文件对应一个模块，每个模块含有一个Initialize方法 
Initialize方法用于运行模块内的root代码 如：初始化变量、方法和运行根语句，并将模块的root变量和func注册到模块中
public static void Initialize(ScriptDomain A_0, ScriptModule A_1)

你还需要将模块的import语句注册为模块的变量，当然它指向了导入的模块对象实例
需要注意的是，import语句的属性Include为true时它没有Name属性，这时你需要将它的模块所有语句嵌入到当前位置。



## 变量/属性/全局属性的区别
在访问语法树时你应该区别脚本中每个Scope的变量

方法内变量：编译为原生本地栈变量
模块的属性：编译为Get/Set ModuleProperty
全局属性：编译为Get/Set GlobalProperty

变量赋值原则：先检查本地变量的Scope，再检查模块的Scope，如果前两项不满足则写入Global
变量获取原则：先检查本地变量的Scope，再检查模块的Scope，如果前两项不满足则从Global中获取


## 调用上下文
每次闭包方法的调用必须为它分配新的CILContext，
CILContext的解构设计如下
Domain:当前的Domain对象
Global:当前的Global对象
Module:当前方法所属的Module对象
UserState:当前的UserState对象
Arguments:当前方法的参数列表
Upvalues:当前方法的捕获的闭包变量



## 语法树
LiteralExpression  字面量，可能时字符串、数字、布尔值、null
要判断字面量的类型需要检查Token
switch(LiteralExpression.Token){
    case StringToken:
    case NumberToken:
    case BooleanToken:
    case NullToken:
}

NameExpression     脚本中的变量或属性 如 __testCases/console/log/count/title等
TokenExpression    词法单元




## 注意事项

你可以少量的使用Helper方法，但不能滥用Helper方法
你需要理解ScriptObject与ScriptDatum的工作方式
生成的CIL优先使用ScriptDatum而不是ScriptObject，这样可以提升计算性能 Property相关访问才使用ScriptObject
我需要你生成的CIL应该与原始脚本语法基本一致。可读性优，编译的CIL应该是性能优先的。
你应该尽可能减小CILEmitter.cs的尺寸。 将静态方法移动到Helper类中。
严格遵守我的要求，不要擅自修改我的要求，否则我将无法继续工作。
现阶段我注重的是CIL生成顺利，而不是CIL运行顺利， 所以我需要你保证CIL生成顺利并成功生成DLL， 你不需要考虑执行IL。












