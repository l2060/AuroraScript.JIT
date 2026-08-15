using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Code;
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
            il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextDomain);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextModule);
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
                il.Emit(OpCodes.Call, TypedRuntimeMetadata.EmptyUpvalues);
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
                var delegateId = session.GetDynamicDelegateId(function, dynamicMethod);
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
            DynamicMethodRegistry.RegisterReserved(id, del);
        }

        private static MethodInfo GetResolveDelegateMethod(FunctionCallConvention convention)
        {
            return convention switch
            {
                FunctionCallConvention.Fast0 => TypedRuntimeMetadata.ResolveClosureDelegate[1],
                FunctionCallConvention.Fast1 => TypedRuntimeMetadata.ResolveClosureDelegate[2],
                FunctionCallConvention.Fast2 => TypedRuntimeMetadata.ResolveClosureDelegate[3],
                FunctionCallConvention.Fast3 => TypedRuntimeMetadata.ResolveClosureDelegate[4],
                FunctionCallConvention.Fast4 => TypedRuntimeMetadata.ResolveClosureDelegate[5],
                FunctionCallConvention.Fast5 => TypedRuntimeMetadata.ResolveClosureDelegate[6],
                FunctionCallConvention.Fast6 => TypedRuntimeMetadata.ResolveClosureDelegate[7],
                FunctionCallConvention.Fast7 => TypedRuntimeMetadata.ResolveClosureDelegate[8],
                _ => TypedRuntimeMetadata.ResolveClosureDelegate[0]
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
                FunctionCallConvention.Fast0 => TypedRuntimeMetadata.ClosureConstructors[1],
                FunctionCallConvention.Fast1 => TypedRuntimeMetadata.ClosureConstructors[2],
                FunctionCallConvention.Fast2 => TypedRuntimeMetadata.ClosureConstructors[3],
                FunctionCallConvention.Fast3 => TypedRuntimeMetadata.ClosureConstructors[4],
                FunctionCallConvention.Fast4 => TypedRuntimeMetadata.ClosureConstructors[5],
                FunctionCallConvention.Fast5 => TypedRuntimeMetadata.ClosureConstructors[6],
                FunctionCallConvention.Fast6 => TypedRuntimeMetadata.ClosureConstructors[7],
                FunctionCallConvention.Fast7 => TypedRuntimeMetadata.ClosureConstructors[8],
                _ => TypedRuntimeMetadata.ClosureConstructors[0]
            };
        }
    }
}
