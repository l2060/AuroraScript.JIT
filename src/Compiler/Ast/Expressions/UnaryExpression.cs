using AuroraScript.Runtime;


namespace AuroraScript.Compiler.Ast.Expressions
{
    internal enum UnaryType
    {
        Prefix,
        Post
    }



    /// <summary>
    /// Postfix Expression
    /// i++
    /// i--
    /// </summary>
    internal class UnaryExpression : OperatorExpression
    {
        public readonly UnaryType Type;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptUnaryExpression(this);
        }

        internal UnaryExpression(Operator @operator, UnaryType type, Expression expression) : base(@operator)
        {
            Type = type;
            Expression = expression;
            Expression.Parent = this;
        }

        public readonly Expression Expression;



        public override string ToString()
        {
            if (Type == UnaryType.Post)
            {
                return $"{Expression}{this.Operator}";
            }
            else
            {
                return $"{this.Operator}{Expression}";
            }
        }



        public override bool TryEvalConst(EvaluationContext ctx, ref ScriptDatum value)
        {
            if (Expression.TryEvalConst(ctx, ref value))
            {
                if (Operator == Operator.PreIncrement)
                {
                    if (ScriptDatum.TryToNumber(in value, out var numValue))
                    {
                        ScriptDatum.WriteAsNumber(ref value, numValue + 1);
                        return true;
                    }
                }
                else if (Operator == Operator.PostIncrement)
                {
                    // Ignore post operation side effect in const eval
                    return true;
                }
                else if (Operator == Operator.PreDecrement)
                {
                    if (ScriptDatum.TryToNumber(in value, out var numValue))
                    {
                        ScriptDatum.WriteAsNumber(ref value, numValue - 1);
                        return true;
                    }
                }
                else if (Operator == Operator.PostDecrement)
                {
                    // Ignore post operation side effect in const eval
                    return true;
                }
            }
            return false;
        }



    }

}
