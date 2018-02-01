namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents a point where the function will return to its caller.
    /// </summary>
    /// <remarks>
    /// If the containing function is inlined, this will become a local store.
    /// The variables are renamed so that the result variable becomes a local in the inlining function.
    /// </remarks>
    internal class ReturnExpression : Expression
    {
        /// <summary>
        /// The return value, which may be null.
        /// </summary>
        public Expression Value;

        public ReturnExpression(Expression returnValue)
        {
            Value = returnValue;
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            if (Value != null)
                return new ReturnExpression(Value.Fold(compiler));
            else
                return this;
        }

        internal override Expression TryInlineLocalAssignment(Expression assignment)
        {
            if (Value is LocalLoadExpression load && load.Local == assignment.Store)
            {
                return new ReturnExpression(assignment);
            }
            return null;
        }
    }
}
