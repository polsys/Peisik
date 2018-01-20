﻿using System;
using System.Collections.Generic;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Contains the compiler representation of a single function.
    /// </summary>
    internal class Function
    {
        public string FullName;

        public string ModulePrefix;

        public Expression ExpressionTree;

        public SideEffect SideEffects;

        internal List<LocalVariable> Locals = new List<LocalVariable>();

        internal LocalVariable ResultValue => Locals[0];

        public OptimizingCompiler Compiler => _compiler;
        private OptimizingCompiler _compiler;
        private FunctionSyntax _syntaxTree;

        internal Function(OptimizingCompiler compiler)
        {
            _compiler = compiler;
        }

        /// <summary>
        /// Initializes the function object so that it may be referenced, but does not compile the function yet.
        /// </summary>
        public static Function InitializeFromSyntax(FunctionSyntax syntaxTree, OptimizingCompiler compiler, string moduleName)
        {
            var function = new Function(compiler);

            if (string.IsNullOrEmpty(moduleName))
                function.ModulePrefix = "";
            else
                function.ModulePrefix = moduleName + ".";
            function.FullName = (function.ModulePrefix + syntaxTree.Name).ToLowerInvariant();

            // Initialize a local for the result
            // See the doc comment on ReturnExpression for explanation
            // This is done for simplicity even when returning void
            function.Locals = new List<LocalVariable>
            {
                new LocalVariable(syntaxTree.ReturnType, "$result")
            };

            // Initialize locals for parameters
            foreach (var param in syntaxTree.Parameters)
            {
                function.Locals.Add(new LocalVariable(param.Type, param.Name.ToLowerInvariant()) { AssignmentCount = 1 });
            }

            // Store the expression tree for later compilation
            function._syntaxTree = syntaxTree;

            return function;
        }

        /// <summary>
        /// Actually compiles the function, performing syntactic analysis but not optimizations.
        /// </summary>
        public void Compile()
        {
            ExpressionTree = Expression.FromSyntax(_syntaxTree.CodeBlock, this, _compiler, new LocalVariableContext(this));
        }
        
        internal LocalVariable AddVariable(string debugName, PrimitiveType type)
        {
            var local = new LocalVariable(type, debugName + "$" + Locals.Count);
            Locals.Add(local);
            return local;
        }
    }

    /// <summary>
    /// Represents the purity of the function.
    /// </summary>
    internal enum SideEffect
    {
        /// <summary>
        /// No value has been computed yet.
        /// </summary>
        Unknown,
        /// <summary>
        /// This function has side effects.
        /// Side-effecting calls may not be omitted or reordered.
        /// </summary>
        SideEffecting,
        /// <summary>
        /// This function has no side effects or dependencies on global state.
        /// </summary>
        Pure
    }
}