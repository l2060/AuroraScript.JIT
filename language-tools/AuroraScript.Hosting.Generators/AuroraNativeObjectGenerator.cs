using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace AuroraScript.Hosting.Generators
{
    public sealed partial class AuroraExportGenerator
    {
        private static NativeObjectModel? ParseNativeObject(
            GeneratorAttributeSyntaxContext context,
            System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            {
                return null;
            }

            var typeAttribute = context.Attributes.FirstOrDefault(
                attribute => attribute.AttributeClass?.ToDisplayString() == NativeTypeAttribute);
            if (typeAttribute == null)
            {
                return null;
            }

            var diagnostics = new List<Diagnostic>();
            if (typeSymbol.IsRecord || typeSymbol.IsAbstract || !typeSymbol.IsSealed)
            {
                diagnostics.Add(Diagnostic.Create(
                    InvalidGlobal,
                    GetLocation(typeSymbol),
                    $"Type '{typeSymbol.ToDisplayString()}' must be a sealed non-abstract class."));
            }
            if (!IsPartialClass(typeSymbol))
            {
                diagnostics.Add(Diagnostic.Create(
                    InvalidGlobal,
                    GetLocation(typeSymbol),
                    $"Type '{typeSymbol.ToDisplayString()}' must be partial to use AuroraNativeType."));
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
            var scriptObjectBase = FindScriptObjectBase(typeSymbol);

            var typeName = typeAttribute.ConstructorArguments.Length > 0
                ? typeAttribute.ConstructorArguments[0].Value as string
                : null;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                diagnostics.Add(Diagnostic.Create(
                    InvalidGlobal,
                    GetLocation(typeSymbol),
                    "AuroraNativeType requires a non-empty type name."));
                typeName = typeSymbol.Name;
            }

            var exports = new List<ExportModel>();
            var fields = new List<InstanceFieldModel>();
            var staticExports = new List<ExportModel>();
            var staticConstants = new List<ConstantModel>();
            var exportedNames = new HashSet<string>(StringComparer.Ordinal);
            var staticExportedNames = new HashSet<string>(StringComparer.Ordinal);
            var adapterNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var member in typeSymbol.GetMembers())
            {
                var exportAttribute = member.GetAttributes()
                    .FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == ExportAttribute);
                if (exportAttribute == null || member is IMethodSymbol { MethodKind: MethodKind.Constructor })
                {
                    continue;
                }

                if (member is IMethodSymbol methodSymbol &&
                    methodSymbol.MethodKind == MethodKind.Ordinary &&
                    !methodSymbol.IsStatic &&
                    !methodSymbol.IsImplicitlyDeclared)
                {
                    if (methodSymbol.Parameters.Any(IsThisObjectParameter))
                    {
                        diagnostics.Add(Diagnostic.Create(
                            InvalidExport,
                            GetLocation(member),
                            $"Instance export '{member.ToDisplayString()}' cannot declare a thisObject parameter."));
                        continue;
                    }

                    var export = ParseExport(typeSymbol, methodSymbol, exportAttribute);
                    if (export != null)
                    {
                        if (exportedNames.Add(export.ScriptName) &&
                            adapterNames.Add(export.AdapterMethodName))
                        {
                            exports.Add(export);
                        }
                        else
                        {
                            diagnostics.Add(Diagnostic.Create(
                                DuplicateExport,
                                GetLocation(member),
                                typeName,
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
                    !fieldSymbol.IsStatic &&
                    !fieldSymbol.IsConst &&
                    !fieldSymbol.IsImplicitlyDeclared)
                {
                    var field = ParseInstanceField(fieldSymbol, exportAttribute);
                    if (field != null)
                    {
                        if (exportedNames.Add(field.ScriptName))
                        {
                            fields.Add(field);
                        }
                        else
                        {
                            diagnostics.Add(Diagnostic.Create(
                                DuplicateExport,
                                GetLocation(member),
                                typeName,
                                field.ScriptName));
                        }
                    }
                    else
                    {
                        diagnostics.Add(Diagnostic.Create(
                            InvalidExport,
                            GetLocation(member),
                            $"Field '{member.ToDisplayString()}' must be a public instance double, int, bool, or string field."));
                    }
                }
                else if (member is IMethodSymbol staticMethodSymbol &&
                    staticMethodSymbol.MethodKind == MethodKind.Ordinary &&
                    staticMethodSymbol.IsStatic &&
                    !staticMethodSymbol.IsImplicitlyDeclared)
                {
                    var export = ParseExport(
                        typeSymbol,
                        staticMethodSymbol,
                        exportAttribute,
                        adapterPrefix: "__Static_");
                    if (export != null)
                    {
                        if (staticExportedNames.Add(export.ScriptName) &&
                            adapterNames.Add(export.AdapterMethodName))
                        {
                            staticExports.Add(export);
                        }
                        else
                        {
                            diagnostics.Add(Diagnostic.Create(
                                DuplicateExport,
                                GetLocation(member),
                                typeName,
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
                else if (member is IFieldSymbol staticFieldSymbol &&
                    staticFieldSymbol.IsStatic &&
                    !staticFieldSymbol.IsImplicitlyDeclared)
                {
                    var constant = ParseConstant(staticFieldSymbol, exportAttribute);
                    if (constant != null)
                    {
                        if (staticExportedNames.Add(constant.ScriptName))
                        {
                            staticConstants.Add(constant);
                        }
                        else
                        {
                            diagnostics.Add(Diagnostic.Create(
                                DuplicateExport,
                                GetLocation(member),
                                typeName,
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
                        $"Member '{member.ToDisplayString()}' must be an instance method, instance field, static method, or static readonly field."));
                }
            }

            var constructor = SelectConstructor(typeSymbol, diagnostics, typeName!);
            var hasInstanceSurface = fields.Count != 0 || exports.Count != 0 || constructor != null;
            if (hasInstanceSurface && scriptObjectBase == null)
            {
                diagnostics.Add(Diagnostic.Create(
                    InvalidGlobal,
                    GetLocation(typeSymbol),
                    $"Type '{typeSymbol.ToDisplayString()}' must derive from ScriptObject when it exports instance members or a constructor."));
            }
            var hasUserConstructor = typeSymbol.InstanceConstructors.Any(
                static candidate => !candidate.IsImplicitlyDeclared);
            var generateConstructor = scriptObjectBase != null && !hasUserConstructor;
            var generateTypedDocumentFactory = ImplementsTypedDocument(typeSymbol) &&
                !HasCreateTypedDocumentFactory(typeSymbol) &&
                !HasParameterlessConstructor(typeSymbol) &&
                !generateConstructor;

            exports.Sort(static (left, right) =>
                string.Compare(left.ScriptName, right.ScriptName, StringComparison.Ordinal));
            fields.Sort(static (left, right) =>
                string.Compare(left.ScriptName, right.ScriptName, StringComparison.Ordinal));
            staticExports.Sort(static (left, right) =>
                string.Compare(left.ScriptName, right.ScriptName, StringComparison.Ordinal));
            staticConstants.Sort(static (left, right) =>
                string.Compare(left.ScriptName, right.ScriptName, StringComparison.Ordinal));

            return new NativeObjectModel(
                typeSymbol.ContainingNamespace.ToDisplayString(),
                typeSymbol.Name,
                typeSymbol.ToDisplayString(),
                typeSymbol.DeclaredAccessibility == Accessibility.Public,
                GetConstructorAccessibility(typeSymbol),
                generateConstructor,
                generateTypedDocumentFactory,
                typeName!,
                ResolveOverrideAccessibility(scriptObjectBase, context.SemanticModel.Compilation),
                exports,
                fields,
                staticExports,
                staticConstants,
                constructor,
                scriptObjectBase != null,
                diagnostics);
        }

        private static bool ImplementsTypedDocument(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.AllInterfaces.Any(
                iface => iface.ToDisplayString() == TypedDocumentInterface);
        }

        private static bool HasCreateTypedDocumentFactory(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers("CreateTypedDocument")
                .OfType<IMethodSymbol>()
                .Any(method =>
                    method.IsStatic &&
                    method.Parameters.Length == 0 &&
                    method.DeclaredAccessibility == Accessibility.Public);
        }

        private static bool HasParameterlessConstructor(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.InstanceConstructors.Any(
                constructor => constructor.Parameters.Length == 0 && !constructor.IsStatic);
        }

        private static InstanceFieldModel? ParseInstanceField(
            IFieldSymbol fieldSymbol,
            AttributeData exportAttribute)
        {
            if (fieldSymbol.DeclaredAccessibility != Accessibility.Public)
            {
                return null;
            }

            var kind = ResolveParameterKind(fieldSymbol.Type);
            if (kind is ParameterKind.Unsupported or
                ParameterKind.Object or
                ParameterKind.Datum or
                ParameterKind.NumberParams or
                ParameterKind.DatumParams)
            {
                return null;
            }

            var scriptName = exportAttribute.ConstructorArguments.Length > 0
                ? exportAttribute.ConstructorArguments[0].Value as string
                : null;
            if (string.IsNullOrWhiteSpace(scriptName))
            {
                scriptName = InferScriptName(fieldSymbol.Name);
            }

            return new InstanceFieldModel(
                scriptName!,
                fieldSymbol.Name,
                kind,
                fieldSymbol.IsReadOnly);
        }

        private static ConstructorModel? SelectConstructor(
            INamedTypeSymbol typeSymbol,
            List<Diagnostic> diagnostics,
            string typeName)
        {
            var marked = typeSymbol.InstanceConstructors
                .Where(constructor => constructor.GetAttributes().Any(
                    attribute => attribute.AttributeClass?.ToDisplayString() == ExportAttribute))
                .ToList();
            if (marked.Count > 1)
            {
                diagnostics.Add(Diagnostic.Create(
                    DuplicateExport,
                    GetLocation(typeSymbol),
                    typeName,
                    "constructor"));
                return null;
            }

            if (marked.Count == 0)
            {
                return null;
            }
            var selected = marked[0];
            if (selected.DeclaredAccessibility != Accessibility.Public)
            {
                diagnostics.Add(Diagnostic.Create(
                    InvalidExport,
                    GetLocation(selected),
                    $"Constructor '{selected.ToDisplayString()}' must be public for native direct calls."));
                return null;
            }

            var parameters = new List<ParameterModel>();
            for (var index = 0; index < selected.Parameters.Length; index++)
            {
                var parameter = selected.Parameters[index];
                if (parameter.RefKind != RefKind.None ||
                    IsScriptContext(parameter.Type) ||
                    IsThisObjectParameter(parameter))
                {
                    diagnostics.Add(Diagnostic.Create(
                        InvalidExport,
                        GetLocation(selected),
                        $"Constructor '{selected.ToDisplayString()}' has an unsupported AuroraNativeType signature."));
                    return null;
                }

                var kind = ResolveParameterKind(parameter);
                if (kind is ParameterKind.Unsupported or
                    ParameterKind.NumberParams or
                    ParameterKind.DatumParams)
                {
                    diagnostics.Add(Diagnostic.Create(
                        InvalidExport,
                        GetLocation(selected),
                        $"Constructor '{selected.ToDisplayString()}' has an unsupported AuroraNativeType signature."));
                    return null;
                }

                string? defaultLiteral = null;
                if (parameter.HasExplicitDefaultValue)
                {
                    defaultLiteral = FormatDefaultLiteral(parameter);
                    if (defaultLiteral == null)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            InvalidExport,
                            GetLocation(selected),
                            $"Constructor '{selected.ToDisplayString()}' has an unsupported default value."));
                        return null;
                    }
                }

                parameters.Add(new ParameterModel(
                    index,
                    $"arg{index}",
                    kind,
                    ParseCoercion(parameter),
                    defaultLiteral,
                    parameter.Type.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat)));
            }

            return new ConstructorModel(parameters);
        }

        private static INamedTypeSymbol? FindScriptObjectBase(INamedTypeSymbol typeSymbol)
        {
            for (var current = typeSymbol.BaseType; current != null; current = current.BaseType)
            {
                if (string.Equals(
                        current.ToDisplayString(),
                        "AuroraScript.Runtime.Types.ScriptObject",
                        StringComparison.Ordinal))
                {
                    return current;
                }
            }

            return null;
        }

        /// <summary>
        /// C# forces an override of a <c>protected internal</c> member to be declared
        /// <c>protected</c> unless the declaring assembly grants internal access.
        /// </summary>
        private static string ResolveOverrideAccessibility(
            INamedTypeSymbol? scriptObjectBase,
            Compilation compilation)
        {
            var runtimeAssembly = scriptObjectBase?.ContainingAssembly;
            if (runtimeAssembly == null ||
                SymbolEqualityComparer.Default.Equals(runtimeAssembly, compilation.Assembly) ||
                runtimeAssembly.GivesAccessTo(compilation.Assembly))
            {
                return "protected internal";
            }

            return "protected";
        }

        private static void ExecuteNativeObjects(
            SourceProductionContext context,
            ImmutableArray<NativeObjectModel?> models)
        {
            var typeNames = new HashSet<string>(StringComparer.Ordinal);
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
                if (!typeNames.Add(model.TypeName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidGlobal,
                        Location.None,
                        $"Native type '{model.TypeName}' is declared more than once."));
                    continue;
                }

                context.AddSource(
                    $"{model.Namespace.Replace('.', '_')}.{model.ClassName}.AuroraNativeType.g.cs",
                    SourceText.From(GenerateNativeObjectSource(model), Encoding.UTF8));
            }
        }

        /// <summary>
        /// Emits assembly metadata that lets the script compiler bind proven
        /// receivers straight to CLR fields, instance methods, and constructors.
        /// </summary>
        private static void EmitNativeObjectCatalog(
            SourceProductionContext context,
            ImmutableArray<NativeObjectModel?> models)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#nullable disable");
            builder.AppendLine("#pragma warning disable CS1591");

            var count = 0;
            var typeNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var model in models)
            {
                if (model == null ||
                    !model.IsPublic ||
                    model.Diagnostics.Any(static diagnostic =>
                        diagnostic.Severity == DiagnosticSeverity.Error) ||
                    !typeNames.Add(model.TypeName))
                {
                    continue;
                }

                if (model.HasNativeInstances)
                {
                    builder.Append("[assembly: global::AuroraScript.Hosting.AuroraGeneratedNativeObjectAttribute(");
                    builder.Append('"').Append(EscapeString(model.TypeName)).Append("\", ");
                    builder.Append("typeof(global::").Append(model.TypeDisplayName).Append("), ");
                    AppendCatalogKinds(
                        builder,
                        model.Constructor?.Parameters,
                        static parameter => GetCatalogKind(parameter.Kind));
                    builder.Append(", ");
                    builder.Append(model.Constructor != null ? "true" : "false");
                    builder.AppendLine(")]");
                    count++;

                    foreach (var field in model.Fields)
                    {
                        builder.Append("[assembly: global::AuroraScript.Hosting.AuroraGeneratedNativeFieldAttribute(");
                        builder.Append('"').Append(EscapeString(model.TypeName)).Append("\", ");
                        builder.Append('"').Append(EscapeString(field.ScriptName)).Append("\", ");
                        builder.Append("typeof(global::").Append(model.TypeDisplayName).Append("), ");
                        builder.Append('"').Append(EscapeString(field.FieldName)).Append("\", ");
                        builder.Append("global::AuroraScript.Hosting.AuroraExportValueKind.")
                            .Append(GetCatalogKind(field.Kind)).Append(", ");
                        builder.Append(field.IsReadOnly ? "true" : "false");
                        builder.AppendLine(")]");
                        count++;
                    }

                    foreach (var export in model.Exports)
                    {
                        if (!export.CanDirectCall || !export.IsInstance)
                        {
                            continue;
                        }

                        builder.Append("[assembly: global::AuroraScript.Hosting.AuroraGeneratedNativeMethodAttribute(");
                        builder.Append('"').Append(EscapeString(model.TypeName)).Append("\", ");
                        builder.Append('"').Append(EscapeString(export.ScriptName)).Append("\", ");
                        builder.Append("typeof(global::").Append(model.TypeDisplayName).Append("), ");
                        builder.Append('"').Append(EscapeString(export.CoreMethodName)).Append("\", ");
                        builder.Append("global::AuroraScript.Hosting.AuroraExportValueKind.")
                            .Append(GetCatalogKind(export.ReturnKind)).Append(", ");
                        AppendCatalogKinds(
                            builder,
                            export.Parameters,
                            static parameter => GetCatalogKind(parameter.Kind));
                        builder.Append(", ");
                        builder.Append(export.TakesContext ? "true" : "false");
                        builder.AppendLine(")]");
                        count++;
                    }
                }

                foreach (var export in model.StaticExports)
                {
                    if (!export.CanDirectCall)
                    {
                        continue;
                    }

                    builder.Append("[assembly: global::AuroraScript.Hosting.AuroraGeneratedExportAttribute(");
                    builder.Append('"').Append(EscapeString(model.TypeName)).Append("\", ");
                    builder.Append('"').Append(EscapeString(export.ScriptName)).Append("\", ");
                    builder.Append("typeof(global::").Append(model.TypeDisplayName).Append("), ");
                    builder.Append('"').Append(EscapeString(export.CoreMethodName)).Append("\", ");
                    builder.Append("global::AuroraScript.Hosting.AuroraExportValueKind.")
                        .Append(GetCatalogKind(export.ReturnKind)).Append(", ");
                    AppendCatalogKinds(
                        builder,
                        export.Parameters,
                        static parameter => GetCatalogKind(parameter.Kind));
                    builder.Append(", ");
                    builder.Append(export.TakesContext ? "true" : "false").Append(", ");
                    builder.Append(export.TakesThisObject ? "true" : "false");
                    builder.AppendLine(")]");
                    count++;
                }

                foreach (var constant in model.StaticConstants)
                {
                    builder.Append("[assembly: global::AuroraScript.Hosting.AuroraGeneratedConstantAttribute(");
                    builder.Append('"').Append(EscapeString(model.TypeName)).Append("\", ");
                    builder.Append('"').Append(EscapeString(constant.ScriptName)).Append("\", ");
                    builder.Append("typeof(global::").Append(model.TypeDisplayName).Append("), ");
                    builder.Append('"').Append(EscapeString(constant.FieldName)).AppendLine("\")]");
                    count++;
                }
            }

            if (count != 0)
            {
                context.AddSource(
                    "AuroraNativeTypeCatalog.g.cs",
                    SourceText.From(builder.ToString(), Encoding.UTF8));
            }
        }

        private static void AppendCatalogKinds(
            StringBuilder builder,
            IReadOnlyList<ParameterModel>? parameters,
            Func<ParameterModel, string> selector)
        {
            builder.Append("new global::AuroraScript.Hosting.AuroraExportValueKind[] { ");
            for (var i = 0; parameters != null && i < parameters.Count; i++)
            {
                if (i != 0)
                {
                    builder.Append(", ");
                }
                builder.Append("global::AuroraScript.Hosting.AuroraExportValueKind.")
                    .Append(selector(parameters[i]));
            }
            builder.Append(" }");
        }

        private static string GenerateNativeObjectSource(NativeObjectModel model)
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
            builder.Append("    partial class ").Append(model.ClassName);
            if (model.HasNativeInstances)
            {
                builder.Append(" : IAuroraNativeInstance");
            }
            builder.AppendLine();
            builder.AppendLine("    {");
            if (model.HasNativeInstances)
            {
                builder.AppendLine("        private static readonly ScriptDatum NativeTypeName = ScriptDatum.FromString(\"" +
                    EscapeString(model.TypeName) + "\");");
            }
            builder.AppendLine("        public static readonly ScriptType Type = new NativeConstructor();");
            builder.AppendLine();
            if (model.GenerateConstructor)
            {
                builder.Append("        ").Append(model.ConstructorAccessibility)
                    .Append(' ').Append(model.ClassName)
                    .AppendLine("()");
                builder.AppendLine("        {");
                builder.AppendLine("        }");
                builder.AppendLine();
            }

            if (model.GenerateTypedDocumentFactory)
            {
                builder.AppendLine("        private readonly struct __AuroraTypedDocumentConstruction");
                builder.AppendLine("        {");
                builder.AppendLine("        }");
                builder.AppendLine();
                builder.Append("        private ").Append(model.ClassName)
                    .AppendLine("(__AuroraTypedDocumentConstruction _)");
                builder.AppendLine("        {");
                builder.AppendLine("        }");
                builder.AppendLine();
                builder.Append("        public static ").Append(model.ClassName)
                    .Append(" CreateTypedDocument() => new ").Append(model.ClassName)
                    .AppendLine("(default(__AuroraTypedDocumentConstruction));");
                builder.AppendLine();
            }

            builder.AppendLine("        public static void Register(ScriptObject target, bool writeable = false, bool enumerable = false)");
            builder.AppendLine("        {");
            builder.AppendLine("            target.Define(\"" + EscapeString(model.TypeName) +
                "\", Type, writeable, enumerable);");
            builder.AppendLine("        }");
            builder.AppendLine();
            if (model.HasNativeInstances)
            {
                builder.AppendLine("        " + model.OverrideAccessibility +
                    " override ScriptDatum TypeOfValue => NativeTypeName;");
                builder.AppendLine();
            }
            foreach (var export in model.Exports)
            {
                builder.AppendLine("        private static readonly BondingFunction " +
                    export.AdapterMethodName + "Bonding = new BondingFunction(" +
                    export.AdapterMethodName + ");");
            }
            foreach (var export in model.StaticExports)
            {
                builder.AppendLine("        private static readonly BondingFunction " +
                    export.AdapterMethodName + "Bonding = new BondingFunction(" +
                    export.AdapterMethodName + ");");
            }
            if (model.Exports.Count != 0 || model.StaticExports.Count != 0)
            {
                builder.AppendLine();
            }
            if (model.HasNativeInstances)
            {
                AppendNativePropertyAccess(builder, model);
                AppendNativeEnumerator(builder, model);
            }

            foreach (var export in model.Exports)
            {
                builder.AppendLine("        public static void " + export.AdapterMethodName + "(");
                builder.AppendLine("            ScriptContext ctx,");
                builder.AppendLine("            ScriptObject thisObject,");
                builder.AppendLine("            Span<ScriptDatum> args,");
                builder.AppendLine("            ref ScriptDatum result)");
                builder.AppendLine("        {");
                builder.AppendLine("            if (thisObject is not " + model.ClassName + " self)");
                builder.AppendLine("            {");
                AppendFailureReturn(builder, export, indent: "                ");
                builder.AppendLine("            }");
                AppendParameterCoercion(builder, export);
                AppendCoreInvocation(builder, export);
                builder.AppendLine("        }");
                builder.AppendLine();
            }
            foreach (var export in model.StaticExports)
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

            AppendNativeConstructor(builder, model);
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendNativePropertyAccess(StringBuilder builder, NativeObjectModel model)
        {
            builder.AppendLine("        " + model.OverrideAccessibility +
                " override ScriptDatum GetPropertyDatum(ScriptContext ctx, string key)");
            builder.AppendLine("        {");
            if (model.Fields.Count != 0 || model.Exports.Count != 0)
            {
                builder.AppendLine("            switch (key)");
                builder.AppendLine("            {");
                foreach (var field in model.Fields)
                {
                    builder.Append("                case \"").Append(EscapeString(field.ScriptName)).AppendLine("\":");
                    builder.Append("                    return ");
                    builder.AppendLine(GetFieldReadExpression(field) + ";");
                }
                foreach (var export in model.Exports)
                {
                    builder.Append("                case \"").Append(EscapeString(export.ScriptName)).AppendLine("\":");
                    builder.AppendLine("                    return ScriptDatum.FromObject(" +
                        export.AdapterMethodName + "Bonding.Bind(this));");
                }
                builder.AppendLine("            }");
            }
            builder.AppendLine("            return base.GetPropertyDatum(ctx, key);");
            builder.AppendLine("        }");
            builder.AppendLine();

            builder.AppendLine("        " + model.OverrideAccessibility +
                " override void SetPropertyDatum(ScriptContext ctx, string key, ScriptDatum value)");
            builder.AppendLine("        {");
            var writable = model.Fields.Where(static field => !field.IsReadOnly).ToList();
            if (writable.Count != 0)
            {
                builder.AppendLine("            switch (key)");
                builder.AppendLine("            {");
                foreach (var field in writable)
                {
                    builder.Append("                case \"").Append(EscapeString(field.ScriptName)).AppendLine("\":");
                    AppendFieldWrite(builder, field);
                    builder.AppendLine("                    return;");
                }
                builder.AppendLine("            }");
            }
            foreach (var field in model.Fields.Where(static field => field.IsReadOnly))
            {
                builder.AppendLine("            if (key == \"" + EscapeString(field.ScriptName) + "\")");
                builder.AppendLine("            {");
                builder.AppendLine("                return;");
                builder.AppendLine("            }");
            }
            builder.AppendLine("            base.SetPropertyDatum(ctx, key, value);");
            builder.AppendLine("        }");
            builder.AppendLine();

            builder.AppendLine("        " + model.OverrideAccessibility +
                " override bool DeletePropertyValue(ScriptContext ctx, string key)");
            builder.AppendLine("        {");
            foreach (var field in model.Fields)
            {
                builder.AppendLine("            if (key == \"" + EscapeString(field.ScriptName) + "\")");
                builder.AppendLine("            {");
                builder.AppendLine("                return false;");
                builder.AppendLine("            }");
            }
            foreach (var export in model.Exports)
            {
                builder.AppendLine("            if (key == \"" + EscapeString(export.ScriptName) + "\")");
                builder.AppendLine("            {");
                builder.AppendLine("                return false;");
                builder.AppendLine("            }");
            }
            builder.AppendLine("            return base.DeletePropertyValue(ctx, key);");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        private static void AppendNativeEnumerator(StringBuilder builder, NativeObjectModel model)
        {
            if (model.Fields.Count == 0)
            {
                return;
            }

            builder.AppendLine("        public override ScriptEnumerator GetEnumerator()");
            builder.AppendLine("        {");
            builder.AppendLine("            var keys = new System.Collections.Generic.List<ScriptDatum>(" +
                model.Fields.Count + ");");
            foreach (var field in model.Fields)
            {
                builder.AppendLine("            keys.Add(ScriptDatum.FromString(\"" +
                    EscapeString(field.ScriptName) + "\"));");
            }
            builder.AppendLine("            var rest = base.GetEnumerator();");
            builder.AppendLine("            while (rest.NextValue(out var key))");
            builder.AppendLine("            {");
            builder.AppendLine("                keys.Add(key);");
            builder.AppendLine("            }");
            builder.AppendLine("            return new ScriptEnumerator(keys);");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        private static void AppendNativeConstructor(StringBuilder builder, NativeObjectModel model)
        {
            builder.AppendLine("        private sealed class NativeConstructor : ScriptType");
            builder.AppendLine("        {");
            builder.AppendLine("            public NativeConstructor() : base(\"" +
                EscapeString(model.TypeName) + "\", " +
                (model.Constructor != null ? "true" : "false") + ")");
            builder.AppendLine("            {");
            foreach (var constant in model.StaticConstants)
            {
                builder.Append("                Define(\"")
                    .Append(EscapeString(constant.ScriptName))
                    .Append("\", ScriptDatum.FromNumber(")
                    .Append(constant.FieldName)
                    .AppendLine("), writeable: false, enumerable: false);");
            }
            foreach (var export in model.StaticExports)
            {
                builder.Append("                Define(\"")
                    .Append(EscapeString(export.ScriptName))
                    .Append("\", ScriptDatum.FromBonding(")
                    .Append(export.AdapterMethodName)
                    .AppendLine("), writeable: false, enumerable: false);");
            }
            builder.AppendLine("                Frozen();");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)");
            builder.AppendLine("            {");
            if (model.Constructor == null)
            {
                builder.AppendLine("                throw new AuroraRuntimeException(\"Type '" +
                    EscapeString(model.TypeName) + "' is not constructible.\");");
            }
            else
            {
                var export = new ExportModel(
                    "new",
                    model.ClassName,
                    "Construct",
                    model.ClassName,
                    canDirectCall: false,
                    HostExportFailure.ReturnNull,
                    ReturnKind.Object,
                    model.Constructor.Parameters,
                    takesContext: false,
                    takesThisObject: false);
                AppendParameterCoercion(builder, export);
                builder.Append("                ScriptDatum.WriteObject(ref result, new ");
                builder.Append(model.ClassName);
                builder.Append('(');
                for (var i = 0; i < model.Constructor.Parameters.Count; i++)
                {
                    if (i != 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(model.Constructor.Parameters[i].VariableName);
                }
                builder.AppendLine("));");
            }
            builder.AppendLine("            }");
            builder.AppendLine("        }");
        }

        private static string GetFieldReadExpression(InstanceFieldModel field)
        {
            return field.Kind switch
            {
                ParameterKind.Number => "ScriptDatum.FromNumber(" + field.FieldName + ")",
                ParameterKind.Int32 => "ScriptDatum.FromNumber(" + field.FieldName + ")",
                ParameterKind.Boolean => "ScriptDatum.FromBoolean(" + field.FieldName + ")",
                ParameterKind.String => "ScriptDatum.FromString(" + field.FieldName + ")",
                _ => "ScriptDatum.Null"
            };
        }

        private static void AppendFieldWrite(StringBuilder builder, InstanceFieldModel field)
        {
            switch (field.Kind)
            {
                case ParameterKind.Number:
                    builder.AppendLine("                    if (ScriptDatum.TryToNumber(in value, out var " + field.FieldName + "Number))");
                    builder.AppendLine("                    {");
                    builder.AppendLine("                        " + field.FieldName + " = " + field.FieldName + "Number;");
                    builder.AppendLine("                    }");
                    break;
                case ParameterKind.Int32:
                    builder.AppendLine("                    if (ScriptDatum.TryToNumber(in value, out var " + field.FieldName + "Number))");
                    builder.AppendLine("                    {");
                    builder.AppendLine("                        " + field.FieldName + " = (int)" + field.FieldName + "Number;");
                    builder.AppendLine("                    }");
                    break;
                case ParameterKind.Boolean:
                    builder.AppendLine("                    " + field.FieldName + " = value.Kind == ValueKind.Boolean");
                    builder.AppendLine("                        ? value.Boolean");
                    builder.AppendLine("                        : ScriptDatum.IsTrue(value);");
                    break;
                case ParameterKind.String:
                    builder.AppendLine("                    " + field.FieldName + " = ScriptDatum.ToString(value);");
                    break;
            }
        }

        private sealed class NativeObjectModel
        {
            public NativeObjectModel(
                string namespaceName,
                string className,
                string typeDisplayName,
                bool isPublic,
                string constructorAccessibility,
                bool generateConstructor,
                bool generateTypedDocumentFactory,
                string typeName,
                string overrideAccessibility,
                IReadOnlyList<ExportModel> exports,
                IReadOnlyList<InstanceFieldModel> fields,
                IReadOnlyList<ExportModel> staticExports,
                IReadOnlyList<ConstantModel> staticConstants,
                ConstructorModel? constructor,
                bool hasNativeInstances,
                IReadOnlyList<Diagnostic> diagnostics)
            {
                Namespace = namespaceName;
                ClassName = className;
                TypeDisplayName = typeDisplayName;
                IsPublic = isPublic;
                ConstructorAccessibility = constructorAccessibility;
                GenerateConstructor = generateConstructor;
                GenerateTypedDocumentFactory = generateTypedDocumentFactory;
                TypeName = typeName;
                OverrideAccessibility = overrideAccessibility;
                Exports = exports;
                Fields = fields;
                StaticExports = staticExports;
                StaticConstants = staticConstants;
                Constructor = constructor;
                HasNativeInstances = hasNativeInstances;
                Diagnostics = diagnostics;
            }

            public string Namespace { get; }
            public string ClassName { get; }
            public string TypeDisplayName { get; }
            public bool IsPublic { get; }
            public string ConstructorAccessibility { get; }
            public bool GenerateConstructor { get; }
            public bool GenerateTypedDocumentFactory { get; }
            public string TypeName { get; }
            public string OverrideAccessibility { get; }
            public IReadOnlyList<ExportModel> Exports { get; }
            public IReadOnlyList<InstanceFieldModel> Fields { get; }
            public IReadOnlyList<ExportModel> StaticExports { get; }
            public IReadOnlyList<ConstantModel> StaticConstants { get; }
            public ConstructorModel? Constructor { get; }
            public bool HasNativeInstances { get; }
            public IReadOnlyList<Diagnostic> Diagnostics { get; }
        }

        private sealed class InstanceFieldModel
        {
            public InstanceFieldModel(
                string scriptName,
                string fieldName,
                ParameterKind kind,
                bool isReadOnly)
            {
                ScriptName = scriptName;
                FieldName = fieldName;
                Kind = kind;
                IsReadOnly = isReadOnly;
            }

            public string ScriptName { get; }
            public string FieldName { get; }
            public ParameterKind Kind { get; }
            public bool IsReadOnly { get; }
        }

        private sealed class ConstructorModel
        {
            public ConstructorModel(IReadOnlyList<ParameterModel> parameters)
            {
                Parameters = parameters;
            }

            public IReadOnlyList<ParameterModel> Parameters { get; }
        }
    }
}
