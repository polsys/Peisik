using System;
using System.Collections.Generic;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Register backend for native x64 code.
    /// </summary>
    internal class X64RegisterBackend : RegisterBackend
    {
        // The free registers are stored in a stack so that adjacent operations may reuse registers
        private Stack<X64Register> _freeGeneralRegisters = new Stack<X64Register>();
        private Stack<X64Register> _freeSseRegisters = new Stack<X64Register>();

        // Currently the stack locations are never shared
        private int _stackLevel;

        public X64RegisterBackend()
        {
            // Initialize the free register stacks in order of increasing preference
            _freeGeneralRegisters.Push(X64Register.Rdx);
            _freeGeneralRegisters.Push(X64Register.Rcx);
            _freeGeneralRegisters.Push(X64Register.Rbx);

            _freeSseRegisters.Push(X64Register.Xmm3);
            _freeSseRegisters.Push(X64Register.Xmm2);
            _freeSseRegisters.Push(X64Register.Xmm1);

            // TODO: These lists are short for testing - add the remaining registers soon
            // TODO: Do a test run using only REX encoded registers
        }

        public override int GetLocation(PrimitiveType type, bool isParameter, out bool onStack)
        {
            if (type == PrimitiveType.Real)
            {
                if (_freeSseRegisters.Count > 0)
                {
                    onStack = false;
                    return (int)_freeSseRegisters.Pop();
                }
                else
                {
                    onStack = true;
                    _stackLevel += 8;
                    return _stackLevel - 8;
                }
            }
            else
            {
                if (_freeGeneralRegisters.Count > 0)
                {
                    onStack = false;
                    return (int)_freeGeneralRegisters.Pop();
                }
                else
                {
                    onStack = true;
                    _stackLevel += 8;
                    return _stackLevel - 8;
                }
            }
        }

        public override void ReturnLocation(int location)
        {
            if (location < -1)
            {
                if (location <= (int)X64Register.Xmm0)
                {
                    _freeSseRegisters.Push((X64Register)location);
                }
                else
                {
                    _freeGeneralRegisters.Push((X64Register)location);
                }
            }
        }
    }

    /// <summary>
    /// X64 registers available to programs.
    /// Not all may be allocated for local variables.
    /// These are negative integers, whereas non-negative integers signal stack positions.
    /// </summary>
    internal enum X64Register
    {
        Invalid = -1,
        /// <summary>
        /// Reserved as a temporary register.
        /// </summary>
        Rax = -2,
        Rbx = -3,
        Rcx = -4,
        Rdx = -5,
        Rdi = -6,
        Rsi = -7,
        /// <summary>
        /// Reserved.
        /// </summary>
        Rbp = -8,
        /// <summary>
        /// Stack pointer, reserved.
        /// </summary>
        Rsp = -9,
        R8 = -10,
        R9 = -11,
        R10 = -12,
        R11 = -13,
        R12 = -14,
        R13 = -15,
        R14 = -16,
        R15 = -17,
        /// <summary>
        /// Reserved as a temporary register.
        /// </summary>
        Xmm0 = -20,
        Xmm1 = -21,
        Xmm2 = -22,
        Xmm3 = -23,
        Xmm4 = -24,
        Xmm5 = -25,
        Xmm6 = -26,
        Xmm7 = -27,
    }
}
