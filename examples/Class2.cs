namespace Examples
{
    internal class Class2
    {



        public void Method2()
        {

            //AssemblyName assemblyName = new AssemblyName();
            //assemblyName.Name = "DynamicllyGeneratedAssembly";
            //AssemblyBuilder assemblyBuilder = System.Threading.Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);

            //ConstructorInfo daCtor = typeof(DebuggableAttribute).GetConstructor(new Type[] { typeof(DebuggableAttribute.DebuggingModes) });
            //CustomAttributeBuilder daBuilder = new CustomAttributeBuilder(daCtor, new object[] { DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.Default });
            //assemblyBuilder.SetCustomAttribute(daBuilder);

            //ModuleBuilder module = assemblyBuilder.DefineDynamicModule("DynamicllyGeneratedModule.exe", true);

            //ISymbolDocumentWriter doc = module.DefineDocument(@"Source.txt", Guid.Empty, Guid.Empty, Guid.Empty);
            //TypeBuilder typeBuilder = module.DefineType("DynamicllyGeneratedType", TypeAttributes.Public | TypeAttributes.Class);
            //MethodBuilder methodbuilder = typeBuilder.DefineMethod("Main", MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.Public, typeof(void), new Type[] { typeof(string[]) });
            //ILGenerator ilGenerator = methodbuilder.GetILGenerator();

            //ilGenerator.MarkSequencePoint(doc, 1, 1, 1, 100);
            //ilGenerator.Emit(OpCodes.Ldstr, "Hello world!");
            //MethodInfo infoWriteLine = typeof(System.Console).GetMethod("WriteLine", new Type[] { typeof(string) });
            //ilGenerator.EmitCall(OpCodes.Call, infoWriteLine, null);

            //ilGenerator.MarkSequencePoint(doc, 2, 1, 2, 100);
            //ilGenerator.Emit(OpCodes.Ret);

            //Type helloWorldType = typeBuilder.CreateType();

            //System.Diagnostics.Debugger.Break();
            //helloWorldType.GetMethod("Main").Invoke(null, new string[] { null });
            //Console.WriteLine("This is Method2 in Class2.");
        }

    }
}
