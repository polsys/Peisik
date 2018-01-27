using System;
using System.Collections.Generic;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents an ordered sequence of expressions.
    /// </summary>
    internal class SequenceExpression : Expression
    {
        internal List<Expression> Expressions = new List<Expression>();

        /// <summary>
        /// Only for testing.
        /// </summary>
        internal SequenceExpression()
        {
        }

        public SequenceExpression(BlockSyntax syntax, Function function,
            OptimizingCompiler compiler, LocalVariableContext localContext)
        {
            foreach (var statement in syntax.Statements)
            {
                Expressions.Add(FromSyntax(statement, function, compiler, localContext));
            }
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            // TODO: If this sequence has only 1 item, return it

            // CODE SMELL: And here we violate our kind-of qimmutability...
            for (var i = 0; i < Expressions.Count; i++)
            {
                Expressions[i] = Expressions[i].Fold(compiler);
            }
            return this;
        }

        public override void FoldSingleUseLocals()
        {
            // Fold local assignments where the local is only used once.
            // This is a major code quality improvement.
            // TODO: Currently this is restricted to adjacent expressions.
            //       This is not necessary if reordering can be proven to not change behavior.
            
            for (var i = 0; i < Expressions.Count - 1; i++)
            {
                if (Expressions[i].Store != null && Expressions[i].Store.UseCount == 1)
                {
                    var newExpr = Expressions[i + 1].TryInlineLocalAssignment(Expressions[i]);
                    if (newExpr != null)
                    {
                        // Decrement the reference count of the old local
                        Expressions[i].Store.AssignmentCount--;
                        Expressions[i].Store.UseCount--;
                        Expressions[i].Store = null;

                        // Update the use
                        newExpr.Store = Expressions[i + 1].Store;
                        Expressions[i + 1] = newExpr;
                        
                        // Remove the old store operation
                        Expressions.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }
}
