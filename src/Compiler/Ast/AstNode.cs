using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuroraScript.Compiler.Ast
{
    internal abstract class AstNode
    {
        public SourceSpan Range { get; set; } = SourceSpan.None;
        public bool IsIndependent { get; set; } = false;

        public Int32 LineNumber => Range.StartLine;
        public Int32 ColumnNumber => Range.StartColumn;
        public String FileName => Range.FileName;

        protected List<AstNode> _children;


        [JsonIgnore]
        public AstNode Parent { get; internal set; }


        internal AstNode()
        {
        }


        public Int32 Length
        {
            get
            {
                return this._children?.Count ?? 0;
            }
        }

        public AstNode this[Int32 index]
        {
            get
            {
                return this._children[index];
            }
        }


        public virtual void AddNode(AstNode node)
        {
            // if (node.Parent != null) throw new InvalidOperationException();
            this._children ??= new List<AstNode>();
            this._children.Add(node);
            node.Parent = this;
        }

        public abstract void Accept(IAstVisitor visitor);

    }
}
