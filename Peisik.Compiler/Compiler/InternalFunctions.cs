using System;
using System.Collections.Generic;

namespace Polsys.Peisik.Compiler
{
    internal static class InternalFunctions
    {
        public readonly static Dictionary<string, InternalFunctionDefinition> Functions 
            = new Dictionary<string, InternalFunctionDefinition>() 
        {
                { "+", new InternalFunctionDefinition(InternalFunction.Plus, 2, 2, ParameterConstraint.AnyNumericType, InternalReturnType.RealOrInt) },
                { "-", new InternalFunctionDefinition(InternalFunction.Minus, 1, 2, ParameterConstraint.AnyNumericType, InternalReturnType.RealOrInt) },
                { "*", new InternalFunctionDefinition(InternalFunction.Multiply, 2, 2, ParameterConstraint.AnyNumericType, InternalReturnType.RealOrInt) },
                { "/", new InternalFunctionDefinition(InternalFunction.Divide, 2, 2, ParameterConstraint.AnyNumericType, InternalReturnType.Real) },
                { "//", new InternalFunctionDefinition(InternalFunction.FloorDivide, 2, 2, ParameterConstraint.AnyNumericType, InternalReturnType.Int) },
                { "%", new InternalFunctionDefinition(InternalFunction.Mod, 2, 2, ParameterConstraint.Int, InternalReturnType.Int) },
                { "<", new InternalFunctionDefinition(InternalFunction.Less, 2, 2, ParameterConstraint.AnyNumericType, InternalReturnType.Bool) },
                { "<=", new InternalFunctionDefinition(InternalFunction.LessEqual, 2, 2, ParameterConstraint.AnyNumericType, InternalReturnType.Bool) },
                { ">", new InternalFunctionDefinition(InternalFunction.Greater, 2, 2, ParameterConstraint.AnyNumericType, InternalReturnType.Bool) },
                { ">=", new InternalFunctionDefinition(InternalFunction.GreaterEqual, 2, 2, ParameterConstraint.AnyNumericType, InternalReturnType.Bool) },
                { "==", new InternalFunctionDefinition(InternalFunction.Equal, 2, 2, ParameterConstraint.SameType, InternalReturnType.Bool) },
                { "!=", new InternalFunctionDefinition(InternalFunction.NotEqual, 2, 2, ParameterConstraint.SameType, InternalReturnType.Bool) },
                { "and", new InternalFunctionDefinition(InternalFunction.And, 2, 2, ParameterConstraint.BoolOrInt, InternalReturnType.SameAsParameter) },
                { "or", new InternalFunctionDefinition(InternalFunction.Or, 2, 2, ParameterConstraint.BoolOrInt, InternalReturnType.SameAsParameter) },
                { "xor", new InternalFunctionDefinition(InternalFunction.Xor, 2, 2, ParameterConstraint.BoolOrInt, InternalReturnType.SameAsParameter) },
                { "not", new InternalFunctionDefinition(InternalFunction.Not, 1, 1, ParameterConstraint.BoolOrInt, InternalReturnType.SameAsParameter) },
                { "print", new InternalFunctionDefinition(InternalFunction.Print, 0, 7, ParameterConstraint.None, InternalReturnType.Void) },
                { "failfast", new InternalFunctionDefinition(InternalFunction.FailFast, 0, 0, ParameterConstraint.None, InternalReturnType.Void) },
                { "math.abs", new InternalFunctionDefinition(InternalFunction.MathAbs, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.RealOrInt) },
                { "math.acos", new InternalFunctionDefinition(InternalFunction.MathAcos, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.Real) },
                { "math.asin", new InternalFunctionDefinition(InternalFunction.MathAsin, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.Real) },
                { "math.atan", new InternalFunctionDefinition(InternalFunction.MathAtan, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.Real) },
                { "math.ceil", new InternalFunctionDefinition(InternalFunction.MathCeil, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.Int) },
                { "math.cos", new InternalFunctionDefinition(InternalFunction.MathCos, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.Real) },
                { "math.exp", new InternalFunctionDefinition(InternalFunction.MathExp, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.Real) },
                { "math.floor", new InternalFunctionDefinition(InternalFunction.MathFloor, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.Int) },
                { "math.log", new InternalFunctionDefinition(InternalFunction.MathLog, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.Real) },
                { "math.pow", new InternalFunctionDefinition(InternalFunction.MathPow, 2, 2, ParameterConstraint.AnyNumericType, InternalReturnType.Real) },
                { "math.round", new InternalFunctionDefinition(InternalFunction.MathRound, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.Int) },
                { "math.sin", new InternalFunctionDefinition(InternalFunction.MathSin, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.Real) },
                { "math.sqrt", new InternalFunctionDefinition(InternalFunction.MathSqrt, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.Real) },
                { "math.tan", new InternalFunctionDefinition(InternalFunction.MathTan, 1, 1, ParameterConstraint.AnyNumericType, InternalReturnType.Real) },
        };
    }

    internal class InternalFunctionDefinition
    {
        public readonly InternalFunction Index;
        public readonly int MaxParameters;
        public readonly int MinParameters;
        public readonly ParameterConstraint ParamConstraint;
        public readonly InternalReturnType ReturnType;

        public InternalFunctionDefinition(InternalFunction index, int minParams, int maxParams, 
            ParameterConstraint paramConstraint, InternalReturnType returnType)
        {
            Index = index;
            MaxParameters = maxParams;
            MinParameters = minParams;
            ParamConstraint = paramConstraint;
            ReturnType = returnType;
        }
    }

    internal enum ParameterConstraint
    {
        /// <summary>
        /// The parameters may be of any type.
        /// </summary>
        None,
        /// <summary>
        /// The parameters must be some numeric type, but not necessarily the same.
        /// </summary>
        AnyNumericType,
        /// <summary>
        /// Either all bools or all any numeric type.
        /// </summary>
        SameType,
        /// <summary>
        /// Either all bools or all ints.
        /// </summary>
        BoolOrInt,
        /// <summary>
        /// The parameters must be integers.
        /// </summary>
        Int
    }

    internal enum InternalReturnType
    {
        Void,
        Int,
        Real,
        Bool,
        SameAsParameter,
        /// <summary>
        /// Real if there are real parameters.
        /// </summary>
        RealOrInt
    }
}
