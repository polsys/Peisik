using System;
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

        public Expression ExpressionTree;

        public SideEffect SideEffects;

        internal List<LocalVariable> Locals = new List<LocalVariable>();

        internal LocalVariable ResultValue => Locals[0];

        public OptimizingCompiler Compiler => _compiler;
        private OptimizingCompiler _compiler;

        internal Function(OptimizingCompiler compiler)
        {
            _compiler = compiler;
        }

        /// <summary>
        /// Transforms the parse tree into a compiler representation of the function.
        /// </summary>
        public static Function FromSyntax(FunctionSyntax syntaxTree, OptimizingCompiler compiler)
        {
            var function = new Function(compiler);

            // TODO: Module prefix
            function.FullName = syntaxTree.Name.ToLowerInvariant();

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

            // Build the expression tree
            function.ExpressionTree = Expression.FromSyntax(syntaxTree.CodeBlock, function,
                compiler, new LocalVariableContext(function));

            return function;
        }

        /// <summary>
        /// Generates final code using the code generator associated with the compiler.
        /// </summary>
        public void Emit(OptimizingCompiler compiler)
        {
            // TODO: Compute lifetimes for locals

            // Emit code
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
