using System;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// A special expression for the x64 backend.
    /// This expression converts an integer expression result into a floating-point value.
    /// </summary>
    internal class RealConversionExpression : Expression
    {
        public readonly Expression Expression;

        public RealConversionExpression(Expression integerExpression)
        {
            if (integerExpression.Type != PrimitiveType.Int)
                throw new ArgumentException("Expression type must be Int!");

            Expression = integerExpression;
            Type = PrimitiveType.Real;
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            return this;
        }
    }
}
