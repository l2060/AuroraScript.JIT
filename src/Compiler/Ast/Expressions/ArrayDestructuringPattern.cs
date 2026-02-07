using System.Collections.Generic;
using System.Linq;

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

        public override IEnumerable<AstNode> ChildNodes
        {
            get
            {
                foreach (var element in Elements)
                {
                    if (element != null) yield return element;
                }
            }
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptArrayDestructuringPattern(this);
        }

        public override string ToString()
        {
            var elements = string.Join(", ", Elements.Select(e => e?.ToString() ?? ""));
            return $"[ {elements} ]";
        }
    }
}
