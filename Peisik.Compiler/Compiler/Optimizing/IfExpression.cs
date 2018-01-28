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
            Expression condition = Condition.Fold(compiler);

            // If the condition is always true or false, omit the unused part
            if (condition is ConstantExpression constant && constant.Value is bool value)
            {
                if (value)
                    return ThenExpression.Fold(compiler);
                else
                    return ElseExpression.Fold(compiler);
            }

            // Else, just fold as much as possible
            return new IfExpression(condition, ThenExpression.Fold(compiler), ElseExpression.Fold(compiler));
        }
    }
}
