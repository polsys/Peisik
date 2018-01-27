using System;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents a conditional statement.
    /// </summary>
    internal class IfExpression : Expression
    {
        public Expression Condition { get; private set; }
        public Expression ThenExpression { get; private set; }
        public Expression ElseExpression { get; private set; }

        public IfExpression(Expression condition, Expression thenExpr, Expression elseExpr)
        {
            Condition = condition;
            ThenExpression = thenExpr;
            ElseExpression = elseExpr;
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            return new IfExpression(Condition.Fold(compiler), ThenExpression.Fold(compiler), ElseExpression.Fold(compiler));
        }
    }
}
