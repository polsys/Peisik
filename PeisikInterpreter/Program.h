#pragma once

#include "Bytecode.h"
#include "PObject.h"
#include <iostream>
#include <vector>

namespace Peisik
{
    class Program;
    // Loads a program object from the specified stream.
    // The stream is expected to be a binary stream.
    Program DeserializeProgram(std::istream& stream);

    // Represents a single function.
    class Function
    {
    public:
        // Gets a reference to the bytecode vector.
        const std::vector<BytecodeOp>& GetBytecode() const;

        // Gets the function table index of this function.
        short GetFunctionIndex() const;

        // Gets a reference to the local type vector.
        const std::vector<PrimitiveType>& GetLocalTypes() const;

        // Gets the parameter count.
        short GetParameterCount() const;

        // Gets the type of the function return value.
        PrimitiveType GetReturnType() const;

    private:
        std::vector<BytecodeOp> m_bytecode;
        short m_functionIndex;
        std::vector<PrimitiveType> m_localTypes;
        short m_parameterCount;
        PrimitiveType m_returnType;

        friend Program DeserializeProgram(std::istream&);
    };

    // Represents a complete compiled program.
    class Program
    {
    public:
        // Gets the constant with the specified index.
        // If the index is higher than allowed or less than zero, an exception is thrown.
        PObject GetConstant(short index) const;

        // Gets the number of constants in the constant table.
        short GetConstantCount() const;

        // Gets the function with the specified index.
        // If the index is higher than allowed or less than zero, an exception is thrown.
        const Function& GetFunction(short index) const;

        // Gets the number of functions in the function table.
        short GetFunctionCount() const;

        // Gets the function table index of the program entry point.
        short GetMainFunctionIndex() const;

    private:
        short m_mainFunctionIndex;
        std::vector<PObject> m_constants;
        std::vector<Function> m_functions;

        friend Program DeserializeProgram(std::istream&);

        static const int BytecodeVersion = 6;
    };
}