using System;
using System.Collections.Generic;
using System.Globalization;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Compiler
{
    /// <summary>
    /// Transforms parse trees into a compiled module.
    /// </summary>
    internal class SemanticCompiler
    {
        private List<ModuleSyntax> _modules;

        private Dictionary<string, CompiledConstant> _constants;
        private Dictionary<string, CompiledFunction> _functions;
        private List<CompilationDiagnostic> _diagnostics;
        private CompiledProgram _program;

        public SemanticCompiler(List<ModuleSyntax> modules)
        {
            _modules = modules;
        }

        private void LogError(DiagnosticCode error, TokenPosition position, string token = "", string expected = "")
        {
            _diagnostics.Add(new CompilationDiagnostic(error, true, token, expected, position));
            throw new CompilerException();
        }

        private void LogWarning(DiagnosticCode error, TokenPosition position, string token = "", string expected = "")
        {
            _diagnostics.Add(new CompilationDiagnostic(error, false, token, expected, position));
        }

        public (CompiledProgram program, List<CompilationDiagnostic> diagnostics) Compile()
        {
            _diagnostics = new List<CompilationDiagnostic>();
            _program = new CompiledProgram();
            _constants = new Dictionary<string, CompiledConstant>();
            _functions = new Dictionary<string, CompiledFunction>();

            try
            {
                // Fill the constant and function dictionaries
                foreach (var module in _modules)
                {
                    var prefix = string.IsNullOrEmpty(module.ModuleName) ? "" : module.ModuleName + ".";
                    foreach (var function in module.Functions)
                    {
                        var name = prefix + function.Name.ToLowerInvariant();

                        _functions.Add(name, new CompiledFunction(function, name, module.ModuleName, 
                            function.Visibility == Visibility.Private, false));
                    }
                    foreach (var constant in module.Constants)
                    {
                        var name = prefix + constant.Name.ToLowerInvariant();
                        var visibleToModules = "";
                        if (constant.Visibility == Visibility.Private)
                            visibleToModules = module.ModuleName;

                        _constants.Add(name, new CompiledConstant(name, visibleToModules, constant.Type, constant.Value));
                    }
                }

                // Compile each method in each module and their dependencies in a depth-first fashion
                // DFS is required because codegen emits function/constant indices, not names anymore
                // TODO: Mark the tree rooted in Main() and only generate code for it
                //       To make this easier, compile that first and only then the rest
                foreach (var kvp in _functions)
                {
                    CompileFunction(kvp.Value);
                }

                // Assert that there is a main method and set it so in the CompiledProgram
                if (_functions.ContainsKey("main"))
                {
                    var main = _functions["main"];
                    _program.MainFunctionIndex = main.FunctionTableIndex;
                    if (main.ParameterTypes.Count > 0)
                        LogError(DiagnosticCode.MainMayNotHaveParameters, main.SyntaxTree.Position);
                }
                else
                {
                    LogError(DiagnosticCode.NoMainFunction, _modules[0].Position);
                }

                return (_program, _diagnostics);
            }
            catch (CompilerException)
            {
                return (null, _diagnostics);
            }
        }

        private void CompileFunction(CompiledFunction function)
        {
            // Check that the function has not already been compiled
            if (function.IsCompiled)
                return;

            // Mark the function as compiled (already here to not blow up recursive calls)
            // and insert it into the method table
            function.IsCompiled = true;
            function.FunctionTableIndex = _program.Functions.Count;
            _program.Functions.Add(function);

            // Store the return value and parameter types
            // Generate local variables for parameters
            // The params are stored in first local slots evaluated from left to right
            function.ReturnType = function.SyntaxTree.ReturnType;
            foreach (var paramSyntax in function.SyntaxTree.Parameters)
            {
                function.ParameterTypes.Add(paramSyntax.Type);
                function.AddLocal(paramSyntax.Name.ToLowerInvariant(), paramSyntax.Type);
            }

            bool guaranteedReturn = false;
            bool unreachableWarningEmitted = false;

            // For each statement: compile the statement
            BlockSyntax block = function.SyntaxTree.CodeBlock;
            CompileBlock(block, function, function.Bytecode, ref guaranteedReturn, ref unreachableWarningEmitted);

            if (!guaranteedReturn)
            {
                if (function.ReturnType != PrimitiveType.Void)
                {
                    // Non-void functions must be guaranteed to return
                    LogError(DiagnosticCode.ReturnNotGuaranteed, function.SyntaxTree.Position, function.SyntaxTree.Name);
                }
                else
                {
                    // Void functions may return implicitly
                    function.Bytecode.Add(new BytecodeOp(Opcode.Return, 0));
                }
            }
        }

        private void CompileBlock(BlockSyntax block, CompiledFunction function, List<BytecodeOp> bytecode,
            ref bool guaranteedReturn, ref bool unreachableWarningEmitted)
        {
            // Store the names defined in this scope and remove them at the end of this block
            var localsDefinedInThisBlock = new HashSet<string>();

            foreach (var statement in block.Statements)
            {
                // Unreachable code warning
                if (guaranteedReturn && !unreachableWarningEmitted)
                {
                    LogWarning(DiagnosticCode.UnreachableCode, statement.Position);
                    unreachableWarningEmitted = true;
                }

                // The actual compilation
                switch (statement)
                {
                    case AssignmentSyntax assign:
                        {
                            var fullName = assign.Target.ToLowerInvariant();
                            if (!function._localMap.ContainsKey(fullName))
                            {
                                // There is a special diagnostic for trying to assign to a global const
                                if (_constants.ContainsKey(fullName))
                                    LogError(DiagnosticCode.MayNotAssignToConst, assign.Position, assign.Target);
                                else
                                    LogError(DiagnosticCode.NameNotFound, assign.Position, assign.Target);
                            }

                            var localIndex = function._localMap[fullName];
                            var localType = function.Locals[localIndex].type;

                            // Push the expression result and pop it to a local
                            CompileExpression(assign.Expression, function, bytecode, localType);
                            bytecode.Add(new BytecodeOp(Opcode.PopLocal, localIndex));
                            break;
                        }
                    case FunctionCallStatementSyntax call:
                        {
                            // Perform the call and discard the result
                            var returnType = CompileExpression(call.Expression, function, bytecode, PrimitiveType.NoType);
                            if (returnType != PrimitiveType.Void)
                                bytecode.Add(new BytecodeOp(Opcode.PopDiscard, 0));
                            break;
                        }
                    case IfSyntax cond:
                        {
                            // The if statement is emitted as follows:
                            //   - Load condition expression (? ops)
                            //   - If false, jump to the end of 'then' block (1 op)
                            //   - The 'then' block (? ops)
                            //   - If there is an 'else' block, a jump over it (0-1 ops)
                            //   - The optional else block (0/? ops)

                            // Emit the condition check
                            CompileExpression(cond.Condition, function, bytecode, PrimitiveType.Bool);

                            // Emit the 'then' block to a temporary buffer
                            var thenBytecode = new List<BytecodeOp>();
                            bool thenReturnsAlways = false, discard = false;
                            CompileBlock(cond.ThenBlock, function, thenBytecode, ref thenReturnsAlways, ref discard);

                            // Emit the 'else' block as well
                            var elseBytecode = new List<BytecodeOp>();
                            bool elseReturnsAlways = false;
                            CompileBlock(cond.ElseBlock, function, elseBytecode, ref elseReturnsAlways, ref discard);
                            var hasElse = elseBytecode.Count > 0;

                            // Now emit the final bytecode
                            // Jump to end of 'then'
                            bytecode.Add(new BytecodeOp(Opcode.JumpFalse, 
                                (short)(thenBytecode.Count + 1 + (hasElse ? 1 : 0))));
                            // 'Then' block
                            bytecode.AddRange(thenBytecode);
                            // If there is an 'else' block, jump over it
                            if (hasElse)
                                bytecode.Add(new BytecodeOp(Opcode.Jump, (short)(thenBytecode.Count + elseBytecode.Count - 1)));
                            // 'Else' block
                            bytecode.AddRange(elseBytecode);

                            if (thenReturnsAlways && elseReturnsAlways)
                                guaranteedReturn = true;
                            break;
                        }
                    case ReturnSyntax ret:
                        {
                            // Push the result (unless void), then return
                            if (ret.Expression != null)
                                CompileExpression(ret.Expression, function, bytecode, function.ReturnType);
                            bytecode.Add(new BytecodeOp(Opcode.Return, 0));
                            guaranteedReturn = true;
                            break;
                        }
                    case VariableDeclarationSyntax decl:
                        {
                            var fullName = decl.Name.ToLowerInvariant();
                            if (function._localMap.ContainsKey(fullName))
                                LogError(DiagnosticCode.NameAlreadyDefined, decl.Position, decl.Name);
                            var localIndex = function.AddLocal(fullName, decl.Type);
                            localsDefinedInThisBlock.Add(fullName);

                            // Emit the initial store
                            CompileExpression(decl.InitialValue, function, bytecode, decl.Type);
                            bytecode.Add(new BytecodeOp(Opcode.PopLocal, localIndex));
                            break;
                        }
                    case WhileSyntax loop:
                        {
                            // The loop is emitted as follows:
                            //   - Load condition expression (? ops)
                            //   - If false, jump to next statement (1 op)
                            //   - Loop body (? ops)
                            //   - Jump to condition (1 op)

                            // Emit the condition check
                            var conditionPos = bytecode.Count;
                            CompileExpression(loop.Condition, function, bytecode, PrimitiveType.Bool);
                            var conditionSize = bytecode.Count - conditionPos;

                            // Emit the loop to a temporary buffer, because the jump instruction depends on its size
                            var bodyBytecode = new List<BytecodeOp>();
                            bool discard = false, discard2 = true;
                            CompileBlock(loop.CodeBlock, function, bodyBytecode, ref discard, ref discard2);

                            // Emit the condition check jump
                            bytecode.Add(new BytecodeOp(Opcode.JumpFalse, (short)(bodyBytecode.Count + 2)));

                            // Then the loop body
                            bytecode.AddRange(bodyBytecode);
                            bytecode.Add(new BytecodeOp(Opcode.Jump, (short)-(conditionSize + bodyBytecode.Count + 1)));
                            break;
                        }
                    default:
                        throw new NotImplementedException();
                }
            }

            // Now remove the names that go out of scope
            foreach (var name in localsDefinedInThisBlock)
            {
                function._localMap.Remove(name);
            }
        }

        private PrimitiveType CompileExpression(ExpressionSyntax expression, CompiledFunction function, 
            List<BytecodeOp> target, PrimitiveType expectedType)
        {
            bool doTypeChecks = expectedType != PrimitiveType.NoType;

            switch (expression)
            {
                case LiteralSyntax literal:
                    {
                        var finalLiteral = literal;
                        // Make sure that the literal is of the right type
                        if (doTypeChecks && literal.Type != expectedType)
                        {
                            // However, promote ints to real numbers
                            if (expectedType == PrimitiveType.Real && literal.Type == PrimitiveType.Int)
                            {
                                finalLiteral = LiteralSyntax.CreateRealLiteral(literal.Position, (long)literal.Value);
                            }
                            else
                            {
                                LogError(DiagnosticCode.WrongType, literal.Position, literal.Type.ToString(), expectedType.ToString());
                            }
                        }

                        // Push the literal onto the stack
                        // There is no "push literal" op, so it is implemented using a compiler-generated constant
                        var index = (short)GetConstantForLiteral(finalLiteral);
                        target.Add(new BytecodeOp(Opcode.PushConst, index));

                        return finalLiteral.Type;
                    }
                case IdentifierSyntax name:
                    {
                        if (function._localMap.ContainsKey(name.Name.ToLowerInvariant()))
                        {
                            // Push the local onto the execution stack
                            var index = function._localMap[name.Name.ToLowerInvariant()];
                            (_, var type) = function.Locals[index];
                            if (doTypeChecks && type != expectedType)
                            {
                                LogError(DiagnosticCode.WrongType, name.Position, type.ToString(), expectedType.ToString());
                            }
                            target.Add(new BytecodeOp(Opcode.PushLocal, index));

                            return type;
                        }
                        else
                        {
                            // Push the constant onto the execution stack
                            var index = (short)GetConstant(name.Name, function.ModuleName, name.Position);
                            var type = _program.Constants[index].Type;
                            if (doTypeChecks && type != expectedType)
                            {
                                LogError(DiagnosticCode.WrongType, name.Position, type.ToString(), expectedType.ToString());
                            }
                            target.Add(new BytecodeOp(Opcode.PushConst, index));

                            return type;
                        }
                    }
                case FunctionCallSyntax call:
                    {
                        if (InternalFunctions.Functions.ContainsKey(call.FunctionName.ToLowerInvariant()))
                        {
                            // Compile the parameter expressions (left to right) and store their types
                            // The expectedType check cannot be used here because of function overloading
                            var paramTypes = new List<PrimitiveType>();
                            foreach (var param in call.Parameters)
                            {
                                paramTypes.Add(CompileExpression(param, function, target, PrimitiveType.NoType));
                            }

                            return EmitInternalCall(call, function, target, expectedType, paramTypes);
                        }
                        else
                        {
                            // EmitFunctionCall performs parameter compilation and type checking
                            return EmitFunctionCall(call, function, target, expectedType);
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private PrimitiveType EmitFunctionCall(FunctionCallSyntax call, CompiledFunction function, 
            List<BytecodeOp> target, PrimitiveType expectedReturnType)
        {
            var index = (short)GetFunction(call.FunctionName, function.ModuleName, call.Position);
            var calledFunction = _program.Functions[index];

            // Check the return type
            // If NoType is expected, the result will be discarded and there is no need to check
            if (expectedReturnType != PrimitiveType.NoType &&
                calledFunction.ReturnType != expectedReturnType)
            {
                LogError(DiagnosticCode.WrongType, call.Position, calledFunction.ReturnType.ToString(), expectedReturnType.ToString());
            }

            // Make sure that there is correct number of parameters and that they are of correct type
            // This also compiles the parameter expressions
            AssertParameterCount(call.Parameters.Count, calledFunction.ParameterTypes.Count, call.Position);
            for (int i = 0; i < call.Parameters.Count; i++)
            {
                CompileExpression(call.Parameters[i], function, target, calledFunction.ParameterTypes[i]);
            }

            // Perform the call, the result will be pushed onto the stack
            target.Add(new BytecodeOp(Opcode.Call, index));

            return calledFunction.ReturnType;
        }

        private PrimitiveType EmitInternalCall(FunctionCallSyntax call, CompiledFunction function,
            List<BytecodeOp> target, PrimitiveType expectedType, List<PrimitiveType> paramTypes)
        {
            if (InternalFunctions.Functions.TryGetValue(call.FunctionName.ToLowerInvariant(), out var calledFunction))
            {
                // Is an internal function, now perform type checking for parameters...
                AssertParameterCount(paramTypes.Count, calledFunction.MinParameters, calledFunction.MaxParameters, call.Position);
                var paramTypeInfo = PrimitiveType.NoType;
                switch (calledFunction.ParamConstraint)
                {
                    case ParameterConstraint.AnyNumericType:
                        paramTypeInfo = AssertAnyNumericType(paramTypes, call.Parameters[0].Position);
                        break;
                    case ParameterConstraint.BoolOrInt:
                        paramTypeInfo = AssertBoolOrInt(paramTypes, call.Parameters[0].Position);
                        break;
                    case ParameterConstraint.Int:
                        for (int i = 0; i < paramTypes.Count; i++)
                        {
                            if (paramTypes[i] != PrimitiveType.Int)
                                LogError(DiagnosticCode.WrongType, call.Parameters[i].Position, 
                                    paramTypes[i].ToString(), PrimitiveType.Int.ToString());
                        }
                        break;
                    case ParameterConstraint.None:
                        break;
                    case ParameterConstraint.SameType:
                        paramTypeInfo = AssertSameType(paramTypes, call.Parameters[0].Position);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                // ...and for the return type
                var returnType = PrimitiveType.NoType;
                switch (calledFunction.ReturnType)
                {
                    case InternalReturnType.RealOrInt:
                    case InternalReturnType.SameAsParameter:
                        returnType = paramTypeInfo;
                        break;
                    case InternalReturnType.Bool:
                        returnType = PrimitiveType.Bool;
                        break;
                    case InternalReturnType.Int:
                        returnType = PrimitiveType.Int;
                        break;
                    case InternalReturnType.Real:
                        returnType = PrimitiveType.Real;
                        break;
                    case InternalReturnType.Void:
                        returnType = PrimitiveType.Void;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (expectedType != PrimitiveType.NoType && returnType != expectedType)
                    LogError(DiagnosticCode.WrongType, call.Position, returnType.ToString(), expectedType.ToString());

                // There are instructions for 0 to 7 parameters
                if (paramTypes.Count > 7)
                    LogError(DiagnosticCode.TooManyParameters, call.Position);

                // Emit the call
                target.Add(new BytecodeOp((Opcode)((short)Opcode.CallI0 + paramTypes.Count), (short)calledFunction.Index));
                return returnType;
            }
            else
            {
                // Not an internal function
                return PrimitiveType.NoType;
            }
        }

        private void AssertParameterCount(int count, int expected, TokenPosition position)
        {
            AssertParameterCount(count, expected, expected, position);
        }

        private void AssertParameterCount(int count, int min, int max, TokenPosition position)
        {
            if (count < min)
            {
                LogError(DiagnosticCode.NotEnoughParameters, position, count.ToString(), min.ToString());
            }
            else if (count > max)
            {
                LogError(DiagnosticCode.TooManyParameters, position, count.ToString(), max.ToString());
            }
        }

        private PrimitiveType AssertAnyNumericType(List<PrimitiveType> paramList, TokenPosition firstParamPos)
        {
            // Returns Real if any parameter is real, Int otherwise, and checks that all parameters are numbers
            var finalType = PrimitiveType.Int;
            foreach (var type in paramList)
            {
                if (type == PrimitiveType.Int)
                    continue;
                else if (type == PrimitiveType.Real)
                    finalType = PrimitiveType.Real;
                else
                    LogError(DiagnosticCode.WrongType, firstParamPos, type.ToString(), "Int|Real"); // TODO: Better positioning
            }

            return finalType;
        }

        private PrimitiveType AssertSameType(List<PrimitiveType> paramList, TokenPosition firstParamPos)
        {
            var firstType = paramList[0];

            foreach (var type in paramList)
            {
                if (type != firstType)
                    LogError(DiagnosticCode.ParamsMustBeSameType, firstParamPos, type.ToString(), firstType.ToString());
            }

            return firstType;
        }

        private PrimitiveType AssertBoolOrInt(List<PrimitiveType> paramList, TokenPosition firstParamPos)
        {
            var firstType = paramList[0];
            if (firstType != PrimitiveType.Int && firstType != PrimitiveType.Bool)
                LogError(DiagnosticCode.WrongType, firstParamPos, firstType.ToString(), "Bool|Int");

            foreach (var type in paramList)
            {
                if (type != firstType)
                    LogError(DiagnosticCode.ParamsMustBeSameType, firstParamPos, type.ToString(), firstType.ToString());
            }

            return firstType;
        }

        private int GetConstant(string name, string moduleName, TokenPosition refPosition)
        {
            var fullName = name.ToLowerInvariant();
            // Promote unqualified names to full names if needed
            if (!fullName.Contains(".") && !string.IsNullOrEmpty(moduleName))
            {
                fullName = moduleName + "." + fullName;
            }

            if (_constants.ContainsKey(fullName))
            {
                var compiledConstant = _constants[fullName];
                if (!string.IsNullOrEmpty(compiledConstant.VisibleToModules) &&
                    compiledConstant.VisibleToModules != moduleName)
                {
                    LogError(DiagnosticCode.NameIsPrivate, refPosition, name);
                }

                if (compiledConstant.ConstantTableIndex == -1)
                {
                    // Add the constant to the table
                    compiledConstant.ConstantTableIndex = _program.Constants.Count;
                    _program.Constants.Add(compiledConstant);
                }

                return compiledConstant.ConstantTableIndex;
            }
            else
            {
                LogError(DiagnosticCode.NameNotFound, refPosition, name);
                return -1; // Not actually reached
            }
        }

        private int GetConstantForLiteral(LiteralSyntax literal)
        {
            var constantName = string.Format(CultureInfo.InvariantCulture, "$literal_{0}", literal.Value).ToLowerInvariant();
            if (literal.Type == PrimitiveType.Real)
                constantName += "r";

            if (_constants.ContainsKey(constantName))
            {
                return _constants[constantName].ConstantTableIndex;
            }
            else
            {
                // Add the constant
                var index = _program.Constants.Count;
                var newConst = new CompiledConstant(constantName, "", literal.Type, literal.Value) {
                    ConstantTableIndex = index
                };
                _program.Constants.Add(newConst);
                _constants.Add(constantName, newConst);

                return index;
            }
        }

        private int GetFunction(string functionName, string moduleName, TokenPosition callPosition)
        {
            var fullName = functionName.ToLowerInvariant();
            // Promote unqualified names to full names if needed
            if (!fullName.Contains(".") && !string.IsNullOrEmpty(moduleName))
            {
                fullName = moduleName + "." + fullName;
            }

            if (_functions.ContainsKey(fullName))
            {
                var func = _functions[fullName];
                if (func.IsPrivate && func.ModuleName != moduleName)
                {
                    LogError(DiagnosticCode.NameIsPrivate, callPosition, functionName);
                }

                CompileFunction(func);

                return func.FunctionTableIndex;
            }
            else
            {
                LogError(DiagnosticCode.NameNotFound, callPosition, functionName);
                return -1; // Not actually reached
            }
        }
    }
}
