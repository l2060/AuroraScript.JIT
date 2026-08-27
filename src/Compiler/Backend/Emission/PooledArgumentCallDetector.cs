using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Traversal;
using System;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal static class PooledArgumentCallDetector
    {
        public static bool Contains(
            AstNode node,
            Func<FunctionCallExpression, bool> callUsesBuffer = null,
            Func<NewExpression, bool> constructorUsesBuffer = null)
        {
            var visitor = new Visitor(callUsesBuffer, constructorUsesBuffer);
            visitor.VisitRoot(node);
            return visitor.Found;
        }

        private struct Visitor : IAstChildVisitor
        {
            private readonly Func<FunctionCallExpression, bool> _callUsesBuffer;
            private readonly Func<NewExpression, bool> _constructorUsesBuffer;

            public Visitor(
                Func<FunctionCallExpression, bool> callUsesBuffer,
                Func<NewExpression, bool> constructorUsesBuffer)
            {
                _callUsesBuffer = callUsesBuffer;
                _constructorUsesBuffer = constructorUsesBuffer;
                Found = false;
            }

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

                if (node is NewExpression { Expression: FunctionCallExpression constructor } @new &&
                    (_constructorUsesBuffer?.Invoke(@new) ??
                        (constructor.Arguments.Count > 2 || HasSpread(constructor))))
                {
                    Found = true;
                    return;
                }

                if (node is FunctionCallExpression call &&
                    node.Parent is not NewExpression &&
                    (_callUsesBuffer?.Invoke(call) ??
                        (call.Arguments.Count > 7 || HasSpread(call))))
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
