using AuroraScript.Compiler.Ast.Expressions;

namespace AuroraScript.LanguageServices.Internal;

internal sealed class AstQueryContext
{
    public Expression? Expression { get; init; }
    public NameExpression? Name { get; init; }
    public GetPropertyExpression? PropertyAccess { get; init; }
    public FunctionCallExpression? Call { get; init; }
    public bool IsOnPropertyName { get; init; }
    public bool IsAfterMemberAccessDot { get; init; }
}
