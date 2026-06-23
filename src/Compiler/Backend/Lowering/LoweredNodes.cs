using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Tokens;
using System;

namespace AuroraScript.Compiler.Backend.Lowering
{
    internal enum LoweredStatementKind
    {
        Unsupported,
        Block,
        Expression,
        Return,
        VariableDeclaration,
        ObjectDestructuringDeclaration,
        ArrayDestructuringDeclaration,
        FunctionDeclaration,
        If,
        While,
        For,
        ForIn,
        Try,
        Throw,
        Delete,
        Debugger,
        Break,
        Continue
    }

    internal enum LoweredExpressionKind
    {
        Unsupported,
        Literal,
        Name,
        Binary,
        Call,
        Lambda,
        Assignment,
        Compound,
        Unary,
        In,
        GetProperty,
        GetElement,
        SetProperty,
        SetElement,
        ArrayLiteral,
        Map,
        Spread,
        New
    }

    internal abstract class LoweredNode
    {
        protected LoweredNode(AstNode source)
        {
            Source = source;
            Range = source?.Range ?? SourceSpan.None;
        }

        public AstNode Source { get; }
        public SourceSpan Range { get; }
    }

    internal abstract class LoweredStatement : LoweredNode
    {
        protected LoweredStatement(AstNode source, LoweredStatementKind kind) : base(source)
        {
            Kind = kind;
        }

        public LoweredStatementKind Kind { get; }
    }

    internal abstract class LoweredExpression : LoweredNode
    {
        protected LoweredExpression(Expression source, LoweredExpressionKind kind) : base(source)
        {
            Kind = kind;
        }

        public LoweredExpressionKind Kind { get; }
    }

    internal sealed class LoweredBlockStatement : LoweredStatement
    {
        public LoweredBlockStatement(AstNode source, LoweredStatement[] statements) : base(source, LoweredStatementKind.Block)
        {
            Statements = statements ?? Array.Empty<LoweredStatement>();
        }

        public LoweredStatement[] Statements { get; }
    }

    internal sealed class LoweredExpressionStatement : LoweredStatement
    {
        public LoweredExpressionStatement(AstNode source, LoweredExpression expression) : base(source, LoweredStatementKind.Expression)
        {
            Expression = expression;
        }

        public LoweredExpression Expression { get; }
    }

    internal sealed class LoweredReturnStatement : LoweredStatement
    {
        public LoweredReturnStatement(AstNode source, LoweredExpression expression) : base(source, LoweredStatementKind.Return)
        {
            Expression = expression;
        }

        public LoweredExpression Expression { get; }
    }

    internal sealed class LoweredVariableDeclarationStatement : LoweredStatement
    {
        public LoweredVariableDeclarationStatement(AstNode source, LocalSlotId slot, LoweredExpression initializer)
            : base(source, LoweredStatementKind.VariableDeclaration)
        {
            Slot = slot;
            Initializer = initializer;
        }

        public LocalSlotId Slot { get; }
        public LoweredExpression Initializer { get; }
    }

    internal readonly struct LoweredObjectDestructuringBinding
    {
        public LoweredObjectDestructuringBinding(Token property, LocalSlotId slot)
        {
            Property = property;
            Slot = slot;
        }

        public Token Property { get; }
        public LocalSlotId Slot { get; }
    }

    internal sealed class LoweredObjectDestructuringDeclarationStatement : LoweredStatement
    {
        public LoweredObjectDestructuringDeclarationStatement(
            AstNode source,
            LoweredExpression initializer,
            LoweredObjectDestructuringBinding[] bindings)
            : base(source, LoweredStatementKind.ObjectDestructuringDeclaration)
        {
            Initializer = initializer;
            Bindings = bindings ?? Array.Empty<LoweredObjectDestructuringBinding>();
        }

        public LoweredExpression Initializer { get; }
        public LoweredObjectDestructuringBinding[] Bindings { get; }
    }

    internal readonly struct LoweredArrayDestructuringBinding
    {
        public LoweredArrayDestructuringBinding(LocalSlotId slot, int index, bool isRest, int trailingCount)
        {
            Slot = slot;
            Index = index;
            IsRest = isRest;
            TrailingCount = trailingCount;
        }

        public LocalSlotId Slot { get; }
        public int Index { get; }
        public bool IsRest { get; }
        public int TrailingCount { get; }
    }

    internal sealed class LoweredArrayDestructuringDeclarationStatement : LoweredStatement
    {
        public LoweredArrayDestructuringDeclarationStatement(
            AstNode source,
            LoweredExpression initializer,
            LoweredArrayDestructuringBinding[] bindings)
            : base(source, LoweredStatementKind.ArrayDestructuringDeclaration)
        {
            Initializer = initializer;
            Bindings = bindings ?? Array.Empty<LoweredArrayDestructuringBinding>();
        }

        public LoweredExpression Initializer { get; }
        public LoweredArrayDestructuringBinding[] Bindings { get; }
    }

    internal sealed class LoweredFunctionDeclarationStatement : LoweredStatement
    {
        public LoweredFunctionDeclarationStatement(AstNode source, FunctionId function, LocalSlotId localSlot)
            : base(source, LoweredStatementKind.FunctionDeclaration)
        {
            Function = function;
            LocalSlot = localSlot;
        }

        public FunctionId Function { get; }
        public LocalSlotId LocalSlot { get; }
    }

    internal sealed class LoweredIfStatement : LoweredStatement
    {
        public LoweredIfStatement(AstNode source, LoweredExpression condition, LoweredStatement body, LoweredStatement @else)
            : base(source, LoweredStatementKind.If)
        {
            Condition = condition;
            Body = body;
            Else = @else;
        }

        public LoweredExpression Condition { get; }
        public LoweredStatement Body { get; }
        public LoweredStatement Else { get; }
    }

    internal sealed class LoweredWhileStatement : LoweredStatement
    {
        public LoweredWhileStatement(AstNode source, LoweredExpression condition, LoweredStatement body)
            : base(source, LoweredStatementKind.While)
        {
            Condition = condition;
            Body = body;
        }

        public LoweredExpression Condition { get; }
        public LoweredStatement Body { get; }
    }

    internal sealed class LoweredForStatement : LoweredStatement
    {
        public LoweredForStatement(
            AstNode source,
            LoweredStatement initializer,
            LoweredExpression condition,
            LoweredExpression incrementor,
            LoweredStatement body)
            : base(source, LoweredStatementKind.For)
        {
            Initializer = initializer;
            Condition = condition;
            Incrementor = incrementor;
            Body = body;
        }

        public LoweredStatement Initializer { get; }
        public LoweredExpression Condition { get; }
        public LoweredExpression Incrementor { get; }
        public LoweredStatement Body { get; }
    }

    internal sealed class LoweredForInStatement : LoweredStatement
    {
        public LoweredForInStatement(
            AstNode source,
            LoweredStatement initializer,
            LoweredInExpression iterator,
            LoweredStatement body)
            : base(source, LoweredStatementKind.ForIn)
        {
            Initializer = initializer;
            Iterator = iterator;
            Body = body;
        }

        public LoweredStatement Initializer { get; }
        public LoweredInExpression Iterator { get; }
        public LoweredStatement Body { get; }
    }

    internal sealed class LoweredTryStatement : LoweredStatement
    {
        public LoweredTryStatement(
            AstNode source,
            LoweredStatement body,
            string catchVariable,
            LocalSlotId catchSlot,
            LoweredStatement catchBody,
            LoweredStatement finallyBody)
            : base(source, LoweredStatementKind.Try)
        {
            Body = body;
            CatchVariable = catchVariable;
            CatchSlot = catchSlot;
            CatchBody = catchBody;
            FinallyBody = finallyBody;
        }

        public LoweredStatement Body { get; }
        public string CatchVariable { get; }
        public LocalSlotId CatchSlot { get; }
        public LoweredStatement CatchBody { get; }
        public LoweredStatement FinallyBody { get; }
    }

    internal sealed class LoweredThrowStatement : LoweredStatement
    {
        public LoweredThrowStatement(AstNode source, LoweredExpression expression)
            : base(source, LoweredStatementKind.Throw)
        {
            Expression = expression;
        }

        public LoweredExpression Expression { get; }
    }

    internal sealed class LoweredDeleteStatement : LoweredStatement
    {
        public LoweredDeleteStatement(AstNode source, LoweredExpression expression)
            : base(source, LoweredStatementKind.Delete)
        {
            Expression = expression;
        }

        public LoweredExpression Expression { get; }
    }

    internal sealed class LoweredDebuggerStatement : LoweredStatement
    {
        public LoweredDebuggerStatement(AstNode source) : base(source, LoweredStatementKind.Debugger)
        {
        }
    }

    internal sealed class LoweredBreakStatement : LoweredStatement
    {
        public LoweredBreakStatement(AstNode source) : base(source, LoweredStatementKind.Break)
        {
        }
    }

    internal sealed class LoweredContinueStatement : LoweredStatement
    {
        public LoweredContinueStatement(AstNode source) : base(source, LoweredStatementKind.Continue)
        {
        }
    }

    internal sealed class LoweredUnsupportedStatement : LoweredStatement
    {
        public LoweredUnsupportedStatement(AstNode source) : base(source, LoweredStatementKind.Unsupported)
        {
        }
    }

    internal readonly struct LoweredUnsupportedNode
    {
        public LoweredUnsupportedNode(string nodeType, SourceSpan range, bool isExpression)
        {
            NodeType = nodeType;
            Range = range;
            IsExpression = isExpression;
        }

        public string NodeType { get; }
        public SourceSpan Range { get; }
        public bool IsExpression { get; }
    }

    internal sealed class LoweredLiteralExpression : LoweredExpression
    {
        public LoweredLiteralExpression(LiteralExpression source) : base(source, LoweredExpressionKind.Literal)
        {
            Token = source.Token;
        }

        public Token Token { get; }
    }

    internal sealed class LoweredNameExpression : LoweredExpression
    {
        public LoweredNameExpression(NameExpression source, LocalSlotId localSlot, UpvalueSlotId upvalueSlot, SymbolId moduleSymbol)
            : base(source, LoweredExpressionKind.Name)
        {
            Name = source.Identifier?.Value;
            LocalSlot = localSlot;
            UpvalueSlot = upvalueSlot;
            ModuleSymbol = moduleSymbol;
        }

        public string Name { get; }
        public LocalSlotId LocalSlot { get; }
        public UpvalueSlotId UpvalueSlot { get; }
        public SymbolId ModuleSymbol { get; }
    }

    internal sealed class LoweredBinaryExpression : LoweredExpression
    {
        public LoweredBinaryExpression(BinaryExpression source, LoweredExpression left, LoweredExpression right)
            : base(source, LoweredExpressionKind.Binary)
        {
            Operator = source.Operator;
            Left = left;
            Right = right;
        }

        public Operator Operator { get; }
        public LoweredExpression Left { get; }
        public LoweredExpression Right { get; }
    }

    internal sealed class LoweredCallExpression : LoweredExpression
    {
        public LoweredCallExpression(FunctionCallExpression source, LoweredExpression target, LoweredExpression[] arguments, FunctionId directFunction)
            : base(source, LoweredExpressionKind.Call)
        {
            Target = target;
            Arguments = arguments ?? Array.Empty<LoweredExpression>();
            DirectFunction = directFunction;
        }

        public LoweredExpression Target { get; }
        public LoweredExpression[] Arguments { get; }
        public FunctionId DirectFunction { get; }
    }

    internal sealed class LoweredAssignmentExpression : LoweredExpression
    {
        public LoweredAssignmentExpression(AssignmentExpression source, LoweredExpression left, LoweredExpression right)
            : base(source, LoweredExpressionKind.Assignment)
        {
            Operator = source.Operator;
            Left = left;
            Right = right;
        }

        public Operator Operator { get; }
        public LoweredExpression Left { get; }
        public LoweredExpression Right { get; }
    }

    internal sealed class LoweredCompoundExpression : LoweredExpression
    {
        public LoweredCompoundExpression(CompoundExpression source, LoweredExpression left, LoweredExpression right)
            : base(source, LoweredExpressionKind.Compound)
        {
            Operator = source.Operator;
            Left = left;
            Right = right;
        }

        public Operator Operator { get; }
        public LoweredExpression Left { get; }
        public LoweredExpression Right { get; }
    }

    internal sealed class LoweredUnaryExpression : LoweredExpression
    {
        public LoweredUnaryExpression(UnaryExpression source, LoweredExpression expression)
            : base(source, LoweredExpressionKind.Unary)
        {
            Operator = source.Operator;
            Type = source.Type;
            Expression = expression;
        }

        public Operator Operator { get; }
        public UnaryType Type { get; }
        public LoweredExpression Expression { get; }
    }

    internal sealed class LoweredInExpression : LoweredExpression
    {
        public LoweredInExpression(Expression source, LoweredExpression left, LoweredExpression right)
            : base(source, LoweredExpressionKind.In)
        {
            Left = left;
            Right = right;
        }

        public LoweredExpression Left { get; }
        public LoweredExpression Right { get; }
    }

    internal sealed class LoweredGetPropertyExpression : LoweredExpression
    {
        public LoweredGetPropertyExpression(GetPropertyExpression source, LoweredExpression instance, LoweredExpression property)
            : base(source, LoweredExpressionKind.GetProperty)
        {
            Instance = instance;
            Property = property;
        }

        public LoweredExpression Instance { get; }
        public LoweredExpression Property { get; }
    }

    internal sealed class LoweredGetElementExpression : LoweredExpression
    {
        public LoweredGetElementExpression(GetElementExpression source, LoweredExpression instance, LoweredExpression index)
            : base(source, LoweredExpressionKind.GetElement)
        {
            Instance = instance;
            Index = index;
        }

        public LoweredExpression Instance { get; }
        public LoweredExpression Index { get; }
    }

    internal sealed class LoweredSetPropertyExpression : LoweredExpression
    {
        public LoweredSetPropertyExpression(SetPropertyExpression source, LoweredExpression instance, LoweredExpression property, LoweredExpression value)
            : base(source, LoweredExpressionKind.SetProperty)
        {
            Instance = instance;
            Property = property;
            Value = value;
        }

        public LoweredExpression Instance { get; }
        public LoweredExpression Property { get; }
        public LoweredExpression Value { get; }
    }

    internal sealed class LoweredSetElementExpression : LoweredExpression
    {
        public LoweredSetElementExpression(SetElementExpression source, LoweredExpression instance, LoweredExpression index, LoweredExpression value)
            : base(source, LoweredExpressionKind.SetElement)
        {
            Instance = instance;
            Index = index;
            Value = value;
        }

        public LoweredExpression Instance { get; }
        public LoweredExpression Index { get; }
        public LoweredExpression Value { get; }
    }

    internal sealed class LoweredArrayLiteralExpression : LoweredExpression
    {
        public LoweredArrayLiteralExpression(ArrayLiteralExpression source, LoweredExpression[] elements)
            : base(source, LoweredExpressionKind.ArrayLiteral)
        {
            Elements = elements ?? Array.Empty<LoweredExpression>();
        }

        public LoweredExpression[] Elements { get; }
    }

    internal readonly struct LoweredMapEntry
    {
        public LoweredMapEntry(Token key, LoweredExpression value, SourceSpan range)
        {
            Key = key;
            Value = value;
            Range = range;
        }

        public Token Key { get; }
        public LoweredExpression Value { get; }
        public SourceSpan Range { get; }
    }

    internal sealed class LoweredMapExpression : LoweredExpression
    {
        public LoweredMapExpression(MapExpression source, LoweredMapEntry[] entries)
            : base(source, LoweredExpressionKind.Map)
        {
            Entries = entries ?? Array.Empty<LoweredMapEntry>();
        }

        public LoweredMapEntry[] Entries { get; }
    }

    internal sealed class LoweredSpreadExpression : LoweredExpression
    {
        public LoweredSpreadExpression(SpreadExpression source, LoweredExpression expression)
            : base(source, LoweredExpressionKind.Spread)
        {
            Expression = expression;
        }

        public LoweredExpression Expression { get; }
    }

    internal sealed class LoweredNewExpression : LoweredExpression
    {
        public LoweredNewExpression(NewExpression source, LoweredCallExpression expression)
            : base(source, LoweredExpressionKind.New)
        {
            Expression = expression;
        }

        public LoweredCallExpression Expression { get; }
    }

    internal sealed class LoweredLambdaExpression : LoweredExpression
    {
        public LoweredLambdaExpression(LambdaExpression source, FunctionId function)
            : base(source, LoweredExpressionKind.Lambda)
        {
            Function = function;
        }

        public FunctionId Function { get; }
    }

    internal sealed class LoweredUnsupportedExpression : LoweredExpression
    {
        public LoweredUnsupportedExpression(Expression source) : base(source, LoweredExpressionKind.Unsupported)
        {
        }
    }
}
