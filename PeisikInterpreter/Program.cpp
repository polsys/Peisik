#include "pch.h"
#include "Bytecode.h"
#include "PeisikException.h"
#include "Program.h"

using namespace Peisik;

/*
 * Function
 */

const std::vector<BytecodeOp>& Function::GetBytecode() const
{
    return m_bytecode;
}

short Peisik::Function::GetFunctionIndex() const
{
    return m_functionIndex;
}

const std::vector<PrimitiveType>& Peisik::Function::GetLocalTypes() const
{
    return m_localTypes;
}

short Peisik::Function::GetParameterCount() const
{
    return m_parameterCount;
}

PrimitiveType Function::GetReturnType() const
{
    return m_returnType;
}



/*
* Program
*/

PObject Program::GetConstant(short index) const
{
    if (index < 0 || index >= GetConstantCount())
    {
        throw std::range_error("Constant index out of range.");
    }

    return m_constants[index];
}

short Program::GetConstantCount() const
{
    return static_cast<short>(m_constants.size());
}

const Function& Program::GetFunction(short index) const
{
    if (index < 0 || index >= GetFunctionCount())
    {
        throw std::range_error("Function index out of range.");
    }

    return m_functions[index];
}

short Program::GetFunctionCount() const
{
    return static_cast<short>(m_functions.size());
}

short Program::GetMainFunctionIndex() const
{
    return m_mainFunctionIndex;
}


/*
 * Global namespace
 */

static void AssertValidType(short type)
{
    if (type <= (short)PrimitiveType::NoType || type > (short)PrimitiveType::Bool)
        throw std::invalid_argument("Invalid constant type.");
}

template <typename T>
static void Read(T* to, std::istream& stream)
{
    stream.read(reinterpret_cast<char*>(to), sizeof(T));
}

Program Peisik::DeserializeProgram(std::istream& stream)
{
    // Make all errors throw
    stream.exceptions(std::istream::badbit | std::istream::eofbit | std::istream::failbit);

    Program result;

    // The header contains a magic number, bytecode version and the main function index
    uint32_t magic = 0;
    Read(&magic, stream);
    if (magic != 0x53494550 /* PEIS (notice the endianness) */)
        throw InterpreterException("Not a compiled Peisik file.");

    uint32_t bytecodeVersion = 0;
    Read(&bytecodeVersion, stream);
    if (bytecodeVersion != Program::BytecodeVersion)
        throw InterpreterException("Wrong bytecode version.");

    uint32_t mainIndex = 0;
    Read(&mainIndex, stream);
    result.m_mainFunctionIndex = static_cast<short>(mainIndex);

    // Then, the constants.
    // First, a 32-bit integer for their count and then each constant
    int32_t constCount = -1;
    Read(&constCount, stream);
    if (constCount < 0)
        throw InterpreterException("Constant count less than 0.");

    for (int i = 0; i < constCount; i++)
    {
        // Type code
        short type = 0;
        Read(&type, stream);
        AssertValidType(type);

        // 6 bytes of UTF-8 encoded name as padding - ignore
        char name[6];
        stream.read(name, 6 * sizeof(char));

        // Value
        int64_t value = -1;
        Read(&value, stream);

        // Add the constant
        result.m_constants.push_back(PObject(static_cast<PrimitiveType>(type), value));
    }

    // Then the functions.
    // First, a 32-bit integer for their count.
    // Then for each function:
    //   1. The return type (2 bytes)
    //   2. Parameter count (2 bytes)
    //   3. Parameter types, 2 bytes each
    //   (4. 2 bytes of padding if odd number of parameters)
    //   5. Bytecode size (4 bytes)
    //   6. Bytecode
    int32_t functionCount = -1;
    Read(&functionCount, stream);
    if (functionCount < 0)
        throw InterpreterException("Function count less than 0.");
    if (functionCount > 32768)
        throw std::range_error("Too many functions.");

    for (int i = 0; i < functionCount; i++)
    {
        Function func;
        func.m_functionIndex = static_cast<short>(i);

        // Return type
        short returnType;
        Read(&returnType, stream);
        AssertValidType(returnType);
        func.m_returnType = static_cast<PrimitiveType>(returnType);

        // Parameter count
        Read(&func.m_parameterCount, stream);
        if (func.m_parameterCount < 0)
            throw InterpreterException("Parameter count less than 0.");

        // Locals
        short localCount = -1;
        Read(&localCount, stream);
        if (localCount < 0)
            throw InterpreterException("Local count less than 0.");
        func.m_localTypes.reserve(localCount);

        for (int localIdx = 0; localIdx < localCount; localIdx++)
        {
            short type = 0;
            Read(&type, stream);
            AssertValidType(type);

            func.m_localTypes.push_back(static_cast<PrimitiveType>(type));
        }

        if (localCount % 2 == 1)
        {
            short unused = 0;
            Read(&unused, stream);
        }

        // Bytecode
        int32_t codeSize = -1;
        Read(&codeSize, stream);
        if (codeSize < 0)
            throw InterpreterException("Code size less than 0.");

        func.m_bytecode.reserve(codeSize);
        for (int j = 0; j < codeSize; j++)
        {
            short op = -1;
            Read(&op, stream);

            short param = -1;
            Read(&param, stream);

            func.m_bytecode.push_back(BytecodeOp(static_cast<Opcode>(op), param));
        }

        result.m_functions.push_back(func);
    }

    return result;
}
