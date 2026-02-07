using AuroraScript.Runtime;


namespace AuroraScript.Compiler.Ast.Expressions
{
    internal abstract class Expression : AstNode, IConstEvaluable
    {
        public virtual bool TryEvalConst(EvaluationContext ctx, ref ScriptDatum value)
        {
            return false;
        }
    }

    internal class ExpressionStack : Expression
    {
        public override void Accept(IAstVisitor visitor)
        {

        }
    }

}