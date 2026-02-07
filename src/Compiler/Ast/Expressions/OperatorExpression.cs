using System;

namespace AuroraScript.Compiler.Ast.Expressions
{
    internal abstract class OperatorExpression : Expression
    {
        internal OperatorExpression(Operator @operator)
        {
            this.Operator = @operator;
            this.Precedence = this.Operator.Precedence;
            this.IsOperand = this.Operator.IsOperand;
        }

        /// <summary>
        /// operator precedence
        /// </summary>
        internal Int32 Precedence
        {
            get;
            private set;
        }

        /// <summary>
        /// this expression is Operand
        /// </summary>
        internal Boolean IsOperand
        {
            get;
            private set;
        }




        /// <summary>
        /// Gets or sets the operator this expression refers to.
        /// </summary>
        public Operator Operator
        {
            get;
            private set;
        }
    }
}