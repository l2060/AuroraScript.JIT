using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Examples
{
    internal static class Class1
    {
        public delegate void MethodFunc(out nint method, out RuntimeMethodHandle handle);
        public static void Test()
        {
            DynamicMethod addMethod = new("Add", typeof(int), [typeof(int), typeof(int)], true);
            ILGenerator addIl = addMethod.GetILGenerator();
            addIl.Emit(OpCodes.Ldarg_0);
            addIl.Emit(OpCodes.Ldarg_1);
            addIl.Emit(OpCodes.Add);
            addIl.Emit(OpCodes.Ret);







            DynamicMethod getMethodPointerAndHandleMethod = new("", typeof(void), [typeof(nint).MakeByRefType(), typeof(RuntimeMethodHandle).MakeByRefType()]);
            ILGenerator il = getMethodPointerAndHandleMethod.GetILGenerator();
            Label actualMethodBody = il.DefineLabel();

            // The method "call" is here solely to register the "Add" method within the dynamic method's scope,
            // avoiding the need for extensive use of reflection. However, Mono and .NET (Fx, Core, etc.)
            // create metadata tokens in slightly different manners - this is why we need the ILGenerator APIs
            // to function correctly, so we don't have to manufacture these tokens through some cursed means.
            il.Emit(OpCodes.Br_S, actualMethodBody);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, addMethod);
            il.Emit(OpCodes.Pop);

            int addMethodMetadataToken = 0x6000002;

            il.MarkLabel(actualMethodBody);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldftn, addMethodMetadataToken);
            il.Emit(OpCodes.Stind_I);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldtoken, addMethodMetadataToken);
            il.Emit(OpCodes.Stobj, typeof(RuntimeMethodHandle));
            il.Emit(OpCodes.Ret);

            MethodFunc getMethodPointerAndHandle = (MethodFunc)getMethodPointerAndHandleMethod.CreateDelegate(typeof(MethodFunc));
            getMethodPointerAndHandle(out nint addFunctionPointer, out RuntimeMethodHandle addHandle);

            //Mono 6.12, which is installed on my machine, lacks support for `calli`, so it just

            //coredumps whenever it encounters this instruction.If you're using Microsoft's

            //fork(any version released after 2021 or so), this won't be a problem, so you

            //can safely remove this check.
#if !MONO
            unsafe
            {
                var add1 = (delegate*<int, int, int>)addFunctionPointer;
                Console.WriteLine("0x{0:X8}: 40 + 1 == {1}", (nint)add1, add1(40, 1));  // 0x________: 40 + 1 == 41
            }
#endif

            var add2 = (Func<int, int, int>)Activator.CreateInstance(typeof(Func<int, int, int>), null, addFunctionPointer)!;
            Console.WriteLine("{0}: 40 + 2 == {1}", add2.Method, add2(40, 2)); // Int32 Add(Int32, Int32): 40 + 2 == 42





            var add3 = (Func<int, int, int>)((MethodInfo)MethodBase.GetMethodFromHandle(addHandle)!).CreateDelegate(typeof(Func<int, int, int>));
            Console.WriteLine("{0}: 40 + 3 == {1}", add3.Method, add3(40, 3)); // Int32 Add(Int32, Int32): 40 + 3 == 43

            //Console.WriteLine();
            var address = GetDynamicMethodAddress(addMethod);

            var add222 = (Func<int, int, int>)Activator.CreateInstance(typeof(Func<int, int, int>), null, address)!;
            Console.WriteLine("{0}: 40 + 2 == {1}", add222.Method, add222(40, 2)); // Int32 Add(Int32, Int32): 40 + 2 == 42
            Console.WriteLine();

        }




        public static nint GetDynamicMethodAddress(DynamicMethod dynamicMethod)
        {
            DynamicMethod getMethodPointerAndHandleMethod = new("", typeof(void), [typeof(nint).MakeByRefType(), typeof(RuntimeMethodHandle).MakeByRefType()]);
            ILGenerator il = getMethodPointerAndHandleMethod.GetILGenerator();

            Label actualMethodBody = il.DefineLabel();
            il.Emit(OpCodes.Br_S, actualMethodBody);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, dynamicMethod);
            il.Emit(OpCodes.Pop);
            //int dynamicMethodMetadataToken = dynamicMethod.GetDynamicILInfo().GetTokenFor(dynamicMethod);
            int dynamicMethodMetadataToken = 0x6000002;
            il.MarkLabel(actualMethodBody);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldftn, dynamicMethodMetadataToken);
            il.Emit(OpCodes.Stind_I);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldtoken, dynamicMethodMetadataToken);
            il.Emit(OpCodes.Stobj, typeof(RuntimeMethodHandle));
            il.Emit(OpCodes.Ret);
            MethodFunc getMethodPointerAndHandle = (MethodFunc)getMethodPointerAndHandleMethod.CreateDelegate(typeof(MethodFunc));
            getMethodPointerAndHandle(out nint addFunctionPointer, out RuntimeMethodHandle addHandle);
            return addFunctionPointer;
        }











    }
}
