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

            // For many functions, the parameters may be either all integers, in which case the result
            // is an integer, or all floats / mixed, in which case the result is a floating-point value.
            // Some accept all bools, and some just want everything to be the same type.
            if (leftConst.Type == PrimitiveType.Int && rightConst.Type == PrimitiveType.Int)
            {
                var leftLong = (long)leftConst.Value;
                var rightLong = (long)rightConst.Value;

                switch (InternalFunctionId)
                {
                    case InternalFunction.Plus:
                        return new ConstantExpression(leftLong + rightLong, compiler, Store);
                    case InternalFunction.Minus:
                        return new ConstantExpression(leftLong - rightLong, compiler, Store);
                    case InternalFunction.Multiply:
                        return new ConstantExpression(leftLong * rightLong, compiler, Store);
                    case InternalFunction.Divide:
                        if (rightLong == 0)
                            break;
                        return new ConstantExpression((double)leftLong
                            / rightLong, compiler, Store);
                    case InternalFunction.FloorDivide:
                        if (rightLong == 0)
                            break;
                        return new ConstantExpression(leftLong / rightLong, compiler, Store);
                    case InternalFunction.Mod:
                        if (rightLong == 0)
                            break;

                        // The result is always non-negative
                        var value = leftLong % rightLong;
                        if (value < 0)
                            value += Math.Abs(rightLong);

                        return new ConstantExpression(value, compiler, Store);
                    case InternalFunction.And:
                        return new ConstantExpression(leftLong & rightLong, compiler, Store);
                    case InternalFunction.Or:
                        return new ConstantExpression(leftLong | rightLong, compiler, Store);
                    case InternalFunction.Xor:
                        return new ConstantExpression(leftLong ^ rightLong, compiler, Store);
                }
            }
            else if ((leftConst.Type == PrimitiveType.Int && rightConst.Type == PrimitiveType.Real)
                || (leftConst.Type == PrimitiveType.Real && rightConst.Type == PrimitiveType.Int)
                || (leftConst.Type == PrimitiveType.Real && rightConst.Type == PrimitiveType.Real))
            {
                var leftDouble = Convert.ToDouble(leftConst.Value);
                var rightDouble = Convert.ToDouble(rightConst.Value);

                switch (InternalFunctionId)
                {
                    case InternalFunction.Plus:
                        return new ConstantExpression(leftDouble + rightDouble, compiler, Store);
                    case InternalFunction.Minus:
                        return new ConstantExpression(leftDouble - rightDouble, compiler, Store);
                    case InternalFunction.Multiply:
                        return new ConstantExpression(leftDouble * rightDouble, compiler, Store);
                    case InternalFunction.Divide:
                        if (rightDouble == 0.0)
                            break;
                        return new ConstantExpression(leftDouble / rightDouble, compiler, Store);
                    case InternalFunction.FloorDivide:
                        if (rightDouble == 0.0)
                            break;
                        return new ConstantExpression((long)(leftDouble / rightDouble), compiler, Store);
                }
            }
            else if (leftConst.Type == PrimitiveType.Bool && rightConst.Type == PrimitiveType.Bool)
            {
                var leftBool = (bool)leftConst.Value;
                var rightBool = (bool)rightConst.Value;

                switch (InternalFunctionId)
                {
                    case InternalFunction.And:
                        return new ConstantExpression(leftBool & rightBool, compiler, Store);
                    case InternalFunction.Or:
                        return new ConstantExpression(leftBool | rightBool, compiler, Store);
                    case InternalFunction.Xor:
                        return new ConstantExpression(leftBool ^ rightBool, compiler, Store);
                }
            }
            
            if (InternalFunctionId == InternalFunction.Equal)
            {
                // Overloaded object.Equals should do the comparison correctly
                return new ConstantExpression(leftConst.Value.Equals(rightConst.Value), compiler, Store);
            }
            // TODO: Implement more

            return this;
        }
    }
}
