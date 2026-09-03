using AuroraScript.Compiler;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal sealed class ExpressionTypeResolver
{
    private readonly BuiltinApiCatalog? _builtins;
    private readonly AmbientContractCatalog? _ambient;
    private readonly ModuleDeclaration _module;
    private readonly AuroraWorkspaceIndex? _workspace;
    private readonly string _modulePath;
    private readonly AuroraLocalSymbolIndex _locals;

    public ExpressionTypeResolver(
        ModuleDeclaration module,
        BuiltinApiCatalog? builtins = null,
        AmbientContractCatalog? ambient = null,
        AuroraWorkspaceIndex? workspace = null,
        string? modulePath = null)
    {
        _module = module;
        _builtins = builtins;
        _ambient = ambient;
        _workspace = workspace;
        _modulePath = modulePath ?? string.Empty;
        _locals = AuroraLocalSymbolIndex.Build(
            new AuroraModuleIndex(_modulePath, string.Empty, module));
    }

    public bool IsLocalReference(NameExpression name)
    {
        return _locals.IsLocalReference(name);
    }

    public bool IsSourceDefinedReference(NameExpression name)
    {
        if (_locals.IsLocalReference(name) ||
            _module.TryGetContext(name.Identifier.Value, out _) ||
            TryFindModuleVariable(name.Identifier.Value, out _) ||
            TryFindFunction(_module, name.Identifier.Value, out _) ||
            _module.TryGetType(name.Identifier.Value, out _))
        {
            return true;
        }

        return false;
    }

    public bool TryResolve(
        Expression expression,
        out TypeReference reference,
        out ModuleDeclaration declaringModule,
        out string declaringPath)
    {
        return TryResolve(
            expression,
            new HashSet<Expression>(),
            out reference,
            out declaringModule,
            out declaringPath);
    }

    public bool TryResolveBuiltinPrototype(
        Expression expression,
        out string prototypeName)
    {
        prototypeName = string.Empty;
        if (_builtins == null ||
            !TryResolve(expression, out var reference, out _, out _))
        {
            return false;
        }

        return TryNormalizeBuiltinType(reference.DisplayName, out prototypeName);
    }

    private bool TryResolve(
        Expression expression,
        HashSet<Expression> visited,
        out TypeReference reference,
        out ModuleDeclaration declaringModule,
        out string declaringPath)
    {
        reference = null!;
        declaringModule = _module;
        declaringPath = _modulePath;
        if (expression == null || !visited.Add(expression))
        {
            return false;
        }

        switch (expression)
        {
            case LiteralExpression { Token: StringToken }:
                reference = CreateTypeReference("String");
                return true;
            case LiteralExpression { Token: RegexToken }:
                reference = CreateTypeReference("Regex");
                return true;
            case LiteralExpression { Token: NumberToken }:
                reference = CreateTypeReference("Number");
                return true;
            case LiteralExpression { Token: BooleanToken }:
                reference = CreateTypeReference("Boolean");
                return true;
            case ArrayLiteralExpression:
                reference = CreateTypeReference("Array");
                return true;
            case TemplateStringExpression:
                reference = CreateTypeReference("String");
                return true;
            case CheckExpression check when check.AssertedType != null:
                reference = check.AssertedType;
                return true;
            case NewExpression { Expression.Target: NameExpression constructor }:
                reference = new TypeReference(constructor.Identifier);
                return true;
            case FunctionCallExpression call:
                return TryResolveCall(
                    call,
                    visited,
                    out reference,
                    out declaringModule,
                    out declaringPath);
            case GetPropertyExpression property:
                return TryResolveProperty(
                    property,
                    visited,
                    out reference,
                    out declaringModule,
                    out declaringPath);
            case NameExpression name:
                return TryResolveName(
                    name,
                    visited,
                    out reference,
                    out declaringModule,
                    out declaringPath);
            default:
                return false;
        }
    }

    private bool TryResolveName(
        NameExpression name,
        HashSet<Expression> visited,
        out TypeReference reference,
        out ModuleDeclaration declaringModule,
        out string declaringPath)
    {
        reference = null!;
        declaringModule = _module;
        declaringPath = _modulePath;
        if (!_locals.TryGetDeclaration(name, out var declaration))
        {
            if (_module.TryGetContext(name.Identifier.Value, out var context) &&
                context.DeclaredType != null)
            {
                reference = context.DeclaredType;
                return true;
            }

            VariableDeclaration moduleVariable;
            return TryFindModuleVariable(name.Identifier.Value, out moduleVariable) &&
                moduleVariable.Initializer != null &&
                TryResolve(
                    moduleVariable.Initializer,
                    visited,
                    out reference,
                    out declaringModule,
                    out declaringPath);
        }

        if (declaration is not VariableDeclaration variable)
        {
            return false;
        }

        if (variable is ParameterDeclaration parameter &&
            parameter.DeclaredType != null)
        {
            reference = parameter.DeclaredType;
            return true;
        }

        return variable.Initializer != null &&
            TryResolve(
                variable.Initializer,
                visited,
                out reference,
                out declaringModule,
                out declaringPath);
    }

    private bool TryResolveCall(
        FunctionCallExpression call,
        HashSet<Expression> visited,
        out TypeReference reference,
        out ModuleDeclaration declaringModule,
        out string declaringPath)
    {
        reference = null!;
        declaringModule = _module;
        declaringPath = _modulePath;

        if (call.Target is NameExpression callable)
        {
            if (_locals.TryGetDeclaration(callable, out var declaration) &&
                declaration is FunctionDeclaration localFunction &&
                localFunction.ReturnType != null)
            {
                reference = localFunction.ReturnType;
                return true;
            }

            if (!_locals.IsLocalReference(callable) &&
                TryFindFunction(_module, callable.Identifier.Value, out var function) &&
                function.ReturnType != null)
            {
                reference = function.ReturnType;
                return true;
            }

            if (_builtins != null &&
                !_locals.IsLocalReference(callable) &&
                _builtins.TryGetGlobal(callable.Identifier.Value, out var global))
            {
                var type = global.Kind == BuiltinApiKind.Constructor &&
                    global.Constructors.Count != 0
                    ? global.Constructors[0].ReturnType
                    : global.Name;
                reference = CreateTypeReference(type);
                return true;
            }
        }

        if (call.Target is not GetPropertyExpression
            {
                Object: var receiver,
                Property: NameExpression property
            })
        {
            return false;
        }

        if (receiver is NameExpression owner &&
            !IsSourceDefinedReference(owner) &&
            TryResolveStaticCall(
                owner.Identifier.Value,
                property.Identifier.Value,
                out reference,
                out declaringModule,
                out declaringPath))
        {
            return true;
        }

        if (!TryResolve(
                receiver,
                visited,
                out var receiverReference,
                out _,
                out _))
        {
            return false;
        }

        if (_builtins != null &&
            TryNormalizeBuiltinType(receiverReference.DisplayName, out var prototypeName) &&
            _builtins.TryGetPrototypeMember(
                prototypeName,
                property.Identifier.Value,
                out var prototypeMember))
        {
            reference = CreateTypeReference(prototypeMember.ReturnType);
            return true;
        }

        if (_ambient != null &&
            _ambient.TryGetMember(
                receiverReference.Name,
                property.Identifier.Value,
                instanceMembers: true,
                out var ambientMember) &&
            ambientMember.ReturnType != null)
        {
            reference = ambientMember.ReturnType;
            return true;
        }

        return false;
    }

    private bool TryResolveStaticCall(
        string ownerName,
        string memberName,
        out TypeReference reference,
        out ModuleDeclaration declaringModule,
        out string declaringPath)
    {
        reference = null!;
        declaringModule = _module;
        declaringPath = _modulePath;

        if (_builtins != null)
        {
            if (BuiltinModuleQuery.TryResolve(_builtins, _module, ownerName, out var module) &&
                module.TryGetMember(memberName, out var moduleMember))
            {
                reference = CreateTypeReference(moduleMember.ReturnType);
                return true;
            }

            if (_builtins.TryGetGlobalMember(ownerName, memberName, out var builtinMember))
            {
                reference = CreateTypeReference(builtinMember.ReturnType);
                return true;
            }
        }

        if (_ambient != null &&
            _ambient.TryGetMember(
                ownerName,
                memberName,
                instanceMembers: false,
                out var ambientMember) &&
            ambientMember.ReturnType != null)
        {
            reference = ambientMember.ReturnType;
            return true;
        }

        if (_workspace == null ||
            string.IsNullOrEmpty(_modulePath))
        {
            return false;
        }

        var indexed = _workspace.TryGetModule(_modulePath);
        if (indexed == null ||
            !indexed.ImportsByAlias.TryGetValue(ownerName, out var import))
        {
            return false;
        }

        var target = _workspace.TryGetModule(import.TargetPath);
        if (target == null ||
            !TryFindFunction(target.Module, memberName, out var imported) ||
            imported.Access != MemberAccess.Export ||
            imported.ReturnType == null)
        {
            return false;
        }

        reference = imported.ReturnType;
        declaringModule = target.Module;
        declaringPath = target.Path;
        return true;
    }

    private bool TryResolveProperty(
        GetPropertyExpression property,
        HashSet<Expression> visited,
        out TypeReference reference,
        out ModuleDeclaration declaringModule,
        out string declaringPath)
    {
        reference = null!;
        declaringModule = _module;
        declaringPath = _modulePath;
        if (property.Property is not NameExpression member ||
            !TryResolve(
                property.Object,
                visited,
                out var ownerReference,
                out var ownerModule,
                out var ownerPath))
        {
            return false;
        }

        if (_builtins != null &&
            TryNormalizeBuiltinType(ownerReference.DisplayName, out var prototypeName) &&
            _builtins.TryGetPrototypeMember(
                prototypeName,
                member.Identifier.Value,
                out var prototypeMember))
        {
            reference = CreateTypeReference(prototypeMember.ReturnType);
            return true;
        }

        if (_ambient != null &&
            _ambient.TryGetMember(
                ownerReference.Name,
                member.Identifier.Value,
                instanceMembers: true,
                out var ambientMember) &&
            ambientMember.ReturnType != null)
        {
            reference = ambientMember.ReturnType;
            return true;
        }

        if (!TryResolveShape(
                ownerReference,
                ownerModule,
                ownerPath,
                out var shape,
                out var shapeModule,
                out var shapePath))
        {
            return false;
        }

        for (var i = 0; i < shape.Fields.Count; i++)
        {
            if (!StringComparer.Ordinal.Equals(
                    shape.Fields[i].Name.Value,
                    member.Identifier.Value))
            {
                continue;
            }

            reference = shape.Fields[i].Type;
            declaringModule = shapeModule;
            declaringPath = shapePath;
            return true;
        }

        return false;
    }

    private bool TryResolveShape(
        TypeReference reference,
        ModuleDeclaration module,
        string modulePath,
        out TypeDeclaration shape,
        out ModuleDeclaration declaringModule,
        out string declaringPath)
    {
        shape = null!;
        declaringModule = module;
        declaringPath = modulePath;
        if (reference.Qualifier == null)
        {
            return module.TryGetType(reference.Name, out shape);
        }

        if (_workspace == null || string.IsNullOrEmpty(modulePath))
        {
            return false;
        }

        var indexed = _workspace.TryGetModule(modulePath);
        if (indexed == null ||
            !indexed.ImportsByAlias.TryGetValue(reference.QualifierName, out var import))
        {
            return false;
        }

        var target = _workspace.TryGetModule(import.TargetPath);
        if (target == null ||
            !target.Module.TryGetType(reference.Name, out shape) ||
            shape.Access != MemberAccess.Export)
        {
            shape = null!;
            return false;
        }

        declaringModule = target.Module;
        declaringPath = target.Path;
        return true;
    }

    private bool TryResolveBuiltinPrototype(
        Expression expression,
        HashSet<Expression> visited,
        out string prototypeName)
    {
        prototypeName = string.Empty;
        return _builtins != null &&
            TryResolve(expression, visited, out var reference, out _, out _) &&
            TryNormalizeBuiltinType(reference.DisplayName, out prototypeName);
    }

    private bool TryNormalizeBuiltinType(string rawType, out string prototypeName)
    {
        prototypeName = string.Empty;
        var parts = rawType.Split(
            '|',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string? resolved = null;
        for (var i = 0; i < parts.Length; i++)
        {
            if (IsNullType(parts[i]))
            {
                continue;
            }

            var normalized = NormalizeSingleType(parts[i]);
            if (resolved != null &&
                !string.Equals(resolved, normalized, StringComparison.Ordinal))
            {
                return false;
            }

            resolved = normalized;
        }

        if (resolved == null || _builtins == null ||
            !_builtins.TryGetGlobal(resolved, out _))
        {
            return false;
        }

        prototypeName = resolved;
        return true;
    }

    private static bool TryFindFunction(
        ModuleDeclaration module,
        string name,
        out FunctionDeclaration function)
    {
        for (var i = 0; i < module.Functions.Count; i++)
        {
            var candidate = module.Functions[i];
            if (candidate.Name != null &&
                StringComparer.Ordinal.Equals(candidate.Name.Value, name))
            {
                function = candidate;
                return true;
            }
        }

        function = null!;
        return false;
    }

    private bool TryFindModuleVariable(
        string name,
        out VariableDeclaration variable)
    {
        for (var i = 0; i < _module.Statements.Count; i++)
        {
            if (_module.Statements[i] is VariableDeclaration candidate &&
                candidate.Name != null &&
                StringComparer.Ordinal.Equals(candidate.Name.Value, name))
            {
                variable = candidate;
                return true;
            }
        }

        variable = null!;
        return false;
    }

    private static TypeReference CreateTypeReference(string typeName)
    {
        var token = new IdentifierToken
        {
            Value = NormalizeSingleType(typeName)
        };
        return new TypeReference(token);
    }

    private static string NormalizeSingleType(string rawType)
    {
        var type = rawType.Trim();
        if (type.EndsWith("[]", StringComparison.Ordinal))
        {
            return "Array";
        }

        return type.ToLowerInvariant() switch
        {
            "array" => "Array",
            "boolean" or "bool" => "Boolean",
            "date" => "Date",
            "function" or "func" => "Function",
            "number" => "Number",
            "object" => "Object",
            "regex" or "regexp" => "Regex",
            "string" => "String",
            _ => type
        };
    }

    private static bool IsNullType(string type)
    {
        return string.Equals(type, "null", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type, "undefined", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type, "void", StringComparison.OrdinalIgnoreCase);
    }
}
