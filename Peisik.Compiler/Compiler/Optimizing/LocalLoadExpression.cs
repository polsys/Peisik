namespace Polsys.Peisik.Compiler.Optimizing
{
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
            base.SetStore(newStore, compiler, position);
        }
    }
}
