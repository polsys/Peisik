using System;
using System.Collections.Generic;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents a function call.
    /// The call target is always a user-defined function.
    /// </summary>
    internal class FunctionCallExpression : Expression
    {
        public Function Callee { get; private set; }
        public List<Expression> Parameters { get; private set; }
        public bool DiscardResult { get; private set; }

        public FunctionCallExpression(Function callee, List<Expression> parameters,
            bool discardResult, OptimizingCompiler compiler)
        {
            Callee = callee;
            Parameters = parameters;
            DiscardResult = discardResult;
            Type = callee.ResultValue.Type;
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            // CODE SMELL: Immutability violation
            for (var i = 0; i < Parameters.Count; i++)
                Parameters[i] = Parameters[i].Fold(compiler);

            return this;
        }

        protected override void SetStore(LocalVariable newStore, OptimizingCompiler compiler, TokenPosition position = default)
        {
            if (DiscardResult && newStore != null)
                throw new InvalidOperationException("Cannot set Store when DiscardResult is true");

            base.SetStore(newStore, compiler, position);
        }
    }
}
