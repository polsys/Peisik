using System.Collections.Generic;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents a Print() function call.
    /// This internal function accepts variable number of parameters.
    /// </summary>
    internal class PrintExpression : Expression
    {
        public List<Expression> Expressions { get; private set; }

        public PrintExpression(List<Expression> parameters)
        {
            Expressions = parameters;
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            // CODE SMELL: Immutability violation
            for (var i = 0; i < Expressions.Count; i++)
                Expressions[i] = Expressions[i].Fold(compiler);

            return this;
        }
    }
}
