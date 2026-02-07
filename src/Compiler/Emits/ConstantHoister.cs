using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Runtime.Types;
using AuroraScript.Tokens;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Emits
{
    internal class ConstantHoister : IAstVisitor
    {
        private int _loopDepth = 0;
        private readonly Dictionary<object, int> _usageCount = new();
        private readonly HashSet<object> _hotValues = new();

        public struct LiteralStats
        {
            public HashSet<object> HotValues;
            public Dictionary<object, int> UsageCount;
        }

        public LiteralStats GetLiteralStats(AstNode node)
        {
            _usageCount.Clear();
            _hotValues.Clear();
            node.Accept(this);
            return new LiteralStats { HotValues = _hotValues, UsageCount = _usageCount };
        }

        protected override void VisitWhileStatement(WhileStatement node)
        {
            _loopDepth++;
            base.VisitWhileStatement(node);
            _loopDepth--;
        }

        protected override void VisitForStatement(ForStatement node)
        {
            // Initializer is only executed once, not "hot" by itself
            node.Initializer?.Accept(this);

            _loopDepth++;
            node.Condition?.Accept(this);
            node.Incrementor?.Accept(this);
            node.Body?.Accept(this);
            _loopDepth--;
        }

        protected override void VisitForInStatement(ForInStatement node)
        {
            _loopDepth++;
            base.VisitForInStatement(node);
            _loopDepth--;
        }

        protected override void VisitLiteralExpression(LiteralExpression node)
        {
            object val = node.Token switch
            {
                NumberToken n => n.NumberValue,
                StringToken s => s.Value,
                BooleanToken b => b.BoolValue,
                NullToken n => ScriptObject.Null,
                _ => null
            };

            if (val == null) return;

            _usageCount[val] = _usageCount.GetValueOrDefault(val) + 1;
            if (_loopDepth > 0)
            {
                _hotValues.Add(val);
            }
        }

        protected override void VisitFunction(FunctionDeclaration node) { }
        protected override void VisitLambdaExpression(LambdaExpression node) { }
    }

}