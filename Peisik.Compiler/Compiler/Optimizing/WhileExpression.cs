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
            // Omit always-false loops altogether
            var foldedCondition = Condition.Fold(compiler);
            if (foldedCondition is ConstantExpression constantCondition && constantCondition.Value is bool value)
            {
                if (value == false)
                {
                    return null;
                }
            }
            
            return new WhileExpression(Condition.Fold(compiler), Body.Fold(compiler));
        }
    }
}
