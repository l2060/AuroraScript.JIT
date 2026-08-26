using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace AuroraScript.Hosting.Generators
{
    internal enum HostParamCoercion
    {
        Weak,
        Exact,
        Strict
    }

    internal enum HostExportFailure
    {
        Default,
        ReturnNaN,
        ReturnNull,
        Throw
    }
    [Generator]
    public sealed class AuroraExportGenerator : IIncrementalGenerator
    {
        private const string BuiltinGlobalAttribute = "AuroraScript.Hosting.AuroraBuiltinGlobalAttribute";
        private const string ExportAttribute = "AuroraScript.Hosting.AuroraExportAttribute";
        private const string ParamAttribute = "AuroraScript.Hosting.AuroraParamAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
                BuiltinGlobalAttribute,
                static (node, _) => node is ClassDeclarationSyntax,
                static (context, cancellationToken) => ParseBuiltinGlobal(context, cancellationToken));

            context.RegisterSourceOutput(
                candidates.Collect(),
                static (productionContext, models) => Execute(productionContext, models));
            context.RegisterSourceOutput(
                candidates.Collect(),
                static (productionContext, models) =>
                    EmitCompilerCatalog(productionContext, models));
        }

        private static BuiltinGlobalModel? ParseBuiltinGlobal(
            GeneratorAttributeSyntaxContext context,
            System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context.TargetSymbol is not INamedTypeSymbol typeSymbol ||
                !IsPartialClass(typeSymbol))
            {
                return null;
            }

            var globalAttribute = context.Attributes.FirstOrDefault(
                attribute => attribute.AttributeClass?.ToDisplayString() == BuiltinGlobalAttribute);
            if (globalAttribute == null)
            {
                return null;
            }

            var globalName = globalAttribute.ConstructorArguments.Length > 0
                ? globalAttribute.ConstructorArguments[0].Value as string
                : null;
            if (string.IsNullOrWhiteSpace(globalName))
            {
                return null;
            }

            var writable = GetNamedBool(globalAttribute, "Writable");
            var enumerable = GetNamedBool(globalAttribute, "Enumerable");

            var exports = new List<ExportModel>();
            foreach (var member in typeSymbol.GetMembers())
            {
                if (member is not IMethodSymbol methodSymbol ||
                    methodSymbol.MethodKind != MethodKind.Ordinary ||
                    !methodSymbol.IsStatic ||
                    methodSymbol.IsImplicitlyDeclared)
                {
                    continue;
                }

                var exportAttribute = methodSymbol.GetAttributes()
                    .FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == ExportAttribute);
                if (exportAttribute == null)
                {
                    continue;
                }

                var export = ParseExport(typeSymbol, methodSymbol, exportAttribute);
                if (export != null)
                {
                    exports.Add(export);
                }
            }

            if (exports.Count == 0)
            {
                return null;
            }

            exports.Sort(static (left, right) =>
                string.Compare(left.ScriptName, right.ScriptName, StringComparison.Ordinal));

            return new BuiltinGlobalModel(
                typeSymbol.ContainingNamespace.ToDisplayString(),
                typeSymbol.Name,
                globalName!,
                writable,
                enumerable,
                exports);
        }

        private static ExportModel? ParseExport(
            INamedTypeSymbol containingType,
            IMethodSymbol methodSymbol,
            AttributeData exportAttribute)
        {
            var scriptName = exportAttribute.ConstructorArguments.Length > 0
                ? exportAttribute.ConstructorArguments[0].Value as string
                : null;
            if (string.IsNullOrWhiteSpace(scriptName))
            {
                scriptName = InferScriptName(methodSymbol.Name);
            }

            var failure = ParseFailure(exportAttribute, methodSymbol.ReturnType);
            var parameters = new List<ParameterModel>();
            for (var index = 0; index < methodSymbol.Parameters.Length; index++)
            {
                var parameter = methodSymbol.Parameters[index];
                var parameterKind = ResolveParameterKind(parameter.Type);
                if (parameterKind == ParameterKind.Unsupported)
                {
                    return null;
                }

                var coercion = ParseCoercion(parameter);
                parameters.Add(new ParameterModel(
                    index,
                    $"arg{index}",
                    parameterKind,
                    coercion));
            }

            var returnKind = ResolveReturnKind(methodSymbol.ReturnType);
            if (returnKind == ReturnKind.Unsupported)
            {
                return null;
            }

            return new ExportModel(
                scriptName!,
                methodSymbol.Name,
                BuildAdapterName(scriptName!),
                containingType.ToDisplayString(),
                containingType.DeclaredAccessibility == Accessibility.Public &&
                    methodSymbol.DeclaredAccessibility == Accessibility.Public,
                failure,
                returnKind,
                parameters);
        }

        private static void Execute(
            SourceProductionContext context,
            ImmutableArray<BuiltinGlobalModel?> models)
        {
            foreach (var model in models)
            {
                if (model == null)
                {
                    continue;
                }

                var source = SourceText.From(GenerateSource(model), Encoding.UTF8);
                context.AddSource($"{model.ClassName}.AuroraExports.g.cs", source);
            }
        }

        private static void EmitCompilerCatalog(
            SourceProductionContext context,
            ImmutableArray<BuiltinGlobalModel?> models)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#nullable disable");
            builder.AppendLine("#pragma warning disable CS1591");

            var count = 0;
            foreach (var model in models)
            {
                if (model == null)
                {
                    continue;
                }

                foreach (var export in model.Exports)
                {
                    if (!export.CanDirectCall)
                    {
                        continue;
                    }

                    builder.Append("[assembly: global::AuroraScript.Hosting.AuroraGeneratedExportAttribute(");
                    builder.Append('"').Append(EscapeString(model.GlobalName)).Append("\", ");
                    builder.Append('"').Append(EscapeString(export.ScriptName)).Append("\", ");
                    builder.Append("typeof(global::").Append(export.ContainingTypeDisplayName).Append("), ");
                    builder.Append('"').Append(EscapeString(export.CoreMethodName)).Append("\", ");
                    builder.Append("global::AuroraScript.Hosting.AuroraExportValueKind.")
                        .Append(GetCatalogKind(export.ReturnKind)).Append(", new global::AuroraScript.Hosting.AuroraExportValueKind[] { ");
                    for (var i = 0; i < export.Parameters.Count; i++)
                    {
                        if (i != 0)
                        {
                            builder.Append(", ");
                        }
                        builder.Append("global::AuroraScript.Hosting.AuroraExportValueKind.")
                            .Append(GetCatalogKind(export.Parameters[i].Kind));
                    }
                    builder.AppendLine(" })]");
                    count++;
                }
            }

            if (count != 0)
            {
                context.AddSource(
                    "AuroraHostExportCatalog.g.cs",
                    SourceText.From(builder.ToString(), Encoding.UTF8));
            }
        }

        private static string GetCatalogKind(ParameterKind kind)
        {
            return kind switch
            {
                ParameterKind.Number => "Number",
                ParameterKind.Int32 => "Int32",
                ParameterKind.Boolean => "Boolean",
                ParameterKind.String => "String",
                ParameterKind.Object => "Object",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        private static string GetCatalogKind(ReturnKind kind)
        {
            return kind switch
            {
                ReturnKind.Void => "Void",
                ReturnKind.Number => "Number",
                ReturnKind.Int32 => "Int32",
                ReturnKind.Boolean => "Boolean",
                ReturnKind.String => "String",
                ReturnKind.Object => "Object",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        private static string GenerateSource(BuiltinGlobalModel model)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#nullable disable");
            builder.AppendLine("#pragma warning disable CS1591");
            builder.AppendLine("using System;");
            builder.AppendLine("using AuroraScript;");
            builder.AppendLine("using AuroraScript.Core;");
            builder.AppendLine("using AuroraScript.Runtime;");
            builder.AppendLine("using AuroraScript.Runtime.Types;");
            builder.AppendLine();
            builder.AppendLine($"namespace {model.Namespace}");
            builder.AppendLine("{");
            builder.AppendLine($"    partial class {model.ClassName}");
            builder.AppendLine("    {");

            builder.AppendLine("        private void RegisterAuroraExports()");
            builder.AppendLine("        {");
            foreach (var export in model.Exports)
            {
                builder.Append("            Define(\"");
                builder.Append(EscapeString(export.ScriptName));
                builder.Append("\", ScriptDatum.FromBonding(");
                builder.Append(export.AdapterMethodName);
                builder.Append("), writeable: ");
                builder.Append(model.Writable ? "true" : "false");
                builder.Append(", enumerable: ");
                builder.Append(model.Enumerable ? "true" : "false");
                builder.AppendLine(");");
            }
            builder.AppendLine("        }");
            builder.AppendLine();

            foreach (var export in model.Exports)
            {
                builder.AppendLine("        public static void " + export.AdapterMethodName + "(");
                builder.AppendLine("            ScriptContext ctx,");
                builder.AppendLine("            ScriptObject thisObject,");
                builder.AppendLine("            Span<ScriptDatum> args,");
                builder.AppendLine("            ref ScriptDatum result)");
                builder.AppendLine("        {");
                AppendParameterCoercion(builder, export);
                AppendCoreInvocation(builder, export);
                builder.AppendLine("        }");
                builder.AppendLine();
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        private static void AppendParameterCoercion(StringBuilder builder, ExportModel export)
        {
            foreach (var parameter in export.Parameters)
            {
                switch (parameter.Kind)
                {
                    case ParameterKind.Number:
                        AppendNumberCoercion(builder, export, parameter);
                        break;
                    case ParameterKind.Int32:
                        AppendInt32Coercion(builder, export, parameter);
                        break;
                    case ParameterKind.Boolean:
                        AppendBooleanCoercion(builder, export, parameter);
                        break;
                    case ParameterKind.String:
                        AppendStringCoercion(builder, export, parameter);
                        break;
                    case ParameterKind.Object:
                        AppendObjectCoercion(builder, export, parameter);
                        break;
                }
            }
        }

        private static void AppendNumberCoercion(
            StringBuilder builder,
            ExportModel export,
            ParameterModel parameter)
        {
            switch (parameter.Coercion)
            {
                case HostParamCoercion.Exact:
                    builder.AppendLine("            if ((uint)" + parameter.Index + " >= (uint)args.Length)");
                    builder.AppendLine("            {");
                    AppendFailureReturn(builder, export, indent: "                ");
                    builder.AppendLine("            }");
                    builder.AppendLine("            var " + parameter.VariableName + "Datum = args[" + parameter.Index + "];");
                    builder.AppendLine("            TypeCheckOps.Check(" + parameter.VariableName + "Datum, CheckedType.Number);");
                    builder.AppendLine("            var " + parameter.VariableName + " = " + parameter.VariableName + "Datum.Number;");
                    break;

                case HostParamCoercion.Strict:
                    builder.AppendLine("            if (!args.TryGetStrictNumber(" + parameter.Index + ", out var " + parameter.VariableName + "))");
                    builder.AppendLine("            {");
                    AppendFailureReturn(builder, export, indent: "                ");
                    builder.AppendLine("            }");
                    break;

                default:
                    builder.AppendLine("            if (!args.TryGetNumber(" + parameter.Index + ", out var " + parameter.VariableName + "))");
                    builder.AppendLine("            {");
                    AppendFailureReturn(builder, export, indent: "                ");
                    builder.AppendLine("            }");
                    break;
            }
        }

        private static void AppendBooleanCoercion(
            StringBuilder builder,
            ExportModel export,
            ParameterModel parameter)
        {
            if (parameter.Coercion == HostParamCoercion.Exact)
            {
                builder.AppendLine("            if ((uint)" + parameter.Index + " >= (uint)args.Length)");
                builder.AppendLine("            {");
                AppendFailureReturn(builder, export, indent: "                ");
                builder.AppendLine("            }");
                builder.AppendLine("            var " + parameter.VariableName + "Datum = args[" + parameter.Index + "];");
                builder.AppendLine("            TypeCheckOps.Check(" + parameter.VariableName + "Datum, CheckedType.Boolean);");
                builder.AppendLine("            var " + parameter.VariableName + " = " + parameter.VariableName + "Datum.Boolean;");
                return;
            }

            builder.AppendLine("            if (!args.TryGetBoolean(" + parameter.Index + ", out var " + parameter.VariableName + "))");
            builder.AppendLine("            {");
            AppendFailureReturn(builder, export, indent: "                ");
            builder.AppendLine("            }");
        }

        private static void AppendInt32Coercion(
            StringBuilder builder,
            ExportModel export,
            ParameterModel parameter)
        {
            if (parameter.Coercion == HostParamCoercion.Exact)
            {
                builder.AppendLine("            if ((uint)" + parameter.Index + " >= (uint)args.Length)");
                builder.AppendLine("            {");
                AppendFailureReturn(builder, export, indent: "                ");
                builder.AppendLine("            }");
                builder.AppendLine("            var " + parameter.VariableName + "Datum = args[" + parameter.Index + "];");
                builder.AppendLine("            TypeCheckOps.Check(" + parameter.VariableName + "Datum, CheckedType.Number);");
                builder.AppendLine("            var " + parameter.VariableName + " = (int)" + parameter.VariableName + "Datum.Number;");
                return;
            }

            builder.AppendLine("            if (!args.TryGetInt32(" + parameter.Index + ", out var " + parameter.VariableName + "))");
            builder.AppendLine("            {");
            AppendFailureReturn(builder, export, indent: "                ");
            builder.AppendLine("            }");
        }

        private static void AppendStringCoercion(
            StringBuilder builder,
            ExportModel export,
            ParameterModel parameter)
        {
            if (parameter.Coercion == HostParamCoercion.Exact)
            {
                builder.AppendLine("            if ((uint)" + parameter.Index + " >= (uint)args.Length)");
                builder.AppendLine("            {");
                AppendFailureReturn(builder, export, indent: "                ");
                builder.AppendLine("            }");
                builder.AppendLine("            var " + parameter.VariableName + "Datum = args[" + parameter.Index + "];");
                builder.AppendLine("            TypeCheckOps.Check(" + parameter.VariableName + "Datum, CheckedType.String);");
                builder.AppendLine("            var " + parameter.VariableName + " = " + parameter.VariableName + "Datum.StringText;");
                return;
            }

            builder.AppendLine("            if (!args.TryGetString(" + parameter.Index + ", out var " + parameter.VariableName + "))");
            builder.AppendLine("            {");
            AppendFailureReturn(builder, export, indent: "                ");
            builder.AppendLine("            }");
        }

        private static void AppendObjectCoercion(
            StringBuilder builder,
            ExportModel export,
            ParameterModel parameter)
        {
            if (parameter.Coercion == HostParamCoercion.Exact)
            {
                builder.AppendLine("            if ((uint)" + parameter.Index + " >= (uint)args.Length)");
                builder.AppendLine("            {");
                AppendFailureReturn(builder, export, indent: "                ");
                builder.AppendLine("            }");
                builder.AppendLine("            var " + parameter.VariableName + "Datum = args[" + parameter.Index + "];");
                builder.AppendLine("            TypeCheckOps.Check(" + parameter.VariableName + "Datum, CheckedType.Object);");
                builder.AppendLine("            var " + parameter.VariableName + " = " + parameter.VariableName + "Datum.Object;");
                return;
            }

            builder.AppendLine("            if (!args.TryGetObject(" + parameter.Index + ", out var " + parameter.VariableName + "))");
            builder.AppendLine("            {");
            AppendFailureReturn(builder, export, indent: "                ");
            builder.AppendLine("            }");
        }

        private static void AppendFailure(StringBuilder builder, ExportModel export, string indent)
        {
            switch (export.EffectiveFailure)
            {
                case HostExportFailure.ReturnNaN:
                    builder.AppendLine(indent + "ScriptDatum.WriteAsNumber(ref result, double.NaN);");
                    break;
                case HostExportFailure.ReturnNull:
                    builder.AppendLine(indent + "ScriptDatum.MarkAsNull(ref result);");
                    break;
                case HostExportFailure.Throw:
                    builder.AppendLine(indent + "throw new AuroraRuntimeException(\"Type check failed while invoking " +
                        EscapeString(export.ScriptName) + ".\");");
                    break;
            }
        }

        private static void AppendFailureReturn(StringBuilder builder, ExportModel export, string indent)
        {
            AppendFailure(builder, export, indent);
            if (export.EffectiveFailure != HostExportFailure.Throw)
            {
                builder.AppendLine(indent + "return;");
            }
        }

        private static void AppendCoreInvocation(StringBuilder builder, ExportModel export)
        {
            var argumentList = string.Join(", ", export.Parameters.Select(parameter => parameter.VariableName));
            if (export.ReturnKind == ReturnKind.Void)
            {
                builder.AppendLine("            " + export.CoreMethodName + "(" + argumentList + ");");
                return;
            }

            builder.Append("            var coreResult = " + export.CoreMethodName + "(" + argumentList + ");");
            builder.AppendLine();
            switch (export.ReturnKind)
            {
                case ReturnKind.Number:
                case ReturnKind.Int32:
                    builder.AppendLine("            ScriptDatum.WriteAsNumber(ref result, coreResult);");
                    break;
                case ReturnKind.Boolean:
                    builder.AppendLine("            ScriptDatum.WriteAsBoolean(ref result, coreResult);");
                    break;
                case ReturnKind.String:
                    builder.AppendLine("            ScriptDatum.WriteAsString(ref result, coreResult);");
                    break;
                case ReturnKind.Object:
                    builder.AppendLine("            ScriptDatum.WriteAsObject(ref result, coreResult);");
                    break;
            }
        }

        private static HostExportFailure ParseFailure(AttributeData exportAttribute, ITypeSymbol returnType)
        {
            var failureValue = GetNamedEnum<HostExportFailure>(exportAttribute, "Failure");
            if (failureValue != HostExportFailure.Default)
            {
                return failureValue;
            }

            return ResolveReturnKind(returnType) switch
            {
                ReturnKind.Number => HostExportFailure.ReturnNaN,
                ReturnKind.Int32 => HostExportFailure.ReturnNaN,
                ReturnKind.Void => HostExportFailure.ReturnNull,
                _ => HostExportFailure.ReturnNull
            };
        }

        private static HostParamCoercion ParseCoercion(IParameterSymbol parameter)
        {
            var attribute = parameter.GetAttributes()
                .FirstOrDefault(data => data.AttributeClass?.ToDisplayString() == ParamAttribute);
            if (attribute == null)
            {
                return HostParamCoercion.Weak;
            }

            return GetNamedEnum<HostParamCoercion>(attribute, "Coercion");
        }

        private static ParameterKind ResolveParameterKind(ITypeSymbol typeSymbol)
        {
            switch (typeSymbol.SpecialType)
            {
                case SpecialType.System_Double:
                    return ParameterKind.Number;
                case SpecialType.System_Int32:
                    return ParameterKind.Int32;
                case SpecialType.System_Boolean:
                    return ParameterKind.Boolean;
                case SpecialType.System_String:
                    return ParameterKind.String;
            }

            if (typeSymbol.ToDisplayString() == "AuroraScript.Runtime.Types.ScriptObject")
            {
                return ParameterKind.Object;
            }

            return ParameterKind.Unsupported;
        }

        private static ReturnKind ResolveReturnKind(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.SpecialType == SpecialType.System_Void)
            {
                return ReturnKind.Void;
            }

            switch (typeSymbol.SpecialType)
            {
                case SpecialType.System_Double:
                    return ReturnKind.Number;
                case SpecialType.System_Int32:
                    return ReturnKind.Int32;
                case SpecialType.System_Boolean:
                    return ReturnKind.Boolean;
                case SpecialType.System_String:
                    return ReturnKind.String;
            }

            if (typeSymbol.ToDisplayString() == "AuroraScript.Runtime.Types.ScriptObject")
            {
                return ReturnKind.Object;
            }

            return ReturnKind.Unsupported;
        }

        private static bool IsType(ITypeSymbol typeSymbol, string displayName)
        {
            return string.Equals(typeSymbol.ToDisplayString(), displayName, StringComparison.Ordinal);
        }

        private static string InferScriptName(string methodName)
        {
            if (methodName.EndsWith("Core", StringComparison.Ordinal) && methodName.Length > 4)
            {
                methodName = methodName.Substring(0, methodName.Length - 4);
            }

            if (methodName.Length == 0)
            {
                return methodName;
            }

            if (methodName.Length == 1)
            {
                return methodName.ToLowerInvariant();
            }

            return char.ToLowerInvariant(methodName[0]) + methodName.Substring(1);
        }

        private static string BuildAdapterName(string scriptName)
        {
            return scriptName.ToUpperInvariant();
        }

        private static bool GetNamedBool(AttributeData attribute, string name)
        {
            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == name && argument.Value.Value is bool value)
                {
                    return value;
                }
            }

            return false;
        }

        private static TEnum GetNamedEnum<TEnum>(AttributeData attribute, string name)
            where TEnum : struct
        {
            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key != name || argument.Value.Value == null)
                {
                    continue;
                }

                if (argument.Value.Value is TEnum typed)
                {
                    return typed;
                }

                if (argument.Value.Value is int value &&
                    Enum.IsDefined(typeof(TEnum), value))
                {
                    return (TEnum)Enum.ToObject(typeof(TEnum), value);
                }
            }

            return default;
        }

        private static bool IsPartialClass(INamedTypeSymbol typeSymbol)
        {
            foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is ClassDeclarationSyntax classDeclaration &&
                    classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        private static string EscapeString(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private sealed class BuiltinGlobalModel
        {
            public BuiltinGlobalModel(
                string namespaceName,
                string className,
                string globalName,
                bool writable,
                bool enumerable,
                IReadOnlyList<ExportModel> exports)
            {
                Namespace = namespaceName;
                ClassName = className;
                GlobalName = globalName;
                Writable = writable;
                Enumerable = enumerable;
                Exports = exports;
            }

            public string Namespace { get; }
            public string ClassName { get; }
            public string GlobalName { get; }
            public bool Writable { get; }
            public bool Enumerable { get; }
            public IReadOnlyList<ExportModel> Exports { get; }
        }

        private sealed class ExportModel
        {
            public ExportModel(
                string scriptName,
                string coreMethodName,
                string adapterMethodName,
                string containingTypeDisplayName,
                bool canDirectCall,
                HostExportFailure failure,
                ReturnKind returnKind,
                IReadOnlyList<ParameterModel> parameters)
            {
                ScriptName = scriptName;
                CoreMethodName = coreMethodName;
                AdapterMethodName = adapterMethodName;
                ContainingTypeDisplayName = containingTypeDisplayName;
                CanDirectCall = canDirectCall;
                Failure = failure;
                ReturnKind = returnKind;
                Parameters = parameters;
                EffectiveFailure = failure == HostExportFailure.Default
                    ? ResolveDefaultFailure(returnKind)
                    : failure;
            }

            public string ScriptName { get; }
            public string CoreMethodName { get; }
            public string AdapterMethodName { get; }
            public string ContainingTypeDisplayName { get; }
            public bool CanDirectCall { get; }
            public HostExportFailure Failure { get; }
            public HostExportFailure EffectiveFailure { get; }
            public ReturnKind ReturnKind { get; }
            public IReadOnlyList<ParameterModel> Parameters { get; }

            private static HostExportFailure ResolveDefaultFailure(ReturnKind returnKind)
            {
                return returnKind switch
                {
                    ReturnKind.Number => HostExportFailure.ReturnNaN,
                    ReturnKind.Void => HostExportFailure.ReturnNull,
                    _ => HostExportFailure.ReturnNull
                };
            }
        }

        private sealed class ParameterModel
        {
            public ParameterModel(
                int index,
                string variableName,
                ParameterKind kind,
                HostParamCoercion coercion)
            {
                Index = index;
                VariableName = variableName;
                Kind = kind;
                Coercion = coercion;
            }

            public int Index { get; }
            public string VariableName { get; }
            public ParameterKind Kind { get; }
            public HostParamCoercion Coercion { get; }
        }

        private enum ParameterKind
        {
            Unsupported,
            Number,
            Int32,
            Boolean,
            String,
            Object
        }

        private enum ReturnKind
        {
            Unsupported,
            Void,
            Number,
            Int32,
            Boolean,
            String,
            Object
        }
    }
}
