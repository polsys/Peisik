#include "pch.h"
#include "PeisikException.h"
#include "PObject.h"

using namespace Peisik;

PrimitiveType PObject::GetType() const
{
    return m_type;
}

bool PObject::GetBoolValue() const
{
    if (m_type != PrimitiveType::Bool)
        throw InterpreterException("Trying to get bool value of a non-bool constant.");

    return m_boolValue;
}

int64_t PObject::GetIntValue() const
{
    if (m_type != PrimitiveType::Int)
        throw InterpreterException("Trying to get int value of a non-int constant.");

    return m_intValue;
}

double PObject::GetRealValue() const
{
    if (m_type != PrimitiveType::Real)
        throw InterpreterException("Trying to get real value of a non-real constant.");

    return m_realValue;
}

double Peisik::PObject::GetRealValueForAnyNumeric() const
{
    if (m_type == PrimitiveType::Real)
        return m_realValue;
    else if (m_type == PrimitiveType::Int)
        return static_cast<double>(m_intValue);
    else
        throw InterpreterException("Trying to get real value of non-numeric constant.");
}

void Peisik::PObject::SetValue(bool newValue)
{
    if (m_type != PrimitiveType::Bool)
        throw InterpreterException("Trying to set bool value of a non-bool constant.");

    m_boolValue = newValue;
}

void Peisik::PObject::SetValue(int64_t newValue)
{
    if (m_type != PrimitiveType::Int)
        throw InterpreterException("Trying to set int value of a non-int constant.");

    m_intValue = newValue;
}

void Peisik::PObject::SetValue(double newValue)
{
    if (m_type != PrimitiveType::Real)
        throw InterpreterException("Trying to set real value of a non-real constant.");

    m_realValue = newValue;
}

void PObject::SetValue(const PObject newValue)
{
    if (m_type != newValue.m_type)
        throw InterpreterException("Type mismatch in PObject::SetValue.");

    // Since the value is an union, we can just copy the int value
    m_intValue = newValue.m_intValue;
}
