using AuroraScript.Compiler.Backend.Lowering;
using AuroraScript.Compiler.Backend.Plans;
using System;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class UnsupportedEmissionException : NotSupportedException
    {
        public UnsupportedEmissionException(FunctionPlan function, LoweredUnsupportedNode node)
            : base($"Unsupported lowered {(node.IsExpression ? "expression" : "statement")} '{node.NodeType}' in function '{function?.Name ?? "<anonymous>"}' at {node.Range}.")
        {
            Function = function?.Id ?? FunctionId.Invalid;
            FunctionName = function?.Name;
            NodeType = node.NodeType;
            Range = node.Range;
            IsExpression = node.IsExpression;
        }

        public FunctionId Function { get; }
        public string FunctionName { get; }
        public string NodeType { get; }
        public SourceSpan Range { get; }
        public bool IsExpression { get; }
    }
}
