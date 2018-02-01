using System;
using System.Collections.Generic;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Base class for compiler expression tree nodes.
    /// </summary>
    internal abstract class Expression
    {
        /// <summary>
        /// The local where the expression result is stored.
        /// This may be null if the expression does not store a result.
        /// </summary>
        public LocalVariable Store;

        /// <summary>
        /// The result type of this expression.
        /// </summary>
        public PrimitiveType Type = PrimitiveType.Void;

        protected virtual void SetStore(LocalVariable newStore, OptimizingCompiler compiler, TokenPosition position = default)
        {
            // Check the type
            if (newStore != null && Type != newStore.Type)
                compiler.LogError(DiagnosticCode.WrongType, position, Type.ToString(), newStore.Type.ToString());

            // Set the store
            if (Store != null)
                Store.AssignmentCount--;
            Store = newStore;
            if (Store != null)
                Store.AssignmentCount++;
        }

        /// <summary>
        /// Tries to perform constant folding on this expression.
        /// </summary>
        /// <param name="compiler">The compiler instance. This is used for getting methods, emitting errors, etc.</param>
        /// <returns>The resulting expression, which may be the same as this one.</returns>
        public abstract Expression Fold(OptimizingCompiler compiler);

        /// <summary>
        /// If a local is assigned and used exactly once, and the use is immediately adjacent to the assignment,
        /// move the local expression directly to the use site.
        /// This improves Peisik bytecode quality.
        /// On other targets this might not be applied.
        /// </summary>
        public virtual void FoldSingleUseLocals()
        {
        }

        /// <summary>
        /// If this expression uses the local assigned to in <paramref name="assignment"/>,
        /// replace the local load with the assignment expression.
        /// </summary>
        /// <returns>
        /// A new version of this expression if the substitution was performed.
        /// Null otherwise.
        /// </returns>
        internal virtual Expression TryInlineLocalAssignment(Expression assignment)
        {
            return null;
        }

        public static Expression FromSyntax(SyntaxNode syntax, Function function,
            OptimizingCompiler compiler, LocalVariableContext localContext)
        {
            if (syntax is AssignmentSyntax assign)
            {
                // First check for the 'assignment to const' error case
                if (compiler.TryGetConstant(assign.Target, function.ModulePrefix, out _))
                {
                    compiler.LogError(DiagnosticCode.MayNotAssignToConst, assign.Position, assign.Target);
                }

                // Then do the actual assignment (with implicit error checks)
                var local = localContext.GetLocal(assign.Target, assign.Position);
                var expr = FromSyntax(assign.Expression, function, compiler, localContext);
                
                // Promote integer LITERALS to reals (parser deficiency)
                if (expr.Type == PrimitiveType.Int && local.Type == PrimitiveType.Real
                    && assign.Expression is LiteralSyntax initialValue)
                {
                    expr = new ConstantExpression(Convert.ToDouble(initialValue.Value), compiler);
                }

                expr.SetStore(local, compiler, assign.Position);

                return expr;
            }
            else if (syntax is BlockSyntax block)
            {
                return new SequenceExpression(block, function, compiler, localContext);
            }
            else if (syntax is FunctionCallSyntax || syntax is FunctionCallStatementSyntax)
            {
                FunctionCallSyntax call;
                var discardResult = false;
                if (syntax is FunctionCallSyntax)
                    call = (FunctionCallSyntax)syntax;
                else
                {
                    call = ((FunctionCallStatementSyntax)syntax).Expression;
                    discardResult = true;
                }

                if (InternalFunctions.Functions.TryGetValue(call.FunctionName.ToLowerInvariant(), out var internalFunc))
                {
                    // The function is an internal one
                    // This logic is quite complicated
                    return MakeInternalCall(function, compiler, localContext, call, internalFunc);
                }
                else if (compiler.TryGetFunction(call.FunctionName, function.ModulePrefix, out var callee))
                {
                    // Check that the parameter counts match
                    AssertFunctionCallParameterCount(compiler, call, call.Parameters.Count,
                        callee.ParameterTypes.Count, callee.ParameterTypes.Count);

                    var parameters = new List<Expression>();
                    for (var i = 0; i < call.Parameters.Count; i++)
                    {
                        // Check that the expected and actual types match
                        var paramExpr = FromSyntax(call.Parameters[i], function, compiler, localContext);
                        if (paramExpr.Type != callee.ParameterTypes[i])
                        {
                            // However, there is one exception: int LITERALS may be interpreted as reals
                            if (paramExpr.Type == PrimitiveType.Int &&
                                callee.ParameterTypes[i] == PrimitiveType.Real &&
                                call.Parameters[i] is LiteralSyntax literal)
                            {
                                paramExpr = new ConstantExpression(Convert.ToDouble(literal.Value), compiler);
                            }
                            else
                            {
                                compiler.LogError(DiagnosticCode.WrongType, call.Parameters[i].Position,
                                    paramExpr.Type.ToString(), callee.ParameterTypes[i].ToString());
                            }
                        }

                        parameters.Add(paramExpr);
                    }

                    return new FunctionCallExpression(callee, parameters, discardResult, compiler);
                }
                else
                {
                    compiler.LogError(DiagnosticCode.NameNotFound, call.Position, call.FunctionName);
                    return null; // Unreached
                }
            }
            else if (syntax is IdentifierSyntax identifier)
            {
                // First try loading a constant
                // If that does not work, it must be a local
                if (compiler.TryGetConstant(identifier.Name, function.ModulePrefix, out var constValue))
                {
                    return new ConstantExpression(constValue, compiler);
                }
                return new LocalLoadExpression(localContext.GetLocal(identifier.Name, identifier.Position), compiler);
            }
            else if (syntax is IfSyntax conditional)
            {
                // Compile the boolean condition
                var condition = FromSyntax(conditional.Condition, function, compiler, localContext);
                if (condition.Type != PrimitiveType.Bool)
                    compiler.LogError(DiagnosticCode.WrongType, conditional.Condition.Position, condition.Type.ToString(), "Bool");

                // Compile the executable blocks
                var thenExpr = FromSyntax(conditional.ThenBlock, function, compiler, localContext);
                var elseExpr = FromSyntax(conditional.ElseBlock, function, compiler, localContext);

                return new IfExpression(condition, thenExpr, elseExpr);
            }
            else if (syntax is LiteralSyntax literal)
            {
                return new ConstantExpression(literal, compiler);
            }
            else if (syntax is ReturnSyntax ret)
            {
                // Return void
                if (ret.Expression == null)
                    return new ReturnExpression(null);

                // Do the return type check here instead of complicating ReturnExpression
                var returnValue = FromSyntax(ret.Expression, function, compiler, localContext);
                if (returnValue.Type != function.ResultValue.Type)
                    compiler.LogError(DiagnosticCode.WrongType, ret.Position,
                        returnValue.Type.ToString(), function.ResultValue.Type.ToString());

                return new ReturnExpression(returnValue);
            }
            else if (syntax is VariableDeclarationSyntax decl)
            {
                var local = localContext.AddLocal(decl.Name, decl.Type, decl.Position);
                var result = FromSyntax(decl.InitialValue, function, compiler, localContext);

                // Promote integer LITERALS to reals (parser deficiency)
                if (result.Type == PrimitiveType.Int && local.Type == PrimitiveType.Real
                    && decl.InitialValue is LiteralSyntax initialValue)
                {
                    result = new ConstantExpression(Convert.ToDouble(initialValue.Value), compiler);
                }

                result.SetStore(local, compiler, decl.Position);
                return result;
            }
            else if (syntax is WhileSyntax loop)
            {
                var condition = FromSyntax(loop.Condition, function, compiler, localContext);
                if (condition.Type != PrimitiveType.Bool)
                    compiler.LogError(DiagnosticCode.WrongType, loop.Condition.Position, condition.Type.ToString(), "Bool");

                var body = FromSyntax(loop.CodeBlock, function, compiler, localContext);

                return new WhileExpression(condition, body);
            }
            else
            {
                throw new NotImplementedException($"Unimplemented syntax {syntax}");
            }
        }

        private static Expression MakeInternalCall(Function function, OptimizingCompiler compiler,
            LocalVariableContext localContext, FunctionCallSyntax call, InternalFunctionDefinition internalFunc)
        {
            // Check parameter count
            AssertFunctionCallParameterCount(compiler, call, call.Parameters.Count,
                internalFunc.MinParameters, internalFunc.MaxParameters);

            // Check parameter types
            var parameters = new List<Expression>();
            var firstType = PrimitiveType.NoType;
            for (var i = 0; i < call.Parameters.Count; i++)
            {
                var paramExpr = FromSyntax(call.Parameters[i], function, compiler, localContext);
                parameters.Add(paramExpr);

                if (i == 0)
                    firstType = paramExpr.Type;

                switch (internalFunc.ParamConstraint)
                {
                    case ParameterConstraint.AnyNumericType:
                        if (paramExpr.Type != PrimitiveType.Int && paramExpr.Type != PrimitiveType.Real)
                        {
                            compiler.LogError(DiagnosticCode.WrongType, call.Parameters[i].Position,
                                paramExpr.Type.ToString(), "Int|Real");
                        }
                        break;
                    case ParameterConstraint.BoolOrInt:
                        if (paramExpr.Type != PrimitiveType.Bool && paramExpr.Type != PrimitiveType.Int)
                        {
                            compiler.LogError(DiagnosticCode.WrongType, call.Parameters[i].Position,
                                paramExpr.Type.ToString(), "Bool|Int");
                        }
                        else if (paramExpr.Type != firstType)
                        {
                            compiler.LogError(DiagnosticCode.ParamsMustBeSameType, call.Parameters[i].Position,
                                paramExpr.Type.ToString(), firstType.ToString());
                        }
                        break;
                    case ParameterConstraint.Int:
                        if (paramExpr.Type != PrimitiveType.Int)
                        {
                            compiler.LogError(DiagnosticCode.WrongType, call.Parameters[i].Position,
                                paramExpr.Type.ToString(), "Int");
                        }
                        break;
                    case ParameterConstraint.SameType:
                        if (paramExpr.Type != firstType)
                        {
                            compiler.LogError(DiagnosticCode.ParamsMustBeSameType, call.Parameters[i].Position,
                                paramExpr.Type.ToString(), firstType.ToString());
                        }
                        break;
                    case ParameterConstraint.None:
                        if (paramExpr.Type == PrimitiveType.Void)
                        {
                            compiler.LogError(DiagnosticCode.WrongType, call.Parameters[i].Position,
                                paramExpr.Type.ToString(), "Bool|Int|Real");
                        }
                        break;
                    default:
                        throw new NotImplementedException("Unimplemented ParameterConstraint");
                }
            }

            // Emit the right type of expression based on the function and parameter count
            if (internalFunc.Index == InternalFunction.Print)
            {
                // Print takes 0 to 7 parameters of any type and is therefore a special node
                return new PrintExpression(parameters);
            }
            else if (internalFunc.Index == InternalFunction.FailFast)
            {
                return new FailFastExpression();
            }
            else if (parameters.Count == 1)
            {
                return new UnaryExpression(internalFunc, parameters[0]);
            }
            else if (parameters.Count == 2)
            {
                return new BinaryExpression(internalFunc, parameters[0], parameters[1]);
            }
            else
            {
                throw new NotImplementedException("Unimplemented internal function");
            }
        }

        private static void AssertFunctionCallParameterCount(OptimizingCompiler compiler, FunctionCallSyntax call, int paramCount, int minParams, int maxParams)
        {
            if (paramCount < minParams)
                compiler.LogError(DiagnosticCode.NotEnoughParameters, call.Position,
                    paramCount.ToString(), minParams.ToString());
            if (paramCount > maxParams)
                compiler.LogError(DiagnosticCode.TooManyParameters, call.Position,
                    paramCount.ToString(), maxParams.ToString());
        }
    }
}
