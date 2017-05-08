#pragma once

namespace Peisik
{
    // Defines all the bytecode instruction types.
    enum class Opcode
    {
        Invalid = 0,
        PushConst,
        PushLocal,
        PopLocal,
        PopDiscard,
        Call,
        Return,
        Jump,
        JumpFalse,
        CallI0,
        CallI1,
        CallI2,
        CallI3,
        CallI4,
        CallI5,
        CallI6,
        CallI7,
        OpcodeCount
    };

    // Defines parameter values for CallIx instructions.
    // Directly copied from the compiler source.
    enum class InternalFunction
    {
        Invalid = 0,
        // Globals
        Plus,
        Minus,
        Multiply,
        Divide,
        FloorDivide,
        Mod,
        Equal,
        NotEqual,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,
        And,
        Or,
        Not,
        Xor,
        Print,
        FailFast,
        // Math module
        MathAbs,
        MathAcos,
        MathAsin,
        MathAtan,
        MathCeil,
        MathCos,
        MathExp,
        MathFloor,
        MathLog,
        MathPow,
        MathRound,
        MathSin,
        MathSqrt,
        MathTan
    };

    // Represents a single bytecode instruction with a parameter.
    struct BytecodeOp
    {
        BytecodeOp(Opcode o, short p)
        {
            op = o;
            param = p;
        }

        Opcode op;
        short param;
    };
}