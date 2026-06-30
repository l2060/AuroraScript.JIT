using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace AuroraScript.VisualStudio.Debugger;

public sealed class AuroraExpressionCompiler : IDkmClrExpressionCompiler
{
    private static uint nextClassId;

    void IDkmClrExpressionCompiler.CompileExpression(
        DkmLanguageExpression expression,
        DkmClrInstructionAddress instructionAddress,
        DkmInspectionContext inspectionContext,
        out string error,
        out DkmCompiledClrInspectionQuery result)
    {
        error = null!;
        result = null!;

        if (expression == null)
        {
            error = "AuroraScript debugger expression is empty.";
            return;
        }

        var text = NormalizeExpression(expression.Text);
        if (!AuroraExpressionName.TryParse(text, out var name))
        {
            error = "Only AuroraScript identifier expressions are supported by the debugger evaluator.";
            return;
        }

        try
        {
            var scope = AuroraInspectionScope.Create(instructionAddress);
            var query = AuroraInspectionQuery.CreateExpression(scope, name);
            result = DkmCompiledClrInspectionQuery.Create(
                instructionAddress.RuntimeInstance,
                null,
                expression.Language!.Id,
                new ReadOnlyCollection<byte>(query.PeBytes),
                query.ClassName,
                query.MethodName,
                new ReadOnlyCollection<string>(Array.Empty<string>()),
                DkmClrCompilationResultFlags.ReadOnlyResult,
                DkmEvaluationResultCategory.Data,
                DkmEvaluationResultAccessType.None,
                DkmEvaluationResultStorageType.None,
                DkmEvaluationResultTypeModifierFlags.None,
                null);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
    }

    DkmCompiledClrLocalsQuery IDkmClrExpressionCompiler.GetClrLocalVariableQuery(
        DkmInspectionContext inspectionContext,
        DkmClrInstructionAddress instructionAddress,
        bool argumentsOnly)
    {
        var scope = AuroraInspectionScope.Create(instructionAddress);
        var query = AuroraInspectionQuery.CreateLocals(scope, argumentsOnly);
        return DkmCompiledClrLocalsQuery.Create(
            inspectionContext.RuntimeInstance,
            null,
            inspectionContext.Language!.Id,
            new ReadOnlyCollection<byte>(query.PeBytes),
            query.ClassName,
            new ReadOnlyCollection<DkmClrLocalVariableInfo>(query.Locals));
    }

    void IDkmClrExpressionCompiler.CompileAssignment(
        DkmLanguageExpression expression,
        DkmClrInstructionAddress instructionAddress,
        DkmEvaluationResult lValue,
        out string error,
        out DkmCompiledClrInspectionQuery result)
    {
        error = "AuroraScript debugger assignment is not supported.";
        result = null!;
    }

    internal static string NextClassName()
    {
        return "AuroraScript_DebugQuery_" + nextClassId++;
    }

    private static string NormalizeExpression(string? text)
    {
        var comma = text?.IndexOf(',');
        return comma.GetValueOrDefault(-1) >= 0 ? text!.Substring(0, comma.GetValueOrDefault()).Trim() : text?.Trim() ?? string.Empty;
    }
}

internal sealed class AuroraInspectionScope
{
    private AuroraInspectionScope(
        DkmClrInstructionAddress address,
        AuroraDebuggerMethodMetadata metadata,
        IReadOnlyList<AuroraClrVariable> parameters,
        IReadOnlyList<AuroraClrVariable> locals)
    {
        Address = address;
        Metadata = metadata;
        Parameters = parameters;
        Locals = locals;
    }

    public DkmClrInstructionAddress Address { get; }

    public AuroraDebuggerMethodMetadata Metadata { get; }

    public IReadOnlyList<AuroraClrVariable> Parameters { get; }

    public IReadOnlyList<AuroraClrVariable> Locals { get; }

    public static AuroraInspectionScope Create(DkmClrInstructionAddress address)
    {
        var metadata = AuroraDebuggerMethodMetadata.Read(address);
        return new AuroraInspectionScope(address, metadata, GetParameters(metadata), GetLocals(address, metadata));
    }

    public AuroraDebuggerVariable? FindScriptVariable(string name)
    {
        return Metadata.Find(name);
    }

    public AuroraClrVariable? FindClrVariable(string name)
    {
        for (var i = 0; i < Locals.Count; i++)
        {
            if (string.Equals(Locals[i].Name, name, StringComparison.Ordinal))
            {
                return Locals[i];
            }
        }

        for (var i = 0; i < Parameters.Count; i++)
        {
            if (string.Equals(Parameters[i].Name, name, StringComparison.Ordinal))
            {
                return Parameters[i];
            }
        }

        return null;
    }

    public AuroraClrVariable? ContextParameter => Parameters.Count > 0 ? Parameters[0] : null;

    private static IReadOnlyList<AuroraClrVariable> GetParameters(AuroraDebuggerMethodMetadata metadata)
    {
        var result = new List<AuroraClrVariable>();
        result.Add(AuroraClrVariable.Parameter("ctx", 0, "class [AuroraScript]AuroraScript.Runtime.ScriptContext"));
        if (metadata.CallConvention == AuroraDebuggerCallConvention.Span)
        {
            result.Add(AuroraClrVariable.Parameter("args", 1, "valuetype [System.Runtime]System.Span`1<valuetype [AuroraScript]AuroraScript.Runtime.ScriptDatum>"));
            return result;
        }

        for (var i = 0; i < metadata.Arity; i++)
        {
            result.Add(AuroraClrVariable.Parameter("arg" + i, i + 1, AuroraClrVariable.ScriptDatumTypeName));
        }

        return result;
    }

    private static IReadOnlyList<AuroraClrVariable> GetLocals(DkmClrInstructionAddress address, AuroraDebuggerMethodMetadata metadata)
    {
        var result = new Dictionary<int, AuroraClrVariable>();
        foreach (var variable in metadata.Variables)
        {
            if (variable.Kind == AuroraDebuggerVariableKind.Local ||
                (variable.Kind == AuroraDebuggerVariableKind.Parameter && variable.Slot >= 0 && !variable.DirectParameter))
            {
                result[variable.Slot] = AuroraClrVariable.Local(variable.Name, variable.Slot, AuroraClrVariable.ScriptDatumTypeName);
            }
        }

        foreach (var variable in metadata.Variables)
        {
            if (variable.Kind == AuroraDebuggerVariableKind.CapturedLocal && variable.LocalSlot >= 0)
            {
                result[variable.LocalSlot] = AuroraClrVariable.Local("$capturedUpvalues", variable.LocalSlot, "object[]");
            }
        }

        try
        {
            var scopes = address.ModuleInstance.Module.GetMethodSymbolStoreData(address.MethodId);
            foreach (var scope in scopes)
            {
                if (scope.ILRange.StartOffset > address.ILOffset || scope.ILRange.EndOffset < address.ILOffset)
                {
                    continue;
                }

                foreach (var local in scope.LocalVariables)
                {
                    if (string.IsNullOrEmpty(local.Name))
                    {
                        continue;
                    }

                    if (!result.ContainsKey(local.Slot))
                    {
                        result.Add(local.Slot, AuroraClrVariable.Local(local.Name, local.Slot, AuroraClrVariable.ScriptDatumTypeName));
                    }
                }
            }
        }
        catch
        {
        }

        return result.Values.OrderBy(variable => variable.Slot).ToArray();
    }
}

internal sealed class AuroraClrVariable
{
    public const string ScriptDatumTypeName = "valuetype [AuroraScript]AuroraScript.Runtime.ScriptDatum";
    public const string ScriptDatumArrayTypeName = "valuetype [AuroraScript]AuroraScript.Runtime.ScriptDatum[]";

    private AuroraClrVariable(string name, int slot, bool isParameter, string cilTypeName)
    {
        Name = name;
        Slot = slot;
        IsParameter = isParameter;
        CilTypeName = cilTypeName;
    }

    public string Name { get; }

    public int Slot { get; }

    public bool IsParameter { get; }

    public string CilTypeName { get; }

    public static AuroraClrVariable Parameter(string name, int slot, string cilTypeName)
    {
        return new AuroraClrVariable(name, slot, isParameter: true, cilTypeName);
    }

    public static AuroraClrVariable Local(string name, int slot, string cilTypeName)
    {
        return new AuroraClrVariable(name, slot, isParameter: false, cilTypeName);
    }
}

internal enum AuroraDebuggerCallConvention
{
    Span,
    Fast
}

internal enum AuroraDebuggerVariableKind
{
    Parameter,
    Local,
    Upvalue,
    CapturedLocal,
    Module
}

internal readonly struct AuroraDebuggerVariable
{
    public AuroraDebuggerVariable(
        AuroraDebuggerVariableKind kind,
        string name,
        int slot,
        int parameterIndex = -1,
        bool directParameter = false,
        int localSlot = -1)
    {
        Kind = kind;
        Name = name;
        Slot = slot;
        ParameterIndex = parameterIndex;
        DirectParameter = directParameter;
        LocalSlot = localSlot;
    }

    public AuroraDebuggerVariableKind Kind { get; }

    public string Name { get; }

    public int Slot { get; }

    public int ParameterIndex { get; }

    public bool DirectParameter { get; }

    public int LocalSlot { get; }
}

internal sealed class AuroraDebuggerMethodMetadata
{
    private const string AttributeName = "AuroraScript.Runtime.Debugging.ScriptDebuggerMetadataAttribute";

    private AuroraDebuggerMethodMetadata()
    {
    }

    public AuroraDebuggerCallConvention CallConvention { get; private set; } = AuroraDebuggerCallConvention.Span;

    public int Arity { get; private set; }

    public IReadOnlyList<AuroraDebuggerVariable> Variables { get; private set; } = Array.Empty<AuroraDebuggerVariable>();

    public AuroraDebuggerVariable? Find(string name)
    {
        for (var i = 0; i < Variables.Count; i++)
        {
            var variable = Variables[i];
            if (string.Equals(variable.Name, name, StringComparison.Ordinal))
            {
                return variable;
            }
        }

        return null;
    }

    public static AuroraDebuggerMethodMetadata Read(DkmClrInstructionAddress address)
    {
        try
        {
            var bytes = address.ModuleInstance.GetMetaDataBytes();
            using var provider = MetadataReaderProvider.FromMetadataImage(bytes.ToImmutableArray());
            var reader = provider.GetMetadataReader();
            var handle = (MethodDefinitionHandle)MetadataTokens.Handle(address.MethodId.Token);
            if (handle.IsNil)
            {
                return new AuroraDebuggerMethodMetadata();
            }

            var method = reader.GetMethodDefinition(handle);
            foreach (var attributeHandle in method.GetCustomAttributes())
            {
                var attribute = reader.GetCustomAttribute(attributeHandle);
                if (!IsDebuggerMetadataAttribute(reader, attribute))
                {
                    continue;
                }

                var value = attribute.DecodeValue(new StringOnlyAttributeTypeProvider()).FixedArguments;
                if (value.Length == 1 && value[0].Value is string metadata)
                {
                    return Parse(metadata);
                }
            }
        }
        catch
        {
        }

        return new AuroraDebuggerMethodMetadata();
    }

    private static bool IsDebuggerMetadataAttribute(MetadataReader reader, CustomAttribute attribute)
    {
        EntityHandle constructorType;
        if (attribute.Constructor.Kind == HandleKind.MemberReference)
        {
            constructorType = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
        }
        else if (attribute.Constructor.Kind == HandleKind.MethodDefinition)
        {
            constructorType = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
        }
        else
        {
            return false;
        }

        if (constructorType.Kind != HandleKind.TypeReference)
        {
            return false;
        }

        var type = reader.GetTypeReference((TypeReferenceHandle)constructorType);
        var ns = reader.GetString(type.Namespace);
        var name = reader.GetString(type.Name);
        return string.Equals(ns + "." + name, AttributeName, StringComparison.Ordinal);
    }

    private static AuroraDebuggerMethodMetadata Parse(string text)
    {
        var result = new AuroraDebuggerMethodMetadata();
        var variables = new List<AuroraDebuggerVariable>();
        foreach (var part in SplitEscaped(text, ';'))
        {
            if (part.Length == 0 || string.Equals(part, "v=1", StringComparison.Ordinal))
            {
                continue;
            }

            if (part.StartsWith("cc=", StringComparison.Ordinal))
            {
                result.CallConvention = string.Equals(part.Substring(3), "fast", StringComparison.Ordinal)
                    ? AuroraDebuggerCallConvention.Fast
                    : AuroraDebuggerCallConvention.Span;
                continue;
            }

            if (part.StartsWith("arity=", StringComparison.Ordinal))
            {
                int.TryParse(part.Substring(6), out var arity);
                result.Arity = arity;
                continue;
            }

            var fields = SplitEscaped(part, ':');
            if (fields.Length < 2)
            {
                continue;
            }

            var kind = fields[0] switch
            {
                "p" => AuroraDebuggerVariableKind.Parameter,
                "l" => AuroraDebuggerVariableKind.Local,
                "u" => AuroraDebuggerVariableKind.Upvalue,
                "c" => AuroraDebuggerVariableKind.CapturedLocal,
                "m" => AuroraDebuggerVariableKind.Module,
                _ => (AuroraDebuggerVariableKind?)null
            };

            if (kind == null)
            {
                continue;
            }

            var slot = -1;
            var parameterIndex = -1;
            var directParameter = false;
            var localSlot = -1;
            if (fields.Length > 2)
            {
                int.TryParse(fields[2], out slot);
            }
            if (kind == AuroraDebuggerVariableKind.Parameter)
            {
                if (fields.Length > 3)
                {
                    int.TryParse(fields[3], out parameterIndex);
                }

                directParameter = fields.Length > 4 && string.Equals(fields[4], "1", StringComparison.Ordinal);
            }
            else if (kind == AuroraDebuggerVariableKind.CapturedLocal && fields.Length > 3)
            {
                int.TryParse(fields[3], out localSlot);
            }

            variables.Add(new AuroraDebuggerVariable(kind.Value, fields[1], slot, parameterIndex, directParameter, localSlot));
        }

        result.Variables = variables;
        return result;
    }

    private static string[] SplitEscaped(string text, char separator)
    {
        var result = new List<string>();
        var builder = new StringBuilder();
        var escaped = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (escaped)
            {
                builder.Append(ch);
                escaped = false;
                continue;
            }

            if (ch == '\\')
            {
                escaped = true;
                continue;
            }

            if (ch == separator)
            {
                result.Add(builder.ToString());
                builder.Clear();
                continue;
            }

            builder.Append(ch);
        }

        result.Add(builder.ToString());
        return result.ToArray();
    }
}

internal sealed class StringOnlyAttributeTypeProvider : ICustomAttributeTypeProvider<string>
{
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();

    public string GetSystemType() => "System.Type";

    public string GetSZArrayType(string elementType) => elementType + "[]";

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var type = reader.GetTypeDefinition(handle);
        return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
    }

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var type = reader.GetTypeReference(handle);
        return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
    }

    public string GetTypeFromSerializedName(string name) => name;

    public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;

    public bool IsSystemType(string type) => string.Equals(type, "System.Type", StringComparison.Ordinal);
}

internal sealed class AuroraInspectionQuery
{
    private AuroraInspectionQuery(
        byte[] peBytes,
        string className,
        string? methodName,
        List<DkmClrLocalVariableInfo>? locals)
    {
        PeBytes = peBytes;
        ClassName = className;
        MethodName = methodName;
        Locals = locals ?? new List<DkmClrLocalVariableInfo>();
    }

    public byte[] PeBytes { get; }

    public string ClassName { get; }

    public string? MethodName { get; }

    public List<DkmClrLocalVariableInfo> Locals { get; }

    public static AuroraInspectionQuery CreateExpression(AuroraInspectionScope scope, string name)
    {
        var className = AuroraExpressionCompiler.NextClassName();
        const string methodName = "M0";
        using var builder = AuroraQueryAssemblyBuilder.Create(className);
        builder.EmitMethod(methodName, scope, il => EmitExpression(il, scope, name));
        return new AuroraInspectionQuery(builder.ToPeBytes(), className, methodName, null);
    }

    public static AuroraInspectionQuery CreateLocals(AuroraInspectionScope scope, bool argumentsOnly)
    {
        var className = AuroraExpressionCompiler.NextClassName();
        using var builder = AuroraQueryAssemblyBuilder.Create(className);
        var locals = new List<DkmClrLocalVariableInfo>();
        var methodIndex = 0;

        if (argumentsOnly)
        {
            AddLocal(builder, locals, scope, "$args", ref methodIndex);
        }
        else
        {
            AddLocal(builder, locals, scope, "global", ref methodIndex);
            AddLocal(builder, locals, scope, "$state", ref methodIndex);
            AddLocal(builder, locals, scope, "$args", ref methodIndex);
        }

        foreach (var variable in scope.Parameters)
        {
            if (variable.Slot == 0 ||
                !string.Equals(variable.CilTypeName, AuroraClrVariable.ScriptDatumTypeName, StringComparison.Ordinal))
            {
                continue;
            }

            AddLocal(builder, locals, scope, variable.Name, ref methodIndex);
        }

        if (!argumentsOnly)
        {
            foreach (var variable in scope.Locals)
            {
                AddLocal(builder, locals, scope, variable.Name, ref methodIndex);
            }
        }

        return new AuroraInspectionQuery(builder.ToPeBytes(), className, null, locals);
    }

    private static void AddLocal(
        AuroraQueryAssemblyBuilder builder,
        List<DkmClrLocalVariableInfo> locals,
        AuroraInspectionScope scope,
        string name,
        ref int methodIndex)
    {
        var methodName = "M" + methodIndex++;
        builder.EmitMethod(methodName, scope, il => EmitExpression(il, scope, name));
        locals.Add(DkmClrLocalVariableInfo.Create(
            name,
            name,
            methodName,
            DkmClrCompilationResultFlags.ReadOnlyResult,
            DkmEvaluationResultCategory.Data,
            null!));
    }

    private static void EmitExpression(AuroraQueryIlBuilder il, AuroraInspectionScope scope, string name)
    {
        var scriptVariable = scope.FindScriptVariable(name);
        if (scriptVariable != null)
        {
            il.LoadScriptVariable(scope, scriptVariable.Value);
            return;
        }

        var variable = scope.FindClrVariable(name);
        if (variable != null &&
            string.Equals(variable.CilTypeName, AuroraClrVariable.ScriptDatumTypeName, StringComparison.Ordinal))
        {
            il.LoadVariable(variable);
            return;
        }

        var specialName = AuroraExpressionName.GetSpecialName(name);
        if (specialName != null)
        {
            il.LoadContext(scope);
            il.LoadString(specialName);
            il.LoadPackedArguments(scope);
            il.Call(
                "AuroraScript.Runtime.Debugging.ScriptDebuggerExpressionEvaluator::GetSpecial",
                "class [AuroraScript]AuroraScript.Runtime.ScriptContext, string, " + AuroraClrVariable.ScriptDatumArrayTypeName);
            return;
        }

        il.LoadContext(scope);
        il.LoadString(name);
        il.Call("AuroraScript.Runtime.Debugging.ScriptDebuggerExpressionEvaluator::GetModuleProperty");
    }
}

internal sealed class AuroraQueryAssemblyBuilder : IDisposable
{
    private readonly string assemblyName;
    private readonly string ilPath;
    private readonly string outputPath;
    private readonly StreamWriter writer;
    private bool completed;

    private AuroraQueryAssemblyBuilder(string assemblyName, string ilPath, string outputPath, StreamWriter writer)
    {
        this.assemblyName = assemblyName;
        this.ilPath = ilPath;
        this.outputPath = outputPath;
        this.writer = writer;
    }

    public static AuroraQueryAssemblyBuilder Create(string className)
    {
        var basePath = Path.Combine(Path.GetTempPath(), className + "_" + Guid.NewGuid().ToString("N"));
        var ilPath = basePath + ".il";
        var outputPath = basePath + ".dll";
        var writer = new StreamWriter(File.Open(ilPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read), Encoding.UTF8);
        var builder = new AuroraQueryAssemblyBuilder(className, ilPath, outputPath, writer);
        builder.Begin();
        return builder;
    }

    public void EmitMethod(string methodName, AuroraInspectionScope scope, Action<AuroraQueryIlBuilder> emitBody)
    {
        writer.Write("  .method public hidebysig static valuetype [AuroraScript]AuroraScript.Runtime.ScriptDatum ");
        writer.Write(methodName);
        writer.Write('(');
        for (var i = 0; i < scope.Parameters.Count; i++)
        {
            if (i > 0)
            {
                writer.Write(", ");
            }

            writer.Write(scope.Parameters[i].CilTypeName);
            writer.Write(' ');
            writer.Write(SanitizeName(scope.Parameters[i].Name));
        }

        writer.WriteLine(") cil managed");
        writer.WriteLine("  {");
        writer.WriteLine("    .maxstack 8");
        WriteLocals(scope);
        var il = new AuroraQueryIlBuilder(writer);
        emitBody(il);
        writer.WriteLine("    ret");
        writer.WriteLine("  }");
    }

    public byte[] ToPeBytes()
    {
        if (!completed)
        {
            completed = true;
            writer.WriteLine("}");
            writer.Dispose();
            RunIlasm();
        }

        return File.ReadAllBytes(outputPath);
    }

    public void Dispose()
    {
        writer.Dispose();
        TryDelete(ilPath);
        TryDelete(outputPath);
    }

    private void Begin()
    {
        writer.WriteLine(".assembly extern mscorlib { }");
        writer.WriteLine(".assembly extern System.Runtime { }");
        writer.WriteLine(".assembly extern AuroraScript { }");
        writer.Write(".assembly ");
        writer.Write(assemblyName);
        writer.WriteLine(" { }");
        writer.Write(".class public auto ansi beforefieldinit ");
        writer.Write(assemblyName);
        writer.WriteLine(" extends [mscorlib]System.Object");
        writer.WriteLine("{");
    }

    private void RunIlasm()
    {
        var ilasm = Path.Combine(Path.GetDirectoryName(typeof(AuroraQueryAssemblyBuilder).Assembly.Location) ?? string.Empty, "ilasm.exe");
        if (!File.Exists(ilasm))
        {
            ilasm = Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "ilasm.exe");
        }

        if (!File.Exists(ilasm))
        {
            throw new FileNotFoundException("ilasm.exe was not found in the AuroraScript VSIX payload.", ilasm);
        }

        using var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = ilasm;
        process.StartInfo.WorkingDirectory = Path.GetDirectoryName(outputPath);
        process.StartInfo.Arguments = "\"" + ilPath + "\" -QUIET -DLL -OUTPUT=\"" + outputPath + "\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("AuroraScript debugger query compilation failed: " + process.StandardError.ReadToEnd());
        }
    }

    private void WriteLocals(AuroraInspectionScope scope)
    {
        if (scope.Locals.Count == 0)
        {
            return;
        }

        var bySlot = new Dictionary<int, AuroraClrVariable>();
        var maxSlot = -1;
        for (var i = 0; i < scope.Locals.Count; i++)
        {
            var local = scope.Locals[i];
            bySlot[local.Slot] = local;
            if (local.Slot > maxSlot)
            {
                maxSlot = local.Slot;
            }
        }

        writer.Write("    .locals init (");
        for (var slot = 0; slot <= maxSlot; slot++)
        {
            if (slot > 0)
            {
                writer.Write(", ");
            }

            writer.Write('[');
            writer.Write(slot);
            writer.Write("] ");
            writer.Write(bySlot.TryGetValue(slot, out var local) ? local.CilTypeName : AuroraClrVariable.ScriptDatumTypeName);
        }

        writer.WriteLine(")");
    }

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "p";
        }

        var builder = new StringBuilder(name.Length);
        for (var i = 0; i < name.Length; i++)
        {
            var ch = name[i];
            builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
        }

        return builder.ToString();
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}

internal sealed class AuroraQueryIlBuilder
{
    private readonly TextWriter writer;

    public AuroraQueryIlBuilder(TextWriter writer)
    {
        this.writer = writer;
    }

    public void LoadVariable(AuroraClrVariable variable)
    {
        WriteIndexed(variable.IsParameter ? "ldarg" : "ldloc", variable.Slot);
    }

    public void LoadScriptVariable(AuroraInspectionScope scope, AuroraDebuggerVariable variable)
    {
        switch (variable.Kind)
        {
            case AuroraDebuggerVariableKind.Parameter:
                LoadScriptParameter(scope, variable);
                return;
            case AuroraDebuggerVariableKind.Local:
                WriteIndexed("ldloc", variable.Slot);
                return;
            case AuroraDebuggerVariableKind.Upvalue:
                LoadContext(scope);
                LoadInt(variable.Slot);
                Call("AuroraScript.Runtime.Debugging.ScriptDebuggerExpressionEvaluator::GetUpvalue", "class [AuroraScript]AuroraScript.Runtime.ScriptContext, int32");
                return;
            case AuroraDebuggerVariableKind.CapturedLocal:
                WriteIndexed("ldloc", variable.LocalSlot);
                LoadInt(variable.Slot);
                Call("AuroraScript.Runtime.Debugging.ScriptDebuggerExpressionEvaluator::GetCapturedLocal", "object, int32");
                return;
            case AuroraDebuggerVariableKind.Module:
                LoadContext(scope);
                LoadString(variable.Name);
                Call("AuroraScript.Runtime.Debugging.ScriptDebuggerExpressionEvaluator::GetModuleProperty", "class [AuroraScript]AuroraScript.Runtime.ScriptContext, string");
                return;
            default:
                throw new InvalidOperationException("Unsupported AuroraScript debugger variable kind.");
        }
    }

    public void LoadContext(AuroraInspectionScope scope)
    {
        if (scope.ContextParameter == null)
        {
            throw new InvalidOperationException("Current frame does not expose an AuroraScript ScriptContext.");
        }

        LoadVariable(scope.ContextParameter);
    }

    public void LoadPackedArguments(AuroraInspectionScope scope)
    {
        if (scope.Metadata.CallConvention == AuroraDebuggerCallConvention.Span)
        {
            var argsParameter = scope.Parameters.FirstOrDefault(parameter => parameter.Slot == 1);
            if (argsParameter == null)
            {
                writer.WriteLine("    call valuetype [AuroraScript]AuroraScript.Runtime.ScriptDatum[] [AuroraScript]AuroraScript.Runtime.Debugging.ScriptDebuggerExpressionEvaluator::PackArguments()");
                return;
            }

            LoadVariable(argsParameter);
            writer.WriteLine("    call valuetype [AuroraScript]AuroraScript.Runtime.ScriptDatum[] [AuroraScript]AuroraScript.Runtime.Debugging.ScriptDebuggerExpressionEvaluator::PackSpanArguments(valuetype [System.Runtime]System.Span`1<valuetype [AuroraScript]AuroraScript.Runtime.ScriptDatum>)");
            return;
        }

        var args = scope.Parameters.Where(parameter => parameter.Slot > 0).Take(7).ToArray();
        foreach (var arg in args)
        {
            LoadVariable(arg);
        }

        writer.Write("    call valuetype [AuroraScript]AuroraScript.Runtime.ScriptDatum[] [AuroraScript]AuroraScript.Runtime.Debugging.ScriptDebuggerExpressionEvaluator::PackArguments(");
        for (var i = 0; i < args.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(", ");
            }

            writer.Write("valuetype [AuroraScript]AuroraScript.Runtime.ScriptDatum");
        }

        writer.WriteLine(")");
    }

    public void LoadString(string value)
    {
        writer.Write("    ldstr ");
        writer.WriteLine(Quote(value));
    }

    public void Call(string method)
    {
        Call(method, "class [AuroraScript]AuroraScript.Runtime.ScriptContext, string");
    }

    public void Call(string method, string parameters)
    {
        writer.Write("    call valuetype [AuroraScript]AuroraScript.Runtime.ScriptDatum [AuroraScript]");
        writer.Write(method);
        writer.Write('(');
        writer.Write(parameters);
        writer.WriteLine(")");
    }

    private void LoadScriptParameter(AuroraInspectionScope scope, AuroraDebuggerVariable variable)
    {
        if (variable.DirectParameter)
        {
            WriteIndexed("ldarg", variable.ParameterIndex + 1);
            return;
        }

        WriteIndexed("ldloc", variable.Slot);
    }

    private void LoadInt(int value)
    {
        switch (value)
        {
            case 0:
                writer.WriteLine("    ldc.i4.0");
                return;
            case 1:
                writer.WriteLine("    ldc.i4.1");
                return;
            case 2:
                writer.WriteLine("    ldc.i4.2");
                return;
            case 3:
                writer.WriteLine("    ldc.i4.3");
                return;
            case 4:
                writer.WriteLine("    ldc.i4.4");
                return;
            case 5:
                writer.WriteLine("    ldc.i4.5");
                return;
            case 6:
                writer.WriteLine("    ldc.i4.6");
                return;
            case 7:
                writer.WriteLine("    ldc.i4.7");
                return;
            case 8:
                writer.WriteLine("    ldc.i4.8");
                return;
            default:
                writer.Write("    ldc.i4 ");
                writer.WriteLine(value);
                return;
        }
    }

    private void WriteIndexed(string op, int index)
    {
        if (index >= 0 && index <= 3)
        {
            writer.Write("    ");
            writer.Write(op);
            writer.Write('.');
            writer.WriteLine(index);
            return;
        }

        writer.Write("    ");
        writer.Write(op);
        writer.Write(index <= byte.MaxValue ? ".s " : " ");
        writer.WriteLine(index);
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}

internal static class AuroraExpressionName
{
    public static bool TryParse(string text, out string name)
    {
        name = text;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (IsSpecial(name))
        {
            return true;
        }

        if (!IsIdentifierStart(name[0]))
        {
            return false;
        }

        for (var i = 1; i < name.Length; i++)
        {
            if (!IsIdentifierPart(name[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsSpecial(string name)
    {
        return string.Equals(name, "global", StringComparison.Ordinal) ||
            string.Equals(name, "$state", StringComparison.Ordinal) ||
            string.Equals(name, "$args", StringComparison.Ordinal);
    }

    public static string? GetSpecialName(string name)
    {
        if (IsSpecial(name))
        {
            return name;
        }

        if (string.Equals(name, "state", StringComparison.Ordinal))
        {
            return "$state";
        }

        return string.Equals(name, "args", StringComparison.Ordinal) ? "$args" : null;
    }

    private static bool IsIdentifierStart(char ch)
    {
        return char.IsLetter(ch) || ch == '_';
    }

    private static bool IsIdentifierPart(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_';
    }
}
