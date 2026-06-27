using System.Collections.Generic;

namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// Represents an object destructuring pattern like { a, b, c }
    /// </summary>
    internal class ObjectDestructuringPattern : Expression
    {
        public ObjectDestructuringPattern()
        {
            Properties = new List<Token>();
        }

        /// <summary>
        /// List of property identifiers being destructured
        /// </summary>
        public List<Token> Properties { get; set; }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptObjectDestructuringPattern(this);
        }
    }
}
