using AuroraScript.Core;
using AuroraScript.Runtime;


namespace AuroraScript.Compiler.Ast
{

    internal class EvaluationContext
    {
        private readonly CodeScope _scope;
        public EvaluationContext(CodeScope scope)
        {
            _scope = scope;
        }


        internal AstNode ResolveSymbol(string name)
        {
            if (_scope.Resolve(name, out var node) && node.VariableNode != null)
            {
                return node.VariableNode;
            }
            return null;
        }

    }





    internal interface IConstEvaluable
    {
        bool TryEvalConst(EvaluationContext ctx, ref ScriptDatum value);
    }

}
