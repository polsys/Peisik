﻿using System;
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
                // First check for the assignment to const error case
                if (compiler.TryGetConstant(assign.Target, function.ModulePrefix, out _))
                {
                    compiler.LogError(DiagnosticCode.MayNotAssignToConst, assign.Position, assign.Target);
                }

                // Then do the actual assignment (with implicit error checks)
                var local = localContext.GetLocal(assign.Target, assign.Position);
                var expr = FromSyntax(assign.Expression, function, compiler, localContext);
                expr.SetStore(local, compiler, assign.Position);

                return expr;
            }
            else if (syntax is BlockSyntax block)
            {
                return new SequenceExpression(block, function, compiler, localContext);
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
            else if (syntax is LiteralSyntax literal)
            {
                return new ConstantExpression(literal, compiler);
            }
            else if (syntax is ReturnSyntax ret)
            {
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
                result.SetStore(local, compiler, decl.Position);
                return result;
            }
            else
            {
                throw new NotImplementedException("Unimplemented syntax");
            }
        }
    }

    /// <summary>
    /// Represents a compile-time constant.
    /// </summary>
    internal class ConstantExpression : Expression
    {
        public object Value;

        public ConstantExpression(object value, OptimizingCompiler compiler, LocalVariable store = null, TokenPosition position = default)
        {
            // Store the type
            switch (value)
            {
                case bool b:
                    Type = PrimitiveType.Bool;
                    break;
                case long l:
                    Type = PrimitiveType.Int;
                    break;
                case double d:
                    Type = PrimitiveType.Real;
                    break;
                default:
                    throw new ArgumentException("Unknown constant type");
            }

            Value = value;
            SetStore(store, compiler, position);
        }

        public ConstantExpression(LiteralSyntax expression, OptimizingCompiler compiler, LocalVariable store = null)
            : this(expression.Value, compiler, store, expression.Position)
        {
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            return this;
        }

        protected override void SetStore(LocalVariable newStore, OptimizingCompiler compiler, TokenPosition position = default)
        {
            // Check the type
            if (newStore != null && Type != newStore.Type)
                compiler.LogError(DiagnosticCode.WrongType, position, Type.ToString(), newStore.Type.ToString());

            base.SetStore(newStore, compiler, position);
        }
    }

    /// <summary>
    /// Represents a local variable load.
    /// </summary>
    internal class LocalLoadExpression : Expression
    {
        public LocalVariable Local;

        public LocalLoadExpression(LocalVariable local, OptimizingCompiler compiler, LocalVariable store = null)
        {
            Local = local;
            Local.UseCount++;
            Type = local.Type;

            SetStore(store, compiler);
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            return this;
        }

        internal override Expression TryInlineLocalAssignment(Expression assignment)
        {
            if (Local == assignment.Store)
            {
                return assignment;
            }
            return null;
        }

        protected override void SetStore(LocalVariable newStore, OptimizingCompiler compiler, TokenPosition position = default)
        {
            if (newStore != null && newStore.Type != Type)
                compiler.LogError(DiagnosticCode.WrongType, position, Type.ToString(), newStore.Type.ToString());

            base.SetStore(newStore, compiler, position);
        }
    }

    /// <summary>
    /// Represents a point where the function will return to its caller.
    /// </summary>
    /// <remarks>
    /// If the containing function is inlined, this will become a local store.
    /// The variables are renamed so that the result variable becomes a local in the inlining function.
    /// </remarks>
    internal class ReturnExpression : Expression
    {
        /// <summary>
        /// The return value, which may be null.
        /// </summary>
        public Expression Value;

        public ReturnExpression(Expression returnValue)
        {
            Value = returnValue;
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            return this;
        }

        internal override Expression TryInlineLocalAssignment(Expression assignment)
        {
            if (Value is LocalLoadExpression load && load.Local == assignment.Store)
            {
                return new ReturnExpression(assignment);
            }
            return null;
        }
    }
}