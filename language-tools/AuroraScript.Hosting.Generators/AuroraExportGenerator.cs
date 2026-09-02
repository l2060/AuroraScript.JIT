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
    public sealed partial class AuroraExportGenerator : IIncrementalGenerator
    {
        private const string BuiltinGlobalAttribute = "AuroraScript.Hosting.AuroraNativeModuleAttribute";
        private const string NativeTypeAttribute = "AuroraScript.Hosting.AuroraNativeTypeAttribute";
        private const string TypedDocumentInterface = "AuroraScript.Runtime.Serialization.INativeTypedDocument";
        private const string ExportAttribute = "AuroraScript.Hosting.AuroraExportAttribute";
        private const string ParamAttribute = "AuroraScript.Hosting.AuroraParamAttribute";
        private static readonly DiagnosticDescriptor InvalidGlobal = new(
            "AURORAEXP001",
            "Invalid Aurora builtin global",
            "{0}",
            "AuroraScript.Hosting",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor InvalidExport = new(
            "AURORAEXP002",
            "Invalid Aurora export",
            "{0}",
            "AuroraScript.Hosting",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor DuplicateExport = new(
            "AURORAEXP003",
            "Duplicate Aurora export",
            "Global '{0}' exports script member '{1}' more than once",
            "AuroraScript.Hosting",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor ManualRegistration = new(
            "AURORAEXP004",
            "Aurora exports require manual registration",
            "{0}",
            "AuroraScript.Hosting",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
                BuiltinGlobalAttribute,
                static (node, _) => node is TypeDeclarationSyntax,
                static (context, cancellationToken) => ParseBuiltinGlobal(context, cancellationToken));
            var nativeTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
                NativeTypeAttribute,
                static (node, _) => node is TypeDeclarationSyntax,
                static (context, cancellationToken) => ParseNativeObject(context, cancellationToken));
            var allNativeTypes = nativeTypes.Collect();

            context.RegisterSourceOutput(
                candidates.Collect(),
                static (productionContext, models) => Execute(productionContext, models));
            context.RegisterSourceOutput(
                candidates.Collect(),
                static (productionContext, models) =>
                    EmitCompilerCatalog(productionContext, models));
            context.RegisterSourceOutput(
                allNativeTypes,
                static (productionContext, models) => ExecuteNativeObjects(productionContext, models));
            context.RegisterSourceOutput(
                allNativeTypes,
                static (productionContext, models) =>
                    EmitNativeObjectCatalog(productionContext, models));
        }

        private static BuiltinGlobalModel? ParseBuiltinGlobal(
            GeneratorAttributeSyntaxContext context,
            System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            {
                return null;
            }

            var globalAttribute = context.Attributes.FirstOrDefault(
                attribute => attribute.AttributeClass?.ToDisplayString() == BuiltinGlobalAttribute);
            if (globalAttribute == null)
            {
                return null;
            }

            var diagnostics = new List<Diagnostic>();
            if (typeSymbol.IsRecord || typeSymbol.IsAbstract)
            {
                diagnostics.Add(Diagnostic.Create(
                    InvalidGlobal,
                    GetLocation(typeSymbol),
                    $"Type '{typeSymbol.ToDisplayString()}' must be a non-abstract class."));
            }
            if (!IsPartialClass(typeSymbol))
            {
                diagnostics.Add(Diagnostic.Create(
                    InvalidGlobal,
                    GetLocation(typeSymbol),
                    $"Type '{typeSymbol.ToDisplayString()}' must be partial to use AuroraNativeModule."));
            }
            if (typeSymbol.ContainingType != null || typeSymbol.TypeParameters.Length != 0)
            {
                diagnostics.Add(Diagnostic.Create(
                    InvalidGlobal,
                    GetLocation(typeSymbol),
                    $"Type '{typeSymbol.ToDisplayString()}' must be a non-generic top-level class."));
            }
            if (typeSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                diagnostics.Add(Diagnostic.Create(
                    InvalidGlobal,
                    GetLocation(typeSymbol),
                    $"Type '{typeSymbol.ToDisplayString()}' must be declared in a namespace."));
            }
            if (!DerivesFromScriptObject(typeSymbol))
            {
                diagnostics.Add(Diagnostic.Create(
                    InvalidGlobal,
                    GetLocation(typeSymbol),
                    $"Type '{typeSymbol.ToDisplayString()}' must derive from ScriptObject."));
            }
            if (typeSymbol.GetAttributes().Any(
                    attribute => attribute.AttributeClass?.ToDisplayString() == NativeTypeAttribute))
            {
                diagnostics.Add(Diagnostic.Create(
                    InvalidGlobal,
                    GetLocation(typeSymbol),
                    $"Type '{typeSymbol.ToDisplayString()}' cannot be both AuroraNativeModule and AuroraNativeType."));
            }
            if (typeSymbol.InstanceConstructors.Any(
                    static constructor => !constructor.IsImplicitlyDeclared))
            {
                diagnostics.Add(Diagnostic.Create(
                    ManualRegistration,
                    GetLocation(typeSymbol),
                    $"Type '{typeSymbol.ToDisplayString()}' declares an instance constructor and must call RegisterAuroraExports()."));
            }

            var globalName = globalAttribute.ConstructorArguments.Length > 0
                ? globalAttribute.ConstructorArguments[0].Value as string
                : null;
            if (string.IsNullOrWhiteSpace(globalName))
            {
                diagnostics.Add(Diagnostic.Create(
                    InvalidGlobal,
                    GetLocation(typeSymbol),
                    "AuroraNativeModule requires a non-empty global name."));
                globalName = typeSymbol.Name;
            }

            var writable = GetNamedBool(globalAttribute, "Writable");
            var enumerable = GetNamedBool(globalAttribute, "Enumerable");

            var exports = new List<ExportModel>();
            var constants = new List<ConstantModel>();
            var exportedNames = new HashSet<string>(StringComparer.Ordinal);
            var adapterNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var member in typeSymbol.GetMembers())
            {
                var exportAttribute = member.GetAttributes()
                    .FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == ExportAttribute);
                if (exportAttribute == null)
                {
                    continue;
                }

                if (member is IMethodSymbol methodSymbol &&
                    methodSymbol.MethodKind == MethodKind.Ordinary &&
                    methodSymbol.IsStatic &&
                    !methodSymbol.IsImplicitlyDeclared)
                {
                    var export = ParseExport(typeSymbol, methodSymbol, exportAttribute);
                    if (export != null)
                    {
                        if (exportedNames.Add(export.ScriptName))
                        {
                            if (adapterNames.Add(export.AdapterMethodName))
                            {
                                exports.Add(export);
                            }
                            else
                            {
                                diagnostics.Add(Diagnostic.Create(
                                    DuplicateExport,
                                    GetLocation(member),
                                    globalName,
                                    export.ScriptName));
                            }
                        }
                        else
                        {
                            diagnostics.Add(Diagnostic.Create(
                                DuplicateExport,
                                GetLocation(member),
                                globalName,
                                export.ScriptName));
                        }
                    }
                    else
                    {
                        diagnostics.Add(Diagnostic.Create(
                            InvalidExport,
                            GetLocation(member),
                            $"Method '{member.ToDisplayString()}' has an unsupported Aurora export signature."));
                    }
                }
                else if (member is IFieldSymbol fieldSymbol &&
                    fieldSymbol.IsStatic &&
                    !fieldSymbol.IsImplicitlyDeclared)
                {
                    var constant = ParseConstant(fieldSymbol, exportAttribute);
                    if (constant != null)
                    {
                        if (exportedNames.Add(constant.ScriptName))
                        {
                            constants.Add(constant);
                        }
                        else
                        {
                            diagnostics.Add(Diagnostic.Create(
                                DuplicateExport,
                                GetLocation(member),
                                globalName,
                                constant.ScriptName));
                        }
                    }
                    else
                    {
                        diagnostics.Add(Diagnostic.Create(
                            InvalidExport,
                            GetLocation(member),
                            $"Field '{member.ToDisplayString()}' must be a public static readonly double."));
                    }
                }
                else
                {
                    diagnostics.Add(Diagnostic.Create(
                        InvalidExport,
                        GetLocation(member),
                        $"Member '{member.ToDisplayString()}' must be a static method or static readonly field."));
                }
            }

            exports.Sort(static (left, right) =>
                string.Compare(left.ScriptName, right.ScriptName, StringComparison.Ordinal));
            constants.Sort(static (left, right) =>
                string.Compare(left.ScriptName, right.ScriptName, StringComparison.Ordinal));

            return new BuiltinGlobalModel(
                typeSymbol.ContainingNamespace.ToDisplayString(),
                typeSymbol.Name,
                GetConstructorAccessibility(typeSymbol),
                !typeSymbol.InstanceConstructors.Any(
                    constructor => !constructor.IsImplicitlyDeclared),
                globalName!,
                writable,
                enumerable,
                exports,
                constants,
                diagnostics);
        }

        private static ExportModel? ParseExport(
            INamedTypeSymbol containingType,
            IMethodSymbol methodSymbol,
            AttributeData exportAttribute,
            string adapterPrefix = "")
        {
            if (methodSymbol.TypeParameters.Length != 0 ||
                methodSymbol.ReturnsByRef ||
                methodSymbol.ReturnsByRefReadonly ||
                !HasValidEnumArgument<HostExportFailure>(
                    exportAttribute,
                    constructorIndex: 1,
                    namedArgument: "Failure"))
            {
                return null;
            }

            var scriptName = exportAttribute.ConstructorArguments.Length > 0
                ? exportAttribute.ConstructorArguments[0].Value as string
                : null;
            if (string.IsNullOrWhiteSpace(scriptName))
            {
                scriptName = InferScriptName(methodSymbol.Name);
            }

            var failure = ParseFailure(exportAttribute, methodSymbol.ReturnType);
            var start = 0;
            var takesContext = false;
            var takesThisObject = false;
            if (methodSymbol.Parameters.Length > 0 &&
                IsScriptContext(methodSymbol.Parameters[0].Type))
            {
                takesContext = true;
                start = 1;
            }
            if (start < methodSymbol.Parameters.Length &&
                IsThisObjectParameter(methodSymbol.Parameters[start]))
            {
                takesThisObject = true;
                start++;
            }

            var parameters = new List<ParameterModel>();
            for (var index = start; index < methodSymbol.Parameters.Length; index++)
            {
                var parameter = methodSymbol.Parameters[index];
                if (IsScriptContext(parameter.Type) ||
                    IsThisObjectParameter(parameter) ||
                    parameter.RefKind != RefKind.None)
                {
                    return null;
                }

                var parameterKind = ResolveParameterKind(parameter);
                if (parameterKind == ParameterKind.Unsupported)
                {
                    return null;
                }

                var coercion = ParseCoercion(parameter);
                var parameterAttribute = parameter.GetAttributes()
                    .FirstOrDefault(data =>
                        data.AttributeClass?.ToDisplayString() == ParamAttribute);
                if ((parameter.IsParams && parameterAttribute != null) ||
                    (parameterAttribute != null &&
                        !HasValidEnumArgument<HostParamCoercion>(
                            parameterAttribute,
                            constructorIndex: 0,
                            namedArgument: "Coercion")))
                {
                    return null;
                }
                var scriptIndex = index - start;
                string? defaultLiteral = null;
                if (parameter.HasExplicitDefaultValue)
                {
                    defaultLiteral = FormatDefaultLiteral(parameter);
                    if (defaultLiteral == null)
                    {
                        return null;
                    }
                }
                parameters.Add(new ParameterModel(
                    scriptIndex,
                    $"arg{scriptIndex}",
                    parameterKind,
                    coercion,
                    defaultLiteral,
                    parameter.Type.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat)));
            }

            var returnKind = ResolveReturnKind(methodSymbol.ReturnType);
            if (returnKind == ReturnKind.Unsupported)
            {
                return null;
            }

            var hasParams = parameters.Count != 0 &&
                parameters[parameters.Count - 1].Kind is
                    ParameterKind.NumberParams or ParameterKind.DatumParams;
            var canDirectCall = containingType.DeclaredAccessibility == Accessibility.Public &&
                methodSymbol.DeclaredAccessibility == Accessibility.Public &&
                !hasParams;

            return new ExportModel(
                scriptName!,
                methodSymbol.Name,
                adapterPrefix + BuildAdapterName(scriptName!),
                containingType.ToDisplayString(),
                canDirectCall,
                failure,
                returnKind,
                parameters,
                takesContext,
                takesThisObject,
                isInstance: !methodSymbol.IsStatic);
        }

        private static ConstantModel? ParseConstant(
            IFieldSymbol fieldSymbol,
            AttributeData exportAttribute)
        {
            if (fieldSymbol.Type.SpecialType != SpecialType.System_Double ||
                fieldSymbol.DeclaredAccessibility != Accessibility.Public ||
                !fieldSymbol.IsReadOnly)
            {
                return null;
            }

            var scriptName = exportAttribute.ConstructorArguments.Length > 0
                ? exportAttribute.ConstructorArguments[0].Value as string
                : null;
            if (string.IsNullOrWhiteSpace(scriptName))
            {
                scriptName = fieldSymbol.Name;
            }

            return new ConstantModel(
                scriptName!,
                fieldSymbol.Name,
                fieldSymbol.ContainingType.ToDisplayString());
        }

        private static void Execute(
            SourceProductionContext context,
            ImmutableArray<BuiltinGlobalModel?> models)
        {
            var globalNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var model in models)
            {
                if (model == null)
                {
                    continue;
                }
                foreach (var diagnostic in model.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
                if (model.Diagnostics.Any(static diagnostic =>
                        diagnostic.Severity == DiagnosticSeverity.Error))
                {
                    continue;
                }
                if (!globalNames.Add(model.GlobalName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidGlobal,
                        Location.None,
                        $"Global '{model.GlobalName}' is declared by more than one AuroraNativeModule type."));
                    continue;
                }

                var source = SourceText.From(GenerateSource(model), Encoding.UTF8);
                context.AddSource(
                    $"{model.Namespace.Replace('.', '_')}.{model.ClassName}.AuroraExports.g.cs",
                    source);
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
            var globalNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var model in models)
            {
                if (model == null)
                {
                    continue;
                }
                if (model.Diagnostics.Any(static diagnostic =>
                        diagnostic.Severity == DiagnosticSeverity.Error))
                {
                    continue;
                }
                if (!globalNames.Add(model.GlobalName))
                {
                    continue;
                }

                foreach (var export in model.Exports)
                {
                    if (!export.CanDirectCall || export.IsInstance)
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
                    builder.Append(" }, ");
                    builder.Append(export.TakesContext ? "true" : "false").Append(", ");
                    builder.Append(export.TakesThisObject ? "true" : "false");
                    builder.AppendLine(")]");
                    count++;
                }

                if (!model.Writable)
                {
                    foreach (var constant in model.Constants)
                    {
                        builder.Append("[assembly: global::AuroraScript.Hosting.AuroraGeneratedConstantAttribute(");
                        builder.Append('"').Append(EscapeString(model.GlobalName)).Append("\", ");
                        builder.Append('"').Append(EscapeString(constant.ScriptName)).Append("\", ");
                        builder.Append("typeof(global::").Append(constant.ContainingTypeDisplayName).Append("), ");
                        builder.Append('"').Append(EscapeString(constant.FieldName)).AppendLine("\")]");
                        count++;
                    }
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
                ParameterKind.Datum => "Datum",
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
                ReturnKind.Datum => "Datum",
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

            if (model.GenerateConstructor)
            {
                builder.Append("        ").Append(model.ConstructorAccessibility)
                    .Append(' ').Append(model.ClassName).AppendLine("()");
                builder.AppendLine("        {");
                builder.AppendLine("            RegisterAuroraExports();");
                builder.AppendLine("        }");
                builder.AppendLine();
            }

            builder.AppendLine("        private void RegisterAuroraExports()");
            builder.AppendLine("        {");
            foreach (var constant in model.Constants)
            {
                builder.Append("            Define(\"");
                builder.Append(EscapeString(constant.ScriptName));
                builder.Append("\", ScriptDatum.FromNumber(");
                builder.Append(constant.FieldName);
                builder.Append("), writeable: ");
                builder.Append(model.Writable ? "true" : "false");
                builder.Append(", enumerable: ");
                builder.Append(model.Enumerable ? "true" : "false");
                builder.AppendLine(");");
            }
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
                if (parameter.IsOptional)
                {
                    AppendOptionalCoercion(builder, export, parameter);
                    continue;
                }

                switch (parameter.Kind)
                {
                    case ParameterKind.Number:
                        AppendNumberCoercion(builder, export, parameter);
                        break;
                    case ParameterKind.NumberParams:
                        AppendNumberParamsCoercion(builder, export, parameter);
                        break;
                    case ParameterKind.DatumParams:
                        AppendDatumParamsCoercion(builder, parameter);
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
                    case ParameterKind.Datum:
                        AppendDatumCoercion(builder, export, parameter);
                        break;
                }
            }
        }

        private static void AppendOptionalCoercion(
            StringBuilder builder,
            ExportModel export,
            ParameterModel parameter)
        {
            builder.AppendLine(
                "            " + parameter.ClrTypeDisplayName + " " +
                parameter.VariableName + " = " + parameter.DefaultLiteral + ";");
            builder.AppendLine("            if ((uint)" + parameter.Index + " < (uint)args.Length)");
            builder.AppendLine("            {");
            switch (parameter.Kind)
            {
                case ParameterKind.Number:
                    builder.AppendLine("                if (args.TryGetNumber(" + parameter.Index + ", out var " + parameter.VariableName + "Specified))");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    " + parameter.VariableName + " = " + parameter.VariableName + "Specified;");
                    builder.AppendLine("                }");
                    break;
                case ParameterKind.Int32:
                    builder.AppendLine("                if (args.TryGetInt32(" + parameter.Index + ", out var " + parameter.VariableName + "Specified))");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    " + parameter.VariableName + " = " + parameter.VariableName + "Specified;");
                    builder.AppendLine("                }");
                    break;
                case ParameterKind.Boolean:
                    builder.AppendLine("                if (args.TryGetBoolean(" + parameter.Index + ", out var " + parameter.VariableName + "Specified))");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    " + parameter.VariableName + " = " + parameter.VariableName + "Specified;");
                    builder.AppendLine("                }");
                    break;
                case ParameterKind.String:
                    builder.AppendLine("                if (args.TryGetString(" + parameter.Index + ", out var " + parameter.VariableName + "Specified))");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    " + parameter.VariableName + " = " + parameter.VariableName + "Specified;");
                    builder.AppendLine("                }");
                    break;
                case ParameterKind.Object:
                    builder.AppendLine(
                        "                if (ScriptDatum.TryGetScriptObject(in args[" +
                        parameter.Index + "], out var " + parameter.VariableName +
                        "SpecifiedObject) && " + parameter.VariableName +
                        "SpecifiedObject is " + parameter.ClrTypeDisplayName + " " +
                        parameter.VariableName + "Specified)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    " + parameter.VariableName + " = " + parameter.VariableName + "Specified;");
                    builder.AppendLine("                }");
                    break;
                case ParameterKind.Datum:
                    builder.AppendLine("                args.TryGetRef(" + parameter.Index + ", ref " + parameter.VariableName + ");");
                    break;
                default:
                    AppendFailureReturn(builder, export, indent: "                ");
                    break;
            }
            builder.AppendLine("            }");
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
                    builder.AppendLine("            TypeCheckOps.CheckNumber(" + parameter.VariableName + "Datum);");
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

        private static void AppendNumberParamsCoercion(
            StringBuilder builder,
            ExportModel export,
            ParameterModel parameter)
        {
            var countName = parameter.VariableName + "Count";
            var indexName = parameter.VariableName + "Index";
            builder.AppendLine("            var " + countName + " = 0;");
            builder.AppendLine("            for (var " + indexName + " = " + parameter.Index + "; " + indexName + " < args.Length; " + indexName + "++)");
            builder.AppendLine("            {");
            builder.AppendLine("                if (!args.TryGetNumber(" + indexName + ", out _))");
            builder.AppendLine("                {");
            builder.AppendLine("                    break;");
            builder.AppendLine("                }");
            builder.AppendLine("                " + countName + "++;");
            builder.AppendLine("            }");
            builder.AppendLine("            if (" + countName + " == 0)");
            builder.AppendLine("            {");
            AppendFailureReturn(builder, export, indent: "                ");
            builder.AppendLine("            }");
            builder.AppendLine("            var " + parameter.VariableName + " = new double[" + countName + "];");
            builder.AppendLine("            for (var " + indexName + " = 0; " + indexName + " < " + countName + "; " + indexName + "++)");
            builder.AppendLine("            {");
            builder.AppendLine("                args.TryGetNumber(" + parameter.Index + " + " + indexName + ", out " + parameter.VariableName + "[" + indexName + "]);");
            builder.AppendLine("            }");
        }

        private static void AppendDatumParamsCoercion(
            StringBuilder builder,
            ParameterModel parameter)
        {
            builder.AppendLine(
                "            var " + parameter.VariableName +
                " = args.Slice(" + parameter.Index + ").ToArray();");
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
                builder.AppendLine("            TypeCheckOps.CheckBoolean(" + parameter.VariableName + "Datum);");
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
                builder.AppendLine("            TypeCheckOps.CheckNumber(" + parameter.VariableName + "Datum);");
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
                builder.AppendLine("            TypeCheckOps.CheckString(" + parameter.VariableName + "Datum);");
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
            builder.AppendLine(
                "            if ((uint)" + parameter.Index +
                " >= (uint)args.Length || !ScriptDatum.TryGetScriptObject(in args[" +
                parameter.Index + "], out var " + parameter.VariableName +
                "Object) || " + parameter.VariableName + "Object is not " +
                parameter.ClrTypeDisplayName + " " + parameter.VariableName + ")");
            builder.AppendLine("            {");
            AppendFailureReturn(builder, export, indent: "                ");
            builder.AppendLine("            }");
        }

        private static void AppendDatumCoercion(
            StringBuilder builder,
            ExportModel export,
            ParameterModel parameter)
        {
            builder.AppendLine("            var " + parameter.VariableName + " = default(ScriptDatum);");
            builder.AppendLine("            if (!args.TryGetRef(" + parameter.Index + ", ref " + parameter.VariableName + "))");
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
            var argumentNames = new List<string>();
            if (export.TakesContext)
            {
                argumentNames.Add("ctx");
            }
            if (export.TakesThisObject)
            {
                argumentNames.Add("thisObject");
            }
            for (var i = 0; i < export.Parameters.Count; i++)
            {
                argumentNames.Add(export.Parameters[i].VariableName);
            }
            var argumentList = string.Join(", ", argumentNames);
            var callee = export.IsInstance
                ? "self." + export.CoreMethodName
                : export.CoreMethodName;
            if (export.ReturnKind == ReturnKind.Void)
            {
                builder.AppendLine("            " + callee + "(" + argumentList + ");");
                return;
            }

            builder.Append("            var coreResult = " + callee + "(" + argumentList + ");");
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
                    builder.AppendLine("            ScriptDatum.WriteObject(ref result, coreResult);");
                    break;
                case ReturnKind.Datum:
                    builder.AppendLine("            result = coreResult;");
                    break;
            }
        }

        private static HostExportFailure ParseFailure(AttributeData exportAttribute, ITypeSymbol returnType)
        {
            var failureValue = GetConstructorEnum<HostExportFailure>(
                exportAttribute,
                argumentIndex: 1);
            var namedFailure = GetNamedEnum<HostExportFailure>(
                exportAttribute,
                "Failure");
            if (namedFailure != HostExportFailure.Default)
            {
                failureValue = namedFailure;
            }

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

            var coercion = GetConstructorEnum<HostParamCoercion>(
                attribute,
                argumentIndex: 0);
            var namedCoercion = GetNamedEnum<HostParamCoercion>(
                attribute,
                "Coercion");
            return namedCoercion != default ? namedCoercion : coercion;
        }

        private static ParameterKind ResolveParameterKind(IParameterSymbol parameter)
        {
            if (parameter.IsParams &&
                parameter.Type is IArrayTypeSymbol arrayType &&
                arrayType.ElementType.SpecialType == SpecialType.System_Double)
            {
                return ParameterKind.NumberParams;
            }
            if (parameter.IsParams &&
                parameter.Type is IArrayTypeSymbol datumArray &&
                IsType(datumArray.ElementType, "AuroraScript.Runtime.ScriptDatum"))
            {
                return ParameterKind.DatumParams;
            }

            return ResolveParameterKind(parameter.Type);
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

            if (IsScriptObjectType(typeSymbol))
            {
                return ParameterKind.Object;
            }

            if (typeSymbol.ToDisplayString() == "AuroraScript.Runtime.ScriptDatum")
            {
                return ParameterKind.Datum;
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

            if (IsScriptObjectType(typeSymbol))
            {
                return ReturnKind.Object;
            }

            if (typeSymbol.ToDisplayString() == "AuroraScript.Runtime.ScriptDatum")
            {
                return ReturnKind.Datum;
            }

            return ReturnKind.Unsupported;
        }

        private static bool IsScriptContext(ITypeSymbol typeSymbol)
        {
            return IsType(typeSymbol, "AuroraScript.Runtime.ScriptContext");
        }

        private static bool IsThisObjectParameter(IParameterSymbol parameter)
        {
            return string.Equals(parameter.Name, "thisObject", StringComparison.Ordinal) &&
                IsType(parameter.Type, "AuroraScript.Runtime.Types.ScriptObject");
        }

        private static bool IsType(ITypeSymbol typeSymbol, string displayName)
        {
            return string.Equals(typeSymbol.ToDisplayString(), displayName, StringComparison.Ordinal);
        }

        private static bool IsScriptObjectType(ITypeSymbol typeSymbol)
        {
            for (var current = typeSymbol as INamedTypeSymbol;
                current != null;
                current = current.BaseType)
            {
                if (IsType(current, "AuroraScript.Runtime.Types.ScriptObject"))
                {
                    return true;
                }
            }
            return false;
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
            var builder = new StringBuilder(scriptName.Length);
            for (var i = 0; i < scriptName.Length; i++)
            {
                var character = scriptName[i];
                if (character == '_' ||
                    char.IsLetter(character) ||
                    (i != 0 && char.IsDigit(character)))
                {
                    builder.Append(char.ToUpperInvariant(character));
                }
                else
                {
                    builder.Append('_');
                    builder.Append(((int)character).ToString("X4"));
                }
            }
            return builder.ToString();
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

        private static TEnum GetConstructorEnum<TEnum>(
            AttributeData attribute,
            int argumentIndex)
            where TEnum : struct
        {
            if ((uint)argumentIndex >=
                (uint)attribute.ConstructorArguments.Length)
            {
                return default;
            }

            var value = attribute.ConstructorArguments[argumentIndex].Value;
            if (value is TEnum typed)
            {
                return typed;
            }

            if (value != null)
            {
                var numericValue = Convert.ToInt32(value);
                if (Enum.IsDefined(typeof(TEnum), numericValue))
                {
                    return (TEnum)Enum.ToObject(
                        typeof(TEnum),
                        numericValue);
                }
            }

            return default;
        }

        private static bool HasValidEnumArgument<TEnum>(
            AttributeData attribute,
            int constructorIndex,
            string namedArgument)
            where TEnum : struct
        {
            if ((uint)constructorIndex <
                (uint)attribute.ConstructorArguments.Length)
            {
                var value = attribute.ConstructorArguments[constructorIndex].Value;
                if (value != null &&
                    !Enum.IsDefined(typeof(TEnum), Convert.ToInt32(value)))
                {
                    return false;
                }
            }

            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == namedArgument &&
                    argument.Value.Value != null &&
                    !Enum.IsDefined(
                        typeof(TEnum),
                        Convert.ToInt32(argument.Value.Value)))
                {
                    return false;
                }
            }

            return true;
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

        private static bool DerivesFromScriptObject(INamedTypeSymbol typeSymbol)
        {
            for (var current = typeSymbol; current != null; current = current.BaseType)
            {
                if (string.Equals(
                        current.ToDisplayString(),
                        "AuroraScript.Runtime.Types.ScriptObject",
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static Location GetLocation(ISymbol symbol)
        {
            return symbol.Locations.FirstOrDefault(static location => location.IsInSource)
                ?? Location.None;
        }

        private static string GetConstructorAccessibility(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.DeclaredAccessibility == Accessibility.Public
                ? "public"
                : "internal";
        }

        private static string EscapeString(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private static string? FormatDefaultLiteral(IParameterSymbol parameter)
        {
            var value = parameter.ExplicitDefaultValue;
            switch (parameter.Type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return value is true ? "true" : "false";
                case SpecialType.System_Int32:
                    return Convert.ToInt32(value).ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                case SpecialType.System_Double:
                    var number = Convert.ToDouble(value);
                    if (double.IsNaN(number))
                    {
                        return "double.NaN";
                    }
                    if (double.IsPositiveInfinity(number))
                    {
                        return "double.PositiveInfinity";
                    }
                    if (double.IsNegativeInfinity(number))
                    {
                        return "double.NegativeInfinity";
                    }
                    return number.ToString(
                        "R",
                        System.Globalization.CultureInfo.InvariantCulture) + "D";
                case SpecialType.System_String:
                    return value == null ? "null" : "\"" + EscapeString((string)value) + "\"";
            }

            if (value == null)
            {
                if (IsType(parameter.Type, "AuroraScript.Runtime.ScriptDatum"))
                {
                    return "default(global::AuroraScript.Runtime.ScriptDatum)";
                }
                return "null";
            }

            return null;
        }

        private sealed class BuiltinGlobalModel
        {
            public BuiltinGlobalModel(
                string namespaceName,
                string className,
                string constructorAccessibility,
                bool generateConstructor,
                string globalName,
                bool writable,
                bool enumerable,
                IReadOnlyList<ExportModel> exports,
                IReadOnlyList<ConstantModel> constants,
                IReadOnlyList<Diagnostic> diagnostics)
            {
                Namespace = namespaceName;
                ClassName = className;
                ConstructorAccessibility = constructorAccessibility;
                GenerateConstructor = generateConstructor;
                GlobalName = globalName;
                Writable = writable;
                Enumerable = enumerable;
                Exports = exports;
                Constants = constants;
                Diagnostics = diagnostics;
            }

            public string Namespace { get; }
            public string ClassName { get; }
            public string ConstructorAccessibility { get; }
            public bool GenerateConstructor { get; }
            public string GlobalName { get; }
            public bool Writable { get; }
            public bool Enumerable { get; }
            public IReadOnlyList<ExportModel> Exports { get; }
            public IReadOnlyList<ConstantModel> Constants { get; }
            public IReadOnlyList<Diagnostic> Diagnostics { get; }
        }

        private sealed class ConstantModel
        {
            public ConstantModel(
                string scriptName,
                string fieldName,
                string containingTypeDisplayName)
            {
                ScriptName = scriptName;
                FieldName = fieldName;
                ContainingTypeDisplayName = containingTypeDisplayName;
            }

            public string ScriptName { get; }
            public string FieldName { get; }
            public string ContainingTypeDisplayName { get; }
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
                IReadOnlyList<ParameterModel> parameters,
                bool takesContext,
                bool takesThisObject,
                bool isInstance = false)
            {
                ScriptName = scriptName;
                CoreMethodName = coreMethodName;
                AdapterMethodName = adapterMethodName;
                ContainingTypeDisplayName = containingTypeDisplayName;
                CanDirectCall = canDirectCall;
                Failure = failure;
                ReturnKind = returnKind;
                Parameters = parameters;
                TakesContext = takesContext;
                TakesThisObject = takesThisObject;
                IsInstance = isInstance;
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
            public bool TakesContext { get; }
            public bool TakesThisObject { get; }
            public bool IsInstance { get; }

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
                HostParamCoercion coercion,
                string? defaultLiteral,
                string clrTypeDisplayName)
            {
                Index = index;
                VariableName = variableName;
                Kind = kind;
                Coercion = coercion;
                DefaultLiteral = defaultLiteral;
                ClrTypeDisplayName = clrTypeDisplayName;
            }

            public int Index { get; }
            public string VariableName { get; }
            public ParameterKind Kind { get; }
            public HostParamCoercion Coercion { get; }
            public string? DefaultLiteral { get; }
            public string ClrTypeDisplayName { get; }
            public bool IsOptional => DefaultLiteral != null;
        }

        private enum ParameterKind
        {
            Unsupported,
            Number,
            NumberParams,
            DatumParams,
            Int32,
            Boolean,
            String,
            Object,
            Datum
        }

        private enum ReturnKind
        {
            Unsupported,
            Void,
            Number,
            Int32,
            Boolean,
            String,
            Object,
            Datum
        }
    }
}
