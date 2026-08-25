using AuroraScript.Compiler;
using AuroraScript.Compiler.Ast.Expressions;

namespace AuroraScript.LanguageServices.Internal;

internal sealed class AstQueryContext
{
    public Expression? Expression { get; init; }

    /// <summary>
    /// Type name asserted by a typed parameter or an <c>as</c> expression.
    /// </summary>
    public Token? TypeReference { get; init; }

    public Token? TypeQualifier { get; init; }

    public NameExpression? Name { get; init; }
    public GetPropertyExpression? PropertyAccess { get; init; }
    public FunctionCallExpression? Call { get; init; }
    public NewExpression? NewExpression { get; init; }
    public bool IsOnPropertyOwner { get; init; }
    public bool IsOnPropertyName { get; init; }
    public bool IsAfterMemberAccessDot { get; init; }
}
