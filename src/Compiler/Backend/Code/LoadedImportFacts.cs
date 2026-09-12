using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Code
{
    // CompileBlock imports are readonly parameters, captured by normal upvalues.
    // These facts live only for compilation; no imported AST or compiler session survives.
    internal static class LoadedImportFacts
    {
        internal static ImportDeclaration Resolve(ModulePlan module, FunctionPlan function, BoundName binding)
        {
            var local = binding.Local;
            var upvalue = binding.Upvalue;
            while (upvalue.IsValid)
            {
                var capture = function.UpvalueSlots[upvalue.Value];
                function = module.Functions[module.GetFunctionIndex(capture.SourceFunction)];
                local = capture.SourceLocal;
                upvalue = capture.SourceUpvalue;
            }
            return local.IsValid && !binding.ModuleSymbol.IsValid
                ? (function.LocalSlots[local.Value].Declaration as ParameterDeclaration)?.LoadedImport
                : null;
        }

        internal static bool TryGetStatic(ImportDeclaration import, string name, out ScriptDatum value)
        {
            value = default;
            return import?.LoadedModule != null &&
                import.LoadedModule.TryGetExport(name, out value, out var readOnly) &&
                (readOnly || import.LoadedModule.IsNativeFunction(name));
        }

        internal static void Record(ImportDeclaration import, string name, ScriptDatum value) =>
            (import.StaticMembers ??= new(StringComparer.Ordinal))[name] = value;

        internal static HostExportDescriptor GetNative(ImportDeclaration import, string name, HostExportCatalog catalog)
        {
            if (!TryGetStatic(import, name, out var value)) return null;
            if (catalog.TryGetPackageTypeName(import.Reference, out var owner) &&
                catalog.TryGetGlobal(owner, name, out var host))
            {
                Record(import, name, value);
                return host;
            }
            if (value.Reference is not ClosureFunction closure) return null;
            var descriptor = catalog.GetLoadedNative(closure);
            if (descriptor != null) Record(import, name, value);
            return descriptor;
        }

        internal static HostExportDescriptor CreateNativeDescriptor(ClosureFunction closure)
        {
            if (closure.NativeEntry is not { } method ||
                !closure.NativeSignatureComplete ||
                !closure.NativeEntryTakesContext)
            {
                return null;
            }
            var parameters = method.GetParameters();
            var kinds = new AuroraExportValueKind[parameters.Length - 1];
            if (!TryGetKind(method.ReturnType, out var resultKind)) return null;
            for (var i = 0; i < kinds.Length; i++)
                if (!TryGetKind(parameters[i + 1].ParameterType, out kinds[i])) return null;
            return new HostExportDescriptor(CreateContextCall(method), resultKind, kinds,
                takesContext: true, takesThisObject: true, runtimeDefaults: closure.NativeDefaults)
            { ImportedNative = true };
        }

        private static bool TryGetKind(Type type, out AuroraExportValueKind kind)
        {
            kind = type == typeof(void) ? AuroraExportValueKind.Void :
                type == typeof(double) ? AuroraExportValueKind.Number :
                type == typeof(int) ? AuroraExportValueKind.Int32 :
                type == typeof(long) ? AuroraExportValueKind.Int64 :
                type == typeof(ulong) ? AuroraExportValueKind.UInt64 :
                type == typeof(bool) ? AuroraExportValueKind.Boolean :
                type == typeof(string) ? AuroraExportValueKind.String :
                type == typeof(ScriptDatum) ? AuroraExportValueKind.Datum :
                typeof(ScriptObject).IsAssignableFrom(type) ? AuroraExportValueKind.Object : (AuroraExportValueKind)255;
            return kind != (AuroraExportValueKind)255;
        }

        // Keep the exception region in a small CLR thunk, so a call embedded in an
        // expression never opens a try/finally with values on its evaluation stack.
        private static MethodInfo CreateContextCall(MethodInfo native)
        {
            var parameters = native.GetParameters();
            var types = new Type[parameters.Length + 1];
            types[0] = typeof(ScriptContext);
            types[1] = typeof(ScriptObject);
            for (var i = 1; i < parameters.Length; i++) types[i + 1] = parameters[i].ParameterType;
            var method = new DynamicMethod(native.Name + "$import", native.ReturnType, types,
                typeof(LoadedImportFacts).Module, skipVisibility: true);
            var il = method.GetILGenerator();
            var frame = il.DeclareLocal(typeof(int));
            var result = native.ReturnType == typeof(void) ? null : il.DeclareLocal(native.ReturnType);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, typeof(ClosureFunction));
            il.Emit(OpCodes.Call, typeof(ScriptContext).GetMethod("EnterClosure", BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(OpCodes.Stloc, frame);
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldarg_0);
            for (var i = 1; i < parameters.Length; i++) il.Emit(OpCodes.Ldarg, i + 1);
            il.Emit(OpCodes.Call, native);
            if (result != null) il.Emit(OpCodes.Stloc, result);
            il.BeginCatchBlock(typeof(Exception));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(ScriptContext).GetMethod("CaptureExceptionStack", BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(OpCodes.Rethrow);
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, frame);
            il.Emit(OpCodes.Call, typeof(ScriptContext).GetMethod("LeaveFrame", BindingFlags.Instance | BindingFlags.NonPublic));
            il.EndExceptionBlock();
            if (result != null) il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ret);
            return method;
        }
    }
}
