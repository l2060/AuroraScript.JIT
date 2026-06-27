using AuroraScript.Runtime.Pool;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Static registry of reflected metadata used by the compiler backend to generate JIT code.
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
        public static readonly MethodInfo ScriptObject_CopyEnumerablePropertysFrom = typeof(ScriptObject).GetMethod(nameof(ScriptObject.CopyEnumerablePropertysFrom), [typeof(ScriptObject), typeof(bool)]);
        public static readonly MethodInfo ScriptObject_Define = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Define), [typeof(string), typeof(ScriptObject), typeof(bool), typeof(bool)]);
        public static readonly MethodInfo ScriptObject_GetPropertyValue = typeof(ScriptObject).GetMethod(nameof(ScriptObject.GetPropertyValue), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(string)]);
        public static readonly MethodInfo ScriptObject_GetPropertyDatum = typeof(ScriptObject).GetMethod(nameof(ScriptObject.GetPropertyDatum), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(string)]);
        public static readonly MethodInfo ScriptObject_SetPropertyValue = typeof(ScriptObject).GetMethod(nameof(ScriptObject.SetPropertyValue), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(string), typeof(ScriptObject)]);
        public static readonly MethodInfo ScriptObject_SetPropertyDatum = typeof(ScriptObject).GetMethod(nameof(ScriptObject.SetPropertyDatum), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(string), typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptObject_InternalDefineDatum = typeof(ScriptObject).GetMethod("InternalDefine", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(string), typeof(ScriptDatum), typeof(bool), typeof(bool), typeof(bool)]);
        public static readonly MethodInfo ScriptObject_Patch = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Patch), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(string), typeof(ScriptObject), typeof(bool), typeof(bool)]);
        public static readonly MethodInfo ScriptObject_ClearProperties = typeof(ScriptObject).GetMethod(nameof(ScriptObject.ClearProperties), BindingFlags.NonPublic | BindingFlags.Instance, []);
        public static readonly MethodInfo ScriptObject_Invoke = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Invoke), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(Span<ScriptDatum>)]);
        public static readonly MethodInfo ScriptObject_Invoke_0 = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Invoke), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext)]);
        public static readonly MethodInfo ScriptObject_Invoke_1 = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Invoke), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptObject_Invoke_2 = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Invoke), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptObject_Invoke_3 = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Invoke), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptObject_Invoke_4 = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Invoke), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptObject_Invoke_5 = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Invoke), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptObject_Invoke_6 = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Invoke), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptObject_Invoke_7 = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Invoke), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptObject_Invoke_8 = typeof(ScriptObject).GetMethod(nameof(ScriptObject.Invoke), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptObject_GetIterator = typeof(ScriptObject).GetMethod(nameof(ScriptObject.GetEnumerator));

        // Upvalue
        public static readonly FieldInfo Upvalue_Value = typeof(Upvalue).GetField(nameof(Upvalue.Value));
        public static readonly ConstructorInfo Upvalue_CtorEmpty = typeof(Upvalue).GetConstructor([]);

        // ClosureFunction
        public static readonly ConstructorInfo ClosureFunction_Ctor = typeof(ClosureFunction).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(ScriptDomain), typeof(ScriptModule), typeof(ScriptFunctionDelegate), typeof(Upvalue[]), typeof(string)]);
        public static readonly ConstructorInfo ClosureFunction_Ctor0 = typeof(ClosureFunction).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(ScriptDomain), typeof(ScriptModule), typeof(ScriptFunctionDelegate0), typeof(Upvalue[]), typeof(string)]);
        public static readonly ConstructorInfo ClosureFunction_Ctor1 = typeof(ClosureFunction).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(ScriptDomain), typeof(ScriptModule), typeof(ScriptFunctionDelegate1), typeof(Upvalue[]), typeof(string)]);
        public static readonly ConstructorInfo ClosureFunction_Ctor2 = typeof(ClosureFunction).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(ScriptDomain), typeof(ScriptModule), typeof(ScriptFunctionDelegate2), typeof(Upvalue[]), typeof(string)]);
        public static readonly ConstructorInfo ClosureFunction_Ctor3 = typeof(ClosureFunction).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(ScriptDomain), typeof(ScriptModule), typeof(ScriptFunctionDelegate3), typeof(Upvalue[]), typeof(string)]);
        public static readonly ConstructorInfo ClosureFunction_Ctor4 = typeof(ClosureFunction).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(ScriptDomain), typeof(ScriptModule), typeof(ScriptFunctionDelegate4), typeof(Upvalue[]), typeof(string)]);
        public static readonly ConstructorInfo ClosureFunction_Ctor5 = typeof(ClosureFunction).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(ScriptDomain), typeof(ScriptModule), typeof(ScriptFunctionDelegate5), typeof(Upvalue[]), typeof(string)]);
        public static readonly ConstructorInfo ClosureFunction_Ctor6 = typeof(ClosureFunction).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(ScriptDomain), typeof(ScriptModule), typeof(ScriptFunctionDelegate6), typeof(Upvalue[]), typeof(string)]);
        public static readonly ConstructorInfo ClosureFunction_Ctor7 = typeof(ClosureFunction).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(ScriptDomain), typeof(ScriptModule), typeof(ScriptFunctionDelegate7), typeof(Upvalue[]), typeof(string)]);

        // ScriptDatum
        public static readonly MethodInfo ScriptDatum_FromObject = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.FromObject), [typeof(ScriptObject)]);
        public static readonly MethodInfo ScriptDatum_FromString = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.FromString), [typeof(string)]);
        public static readonly MethodInfo ScriptDatum_FromNumber = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.FromNumber), [typeof(double)]);
        public static readonly MethodInfo ScriptDatum_FromBoolean = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.FromBoolean), [typeof(bool)]);
        public static readonly MethodInfo ScriptDatum_ToObject = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.ToObject), BindingFlags.Public | BindingFlags.Static, [typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptDatum_ToString = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.ToString), BindingFlags.Public | BindingFlags.Static, [typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptDatum_Equals = typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.Equals), [typeof(ScriptDatum)]);
        public static readonly FieldInfo ScriptDatum_Null = typeof(ScriptDatum).GetField(nameof(ScriptDatum.Null));

        // NullValue
        public static readonly FieldInfo NullValue_Instance = typeof(NullValue).GetField(nameof(NullValue.Instance));

        // Primitive Types (String, Number, Boolean)
        public static readonly MethodInfo StringValue_Of = typeof(StringValue).GetMethod(nameof(StringValue.Of), [typeof(string)]);
        public static readonly MethodInfo NumberValue_Of = typeof(NumberValue).GetMethod(nameof(NumberValue.Of), [typeof(double)]);
        public static readonly MethodInfo BooleanValue_Of = typeof(BooleanValue).GetMethod(nameof(BooleanValue.Of), [typeof(bool)]);

        // System.Array
        public static readonly MethodInfo Array_Empty_Upvalue = typeof(Array).GetMethod(nameof(Array.Empty)).MakeGenericMethod(typeof(Upvalue));
        public static readonly MethodInfo ScriptDatum_Array_Empty = typeof(Array).GetMethod(nameof(Array.Empty)).MakeGenericMethod(typeof(ScriptDatum));

        // ScriptArray
        public static readonly ConstructorInfo ScriptArray_Ctor = typeof(ScriptArray).GetConstructor([typeof(ScriptDatum[])]);
        public static readonly ConstructorInfo ScriptArray_SpanCtor = typeof(ScriptArray).GetConstructor([typeof(Span<ScriptDatum>)]);


        public static readonly ConstructorInfo ScriptArray_CtorCapacity = typeof(ScriptArray).GetConstructor([typeof(int)]);
        public static readonly MethodInfo ScriptArray_Get = typeof(ScriptArray).GetMethod(nameof(ScriptArray.GetElement), [typeof(int)]);
        public static readonly MethodInfo ScriptArray_SetElementValue = typeof(ScriptArray).GetMethod(nameof(ScriptArray.SetElementValue), BindingFlags.Instance | BindingFlags.NonPublic, [typeof(int), typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptArray_Push = typeof(ScriptArray).GetMethod(nameof(ScriptArray.Push), [typeof(ScriptDatum)]);
        public static readonly MethodInfo ScriptArray_ToDatumArray = typeof(ScriptArray).GetMethod(nameof(ScriptArray.ToDatumArray), []);
        public static readonly MethodInfo ScriptArray_get_Length = typeof(ScriptArray).GetProperty(nameof(ScriptArray.Length)).GetGetMethod();
        public static readonly MethodInfo ScriptArray_SliceTo = typeof(ScriptArray).GetMethod(nameof(ScriptArray.SliceTo), [typeof(int), typeof(int), typeof(ScriptDatum).MakeByRefType()]);

        // List<ScriptDatum>
        public static readonly ConstructorInfo List_ScriptDatum_Ctor = typeof(List<ScriptDatum>).GetConstructor([]);
        public static readonly MethodInfo List_ScriptDatum_Add = typeof(List<ScriptDatum>).GetMethod(nameof(List<ScriptDatum>.Add), [typeof(ScriptDatum)]);

        // CollectionsMarshal
        public static readonly MethodInfo CollectionsMarshal_AsSpan = typeof(CollectionsMarshal).GetMethod(nameof(CollectionsMarshal.AsSpan), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(typeof(ScriptDatum));

        // Iteration (IEnumerator, ItemIterator)
        public static readonly MethodInfo ScriptEnumerator_NextValue = typeof(ScriptEnumerator).GetMethod(nameof(ScriptEnumerator.NextValue));
        public static readonly MethodInfo ScriptEnumerator_HasValue = typeof(ScriptEnumerator).GetMethod(nameof(ScriptEnumerator.HasValue));
        public static readonly MethodInfo ScriptEnumerator_Next = typeof(ScriptEnumerator).GetMethod(nameof(ScriptEnumerator.Next));
        public static readonly MethodInfo ScriptEnumerator_Value = typeof(ScriptEnumerator).GetMethod(nameof(ScriptEnumerator.Value));

        // CILHelper
        public static readonly MethodInfo CILHelper_GetElement = typeof(CILHelper).GetMethod(nameof(CILHelper.GetElement), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_GetElementDatum = typeof(CILHelper).GetMethod(nameof(CILHelper.GetElement), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_GetElementNumber = typeof(CILHelper).GetMethod(nameof(CILHelper.GetElementNumber), [typeof(ScriptDatum), typeof(double)]);
        public static readonly MethodInfo CILHelper_GetElementAddNumberRight = typeof(CILHelper).GetMethod(nameof(CILHelper.GetElementAddNumberRight), [typeof(ScriptDatum), typeof(ScriptDatum), typeof(double)]);
        public static readonly MethodInfo CILHelper_GetElementAddNumberLeft = typeof(CILHelper).GetMethod(nameof(CILHelper.GetElementAddNumberLeft), [typeof(ScriptDatum), typeof(double), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_SetElement = typeof(CILHelper).GetMethod(nameof(CILHelper.SetElement), [typeof(ScriptObject), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_SetElementDatum = typeof(CILHelper).GetMethod(nameof(CILHelper.SetElement), [typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_CompoundAddElement = typeof(CILHelper).GetMethod(nameof(CILHelper.CompoundAddElement), [typeof(ScriptObject), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_CompoundAddElementDatum = typeof(CILHelper).GetMethod(nameof(CILHelper.CompoundAddElement), [typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_GetLength = typeof(CILHelper).GetMethod(nameof(CILHelper.GetLength), [typeof(ScriptObject), typeof(ScriptContext)]);
        public static readonly MethodInfo CILHelper_GetLengthDatum = typeof(CILHelper).GetMethod(nameof(CILHelper.GetLength), [typeof(ScriptDatum), typeof(ScriptContext)]);
        public static readonly MethodInfo CILHelper_GetProperty = typeof(CILHelper).GetMethod(nameof(CILHelper.GetProperty), [typeof(ScriptDatum), typeof(ScriptContext), typeof(string)]);
        public static readonly MethodInfo CILHelper_GetProperty2 = typeof(CILHelper).GetMethod(nameof(CILHelper.GetProperty2), [typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(string)]);
        public static readonly MethodInfo CILHelper_GetProperty2Datum = typeof(CILHelper).GetMethod(nameof(CILHelper.GetProperty2), [typeof(ScriptDatum), typeof(ScriptContext), typeof(string), typeof(string)]);
        public static readonly MethodInfo CILHelper_GetProperty3 = typeof(CILHelper).GetMethod(nameof(CILHelper.GetProperty3), [typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(string), typeof(string)]);
        public static readonly MethodInfo CILHelper_GetProperty3Datum = typeof(CILHelper).GetMethod(nameof(CILHelper.GetProperty3), [typeof(ScriptDatum), typeof(ScriptContext), typeof(string), typeof(string), typeof(string)]);
        public static readonly MethodInfo CILHelper_CreateObject3 = typeof(CILHelper).GetMethod(nameof(CILHelper.CreateObject3), [typeof(string), typeof(ScriptDatum), typeof(string), typeof(ScriptDatum), typeof(string), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_InvokeProperty = typeof(CILHelper).GetMethod(nameof(CILHelper.InvokeProperty), [typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(ScriptDatum[])]);
        public static readonly MethodInfo CILHelper_InvokeProperty0 = typeof(CILHelper).GetMethod(nameof(CILHelper.InvokeProperty0), [typeof(ScriptObject), typeof(ScriptContext), typeof(string)]);
        public static readonly MethodInfo CILHelper_InvokeProperty1 = typeof(CILHelper).GetMethod(nameof(CILHelper.InvokeProperty1), [typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_InvokeProperty2 = typeof(CILHelper).GetMethod(nameof(CILHelper.InvokeProperty2), [typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_InvokeProperty3 = typeof(CILHelper).GetMethod(nameof(CILHelper.InvokeProperty3), [typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_InvokeProperty4 = typeof(CILHelper).GetMethod(nameof(CILHelper.InvokeProperty4), [typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_InvokeProperty5 = typeof(CILHelper).GetMethod(nameof(CILHelper.InvokeProperty5), [typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_InvokeProperty6 = typeof(CILHelper).GetMethod(nameof(CILHelper.InvokeProperty6), [typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_InvokeProperty7 = typeof(CILHelper).GetMethod(nameof(CILHelper.InvokeProperty7), [typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_InvokePropertyMany = typeof(CILHelper).GetMethod(nameof(CILHelper.InvokePropertyMany), [typeof(ScriptObject), typeof(ScriptContext), typeof(string), typeof(ScriptDatum[]), typeof(int)]);
        public static readonly MethodInfo CILHelper_Invoke0 = typeof(CILHelper).GetMethod(nameof(CILHelper.Invoke0), [typeof(ScriptObject), typeof(ScriptContext)]);
        public static readonly MethodInfo CILHelper_Invoke1 = typeof(CILHelper).GetMethod(nameof(CILHelper.Invoke1), [typeof(ScriptObject), typeof(ScriptContext), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Invoke2 = typeof(CILHelper).GetMethod(nameof(CILHelper.Invoke2), [typeof(ScriptObject), typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Invoke3 = typeof(CILHelper).GetMethod(nameof(CILHelper.Invoke3), [typeof(ScriptObject), typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Invoke4 = typeof(CILHelper).GetMethod(nameof(CILHelper.Invoke4), [typeof(ScriptObject), typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Invoke5 = typeof(CILHelper).GetMethod(nameof(CILHelper.Invoke5), [typeof(ScriptObject), typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Invoke6 = typeof(CILHelper).GetMethod(nameof(CILHelper.Invoke6), [typeof(ScriptObject), typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Invoke7 = typeof(CILHelper).GetMethod(nameof(CILHelper.Invoke7), [typeof(ScriptObject), typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_RentArguments = typeof(CILHelper).GetMethod(nameof(CILHelper.RentArguments), [typeof(int)]);
        public static readonly MethodInfo CILHelper_AddArgument = typeof(CILHelper).GetMethod(nameof(CILHelper.AddArgument), [typeof(ScriptDatum[]), typeof(int).MakeByRefType(), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_SpreadIntoArguments = typeof(CILHelper).GetMethod(nameof(CILHelper.SpreadIntoArguments), [typeof(ScriptDatum[]), typeof(int).MakeByRefType(), typeof(ScriptObject)]);
        public static readonly MethodInfo CILHelper_InvokeMany = typeof(CILHelper).GetMethod(nameof(CILHelper.InvokeMany), [typeof(ScriptObject), typeof(ScriptContext), typeof(ScriptDatum[]), typeof(int)]);
        public static readonly MethodInfo CILHelper_ReturnArguments = typeof(CILHelper).GetMethod(nameof(CILHelper.ReturnArguments), [typeof(ScriptDatum[])]);
        public static readonly MethodInfo CILHelper_EnterDirect = typeof(CILHelper).GetMethod(nameof(CILHelper.EnterDirect), [typeof(ScriptContext), typeof(string)]);
        public static readonly MethodInfo CILHelper_EnterDirectClosure = typeof(CILHelper).GetMethod(nameof(CILHelper.EnterDirect), [typeof(ScriptContext), typeof(ClosureFunction)]);
        public static readonly MethodInfo CILHelper_LeaveDirect = typeof(CILHelper).GetMethod(nameof(CILHelper.LeaveDirect), [typeof(ScriptContext), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Add = typeof(CILHelper).GetMethod(nameof(CILHelper.Add), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_AddBool = typeof(CILHelper).GetMethod(nameof(CILHelper.AddBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_AddStringRight = typeof(CILHelper).GetMethod(nameof(CILHelper.AddStringRight), [typeof(ScriptDatum), typeof(string)]);
        public static readonly MethodInfo CILHelper_AddStringLeft = typeof(CILHelper).GetMethod(nameof(CILHelper.AddStringLeft), [typeof(string), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_AddStringMiddle = typeof(CILHelper).GetMethod(nameof(CILHelper.AddStringMiddle), [typeof(ScriptDatum), typeof(string), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_GetStringLength = typeof(CILHelper).GetMethod(nameof(CILHelper.GetStringLength), [typeof(string)]);
        public static readonly MethodInfo CILHelper_Subtract = typeof(CILHelper).GetMethod(nameof(CILHelper.Subtract), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Multiply = typeof(CILHelper).GetMethod(nameof(CILHelper.Multiply), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Divide = typeof(CILHelper).GetMethod(nameof(CILHelper.Divide), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Modulo = typeof(CILHelper).GetMethod(nameof(CILHelper.Modulo), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_SubtractBool = typeof(CILHelper).GetMethod(nameof(CILHelper.SubtractBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_MultiplyBool = typeof(CILHelper).GetMethod(nameof(CILHelper.MultiplyBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_DivideBool = typeof(CILHelper).GetMethod(nameof(CILHelper.DivideBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_ModuloBool = typeof(CILHelper).GetMethod(nameof(CILHelper.ModuloBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Equal = typeof(CILHelper).GetMethod(nameof(CILHelper.Equal), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_NotEqual = typeof(CILHelper).GetMethod(nameof(CILHelper.NotEqual), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Less = typeof(CILHelper).GetMethod(nameof(CILHelper.Less), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_LessEqual = typeof(CILHelper).GetMethod(nameof(CILHelper.LessEqual), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Greater = typeof(CILHelper).GetMethod(nameof(CILHelper.Greater), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_GreaterEqual = typeof(CILHelper).GetMethod(nameof(CILHelper.GreaterEqual), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_EqualBool = typeof(CILHelper).GetMethod(nameof(CILHelper.EqualBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_NotEqualBool = typeof(CILHelper).GetMethod(nameof(CILHelper.NotEqualBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_LessBool = typeof(CILHelper).GetMethod(nameof(CILHelper.LessBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_LessEqualBool = typeof(CILHelper).GetMethod(nameof(CILHelper.LessEqualBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_GreaterBool = typeof(CILHelper).GetMethod(nameof(CILHelper.GreaterBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_GreaterEqualBool = typeof(CILHelper).GetMethod(nameof(CILHelper.GreaterEqualBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_BitwiseAnd = typeof(CILHelper).GetMethod(nameof(CILHelper.BitwiseAnd), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_BitwiseOr = typeof(CILHelper).GetMethod(nameof(CILHelper.BitwiseOr), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_BitwiseXor = typeof(CILHelper).GetMethod(nameof(CILHelper.BitwiseXor), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_BitwiseAndBool = typeof(CILHelper).GetMethod(nameof(CILHelper.BitwiseAndBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_BitwiseOrBool = typeof(CILHelper).GetMethod(nameof(CILHelper.BitwiseOrBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_BitwiseXorBool = typeof(CILHelper).GetMethod(nameof(CILHelper.BitwiseXorBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_LeftShift = typeof(CILHelper).GetMethod(nameof(CILHelper.LeftShift), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_RightShift = typeof(CILHelper).GetMethod(nameof(CILHelper.RightShift), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_UnsignedRightShift = typeof(CILHelper).GetMethod(nameof(CILHelper.UnsignedRightShift), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_LeftShiftBool = typeof(CILHelper).GetMethod(nameof(CILHelper.LeftShiftBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_RightShiftBool = typeof(CILHelper).GetMethod(nameof(CILHelper.RightShiftBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_UnsignedRightShiftBool = typeof(CILHelper).GetMethod(nameof(CILHelper.UnsignedRightShiftBool), [typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Not = typeof(CILHelper).GetMethod(nameof(CILHelper.Not), [typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_BitwiseNot = typeof(CILHelper).GetMethod(nameof(CILHelper.BitwiseNot), [typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Negate = typeof(CILHelper).GetMethod(nameof(CILHelper.Negate), [typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_TypeOf = typeof(CILHelper).GetMethod(nameof(CILHelper.TypeOf), [typeof(ScriptDatum)]);

        // System.String / StringBuilder
        public static readonly MethodInfo String_Concat2 = typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)]);
        public static readonly MethodInfo String_Concat3 = typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string), typeof(string)]);
        public static readonly MethodInfo String_Concat4 = typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string), typeof(string), typeof(string)]);
        public static readonly ConstructorInfo StringBuilder_CtorCapacity = typeof(StringBuilder).GetConstructor([typeof(int)]);
        public static readonly MethodInfo StringBuilder_AppendString = typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), [typeof(string)]);
        public static readonly MethodInfo StringBuilder_ToString = typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), Type.EmptyTypes);

        public static readonly MethodInfo CILHelper_IncrementPrefix = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementPrefix), [typeof(ScriptDatum).MakeByRefType()]);
        public static readonly MethodInfo CILHelper_IncrementPostfix = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementPostfix), [typeof(ScriptDatum).MakeByRefType()]);
        public static readonly MethodInfo CILHelper_DecrementPrefix = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementPrefix), [typeof(ScriptDatum).MakeByRefType()]);
        public static readonly MethodInfo CILHelper_DecrementPostfix = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementPostfix), [typeof(ScriptDatum).MakeByRefType()]);
        public static readonly MethodInfo CILHelper_IncrementVoid = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementVoid), [typeof(ScriptDatum).MakeByRefType()]);
        public static readonly MethodInfo CILHelper_DecrementVoid = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementVoid), [typeof(ScriptDatum).MakeByRefType()]);
        public static readonly MethodInfo CILHelper_IncrementElementPrefix = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementElementPrefix), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_IncrementElementPostfix = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementElementPostfix), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_DecrementElementPrefix = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementElementPrefix), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_DecrementElementPostfix = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementElementPostfix), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_IncrementElementVoid = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementElementVoid), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_DecrementElementVoid = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementElementVoid), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_IncrementPropertyPrefix = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementPropertyPrefix), [typeof(ScriptObject), typeof(string)]);
        public static readonly MethodInfo CILHelper_IncrementPropertyPostfix = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementPropertyPostfix), [typeof(ScriptObject), typeof(string)]);
        public static readonly MethodInfo CILHelper_DecrementPropertyPrefix = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementPropertyPrefix), [typeof(ScriptObject), typeof(string)]);
        public static readonly MethodInfo CILHelper_DecrementPropertyPostfix = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementPropertyPostfix), [typeof(ScriptObject), typeof(string)]);
        public static readonly MethodInfo CILHelper_IncrementPropertyVoid = typeof(CILHelper).GetMethod(nameof(CILHelper.IncrementPropertyVoid), [typeof(ScriptObject), typeof(string)]);
        public static readonly MethodInfo CILHelper_DecrementPropertyVoid = typeof(CILHelper).GetMethod(nameof(CILHelper.DecrementPropertyVoid), [typeof(ScriptObject), typeof(string)]);
        public static readonly MethodInfo CILHelper_DeleteProperty = typeof(CILHelper).GetMethod(nameof(CILHelper.DeleteProperty), [typeof(ScriptContext), typeof(ScriptObject), typeof(string)]);
        public static readonly MethodInfo CILHelper_DeleteElement = typeof(CILHelper).GetMethod(nameof(CILHelper.DeleteElement), [typeof(ScriptContext), typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_Included = typeof(CILHelper).GetMethod(nameof(CILHelper.Included), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_IncludedBool = typeof(CILHelper).GetMethod(nameof(CILHelper.IncludedBool), [typeof(ScriptObject), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_ToBoolean = typeof(CILHelper).GetMethod(nameof(CILHelper.ToBoolean), [typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_ToBoolean2 = typeof(CILHelper).GetMethod(nameof(CILHelper.ToBoolean), [typeof(ScriptObject)]);
        public static readonly MethodInfo CILHelper_SpreadInto = typeof(CILHelper).GetMethod(nameof(CILHelper.SpreadInto), [typeof(ScriptArray), typeof(ScriptObject)]);
        public static readonly MethodInfo CILHelper_SpreadIntoList = typeof(CILHelper).GetMethod(nameof(CILHelper.SpreadIntoList), [typeof(List<ScriptDatum>), typeof(ScriptObject)]);
        public static readonly MethodInfo CILHelper_New = typeof(CILHelper).GetMethod(nameof(CILHelper.New), [typeof(ScriptObject), typeof(ScriptContext), typeof(Span<ScriptDatum>)]);
        public static readonly MethodInfo CILHelper_New0 = typeof(CILHelper).GetMethod(nameof(CILHelper.New0), [typeof(ScriptObject), typeof(ScriptContext)]);
        public static readonly MethodInfo CILHelper_New1 = typeof(CILHelper).GetMethod(nameof(CILHelper.New1), [typeof(ScriptObject), typeof(ScriptContext), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_New2 = typeof(CILHelper).GetMethod(nameof(CILHelper.New2), [typeof(ScriptObject), typeof(ScriptContext), typeof(ScriptDatum), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_NewMany = typeof(CILHelper).GetMethod(nameof(CILHelper.NewMany), [typeof(ScriptObject), typeof(ScriptContext), typeof(ScriptDatum[]), typeof(int)]);
        public static readonly MethodInfo CILHelper_TryGetArg = typeof(CILHelper).GetMethod(nameof(CILHelper.TryGetArg), [typeof(Span<ScriptDatum>), typeof(int), typeof(ScriptDatum)]);
        public static readonly MethodInfo CILHelper_GetArg = typeof(CILHelper).GetMethod(nameof(CILHelper.GetArg), [typeof(Span<ScriptDatum>), typeof(int)]);
        public static readonly MethodInfo CILHelper_ResolveDelegate = typeof(CILHelper).GetMethod(nameof(CILHelper.ResolveDelegate), [typeof(ScriptModule), typeof(int)]);
        public static readonly MethodInfo CILHelper_ResolveDelegate0 = typeof(CILHelper).GetMethod(nameof(CILHelper.ResolveDelegate0), [typeof(ScriptModule), typeof(int)]);
        public static readonly MethodInfo CILHelper_ResolveDelegate1 = typeof(CILHelper).GetMethod(nameof(CILHelper.ResolveDelegate1), [typeof(ScriptModule), typeof(int)]);
        public static readonly MethodInfo CILHelper_ResolveDelegate2 = typeof(CILHelper).GetMethod(nameof(CILHelper.ResolveDelegate2), [typeof(ScriptModule), typeof(int)]);
        public static readonly MethodInfo CILHelper_ResolveDelegate3 = typeof(CILHelper).GetMethod(nameof(CILHelper.ResolveDelegate3), [typeof(ScriptModule), typeof(int)]);
        public static readonly MethodInfo CILHelper_ResolveDelegate4 = typeof(CILHelper).GetMethod(nameof(CILHelper.ResolveDelegate4), [typeof(ScriptModule), typeof(int)]);
        public static readonly MethodInfo CILHelper_ResolveDelegate5 = typeof(CILHelper).GetMethod(nameof(CILHelper.ResolveDelegate5), [typeof(ScriptModule), typeof(int)]);
        public static readonly MethodInfo CILHelper_ResolveDelegate6 = typeof(CILHelper).GetMethod(nameof(CILHelper.ResolveDelegate6), [typeof(ScriptModule), typeof(int)]);
        public static readonly MethodInfo CILHelper_ResolveDelegate7 = typeof(CILHelper).GetMethod(nameof(CILHelper.ResolveDelegate7), [typeof(ScriptModule), typeof(int)]);
        public static readonly MethodInfo CILHelper_Throw = typeof(CILHelper).GetMethod(nameof(CILHelper.Throw), [typeof(ScriptDatum)]);

        // ClrMarshaller
        public static readonly MethodInfo CILHelper_ExceptionToError = typeof(CILHelper).GetMethod(nameof(CILHelper.ExceptionToError), [typeof(Exception)]);
        public static readonly MethodInfo CILHelper_ExceptionToErrorWithContext = typeof(CILHelper).GetMethod(nameof(CILHelper.ExceptionToError), [typeof(Exception), typeof(ScriptContext)]);

        // RegexManager
        public static readonly MethodInfo RegexManager_LoadRegex = typeof(RegexManager).GetMethod(nameof(RegexManager.Resolve), BindingFlags.Static | BindingFlags.Public, [typeof(String), typeof(String)]);

    }
}
