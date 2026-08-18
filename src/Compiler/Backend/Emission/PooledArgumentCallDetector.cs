using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Traversal;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal static class PooledArgumentCallDetector
    {
        public static bool Contains(AstNode node)
        {
            var visitor = new Visitor();
            visitor.VisitRoot(node);
            return visitor.Found;
        }

        private struct Visitor : IAstChildVisitor
        {
            public bool Found;

            public void VisitRoot(AstNode node)
            {
                VisitNode(node, isRoot: true);
            }

            public void Visit(AstNode node)
            {
                VisitNode(node, isRoot: false);
            }

            private void VisitNode(AstNode node, bool isRoot)
            {
                if (Found || node == null)
                {
                    return;
                }

                if (!isRoot && node is FunctionDeclaration or LambdaExpression)
                {
                    return;
                }

                if (node is NewExpression { Expression: FunctionCallExpression constructor } &&
                    (constructor.Arguments.Count > 2 || HasSpread(constructor)))
                {
                    Found = true;
                    return;
                }

                if (node is FunctionCallExpression call &&
                    (call.Arguments.Count > 7 || HasSpread(call)))
                {
                    Found = true;
                    return;
                }

                AstTraversal.VisitChildren(node, ref this);
            }

            private static bool HasSpread(FunctionCallExpression call)
            {
                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    if (call.Arguments[i] is SpreadExpression) return true;
                }
                return false;
            }
        }
    }
}
