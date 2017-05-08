namespace Polsys.Peisik.Compiler
{
    internal struct BytecodeOp
    {
        public Opcode Opcode;
        public short Parameter;

        public BytecodeOp(Opcode op, short param)
        {
            Opcode = op;
            Parameter = param;
        }
    }

    // *****
    // The enums below are defined in Bytecode.h for the interpreter.
    // Obviously, they must be kept in sync.
    //
    // Also bump the bytecode version in CompiledProgram when these are changed!
    // *****

    internal enum Opcode : short
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
        CallI7
    }

    internal enum InternalFunction : short
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
    }
}
