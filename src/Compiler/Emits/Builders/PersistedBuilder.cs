using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;


namespace AuroraScript.Compiler.Emits.Builders
{
#if NET9_0_OR_GREATER
    internal class PersistedBuilder : AbstractCILBuilder
    {
        private readonly PersistedAssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Dictionary<String, ISymbolDocumentWriter> _sourceDocumentMap = new();

        public PersistedBuilder(EngineOptions options) : base(options)
        {
            var assemblyName = new AssemblyName(InternalConstant.AssemblyName);
            assemblyName.Version = new Version(1, 0, 0, 0);
            var optimizeOption = IsDebugMode ? InternalConstant.Debug : InternalConstant.Release;
            _assemblyBuilder = new PersistedAssemblyBuilder(assemblyName, typeof(int).Assembly, [optimizeOption]);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(InternalConstant.AssemblyName);
        }

        public override (MethodInfo Method, ILGenerator IL) DefineModuleInitMethod(ModuleDeclaration module)
        {
            //
            var typeBuilder = _moduleBuilder.DefineType(ConfuseTypeName(module.ModuleName, ConfuseTarget.Class), TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
            var methodBuilder = typeBuilder.DefineMethod(ConfuseTypeName("Initialize", ConfuseTarget.Method), MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, typeof(void), [typeof(ScriptContext), typeof(Span<ScriptDatum>)]);
            ISymbolDocumentWriter symbolDoc = null;
            symbolDoc = _moduleBuilder.DefineDocument(module.FullPath, Guid.Empty, Guid.Empty, SymDocumentType.Text);
            _sourceDocumentMap.Add(module.FullPath, symbolDoc);
            RegisterType(module.ModuleName, typeBuilder);
            return (methodBuilder, methodBuilder.GetILGenerator());
        }

        public override (MethodInfo Method, ILGenerator IL) DefineDomainInitMethod()
        {
            var typeBuilder = _moduleBuilder.DefineType(EntryPointTypeName, TypeAttributes.Public | TypeAttributes.Class);
            var methodBuilder = typeBuilder.DefineMethod(EntryPointMethodName, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, typeof(ScriptDatum), [typeof(ScriptContext), typeof(Span<ScriptDatum>)]);
            RegisterType(EntryPointTypeName, typeBuilder);
            return (methodBuilder, methodBuilder.GetILGenerator());
        }

        public override (MethodInfo Method, ILGenerator IL) DefineMethod(string moduleName, string methodName, Type returnType, Type[] parameterTypes)
        {
            var typeName = moduleName;
            if (!TryResolveType(typeName, out var typeBuilder))
            {
                throw new Exception($"Module {moduleName} not defined");
            }
            var method = typeBuilder.DefineMethod(ConfuseTypeName(methodName, ConfuseTarget.Method), MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);

            return (method, method.GetILGenerator());
        }


        public byte[] Serialize()
        {
            var builder = _assemblyBuilder;
            // 1. 生成元数据（确保只调用一次）
            var metadataBuilder = builder.GenerateMetadata(out BlobBuilder ilBuilder, out var mappedFieldData, out var pdbMetadataBuilder);

            DebugDirectoryBuilder debugDirectoryBuilder = null;
            BlobBuilder pdbBlob = null;
            BlobContentId pdbContentId = default;

            if (IsDebugMode)
            {
                // TableIndex
                var rowCounts = pdbMetadataBuilder.GetRowCounts();
                var typeSystemRowCounts = rowCounts.Take(MetadataTokens.TableCount).ToArray();
                EnsureValidPortablePdbRowCounts(ref typeSystemRowCounts);
                var pdbBuilder = new PortablePdbBuilder(pdbMetadataBuilder, typeSystemRowCounts.ToImmutableArray(), default);
                pdbBlob = new BlobBuilder();
                pdbContentId = pdbBuilder.Serialize(pdbBlob);
                // 3. 构建调试目录
                debugDirectoryBuilder = new DebugDirectoryBuilder();
                debugDirectoryBuilder.AddReproducibleEntry();
                debugDirectoryBuilder.AddEmbeddedPortablePdbEntry(pdbBlob, pdbBuilder.FormatVersion);
            }

            Func<IEnumerable<Blob>, BlobContentId> idProvider = content => pdbContentId;
            var peHeader = new PEHeaderBuilder(
                machine: Machine.I386,
                sectionAlignment: 0x2000,            // 标准对齐
                fileAlignment: 0x200,                // 标准文件对齐
                imageBase: 0x10000000,
                majorLinkerVersion: 14,              // 现代链接器版本
                minorLinkerVersion: 0,
                majorOperatingSystemVersion: 6,      // Windows 10/11
                minorOperatingSystemVersion: 0,
                majorImageVersion: 0,
                minorImageVersion: 0,
                majorSubsystemVersion: 6,            // Windows 10/11子系统
                minorSubsystemVersion: 0,
                subsystem: Subsystem.Unknown,
                dllCharacteristics: GetDllCharacteristics(),
                imageCharacteristics: Characteristics.Dll,
                sizeOfStackReserve: 0x00100000,
                sizeOfStackCommit: 0x00001000,
                sizeOfHeapReserve: 0x00100000,
                sizeOfHeapCommit: 0x00001000);

            // 4. 创建 PE 构建器（使用正确的参数）
            var peBuilder = new ManagedPEBuilder(
                header: peHeader,
                metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
                ilStream: ilBuilder,
                mappedFieldData: mappedFieldData,
                debugDirectoryBuilder: debugDirectoryBuilder,
                deterministicIdProvider: idProvider,
                flags: CorFlags.ILOnly | CorFlags.ILLibrary | CorFlags.TrackDebugData,
                strongNameSignatureSize: 0
                );

            // 5. 执行序列化（关键：确保 PE 构建器未被重复使用）
            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);
            return peBlob.ToArray();
        }


        private DllCharacteristics GetDllCharacteristics()
        {
            return DllCharacteristics.DynamicBase |      // ASLR支持
                   DllCharacteristics.NoSeh |           // 无SEH
                   DllCharacteristics.NxCompatible |        // NX兼容
                   DllCharacteristics.TerminalServerAware | // 终端服务感知
                   DllCharacteristics.HighEntropyVirtualAddressSpace;    // 高熵地址空间（64位）
        }



        // 关键修复方法：确保 Portable PDB 行计数有效
        private void EnsureValidPortablePdbRowCounts(ref int[] typeSystemRowCounts)
        {
            // Portable PDB 允许的表格掩码
            // 根据 ECMA-335 和 Portable PDB 规范
            const ulong ValidPortablePdbExternalTables = 0x0000041007F3D857;

            for (int i = 0; i < typeSystemRowCounts.Length; i++)
            {
                // 检查是否在允许的表格掩码中
                if (((1UL << i) & ValidPortablePdbExternalTables) == 0)
                {
                    // 不允许的表格必须行数为0
                    typeSystemRowCounts[i] = 0;
                }
                else
                {
                    // 允许的表格，确保行数有效
                    if (typeSystemRowCounts[i] < 0)
                    {
                        typeSystemRowCounts[i] = 0;
                    }
                }
            }

            // 特别处理表 #48（ModuleRef），它必须为0
            const int MODULE_REF_TABLE_INDEX = 48;
            if (MODULE_REF_TABLE_INDEX < typeSystemRowCounts.Length)
            {
                typeSystemRowCounts[MODULE_REF_TABLE_INDEX] = 0;
            }
        }

        public override MethodInfo GetRuntimeEntryPoint() => null;

        public override void SetLocalSymInfo(LocalBuilder local, string name)
        {
            name ??= String.Empty;
            local.SetLocalSymInfo(ConfuseTypeName(IsDebugMode ? name : String.Empty, ConfuseTarget.Local));
        }
        public override void MarkSequencePoint(AstNode node, ILGenerator il)
        {
            if (node is ModuleDeclaration || node is BlockStatement || il == null) return;
            MarkSequencePoint(node.Range, il);
        }

        public override void MarkSequencePoint(SourceSpan range, ILGenerator il)
        {
            if (IsConfused || IsReleaseMode || il == null) return;
            if (range.StartLine <= 0)
            {
                return;
            }
            if (_sourceDocumentMap.TryGetValue(range.FileName, out var doc))
            {
                var endLine = range.EndLine;
                var endColumn = range.EndColumn;
                if (endLine == range.StartLine && endColumn <= range.StartColumn)
                {
                    endColumn = range.StartColumn + 1;
                }
                il.MarkSequencePoint(doc, range.StartLine, range.StartColumn, endLine, endColumn);
                il.Emit(OpCodes.Nop);
            }
        }




        public override LoadState LoadNumber(ILGenerator il, double number)
        {
            if (IsConfused)
            {
                var id = "N" + number;
                var field = consts.GetOrAdd(id, (key) =>
                {
                    var bytes = new Byte[24];
                    bytes[0] = (byte)ValueKind.Number;
                    BitConverter.TryWriteBytes(new Span<Byte>(bytes, 8, 8), number);
                    var name = ConfuseTypeName(id, ConfuseTarget.Constant);
                    return _moduleBuilder.DefineInitializedData(name, bytes, FieldAttributes.Public | FieldAttributes.Static);
                });
                il.Emit(OpCodes.Ldsfld, field);
                return LoadState.Struct;
            }
            else
            {
                return base.LoadNumber(il, number);
            }
        }

        public override LoadState LoadBoolean(ILGenerator il, Boolean b)
        {
            if (IsConfused)
            {
                var id = b ? "B1" : "B0";
                var field = consts.GetOrAdd(id, (key) =>
                {
                    var bytes = new Byte[24];
                    bytes[0] = (byte)ValueKind.Boolean;
                    bytes[8] = (byte)(b ? 1 : 0);
                    var name = ConfuseTypeName(id, ConfuseTarget.Constant);
                    return _moduleBuilder.DefineInitializedData(name, bytes, FieldAttributes.Public | FieldAttributes.Static);
                });
                il.Emit(OpCodes.Ldsfld, field);
                return LoadState.Struct;
            }
            else
            {
                return base.LoadBoolean(il, b);
            }
        }

        public override LoadState LoadNull(ILGenerator il)
        {
            if (IsConfused)
            {
                var id = "Null";
                var field = consts.GetOrAdd(id, (key) =>
                {
                    var bytes = new Byte[24];
                    var name = ConfuseTypeName(id, ConfuseTarget.Constant);
                    return _moduleBuilder.DefineInitializedData(name, bytes, FieldAttributes.Public | FieldAttributes.Static);
                });
                il.Emit(OpCodes.Ldsfld, field);
                return LoadState.Struct;
            }
            else
            {
                return base.LoadNull(il);
            }
        }


        public override LoadState LoadString(ILGenerator il, String value)
        {
            //if (IsConfused)
            //{
            //    if (value != null && value.Length >0)
            //    {
            //        var id = "S" + value.GetHashCode();
            //        var field = consts.GetOrAdd(id, (key) =>
            //        {
            //            var bytes = Encoding.UTF8.GetBytes(value);
            //            strLengths[value] = bytes.Length;
            //            var name = ConfuseTypeName(id, ConfuseTarget.Constant);
            //            return _moduleBuilder.DefineInitializedData(name, bytes, FieldAttributes.Public | FieldAttributes.Static);
            //        });
            //        il.Emit(OpCodes.Ldsflda, field);
            //        //il.Emit(OpCodes.Conv_I);
            //        il.Emit(OpCodes.Ldc_I4_0);
            //        var len = strLengths[value];
            //        il.Emit(OpCodes.Ldc_I4, len);
            //        il.Emit(OpCodes.Call, typeof(String).GetConstructor([typeof(char).MakePointerType(), typeof(int), typeof(int)]));
            //    }
            //    else
            //    {
            //        il.Emit(OpCodes.Ldsfld, typeof(String).GetField("Empty", BindingFlags.Static | BindingFlags.Public));
            //    }
            //        //new String((char*)0, 1, 2);
            //        return LoadState.Constant;
            //}
            //else
            {
                return base.LoadString(il, value);
            }
        }

        public override LoadState LoadStringConstant(ILGenerator il, String value)
        {
            il.Emit(OpCodes.Ldstr, value);
            return LoadState.Constant;
        }

        public override (MethodInfo Method, ILGenerator IL) DefineDynamicMethod(ModuleDeclaration module)
        {
            throw new NotImplementedException();
        }

        private ConcurrentDictionary<String, int> strLengths = new ConcurrentDictionary<string, int>();
        private ConcurrentDictionary<String, FieldBuilder> consts = new ConcurrentDictionary<string, FieldBuilder>();

    }
#else
    internal class PersistedBuilder : AbstractCILBuilder
    {
        public PersistedBuilder(EngineOptions options) : base(options)
        {
            throw new PlatformNotSupportedException("CompilationMode.Persistence requires .NET 9.0 or later.");
        }

        public byte[] Serialize()
        {
            throw new PlatformNotSupportedException("CompilationMode.Persistence requires .NET 9.0 or later.");
        }

        public override (MethodInfo Method, ILGenerator IL) DefineModuleInitMethod(ModuleDeclaration module)
        {
            throw new PlatformNotSupportedException("CompilationMode.Persistence requires .NET 9.0 or later.");
        }

        public override (MethodInfo Method, ILGenerator IL) DefineDomainInitMethod()
        {
            throw new PlatformNotSupportedException("CompilationMode.Persistence requires .NET 9.0 or later.");
        }

        public override (MethodInfo Method, ILGenerator IL) DefineMethod(string moduleName, string methodName, Type returnType, Type[] parameterTypes)
        {
            throw new PlatformNotSupportedException("CompilationMode.Persistence requires .NET 9.0 or later.");
        }

        public override MethodInfo GetRuntimeEntryPoint() => null;

        public override void SetLocalSymInfo(LocalBuilder local, string name)
        {
        }

        public override void MarkSequencePoint(AstNode node, ILGenerator il)
        {
        }

        public override void MarkSequencePoint(SourceSpan range, ILGenerator il)
        {
        }

        public override (MethodInfo Method, ILGenerator IL) DefineDynamicMethod(ModuleDeclaration module)
        {
            throw new PlatformNotSupportedException("CompilationMode.Persistence requires .NET 9.0 or later.");
        }
    }
#endif
}
