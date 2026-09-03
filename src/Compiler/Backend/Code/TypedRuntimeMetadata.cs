using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using AuroraScript.Runtime.Pool;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Core;
using System;
using System.Reflection;
using System.Text;

namespace AuroraScript.Compiler.Backend.Code
{
    internal static class TypedRuntimeMetadata
    {
        public static readonly MethodInfo DatumFromNumber = Method(typeof(ScriptDatum), nameof(ScriptDatum.FromNumber), typeof(double));
        public static readonly MethodInfo DatumFromInt32 = Method(typeof(ScriptDatum), nameof(ScriptDatum.FromNumber), typeof(int));
        public static readonly MethodInfo DatumFromInt64 = Method(typeof(ScriptDatum), nameof(ScriptDatum.FromNumber), typeof(long));
        public static readonly MethodInfo DatumFromBoolean = Method(typeof(ScriptDatum), nameof(ScriptDatum.FromBoolean), typeof(bool));
        public static readonly MethodInfo DatumFromString = Method(typeof(ScriptDatum), nameof(ScriptDatum.FromString), typeof(string));
        public static readonly MethodInfo DatumFromObject = Method(typeof(ScriptDatum), nameof(ScriptDatum.FromObject), typeof(ScriptObject));
        public static readonly MethodInfo DatumToObject = Method(typeof(ScriptDatum), nameof(ScriptDatum.ToObject), typeof(ScriptDatum));
        public static readonly MethodInfo DatumToString = Method(typeof(ScriptDatum), nameof(ScriptDatum.ToString), typeof(ScriptDatum));
        public static readonly FieldInfo DatumNull = typeof(ScriptDatum).GetField(nameof(ScriptDatum.Null));
        public static readonly MethodInfo CheckNull = TypeCheck(nameof(TypeCheckOps.CheckNull));
        public static readonly MethodInfo CheckBoolean = TypeCheck(nameof(TypeCheckOps.CheckBoolean));
        public static readonly MethodInfo CheckNumber = TypeCheck(nameof(TypeCheckOps.CheckNumber));
        public static readonly MethodInfo CheckString = TypeCheck(nameof(TypeCheckOps.CheckString));
        public static readonly MethodInfo CheckObject = TypeCheck(nameof(TypeCheckOps.CheckObject));
        public static readonly MethodInfo CheckArray = TypeCheck(nameof(TypeCheckOps.CheckArray));
        public static readonly MethodInfo CheckInt32Array = TypeCheck(nameof(TypeCheckOps.CheckInt32Array));
        public static readonly MethodInfo CheckInt8Array = TypeCheck(nameof(TypeCheckOps.CheckInt8Array));
        public static readonly MethodInfo CheckFloat64Array = TypeCheck(nameof(TypeCheckOps.CheckFloat64Array));
        public static readonly MethodInfo CheckBooleanArray = TypeCheck(nameof(TypeCheckOps.CheckBooleanArray));
        public static readonly MethodInfo CheckUInt8Array = TypeCheck(nameof(TypeCheckOps.CheckUInt8Array));
        public static readonly MethodInfo CheckInt16Array = TypeCheck(nameof(TypeCheckOps.CheckInt16Array));
        public static readonly MethodInfo CheckUInt16Array = TypeCheck(nameof(TypeCheckOps.CheckUInt16Array));
        public static readonly MethodInfo CheckUInt32Array = TypeCheck(nameof(TypeCheckOps.CheckUInt32Array));
        public static readonly MethodInfo CheckInt64Array = TypeCheck(nameof(TypeCheckOps.CheckInt64Array));
        public static readonly MethodInfo CheckUInt64Array = TypeCheck(nameof(TypeCheckOps.CheckUInt64Array));
        public static readonly MethodInfo DatumNumber = typeof(ScriptDatum).GetProperty(nameof(ScriptDatum.Number)).GetMethod;
        public static readonly MethodInfo DatumBoolean = typeof(ScriptDatum).GetProperty(nameof(ScriptDatum.Boolean)).GetMethod;
        public static readonly FieldInfo ContextDomain = typeof(ScriptContext).GetField(nameof(ScriptContext.Domain));
        public static readonly FieldInfo ContextModule = typeof(ScriptContext).GetField(nameof(ScriptContext.Module));
        public static readonly FieldInfo ContextGlobal = typeof(ScriptContext).GetField(nameof(ScriptContext.Global));
        public static readonly FieldInfo ContextUserState = typeof(ScriptContext).GetField(nameof(ScriptContext.UserState));
        public static readonly FieldInfo ContextUpvalues = typeof(ScriptContext).GetField(nameof(ScriptContext.Upvalues), BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly FieldInfo ContextLocation = typeof(ScriptContext).GetField(nameof(ScriptContext.Location));
        public static readonly FieldInfo UpvalueValue = typeof(Upvalue).GetField(nameof(Upvalue.Value));
        public static readonly ConstructorInfo UpvalueConstructor = Constructor(typeof(Upvalue));
        public static readonly MethodInfo EmptyUpvalues = typeof(Array).GetMethod(nameof(Array.Empty)).MakeGenericMethod(typeof(Upvalue));
        public static readonly MethodInfo[] ResolveClosureDelegate = ResolveClosureDelegates();
        public static readonly ConstructorInfo[] ClosureConstructors = GetClosureConstructors();

        public static readonly MethodInfo ToBooleanDatum = Method(typeof(ValueOps), nameof(ValueOps.ToBoolean), typeof(ScriptDatum));
        public static readonly MethodInfo ToBooleanNumber = Method(typeof(ValueOps), nameof(ValueOps.ToBoolean), typeof(double));
        public static readonly MethodInfo ToBooleanObject = Method(typeof(ValueOps), nameof(ValueOps.ToBoolean), typeof(ScriptObject));
        public static readonly MethodInfo ToArithmeticNumber = Method(typeof(ValueOps), nameof(ValueOps.ToArithmeticNumber), typeof(ScriptDatum));
        public static readonly MethodInfo TryToNumber = Method(typeof(ValueOps), nameof(ValueOps.TryToNumber), typeof(ScriptDatum), typeof(double).MakeByRefType());
        public static readonly MethodInfo TryToInteger = Method(typeof(ScriptDatum), nameof(ScriptDatum.TryToInteger), typeof(ScriptDatum).MakeByRefType(), typeof(long).MakeByRefType());
        public static readonly MethodInfo Add = Method(typeof(ValueOps), nameof(ValueOps.Add), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo AddBoolean = Method(typeof(ValueOps), nameof(ValueOps.AddBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo AddToNumberLeft = Method(typeof(ValueOps), nameof(ValueOps.AddToNumberLeft), typeof(double), typeof(ScriptDatum));
        public static readonly MethodInfo AddToNumberRight = Method(typeof(ValueOps), nameof(ValueOps.AddToNumberRight), typeof(ScriptDatum), typeof(double));
        public static readonly MethodInfo AddStringRight = Method(typeof(ValueOps), nameof(ValueOps.AddStringRight), typeof(ScriptDatum), typeof(string));
        public static readonly MethodInfo AddStringLeft = Method(typeof(ValueOps), nameof(ValueOps.AddStringLeft), typeof(string), typeof(ScriptDatum));
        public static readonly MethodInfo AddStringMiddle = Method(typeof(ValueOps), nameof(ValueOps.AddStringMiddle), typeof(ScriptDatum), typeof(string), typeof(ScriptDatum));
        public static readonly MethodInfo Subtract = Method(typeof(ValueOps), nameof(ValueOps.Subtract), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo SubtractBoolean = Method(typeof(ValueOps), nameof(ValueOps.SubtractBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo Multiply = Method(typeof(ValueOps), nameof(ValueOps.Multiply), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo MultiplyBoolean = Method(typeof(ValueOps), nameof(ValueOps.MultiplyBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo Divide = Method(typeof(ValueOps), nameof(ValueOps.Divide), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo DivideBoolean = Method(typeof(ValueOps), nameof(ValueOps.DivideBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo Modulo = Method(typeof(ValueOps), nameof(ValueOps.Modulo), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo ModuloBoolean = Method(typeof(ValueOps), nameof(ValueOps.ModuloBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo Equal = Method(typeof(ValueOps), nameof(ValueOps.Equal), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo EqualBoolean = Method(typeof(ValueOps), nameof(ValueOps.EqualBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo NotEqual = Method(typeof(ValueOps), nameof(ValueOps.NotEqual), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo NotEqualBoolean = Method(typeof(ValueOps), nameof(ValueOps.NotEqualBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo Less = Method(typeof(ValueOps), nameof(ValueOps.Less), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo LessBoolean = Method(typeof(ValueOps), nameof(ValueOps.LessBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo LessEqual = Method(typeof(ValueOps), nameof(ValueOps.LessEqual), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo LessEqualBoolean = Method(typeof(ValueOps), nameof(ValueOps.LessEqualBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo Greater = Method(typeof(ValueOps), nameof(ValueOps.Greater), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo GreaterBoolean = Method(typeof(ValueOps), nameof(ValueOps.GreaterBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo GreaterEqual = Method(typeof(ValueOps), nameof(ValueOps.GreaterEqual), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo GreaterEqualBoolean = Method(typeof(ValueOps), nameof(ValueOps.GreaterEqualBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo BitwiseAnd = Method(typeof(ValueOps), nameof(ValueOps.BitwiseAnd), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo BitwiseAndBoolean = Method(typeof(ValueOps), nameof(ValueOps.BitwiseAndBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo BitwiseOr = Method(typeof(ValueOps), nameof(ValueOps.BitwiseOr), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo BitwiseOrBoolean = Method(typeof(ValueOps), nameof(ValueOps.BitwiseOrBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo BitwiseXor = Method(typeof(ValueOps), nameof(ValueOps.BitwiseXor), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo BitwiseXorBoolean = Method(typeof(ValueOps), nameof(ValueOps.BitwiseXorBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo LeftShift = Method(typeof(ValueOps), nameof(ValueOps.LeftShift), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo LeftShiftBoolean = Method(typeof(ValueOps), nameof(ValueOps.LeftShiftBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo RightShift = Method(typeof(ValueOps), nameof(ValueOps.RightShift), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo RightShiftBoolean = Method(typeof(ValueOps), nameof(ValueOps.RightShiftBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo UnsignedRightShift = Method(typeof(ValueOps), nameof(ValueOps.UnsignedRightShift), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo UnsignedRightShiftBoolean = Method(typeof(ValueOps), nameof(ValueOps.UnsignedRightShiftBoolean), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo Not = Method(typeof(ValueOps), nameof(ValueOps.Not), typeof(ScriptDatum));
        public static readonly MethodInfo Negate = Method(typeof(ValueOps), nameof(ValueOps.Negate), typeof(ScriptDatum));
        public static readonly MethodInfo BitwiseNot = Method(typeof(ValueOps), nameof(ValueOps.BitwiseNot), typeof(ScriptDatum));
        public static readonly MethodInfo ChangeByOne = Method(typeof(ValueOps), nameof(ValueOps.ChangeByOne), typeof(ScriptDatum), typeof(double));
        public static readonly MethodInfo TypeOf = Method(typeof(ValueOps), nameof(ValueOps.TypeOf), typeof(ScriptDatum));

        public static readonly MethodInfo GetProperty = Method(typeof(ObjectOps), nameof(ObjectOps.GetProperty), typeof(ScriptDatum), typeof(ScriptContext), typeof(string));
        public static readonly MethodInfo GetPropertyDirect = Method(typeof(ObjectOps), nameof(ObjectOps.GetProperty), typeof(ScriptDatum), typeof(string));
        public static readonly MethodInfo GetObjectProperty = Method(typeof(ObjectOps), nameof(ObjectOps.GetProperty), typeof(ScriptObject), typeof(ScriptContext), typeof(string));
        public static readonly MethodInfo GetObjectPropertyDirect = Method(typeof(ObjectOps), nameof(ObjectOps.GetProperty), typeof(ScriptObject), typeof(string));
        public static readonly MethodInfo SetProperty = Method(typeof(ObjectOps), nameof(ObjectOps.SetProperty), typeof(ScriptDatum), typeof(ScriptContext), typeof(string), typeof(ScriptDatum));
        public static readonly MethodInfo SetObjectProperty = Method(typeof(ObjectOps), nameof(ObjectOps.SetProperty), typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(ScriptDatum));
        public static readonly MethodInfo GetElement = Method(typeof(ObjectOps), nameof(ObjectOps.GetElement), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo GetElementNumber = Method(typeof(ObjectOps), nameof(ObjectOps.GetElementNumber), typeof(ScriptDatum), typeof(double));
        public static readonly MethodInfo SetElement = Method(typeof(ObjectOps), nameof(ObjectOps.SetElement), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo SetElementNumber = Method(typeof(ObjectOps), nameof(ObjectOps.SetElementNumber), typeof(ScriptDatum), typeof(double), typeof(ScriptDatum));
        public static readonly MethodInfo CompoundAddElement = Method(typeof(ObjectOps), nameof(ObjectOps.CompoundAddElement), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo CompoundAddElementNumber = Method(typeof(ObjectOps), nameof(ObjectOps.CompoundAddElementNumber), typeof(ScriptDatum), typeof(double), typeof(ScriptDatum));
        public static readonly MethodInfo ChangeElement = Method(typeof(ObjectOps), nameof(ObjectOps.ChangeElement), typeof(ScriptDatum), typeof(ScriptDatum), typeof(double), typeof(bool));
        public static readonly MethodInfo ChangeElementNumber = Method(typeof(ObjectOps), nameof(ObjectOps.ChangeElementNumber), typeof(ScriptDatum), typeof(double), typeof(double), typeof(bool));
        public static readonly MethodInfo GetElementIndex = Method(typeof(ObjectOps), nameof(ObjectOps.GetElementIndex), typeof(ScriptDatum), typeof(int));
        public static readonly MethodInfo SetElementIndex = Method(typeof(ObjectOps), nameof(ObjectOps.SetElementIndex), typeof(ScriptDatum), typeof(int), typeof(ScriptDatum));
        public static readonly MethodInfo CompoundAddElementIndex = Method(typeof(ObjectOps), nameof(ObjectOps.CompoundAddElementIndex), typeof(ScriptDatum), typeof(int), typeof(ScriptDatum));
        public static readonly MethodInfo ChangeElementIndex = Method(typeof(ObjectOps), nameof(ObjectOps.ChangeElementIndex), typeof(ScriptDatum), typeof(int), typeof(double), typeof(bool));
        public static readonly MethodInfo ChangeDatumProperty = Method(typeof(ObjectOps), nameof(ObjectOps.ChangeProperty), typeof(ScriptDatum), typeof(ScriptContext), typeof(string), typeof(double), typeof(bool));
        public static readonly MethodInfo ChangeObjectProperty = Method(typeof(ObjectOps), nameof(ObjectOps.ChangeProperty), typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(double), typeof(bool));
        public static readonly MethodInfo CreateObject3 = Method(typeof(ObjectOps), nameof(ObjectOps.CreateObject3), typeof(string), typeof(ScriptDatum), typeof(string), typeof(ScriptDatum), typeof(string), typeof(ScriptDatum));
        public static readonly MethodInfo SpreadIntoArray = Method(typeof(ObjectOps), nameof(ObjectOps.SpreadInto), typeof(ScriptArray), typeof(ScriptDatum));
        public static readonly MethodInfo CopyProperties = Method(typeof(ObjectOps), nameof(ObjectOps.CopyProperties), typeof(ScriptObject), typeof(ScriptDatum));
        public static readonly MethodInfo Includes = Method(typeof(ObjectOps), nameof(ObjectOps.Includes), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo GetDestructureElement = Method(typeof(ObjectOps), nameof(ObjectOps.GetDestructureElement), typeof(ScriptDatum), typeof(int), typeof(int));
        public static readonly MethodInfo SliceDestructureArray = Method(typeof(ObjectOps), nameof(ObjectOps.SliceDestructureArray), typeof(ScriptDatum), typeof(int), typeof(int));

        public static readonly MethodInfo[] Invoke = CallMethods(property: false);
        public static readonly MethodInfo[] InvokeProperty = CallMethods(property: true);
        public static readonly MethodInfo RentArguments = Method(typeof(CallOps), nameof(CallOps.RentArguments), typeof(int));
        public static readonly MethodInfo AppendArgument = Method(typeof(CallOps), nameof(CallOps.AppendArgument), typeof(ScriptDatum[]), typeof(int).MakeByRefType(), typeof(ScriptDatum));
        public static readonly MethodInfo AppendSpread = Method(typeof(CallOps), nameof(CallOps.AppendSpread), typeof(ScriptDatum[]), typeof(int).MakeByRefType(), typeof(ScriptDatum));
        public static readonly MethodInfo ReturnArguments = Method(typeof(CallOps), nameof(CallOps.ReturnArguments), typeof(ScriptDatum[]), typeof(int));
        public static readonly MethodInfo InvokeMany = Method(typeof(CallOps), nameof(CallOps.InvokeMany), typeof(ScriptDatum), typeof(ScriptContext), typeof(ScriptDatum[]), typeof(int));
        public static readonly MethodInfo InvokePropertyMany = Method(typeof(CallOps), nameof(CallOps.InvokePropertyMany), typeof(ScriptDatum), typeof(ScriptContext), typeof(string), typeof(ScriptDatum[]), typeof(int));
        public static readonly MethodInfo New0 = Method(typeof(CallOps), nameof(CallOps.New0), typeof(ScriptDatum), typeof(ScriptContext));
        public static readonly MethodInfo New1 = Method(typeof(CallOps), nameof(CallOps.New1), typeof(ScriptDatum), typeof(ScriptContext), typeof(ScriptDatum));
        public static readonly MethodInfo New2 = Method(typeof(CallOps), nameof(CallOps.New2), typeof(ScriptDatum), typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo NewMany = Method(typeof(CallOps), nameof(CallOps.NewMany), typeof(ScriptDatum), typeof(ScriptContext), typeof(ScriptDatum[]), typeof(int));

        public static readonly MethodInfo GetModule = Method(typeof(ScopeOps), nameof(ScopeOps.GetModule), typeof(ScriptContext), typeof(string));
        public static readonly MethodInfo SetModule = Method(typeof(ScopeOps), nameof(ScopeOps.SetModule), typeof(ScriptContext), typeof(string), typeof(ScriptDatum));
        public static readonly MethodInfo GetGlobal = Method(typeof(ScopeOps), nameof(ScopeOps.GetGlobal), typeof(ScriptContext), typeof(string));
        public static readonly MethodInfo SetGlobal = Method(typeof(ScopeOps), nameof(ScopeOps.SetGlobal), typeof(ScriptContext), typeof(string), typeof(ScriptDatum));
        public static readonly MethodInfo GetGlobalObject = Method(typeof(ScopeOps), nameof(ScopeOps.GetGlobalObject), typeof(ScriptContext));
        public static readonly MethodInfo GetUserState = Method(typeof(ScopeOps), nameof(ScopeOps.GetUserState), typeof(ScriptContext));
        public static readonly MethodInfo GetUserStateObject = Method(typeof(ScopeOps), nameof(ScopeOps.GetUserStateObject), typeof(ScriptContext));
        public static readonly MethodInfo GetEnumerator = Method(typeof(IterationOps), nameof(IterationOps.GetEnumerator), typeof(ScriptDatum));
        public static readonly MethodInfo MoveNext = Method(typeof(IterationOps), nameof(IterationOps.MoveNext), typeof(ScriptEnumerator), typeof(ScriptDatum).MakeByRefType());
        public static readonly MethodInfo ResolveRegex = Method(typeof(RegexManager), nameof(RegexManager.Resolve), typeof(string), typeof(string));
        public static readonly MethodInfo Throw = Method(typeof(ExceptionOps), nameof(ExceptionOps.Throw), typeof(ScriptDatum));
        public static readonly MethodInfo ToScriptError = Method(typeof(ExceptionOps), nameof(ExceptionOps.ToScriptError), typeof(Exception), typeof(ScriptContext));
        public static readonly Type ReturnSignalType = typeof(ScriptReturnSignal);
        public static readonly Type LoopTransferSignalType = typeof(ScriptLoopTransferSignal);
        public static readonly MethodInfo ReturnFromFinally = Method(typeof(ExceptionOps), nameof(ExceptionOps.ReturnFromFinally), typeof(ScriptDatum));
        public static readonly MethodInfo BreakFromFinally = Method(typeof(ExceptionOps), nameof(ExceptionOps.BreakFromFinally));
        public static readonly MethodInfo ContinueFromFinally = Method(typeof(ExceptionOps), nameof(ExceptionOps.ContinueFromFinally));
        public static readonly MethodInfo PrepareCatch = Method(typeof(ExceptionOps), nameof(ExceptionOps.PrepareCatch), typeof(Exception));
        public static readonly MethodInfo GetReturnValue = Method(typeof(ExceptionOps), nameof(ExceptionOps.GetReturnValue), typeof(ScriptReturnSignal));
        public static readonly MethodInfo IsContinue = Method(typeof(ExceptionOps), nameof(ExceptionOps.IsContinue), typeof(ScriptLoopTransferSignal));
        public static readonly MethodInfo DeleteProperty = Method(typeof(ObjectOps), nameof(ObjectOps.DeleteProperty), typeof(ScriptContext), typeof(ScriptDatum), typeof(string));
        public static readonly MethodInfo DeleteElement = Method(typeof(ObjectOps), nameof(ObjectOps.DeleteElement), typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly MethodInfo BindTypedDocument = Method(typeof(TypedDocumentBinder), nameof(TypedDocumentBinder.BindInterpolation), typeof(ScriptContext), typeof(string), typeof(ScriptDatum));
        public static readonly MethodInfo BindTypedDocumentAtPath = Method(typeof(TypedDocumentBinder), nameof(TypedDocumentBinder.BindInterpolationAtPath), typeof(ScriptContext), typeof(string), typeof(ScriptDatum), typeof(string));
        public static readonly MethodInfo SetTypedDocumentPackedElement = Method(typeof(TypedDocumentBinder), nameof(TypedDocumentBinder.SetPackedElement), typeof(ScriptPackedArray), typeof(int), typeof(ScriptDatum), typeof(string));
        public static readonly MethodInfo CreateTypedDocumentClrObject = Method(typeof(TypedDocumentBinder), nameof(TypedDocumentBinder.CreateClrObject), typeof(ScriptContext), typeof(string), typeof(string));
        public static readonly MethodInfo SetTypedDocumentClrMember = Method(typeof(TypedDocumentBinder), nameof(TypedDocumentBinder.SetClrObjectMember), typeof(ClrInstanceObject), typeof(string), typeof(string), typeof(bool), typeof(ScriptDatum), typeof(string));
        public static readonly MethodInfo CreateTypedDocumentNativeObject = Method(typeof(TypedDocumentBinder), nameof(TypedDocumentBinder.CreateNativeTypedDocument), typeof(ScriptContext), typeof(string), typeof(string));
        public static readonly MethodInfo ReadTypedDocumentNativeMember = Method(typeof(TypedDocumentBinder), nameof(TypedDocumentBinder.ReadNativeTypedDocument), typeof(INativeTypedDocument), typeof(string), typeof(bool), typeof(ScriptDatum), typeof(string));
        public static readonly MethodInfo ReadTypedDocumentNativeElement = Method(typeof(TypedDocumentBinder), nameof(TypedDocumentBinder.ReadNativeTypedDocument), typeof(INativeTypedDocument), typeof(int), typeof(ScriptDatum), typeof(string));
        public static readonly MethodInfo ReadTypedDocumentNativeValue = Method(typeof(TypedDocumentBinder), nameof(TypedDocumentBinder.ReadNativeTypedDocument), typeof(INativeTypedDocument), typeof(ScriptDatum), typeof(string));

        public static readonly ConstructorInfo ScriptArrayCapacity = Constructor(typeof(ScriptArray), typeof(int));
        public static readonly MethodInfo ScriptArrayCreateWithLength = StaticMethod(typeof(ScriptArray), nameof(ScriptArray.CreateWithLength), typeof(ScriptDatum));
        public static readonly MethodInfo ScriptArrayCreateEmptyWithCapacity = StaticMethod(typeof(ScriptArray), nameof(ScriptArray.CreateEmptyWithCapacity), typeof(ScriptDatum));
        public static readonly MethodInfo ScriptArrayGetElement = InstanceMethod(typeof(ScriptArray), nameof(ScriptArray.GetElementValue), typeof(int));
        public static readonly MethodInfo ScriptArraySetElement = InstanceMethod(typeof(ScriptArray), nameof(ScriptArray.SetElementValue), typeof(int), typeof(ScriptDatum));
        public static readonly MethodInfo ScriptArrayPush = InstanceMethod(typeof(ScriptArray), nameof(ScriptArray.Push), typeof(ScriptDatum));
        public static readonly MethodInfo ScriptArrayHasOwnPushProperty = InstanceMethod(typeof(ScriptArray), nameof(ScriptArray.HasOwnPushProperty));
        public static readonly MethodInfo ScriptArrayLength = typeof(ScriptArray).GetProperty(nameof(ScriptArray.Length))?.GetMethod
            ?? throw new MissingMethodException(typeof(ScriptArray).FullName, "get_" + nameof(ScriptArray.Length));
        public static readonly ConstructorInfo ScriptObjectConstructor = Constructor(typeof(ScriptObject));
        public static readonly MethodInfo ScriptObjectSetProperty = InstanceMethod(typeof(ScriptObject), nameof(ScriptObject.SetPropertyDatum), typeof(ScriptContext), typeof(string), typeof(ScriptDatum));
        public static readonly MethodInfo ScriptObjectGetProperty = InstanceMethod(typeof(ScriptObject), nameof(ScriptObject.GetPropertyDatum), typeof(ScriptContext), typeof(string));
        public static readonly MethodInfo ScriptObjectDefineDatum = InstanceMethod(typeof(ScriptObject), nameof(ScriptObject.Define), typeof(string), typeof(ScriptDatum), typeof(bool), typeof(bool));
        public static readonly MethodInfo ScriptObjectCopyProperties = InstanceMethod(typeof(ScriptObject), nameof(ScriptObject.CopyPropertysFrom), typeof(ScriptObject), typeof(bool));
        public static readonly MethodInfo ScriptObjectCopyModuleExports = InstanceMethod(typeof(ScriptObject), nameof(ScriptObject.CopyModuleExportsFrom), typeof(ScriptObject), typeof(bool));
        public static readonly MethodInfo ScriptGlobalGetModuleByPath = InstanceMethod(typeof(ScriptGlobal), nameof(ScriptGlobal.GetModuleByPath), typeof(string));
        public static readonly MethodInfo ScriptGlobalEnsureModule = InstanceMethod(typeof(ScriptGlobal), nameof(ScriptGlobal.EnsureModule), typeof(string), typeof(ScriptSourceReference));
        public static readonly MethodInfo ScriptGlobalRegisterModule = InstanceMethod(typeof(ScriptGlobal), nameof(ScriptGlobal.RegisterModule), typeof(int), typeof(ScriptModule));
        public static readonly ConstructorInfo ScriptSourceReferenceConstructor = Constructor(typeof(ScriptSourceReference), typeof(string), typeof(string), typeof(string));
        public static readonly ConstructorInfo ScriptModuleConstructor = Constructor(typeof(ScriptModule), typeof(string), typeof(ScriptSourceReference));
        public static readonly MethodInfo ScriptModuleDefineExport =
            InstanceMethod(
                typeof(ScriptModule),
                nameof(ScriptModule.DefineExport),
                typeof(string),
                typeof(ScriptDatum),
                typeof(bool),
                typeof(bool),
                typeof(bool));
        public static readonly MethodInfo ScriptModuleDefineInternal =
            InstanceMethod(
                typeof(ScriptModule),
                nameof(ScriptModule.DefineInternal),
                typeof(string),
                typeof(ScriptDatum),
                typeof(bool),
                typeof(bool),
                typeof(bool));
        public static readonly MethodInfo ScriptObjectClearProperties = InstanceMethod(typeof(ScriptObject), nameof(ScriptObject.ClearProperties));

        public static readonly MethodInfo ValidatePackedArrayLength = Method(typeof(ScriptPackedArray), nameof(ScriptPackedArray.ValidateLength), typeof(double));
        public static readonly MethodInfo ToExactInt64Number = Method(typeof(ScriptPackedArray), nameof(ScriptPackedArray.ToExactInt64Number), typeof(long), typeof(int));
        public static readonly MethodInfo ToExactUInt64Number = Method(typeof(ScriptPackedArray), nameof(ScriptPackedArray.ToExactUInt64Number), typeof(ulong), typeof(int));
        public static readonly ConstructorInfo ScriptInt32ArrayConstructor = Constructor(typeof(ScriptInt32Array), typeof(int));
        public static readonly ConstructorInfo ScriptInt8ArrayConstructor = Constructor(typeof(ScriptInt8Array), typeof(int));
        public static readonly ConstructorInfo ScriptFloat64ArrayConstructor = Constructor(typeof(ScriptFloat64Array), typeof(int));
        public static readonly ConstructorInfo ScriptBooleanArrayConstructor = Constructor(typeof(ScriptBooleanArray), typeof(int));
        public static readonly ConstructorInfo ScriptUInt8ArrayConstructor = Constructor(typeof(ScriptUInt8Array), typeof(int));
        public static readonly ConstructorInfo ScriptInt16ArrayConstructor = Constructor(typeof(ScriptInt16Array), typeof(int));
        public static readonly ConstructorInfo ScriptUInt16ArrayConstructor = Constructor(typeof(ScriptUInt16Array), typeof(int));
        public static readonly ConstructorInfo ScriptUInt32ArrayConstructor = Constructor(typeof(ScriptUInt32Array), typeof(int));
        public static readonly ConstructorInfo ScriptInt64ArrayConstructor = Constructor(typeof(ScriptInt64Array), typeof(int));
        public static readonly ConstructorInfo ScriptUInt64ArrayConstructor = Constructor(typeof(ScriptUInt64Array), typeof(int));
        public static readonly ConstructorInfo ScriptDateTicksConstructor = Constructor(typeof(ScriptDate), typeof(long));
        public static readonly ConstructorInfo ScriptHashMapConstructor = Constructor(typeof(ScriptHashMap), typeof(int));
        public static readonly MethodInfo ScriptHashMapPut = InstanceMethod(typeof(ScriptHashMap), nameof(ScriptHashMap.Put), typeof(ScriptDatum), typeof(ScriptDatum));
        public static readonly FieldInfo ScriptInt32ArrayItems = Field(typeof(ScriptInt32Array), "_items");
        public static readonly FieldInfo ScriptInt8ArrayItems = Field(typeof(ScriptInt8Array), "_items");
        public static readonly FieldInfo ScriptFloat64ArrayItems = Field(typeof(ScriptFloat64Array), "_items");
        public static readonly FieldInfo ScriptBooleanArrayItems = Field(typeof(ScriptBooleanArray), "_items");
        public static readonly FieldInfo ScriptUInt8ArrayItems = Field(typeof(ScriptUInt8Array), "_items");
        public static readonly FieldInfo ScriptInt16ArrayItems = Field(typeof(ScriptInt16Array), "_items");
        public static readonly FieldInfo ScriptUInt16ArrayItems = Field(typeof(ScriptUInt16Array), "_items");
        public static readonly FieldInfo ScriptUInt32ArrayItems = Field(typeof(ScriptUInt32Array), "_items");
        public static readonly FieldInfo ScriptInt64ArrayItems = Field(typeof(ScriptInt64Array), "_items");
        public static readonly FieldInfo ScriptUInt64ArrayItems = Field(typeof(ScriptUInt64Array), "_items");

        public static MethodInfo PackedArrayBoundary(
            string methodName,
            Type parameterType) =>
            Method(
                typeof(PackedArrayBoundaryOps),
                methodName,
                parameterType);

        public static readonly MethodInfo EnterModuleFrame = Method(typeof(CallFrameOps), nameof(CallFrameOps.EnterModule), typeof(ScriptContext), typeof(ScriptModule));
        public static readonly MethodInfo LeaveFrame = Method(typeof(CallFrameOps), nameof(CallFrameOps.Leave), typeof(ScriptContext), typeof(int));
        public static readonly MethodInfo GetArgument = Method(typeof(CallFrameOps), nameof(CallFrameOps.GetArgument), typeof(Span<ScriptDatum>), typeof(int));
        public static readonly MethodInfo GetArgumentOrDefault = Method(typeof(CallFrameOps), nameof(CallFrameOps.GetArgumentOrDefault), typeof(Span<ScriptDatum>), typeof(int), typeof(ScriptDatum));
        public static readonly MethodInfo IsNullOrEmpty = Method(typeof(string), nameof(string.IsNullOrEmpty), typeof(string));
        public static readonly MethodInfo StringConcat2 = Method(typeof(string), nameof(string.Concat), typeof(string), typeof(string));
        public static readonly MethodInfo StringConcat3 = Method(typeof(string), nameof(string.Concat), typeof(string), typeof(string), typeof(string));
        public static readonly MethodInfo StringConcat4 = Method(typeof(string), nameof(string.Concat), typeof(string), typeof(string), typeof(string), typeof(string));
        public static readonly MethodInfo StringLength = Method(typeof(ValueOps), nameof(ValueOps.GetStringLength), typeof(string));
        public static readonly MethodInfo StringCharCodeAt = Method(typeof(ValueOps), nameof(ValueOps.GetStringCharCodeAt), typeof(string), typeof(int));
        public static readonly MethodInfo StringCharCodeAtInt32 = Method(typeof(ValueOps), nameof(ValueOps.GetStringCharCodeAtInt32), typeof(string), typeof(int));
        public static readonly MethodInfo AscendingLoopBound = Method(typeof(ValueOps), nameof(ValueOps.ToAscendingLoopBound), typeof(double));
        public static readonly ConstructorInfo StringBuilderCapacity = Constructor(typeof(StringBuilder), typeof(int));
        public static readonly MethodInfo StringBuilderAppend = InstanceMethod(typeof(StringBuilder), nameof(StringBuilder.Append), typeof(string));
        public static readonly MethodInfo StringBuilderToString = typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), Type.EmptyTypes);

        public static MethodInfo GetTypeCheck(CheckedType type)
        {
            return type switch
            {
                CheckedType.Null => CheckNull,
                CheckedType.Boolean => CheckBoolean,
                CheckedType.Number => CheckNumber,
                CheckedType.String => CheckString,
                CheckedType.Object => CheckObject,
                CheckedType.Array => CheckArray,
                CheckedType.Int32Array => CheckInt32Array,
                CheckedType.Int8Array => CheckInt8Array,
                CheckedType.Float64Array => CheckFloat64Array,
                CheckedType.BooleanArray => CheckBooleanArray,
                CheckedType.UInt8Array => CheckUInt8Array,
                CheckedType.Int16Array => CheckInt16Array,
                CheckedType.UInt16Array => CheckUInt16Array,
                CheckedType.UInt32Array => CheckUInt32Array,
                CheckedType.Int64Array => CheckInt64Array,
                CheckedType.UInt64Array => CheckUInt64Array,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private static MethodInfo TypeCheck(string name)
        {
            return Method(typeof(TypeCheckOps), name, typeof(ScriptDatum));
        }

        private static MethodInfo Method(Type type, string name, params Type[] parameterTypes)
        {
            return type.GetMethod(name, BindingFlags.Public | BindingFlags.Static, parameterTypes)
                ?? throw new MissingMethodException(type.FullName, name);
        }

        private static MethodInfo StaticMethod(Type type, string name, params Type[] parameterTypes)
        {
            return type.GetMethod(
                    name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    parameterTypes,
                    null)
                ?? throw new MissingMethodException(type.FullName, name);
        }

        private static MethodInfo InstanceMethod(Type type, string name, params Type[] parameterTypes)
        {
            return type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null)
                ?? throw new MissingMethodException(type.FullName, name);
        }

        private static ConstructorInfo Constructor(Type type, params Type[] parameterTypes)
        {
            return type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null)
                ?? throw new MissingMethodException(type.FullName, ".ctor");
        }

        private static FieldInfo Field(Type type, string name)
        {
            return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                ?? throw new MissingFieldException(type.FullName, name);
        }

        private static MethodInfo[] CallMethods(bool property)
        {
            var result = new MethodInfo[8];
            for (var arity = 0; arity < result.Length; arity++)
            {
                var prefix = property
                    ? new[] { typeof(ScriptDatum), typeof(ScriptContext), typeof(string) }
                    : new[] { typeof(ScriptDatum), typeof(ScriptContext) };
                var parameters = new Type[prefix.Length + arity];
                Array.Copy(prefix, parameters, prefix.Length);
                for (var i = prefix.Length; i < parameters.Length; i++) parameters[i] = typeof(ScriptDatum);
                result[arity] = Method(
                    typeof(CallOps),
                    (property ? nameof(CallOps.InvokeProperty0) : nameof(CallOps.Invoke0))[..^1] + arity,
                    parameters);
            }
            return result;
        }

        private static MethodInfo[] ResolveClosureDelegates()
        {
            var result = new MethodInfo[9];
            result[0] = Method(typeof(ClosureOps), nameof(ClosureOps.Resolve), typeof(int));
            for (var arity = 0; arity <= 7; arity++)
            {
                result[arity + 1] = Method(
                    typeof(ClosureOps),
                    nameof(ClosureOps.Resolve) + arity,
                    typeof(int));
            }
            return result;
        }

        private static ConstructorInfo[] GetClosureConstructors()
        {
            var delegates = new[]
            {
                typeof(ScriptFunctionDelegate),
                typeof(ScriptFunctionDelegate0),
                typeof(ScriptFunctionDelegate1),
                typeof(ScriptFunctionDelegate2),
                typeof(ScriptFunctionDelegate3),
                typeof(ScriptFunctionDelegate4),
                typeof(ScriptFunctionDelegate5),
                typeof(ScriptFunctionDelegate6),
                typeof(ScriptFunctionDelegate7)
            };
            var result = new ConstructorInfo[delegates.Length];
            for (var i = 0; i < delegates.Length; i++)
            {
                result[i] = Constructor(
                    typeof(ClosureFunction),
                    typeof(ScriptDomain),
                    typeof(ScriptModule),
                    delegates[i],
                    typeof(Upvalue[]),
                    typeof(string));
            }
            return result;
        }
    }
}
