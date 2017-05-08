# Peisik Language Specification

## Miscellaneous notes
* The language is completely case-insensitive. Names are normalized into lower case using locale-invariant rules. This may cause issues when importing modules on a case-sensitive file system.
* Names may include any Unicode letters, `_` and decimal digits. A name may not consist solely of decimal digits. A name may not equal a language keyword.
* `\n` is the line separator. End-of-line `\r` is ignored by the parser.
* Tokens are separated by whitespace, commas and parentheses.
* Whitespace equals anything Unicode classifies as whitespace.
* End-of-line separates statements. A statement may span lines, but it must end on its own line. _Some of these rules are too strict and should be relaxed in future language versions._

## Data types
`Int` is a 64-bit signed two's complement integer. `Int` literals are decimal and without any digit separators.

`Bool` represents a boolean value. Its storage is implementation-defined. Possible `bool` literals are `true` and `false`.

`Real` is a 64-bit binary IEEE 754 floating point number. `Real` literals use `.` for decimal separator and allow exponent notation. Real operations are not required to produce same values across all implementations. The implementations should disallow infinities/NaNs.

`Void` has no value. It is only allowed as a function return type.

There is no implicit conversion between types (save for integer literals, which may be interpreted as reals, with undefined behavior at extreme values). Some built-in functions allow parameter type overloading, but this does not apply to user-defined functions. 

## Modules
A module corresponds to a source code file (`.peisik` file). The file name specifies the module name. Modules contain import statements, constants and functions.

The order of constants and functions is not important: a function may refer to a member defined later in the source. The order of imports is significant: the parser will reject references to fully qualified names without a prior import of the module.

### Import statement
```
import (modulename) (linefeed)
import UnitTest
```
Instructs the compiler to locate and parse the specified module file. All public members of the module will be made available to the importing module, using fully qualified names.

As an exception, the language-provided `Math` functions always exist in the namespace. A user-defined `Math` module is allowed, but it should not define members with same names as the built-in functions. 

### Constant
```
(visibility modifier) (data type) (name) (value) (linefeed)
public real Pi 3.14
private bool UsableLanguage false
```
Defines a new constant with the specified visibility, name and value.

### Function
```
(visibility modifier) (data type) (name) '(' (parameter list) ')' (linefeed)
(block)
```
Defines a new function with the specified visibility, name and return type. The parameter list contains any number of `(data type) (name)` statements separated by commas.

The parameters are local variables that are initialized during the function call. The function must return a value of the specified return type. (The compiler must be able to guarantee that all code paths return correctly.) However, `void` functions may omit the return statement, and in that case one is added to the end of the function.

For example:
```
private real Dist(real x, real y)
begin
    return +(*(x, x), *(y, y))
end
```

A program must have a function named `Main` in its main module. The function may return an arbitrary type and have any visibility, but it must not accept any parameters.

### Visibility modifier
```
(public|private)
```
A constant/function defined `public` is accessible from other modules, whereas a `private` member is not. A visibility modifier is a mandatory part of constant and function declarations.

## Statements

### Block
```
begin (linefeed)
[statement]*
end (linefeed)
```
Block contains statements, which are executed in order. Block is also a scope for variables: any variables defined in a block are visible to the block and any nested blocks, but not to a parent or a sibling block.

### Variable definition
```
(data type) (name) (initial value) (linefeed)
int seed 45678
```
Defines a new local variable with the specified type, name and initial value. Initial value may be any expression of the correct type. The name must be unique in the scope.

### Assignment
```
(name) = (expression) (linefeed)
seed = RandomNext(seed)
```
Assigns the result of the expression to the specified local variable.

### Function call statement
```
(function call) (linefeed)
```
Otherwise the same as a function call expression, but discards the result.

### Return
```
return (expression) (linefeed)
return true
```
Exits the function, returning the value of the expression. The expression must result in the same type as the function.

For a void function, the return statement is `return void`.

### If-Else
```
if (expression) (linefeed)
(block)
[else (linefeed)
(block)]
```
The if statement evaluates the boolean expression, and if true, executes the attached block. The optional else block will be executed if the expression is false.

_As of now the syntax requires very many lines. This may be condensed in the future. An `else if` construct may be added as well._

Example:
```
if true
begin
  return 2
end
else
begin
  return -2
end
```

### While
```
while (expression) (linefeed)
(block)
```
Evaluates the expression and if true, executes the block, then repeats. Example:

```
while true
begin
  print(2)
end
```

## Expressions

### Function call
```
(name) '(' (parameters) ')' (linefeed)
%(seed, 4294967296)
```
Calls the specified function and returns its result value. The parameter list is a comma-separated list of zero or more expressions. Each expression must match the type of the corresponding parameter. Parameters are always evaluated from left to right. All parameters are always evaluated.

### Literal
See the section on data types.

### Name
Returns the value of the specified local variable or constant.

There are two types of names: regular (`name`) and fully qualified (`module.name`). Regular names refer to the same module. Fully qualified names refer to an imported module.

## Possible future features
* Custom data types (structs).
* Arrays.
* Textual data type (string).
* Enumeration types that map integer values to names.
* For statements that combine a while statement with automatic iteration variable.
* Function overloading for various parameter types and counts.
* Multiple return types from a single function.

The following keywords are reserved for this purpose:
```
enum for foreach string struct type
```