#pragma once

#include <cstdint>

namespace Peisik
{
    // All primitive types in the Peisik type system.
    enum class PrimitiveType
    {
        NoType,
        Void,
        Int,
        Real,
        Bool
    };

    // Represents an object in the Peisik type system.
    class PObject
    {
    public:
        // Constructs a new object from the raw value.
        PObject(PrimitiveType type, int64_t rawValue)
            : m_type(type), m_intValue(rawValue) { };
        ~PObject() = default;

        // Gets the type of this object.
        PrimitiveType GetType() const;

        // Gets the boolean value of this object.
        // If this is not an bool object, an exception is thrown.
        bool GetBoolValue() const;

        // Gets the integer value of this object.
        // If this is not an integer object, an exception is thrown.
        int64_t GetIntValue() const;

        // Gets the floating-point value of this object.
        // If this is not a floating-point object, an exception is thrown.
        double GetRealValue() const;

        // Gets the floating-point value of this object, even if this object is an integer.
        // For other object types, an exception is thrown.
        double GetRealValueForAnyNumeric() const;

        // Sets the boolean value of this object.
        // If this is not an bool object, an exception is thrown.
        void SetValue(bool newValue);

        // Sets the integer value of this object.
        // If this is not an integer object, an exception is thrown.
        void SetValue(int64_t newValue);

        // Sets the floating-point value of this object.
        // If this is not a floating-point object, an exception is thrown.
        void SetValue(double newValue);

        // Sets the value of this object.
        // If the old and new object types do not match, an exception is thrown.
        void SetValue(const PObject newValue);

    private:
        PrimitiveType m_type;
        union
        {
            bool m_boolValue;
            int64_t m_intValue;
            double m_realValue;
        };
    };

    inline PObject ObjectFromBool(const bool value)
    {
        return PObject(PrimitiveType::Bool, value);
    }

    inline PObject ObjectFromInt(const int64_t value)
    {
        return PObject(PrimitiveType::Int, value);
    }

    inline PObject ObjectFromReal(const double value)
    {
        PObject result(PrimitiveType::Real, 0);
        result.SetValue(value);
        return result;
    }
}