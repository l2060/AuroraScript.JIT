using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Runtime;
using AuroraScript.Tokens;
using System;


namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// variable declaration
    /// </summary>
    internal class VariableDeclaration : Statement, INamedStatement, IConstEvaluable
    {
        internal VariableDeclaration(MemberAccess access, Boolean isConst, Token nameToken, Expression initializer)
        {
            Access = access;
            IsConst = isConst;
            Name = nameToken;

            if (initializer != null)
            {
                Initializer = initializer;
                Initializer.Parent = this;
            }
        }

        /// <summary>
        /// Constructor for destructuring patterns
        /// </summary>
        internal VariableDeclaration(MemberAccess access, Boolean isConst, Expression pattern, Expression initializer)
        {
            Access = access;
            IsConst = isConst;
            Pattern = pattern;
            Pattern.Parent = this;

            if (initializer != null)
            {
                Initializer = initializer;
                Initializer.Parent = this;
            }
        }

        /// <summary>
        /// variable names
        /// </summary>
        public Token Name { get; set; }

        /// <summary>
        /// Destructuring pattern (for object/array destructuring)
        /// </summary>
        public Expression Pattern { get; set; }

        /// <summary>
        /// var initialize statement
        /// </summary>
        public Expression Initializer;


        /// <summary>
        /// Function Access
        /// </summary>
        public MemberAccess Access { get; set; }

        /// <summary>
        /// this variable use const declare
        /// </summary>
        public bool IsConst { get; set; }

        /// <summary>
        /// External variable declaration only. No module property is emitted for this declaration.
        /// </summary>
        public bool IsDeclare { get; set; }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptVarDeclaration(this);
        }
        public void TryFolding(EvaluationContext ctx)
        {
            ScriptDatum datum = default;
            TryEvalConst(ctx, ref datum);
        }




        public bool TryEvalConst(EvaluationContext ctx, ref ScriptDatum value)
        {
            if (IsConst)
            {
                if (Initializer is LiteralExpression literal)
                {
                    return literal.TryEvalConst(ctx, ref value);
                }
                if (Initializer != null && Initializer.TryEvalConst(ctx, ref value))
                {
                    ValueToken token;
                    if (value.Kind == ValueKind.Null)
                    {
                        token = new NullToken();
                    }
                    else if (value.Kind == ValueKind.Boolean)
                    {
                        token = new BooleanToken(value.Boolean);
                    }
                    else if (value.Kind == ValueKind.Number)
                    {
                        token = new NumberToken(value.Number);
                    }
                    else if (value.Kind == ValueKind.String)
                    {
                        token = new StringToken();
                        token.Value = value.StringText;
                    }
                    else
                    {
                        return false;
                    }
                    Initializer = new LiteralExpression(token);
                    return true;
                }
            }
            return false;
        }
    }
}
