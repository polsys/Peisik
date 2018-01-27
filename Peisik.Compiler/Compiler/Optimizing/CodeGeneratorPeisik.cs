using System;
using System.Collections.Generic;
using System.Globalization;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// The code generator that emits Peisik bytecode.
    /// </summary>
    internal class CodeGeneratorPeisik
    {
        public CompiledProgram Result => _program;
        CompiledProgram _program = new CompiledProgram();

        Dictionary<string, short> _constants = new Dictionary<string, short>();
        Dictionary<string, short> _functionIndices = new Dictionary<string, short>();

        public CodeGeneratorPeisik()
        {
        }

        /// <summary>
        /// Emits bytecode for the function and stores it in the result program.
        /// </summary>
        public void CompileFunction(Function function)
        {
            // You might notice that CompiledFunction wasn't really designed for this...
            var compiled = new CompiledFunction(null, function.FullName, "", false, true)
            {
                ReturnType = function.ResultValue.Type
            };

            // Assign function index for this function
            // If this function is Main(), also set the main function index
            var functionIndex = GetFunctionIndex(function.FullName);
            if (function.FullName == "main")
            {
                _program.MainFunctionIndex = functionIndex;
            }

            // Remove redundant locals
            FoldSingleUseLocals(function);

            // Create the locals table
            // Parameters are always included, whereas unused locals are ignored
            // TODO: This will be changed once register allocation exists
            foreach (var local in function.Locals)
            {
                if (!local.IsParameter && local.UseCount == 0 && local.AssignmentCount == 0)
                    continue;
                local.LocalIndex = compiled.Locals.Count;
                compiled.AddLocal(local.Name, local.Type);
            }

            // Then compile the code
            CompileExpression(function.ExpressionTree, function, compiled);
            _program.Functions.Add(compiled);
        }

        /// <summary>
        /// Transforms locals used only once into expressions that are computed on stack.
        /// This avoids redundant local variables and makes the code quality match that of SemanticCompiler.
        /// </summary>
        private void FoldSingleUseLocals(Function function)
        {
            function.ExpressionTree.FoldSingleUseLocals();
        }

        private void CompileExpression(Expression expression, Function function, CompiledFunction compiled)
        {
            switch (expression)
            {
                case BinaryExpression binary:
                    CompileExpression(binary.Left, function, compiled);
                    CompileExpression(binary.Right, function, compiled);
                    compiled.Bytecode.Add(new BytecodeOp(Opcode.CallI2, (short)binary.InternalFunctionId));
                    break;
                case ConstantExpression c:
                    EmitPush(c, compiled);
                    if (c.Store != null)
                        EmitStore(c.Store, compiled);
                    break;
                case FailFastExpression _:
                    compiled.Bytecode.Add(new BytecodeOp(Opcode.CallI0, (short)InternalFunction.FailFast));
                    break;
                case FunctionCallExpression call:
                    CompileCall(call, function, compiled);
                    break;
                case LocalLoadExpression load:
                    EmitPush(load.Local, compiled);
                    if (load.Store != null)
                        EmitStore(load.Store, compiled);
                    break;
                case PrintExpression print:
                    CompilePrint(print, function, compiled);
                    break;
                case ReturnExpression ret:
                    CompileReturn(ret, function, compiled);
                    break;
                case SequenceExpression sequence:
                    foreach (var expr in sequence.Expressions)
                        CompileExpression(expr, function, compiled);
                    break;
                case UnaryExpression unary:
                    CompileExpression(unary.Expression, function, compiled);
                    compiled.Bytecode.Add(new BytecodeOp(Opcode.CallI1, (short)unary.InternalFunctionId));
                    break;
                default:
                    throw new NotImplementedException($"Unhandled expression type {expression}");
            }
        }

        private void CompileCall(FunctionCallExpression call, Function function, CompiledFunction compiled)
        {
            // Load each parameter onto the execution stack
            foreach (var param in call.Parameters)
            {
                CompileExpression(param, function, compiled);
            }

            // Emit call
            compiled.Bytecode.Add(new BytecodeOp(Opcode.Call, GetFunctionIndex(call.Callee.FullName)));

            // If the result should be discarded, do it
            if (call.DiscardResult && call.Type != PrimitiveType.Void)
            {
                compiled.Bytecode.Add(new BytecodeOp(Opcode.PopDiscard, 0));
            }
        }

        private void CompilePrint(PrintExpression print, Function function, CompiledFunction compiled)
        {
            // Sanity check for parameter count
            if (print.Expressions.Count > 7)
                throw new ArgumentOutOfRangeException("print.Expressions.Count");

            // Compile each parameter
            foreach (var param in print.Expressions)
                CompileExpression(param, function, compiled);

            // Then emit the call
            // The opcode is based on the parameter count
            var opcode = (Opcode)((int)Opcode.CallI0 + print.Expressions.Count);
            compiled.Bytecode.Add(new BytecodeOp(opcode, (short)InternalFunction.Print));
        }

        private void CompileReturn(ReturnExpression ret, Function function, CompiledFunction compiled)
        {
            // Load the return value, unless we are returning void
            if (ret.Value != null)
                CompileExpression(ret.Value, function, compiled);

            // Exit the function
            compiled.Bytecode.Add(new BytecodeOp(Opcode.Return, 0));
        }

        private void EmitStore(LocalVariable target, CompiledFunction compiled)
        {
            compiled.Bytecode.Add(new BytecodeOp(Opcode.PopLocal, (short)target.LocalIndex));
        }

        private void EmitPush(LocalVariable local, CompiledFunction compiled)
        {
            var localIndex = (short)local.LocalIndex;
            compiled.Bytecode.Add(new BytecodeOp(Opcode.PushLocal, localIndex));
        }

        private void EmitPush(ConstantExpression constant, CompiledFunction compiled)
        {
            // Ensure that the constant exists in the constant table, then load it
            var constantIndex = GetConstant(constant.Value);
            compiled.Bytecode.Add(new BytecodeOp(Opcode.PushConst, constantIndex));
        }
        
        private short GetConstant(object value)
        {
            var constantName = string.Format(CultureInfo.InvariantCulture, "$literal_{0}", value).ToLowerInvariant();
            if (value is double)
                constantName += "r";

            if (_constants.ContainsKey(constantName))
            {
                return _constants[constantName];
            }
            else
            {
                // Add the constant
                var index = (short)_program.Constants.Count;
                var type = PrimitiveType.NoType;
                switch (value)
                {
                    case bool b: type = PrimitiveType.Bool; break;
                    case long l: type = PrimitiveType.Int; break;
                    case double d: type = PrimitiveType.Real; break;
                    default: throw new ArgumentOutOfRangeException("Unhandled type");
                }

                var newConst = new CompiledConstant(constantName, "", type, value)
                {
                    ConstantTableIndex = index
                };
                _program.Constants.Add(newConst);
                _constants.Add(constantName, index);

                return index;
            }
        }

        private short GetFunctionIndex(string fullName)
        {
            // Get the index if it exists
            // Add it if it does not
            if (_functionIndices.TryGetValue(fullName, out var index))
            {
                return index;
            }
            else
            {
                var count = (short)_functionIndices.Count;
                _functionIndices.Add(fullName, count);
                return count;
            }
        }
    }
}
