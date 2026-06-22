using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Core;

namespace AuroraScript.Compiler.Emits
{
    internal class DeclarationVisitor : IAstVisitor
    {
        private CodeScope _scope;
        public DeclarationVisitor(CodeScope scope) => _scope = scope;

        public void Reset(CodeScope scope) => _scope = scope;

        public void VisitFunctionBody(FunctionDeclaration node)
        {
            foreach (var p in node.Parameters) p.Accept(this);
            node.Body?.Accept(this);
        }

        private DeclareType GetDeclType() => _scope.ScopeType == Core.ScopeType.Module ? DeclareType.Property : DeclareType.Variable;

        protected override void VisitParameterDeclaration(ParameterDeclaration node)
        {
            _scope.Declare(node.Name, DeclareType.Variable);
        }

        protected override void VisitVarDeclaration(VariableDeclaration node)
        {
            if (node.Name != null) _scope.Declare(node.Name, GetDeclType(), MemberAccess.Internal, node);
            if (node.Pattern != null) node.Pattern.Accept(this);
        }

        protected override void VisitArrayDestructuringPattern(ArrayDestructuringPattern node)
        {
            var type = GetDeclType();
            var val = node.Parent as VariableDeclaration;
            foreach (var item in node.Elements)
            {
                if (item is NameExpression name) _scope.Declare(name.Identifier, type, val.Access, val);
                else if (item is SpreadExpression spread && spread.Expression is NameExpression sn) _scope.Declare(sn.Identifier, type, val.Access, val);
                else item?.Accept(this);
            }
        }

        protected override void VisitObjectDestructuringPattern(ObjectDestructuringPattern node)
        {
            var type = GetDeclType();
            var val = node.Parent as VariableDeclaration;
            foreach (var prop in node.Properties)
            {
                _scope.Declare(prop, type, val.Access, val);
            }
        }

        protected override void VisitFunction(FunctionDeclaration node)
        {
            if (node.Flags == FunctionFlags.Declare) return;
            if (node.Name != null) _scope.Declare(node.Name, GetDeclType());
        }

        protected override void VisitLambdaExpression(LambdaExpression node) { }
    }
}
