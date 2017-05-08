#pragma once

#include <stdexcept>

namespace Peisik
{
    // Errors arising from user code.
    class ApplicationException : public std::runtime_error
    {
    public:
        ApplicationException(const char* msg) : std::runtime_error(msg) {};
    };

    // Invalid program.
    class InterpreterException : public std::runtime_error
    {
    public:
        InterpreterException(const char* msg) : std::runtime_error(msg) {};
    };
}