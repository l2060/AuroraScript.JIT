using System.Collections.Generic;


namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class LambdaExpression : Expression
    {

        public LambdaExpression(FunctionDeclaration function)
        {
            Function = function;
            Function.Parent = this;
        }

        public readonly FunctionDeclaration Function;

        public override IEnumerable<AstNode> ChildNodes
        {
            get
            {
                if (Function != null) yield return Function;
            }
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptLambdaExpression(this);
        }
    }
}