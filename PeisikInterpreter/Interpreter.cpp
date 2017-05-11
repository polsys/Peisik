#include "pch.h"
#include "Bytecode.h"
#include "InternalFunctions.h"
#include "Interpreter.h"
#include "PeisikException.h"
#include "PObject.h"
#include "Program.h"

using namespace Peisik;

// Forward declarations
static std::string OpcodeToString(const Opcode op);
static PObject PopTop(std::stack<PObject>& stack);
static void PrintObject(const PObject& object);

Interpreter::Interpreter(Program program)
    : m_program(program), m_opCounts(static_cast<size_t>(Opcode::OpcodeCount), 0), m_trace(false),
    m_shouldHalt(false), m_iCallParams()
{
}

// Some magic to reduce code repeat in DispatchInternalCall
static PObject CallOneArgFunc(std::stack<PObject>& params,
    PObject(*func)(const PObject& value))
{
    if (params.size() != 1)
        throw InterpreterException("The called function expects 1 parameter.");
    return func(params.top());
}

static PObject CallTwoArgFunc(std::stack<PObject>& params,
    PObject(*func)(const PObject& left, const PObject& right))
{
    if (params.size() != 2)
        throw InterpreterException("The called function expects 2 parameters.");
    auto left = PopTop(params);
    auto right = PopTop(params);
    return func(left, right);
}

PObject Interpreter::DispatchInternalCall(const InternalFunction funcIndex, std::stack<PObject>& params)
{
    switch (funcIndex)
    {
    case InternalFunction::Plus:
        return InternalFunc::Plus(params);
    case InternalFunction::Minus:
        if (params.size() == 1)
        {
            return InternalFunc::Minus(params.top());
        }
        else if (params.size() == 2)
        {
            auto left = PopTop(params);
            auto right = PopTop(params);
            return InternalFunc::Minus(left, right);
        }
        else
        {
            throw InterpreterException("- expects 1 or 2 parameters.");
        }
    case InternalFunction::Multiply:
        return CallTwoArgFunc(params, InternalFunc::Multiply);
    case InternalFunction::Divide:
        return CallTwoArgFunc(params, InternalFunc::Divide);
    case InternalFunction::FloorDivide:
        return CallTwoArgFunc(params, InternalFunc::FloorDivide);
    case InternalFunction::Mod:
        return CallTwoArgFunc(params, InternalFunc::Mod);
    case InternalFunction::Less:
        return CallTwoArgFunc(params, InternalFunc::Less);
    case InternalFunction::LessEqual:
        return CallTwoArgFunc(params, InternalFunc::LessEqual);
    case InternalFunction::Greater:
        return CallTwoArgFunc(params, InternalFunc::Greater);
    case InternalFunction::GreaterEqual:
        return CallTwoArgFunc(params, InternalFunc::GreaterEqual);
    case InternalFunction::Equal:
        return CallTwoArgFunc(params, InternalFunc::Equal);
    case InternalFunction::NotEqual:
        return CallTwoArgFunc(params, InternalFunc::NotEqual);
    case InternalFunction::And:
        return CallTwoArgFunc(params, InternalFunc::And);
    case InternalFunction::Or:
        return CallTwoArgFunc(params, InternalFunc::Or);
    case InternalFunction::Xor:
        return CallTwoArgFunc(params, InternalFunc::Xor);
    case InternalFunction::Not:
        return CallOneArgFunc(params, InternalFunc::Not);
    case InternalFunction::Print:
    {
        while (!params.empty())
        {
            PrintObject(PopTop(params));
            if (!params.empty())
                std::cout << " ";
        }
        std::cout << std::endl;
        return PObject(PrimitiveType::Void, 0);
    }
    case InternalFunction::FailFast:
    {
        std::cout << "The program requested termination by calling FailFast. Stack trace:" << std::endl;
        auto stack(m_stack);
        while (!stack.empty())
        {
            auto &frame = stack.top();
            std::cout << "Function " << frame.function.GetFunctionIndex() << ", instruction " << frame.programCounter - 1 << std::endl;
            stack.pop();
        }
        m_shouldHalt = true;
        return PObject(PrimitiveType::Void, 0);
    }
    case InternalFunction::MathAbs:
        return CallOneArgFunc(params, InternalFunc::MathAbs);
    case InternalFunction::MathAcos:
        return CallOneArgFunc(params, InternalFunc::MathAcos);
    case InternalFunction::MathAsin:
        return CallOneArgFunc(params, InternalFunc::MathAsin);
    case InternalFunction::MathAtan:
        return CallOneArgFunc(params, InternalFunc::MathAtan);
    case InternalFunction::MathCeil:
        return CallOneArgFunc(params, InternalFunc::MathCeil);
    case InternalFunction::MathCos:
        return CallOneArgFunc(params, InternalFunc::MathCos);
    case InternalFunction::MathExp:
        return CallOneArgFunc(params, InternalFunc::MathExp);
    case InternalFunction::MathFloor:
        return CallOneArgFunc(params, InternalFunc::MathFloor);
    case InternalFunction::MathLog:
        return CallOneArgFunc(params, InternalFunc::MathLog);
    case InternalFunction::MathPow:
        return CallTwoArgFunc(params, InternalFunc::MathPow);
    case InternalFunction::MathRound:
        return CallOneArgFunc(params, InternalFunc::MathRound);
    case InternalFunction::MathSin:
        return CallOneArgFunc(params, InternalFunc::MathSin);
    case InternalFunction::MathSqrt:
        return CallOneArgFunc(params, InternalFunc::MathSqrt);
    case InternalFunction::MathTan:
        return CallOneArgFunc(params, InternalFunc::MathTan);
    default:
        std::cout << "Trying to call internal function " << static_cast<short>(funcIndex) << std::endl;
        throw InterpreterException("Unknown internal function.");
    }
}

void Interpreter::Execute()
{
    // Create the initial frame
    m_stack.push(PrepareFrameForFunction(m_program.GetFunction(m_program.GetMainFunctionIndex())));

    // Run the main loop until done
    while (!m_shouldHalt)
    {
        // References to the current frame and instruction
        StackFrame& frame = m_stack.top();
        auto& bytecode = frame.function.GetBytecode();
        if (frame.programCounter >= bytecode.size())
            throw InterpreterException("Out of bytecode bounds.");
        BytecodeOp op = bytecode[frame.programCounter];

        // Increase the program counter now
        frame.programCounter++;

        // Tracing and opcode counting
        m_opCounts[static_cast<int>(op.op)]++;
        if (m_trace)
        {
            std::cout << "* "
                << std::right << std::setw(3) << frame.function.GetFunctionIndex() << ":"
                << std::left << std::setw(3) << (frame.programCounter - 1)
                << " " << std::setw(12) << OpcodeToString(op.op)
                << " " << op.param << std::endl;
        }

        switch (op.op)
        {
        case Opcode::Call:
        {
            const Function& func = m_program.GetFunction(op.param);
            auto callFrame = PrepareFrameForFunction(func);
            // Parameters are passed as locals.
            // Since they are evaluated left to right, they are in reverse order on the stack.
            for (int i = func.GetParameterCount(); i > 0; i--)
            {
                callFrame.locals[i - 1] = frame.functionStack.top();
                frame.functionStack.pop();
            }

            // Optimization: If this is a tail call, turn the call into a jump by removing the current frame.
            // Because this is implemented in the interpreter, no compiler magic is needed.
            // On the other hand, stack traces may become more inaccurate... but they weren't exactly useful in the first place.
            if (op.param == frame.function.GetFunctionIndex() && bytecode[frame.programCounter].op == Opcode::Return)
            {
                m_stack.pop();
            }
            m_stack.push(callFrame);
            break;
        }

        /* CALLIx cases have intentional fallthroughs */
        case Opcode::CallI7:
            m_iCallParams.push(PopTop(frame.functionStack));
        case Opcode::CallI6:
            m_iCallParams.push(PopTop(frame.functionStack));
        case Opcode::CallI5:
            m_iCallParams.push(PopTop(frame.functionStack));
        case Opcode::CallI4:
            m_iCallParams.push(PopTop(frame.functionStack));
        case Opcode::CallI3:
            m_iCallParams.push(PopTop(frame.functionStack));
        case Opcode::CallI2:
            m_iCallParams.push(PopTop(frame.functionStack));
        case Opcode::CallI1:
            m_iCallParams.push(PopTop(frame.functionStack));
        case Opcode::CallI0:
        {
            PObject callResult = DispatchInternalCall(static_cast<InternalFunction>(op.param), m_iCallParams);
            // Clean up the param stack since it is cached
            while (!m_iCallParams.empty())
            {
                m_iCallParams.pop();
            }
            if (callResult.GetType() != PrimitiveType::Void)
                frame.functionStack.push(callResult);
            break;
        }

        case Opcode::Jump:
            frame.programCounter += op.param - 1; // -1 because it was already incremented
            break;
        case Opcode::JumpFalse:
            if (PopTop(frame.functionStack).GetBoolValue() == false)
            {
                frame.programCounter += op.param - 1; // -1 because it was already incremented
            }
            break;
        case Opcode::PopDiscard:
            frame.functionStack.pop();
            break;
        case Opcode::PopLocal:
            frame.locals[op.param].SetValue(frame.functionStack.top());
            frame.functionStack.pop();
            break;
        case Opcode::PushConst:
            frame.functionStack.push(m_program.GetConstant(op.param));
            break;
        case Opcode::PushLocal:
            frame.functionStack.push(frame.locals[op.param]);
            break;
        case Opcode::Return:
            if (m_stack.size() == 1)
            {
                // If this is the main function, print the possible return value
                if (frame.function.GetReturnType() != PrimitiveType::Void)
                {
                    PrintObject(frame.functionStack.top());
                    std::cout << std::endl;
                }
                m_shouldHalt = true;
                break;
            }
            else
            {
                // Else, pop off the frame to return to the caller
                if (frame.function.GetReturnType() != PrimitiveType::Void)
                {
                    // Move the return value onto the caller's stack
                    PObject returnValue = frame.functionStack.top();
                    m_stack.pop();
                    m_stack.top().functionStack.push(returnValue);
                }
                else
                {
                    m_stack.pop();
                }
                break;
            }
        default:
            throw InterpreterException("Unknown opcode");
        }
    }
}

Interpreter::StackFrame Interpreter::PrepareFrameForFunction(const Function & func) const
{
    auto frame = StackFrame(func);
    
    // Initialize locals
    auto localTypes = func.GetLocalTypes();
    frame.locals.reserve(localTypes.size());
    for (auto type : localTypes)
        frame.locals.push_back(PObject(type, 0));

    return frame;
}

void Interpreter::PrintOpCount() const
{
    struct OpHits
    {
        int hits;
        Opcode op;

        OpHits(Opcode o, int h) : hits(h), op(o) { };
    };

    // Sort the ops by their hit count
    size_t total = 0;
    std::vector<OpHits> sortedOps;
    sortedOps.reserve(static_cast<size_t>(Opcode::OpcodeCount));
    for (auto i = 1; i < static_cast<int>(Opcode::OpcodeCount); i++)
    {
        total += m_opCounts[i];
        sortedOps.push_back(OpHits(static_cast<Opcode>(i), m_opCounts[i]));
    }
    std::sort(sortedOps.begin(), sortedOps.end(), [](const OpHits& a, const OpHits& b)
    {
        return a.hits > b.hits;
    });

    // Output
    std::cout << "-- Executed opcode count: " << total << std::endl;
    for (auto oh : sortedOps)
    {
        std::cout << std::left << std::setw(12) << OpcodeToString(oh.op) << oh.hits << std::endl;
    }
}

static PObject PopTop(std::stack<PObject>& stack)
{
    // The Poptop hums beautifully to confuse its prey.
    auto object = stack.top();
    stack.pop();

    return object;
}

static void PrintObject(const PObject& object)
{
    switch (object.GetType())
    {
    case PrimitiveType::Bool:
        if (object.GetBoolValue())
            std::cout << "true";
        else
            std::cout << "false";
        break;
    case PrimitiveType::Int:
        std::cout << object.GetIntValue();
        break;
    case PrimitiveType::Real:
        std::cout << object.GetRealValue();
        break;
    default:
        throw std::invalid_argument("Unimplemented type in PrintObject().");
    }
}

static std::string OpcodeToString(const Opcode op)
{
    switch (op)
    {
    case Opcode::Call: return "Call";
    case Opcode::CallI0: return "CallI0";
    case Opcode::CallI1: return "CallI1";
    case Opcode::CallI2: return "CallI2";
    case Opcode::CallI3: return "CallI3";
    case Opcode::CallI4: return "CallI4";
    case Opcode::CallI5: return "CallI5";
    case Opcode::CallI6: return "CallI6";
    case Opcode::CallI7: return "CallI7";
    case Opcode::Jump: return "Jump";
    case Opcode::JumpFalse: return "JumpFalse";
    case Opcode::PopDiscard: return "PopDiscard";
    case Opcode::PopLocal: return "PopLocal";
    case Opcode::PushConst: return "PushConst";
    case Opcode::PushLocal: return "PushLocal";
    case Opcode::Return: return "Return";
    default:
        return "????";
    }
}