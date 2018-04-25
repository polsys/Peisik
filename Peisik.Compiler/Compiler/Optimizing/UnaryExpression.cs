namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents an internal function call with a single parameter.
    /// </summary>
    internal class UnaryExpression : Expression
    {
        public Expression Expression { get; private set; }

        public InternalFunction InternalFunctionId => _internalFunction.Index;
        private InternalFunctionDefinition _internalFunction;

        public UnaryExpression(InternalFunctionDefinition func, Expression parameter)
        {
            Expression = parameter;
            _internalFunction = func;

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
            var folded = Expression.Fold(compiler);
            
            if (folded is ConstantExpression constant)
            {
                if (InternalFunctionId == InternalFunction.Minus)
                {
                    if (constant.Value is long longValue)
                    {
                        return new ConstantExpression(-longValue, compiler, Store);
                    }
                    else if (constant.Value is double doubleValue)
                    {
                        return new ConstantExpression(-doubleValue, compiler, Store);
                    }
                }
                else if (InternalFunctionId == InternalFunction.Not)
                {
                    if (constant.Value is bool boolValue)
                    {
                        return new ConstantExpression(!boolValue, compiler, Store);
                    }
                    else if (constant.Value is long longValue)
                    {
                        return new ConstantExpression(~longValue, compiler, Store);
                    }
                }
            }

            // Could not fold, but the inner expression might be simplified now
            return new UnaryExpression(_internalFunction, folded);
        }
    }
}
