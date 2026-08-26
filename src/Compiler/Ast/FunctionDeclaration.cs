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
        Declare = 2,

        /// <summary>
        /// Uses an explicit CLR-native call signature. A Datum-compatible
        /// closure entry is emitted whenever the function can escape.
        /// </summary>
        Native = 4
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
            TypeReference returnType = null)
        {
            Access = access;
            Name = identifier;
            Parameters = parameters ?? Array.Empty<ParameterDeclaration>();
            Body = body;
            Flags = flags;
            ReturnType = returnType;
            if (Parameters.Count > 0)
            {
                for (int i = 0; i < Parameters.Count; i++) Parameters[i].Parent = this;
            }
            if (body != null) body.Parent = this;
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
        /// function name
        /// </summary>
        public Token Name { get; private set; }

        public FunctionFlags Flags { get; private set; }

        public bool IsNative => (Flags & FunctionFlags.Native) != 0;

        public Token NativeToken { get; internal set; }

        /// <summary>
        /// Optional source-level return contract. A missing contract preserves
        /// the existing weakly typed, inference-only behavior.
        /// </summary>
        public TypeReference ReturnType { get; }


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptFunction(this);
        }
    }
}
