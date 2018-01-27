namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// When executed, makes the program die.
    /// This is a special internal function call with zero parameters.
    /// </summary>
    internal class FailFastExpression : Expression
    {
        public FailFastExpression()
        {
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            return this;
        }
    }
}
