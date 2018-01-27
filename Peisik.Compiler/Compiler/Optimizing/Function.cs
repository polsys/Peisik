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

        public string ModulePrefix;

        public Expression ExpressionTree;

        public List<PrimitiveType> ParameterTypes { get; private set; }

        public SideEffect SideEffects;

        internal List<LocalVariable> Locals = new List<LocalVariable>();

        internal LocalVariable ResultValue { get; private set; }

        public OptimizingCompiler Compiler => _compiler;
        private OptimizingCompiler _compiler;
        private FunctionSyntax _syntaxTree;
        private LocalVariableContext _localContext;

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

            // Initialize a local for parameters and the result
            // See the doc comment on ReturnExpression for explanation
            // This is done for simplicity even when returning void
            function._localContext = new LocalVariableContext(function);
            function.ParameterTypes = new List<PrimitiveType>();
            foreach (var param in syntaxTree.Parameters)
            {
                function.ParameterTypes.Add(param.Type);
                var localForParam = function._localContext.AddLocal(param.Name, param.Type, param.Position);
                localForParam.IsParameter = true;
            }
            var resultLocal = new LocalVariable(syntaxTree.ReturnType, "$result");
            function.Locals.Add(resultLocal);
            function.ResultValue = resultLocal;

            // Store the expression tree for later compilation
            function._syntaxTree = syntaxTree;

            return function;
        }

        /// <summary>
        /// Actually compiles the function, performing syntactic analysis but not optimizations.
        /// </summary>
        public void Compile()
        {
            ExpressionTree = Expression.FromSyntax(_syntaxTree.CodeBlock, this, _compiler, _localContext);

            // Void functions may return implicitly, and there must be a return expression in the end
            // TODO: Refactor this check once the guaranteed return check is in place
            List<Expression> expressions = ((SequenceExpression)ExpressionTree).Expressions;
            if (expressions.Count == 0 || !(expressions[expressions.Count - 1] is ReturnExpression))
            {
                ((SequenceExpression)ExpressionTree).Expressions.Add(new ReturnExpression(null));
            }
        }

        /// <summary>
        /// Does analysis related to optimization and some optimization passes.
        /// This is required before this function can be inlined.
        /// </summary>
        /// <param name="optimizationLevel">Controls which optimizations to perform.</param>
        public void AnalyzeAndOptimizePreInlining(Optimization optimizationLevel)
        {
            // Do a first pass of constant folding
            if (optimizationLevel.HasFlag(Optimization.ConstantFolding))
            {
                ExpressionTree = ExpressionTree.Fold(Compiler);
            }

            // TODO: Remove dead code for the first time
            // TODO: Analyze the inlinability and purity of this function
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
