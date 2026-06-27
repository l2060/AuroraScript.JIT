using AuroraScript.Runtime;


namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class NameExpression : Expression
    {

        public NameExpression(Token identifier)
        {
            Identifier = identifier;
        }

        /// <summary>
        /// member name
        /// </summary>
        public readonly Token Identifier;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptName(this);
        }

        public override bool TryEvalConst(EvaluationContext ctx, ref ScriptDatum value)
        {
            var symbol = ctx.ResolveSymbol(this.Identifier.Value);
            if (symbol is VariableDeclaration variableDeclaration && variableDeclaration.IsConst && variableDeclaration.Initializer is LiteralExpression literal)
            {
                return literal.TryEvalConst(ctx, ref value);
            }
            // not a constant
            return false;
        }
    }
}