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
        private int _sizeOfImage;
        private int _addressOfEntryPoint;

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
            var stackSize = RegisterAllocator<X64RegisterBackend>.Allocate(function);

            // EMIT PHASE

            // Emit the function header
            PadStreamTo16ByteBoundary();
            DisasmFunctionHeader(function, stackSize, _exeWriter.BaseStream.Position);

            // Emit the expression tree
            _exeWriter.Write(0xDEADBEEF);
        }

        private void PadStreamTo16ByteBoundary()
        {
            var padLength = (16 - (_exeWriter.BaseStream.Position % 16)) % 16;
            var buffer = new byte[padLength];
            _exeWriter.Write(buffer);
        }

        private void DisasmFunctionHeader(Function function, int stackSize, long position)
        {
            if (_debugWriter != null)
                _debugWriter.WriteLine();

            DisasmComment($"Function '{function.FullName}' at 0x{(int)position:x8}");
            DisasmComment($"Stack size {stackSize} (0x{stackSize:x})");
            DisasmComment("Local variables:");
            foreach (var local in function.Locals)
            {
                if (local.StorageLocation < -1)
                    DisasmComment($"  {local.Name}  {(X64Register)local.StorageLocation}"
                        + $"  [{local.IntervalStart}, {local.IntervalEnd})");
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
            _exeWriter.Write((uint)0x0a0d0d2e); // End of string, I presume?
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
            _exeWriter.Write(_addressOfEntryPoint + BaseOfCode - SizeOfHeaderSection); // Entry point, relative to image base
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
            _exeWriter.Write(_sizeOfImage); // Total size of image
            _exeWriter.Write(SizeOfHeaderSection); // Total size of headers
            _exeWriter.Write(0); // Checksum - not used
            _exeWriter.Write((ushort)0x0003); // Subsystem: Windows console
            // DLL characteristics:
            // 0x0100 NX_COMPAT
            // 0x0400 NO_SEH
            _exeWriter.Write((ushort)0b_0000_0101_0000_0000);
            _exeWriter.Write((ulong)0x00000000_00100000); // Stack reserve: 1 MB
            _exeWriter.Write((ulong)0x00000000_00001000); // Stack commit: 64 KB
            _exeWriter.Write((ulong)0x00000000_00100000); // Heap reserve: 1 MB
            _exeWriter.Write((ulong)0x00000000_00001000); // Heap commit: 64 KB
            _exeWriter.Write(0); // Reserved
            _exeWriter.Write(16); // Number of RVAs (fixed at all 16 documented)

            // RVAs
            // TODO: Need to add an entry for import table and import address table
            for (var i = 0; i < 16; i++)
                _exeWriter.Write((ulong)0);

            // Section table entry for .text
            _exeWriter.Write((ulong)0x000000747865742e); // '.text\0\0\0'
            _exeWriter.Write(_sizeOfCodeSection); // Virtual size - MSFT linker rounds up but we don't
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
    }
}
