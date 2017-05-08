# Peisik Bytecode Format
Compiled Peisik programs may be stored in interpretable `.cpeisik` files. The current implementation can be found in `Peisik.Compiler/Compiler/CompiledProgram.cs` and `PeisikInterpreter/Program.cpp`.

The file has a header containing ASCII string `PEIS` followed by the format version as a 32-bit integer. The rest of the format is undocumented here, but there are two main parts: the constant table and the function table.

The constant table contains all constants (including literals) used by the program. Each entry has a type and a value.

The function table contains all functions. Each function has information on parameter count, return type, types of locals and the attached bytecode.

Calling the format bytecode is a slight misnomer, as each instruction takes 32 bits. This is for easier alignment in memory, though it wastes storage space (quite greatly, indeed). Each instruction first contains a 16-bit opcode and then a 16-bit parameter. The opcodes are documented below for reference. They are subject to change and should be kept in sync with this document (if not, `git blame`).

### `Call`
The parameter is the target function index in the function table. Pops off all parameters from the stack and passes them to the function as locals, then transfers execution to the callee. Since parameters are evaluated left to right, the topmost stack entry is the rightmost parameter.

### `CallIx`
where `x` ranges from 0 to 7. The parameter is an internal function index. `x` denotes the number of parameters to be popped off the stack.

### `Jump`
The parameter is a signed offset. The next executed instruction is at offset `current instruction offset + parameter`.

### `JumpFalse`
Same as `Jump`, but pops a value off the stack and only performs the jump if the value is false. The value must be boolean.

### `PopDiscard`
Pops the topmost value off the stack.

### `PopLocal`
The parameter is an index to the function local table. Pops the topmost value of the stack and stores it in the specified local. The popped type must match the local type.

### `PushConst`
The parameter is an index to the constant table. Pushes the specified constant onto the stack.

### `PushLocal`
The parameter is an index to the function local table. Pushes the specified local onto the stack.

### `Return`
Pops the topmost value off the stack and puts it on the caller's stack, then jumps to the instruction succeeding the `Call` instruction. If this function returns void, no stack operations are performed. 