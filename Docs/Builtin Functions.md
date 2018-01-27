# Peisik builtin functions
The language provides a minimal set of basic functions. Unlike user-defined functions, some of them support multiple parameter types. This document lists all the functions.

The first section lists functions that live in the same namespace as the module. (That is, they are not accessed through a module prefix.) These names are reserved by the language. This list includes the arithmetic and comparison operators.

The second section list functions in the `Math` namespace. The namespace is always available, because the functions *do not* live in a module called `Math` - there is no need for an import. (Nothing prevents you from creating a module called `Math`. However, accessing f.ex. `Math.Sin` always uses the builtin version.) 

## Global namespace
### `FailFast`
**Parameters:** None.
**Returns:** N/A.
Immediately terminates program execution.

### `Print`
**Parameters:** 0 to 7 parameters. They may be a mix of Bool, Int and Real parameters.
**Returns:** Void.
Prints a line containing each parameter separated by a single space.

### `+`
**Parameters:** 2 numeric parameters (Int or Real).
**Returns:** If either of the parameters is Real, then Real. Otherwise, Int.
Performs addition. Integer addition may over/underflow and floating point addition might not be accurate.

### `-`
**Parameters:** Either 1 or 2 numeric parameters.
**Returns:** If either of the parameters is Real, then Real. Otherwise, Int.

The one-parameter version negates its input. The two-parameter version subtracts the right-hand value from the left-hand one.

### `*`
**Parameters:** 2 numeric parameters (Int or Real).
**Returns:** If either of the parameters is Real, then Real. Otherwise, Int.
Multiplies the two parameters.

### `/`
**Parameters:** 2 numeric parameters (Int or Real).
**Returns:** Real.
Divides the left-hand value by the right-hand one, and returns the floating-point result. Division by zero results in program termination.

### `//`
**Parameters:** 2 numeric parameters (Int or Real).
**Returns:** Int.
Divides the left-hand value by the right-hand one, and returns the result rounded towards zero. Division by zero results in program termination.

### `%`
**Parameters:** 2 Int parameters.
**Returns:** Int.
Returns the remainder when the left-hand parameter is divided by the right-hand one. The result is always non-negative. Division by zero results in program termination.

### `and`
**Parameters:** Either 2 Int or 2 Bool parameters.
**Returns:** Same as parameter.
For booleans, performs logical and. For integers, performs bitwise and. (The language specifies integers to always be 64-bit signed two's complement binary integers.)

### `not`
**Parameters:** Either 1 Int or 1 Bool parameter.
**Returns:** Same as parameter.
For booleans, performs logical not. For integers, performs bitwise not (inverts the bits).

### `or`
**Parameters:** Either 2 Int or 2 Bool parameters.
**Returns:** Same as parameter.
For booleans, performs logical or. For integers, performs bitwise or.

### `xor`
**Parameters:** Either 2 Int or 2 Bool parameters.
**Returns:** Same as parameter.
For booleans, performs logical exclusive-or. For integers, performs bitwise exclusive-or.

### Comparison operators
**Parameters:** 2 parameters of the same type.
**Returns:** Bool.
Compares the two parameters and returns the result. The supported operations are
```
< <= == != >= >
```
for 'less than', 'less than or equal to', 'equal to', 'not equal to', 'greater than or equal to', and 'greater than', respectively.


## `Math` namespace

### `Math.Abs`
**Parameters:** 1 numeric parameter.
**Returns:** Same as parameter.
Returns the absolute value (distance from zero) of the parameter. The behavior for the smallest integer is undefined.

### `Math.Acos`
**Parameters:** 1 numeric parameter.
**Returns:** Real.
Returns the angle as radians corresponding to the cosine value. The angle is always in the upper quadrants of the unit circle. If the parameter is greater than 1 or less than -1, the program is terminated.

### `Math.Asin`
**Parameters:** 1 numeric parameter.
**Returns:** Real.
Returns the angle as radians corresponding to the sine value. The angle is always in the right-hand quadrants of the unit circle. If the parameter is greater than 1 or less than -1, the program is terminated.

### `Math.Atan`
**Parameters:** 1 numeric parameter.
**Returns:** Real.
Returns the angle as radians corresponding to the tangent value. The angle is always in the right-hand quadrants of the unit circle.

### `Math.Ceil`
**Parameters:** 1 numeric parameter.
**Returns:** Int.
Returns the value rounded up. (For integer values, this returns the value as is.)

### `Math.Cos`
**Parameters:** 1 numeric parameter.
**Returns:** Real.
Returns the cosine of the parameter, which is an angle measured in radians.

### `Math.Exp`
**Parameters:** 1 numeric parameter.
**Returns:** Real.
Returns `e` raised to the specified power.

### `Math.Floor`
**Parameters:** 1 numeric parameter.
**Returns:** Int.
Returns the value rounded down. (For integer values, this returns the value as is.)

### `Math.Log`
**Parameters:** 1 numeric parameter.
**Returns:** Real.
Returns the natural logarithm of the value. If the parameter is negative, the program is terminated.

### `Math.Pow`
**Parameters:** 2 numeric parameters.
**Returns:** Real.
Returns the left-hand value raised to the right-hand power. This always returns real values, so precision is lost for integer arguments. If the left-hand value is a negative real number, the right-hand value must be an integer.

### `Math.Round`
**Parameters:** 1 numeric parameter.
**Returns:** Int.
Returns the value rounded to the nearest integer. (For integer values, this returns the value as is.)

### `Math.Sin`
**Parameters:** 1 numeric parameter.
**Returns:** Real.
Returns the sine of the parameter, which is an angle measured in radians.

### `Math.Sqrt`
**Parameters:** 1 numeric parameter.
**Returns:** Real.
Returns the square root of the value. If the value is negative, the program is terminated.

### `Math.Tan`
**Parameters:** 1 numeric parameter.
**Returns:** Real.
Returns the tangent of the parameter, which is an angle measured in radians.