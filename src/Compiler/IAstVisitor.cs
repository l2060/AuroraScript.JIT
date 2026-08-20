using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;


namespace AuroraScript.Compiler
{
    internal abstract class IAstVisitor
    {
        public void AcceptImportDeclaration(ImportDeclaration node)
        {
            BeforeVisitNode(node);
            VisitImportDeclaration(node);
            AfterVisitNode(node);
        }


        public void AcceptModule(ModuleDeclaration node)
        {
            BeforeVisitNode(node);
            VisitModule(node);
            AfterVisitNode(node);
        }

        public void AcceptFunction(FunctionDeclaration node)
        {
            BeforeVisitNode(node);
            VisitFunction(node);
            AfterVisitNode(node);
        }


        public void AcceptLambdaExpression(LambdaExpression node)
        {
            BeforeVisitNode(node);
            VisitLambdaExpression(node);
            AfterVisitNode(node);
        }


        public void AcceptBlock(BlockStatement node)
        {
            BeforeVisitNode(node);
            VisitBlock(node);
            AfterVisitNode(node);
        }


        public void AcceptName(NameExpression node)
        {
            BeforeVisitNode(node);
            VisitName(node);
            AfterVisitNode(node);
        }


        public void AcceptVarDeclaration(VariableDeclaration node)
        {
            BeforeVisitNode(node);
            VisitVarDeclaration(node);
            AfterVisitNode(node);
        }


        public void AcceptArrayDestructuringPattern(ArrayDestructuringPattern node)
        {
            BeforeVisitNode(node);
            VisitArrayDestructuringPattern(node);
            AfterVisitNode(node);
        }

        public void AcceptObjectDestructuringPattern(ObjectDestructuringPattern node)
        {
            BeforeVisitNode(node);
            VisitObjectDestructuringPattern(node);
            AfterVisitNode(node);
        }

        public void AcceptIfStatement(IfStatement node)
        {
            BeforeVisitNode(node);
            VisitIfStatement(node);
            AfterVisitNode(node);
        }

        public void AcceptWhileStatement(WhileStatement node)
        {
            BeforeVisitNode(node);
            VisitWhileStatement(node);
            AfterVisitNode(node);
        }

        public void AcceptForStatement(ForStatement node)
        {
            BeforeVisitNode(node);
            VisitForStatement(node);
            AfterVisitNode(node);
        }

        public void AcceptForInStatement(ForInStatement node)
        {
            BeforeVisitNode(node);
            VisitForInStatement(node);
            AfterVisitNode(node);
        }

        public void AcceptInExpression(InExpression node)
        {
            BeforeVisitNode(node);
            VisitInExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptIncludedExpression(IncludedExpression node)
        {
            BeforeVisitNode(node);
            VisitIncludedExpression(node);
            AfterVisitNode(node);
        }





        public void AcceptNewExpression(NewExpression node)
        {
            BeforeVisitNode(node);
            VisitNewExpression(node);
            AfterVisitNode(node);
        }



        public void AcceptTryStatement(TryStatement node)
        {
            BeforeVisitNode(node);
            VisitTryStatement(node);
            AfterVisitNode(node);
        }

        public void AcceptThrowStatement(ThrowStatement node)
        {
            BeforeVisitNode(node);
            VisitThrowStatement(node);
            AfterVisitNode(node);
        }


        public void AcceptReturnStatement(ReturnStatement node)
        {
            BeforeVisitNode(node);
            VisitReturnStatement(node);
            AfterVisitNode(node);
        }

        public void AcceptExpressionStatement(ExpressionStatement node)
        {
            BeforeVisitNode(node);
            VisitExpressionStatement(node);
            AfterVisitNode(node);
        }

        public void AcceptDeleteStatement(DeleteStatement node)
        {
            BeforeVisitNode(node);
            VisitDeleteStatement(node);
            AfterVisitNode(node);
        }

        public void AcceptAssignmentExpression(AssignmentExpression node)
        {
            BeforeVisitNode(node);
            VisitAssignmentExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptCompoundExpression(CompoundExpression node)
        {
            BeforeVisitNode(node);
            VisitCompoundExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptBinaryExpression(BinaryExpression node)
        {
            BeforeVisitNode(node);
            VisitBinaryExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptUnaryExpression(UnaryExpression node)
        {
            BeforeVisitNode(node);
            VisitUnaryExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptCallExpression(FunctionCallExpression node)
        {
            BeforeVisitNode(node);
            VisitCallExpression(node);
            AfterVisitNode(node);
        }


        public void AcceptLiteralExpression(LiteralExpression node)
        {
            BeforeVisitNode(node);
            VisitLiteralExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptTypedDocumentExpression(TypedDocumentExpression node)
        {
            BeforeVisitNode(node);
            VisitTypedDocumentExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptTemplateStringExpression(TemplateStringExpression node)
        {
            BeforeVisitNode(node);
            VisitTemplateStringExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptGroupingExpression(GroupExpression node)
        {
            BeforeVisitNode(node);
            VisitGroupingExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptArrayExpression(ArrayLiteralExpression node)
        {
            BeforeVisitNode(node);
            VisitArrayExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptGetElementExpression(GetElementExpression node)
        {
            BeforeVisitNode(node);
            VisitGetElementExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptSetElementExpression(SetElementExpression node)
        {
            BeforeVisitNode(node);
            VisitSetElementExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptGetPropertyExpression(GetPropertyExpression node)
        {
            BeforeVisitNode(node);
            VisitGetPropertyExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptSetPropertyExpression(SetPropertyExpression node)
        {
            BeforeVisitNode(node);
            VisitSetPropertyExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptMapExpression(MapExpression node)
        {
            BeforeVisitNode(node);
            VisitMapExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptSpreadExpression(SpreadExpression node)
        {
            BeforeVisitNode(node);
            VisitSpreadExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptBreakExpression(BreakStatement node)
        {
            BeforeVisitNode(node);
            VisitBreakExpression(node);
            AfterVisitNode(node);
        }


        public void AcceptDebuggerExpression(DebuggerStatement node)
        {
            BeforeVisitNode(node);
            VisitDebuggerExpression(node);
            AfterVisitNode(node);
        }



        public void AcceptContinueExpression(ContinueStatement node)
        {
            BeforeVisitNode(node);
            VisitContinueExpression(node);
            AfterVisitNode(node);
        }

        public void AcceptParameterDeclaration(ParameterDeclaration node)
        {
            BeforeVisitNode(node);
            VisitParameterDeclaration(node);
            AfterVisitNode(node);
        }



        public void AcceptEnumDeclaration(EnumDeclaration node)
        {
            BeforeVisitNode(node);
            VisitEnumDeclaration(node);
            AfterVisitNode(node);
        }



        protected virtual void BeforeVisitNode(AstNode node)
        {

        }

        protected virtual void AfterVisitNode(AstNode node)
        {

        }


        protected virtual void VisitImportDeclaration(ImportDeclaration node)
        {
            if (node.Include && node.Module != null)
            {
                for (int i = 0; i < node.Module.Statements.Count; i++)
                {
                    node.Module.Statements[i].Accept(this);
                }
                for (int i = 0; i < node.Module.Functions.Count; i++)
                {
                    node.Module.Functions[i].Accept(this);
                }
            }
        }

        protected virtual void VisitModule(ModuleDeclaration node)
        {
            for (int i = 0; i < node.Imports.Count; i++)
            {
                node.Imports[i].Accept(this);
            }
            VisitBlock(node);
        }

        protected virtual void VisitFunction(FunctionDeclaration node)
        {
            for (int i = 0; i < node.Parameters.Count; i++)
            {
                node.Parameters[i].Accept(this);
            }
            node.Body?.Accept(this);
        }


        protected virtual void VisitLambdaExpression(LambdaExpression node)
        {
            node.Function.Accept(this);
        }


        protected virtual void VisitBlock(BlockStatement node)
        {
            for (int i = 0; i < node.Statements.Count; i++)
            {
                node.Statements[i].Accept(this);
            }
            for (int i = 0; i < node.Functions.Count; i++)
            {
                node.Functions[i].Accept(this);
            }
        }


        protected virtual void VisitName(NameExpression node)
        {

        }


        protected virtual void VisitVarDeclaration(VariableDeclaration node)
        {
            node.Initializer?.Accept(this);
            node.Pattern?.Accept(this);
        }

        protected virtual void VisitArrayDestructuringPattern(ArrayDestructuringPattern node)
        {

        }

        protected virtual void VisitObjectDestructuringPattern(ObjectDestructuringPattern node)
        {

        }


        protected virtual void VisitIfStatement(IfStatement node)
        {
            node.Condition.Accept(this);
            node.Body?.Accept(this);
            node.Else?.Accept(this);
        }
        protected virtual void VisitWhileStatement(WhileStatement node)
        {
            node.Condition.Accept(this);
            node.Body.Accept(this);
        }
        protected virtual void VisitForStatement(ForStatement node)
        {
            node.Initializer?.Accept(this);
            node.Condition?.Accept(this);
            node.Body.Accept(this);
            node.Incrementor?.Accept(this);
        }



        protected virtual void VisitForInStatement(ForInStatement node)
        {
            node.Initializer?.Accept(this);
            node.Body.Accept(this);
            node.Iterator?.Accept(this);
        }

        protected virtual void VisitInExpression(InExpression node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
        }

        protected virtual void VisitIncludedExpression(IncludedExpression node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
        }


        protected virtual void VisitNewExpression(NewExpression node)
        {
            node.Expression.Accept(this);
        }


        protected virtual void VisitEnumDeclaration(EnumDeclaration node)
        {

        }



        protected virtual void VisitTryStatement(TryStatement node)
        {
            node.Body?.Accept(this);
            node.CatchBody?.Accept(this);
            node.FinallyBody?.Accept(this);
        }

        protected virtual void VisitThrowStatement(ThrowStatement node)
        {
            node.Expression?.Accept(this);
        }


        protected virtual void VisitReturnStatement(ReturnStatement node)
        {
            node.Expression?.Accept(this);
        }

        protected virtual void VisitExpressionStatement(ExpressionStatement node)
        {
            node.Expression?.Accept(this);
        }


        protected virtual void VisitDeleteStatement(DeleteStatement node)
        {
            node.Expression?.Accept(this);
        }


        protected virtual void VisitAssignmentExpression(AssignmentExpression node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
        }


        protected virtual void VisitCompoundExpression(CompoundExpression node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
        }

        protected virtual void VisitBinaryExpression(BinaryExpression node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
        }

        protected virtual void VisitUnaryExpression(UnaryExpression node)
        {
            node.Expression.Accept(this);
        }

        protected virtual void VisitCallExpression(FunctionCallExpression node)
        {
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                node.Arguments[i].Accept(this);
            }
            node.Target.Accept(this);
        }


        protected virtual void VisitLiteralExpression(LiteralExpression node)
        {

        }

        protected virtual void VisitTypedDocumentExpression(TypedDocumentExpression node)
        {
            node.Value?.Accept(this);
        }

        protected virtual void VisitTemplateStringExpression(TemplateStringExpression node)
        {
            for (int i = 0; i < node.PartCount; i++)
            {
                var part = node.Parts[i];
                if (!part.IsLiteral)
                {
                    part.Expression.Accept(this);
                }
            }
        }

        protected virtual void VisitGroupingExpression(GroupExpression node)
        {
            for (int i = 0; i < node.Expressions.Count; i++)
            {
                node.Expressions[i].Accept(this);
            }
        }

        protected virtual void VisitArrayExpression(ArrayLiteralExpression node)
        {
            for (int i = 0; i < node.Elements.Count; i++)
            {
                node.Elements[i].Accept(this);
            }
        }

        protected virtual void VisitGetElementExpression(GetElementExpression node)
        {
            node.Object.Accept(this);
            node.Index.Accept(this);
        }
        protected virtual void VisitSetElementExpression(SetElementExpression node)
        {
            node.Value.Accept(this);
            node.Object.Accept(this);
            node.Index.Accept(this);



        }
        protected virtual void VisitGetPropertyExpression(GetPropertyExpression node)
        {
            node.Object.Accept(this);
            node.Property.Accept(this);
        }

        protected virtual void VisitSetPropertyExpression(SetPropertyExpression node)
        {
            node.Object.Accept(this);
            node.Value.Accept(this);
            node.Property.Accept(this);
        }


        protected virtual void VisitMapExpression(MapExpression node)
        {
            for (int i = 0; i < node.Entries.Count; i++)
            {
                var entry = node.Entries[i];
                if (entry is MapKeyValueExpression property)
                {
                    property.Value.Accept(this);
                }
                else
                {
                    entry.Accept(this);
                }
            }
        }

        protected virtual void VisitSpreadExpression(SpreadExpression node)
        {
            node.Expression.Accept(this);
        }
        protected virtual void VisitBreakExpression(BreakStatement node)
        {

        }

        protected virtual void VisitDebuggerExpression(DebuggerStatement node)
        {

        }


        protected virtual void VisitContinueExpression(ContinueStatement node)
        {

        }

        protected virtual void VisitParameterDeclaration(ParameterDeclaration node)
        {
            node.Initializer?.Accept(this);
        }


    }
}
