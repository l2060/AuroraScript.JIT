using AuroraScript.Compiler.Ast;
using AuroraScript.Runtime;
using AuroraScript.Compiler.Backend.Code;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace AuroraScript.Compiler.Backend.Builders
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

        public virtual (MethodInfo Method, ILGenerator IL) DefineDynamicMethod(ModuleDeclaration module)
        {
            throw new NotSupportedException($"{GetType().Name} does not support hot-patch dynamic methods.");
        }

        public virtual (MethodInfo Method, ILGenerator IL) DefineBlockMethod(string methodName)
        {
            throw new NotSupportedException($"{GetType().Name} does not support standalone compiled blocks.");
        }
        public abstract (MethodInfo Method, ILGenerator IL) DefineModuleInitMethod(ModuleDeclaration module);
        public abstract (MethodInfo Method, ILGenerator IL) DefineDomainInitMethod();

        public abstract (MethodInfo Method, ILGenerator IL) DefineMethod(
            string moduleKey,
            string methodName,
            Type returnType,
            Type[] parameterTypes,
            bool aggressiveInlining = false);

        public virtual void SetDebuggerMetadata(MethodInfo method, string metadata)
        {
        }

        public virtual void SetLocalSymInfo(LocalBuilder local, String name)
        {
        }

        public virtual void MarkSequencePoint(AstNode node, ILGenerator il)
        {
        }

        public virtual void MarkSequencePoint(SourceSpan range, ILGenerator il)
        {
        }

        protected bool IsDebugMode => _options.Optimization.Level == OptimizeOptions.Debug;
        protected bool IsReleaseMode => _options.Optimization.Level == OptimizeOptions.Release;
        protected bool IsConfused => _options.Output.EnableConfused;

        public abstract MethodInfo GetRuntimeEntryPoint();

        protected String ConfuseTypeName(String typeName, ConfuseTarget target)
        {
            String name = typeName;
            if (_options.Output.EnableConfused)
            {
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

        public LoadState LoadNullableStringConstant(ILGenerator il, String value)
        {
            if (value == null)
            {
                il.Emit(OpCodes.Ldnull);
                return LoadState.Constant;
            }

            return LoadStringConstant(il, value);
        }

        public virtual LoadState LoadNull(ILGenerator il)
        {
            il.Emit(OpCodes.Ldsfld, TypedRuntimeMetadata.DatumNull);
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
