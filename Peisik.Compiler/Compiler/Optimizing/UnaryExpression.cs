namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents an internal function call with a single parameter.
    /// </summary>
    internal class UnaryExpression : Expression
    {
        public Expression Expression { get; private set; }

        public InternalFunction InternalFunctionId { get; private set; }

        public UnaryExpression(InternalFunctionDefinition func, Expression parameter)
        {
            Expression = parameter;
            Type = Expression.Type;
            InternalFunctionId = func.Index;
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            return this;
        }
    }
}
