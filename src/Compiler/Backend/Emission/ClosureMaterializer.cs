using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Emits;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal static class ClosureMaterializer
    {
        public static bool CanMaterialize(FunctionPlan function, bool requireName)
        {
            return function != null &&
                function.RequiresClosureObject &&
                function.Method != null &&
                (!requireName || !string.IsNullOrEmpty(function.Name));
        }

        public static bool CanPlanMaterialize(FunctionPlan function, bool requireName)
        {
            return function != null &&
                function.RequiresClosureObject &&
                (!requireName || !string.IsNullOrEmpty(function.Name));
        }

        public static void EmitClosure(
            EmissionSession session,
            ILGenerator il,
            FunctionPlan function,
            Action<UpvalueSlot> emitUpvalue = null)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Domain);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);
            EmitDelegate(session, il, function);
            EmitUpvalues(il, function, emitUpvalue);
            if (string.IsNullOrEmpty(function.Name))
            {
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
                session.Builder.LoadStringConstant(il, function.Name);
            }
            il.Emit(OpCodes.Newobj, GetClosureConstructor(function.CallConvention));
        }

        private static void EmitUpvalues(ILGenerator il, FunctionPlan function, Action<UpvalueSlot> emitUpvalue)
        {
            if (function.UpvalueSlots.Length == 0)
            {
                il.Emit(OpCodes.Call, RuntimeMetadata.Array_Empty_Upvalue);
                return;
            }

            if (emitUpvalue == null)
            {
                throw new NotSupportedException("Closure requires lexical upvalues.");
            }

            il.Emit(OpCodes.Ldc_I4, function.UpvalueSlots.Length);
            il.Emit(OpCodes.Newarr, typeof(Upvalue));
            for (var i = 0; i < function.UpvalueSlots.Length; i++)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, i);
                emitUpvalue(function.UpvalueSlots[i]);
                il.Emit(OpCodes.Stelem_Ref);
            }
        }

        private static void EmitDelegate(EmissionSession session, ILGenerator il, FunctionPlan function)
        {
            if (function.Method is DynamicMethod dynamicMethod)
            {
                var delegateId = session.GetDynamicDelegateId(dynamicMethod, function.CallConvention);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, delegateId);
                il.Emit(OpCodes.Call, GetResolveDelegateMethod(function.CallConvention));
                return;
            }

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldftn, function.Method);
            il.Emit(OpCodes.Newobj, GetDelegateConstructor(function.CallConvention));
        }

        public static void RegisterDynamicDelegate(int id, DynamicMethod dynamicMethod, FunctionCallConvention convention)
        {
            var del = convention switch
            {
                FunctionCallConvention.Fast0 => (Delegate)(ScriptFunctionDelegate0)dynamicMethod.CreateDelegate(typeof(ScriptFunctionDelegate0)),
                FunctionCallConvention.Fast1 => (ScriptFunctionDelegate1)dynamicMethod.CreateDelegate(typeof(ScriptFunctionDelegate1)),
                FunctionCallConvention.Fast2 => (ScriptFunctionDelegate2)dynamicMethod.CreateDelegate(typeof(ScriptFunctionDelegate2)),
                FunctionCallConvention.Fast3 => (ScriptFunctionDelegate3)dynamicMethod.CreateDelegate(typeof(ScriptFunctionDelegate3)),
                FunctionCallConvention.Fast4 => (ScriptFunctionDelegate4)dynamicMethod.CreateDelegate(typeof(ScriptFunctionDelegate4)),
                FunctionCallConvention.Fast5 => (ScriptFunctionDelegate5)dynamicMethod.CreateDelegate(typeof(ScriptFunctionDelegate5)),
                FunctionCallConvention.Fast6 => (ScriptFunctionDelegate6)dynamicMethod.CreateDelegate(typeof(ScriptFunctionDelegate6)),
                FunctionCallConvention.Fast7 => (ScriptFunctionDelegate7)dynamicMethod.CreateDelegate(typeof(ScriptFunctionDelegate7)),
                _ => (ScriptFunctionDelegate)dynamicMethod.CreateDelegate(typeof(ScriptFunctionDelegate))
            };
            DynamicMethodRegistry.RegisterReserved(id, dynamicMethod.Name, del);
        }

        private static MethodInfo GetResolveDelegateMethod(FunctionCallConvention convention)
        {
            return convention switch
            {
                FunctionCallConvention.Fast0 => RuntimeMetadata.CILHelper_ResolveDelegate0,
                FunctionCallConvention.Fast1 => RuntimeMetadata.CILHelper_ResolveDelegate1,
                FunctionCallConvention.Fast2 => RuntimeMetadata.CILHelper_ResolveDelegate2,
                FunctionCallConvention.Fast3 => RuntimeMetadata.CILHelper_ResolveDelegate3,
                FunctionCallConvention.Fast4 => RuntimeMetadata.CILHelper_ResolveDelegate4,
                FunctionCallConvention.Fast5 => RuntimeMetadata.CILHelper_ResolveDelegate5,
                FunctionCallConvention.Fast6 => RuntimeMetadata.CILHelper_ResolveDelegate6,
                FunctionCallConvention.Fast7 => RuntimeMetadata.CILHelper_ResolveDelegate7,
                _ => RuntimeMetadata.CILHelper_ResolveDelegate
            };
        }

        private static ConstructorInfo GetDelegateConstructor(FunctionCallConvention convention)
        {
            return GetDelegateType(convention).GetConstructors()[0];
        }

        private static Type GetDelegateType(FunctionCallConvention convention)
        {
            return convention switch
            {
                FunctionCallConvention.Fast0 => typeof(ScriptFunctionDelegate0),
                FunctionCallConvention.Fast1 => typeof(ScriptFunctionDelegate1),
                FunctionCallConvention.Fast2 => typeof(ScriptFunctionDelegate2),
                FunctionCallConvention.Fast3 => typeof(ScriptFunctionDelegate3),
                FunctionCallConvention.Fast4 => typeof(ScriptFunctionDelegate4),
                FunctionCallConvention.Fast5 => typeof(ScriptFunctionDelegate5),
                FunctionCallConvention.Fast6 => typeof(ScriptFunctionDelegate6),
                FunctionCallConvention.Fast7 => typeof(ScriptFunctionDelegate7),
                _ => typeof(ScriptFunctionDelegate)
            };
        }

        private static ConstructorInfo GetClosureConstructor(FunctionCallConvention convention)
        {
            return convention switch
            {
                FunctionCallConvention.Fast0 => RuntimeMetadata.ClosureFunction_Ctor0,
                FunctionCallConvention.Fast1 => RuntimeMetadata.ClosureFunction_Ctor1,
                FunctionCallConvention.Fast2 => RuntimeMetadata.ClosureFunction_Ctor2,
                FunctionCallConvention.Fast3 => RuntimeMetadata.ClosureFunction_Ctor3,
                FunctionCallConvention.Fast4 => RuntimeMetadata.ClosureFunction_Ctor4,
                FunctionCallConvention.Fast5 => RuntimeMetadata.ClosureFunction_Ctor5,
                FunctionCallConvention.Fast6 => RuntimeMetadata.ClosureFunction_Ctor6,
                FunctionCallConvention.Fast7 => RuntimeMetadata.ClosureFunction_Ctor7,
                _ => RuntimeMetadata.ClosureFunction_Ctor
            };
        }
    }
}
