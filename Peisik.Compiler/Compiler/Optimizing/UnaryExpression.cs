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
            InternalFunctionId = func.Index;

            switch (func.ReturnType)
            {
                case InternalReturnType.Bool:
                    Type = PrimitiveType.Bool;
                    break;
                case InternalReturnType.Int:
                    Type = PrimitiveType.Int;
                    break;
                case InternalReturnType.Real:
                    Type = PrimitiveType.Real;
                    break;
                case InternalReturnType.RealOrInt:
                case InternalReturnType.SameAsParameter:
                    Type = Expression.Type;
                    break;
                case InternalReturnType.Void:
                default:
                    Type = PrimitiveType.Void;
                    break;
            }
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            return this;
        }
    }
}
