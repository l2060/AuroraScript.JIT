using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
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


        /// <summary>
        /// 丢弃返回值
        /// </summary>
        public Boolean NeedResult
        {

            get
            {
                if (Parent is Expression ||
                    Parent is ForInStatement ||
                    //Parent is ForStatement ||
                    Parent is IfStatement ||
                    Parent is ReturnStatement ||
                    Parent is WhileStatement ||
                    Parent is BinaryExpression ||
                    Parent is VariableDeclaration)
                {




                    return true;
                }
                return false;
            }
        }


        [JsonIgnore]
        public AstNode Parent { get; internal set; }



        public AstNode ResolveParent<ParentType>()
        {
            var p = Parent;
            while (p != null)
            {
                if (typeof(ParentType) == p.GetType()) return p;
                p = p.Parent;
            }
            return null;
        }


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

        public virtual IEnumerable<AstNode> ChildNodes
        {
            get
            {
                return _children ?? (IEnumerable<AstNode>)Array.Empty<AstNode>();
            }
        }

        public void Remove()
        {
            if (this.Parent != null)
            {
                this.Parent._children?.Remove(this);
                this.Parent = null;
            }
        }

        public virtual void AddNode(AstNode node)
        {
            // if (node.Parent != null) throw new InvalidOperationException();
            this._children ??= new List<AstNode>();
            this._children.Add(node);
            node.Parent = this;
        }
        public virtual void InsertNode(int index, AstNode node)
        {
            // if (node.Parent != null) throw new InvalidOperationException();
            this._children ??= new List<AstNode>();
            this._children.Insert(index, node);
            node.Parent = this;
        }


        public abstract void Accept(IAstVisitor visitor);

    }
}
