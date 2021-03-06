﻿using System;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents an internal function call with a single parameter.
    /// </summary>
    internal class BinaryExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }

        public InternalFunction InternalFunctionId => _internalFunction.Index;
        private InternalFunctionDefinition _internalFunction;

        public BinaryExpression(InternalFunctionDefinition func, Expression left, Expression right)
        {
            Left = left;
            Right = right;
            _internalFunction = func;

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
            if (Left.Fold(compiler) is ConstantExpression left
                && Right.Fold(compiler) is ConstantExpression right)
            {
                return FoldTwoConstants(left, right, compiler);
            }
            return this;
        }

        private Expression FoldTwoConstants(ConstantExpression leftConst,
            ConstantExpression rightConst, OptimizingCompiler compiler)
        {
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
                    case InternalFunction.Equal:
                        return new ConstantExpression(leftLong == rightLong, compiler, Store);
                    case InternalFunction.NotEqual:
                        return new ConstantExpression(leftLong != rightLong, compiler, Store);
                    case InternalFunction.Less:
                        return new ConstantExpression(leftLong < rightLong, compiler, Store);
                    case InternalFunction.LessEqual:
                        return new ConstantExpression(leftLong <= rightLong, compiler, Store);
                    case InternalFunction.Greater:
                        return new ConstantExpression(leftLong > rightLong, compiler, Store);
                    case InternalFunction.GreaterEqual:
                        return new ConstantExpression(leftLong >= rightLong, compiler, Store);
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
                    // These comparisons are of course imprecise when the types are mixed,
                    // but the interpreter does it anyway.
                    case InternalFunction.Equal:
                        return new ConstantExpression(leftDouble == rightDouble, compiler, Store);
                    case InternalFunction.NotEqual:
                        return new ConstantExpression(leftDouble != rightDouble, compiler, Store);
                    case InternalFunction.Less:
                        return new ConstantExpression(leftDouble < rightDouble, compiler, Store);
                    case InternalFunction.LessEqual:
                        return new ConstantExpression(leftDouble <= rightDouble, compiler, Store);
                    case InternalFunction.Greater:
                        return new ConstantExpression(leftDouble > rightDouble, compiler, Store);
                    case InternalFunction.GreaterEqual:
                        return new ConstantExpression(leftDouble >= rightDouble, compiler, Store);
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
                    case InternalFunction.Equal:
                        return new ConstantExpression(leftBool == rightBool, compiler, Store);
                    case InternalFunction.NotEqual:
                        return new ConstantExpression(leftBool != rightBool, compiler, Store);
                }
            }
            
            // Did not fold the expression, but the parameters might be folded
            return new BinaryExpression(_internalFunction, leftConst, rightConst);
        }
    }
}
