using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using System;
using System.Collections.Generic;


namespace AuroraScript.Compiler.Ast
{
    internal enum FunctionFlags
    {

        /// <summary>
        /// 普通方法
        /// </summary>
        General = 0,

        /// <summary>
        /// Lambda 方法
        /// </summary>
        Lambda = 1,

        /// <summary>
        /// 仅声明
        /// </summary>
        Declare = 2
    }





    /// <summary>
    /// 函数定义
    /// </summary>
    internal class FunctionDeclaration : Statement, INamedStatement
    {

        internal FunctionDeclaration(
            MemberAccess access,
            Token identifier,
            IReadOnlyList<ParameterDeclaration> parameters,
            Statement body,
            FunctionFlags flags,
            IReadOnlyList<FunctionAnnotation> annotations = null)
        {
            Access = access;
            Name = identifier;
            Parameters = parameters ?? Array.Empty<ParameterDeclaration>();
            Body = body;
            Flags = flags;
            Annotations = annotations ?? Array.Empty<FunctionAnnotation>();
            if (Parameters.Count > 0)
            {
                for (int i = 0; i < Parameters.Count; i++) Parameters[i].Parent = this;
            }
            if (body != null) body.Parent = this;
            for (var i = 0; i < Annotations.Count; i++) Annotations[i].Parent = this;
        }




        /// <summary>
        /// parameters
        /// </summary>
        public IReadOnlyList<ParameterDeclaration> Parameters { get; private set; }

        /// <summary>
        /// function code
        /// </summary>
        public Statement Body { get; private set; }

        /// <summary>
        /// Function Access
        /// </summary>
        public MemberAccess Access { get; private set; }

        /// <summary>
        /// Export ....
        /// </summary>
        public List<Token> Modifiers { get; private set; }

        /// <summary>
        /// function name
        /// </summary>
        public Token Name { get; private set; }

        public FunctionFlags Flags { get; private set; }

        public IReadOnlyList<FunctionAnnotation> Annotations { get; private set; }


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptFunction(this);
        }

        override public string ToString()
        {
            return $"FunctionDeclaration: {Name?.Value}";
        }
    }
}
