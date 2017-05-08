#pragma once

#include "PObject.h"

namespace Peisik
{
    namespace InternalFunc
    {
        PObject Plus(std::stack<PObject>& values);
        PObject Minus(const PObject& value);
        PObject Minus(const PObject& left, const PObject& right);
        PObject Multiply(const PObject& left, const PObject& right);
        PObject Divide(const PObject& left, const PObject& right);
        PObject FloorDivide(const PObject& left, const PObject& right);
        PObject Mod(const PObject& value, const PObject& modulus);

        PObject Less(const PObject& left, const PObject& right);
        PObject LessEqual(const PObject& left, const PObject& right);
        PObject Greater(const PObject& left, const PObject& right);
        PObject GreaterEqual(const PObject& left, const PObject& right);
        PObject Equal(const PObject& left, const PObject& right);
        PObject NotEqual(const PObject& left, const PObject& right);

        PObject And(const PObject& left, const PObject& right);
        PObject Or(const PObject& left, const PObject& right);
        PObject Xor(const PObject& left, const PObject& right);
        PObject Not(const PObject& value);

        PObject MathAbs(const PObject& value);
        PObject MathAcos(const PObject& value);
        PObject MathAsin(const PObject& value);
        PObject MathAtan(const PObject& value);
        PObject MathCeil(const PObject& value);
        PObject MathCos(const PObject& value);
        PObject MathExp(const PObject& value);
        PObject MathFloor(const PObject& value);
        PObject MathLog(const PObject& value);
        PObject MathPow(const PObject& left, const PObject& right);
        PObject MathRound(const PObject& value);
        PObject MathSin(const PObject& value);
        PObject MathSqrt(const PObject& value);
        PObject MathTan(const PObject& value);
    }
}
