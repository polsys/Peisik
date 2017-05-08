using System;
using System.Text;

namespace Polsys.Peisik.Compiler
{
    internal static class BytecodeDisassembler
    {
        public static string Disassemble(CompiledFunction function, CompiledProgram parent)
        {
            var sb = new StringBuilder();

            sb.Append(function.ReturnType.ToString());
            sb.Append(" ");
            sb.Append(function.FullName);
            sb.Append("(");
            for (int i = 0; i < function.ParameterTypes.Count; i++)
            {
                sb.Append(function.ParameterTypes[i]);
                if (i != function.ParameterTypes.Count - 1)
                    sb.Append(",");
            }
            sb.Append(")");
            sb.Append(" [");
            sb.Append(function.Locals.Count);
            sb.AppendLine(" locals]");

            foreach (var op in function.Bytecode)
            {
                if (IsParameterAddress(op.Opcode))
                {
                    // Append sign even to positive offsets, since they are relative
                    if (op.Parameter > 0)
                        sb.AppendLine($"{op.Opcode,-12}+{op.Parameter}");
                    else
                        sb.AppendLine($"{op.Opcode,-12}{op.Parameter}");
                }
                else if (IsParameterConstant(op.Opcode))
                {
                    sb.AppendLine($"{op.Opcode,-12}{parent.Constants[op.Parameter].FullName}");
                }
                else if (IsParameterFunction(op.Opcode))
                {
                    sb.AppendLine($"{op.Opcode,-12}{parent.Functions[op.Parameter].FullName}");
                }
                else if (IsParameterInternalFunction(op.Opcode))
                {
                    sb.AppendLine($"{op.Opcode,-12}{(InternalFunction)op.Parameter}");
                }
                else if (IsParameterLocal(op.Opcode))
                {
                    sb.AppendLine($"{op.Opcode,-12}{GetLocalName(function, op.Parameter)}");
                }
                else
                {
                    sb.AppendLine(op.Opcode.ToString());
                }
            }

            return sb.ToString();
        }

        private static bool IsParameterAddress(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.Jump:
                case Opcode.JumpFalse:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsParameterConstant(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.PushConst:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsParameterFunction(Opcode opcode)
        {
            if (opcode == Opcode.Call)
                return true;
            else
                return false;
        }

        private static bool IsParameterInternalFunction(Opcode opcode)
        {
            if (opcode >= Opcode.CallI0 && opcode <= Opcode.CallI7)
                return true;
            else
                return false;
        }

        private static bool IsParameterLocal(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.PopLocal:
                case Opcode.PushLocal:
                    return true;
                default:
                    return false;
            }
        }

        private static string GetLocalName(CompiledFunction function, short index)
        {
            return function.Locals[index].name;
        }
    }
}
