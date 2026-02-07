using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using System;
using System.Collections.Generic;

namespace AuroraScript.Core
{
    /// <summary>
    /// Specifies the type of a declared object within a scope.
    /// </summary>
    internal enum DeclareType
    {
        /// <summary> A property belonging to a module. </summary>
        Property,

        /// <summary> A local variable within a code segment or function. </summary>
        Variable,

        /// <summary> A global variable accessible throughout the engine. </summary>
        Global
    }

    /// <summary>
    /// Represents a declared object (variable or property) within a <see cref="CodeScope"/>.
    /// </summary>
    internal class DeclareObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeclareObject"/> class.
        /// </summary>
        /// <param name="name">The name of the declaration.</param>
        /// <param name="type">The type of declaration.</param>
        /// <param name="access">The member access level.</param>
        /// <param name="variableNode">The associated AST node, if any.</param>
        public DeclareObject(string name, DeclareType type, MemberAccess access, VariableDeclaration variableNode = null)
        {
            Name = name;
            Type = type;
            Access = access;
            VariableNode = variableNode;
        }

        /// <summary> The name of the declaration. </summary>
        public readonly string Name;
        /// <summary> The type of the declaration (Property, Variable, or Global). </summary>
        public readonly DeclareType Type;
        /// <summary> The index of the variable (e.g., in a local variable table). </summary>
        public readonly int Index;
        /// <summary> The access level (Public, Internal, Private). </summary>
        public readonly MemberAccess Access;
        /// <summary> The AST node representing this variable declaration. </summary>
        public readonly VariableDeclaration VariableNode;
    }

    /// <summary>
    /// Specifies the domain level of a code scope.
    /// </summary>
    internal enum ScopeType
    {
        /// <summary> Global engine scope. </summary>
        Global,
        /// <summary> Module-level scope. </summary>
        Module,
        /// <summary> Function-level or local scope. </summary>
        Function
    }

    /// <summary>
    /// Manages nested scopes and variable declarations during the compilation or analysis phase.
    /// Supports hierarchical lookups and variable resolution.
    /// </summary>
    internal class CodeScope
    {
        /// <summary> Gets the parent scope. </summary>
        public CodeScope Parent { get; private set; }

        /// <summary> Gets the depth of the current scope, starting from 0. </summary>
        public int ScopeDepth { get; private set; } = 0;

        /// <summary> The list of variables declared directly in this scope. </summary>
        public readonly List<DeclareObject> Variables = new();

        /// <summary> Gets the type of this scope. </summary>
        public ScopeType ScopeType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeScope"/> class.
        /// </summary>
        /// <param name="parent">The parent scope, or null for the root scope.</param>
        /// <param name="domain">The domain type of the scope.</param>
        public CodeScope(CodeScope parent, ScopeType domain)
        {
            Parent = parent;
            if (Parent != null)
            {
                ScopeDepth = Parent.ScopeDepth + 1;
            }
            ScopeType = domain;
        }

        /// <summary>
        /// Enters a new child scope of the specified type.
        /// </summary>
        /// <param name="domain">The type of child scope to enter.</param>
        /// <returns>The newly created child scope.</returns>
        public CodeScope Enter(ScopeType domain)
        {
            return new CodeScope(this, domain);
        }

        /// <summary>
        /// Leaves the current scope and returns its parent.
        /// </summary>
        /// <returns>The parent scope.</returns>
        public CodeScope Leave()
        {
            return Parent;
        }

        /// <summary>
        /// Searches for a variable by name only within the current local scope.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <returns>The <see cref="DeclareObject"/> if found; otherwise, null.</returns>
        public DeclareObject FindByNameLocal(string name)
        {
            for (int i = 0; i < Variables.Count; i++)
            {
                if (Variables[i].Name == name) return Variables[i];
            }
            return null;
        }

        /// <summary>
        /// Declares a new variable or property in the current scope.
        /// If a declaration with the same name already exists in this local scope, it is returned.
        /// </summary>
        /// <param name="name">The name to declare.</param>
        /// <param name="type">The type of declaration.</param>
        /// <param name="access">The access level.</param>
        /// <param name="variableNode">The associated AST node.</param>
        /// <returns>The newly created or existing <see cref="DeclareObject"/>.</returns>
        public DeclareObject Declare(string name, DeclareType type, MemberAccess access = MemberAccess.Internal, VariableDeclaration variableNode = null)
        {
            var existing = FindByNameLocal(name);
            if (existing != null) return existing;
            var declare = new DeclareObject(name, type, access, variableNode);
            Variables.Add(declare);
            return declare;
        }

        /// <summary>
        /// Finds the nearest scope of the specified type in the hierarchy.
        /// </summary>
        /// <param name="scopeType">The type of scope to search for.</param>
        /// <returns>The matching <see cref="CodeScope"/> or null if not found.</returns>
        public CodeScope FindScope(ScopeType scopeType)
        {
            if (ScopeType == scopeType) return this;
            return Parent?.FindScope(scopeType);
        }

        /// <summary>
        /// Resolves a name to a <see cref="DeclareObject"/> by searching through the scope hierarchy.
        /// If the name is not found in any scope, it is treated as a global property lookup.
        /// </summary>
        /// <param name="name">The name to resolve.</param>
        /// <param name="value">The resolved <see cref="DeclareObject"/>.</param>
        /// <returns>Always returns true, either with a matched local/parent variable or a fallback Global declaration.</returns>
        public bool Resolve(string name, out DeclareObject value)
        {
            var val = FindByNameLocal(name);
            if (val != null)
            {
                value = val;
                return true;
            }

            if (Parent != null)
            {
                return Parent.Resolve(name, out value);
            }

            // Fallback: If not found in any scope, treat as a Global property/variable
            value = new DeclareObject(name, DeclareType.Global, MemberAccess.Export);
            return true;
        }
    }
}
