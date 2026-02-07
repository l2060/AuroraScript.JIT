using AuroraScript.Runtime;

namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// 二元表达式
    /// </summary>
    internal class BinaryExpression : OperatorExpression
    {

        internal BinaryExpression(Operator @operator, Expression left, Expression right) : base(@operator)
        {
            Left = left;
            Right = right;
            Left.Parent = this;
            Right.Parent = this;
        }

        public readonly Expression Left;
        public readonly Expression Right;


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptBinaryExpression(this);
        }


        public override string ToString()
        {
            var isPriority = false;
            if (this.Parent is BinaryExpression parent)
            {
                isPriority = parent.Operator.Precedence > this.Operator.Precedence;
            }
            var value = $"{Left} {Operator} {Right}";
            if (isPriority) return $"({value})";
            return value;
        }



        public override bool TryEvalConst(EvaluationContext ctx, ref ScriptDatum value)
        {
            ScriptDatum leftSlot = default;
            ScriptDatum rightSlot = default;

            if (!Left.TryEvalConst(ctx, ref leftSlot) || !Right.TryEvalConst(ctx, ref rightSlot)) return false;


            if (Operator == Operator.Add)
            {
                if (leftSlot.Kind == ValueKind.Number && rightSlot.Kind == ValueKind.Number)
                {
                    ScriptDatum.WriteAsNumber(ref value, leftSlot.Number + rightSlot.Number);
                    return true;
                }
                else
                {
                    ScriptDatum.WriteAsString(ref value, ScriptDatum.ToString(leftSlot) + ScriptDatum.ToString(rightSlot));
                    return true;
                }
            }

            if (Operator == Operator.Subtract)
            {
                if (leftSlot.Kind == ValueKind.Number && rightSlot.Kind == ValueKind.Number)
                {
                    ScriptDatum.WriteAsNumber(ref value, leftSlot.Number - rightSlot.Number);
                    return true;
                }
            }
            if (Operator == Operator.Multiply)
            {
                if (leftSlot.Kind == ValueKind.Number && rightSlot.Kind == ValueKind.Number)
                {
                    ScriptDatum.WriteAsNumber(ref value, leftSlot.Number * rightSlot.Number);
                    return true;
                }
            }
            if (Operator == Operator.Divide)
            {
                if (leftSlot.Kind == ValueKind.Number && rightSlot.Kind == ValueKind.Number)
                {
                    ScriptDatum.WriteAsNumber(ref value, leftSlot.Number / rightSlot.Number);
                    return true;
                }
            }


            //if (Operator == Operator.Equal) return new BinaryExpression(_operator);
            //if (Operator == Operator.LeftShift) return new BinaryExpression(_operator);
            //if (Operator == Operator.LessThan) return new BinaryExpression(_operator);
            //if (Operator == Operator.LessThanOrEqual) return new BinaryExpression(_operator);
            //if (Operator == Operator.GreaterThan) return new BinaryExpression(_operator);
            //if (Operator == Operator.GreaterThanOrEqual) return new BinaryExpression(_operator);
            //if (Operator == Operator.BitwiseAnd) return new BinaryExpression(_operator);
            //if (Operator == Operator.BitwiseOr) return new BinaryExpression(_operator);
            //if (Operator == Operator.BitwiseXor) return new BinaryExpression(_operator);
            //if (Operator == Operator.LogicalAnd) return new BinaryExpression(_operator);
            //if (Operator == Operator.LogicalOr) return new BinaryExpression(_operator);
            //if (Operator == Operator.Modulo) return new BinaryExpression(_operator);
            //if (Operator == Operator.NotEqual) return new BinaryExpression(_operator);
            //if (Operator == Operator.SignedRightShift) return new BinaryExpression(_operator);
            //if (Operator == Operator.UnSignedRightShift) return new BinaryExpression(_operator);


            // TODO : 实现常量表达式求值
            return false;
        }

    }
}