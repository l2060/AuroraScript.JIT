using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Runtime;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;


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
        /// parameter Modifier  ....
        /// </summary>
        public Token Modifier { get; set; }

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

        public override IEnumerable<AstNode> ChildNodes
        {
            get
            {
                if (Pattern != null) yield return Pattern;
                if (Initializer != null) yield return Initializer;
            }
        }

        /// <summary>
        /// Function Access
        /// </summary>
        public MemberAccess Access { get; set; }

        /// <summary>
        /// this variable use const declare
        /// </summary>
        public bool IsConst { get; set; }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptVarDeclaration(this);
        }

        override public string ToString()
        {
            return $"VariableDeclaration: {Name?.Value}";
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
                        token = new BooleanToken(value.Boolean.ToString());
                    }
                    else if (value.Kind == ValueKind.Number)
                    {
                        token = new NumberToken(value.Number.ToString());
                    }
                    else if (value.Kind == ValueKind.String)
                    {
                        token = new StringToken();
                        token.Value = value.String.Value;
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