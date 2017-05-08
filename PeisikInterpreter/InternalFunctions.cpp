#include "pch.h"
#include "InternalFunctions.h"
#include "PeisikException.h"
#include "PObject.h"

using namespace Peisik;

PObject InternalFunc::Plus(std::stack<PObject>& values)
{
    // Store both exact integer and floating point values and return the latter only if
    // there is a floating-point parameter.
    int64_t intValue = 0;
    double realValue = 0;
    bool shouldReturnDouble = false;

    while (values.size() > 0)
    {
        PObject object = values.top();
        values.pop();

        if (object.GetType() == PrimitiveType::Int)
        {
            int64_t value = object.GetIntValue();
            intValue += value;
            realValue += value;
        }
        else if (object.GetType() == PrimitiveType::Real)
        {
            double value = object.GetRealValue();
            realValue += value; // No need to update intValue any more
            shouldReturnDouble = true;
        }
        else
        {
            throw Peisik::ApplicationException("+ arguments must be Int or Real.");
        }
    }

    if (shouldReturnDouble)
    {
        return ObjectFromReal(realValue);
    }
    else
    {
        return ObjectFromInt(intValue);
    }
}

PObject Peisik::InternalFunc::Minus(const PObject& value)
{
    if (value.GetType() == PrimitiveType::Int)
        return ObjectFromInt(-value.GetIntValue());
    else if (value.GetType() == PrimitiveType::Real)
        return ObjectFromReal(-value.GetRealValue());
    else
        throw Peisik::ApplicationException("- arguments must be Int or Real.");
}

PObject Peisik::InternalFunc::Minus(const PObject & left, const PObject & right)
{
    if (left.GetType() == PrimitiveType::Int && right.GetType() == PrimitiveType::Int)
    {
        return ObjectFromInt(left.GetIntValue() - right.GetIntValue());
    }
    else
    {
        return ObjectFromReal(left.GetRealValueForAnyNumeric() - right.GetRealValueForAnyNumeric());
    }
}

PObject InternalFunc::Multiply(const PObject& left, const PObject& right)
{
    if (left.GetType() == PrimitiveType::Int && right.GetType() == PrimitiveType::Int)
    {
        return ObjectFromInt(left.GetIntValue() * right.GetIntValue());
    }
    else
    {
        return ObjectFromReal(left.GetRealValueForAnyNumeric() * right.GetRealValueForAnyNumeric());
    }
}

PObject InternalFunc::Divide(const PObject& left, const PObject& right)
{
    if (right.GetRealValueForAnyNumeric() == 0)
        throw Peisik::ApplicationException("Division by zero.");

    return ObjectFromReal(left.GetRealValueForAnyNumeric() / right.GetRealValueForAnyNumeric());
}

PObject InternalFunc::FloorDivide(const PObject& left, const PObject& right)
{
    if (right.GetRealValueForAnyNumeric() == 0)
        throw Peisik::ApplicationException("Division by zero.");

    // Be exact if both parameters are integers
    if (left.GetType() == PrimitiveType::Int && right.GetType() == PrimitiveType::Int)
    {
        return ObjectFromInt(left.GetIntValue() / right.GetIntValue());
    }
    else
    {
        return ObjectFromInt(static_cast<int64_t>(left.GetRealValueForAnyNumeric() / right.GetRealValueForAnyNumeric()));
    }
}

PObject Peisik::InternalFunc::Mod(const PObject& value, const PObject& modulus)
{
    if (modulus.GetIntValue() == 0)
        throw Peisik::ApplicationException("Division by zero in %.");

    // The result is always non-negative
    int64_t result = value.GetIntValue() % modulus.GetIntValue();
    if (result < 0)
        result = std::abs(modulus.GetIntValue()) + result;

    return ObjectFromInt(result);
}


PObject InternalFunc::Less(const PObject& left, const PObject& right)
{
    // If both parameters are integers, compare them exactly
    if (left.GetType() == PrimitiveType::Int && right.GetType() == PrimitiveType::Int)
    {
        return ObjectFromBool(left.GetIntValue() < right.GetIntValue());
    }
    else
    {
        return ObjectFromBool(left.GetRealValueForAnyNumeric() < right.GetRealValueForAnyNumeric());
    }
}

PObject InternalFunc::LessEqual(const PObject& left, const PObject& right)
{
    // If both parameters are integers, compare them exactly
    if (left.GetType() == PrimitiveType::Int && right.GetType() == PrimitiveType::Int)
    {
        return ObjectFromBool(left.GetIntValue() <= right.GetIntValue());
    }
    else
    {
        return ObjectFromBool(left.GetRealValueForAnyNumeric() <= right.GetRealValueForAnyNumeric());
    }
}

PObject InternalFunc::Greater(const PObject& left, const PObject& right)
{
    // If both parameters are integers, compare them exactly
    if (left.GetType() == PrimitiveType::Int && right.GetType() == PrimitiveType::Int)
    {
        return ObjectFromBool(left.GetIntValue() > right.GetIntValue());
    }
    else
    {
        return ObjectFromBool(left.GetRealValueForAnyNumeric() > right.GetRealValueForAnyNumeric());
    }
}

PObject InternalFunc::GreaterEqual(const PObject& left, const PObject& right)
{
    // If both parameters are integers, compare them exactly
    if (left.GetType() == PrimitiveType::Int && right.GetType() == PrimitiveType::Int)
    {
        return ObjectFromBool(left.GetIntValue() >= right.GetIntValue());
    }
    else
    {
        return ObjectFromBool(left.GetRealValueForAnyNumeric() >= right.GetRealValueForAnyNumeric());
    }
}

PObject InternalFunc::Equal(const PObject& left, const PObject& right)
{
    if (left.GetType() == PrimitiveType::Bool && right.GetType() == PrimitiveType::Bool)
    {
        return ObjectFromBool(left.GetBoolValue() == right.GetBoolValue());
    }
    else if (left.GetType() == PrimitiveType::Int && right.GetType() == PrimitiveType::Int)
    {
        // If both parameters are integers, compare them exactly
        return ObjectFromBool(left.GetIntValue() == right.GetIntValue());
    }
    else
    {
        return ObjectFromBool(left.GetRealValueForAnyNumeric() == right.GetRealValueForAnyNumeric());
    }
}

PObject InternalFunc::NotEqual(const PObject& left, const PObject& right)
{
    if (left.GetType() == PrimitiveType::Bool && right.GetType() == PrimitiveType::Bool)
    {
        return ObjectFromBool(left.GetBoolValue() != right.GetBoolValue());
    }
    else if (left.GetType() == PrimitiveType::Int && right.GetType() == PrimitiveType::Int)
    {
        // If both parameters are integers, compare them exactly
        return ObjectFromBool(left.GetIntValue() != right.GetIntValue());
    }
    else
    {
        return ObjectFromBool(left.GetRealValueForAnyNumeric() != right.GetRealValueForAnyNumeric());
    }
}


PObject InternalFunc::And(const PObject& left, const PObject& right)
{
    if (left.GetType() == PrimitiveType::Bool && right.GetType() == PrimitiveType::Bool)
    {
        return ObjectFromBool(left.GetBoolValue() & right.GetBoolValue());
    }
    else
    {
        return ObjectFromInt(left.GetIntValue() & right.GetIntValue());
    }
}

PObject InternalFunc::Or(const PObject& left, const PObject& right)
{
    if (left.GetType() == PrimitiveType::Bool && right.GetType() == PrimitiveType::Bool)
    {
        return ObjectFromBool(left.GetBoolValue() | right.GetBoolValue());
    }
    else
    {
        return ObjectFromInt(left.GetIntValue() | right.GetIntValue());
    }
}

PObject InternalFunc::Xor(const PObject& left, const PObject& right)
{
    if (left.GetType() == PrimitiveType::Bool && right.GetType() == PrimitiveType::Bool)
    {
        return ObjectFromBool(left.GetBoolValue() ^ right.GetBoolValue());
    }
    else
    {
        return ObjectFromInt(left.GetIntValue() ^ right.GetIntValue());
    }
}

PObject InternalFunc::Not(const PObject& value)
{
    if (value.GetType() == PrimitiveType::Bool)
    {
        return ObjectFromBool(!value.GetBoolValue());
    }
    else
    {
        return ObjectFromInt(~value.GetIntValue());
    }
}


PObject InternalFunc::MathAbs(const PObject& value)
{
    if (value.GetType() == PrimitiveType::Int)
    {
        return ObjectFromInt(static_cast<int64_t>(std::abs(value.GetIntValue())));
    }
    else
    {
        return ObjectFromReal(std::abs(value.GetRealValue()));
    }
}

PObject InternalFunc::MathAcos(const PObject& value)
{
    double realValue = value.GetRealValueForAnyNumeric();
    if (realValue < -1 || realValue > 1)
        throw Peisik::ApplicationException("Math.Acos called with argument outside [-1, 1].");

    return ObjectFromReal(std::acos(realValue));
}

PObject InternalFunc::MathAsin(const PObject& value)
{
    double realValue = value.GetRealValueForAnyNumeric();
    if (realValue < -1 || realValue > 1)
        throw Peisik::ApplicationException("Math.Asin called with argument outside [-1, 1].");

    return ObjectFromReal(std::asin(realValue));
}

PObject InternalFunc::MathAtan(const PObject& value)
{
    return ObjectFromReal(std::atan(value.GetRealValueForAnyNumeric()));
}

PObject InternalFunc::MathCeil(const PObject& value)
{
    return ObjectFromInt(static_cast<int64_t>(std::ceil(value.GetRealValueForAnyNumeric())));
}

PObject InternalFunc::MathCos(const PObject& value)
{
    return ObjectFromReal(std::cos(value.GetRealValueForAnyNumeric()));
}

PObject InternalFunc::MathExp(const PObject& value)
{
    return ObjectFromReal(std::exp(value.GetRealValueForAnyNumeric()));
}

PObject InternalFunc::MathFloor(const PObject& value)
{
    return ObjectFromInt(static_cast<int64_t>(std::floor(value.GetRealValueForAnyNumeric())));
}

PObject InternalFunc::MathLog(const PObject& value)
{
    if (value.GetRealValueForAnyNumeric() < 0)
        throw Peisik::ApplicationException("Called Math.Log with negative argument.");

    return ObjectFromReal(std::log(value.GetRealValueForAnyNumeric()));
}

PObject InternalFunc::MathPow(const PObject& left, const PObject& right)
{
    if (left.GetRealValueForAnyNumeric() < 0 && right.GetType() == PrimitiveType::Real)
        throw Peisik::ApplicationException("Called Math.Pow with negative argument and non-integer exponent.");

    return ObjectFromReal(std::pow(left.GetRealValueForAnyNumeric(), right.GetRealValueForAnyNumeric()));
}

PObject InternalFunc::MathRound(const PObject& value)
{
    return ObjectFromInt(static_cast<int64_t>(std::round(value.GetRealValueForAnyNumeric())));
}

PObject InternalFunc::MathSin(const PObject& value)
{
    return ObjectFromReal(std::sin(value.GetRealValueForAnyNumeric()));
}

PObject InternalFunc::MathSqrt(const PObject& value)
{
    if (value.GetRealValueForAnyNumeric() < 0)
        throw Peisik::ApplicationException("Called Math.Sqrt with negative argument.");

    return ObjectFromReal(std::sqrt(value.GetRealValueForAnyNumeric()));
}

PObject InternalFunc::MathTan(const PObject& value)
{
    return ObjectFromReal(std::tan(value.GetRealValueForAnyNumeric()));
}
