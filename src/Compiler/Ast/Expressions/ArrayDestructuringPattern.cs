using System.Collections.Generic;

namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// Represents an array destructuring pattern like [ a, b, ..c ]
    /// </summary>
    internal class ArrayDestructuringPattern : Expression
    {
        public ArrayDestructuringPattern()
        {
            Elements = new List<Expression>();
        }

        /// <summary>
        /// List of element identifiers or rest elements being destructured
        /// Can contain NameExpression for simple identifiers or SpreadExpression for rest elements
        /// </summary>
        public List<Expression> Elements { get; set; }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptArrayDestructuringPattern(this);
        }
    }
}
