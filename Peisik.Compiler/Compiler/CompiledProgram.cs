using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Polsys.Peisik.Compiler
{
    internal class CompiledProgram
    {
        public int BytecodeVersion { get { return 6; } }

        public List<CompiledConstant> Constants { get; private set; }

        public List<CompiledFunction> Functions { get; private set; }

        public int MainFunctionIndex { get; set; }

        public CompiledProgram()
        {
            Constants = new List<CompiledConstant>();
            Functions = new List<CompiledFunction>();
        }

        public void Serialize(BinaryWriter writer)
        {
            // Header
            writer.Write(new char[] { 'P', 'E', 'I', 'S' }, 0, 4);
            writer.Write(BytecodeVersion);
            writer.Write(MainFunctionIndex);

            // Constants - each entry is 16-byte aligned
            writer.Write(Constants.Count);
            foreach (var c in Constants)
            {
                // TTNNNNNN VVVVVVVV
                // 2 bytes of type code
                // 6 bytes of UTF-8 encoded name as a padding (the last 6 bytes because of $literal_xxx naming)
                // 8 bytes of value
                writer.Write((short)c.Type);

                var bytes = Encoding.UTF8.GetBytes(c.FullName);
                if (bytes.Length < 6)
                    Array.Resize(ref bytes, 6);
                writer.Write(bytes, bytes.Length - 6, 6);

                if (c.Value is long longValue)
                    writer.Write(longValue);
                else if (c.Value is double doubleValue)
                    writer.Write(doubleValue);
                else if (c.Value is bool boolValue)
                    writer.Write(boolValue ? 1UL : 0UL);
                else
                    throw new NotImplementedException();
            }

            // Functions - everything is 4-byte aligned
            writer.Write(Functions.Count);
            foreach (var f in Functions)
            {
                writer.Write((short)f.ReturnType);

                // Write the parameter count
                // Parameters are passed as locals so the type information is there
                writer.Write((short)f.ParameterTypes.Count);

                // Write the local count and local types
                writer.Write((short)f.Locals.Count);
                foreach ((_, var localType) in f.Locals)
                    writer.Write((short)localType);

                // 2 bytes of padding if odd number of locals
                if (f.Locals.Count % 2 == 1)
                    writer.Write((short)0);

                // Bytecode
                writer.Write(f.Bytecode.Count);
                foreach (var op in f.Bytecode)
                {
                    writer.Write((short)op.Opcode);
                    writer.Write(op.Parameter);
                }
            }
        }
    }
}
