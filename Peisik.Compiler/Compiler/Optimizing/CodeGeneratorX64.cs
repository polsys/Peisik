using System;
using System.IO;

namespace Polsys.Peisik.Compiler.Optimizing
{
    internal class CodeGeneratorX64
    {
        private BinaryWriter _exeWriter;
        private StreamWriter _debugWriter;

        private const int SizeOfHeaderSection = 1024;
        private const int ImageBase = 0x00400000;
        private const int BaseOfCode = 0x00001000; // Relative to image base
        private int _sizeOfCodeSection;
        private int _unpaddedSizeOfCodeSection;
        private int _sizeOfImage;
        private int _entryPointPosition;

        // Because passing parameters is annoying
        private int _currentStackSize;

        public CodeGeneratorX64(BinaryWriter exeWriter, StreamWriter debugWriter)
        {
            _exeWriter = exeWriter;
            _debugWriter = debugWriter;

            InitializeOutput();
        }

        private void InitializeOutput()
        {
            // Reserve 1024 bytes for the PE header section
            for (var i = 0; i < SizeOfHeaderSection / 8; i++)
                _exeWriter.Write(0L);
        }

        /// <summary>
        /// Performs address fixups and finalizes the PE file.
        /// Must be called after all the functions have been compiled.
        /// </summary>
        public void FinalizeOutput()
        {
            // Pad the .text section to a 512-byte boundary
            _unpaddedSizeOfCodeSection = (int)_exeWriter.BaseStream.Position - SizeOfHeaderSection;
            var bytesToPad = (512 - (_exeWriter.BaseStream.Position % 512)) % 512;
            for (var i = 0; i < bytesToPad; i++)
                _exeWriter.Write((byte)0);
            _sizeOfCodeSection = (int)_exeWriter.BaseStream.Position - SizeOfHeaderSection;

            // TODO: Import table (update number of sections and section table!)

            // TODO: Relocation table (update number of sections and section table!)

            _sizeOfImage = (int)_exeWriter.BaseStream.Position;

            // TODO: Do fixups

            WriteExecutableHeader();
        }

        /// <summary>
        /// Emits code for the specified function.
        /// The code may need fixups once all functions have been compiled.
        /// </summary>
        public void CompileFunction(Function function, Optimization optimizationLevel)
        {
            // PRE-EMIT PHASE

            // Linearize the function: store results of operations in locals
            function.ExpressionTree.FoldSingleUseLocals();

            // TODO TODO TODO: Long expression chains will fail without linearization

            // Perform register allocation
            // Each stack variable uses 8 bytes
            var stackSize = RegisterAllocator<X64RegisterBackend>.Allocate(function) * 8;
            _currentStackSize = stackSize;

            // TODO: X64 calling convention requires callers to reserve stack space for spilling
            // parameters passed in registers.
            // Not only do this but also take advantage of the already-reserved space!

            // EMIT PHASE

            // Emit the function header
            PadFunctionTo16ByteBoundary();
            DisasmFunctionHeader(function, stackSize);

            // If this is the main function, save it as the entry point
            if (function.FullName == "main")
            {
                _entryPointPosition = (int)_exeWriter.BaseStream.Position;
            }

            // Reserve space for stack locals
            // The space is freed in EmitRet
            EmitReserveStack(stackSize);

            // Emit the expression tree
            GenerateExpression(function.ExpressionTree);
        }

        private void GenerateExpression(Expression expr)
        {
            switch (expr)
            {
                case ConstantExpression constant:
                    EmitConstantLoad(constant.Value, constant.Store.StorageLocation);
                    break;
                case ReturnExpression ret:
                    GenerateReturn(ret);
                    break;
                case SequenceExpression sequence:
                    foreach (var e in sequence.Expressions)
                        GenerateExpression(e);
                    break;
                default:
                    throw new NotImplementedException($"Unimplemented expression: {expr}");
            }
        }

        private void GenerateReturn(ReturnExpression ret)
        {
            if (ret.Value is ConstantExpression c)
            {
                if (c.Type == PrimitiveType.Real)
                {
                    EmitConstantLoadIntoRegister(c.Value, X64Register.Xmm0);
                }
                else
                {
                    EmitConstantLoadIntoRegister(c.Value, X64Register.Rax);
                }
            }
            else if (ret.Value is LocalLoadExpression local)
            {
                if (local.Type == PrimitiveType.Real)
                {
                    EmitMoveIntoRegister(local.Local.StorageLocation, X64Register.Xmm0);
                }
                else
                {
                    EmitMoveIntoRegister(local.Local.StorageLocation, X64Register.Rax);
                }
            }
            else
            {
                throw new NotImplementedException("ReturnExpression with unimplemented type");
            }

            EmitRet();
        }

        private void EmitConstantLoad(object value, int location)
        {
            if (location == (int)X64Register.Invalid)
                throw new InvalidOperationException("No location assigned");

            if (location < 0)
            {
                EmitConstantLoadIntoRegister(value, (X64Register)location);
            }
            else
            {
                EmitConstantLoadIntoRegister(value, X64Register.Rax);
                EmitMoveOntoStack(X64Register.Rax, location);
            }
        }

        private void EmitConstantLoadIntoRegister(object value, X64Register register)
        {
            if (value is long || value is bool)
            {
                var valueBits = Convert.ToInt64(value);

                // Load an immediate into the general purpose register
                DisasmInstruction($"mov {register.ToString().ToLower()}, {valueBits:x}h");

                (byte encodedReg, bool needB) = GetRegisterEncoding(register);
                EmitRexPrefix(true, false, false, needB);
                _exeWriter.Write((byte)(0xB8 | encodedReg)); // mov [reg]
                _exeWriter.Write(valueBits);
            }
            else if (value is double d)
            {
                var valueBits = BitConverter.DoubleToInt64Bits(d);

                // There is no instruction to load an immediate into an XMM register.
                // Load the immediate into RAX and move it from there.
                DisasmInstruction($"mov rax, {valueBits:x}h  ; {d}");

                (byte encodedRax, bool _) = GetRegisterEncoding(X64Register.Rax);
                EmitRexPrefix(true, false, false, false);
                _exeWriter.Write((byte)(0xB8 | encodedRax)); // mov eax
                _exeWriter.Write(valueBits);

                // In case of constant load onto stack, RAX is used as a temporary register
                // There is then no need to move the value from RAX to RAX, especially considering the next point...
                if (register == X64Register.Rax)
                    return;

                if (register > X64Register.Xmm0)
                    throw new InvalidOperationException("Trying to load a double into a general-purpose register (not RAX).");

                DisasmInstruction($"movq {register.ToString().ToLower()}, rax");

                (byte encodedXmm, bool needR) = GetRegisterEncoding(register);
                _exeWriter.Write((byte)0x66);
                EmitRexPrefix(true, needR, false, false);
                _exeWriter.Write((byte)0x0F);
                _exeWriter.Write((byte)0x6E);
                EmitModRMForRegisterToRegister(encodedXmm, encodedRax);
            }
        }

        private void EmitMoveIntoRegister(int sourceLocation, X64Register dest)
        {
            if (sourceLocation < 0)
            {
                // Register to register
                if ((sourceLocation <= (int)X64Register.Xmm0) != (dest <= X64Register.Xmm0))
                {
                    // This case only applies to EmitConstantLoadIntoRegister().
                    // If you can prove me (probably you, three months ago) wrong, please refactor!
                    throw new NotImplementedException("Moving between XMM and general purpose registers");
                }
                else if (dest > X64Register.Xmm0)
                {
                    // Moving into a general purpose register
                    DisasmInstruction($"mov {dest.ToString().ToLower()}, " +
                        $"{((X64Register)sourceLocation).ToString().ToLower()}");

                    (byte encodedDest, bool needR) = GetRegisterEncoding(dest);
                    (byte encodedSrc, bool needB) = GetRegisterEncoding((X64Register)sourceLocation);

                    EmitRexPrefix(true, needR, false, needB);
                    _exeWriter.Write((byte)0x8B);
                    EmitModRMForRegisterToRegister(encodedDest, encodedSrc);
                }
                else if (dest <= X64Register.Xmm0)
                {
                    // Moving into a SIMD register
                    DisasmInstruction($"movsd {dest.ToString().ToLower()}, " +
                        $"{((X64Register)sourceLocation).ToString().ToLower()}");

                    (byte encodedDest, bool needR) = GetRegisterEncoding(dest);
                    (byte encodedSrc, bool needB) = GetRegisterEncoding((X64Register)sourceLocation);
                    if (needR || needB)
                    {
                        EmitRexPrefix(false, needR, false, needB);
                    }
                    _exeWriter.Write((byte)0xF2);
                    _exeWriter.Write((byte)0x0F);
                    _exeWriter.Write((byte)0x10);
                    EmitModRMForRegisterToRegister(encodedDest, encodedSrc);
                }
            }
            else
            {
                var offset = _currentStackSize - sourceLocation - 8;
                if (offset > sbyte.MaxValue)
                    throw new NotImplementedException("Too large stack offset");

                if (dest > X64Register.Xmm0)
                {
                    // Moving from stack into a general purpose register
                    // This is exactly the same as with EmitMoveOntoStack,
                    // but with a (slightly) different opcode.
                    
                    DisasmInstruction($"mov {dest.ToString().ToLower()}, qword ptr [rsp+{offset:x}h]");

                    (var dst, var needB) = GetRegisterEncoding(dest);

                    EmitRexPrefix(true, false, false, needB);
                    _exeWriter.Write((byte)0x8B);
                    EmitModRMForSIBWithByteDisplacement(dst);
                    _exeWriter.Write((byte)0x24); // SIB for [esp + 0 +]
                    _exeWriter.Write((byte)offset);
                }
                else if (dest <= X64Register.Xmm0)
                {
                    // Moving from stack into an XMM register

                    DisasmInstruction($"movsd {dest.ToString().ToLower()}, mmword ptr [rsp+{offset:x}h]");

                    (var dst, var needB) = GetRegisterEncoding(dest);

                    EmitRexPrefix(true, false, false, needB);
                    _exeWriter.Write((byte)0xF2);
                    _exeWriter.Write((byte)0x0F);
                    _exeWriter.Write((byte)0x10);
                    EmitModRMForSIBWithByteDisplacement(dst);
                    _exeWriter.Write((byte)0x24); // SIB for [esp + 0 +]
                    _exeWriter.Write((byte)offset);
                }
            }
        }

        private void EmitMoveOntoStack(X64Register source, int dest)
        {
            if ((int)source >= -1)
                throw new NotImplementedException("Moving within stack");
            
            // On x64 the stack grows downwards.
            // So if we have three variables on stack and want to access the second one,
            // we use [rsp + sizeof(variable)].
            var offset = _currentStackSize - dest - 8;
            if (offset > sbyte.MaxValue)
                throw new NotImplementedException("Too large stack offset");

            if (source < X64Register.Xmm0)
            {
                throw new NotImplementedException("Moving from XMM to stack");
            }
            else
            {
                DisasmInstruction($"mov qword ptr [rsp+{offset:x}h], {source.ToString().ToLower()}");

                (var src, var needB) = GetRegisterEncoding(source);

                EmitRexPrefix(true, false, false, needB);
                _exeWriter.Write((byte)0x89);
                EmitModRMForSIBWithByteDisplacement(src);
                _exeWriter.Write((byte)0x24); // SIB for [esp + 0 +]
                _exeWriter.Write((byte)offset);
            }
        }

        private void EmitReserveStack(int stackSize)
        {
            if (stackSize == 0)
                return;
            if (stackSize > sbyte.MaxValue)
                throw new NotImplementedException("Stack too big!");

            DisasmInstruction($"sub rsp, {stackSize:x}h");

            (var rsp, var needB) = GetRegisterEncoding(X64Register.Rsp);
            EmitRexPrefix(true, false, false, needB);
            _exeWriter.Write((byte)0x83);
            EmitModRMForRegisterToRegister(5, rsp); // 5 is part of instruction encoding
            _exeWriter.Write((byte)stackSize);
        }

        private void EmitFreeStack(int stackSize)
        {
            if (stackSize == 0)
                return;
            if (stackSize > sbyte.MaxValue)
                throw new NotImplementedException("Stack too big!");

            DisasmInstruction($"add rsp, {stackSize:x}h");

            (var rsp, var needB) = GetRegisterEncoding(X64Register.Rsp);
            EmitRexPrefix(true, false, false, needB);
            _exeWriter.Write((byte)0x83);
            EmitModRMForRegisterToRegister(0, rsp); // 0 is part of instruction encoding
            _exeWriter.Write((byte)stackSize);
        }

        private void EmitRet()
        {
            EmitFreeStack(_currentStackSize);

            DisasmInstruction("ret");
            _exeWriter.Write((byte)0xC3);
        }

        private void EmitRexPrefix(bool w, bool r, bool x, bool b)
        {
            byte rex = 0b_0100_0000;
            if (w)
                rex |= 0b_0000_1000;
            if (r)
                rex |= 0b_0000_0100;
            if (x)
                rex |= 0b_0000_0010;
            if (b)
                rex |= 0b_0000_0001;

            _exeWriter.Write(rex);
        }

        private void EmitModRMForRegisterToRegister(byte dest, byte source)
        {
            byte modrm = 0b_11_000_000;
            modrm |= (byte)(dest << 3);
            modrm |= source;

            _exeWriter.Write(modrm);
        }

        private void EmitModRMForSIBWithByteDisplacement(byte dest)
        {
            byte modrm = 0b_01_000_100;
            modrm |= (byte)(dest << 3);

            _exeWriter.Write(modrm);
        }

        private (byte register, bool rex) GetRegisterEncoding(X64Register register)
        {
            switch (register)
            {
                case X64Register.Rax: return (0, false);
                case X64Register.Rcx: return (1, false);
                case X64Register.Rdx: return (2, false);
                case X64Register.Rbx: return (3, false);
                case X64Register.Rsp: return (4, false);
                case X64Register.Rbp: return (5, false);
                case X64Register.Rsi: return (6, false);
                case X64Register.Rdi: return (7, false);
                case X64Register.R8: return (0, true);
                case X64Register.R9: return (1, true);
                case X64Register.R10: return (2, true);
                case X64Register.R11: return (3, true);
                case X64Register.R12: return (4, true);
                case X64Register.R13: return (5, true);
                case X64Register.R14: return (6, true);
                case X64Register.R15: return (7, true);
                case X64Register.Xmm0: return (0, false);
                case X64Register.Xmm1: return (1, false);
                case X64Register.Xmm2: return (2, false);
                case X64Register.Xmm3: return (3, false);
                case X64Register.Xmm4: return (4, false);
                case X64Register.Xmm5: return (5, false);
                case X64Register.Xmm6: return (6, false);
                case X64Register.Xmm7: return (7, false);
                default:
                    throw new NotImplementedException("Register encoding not implemented");
            }
        }

        private void PadFunctionTo16ByteBoundary()
        {
            var padLength = (16 - (_exeWriter.BaseStream.Position % 16)) % 16;
            var buffer = new byte[padLength];

            // Since this padding is between functions, use a break opcode
            for (var i = 0; i < padLength; i++)
                buffer[i] = 0xCC;

            _exeWriter.Write(buffer);
        }

        private void DisasmFunctionHeader(Function function, int stackSize)
        {
            if (_debugWriter != null)
                _debugWriter.WriteLine();

            var position = (int)_exeWriter.BaseStream.Position;
            var virtualPosition = position - SizeOfHeaderSection + BaseOfCode + ImageBase;

            DisasmComment($"Function '{function.FullName}' at 0x{position:x8} (in memory: 0x{virtualPosition:x8})");
            DisasmComment($"Stack size {stackSize} (0x{stackSize:x})");
            DisasmComment("Local variables:");
            foreach (var local in function.Locals)
            {
                if (local.StorageLocation < -1)
                {
                    DisasmComment($"  {local.Name}  {(X64Register)local.StorageLocation}"
                        + $"  [{local.IntervalStart}, {local.IntervalEnd})");
                }
                else if (local.StorageLocation >= 0)
                {
                    DisasmComment($"  {local.Name}  [rsp+{stackSize - local.StorageLocation - 8:x}h]"
                        + $"  [{local.IntervalStart}, {local.IntervalEnd})");
                }
                // If location is -1, the local is unused
            }
        }

        private void DisasmComment(string content)
        {
            if (_debugWriter != null)
            {
                _debugWriter.Write("; ");
                _debugWriter.WriteLine(content);
            }
        }

        private void DisasmInstruction(string content)
        {
            if (_debugWriter != null)
            {
                _debugWriter.WriteLine($"{_exeWriter.BaseStream.Position:x8}: {content}");
            }
        }

        private void WriteExecutableHeader()
        {
            _exeWriter.Seek(0, SeekOrigin.Begin);

            // MS-DOS header
            // See http://www.delorie.com/djgpp/doc/exe/
            // and https://marcin-chwedczuk.github.io/a-closer-look-at-portable-executable-msdos-stub
            _exeWriter.Write((ushort)0x5a4d); // 'MZ'
            _exeWriter.Write((ushort)0x0090);
            _exeWriter.Write((ushort)0x0003);
            _exeWriter.Write((ushort)0x0000);
            _exeWriter.Write((ushort)0x0004);
            _exeWriter.Write((ushort)0x0000);
            _exeWriter.Write((ushort)0xffff);
            _exeWriter.Write((ushort)0x0000);
            _exeWriter.Write((ushort)0x00b8);
            _exeWriter.Write((ushort)0x0000);
            _exeWriter.Write((ushort)0x0000);
            _exeWriter.Write((ushort)0x0000);
            _exeWriter.Write((ushort)0x0040);
            _exeWriter.Write((ushort)0x0000);
            _exeWriter.Write(new byte[32]);
            _exeWriter.Write((uint)0x00000080); // Address of the PE header

            // MS-DOS stub program
            _exeWriter.Write((byte)0x0e); // push cs
            _exeWriter.Write((byte)0x1f); // pop ds
            _exeWriter.Write((ushort)0x0eba); // mov dx,...
            _exeWriter.Write((byte)0x00); // ... 0x0e
            _exeWriter.Write((ushort)0x09b4); // mov ah, 0x09
            _exeWriter.Write((ushort)0x21cd); // int 0x21 (ah=9h, write string)
            _exeWriter.Write((byte)0xb8); // mov ax,...
            _exeWriter.Write((ushort)0x4c01); // ... 0x4c01
            _exeWriter.Write((ushort)0x21cd); // int 0x21 (ax=4c01h, terminate program)
            _exeWriter.Write(System.Text.Encoding.ASCII.GetBytes("This program requires MS-DOS Ultimate (x64)."));
            _exeWriter.Write((uint)0x240a0d0d); // End of string
            _exeWriter.Write((ushort)0x0000); // Padding

            // COFF header (at 0x80)
            _exeWriter.Write((uint)0x00004550); // 'PE\0\0'
            _exeWriter.Write((ushort)0x8664); // Machine type: x86-64
            _exeWriter.Write((ushort)0x0001); // Number of sections: 1 (.text only)
            _exeWriter.Write((uint)0x00000000); // Time stamp: set to 0 because determinism is cool (or we don't care)
            _exeWriter.Write((uint)0x00000000); // Pointer to symbol table (deprecated)
            _exeWriter.Write((uint)0x00000000); // Number of symbols (deprecated)
            _exeWriter.Write((ushort)0x00f0); // Size of optional header: 240 bytes
            // Characteristics:
            // 0x0001 IMAGE_FILE_RELOCS_STRIPPED
            // 0x0002 IMAGE_FILE_EXECUTABLE_IMAGE
            // 0x0200 IMAGE_FILE_DEBUG_STRIPPED
            _exeWriter.Write((ushort)0b0000_0010_0000_0011);

            // Image header - standard fields
            _exeWriter.Write((ushort)0x020b); // PE32+ magic number
            _exeWriter.Write((ushort)0x0001); // Linker version: 01 00
            _exeWriter.Write(_sizeOfCodeSection); // Size of code
            _exeWriter.Write(0); // Size of initialized data
            _exeWriter.Write(0); // Size of uninitialized data
            _exeWriter.Write(_entryPointPosition + BaseOfCode - SizeOfHeaderSection); // Entry point, relative to image base
            _exeWriter.Write(BaseOfCode); // Base of code

            // Image header - Windows-specific fields
            _exeWriter.Write((ulong)ImageBase); // Image base
            _exeWriter.Write(4096); // Section alignment
            _exeWriter.Write(512); // File alignment
            _exeWriter.Write((ushort)0x06); // Major OS version
            _exeWriter.Write((ushort)0x00); // Minor OS version
            _exeWriter.Write((ushort)0x00); // Major image version
            _exeWriter.Write((ushort)0x00); // Minor image version
            _exeWriter.Write((ushort)0x06); // Major subsystem version
            _exeWriter.Write((ushort)0x00); // Minor subsystem version
            _exeWriter.Write(0); // Reserved
            // Total size of image when mapped in memory.
            // This must be a multiple of the section alignment.
            // NOTE: Must be kept in sync with section sizes!
            _exeWriter.Write(RoundUpToPageSize(SizeOfHeaderSection) + RoundUpToPageSize(_sizeOfCodeSection));
            _exeWriter.Write(SizeOfHeaderSection); // Total size of headers
            _exeWriter.Write(0); // Checksum - not used
            _exeWriter.Write((ushort)0x0003); // Subsystem: Windows console
            // DLL characteristics:
            // 0x0100 NX_COMPAT
            // 0x0400 NO_SEH
            // 0x8000 TERMINAL_SERVER_AWARE
            //
            // NOTE: We explicitly do not support dynamic base address!
            // A more serious compiler must support relocations. 
            _exeWriter.Write((ushort)0b_0000_0101_0000_0000);
            _exeWriter.Write((ulong)0x00000000_00100000); // Stack reserve: 1 MB
            _exeWriter.Write((ulong)0x00000000_00001000); // Stack commit: 64 KB
            _exeWriter.Write((ulong)0x00000000_00100000); // Heap reserve: 1 MB
            _exeWriter.Write((ulong)0x00000000_00001000); // Heap commit: 64 KB
            _exeWriter.Write(0); // Reserved
            _exeWriter.Write(16); // Number of RVAs (fixed at all 16 documented)

            // RVAs
            // TODO: Need to add an entry for import table and import address table
            //       Also add a section and update the image size
            for (var i = 0; i < 16; i++)
                _exeWriter.Write((ulong)0);

            // Section table entry for .text
            _exeWriter.Write((ulong)0x000000747865742e); // '.text\0\0\0'
            _exeWriter.Write(_unpaddedSizeOfCodeSection); // Virtual size, no need to be a multiple of page size
            _exeWriter.Write(BaseOfCode); // Virtual address relative to image base
            _exeWriter.Write(_sizeOfCodeSection); // Size of raw data
            _exeWriter.Write(SizeOfHeaderSection); // Pointer to raw data - begins immediately after headers
            _exeWriter.Write(0); // Pointer to relocations
            _exeWriter.Write(0); // Pointer to line numbers, deprecated
            _exeWriter.Write((ushort)0); // Number of relocations
            _exeWriter.Write((ushort)0); // Number of line numbers
            // Section characteristics:
            // 0x00000020 CNT_CODE
            // 0x20000000 MEM_EXECUTE
            // 0x40000000 MEM_READ
            _exeWriter.Write((ulong)0x6000_0020);
        }

        private int RoundUpToPageSize(int size)
        {
            return (size + (4096 - (size % 4096)));
        }
    }
}
