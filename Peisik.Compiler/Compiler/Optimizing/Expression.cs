using System;
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

        protected void SetStore(LocalVariable newStore)
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
            if (syntax is BlockSyntax block)
            {
                return new SequenceExpression(block, function, compiler, localContext);
            }
            else if (syntax is IdentifierSyntax identifier)
            {
                // First try loading a constant
                // If that does not work, it must be a local
                if (compiler.TryGetConstant(identifier.Name, out var constValue))
                {
                    return new ConstantExpression(constValue);
                }
                return new LocalLoadExpression(localContext.GetLocal(identifier.Name, identifier.Position));
            }
            else if (syntax is LiteralSyntax literal)
            {
                return new ConstantExpression(literal);
            }
            else if (syntax is ReturnSyntax ret)
            {
                return new ReturnExpression(FromSyntax(ret.Expression, function, compiler, localContext));
            }
            else if (syntax is VariableDeclarationSyntax decl)
            {
                var local = localContext.AddLocal(decl.Name, decl.Type, decl.Position);
                var result = FromSyntax(decl.InitialValue, function, compiler, localContext);
                result.SetStore(local);
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

        public ConstantExpression(object value, LocalVariable store = null)
        {
            Value = value;
            SetStore(store);
        }

        public ConstantExpression(LiteralSyntax expression, LocalVariable store = null)
            : this(expression.Value, store)
        {
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            return this;
        }
    }

    /// <summary>
    /// Represents a local variable load.
    /// </summary>
    internal class LocalLoadExpression : Expression
    {
        public LocalVariable Local;

        public LocalLoadExpression(LocalVariable local, LocalVariable store = null)
        {
            Local = local;
            Local.UseCount++;

            SetStore(store);
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
