using System;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents a loop statement.
    /// </summary>
    internal class WhileExpression : Expression
    {
        public Expression Condition { get; private set; }
        public Expression Body { get; private set; }

        public WhileExpression(Expression condition, Expression loop)
        {
            Condition = condition;
            Body = loop;
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            return this;
        }
    }
}
