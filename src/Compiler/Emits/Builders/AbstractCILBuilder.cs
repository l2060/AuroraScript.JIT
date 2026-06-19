using AuroraScript.Compiler.Ast;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace AuroraScript.Compiler.Emits.Builders
{

    internal enum LoadState
    {
        Constant,
        Struct,
    }

    internal enum ConfuseTarget
    {
        Class,
        Method,
        Local,
        Constant,
    }
    internal abstract class AbstractCILBuilder
    {
        public const String EntryPointTypeName = "AuroraScriptInitializer";
        public const String EntryPointMethodName = "InitializeDomain";

        private readonly Dictionary<string, TypeBuilder> _typesToCreate = new();
        private readonly EngineOptions _options;

        public AbstractCILBuilder(EngineOptions options)
        {
            _options = options;
        }

        public abstract (MethodInfo Method, ILGenerator IL) DefineDynamicMethod(ModuleDeclaration module);
        public virtual (MethodInfo Method, ILGenerator IL) DefineBlockMethod(string methodName)
        {
            throw new NotImplementedException();
        }
        public abstract (MethodInfo Method, ILGenerator IL) DefineModuleInitMethod(ModuleDeclaration module);
        public abstract (MethodInfo Method, ILGenerator IL) DefineDomainInitMethod();

        public abstract (MethodInfo Method, ILGenerator IL) DefineMethod(string moduleName, string methodName, Type returnType, Type[] parameterTypes);

        public virtual FieldInfo DefineModuleField(string moduleName, string fieldName, Type fieldType)
        {
            if (!TryResolveType(moduleName, out var typeBuilder))
            {
                throw new Exception($"Module {moduleName} not defined");
            }
            return typeBuilder.DefineField(ConfuseTypeName(fieldName, ConfuseTarget.Constant), fieldType, FieldAttributes.Private | FieldAttributes.Static);
        }

        public abstract void SetLocalSymInfo(LocalBuilder local, String name);

        public abstract void MarkSequencePoint(AstNode node, ILGenerator il);
        public abstract void MarkSequencePoint(SourceSpan range, ILGenerator il);

        protected bool IsDebugMode => _options.OptimizeOption == OptimizeOptions.Debug;
        protected bool IsReleaseMode => _options.OptimizeOption == OptimizeOptions.Release;
        protected bool IsConfused => _options.EnableConfused;

        public abstract MethodInfo GetRuntimeEntryPoint();

        protected String ConfuseTypeName(String typeName, ConfuseTarget target)
        {
            String name = typeName;
            if (_options.EnableConfused)
            {
                //if (target == ConfuseTarget.Method) return "ToString";
                //if (target == ConfuseTarget.Class) return "record";
                //if (target == ConfuseTarget.Local) return "String\0" + Random.Shared.Next();



                name = "../..//" + GenSymbol();
            }
            return name;
        }


        protected string GenSymbol()
        {
            Char[] symbols = ['/', '.', '\n', '\r', '\t', '&', '|', '-', ' ', '-', (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10, (char)126, (char)127, (char)128];
            Byte[] bytes = new byte[32];
            int index = 0;
            while (index < bytes.Length)
            {
                var i = Random.Shared.Next(0, symbols.Length);
                bytes[index] = (Byte)symbols[i];
                index++;
            }
            return Encoding.UTF8.GetString(bytes);
        }


        protected void RegisterType(string typeName, TypeBuilder typeBuilder)
        {
            _typesToCreate.Add(typeName, typeBuilder);
        }

        protected bool TryResolveType(string typeName, out TypeBuilder typeBuilder)
        {
            return _typesToCreate.TryGetValue(typeName, out typeBuilder);
        }

        public void FinalizeBuild()
        {
            foreach (var typeBuilder in _typesToCreate)
            {
                typeBuilder.Value.CreateType();
            }
        }

        /// <summary>
        /// 提供常量隐藏的支持
        /// </summary>
        /// <param name="il"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public virtual LoadState LoadNumber(ILGenerator il, Double number)
        {
            il.Emit(OpCodes.Ldc_R8, number);
            return LoadState.Constant;
        }

        public virtual LoadState LoadBoolean(ILGenerator il, Boolean b)
        {
            il.Emit(b ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            return LoadState.Constant;
        }
        public virtual LoadState LoadString(ILGenerator il, String value)
        {
            il.Emit(OpCodes.Ldstr, value);
            return LoadState.Constant;
        }

        public virtual LoadState LoadStringConstant(ILGenerator il, String value)
        {
            il.Emit(OpCodes.Ldstr, value);
            return LoadState.Constant;
        }

        public virtual LoadState LoadNull(ILGenerator il)
        {
            il.Emit(OpCodes.Ldsfld, RuntimeMetadata.ScriptDatum_Null);
            return LoadState.Struct;
        }

    }



    internal class InternalConstant
    {
        internal const string AssemblyName = "AuroraScript.Generated";
        public readonly static CustomAttributeBuilder Release = new CustomAttributeBuilder(typeof(DebuggableAttribute).GetConstructor([typeof(bool), typeof(bool)]), [false, false]);
        public readonly static CustomAttributeBuilder Debug = new CustomAttributeBuilder(typeof(DebuggableAttribute).GetConstructor([typeof(DebuggableAttribute.DebuggingModes)])!, [DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.Default]);
    }






}
