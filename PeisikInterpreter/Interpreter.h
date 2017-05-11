#pragma once

#include <iostream>
#include <stack>
#include "Program.h"

namespace Peisik
{
    class Interpreter
    {
    public:
        Interpreter(Program program);
        ~Interpreter() = default;

        // Runs the program.
        void Execute();

        // Prints an instruction count report
        void PrintOpCount() const;

        // Controls whether to output each instruction to the standard output.
        void SetTrace(bool value)
        {
            m_trace = value;
        }

    private:
        bool m_trace;

        std::vector<int> m_opCounts;
        Program m_program;
        bool m_shouldHalt;

        class StackFrame
        {
        public:
            StackFrame(const Function& func)
                : function(func), programCounter(0)
            {
            };

            const Function& function;
            std::stack<PObject> functionStack;
            std::vector<PObject> locals;
            uint32_t programCounter;
        };
        std::stack<StackFrame> m_stack;
        // Cached stack for internal call parameters
        std::stack<PObject> m_iCallParams;

        PObject DispatchInternalCall(const InternalFunction funcIndex, std::stack<PObject>& params);
        StackFrame PrepareFrameForFunction(const Function& func) const;
    };
}
