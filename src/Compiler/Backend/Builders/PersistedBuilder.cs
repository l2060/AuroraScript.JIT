using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;


namespace AuroraScript.Compiler.Backend.Builders
{
#if NET9_0_OR_GREATER
    internal sealed class PersistedBuilder : AbstractCILBuilder
    {
        private static readonly Guid AuroraScriptLanguageId = new Guid("72B5C67C-4C8F-4A17-93A4-35C34487374D");
        private static readonly Guid MicrosoftVendorId = new Guid("994b45c4-e6e9-11d2-903f-00c04fa302a1");
        private static readonly Guid TextDocumentType = new Guid("5a869d0b-6611-11d3-bd2a-0000f80849bd");

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
            var typeBuilder = _moduleBuilder.DefineType(ConfuseTypeName(module.ModuleName, ConfuseTarget.Class), TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
            var methodBuilder = typeBuilder.DefineMethod(ConfuseTypeName("Initialize", ConfuseTarget.Method), MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, typeof(void), [typeof(ScriptContext), typeof(Span<ScriptDatum>)]);
            ISymbolDocumentWriter symbolDoc = null;
            symbolDoc = _moduleBuilder.DefineDocument(module.FullPath, AuroraScriptLanguageId, MicrosoftVendorId, TextDocumentType);
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

        public override (MethodInfo Method, ILGenerator IL) DefineMethod(
            string moduleName,
            string methodName,
            Type returnType,
            Type[] parameterTypes,
            bool aggressiveInlining = false)
        {
            var typeName = moduleName;
            if (!TryResolveType(typeName, out var typeBuilder))
            {
                throw new Exception($"Module {moduleName} not defined");
            }
            var method = typeBuilder.DefineMethod(ConfuseTypeName(methodName, ConfuseTarget.Method), MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);
            if (aggressiveInlining)
            {
                method.SetImplementationFlags(
                    MethodImplAttributes.IL |
                    MethodImplAttributes.Managed |
                    MethodImplAttributes.AggressiveInlining);
            }

            return (method, method.GetILGenerator());
        }

        public override void SetDebuggerMetadata(MethodInfo method, string metadata)
        {
            if (!IsDebugMode || method is not MethodBuilder methodBuilder || string.IsNullOrEmpty(metadata))
            {
                return;
            }

            var constructor = typeof(Runtime.Debugging.ScriptDebuggerMetadataAttribute).GetConstructor([typeof(string)]);
            methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructor, [metadata]));
        }


        public byte[] Serialize()
        {
            var builder = _assemblyBuilder;
            var metadataBuilder = builder.GenerateMetadata(out BlobBuilder ilBuilder, out var mappedFieldData, out var pdbMetadataBuilder);

            DebugDirectoryBuilder debugDirectoryBuilder = null;
            BlobBuilder pdbBlob = null;
            BlobContentId pdbContentId = default;

            if (IsDebugMode)
            {
                var rowCounts = pdbMetadataBuilder.GetRowCounts();
                var typeSystemRowCounts = rowCounts.Take(MetadataTokens.TableCount).ToArray();
                EnsureValidPortablePdbRowCounts(typeSystemRowCounts);
                var pdbBuilder = new PortablePdbBuilder(pdbMetadataBuilder, typeSystemRowCounts.ToImmutableArray(), default);
                pdbBlob = new BlobBuilder();
                pdbContentId = pdbBuilder.Serialize(pdbBlob);
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
                imageCharacteristics: Characteristics.ExecutableImage |
                    Characteristics.LargeAddressAware |
                    Characteristics.Dll,
                sizeOfStackReserve: 0x00100000,
                sizeOfStackCommit: 0x00001000,
                sizeOfHeapReserve: 0x00100000,
                sizeOfHeapCommit: 0x00001000);

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



        private static void EnsureValidPortablePdbRowCounts(int[] typeSystemRowCounts)
        {
            const ulong ValidPortablePdbExternalTables = 0x0000041007F3D857;

            for (int i = 0; i < typeSystemRowCounts.Length; i++)
            {
                if (((1UL << i) & ValidPortablePdbExternalTables) == 0)
                {
                    typeSystemRowCounts[i] = 0;
                }
                else
                {
                    if (typeSystemRowCounts[i] < 0)
                    {
                        typeSystemRowCounts[i] = 0;
                    }
                }
            }

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
    }
#else
    internal sealed class PersistedBuilder : AbstractCILBuilder
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

        public override (MethodInfo Method, ILGenerator IL) DefineMethod(
            string moduleName,
            string methodName,
            Type returnType,
            Type[] parameterTypes,
            bool aggressiveInlining = false)
        {
            throw new PlatformNotSupportedException("CompilationMode.Persistence requires .NET 9.0 or later.");
        }

        public override MethodInfo GetRuntimeEntryPoint() => null;

    }
#endif
}
