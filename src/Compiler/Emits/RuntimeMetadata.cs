using AuroraScript.Runtime;
using AuroraScript.Runtime.Pool;
using AuroraScript.Runtime.Types;
using System;
using System.Reflection;

namespace AuroraScript.Compiler.Emits
{
    /// <summary>
    /// Static registry of Reflected metadata used by the <see cref="CILEmitter"/> to generate JIT code.
    /// This class caches MethodInfo and FieldInfo objects to avoid repeated reflection lookups during compilation.
    /// </summary>
    internal static class RuntimeMetadata
    {
        // CILContext (ScriptContext)
        public static readonly FieldInfo CILContext_Global = typeof(ScriptContext).GetField(nameof(ScriptContext.Global));
        public static readonly FieldInfo CILContext_Upvalues = typeof(ScriptContext).GetField(nameof(ScriptContext.Upvalues), BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly FieldInfo CILContext_Domain = typeof(ScriptContext).GetField(nameof(ScriptContext.Domain));
        public static readonly FieldInfo CILContext_Module = typeof(ScriptContext).GetField(nameof(ScriptContext.Module));
        public static readonly FieldInfo CILContext_UserState = typeof(ScriptContext).GetField(nameof(ScriptContext.UserState));
        public static readonly MethodInfo CILContext_With = typeof(ScriptContext).GetMethod(nameof(ScriptContext.With), BindingFlags.Instance | BindingFlags.Public, [typeof(ScriptModule), typeof(ClosureFunction)]);
        public static readonly FieldInfo CILContext_Location = typeof(ScriptContext).GetField(nameof(ScriptContext.Location));

        // ScriptGlobal
        public static readonly MethodInfo ScriptGlobal_RegisterModule = typeof(ScriptGlobal).GetMethod(nameof(ScriptGlobal.RegisterModule), BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo ScriptGlobal_GetModule = typeof(ScriptGlobal).GetMethod(nameof(ScriptGlobal.GetModule), BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo ScriptGlobal_EnsureModule = typeof(ScriptGlobal).GetMethod(nameof(ScriptGlobal.EnsureModule), BindingFlags.Instance | BindingFlags.NonPublic);

        // ScriptModule
        public static readonly ConstructorInfo ScriptModule_Ctor = typeof(ScriptModule).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(string), typeof(string)]);

        // ScriptObject
        public static readonly ConstructorInfo ScriptObject_Ctor = typeof(ScriptObject).GetConstructor([]);
        public static readonly MethodInfo ScriptObject_CopyPropertysFrom = typeof(ScriptObject).GetMethod(nameof(ScriptObject.CopyPropertysFrom), [typeof(ScriptObject), typeof(bool)]);
        public static readonly MethodInfo ScriptObject_Define = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Define), [typeof(string), typeof(ScriptObject), typeof(bool), typeof(bool)]);
        public static readonly MethodInfo ScriptObject_GetPropertyValue = typeof(ScriptObject).GetMethod(nameof(ScriptObject.GetPropertyValue), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(string)]);
        public static readonly MethodInfo ScriptObject_SetPropertyValue = typeof(ScriptObject).GetMethod(nameof(ScriptObject.SetPropertyValue), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(string), typeof(ScriptObject)]);
        public static readonly MethodInfo ScriptObject_Patch = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Patch), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(string), typeof(ScriptObject), typeof(bool), typeof(bool)]);
        public static readonly MethodInfo ScriptObject_ClearProperties = typeof(ScriptObject).GetMethod(nameof(ScriptObject.ClearProperties), BindingFlags.NonPublic | BindingFlags.Instance, []);
        public static readonly MethodInfo ScriptObject_Invoke = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Invoke), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(ScriptDatum[])]);
        public static readonly MethodInfo ScriptObject_GetIterator = typeof(ScriptObject).GetMethod(nameof(ScriptObject.GetEnumerator));

        // Upvalue
        public static readonly FieldInfo Upvalue_Value = typeof(Upvalue).GetField(nameof(Upvalue.Value));
        public static readonly ConstructorInfo Upvalue_CtorEmpty = typeof(Upvalue).GetConstructor([]);

        // ClosureFunction
        public static readonly ConstructorInfo ClosureFunction_Ctor = typeof(ClosureFunction).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(ScriptDomain), typeof(ScriptModule), typeof(ScriptFunctionDelegate), typeof(Upvalue[]), typeof(string)]);

        // ScriptDatum
        public static readonly MethodInfo ScriptDatum_FromObject = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.FromObject), [typeof(ScriptObject)]);
        public static readonly MethodInfo ScriptDatum_FromString = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.FromString), [typeof(string)]);
        public static readonly MethodInfo ScriptDatum_FromNumber = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.FromNumber), [typeof(double)]);
        public static readonly MethodInfo ScriptDatum_FromBoolean = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.FromBoolean), [typeof(bool)]);
        public static readonly MethodInfo ScriptDatum_ToObject = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.ToObject), BindingFlags.Public | BindingFlags.Static, [typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptDatum_ToString = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.ToString), BindingFlags.Public | BindingFlags.Static, [typeof(ScriptDatum)]);
        public static readonly FieldInfo ScriptDatum_Null = typeof(ScriptDatum).GetField(nameof(ScriptDatum.Null));

        // NullValue
        public static readonly FieldInfo NullValue_Instance = typeof(NullValue).GetField(nameof(NullValue.Instance));

        // Primitive Types (String, Number, Boolean)
        public static readonly MethodInfo StringValue_Of = typeof(StringValue).GetMethod(nameof(StringValue.Of), [typeof(string)]);
        public static readonly MethodInfo NumberValue_Of = typeof(NumberValue).GetMethod(nameof(NumberValue.Of), [typeof(double)]);
        public static readonly MethodInfo BooleanValue_Of = typeof(BooleanValue).GetMethod(nameof(BooleanValue.Of), [typeof(bool)]);

        // System.Array
        public static readonly MethodInfo Array_Empty_Upvalue = typeof(Array).GetMethod(nameof(Array.Empty)).MakeGenericMethod(typeof(Upvalue));

        // ScriptArray
        public static readonly ConstructorInfo ScriptArray_Ctor = typeof(ScriptArray).GetConstructor([typeof(ScriptDatum[])]);
        public static readonly ConstructorInfo ScriptArray_CtorCapacity = typeof(ScriptArray).GetConstructor([typeof(int)]);
        public static readonly MethodInfo ScriptArray_Get = typeof(ScriptArray).GetMethod(nameof(ScriptArray.GetElement), [typeof(int)]);
        public static readonly MethodInfo ScriptArray_Push = typeof(ScriptArray).GetMethod(nameof(ScriptArray.Push), [typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptArray_ToDatumArray = typeof(ScriptArray).GetMethod(nameof(ScriptArray.ToDatumArray), []);
        public static readonly MethodInfo ScriptArray_get_Length = typeof(ScriptArray).GetProperty(nameof(ScriptArray.Length)).GetGetMethod();
        public static readonly MethodInfo ScriptArray_SliceTo = typeof(ScriptArray).GetMethod(nameof(ScriptArray.SliceTo), [typeof(int), typeof(int), typeof(ScriptDatum).MakeByRefType()]);

        // Iteration (IEnumerator, ItemIterator)
        public static readonly MethodInfo ScriptEnumerator_NextValue = typeof(ScriptEnumerator).GetMethod(nameof(ScriptEnumerator.NextValue));
        public static readonly MethodInfo ScriptEnumerator_HasValue = typeof(ScriptEnumerator).GetMethod(nameof(ScriptEnumerator.HasValue));
        public static readonly MethodInfo ScriptEnumerator_Next = typeof(ScriptEnumerator).GetMethod(nameof(ScriptEnumerator.Next));
        public static readonly MethodInfo ScriptEnumerator_Value = typeof(ScriptEnumerator).GetMethod(nameof(ScriptEnumerator.Value));

        // CILHelper
        public static readonly MethodInfo CILHelper_GetElement = typeof(CILHelper).GetMethod(nameof(CILHelper.GetElement), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_SetElement = typeof(CILHelper).GetMethod(nameof(CILHelper.SetElement), [typeof(ScriptObject), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Add = typeof(CILHelper).GetMethod(nameof(CILHelper.Add), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Subtract = typeof(CILHelper).GetMethod(nameof(CILHelper.Subtract), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Multiply = typeof(CILHelper).GetMethod(nameof(CILHelper.Multiply), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Divide = typeof(CILHelper).GetMethod(nameof(CILHelper.Divide), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Modulo = typeof(CILHelper).GetMethod(nameof(CILHelper.Modulo), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Equal = typeof(CILHelper).GetMethod(nameof(CILHelper.Equal), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_NotEqual = typeof(CILHelper).GetMethod(nameof(CILHelper.NotEqual), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Less = typeof(CILHelper).GetMethod(nameof(CILHelper.Less), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_LessEqual = typeof(CILHelper).GetMethod(nameof(CILHelper.LessEqual), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Greater = typeof(CILHelper).GetMethod(nameof(CILHelper.Greater), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_GreaterEqual = typeof(CILHelper).GetMethod(nameof(CILHelper.GreaterEqual), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_BitwiseAnd = typeof(CILHelper).GetMethod(nameof(CILHelper.BitwiseAnd), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_BitwiseOr = typeof(CILHelper).GetMethod(nameof(CILHelper.BitwiseOr), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_BitwiseXor = typeof(CILHelper).GetMethod(nameof(CILHelper.BitwiseXor), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_LeftShift = typeof(CILHelper).GetMethod(nameof(CILHelper.LeftShift), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_RightShift = typeof(CILHelper).GetMethod(nameof(CILHelper.RightShift), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_UnsignedRightShift = typeof(CILHelper).GetMethod(nameof(CILHelper.UnsignedRightShift), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Not = typeof(CILHelper).GetMethod(nameof(CILHelper.Not), [typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_BitwiseNot = typeof(CILHelper).GetMethod(nameof(CILHelper.BitwiseNot), [typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Negate = typeof(CILHelper).GetMethod(nameof(CILHelper.Negate), [typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_TypeOf = typeof(CILHelper).GetMethod(nameof(CILHelper.TypeOf), [typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_IncrementPrefix = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementPrefix), [typeof(ScriptDatum).MakeByRefType()]);
        public static readonly MethodInfo CILHelper_IncrementPostfix = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementPostfix), [typeof(ScriptDatum).MakeByRefType()]);
        public static readonly MethodInfo CILHelper_DecrementPrefix = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementPrefix), [typeof(ScriptDatum).MakeByRefType()]);
        public static readonly MethodInfo CILHelper_DecrementPostfix = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementPostfix), [typeof(ScriptDatum).MakeByRefType()]);
        public static readonly MethodInfo CILHelper_IncrementElementPrefix = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementElementPrefix), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_IncrementElementPostfix = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementElementPostfix), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_DecrementElementPrefix = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementElementPrefix), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_DecrementElementPostfix = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementElementPostfix), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_IncrementPropertyPrefix = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementPropertyPrefix), [typeof(ScriptObject), typeof(string)]);
        public static readonly MethodInfo CILHelper_IncrementPropertyPostfix = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementPropertyPostfix), [typeof(ScriptObject), typeof(string)]);
        public static readonly MethodInfo CILHelper_DecrementPropertyPrefix = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementPropertyPrefix), [typeof(ScriptObject), typeof(string)]);
        public static readonly MethodInfo CILHelper_DecrementPropertyPostfix = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementPropertyPostfix), [typeof(ScriptObject), typeof(string)]);
        public static readonly MethodInfo CILHelper_DeleteProperty = typeof(CILHelper).GetMethod(nameof(CILHelper.DeleteProperty), [typeof(ScriptContext), typeof(ScriptObject), typeof(string)]);
        public static readonly MethodInfo CILHelper_DeleteElement = typeof(CILHelper).GetMethod(nameof(CILHelper.DeleteElement), [typeof(ScriptContext), typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_ToBoolean = typeof(CILHelper).GetMethod(nameof(CILHelper.ToBoolean), [typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_ToBoolean2 = typeof(CILHelper).GetMethod(nameof(CILHelper.ToBoolean), [typeof(ScriptObject)]);
        public static readonly MethodInfo CILHelper_SpreadInto = typeof(CILHelper).GetMethod(nameof(CILHelper.SpreadInto), [typeof(ScriptArray), typeof(ScriptObject)]);
        public static readonly MethodInfo CILHelper_New = typeof(CILHelper).GetMethod(nameof(CILHelper.New), [typeof(ScriptObject), typeof(ScriptContext), typeof(ScriptDatum[])]);
        public static readonly MethodInfo CILHelper_TryGetArg = typeof(CILHelper).GetMethod(nameof(CILHelper.TryGetArg), [typeof(ScriptDatum[]), typeof(int), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_GetArg = typeof(CILHelper).GetMethod(nameof(CILHelper.GetArg), [typeof(ScriptDatum[]), typeof(int)]);
        public static readonly MethodInfo CILHelper_ResolveDelegate = typeof(CILHelper).GetMethod(nameof(CILHelper.ResolveDelegate), [typeof(ScriptModule), typeof(int)]);
        public static readonly MethodInfo CILHelper_Throw = typeof(CILHelper).GetMethod(nameof(CILHelper.Throw), [typeof(ScriptDatum)]);

        // ClrMarshaller
        public static readonly MethodInfo CILHelper_ExceptionToError = typeof(CILHelper).GetMethod(nameof(CILHelper.ExceptionToError), [typeof(Exception)]);

        // RegexManager
        public static readonly MethodInfo RegexManager_LoadRegex = typeof(RegexManager).GetMethod(nameof(RegexManager.Resolve), BindingFlags.Static | BindingFlags.Public, [typeof(String), typeof(String)]);

    }
}
