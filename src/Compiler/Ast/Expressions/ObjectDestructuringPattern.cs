using System.Collections.Generic;
using System.Linq;

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

        public override IEnumerable<AstNode> ChildNodes
        {
            get { yield break; }
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptObjectDestructuringPattern(this);
        }


        public override string ToString()
        {
            var props = string.Join(", ", Properties.Select(p => p.Value));
            return $"{{ {props} }}";
        }
    }
}
