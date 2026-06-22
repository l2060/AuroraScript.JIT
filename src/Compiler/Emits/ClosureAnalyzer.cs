using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Core;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Emits
{
    /// <summary>
    /// Analyzes a function to identify:
    /// 1. Upvalues (variables captured from outer scopes).
    /// 2. EscapedLocals (local variables captured by inner functions).
    /// </summary>
    internal class ClosureAnalyzer : IAstVisitor
    {
        // Variables declared in this function (including parameters)
        public HashSet<string> DeclaredVariables { get; } = new HashSet<string>();

        // Variables used in this function (excluding those used only in nested functions)
        public HashSet<string> UsedVariables { get; } = new HashSet<string>();

        // Variables captured from outer scopes
        public HashSet<string> Upvalues { get; } = new HashSet<string>();

        // Local variables that are captured by nested functions
        public HashSet<string> EscapedLocals { get; } = new HashSet<string>();

        // Variables used in nested functions
        public HashSet<string> NestedUsages { get; } = new HashSet<string>();

        private readonly List<FunctionDeclaration> _nestedFunctions = new List<FunctionDeclaration>();
        private readonly List<LambdaExpression> _nestedLambdas = new List<LambdaExpression>();

        public void Analyze(AstNode node, CodeScope parentScope, IEnumerable<string> additionalDecls = null)
        {
            if (node == null) return;

            DeclaredVariables.Clear();
            UsedVariables.Clear();
            Upvalues.Clear();
            EscapedLocals.Clear();
            NestedUsages.Clear();
            _nestedFunctions.Clear();
            _nestedLambdas.Clear();

            if (additionalDecls != null)
            {
                foreach (var decl in additionalDecls) DeclaredVariables.Add(decl);
            }

            // 1. Collect Declarations (Pre-pass)
            if (node is FunctionDeclaration func)
            {
                for (int i = 0; i < func.Parameters.Count; i++) DeclaredVariables.Add(func.Parameters[i].Name.Value);
                if (func.Name != null) DeclaredVariables.Add(func.Name.Value);
            }

            var declCollector = new DeclarationCollector(DeclaredVariables);
            if (node is FunctionDeclaration f)
            {
                f.Body?.Accept(declCollector);
            }
            else
            {
                node.Accept(declCollector);
            }

            // 2. Visit Body (Collect Usages and Nested Functions)
            if (node is FunctionDeclaration fVisit)
            {
                fVisit.Body?.Accept(this);
            }
            else
            {
                node.Accept(this);
            }

            // 3. Process Nested Functions
            foreach (var nestedFunc in _nestedFunctions)
            {
                AnalyzeNested(nestedFunc, parentScope);
            }
            foreach (var lambda in _nestedLambdas)
            {
                AnalyzeNested(lambda.Function, parentScope);
            }

            // 4. Determine Upvalues
            // IMPORTANT: Only treat as an Upvalue (for closure purposes) if:
            // 1. It is a nested usage (captured by a child)
            // 2. OR the current node is a function (meaning its own usages are its closure's upvalues)
            foreach (var usage in UsedVariables)
            {
                if (!DeclaredVariables.Contains(usage))
                {
                    bool isCaptured = NestedUsages.Contains(usage);
                    bool isFunction = node is FunctionDeclaration || node is LambdaExpression;

                    if (isCaptured || isFunction)
                    {
                        if (parentScope == null)
                        {
                            Upvalues.Add(usage);
                        }
                        else if (parentScope.Resolve(usage, out var val) && val.Type == DeclareType.Variable)
                        {
                            Upvalues.Add(usage);
                        }
                    }
                }
            }
        }

        private void AnalyzeNested(FunctionDeclaration nestedNode, CodeScope parentScope)
        {
            var nestedAnalyzer = new ClosureAnalyzer();
            // Create a temporary scope for the nested analysis if strict resolution is needed,
            // but relying on string matching is often sufficient for captures.
            // However, to correctly resolve 'parentScope' for the child, we should pass *our* scope.
            // But we don't have a fully constructed CodeScope object for *this* function state during analysis.
            // Effectively, for the child, "Upvalues" come from "Me".
            // So: Child.Upvalue checks if name is in Me.Declared OR Me.Upvalue.

            // We can pass 'null' as scope to child and handle resolution manually, 
            // OR we can pass the real parentScope and handle the "intermediate" variables ourselves.
            // VariableCatcher passed 'parentScope.Enter(Function)'.

            // Let's rely on recursive set logic:
            nestedAnalyzer.Analyze(nestedNode, parentScope); // Use parent scope for better resolution

            foreach (var neededVar in nestedAnalyzer.Upvalues)
            {
                // Child needs 'neededVar'.
                if (DeclaredVariables.Contains(neededVar))
                {
                    // It's my local. Capture it!
                    EscapedLocals.Add(neededVar);
                }
                else
                {
                    // It's not my local. I need it from my parent too.
                    // Add to My UsedVariables so step 4 picks it up as an Upvalue for me.
                    UsedVariables.Add(neededVar);
                    NestedUsages.Add(neededVar);
                }
            }
            // Note: child also returns EscapedLocals, but those are internal to child. We don't care.
            // We only care about what child *takes from outside*.

            // Logic Check: The nested analyzer with 'null' parent won't know if a variable is global or upvalue.
            // It will put ALL non-local usages into Upvalues (because Resolve fails).
            // This is actually what we want! We want ALL free variables of the child.
            // Then WE decide if we can satisfy them.
        }


        // --- Visitor Implementations ---

        protected override void VisitName(NameExpression node)
        {
            if (node.Identifier != null)
                UsedVariables.Add(node.Identifier.Value);
        }

        protected override void VisitFunction(FunctionDeclaration node)
        {
            // Do not visit body for usage collection in the CURRENT scope.
            // Nested functions are collected and analyzed separately in AnalyzeNested.
            if (node.Name != null) DeclaredVariables.Add(node.Name.Value);
            _nestedFunctions.Add(node);
        }

        protected override void VisitLambdaExpression(LambdaExpression node)
        {
            _nestedLambdas.Add(node);
        }

        // Pass-throughs that might contain variable refs
        protected override void VisitVarDeclaration(VariableDeclaration node)
        {
            // Initializer is Usage
            node.Initializer?.Accept(this);
            // Visit pattern to identify any nested usages or transformations (if any)
            node.Pattern?.Accept(this);
        }

        protected override void VisitForInStatement(ForInStatement node)
        {
            // Initializer (loop var) is Declaration (handled in pre-pass)
            // But if it has initializer expr? 'for (var x = 0 in ...)' - rare logic but possible?
            // Usually 'for (var x in y)'. 'y' is usage. 'x' is decl.
            node.Iterator?.Accept(this);
            node.Body?.Accept(this);
        }

        // IMPORTANT: We must visit all children that can contain expressions.
        // Base IAstVisitor does this for most nodes. 
        // We rely on base implementation for traversal, only overriding what we need.
        // But NameExpression is a leaf.
    }

    /// <summary>
    /// Simple visitor to collect variable declarations.
    /// </summary>
    internal class DeclarationCollector : IAstVisitor
    {
        private readonly HashSet<string> _decls;
        public DeclarationCollector(HashSet<string> decls) { _decls = decls; }

        protected override void VisitVarDeclaration(VariableDeclaration node)
        {
            if (node.Name != null) _decls.Add(node.Name.Value);
            if (node.Pattern != null) node.Pattern.Accept(this);
        }

        protected override void VisitParameterDeclaration(ParameterDeclaration node)
        {
            _decls.Add(node.Name.Value);
        }

        protected override void VisitFunction(FunctionDeclaration node)
        {
            if (node.Name != null) _decls.Add(node.Name.Value);
        }

        protected override void VisitLambdaExpression(LambdaExpression node) { } // Don't recurse

        protected override void VisitArrayDestructuringPattern(ArrayDestructuringPattern node)
        {
            foreach (var item in node.Elements)
            {
                if (item is NameExpression n) _decls.Add(n.Identifier.Value);
                else if (item is SpreadExpression spread && spread.Expression is NameExpression sn) _decls.Add(sn.Identifier.Value);
                else item?.Accept(this);
            }
        }
        protected override void VisitObjectDestructuringPattern(ObjectDestructuringPattern node)
        {
            foreach (var item in node.Properties) _decls.Add(item.Value);
        }
    }
}
