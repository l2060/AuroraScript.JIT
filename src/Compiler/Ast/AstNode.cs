using System;

namespace AuroraScript.Compiler.Ast
{
    internal abstract class AstNode
    {
        public SourceSpan Range { get; set; } = SourceSpan.None;
        public bool IsIndependent { get; set; } = false;

        public Int32 LineNumber => Range.StartLine;
        public Int32 ColumnNumber => Range.StartColumn;
        public String FileName => Range.FileName;

        public AstNode Parent { get; internal set; }


        internal AstNode()
        {
        }


        protected static void AttachParent(AstNode node, AstNode parent)
        {
            if (node != null)
            {
                node.Parent = parent;
            }
        }

        public abstract void Accept(IAstVisitor visitor);

    }
}
