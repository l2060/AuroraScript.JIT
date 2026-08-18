using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend.Plans;
using System;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class UnsupportedEmissionException : NotSupportedException
    {
        public UnsupportedEmissionException(FunctionPlan function, AstNode node)
            : base($"Unsupported AST node '{node?.GetType().Name ?? "<null>"}' in function '{function?.Name ?? "<anonymous>"}' at {node?.Range ?? SourceSpan.None}.")
        {
            Function = function?.Id ?? FunctionId.Invalid;
            FunctionName = function?.Name;
            NodeType = node?.GetType().Name ?? "<null>";
            Range = node?.Range ?? SourceSpan.None;
            IsExpression = node is Compiler.Ast.Expressions.Expression;
        }

        public FunctionId Function { get; }
        public string FunctionName { get; }
        public string NodeType { get; }
        public SourceSpan Range { get; }
        public bool IsExpression { get; }
    }
}
