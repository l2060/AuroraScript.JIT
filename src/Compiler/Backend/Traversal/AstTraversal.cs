using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;

namespace AuroraScript.Compiler.Backend.Traversal
{
    internal interface IAstChildVisitor
    {
        void Visit(AstNode node);
    }

    internal static class AstTraversal
    {
        public static void VisitChildren<TVisitor>(AstNode node, ref TVisitor visitor)
            where TVisitor : struct, IAstChildVisitor
        {
            if (node == null)
            {
                return;
            }

            switch (node)
            {
                case ModuleDeclaration module:
                    VisitImports(module, ref visitor);
                    VisitBlockChildren(module, ref visitor);
                    return;
                case BlockStatement block:
                    VisitBlockChildren(block, ref visitor);
                    return;
                case FunctionDeclaration function:
                    for (var i = 0; i < function.Parameters.Count; i++)
                    {
                        visitor.Visit(function.Parameters[i]);
                    }
                    VisitIfNotNull(function.Body, ref visitor);
                    return;
                case LambdaExpression lambda:
                    VisitIfNotNull(lambda.Function, ref visitor);
                    return;
                case VariableDeclaration variable:
                    VisitIfNotNull(variable.Pattern, ref visitor);
                    VisitIfNotNull(variable.Initializer, ref visitor);
                    return;
                case ArrayDestructuringPattern arrayPattern:
                    for (var i = 0; i < arrayPattern.Elements.Count; i++)
                    {
                        VisitIfNotNull(arrayPattern.Elements[i], ref visitor);
                    }
                    return;
                case ObjectDestructuringPattern:
                    return;
                case IfStatement ifStatement:
                    VisitIfNotNull(ifStatement.Condition, ref visitor);
                    VisitIfNotNull(ifStatement.Body, ref visitor);
                    VisitIfNotNull(ifStatement.Else, ref visitor);
                    return;
                case WhileStatement whileStatement:
                    VisitIfNotNull(whileStatement.Condition, ref visitor);
                    VisitIfNotNull(whileStatement.Body, ref visitor);
                    return;
                case ForStatement forStatement:
                    VisitIfNotNull(forStatement.Initializer, ref visitor);
                    VisitIfNotNull(forStatement.Condition, ref visitor);
                    VisitIfNotNull(forStatement.Incrementor, ref visitor);
                    VisitIfNotNull(forStatement.Body, ref visitor);
                    return;
                case ForInStatement forInStatement:
                    VisitIfNotNull(forInStatement.Initializer, ref visitor);
                    VisitIfNotNull(forInStatement.Iterator, ref visitor);
                    VisitIfNotNull(forInStatement.Body, ref visitor);
                    return;
                case TryStatement tryStatement:
                    VisitIfNotNull(tryStatement.Body, ref visitor);
                    VisitIfNotNull(tryStatement.CatchBody, ref visitor);
                    VisitIfNotNull(tryStatement.FinallyBody, ref visitor);
                    return;
                case ReturnStatement returnStatement:
                    VisitIfNotNull(returnStatement.Expression, ref visitor);
                    return;
                case ThrowStatement throwStatement:
                    VisitIfNotNull(throwStatement.Expression, ref visitor);
                    return;
                case DeleteStatement deleteStatement:
                    VisitIfNotNull(deleteStatement.Expression, ref visitor);
                    return;
                case ExpressionStatement expressionStatement:
                    VisitIfNotNull(expressionStatement.Expression, ref visitor);
                    return;
                case AssignmentExpression assignment:
                    VisitIfNotNull(assignment.Left, ref visitor);
                    VisitIfNotNull(assignment.Right, ref visitor);
                    return;
                case CompoundExpression compound:
                    VisitIfNotNull(compound.Left, ref visitor);
                    VisitIfNotNull(compound.Right, ref visitor);
                    return;
                case BinaryExpression binary:
                    VisitIfNotNull(binary.Left, ref visitor);
                    VisitIfNotNull(binary.Right, ref visitor);
                    return;
                case TemplateStringExpression template:
                    for (var i = 0; i < template.PartCount; i++)
                    {
                        var part = template.Parts[i];
                        if (!part.IsLiteral)
                        {
                            VisitIfNotNull(part.Expression, ref visitor);
                        }
                    }
                    return;
                case IncludedExpression included:
                    VisitIfNotNull(included.Left, ref visitor);
                    VisitIfNotNull(included.Right, ref visitor);
                    return;
                case InExpression inExpression:
                    VisitIfNotNull(inExpression.Left, ref visitor);
                    VisitIfNotNull(inExpression.Right, ref visitor);
                    return;
                case FunctionCallExpression call:
                    VisitIfNotNull(call.Target, ref visitor);
                    for (var i = 0; i < call.Arguments.Count; i++)
                    {
                        VisitIfNotNull(call.Arguments[i], ref visitor);
                    }
                    return;
                case NewExpression newExpression:
                    VisitIfNotNull(newExpression.Expression, ref visitor);
                    return;
                case GetElementExpression getElement:
                    VisitIfNotNull(getElement.Object, ref visitor);
                    VisitIfNotNull(getElement.Index, ref visitor);
                    return;
                case SetElementExpression setElement:
                    VisitIfNotNull(setElement.Object, ref visitor);
                    VisitIfNotNull(setElement.Index, ref visitor);
                    VisitIfNotNull(setElement.Value, ref visitor);
                    return;
                case GetPropertyExpression getProperty:
                    VisitIfNotNull(getProperty.Object, ref visitor);
                    VisitIfNotNull(getProperty.Property, ref visitor);
                    return;
                case SetPropertyExpression setProperty:
                    VisitIfNotNull(setProperty.Object, ref visitor);
                    VisitIfNotNull(setProperty.Property, ref visitor);
                    VisitIfNotNull(setProperty.Value, ref visitor);
                    return;
                case MapKeyValueExpression mapEntry:
                    VisitIfNotNull(mapEntry.Value, ref visitor);
                    return;
                case GroupExpression group:
                    for (var i = 0; i < group.Expressions.Count; i++)
                    {
                        VisitIfNotNull(group.Expressions[i], ref visitor);
                    }
                    return;
                case ArrayLiteralExpression array:
                    for (var i = 0; i < array.Elements.Count; i++)
                    {
                        VisitIfNotNull(array.Elements[i], ref visitor);
                    }
                    return;
                case MapExpression map:
                    for (var i = 0; i < map.Entries.Count; i++)
                    {
                        VisitIfNotNull(map.Entries[i], ref visitor);
                    }
                    return;
                case PrefixUnaryExpression prefix:
                    VisitIfNotNull(prefix.Expression, ref visitor);
                    if (prefix is CastTypeExpression cast)
                    {
                        VisitIfNotNull(cast.Typed, ref visitor);
                    }
                    return;
                case UnaryExpression unary:
                    VisitIfNotNull(unary.Expression, ref visitor);
                    return;
            }
        }

        public static void VisitDescendants<TVisitor>(AstNode node, ref TVisitor visitor)
            where TVisitor : struct, IAstChildVisitor
        {
            var recursive = new RecursiveVisitor<TVisitor>(ref visitor);
            VisitChildren(node, ref recursive);
            visitor = recursive.Inner;
        }

        private static void VisitBlockChildren<TVisitor>(BlockStatement block, ref TVisitor visitor)
            where TVisitor : struct, IAstChildVisitor
        {
            for (var i = 0; i < block.Statements.Count; i++)
            {
                VisitIfNotNull(block.Statements[i], ref visitor);
            }
            for (var i = 0; i < block.Functions.Count; i++)
            {
                visitor.Visit(block.Functions[i]);
            }
        }

        private static void VisitImports<TVisitor>(ModuleDeclaration module, ref TVisitor visitor)
            where TVisitor : struct, IAstChildVisitor
        {
            for (var i = 0; i < module.Imports.Count; i++)
            {
                visitor.Visit(module.Imports[i]);
            }
        }

        private static void VisitIfNotNull<TVisitor>(AstNode node, ref TVisitor visitor)
            where TVisitor : struct, IAstChildVisitor
        {
            if (node != null)
            {
                visitor.Visit(node);
            }
        }

        private struct RecursiveVisitor<TVisitor> : IAstChildVisitor
            where TVisitor : struct, IAstChildVisitor
        {
            public RecursiveVisitor(ref TVisitor inner)
            {
                Inner = inner;
            }

            public TVisitor Inner;

            public void Visit(AstNode node)
            {
                Inner.Visit(node);
                AstTraversal.VisitChildren(node, ref this);
            }
        }
    }
}
