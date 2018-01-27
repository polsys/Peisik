using System;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents an internal function call with a single parameter.
    /// </summary>
    internal class BinaryExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }

        public InternalFunction InternalFunctionId { get; private set; }

        public BinaryExpression(InternalFunctionDefinition func, Expression left, Expression right)
        {
            Left = left;
            Right = right;
            InternalFunctionId = func.Index;

            // There are several options for the resulting type
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
                    if (Left.Type == PrimitiveType.Real || Right.Type == PrimitiveType.Real)
                        Type = PrimitiveType.Real;
                    else
                        Type = PrimitiveType.Int;
                    break;
                case InternalReturnType.SameAsParameter:
                    // Assuming that Left.Type == Right.Type
                    Type = Left.Type;
                    break;
                default:
                    throw new NotImplementedException("Unimplemented InternalReturnType");
            }
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            if (Left is ConstantExpression && Right is ConstantExpression)
            {
                return FoldTwoConstants(compiler);
            }
            return this;
        }

        private Expression FoldTwoConstants(OptimizingCompiler compiler)
        {
            var leftConst = (ConstantExpression)Left;
            var rightConst = (ConstantExpression)Right;

            if (InternalFunctionId == InternalFunction.Plus)
            {
                // The parameters may be either all integers, in which case the result is an integer,
                // or all floats / mixed, in which case the result is a floating-point value.
                if (leftConst.Type == PrimitiveType.Int && rightConst.Type == PrimitiveType.Int)
                {
                    return new ConstantExpression((long)leftConst.Value + (long)rightConst.Value, compiler, Store);
                }
                else
                {
                    return new ConstantExpression(Convert.ToDouble(leftConst.Value) + Convert.ToDouble(rightConst.Value), compiler, Store);
                }
            }
            else if (InternalFunctionId == InternalFunction.Equal)
            {
                // Overloaded object.Equals should do the comparison correctly
                return new ConstantExpression(leftConst.Value.Equals(rightConst.Value), compiler, Store);
            }
            // TODO: Implement more

            return this;
        }
    }
}
